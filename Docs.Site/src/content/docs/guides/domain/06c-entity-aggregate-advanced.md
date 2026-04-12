---
title: "Entity and Aggregate Implementation — Advanced Patterns"
---

This document covers advanced implementation patterns for Entity/Aggregate. For core patterns (class hierarchy, ID system, creation patterns, command methods, domain events), see [06b-entity-aggregate-core.md](./06b-entity-aggregate-core).

## Introduction

Once you have the basic Aggregate structure in place, the following questions immediately arise in practice:

- When referencing another Aggregate, is it okay to directly hold the object?
- Where should common concerns like creation/modification timestamps and soft delete be implemented?
- How do you protect against simultaneous modifications of the same data?

### What You Will Learn

1. How to restrict Cross-Aggregate references to EntityId and communicate via domain events
2. Implementation patterns for `IAuditable`, `ISoftDeletable`, `IConcurrencyAware` supplementary interfaces
3. Infrastructure (Mapper, EF Core, Repository) integration checklist for each supplementary interface

### Prerequisites

- [Entity/Aggregate Core Patterns](./06b-entity-aggregate-core) — Class hierarchy, ID system, creation patterns, command methods, domain events
- [Aggregate Design Principles](./06a-aggregate-design) — Invariant and boundary setting concepts

> Cross-Aggregate references always use EntityId only, and common concerns like audit, soft delete, and concurrency control are declared as supplementary interfaces so the domain explicitly expresses the need. Infrastructure implementation follows the checklist for each interface.

## Summary

### Key Concepts

| Concept | Description |
|------|------|
| Cross-Aggregate references | Reference only by EntityId, inter-Aggregate communication via domain events |
| IAuditable | Tracks creation/modification timestamps, managed directly by the domain |
| ISoftDeletable | Supports soft delete, `Option<DateTime>` as single source of truth |
| IConcurrencyAware | Optimistic concurrency control, RowVersion-based Lost Update prevention |

### Key Procedures

1. Use only EntityId for Cross-Aggregate references, query external Aggregates via Domain Port
2. Apply supplementary interfaces as needed (`IAuditable`, `ISoftDeletable`, `IConcurrencyAware`)
3. Implement domain model + infrastructure according to each interface checklist

---

## Cross-Aggregate Relationships

### ID Reference Pattern

When referencing another Aggregate, **only EntityId is stored.**

Note in the code below that `ProductId` holds only the ID value, not the Product Aggregate itself.

```csharp
// Order Aggregate references Product Aggregate by ID
public sealed class Order : AggregateRoot<OrderId>
{
    // Cross-Aggregate reference (references Product by ID value)
    public ProductId ProductId { get; private set; }

    public Quantity Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money TotalAmount { get; private set; }
    public ShippingAddress ShippingAddress { get; private set; }
}
```

### Querying External Aggregates via Domain Port

When information from another Aggregate is needed, **define a Domain Port (interface)** and implement it in the Application Layer.

```csharp
// Domain Layer: Port definition
public interface IProductCatalog : IObservablePort
{
    /// <summary>
    /// Batch query prices for multiple products
    /// </summary>
    FinT<IO, Map<ProductId, Money>> GetPricesForProducts(IReadOnlyList<ProductId> productIds);
}
```

The Port **expresses what the domain needs:**
- `IProductCatalog` does not expose the entire Product Aggregate
- Provides needed information (prices) efficiently via batch API (prevents N+1 problem)
- Implementation is handled by the Application/Adapter Layer

### Inter-Aggregate Communication via Domain Events

State synchronization between Aggregates is handled through domain events.

```
Order Aggregate                     Inventory Aggregate
┌──────────────────┐                ┌──────────────────┐
│ Order.Create()   │                │                  │
│   └─ event publishing  │───────────────→│ DeductStock()    │
│     CreatedEvent │  Event Handler │                  │
└──────────────────┘                └──────────────────┘
      Transaction 1                         Transaction 2
```

### Entities Referencing Other Entities

When an Entity references another Entity, **only EntityId is referenced** (foreign key pattern).

