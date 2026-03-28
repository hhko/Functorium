---
title: "전문 에이전트"
description: "6개 전문 에이전트의 역할과 활용법"
---

## 스킬 vs 에이전트

functorium-develop 플러그인은 스킬과 에이전트, 두 가지 도구를 제공합니다. 목적과 사용 방식이 다릅니다.

| 구분 | 스킬 | 에이전트 |
|------|------|---------|
| 비유 | 자동 워크플로 | 전문가 상담 |
| 동작 방식 | 정해진 Phase를 순서대로 실행 | 자유로운 대화로 설계 결정 토론 |
| 산출물 | 문서 + 코드 (구조화된 출력) | 설계 조언, 의사결정 근거 |
| 사용 시점 | "이것을 구현해줘" | "이것을 어떻게 설계해야 할까?" |
| 트리거 | 키워드 기반 자동 매칭 | 대화에서 전문성이 필요할 때 활성화 |

스킬은 반복 작업을 자동화합니다. 에이전트는 설계 판단이 필요한 순간에 전문가의 관점을 제공합니다. 복잡한 프로젝트에서는 에이전트로 설계 방향을 먼저 결정한 뒤, 스킬로 구현을 자동화하는 조합이 효과적입니다.

## 에이전트 목록

| 에이전트 | 전문 영역 | 활용 장면 |
|---------|-----------|----------|
| `product-analyst` | PRD 작성, 요구사항 분석, 사용자 스토리, Aggregate 경계 | 프로젝트 기획, 요구사항 정의 |
| `domain-architect` | 유비쿼터스 언어, Aggregate 경계, 타입 전략 | 도메인 모델 설계 결정 |
| `application-architect` | CQRS 분리, 포트 식별, FinT 합성, CtxEnricher 3-Pillar | 유스케이스 아키텍처 결정 |
| `adapter-engineer` | Repository, Endpoint, DI, Observable Port, CtxEnricherPipeline | 어댑터 구현 전략 |
| `observability-engineer` | KPI→메트릭 매핑, 대시보드, 알림, ctx.* 전파, 분산 추적 | 관측성 전략 수립 |
| `test-engineer` | 단위/통합/아키텍처 테스트, ctx 3-Pillar 스냅샷, 관측성 검증 | 테스트 전략 수립 |

## domain-architect -- 도메인 설계 전문가

에릭 에반스의 DDD 원칙을 기반으로 도메인 모델을 설계합니다.

**전문 영역:**
- 유비쿼터스 언어(Ubiquitous Language) 정의
- 바운디드 컨텍스트(Bounded Context) 경계 설계
- 불변식(Invariant) 분류 및 Functorium 타입 매핑
- Aggregate Root 경계 결정
- Value Object vs Entity 판단
- Domain Service 벌크 연산 설계

**Functorium 타입 매핑 원칙:**

| 불변식 유형 | Functorium 타입 |
|-------------|-----------------|
| 단일값 불변식 | `SimpleValueObject<T>` |
| 비교/연산 불변식 | `ComparableSimpleValueObject<T>` |
| 열거형 상태 | SmartEnum (`SimpleValueObject<string>` + `HashMap`) |
| 상태 전이 | `UnionValueObject<TSelf>` + `TransitionFrom` |
| 생명주기 관리 | `AggregateRoot<TId>` + `[GenerateEntityId]` |
| 조건부 쿼리 | `ExpressionSpecification<T>` |
| 교차 Aggregate 규칙 | static Domain Service (`Fin<T>` 반환) |

### 예시 1 (초급): Value Object 타입 결정

```text
사용자: 상품 가격을 어떻게 모델링해야 할까?
에이전트: 가격은 양수 검증이 필요한 단일 숫자 값이므로 ComparableSimpleValueObject<decimal>이 적합합니다.
         Zero 상수, Add/Subtract 연산도 제공됩니다. 음수 차감 시 Fin<Money> 실패를 반환합니다.
```

### 예시 2 (중급): Aggregate 경계 결정

