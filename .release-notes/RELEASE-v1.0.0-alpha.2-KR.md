# Functorium Release v1.0.0-alpha.2

**[English](./RELEASE-v1.0.0-alpha.2.md)** | **한국어**

**발표 자료**: [PDF](./RELEASE-v1.0.0-alpha.2-KR.pdf) | [PPTX](./RELEASE-v1.0.0-alpha.2-KR.pptx) | [MP4](./RELEASE-v1.0.0-alpha.2-KR.mp4) | [M4A](./RELEASE-v1.0.0-alpha.2-KR.m4a)

## 개요

Functorium v1.0.0-alpha.2는 **DDD 도메인 모델링 프레임워크**와 **Functorium.Adapters 프로젝트 분리**를 핵심으로 하는 대규모 릴리스입니다. 도메인 중심 기능 아키텍처를 본격적으로 구현할 수 있도록 Entity, AggregateRoot, Specification, Domain Event 등 전체 DDD 빌딩 블록을 제공하며, 관측성(Observability) 시스템을 ctx.* 컨텍스트 전파 기반으로 근본적으로 재설계했습니다.

**주요 기능**:

- **DDD 도메인 모델링**: Entity, AggregateRoot, Specification, Domain Event, Domain Service, IRepository 등 완전한 DDD 빌딩 블록 프레임워크
- **계층별 타입 안전 에러 시스템**: DomainError, ApplicationError, AdapterError로 분리된 sealed record 기반 에러 타입 계층
- **Functorium.Adapters 프로젝트 분리**: Pipeline, Observability, Repository 구현체를 독립 패키지로 추출하여 의존성 최소화
- **검증 시스템 확장**: Contextual/Typed 검증, Apply/ApplyT 패턴, FluentValidation 통합
- **ctx.* 관측성 재설계**: CtxEnricher + 소스 생성기 기반 구조화된 로그 컨텍스트 전파
- **아키텍처 테스트 Suite**: DomainArchitectureTestSuite, ApplicationArchitectureTestSuite로 규칙 검증 자동화

## Breaking Changes

### 1. Functorium.Adapters 프로젝트 분리

Pipeline, Observability, Repository 구현체가 `Functorium` 패키지에서 `Functorium.Adapters`로 이동했습니다. Abstractions(인터페이스/추상)는 `Functorium`에 유지됩니다.

**이전 (v1.0.0-alpha.1)**:
```csharp
using Functorium.Adapters.Registrations;    // AdapterPipelineRegistration
using Functorium.Observabilities;           // OpenTelemetryBuilder
using Functorium.Applications.Pipelines;    // UsecaseLoggingPipeline
```

**이후 (v1.0.0-alpha.2)**:
```csharp
using Functorium.Adapters.Abstractions.Registrations;    // OpenTelemetryRegistration
using Functorium.Adapters.Observabilities.Builders;      // OpenTelemetryBuilder
using Functorium.Adapters.Pipelines;                     // UsecaseLoggingPipeline
```

**마이그레이션 가이드**:
1. NuGet 패키지 `Functorium.Adapters` 추가: `dotnet add package Functorium.Adapters`
2. `Functorium.Observabilities.*` 네임스페이스를 `Functorium.Adapters.Observabilities.*`로 변경
3. `Functorium.Applications.Pipelines.*`을 `Functorium.Adapters.Pipelines.*`으로 변경

<!-- 관련 커밋: 6f859dab refactor: Functorium.Adapters 프로젝트 분리 및 Abstractions 네임스페이스 변경 -->
<!-- 관련 커밋: 8894c2a4 refactor(adapters)!: 기술 관심사 단위로 Adapters 폴더 재구성 -->

---

### 2. 파이프라인 Opt-in 모델 전환

모든 파이프라인 단계가 기본 비활성화되어, `ConfigurePipelines`에서 명시적으로 활성화해야 합니다.

**이전 (v1.0.0-alpha.1)**:
```csharp
// 모든 파이프라인이 자동 등록됨
services.RegisterOpenTelemetry(configuration, assembly)
    .Build();
```

**이후 (v1.0.0-alpha.2)**:
```csharp
services.RegisterOpenTelemetry(configuration, assembly)
    .ConfigurePipelines(p => p
        .UseValidation()
        .UseLogging()
        .UseMetrics()
        .UseTracing()
        .UseException()
        .UseTransaction())
    .Build();
```

**마이그레이션 가이드**:
1. `RegisterOpenTelemetry()` 호출 후 `.ConfigurePipelines()` 추가
2. 필요한 파이프라인만 `.UseXxx()` 메서드로 활성화
3. 관찰가능성 전체를 한번에 활성화하려면 `.UseObservability()` 사용

<!-- 관련 커밋: 4a08b441 refactor(pipeline)!: 파이프라인 단계를 opt-in 모델로 전환 -->

---

### 3. Custom ErrorType sealed record 파생으로 변경

커스텀 에러 타입 정의가 문자열 기반에서 타입 안전한 sealed record 파생으로 변경되었습니다.

**이전 (v1.0.0-alpha.1)**:
```csharp
// 문자열 기반 커스텀 에러
var error = Error.New("CustomError", "Something went wrong");
```

**이후 (v1.0.0-alpha.2)**:
```csharp
// 타입 안전 sealed record 파생
public sealed class InsufficientStock : DomainErrorType.Custom;

var error = DomainError.For<Product>(
    new DomainErrorType.InsufficientStock(),
    currentQuantity,
    "Requested quantity exceeds available stock");
```

**마이그레이션 가이드**:
1. 기존 문자열 기반 에러를 `DomainErrorType.Custom`, `ApplicationErrorType.Custom`, 또는 `AdapterErrorType.Custom`에서 파생하는 sealed record로 변경
2. `DomainError.For<TDomain>(errorType, currentValue, message)` 팩토리 메서드 사용
3. 에러 매칭 시 패턴 매칭으로 전환: `error is DomainErrorType.NotFound`

