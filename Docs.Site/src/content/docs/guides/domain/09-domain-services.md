---
title: "Domain Services"
---

Where should business rules that span multiple Aggregates be placed? Putting them in an Entity crosses boundaries, and putting them in a Usecase leaks domain logic. Domain Services solve this problem.

## Introduction

"Should the logic to verify whether an order amount exceeds a customer's credit limit go in Order or Customer?"
"If a business rule referencing multiple Aggregates is in a Usecase, doesn't that leak domain logic?"
"Where is the boundary between Domain Service and Application Service (Usecase)?"
"Is it acceptable for a Domain Service to use a Repository?"

These questions arise repeatedly when business logic crosses the boundary of a single Aggregate. Domain Services are building blocks that keep domain logic referencing multiple Aggregates in the Domain Layer.

### What You Will Learn

1. **Placement decision criteria for Domain Services** -- Decision tree for determining whether to place logic in Entity methods, Usecases, or Domain Services
2. **Two implementation patterns** -- Differences and selection criteria between the Pure pattern (default) and Repository pattern (Evans Ch.9)
3. **Integration methods from Usecases** -- Creation approach per pattern and LINQ chain usage

### Prerequisites

- [Aggregate Design Principles](../06a-aggregate-design) -- Aggregate boundaries and transaction principles
- [Error System: Basics and Naming](../08a-error-system) -- `Fin<T>` return patterns

> Evans requires Domain Services to be **Stateless** (no mutable state between calls), but does not require them to be **Pure** (no I/O).
> Functorium recommends the stricter pure function pattern by default and also presents the Repository usage pattern depending on cross-data scale.

## Summary

### Pure Pattern (Default) -- Small-Scale Cross Data

```csharp
// Domain Service definition -- no state, no I/O
public sealed class OrderCreditCheckService : IDomainService
{
    public Fin<Unit> ValidateCreditLimit(Customer customer, Money orderAmount) { ... }
}

// Directly instantiate in Usecase (DI not needed)
private readonly OrderCreditCheckService _creditCheckService = new();

// Used in FinT<IO, T> LINQ chain (Fin<T> auto-lifting)
from _2 in _creditCheckService.ValidateCreditLimit(customer, amount)
```

### Repository Pattern (Evans Ch.9) -- Large-Scale Cross Data

```csharp
// Domain Service definition -- depends on Repository interface
public sealed class ContactEmailCheckService : IDomainService
{
    private readonly IContactRepository _repository;
    public ContactEmailCheckService(IContactRepository repository) => _repository = repository;

    public FinT<IO, Unit> ValidateEmailUnique(
        EmailAddress email, Option<ContactId> excludeId = default) { ... }
}

// DI injection in Usecase
public sealed class Usecase(
    IContactRepository repository,
    ContactEmailCheckService emailCheckService) { ... }

// Used directly in FinT<IO, T> LINQ chain (already FinT<IO, T>)
from _ in _emailCheckService.ValidateEmailUnique(email, excludeId)
```

### Pattern Selection Criteria

| Decision Question | Pure Pattern | Repository Pattern |
|----------|----------|----------------|
| Is the data scale loadable by the Usecase? | YES | NO (requires full table scan) |
| Is cross data 1 to a few records? | YES | NO (large volume) |
| Does the Service need to own query rules? | NO | YES (Specification creation) |

### Key Procedures

1. **Placement decision**: Verify whether logic spans multiple Aggregates and the cross-data scale
2. **Pattern selection**: Decide between Pure pattern (default) or Repository pattern (Evans Ch.9)
3. **Class definition**: `sealed class`, implement `IDomainService` marker
4. **Method implementation**: Pure pattern returns `Fin<T>`, Repository pattern returns `FinT<IO, T>`
5. **Error definition**: Generate error codes with `DomainError.For<{ServiceName}>()` pattern
6. **Usecase integration**: Pure pattern uses `new()` direct creation, Repository pattern uses DI injection

### Key Concepts

| Concept | Description |
|------|------|
| `IDomainService` | Empty marker interface, for architecture test verification |
| Pure Pattern (default) | No external I/O, no state, returns `Fin<T>`, DI not needed |
| Repository Pattern (Evans Ch.9) | Depends on Repository interface, returns `FinT<IO, T>`, DI needed |
| Auto-lifting | `Fin<T>` auto-lifted in `FinT<IO, T>` LINQ chain (pure pattern only) |

---

## Why Domain Services

Domain Services are building blocks in DDD (Domain-Driven Design) for **placing domain logic that spans multiple Aggregates.**