```text
사용자: 상품과 재고를 하나의 Aggregate로 묶어도 될까?
에이전트: 분리를 권장합니다. 이유:
         1. 변경 빈도가 다릅니다 -- 상품 정보는 드물게, 재고는 주문마다 변경
         2. 동시성 충돌 -- 같은 Aggregate면 상품 수정과 재고 차감이 충돌
         3. 트랜잭션 경계 -- DDD에서 하나의 트랜잭션은 하나의 Aggregate만 변경
         ProductId를 교차 참조로 사용하고, 도메인 이벤트로 연결하세요.
```

### 예시 3 (고급): 바운디드 컨텍스트 설계

```text
사용자: 전자상거래 시스템의 바운디드 컨텍스트를 어떻게 나눠야 할까?
에이전트: 유비쿼터스 언어 기준으로 분리합니다:
         - 상품 카탈로그 (Product, Tag, Category)
         - 주문 관리 (Order, OrderLine, ShippingAddress)
         - 고객 관리 (Customer, CreditLimit)
         - 재고 관리 (Inventory, StockQuantity)

         컨텍스트 간 통신은 도메인 이벤트로:
         Order.CreatedEvent -> Inventory 재고 차감
         Order.CancelledEvent -> Inventory 재고 복원

         각 컨텍스트는 독립 Aggregate를 가지며,
         교차 참조는 ID(ProductId, CustomerId)만 사용합니다.
```

## application-architect -- 유스케이스 설계 전문가

CQRS 패턴 기반으로 유스케이스를 설계합니다.

**전문 영역:**
- Command vs Query 분류
- 포트 식별 (IRepository, IQueryPort)
- FinT<IO, T> LINQ 합성 패턴
- FluentValidation 통합
- 도메인 이벤트 핸들러 설계
- CtxEnricher 3-Pillar 설계 (Logging + Tracing + MetricsTag 동시 전파)
- IDomainEventCollector.TrackEvent 벌크 이벤트 추적

**핵심 패턴:**

| 패턴 | 용도 |
|------|------|
| `ICommandRequest<Response>` + `ICommandUsecase<Request, Response>` | 상태 변경 유스케이스 |
| `IQueryRequest<Response>` + `IQueryUsecase<Request, Response>` | 읽기 유스케이스 |
| `IDomainEventHandler<T.Event>` | 이벤트 반응 |
| `from...in...select` LINQ 합성 | 함수형 체이닝 |
| `guard()` 조건 체크 | 선언적 조건 단락 |
| `ApplyT` 패턴 | 병렬 VO 검증 + FinT 리프팅 |
| `[CtxRoot]` / `[CtxTarget]` / `[CtxIgnore]` | 관측성 필드 타겟팅 |

### 예시 1 (초급): Command vs Query 판단

```text
사용자: 상품 생성은 Command? Query?
에이전트: Command입니다. 상태를 변경(생성)하므로 ICommandRequest<Response>를 사용합니다.
         상품 조회는 Query -- IQueryRequest<Response>를 사용합니다.
```

### 예시 2 (중급): 복합 검증 설계

```text
사용자: 상품 생성 시 이름 중복과 입력값 검증을 어떻게 처리해야 할까?
에이전트: 2단계로 분리합니다:
         1. 입력값 검증: FluentValidation (Validator 클래스)
            - MustSatisfyValidation(ProductName.Validate) -> 형식 검증
         2. 비즈니스 규칙: Usecase 내부
            - guard(!exists, ApplicationError) -> 중복 검증

         FluentValidation은 파이프라인에서 Usecase 전에 실행되므로,
         형식 오류는 DB 조회 없이 즉시 400을 반환합니다.
```

### 예시 3 (고급): 이벤트 기반 워크플로 설계

```text
사용자: 주문 생성 -> 재고 차감 -> 결제 처리 -> 배송 예약 흐름을 설계해줘.
에이전트: Eventual Consistency 패턴으로 설계합니다:

         1. CreateOrderCommand (동기)
            -> Order.Create() -> Order.CreatedEvent
            -> Repository.Create() + SaveChanges

         2. OnOrderCreated EventHandler (비동기)
            -> Inventory.Deduct(quantity)
            -> 실패 시 -> Order.CancelledEvent 발행

         3. OnOrderConfirmed EventHandler (비동기)
            -> PaymentService.Charge()
            -> 실패 시 -> Order.CancelledEvent + Inventory.Restock

         각 단계는 독립 트랜잭션. 실패 시 보상 이벤트로 롤백.
```

