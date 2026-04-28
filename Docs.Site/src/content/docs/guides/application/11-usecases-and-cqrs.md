---
title: "Use Cases and CQRS"
---

This document explains how to implement use cases with the CQRS pattern, which separates reads and writes for individual optimization.

## Introduction

"Should navigation properties be added to the domain model for query performance?"
"As business logic in the Application Service keeps growing, how can it be separated?"
"Where should SaveChanges be called, and who is responsible for publishing domain events?"

These are problems repeatedly encountered when designing the Application Layer. CQRS separates reads and writes so each can choose the optimal technology, and Functorium's pipeline system automatically handles transactions and event publishing so that Use Cases can focus solely on business logic.

### What You Will Learn

Through this document, you will learn:

1. **Benefits of the CQRS pattern and Command/Query separation criteria** - Practical effects of separating read/write paths
2. **Implementing use cases with the nested class pattern** - Cohesive grouping of Request, Response, Validator, and Usecase in a single file
3. **Apply merge pattern and LINQ-based functional implementation** - Value Object validation and functional chaining
4. **Automatic handling by UsecaseTransactionPipeline** - Automation of SaveChanges and domain event publishing
5. **Application errors and FluentValidation integration** - Dual validation strategy

### Prerequisites

A basic understanding of the following concepts is required to understand this document:

- Layer architecture from the [DDD Tactical Design Overview](../domain/04-ddd-tactical-overview)
- Create/Validate pattern of [Value Objects](../domain/05a-value-objects)
- Basic concepts of LanguageExt's `Fin<T>` and `FinT<IO, T>`

> **The core of CQRS is** separating reads and writes so each can choose the optimal technology, and Functorium's pipeline automatically handles transactions and event publishing so that Use Cases can focus solely on business logic.

## Why CQRS

### Role of Application Service in DDD

The Application Layer is the layer that orchestrates domain objects to perform use cases. It does not contain domain logic itself but delegates work to domain objects.

In a traditional Application Service, a single service class handles creation, retrieval, modification, and deletion. It looks concise at first, but problems emerge as the business grows.

Queries require DTOs that join multiple tables, while creation requires immutable validation through Aggregate Roots and transactions. If you try to satisfy both with a single model, you either add navigation properties to the domain model for query performance, or conversely make query code unnecessarily complex to preserve domain integrity.

CQRS solves this problem by separating the read path (Query) from the write path (Command). Commands persist Aggregates via EF Core, and Queries write SQL directly via Dapper, allowing each to choose the optimal technology.

### Benefits of Command/Query Separation

The following table compares a unified model with CQRS. The key benefit is that Command and Query can each choose the optimal technology stack.

| Aspect | Unified Model | CQRS |
|------|----------|------|
| Read/Write optimization | Compromise with a single model | Each can be optimized independently |
| Technology stack | Same ORM enforced | **Command: EF Core, Query: Dapper** independently chosen |
| Scalability | Scale together | Scale independently |
| Complexity management | Concentrated in one place | Separation of concerns |

### Technology Separation in the Adapter Layer

The benefits of CQRS are realized in the Adapter layer:

| Aspect | Command | Query |
|------|---------|-------|
| **Adapter type** | Repository (`IRepository<T, TId>`) | Query Adapter (`IQueryPort<TEntity, TDto>`) |
| **ORM** | EF Core | Dapper + explicit SQL |
| **Reason** | Change tracking, UnitOfWork, migrations | Maximum performance, easy SQL tuning |
| **Return type** | Domain Entity (`FinT<IO, T>`) | DTO (`FinT<IO, PagedResult<TDto>>`) |
| **Port location** | Domain Layer | Application Layer |

> For detailed implementation, see [13-adapters.md](../adapter/13-adapters) §2.6 Query Adapter

### Use Case = Explicit Expression of Business Intent

In Functorium, each use case is represented as a single class. The business intent is expressed in the class name, such as `CreateProductCommand` or `GetProductByIdQuery`.

## Summary

### Key Interfaces

| Purpose | Request Interface | Handler Interface |
|------|-------------------|-------------------|
| Command | `ICommandRequest<TSuccess>` | `ICommandUsecase<TCommand, TSuccess>` |
| Query | `IQueryRequest<TSuccess>` | `IQueryUsecase<TQuery, TSuccess>` |
| Event | `IDomainEvent` | `IDomainEventHandler<TEvent>` |

### Key Types

| Type | Purpose | Layer |
|------|------|------|
| `Fin<A>` | LanguageExt success/failure type | Domain or Adapter |
| `FinT<IO, A>` | Fin type with IO effect | Repository/Adapter |
| `FinResponse<A>` | Functorium Response success/failure type | Usecase |
| `Error` | Error information | Common |
| `ICacheable` | Query caching marker interface (`CacheKey`, `Duration`) | Usecase |

### Recommended Implementation Pattern

```csharp
using Functorium.Applications.Errors;
using static Functorium.Applications.Errors.ApplicationErrorKind;

public sealed class CreateProductCommand
{
    public sealed record Request(...) : ICommandRequest<Response>;
    public sealed record Response(...);
    public sealed class Validator : AbstractValidator<Request> { ... }

    internal sealed class Usecase(
        IProductRepository productRepository)
        : ICommandUsecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // 1. Value Object validation + Apply merge
            var productResult = CreateProduct(request);
            if (productResult.IsFail)
            {
                return productResult.Match(
                    Succ: _ => throw new InvalidOperationException(),
                    Fail: error => FinResponse.Fail<Response>(error));
            }

            // 2. Business logic processing via LINQ query
            var productName = ProductName.Create(request.Name).Unwrap();

            FinT<IO, Response> usecase =
                from exists in _productRepository.ExistsByName(productName)
                from _ in guard(!exists, ApplicationError.For<CreateProductCommand>(
                    new AlreadyExists(),
                    request.Name,
                    $"Product name already exists: '{request.Name}'"))
                from product in _productRepository.Create((Product)productResult)
                select new Response(...);
            // SaveChanges + domain event publishing is handled automatically by UsecaseTransactionPipeline

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }

        private static Fin<Product> CreateProduct(Request request)
        {
            var name = ProductName.Validate(request.Name);
            var description = ProductDescription.Validate(request.Description);
            var price = Money.Validate(request.Price);
            var stockQuantity = Quantity.Validate(request.StockQuantity);

            return (name, description, price, stockQuantity)
                .Apply((n, d, p, s) =>
                    Product.Create(
                        ProductName.Create(n).Unwrap(),
                        ProductDescription.Create(d).Unwrap(),
                        Money.Create(p).Unwrap(),
                        Quantity.Create(s).Unwrap()))
                .As()
                .ToFin();
        }
    }
}
```

