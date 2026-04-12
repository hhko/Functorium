---
title: "Adapter Integration -- Unit Testing"
---

This document is a guide covering unit test authoring for Adapters, End-to-End Walkthroughs, and an architecture appendix. For Pipeline creation and DI registration, see [14a-adapter-pipeline-di.md](./14a-adapter-pipeline-di); for Port definitions, see [12-ports.md](./12-ports); for Adapter implementation, see [13-adapters.md](./13-adapters).

## Introduction

"How should unit tests for the Adapter layer be organized, and how should Port dependencies be isolated?"
"Why should we directly test the original Adapter rather than the Pipeline?"
"How do we execute and verify `FinT<IO, T>` return values in tests?"

Adapter unit testing is the final gate for verifying the correctness of business logic. By directly testing the original Adapter class rather than the Pipeline (Observable), we verify pure logic without observability wrapping. This document covers type-specific test strategies and End-to-End Walkthroughs.

### What You Will Learn

This document covers the following topics:

1. **Adapter unit test structure** -- Direct testing of the original Adapter, AAA pattern, IO execution pattern
2. **Mock/Stub strategy** -- Repository (direct instance), External API (MockHttpMessageHandler), Messaging (NSubstitute)
3. **E2E walkthrough** -- Summary of the full implementation process for Repository, External API, Messaging, and Query Adapter

### Prerequisites

A basic understanding of the following concepts is needed to understand this document:

- [Adapter implementation](./13-adapters) -- Type-specific Adapter implementation patterns
- [Pipeline and DI](./14a-adapter-pipeline-di) -- Pipeline creation and DI registration
- [Unit testing guide](../testing/15a-unit-testing) -- Test naming, AAA pattern, Shouldly

> **The test target is the original Adapter, not the Pipeline.** Since Pipeline only adds observability, business logic verification is performed on the original class.

## Summary

### Key Commands

```csharp
// Execute IO in tests
var result = await Task.Run(() => adapter.GetById(id).Run().RunAsync());
```

### Key Procedures

1. Directly instantiate the original Adapter class for testing (not the Pipeline)
2. Write tests using the AAA (Arrange-Act-Assert) pattern
3. Execute IO with `.Run().RunAsync()` or `Task.Run(() => ioResult.Run())`
4. Test both success and failure cases

### Key Concepts

| Concept | Description |
|------|------|
| IO execution pattern | Execute IO in tests with `adapter.Method().Run().RunAsync()` |
| Test target | Original Adapter class (not Pipeline) |
| Mock strategy | Repository: direct instance, External API: MockHttpMessageHandler, Messaging: NSubstitute |

First we examine the principles of Adapter unit testing and the IO execution pattern, then review type-specific (Repository, External API, Messaging, Query) test examples.

---

## Activity 5: Unit Testing

Adapter unit tests **directly test the original class** (not the Pipeline).

### Test Principles / IO Execution Pattern

| Principle | Description |
|------|------|
| Test target | Original Adapter class (not Pipeline) |
| Pattern | AAA (Arrange-Act-Assert) |
| Naming | `T1_T2_T3` (MethodName_Scenario_ExpectedResult) |
| Execution | `.Run().RunAsync()` or `Task.Run(() => ioResult.Run())` |
| Assertion library | Shouldly |
| Mock library | NSubstitute |

> **Note**: For detailed testing rules, see [15a-unit-testing.md](../testing/15a-unit-testing).

**IO execution pattern** - Pattern for executing `FinT<IO, T>` return values in tests:

```csharp
// Act
var ioFin = adapter.MethodUnderTest(args);   // Returns FinT<IO, T>
var ioResult = ioFin.Run();                  // Converts to IO<Fin<T>>
var result = await Task.Run(() => ioResult.Run());  // Executes Fin<T>

// Assert
result.IsSucc.ShouldBeTrue();
```

### Repository Testing

Repository Adapters have no external dependencies (in the case of In-Memory implementation), so they are tested by creating direct instances.

