---
title: "Domain Code Design"
---

The rules defined in natural language in the [business requirements](../00-business-requirements/) were classified as invariants and type strategies were derived in the [type design decisions](../01-type-design-decisions/). This document maps those strategies to C# and Functorium DDD building blocks, and examines the concrete code implementation of each pattern.

## Design Decision to C# Implementation Mapping

The following table shows the 1:1 mapping between design decisions and implementation patterns. Each pattern is examined in code in the subsequent sections.

| Design Decision | Functorium Type | Example | Guarantee Effect |
|---|---|---|---|
| Single value validation + immutability + normalization | `SimpleValueObject<T>` + `Validate` chain | CustomerName, Email, ProductName, TagName | Validation at creation, Trim/ToLower normalization, empty string blocking |
| Comparable single value + arithmetic operations | `ComparableSimpleValueObject<T>` | Money, Quantity | Size comparison (`>`, `<`), arithmetic (Add, Subtract), summation (Sum) |
| Smart Enum + state transition rules | `SimpleValueObject<string>` + `HashMap` transition map | OrderStatus | Only allowed transitions possible, error returned on invalid transition |
| Aggregate Root dual factory | `AggregateRoot<TId>` + `Create`/`CreateFromValidated` | Customer, Product, Order, Inventory, Tag | Separation of domain creation (validation+events) and ORM restoration (no validation) |
| Child entity + collection management | `Entity<TId>` + private `List` + `IReadOnlyList` exposure | OrderLine (child of Order) | External direct collection modification prevented |
| Cross-Aggregate business rules | `IDomainService` | OrderCreditCheckService | Credit limit validation between Customer and Order |
| Queryable domain specifications | `ExpressionSpecification<T>` | ProductNameUniqueSpec, CustomerEmailSpec | Expression Tree based EF Core SQL auto-translation |
| Persistence abstraction | `IRepository<T, TId>` + custom methods | ICustomerRepository | Specification-based existence check (Exists) |
| Domain events + domain errors | Nested `sealed record : DomainEvent` / `DomainErrorKind.Custom` | Customer.CreatedEvent, Order.EmptyOrderLines | Events/errors cohesive within Aggregate at namespace level |
| Soft Delete + guard | `ISoftDeletableWithUser` + `DeletedAt.IsSome` guard | Product.Update() | Blocks changes to deleted Aggregates |

## Code Snippets by Pattern

### 1. SimpleValueObject + Validate Chain

Each value object inherits from `SimpleValueObject<T>` and chains validation rules in the `Validate` method. The private constructor blocks `new`, exposing only the `Create` factory.

**CustomerName** -- String length validation + Trim normalization:

```csharp
public sealed class CustomerName : SimpleValueObject<string>
{
    public const int MaxLength = 100;

    private CustomerName(string value) : base(value) { }

    public static Fin<CustomerName> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new CustomerName(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<CustomerName>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenNormalize(v => v.Trim())
            .ThenMaxLength(MaxLength);

    public static CustomerName CreateFromValidated(string value) => new(value);

    public static implicit operator string(CustomerName name) => name.Value;
}
```

**Email** -- Regex matching + ToLowerInvariant normalization:

```csharp
public sealed partial class Email : SimpleValueObject<string>
{
    public const int MaxLength = 320;

    private Email(string value) : base(value) { }

    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new Email(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<Email>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenNormalize(v => v.Trim().ToLowerInvariant())
            .ThenMaxLength(MaxLength)
            .ThenMatches(EmailRegex(), "Invalid email format");

    public static Email CreateFromValidated(string value) => new(value);

    public static implicit operator string(Email email) => email.Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();
}
```

All single-value VOs follow the same 4 components:

| Component | Role |
|---|---|
| `Create(string?)` -> `Fin<T>` | Validates external input to create VO, returns `Fin.Fail` on failure |
| `Validate(string?)` -> `Validation<Error, string>` | Used for applicative composition in the Application Layer |
| `CreateFromValidated(string)` | For ORM restoration, directly creates from already validated value |
| `implicit operator` | Supports VO -> primitive type conversion |