<!-- 관련 커밋: e28eee6f refactor!: Custom ErrorType을 문자열 기반에서 타입 안전 sealed record 파생으로 변경 -->

---

### 4. Observabilities 네임스페이스 이동

IObservablePort, CtxPillar 등 관측성 추상화가 `Functorium.Abstractions.Observabilities`로 이동했습니다.

**이전 (v1.0.0-alpha.1)**:
```csharp
using Functorium.Applications.Observabilities;
```

**이후 (v1.0.0-alpha.2)**:
```csharp
using Functorium.Abstractions.Observabilities;  // IObservablePort, CtxPillar 등
```

**마이그레이션 가이드**:
1. `Functorium.Applications.Observabilities` 네임스페이스를 `Functorium.Abstractions.Observabilities`로 변경
2. `IAdapter` 인터페이스는 `IObservablePort`로 이름 변경됨

<!-- 관련 커밋: 70860eee refactor!: Observabilities를 Abstractions로 이동하여 레이어 응집도 개선 -->

---

### 5. SourceGenerator 프로젝트 이름 변경

소스 생성기 패키지 이름이 변경되었습니다.

**이전**: `Functorium.SourceGenerators` (또는 `Functorium.Adapters.SourceGenerator`)
**이후**: `Functorium.Adapters.SourceGenerators`

**마이그레이션 가이드**:
1. NuGet 패키지 참조를 `Functorium.Adapters.SourceGenerators`로 변경
2. `GenerateObservablePortAttribute`는 `Functorium.Adapters.SourceGenerators` 네임스페이스에 위치

<!-- 관련 커밋: eb00ce14 refactor!: SourceGenerator 프로젝트 이름 변경 및 네임스페이스 재구성 -->
<!-- 관련 커밋: dee70449 refactor!: Functorium.Adapters.SourceGenerator 이름 변경 -->

## 새로운 기능

### Functorium 라이브러리

#### 1. DDD Entity 및 AggregateRoot 프레임워크

Entity와 AggregateRoot 기본 클래스를 통해 DDD의 핵심 빌딩 블록을 제공합니다. Ulid 기반 `IEntityId<T>` 제약과 `GenerateEntityIdAttribute` 소스 생성기로 ID 타입 보일러플레이트를 제거합니다.

```csharp
// ID 타입 정의 - GenerateEntityIdAttribute로 자동 생성
[GenerateEntityId]
public partial struct ProductId;

// AggregateRoot 정의
public sealed class Product : AggregateRoot<ProductId>
{
    public ProductName Name { get; private set; }

    private Product() { }
    private Product(ProductId id, ProductName name) : base(id)
    {
        Name = name;
        AddDomainEvent(new ProductCreatedEvent(id));
    }

    public static Fin<Product> Create(ProductName name) =>
        Fin.Succ(new Product(ProductId.New(), name));
}
```

**Why this matters (왜 중요한가):**
- Entity/AggregateRoot 기본 클래스가 동등성, 해시코드, 도메인 이벤트 수집 등 50줄 이상의 보일러플레이트를 제거합니다
- `IEntityId<T>`의 `IParsable<T>` 제약으로 API 입력에서 타입 안전한 ID 파싱이 가능합니다
- `GenerateEntityIdAttribute`로 ID 타입의 전체 구현(Create, New, Parse, CompareTo, Equals)을 자동 생성합니다
- 불변 Entity와 팩토리 메서드 패턴으로 유효하지 않은 도메인 객체 생성을 컴파일 타임에 방지합니다

<!-- 관련 커밋: 7555a7ff feat(domains): IRepository<TAggregate, TId> 공통 인터페이스 구현 -->
<!-- 관련 커밋: adfa72c8 feat: IEntityId에 IParsable<T> 제약 추가 -->
<!-- 관련 커밋: 3c5ef59e feat(source-generator): Attribute 정의를 Functorium 라이브러리에 추가 -->

---

#### 2. Specification 패턴 프레임워크

도메인 비즈니스 규칙을 재사용 가능한 Specification 객체로 캡슐화합니다. And/Or/Not 조합과 Expression 기반 자동 번역을 지원하여 EF Core/Dapper 쿼리로 변환할 수 있습니다.

```csharp
// Expression 기반 Specification 정의
public sealed class ActiveProductSpec : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression() =>
        product => product.IsActive;
}

// Specification 조합
Specification<Product> spec = new ActiveProductSpec() & new InStockSpec();
bool satisfies = spec.IsSatisfiedBy(product);

// Expression 자동 번역 (도메인→인프라 모델)
var map = new PropertyMap<Product, ProductModel>()
    .Map(p => p.Name, m => m.ProductName);
Expression<Func<ProductModel, bool>> dbExpr = map.Translate(spec.ToExpression());
```

**Why this matters (왜 중요한가):**
- 비즈니스 규칙이 도메인 레이어에 응집되어 Repository 구현체에 흩어지는 것을 방지합니다
- `&`, `|`, `!` 연산자로 복잡한 비즈니스 규칙을 직관적으로 조합할 수 있습니다
- `PropertyMap<TEntity, TModel>.Translate()`로 도메인 Expression을 인프라 모델 쿼리로 자동 변환하여 수동 매핑 오류를 제거합니다
- `Specification<T>.All`로 필터 없는 전체 조회를 타입 안전하게 표현합니다

<!-- 관련 커밋: da61b6d7 feat(specification): Specification 패턴 프레임워크 추가 -->
<!-- 관련 커밋: c7704dea feat(specifications): Expression 기반 Specification 자동 번역 구현 -->

---

#### 3. 계층별 타입 안전 에러 시스템

Domain, Application, Adapter 세 레이어에 각각 전용 에러 타입을 제공합니다. 모든 에러 타입은 sealed record로 정의되어 패턴 매칭이 가능하며, `ErrorType` 추상 record에서 파생됩니다.