### Problems Domain Services Solve

**Preventing Domain Logic Leakage**:
When business rules need to reference multiple Aggregates, the logic easily leaks to the Application Layer (Usecase). Domain Services keep this logic in the Domain Layer.

**Clear Role Separation**:
The boundary between Domain Service (domain logic) and Application Service (Usecase, I/O orchestration) becomes clear.

**Architecture Testability**:
Architecture rules can be verified with the `IDomainService` marker interface (e.g., whether a Domain Service does not depend on [IObservablePort](../adapter/12-ports)).

### Domain Logic Placement Decision

The following decision tree guides where to place logic based on its characteristics.

```
Does the logic belong to a single Aggregate?
├── YES -> Entity method or Value Object
└── NO
    ├── Is external I/O needed?
    │   ├── Is the cross data loadable by the Usecase?
    │   │   ├── YES -> Pure Domain Service (Usecase passes data)
    │   │   └── NO -> Repository-using Domain Service (Evans Ch.9)
    │   └── I/O not needed -> Pure Domain Service
    └── Does it change the state of multiple Aggregates?
        ├── YES -> Domain Event + separate Handler
        └── NO -> Domain Service
```

**Summary:**

| Condition | Placement |
|------|------|
| Logic within a single Aggregate | Entity method or Value Object |
| Multiple Aggregate reads + pure logic | Domain Service (Pure pattern) |
| Multiple Aggregates + large-scale cross data | Domain Service (Repository pattern) |
| Multiple Aggregate writes or external I/O orchestration | Usecase |

The following table summarizes the results of the decision tree above.

| Placement Location | Criteria | Example |
|----------|------|------|
| **Entity method** | State change within a single Aggregate | `Product.DeductStock()` |
| **Value Object** | Value validation, conversion, operations | `Money.Add()` |
| **Domain Service (Pure)** | References multiple Aggregates, Usecase can load data | `OrderCreditCheckService.ValidateCreditLimit()` |
| **Domain Service (Repository)** | References multiple Aggregates, large-scale cross data | `ContactEmailCheckService.ValidateEmailUnique()` |
| **Usecase** | Orchestration, I/O delegation | Repository calls, Event publishing |

Now that we understand the need for domain services, let us examine their precise definition and characteristics.

---

## What Are Domain Services (WHAT)

### Evans's Domain Service Definition

Three characteristics of Domain Services from Evans Blue Book Ch.9:

1. **Operations that correspond to domain concepts but do not belong to Entity or Value Object**
2. **The interface is defined in terms of other elements of the domain model**
3. **Stateless** -- no mutable state between calls

Evans requires **Stateless** but not **Pure** (no I/O). Since Repository interfaces are defined in the domain layer, it is legitimate in Evans DDD for Domain Services to use them.

### Functorium's Two Patterns

Based on Evans's Stateless principle, Functorium presents two patterns depending on cross-data scale.

| Characteristic | Pure Pattern (default) | Repository Pattern (Evans Ch.9) |
|------|-----------------|---------------------------|
| **Creation** | `new()` direct creation | DI injection |
| **I/O** | None | Uses Repository interface |
| **Return type** | `Fin<T>` | `FinT<IO, T>` |
| **Instance fields** | None | Only Repository references allowed |
| **Testing** | No mocks needed | Repository stub needed |
| **Application scenario** | Small-scale cross data | Large-scale cross data (DB query required) |

Both patterns satisfy Evans's Stateless requirement. The Pure pattern has no instance fields, and the Repository pattern holds only immutable Repository references.

### IDomainService Marker Interface

**Location**: `Functorium.Domains.Services`

```csharp
public interface IDomainService { }
```

An empty marker interface. It declares a class as a Domain Service and enables verification in architecture tests. Both patterns implement this interface.

### Domain Service vs Application Service (Usecase)

The following table summarizes the key differences between Domain Service and Application Service.

| Category | Domain Service | Application Service (Usecase) |
|------|---------------|-------------------------------|
| **Location** | Domain Layer | Application Layer |
| **I/O** | None (Pure pattern) or Repository only (Evans pattern) | Present (Repository, Event publishing) |
| **Role** | Business rules | Orchestration |
| **Return** | `Fin<T>` or `FinT<IO, T>` | `FinResponse<T>` |
| **Marker** | `IDomainService` | `ICommandUsecase<T,R>` / `IQueryUsecase<T,R>` |

### Position in Functorium Type Hierarchy

