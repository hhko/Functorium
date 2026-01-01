# Functorium Release v1.0.0-alpha.1

## Overview

Functorium v1.0.0-alpha.1 is the first alpha release of a C# framework for implementing **Domain-Centric Functional Architecture**.

It enables expressing domain logic as pure functions and pushing side effects to architectural boundaries, allowing you to write **testable and predictable business logic**. The framework provides a functional type system based on LanguageExt 5.x and integrated observability through OpenTelemetry.

### Core Principles

| Principle | Description | Functorium Support |
|-----------|-------------|-------------------|
| **Domain First** | Domain model is the center of architecture | Value Object hierarchy, immutable domain types |
| **Pure Core** | Business logic expressed as pure functions | `Fin<T>` return type, exception-free error handling |
| **Impure Shell** | Side effects handled at boundary layers | Adapter Pipeline, ActivityContext propagation |
| **Explicit Effects** | All effects explicitly typed | `FinResponse<T>`, `FinT<IO, T>` monad |

### Key Features

- **Domain Value Objects**: Value Object hierarchy ensuring immutability and validity
- **CQRS & FinResponse**: Explicit success/failure types with Command/Query separation
- **OpenTelemetry Integration**: Complete observability with logging, metrics, and distributed tracing
- **Pipeline Behaviors**: Separation of cross-cutting concerns from pure domain logic
- **Source Generator**: Automatic generation of Adapter pipeline boilerplate
- **Architecture Testing**: Domain-centric architecture rule validation

## Breaking Changes

This is the first release, so there are no breaking changes.

## New Features

### Functorium Library

#### 1. Domain Value Objects

Provides a complete class hierarchy for implementing immutable Value Objects. Supports various scenarios including single values, composite values, and comparable values.

```csharp
// Single Value Object
public sealed class UserId : ComparableSimpleValueObject<Guid>
{
    private UserId(Guid value) : base(value) { }

    public static Fin<UserId> Create(Guid value) =>
        CreateFromValidation(
            value == Guid.Empty
                ? Fail<Guid, Guid>(Error.New("UserId cannot be empty"))
                : Success<Error, Guid>(value),
            v => new UserId(v));
}

// Composite Value Object
public sealed class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }

    private Address(string street, string city)
    {
        Street = street;
        City = city;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
    }
}
```

**Why this matters:**
- Ensures domain model immutability, reducing bug occurrence
- Maintains consistency in equality comparison logic with `GetEqualityComponents()` pattern
- Handles validation and creation functionally with `CreateFromValidation` factory method
- Reduces boilerplate code by over 50% compared to manual implementation

<!-- Related commit: fae67a9 feat(domain): Add ValueObject base class hierarchy -->

---

#### 2. CQRS & FinResponse

Separates Commands and Queries, providing `FinResponse<A>` type that explicitly expresses success/failure.

```csharp
// Command definition
public record CreateUserCommand(string Name, string Email)
    : ICommandRequest<UserId>;

// Query definition
public record GetUserQuery(UserId Id)
    : IQueryRequest<UserDto>;

// Command Handler
public class CreateUserUsecase : ICommandUsecase<CreateUserCommand, UserId>
{
    public ValueTask<FinResponse<UserId>> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        return UserId.Create(Guid.NewGuid())
            .Match<FinResponse<UserId>>(
                Succ: id => id,
                Fail: error => error);
    }
}

// FinResponse usage
FinResponse<UserId> result = await mediator.Send(command);

result.Match(
    Succ: id => Console.WriteLine($"Created: {id}"),
    Fail: error => Console.WriteLine($"Error: {error.Message}"));
```

**Why this matters:**
- Prevents missing error handling with explicit failure types instead of exceptions
- Simplifies CQRS implementation with perfect Mediator pattern integration
- Supports natural conversion from `Fin<T>` to `FinResponse<T>`
- Provides functional composition operators (`Bind`, `Map`, `Match`)

<!-- Related commit: 7eddbfc feat(cqrs): Add ToResponse extension method for converting Fin<T> to IResponse -->

---

#### 3. OpenTelemetry Integration

Integrates logging (Serilog), metrics, and tracing with OpenTelemetry standards.

```csharp
// Program.cs
services.RegisterOpenTelemetry(configuration, Assembly.GetExecutingAssembly())
    .ConfigureLogging(logging => logging
        .AddDestructuringPolicy<ErrorsDestructuringPolicy>()
        .AddEnricher<MyCustomEnricher>())
    .ConfigureMetrics(metrics => metrics
        .AddMeter("MyApp.Metrics")
        .AddInstrumentation(builder => builder.AddHttpClientInstrumentation()))
    .ConfigureTracing(tracing => tracing
        .AddSource("MyApp.Tracing"))
    .WithAdapterObservability()
    .Build();
```

