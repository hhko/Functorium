---
name: application-develop
description: "Functorium 프레임워크 기반 애플리케이션 레이어(CQRS, Usecase, Port)를 설계하고 구현합니다. '유스케이스 구현', 'Command 만들어줘', 'Query 추가', 'CQRS 설계', '이벤트 핸들러 추가' 등의 요청에 반응합니다."
---

## 선행 조건

`domain-develop` 스킬에서 생성한 `domain/03-implementation-results.md`를 읽어 도메인 모델(Aggregate, VO, Event, Specification)을 확인합니다.

## 후속 스킬

```
project-spec → architecture-design → domain-develop → **application-develop** → adapter-develop → test-develop
```

## 개요

Functorium 프레임워크 기반 애플리케이션 레이어 개발 가이드입니다.
CQRS 패턴으로 Command/Query를 분리하고, FinT 모나드 합성으로 유스케이스를 구현합니다.

## 워크플로우

### Phase 1: 워크플로우 -> 유스케이스 분해

사용자에게 다음을 질문합니다:
- 어떤 워크플로우(비즈니스 흐름)를 구현할까요?
- 쓰기 연산(Command)인가 읽기 연산(Query)인가?
- 이벤트에 반응하는 후속 작업이 있는가? (EventHandler)

유스케이스 카탈로그를 생성합니다:

| Use Case | Type | Trigger |
|----------|------|---------|
| 상품 생성 | Command | API 요청 |
| 상품 조회 | Query | API 요청 |
| 재고 차감 | EventHandler | Order.CreatedEvent |

**출력:** `{context}/application/00-business-requirements.md`

### Phase 2: 유스케이스 -> 포트 식별

각 유스케이스가 의존하는 포트를 식별합니다:

- **Write Port**: `IRepository<T, TId>` (Aggregate 단위 CRUD)
- **Read Port**: `IQueryPort<T, TDto>` 또는 커스텀 `IQueryPort` (DTO 프로젝션)
- **External Port**: `IObservablePort` 기반 커스텀 인터페이스

포트 카탈로그:

| Port | Type | Layer | Depends On |
|------|------|-------|------------|
| IProductRepository | Write | Domain | Product Aggregate |
| IProductQuery | Read | Application | Product -> ProductSummaryDto |
| IProductDetailQuery | Read | Application | Product -> ProductDetailDto |
| IExternalPricingService | External | Application | 외부 가격 API |

**출력:** `{context}/application/01-type-design-decisions.md`

### Phase 3: 핸들러 구현

유스케이스 타입별 구현 패턴:

- **Command**: `ICommandRequest<Response>`, `ICommandUsecase<Request, Response>`
- **Query**: `IQueryRequest<Response>`, `IQueryUsecase<Request, Response>`
- **EventHandler**: `IDomainEventHandler<T.Event>`
- **Validator**: `AbstractValidator<Request>` + `MustSatisfyValidation`

`FinT<IO, T>` LINQ 합성 패턴:
```csharp
FinT<IO, Response> usecase =
    from product in productRepository.GetById(id)
    from _ in guard(condition, error)
    from updated in productRepository.Update(product)
    select new Response(...);

Fin<Response> response = await usecase.Run().RunAsync();
return response.ToFinResponse();
```

**ApplyT 패턴** — 다중 VO 생성이 필요한 Command에서 사용:
```csharp
FinT<IO, Response> usecase =
    from vos in (
        ProductName.Create(request.Name),
        Money.Create(request.Price)
    ).ApplyT((name, price) => (Name: name, Price: price))
    let product = Product.Create(vos.Name, vos.Price)
    from created in productRepository.Create(product)
    select new Response(...);
```

- `ApplyT`: 2~5개 `Fin<T>` 튜플의 에러를 applicative하게 수집 + `FinT<IO, R>` 리프팅
- LINQ `from` 첫 구문에서 사용 → 이후 체인에서 정규화된 VO를 바로 활용
- Presentation Validator가 이미 검증했더라도, Handler의 `Create()` 호출이 **도메인 검증의 권위적 지점이다**