```
Domain Layer
├── Value Object     (SimpleValueObject<T>, ...)
├── Entity           (Entity<TId>, AggregateRoot<TId>)
├── Domain Event     (IDomainEvent, DomainEvent)
├── Domain Service   (IDomainService)         <- here
├── Domain Error     (DomainError, DomainErrorType)
└── Repository       (IRepository<TAggregate, TId>)
```

Now that we have confirmed the definition and location of Domain Services, let us look at the implementation step by step.

---

## Domain Service Implementation (HOW)

### Folder Structure

```
LayeredArch.Domain/
├── AggregateRoots/
│   ├── Customers/
│   └── Orders/
├── Services/                              <- Domain Service placement
│   └── OrderCreditCheckService.cs
└── Using.cs
```

### Namespace

- Framework interface: `Functorium.Domains.Services`
- Implementation class: `{Project}.Domain.Services`

### Pure Pattern (Default)

Suitable for small-scale scenarios where the Usecase can load cross data.

**Basic Structure:**

```csharp
using Functorium.Domains.Errors;
using Functorium.Domains.Services;
using static Functorium.Domains.Errors.DomainErrorType;
using static LanguageExt.Prelude;

namespace {Project}.Domain.Services;

public sealed class {ServiceName} : IDomainService
{
    public sealed record {ErrorName} : DomainErrorType.Custom;

    public Fin<Unit> {MethodName}({AggregateA} a, {AggregateB data} b)
    {
        // Cross-Aggregate business rule validation
        if (/* rule violation */)
            return DomainError.For<{ServiceName}>(
                new {ErrorName}(),
                currentValue,
                "Error message");

        return unit;
    }
}
```

**Complete Example: OrderCreditCheckService**

Implements a cross-Aggregate business rule between Customer credit limit and Order amount:

```csharp
using Functorium.Domains.Errors;
using Functorium.Domains.Services;
using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Orders;
using static Functorium.Domains.Errors.DomainErrorType;
using static LanguageExt.Prelude;

namespace LayeredArch.Domain.Services;

public sealed class OrderCreditCheckService : IDomainService
{
    public sealed record CreditLimitExceeded : DomainErrorType.Custom;

    /// <summary>
    /// Validates whether the order amount is within the customer's credit limit.
    /// </summary>
    public Fin<Unit> ValidateCreditLimit(Customer customer, Money orderAmount)
    {
        if (orderAmount > customer.CreditLimit)
            return DomainError.For<OrderCreditCheckService>(
                new CreditLimitExceeded(),
                customer.Id.ToString(),
                $"Order amount {(decimal)orderAmount} exceeds customer credit limit {(decimal)customer.CreditLimit}");

        return unit;
    }

    /// <summary>
    /// Validates whether the sum of existing orders and the new order is within the credit limit.
    /// </summary>
    public Fin<Unit> ValidateCreditLimitWithExistingOrders(
        Customer customer,
        Seq<Order> existingOrders,
        Money newOrderAmount)
    {
        var totalExisting = existingOrders.Fold(0m, (acc, o) => acc + (decimal)o.TotalAmount);
        var totalWithNew = totalExisting + (decimal)newOrderAmount;

        if (totalWithNew > (decimal)customer.CreditLimit)
            return DomainError.For<OrderCreditCheckService>(
                new CreditLimitExceeded(),
                customer.Id.ToString(),
                $"Total order amount {totalWithNew} exceeds customer credit limit {(decimal)customer.CreditLimit}");

        return unit;
    }
}
```

**Key Points:**

- `sealed class` -- no inheritance intended
- Returns `Fin<Unit>` -- success (`unit`) or `DomainError`
- `DomainError.For<OrderCreditCheckService>` -- auto-generates error code (`DomainErrors.OrderCreditCheckService.CreditLimitExceeded`)
- `Money` comparison uses `ComparableSimpleValueObject<decimal>` operators (`>`, `<`, `>=`, `<=`)
- Uses `Seq<T>.Fold` -- used instead of `Sum()` (to avoid ambiguity between LanguageExt and System.Linq)

### Repository Pattern (Evans Ch.9)

Suitable for large-scale scenarios where the Usecase cannot easily load cross data. The Domain Service directly queries data through the Repository interface.

**Basic Structure:**

