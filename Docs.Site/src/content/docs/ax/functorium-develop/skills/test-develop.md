---
title: "Test Develop"
description: "Writing unit tests, integration tests, and architecture rule tests"
---

> project-spec -> architecture-design -> domain-develop -> application-develop -> adapter-develop -> observability-develop -> **test-develop**

## Prerequisites

- Reads implemented source code to identify test targets.
- If `03-implementation-results.md` from `domain/`, `application/`, `adapter/` is available, the implementation status can be quickly understood.
- Test targets are identified directly from existing code even without prerequisite documents.

## Background

Test code in Functorium projects follows consistent rules. T1_T2_T3 naming convention, AAA pattern, Shouldly verification, NSubstitute Mock, `FinTFactory` helpers, ArchUnitNET architecture rules -- manually writing these patterns each time is repetitive and prone to omissions.

The `/test-develop` skill automates this repetition. When you provide test targets and scenarios, it generates unit tests, integration tests, and architecture rule tests matching the project's test rules.

## Skill Overview

### Test Types

| Test Type | Target | Tool | Description |
|-----------|--------|------|-------------|
| Value Object unit test | `SimpleValueObject`, `ValueObject`, `UnionValueObject` | Shouldly | Create success/failure, Normalize, error code verification |
| AggregateRoot unit test | `AggregateRoot<TId>` | Shouldly | Command methods, event publishing, invariant verification |
| Usecase unit test | `ICommandUsecase`, `IQueryUsecase` | NSubstitute, `FinTFactory` | Mock-based success/failure scenarios |
| Integration test | FastEndpoints | `HostTestFixture<TProgram>` | HTTP request/response, StatusCode verification |
| Architecture rule test | Layer dependencies, naming | ArchUnitNET | sealed class, layer violations, naming rules |

### Core Rules

| Rule | Description |
|------|-------------|
| T1_T2_T3 naming | `Handle_ShouldReturnSuccess_WhenRequestIsValid` |
| AAA pattern | `sut` (test target), `actual` (execution result), `expected` (expected value) |
| Shouldly verification | `actual.IsSucc.ShouldBeTrue()`, `actual.ThrowIfFail().Name.ShouldBe("value")` |
| NSubstitute Mock | `Substitute.For<T>()`, `.Returns(FinTFactory.Succ(value))` |
| `FinTFactory` | `FinTFactory.Succ(value)` / `FinTFactory.Fail<T>(error)` |

## Usage

### Basic Invocation

```text
/test-develop Write unit tests for the ProductName Value Object.
```

### Interactive Mode

Invoking `/test-develop` without arguments starts the skill in interactive mode, collecting test targets and scenarios through conversation.

### Execution Flow

1. **Target analysis** -- Reads the target code and identifies test scenarios
2. **User confirmation** -- Proceed to test generation after confirming the scenario list
3. **Test generation** -- Generates tests with T1_T2_T3 naming convention and AAA pattern
4. **Test execution** -- Runs `dotnet test` to confirm passing

## Example 1: Beginner -- Value Object Unit Test

The most basic test. Writes Create success/failure, Validate verification, Normalize behavior, and error code verification for a `SimpleValueObject` using the AAA pattern.

### Prompt

```text
/test-develop Write unit tests for the ProductName Value Object.
```

### Expected Results

| Test | Method Name | Description |
|------|-------------|-------------|
| Success | `Create_ShouldReturnSuccess_WhenNameIsValid` | Successful creation with valid name |
| Failure (null) | `Create_ShouldReturnFail_WhenNameIsNull` | Failure on null input |
| Failure (empty) | `Create_ShouldReturnFail_WhenNameIsEmpty` | Failure on empty string |
| Failure (max) | `Create_ShouldReturnFail_WhenNameExceedsMaxLength` | Failure when exceeding max length |
| Normalize | `Create_ShouldTrimWhitespace_WhenNameHasLeadingTrailingSpaces` | Leading/trailing whitespace removal |
| Error code | `Validate_ShouldReturnExpectedErrorCode_WhenNameIsEmpty` | Error code verification |

### Key Snippets

**Value Object unit test** -- T1_T2_T3, AAA pattern, Shouldly verification:

```csharp
public class ProductNameTests
{
    [Fact]
    public void Create_ShouldReturnSuccess_WhenNameIsValid()
    {
        // Arrange
        var name = "Valid Product Name";

        // Act
        var actual = ProductName.Create(name);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        ((string)actual.ThrowIfFail()).ShouldBe(name);
    }

    [Fact]
    public void Create_ShouldReturnFail_WhenNameIsEmpty()
    {
        // Arrange
        var name = "";

        // Act
        var actual = ProductName.Create(name);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldTrimWhitespace_WhenNameHasLeadingTrailingSpaces()
    {
        // Arrange
        var name = "  Trimmed Name  ";

        // Act
        var actual = ProductName.Create(name);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        ((string)actual.ThrowIfFail()).ShouldBe("Trimmed Name");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldReturnFail_WhenNameIsNullOrWhitespace(string? name)
    {
        // Act
        var actual = ProductName.Create(name);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
```