**ApplyT vs Unwrap 선택 기준:**

| 기준 | Unwrap | ApplyT |
|------|--------|--------|
| VO 개수 | 1~2개 | 3개 이상 |
| 에러 처리 | 첫 에러에서 즉시 반환 | 모든 에러를 병렬 수집 |
| 코드 스타일 | 명령형 (`var x = ...`) | 선언형 (LINQ `from`) |
| 학습 곡선 | 낮음 | 높음 (모나드 트랜스포머) |
| 적합한 상황 | 간단한 Command, 내부 서비스 | 사용자 입력 폼, 복잡한 검증 |

**판단 기준:** VO가 1~2개이고 에러를 병렬 수집할 필요가 없으면 Unwrap이 더 간결합니다.
VO가 3개 이상이거나 사용자에게 모든 검증 오류를 한 번에 보여줘야 하면 ApplyT를 사용합니다.

상세 패턴은 `references/usecase-patterns.md`를 읽으세요.
포트 설계는 `references/port-design.md`를 읽으세요.

**출력:** `{context}/application/02-code-design.md`

### Domain Service 벌크 이벤트 추적
벌크 연산 Use Case에서 Domain Service가 생성한 벌크 이벤트를 `IDomainEventCollector.TrackEvent()`로 직접 추적합니다:

```csharp
public sealed class Usecase(
    IProductRepository productRepository,
    IDomainEventCollector eventCollector)
    : ICommandUsecase<Request, Response>
{
    FinT<IO, Response> usecase =
        from products in productRepository.GetByIds(ids)
        let bulkResult = ProductBulkOperations.BulkDelete(products.ToList(), "system")
        let _ = eventCollector.TrackEvent(bulkResult.Event)
        from affectedCount in productRepository.UpdateRange(bulkResult.Deleted.ToList())
        select new Response(affectedCount);
}
```

### Ctx Enricher 파이프라인 통합
파이프라인 실행 순서:
`CtxEnricher → Metrics → Tracing → Logging → Validation → Exception → Transaction → Handler`

`IUsecaseCtxEnricher<TRequest, TResponse>`를 구현하면 Request/Response의 비즈니스 필드가
Logging, Tracing, Metrics에 `ctx.*` 필드로 자동 전파됩니다:

```csharp
// Source Generator가 자동 생성. [CtxTarget]으로 Pillar 타겟팅
public sealed record Request(
    string CustomerId,                           // 기본 (Logging + Tracing)
    [CtxTarget(CtxPillar.All)] bool IsExpress,   // 3-Pillar Tag
    [CtxIgnore] string DebugInfo                 // 제외
) : ICommandRequest<Response>;
```

### Phase 4: 구현

실제 .cs 파일 생성 + 단위 테스트:

- Command/Query Usecase 테스트
- Validator 테스트
- EventHandler 테스트
- Shouldly 어설션, AAA 패턴

단위 테스트 규칙은 `Docs.Site/src/content/docs/guides/testing/15a-unit-testing.md`를 준수합니다.

**출력:** `{context}/application/03-implementation-results.md` + 소스 코드

## 핵심 원칙

- **CQRS 분리**: Command는 상태 변경, Query는 읽기 전용
- **Command**: `IRepository<T, TId>`를 통해 Aggregate 조작
- **Query**: `IQueryPort`를 통해 Aggregate 재구성 없이 DTO 직접 프로젝션
- **예외 대신 `Fin<T>`**: 모든 실패는 `FinResponse<T>`로 표현
- **Apply 패턴**: 여러 VO 검증을 병렬로 수행하여 모든 에러를 한 번에 수집
- **guard()**: LINQ 체인 내 조건부 단락 (`from _ in guard(condition, error)`)
- **ApplicationError**: 에러 코드는 `ApplicationErrors.{Usecase}.{ErrorName}` 형식
- **FinT -> FinResponse 변환**: `await usecase.Run().RunAsync()` -> `.ToFinResponse()`

## References

- 유스케이스 패턴: `references/usecase-patterns.md`를 읽으세요
- 포트 설계: `references/port-design.md`를 읽으세요