The core of these 4 components is **separation of creation paths.** External input must always go through `Create` for validation, while ORM restoration uses `CreateFromValidated` to skip validation. Both paths are protected by a private constructor, making it impossible to create instances that bypass validation.

### 2. ComparableSimpleValueObject + Arithmetic Operations

`ComparableSimpleValueObject<T>` supports size comparison operations (`>`, `<`, `>=`, `<=`) and encapsulates domain arithmetic operations.

**Money** -- Allows only positive values, provides identity element for summation (Zero):

```csharp
public sealed class Money : ComparableSimpleValueObject<decimal>
{
    /// <summary>
    /// Identity element for addition
    /// </summary>
    public static readonly Money Zero = new(0m);

    private Money(decimal value) : base(value) { }

    public static Fin<Money> Create(decimal value) =>
        CreateFromValidation(Validate(value), v => new Money(v));

    public static Validation<Error, decimal> Validate(decimal value) =>
        ValidationRules<Money>
            .Positive(value);

    public static Money CreateFromValidated(decimal value) => new(value);

    public static implicit operator decimal(Money money) => money.Value;

    public Money Add(Money other) => new(Value + other.Value);
    public Fin<Money> Subtract(Money other) => Create(Value - other.Value);
    public Money Multiply(decimal factor) => new(Value * factor);

    public static Money Sum(IEnumerable<Money> values) =>
        values.Aggregate(Zero, (acc, m) => acc.Add(m));
}
```

`Add` always succeeds (returns `Money`), but `Subtract` may produce a negative result so it returns `Fin<Money>`. `Sum` supports LINQ summation with the `Zero` identity element and `Aggregate`.

**Quantity** -- 0 or more, clamp-style subtraction:

```csharp
public sealed class Quantity : ComparableSimpleValueObject<int>
{
    private Quantity(int value) : base(value) { }

    public static Fin<Quantity> Create(int value) =>
        CreateFromValidation(Validate(value), v => new Quantity(v));

    public static Validation<Error, int> Validate(int value) =>
        ValidationRules<Quantity>
            .NonNegative(value);

    public static Quantity CreateFromValidated(int value) => new(value);

    public static implicit operator int(Quantity quantity) => quantity.Value;

    public Quantity Add(int amount) => new(Value + amount);
    public Quantity Subtract(int amount) => new(Math.Max(0, Value - amount));
}
```

`Subtract` clamps the result to 0 or more with `Math.Max(0, ...)`. Unlike Money, it applies a floor limit instead of an error because when inventory deduction goes negative, the Aggregate (`Inventory`) handles it as an `InsufficientStock` error.

The design difference between Money and Quantity reflects the difference in domain semantics. Money's `Subtract` returns `Fin<Money>` because the result of monetary subtraction can be negative, which is an invalid state in the domain. In contrast, Quantity's `Subtract` clamps to 0, and the actual insufficient stock validation is performed at the Aggregate level.

Having guaranteed single-value validity and arithmetic operations, the next step is to express enumeration values and state transition rules as types.

### 3. Smart Enum -- OrderStatus + Transition Rules

`OrderStatus` is a Smart Enum pattern using `SimpleValueObject<string>`. Allowed transition rules are declared with `HashMap`, and state transitions are controlled via `CanTransitionTo`/`TransitionTo`.