```csharp
// 도메인 에러 - 30+ 세부 타입
var error = DomainError.For<ProductName>(
    new DomainErrorType.TooLong(MaxLength: 100),
    name,
    "Product name exceeds maximum length");

// 애플리케이션 에러
var error = ApplicationError.For<CreateProductCommand>(
    new ApplicationErrorType.AlreadyExists(),
    productId,
    "Product with this ID already exists");

// 커스텀 에러 타입 확장
public sealed class InsufficientStock : DomainErrorType.Custom;
```

**Why this matters (왜 중요한가):**
- 레이어별 에러 타입 분리로 에러 원인이 Domain/Application/Adapter 어디인지 즉시 식별됩니다
- sealed record 기반으로 `switch` 패턴 매칭 시 컴파일러가 누락된 케이스를 경고합니다
- `DomainErrorType.TooLong(MaxLength: 100)` 같은 구조화된 에러가 메타데이터를 포함하여 로깅과 사용자 메시지 생성을 단순화합니다
- 에러 코드가 `DomainErrors.ProductName.TooLong` 형식으로 자동 생성되어 관측성(Observability) 시스템과 통합됩니다

<!-- 관련 커밋: 781d4d20 feat: 레이어별 Error 타입 및 헬퍼 클래스 추가 -->
<!-- 관련 커밋: af1b1b33 feat(abstractions): ErrorType 기본 추상 record 추가 -->
<!-- 관련 커밋: e28eee6f refactor!: Custom ErrorType을 문자열 기반에서 타입 안전 sealed record 파생으로 변경 -->

---

#### 4. Domain Event 시스템

도메인 이벤트 발행 및 처리를 위한 완전한 인프라를 제공합니다. `AggregateRoot`에서 이벤트를 수집하고, `IDomainEventPublisher`를 통해 발행하며, `IDomainEventHandler<TEvent>`로 처리합니다.

```csharp
// 도메인 이벤트 정의
public sealed record ProductCreatedEvent(ProductId ProductId)
    : DomainEvent;

// AggregateRoot에서 이벤트 발행
public static Fin<Product> Create(ProductName name)
{
    var product = new Product(ProductId.New(), name);
    product.AddDomainEvent(new ProductCreatedEvent(product.Id));
    return Fin.Succ(product);
}

// 이벤트 핸들러
public sealed class ProductCreatedHandler
    : IDomainEventHandler<ProductCreatedEvent>
{
    public async ValueTask Handle(
        ProductCreatedEvent notification,
        CancellationToken cancellationToken) { /* ... */ }
}
```

**Why this matters (왜 중요한가):**
- AggregateRoot 간 결합을 제거하여 각 Aggregate가 독립적으로 진화할 수 있습니다
- `IDomainEventCollector`가 트랜잭션 경계 내에서 이벤트를 수집하여 데이터 일관성을 보장합니다
- `PublishTrackedEvents()`로 저장과 이벤트 발행을 원자적으로 처리합니다
- Mediator 기반으로 핸들러가 자동 발견되어 이벤트 추가 시 기존 코드 수정이 불필요합니다

<!-- 관련 커밋: 1c4d948d feat(domain-event): IDomainEventPublisher 및 도메인 이벤트 발행 기능 추가 -->
<!-- 관련 커밋: 57982399 feat(domain-event): 도메인 이벤트 처리 시스템 개선 -->

---

#### 5. Contextual/Typed 검증 시스템

도메인 Value Object 검증을 위한 두 가지 fluent API를 제공합니다. `ValidationRules.For("context")`를 통한 Contextual 방식과 `TypedValidation<TV, T>`을 통한 Typed 방식 모두 지원합니다.

```csharp
// Contextual 검증 - 필드 컨텍스트 자동 포함
public static Validation<Error, ProductName> Validate(string name) =>
    (ValidationRules.For(nameof(Name)).NotEmpty(name).ThenNormalize(s => s.Trim()).ThenMaxLength(100),
     ValidationRules.For(nameof(Name)).IsLowerCase(name))
    .Apply((trimmed, _) => new ProductName(trimmed));

// Apply/ApplyT 패턴 - 모든 검증 에러 수집
var result = (
    ProductName.Validate(cmd.Name),
    Quantity.Validate(cmd.Quantity),
    Price.Validate(cmd.Price)
).Apply((name, qty, price) => new Product(name, qty, price));

// ApplyT - Fin→FinT<IO, T> 리프팅
var finT = (nameResult, qtyResult).ApplyT((name, qty) => (name, qty));
```

**Why this matters (왜 중요한가):**
- `Apply` 패턴이 첫 번째 에러에서 중단하지 않고 모든 검증 에러를 한번에 수집합니다
- `ValidationRules.For("Name")`이 에러 메시지에 필드 컨텍스트를 자동 포함하여 클라이언트가 어떤 필드에 문제가 있는지 즉시 파악합니다
- `ThenNormalize().ThenMaxLength()` 체이닝으로 정규화와 검증을 단일 파이프라인에서 처리합니다
- `ApplyT`로 `Fin<T>` 결과를 `FinT<IO, T>`로 리프팅하여 검증과 비즈니스 로직을 자연스럽게 연결합니다

<!-- 관련 커밋: 7dafe900 feat(domain): DomainError 헬퍼 및 ValidationRules 라이브러리 추가 -->
<!-- 관련 커밋: 47d88180 feat(validation): FinApplyExtensions.ApplyT 추가 및 CreateProductCommand 참조 구현 -->

---

#### 6. UnionValueObject 타입 시스템

대수적 데이터 타입(ADT) 기반의 Union Value Object를 지원합니다. 상태 머신, 결제 수단 등 제한된 변형을 가진 도메인 개념을 타입 안전하게 모델링합니다.