```csharp
[GenerateEntityId]
public class OrderItem : Entity<OrderItemId>
{
    public OrderId OrderId { get; private set; }      // Reference to Order Entity
    public ProductId ProductId { get; private set; }  // Reference to Product Entity
    public Quantity Quantity { get; private set; }
    public Price UnitPrice { get; private set; }

#pragma warning disable CS8618
    private OrderItem() { }
#pragma warning restore CS8618

    private OrderItem(
        OrderItemId id,
        OrderId orderId,
        ProductId productId,
        Quantity quantity,
        Price unitPrice) : base(id)
    {
        OrderId = orderId;
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    // Create: Receives already validated Value Objects directly, passes EntityId as-is
    public static OrderItem Create(
        OrderId orderId,
        ProductId productId,
        Quantity quantity,
        Price unitPrice)
    {
        var id = OrderItemId.New();
        return new OrderItem(id, orderId, productId, quantity, unitPrice);
    }

    // CreateFromValidated: For ORM restoration
    public static OrderItem CreateFromValidated(
        OrderItemId id,
        OrderId orderId,
        ProductId productId,
        Quantity quantity,
        Price unitPrice)
        => new(id, orderId, productId, quantity, unitPrice);
}
```

> For cases where Navigation Properties are needed, see the [Adapter Implementation Guide](../adapter/13-adapters).

Now that we understand Cross-Aggregate reference rules, let us look at how to apply common supplementary features (audit, soft delete, concurrency control) to Entities via interfaces.

---

## Supplementary Interfaces

These are interfaces that provide additional capabilities to Entities.

### IAuditable

Tracks creation/modification timestamps. Although a cross-cutting concern, it is placed in the Domain Layer following the principle that entities directly manage their own state. Rather than delegating to infrastructure such as EF Core `SaveChanges` interceptors, business methods explicitly set timestamps.

**Location**: `Functorium.Domains.Entities.IAuditable`

#### Interface Definition

```csharp
// Tracks time only
public interface IAuditable
{
    DateTime CreatedAt { get; }
    Option<DateTime> UpdatedAt { get; }
}

// Tracks time + user
public interface IAuditableWithUser : IAuditable
{
    Option<string> CreatedBy { get; }
    Option<string> UpdatedBy { get; }
}
```

**Design Point:** `Option<T>` is used to explicitly represent the presence/absence of a value. Instead of `null`, `Option.None` type-safely represents "not yet modified."

#### Implementation Pattern -- Domain Directly Manages

All 5 entities in SingleHost implement `IAuditable` and follow the same pattern.

| Entity | Where CreatedAt Is Set | Where UpdatedAt Is Set |
|--------|-------------------|-------------------|
| Product | Constructor | `Update()` |
| Order | Constructor | `TransitionTo()` |
| Tag | Constructor | `Rename()` |
| Customer | Constructor | `UpdateCreditLimit()`, `ChangeEmail()` |
| Inventory | Constructor | `DeductStock()`, `AddStock()` |

**Usage Example (excerpt from Product.cs):**

Pattern of setting `CreatedAt` in the constructor and updating `UpdatedAt` in business methods.

```csharp
[GenerateEntityId]
public sealed class Product : AggregateRoot<ProductId>, IAuditable, ISoftDeletableWithUser
{
    public DateTime CreatedAt { get; private set; }
    public Option<DateTime> UpdatedAt { get; private set; }

    // Constructor: Sets CreatedAt
    private Product(ProductId id, ProductName name, ...) : base(id)
    {
        Name = name;
        CreatedAt = DateTime.UtcNow;
    }

    // Business method: Sets UpdatedAt
    public Fin<Product> Update(ProductName name, ...)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
        return this;
    }

    // For ORM restoration: receives createdAt, updatedAt as parameters
    public static Product CreateFromValidated(
        ProductId id, ...,
        DateTime createdAt,
        Option<DateTime> updatedAt, ...)
    {
        var product = new Product(id, ...) { CreatedAt = createdAt, UpdatedAt = updatedAt };
        return product;
    }
}
```

#### Infrastructure Strategy -- Mapper Conversion

| Aspect | Current Implementation | Alternative (Not Used) |
|------|----------|-------------|
| Audit field setting | Domain model sets directly | Auto-injection via EF Core `SaveChanges` interceptor |
| Mapper conversion | `Option<DateTime>.ToNullable()` / `Optional()` | -- |
| Persistence Model | `DateTime?` (nullable) | -- |

