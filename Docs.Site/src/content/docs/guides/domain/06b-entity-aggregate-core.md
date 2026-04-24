---
title: "Entity and Aggregate Implementation — Core Patterns"
---

This document covers the core methods for implementing Entities and Aggregates with the Functorium framework. For design principles and concepts, see [06a-aggregate-design.md](../06a-aggregate-design). For advanced patterns (Cross-Aggregate relationships, supplementary interfaces, practical examples), see [06c-entity-aggregate-advanced.md](../06c-entity-aggregate-advanced).

## Introduction

"Which base class should be used to implement the Aggregate Root and child Entities?"
"Who is responsible for validation during Entity creation, and how is ORM restoration distinguished?"
"How is a business rule violation expressed in a method signature?"

Entity and Aggregate implementation is the core of domain modeling. This document covers patterns needed for actual implementation, from the base class hierarchy provided by the Functorium framework to creation patterns, command methods, and child Entity management.

### What You Will Learn

Through this document, you will learn:

1. **Entity\<TId\> and AggregateRoot\<TId\> class hierarchy** — Features and roles provided by base classes
2. **Ulid-based Entity ID system** — Automatic generation of type-safe identifiers via source generators
3. **Create / CreateFromValidated creation patterns** — Separation of new Entity creation and ORM restoration
4. **Command methods and invariant protection** — Expressing business rule violations as types via `Fin<T>` return
5. **Child Entity implementation and event publishing** — Child management through Aggregate Root and domain events

### Prerequisites

A basic understanding of the following concepts is required to understand this document:

- [Aggregate Design Principles](../06a-aggregate-design) — Aggregate boundaries and design principles (WHY)
- [Value Object Implementation Guide](../05a-value-objects) — Value Object implementation patterns
- [Error System: Basics and Naming](../08a-error-system) — `Fin<T>` and error return patterns

> The core of Entity and Aggregate implementation is **separation of validation responsibilities.** Value Objects guarantee the validity of primitive values, and Entities receive already-validated Value Objects and compose them. Business rule violations are made explicit in the type system via `Fin<T>` returns, forcing callers to handle failures.

## Summary

### Key Commands

```csharp
// Entity ID creation (Ulid-based)
var productId = ProductId.New();

// Aggregate Root creation (receives validated VOs directly)
var product = Product.Create(name, description, price, stockQuantity);

// Factory for ORM restoration
var product = Product.CreateFromValidated(id, name, ..., createdAt, updatedAt);

// Command method (invariant protection, Fin<T> return)
Fin<Unit> result = order.Confirm(updatedBy);

// Domain event publishing
AddDomainEvent(new CreatedEvent(Id, customerId, totalAmount));
```

### Key Procedures

1. Apply `[GenerateEntityId]` attribute to generate EntityId source
2. Inherit from `AggregateRoot<TId>` (or `Entity<TId>`)
3. Implement `Create()` factory method - Receive validated VOs to create Entity + publish domain events
4. Implement `CreateFromValidated()` method - For ORM restoration (no validation)
5. Implement command methods - Check invariants then return `Fin<T>`
6. Define domain events as nested records and publish on state changes

### Key Concepts

| Concept | Description |
|------|------|
| Entity vs AggregateRoot | Entity has ID-based equality, AggregateRoot has transaction boundary + event publishing |
| Create / CreateFromValidated | Create is for new Entity creation (validated), CreateFromValidated is for DB restoration (no validation) |
| Command methods | Returns `Fin.Fail` on invariant violation, performs state change + event publishing on success |
| Ulid-based ID | Distributed generation, time-ordered, excellent index performance |

---

## Class Hierarchy

### Class Hierarchy

Functorium provides a base class hierarchy for Entity implementation.

```
IEntity<TId> (interface)
+-- TId Id
`-- ArchTestContract (nested static class)
    +-- CreateMethodName constant
    `-- CreateFromValidatedMethodName constant
    |
    `-- Entity<TId> (abstract class)
        +-- Id property (protected init)
        +-- Equals() / GetHashCode() - ID-based equality
        +-- == / != operators
        +-- CreateFromValidation<TEntity, TValue>() helper
        `-- GetUnproxiedType() - ORM proxy support
            |
            `-- AggregateRoot<TId> : IDomainEventDrain
                +-- DomainEvents (read-only, IHasDomainEvents)
                +-- AddDomainEvent() (protected)
                `-- ClearDomainEvents() (IDomainEventDrain)
```

The following summarizes the roles of each layer.