```csharp
// File: Tests/{Project}.Tests.Unit/LayerTests/Adapters/ProductRepositoryInMemoryTests.cs

public sealed class ProductRepositoryInMemoryTests
{
    [Fact]
    public async Task Create_ReturnsProduct_WhenProductIsValid()
    {
        // Arrange
        var repository = new ProductRepositoryInMemory();
        var product = Product.Create(
            ProductName.Create("Test Product").ThrowIfFail(),
            ProductDescription.Create("Description").ThrowIfFail(),
            Money.Create(10000m).ThrowIfFail());

        // Act
        var ioFin = repository.Create(product);
        var ioResult = ioFin.Run();
        var result = await Task.Run(() => ioResult.Run());

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Match(
            Succ: created =>
            {
                created.Id.ShouldBe(product.Id);
                created.Name.ShouldBe(product.Name);
            },
            Fail: _ => throw new Exception("Should be success"));
    }

    [Fact]
    public async Task GetById_ReturnsFail_WhenProductNotFound()
    {
        // Arrange
        var repository = new ProductRepositoryInMemory();
        var nonExistentId = ProductId.New();

        // Act
        var ioFin = repository.GetById(nonExistentId);
        var ioResult = ioFin.Run();
        var result = await Task.Run(() => ioResult.Run());

        // Assert
        result.IsFail.ShouldBeTrue();
    }
}
```

### External API Testing

External API Adapters are tested by mocking `HttpClient`.

A pattern that controls HTTP responses with `MockHttpMessageHandler` and separates success/failure scenarios for testing.

```csharp
// File: Tests/{Project}.Tests.Unit/LayerTests/Adapters/ExternalPricingApiServiceTests.cs

public sealed class ExternalPricingApiServiceTests
{
    [Fact]
    public async Task GetPriceAsync_ReturnsMoney_WhenApiReturnsSuccess()
    {
        // Arrange
        var priceResponse = new ExternalPriceResponse(
            "PROD-001", 29900m, "KRW", DateTime.UtcNow.AddHours(1));
        var handler = new MockHttpMessageHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(priceResponse));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.com")
        };

        var service = new ExternalPricingApiService(httpClient);

        // Act
        var ioFin = service.GetPriceAsync("PROD-001", CancellationToken.None);
        var ioResult = ioFin.Run();
        var result = await Task.Run(() => ioResult.Run());

        // Assert
        result.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public async Task GetPriceAsync_ReturnsFail_WhenApiReturns404()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(HttpStatusCode.NotFound);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.com")
        };

        var service = new ExternalPricingApiService(httpClient);

        // Act
        var ioFin = service.GetPriceAsync("INVALID", CancellationToken.None);
        var ioResult = ioFin.Run();
        var result = await Task.Run(() => ioResult.Run());

        // Assert
        result.IsFail.ShouldBeTrue();
    }

    // Helper class for HttpClient mock
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string? _content;

        public MockHttpMessageHandler(
            HttpStatusCode statusCode, string? content = null)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode);
            if (_content is not null)
            {
                response.Content = new StringContent(
                    _content, System.Text.Encoding.UTF8, "application/json");
            }
            return Task.FromResult(response);
        }
    }
}
```

### Messaging Testing

Messaging Adapters are tested by mocking `IMessageBus` with NSubstitute.

```csharp
// File: Tests/{Project}.Tests.Unit/LayerTests/Adapters/RabbitMqInventoryMessagingTests.cs

public sealed class RabbitMqInventoryMessagingTests
{
    [Fact]
    public async Task CheckInventory_SendsRequest_WhenRequestIsValid()
    {
        // Arrange
        var request = new CheckInventoryRequest(Guid.NewGuid(), Quantity: 5);
        var expectedResponse = new CheckInventoryResponse(
            ProductId: request.ProductId,
            IsAvailable: true,
            AvailableQuantity: 10);

        var messageBus = Substitute.For<IMessageBus>();
        messageBus.InvokeAsync<CheckInventoryResponse>(
                request, Arg.Any<CancellationToken>(), Arg.Any<TimeSpan?>())
            .Returns(expectedResponse);

        var messaging = new RabbitMqInventoryMessaging(messageBus);

        // Act
        var ioFin = messaging.CheckInventory(request);
        var ioResult = ioFin.Run();
        var result = await Task.Run(() => ioResult.Run());

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Match(
            Succ: response =>
            {
                response.ProductId.ShouldBe(request.ProductId);
                response.IsAvailable.ShouldBeTrue();
                response.AvailableQuantity.ShouldBe(10);
            },
            Fail: _ => throw new Exception("Should be success"));
    }

    [Fact]
    public async Task ReserveInventory_SendsCommand_WhenCommandIsValid()
    {
        // Arrange
        var command = new ReserveInventoryCommand(
            OrderId: Guid.NewGuid(),
            ProductId: Guid.NewGuid(),
            Quantity: 5);

        var messageBus = Substitute.For<IMessageBus>();
        messageBus.SendAsync(command)
            .Returns(ValueTask.CompletedTask);

        var messaging = new RabbitMqInventoryMessaging(messageBus);

        // Act
        var ioFin = messaging.ReserveInventory(command);
        var ioResult = ioFin.Run();
        var result = await Task.Run(() => ioResult.Run());

        // Assert
        result.IsSucc.ShouldBeTrue();
        await messageBus.Received(1).SendAsync(command);
    }
}
```