## adapter-engineer -- 어댑터 구현 전문가

Repository, Query Adapter, Endpoint 구현을 전문적으로 담당합니다.

**전문 영역:**
- EfCoreRepositoryBase / InMemoryRepositoryBase 구현
- DapperQueryBase 구현 (SQL + SpecTranslator)
- FastEndpoints Endpoint 구현
- [GenerateObservablePort] + DI 등록
- EF Core Configuration, Migration
- CtxEnricherPipeline 파이프라인 통합
- CtxEnricherContext.SetPushFactory 초기화 (Serilog + Activity + MetricsTagContext)
- ObservableSignal을 활용한 Adapter 내부 운영 로깅

**핵심 규칙:**

| 규칙 | 설명 |
|------|------|
| `virtual` 메서드 | 모든 어댑터 메서드는 virtual (Source Generator 파이프라인 필수) |
| `[GenerateObservablePort]` | 관측성 래퍼 자동 생성 필수 |
| `IO.lift()` / `IO.liftAsync()` | 동기/비동기 IO 래핑 |
| `RequestCategory` | 관측성 분류: `"repository"`, `"query"`, `"external_api"`, `"unit_of_work"` |

### 예시 1 (초급): Repository 구현 전략 질문

```text
사용자: InMemory와 EF Core 중 어떤 걸 먼저 구현해야 할까?
에이전트: InMemory를 먼저 구현하세요.
         1. 도메인 로직 테스트에 집중 가능
         2. DB 없이 빠른 피드백 루프
         3. EF Core는 영속성이 필요할 때 추가
         DI 등록에서 Provider 분기로 전환합니다.
```

### 예시 2 (중급): EF Core Mapper 설계

```text
사용자: Product Aggregate를 EF Core Model로 어떻게 매핑해야 할까?
에이전트: 3개 파일이 필요합니다:
         1. ProductModel (IHasStringId) -- DB 테이블 매핑
         2. ProductConfiguration (IEntityTypeConfiguration) -- 컬럼/인덱스 설정
         3. ProductMapper -- ToDomain()/ToModel() 변환

         VO는 primitive로 저장:
         ProductName -> string Name
         Money -> decimal Price
         ProductId -> string Id (Ulid.ToString())
```

### 예시 3 (고급): Dapper + Specification 패턴 구현

```text
사용자: 상품 검색 API를 Dapper로 구현하려면?
에이전트: CQRS Read Side 패턴입니다:
         1. IProductQuery : IQueryPort<Product, ProductDto>
         2. DapperProductQuery : DapperQueryBase<Product, ProductDto>
            - SelectQuery: "SELECT Id, Name, Price FROM Products"
         3. ProductSpecTranslator -- Specification -> WHERE 절 변환
            - ProductNameContainsSpec -> "Name LIKE @Name"
            - PriceRangeSpec -> "Price BETWEEN @Min AND @Max"
         4. 페이지네이션: offset-based (Page/PageSize) 또는 cursor-based
```

## test-engineer -- 테스트 전략 전문가

테스트 피라미드 기반으로 효과적인 테스트 전략을 수립합니다.

**전문 영역:**
- 단위 테스트: NSubstitute, Shouldly, FinTFactory
- 통합 테스트: HostTestFixture, HttpClient
- 아키텍처 규칙: ArchUnitNET
- 스냅샷 테스트: Verify.Xunit, LogTestContext
- CtxEnricher 3-Pillar 스냅샷 테스트 (MetricsTagContext, Activity.Current, LogContext)

**도구:**

