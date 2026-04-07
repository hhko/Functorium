# Functorium Release v1.0.0-alpha.2

**English** | **[한국어](https://github.com/hhko/Functorium/blob/v1.0.0-alpha.2/.release-notes/RELEASE-v1.0.0-alpha.2-KR.md)**

## Overview

Functorium v1.0.0-alpha.2 is a major release centered on the **DDD domain modeling framework** and the **Functorium.Adapters project separation**. It provides the complete set of DDD building blocks--Entity, AggregateRoot, Specification, Domain Event, and more--enabling full-fledged domain-centric functional architecture. The Observability system has been fundamentally redesigned around ctx.* context propagation.

**Key Features**:

- **DDD Domain Modeling**: Complete DDD building block framework including Entity, AggregateRoot, Specification, Domain Event, Domain Service, and IRepository
- **Layer-specific Type-safe Error System**: Sealed record-based error type hierarchy separated into DomainError, ApplicationError, and AdapterError
- **Functorium.Adapters Project Separation**: Pipeline, Observability, and Repository implementations extracted into an independent package to minimize dependencies
- **Validation System Extensions**: Contextual/Typed validation, Apply/ApplyT patterns, FluentValidation integration
- **ctx.* Observability Redesign**: Structured log context propagation based on CtxEnricher + source generators
- **Architecture Test Suites**: Automated rule verification with DomainArchitectureTestSuite and ApplicationArchitectureTestSuite

## Breaking Changes

### 1. Functorium.Adapters Project Separation

Pipeline, Observability, and Repository implementations have been moved from the `Functorium` package to `Functorium.Adapters`. Abstractions (interfaces/abstract types) remain in `Functorium`.

**Before (v1.0.0-alpha.1)**:
```csharp
using Functorium.Adapters.Registrations;    // AdapterPipelineRegistration
using Functorium.Observabilities;           // OpenTelemetryBuilder
using Functorium.Applications.Pipelines;    // UsecaseLoggingPipeline
```

**After (v1.0.0-alpha.2)**:
```csharp
using Functorium.Adapters.Abstractions.Registrations;    // OpenTelemetryRegistration
using Functorium.Adapters.Observabilities.Builders;      // OpenTelemetryBuilder
using Functorium.Adapters.Pipelines;                     // UsecaseLoggingPipeline
```

**Migration Guide**:
1. Add the NuGet package `Functorium.Adapters`: `dotnet add package Functorium.Adapters`
2. Change `Functorium.Observabilities.*` namespaces to `Functorium.Adapters.Observabilities.*`
3. Change `Functorium.Applications.Pipelines.*` to `Functorium.Adapters.Pipelines.*`

<!-- Related commit: 6f859dab refactor: Functorium.Adapters 프로젝트 분리 및 Abstractions 네임스페이스 변경 -->
<!-- Related commit: 8894c2a4 refactor(adapters)!: 기술 관심사 단위로 Adapters 폴더 재구성 -->

---

### 2. Pipeline Opt-in Model Transition

All pipeline stages are now disabled by default and must be explicitly enabled in `ConfigurePipelines`.

**Before (v1.0.0-alpha.1)**:
```csharp
// All pipelines were automatically registered
services.RegisterOpenTelemetry(configuration, assembly)
    .Build();
```

**After (v1.0.0-alpha.2)**:
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

**Migration Guide**:
1. Add `.ConfigurePipelines()` after the `RegisterOpenTelemetry()` call
2. Activate only the pipelines you need using `.UseXxx()` methods
3. Use `.UseObservability()` to enable all observability pipelines at once

<!-- Related commit: 4a08b441 refactor(pipeline)!: 파이프라인 단계를 opt-in 모델로 전환 -->

---

### 3. Custom ErrorType Changed to Sealed Record Derivation

Custom error type definitions have changed from string-based to type-safe sealed record derivation.

**Before (v1.0.0-alpha.1)**:
```csharp
// String-based custom error
var error = Error.New("CustomError", "Something went wrong");
```

**After (v1.0.0-alpha.2)**:
```csharp
// Type-safe sealed record derivation
public sealed class InsufficientStock : DomainErrorType.Custom;

var error = DomainError.For<Product>(
    new DomainErrorType.InsufficientStock(),
    currentQuantity,
    "Requested quantity exceeds available stock");
```

**Migration Guide**:
1. Convert existing string-based errors to sealed records deriving from `DomainErrorType.Custom`, `ApplicationErrorType.Custom`, or `AdapterErrorType.Custom`
2. Use the `DomainError.For<TDomain>(errorType, currentValue, message)` factory method
3. Switch to pattern matching for error matching: `error is DomainErrorType.NotFound`

<!-- Related commit: e28eee6f refactor!: Custom ErrorType을 문자열 기반에서 타입 안전 sealed record 파생으로 변경 -->

---

### 4. Observabilities Namespace Relocation

Observability abstractions such as IObservablePort and CtxPillar have been moved to `Functorium.Abstractions.Observabilities`.

**Before (v1.0.0-alpha.1)**:
```csharp
using Functorium.Applications.Observabilities;
```

**After (v1.0.0-alpha.2)**:
```csharp
using Functorium.Abstractions.Observabilities;  // IObservablePort, CtxPillar, etc.
```

**Migration Guide**:
1. Change the `Functorium.Applications.Observabilities` namespace to `Functorium.Abstractions.Observabilities`
2. The `IAdapter` interface has been renamed to `IObservablePort`

<!-- Related commit: 70860eee refactor!: Observabilities를 Abstractions로 이동하여 레이어 응집도 개선 -->

---

### 5. SourceGenerator Project Renamed

The source generator package has been renamed.

**Before**: `Functorium.SourceGenerators` (or `Functorium.Adapters.SourceGenerator`)
**After**: `Functorium.Adapters.SourceGenerators`

**Migration Guide**:
1. Update the NuGet package reference to `Functorium.Adapters.SourceGenerators`
2. `GenerateObservablePortAttribute` is located in the `Functorium.Adapters.SourceGenerators` namespace

<!-- Related commit: eb00ce14 refactor!: SourceGenerator 프로젝트 이름 변경 및 네임스페이스 재구성 -->
<!-- Related commit: dee70449 refactor!: Functorium.Adapters.SourceGenerator 이름 변경 -->

## New Features

### Functorium Library

#### 1. DDD Entity and AggregateRoot Framework

Provides the core DDD building blocks through Entity and AggregateRoot base classes. Eliminates ID type boilerplate with Ulid-based `IEntityId<T>` constraints and the `GenerateEntityIdAttribute` source generator.

```csharp
// ID type definition - auto-generated via GenerateEntityIdAttribute
[GenerateEntityId]
public partial struct ProductId;

// AggregateRoot definition
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

**Why this matters:**
- Entity/AggregateRoot base classes eliminate 50+ lines of boilerplate for equality, hash codes, domain event collection, and more
- The `IParsable<T>` constraint on `IEntityId<T>` enables type-safe ID parsing from API inputs
- `GenerateEntityIdAttribute` auto-generates the full ID type implementation (Create, New, Parse, CompareTo, Equals)
- Immutable Entities with factory method patterns prevent invalid domain object creation at compile time

<!-- Related commit: 7555a7ff feat(domains): IRepository<TAggregate, TId> 공통 인터페이스 구현 -->
<!-- Related commit: adfa72c8 feat: IEntityId에 IParsable<T> 제약 추가 -->
<!-- Related commit: 3c5ef59e feat(source-generator): Attribute 정의를 Functorium 라이브러리에 추가 -->

---

#### 2. Specification Pattern Framework

Encapsulates domain business rules as reusable Specification objects. Supports And/Or/Not composition and Expression-based automatic translation for conversion to EF Core/Dapper queries.

```csharp
// Expression-based Specification definition
public sealed class ActiveProductSpec : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression() =>
        product => product.IsActive;
}