```csharp
// Domain → Persistence Model (ToModel)
CreatedAt = product.CreatedAt,
UpdatedAt = product.UpdatedAt.ToNullable(),    // Option<DateTime> → DateTime?

// Persistence Model → Domain (ToDomain)
Product.CreateFromValidated(
    ...,
    model.CreatedAt,
    Optional(model.UpdatedAt),                  // DateTime? → Option<DateTime>
    ...);
```

#### IAuditableWithUser Reference

`IAuditableWithUser` is provided for cases where user tracking is needed. It is not yet used in SingleHost and is applied in scenarios requiring user identification such as multi-tenancy.

#### Checklist -- When Applying IAuditable to a New Entity

- [ ] Implement `IAuditable` (`CreatedAt`, `UpdatedAt` properties)
- [ ] Set `CreatedAt = DateTime.UtcNow` in constructor
- [ ] Set `UpdatedAt = DateTime.UtcNow` in state change methods
- [ ] Add `createdAt`, `updatedAt` parameters to `CreateFromValidated()`
- [ ] Persistence Model: `DateTime?` type
- [ ] Mapper: `Option<DateTime>.ToNullable()` / `Optional()` conversion

### ISoftDeletable

Supports soft delete. Records are not actually deleted but marked as deleted.

**Location**: `Functorium.Domains.Entities.ISoftDeletable`

#### Why Soft Delete -- 5 Principles

| # | Value | Description |
|---|------|------|
| 1 | **Referential integrity** | Preserves Cross-Aggregate references. Example: physical deletion is impossible because `OrderLine -> ProductId` references exist |
| 2 | **Business meaning separation** | "Discontinued" is a domain concept, not data destruction. Explicit modeling with `Delete()`/`Restore()` + domain events |
| 3 | **Restorability** | Recoverable via `Restore()` method. Idempotency guaranteed |
| 4 | **Audit trail** | Track who deleted via `DeletedBy` in `ISoftDeletableWithUser` |
| 5 | **Infrastructure concern separation** | EF Core Global Query Filter + Dapper `WHERE DeletedAt IS NULL` automatic filtering |

#### Interface Definition

```csharp
// Tracks deletion status — Option<DateTime> is the single source of truth
public interface ISoftDeletable
{
    Option<DateTime> DeletedAt { get; }
    bool IsDeleted => DeletedAt.IsSome;  // default interface member (derived property)
}

// Tracks deletion status + who deleted
public interface ISoftDeletableWithUser : ISoftDeletable
{
    Option<string> DeletedBy { get; }
}
```

**Design Point:** `bool IsDeleted` is a default interface member derived from `DeletedAt`. Since `Option<DateTime>` is the single source of truth, state inconsistency is impossible.

#### Domain Model Implementation Pattern

**Reference**: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/AggregateRoots/Products/Product.cs`

Idempotency guarantee pattern for `Delete()` and `Restore()`, and guard pattern preventing modification of deleted entities in `Update()`.

```csharp
[GenerateEntityId]
public sealed class Product : AggregateRoot<ProductId>, ISoftDeletableWithUser
{
    // --- Error Type ---
    public sealed record AlreadyDeleted : DomainErrorType.Custom;

    // --- Domain Events ---
    public sealed record DeletedEvent(ProductId ProductId, string DeletedBy) : DomainEvent;
    public sealed record RestoredEvent(ProductId ProductId) : DomainEvent;

    // --- SoftDelete properties ---
    public Option<DateTime> DeletedAt { get; private set; }
    public Option<string> DeletedBy { get; private set; }

    // --- Delete: Idempotency guarantee ---
    public Product Delete(string deletedBy)
    {
        if (DeletedAt.IsSome)           // Already deleted → do nothing
            return this;

        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        AddDomainEvent(new DeletedEvent(Id, deletedBy));
        return this;
    }

    // --- Restore: Idempotency guarantee ---
    public Product Restore()
    {
        if (DeletedAt.IsNone)           // Not deleted → do nothing
            return this;

        DeletedAt = Option<DateTime>.None;
        DeletedBy = Option<string>.None;
        AddDomainEvent(new RestoredEvent(Id));
        return this;
    }