| 도구 | 용도 |
|------|------|
| xUnit v3 (MTP) | 테스트 프레임워크 |
| Shouldly | 어설션 |
| NSubstitute | 모킹 |
| FinTFactory | `FinT<IO, T>` mock 반환값 |
| HostTestFixture<Program> | 통합 테스트 픽스처 |
| ArchUnitNET | 아키텍처 규칙 검증 |
| MetricsTagContext | AsyncLocal 기반 메트릭 태그 검증 |

### 예시 1 (초급): 테스트 네이밍 질문

```text
사용자: 테스트 메서드 이름을 어떻게 짓나요?
에이전트: T1_T2_T3 패턴을 사용합니다:
         T1: 테스트 대상 메서드 (Create, Handle, Delete)
         T2: 기대 결과 (ShouldSucceed, ShouldFail, ShouldRaise)
         T3: 조건 (WhenNameIsValid, WhenPriceIsNegative)

         예: Create_ShouldSucceed_WhenNameIsValid
```

### 예시 2 (중급): Mock 전략

```text
사용자: Usecase 테스트에서 Repository를 어떻게 모킹하나요?
에이전트: NSubstitute + FinTFactory를 사용합니다:

         _repo.GetById(Arg.Any<ProductId>())
             .Returns(FinTFactory.Succ(product));

         _repo.Create(Arg.Any<Product>())
             .Returns(FinTFactory.Fail<Product>(error));

         FinTFactory가 FinT<IO, T> 반환값을 간편하게 생성합니다.
```

### 예시 3 (고급): 테스트 피라미드 전략

```text
사용자: 새 Aggregate 추가 시 어떤 테스트를 작성해야 할까?
에이전트: 3계층으로 구성합니다:

         1. 단위 테스트 (70%) -- 빠르고 많이
            - Value Object: Create 성공/실패/정규화 (VO당 3-5개)
            - AggregateRoot: 상태변경+이벤트+가드조건 (메서드당 3-5개)
            - Usecase: 성공/실패/엣지케이스 (시나리오당 3-5개)

         2. 통합 테스트 (20%) -- 핵심 흐름
            - Endpoint: 성공 201, 실패 400, NotFound 404
            - E2E: 생성->조회->수정->삭제

         3. 아키텍처 규칙 (10%) -- 구조 보장
            - sealed class, private 생성자
            - [GenerateEntityId] 어트리뷰트
            - 레이어 의존성 방향
```

## 에이전트 활용 가이드

### 에이전트 간 협업

복잡한 설계에서 여러 에이전트를 순차적으로 활용합니다:

1. **domain-architect** -> Aggregate 경계를 먼저 결정
2. **application-architect** -> 유스케이스 패턴과 포트 식별
3. **adapter-engineer** -> 구현 기술 선택과 전략 결정
4. **test-engineer** -> 테스트 피라미드와 커버리지 전략

### 에이전트 호출

대화에서 에이전트의 전문 영역에 해당하는 질문을 하면 해당 전문성이 활성화됩니다. 명시적으로 에이전트를 지정할 수도 있습니다.

### 스킬과의 조합

| 상황 | 접근 방법 |
|------|----------|
| 설계 판단이 명확한 경우 | 스킬로 바로 구현 |
| Aggregate 경계가 모호한 경우 | domain-architect와 상담 -> domain-develop 스킬로 구현 |
| CQRS 분리가 복잡한 경우 | application-architect와 상담 -> application-develop 스킬로 구현 |
| 영속성 전략을 비교해야 하는 경우 | adapter-engineer와 상담 -> adapter-develop 스킬로 구현 |
| 테스트 범위를 결정해야 하는 경우 | test-engineer와 상담 -> test-develop 스킬로 구현 |

## 참고 자료

- [워크플로](./workflow/) -- 6단계 개발 워크플로
- [도메인 개발 스킬](./skills/domain-develop/) -- domain-architect의 전문성이 반영된 스킬
- [Application 개발 스킬](./skills/application-develop/) -- application-architect의 전문성이 반영된 스킬
- [Adapter 개발 스킬](./skills/adapter-develop/) -- adapter-engineer의 전문성이 반영된 스킬
- [테스트 개발 스킬](./skills/test-develop/) -- test-engineer의 전문성이 반영된 스킬