```csharp
using Functorium.Domains.Errors;
using Functorium.Domains.Services;
using static Functorium.Domains.Errors.DomainErrorType;
using static LanguageExt.Prelude;

namespace {Project}.Domain.Services;

public sealed class {ServiceName} : IDomainService
{
    private readonly I{Aggregate}Repository _repository;

    public {ServiceName}(I{Aggregate}Repository repository)
        => _repository = repository;

    public sealed record {ErrorName} : DomainErrorType.Custom;

    public FinT<IO, Unit> {MethodName}({Parameters})
    {
        // Specification creation -> Repository query -> validation
        var spec = new {Specification}({Parameters});
        return from exists in _repository.Exists(spec)
               from _ in CheckCondition(exists)
               select unit;
    }

    private static Fin<Unit> CheckCondition(bool condition)
    {
        if (condition)
            return DomainError.For<{ServiceName}>(
                new {ErrorName}(), currentValue, "Error message");
        return unit;
    }
}
```

**Complete Example: ContactEmailCheckService**

Implements a cross-Aggregate business rule that validates Contact email uniqueness. Since it requires scanning the entire Contact table, it cannot be implemented with the Pure pattern where the Usecase passes data:

```csharp
public sealed class ContactEmailCheckService : IDomainService
{
    private readonly IContactRepository _repository;

    public ContactEmailCheckService(IContactRepository repository)
        => _repository = repository;

    public sealed record EmailAlreadyInUse : DomainErrorType.Custom;

    /// <summary>
    /// Validates that the email address is not used by another Contact.
    /// </summary>
    public FinT<IO, Unit> ValidateEmailUnique(
        EmailAddress email, Option<ContactId> excludeId = default)
    {
        var spec = new ContactEmailUniqueSpec(email, excludeId);
        return from exists in _repository.Exists(spec)
               from _ in CheckNotExists(email, exists)
               select unit;
    }

    private static Fin<Unit> CheckNotExists(EmailAddress email, bool exists)
    {
        if (exists)
            return DomainError.For<ContactEmailCheckService>(
                new EmailAlreadyInUse(),
                (string)email,
                $"Email '{(string)email}' is already in use");
        return unit;
    }
}
```

**Key Points:**

- Returns `FinT<IO, Unit>` -- `FinT<IO, T>` not `Fin<T>` because it includes Repository I/O
- Depends on Repository **via interface only** -- interfaces defined in Domain Layer
- `Specification` creation -- Domain Service owns query rules
- LINQ query syntax -- composes I/O and pure validation via `from ... in ...` chain

### Global Using Configuration

Add to the Domain project's `Using.cs`:

```csharp
global using Functorium.Domains.Services;
```

Now that we have completed the Domain Service implementation, let us see how to call and integrate it from a Usecase.

---

## Usage from Usecase (HOW)

### Pure Pattern: Direct Creation + Auto-Lifting

Pure pattern Domain Services have no state or I/O, so they are **directly created as member variables** in the Usecase. `Fin<Unit>` return values are **auto-lifted** in `FinT<IO, T>` LINQ chains.

#### Fin<T> Auto-Lifting

Methods returning `Fin<T>` can be used directly in `FinT<IO, T>` LINQ chains with `from ... in` syntax:

```csharp
FinT<IO, Response> usecase =
    from customer in _customerRepository.GetById(customerId)      // FinT<IO, Customer>
    from _2 in _creditCheckService.ValidateCreditLimit(customer, amount)  // Fin<Unit> -> auto-lifting
    from order in _orderRepository.Create(Order.Create(...))      // FinT<IO, Order>
    select new Response(...);
```

This pattern is identical to existing Entity methods like `Product.DeductStock()`:

```csharp
// Entity method (existing pattern)
from _1 in product.DeductStock(quantity)        // Fin<Unit> -> auto-lifting

// Domain Service (same pattern)
from _2 in _creditCheckService.ValidateCreditLimit(customer, amount)  // Fin<Unit> -> auto-lifting
```

#### Complete Usecase Example