> **Reference**: `Tutorials/Cqrs06Services/Tests/OrderService.Tests.Unit/LayerTests/Adapters/RabbitMqInventoryMessagingTests.cs`

### Query Adapter Testing

Query Adapters are tested by directly instantiating the InMemory implementation. They use the same IO execution pattern as Repository tests.

```csharp
// File: Tests/{Project}.Tests.Unit/Application/Products/SearchProductsQueryTests.cs

[Fact]
public async Task Search_ReturnsPagedResult_WhenProductsExist()
{
    // Arrange
    var queryAdapter = new ProductQueryInMemory(repository);

    // Act
    var ioFin = queryAdapter.Search(Specification<Product>.All, new PageRequest(), SortExpression.Empty);
    var ioResult = ioFin.Run();
    var result = await Task.Run(() => ioResult.Run());

    // Assert
    result.IsSucc.ShouldBeTrue();
}
```

> **Reference**: `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Application/Products/SearchProductsQueryTests.cs`

> **Note**: SQL execution tests for Dapper Query Adapters are performed in integration tests. In unit tests, the InMemory implementation is used to verify Query logic.

Now that you have learned the type-specific test patterns, let us review the full implementation process for each Adapter end-to-end.

---

## End-to-End Walkthroughs

This section summarizes the full implementation process for each Adapter type. Refer to the corresponding Activity section for detailed code at each step.

### Repository (01-SingleHost IProductRepository)