// Specification composition
Specification<Product> spec = new ActiveProductSpec() & new InStockSpec();
bool satisfies = spec.IsSatisfiedBy(product);

// Automatic Expression translation (Domain → Infrastructure model)
var map = new PropertyMap<Product, ProductModel>()
    .Map(p => p.Name, m => m.ProductName);
Expression<Func<ProductModel, bool>> dbExpr = map.Translate(spec.ToExpression());
```

**Why this matters:**
- Business rules are cohesively located in the domain layer, preventing them from scattering across Repository implementations
- The `&`, `|`, `!` operators enable intuitive composition of complex business rules
- `PropertyMap<TEntity, TModel>.Translate()` automatically converts domain Expressions to infrastructure model queries, eliminating manual mapping errors
- `Specification<T>.All` provides a type-safe way to express unfiltered full queries

<!-- Related commit: da61b6d7 feat(specification): Specification 패턴 프레임워크 추가 -->
<!-- Related commit: c7704dea feat(specifications): Expression 기반 Specification 자동 번역 구현 -->

---

#### 3. Layer-specific Type-safe Error System

Provides dedicated error types for each of the three layers: Domain, Application, and Adapter. All error types are defined as sealed records enabling pattern matching, and derive from the `ErrorType` abstract record.

```csharp
// Domain error - 30+ detailed types
var error = DomainError.For<ProductName>(
    new DomainErrorType.TooLong(MaxLength: 100),
    name,
    "Product name exceeds maximum length");