```csharp
public sealed class OrderStatus : SimpleValueObject<string>
{
    #region Error Types

    public sealed record InvalidValue : DomainErrorKind.Custom;

    #endregion

    public static readonly OrderStatus Pending = new("Pending");
    public static readonly OrderStatus Confirmed = new("Confirmed");
    public static readonly OrderStatus Shipped = new("Shipped");
    public static readonly OrderStatus Delivered = new("Delivered");
    public static readonly OrderStatus Cancelled = new("Cancelled");

    private static readonly HashMap<string, OrderStatus> All = HashMap(
        ("Pending", Pending),
        ("Confirmed", Confirmed),
        ("Shipped", Shipped),
        ("Delivered", Delivered),
        ("Cancelled", Cancelled));

    private static readonly HashMap<string, Seq<string>> AllowedTransitions = HashMap(
        ("Pending", Seq("Confirmed", "Cancelled")),
        ("Confirmed", Seq("Shipped", "Cancelled")),
        ("Shipped", Seq("Delivered")));

    private OrderStatus(string value) : base(value) { }

    public static Fin<OrderStatus> Create(string value) =>
        Validate(value).ToFin();

    public static Validation<Error, OrderStatus> Validate(string value) =>
        All.Find(value)
            .ToValidation(DomainError.For<OrderStatus>(
                new InvalidValue(),
                currentValue: value,
                message: $"Invalid order status: '{value}'"));

    public static OrderStatus CreateFromValidated(string value) =>
        All.Find(value)
            .IfNone(() => throw new InvalidOperationException(
                $"Invalid order status for CreateFromValidated: '{value}'"));

    public bool CanTransitionTo(OrderStatus target) =>
        AllowedTransitions.Find(Value)
            .Map(allowed => allowed.Any(v => v == target.Value))
            .IfNone(false);

    public static implicit operator string(OrderStatus status) => status.Value;
}
```

Transition rule summary:

| Current State | Allowed Transition Targets |
|---|---|
| Pending | Confirmed, Cancelled |
| Confirmed | Shipped, Cancelled |
| Shipped | Delivered |
| Delivered | (terminal state) |
| Cancelled | (terminal state) |

States not in `AllowedTransitions` (Delivered, Cancelled) cause `CanTransitionTo` to always return `false`, functioning as terminal states.

Having defined the creation and validation patterns for value objects, we now look at the creation strategy for Aggregate Roots that compose these value objects.

### 4. AggregateRoot Dual Factory + Guards

All Aggregate Roots use the dual factory pattern:

| Factory | Purpose | Validation | Events |
|---|---|---|---|
| `Create(VO...)` | Domain creation | Receives already validated VOs | Publishes domain events |
| `CreateFromValidated(id, VO..., audit...)` | ORM/Repository restoration | None (trusts DB data) | None |

**Customer.Create()** -- Receives VOs and creates + publishes event:

```csharp
public static Customer Create(
    CustomerName name,
    Email email,
    Money creditLimit)
{
    var customer = new Customer(CustomerId.New(), name, email, creditLimit);
    customer.AddDomainEvent(new CreatedEvent(customer.Id, name, email));
    return customer;
}
```

**Customer.CreateFromValidated()** -- For ORM restoration:

```csharp
public static Customer CreateFromValidated(
    CustomerId id,
    CustomerName name,
    Email email,
    Money creditLimit,
    DateTime createdAt,
    Option<DateTime> updatedAt)
{
    return new Customer(id, name, email, creditLimit)
    {
        CreatedAt = createdAt,
        UpdatedAt = updatedAt
    };
}
```

**Product.Update()** -- Soft Delete guard:

A guard pattern that blocks changes to deleted Aggregates. Returns an `AlreadyDeleted` error if `DeletedAt.IsSome`.

```csharp
public Fin<Product> Update(
    ProductName name,
    ProductDescription description,
    Money price)
{
    if (DeletedAt.IsSome)
        return DomainError.For<Product>(
            new AlreadyDeleted(),
            Id.ToString(),
            "Cannot update a deleted product");

    var oldPrice = Price;

    Name = name;
    Description = description;
    Price = price;
    UpdatedAt = DateTime.UtcNow;

    AddDomainEvent(new UpdatedEvent(Id, name, oldPrice, price));

    return this;
}
```

**Product.Delete()** -- Idempotent deletion:

Calling Delete() again on an already deleted state returns `this` without error.

```csharp
public Product Delete(string deletedBy)
{
    if (DeletedAt.IsSome)
        return this;

    DeletedAt = DateTime.UtcNow;
    DeletedBy = deletedBy;
    AddDomainEvent(new DeletedEvent(Id, deletedBy));
    return this;
}
```

### 5. Entity Child Entity (OrderLine)

`OrderLine` is a child entity inheriting from `Entity<OrderLineId>`, existing only within the Order Aggregate boundary.

**OrderLine.Create()** -- Quantity > 0 validation + LineTotal auto-calculation:

```csharp
public static Fin<OrderLine> Create(ProductId productId, Quantity quantity, Money unitPrice)
{
    if ((int)quantity <= 0)
        return DomainError.For<OrderLine, int>(
            new InvalidQuantity(),
            currentValue: quantity,
            message: "Order line quantity must be greater than 0");

    var lineTotal = unitPrice.Multiply(quantity);
    return new OrderLine(OrderLineId.New(), productId, quantity, unitPrice, lineTotal);
}
```

The `Quantity` VO allows 0 or more, but in the order line context, 0 quantity is meaningless, so additional validation is performed at the Entity level. `LineTotal` is auto-computed via `Money.Multiply` to ensure consistency.

**Order.Create()** -- Empty order line blocking + TotalAmount auto-calculation:

```csharp
public static Fin<Order> Create(
    CustomerId customerId,
    IEnumerable<OrderLine> orderLines,
    ShippingAddress shippingAddress)
{
    var lines = orderLines.ToList();
    if (lines.Count == 0)
        return DomainError.For<Order, int>(
            new EmptyOrderLines(),
            currentValue: 0,
            message: "Order must contain at least one order line");

    var totalAmount = Money.CreateFromValidated(lines.Sum(l => (decimal)l.LineTotal));
    var order = new Order(OrderId.New(), customerId, lines, totalAmount, shippingAddress);

    var lineInfos = Seq(lines.Select(l => new OrderLineInfo(l.ProductId, l.Quantity, l.UnitPrice, l.LineTotal)));
    order.AddDomainEvent(new CreatedEvent(order.Id, customerId, lineInfos, totalAmount));
    return order;
}
```

Order manages child entities with a private `List<OrderLine>` and exposes only `IReadOnlyList` externally:

```csharp
private readonly List<OrderLine> _orderLines = [];
public IReadOnlyList<OrderLine> OrderLines => _orderLines.AsReadOnly();
```

Having defined the ownership relationships within the Aggregate, we now look at query patterns for cross-Aggregate validation.

### 6. ExpressionSpecification

`ExpressionSpecification<T>` returns `Expression<Func<T, bool>>`, which EF Core automatically translates to SQL.

**ProductNameUniqueSpec** -- Self-exclusion option:

```csharp
public sealed class ProductNameUniqueSpec : ExpressionSpecification<Product>
{
    public ProductName Name { get; }
    public Option<ProductId> ExcludeId { get; }

    public ProductNameUniqueSpec(ProductName name, Option<ProductId> excludeId = default)
    {
        Name = name;
        ExcludeId = excludeId;
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        string nameStr = Name;
        string? excludeIdStr = ExcludeId.Match<string?>(id => id.ToString(), () => null);
        return product => (string)product.Name == nameStr &&
                          (excludeIdStr == null || product.Id.ToString() != excludeIdStr);
    }
}
```

`Option<ProductId> ExcludeId` supports duplicate checking excluding self during updates. `default` (None) is used during creation, and the current product ID is passed during updates.

**CustomerEmailSpec** -- Email match search:

```csharp
public sealed class CustomerEmailSpec : ExpressionSpecification<Customer>
{
    public Email Email { get; }

    public CustomerEmailSpec(Email email)
    {
        Email = email;
    }

    public override Expression<Func<Customer, bool>> ToExpression()
    {
        string emailStr = Email;
        return customer => (string)customer.Email == emailStr;
    }
}
```

The VO's `implicit operator` is used to convert to string comparison within the Expression Tree. This conversion happens through Expression variable capture (`string emailStr = Email`), which EF Core can translate into a SQL `WHERE` clause.

### 7. IDomainService

`OrderCreditCheckService` implements cross-Aggregate business rules between Customer and Order. As a stateless service, the Application Layer queries the necessary data and passes only the minimum data.