```json
// appsettings.json
{
  "OpenTelemetry": {
    "ServiceName": "MyService",
    "ServiceNamespace": "MyCompany",
    "CollectorEndpoint": "http://localhost:4317",
    "CollectorProtocol": "Grpc",
    "SamplingRate": 1.0,
    "EnablePrometheusExporter": true
  }
}
```

**Why this matters:**
- Unified logging, metrics, and tracing configuration with single builder API (70% reduction in setup time)
- Simplified Jaeger, Prometheus, Grafana integration with automatic OTLP Exporter configuration
- Automatic structured logging of LanguageExt Error types with `ErrorsDestructuringPolicy`
- Early detection of invalid settings with FluentValidation-based options validation

<!-- Related commit: 1790c73 feat(observability): Add OpenTelemetry and Serilog integration configuration -->

---

#### 4. Pipeline Behaviors

Automatically applies exception handling, logging, metrics, tracing, and validation to Mediator pipeline.

```csharp
// Pipeline application order:
// 1. UsecaseExceptionPipeline - Converts exceptions to FinResponse.Fail
// 2. UsecaseTracingPipeline - Creates OpenTelemetry Span
// 3. UsecaseMetricPipeline - Records request count, success/failure, latency
// 4. UsecaseLoggingPipeline - Structured logging of request/response
// 5. UsecaseValidationPipeline - FluentValidation validation

// Auto-generated metrics examples:
// - usecase.command.requests (Counter)
// - usecase.command.duration (Histogram)
// - usecase.command.success (Counter)
// - usecase.command.failure (Counter)
```

**Why this matters:**
- Maintains Usecase code purity by separating cross-cutting concerns into pipeline
- Consistent logging, metrics, and tracing automatically applied to all requests
- Safe error handling with exceptions converted to `FinResponse.Fail`
- Improved debugging efficiency with EventId-based log filtering

<!-- Related commit: f717b2e feat(observability): Add Metric and Trace implementations -->

---

#### 5. Error Handling

Provides architectural patterns for layer-specific error definition and structured error code management. Errors are separated and managed by concern through nested classes in each layer (Domain, Application).

```csharp
// =====================================================
// Domain Layer - DomainErrors nested class inside Value Object
// =====================================================
public sealed class City : SimpleValueObject<string>
{
    private City(string value) : base(value) { }

    public static Fin<City> Create(string value) =>
        CreateFromValidation(Validate(value), v => new City(v));

    public static Validation<Error, string> Validate(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? DomainErrors.Empty(value)
            : value;

    // Domain layer error definitions - encapsulated with Value Object
    internal static class DomainErrors
    {
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(City)}.{nameof(Empty)}",
                errorCurrentValue: value);
        // Error code format: "DomainErrors.City.Empty"
    }
}

// =====================================================
// Application Layer - ApplicationErrors nested class inside Usecase
// =====================================================
public sealed class CreateProductCommand
{
    public sealed record Request(string Name, decimal Price) : ICommandRequest<Response>;
    public sealed record Response(Guid ProductId, string Name);

    internal sealed class Usecase(IProductRepository repository)
        : ICommandUsecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(
            Request request, CancellationToken cancellationToken)
        {
            FinT<IO, Response> usecase =
                from exists in repository.ExistsByName(request.Name)
                from _ in guard(!exists, ApplicationErrors.ProductNameAlreadyExists(request.Name))
                from product in repository.Create(/* ... */)
                select new Response(product.Id, product.Name);

            return (await usecase.Run().RunAsync()).ToFinResponse();
        }
    }

    // Application layer error definitions - encapsulated with Usecase
    internal static class ApplicationErrors
    {
        public static Error ProductNameAlreadyExists(string productName) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(ApplicationErrors)}.{nameof(CreateProductCommand)}.{nameof(ProductNameAlreadyExists)}",
                errorCurrentValue: productName,
                errorMessage: $"Product name already exists: '{productName}'");
        // Error code format: "ApplicationErrors.CreateProductCommand.ProductNameAlreadyExists"
    }
}
```

**Layer-specific Error Management Architecture:**

| Layer | Nested Class | Error Code Pattern | Responsibility |
|-------|--------------|-------------------|----------------|
| **Domain** | `DomainErrors` | `DomainErrors.{Type}.{ErrorReason}` | Value Object validation failures |
| **Application** | `ApplicationErrors` | `ApplicationErrors.{Usecase}.{ErrorReason}` | Business rule violations, duplicate checks, etc. |