    // --- Update guard: Prevent modification of deleted entities ---
    public Fin<Product> Update(ProductName name, ProductDescription description, Money price)
    {
        if (DeletedAt.IsSome)
            return DomainError.For<Product>(
                new AlreadyDeleted(), Id.ToString(),
                "Cannot update a deleted product");
        // ... update logic
        return this;
    }

    // --- ORM restoration factory: includes deletedAt, deletedBy parameters ---
    public static Product CreateFromValidated(
        ProductId id, ...,
        Option<DateTime> deletedAt, Option<string> deletedBy)
    {
        var product = new Product(id, ...) { DeletedAt = deletedAt, DeletedBy = deletedBy };
        return product;
    }
}
```

**Key Pattern Summary:**

| Pattern | Implementation |
|------|------|
| Idempotency | `Delete()` -- `DeletedAt.IsSome` -> early return |
| Idempotency | `Restore()` -- `DeletedAt.IsNone` -> early return |
| Error guard | `Update()` -- `DeletedAt.IsSome` -> `Fin.Fail(AlreadyDeleted)` |
| Domain events | Publish `DeletedEvent`/`RestoredEvent` on state changes |
| Initialization | Restore with `Option<T>.None` (not null) |

#### Repository Port Pattern

**Reference**: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/AggregateRoots/Products/IProductRepository.cs`

```csharp
public interface IProductRepository : IRepository<Product, ProductId>
{
    FinT<IO, Product> GetByIdIncludingDeleted(ProductId id);
}
```

Why `GetByIdIncludingDeleted()` is needed: Delete/Restore commands need to access deleted entities, so a separate method that bypasses the Global Query Filter is required.

#### Infrastructure Filtering Strategy

| Adapter | Filter Strategy | Bypass Method |
|---------|-----------|-----------|
| EF Core | `HasQueryFilter(p => p.DeletedAt == null)` | `IgnoreQueryFilters()` |
| Dapper | `WHERE DeletedAt IS NULL` (BuildWhereClause) | Write separate query |
| InMemory | `p.DeletedAt.IsNone` condition | Remove condition |

**Mapper Conversion:**
- Domain -> Model: `Option<DateTime>.ToNullable()` (stored as `NULL` in DB)
- Model -> Domain: `Optional(model.DeletedAt)` (`NULL` -> `Option.None`)

> For detailed infrastructure implementation, see the [Adapter Implementation Guide](../adapter/13-adapters).

#### Checklist

Items to check when applying Soft Delete to a new Aggregate:

- [ ] Domain model: Implement `ISoftDeletableWithUser`
- [ ] Domain model: Idempotent `Delete()`/`Restore()` methods
- [ ] Domain model: `DeletedAt.IsSome` guard in state change methods
- [ ] Domain model: Publish domain events (`DeletedEvent`, `RestoredEvent`)
- [ ] Domain model: `deletedAt`/`deletedBy` parameters in `CreateFromValidated()`
- [ ] Repository Port: `GetByIdIncludingDeleted()` method
- [ ] EF Core: `HasQueryFilter(e => e.DeletedAt == null)` configuration
- [ ] Dapper: `WHERE DeletedAt IS NULL` automatic filtering
- [ ] Mapper: `Option<DateTime>` <-> `DateTime?` conversion

### IConcurrencyAware

Supports optimistic concurrency control. An Aggregate's invariants are protected only within a single transaction, so invariant protection through domain logic alone is impossible across concurrent transactions (Lost Update). This is an interface where the domain explicitly declares "I need concurrency protection," and it is selectively applied to high-contention Aggregates.

**Location**: `Functorium.Domains.Entities.IConcurrencyAware`

#### Interface Definition

```csharp
public interface IConcurrencyAware
{
    byte[] RowVersion { get; }
}
```

#### Why It Is Needed -- Lost Update Scenario

The following scenario shows the Lost Update problem that occurs when two transactions simultaneously deduct stock without RowVersion.

Explaining the concurrency issue with the Inventory `DeductStock` example:

```
Initial state: stock = 10 items

1. [Transaction A] Reads stock -> 10 items
2. [Transaction B] Reads stock -> 10 items  (same value because A has not saved yet)
3. [Transaction A] DeductStock(7): 7 <= 10 OK -> stock = 3 -> save to DB
4. [Transaction B] DeductStock(7): 7 <= 10 OK -> stock = 3 -> save to DB (overwrites A's result!)

Final result: stock = 3 items
Expected result: B should be rejected (actual stock = 3 after A, cannot deduct 7)
```

Key point: The `if (quantity > StockQuantity)` guard in `DeductStock()` judges **only based on the value at the time of reading.** Transaction B passes validation because it read the value before A saved (10), but in reality stock has already decreased to 3. This is the **Lost Update** problem, and it cannot be prevented by domain logic alone.

#### Why Place It in the Domain Layer

| Aspect | Description |
|------|------|
| Domain modeling decision | Which Aggregate has high contention is domain knowledge. Inventory (deducted with every order) is high contention, Product (low-frequency admin edits) is low contention |
| Explicit declaration | The domain declares it, rather than infrastructure guessing |
| Infrastructure separation | Interface is in domain, `IsRowVersion()` mapping is in infrastructure. Domain does not know about DB |

#### Domain Model Implementation Pattern

**Reference**: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/AggregateRoots/Inventories/Inventory.cs`

```csharp
[GenerateEntityId]
public sealed class Inventory : AggregateRoot<InventoryId>, IAuditable, IConcurrencyAware
{
    public sealed record InsufficientStock : DomainErrorType.Custom;

    // Value Object properties
    public Quantity StockQuantity { get; private set; }

    // Optimistic concurrency control
    public byte[] RowVersion { get; private set; } = [];

    // Audit properties
    public DateTime CreatedAt { get; private set; }
    public Option<DateTime> UpdatedAt { get; private set; }

    // Business method: RowVersion is auto-updated by DB
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