## Example 2: Intermediate -- Usecase Unit Test (Mock)

Adds Usecase tests to Example 1. Mocks the Repository with NSubstitute and configures success/failure scenarios with `FinTFactory.Succ`/`FinTFactory.Fail`. Covers various scenarios including duplication checks and validation failures.

### Prompt

```text
/test-develop Write unit tests for the CreateProductCommand Usecase.
```

### Expected Results

| Test | Method Name | Description |
|------|-------------|-------------|
| Success | `Handle_ShouldReturnSuccess_WhenRequestIsValid` | Valid request -> success response |
| Failure (validation) | `Handle_ShouldReturnFailure_WhenNameIsEmpty` | VO validation failure |
| Failure (duplicate) | `Handle_ShouldReturnFailure_WhenDuplicateName` | Name duplicate -> AlreadyExists |

### Key Snippets

**Usecase unit test** -- NSubstitute Mock, `FinTFactory`, AAA pattern:

```csharp
public class CreateProductCommandTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IInventoryRepository _inventoryRepository = Substitute.For<IInventoryRepository>();
    private readonly CreateProductCommand.Usecase _sut;

    public CreateProductCommandTests()
    {
        _sut = new CreateProductCommand.Usecase(_productRepository, _inventoryRepository);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateProductCommand.Request("Test Product", "Description", 100m, 10);

        _productRepository.Exists(Arg.Any<Specification<Product>>())
            .Returns(FinTFactory.Succ(false));
        _productRepository.Create(Arg.Any<Product>())
            .Returns(call => FinTFactory.Succ(call.Arg<Product>()));
        _inventoryRepository.Create(Arg.Any<Inventory>())
            .Returns(call => FinTFactory.Succ(call.Arg<Inventory>()));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Name.ShouldBe("Test Product");
        actual.ThrowIfFail().Price.ShouldBe(100m);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenNameIsEmpty()
    {
        // Arrange
        var request = new CreateProductCommand.Request("", "Description", 100m, 10);

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenDuplicateName()
    {
        // Arrange
        var request = new CreateProductCommand.Request("Existing Product", "Description", 100m, 10);

        _productRepository.Exists(Arg.Any<Specification<Product>>())
            .Returns(FinTFactory.Succ(true));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }
}
```

## Example 3: Advanced -- Integration Test + Architecture Rules

Adds integration tests and architecture rule tests to Example 2. Tests endpoints by running an actual HTTP server with `HostTestFixture<Program>`, and verifies layer dependencies and sealed class rules with ArchUnitNET.

### Prompt

```text
/test-develop Write product API integration tests and domain architecture rule tests.
```

### Expected Results

| Test Type | Class | Description |
|-----------|-------|-------------|
| Integration Test | `CreateProductEndpointTests` | POST /api/products, 201/400 verification |
| Architecture Rule | `DomainArchitectureRuleTests` | sealed class, AggregateRoot inheritance |
| Architecture Rule | `LayerDependencyArchitectureRuleTests` | Layer dependency violation check |

### Key Snippets

**Integration test** -- `HostTestFixture<Program>`, HttpClient, StatusCode verification:

```csharp
public class LayeredArchFixture : HostTestFixture<Program> { }

public abstract class IntegrationTestBase : IClassFixture<LayeredArchFixture>
{
    protected HttpClient Client { get; }
    protected IntegrationTestBase(LayeredArchFixture fixture) => Client = fixture.Client;
}

public class CreateProductEndpointTests : IntegrationTestBase
{
    public CreateProductEndpointTests(LayeredArchFixture fixture) : base(fixture) { }

    [Fact]
    public async Task CreateProduct_ShouldReturn201Created_WhenRequestIsValid()
    {
        // Arrange
        var request = new
        {
            Name = $"Test Product {Guid.NewGuid()}",
            Description = "Test Description",
            Price = 100.00m,
            StockQuantity = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/products", request,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var result = await response.Content
            .ReadFromJsonAsync<CreateProductEndpoint.Response>(
                TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Name.ShouldBe(request.Name);
    }

    [Fact]
    public async Task CreateProduct_ShouldReturn400BadRequest_WhenNameIsEmpty()
    {
        // Arrange
        var request = new { Name = "", Description = "Desc", Price = 100.00m, StockQuantity = 10 };

        // Act
        var response = await Client.PostAsJsonAsync("/api/products", request,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
```

**Architecture rule test** -- ArchUnitNET, layer dependency verification:

```csharp
public sealed class LayerDependencyArchitectureRuleTests
{
    [Fact]
    public void DomainLayer_ShouldNotDependOn_ApplicationLayer()
    {
        Types()
            .That()
            .ResideInNamespace(ArchitectureTestBase.DomainNamespace)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.ApplicationNamespace)
            .Check(ArchitectureTestBase.Architecture);
    }

    [Fact]
    public void DomainLayer_ShouldNotDependOn_AdapterLayer()
    {
        Types()
            .That()
            .ResideInNamespace(ArchitectureTestBase.DomainNamespace)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.PersistenceNamespace)
            .OrShould().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.InfrastructureNamespace)
            .OrShould().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.PresentationNamespace)
            .Check(ArchitectureTestBase.Architecture);
    }

    [Fact]
    public void ApplicationLayer_ShouldNotDependOn_AdapterLayer()
    {
        Types()
            .That()
            .ResideInNamespace(ArchitectureTestBase.ApplicationNamespace)
            .Should().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.PersistenceNamespace)
            .OrShould().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.InfrastructureNamespace)
            .OrShould().NotDependOnAnyTypesThat()
            .ResideInNamespace(ArchitectureTestBase.PresentationNamespace)
            .Check(ArchitectureTestBase.Architecture);
    }
}
```

**Domain architecture rule** -- AggregateRoot inheritance verification:

```csharp
public sealed class DomainArchitectureRuleTests : DomainArchitectureTestSuite
{
    protected override ArchUnitNET.Domain.Architecture Architecture => ArchitectureTestBase.Architecture;
    protected override string DomainNamespace => ArchitectureTestBase.DomainNamespace;

    [Fact]
    public void AggregateRoot_ShouldInherit_AggregateRootBase()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().AreAssignableTo(typeof(AggregateRoot<>))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireInherits(typeof(AggregateRoot<>)),
                verbose: true)
            .ThrowIfAnyFailures("AggregateRoot Inheritance Rule");
    }
}
```

## Snapshot Tests (Verify.Xunit)

Snapshot tests using Verify.Xunit detect structural changes in complex objects. After test execution, they compare against `.verified.` files and fail if differences are found.

```csharp
[Fact]
public async Task CreateProduct_ShouldMatchSnapshot_WhenRequestIsValid()
{
    // Arrange
    var request = new CreateProductCommand.Request("Test Product", 100m);

    // Act
    var actual = await _sut.Handle(request, CancellationToken.None);

    // Assert
    await Verify(actual.ThrowIfFail());
}
```

Snapshot approval: `./Build-VerifyAccept.ps1` batch-approves pending snapshots.

## Observability Verification Tests

Verifies that the ctx.* propagation strategy designed in the `observability-develop` skill works correctly.

### ctx.* 3-Pillar Snapshot Test

Verifies that CtxEnricher propagates the correct fields to the Logging, Tracing, and MetricsTag 3-Pillar.

```csharp
[Fact]
public async Task Handle_ShouldPropagateCtxFields_WhenCommandSucceeds()
{
    // Arrange
    using var logContext = new LogTestContext();
    using var activity = new Activity("test").Start();
    var metricsContext = MetricsTagContext.Current;

    var request = new CreateProductCommand.Request("Test", 100m);

    // Act
    var actual = await _sut.Handle(request, CancellationToken.None);

    // Assert -- Verify all 3-Pillar
    logContext.Properties.ShouldContainKey("ctx.product_id");
    activity.Tags.ShouldContain(t => t.Key == "ctx.product_id");
    metricsContext.Tags.ShouldNotContainKey("ctx.product_id"); // High cardinality excluded from MetricsTag
}
```

### Observable Port Observability Verification

Verifies that the Observable wrapper generated by `[GenerateObservablePort]` collects the correct fields.

| Verification Item | Field | Expected Value |
|-------------------|-------|---------------|
| Layer | `request.layer` | `"adapter"` |
| Category | `request.category.name` | `"repository"` |
| Handler | `request.handler.name` | `"ProductRepository"` |
| Status | `response.status` | `"success"` or `"failure"` |
| Error classification | `error.type` | `"expected"`, `"exceptional"` |

## References

### Workflow

- [Workflow](../workflow/) -- 7-step overall flow
- [Adapter Develop Skill](./adapter-develop/) -- Previous step: Repository, Endpoint, DI implementation
- [Domain Review Skill](./domain-review/) -- Quality check through code review

### Framework Guides

- [Unit Testing Guide](../guides/testing/15a-unit-testing/)
- [Integration Testing Guide](../guides/testing/15b-integration-testing/)
- [Testing Library](../guides/testing/16-testing-library/)
- [Error System: Adapter & Testing](../guides/domain/08c-error-system-adapter-testing/)

### Related Skills

- [Domain Develop Skill](./domain-develop/) -- Generate domain building blocks: Aggregate, Value Object, Event, etc.
- [Application Layer Develop Skill](./application-develop/) -- Generate Command/Query/EventHandler use cases
- [Adapter Layer Develop Skill](./adapter-develop/) -- Generate Repository, Query Adapter, Endpoint, DI registration