```csharp
public sealed class Usecase(
    ICustomerRepository customerRepository,
    IOrderRepository orderRepository,
    IProductCatalog productCatalog)
    : ICommandUsecase<Request, Response>
{
    private readonly ICustomerRepository _customerRepository = customerRepository;
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly IProductCatalog _productCatalog = productCatalog;
    private readonly OrderCreditCheckService _creditCheckService = new();  // Direct creation

    public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
    {
        // 1. Value Object creation (pure validation)
        var shippingAddressResult = ShippingAddress.Create(request.ShippingAddress);
        var quantityResult = Quantity.Create(request.Quantity);

        if (shippingAddressResult.IsFail)
            return FinResponse.Fail<Response>(shippingAddressResult.Match(
                Succ: _ => throw new InvalidOperationException(), Fail: e => e));
        if (quantityResult.IsFail)
            return FinResponse.Fail<Response>(quantityResult.Match(
                Succ: _ => throw new InvalidOperationException(), Fail: e => e));

        var customerId = CustomerId.Create(request.CustomerId);
        var productId = ProductId.Create(request.ProductId);
        var shippingAddress = (ShippingAddress)shippingAddressResult;
        var quantity = (Quantity)quantityResult;

        // 2. Query -> Credit check (Domain Service) -> Order creation -> event publishing
        FinT<IO, Response> usecase =
            from customer in _customerRepository.GetById(customerId)           // 1. Customer lookup
            from exists in _productCatalog.ExistsById(productId)               // 2. Product existence check
            from _1 in guard(exists, ApplicationError.For<...>(...))            // 3. Fail if product not found
            from unitPrice in _productCatalog.GetPrice(productId)              // 4. Price lookup
            from _2 in _creditCheckService.ValidateCreditLimit(                 // 5. Credit limit validation
                customer, unitPrice.Multiply(quantity))
            from order in _orderRepository.Create(                             // 6. Order creation
                Order.Create(productId, quantity, unitPrice, shippingAddress))
            select new Response(...);
            // SaveChanges + event publishing are automatically handled by UsecaseTransactionPipeline

        Fin<Response> response = await usecase.Run().RunAsync();
        return response.ToFinResponse();
    }
}
```

### Repository Pattern: DI Injection + Direct Chaining

Repository pattern Domain Services are injected via DI. Since they return `FinT<IO, T>`, they are **directly chained** rather than auto-lifted.

```csharp
public sealed class Usecase(
    IContactRepository repository,
    ContactEmailCheckService emailCheckService)
    : ICommandUsecase<Request, Response>
{
    private readonly IContactRepository _repository = repository;
    private readonly ContactEmailCheckService _emailCheckService = emailCheckService;

    public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
    {
        // ...
        FinT<IO, Response> usecase =
            from _ in _emailCheckService.ValidateEmailUnique(email, excludeId)  // FinT<IO, Unit> direct chaining
            from saved in _repository.Create(contact)
            select new Response(...);
        // ...
    }
}
```

### Flow Comparison

**Pure Pattern:**

```
Usecase (Application Layer, I/O orchestration)
│
├── Repository.GetById()        ← I/O (Adapter)
├── ProductCatalog.GetPrice()   ← I/O (Adapter)
├── CreditCheckService.Validate()  <- pure logic (Domain Service)
└── Repository.Create()         ← I/O (Adapter)
    // SaveChanges + event publishing are automatically handled by UsecaseTransactionPipeline
```

**Repository Pattern:**

```
Usecase (Application Layer, I/O orchestration)
│
├── EmailCheckService.ValidateEmailUnique()  <- Domain Service (uses Repository internally)
└── Repository.Create()                      ← I/O (Adapter)
    // SaveChanges + event publishing are automatically handled by UsecaseTransactionPipeline
```

---

## DI Registration

### Pure Pattern: DI Registration Not Needed

Pure pattern Domain Services have no state or constructor parameters, so they are **not registered in the DI container**. They are directly created as member variables in the Usecase.

```csharp
// Directly created inside Usecase
private readonly OrderCreditCheckService _creditCheckService = new();
```

### Repository Pattern: DI Registration Needed

Repository pattern Domain Services receive Repository injection in the constructor, so they **must be registered in the DI container**.

```csharp
services.AddScoped<ContactEmailCheckService>();
```

### Differences from IObservablePort

| Category | Domain Service (Pure) | Domain Service (Repository) | Adapter (IObservablePort) |
|------|---------------------|---------------------------|-------------------|
| **Creation** | `new()` direct creation | DI `AddScoped<>()` | DI `RegisterScopedObservablePort<I, P>()` |
| **Pipeline** | Not needed | Not needed | Auto-generated (observability) |
| **Lifetime** | Same as Usecase | Scoped (per request) | Scoped (per request) |
| **Observability** | Not needed | Not needed | Auto-applied |

### Inter-Domain Service Calls

Pure pattern Domain Services can call other pure Domain Services:

```csharp
public sealed class OrderPricingService : IDomainService
{
    private readonly DiscountCalculationService _discountService = new();

    public Fin<Money> CalculateFinalPrice(Order order, Customer customer)
    {
        // Call another Domain Service
        var discount = _discountService.CalculateDiscount(customer, order.TotalAmount);
        return discount.Map(d => order.TotalAmount.Subtract(d));
    }
}
```