### ApplyT vs Unwrap Selection Criteria

| Criteria | Unwrap | ApplyT |
|------|--------|--------|
| Number of VOs | 1-2 | 3 or more |
| Error handling | Returns immediately at the first error | Collects all errors in parallel |
| Code style | Imperative (`var x = ...`) | Declarative (LINQ `from`) |
| Learning curve | Low | High (monad transformers) |
| Suitable for | Simple Commands, internal services | User input forms, complex validation |

**Decision criteria:** If there are 1-2 VOs and no need to collect errors in parallel, Unwrap is more concise.
If there are 3 or more VOs, or you need to show all validation errors to the user at once, use ApplyT.

Now that we have grasped the overall structure from the summary, let us examine the specific structure of the CQRS pattern.

---

## CQRS Pattern Overview

### Command and Query Separation

| Category | Command | Query |
|------|---------|-------|
| Purpose | State change (write) | Data query (read) |
| Example | Create, Update, Delete | GetById, GetAll, Search |
| Return | Created/modified entity info | Retrieved data |

### Mediator Pattern Integration

Functorium CQRS is based on the [Mediator](https://github.com/martinothamar/Mediator) library:

```csharp
// Request inherits ICommand or IQuery
public interface ICommandRequest<TSuccess> : ICommand<FinResponse<TSuccess>> { }
public interface IQueryRequest<TSuccess> : IQuery<FinResponse<TSuccess>> { }

// Handler inherits ICommandHandler or IQueryHandler
public interface ICommandUsecase<in TCommand, TSuccess>
    : ICommandHandler<TCommand, FinResponse<TSuccess>>
    where TCommand : ICommandRequest<TSuccess> { }
```

---

## Project Structure

### Recommended Folder Structure

```
{Project}.Application/
├── Ports/
│   └── I{Interface}.cs                # Technical concern interface
└── Usecases/
    ├── {Entity}/
    │   ├── Create{Entity}Command.cs    # Command Use Case
    │   ├── Update{Entity}Command.cs    # Command Use Case
    │   ├── Get{Entity}ByIdQuery.cs     # Query Use Case
    │   ├── GetAll{Entity}sQuery.cs     # Query Use Case
    │   ├── On{Entity}Created.cs        # Event Use Case
    │   └── On{Entity}Updated.cs        # Event Use Case
    └── ...
```

> **Note**: Event Handlers are also a type of Use Case. As Event-Driven Use Cases, they are placed in the same folder alongside Commands/Queries.

We have confirmed the overall structure of the CQRS pattern and Mediator integration. The next section examines the nested class pattern that composes a single use case.

---

## Nested Class Pattern

### Pattern Description

Request, Response, Validator, and Usecase composing a single use case are defined as nested classes in one file.

**Advantages:**
- Related code is gathered in one place, improving cohesion
- The entire use case can be understood without navigating files
- Prevents naming conflicts (`CreateProductCommand.Request` vs `UpdateProductCommand.Request`)

### Basic Structure

```csharp
/// <summary>
/// {Feature description}
/// </summary>
public sealed class {Verb}{Entity}{Command|Query}
{
    /// <summary>
    /// {Command|Query} Request - {Request data description}
    /// </summary>
    public sealed record Request(...) : I{Command|Query}Request<Response>;

    /// <summary>
    /// {Command|Query} Response - {Response data description}
    /// </summary>
    public sealed record Response(...);

    /// <summary>
    /// Request Validator - FluentValidation validation rules (optional)
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            // Define validation rules
        }
    }

    /// <summary>
    /// {Command|Query} Handler - {Business logic description}
    /// </summary>
    internal sealed class Usecase(...) : I{Command|Query}Usecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(
            Request request,
            CancellationToken cancellationToken)
        {
            // Implementation (executed after Validator passes)
            // Application error: use ApplicationError.For<{UsecaseName}>(new {ErrorKind}(), value, message)
        }
    }
}
```

### Components

| Class | Access Modifier | Required | Description |
|--------|-----------|----------|------|
| `Request` | `public` | Required | Input data definition |
| `Response` | `public` | Required | Output data definition |
| `Validator` | `public` | Optional | FluentValidation validation rules |
| `Usecase` | `internal` | Required | Business logic implementation |

> **Note**: When a `Validator` is defined, it is automatically validated before Handler execution through the Pipeline.

We now understand the nested class structure. The next section covers the Apply merge pattern for simultaneously validating multiple Value Objects and creating Entities within a Usecase.

---

## Value Object Validation and Apply Merge Pattern

### Dual Validation Strategy

There are two validation layers in a Usecase. FluentValidation handles fast format validation, while Value Objects handle domain invariant validation.

| Validation Layer | Responsible | Purpose |
|------------|------|------|
| **FluentValidation** | Presentation Layer | Fast input format validation |
| **Value Object Validate()** | Domain Layer | Domain invariant validation |

### Apply Merge Pattern

The Apply pattern is used when validating multiple Value Objects simultaneously and creating an Entity.

The key point in the following code is that all fields are first validated with `Validate()`, then parallel validation results are merged with `Apply()`, and already-validated values are safely converted with `Unwrap()`.

```csharp
private static Fin<Product> CreateProduct(Request request)
{
    // 1. All fields: call VO Validate() (returns Validation<Error, T>)
    var name = ProductName.Validate(request.Name);
    var description = ProductDescription.Validate(request.Description);
    var price = Money.Validate(request.Price);
    var stockQuantity = Quantity.Validate(request.StockQuantity);

    // 2. Parallel validation via Apply, then Entity creation
    return (name, description, price, stockQuantity)
        .Apply((n, d, p, s) =>
            Product.Create(
                ProductName.Create(n).Unwrap(),
                ProductDescription.Create(d).Unwrap(),
                Money.Create(p).Unwrap(),
                Quantity.Create(s).Unwrap()))
        .As()
        .ToFin();
}
```

### Pattern Description

| Step | Method | Description |
|------|--------|------|
| 1 | `Validate()` | Collect validation of all fields as `Validation<Error, T>` |
| 2 | `Apply()` | All validations must succeed before Entity creation proceeds (parallel validation) |
| 3 | `Unwrap()` | Since values are already validated, safely convert to VO |
| 4 | `As().ToFin()` | Convert `Validation` to `Fin`. Multiple errors collected by `Apply` are preserved as `ManyErrors` — never reduced to the first error |

### Validation of Fields Without VOs

When not all fields are defined as Value Objects, use Named Context validation:

```csharp
private static Fin<Product> CreateProduct(Request request)
{
    // Fields with VOs
    var name = ProductName.Validate(request.Name);
    var price = Money.Validate(request.Price);

    // Fields without VOs: use Named Context
    var note = ValidationRules.For("Note")
        .NotEmpty(request.Note)
        .ThenMaxLength(500);

    // Merge all into a tuple - parallel validation via Apply
    return (name, price, note.Value)
        .Apply((n, p, noteValue) =>
            Product.Create(
                ProductName.Create(n).Unwrap(),
                noteValue,
                Money.Create(p).Unwrap()))
        .As()
        .ToFin();
}
```

> **Recommended**: Define frequently used fields as separate ValueObjects instead of Named Context.

---

## LINQ-Based Functional Implementation

### Recommendations

**LINQ-based functional implementation is recommended first.** It has the following advantages over traditional imperative implementation:

- **Code conciseness**: Eliminates imperative if-statements and intermediate variables (50-60% code reduction)
- **Automatic error handling**: Automatically returns `FinT.Fail` on Repository failure
- **Improved readability**: Declarative LINQ queries clarify business logic
- **Maintainability**: Functional chaining minimizes the impact of changes

### Conditional Checks with guard

Functional conditional checks are implemented using LanguageExt's `guard`.

The key point in the following code is that `guard(!exists, error)` returns an immediate failure when the condition is `false`, expressing conditional checks declaratively within a LINQ chain without the imperative `if` + `return` pattern.

```csharp
using static Functorium.Applications.Errors.ApplicationErrorKind;

// Using guard in a LINQ query
from exists in _productRepository.ExistsByName(productName)
from _ in guard(!exists, ApplicationError.For<CreateProductCommand>(
    new AlreadyExists(),
    request.Name,
    $"Product name already exists: '{request.Name}'"))
from product in _productRepository.Create(...)
select new Response(...)
```

`guard(condition, error)` returns `FinT.Fail` when the condition is `false`.

### What is the guard() Function?

`guard()` is a function provided by LanguageExt that performs conditional short-circuiting in LINQ comprehension syntax. When the condition is `false`, it immediately fails with the specified error; when `true`, it returns `Unit` and proceeds to the next step.

```csharp
// guard() in LINQ comprehension
from _  in guard(condition, Error.New("error message"))

// Equivalent imperative code
if (!condition) return Fin.Fail<T>(Error.New("error message"));
```

Using `guard()` allows expressing conditional checks declaratively within a LINQ chain without the imperative `if` + `return` pattern. Since the return type is `Fin<Unit>`, it is automatically lifted in a `FinT<IO, T>` chain.

### Execution Flow

```csharp
FinT<IO, Response> usecase = ...;

// FinT<IO, Response>
//  -Run()→           IO<Fin<Response>>
//  -RunAsync()→      Fin<Response>
//  -ToFinResponse()→ FinResponse<Response>
Fin<Response> response = await usecase.Run().RunAsync();
return response.ToFinResponse();
```

---

## Application Error Usage Patterns

### Usage

Use the `ApplicationError.For<TUsecase>()` method with `ApplicationErrorKind` sealed records:

```csharp
using Functorium.Applications.Errors;
using static Functorium.Applications.Errors.ApplicationErrorKind;

// Using within guard in a LINQ query
from exists in _productRepository.ExistsByName(productName)
from _ in guard(!exists, ApplicationError.For<CreateProductCommand>(
    new AlreadyExists(),
    request.Name,
    $"Product name already exists: '{request.Name}'"))
from product in _productRepository.Create(...)
select new Response(...)

// When returning directly
return FinResponse.Fail<Response>(
    ApplicationError.For<GetProductByIdQuery>(
        new NotFound(),
        productId.ToString(),
        $"Product not found. ID: {productId}"));
```

### Key ApplicationErrorKind

The following table lists the standard Application error types provided by Functorium. Most use cases require only these types, and for special cases you can extend by inheriting from `Custom`.

| Error Type | Description | Usage Example |
|-----------|------|----------|
| `Empty` | Value is empty | `new Empty()` |
| `Null` | Value is null | `new Null()` |
| `NotFound` | Cannot be found | `new NotFound()` |
| `AlreadyExists` | Already exists | `new AlreadyExists()` |
| `Duplicate` | Duplicate | `new Duplicate()` |
| `InvalidState` | Invalid state | `new InvalidState()` |
| `Unauthorized` | Not authenticated | `new Unauthorized()` |
| `Forbidden` | Access forbidden | `new Forbidden()` |
| `ValidationFailed` | Validation failed | `new ValidationFailed(PropertyName: "Email")` |
| `BusinessRuleViolated` | Business rule violated | `new BusinessRuleViolated(RuleName: "MaxOrderLimit")` |
| `ConcurrencyConflict` | Concurrency conflict | `new ConcurrencyConflict()` |
| `ResourceLocked` | Resource locked | `new ResourceLocked(ResourceName: "Order")` |
| `OperationCancelled` | Operation cancelled | `new OperationCancelled()` |
| `InsufficientPermission` | Insufficient permission | `new InsufficientPermission(Permission: "Admin")` |
| `Custom` | Custom error (define by inheritance) | `public sealed record PaymentDeclined : ApplicationErrorKind.Custom;` → `new PaymentDeclined()` |

### Error Code Format

```
ApplicationErrors.{UsecaseName}.{ErrorTypeName}
```

Examples:
- `Application.CreateProductCommand.AlreadyExists`
- `Application.GetProductByIdQuery.NotFound`
- `Application.UpdateOrderCommand.BusinessRuleViolated`

### Advantages

- **Type safety**: Compile-time validation based on sealed records
- **Consistency**: Same API pattern as DomainError and AdapterError
- **Conciseness**: Can be used inline without separate class definitions
- **Standardization**: Leverages standard error types from `ApplicationErrorKind`

---

## Command Implementation

### Complete Command Example

```csharp
using LayeredArch.Domain.Entities;
using LayeredArch.Domain.ValueObjects;
using LayeredArch.Domain.Repositories;
using Functorium.Applications.Errors;
using Functorium.Applications.Linq;
using static Functorium.Applications.Errors.ApplicationErrorKind;

namespace LayeredArch.Application.Usecases.Products;

/// <summary>
/// Create product Command - Apply pattern + LINQ implementation
/// </summary>
public sealed class CreateProductCommand
{
    public sealed record Request(
        string Name,
        string Description,
        decimal Price,
        int StockQuantity) : ICommandRequest<Response>;

    public sealed record Response(
        string ProductId,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        DateTime CreatedAt);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name is required")
                .MaximumLength(ProductName.MaxLength);

            RuleFor(x => x.Description)
                .MaximumLength(ProductDescription.MaxLength);

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than 0");

            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("Stock quantity must be 0 or greater");
        }
    }

    internal sealed class Usecase(
        IProductRepository productRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly IProductRepository _productRepository = productRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // 1. Value Object validation + Apply merge
            var productResult = CreateProduct(request);
            if (productResult.IsFail)
            {
                return productResult.Match(
                    Succ: _ => throw new InvalidOperationException(),
                    Fail: error => FinResponse.Fail<Response>(error));
            }

            // 2. Create ProductName (for duplicate check)
            var productName = ProductName.Create(request.Name).Unwrap();

            // 3. Duplicate check + save via LINQ (SaveChanges + event publishing handled automatically by the pipeline)
            FinT<IO, Response> usecase =
                from exists in _productRepository.ExistsByName(productName)
                from _ in guard(!exists, ApplicationError.For<CreateProductCommand>(
                    new AlreadyExists(),
                    request.Name,
                    $"Product name already exists: '{request.Name}'"))
                from product in _productRepository.Create((Product)productResult)
                select new Response(
                    product.Id.ToString(),
                    product.Name,
                    product.Description,
                    product.Price,
                    product.StockQuantity,
                    product.CreatedAt);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }

        private static Fin<Product> CreateProduct(Request request)
        {
            var name = ProductName.Validate(request.Name);
            var description = ProductDescription.Validate(request.Description);
            var price = Money.Validate(request.Price);
            var stockQuantity = Quantity.Validate(request.StockQuantity);

            return (name, description, price, stockQuantity)
                .Apply((n, d, p, s) =>
                    Product.Create(
                        ProductName.Create(n).Unwrap(),
                        ProductDescription.Create(d).Unwrap(),
                        Money.Create(p).Unwrap(),
                        Quantity.Create(s).Unwrap()))
                .As()
                .ToFin();
        }
    }
}
```

---

## Query Implementation

> **Core Principle**: Queries do not use `IRepository`. Through `IQueryPort`-based Read Adapters, **SQL is directly mapped to DTOs without Aggregate reconstruction.** This rule is enforced by `CqrsArchitectureRuleTests`.

### Query Port Definition Pattern

Ports used by Queries are defined in the Application layer (different from Domain's `IRepository`):

| Pattern | Interface | Purpose | Adapter Base Class |
|------|-----------|------|-------------------|
| List/Search | `IQueryPort<TEntity, TDto>` | `Search(spec, page, sort)` → `PagedResult<TDto>` | `DapperQueryBase<TEntity, TDto>` |
| Single lookup | `IQueryPort` (non-generic) | Define custom methods directly | Direct implementation |

#### List Query Port Definition

```csharp
// Application/Usecases/Products/Ports/IProductQuery.cs
using Functorium.Applications.Queries;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Application.Usecases.Products.Ports;

/// <summary>
/// Product read-only adapter port.
/// Projects directly from DB to DTO without Aggregate reconstruction.
/// </summary>
public interface IProductQuery : IQueryPort<Product, ProductSummaryDto> { }

public sealed record ProductSummaryDto(
    string ProductId,
    string Name,
    decimal Price);
```

#### Single Lookup Port Definition

```csharp
// Application/Usecases/Products/Ports/IProductDetailQuery.cs
using Functorium.Applications.Queries;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Application.Usecases.Products.Ports;

/// <summary>
/// Product single-item read-only adapter port.
/// Projects directly from DB to DTO without Aggregate reconstruction.
/// </summary>
public interface IProductDetailQuery : IQueryPort
{
    FinT<IO, ProductDetailDto> GetById(ProductId id);
}

public sealed record ProductDetailDto(
    string ProductId,
    string Name,
    string Description,
    decimal Price,
    DateTime CreatedAt,
    Option<DateTime> UpdatedAt);
```

### Single Lookup Query Example

Injects a custom Port that extends `IQueryPort` (non-generic):

```csharp
// Reference: Tests.Hosts/01-SingleHost/.../GetCustomerByIdQuery.cs
using LayeredArch.Application.Usecases.Customers.Ports;
using LayeredArch.Domain.AggregateRoots.Customers;

public sealed class GetCustomerByIdQuery
{
    public sealed record Request(string CustomerId) : IQueryRequest<Response>;

    public sealed record Response(
        string CustomerId,
        string Name,
        string Email,
        decimal CreditLimit,
        DateTime CreatedAt);

    public sealed class Usecase(ICustomerDetailQuery customerDetailQuery)
        : IQueryUsecase<Request, Response>
    {
        private readonly ICustomerDetailQuery _adapter = customerDetailQuery;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var customerId = CustomerId.Create(request.CustomerId);
            FinT<IO, Response> usecase =
                from dto in _adapter.GetById(customerId)
                select new Response(
                    dto.CustomerId,
                    dto.Name,
                    dto.Email,
                    dto.CreditLimit,
                    dto.CreatedAt);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
```

> **Key Point**: There is no Entity → DTO conversion code. The Adapter returns DTOs directly via SQL.

### List/Search Query Example

Uses the `Search()` method of `IQueryPort<TEntity, TDto>` with the Specification pattern:

```csharp
// Reference: samples/ecommerce-ddd/.../SearchProductsQuery.cs
using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using ECommerce.Application.Usecases.Products.Ports;
using ECommerce.Domain.AggregateRoots.Products;
using ECommerce.Domain.AggregateRoots.Products.Specifications;

public sealed class SearchProductsQuery
{
    private static readonly string[] AllowedSortFields = ["Name", "Price"];

    // Option<T>: optional filter field. default(Option<T>) = None → filter not applied
    public sealed record Request(
        Option<string> Name = default,
        Option<decimal> MinPrice = default,
        Option<decimal> MaxPrice = default,
        int Page = 1,
        int PageSize = PageRequest.DefaultPageSize,
        string SortBy = "",
        string SortDirection = "") : IQueryRequest<Response>;

    public sealed record Response(
        IReadOnlyList<ProductSummaryDto> Products,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages,
        bool HasNextPage,
        bool HasPreviousPage);

    // Validator: leveraging Option<T>-specific validation extension methods
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .MustSatisfyValidation(ProductName.Validate);

            this.MustBePairedRange(
                x => x.MinPrice,
                x => x.MaxPrice,
                Money.Validate,
                inclusive: true);

            RuleFor(x => x.SortBy).MustBeOneOf(AllowedSortFields);

            RuleFor(x => x.SortDirection)
                .MustBeEnumValue<Request, SortDirection>();
        }
    }

    public sealed class Usecase(IProductQuery productQuery)
        : IQueryUsecase<Request, Response>
    {
        private readonly IProductQuery _productQuery = productQuery;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var spec = BuildSpecification(request);
            var pageRequest = new PageRequest(request.Page, request.PageSize);
            var sortExpression = SortExpression.By(request.SortBy, SortDirection.Parse(request.SortDirection));

            FinT<IO, Response> usecase =
                from result in _productQuery.Search(spec, pageRequest, sortExpression)
                select new Response(
                    result.Items,
                    result.TotalCount,
                    result.Page,
                    result.PageSize,
                    result.TotalPages,
                    result.HasNextPage,
                    result.HasPreviousPage);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }

        private static Specification<Product> BuildSpecification(Request request)
        {
            var spec = Specification<Product>.All;

            // Option<T>.Iter(): adds filter if Some, ignores if None
            request.Name.Iter(name =>
                spec &= new ProductNameSpec(
                    ProductName.Create(name).Unwrap()));

            // Bind().Map().Iter(): adds range filter only when both Options are Some
            request.MinPrice.Bind(min => request.MaxPrice.Map(max => (min, max)))
                .Iter(t => spec &= new ProductPriceRangeSpec(
                    Money.Create(t.min).Unwrap(),
                    Money.Create(t.max).Unwrap()));

            return spec;
        }
    }
}
```

> **Note**: For details on Specification pattern definition, composition, and Repository integration, see [10-specifications.md](../domain/10-specifications).

### Full Retrieval (No Filter)

```csharp
// Reference: Tests.Hosts/01-SingleHost/.../GetAllProductsQuery.cs
public sealed class GetAllProductsQuery
{
    public sealed record Request() : IQueryRequest<Response>;

    public sealed record Response(IReadOnlyList<ProductSummaryDto> Products);

    public sealed class Usecase(IProductQuery productQuery)
        : IQueryUsecase<Request, Response>
    {
        private readonly IProductQuery _productQuery = productQuery;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            PageRequest pageRequest = new(1, int.MaxValue);

            FinT<IO, Response> usecase =
                from result in _productQuery.Search(Specification<Product>.All, pageRequest, SortExpression.Empty)
                select new Response(result.Items);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
```

---

## Domain Events

For domain event publishing and Event Handler implementation, see [07-domain-events.md](../domain/07-domain-events).

---

## Source Generator CtxEnricher

### Automatic Generation

When you define a Request record that implements `ICommandRequest<T>` or `IQueryRequest<T>`, `CtxEnricherGenerator` automatically generates `IUsecaseCtxEnricher<TRequest, TResponse>` implementation code that converts scalar properties of the Request/Response into `ctx.*` fields.

```csharp
public sealed class PlaceOrderCommand
{
    public sealed record Request(string CustomerId, List<OrderLine> Lines)
        : ICommandRequest<Response>, ICustomerRequest;
    //   CustomerId → ctx.customer_id  (Root: [CtxRoot] on ICustomerRequest)
    //   Lines      → ctx.place_order_command.request.lines_count  (collection → _count)

    public sealed record Response(string OrderId, int LineCount, decimal TotalAmount);
    //   OrderId     → ctx.place_order_command.response.order_id
    //   LineCount   → ctx.place_order_command.response.line_count
    //   TotalAmount → ctx.place_order_command.response.total_amount
}
```

### `[CtxRoot]` -- Cross-Usecase Search

When `[CtxRoot]` is applied to an interface, properties of that interface are promoted to `ctx.{field}` without the Usecase prefix. When multiple Usecases implement the same interface, all activities can be searched with a single `ctx.customer_id: "CUST-001"` in OpenSearch:

```csharp
[CtxRoot]
public interface ICustomerRequest { string CustomerId { get; } }
```

### `[CtxIgnore]` -- Exclude from Generation

Excludes sensitive or unnecessary properties from Enricher generation:

```csharp
public sealed record Request(
    string CustomerId,
    [property: CtxIgnore] string InternalToken  // ctx field not generated
) : ICommandRequest<Response>;
```

> **Details**: See [Logging Manual §Source Generator CtxEnricher](../observability/19-observability-logging#source-generator-자동-생성-ctxenrichergenerator).

---

## Transactions and Event Publishing (UsecaseTransactionPipeline)

### Automatic Pipeline Handling

Transaction commits (`SaveChanges`) and domain event publishing for Commands are handled automatically by `UsecaseTransactionPipeline`. **There is no need to directly inject `IUnitOfWork` or `IDomainEventPublisher` in the Usecase.**

```
[Command Handler]
  ↓ Repository.Create(aggregate)
  ↓   → IDomainEventCollector.Track(aggregate)  ← Repository calls automatically
  ↓ return FinResponse.Succ(response)
  ↓
[UsecaseTransactionPipeline]
  1. BeginTransactionAsync()           ← Explicit transaction start
  2. response = await next()           ← Handler execution
  3. if (response.IsFail) return       ← On failure, rollback via transaction Dispose
  4. UoW.SaveChanges()                 ← Save changes
  5. transaction.CommitAsync()         ← Transaction commit
  6. PublishTrackedEvents()            ← Collect, publish, clear events
  7. return response                   ← Return original success response
```

### Usecase Constructor Pattern

```csharp
// Command: inject only Repository (SaveChanges + event publishing handled by pipeline)
internal sealed class Usecase(
    IProductRepository productRepository)
    : ICommandUsecase<Request, Response>

// Query: inject IQueryPort-based Read Adapter (Transaction excluded at compile time via where ICommand constraint)
internal sealed class Usecase(IProductQuery productQuery)
    : IQueryUsecase<Request, Response>
```

### Command LINQ Pattern

```csharp
FinT<IO, Response> usecase =
    from product in _productRepository.Create(newProduct)  // Repository change
    select new Response(...);
// SaveChanges + domain event publishing is handled automatically by UsecaseTransactionPipeline
```

### Pipeline Execution Order

```
[Command] Request → Metrics → Tracing → Logging → Validation → Exception → Transaction → Custom → Handler
[Query]   Request → Metrics → Tracing → Logging → Validation → Caching → Exception → Custom → Handler
```

- **Transaction** applies only to Commands via the `where TRequest : ICommand<TResponse>` constraint (compile-time filtering)
- **Caching** applies only to Queries via the `where TRequest : IQuery<TResponse>` constraint (compile-time filtering)

- Transaction is positioned after Exception → Exception pipeline handles `SaveChanges` exceptions
- Transaction applies only to Commands via `where ICommand<TResponse>` constraint (compile-time)
- Caching applies only to Queries via `where IQuery<TResponse>` constraint (compile-time)

### Pipeline Registration

Enable the Transaction pipeline with explicit opt-in:

```csharp
services.AddMemoryCache();   // Required when UseCaching() is enabled

services
    .RegisterOpenTelemetry(configuration, AssemblyReference.Assembly)
    .ConfigurePipelines(pipelines => pipelines
        .UseObservability()   // Enable CtxEnricher, Metrics, Tracing, Logging all at once
        .UseValidation()
        .UseCaching()         // Caching requires separate activation
        .UseException()
        .UseTransaction())    // Explicitly enable Transaction
    .Build();
```

> The Caching pipeline requires `IMemoryCache` to be registered in DI (`services.AddMemoryCache()`). Without it, `UsecaseCachingPipeline` cannot be activated and a runtime `InvalidOperationException` is thrown.

> The Transaction pipeline requires all three of `IUnitOfWork`, `IDomainEventPublisher`, and `IDomainEventCollector` to be registered in DI (validated by `HasTransactionDependencies`).

### Transaction Isolation and Concurrency

Since multiple Repositories share a single DbContext, the default isolation level is Read Committed, and concurrency conflicts are handled by EF Core's Optimistic Concurrency (`[ConcurrencyCheck]` or `IsConcurrencyToken()`). On an Optimistic Concurrency conflict, `DbUpdateConcurrencyException` is thrown, and `UsecaseExceptionPipeline` converts it to `FinResponse.Fail`.

### Core Principles

| Principle | Description |
|------|------|
| Where SaveChanges is called | **Pipeline** handles it automatically (not called in the Usecase) |
| Repository role | Entity changes + `IDomainEventCollector.Track()` call |
| Multiple Repository calls | Wrapped in a single `SaveChanges()` transaction (guaranteed by pipeline) |
| Event publishing timing | Published only after `SaveChanges()` succeeds (guaranteed by pipeline) |
| On event publishing failure | Success response maintained (data already committed, only warning log recorded) |
| Behavior in Queries | Excluded at compile time via `where ICommand<TResponse>` constraint |

### IUnitOfWork Interface

**Location**: `Functorium.Applications.Persistence`

```csharp
public interface IUnitOfWork : IObservablePort
{
    FinT<IO, Unit> SaveChanges(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts an explicit transaction.
    /// Use when immediate-execution SQL such as ExecuteDeleteAsync/ExecuteUpdateAsync
    /// needs to be grouped in the same transaction as SaveChanges.
    /// </summary>
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
```

**IUnitOfWorkTransaction Interface:**

```csharp
/// <summary>
/// Explicit transaction scope.
/// Uncommitted transactions are automatically rolled back on Dispose.
/// </summary>
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
```

- Since it inherits `IObservablePort`, it supports automatic Pipeline generation and observability.
- In EF Core environments, it calls `DbContext.SaveChangesAsync()`; in InMemory environments, it is a no-op.
- `BeginTransactionAsync()` is called automatically by `UsecaseTransactionPipeline`, so there is no need to use it directly in a Usecase.

> **Reference**: For UoW Adapter implementations (EfCoreUnitOfWork, InMemoryUnitOfWork), see [13-adapters.md](../adapter/13-adapters).

---

## FinResponse and Error Handling

### FinResponse Type

```csharp
public abstract record FinResponse<A>
{
    public sealed record Succ(A Value) : FinResponse<A>;
    public sealed record Fail(Error Error) : FinResponse<A>;

    public abstract bool IsSucc { get; }
    public abstract bool IsFail { get; }
}
```

### Implicit Conversion

```csharp
// Success return - return the value directly
return new Response(productId, name);

// Failure return - return Error directly
return Error.New("Product not found");

// Using FinResponse.Fail
return FinResponse.Fail<Response>(error);
```

### Fin to FinResponse Conversion

```csharp
Fin<Response> fin = await usecase.Run().RunAsync();

// Type conversion only
FinResponse<Response> response = fin.ToFinResponse();

// Convert while mapping the value
return fin.ToFinResponse(product => new Response(...));
```

---

## FluentValidation Integration

### Define validation rules

```csharp
public sealed class Validator : AbstractValidator<Request>
{
    public Validator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(ProductName.MaxLength)
            .WithMessage($"Product name must not exceed {ProductName.MaxLength} characters");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0");
    }
}
```

### Automatic Validation via Pipeline

`UsecaseValidationPipeline` is registered via `UseValidation()` in `ConfigurePipelines`. Validators are automatically executed before Handler execution:

```csharp
services
    .AddValidatorsFromAssembly(typeof(Program).Assembly)
    .ConfigurePipelines(pipelines => pipelines
        .UseObservability()   // Enable CtxEnricher, Metrics, Tracing, Logging all at once
        .UseValidation()      // Explicitly enable Validation
        .UseException());
```

### FluentValidation Failure and Error Type Mapping

FluentValidation validation failures are converted to `AdapterErrorKind.PipelineValidation` in `UsecaseValidationPipeline`. This is a different error type from the Application layer's `ApplicationErrorKind.ValidationFailed`:

| Validation Layer | Error Type | Usage Location |
|------------|----------|----------|
| FluentValidation (Pipeline) | `AdapterErrorKind.PipelineValidation(PropertyName)` | Handled automatically by `UsecaseValidationPipeline` |
| VO/Business rules (Usecase) | `ApplicationErrorKind.ValidationFailed(PropertyName)` | Used manually within the Usecase |

On FluentValidation failure, each `ValidationFailure`'s `PropertyName` and `ErrorMessage` are converted to `AdapterError.For<UsecaseValidationPipeline>(new PipelineValidation(PropertyName), ...)` and returned as `FinResponse.Fail`.

### Value Object Validation Extension Methods

Functorium provides extension methods using C#14 extension members syntax that integrate Value Object `Validate()` methods into FluentValidation rules:

| Method | Usage Condition | Example |
|--------|----------|------|
| `MustSatisfyValidation` | Input type == output type | `RuleFor(x => x.Price).MustSatisfyValidation(Money.ValidateAmount)` |
| `MustSatisfyValidationOf<TVO>` | Input type != output type | `RuleFor(x => x.Name).MustSatisfyValidationOf<ProductName>(ProductName.Validate)` |

```csharp
public sealed class Validator : AbstractValidator<Request>
{
    public Validator()
    {
        // Same input/output type: decimal → Validation<Error, decimal>
        RuleFor(x => x.Price)
            .MustSatisfyValidation(Money.ValidateAmount);

        // Different input/output type: string → Validation<Error, ProductName>
        RuleFor(x => x.Name)
            .MustSatisfyValidationOf<ProductName>(ProductName.Validate);
    }
}
```

> **Note**: `MustSatisfyValidationOf` also provides a traditional extension method overload (`MustSatisfyValidationOf<TRequest, TProperty, TValueObject>`) for cases where C#14 extension members' type inference limitation prevents resolving additional generic parameters in `IRuleBuilderInitial`.

### EntityId / OneOf / PairedRange Validation Extension Methods

Functorium additionally provides extension methods for frequently used validation patterns:

| Method | Purpose | Example |
|--------|------|------|
| `MustBeEntityId<TRequest, TEntityId>` | Validates that a string is a valid EntityId format (NotEmpty + TryParse combined) | `RuleFor(x => x.ProductId).MustBeEntityId<Request, ProductId>()` |
| `MustBeOneOf<TRequest>` | Validates that a value is one of the allowed string list (case-insensitive, skips null/empty) | `RuleFor(x => x.SortBy).MustBeOneOf<Request>(["Name", "Price"])` |
| `MustBePairedRange<TRequest, T>` | Validates `Option<T>` paired range filter (both None = pass, only one Some = fail, both Some = range validation) | See example below |

```csharp
public sealed class Validator : AbstractValidator<Request>
{
    public Validator()
    {
        // EntityId format validation
        RuleFor(x => x.ProductId)
            .MustBeEntityId<Request, ProductId>();

        // Allowed values list validation
        RuleFor(x => x.SortBy)
            .MustBeOneOf<Request>(["Name", "Price", "CreatedAt"]);

        // Option<T> paired range filter validation
        this.MustBePairedRange(
            x => x.MinPrice,
            x => x.MaxPrice,
            Money.Validate);
    }
}
```

### SmartEnum Validation Extension Methods

FluentValidation extension methods for Ardalis.SmartEnum are also provided:

| Method | Purpose |
|--------|------|
| `MustBeEnum<TRequest, TSmartEnum, TValue>` | Validate by SmartEnum Value |
| `MustBeEnum<TRequest, TSmartEnum>` | Simplified overload for int-based SmartEnum |
| `MustBeEnumName<TRequest, TSmartEnum, TValue>` | Validate by SmartEnum Name |
| `MustBeEnumValue<TRequest, TSmartEnum>` | string Value SmartEnum (case-insensitive) |

### ICacheable Interface

Implementing `ICacheable` on a Query Request enables caching support:

```csharp
public sealed record Request(string ProductId) : IQueryRequest<Response>, ICacheable
{
    public string CacheKey => $"Product:{ProductId}";
    public TimeSpan? Duration => TimeSpan.FromMinutes(5);
}
```

`UsecaseCachingPipeline` applies only to Queries via the `where TRequest : IQuery<TResponse>` constraint and automatically caches Query Requests that implement `ICacheable`:
- Uses `IMemoryCache` for cache hit/miss handling based on `CacheKey`
- On cache hit, returns the cached response immediately without calling the Handler
- Only caches when `response.IsSucc` (failure responses are not cached)
- Default 5-minute cache when `Duration` is `null`

---

## Troubleshooting

### Compile Error When Converting `Validation` to `Fin` in Apply Pattern
**Cause:** The result of `Apply()` is a `Validation<Error, T>` type, and using it directly where `Fin<T>` is expected causes a type mismatch.
**Solution:** Use `.As().ToFin()` chaining to convert `Validation` to `Fin`. Example: `(name, price).Apply((n, p) => Product.Create(...)).As().ToFin();`

### Error Handling Not Working After Repository Call in `FinT<IO, T>` LINQ Query
**Cause:** In LINQ `from...in` syntax, when a Repository returns `FinT.Fail`, it automatically switches to the failure track. No separate error handling code is needed.
**Solution:** Do not handle errors with `if` statements inside LINQ queries. Repository failures are automatically propagated. Use the `guard(condition, error)` function when conditional checks are needed.

### Double Commit When Calling `SaveChanges()` Directly in Usecase
**Cause:** `UsecaseTransactionPipeline` automatically calls `SaveChanges()` after Handler success. Calling it directly in the Usecase results in a double commit.
**Solution:** Do not inject `IUnitOfWork` in the Usecase. Both `SaveChanges()` and domain event publishing are handled automatically by the pipeline. Only write code up to the Repository `Create()`/`Update()` calls.

---

## FAQ

### Q1. Are both FluentValidation and VO Validate() necessary?

**A:** Yes, they each serve different purposes:
- **FluentValidation**: Fast format validation at the Presentation Layer
- **VO Validate()**: Domain invariant validation at the Domain Layer

Even if FluentValidation passes, VO validation can still fail (e.g., regex pattern mismatch).

### Q2. When should the Apply merge pattern be used?

**A:** Use it when multiple VOs need to be validated simultaneously during Entity creation. It collects and returns all validation errors at once.

### Q3. When should guard be used?

**A:** Use it for conditional checks within LINQ queries:

```csharp
from exists in _repository.ExistsByName(name)
from _ in guard(!exists, ApplicationError.For<CreateProductCommand>(
    new AlreadyExists(), name, $"Name already exists: '{name}'"))
```

### Q4. How are Application errors defined?

**A:** Use the `ApplicationError.For<TUsecase>(ApplicationErrorKind, value, message)` pattern. Use inline without separate class definitions. Error codes are automatically generated in the format `Application.{UsecaseName}.{ErrorTypeName}`.

### Q5. Can domain entities be returned directly in the Response?

**A:** Not recommended. Use primitive types or DTOs:

```csharp
// ✗ Not recommended - exposing domain entity
public sealed record Response(Product Product);

// ✓ Recommended - use Primitive/DTO
public sealed record Response(
    string ProductId,
    string Name,
    decimal Price);
```

### Q6. Should CancellationToken always be passed?

**A:** Yes, always pass CancellationToken to asynchronous methods. However, when using the FinT<IO, T> pattern, it is handled internally by the Repository.

> **Query Handler note**: The Query Handler's `Handle` method receives a `CancellationToken cancellationToken` parameter, but there is no place to pass it directly within a FinT<IO, T> LINQ chain. CancellationToken is passed by including it in the Adapter method signature when needed within `IO.liftAsync` blocks inside the Adapter.

### Q7. Where are SaveChanges and event publishing handled?

**A:** `UsecaseTransactionPipeline` handles them automatically. There is no need to directly inject `IUnitOfWork` or `IDomainEventPublisher` in the Usecase.

1. **Usecase handles only business logic**: Write code only up to Repository `Create()`/`Update()` calls.
2. **Pipeline automatically calls SaveChanges**: Calls `IUnitOfWork.SaveChanges()` on Handler success, and does not commit on failure.
3. **Pipeline automatically publishes domain events**: After `SaveChanges()` succeeds, automatically publishes domain events from Aggregates tracked by Repository via `IDomainEventCollector.Track()`.

Activation: `.ConfigurePipelines(pipelines => pipelines.UseObservability().UseValidation().UseException().UseTransaction())`

---

## Reference Documents

| Document | Description |
|------|------|
| [05a-value-objects.md](../domain/05a-value-objects) | Value Object implementation patterns |
| [06b-entity-aggregate-core.md](../domain/06b-entity-aggregate-core) | Entity core patterns and Create pattern |
| [07-domain-events.md](../domain/07-domain-events) | Domain event publishing and Event Handler |
| [08a-error-system.md](../domain/08a-error-system) | Error system: foundations and naming |
| [08b-error-system-domain-app.md](../domain/08b-error-system-domain-app) | Error system: Domain/Application errors |
| [08c-error-system-adapter-testing.md](../domain/08c-error-system-adapter-testing) | Error system: Adapter errors and testing |
| [10-specifications.md](../domain/10-specifications) | Specification pattern (used in Use Cases) |
| [12-ports.md](../adapter/12-ports) | Repository interface design |
| [15a-unit-testing.md](../testing/15a-unit-testing) | Usecase test writing methods |

**External References:**
- [Mediator](https://github.com/martinothamar/Mediator) - Base library
- [LanguageExt](https://github.com/louthy/language-ext) - Library providing Fin types

---

## Related Documents

- Port interface definitions used in Use Cases: [Port Definition](../adapter/12-ports)
- Writing Adapters that implement Ports: [Adapter Implementation](../adapter/13-adapters)
- Pipeline and DI registration: [Adapter Integration](../adapter/14a-adapter-pipeline-di)