// Application error
var error = ApplicationError.For<CreateProductCommand>(
    new ApplicationErrorType.AlreadyExists(),
    productId,
    "Product with this ID already exists");

// Custom error type extension
public sealed class InsufficientStock : DomainErrorType.Custom;
```

**Why this matters:**
- Layer-specific error type separation immediately identifies whether the error origin is Domain, Application, or Adapter
- The sealed record base ensures the compiler warns about missing cases in `switch` pattern matching
- Structured errors like `DomainErrorType.TooLong(MaxLength: 100)` include metadata, simplifying logging and user message generation
- Error codes are auto-generated in the format `DomainErrors.ProductName.TooLong`, integrating with the Observability system

<!-- Related commit: 781d4d20 feat: 레이어별 Error 타입 및 헬퍼 클래스 추가 -->
<!-- Related commit: af1b1b33 feat(abstractions): ErrorType 기본 추상 record 추가 -->
<!-- Related commit: e28eee6f refactor!: Custom ErrorType을 문자열 기반에서 타입 안전 sealed record 파생으로 변경 -->

---

#### 4. Domain Event System

Provides a complete infrastructure for domain event publishing and handling. Events are collected in `AggregateRoot`, published through `IDomainEventPublisher`, and processed by `IDomainEventHandler<TEvent>`.

```csharp
// Domain event definition
public sealed record ProductCreatedEvent(ProductId ProductId)
    : DomainEvent;

// Publishing events from AggregateRoot
public static Fin<Product> Create(ProductName name)
{
    var product = new Product(ProductId.New(), name);
    product.AddDomainEvent(new ProductCreatedEvent(product.Id));
    return Fin.Succ(product);
}