**Caution**: If inter-Domain Service calls become frequent with 3 or more, consider introducing a higher-level orchestrating Domain Service or orchestrating directly in the Usecase.

---

## Test Patterns

### Pure Pattern Unit Tests

Pure pattern Domain Services are tested directly without Mocks:

```csharp
public class OrderCreditCheckServiceTests
{
    private readonly OrderCreditCheckService _sut = new();

    private static Customer CreateSampleCustomer(decimal creditLimit = 5000m)
    {
        return Customer.Create(
            CustomerName.Create("John").ThrowIfFail(),
            Email.Create("john@example.com").ThrowIfFail(),
            Money.Create(creditLimit).ThrowIfFail());
    }

    [Fact]
    public void ValidateCreditLimit_ReturnsSuccess_WhenAmountWithinLimit()
    {
        // Arrange
        var customer = CreateSampleCustomer(creditLimit: 5000m);
        var orderAmount = Money.Create(3000m).ThrowIfFail();

        // Act
        var actual = _sut.ValidateCreditLimit(customer, orderAmount);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void ValidateCreditLimit_ReturnsFail_WhenAmountExceedsLimit()
    {
        // Arrange
        var customer = CreateSampleCustomer(creditLimit: 5000m);
        var orderAmount = Money.Create(6000m).ThrowIfFail();

        // Act
        var actual = _sut.ValidateCreditLimit(customer, orderAmount);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void ValidateCreditLimit_ReturnsSuccess_WhenAmountEqualsLimit()
    {
        // Arrange
        var customer = CreateSampleCustomer(creditLimit: 5000m);
        var orderAmount = Money.Create(5000m).ThrowIfFail();

        // Act
        var actual = _sut.ValidateCreditLimit(customer, orderAmount);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }
}
```

**Test Characteristics:**

- No mocks needed -- pure function so only input/output is verified
- `_sut = new()` -- directly created without dependencies
- Boundary value tests -- `=` (equal to limit), `<` (below limit), `>` (above limit)

### Repository Pattern Unit Tests

Repository pattern Domain Services are tested using Repository stubs:

```csharp
public class ContactEmailCheckServiceTests
{
    private static ContactEmailCheckService CreateSut(bool existsResult)
    {
        var repository = Substitute.For<IContactRepository>();
        repository.Exists(Arg.Any<ContactEmailUniqueSpec>())
            .Returns(FinTFactory.Succ(existsResult));
        return new ContactEmailCheckService(repository);
    }

    [Fact]
    public async Task ValidateEmailUnique_ReturnsSuccess_WhenEmailNotExists()
    {
        // Arrange
        var sut = CreateSut(existsResult: false);
        var email = EmailAddress.Create("new@example.com").ThrowIfFail();

        // Act
        var actual = await sut.ValidateEmailUnique(email).Run().RunAsync();

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateEmailUnique_ReturnsFail_WhenEmailExists()
    {
        // Arrange
        var sut = CreateSut(existsResult: true);
        var email = EmailAddress.Create("existing@example.com").ThrowIfFail();

        // Act
        var actual = await sut.ValidateEmailUnique(email).Run().RunAsync();

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
```

**Test Characteristics:**

- Repository stub needed -- `Substitute.For<IContactRepository>()`
- `async Task` -- `FinT<IO, T>` runs asynchronously
- `.Run().RunAsync()` -- IO monad execution

### Usecase Unit Tests (Including Domain Service)

**Pure Pattern**: Domain Service is directly created inside the Usecase, so no separate setup is needed. Only Repository/Adapter are mocked:

```csharp
public class CreateOrderWithCreditCheckCommandTests
{
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IProductCatalog _productCatalog = Substitute.For<IProductCatalog>();
    private readonly CreateOrderWithCreditCheckCommand.Usecase _sut;

    public CreateOrderWithCreditCheckCommandTests()
    {
        _sut = new CreateOrderWithCreditCheckCommand.Usecase(
            _customerRepository, _orderRepository, _productCatalog);
    }

    [Fact]
    public async Task Handle_ReturnsFail_WhenCreditLimitExceeded()
    {
        // Arrange
        var customer = CreateSampleCustomer(creditLimit: 1000m);
        _customerRepository.GetById(Arg.Any<CustomerId>())
            .Returns(FinTFactory.Succ(customer));
        _productCatalog.ExistsById(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(true));
        _productCatalog.GetPrice(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(Money.Create(1000m).ThrowIfFail()));

        var request = new CreateOrderWithCreditCheckCommand.Request(
            customer.Id.ToString(),
            Seq(new CreateOrderWithCreditCheckCommand.OrderLineRequest(
                ProductId.New().ToString(), 2)),
            "Seoul, Korea");

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert -- 1000m x 2 = 2000m > 1000m credit limit
        actual.IsSucc.ShouldBeFalse();
    }
}
```