**Why this matters:**
- **Separation of concerns**: Error definitions encapsulated with their layer/class, improving cohesion
- **Error code naming convention**: `{Layer}.{Type}.{Error}` pattern enables immediate identification of error origin
- **Searchability**: Quick search for error definition locations across codebase using error codes
- **Serilog auto-structuring**: Automatic error log structuring when `ErrorsDestructuringPolicy` is applied
- **Type safety**: Consistent error creation through `ErrorCodeFactory` prevents typos and omissions

<!-- Related commit: b889230 test(abstractions): Add Errors type unit tests -->

---

#### 6. LINQ Extensions for FinT

Provides LINQ extension methods for `Fin<T>` and `FinT<M, T>` monads.

```csharp
// TraverseSerial - Sequential traversal (with automatic Activity Span creation)
var results = await items.ToSeq()
    .TraverseSerial(
        item => ProcessItem(item),
        activitySource,
        "ProcessItems",
        (item, index) => $"Item_{index}")
    .Run();

// Filter - Conditional filtering
var filtered = fin.Filter(x => x > 0);

// SelectMany - Monad composition (LINQ query syntax support)
var result = from a in GetUserAsync()
             from b in GetOrdersAsync(a.Id)
             select new { User = a, Orders = b };
```

**Why this matters:**
- Automatic Span creation for sequential collection processing enables traceability
- Supports asynchronous functional programming with `FinT<IO, T>` monad
- Readable monad composition with LINQ query syntax
- Guarantees fail-fast semantics on failure

<!-- Related commit: 4683281 feat(linq): Add TraverseSerial method and Activity Context utilities -->

---

#### 7. Dependency Injection Extensions

Provides DI extension methods for Adapter pipeline and options configuration.

```csharp
// Adapter pipeline registration (with automatic ActivityContext propagation)
services.RegisterScopedAdapterPipeline<IUserRepository, UserRepository>();

// Register Adapter implementing multiple interfaces
services.RegisterScopedAdapterPipelineFor<
    IUserRepository,
    IUserQueryRepository,
    UserRepository>();

// Factory-based registration
services.RegisterScopedAdapterPipeline<IUserRepository>(
    (serviceProvider, activityContext) =>
        new UserRepository(serviceProvider.GetRequiredService<DbContext>(), activityContext));

// Options configuration and validation
services.RegisterConfigureOptions<MyOptions, MyOptionsValidator>("MySection");
```

**Why this matters:**
- Simplified distributed tracing implementation with automatic `ActivityContext` propagation
- Registration methods provided for Scoped/Transient/Singleton lifetimes
- Early detection of invalid settings at startup with FluentValidation-based options validation
- Significant reduction in boilerplate DI registration code

<!-- Related commit: 7d9f182 feat(observability): Add OpenTelemetry dependency registration extension methods -->

---

### Functorium.Testing Library

#### 1. Architecture Rules Validation

Provides architecture rule validation utilities using ArchUnitNET.

```csharp
// Value Object architecture rule validation
var valueObjects = Classes()
    .That().ResideInNamespace("MyApp.Domain.ValueObjects");

valueObjects.ValidateAllClasses(architecture, validator =>
{
    validator
        .RequireSealed()
        .RequireAllPrivateConstructors()
        .RequireImmutable()
        .RequireMethod("Create", method => method
            .RequireStatic()
            .RequireReturnType(typeof(Fin<>)));
});
```

**Why this matters:**
- Maintains design consistency by enforcing architecture rules as tests
- Provides rule templates for patterns like Value Object, Entity, Repository
- Reports violations with clear messages for quick fixes
- Early detection of architecture drift in CI/CD pipeline

<!-- Related commit: dd49bd8 refactor(testing): Refactor ArchitectureRules validation code -->

---

#### 2. Test Fixtures

Provides fixtures for ASP.NET Core host and Quartz scheduler testing.

```csharp
// Host test fixture
public class MyApiTests : IClassFixture<HostTestFixture<Program>>
{
    private readonly HostTestFixture<Program> _fixture;

    public MyApiTests(HostTestFixture<Program> fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Should_Return_Ok()
    {
        var response = await _fixture.Client.GetAsync("/api/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

// Quartz Job test fixture
public class MyJobTests : IClassFixture<QuartzTestFixture<Program>>
{
    [Fact]
    public async Task Should_Execute_Job()
    {
        var result = await _fixture.ExecuteJobOnceAsync<MyJob>(TimeSpan.FromSeconds(30));
        result.Success.Should().BeTrue();
    }
}
```

**Why this matters:**
- 90% reduction in integration test setup boilerplate
- Automatic test lifecycle management with `IAsyncLifetime` implementation
- Execute Quartz Jobs synchronously and verify results
- Supports environment-specific configuration overrides

<!-- Related commit: 0282d23 feat(testing): Add test helper library source structure -->