```csharp
// Union Value Object 정의
[UnionType]
public abstract class PaymentMethod : UnionValueObject<PaymentMethod>
{
    public sealed class CreditCard : PaymentMethod { /* ... */ }
    public sealed class BankTransfer : PaymentMethod { /* ... */ }
    public sealed class Cash : PaymentMethod { /* ... */ }
}

// 상태 전이 (타입 안전)
public Fin<Shipped> Ship() =>
    TransitionFrom<Confirmed, Shipped>(
        confirmed => new Shipped(confirmed.OrderId),
        "Order must be confirmed before shipping");
```

**Why this matters (왜 중요한가):**
- C#에서 sealed class 계층으로 대수적 데이터 타입을 구현하여 유효하지 않은 상태를 컴파일 타임에 방지합니다
- `TransitionFrom<TSource, TTarget>`으로 상태 머신 전이 규칙을 타입 시스템으로 강제합니다
- `UnreachableCaseException`으로 switch 표현식에서 도달 불가능한 케이스를 명시적으로 표현합니다

<!-- 관련 커밋: a066a9e7 feat(domain): UnionValueObject 기본 타입 추가 -->
<!-- 관련 커밋: ee88c6e6 feat(domain): DomainErrorType.InvalidTransition 추가 -->

---

#### 7. CQRS Query 패턴 및 페이지네이션

`IQueryPort<TEntity, TDto>` 인터페이스와 `PagedResult<T>`, `CursorPagedResult<T>`, `SortExpression` 등을 통해 읽기 모델 조회를 구조화합니다.

```csharp
// Query Port 정의
public interface IProductQueryPort : IQueryPort<Product, ProductDto> { }

// 페이지네이션 쿼리
var page = new PageRequest(page: 1, pageSize: 20);
var sort = SortExpression.By("Name").ThenBy("CreatedAt", SortDirection.Descending);
var result = await queryPort.Search(spec, page, sort).Run(EnvIO.New());

// 커서 기반 페이지네이션
var cursor = new CursorPageRequest(after: lastCursor, pageSize: 20);
var cursorResult = await queryPort.SearchByCursor(spec, cursor, sort).Run(EnvIO.New());
```

**Why this matters (왜 중요한가):**
- Command(쓰기)와 Query(읽기)를 별도 인터페이스로 분리하여 각각 독립적으로 최적화할 수 있습니다
- `PagedResult<T>`가 `HasNextPage`, `TotalPages` 등 페이지네이션 메타데이터를 자동 계산합니다
- `SortExpression`이 허용된 정렬 필드만 통과시켜 SQL 인젝션을 구조적으로 방지합니다
- `IAsyncEnumerable<TDto>` Stream 지원으로 대용량 데이터를 메모리 효율적으로 처리합니다

<!-- 관련 커밋: 8259af48 feat(Functorium): CQRS Query Adapter 패턴 구현 및 DapperQueryAdapterBase 추출 -->
<!-- 관련 커밋: ebc203ec feat: SortExpression.By 빈 필드명 입력 시 Empty 반환 -->

---

#### 8. IRepository 공통 인터페이스 (벌크 연산 포함)

`IRepository<TAggregate, TId>` 인터페이스가 단일 및 벌크 CRUD 연산을 모두 지원합니다.

```csharp
public interface IRepository<TAggregate, TId> : IObservablePort
    where TAggregate : AggregateRoot<TId>
    where TId : struct, IEntityId<TId>
{
    FinT<IO, TAggregate> Create(TAggregate aggregate);
    FinT<IO, Seq<TAggregate>> CreateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, TAggregate> GetById(TId id);
    FinT<IO, Seq<TAggregate>> GetByIds(IReadOnlyList<TId> ids);
    FinT<IO, TAggregate> Update(TAggregate aggregate);
    FinT<IO, Seq<TAggregate>> UpdateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, int> Delete(TId id);
    FinT<IO, int> DeleteRange(IReadOnlyList<TId> ids);
}
```

**Why this matters (왜 중요한가):**
- `FinT<IO, T>` 반환 타입으로 모든 Repository 연산이 예외 대신 에러를 명시적으로 반환합니다
- 벌크 메서드(`CreateRange`, `GetByIds` 등)가 N+1 문제를 구조적으로 방지합니다
- `IObservablePort`를 구현하여 모든 Repository 호출이 자동으로 관측성 파이프라인에 통합됩니다
- AggregateRoot 제약으로 Repository가 항상 Aggregate 단위로 동작함을 컴파일 타임에 보장합니다

<!-- 관련 커밋: 7555a7ff feat(domains): IRepository<TAggregate, TId> 공통 인터페이스 구현 -->
<!-- 관련 커밋: a1dc7ce7 feat: IRepository 벌크 메서드 및 DomainEventCollector O(n) 최적화 추가 -->

---

#### 9. FinT LINQ 확장 및 Unwrap

`FinT<M, A>` 모나드를 위한 LINQ 확장 메서드를 제공합니다. `Fin`, `IO`, `Validation` 간 자연스러운 합성을 지원합니다.

```csharp
// from ... select 구문으로 FinT 합성
var result =
    from product in repository.GetById(productId)
    from validated in Fin.Succ(product).SelectMany(p => validateUpdate(p))
    select updated;

// Fin<T>.Unwrap() - ThrowIfFail 대안
var value = fin.Unwrap(); // Fail이면 예외 발생

// Validation → FinT 변환
var finT =
    from name in ProductName.Validate(input)  // Validation<Error, T>
    from result in repository.Create(product) // FinT<IO, T>
    select result;
```

**Why this matters (왜 중요한가):**
- `Fin`, `IO`, `Validation` 세 모나드를 자유롭게 합성하여 비즈니스 로직을 파이프라인으로 표현할 수 있습니다
- `SelectMany` 확장으로 `from ... select` LINQ 구문을 사용할 수 있어 가독성이 크게 향상됩니다
- `Filter`로 조건부 분기를 함수형으로 처리하여 `if` 문을 제거합니다
- `TraverseSerial`로 컬렉션의 각 요소에 비동기 연산을 순차 적용하면서 첫 번째 실패에서 중단합니다