**Repository Pattern**: Since the Domain Service is also DI-injected, it is directly created and passed in Usecase tests:

```csharp
// Create Domain Service using Repository stub then inject into Usecase
var emailCheckService = new ContactEmailCheckService(stubRepository);
var sut = new CreateContactCommand.Usecase(contactRepository, emailCheckService);
```

### Test Folder Structure

```
LayeredArch.Tests.Unit/
├── Domain/
│   ├── Customers/
│   ├── Orders/
│   ├── Products/
│   ├── Services/                              <- Domain Service tests
│   │   └── OrderCreditCheckServiceTests.cs
│   └── SharedModels/
└── Application/
    └── Orders/
        ├── CreateOrderCommandTests.cs
        └── CreateOrderWithCreditCheckCommandTests.cs  <- Usecase tests
```

---

## Checklist

### Common (Both Patterns)

- [ ] Does it implement the `IDomainService` marker interface?
- [ ] Is it declared as `sealed class`?
- [ ] Is it placed in the Domain Layer (`{Project}.Domain.Services` namespace)?
- [ ] Does it NOT inherit from `IObservablePort`?
- [ ] Does the logic actually span multiple Aggregates? (Single Aggregate logic belongs in Entity methods)
- [ ] Are errors created with `DomainError.For<{ServiceName}>`?
- [ ] Does it only perform validation/calculation without state changes?

### Pure Pattern Additional Checklist

- [ ] Are there no external I/O dependencies? (Repository, HttpClient, etc.)
- [ ] Are there no instance fields?
- [ ] Does it return `Fin<T>` or `Fin<Unit>`?
- [ ] Is it directly created as a member variable in the Usecase? (`new()`)
- [ ] Is it called with `from ... in` syntax in the `FinT<IO, T>` LINQ chain?
- [ ] Are unit tests for the Domain Service itself written without Mocks?

### Repository Pattern Additional Checklist

- [ ] Do instance fields hold only Repository interface references?
- [ ] Does it return `FinT<IO, T>` or `FinT<IO, Unit>`?
- [ ] Is it registered in the DI container with `AddScoped<>()`?
- [ ] Is it received via constructor injection in the Usecase?
- [ ] Are there unit tests using Repository stubs?

---

## Troubleshooting

### Difficulty deciding whether a Domain Service should use a Repository

**Decision criteria:** Verify whether the Usecase can load cross data at the required scale.

- **Small scale (1 to a few records):** The Usecase loads data via Repository then passes it to the pure Domain Service (Pure pattern).
  ```csharp
  from customer in _customerRepository.GetById(customerId)        // Usecase handles I/O
  from _2 in _creditCheckService.ValidateCreditLimit(customer, amount)  // Domain Service does pure validation only
  ```
- **Large scale (full table scan, etc.):** The Domain Service directly queries through the Repository interface (Repository pattern, Evans Ch.9).
  ```csharp
  from _ in _emailCheckService.ValidateEmailUnique(email, excludeId)  // Domain Service uses Repository internally
  ```

By default, **try the Pure pattern first,** and only consider the Repository pattern when the Usecase cannot easily load the data.

### Inter-Domain Service calls have become too complex

**Cause:** Complexity increases when the call chain between Domain Services grows to 3 or more.

**Resolution:** Introduce a higher-level orchestrating Domain Service, or switch to a pattern where the Usecase individually calls and orchestrates each Domain Service.

### Architecture test warns that Domain Service depends on Port

**Cause:** The Domain Service may be inheriting `IObservablePort` or receiving a Port interface as a constructor parameter.

**Resolution:** Domain Services should only implement the `IDomainService` marker. `IObservablePort` is Adapter-only; remove `IObservablePort` dependencies from Domain Services. For the Repository pattern, Repository interfaces use interfaces defined in the Domain Layer, not `IObservablePort`.

### Architecture test blocks Repository pattern instance fields

**Cause:** SingleHost's `DomainServiceArchitectureRuleTests` enforces the Pure pattern with `RequireNoInstanceFields()`.

**Resolution:** This architecture test is a rule applied to SingleHost's reference implementation (Pure pattern). In projects using the Repository pattern, adjust the rule to allow Repository interface references, or separate it into a different test.