// Event handler
public sealed class ProductCreatedHandler
    : IDomainEventHandler<ProductCreatedEvent>
{
    public async ValueTask Handle(
        ProductCreatedEvent notification,
        CancellationToken cancellationToken) { /* ... */ }
}
```

**Why this matters:**
- Removes coupling between AggregateRoots, allowing each Aggregate to evolve independently
- `IDomainEventCollector` collects events within transaction boundaries, ensuring data consistency
- `PublishTrackedEvents()` atomically handles persistence and event publishing
- Mediator-based handler auto-discovery means no existing code modifications are needed when adding new events

<!-- Related commit: 1c4d948d feat(domain-event): IDomainEventPublisher 및 도메인 이벤트 발행 기능 추가 -->
<!-- Related commit: 57982399 feat(domain-event): 도메인 이벤트 처리 시스템 개선 -->

---

#### 5. Contextual/Typed Validation System

Provides two fluent APIs for domain Value Object validation: the Contextual approach via `ValidationRules.For("context")` and the Typed approach via `TypedValidation<TV, T>`.

```csharp
// Contextual validation - field context automatically included
public static Validation<Error, ProductName> Validate(string name) =>
    (ValidationRules.For(nameof(Name)).NotEmpty(name).ThenNormalize(s => s.Trim()).ThenMaxLength(100),
     ValidationRules.For(nameof(Name)).IsLowerCase(name))
    .Apply((trimmed, _) => new ProductName(trimmed));

// Apply/ApplyT pattern - collects all validation errors
var result = (
    ProductName.Validate(cmd.Name),
    Quantity.Validate(cmd.Quantity),
    Price.Validate(cmd.Price)
).Apply((name, qty, price) => new Product(name, qty, price));

// ApplyT - Fin→FinT<IO, T> lifting
var finT = (nameResult, qtyResult).ApplyT((name, qty) => (name, qty));
```

**Why this matters:**
- The `Apply` pattern collects all validation errors at once instead of halting at the first error
- `ValidationRules.For("Name")` automatically includes field context in error messages, allowing clients to immediately identify which field has the issue
- `ThenNormalize().ThenMaxLength()` chaining handles normalization and validation in a single pipeline
- `ApplyT` lifts `Fin<T>` results to `FinT<IO, T>`, naturally connecting validation with business logic

<!-- Related commit: 7dafe900 feat(domain): DomainError 헬퍼 및 ValidationRules 라이브러리 추가 -->
<!-- Related commit: 47d88180 feat(validation): FinApplyExtensions.ApplyT 추가 및 CreateProductCommand 참조 구현 -->

---

#### 6. UnionValueObject Type System

Supports algebraic data type (ADT) based Union Value Objects. Type-safely models domain concepts with a limited set of variants, such as state machines and payment methods.

```csharp
// Union Value Object definition
[UnionType]
public abstract class PaymentMethod : UnionValueObject<PaymentMethod>
{
    public sealed class CreditCard : PaymentMethod { /* ... */ }
    public sealed class BankTransfer : PaymentMethod { /* ... */ }
    public sealed class Cash : PaymentMethod { /* ... */ }
}

// State transition (type-safe)
public Fin<Shipped> Ship() =>
    TransitionFrom<Confirmed, Shipped>(
        confirmed => new Shipped(confirmed.OrderId),
        "Order must be confirmed before shipping");
```

**Why this matters:**
- Implements algebraic data types in C# using sealed class hierarchies, preventing invalid states at compile time
- `TransitionFrom<TSource, TTarget>` enforces state machine transition rules through the type system
- `UnreachableCaseException` explicitly represents unreachable cases in switch expressions

<!-- Related commit: a066a9e7 feat(domain): UnionValueObject 기본 타입 추가 -->
<!-- Related commit: ee88c6e6 feat(domain): DomainErrorType.InvalidTransition 추가 -->

---

#### 7. CQRS Query Patterns and Pagination

Structures read model queries through the `IQueryPort<TEntity, TDto>` interface along with `PagedResult<T>`, `CursorPagedResult<T>`, `SortExpression`, and more.

```csharp
// Query Port definition
public interface IProductQueryPort : IQueryPort<Product, ProductDto> { }

// Pagination query
var page = new PageRequest(page: 1, pageSize: 20);
var sort = SortExpression.By("Name").ThenBy("CreatedAt", SortDirection.Descending);
var result = await queryPort.Search(spec, page, sort).Run(EnvIO.New());

