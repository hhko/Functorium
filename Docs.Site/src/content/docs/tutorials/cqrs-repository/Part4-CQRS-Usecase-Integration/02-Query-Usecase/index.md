---
title: "쿼리 유스케이스"
---
## 개요

목록 조회에는 Repository가 아닌 IQueryPort가 필요한데, Usecase 구조는 어떻게 달라질까요? Command Usecase가 Aggregate Root를 통해 상태를 변경한다면, Query Usecase는 읽기 전용 DTO를 반환합니다. 데이터 소스도, 반환 타입도, 트랜잭션 처리도 다릅니다. 이 장에서는 Query 전용 경로를 설계하고, Command와의 구조적 차이를 직접 확인해봅시다.

---

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. **IQueryRequest / IQueryUsecase** 인터페이스로 Query 요청과 핸들러를 정의할 수 있습니다
2. **Query Port를** 통해 Repository가 아닌 읽기 전용 경로로 데이터를 조회할 수 있습니다
3. **DTO 기반 응답으로** 도메인 엔티티 대신 조회에 최적화된 데이터를 반환할 수 있습니다
4. **Command와 Query의 구조적 차이를** 설명할 수 있습니다

---

## 핵심 개념

### Command vs Query Usecase

Command와 Query는 목적부터 데이터 소스까지 모든 것이 다릅니다. 아래 표에서 두 경로의 핵심 차이를 비교해보세요.

| 구분 | Command | Query |
|------|---------|-------|
| 목적 | 상태 변경 | 데이터 조회 |
| 인터페이스 | `ICommandRequest<T>` | `IQueryRequest<T>` |
| 핸들러 | `ICommandUsecase` | `IQueryUsecase` |
| 데이터 소스 | Repository (Aggregate) | Query Port (DTO) |
| 트랜잭션 | SaveChanges 자동 호출 | 트랜잭션 없음 |

### Query Port 패턴

Query Port는 도메인 엔티티가 아닌 DTO를 직접 반환합니다. `IQueryPort<TEntity, TDto>`를 상속하면 Specification 기반의 Search/SearchByCursor/Stream 세 가지 조회 메서드를 자동으로 사용할 수 있습니다.

```csharp
// Query 전용 인터페이스 - IQueryPort 상속
public interface IProductQuery : IQueryPort<Product, ProductDto>
{
}
```

Usecase에서는 Specification과 PageRequest/SortExpression을 조합하여 동적 검색을 수행합니다.

```csharp
public sealed record Request(string Keyword, PageRequest Page, SortExpression Sort)
    : IQueryRequest<Response>;

public sealed class Usecase(IProductQuery productQuery)
    : IQueryUsecase<Request, Response>
{
    public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken ct)
    {
        var spec = new ProductNameSpec(request.Keyword);

        FinT<IO, Response> usecase =
            from products in productQuery.Search(spec, request.Page, request.Sort)
            select new Response(products);

        Fin<Response> result = await usecase.Run().RunAsync();
        return result.ToFinResponse();
    }
}
```

### ICacheable

`IQueryRequest`가 `ICacheable`을 구현하면 `UsecaseCachingPipeline`이 자동으로 응답을 캐시합니다.

```csharp
public sealed record Request(string Keyword, PageRequest Page, SortExpression Sort)
    : IQueryRequest<Response>, ICacheable
{
    public string CacheKey => $"products:search:{Keyword}:{Page.Page}:{Page.PageSize}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}
```

---

## 프로젝트 설명

아래 파일들이 Query Usecase의 전체 구조를 구성합니다.

| 파일 | 설명 |
|------|------|
| `ProductId.cs` | Ulid 기반 Product 식별자 |
| `Product.cs` | AggregateRoot 기반 상품 엔티티 |
| `ProductDto.cs` | 조회 전용 DTO |
| `IProductQuery.cs` | IQueryPort\<Product, ProductDto\> 상속 인터페이스 |
| `InMemoryProductQuery.cs` | InMemoryQueryBase 기반 Query 어댑터 구현 |
| `ProductNameSpec.cs` | Specification\<Product\> — 이름 키워드 검색 조건 |
| `SearchProductsQuery.cs` | Query Usecase 패턴 (Request, Response, Usecase) |
| `Program.cs` | 실행 데모 |

---

## 한눈에 보는 정리

Query Usecase를 구성하는 핵심 개념을 정리합니다.

| 개념 | 설명 |
|------|------|
| `IQueryRequest<T>` | Query 요청 마커 (Mediator IQuery 확장) |
| `IQueryUsecase<TQuery, T>` | Query 핸들러 (Mediator IQueryHandler 확장) |
| Query Port | 읽기 전용 데이터 접근 인터페이스 |
| DTO | 도메인 엔티티 대신 반환하는 조회 전용 데이터 |

---

## FAQ

### Q1: 왜 Repository 대신 별도의 Query Port를 사용하나요?
**A**: Repository는 Aggregate Root 단위의 CRUD에 초점을 맞추지만, Query는 여러 테이블을 조인하거나 집계하는 등 읽기에 최적화된 별도의 경로가 필요합니다. CQRS의 핵심은 이 읽기/쓰기 경로의 분리입니다.

### Q2: Query Usecase에서도 FinT를 사용하는 이유는?
**A**: 데이터 조회도 실패할 수 있기 때문입니다 (not found, DB 연결 오류 등). FinT를 사용하면 Command와 동일한 합성 패턴으로 에러를 처리할 수 있습니다.

---

Query Usecase를 만들었습니다. 그런데 여러 Repository 호출을 순차 연결하면서 중간에 조건 검증도 끼워야 한다면? 다음 장에서는 FinT 모나딕 합성의 다양한 패턴을 살펴봅니다.