---

## FAQ

### Q1. What is the criterion for distinguishing Domain Service from Usecase (Application Service)?

Domain Services perform business rules and are located in the Domain Layer. Usecases handle I/O orchestration and are located in the Application Layer. In the Pure pattern, the key criterion is "Does this logic need I/O?", and in the Repository pattern, the key criterion is "Is this logic a domain rule or orchestration?"

### Q2. Is it correct for Domain Services to use Repositories in Evans DDD?

Yes. In Evans Blue Book Ch.9, Domain Services only require **Stateless** and not **Pure**. Since Repository interfaces are defined in the domain layer, it is legitimate in Evans DDD for Domain Services to use them. Functorium's Pure pattern is a stricter default than Evans, and it is not the only correct answer.

### Q3. Which should I choose between the Pure pattern and Repository pattern?

**The default is the Pure pattern.** In small-scale scenarios where the Usecase can load cross data, the Pure pattern is simpler and easier to test. Use the Repository pattern only when all of the following conditions are met:

- The data scale is difficult for the Usecase to load (full table scan, etc.)
- The Service needs to own query rules (Specification)
- Domain logic needs to encapsulate query and validation as a single cohesive operation

### Q4. Doesn't the architecture test block the Repository pattern?

SingleHost's `DomainServiceArchitectureRuleTests` enforces the Pure pattern with `RequireNoInstanceFields()`. This is a rule applied to SingleHost's reference implementation, and projects using the Repository pattern must adjust this rule.

### Q5. Why is the Domain Service not registered in the DI container?

This applies only to the Pure pattern. The Pure pattern has no state and no constructor parameters, so DI is unnecessary. The Repository pattern receives Repository injection in the constructor, so DI registration with `AddScoped<>()` is required.

### Q6. How are errors returned from a Domain Service?

Use the `DomainError.For<{ServiceName}>(new {ErrorType}(), currentValue, message)` pattern. Error codes are auto-generated in the format `DomainErrors.{ServiceName}.{ErrorType}`. Both patterns are identical.

### Q7. If logic is within a single Aggregate but the method is too complex, can it be separated into a Domain Service?

No. Logic within a single Aggregate should be placed in Entity methods as a principle. If a method is complex, separate it into private methods within the Entity. Domain Services are only used for logic that **spans multiple Aggregates**.

### Q8. Are Mocks needed in Domain Service tests?

The Pure pattern verifies only input/output directly without Mocks. The Repository pattern requires Repository stubs (NSubstitute, etc.). In both cases, the key is verifying the Domain Service's business rules themselves.

---

## References

- [04-ddd-tactical-overview.md](../04-ddd-tactical-overview) - DDD tactical design overview, type mapping table
- [06a-aggregate-design.md](../06a-aggregate-design) - Aggregate design principles, [06b-entity-aggregate-core.md](../06b-entity-aggregate-core) - Entity/Aggregate core patterns, [06c-entity-aggregate-advanced.md](../06c-entity-aggregate-advanced) - Advanced patterns
- [08a-error-system.md](../08a-error-system) - Error handling basic principles and naming conventions
- [08b-error-system-domain-app.md](../08b-error-system-domain-app) - DomainError definition and test patterns
- [11-usecases-and-cqrs.md](../application/11-usecases-and-cqrs) - Usecase implementation (Application Service)
- [12-ports.md](../adapter/12-ports) - Port/Adapter pattern (difference from IPort)
- [15a-unit-testing.md](../testing/15a-unit-testing) - Unit test rules (T1_T2_T3, AAA pattern)

### Practical Examples Files

| File | Description |
|------|------|
| `Src/Functorium/Domains/Services/IDomainService.cs` | Marker interface |
| `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/Services/OrderCreditCheckService.cs` | Pure pattern implementation |
| `Tests.Hosts/01-SingleHost/Src/LayeredArch.Application/Usecases/Orders/CreateOrderWithCreditCheckCommand.cs` | Pure pattern Usecase usage |
| `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Domain/Services/OrderCreditCheckServiceTests.cs` | Pure pattern tests |
| `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Application/Orders/CreateOrderWithCreditCheckCommandTests.cs` | Usecase tests |

### designing-with-types Example (Repository Pattern)

| File | Description |
|------|------|
| `Docs.Site/src/content/docs/samples/designing-with-types/Src/DesigningWithTypes/AggregateRoots/Contacts/Services/ContactEmailCheckService.cs` | Repository pattern implementation |