// Cursor-based pagination
var cursor = new CursorPageRequest(after: lastCursor, pageSize: 20);
var cursorResult = await queryPort.SearchByCursor(spec, cursor, sort).Run(EnvIO.New());
```

**Why this matters:**
- Separating Command (write) and Query (read) into distinct interfaces allows each to be optimized independently
- `PagedResult<T>` automatically computes pagination metadata such as `HasNextPage` and `TotalPages`
- `SortExpression` only allows permitted sort fields, structurally preventing SQL injection
- `IAsyncEnumerable<TDto>` Stream support enables memory-efficient processing of large datasets

<!-- Related commit: 8259af48 feat(Functorium): CQRS Query Adapter 패턴 구현 및 DapperQueryAdapterBase 추출 -->
<!-- Related commit: ebc203ec feat: SortExpression.By 빈 필드명 입력 시 Empty 반환 -->

---

#### 8. IRepository Common Interface (Including Bulk Operations)

The `IRepository<TAggregate, TId>` interface supports both single and bulk CRUD operations.

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

**Why this matters:**
- The `FinT<IO, T>` return type ensures all Repository operations explicitly return errors instead of throwing exceptions
- Bulk methods (`CreateRange`, `GetByIds`, etc.) structurally prevent N+1 problems
- Implementing `IObservablePort` means all Repository calls are automatically integrated into the observability pipeline
- The AggregateRoot constraint guarantees at compile time that Repositories always operate at the Aggregate level

<!-- Related commit: 7555a7ff feat(domains): IRepository<TAggregate, TId> 공통 인터페이스 구현 -->
<!-- Related commit: a1dc7ce7 feat: IRepository 벌크 메서드 및 DomainEventCollector O(n) 최적화 추가 -->

---

#### 9. FinT LINQ Extensions and Unwrap

Provides LINQ extension methods for the `FinT<M, A>` monad. Supports natural composition between `Fin`, `IO`, and `Validation`.

```csharp
// FinT composition via from ... select syntax
var result =
    from product in repository.GetById(productId)
    from validated in Fin.Succ(product).SelectMany(p => validateUpdate(p))
    select updated;

// Fin<T>.Unwrap() - alternative to ThrowIfFail
var value = fin.Unwrap(); // Throws exception on Fail

// Validation → FinT conversion
var finT =
    from name in ProductName.Validate(input)  // Validation<Error, T>
    from result in repository.Create(product) // FinT<IO, T>
    select result;
```

**Why this matters:**
- Freely compose three monads--`Fin`, `IO`, and `Validation`--to express business logic as pipelines
- `SelectMany` extensions enable the use of `from ... select` LINQ syntax, greatly improving readability
- `Filter` handles conditional branching functionally, eliminating `if` statements
- `TraverseSerial` sequentially applies async operations to each element in a collection, halting at the first failure

<!-- Related commit: 7408a3df feat(linq): FinT SelectMany 역방향 체이닝 확장 추가 -->
<!-- Related commit: cc1bb647 feat(linq): Validation → FinT 변환 SelectMany 확장 메서드 추가 -->

---

#### 10. ctx.* Observability Context Propagation

Automatically propagates structured log context through the `CtxEnricher` interface and source generators. Fields are declaratively controlled with `[CtxRoot]`, `[CtxTarget]`, and `[CtxIgnore]` attributes.

```csharp
// Usecase CtxEnricher definition
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

**Why this matters:**
- `CtxPillar` flags selectively propagate the same context across Logging, Tracing, and Metrics pillars
- The source generator automatically produces CtxEnricher implementations from interfaces, reducing code by over 80% compared to manual authoring
- Serilog dependency is removed from the Application layer, preserving domain purity
- The `ObservableSignal` static API enables structured log creation from anywhere in the infrastructure layer