<!-- 관련 커밋: 7408a3df feat(linq): FinT SelectMany 역방향 체이닝 확장 추가 -->
<!-- 관련 커밋: cc1bb647 feat(linq): Validation → FinT 변환 SelectMany 확장 메서드 추가 -->

---

#### 10. ctx.* 관측성 컨텍스트 전파

`CtxEnricher` 인터페이스와 소스 생성기를 통해 구조화된 로그 컨텍스트를 자동 전파합니다. `[CtxRoot]`, `[CtxTarget]`, `[CtxIgnore]` 어트리뷰트로 필드를 선언적으로 제어합니다.

```csharp
// Usecase CtxEnricher 정의
public class CreateProductCtxEnricher
    : IUsecaseCtxEnricher<CreateProductCommand, FinResponse<ProductId>>
{
    public IDisposable? EnrichRequest(CreateProductCommand request) =>
        CtxEnricherContext.Push("product.name", request.Name);

    public IDisposable? EnrichResponse(
        CreateProductCommand request,
        FinResponse<ProductId> response) =>
        response.IsSucc
            ? CtxEnricherContext.Push("product.id", response.ThrowIfFail().ToString())
            : null;
}
```

**Why this matters (왜 중요한가):**
- `CtxPillar` 플래그로 Logging, Tracing, Metrics 세 pillar에 동일한 컨텍스트를 선택적으로 전파합니다
- 소스 생성기가 인터페이스 기반으로 CtxEnricher 구현체를 자동 생성하여 수동 작성 대비 80% 이상 코드를 절감합니다
- Serilog 의존성이 Application 레이어에서 제거되어 도메인 순수성이 유지됩니다
- `ObservableSignal` 정적 API로 인프라 레이어 어디서든 구조화된 로그를 생성할 수 있습니다

<!-- 관련 커밋: 042d3173 feat(observability): LogEnricher 인터페이스 Application 레이어 이동 + Serilog 의존성 제거 -->
<!-- 관련 커밋: 81233196 feat(source-generator): LogEnricher 소스 제너레이터 구현 -->

---

#### 11. FluentValidation 통합 확장

CQRS Command/Query 요청의 검증을 위한 FluentValidation 확장 메서드를 제공합니다.

```csharp
public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .MustSatisfyValidationOf<CreateProductCommand, string, ProductName>(
                ProductName.Validate);

        RuleFor(x => x.CategoryId)
            .MustBeEntityId<CreateProductCommand, CategoryId>();

        RuleFor(x => x.Status)
            .MustBeEnum<CreateProductCommand, ProductStatus>();
    }
}
```

**Why this matters (왜 중요한가):**
- `MustSatisfyValidation`으로 도메인 Value Object의 `Validate` 메서드를 FluentValidation 규칙으로 직접 재사용합니다
- `MustBeEntityId<TEntityId>`로 문자열 형식의 ID 입력을 Ulid 파싱과 함께 자동 검증합니다
- `MustBeEnum<TSmartEnum>`으로 SmartEnum 값 검증을 한 줄로 처리합니다
- `MustBePairedRange`로 Min/Max 쌍 검증(Optional 포함)을 표준화합니다

<!-- 관련 커밋: 190cf8db feat: FluentValidation MustBeEntityId, MustBeOneOf 확장 메서드 추가 -->
<!-- 관련 커밋: 113e8afb feat(domains): FluentValidation ValueObject 확장 기능 추가 -->
<!-- 관련 커밋: 1577abd4 feat(validation): Option<T> 선택적 필터 검증 확장 메서드 추가 -->

---

### Functorium.Adapters 라이브러리

#### 1. OpenTelemetryBuilder 및 PipelineConfigurator

관측성 설정을 빌더 패턴으로 구성합니다. Logging, Metrics, Tracing, Pipeline을 독립적으로 설정할 수 있으며, Opt-in 모델로 필요한 파이프라인만 활성화합니다.

```csharp
services.RegisterOpenTelemetry(configuration, projectAssembly)
    .ConfigureLogging(l => l
        .AddDestructuringPolicy<ErrorsDestructuringPolicy>())
    .ConfigureMetrics(m => m
        .AddMeter("MyApp.Custom"))
    .ConfigureTracing(t => t
        .AddSource("MyApp.Custom"))
    .ConfigurePipelines(p => p
        .UseObservability()  // Logging + Metrics + Tracing + Exception
        .UseValidation()
        .UseTransaction()
        .UseCaching()
        .UseCtxEnricher())
    .Build();
```

**Why this matters (왜 중요한가):**
- Opt-in 모델로 필요한 파이프라인만 활성화하여 불필요한 오버헤드를 제거합니다
- `UseObservability()` 단일 호출로 Logging, Metrics, Tracing, Exception 파이프라인을 한번에 활성화합니다
- 커스텀 파이프라인 확장 포인트(`AddCustomPipeline<T>()`)로 비즈니스 관심사를 파이프라인에 추가할 수 있습니다
- `ConfigureStartupLogger()`로 애플리케이션 시작 시 설정값을 로그로 출력하여 운영 디버깅을 지원합니다

<!-- 관련 커밋: a9e3e96b feat(pipelines): PipelineConfigurator를 통한 파이프라인 캡슐화 -->
<!-- 관련 커밋: 4a08b441 refactor(pipeline)!: 파이프라인 단계를 opt-in 모델로 전환 -->
<!-- 관련 커밋: 391ca88b feat(observability): 커스텀 파이프라인 확장 포인트 추가 -->

---

#### 2. EfCoreRepositoryBase