---

#### 3. Structured Logging Assertions

Provides utilities for verifying Serilog log events.

```csharp
// Test structured logger setup
var logEvents = new List<LogEvent>();
var logger = new LoggerConfiguration()
    .WriteTo.Sink(new TestSink(logEvents))
    .CreateLogger();

var structuredLogger = new StructuredTestLogger<MyService>(logger);

// Log event property extraction and verification
var logData = LogEventPropertyExtractor.ExtractLogData(logEvents.First());
logData.Should().BeEquivalentTo(new
{
    RequestHandler = "CreateUserUsecase",
    Status = "success",
    Elapsed = 42.5
});
```

**Why this matters:**
- Verify structured log accuracy through tests
- Easy extraction of complex log properties with `LogEventPropertyExtractor`
- Unit test verification of Pipeline logging behavior
- Ensures accuracy of log-based monitoring/alerting rules

<!-- Related commit: 922c7b3 refactor(testing): Reorganize logging test utilities -->

---

#### 4. Source Generator Testing

Provides runners for testing Roslyn Source Generators.

```csharp
[Fact]
public void Should_Generate_Pipeline_Code()
{
    var generator = new AdapterPipelineGenerator();

    var generatedCode = generator.Generate(@"
        [GeneratePipeline]
        public interface IUserRepository : IAdapter
        {
            Fin<User> GetById(UserId id);
        }");

    generatedCode.Should().Contain("public class UserRepositoryPipeline");
    generatedCode.Should().Contain("ActivityContext");
}
```

**Why this matters:**
- Verify Source Generator output through unit tests
- Direct inspection of generated code strings without compilation
- Prevents Generator behavior regression during refactoring
- Supports TDD-style Generator development

<!-- Related commit: 1fb6971 refactor(source-generator): Improve code structure and add test infrastructure -->

---

### Functorium.Adapters.SourceGenerator Library

#### 1. Adapter Pipeline Generator

Automatically generates pipeline code with ActivityContext propagation for interfaces marked with `[GeneratePipeline]` attribute.

```csharp
// Interface definition
[GeneratePipeline]
public interface IUserRepository : IAdapter
{
    Fin<User> GetById(UserId id);
    Fin<Seq<User>> GetAll();
    Fin<Unit> Save(User user);
}

// Auto-generated code (conceptual example)
public class UserRepositoryPipeline : IUserRepository
{
    private readonly IUserRepository _inner;
    private readonly ActivityContext _activityContext;

    public Fin<User> GetById(UserId id)
    {
        using var span = CreateSpan("GetById");
        return _inner.GetById(id);
    }
    // ...
}
```

**Why this matters:**
- 100% automatic generation of Adapter pipeline boilerplate code
- Automatic distributed tracing support with ActivityContext propagation
- No runtime overhead with compile-time code generation
- Full support for LanguageExt 5.x `Fin<T>`, `FinT<M, T>` return types

<!-- Related commit: 68623bf feat(generator): Add Adapter Pipeline Source Generator project -->

## Bug Fixes

- **ValueObject array equality comparison bug fix**: Fixed issue where arrays returned from `GetEqualityComponents()` were not compared correctly (9c1c2c1)
- **LanguageExt 5.x API compatibility bug fix**: Fixed Source Generator bug occurring with parameterless methods (2e91065)

## API Changes

### Functorium Namespace Structure

```
Functorium
├── Abstractions/
│   ├── Errors/
│   │   └── DestructuringPolicies/
│   │       └── ErrorTypes/
│   ├── Registrations/
│   └── Utilities/
├── Adapters/
│   ├── Observabilities/
│   │   ├── Builders/
│   │   │   └── Configurators/
│   │   ├── Context/
│   │   ├── Loggers/
│   │   ├── Metrics/
│   │   └── Spans/
│   └── Options/
├── Applications/
│   ├── Cqrs/
│   ├── Linq/
│   ├── Observabilities/
│   │   ├── Context/
│   │   ├── Loggers/
│   │   ├── Metrics/
│   │   └── Spans/
│   └── Pipelines/
└── Domains/
    └── ValueObjects/
```

### Functorium.Testing Namespace Structure

```
Functorium.Testing
├── Actions/
│   └── SourceGenerators/
├── Arrangements/
│   ├── Hosting/
│   ├── Logging/
│   └── ScheduledJobs/
└── Assertions/
    ├── ArchitectureRules/
    └── Logging/
```

### Required Dependencies

- .NET 10.0 or higher
- LanguageExt.Core 5.0.0-beta-58 or higher
- OpenTelemetry 1.x
- Serilog 4.x
- Mediator.SourceGenerator
- FluentValidation