<!-- Related commit: 042d3173 feat(observability): LogEnricher 인터페이스 Application 레이어 이동 + Serilog 의존성 제거 -->
<!-- Related commit: 81233196 feat(source-generator): LogEnricher 소스 제너레이터 구현 -->

---

#### 11. FluentValidation Integration Extensions

Provides FluentValidation extension methods for validating CQRS Command/Query requests.

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

**Why this matters:**
- `MustSatisfyValidation` directly reuses a domain Value Object's `Validate` method as a FluentValidation rule
- `MustBeEntityId<TEntityId>` automatically validates string-format ID inputs with Ulid parsing
- `MustBeEnum<TSmartEnum>` handles SmartEnum value validation in a single line
- `MustBePairedRange` standardizes Min/Max pair validation (including Optional values)

<!-- Related commit: 190cf8db feat: FluentValidation MustBeEntityId, MustBeOneOf 확장 메서드 추가 -->
<!-- Related commit: 113e8afb feat(domains): FluentValidation ValueObject 확장 기능 추가 -->
<!-- Related commit: 1577abd4 feat(validation): Option<T> 선택적 필터 검증 확장 메서드 추가 -->

---

### Functorium.Adapters Library

#### 1. OpenTelemetryBuilder and PipelineConfigurator

Configures observability settings using the builder pattern. Logging, Metrics, Tracing, and Pipeline can be configured independently, with an opt-in model that activates only the pipelines you need.

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

**Why this matters:**
- The opt-in model activates only the pipelines you need, eliminating unnecessary overhead
- A single `UseObservability()` call enables Logging, Metrics, Tracing, and Exception pipelines all at once
- Custom pipeline extension points (`AddCustomPipeline<T>()`) allow adding business concerns to the pipeline
- `ConfigureStartupLogger()` logs configuration values at application startup, supporting operational debugging

<!-- Related commit: a9e3e96b feat(pipelines): PipelineConfigurator를 통한 파이프라인 캡슐화 -->
<!-- Related commit: 4a08b441 refactor(pipeline)!: 파이프라인 단계를 opt-in 모델로 전환 -->
<!-- Related commit: 391ca88b feat(observability): 커스텀 파이프라인 확장 포인트 추가 -->

---

#### 2. EfCoreRepositoryBase

A generic base class that eliminates boilerplate in EF Core-based Repository implementations. Structurally prevents N+1 problems and automatically converts Specifications to EF Core queries.

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

**Why this matters:**
- Automatically implements all 8 CRUD methods (Create/Read/Update/Delete + Range bulk), eliminating 100+ lines of repetitive code per Repository
- The `applyIncludes` constructor parameter structurally prevents N+1 problems (Includes are automatically applied to all queries)
- `PropertyMap` automatically converts Specification domain Expressions to EF Core model queries
- `IDomainEventCollector` is automatically injected, transparently collecting Aggregate domain events on Create/Update

<!-- Related commit: 6cd7ca21 feat(adapters): EfCoreRepositoryBase 추가로 N+1 문제 구조적 방지 -->
<!-- Related commit: 406fae14 perf(adapters): Repository/Query 베이스 클래스 전면 개선 -->

---

#### 3. DapperQueryBase and InMemoryQueryBase

Base classes for Dapper SQL queries and in-memory queries. Implement the `IQueryPort<TEntity, TDto>` interface with built-in pagination and cursor-based query support.

```csharp
// Dapper Query implementation
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

**Why this matters:**
- Separates SQL query optimization from pagination/sorting logic, letting developers focus on core SQL
- `DapperSpecTranslator` converts domain Specifications to SQL WHERE clauses, supporting type-safe dynamic queries
- `InMemoryQueryBase` enables query logic verification without a real database during integration testing
- `IAsyncEnumerable<TDto>` Stream support enables memory-efficient processing of large datasets

<!-- Related commit: f16e8aa8 feat(adapters): InMemoryQueryBase 베이스 클래스 추가 및 DapperQueryBase 이름 변경 -->
<!-- Related commit: 8259af48 feat(Functorium): CQRS Query Adapter 패턴 구현 및 DapperQueryAdapterBase 추출 -->

---

#### 4. UsecaseTransactionPipeline and UsecaseCachingPipeline

Provides UnitOfWork-based transactions and IMemoryCache-based caching as pipelines.

```csharp
// Transaction pipeline: activated with UseTransaction()
// Transactions are automatically managed inside the Usecase handler:
// BeginTransaction → Handle → SaveChanges → PublishTrackedEvents → Commit