    // For ORM restoration: includes byte[] rowVersion parameter
    public static Inventory CreateFromValidated(
        InventoryId id, ProductId productId, Quantity stockQuantity,
        byte[] rowVersion, DateTime createdAt, Option<DateTime> updatedAt)
    {
        return new Inventory(id, productId, stockQuantity)
        {
            RowVersion = rowVersion,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }
}
```

**Key Pattern Summary:**

| Pattern | Implementation |
|------|------|
| RowVersion declaration | `byte[] RowVersion { get; private set; } = []` |
| Initial value | Empty array `[]` -- auto-generated by EF Core on DB save |
| Business methods | `RowVersion` is not directly changed -- DB auto-updates |
| ORM restoration | Passed as `byte[] rowVersion` parameter in `CreateFromValidated()` |

#### Infrastructure Implementation -- Full Flow

Supporting `IConcurrencyAware` in infrastructure requires 4 files to cooperate:

```
Domain Model ──→ Mapper ──→ Persistence Model ──→ DB Save (UoW)
(byte[] RowVersion)  (direct pass-through)  (byte[] RowVersion)     │
                                       ↑                   │
                              EF Core Configuration        │
                              (.IsRowVersion())            │
                                                           ↓
                                                  UPDATE ... WHERE RowVersion = @original
                                                           │
                                              ┌────────────┴────────────┐
                                              │                         │
                                         Row update success              Zero rows updated
                                              │                         │
                                         Normal response         DbUpdateConcurrencyException
                                                                        │
                                                              ConcurrencyConflict error
```

**Step 1. Persistence Model** -- Define `byte[] RowVersion` property

```csharp
// InventoryModel.cs
public class InventoryModel
{
    public string Id { get; set; } = default!;
    public string ProductId { get; set; } = default!;
    public int StockQuantity { get; set; }
    public byte[] RowVersion { get; set; } = [];    // <- concurrency token
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

**Step 2. EF Core Configuration** -- Map SQL Server ROWVERSION with `.IsRowVersion()`

```csharp
// InventoryConfiguration.cs
builder.Property(i => i.RowVersion)
    .IsRowVersion();    // SQL Server: auto-incrementing 8-byte timestamp
```

**Step 3. Mapper** -- Bidirectional `byte[]` direct pass-through between Domain and Persistence Model

```csharp
// InventoryMapper.cs — Domain → Persistence Model
public static InventoryModel ToModel(this Inventory inventory) => new()
{
    // ...
    RowVersion = inventory.RowVersion,
};

// InventoryMapper.cs -- Persistence Model -> Domain
public static Inventory ToDomain(this InventoryModel model) =>
    Inventory.CreateFromValidated(
        // ...
        model.RowVersion,       // byte[] direct pass-through
        // ...);
```

**Step 4. UoW Conflict Handling** -- `DbUpdateConcurrencyException` -> `ConcurrencyConflict` error conversion

```csharp
// Inside EfCoreUnitOfWork.SaveChanges()
catch (DbUpdateConcurrencyException ex)
{
    return AdapterError.FromException<EfCoreUnitOfWork>(
        new ConcurrencyConflict(), ex);
}
```

**How it Works:** EF Core automatically adds properties configured with `IsRowVersion()` to the `WHERE` clause of UPDATE/DELETE queries. If the RowVersion stored in the DB differs from the value at the time of reading, the update affects 0 rows, and EF Core raises `DbUpdateConcurrencyException`. The UoW converts this into a `ConcurrencyConflict` error and returns it.

> For application timing, conflict handling strategy (Fail-Fast), and full UoW code, see [Section 4. Practical Examples of Aggregate Boundary Setting -- Concurrency Considerations](./06a-aggregate-design#concurrency-considerations).

**Reference Files:**
- `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/EfCore/Models/InventoryModel.cs`
- `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/EfCore/Configurations/InventoryConfiguration.cs`
- `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/EfCore/Mappers/InventoryMapper.cs`
- `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/EfCore/EfCoreUnitOfWork.cs`

#### Checklist -- When Applying IConcurrencyAware to a New Aggregate

- [ ] Domain model: Implement `IConcurrencyAware` (`byte[] RowVersion` property)
- [ ] Domain model: `byte[] rowVersion` parameter in `CreateFromValidated()`
- [ ] Persistence Model: `byte[] RowVersion` property
- [ ] EF Core Configuration: `.IsRowVersion()` setting
- [ ] Mapper: `RowVersion` bidirectional direct pass-through
- [ ] Application decision: See [Section 4 Application Criteria Table](./06a-aggregate-design#concurrency-considerations)

Now that we have learned the individual supplementary interface patterns, let us examine a complete Aggregate example combining all of them.

---

## Practical Examples

### Order Aggregate (Comprehensive Example)

A complete example including Value Object properties, Entity references, and domain events.

```csharp
// Reference: samples/ecommerce-ddd/.../OrderStatus.cs, Order.cs
using Functorium.Domains.Entities;
using Functorium.Domains.Events;
using Functorium.Domains.Errors;
using static Functorium.Domains.Errors.DomainErrorType;
using static LanguageExt.Prelude;

// OrderStatus: Smart Enum based on SimpleValueObject<string> + state transition rules
public sealed class OrderStatus : SimpleValueObject<string>
{
    public sealed record InvalidValue : DomainErrorType.Custom;

    public static readonly OrderStatus Pending = new("Pending");
    public static readonly OrderStatus Confirmed = new("Confirmed");
    public static readonly OrderStatus Shipped = new("Shipped");
    public static readonly OrderStatus Delivered = new("Delivered");
    public static readonly OrderStatus Cancelled = new("Cancelled");

    private static readonly HashMap<string, OrderStatus> All = HashMap(
        ("Pending", Pending), ("Confirmed", Confirmed), ("Shipped", Shipped),
        ("Delivered", Delivered), ("Cancelled", Cancelled));

    // Declare allowed transitions as data -- eliminates per-method hard-coding
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

    public bool CanTransitionTo(OrderStatus target) =>
        AllowedTransitions.Find(Value)
            .Map(allowed => allowed.Any(v => v == target.Value))
            .IfNone(false);
}

// Order Aggregate Root -- Centralized TransitionTo() pattern
[GenerateEntityId]
public class Order : AggregateRoot<OrderId>, IAuditableWithUser
{
    #region Error Types

    public sealed record InvalidOrderStatusTransition : DomainErrorType.Custom;

    #endregion

    #region Domain Events

    public sealed record CreatedEvent(OrderId OrderId, CustomerId CustomerId, Money TotalAmount) : DomainEvent;
    public sealed record ConfirmedEvent(OrderId OrderId) : DomainEvent;
    public sealed record CancelledEvent(OrderId OrderId) : DomainEvent;

    #endregion

    private readonly List<OrderItem> _items = [];

    // Value Object properties
    public Money TotalAmount { get; private set; }
    public Address ShippingAddress { get; private set; }

    // Other Entity references (EntityId)
    public CustomerId CustomerId { get; private set; }

    // Status -- OrderStatus is a Smart Enum based on SimpleValueObject<string>
    public OrderStatus Status { get; private set; }

    // Audit information
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public string? UpdatedBy { get; private set; }

    // Collection
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    // Default constructor for ORM
#pragma warning disable CS8618
    private Order() { }
#pragma warning restore CS8618

    // Internal constructor
    private Order(
        OrderId id,
        CustomerId customerId,
        Money totalAmount,
        Address shippingAddress,
        string createdBy) : base(id)
    {
        CustomerId = customerId;
        TotalAmount = totalAmount;
        ShippingAddress = shippingAddress;
        Status = OrderStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    // Create: Receives already validated Value Objects directly
    public static Order Create(
        CustomerId customerId,
        Money totalAmount,
        Address shippingAddress,
        string createdBy)
    {
        var id = OrderId.New();
        var order = new Order(id, customerId, totalAmount, shippingAddress, createdBy);
        order.AddDomainEvent(new CreatedEvent(id, customerId, totalAmount));
        return order;
    }

    // CreateFromValidated: For ORM restoration
    public static Order CreateFromValidated(
        OrderId id,
        CustomerId customerId,
        Money totalAmount,
        Address shippingAddress,
        OrderStatus status,
        DateTime createdAt,
        string? createdBy,
        DateTime? updatedAt,
        string? updatedBy)
    {
        return new Order
        {
            Id = id,
            CustomerId = customerId,
            TotalAmount = totalAmount,
            ShippingAddress = shippingAddress,
            Status = status,
            CreatedAt = createdAt,
            CreatedBy = createdBy,
            UpdatedAt = updatedAt,
            UpdatedBy = updatedBy
        };
    }

    // Domain operations: each method delegates to TransitionTo()
    public Fin<Unit> Confirm(string updatedBy) =>
        TransitionTo(OrderStatus.Confirmed, new ConfirmedEvent(Id), updatedBy);

    public Fin<Unit> Cancel(string updatedBy) =>
        TransitionTo(OrderStatus.Cancelled, new CancelledEvent(Id), updatedBy);

    // Shipping address change -- invariant checks unrelated to state transitions are separate from CanTransitionTo()
    public Fin<Unit> UpdateShippingAddress(Address newAddress, string updatedBy)
    {
        if (Status != OrderStatus.Pending)
            return DomainError.For<Order, string, string>(
                new InvalidOrderStatusTransition(),
                value1: Status,
                value2: "UpdateShippingAddress",
                message: "Shipping address can only be changed for pending orders");

        ShippingAddress = newAddress;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        return unit;
    }

    // Centralized state transition -- transition rules delegated to OrderStatus.CanTransitionTo()
    private Fin<Unit> TransitionTo(OrderStatus target, DomainEvent domainEvent, string updatedBy)
    {
        if (!Status.CanTransitionTo(target))
            return DomainError.For<Order, string, string>(
                new InvalidOrderStatusTransition(),
                value1: Status,
                value2: target,
                message: $"Cannot transition from '{Status}' to '{target}'");

        Status = target;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        AddDomainEvent(domainEvent);
        return unit;
    }

    // Add order item (internal use)
    internal void AddItem(OrderItem item)
    {
        _items.Add(item);
        RecalculateTotalAmount();
    }

    private void RecalculateTotalAmount()
    {
        var total = _items.Sum(i => (decimal)i.UnitPrice * (int)i.Quantity);
        TotalAmount = Money.CreateFromValidated(total, TotalAmount.Currency);
    }
}
```

---

## Checklist

### Aggregate Boundary Setting Checklist

- [ ] **What invariants does this Aggregate protect?**
  - If there are no clear invariants, the boundary may be incorrect
- [ ] **Is the Aggregate small enough?**
  - Does it contain only the minimum data needed for invariant protection?
- [ ] **Does it reference other Aggregates only by ID?**
  - If there are direct object references, boundary review is needed
- [ ] **Does one transaction change only one Aggregate?**
  - If multiple Aggregates are changed simultaneously, design review is needed
- [ ] **Does the child Entity have meaning without the Aggregate Root?**
  - If so, consider separating it into its own Aggregate
- [ ] **Do command methods encapsulate invariants?**
  - Are invariants not being directly validated from outside?
- [ ] **Are domain events published only from the Aggregate Root?**
  - If attempting to publish events from child Entities, review the design

### Functorium Implementation Checklist

- [ ] Cross-Aggregate references use only `EntityId` types
- [ ] Determine whether to apply supplementary interfaces (`IAuditable`, `ISoftDeletable`, `IConcurrencyAware`)
- [ ] See [Adapter Implementation Guide](../adapter/13-adapters) for EF Core integration

---

## Troubleshooting

### Transaction Boundary Violation Due to Direct Object References Between Aggregates
**Cause:** Directly referencing an Entity from another Aggregate (Navigation Property) causes multiple Aggregates to be changed in a single transaction, violating design principles.
**Resolution:** Always use only EntityId for Cross-Aggregate references. If information from another Aggregate is needed, define a Domain Port, and handle state synchronization between Aggregates via domain events.

---

## FAQ

### Q1. When referencing another Entity, do you use the full Entity or EntityId?

Always reference only by EntityId. See [Cross-Aggregate Relationships](#cross-aggregate-relationships).

---

## References

- [Entity/Aggregate Core Patterns](./06b-entity-aggregate-core) - Class hierarchy, ID system, creation patterns, command methods, domain events
- [Aggregate Design Principles (WHY)](./06a-aggregate-design) - Aggregate design principles and concepts
- [Value Object Implementation Guide](./05a-value-objects) - Value Object implementation patterns, [Validation and Enumeration Guide](./05b-value-objects-validation) - Enumeration, Application validation, FAQ
- [Domain Events Guide](./07-domain-events) - Complete domain events design (IDomainEvent, Pub/Sub, handlers, transactions)
- [Error System: Basics and Naming](./08a-error-system) - Error handling basic principles and naming conventions
- [Error System: Domain/Application Errors](./08b-error-system-domain-app) - Domain/Application error definitions and test patterns
- [Domain Modeling Overview](./04-ddd-tactical-overview) - Domain modeling overview
- [Usecase Implementation Guide](../application/11-usecases-and-cqrs) - Aggregate usage in the Application Layer (Apply pattern, Cross-Aggregate coordination)
- [Adapter Implementation Guide](../adapter/13-adapters) - EF Core integration, Persistence Model mapping
- [Unit Testing Guide](../testing/15a-unit-testing)

---

## Dictionary Lookup Performance Tips

When using Dictionary-based cache or lookup logic in Entity/Aggregate implementation, the `ContainsKey` + indexer combination looks up the same key twice. Using `TryGetValue` checks existence and retrieves the value in a single lookup.

### Value Lookup Pattern

```csharp
// Before: looks up key twice
if (_cache.ContainsKey(id))
{
    return _cache[id];
}

// After: looks up key only once
if (_cache.TryGetValue(id, out var cachedValue))
{
    return cachedValue;
}
```

### GetOrAdd Pattern

```csharp
// Before
if (!_factories.ContainsKey(type))
{
    _factories[type] = CreateFactory(type);
}
return _factories[type];

// After
if (!_factories.TryGetValue(type, out var factory))
{
    factory = CreateFactory(type);
    _factories[type] = factory;
}
return factory;
```

### Performance Comparison

| Pattern | Hash Computation | Bucket Lookup | Total Operations |
|------|----------|----------|--------|
| `ContainsKey` + `[key]` | 2 times | 2 times | 4 |
| `TryGetValue` | 1 time | 1 time | 2 |

For read-intensive workloads, also consider `ConcurrentDictionary`'s `GetOrAdd`:

```csharp
private readonly ConcurrentDictionary<string, MetricsSet> _metrics = new();

public MetricsSet GetMetrics(string category)
{
    return _metrics.GetOrAdd(category, key => CreateMetrics(key));
}
```

> **Code Analysis Tool**: The .NET analyzer `CA1854` detects the `ContainsKey` + indexer pattern.