EF Core 기반 Repository 구현의 보일러플레이트를 제거하는 제네릭 베이스 클래스입니다. N+1 문제를 구조적으로 방지하고, Specification을 EF Core 쿼리로 자동 변환합니다.

```csharp
public sealed class EfCoreProductRepository
    : EfCoreRepositoryBase<Product, ProductId, ProductModel>,
      IProductRepository
{
    public EfCoreProductRepository(
        AppDbContext dbContext,
        IDomainEventCollector eventCollector)
        : base(eventCollector,
               applyIncludes: q => q.Include(p => p.Variants),
               propertyMap: new PropertyMap<Product, ProductModel>()
                   .Map(p => p.Name, m => m.ProductName))
    {
        _dbContext = dbContext;
    }

    protected override DbContext DbContext => _dbContext;
    protected override DbSet<ProductModel> DbSet => _dbContext.Products;
    protected override Product ToDomain(ProductModel model) => /* ... */;
    protected override ProductModel ToModel(Product aggregate) => /* ... */;
}
```

**Why this matters (왜 중요한가):**
- CRUD 8개 메서드(Create/Read/Update/Delete + Range 벌크)를 자동으로 구현하여 Repository 당 100줄 이상의 반복 코드를 제거합니다
- `applyIncludes` 생성자 파라미터로 N+1 문제를 구조적으로 방지합니다 (Include가 모든 쿼리에 자동 적용)
- `PropertyMap`을 통해 Specification의 도메인 Expression을 EF Core 모델 쿼리로 자동 변환합니다
- `IDomainEventCollector`가 자동 주입되어 Create/Update 시 Aggregate의 도메인 이벤트를 투명하게 수집합니다

<!-- 관련 커밋: 6cd7ca21 feat(adapters): EfCoreRepositoryBase 추가로 N+1 문제 구조적 방지 -->
<!-- 관련 커밋: 406fae14 perf(adapters): Repository/Query 베이스 클래스 전면 개선 -->

---

#### 3. DapperQueryBase 및 InMemoryQueryBase

Dapper SQL 쿼리와 InMemory 쿼리를 위한 베이스 클래스입니다. `IQueryPort<TEntity, TDto>` 인터페이스를 구현하며, 페이지네이션과 커서 기반 쿼리를 내장합니다.

```csharp
// Dapper Query 구현
public sealed class DapperProductQuery : DapperQueryBase<Product, ProductDto>
{
    protected override string SelectSql => "SELECT Id, Name, Price FROM Products";
    protected override string CountSql => "SELECT COUNT(*) FROM Products";
    protected override string DefaultOrderBy => "Name ASC";
    protected override Dictionary<string, string> AllowedSortColumns => new()
    {
        ["Name"] = "Name", ["Price"] = "Price"
    };
}
```

**Why this matters (왜 중요한가):**
- SQL 쿼리 최적화와 페이지네이션/정렬 로직을 분리하여 개발자가 핵심 SQL에만 집중할 수 있습니다
- `DapperSpecTranslator`로 도메인 Specification을 SQL WHERE 절로 변환하여 타입 안전한 동적 쿼리를 지원합니다
- `InMemoryQueryBase`로 통합 테스트 시 실제 데이터베이스 없이 쿼리 로직을 검증할 수 있습니다
- `IAsyncEnumerable<TDto>` Stream 지원으로 대용량 데이터를 메모리 효율적으로 처리합니다

<!-- 관련 커밋: f16e8aa8 feat(adapters): InMemoryQueryBase 베이스 클래스 추가 및 DapperQueryBase 이름 변경 -->
<!-- 관련 커밋: 8259af48 feat(Functorium): CQRS Query Adapter 패턴 구현 및 DapperQueryAdapterBase 추출 -->

---

#### 4. UsecaseTransactionPipeline 및 UsecaseCachingPipeline

UnitOfWork 기반 트랜잭션과 IMemoryCache 기반 캐싱을 파이프라인으로 제공합니다.

```csharp
// 트랜잭션 파이프라인: UseTransaction()으로 활성화
// Usecase 핸들러 내부에서 트랜잭션이 자동 관리됨:
// BeginTransaction → Handle → SaveChanges → PublishTrackedEvents → Commit

// 캐싱 파이프라인: Query에 ICacheable 구현
public sealed record GetProductQuery(string ProductId)
    : IQueryRequest<ProductDto>, ICacheable
{
    public string CacheKey => $"product:{ProductId}";
    public TimeSpan? Duration => TimeSpan.FromMinutes(5);
}
```

**Why this matters (왜 중요한가):**
- `IUnitOfWork`와 `IUnitOfWorkTransaction`으로 트랜잭션 범위를 명시적으로 제어합니다
- 트랜잭션 파이프라인이 SaveChanges와 DomainEvent 발행을 원자적으로 처리하여 데이터 불일치를 방지합니다
- `ICacheable` 인터페이스만 구현하면 캐싱이 자동 적용되어 핸들러 코드 수정 없이 성능을 개선합니다
- 파이프라인 기반이므로 트랜잭션/캐싱 로직이 비즈니스 로직과 완전히 분리됩니다

<!-- 관련 커밋: 29ace14d feat: UsecaseTransactionPipeline 및 도메인 이벤트 수집 인프라 구현 -->
<!-- 관련 커밋: dc718a00 feat(pipelines): IMemoryCache 기반 UsecaseCachingPipeline 구현 -->

---

#### 5. Observable Domain Event Publisher

도메인 이벤트 발행/처리에 대한 완전한 관측성을 제공합니다. 로깅, 메트릭, 분산 추적이 자동으로 적용됩니다.

```csharp
// DI 등록
services.RegisterDomainEventPublisher();
services.RegisterDomainEventHandlersFromAssembly(typeof(Program).Assembly);

// 자동 관측성: 로그 필드
// request.event.type: "ProductCreatedEvent"
// request.event.id: "01J..."
// response.status: "success"
// response.elapsed: 0.005
```