// Caching pipeline: implement ICacheable on Query
public sealed record GetProductQuery(string ProductId)
    : IQueryRequest<ProductDto>, ICacheable
{
    public string CacheKey => $"product:{ProductId}";
    public TimeSpan? Duration => TimeSpan.FromMinutes(5);
}
```

**Why this matters:**
- `IUnitOfWork` and `IUnitOfWorkTransaction` provide explicit control over transaction scope
- The transaction pipeline atomically handles SaveChanges and DomainEvent publishing, preventing data inconsistency
- Simply implementing the `ICacheable` interface automatically applies caching without modifying handler code
- The pipeline-based approach completely separates transaction/caching logic from business logic

<!-- Related commit: 29ace14d feat: UsecaseTransactionPipeline 및 도메인 이벤트 수집 인프라 구현 -->
<!-- Related commit: dc718a00 feat(pipelines): IMemoryCache 기반 UsecaseCachingPipeline 구현 -->

---

#### 5. Observable Domain Event Publisher

Provides complete observability for domain event publishing and handling. Logging, metrics, and distributed tracing are automatically applied.

```csharp
// DI registration
services.RegisterDomainEventPublisher();
services.RegisterDomainEventHandlersFromAssembly(typeof(Program).Assembly);

// Automatic observability: log fields
// request.event.type: "ProductCreatedEvent"
// request.event.id: "01J..."
// response.status: "success"
// response.elapsed: 0.005
```

**Why this matters:**
- `ObservableDomainEventPublisher` transparently adds logging, metrics, and tracing to all event publications
- `request.event.type` and `request.event.id` fields enable event tracking across distributed systems
- Per-handler success/failure metrics and elapsed time are automatically collected, enabling SLO monitoring
- `PublishResult` explicitly handles partial failure scenarios (some handlers succeed, some fail)

<!-- Related commit: 64a6e77c feat(event): DomainEvent Publisher/Handler에 Metrics 기능 추가 -->
<!-- Related commit: 5ad79092 feat(observability): DomainEvent Handler 로깅에 request.event.type/request.event.id 필드 추가 -->

---

### Functorium.Testing Library

#### 1. Architecture Test Suite Classes

`DomainArchitectureTestSuite` and `ApplicationArchitectureTestSuite` automatically verify domain architecture rules. Over 20 standard rules are immediately applied through simple inheritance.

```csharp
// Domain architecture test - rules applied through inheritance alone
public class MyDomainArchTests : DomainArchitectureTestSuite
{
    protected override string DomainNamespace => "MyApp.Domain";
    protected override Architecture Architecture => /* ArchUnitNET Architecture */;
}
// Automatically verified rules (partial list):
// - AggregateRoot_ShouldBe_PublicSealedClass
// - ValueObject_ShouldBe_Immutable
// - ValueObject_ShouldHave_CreateFactoryMethod
// - Entity_ShouldHave_AllPrivateConstructors
// - Specification_ShouldInherit_SpecificationBase
// - DomainService_ShouldBe_Stateless
// - DomainEvent_ShouldBe_SealedRecord
```

**Why this matters:**
- A single line of inheritance automatically verifies 20+ DDD rules in CI, including Value Object immutability, Entity constructor access control, and Specification inheritance
- `ClassValidator`/`MethodValidator` fluent APIs allow adding project-specific rules
- The `IArchRule<T>` interface and `CompositeArchRule` enable composition of reusable rules
- `ImmutabilityRule` comprehensively detects immutability violations including setters, mutable fields, and exposed collections

<!-- Related commit: 5af2b12b refactor: 아키텍처 테스트 Suite 클래스 기반으로 재설계 -->
<!-- Related commit: cf751136 feat(testing): ClassValidator/MethodValidator 아키텍처 검증 메서드 추가 -->

---

#### 2. Layer-specific ErrorAssertions Test Helpers

`DomainErrorAssertions`, `ApplicationErrorAssertions`, and `AdapterErrorAssertions` provide concise error type verification.

```csharp
// Domain error assertion
var result = ProductName.Validate("");
result.ShouldHaveDomainError<ProductName, string>(
    new DomainErrorType.Empty());