```csharp
public sealed class OrderCreditCheckService : IDomainService
{
    #region Error Types

    public sealed record CreditLimitExceeded : DomainErrorKind.Custom;

    #endregion

    /// <summary>
    /// Validates that the order amount is within the customer's credit limit.
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
    /// Validates that the sum of existing orders and the new order is within the credit limit.
    /// </summary>
    public Fin<Unit> ValidateCreditLimitWithExistingOrders(
        Customer customer,
        Seq<Order> existingOrders,
        Money newOrderAmount)
    {
        var totalExisting = Money.Sum(existingOrders.Map(o => o.TotalAmount));
        var totalWithNew = totalExisting.Add(newOrderAmount);

        if (totalWithNew > customer.CreditLimit)
            return DomainError.For<OrderCreditCheckService>(
                new CreditLimitExceeded(),
                customer.Id.ToString(),
                $"Total order amount {(decimal)totalWithNew} (existing: {(decimal)totalExisting} + new: {(decimal)newOrderAmount}) exceeds customer credit limit {(decimal)customer.CreditLimit}");

        return unit;
    }
}
```

`ValidateCreditLimitWithExistingOrders` calculates the existing order total with `Money.Sum` and adds the new order with `Money.Add`. The `ComparableSimpleValueObject` comparison operator (`>`) is used to determine limit exceedance. The error type `CreditLimitExceeded` is nested within the service to clearly identify the error source.

The core of Domain Service is that it is **stateless pure logic.** The Application Layer queries all necessary data and passes it, and the Domain Service executes only business rules. Thanks to this separation, Domain Service can be unit tested purely without mocks.

### 8. IRepository Port

`IRepository<T, TId>` adds custom methods on top of basic CRUD to support Specification-based queries.

```csharp
public interface ICustomerRepository : IRepository<Customer, CustomerId>
{
    /// <summary>
    /// Specification-based existence check.
    /// </summary>
    FinT<IO, bool> Exists(Specification<Customer> spec);
}
```

`FinT<IO, bool>` expresses IO effect and failure possibility in the type. When the Application Layer passes `CustomerEmailSpec`, the Repository implementation converts it to SQL via EF Core.

### 9. DomainEvent + DomainError Nested Records

Domain events and error types are defined as nested `sealed record` types within the Aggregate. Which Aggregate an event/error originates from becomes clear at the namespace level.

**Customer's domain events:**

```csharp
public sealed class Customer : AggregateRoot<CustomerId>, IAuditable
{
    #region Domain Events

    public sealed record CreatedEvent(
        CustomerId CustomerId,
        CustomerName Name,
        Email Email) : DomainEvent;

    public sealed record CreditLimitUpdatedEvent(
        CustomerId CustomerId,
        Money OldCreditLimit,
        Money NewCreditLimit) : DomainEvent;

    public sealed record EmailChangedEvent(
        CustomerId CustomerId,
        Email OldEmail,
        Email NewEmail) : DomainEvent;

    #endregion
    // ...
}
```

**Order's domain errors:**

```csharp
public sealed class Order : AggregateRoot<OrderId>, IAuditable
{
    #region Error Types

    public sealed record EmptyOrderLines : DomainErrorKind.Custom;
    public sealed record InvalidOrderStatusTransition : DomainErrorKind.Custom;

    #endregion
    // ...
}
```

Events include before/after values (e.g., `OldPrice`, `NewPrice`) so event consumers can understand the changes. Error types inherit from `DomainErrorKind.Custom`, and when creating errors with `DomainError.For<T>()`, the Aggregate type information is automatically included.

## Failable vs Idempotent Return Types

The return type of domain methods is determined by the possibility of failure. `Fin<T>` is used for operations that can fail, and the self type (T) is used for operations that always succeed. The caller can determine whether error handling is needed by looking at the return type alone.