**Why this matters (왜 중요한가):**
- `ObservableDomainEventPublisher`가 모든 이벤트 발행에 로깅, 메트릭, 트레이싱을 투명하게 추가합니다
- `request.event.type`, `request.event.id` 필드로 분산 시스템에서 이벤트를 추적할 수 있습니다
- 핸들러별 성공/실패 메트릭과 소요 시간이 자동 수집되어 SLO 모니터링이 가능합니다
- `PublishResult`로 부분 실패 시나리오(일부 핸들러 성공, 일부 실패)를 명시적으로 처리합니다

<!-- 관련 커밋: 64a6e77c feat(event): DomainEvent Publisher/Handler에 Metrics 기능 추가 -->
<!-- 관련 커밋: 5ad79092 feat(observability): DomainEvent Handler 로깅에 request.event.type/request.event.id 필드 추가 -->

---

### Functorium.Testing 라이브러리

#### 1. 아키텍처 테스트 Suite 클래스

`DomainArchitectureTestSuite`와 `ApplicationArchitectureTestSuite`로 도메인 아키텍처 규칙을 자동 검증합니다. 상속만으로 20개 이상의 표준 규칙이 즉시 적용됩니다.

```csharp
// 도메인 아키텍처 테스트 - 상속만으로 규칙 적용
public class MyDomainArchTests : DomainArchitectureTestSuite
{
    protected override string DomainNamespace => "MyApp.Domain";
    protected override Architecture Architecture => /* ArchUnitNET Architecture */;
}
// 자동 검증 규칙 (일부):
// - AggregateRoot_ShouldBe_PublicSealedClass
// - ValueObject_ShouldBe_Immutable
// - ValueObject_ShouldHave_CreateFactoryMethod
// - Entity_ShouldHave_AllPrivateConstructors
// - Specification_ShouldInherit_SpecificationBase
// - DomainService_ShouldBe_Stateless
// - DomainEvent_ShouldBe_SealedRecord
```

**Why this matters (왜 중요한가):**
- 상속 한 줄로 Value Object 불변성, Entity 생성자 접근 제어, Specification 상속 등 20개 이상의 DDD 규칙이 CI에서 자동 검증됩니다
- `ClassValidator`/`MethodValidator` fluent API로 프로젝트 특화 규칙을 추가할 수 있습니다
- `IArchRule<T>` 인터페이스와 `CompositeArchRule`로 재사용 가능한 규칙을 조합할 수 있습니다
- `ImmutabilityRule`이 setter, mutable 필드, collection 노출 등 불변성 위반을 포괄적으로 감지합니다

<!-- 관련 커밋: 5af2b12b refactor: 아키텍처 테스트 Suite 클래스 기반으로 재설계 -->
<!-- 관련 커밋: cf751136 feat(testing): ClassValidator/MethodValidator 아키텍처 검증 메서드 추가 -->

---

#### 2. 레이어별 ErrorAssertions 테스트 헬퍼

`DomainErrorAssertions`, `ApplicationErrorAssertions`, `AdapterErrorAssertions`로 에러 타입을 간결하게 검증합니다.

```csharp
// 도메인 에러 검증
var result = ProductName.Validate("");
result.ShouldHaveDomainError<ProductName, string>(
    new DomainErrorType.Empty());

// 애플리케이션 에러 검증
error.ShouldBeApplicationError<CreateProductCommand>(
    new ApplicationErrorType.NotFound());

// Fin<T> 직접 검증
fin.ShouldBeAdapterError<ProductRepository, Product>(
    new AdapterErrorType.NotFound());
```

**Why this matters (왜 중요한가):**
- `ShouldBeXxxError`/`ShouldHaveXxxError` 확장 메서드로 에러 검증 코드가 3~5줄에서 1줄로 줄어듭니다
- 레이어별 전용 Assertion으로 에러 코드, 에러 타입, 현재 값을 한번에 검증합니다
- `Fin<T>`, `Validation<Error, T>`, `Error` 세 타입 모두에 대한 Assertion을 제공합니다
- Exceptional 에러(`ShouldBeXxxExceptionalError`)로 예외 기반 에러도 동일한 패턴으로 검증합니다

<!-- 관련 커밋: ae291a4c feat(testing): 레이어별 ErrorAssertions 테스트 헬퍼 추가 -->
<!-- 관련 커밋: c810709d feat(testing): DomainErrorAssertions 테스트 헬퍼 추가 -->

---

#### 3. LogTestContext 구조화 로그 테스트

Serilog 기반 구조화된 로그를 단위 테스트에서 캡처하고 검증합니다.

```csharp
using var logCtx = new LogTestContext(LogEventLevel.Debug, enrichFromLogContext: true);
var logger = logCtx.CreateLogger<MyHandler>();

// 핸들러 실행 후 로그 검증
logCtx.LogCount.Should().Be(2);
var requestLog = logCtx.GetFirstLog();
var data = logCtx.ExtractFirstLogData();
// 구조화된 필드 검증 가능
```

**Why this matters (왜 중요한가):**
- 관측성 파이프라인이 올바른 구조화된 필드를 생성하는지 단위 테스트로 검증할 수 있습니다
- `ExtractLogData()`로 로그 이벤트에서 구조화된 데이터를 추출하여 필드별 Assertion이 가능합니다
- `LogContext` 기반 Enrichment도 함께 캡처하여 ctx.* 컨텍스트 전파를 검증할 수 있습니다

<!-- 관련 커밋: a5e85cd5 feat(test): Application Layer 로그 필드 검증 테스트 추가 -->

## 버그 수정

- `GetType()` 호출 시 `AccessViolationException` 방어 처리 (`97cffb08`)
- Source Generator의 `ErrorCode` 타입 네임스페이스 및 접근 제어자 수정 (`33160d52`)
- Source Generator의 `request.handler.method` 태그에 실제 메서드 이름 사용 (`4c0c738c`)
- 빌드 커버리지 TRX 파일 읽기 시 `FileShare.ReadWrite` 추가 (`11503c7c`)