| Step | Activity | File | Key Task |
|------|----------|------|----------|
| 1 | Port definition | `LayeredArch.Domain/Repositories/IProductRepository.cs` | `: IObservablePort`, `FinT<IO, T>` return, domain VO parameters |
| 2 | Adapter implementation | `LayeredArch.Adapters.Persistence/Repositories/Products/ProductRepositoryInMemory.cs` | `[GenerateObservablePort]`, `virtual`, `IO.lift`, `AdapterError.For<T>` |
| 3 | Pipeline verification | `obj/GeneratedFiles/.../Repositories.ProductRepositoryInMemoryObservable.g.cs` | Auto-generated after build |
| 4 | DI registration | `AdapterPersistenceRegistration.cs` -> `Program.cs` | `RegisterScopedObservablePort<IProductRepository, ...Observable>()` |
| 5 | Testing | `ProductRepositoryInMemoryTests.cs` | Direct testing of original class, see [Repository Testing](#repository-testing) |

### External API (01-SingleHost IExternalPricingService)

| Step | Activity | File | Key Task |
|------|----------|------|----------|
| 1 | Port definition | `LayeredArch.Application/Ports/IExternalPricingService.cs` | Includes `CancellationToken`, `Async` suffix |
| 2 | Adapter implementation | `LayeredArch.Adapters.Infrastructure/ExternalApis/ExternalPricingApiService.cs` | `IO.liftAsync`, `HandleHttpError<T>`, try/catch pattern |
| 3 | Pipeline verification | `obj/GeneratedFiles/.../ExternalApis.ExternalPricingApiServiceObservable.g.cs` | Auto-generated after build |
| 4 | DI registration | `AdapterInfrastructureRegistration.cs` -> `Program.cs` | `AddHttpClient<...Observable>()` + `RegisterScopedObservablePort` |
| 5 | Testing | `ExternalPricingApiServiceTests.cs` | Uses `MockHttpMessageHandler`, see [External API Testing](#external-api-testing) |

### Messaging (Cqrs06Services IInventoryMessaging)

| Step | Activity | File | Key Task |
|------|----------|------|----------|
| 1 | Port definition | `OrderService/Adapters/Messaging/IInventoryMessaging.cs` | Request/Reply + Fire-and-Forget |
| 2 | Adapter implementation | `OrderService/Adapters/Messaging/RabbitMqInventoryMessaging.cs` | `IMessageBus` injection, `InvokeAsync` / `SendAsync` |
| 3 | Pipeline verification | `obj/GeneratedFiles/.../Messaging.RabbitMqInventoryMessagingObservable.g.cs` | Auto-generated after build |
| 4 | DI registration | `OrderService/Program.cs` (line 57) | `RegisterScopedObservablePort`, MessageBus registered separately via Wolverine |
| 5 | Testing | `RabbitMqInventoryMessagingTests.cs` | Mock `IMessageBus` with NSubstitute, see [Messaging Testing](#messaging-testing) |

### Query Adapter (01-SingleHost IProductQuery)

| Step | Activity | File | Key Task |
|------|----------|------|----------|
| 1 | Port definition | `LayeredArch.Application/Usecases/Products/Ports/IProductQuery.cs` | `: IQueryPort<Product, ProductSummaryDto>` |
| 2a | Dapper implementation | `LayeredArch.Adapters.Persistence/Repositories/Products/Queries/ProductQueryDapper.cs` | Inherits `DapperQueryBase`, `[GenerateObservablePort]`, handles only SQL declarations |
| 2b | InMemory implementation | `LayeredArch.Adapters.Persistence/Repositories/Products/Queries/ProductQueryInMemory.cs` | `[GenerateObservablePort]`, delegates to Repository |
| 3 | Pipeline verification | `obj/GeneratedFiles/.../Repositories.ProductQueryDapperObservable.g.cs` | Auto-generated after build |
| 4 | DI registration | `AdapterPersistenceRegistration.cs` -> `Program.cs` | Sqlite: Dapper Observable, InMemory: InMemory Observable |
| 5 | Testing | `SearchProductsQueryTests.cs` | Direct testing of InMemory Query Adapter, see [Query Adapter Testing](#query-adapter-testing) |

---

## Appendix

### A. Clean Architecture Full Flow

```
+-------------------------------------------------------------------+
|                       Presentation Layer                          |
|  FastEndpoints / Controllers                                      |
+-------------------------------------------------------------------+
                              |
                              v
+-------------------------------------------------------------------+
|                       Application Layer                           |
|  ┌─────────────────────────────────────────────────────────────┐  |
|  │ CreateProductCommand.Usecase                                │  |
|  │   - IProductRepository (depends on Port Interface)           │  |
|  │   - Business logic implementation                            │  |
|  └─────────────────────────────────────────────────────────────┘  |
|  ┌─────────────────────────────────────────────────────────────┐  |
|  │ IProductRepository : IRepository<Product, ProductId> (Port Interface) │  |
|  │   - FinT<IO, Product> GetById(ProductId id)                  │  |
|  │   - FinT<IO, Product> Create(Product product)               │  |
|  └─────────────────────────────────────────────────────────────┘  |
+-------------------------------------------------------------------+
                              |
                              v
+-------------------------------------------------------------------+
|                      Infrastructure Layer                         |
|  ┌─────────────────────────────────────────────────────────────┐  |
|  │ [GenerateObservablePort]                                          │  |
|  │ ProductRepositoryInMemory : IProductRepository              │  |
|  │   - RequestCategory => "Repository"                         │  |
|  │   - Actual data access implementation                        │  |
|  └─────────────────────────────────────────────────────────────┘  |
|                              |                                    |
|                              v (Source Generator)                 |
|  ┌─────────────────────────────────────────────────────────────┐  |
|  │ ProductRepositoryInMemoryObservable (auto-generated)          │  |
|  │   - Tracing, logging, metrics automatically added             │  |
|  │   - Registered as IProductRepository in DI                   │  |
|  └─────────────────────────────────────────────────────────────┘  |
+-------------------------------------------------------------------+
```

**Example of using an Adapter in a Usecase:**

```csharp
// Application/Usecases/GetProductByIdQuery.cs
public sealed class GetProductByIdQuery
{
    public sealed record Request(string ProductId) : IQueryRequest<Response>;
    public sealed record Response(string ProductId, string Name, decimal Price);

    internal sealed class Usecase(IProductDetailQuery productDetailQuery)
        : IQueryUsecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(
            Request request, CancellationToken cancellationToken)
        {
            var productId = ProductId.Create(request.ProductId);

            FinT<IO, Response> usecase =
                from dto in productDetailQuery.GetById(productId)
                select new Response(
                    dto.ProductId,
                    dto.Name,
                    dto.Price);

            Fin<Response> result = await usecase.Run().RunAsync();
            return result.ToFinResponse();
        }
    }
}
```

### D. Quick Reference Checklist

#### Port Interface

- [ ] Inherits `IObservablePort`
- [ ] Return type: `FinT<IO, T>`
- [ ] Uses domain VOs (Repository)
- [ ] `CancellationToken` (External API)
- [ ] Location: Repository -> Domain, External API/Query Adapter -> Application

#### Adapter Implementation

- [ ] `[GenerateObservablePort]` attribute
- [ ] Implements Port interface
- [ ] `RequestCategory` property
- [ ] `virtual` on all methods
- [ ] `IO.lift` (synchronous) or `IO.liftAsync` (asynchronous)
- [ ] Success: `Fin.Succ(value)`
- [ ] Failure: `AdapterError.For<T>(errorType, context, message)`
- [ ] Exception: `AdapterError.FromException<T>(errorType, ex)`

#### DI Registration

- [ ] Create Registration class (`Adapter{Layer}Registration`)
- [ ] `RegisterScopedObservablePort<IObservablePort, ObservablePort>()`
- [ ] HttpClient registration (External API)
- [ ] Query Adapter Observable registration (Dapper or InMemory)
- [ ] Call Registration from `Program.cs`

#### Unit Testing

- [ ] Test original Adapter class (not Pipeline)
- [ ] AAA pattern
- [ ] `T1_T2_T3` naming
- [ ] Execution: `.Run()` -> `Task.Run(() => ioResult.Run())`
- [ ] Test both success and failure cases

### E. Observability Detailed Specification Summary

This is a summary of the Observability features automatically provided by the Pipeline. For detailed specifications, see [08-observability.md](../../spec/08-observability).

**Span name pattern**: `{layer} {category} {handler}.{method}`

**Tracing Tag structure:**

| Tag Key | Success | Failure |
|---------|---------|---------|
| `request.layer` | "adapter" | "adapter" |
| `request.category.name` | category name | category name |
| `request.handler.name` | handler name | handler name |
| `request.handler.method` | method name | method name |
| `response.status` | "success" | "failure" |
| `response.elapsed` | seconds (s) | seconds (s) |
| `error.type` | - | "expected" / "exceptional" / "aggregate" |
| `error.code` | - | error code |

**Metric Instruments:**

| Instrument | Name Pattern | Type | Unit |
|------------|----------|------|------|
| requests | `adapter.{category}.requests` | Counter | `{request}` |
| responses | `adapter.{category}.responses` | Counter | `{response}` |
| duration | `adapter.{category}.duration` | Histogram | `s` |

**Error object (`@error`) structure:**

```json
// Expected Error
{ "ErrorCode": "Product.NotFound", "Message": "Entity not found" }

// Exceptional Error
{ "Message": "Database connection failed" }

// Aggregate Error
{ "Errors": [{ "ErrorCode": "Validation.Required", "Message": "Name is required" }] }
```

---

## Troubleshooting

### Unable to determine how to execute `FinT<IO, T>` in tests

**Cause:** `FinT<IO, T>` return values wrap the IO monad and require explicit execution.

**Resolution:** Use the `.Run().RunAsync()` pattern.

```csharp
var ioFin = adapter.MethodUnderTest(args);   // Returns FinT<IO, T>
var ioResult = ioFin.Run();                  // Converts to IO<Fin<T>>
var result = await Task.Run(() => ioResult.Run());  // Executes Fin<T>
```

---

## FAQ

### Q3. Can I test the original class without Pipeline in tests?

Yes, you can directly instantiate the original class for testing. Refer to the test examples in the [Activity 5](#activity-5-unit-testing) section.

### Q6. When should I distinguish between Repository and Query Adapter?

**Decision Criteria**: Does the query result need to reconstruct an Aggregate?

- **Aggregate needed** (domain invariant validation, Create/Update/Delete) -> **Repository** (`IRepository<T, TId>`, Domain Layer, EF Core)
- **Direct DTO return** (read-only, pagination/sorting) -> **Query Adapter** (`IQueryPort<TEntity, TDto>`, Application Layer, Dapper)

> For detailed decision criteria, refer to the comparison table in [Query Adapter](./13-adapters#query-adapter-cqrs-read-side).

---

## References

| Document | Description |
|------|------|
| [04-ddd-tactical-overview.md](../domain/04-ddd-tactical-overview) | Domain modeling complete overview |
| [11-usecases-and-cqrs.md](../application/11-usecases-and-cqrs) | Usecase implementation (CQRS Command/Query) |
| [08a-error-system.md](../domain/08a-error-system) | Error system: Fundamentals and naming |
| [08b-error-system-domain-app.md](../domain/08b-error-system-domain-app) | Error system: Domain/Application errors |
| [08c-error-system-adapter-testing.md](../domain/08c-error-system-adapter-testing) | Error system: Adapter errors and testing |
| [12-ports.md](./12-ports) | Port definition guide |
| [13-adapters.md](./13-adapters) | Adapter implementation guide |
| [14a-adapter-pipeline-di.md](./14a-adapter-pipeline-di) | Pipeline creation, DI registration, Options pattern |
| [15a-unit-testing.md](../testing/15a-unit-testing) | Unit testing guide |
| [08-observability.md](../../spec/08-observability) | Observability specification (tracing, logging, metrics details) |
| [01-project-structure.md](../architecture/01-project-structure) | Service project structure guide |

**External references:**

- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/) - Distributed tracing
- [LanguageExt](https://github.com/louthy/language-ext) - Functional programming library