| Method | Return Type | Classification | Reason |
|---|---|---|---|
| `Customer.Create()` | `Customer` | Always succeeds | Receives only already validated VOs |
| `Customer.UpdateCreditLimit()` | `Customer` | Idempotent | Always succeeds, fluent chaining |
| `Customer.ChangeEmail()` | `Customer` | Idempotent | Always succeeds, fluent chaining |
| `Product.Create()` | `Product` | Always succeeds | Receives only already validated VOs |
| `Product.Update()` | `Fin<Product>` | Failable | `AlreadyDeleted` when modifying deleted product |
| `Product.Delete()` | `Product` | Idempotent | Allows re-calling on already deleted state |
| `Product.Restore()` | `Product` | Idempotent | Allows re-calling on already restored state |
| `Product.AssignTag()` | `Product` | Idempotent | Allows re-assigning already assigned tag |
| `Product.UnassignTag()` | `Product` | Idempotent | Allows re-unassigning already unassigned tag |
| `Order.Create()` | `Fin<Order>` | Failable | `EmptyOrderLines` on empty order lines |
| `Order.Confirm()` | `Fin<Unit>` | Failable | `InvalidOrderStatusTransition` on invalid transition |
| `Order.Ship()` | `Fin<Unit>` | Failable | `InvalidOrderStatusTransition` on invalid transition |
| `Order.Deliver()` | `Fin<Unit>` | Failable | `InvalidOrderStatusTransition` on invalid transition |
| `Order.Cancel()` | `Fin<Unit>` | Failable | `InvalidOrderStatusTransition` on invalid transition |
| `OrderLine.Create()` | `Fin<OrderLine>` | Failable | `InvalidQuantity` on quantity 0 or less |
| `Inventory.DeductStock()` | `Fin<Unit>` | Failable | `InsufficientStock` on insufficient stock |
| `Inventory.AddStock()` | `Inventory` | Always succeeds | Stock addition is always valid |
| `Tag.Create()` | `Tag` | Always succeeds | Receives only already validated VOs |
| `Tag.Rename()` | `Tag` | Idempotent | Always succeeds, fluent chaining |

Design principle: **Methods that can fail return `Fin<T>`,** while methods that always succeed or are idempotent return the self type. The caller can determine whether error handling is needed by looking at the return type alone.

## Naive to Final Type Tracking Table

| Naive Field | Single Value VO | Owning Aggregate/Entity | Final Location |
|---|---|---|---|
| `string Name` (customer) | CustomerName | Customer | `Customer.Name` |
| `string Email` (customer) | Email | Customer | `Customer.Email` |
| `decimal CreditLimit` | Money | Customer | `Customer.CreditLimit` |
| `string Name` (product) | ProductName | Product | `Product.Name` |
| `string Description` | ProductDescription | Product | `Product.Description` |
| `decimal Price` | Money | Product | `Product.Price` |
| `string Name` (tag) | TagName | Tag | `Tag.Name` |
| `List<string> TagIds` | `List<TagId>` | Product | `Product.TagIds` |
| `string Status` | OrderStatus (Smart Enum) | Order | `Order.Status` |
| `string ShippingAddress` | ShippingAddress (complex VO) | Order | `Order.ShippingAddress` |
| `decimal TotalAmount` | Money (auto-calculated) | Order | `Order.TotalAmount` |
| `string ProductId` (order line) | ProductId (cross-reference) | OrderLine | `OrderLine.ProductId` |
| `int Quantity` | Quantity | OrderLine, Inventory | `OrderLine.Quantity`, `Inventory.StockQuantity` |
| `decimal UnitPrice` | Money | OrderLine | `OrderLine.UnitPrice` |
| `decimal LineTotal` | Money (auto-calculated) | OrderLine | `OrderLine.LineTotal` |
| `int StockQuantity` | Quantity | Inventory | `Inventory.StockQuantity` |
| `string CustomerId` (order) | CustomerId (cross-reference) | Order | `Order.CustomerId` |
| `string ProductId` (inventory) | ProductId (cross-reference) | Inventory | `Inventory.ProductId` |

This table tracks how primitive types were transformed into types with domain meaning. As `string` differentiated into `CustomerName`, `Email`, `ProductName`, etc., the compiler prevents confusing different strings. `decimal` is wrapped in `Money` and `int` in `Quantity`, making it impossible for negative amounts or invalid quantities to exist.

All primitive types (`string`, `decimal`, `int`) have been transformed into Value Objects with domain meaning. Cross-Aggregate references are expressed as ID value objects (`CustomerId`, `ProductId`, `TagId`) to maintain Aggregate boundaries. Auto-calculated fields (`TotalAmount`, `LineTotal`) are computed using VO arithmetic operations at creation time to guarantee consistency.

The [implementation results](../03-implementation-results/) verify how this type structure guarantees business scenarios.