// Application error assertion
error.ShouldBeApplicationError<CreateProductCommand>(
    new ApplicationErrorType.NotFound());

// Fin<T> direct assertion
fin.ShouldBeAdapterError<ProductRepository, Product>(
    new AdapterErrorType.NotFound());
```

**Why this matters:**
- `ShouldBeXxxError`/`ShouldHaveXxxError` extension methods reduce error verification code from 3-5 lines to 1 line
- Layer-specific Assertions verify error code, error type, and current value all at once
- Assertions are provided for all three types: `Fin<T>`, `Validation<Error, T>`, and `Error`
- Exceptional error assertions (`ShouldBeXxxExceptionalError`) verify exception-based errors using the same pattern

<!-- Related commit: ae291a4c feat(testing): 레이어별 ErrorAssertions 테스트 헬퍼 추가 -->
<!-- Related commit: c810709d feat(testing): DomainErrorAssertions 테스트 헬퍼 추가 -->

---

#### 3. LogTestContext Structured Log Testing

Captures and verifies Serilog-based structured logs in unit tests.

```csharp
using var logCtx = new LogTestContext(LogEventLevel.Debug, enrichFromLogContext: true);
var logger = logCtx.CreateLogger<MyHandler>();

// Verify logs after handler execution
logCtx.LogCount.Should().Be(2);
var requestLog = logCtx.GetFirstLog();
var data = logCtx.ExtractFirstLogData();
// Structured field assertions available
```

**Why this matters:**
- Verifies through unit tests that the observability pipeline produces the correct structured fields
- `ExtractLogData()` extracts structured data from log events, enabling field-by-field assertions
- `LogContext`-based Enrichment is also captured, allowing verification of ctx.* context propagation

<!-- Related commit: a5e85cd5 feat(test): Application Layer 로그 필드 검증 테스트 추가 -->

## Bug Fixes

- Defensive handling against `AccessViolationException` on `GetType()` calls (`97cffb08`)
- Fixed Source Generator `ErrorCode` type namespace and access modifier (`33160d52`)
- Source Generator now uses the actual method name for the `request.handler.method` tag (`4c0c738c`)
- Added `FileShare.ReadWrite` when reading build coverage TRX files (`11503c7c`)

## API Changes

### Functorium Namespace Structure

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

### Functorium.Adapters Namespace Structure

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
│   └── Naming/               ObservabilityNaming (Categories, Metrics, Spans, etc.)
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

### Functorium.Testing Namespace Structure

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

## Installation

### NuGet Package Installation

```bash
# Functorium core library
dotnet add package Functorium --version 1.0.0-alpha.2

# Functorium.Adapters (Pipeline, Observability, Repository implementations)
dotnet add package Functorium.Adapters --version 1.0.0-alpha.2

# Functorium.Testing test library (optional)
dotnet add package Functorium.Testing --version 1.0.0-alpha.2
```

### Required Dependencies

- .NET 10 or later
- LanguageExt.Core 5.0.0-beta-77
- Mediator.Abstractions 3.x
- FluentValidation 11.x
- Ardalis.SmartEnum 8.x