- **IEntity\<TId\>**: Interface defining the Entity contract. Includes method name constants for `Create` and `CreateFromValidated`.
- **Entity\<TId\>**: Automatically implements ID-based equality (`Equals`, `GetHashCode`, `==`, `!=`). Also handles ORM proxy types.
- **AggregateRoot\<TId\>**: Provides domain event management. Implements `IDomainEventDrain` (internal), separating event querying (`IHasDomainEvents`) from clearing (`IDomainEventDrain`).

### Entity\<TId\>

Abstract base class for Entities that provides ID-based equality.

**Location**: `Functorium.Domains.Entities.Entity<TId>`

```csharp
[Serializable]
public abstract class Entity<TId> : IEntity<TId>, IEquatable<Entity<TId>>
    where TId : struct, IEntityId<TId>
{
    // Unique identifier for the Entity
    public TId Id { get; protected init; }

    // Default constructor (for ORM/serialization)
    protected Entity();

    // Create Entity with specified ID
    protected Entity(TId id);

    // ID-based equality comparison
    public override bool Equals(object? obj);
    public bool Equals(Entity<TId>? other);
    public override int GetHashCode();

    // Equality operators
    public static bool operator ==(Entity<TId>? a, Entity<TId>? b);
    public static bool operator !=(Entity<TId>? a, Entity<TId>? b);

    // Factory helper method
    public static Fin<TEntity> CreateFromValidation<TEntity, TValue>(
        Validation<Error, TValue> validation,
        Func<TValue, TEntity> factory)
        where TEntity : Entity<TId>;
}
```

The items that must be included when implementing an Entity are as follows.

| Item | Description |
|------|------|
| `[GenerateEntityId]` attribute | Auto-generates EntityId |
| Private constructor (for ORM) | Parameterless default constructor + `#pragma warning disable CS8618` |
| Private constructor (internal) | Constructor that receives ID |
| `Create()` | Entity creation factory method |
| `CreateFromValidated()` | ORM restoration method |