## API 변경사항

### Functorium 네임스페이스 구조

```
Functorium
├── Abstractions/
│   ├── Diagnostics/          CrashDumpHandler
│   ├── Errors/               ErrorType, ErrorCodeFactory, IHasErrorCode
│   ├── Observabilities/      IObservablePort, CtxPillar, CtxEnricherContext,
│   │                         ObservableSignal, CtxIgnore/Root/TargetAttribute
│   ├── Registrations/        ObservablePortRegistration
│   └── Utilities/            IEnumerableUtilities, StringUtilities
├── Applications/
│   ├── Errors/               ApplicationError, ApplicationErrorType
│   ├── Events/               IDomainEventCollector, IDomainEventPublisher,
│   │                         IDomainEventHandler<T>, PublishResult
│   ├── Linq/                 FinTLinqExtensions (SelectMany, Filter, Unwrap)
│   ├── Observabilities/      IUsecaseCtxEnricher, IDomainEventCtxEnricher
│   ├── Persistence/          IUnitOfWork, IUnitOfWorkTransaction
│   ├── Queries/              IQueryPort, PagedResult, CursorPagedResult,
│   │                         SortExpression, SortDirection, PageRequest
│   ├── Usecases/             FinResponse<T>, ICommandRequest, IQueryRequest,
│   │                         ICacheable, IFinResponse
│   └── Validations/          FluentValidationExtensions
└── Domains/
    ├── Entities/             Entity<TId>, AggregateRoot<TId>, IEntityId<T>,
    │                         IAuditable, ISoftDeletable, IConcurrencyAware
    ├── Errors/               DomainError, DomainErrorType
    ├── Events/               DomainEvent, IDomainEvent, IHasDomainEvents
    ├── Repositories/         IRepository<TAggregate, TId>
    ├── Services/             IDomainService
    ├── Specifications/       Specification<T>, ExpressionSpecification<T>,
    │                         PropertyMap<TEntity, TModel>
    └── ValueObjects/
        ├── Unions/           UnionValueObject, UnionTypeAttribute
        └── Validations/
            ├── Contextual/   ValidationRules, ValidationContext,
            │                 ContextualValidation<T>
            └── Typed/        TypedValidation<TV, T>,
                              TypedValidationExtensions
```

### Functorium.Adapters 네임스페이스 구조

```
Functorium.Adapters
├── Abstractions/
│   ├── Errors/               ErrorsDestructuringPolicy, IErrorDestructurer
│   ├── Options/              OptionsConfigurator
│   └── Registrations/        DomainEventRegistration, OpenTelemetryRegistration
├── Errors/                   AdapterError, AdapterErrorType
├── Events/                   DomainEventPublisher, ObservableDomainEventPublisher,
│                             ObservableDomainEventNotificationPublisher
├── Observabilities/
│   ├── Builders/             OpenTelemetryBuilder
│   │   └── Configurators/    LoggingConfigurator, MetricsConfigurator,
│   │                         TracingConfigurator, PipelineConfigurator
│   ├── Contexts/             MetricsTagContext
│   ├── Formatters/           OpenSearchJsonFormatter
│   ├── Loggers/              StartupLogger, IStartupOptionsLogger
│   └── Naming/               ObservabilityNaming (Categories, Metrics, Spans 등)
├── Pipelines/                UsecasePipelineBase, UsecaseLoggingPipeline,
│                             UsecaseMetricsPipeline, UsecaseTracingPipeline,
│                             UsecaseValidationPipeline, UsecaseExceptionPipeline,
│                             UsecaseTransactionPipeline, UsecaseCachingPipeline,
│                             UsecaseMetricCustomPipelineBase, ICustomUsecasePipeline
├── Repositories/             EfCoreRepositoryBase, InMemoryRepositoryBase,
│                             InMemoryQueryBase, DapperQueryBase, DapperSpecTranslator
└── SourceGenerators/         GenerateObservablePortAttribute,
                              ObservablePortIgnoreAttribute
```

### Functorium.Testing 네임스페이스 구조

```
Functorium.Testing
├── Actions/SourceGenerators/  SourceGeneratorTestRunner
├── Arrangements/
│   ├── Effects/              FinTFactory
│   ├── Hosting/              HostTestFixture<T>
│   ├── Loggers/              TestSink
│   ├── Logging/              LogTestContext, StructuredTestLogger<T>
│   └── ScheduledJobs/        QuartzTestFixture<T>, JobCompletionListener
└── Assertions/
    ├── ArchitectureRules/    ClassValidator, InterfaceValidator, MethodValidator,
    │                         TypeValidator<T, TSelf>, IArchRule<T>,
    │                         CompositeArchRule, DelegateArchRule,
    │                         ArchitectureValidationEntryPoint, ValidationResultSummary
    │   ├── Rules/            ImmutabilityRule
    │   └── Suites/           DomainArchitectureTestSuite,
    │                         ApplicationArchitectureTestSuite
    └── Errors/               DomainErrorAssertions, ApplicationErrorAssertions,
                              AdapterErrorAssertions, ErrorCodeAssertions
```

## 설치

### NuGet 패키지 설치

```bash
# Functorium 핵심 라이브러리
dotnet add package Functorium --version 1.0.0-alpha.2

# Functorium.Adapters (Pipeline, Observability, Repository 구현체)
dotnet add package Functorium.Adapters --version 1.0.0-alpha.2

# Functorium.Testing 테스트 라이브러리 (선택적)
dotnet add package Functorium.Testing --version 1.0.0-alpha.2
```

### 필수 의존성

- .NET 10 이상
- LanguageExt.Core 5.0.0-beta-77
- Mediator.Abstractions 3.x
- FluentValidation 11.x
- Ardalis.SmartEnum 8.x