> Entity implementation examples can be found in the [Creation Patterns](#creation-patterns) section.

### AggregateRoot\<TId\>

Abstract base class for Aggregate Root that provides domain event management.

**Location**: `Functorium.Domains.Entities.AggregateRoot<TId>`

```csharp
public abstract class AggregateRoot<TId> : Entity<TId>, IDomainEventDrain
    where TId : struct, IEntityId<TId>
{
    // Domain events list (read-only, IHasDomainEvents)
    public IReadOnlyList<IDomainEvent> DomainEvents { get; }

    // Default constructor (for ORM/serialization)
    protected AggregateRoot();

    // Create Aggregate Root with specified ID
    protected AggregateRoot(TId id);

    // Add domain event
    protected void AddDomainEvent(IDomainEvent domainEvent);

    // Clear all domain events (IDomainEventDrain)
    public void ClearDomainEvents();
}
```

**Interface Segregation Principle:**
- `IHasDomainEvents`: Read-only contract for the domain layer (allows only event querying)
- `IDomainEventDrain` (internal): Infrastructure interface for cleanup after event publishing
- Domain events are immutable facts, so the domain contract does not provide individual deletion methods

The key point in the following example is the structure where `AddDomainEvent()` publishes events and command methods return `Fin<Unit>` to express invariant violations.

```csharp
[GenerateEntityId]
public class Order : AggregateRoot<OrderId>
{
    #region Error Types

    // State transition violation error type
    public sealed record InvalidOrderStatusTransition : DomainErrorType.Custom;

    #endregion

    public Money TotalAmount { get; private set; }
    // OrderStatus: Smart Enum based on SimpleValueObject<string> (details: section 6c practical example)
    public OrderStatus Status { get; private set; }

#pragma warning disable CS8618
    private Order() { }
#pragma warning restore CS8618

    private Order(OrderId id, Money totalAmount) : base(id)
    {
        TotalAmount = totalAmount;
        Status = OrderStatus.Pending;
    }

    // Create: Receives already validated Value Objects directly
    public static Order Create(Money totalAmount)
    {
        var id = OrderId.New();
        var order = new Order(id, totalAmount);
        order.AddDomainEvent(new OrderCreatedEvent(id, totalAmount));
        return order;
    }

    // State transition — delegates to TransitionTo() to centralize transition rules
    public Fin<Unit> Confirm() => TransitionTo(OrderStatus.Confirmed, new OrderConfirmedEvent(Id));

    private Fin<Unit> TransitionTo(OrderStatus target, DomainEvent domainEvent)
    {
        if (!Status.CanTransitionTo(target))
            return DomainError.For<Order, string, string>(
                new InvalidOrderStatusTransition(),
                value1: Status,
                value2: target,
                message: $"Cannot transition from '{Status}' to '{target}'");

        Status = target;
        AddDomainEvent(domainEvent);
        return unit;
    }
}
```

### Supplementary Interface Summary

These are supplementary interfaces mixed into Aggregates/Entities. For detailed implementation and usage examples, see [06c-entity-aggregate-advanced.md](../06c-entity-aggregate-advanced).

| Interface | Properties | Purpose |
|-----------|------|------|
| `IAuditable` | `DateTime CreatedAt`, `Option<DateTime> UpdatedAt` | Creation/modification time tracking |
| `IAuditableWithUser` | + `Option<string> CreatedBy/UpdatedBy` | + User tracking |
| `ISoftDeletable` | `Option<DateTime> DeletedAt`, `bool IsDeleted` | Soft delete |
| `ISoftDeletableWithUser` | + `Option<string> DeletedBy` | + Deleter tracking |
| `IConcurrencyAware` | `byte[] RowVersion` | Optimistic concurrency control |

Now that we understand the class hierarchy, let us look at the ID system that uniquely identifies Entities.

---

## Entity ID System

Functorium provides a type-safe Entity ID system. It is Ulid-based, enabling time-order sorting, and is automatically generated via source generators.

### IEntityId\<T\> Interface

**Location**: `Functorium.Domains.Entities.IEntityId<T>`

```csharp
public interface IEntityId<T> : IEquatable<T>, IComparable<T>, IParsable<T>
    where T : struct, IEntityId<T>
{
    // Ulid value
    Ulid Value { get; }

    // Create new EntityId
    static abstract T New();

    // Create EntityId from Ulid
    static abstract T Create(Ulid id);

    // Create EntityId from string
    static abstract T Create(string id);
}
```

**Why Ulid?**

The following comparison shows why Functorium chose Ulid over GUID.

| Characteristics | GUID | Ulid |
|------|------|------|
| Size | 128bit | 128bit |
| Sorting | Random | Time-ordered |
| Readability | 36 chars (with hyphens) | 26 chars |
| Index performance | Low (random) | High (sequential) |

The key difference is sorting and index performance. Ulid is sorted in time order, resulting in good database index performance and the ability to extract creation time.

### EntityIdGenerator (Source Generator)

When the `[GenerateEntityId]` attribute is applied to an Entity class, the ID type for that Entity is automatically generated.

**Location**: `Functorium.Domains.Entities.GenerateEntityIdAttribute`

```csharp
using Functorium.Domains.Entities;

[GenerateEntityId]  // Auto-generates ProductId, ProductIdComparer, ProductIdConverter
public class Product : Entity<ProductId>
{
    // ...
}
```

### Generated Code

`[GenerateEntityId]` automatically generates the following types. It includes not only the ID itself but also auxiliary types needed for EF Core integration and serialization.

| Generated Type | Purpose |
|----------|------|
| `{Entity}Id` struct | Entity identifier (Ulid-based) |
| `{Entity}IdComparer` | EF Core ValueComparer |
| `{Entity}IdConverter` | EF Core ValueConverter (string ↔ EntityId) |
| `{Entity}IdJsonConverter` | System.Text.Json serialization (built-in) |
| `{Entity}IdTypeConverter` | TypeConverter support (built-in) |

**Generated EntityId Structure:**

```csharp
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(ProductIdJsonConverter))]
[TypeConverter(typeof(ProductIdTypeConverter))]
public readonly partial record struct ProductId :
    IEntityId<ProductId>,
    IParsable<ProductId>
{
    // Type name constant
    public const string Name = "ProductId";

    // Empty value constant
    public static readonly ProductId Empty = new(Ulid.Empty);

    // Ulid value
    public Ulid Value { get; init; }

    // Factory methods
    public static ProductId New();              // New ID generation
    public static ProductId Create(Ulid id);    // Create from Ulid
    public static ProductId Create(string id);  // Create from string

    // Comparison operators
    public int CompareTo(ProductId other);
    public static bool operator <(ProductId left, ProductId right);
    public static bool operator >(ProductId left, ProductId right);
    public static bool operator <=(ProductId left, ProductId right);
    public static bool operator >=(ProductId left, ProductId right);

    // IParsable implementation
    public static ProductId Parse(string s, IFormatProvider? provider);
    public static bool TryParse(string? s, IFormatProvider? provider, out ProductId result);

    // Built-in JsonConverter, TypeConverter
    // ...
}
```

While the ID system provides the means to identify Entities, creation patterns define how to safely create them.

---

## Creation Patterns

The core of Entity implementation is **separation of validation responsibilities.** Value Objects and Entities have different validation responsibilities.

- **Value Object**: Receives primitive values and validates its own validity
- **Entity**: Receives already-validated Value Objects and composes them. Defines Validate only when there are Entity-level business rules

### Role Differences Between Value Object and Entity

| Category | Value Object | Entity |
|------|--------------|--------|
| **Validate** | Primitive value -> returns validated value | Entity-level business rules only |
| **Create** | Receives primitive values | **Receives Value Objects directly** |
| **Validation responsibility** | Validates own values | Validates relationships/rules between VOs |

> **Note**: For Value Object validation patterns, see the [Value Object Implementation Guide - Implementation Patterns](../05a-value-objects#implementation-patterns).

### Create / CreateFromValidated Pattern

Entities provide two creation paths. Check the purpose and behavioral differences of each path.

| Method | Purpose | Validation | ID Generation |
|--------|------|------|---------|
| `Create()` | New Entity creation | VOs already validated | Newly generated |
| `CreateFromValidated()` | ORM/Repository restoration | None | Uses existing ID |

**Create Method:**

Used when creating a new Entity. **Receives already validated Value Objects directly.**

```csharp
// Create: Receives already validated Value Objects directly
public static Product Create(ProductName name, ProductDescription description, Money price)
{
    var id = ProductId.New();  // New ID generation
    var product = new Product(id, name, description, price);
    product.AddDomainEvent(new CreatedEvent(product.Id, name, price));
    return product;
}
```

**CreateFromValidated Method:**

Used when restoring an Entity from ORM or Repository. Values read from the database are already validated, so they are not validated again.

```csharp
public static Product CreateFromValidated(
    ProductId id,
    ProductName name,
    ProductDescription description,
    Money price,
    DateTime createdAt,
    Option<DateTime> updatedAt)
{
    return new Product(id, name, description, price)
    {
        CreatedAt = createdAt,
        UpdatedAt = updatedAt
    };
}
```

**Why are two methods needed?**

1. **Performance**: Improves performance by skipping validation when loading large numbers of Entities from the database.
2. **Semantics**: Creating a new Entity and restoring an existing Entity have different meanings.
3. **ID management**: Create generates a new ID, while CreateFromValidated uses an existing ID.

### Pattern 1: Static Create() Factory Method

Aggregate Root is created via a **`Create` static factory method.** The constructor is encapsulated as `private`. It receives already-validated Value Objects, creates a new Aggregate, auto-generates an ID, and publishes domain events.

```csharp
// Customer Aggregate: Simple creation
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

```csharp
// Product Aggregate: Creation + initial state setup
public static Product Create(
    ProductName name,
    ProductDescription description,
    Money price)
{
    var product = new Product(ProductId.New(), name, description, price);
    product.AddDomainEvent(new CreatedEvent(product.Id, name, price));
    return product;
}
```

**Create() Comparison Across All Aggregate Roots:**

| Aggregate | Parameters | ID Generation | Event |
|-----------|---------|---------|--------|
| `Product.Create()` | `ProductName, ProductDescription, Money` | `ProductId.New()` | `CreatedEvent` |
| `Inventory.Create()` | `ProductId, Quantity` | `InventoryId.New()` | `CreatedEvent` |
| `Order.Create()` | `ProductId, Quantity, Money, ShippingAddress` | `OrderId.New()` | `CreatedEvent` |
| `Customer.Create()` | `CustomerName, Email, Money` | `CustomerId.New()` | `CreatedEvent` |

**Common Rules:**
- `private` constructor + `public static Create()` combination
- Parameters are **already-validated Value Objects** (not primitives)
- ID is auto-generated internally via `XxxId.New()`
- Domain events are published at creation time

### Pattern 2: CreateFromValidated() ORM Restoration

Restores the Aggregate from data read from the DB. Validation is skipped since the data has already passed validation once.

```csharp
// Product.cs
public static Product CreateFromValidated(
    ProductId id,
    ProductName name,
    ProductDescription description,
    Money price,
    DateTime createdAt,
    Option<DateTime> updatedAt)
{
    return new Product(id, name, description, price)
    {
        CreatedAt = createdAt,
        UpdatedAt = updatedAt
    };
}
```

**Create vs CreateFromValidated Comparison:**

| Item | `Create()` | `CreateFromValidated()` |
|------|-----------|------------------------|
| Purpose | New Aggregate creation | ORM/Repository restoration |
| ID generation | `XxxId.New()` auto-issued | Passed from outside |
| Validation | VOs are already validated | Validation skipped (trusts DB data) |
| Event publishing | Calls `AddDomainEvent()` | No events |
| Audit fields | Auto-set (`DateTime.UtcNow`) | Passed from outside |

### When Entity.Validate Is Needed vs Not Needed

**Not needed** -- Simple VO composition:
```csharp
// Value Objects are already validated -> Entity.Validate not needed
public static Order Create(Money amount, CustomerId customerId)
{
    var id = OrderId.New();
    return new Order(id, amount, customerId);
}
```

**Needed** -- Entity-level business rules (relationships between VOs):

The key point in the following example is the flow where `Validate` returns `Validation<Error, Unit>` and `Create` calls it then converts with `ToFin()`.

```csharp
// Selling price > cost rule is Entity-level validation
[GenerateEntityId]
public class Product : Entity<ProductId>
{
    #region Error Types

    public sealed record SellingPriceBelowCost : DomainErrorType.Custom;

    #endregion

    public ProductName Name { get; private set; }
    public Price SellingPrice { get; private set; }
    public Money Cost { get; private set; }

    // Validate: Entity-level business rule (selling price > cost)
    public static Validation<Error, Unit> Validate(Price sellingPrice, Money cost) =>
        sellingPrice.Value > cost.Amount
            ? Success<Error, Unit>(unit)
            : DomainError.For<Product>(
                new SellingPriceBelowCost(),
                sellingPrice.Value,
                $"Selling price must be greater than cost. Price: {sellingPrice.Value}, Cost: {cost.Amount}");

    // Create: Create Entity after calling Validate
    public static Fin<Product> Create(ProductName name, Price sellingPrice, Money cost) =>
        Validate(sellingPrice, cost)
            .Map(_ => new Product(ProductId.New(), name, sellingPrice, cost))
            .ToFin();
}
```

```csharp
// Start date < end date rule is Entity-level
[GenerateEntityId]
public class Subscription : Entity<SubscriptionId>
{
    #region Error Types

    public sealed record StartAfterEnd : DomainErrorType.Custom;

    #endregion

    public Date StartDate { get; private set; }
    public Date EndDate { get; private set; }
    public CustomerId CustomerId { get; private set; }

    // Validate: Entity-level business rule (start date < end date)
    public static Validation<Error, Unit> Validate(Date startDate, Date endDate) =>
        startDate < endDate
            ? Success<Error, Unit>(unit)
            : DomainError.For<Subscription>(
                new StartAfterEnd(),
                startDate.Value,
                $"Start date must be before end date. Start: {startDate.Value}, End: {endDate.Value}");

    // Create: Create Entity after calling Validate
    public static Fin<Subscription> Create(Date startDate, Date endDate, CustomerId customerId) =>
        Validate(startDate, endDate)
            .Map(_ => new Subscription(SubscriptionId.New(), startDate, endDate, customerId))
            .ToFin();
}
```

### Factory Pattern Design Guidelines

| Scenario | Recommended Approach | Example |
|---------|---------|------|
| Simple creation (only VOs needed) | Call static `Create()` directly | `Customer.Create(name, email, creditLimit)` |
| Parallel VO validation needed | Apply pattern (inside Usecase) | `CreateProductCommand.CreateProduct()` |
| External data needed | Orchestrate via Port in Usecase then call `Create()` | `CreateOrderCommand` + `IProductCatalog` |
| Restore from DB | `CreateFromValidated()` (validation skipped) | Repository Mapper |

> **Apply Pattern**: In the Usecase, validate VOs in parallel using `(v1, v2, v3).Apply(...)` tuples then call `Create()`. For details, see [Usecase Implementation Guide -- Value Object Validation and Apply Merge Pattern](../application/11-usecases-and-cqrs).
>
> **Cross-Aggregate Orchestration**: When data from another Aggregate is needed, query via Port in the Usecase's LINQ chain then call `Create()`. For details, see [Usecase Implementation Guide](../application/11-usecases-and-cqrs).

**DDD Principle Compliance:**
- **Encapsulation**: Block direct instantiation with `private` constructor, expose only factory methods
- **Invariant protection**: `Create()` accepts only validated VOs, direct primitive passing not possible
- **Reconstruction separation**: Clear distinction between `Create()` (new creation) vs `CreateFromValidated()` (restoration)
- **Event consistency**: Domain event publishing only on new creation, no events on restoration
- **Layer responsibility**: Aggregate handles only its own creation, external orchestration is Usecase's responsibility

Having covered how to create Entities, let us now examine command methods that safely change the state of created Entities.

---

## Command Methods and Invariant Protection

### Command Methods That Protect Invariants

State changes are only possible through Aggregate Root methods. When business rules are violated, failure is returned as `Fin<Unit>`.

The key point in the following code is the pattern of returning `DomainError` on invariant violation failure and performing state change and event publishing on success.

```csharp
// Inventory: Stock deduction (invariant: stock >= 0)
// Error type definition: public sealed record InsufficientStock : DomainErrorType.Custom;
public Fin<Unit> DeductStock(Quantity quantity)
{
    if (quantity > StockQuantity)
        return DomainError.For<Inventory, int>(
            new InsufficientStock(),
            currentValue: StockQuantity,
            message: $"Insufficient stock. Current: {StockQuantity}, Requested: {quantity}");

    StockQuantity = StockQuantity.Subtract(quantity);
    UpdatedAt = DateTime.UtcNow;
    AddDomainEvent(new StockDeductedEvent(Id, ProductId, quantity));
    return unit;
}
```

```csharp
// Product: Info update (command that always succeeds)
public Product Update(
    ProductName name,
    ProductDescription description,
    Money price)
{
    var oldPrice = Price;

    Name = name;
    Description = description;
    Price = price;
    UpdatedAt = DateTime.UtcNow;

    AddDomainEvent(new UpdatedEvent(Id, name, oldPrice, price));

    return this;
}
```

### Child Entity Management (Add/Remove)

Child Entity collections are encapsulated with the `private List<T>` + `public IReadOnlyList<T>` pattern.

```csharp
public sealed class Product : AggregateRoot<ProductId>
{
    // private mutable collection
    private readonly List<Tag> _tags = [];

    // public read-only view
    public IReadOnlyList<Tag> Tags => _tags.AsReadOnly();

    // Add child Entity only through Root
    public Product AddTag(Tag tag)
    {
        if (_tags.Any(t => t.Id == tag.Id))
            return this;

        _tags.Add(tag);
        AddDomainEvent(new TagAssignedEvent(tag.Id, tag.Name));
        return this;
    }

    // Remove child Entity only through Root
    public Product RemoveTag(TagId tagId)
    {
        var tag = _tags.FirstOrDefault(t => t.Id == tagId);
        if (tag is null)
            return this;

        _tags.Remove(tag);
        AddDomainEvent(new TagRemovedEvent(tagId));
        return this;
    }
}
```

### Query Methods (State Inspection)

Methods that inspect the state of an Entity. They have no side effects and do not change state.

```csharp
// Check if product is expired
public bool IsExpired() => ExpirationDate < DateTime.UtcNow;

// Check if order is in a shippable state
public bool IsShippable() => Status == OrderStatus.Confirmed;
```

### Return Types by Method Type

Choose the appropriate return type based on the nature of the method.

| Method Type | Return Type | Description |
|------------|----------|------|
| Query (simple check) | `bool`, `int`, etc. | Side-effect-free state check |
| Query (VO calculation) | `Money`, `Quantity`, etc. | Returns calculated value object |
| Command (always succeeds) | `void` or `this` | State change without validation |
| Command (can fail) | `Fin<Unit>` | Possible business rule violation |
| Command (returns result) | `Fin<T>` | Can fail + returns calculated result |

An Aggregate Root's command methods only change its own state. So how are child Entities inside the Aggregate managed?

---

## Child Entity Implementation Patterns

### Access Only Through the Aggregate Root

Child Entities do not have independent Repositories and must be created/modified/deleted through the Aggregate Root.

```csharp
// Tag: Child Entity (SharedModels)
[GenerateEntityId]
public sealed class Tag : Entity<TagId>
{
    public TagName Name { get; private set; }

#pragma warning disable CS8618
    private Tag() { }
#pragma warning restore CS8618

    private Tag(TagId id, TagName name) : base(id)
    {
        Name = name;
    }

    public static Tag Create(TagName name) =>
        new(TagId.New(), name);

    public static Tag CreateFromValidated(TagId id, TagName name) =>
        new(id, name);
}
```

### Child Entity Requiring Validation (OrderLine Example)

When a child Entity has domain invariants, `Create()` returns `Fin<T>`:

```csharp
// OrderLine: Child Entity of Order Aggregate
[GenerateEntityId]
public sealed class OrderLine : Entity<OrderLineId>
{
    public sealed record InvalidQuantity : DomainErrorType.Custom;

    public ProductId ProductId { get; private set; }
    public Quantity Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money LineTotal { get; private set; }

    private OrderLine(OrderLineId id, ProductId productId, Quantity quantity, Money unitPrice, Money lineTotal)
        : base(id)
    {
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        LineTotal = lineTotal;
    }

    // Create: Validate invariant quantity > 0, auto-calculate LineTotal
    public static Fin<OrderLine> Create(ProductId productId, Quantity quantity, Money unitPrice)
    {
        if ((int)quantity <= 0)
            return DomainError.For<OrderLine, int>(
                new InvalidQuantity(), currentValue: quantity,
                message: "Order line quantity must be greater than 0");

        var lineTotal = unitPrice.Multiply(quantity);
        return new OrderLine(OrderLineId.New(), productId, quantity, unitPrice, lineTotal);
    }

    // CreateFromValidated: For ORM/Repository restoration (no validation)
    public static OrderLine CreateFromValidated(
        OrderLineId id, ProductId productId, Quantity quantity, Money unitPrice, Money lineTotal)
        => new(id, productId, quantity, unitPrice, lineTotal);
}
```

> **Note**: For production code, see `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/AggregateRoots/Orders/OrderLine.cs`.

### Having Its Own Identifier

Unlike Value Objects, child Entities have **unique identifiers.** This allows identifying specific elements within a collection.

```csharp
// Find and remove a specific Tag by TagId from the Aggregate Root
public Product RemoveTag(TagId tagId)
{
    var tag = _tags.FirstOrDefault(t => t.Id == tagId);
    if (tag is null)
        return this;

    _tags.Remove(tag);
    AddDomainEvent(new TagRemovedEvent(tagId));
    return this;
}
```

### Event Publishing from Child Entities

Child Entities do not directly publish domain events. Instead, **the Aggregate Root publishes events for child Entity changes.**

```csharp
// Aggregate Root (Product) publishes Tag-related events
public Product AddTag(Tag tag)
{
    _tags.Add(tag);
    AddDomainEvent(new TagAssignedEvent(tag.Id, tag.Name));  // Root publishes
    return this;
}

// Child Entity (Tag) directly publishing events
// Tag inherits Entity<TId> so it cannot use AddDomainEvent()
```

---

## Domain Events

Domain events represent significant occurrences in the domain. They can only be published from AggregateRoot.

> **Note**: For the complete design of domain events (`IDomainEvent`/`DomainEvent` definition, Pub/Sub, handler subscription/registration, transaction considerations), see the [Domain Events Guide](../07-domain-events).

### Event Definition Location

Domain events are defined as **nested classes** within the corresponding Entity:

```csharp
[GenerateEntityId]
public class Order : AggregateRoot<OrderId>
{
    #region Domain Events

    // Domain event (nested class)
    public sealed record CreatedEvent(OrderId OrderId, CustomerId CustomerId, Money TotalAmount) : DomainEvent;
    public sealed record ConfirmedEvent(OrderId OrderId) : DomainEvent;
    public sealed record CancelledEvent(OrderId OrderId, string Reason) : DomainEvent;

    #endregion

    // Entity implementation...
}
```

**Advantages**:
- Event ownership is explicit in the type system (`Order.CreatedEvent`)
- IntelliSense shows all related events when typing `Order.`
- Eliminates Entity name duplication (`OrderCreatedEvent` -> `Order.CreatedEvent`)
- **Event publishing origin is explicit in Handler**: When a Handler inherits `IDomainEventHandler<Product.CreatedEvent>`, reading the code alone immediately reveals "this is an event published by the Product Entity"

**Usage Examples**:
```csharp
// Inside Entity (concise)
AddDomainEvent(new CreatedEvent(Id, customerId, totalAmount));

// From outside (explicit)
public void Handle(Order.CreatedEvent @event) { ... }
```

### Event Publishing Pattern

Events are collected using `AddDomainEvent()` within AggregateRoot. They are published when a business-significant state change occurs.

```csharp
[GenerateEntityId]
public class Order : AggregateRoot<OrderId>
{
    #region Error Types

    public sealed record InvalidStatus : DomainErrorType.Custom;

    #endregion

    #region Domain Events

    public sealed record CreatedEvent(OrderId OrderId, Money TotalAmount) : DomainEvent;
    public sealed record ShippedEvent(OrderId OrderId, Address ShippingAddress) : DomainEvent;

    #endregion

    // Create: Publish creation event
    public static Order Create(Money totalAmount)
    {
        var id = OrderId.New();
        var order = new Order(id, totalAmount);
        order.AddDomainEvent(new CreatedEvent(id, totalAmount));
        return order;
    }

    // Ship: Publish event on state change
    public Fin<Unit> Ship(Address address)
    {
        if (Status != OrderStatus.Confirmed)
            return DomainError.For<Order>(
                new InvalidStatus(),
                Status.ToString(),
                "Order must be confirmed before shipping");

        Status = OrderStatus.Shipped;
        AddDomainEvent(new ShippedEvent(Id, address));
        return unit;
    }
}
```

---

## Checklist

### Functorium Implementation Checklist

- [ ] Aggregate Root inherits `AggregateRoot<TId>`
- [ ] Child Entity inherits `Entity<TId>`
- [ ] `[GenerateEntityId]` attribute applied
- [ ] Child Entity collections: `private List<T>` + `public IReadOnlyList<T>`
- [ ] Return `Fin<Unit>` on business rule violation
- [ ] Call `AddDomainEvent()` on state change
- [ ] Default constructor for ORM + `#pragma warning disable CS8618`
- [ ] `Create()` factory method (new Entity creation)
- [ ] `CreateFromValidated()` method (for ORM restoration)
- [ ] Define `Validate()` method when there are Entity-level business rules
- [ ] Domain events defined as nested records (`Order.CreatedEvent`)

---

## Troubleshooting

### EntityId type is not generated after applying `[GenerateEntityId]`
**Cause:** The Source Generator may not have run at build time, or the IDE cache may be stale.
**Solution:** Run a full build with `dotnet build`. If the IDE does not recognize it, close and reopen the solution, or run `dotnet clean` then build.

### Warning occurs due to missing `#pragma warning disable CS8618` during ORM restoration
**Cause:** ORMs like EF Core require a parameterless private constructor, and non-nullable properties are not initialized in this constructor, causing CS8618 warnings.
**Solution:** Apply `#pragma warning disable CS8618` / `#pragma warning restore CS8618` to the ORM default constructor. This is a conventional pattern for ORM proxy creation.

---

## FAQ

### Q1. What are the criteria for choosing between Entity and AggregateRoot?

**AggregateRoot is a "transaction boundary."**

Aggregate Root:
- Is the only Entity that can be accessed directly from outside.
- Defines the consistency boundary of transactions.
- Can publish domain events.

```csharp
// Order is AggregateRoot - accessed directly from outside
[GenerateEntityId]
public class Order : AggregateRoot<OrderId> { }

// OrderItem is Entity - accessed only through Order
[GenerateEntityId]
public class OrderItem : Entity<OrderItemId> { }
```

| Question | Yes | No |
|------|-----|--------|
| Accessed directly from outside? | AggregateRoot | Entity |
| Publishes domain events? | AggregateRoot | Entity |
| Independently stored/queried? | AggregateRoot | Entity |

### Q2. Why use Ulid?

**Ulid provides the advantages of GUID + time ordering.**

| Characteristics | GUID | Auto-increment | Ulid |
|------|------|----------------|------|
| Distributed generation | O | X | O |
| Time ordering | X | O | O |
| Index performance | Low | High | High |
| Predictability | Low | High | Low |

```csharp
var id1 = ProductId.New();  // 01ARZ3NDEKTSV4RRFFQ69G5FAV
var id2 = ProductId.New();  // 01ARZ3NDEKTSV4RRFFQ69G5FAW

// Ulid guarantees time ordering
id1 < id2  // true
```

### Q3. When should CreateFromValidated be used?

**It is used when restoring an Entity from the database.**

| Situation | Method to Use | Reason |
|------|------------|------|
| New Entity creation | `Create()` | Input validation required |
| Restore from DB | `CreateFromValidated()` | Already validated data |
| API request processing | `Create()` | External input validation required |

### Q4. When should domain events be published?

**They are published when a business-significant state change occurs.**

```csharp
// Good: Events with business significance
AddDomainEvent(new OrderCreatedEvent(Id, CustomerId, TotalAmount));
AddDomainEvent(new OrderConfirmedEvent(Id));

// Bad: Events that are too granular
AddDomainEvent(new OrderStatusChangedEvent(Id, OldStatus, NewStatus));  // Too generic
AddDomainEvent(new PropertyUpdatedEvent(Id, "Name", OldValue, NewValue));  // CRUD level
```

> For details on event handler registration, transaction considerations, etc., see the [Domain Events Guide](../07-domain-events).

### Q5. When is a Validate method needed in an Entity?

It is defined only when there are Entity-level business rules (validation of relationships between VOs). See [Creation Patterns -- Entity.Validate](#when-entityvalidate-is-needed-vs-not-needed).

---

## Reference Documents

- [Aggregate Design Principles (WHY)](../06a-aggregate-design) - Aggregate design principles and concepts
- [Entity/Aggregate Advanced Patterns](../06c-entity-aggregate-advanced) - Cross-Aggregate relationships, supplementary interfaces, practical examples
- [Value Object Implementation Guide](../05a-value-objects) - Value Object implementation patterns, [Validation and Enumeration Guide](../05b-value-objects-validation) - Enumerations, Application validation, FAQ
- [Domain Events Guide](../07-domain-events) - Complete domain event design (IDomainEvent, Pub/Sub, handlers, transactions)
- [Error System: Basics and Naming](../08a-error-system) - Error handling basic principles and naming conventions
- [Error System: Domain/Application Errors](../08b-error-system-domain-app) - Domain/Application error definition and test patterns
- [Domain Modeling Overview](../04-ddd-tactical-overview) - Domain modeling overview
- [Usecase Implementation Guide](../application/11-usecases-and-cqrs) - Using Aggregates in Application Layer (Apply pattern, Cross-Aggregate orchestration)
- [Adapter Implementation Guide](../adapter/13-adapters) - EF Core integration, Persistence Model mapping
- [Unit Testing Guide](../testing/15a-unit-testing)
