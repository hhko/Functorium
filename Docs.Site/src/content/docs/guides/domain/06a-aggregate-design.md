---
title: "Aggregate Design (WHY + WHAT)"
---

This document covers Aggregate design principles for correctly setting consistency boundaries to prevent concurrency conflicts and data integrity issues. For Entity/Aggregate implementation, see [06b-entity-aggregate-core.md](../06b-entity-aggregate-core).

## Introduction

"A `DbUpdateConcurrencyException` occurs with every order processing."
"Putting all related data in a single Entity has made transactions slow."
"I understand that multiple Aggregates should not be changed in a single transaction, but how do we ensure data consistency?"

These problems are typical symptoms that appear when Aggregate boundaries are set incorrectly. Aggregate is the most important design decision in DDD, and this boundary determines the system's concurrency, performance, and maintainability.

### What You Will Learn

This document covers the following topics:

1. **Why Aggregates are consistency boundaries** - Invariant protection and transaction principles
2. **Four core rules of Aggregate design** - Invariant protection, small Aggregates, ID references, eventual consistency
3. **Criteria for distinguishing Value Object/Entity/Aggregate Root** - Decision flowchart and judgment criteria
4. **Split/merge decisions** - Signals and criteria for boundary readjustment during operation
5. **Anti-pattern identification and avoidance** - God Aggregate, direct references, external invariant validation, etc.

### Prerequisites

A basic understanding of the following concepts is needed to understand this document:

- The complete building block map from the [DDD Tactical Design Overview](../04-ddd-tactical-overview)
- [Value Object](../05a-value-objects) concepts and immutability principles
- Basic concepts of transactions and concurrency control

> A single Aggregate boundary decision determines the system's concurrency, performance, and maintainability. The core principles are: keep boundaries small, reference between Aggregates only by ID, and handle changes outside the boundary through domain events.

## Summary

### Key Commands

```csharp
// Aggregate Root definition
[GenerateEntityId]
public class Order : AggregateRoot<OrderId> { }

// Invariant protection (inside Aggregate)
public Fin<Unit> DeductStock(Quantity quantity) { ... }

// Domain event publishing
AddDomainEvent(new CreatedEvent(Id, productId, quantity, totalAmount));

// Cross-Aggregate reference (ID only)
public ProductId ProductId { get; private set; }
```

### Key Procedures

**1. Aggregate Design:**
1. Identify invariants of domain concepts
2. Set boundaries as the minimum object group that protects invariants
3. Designate the Aggregate Root (single entry point for external access)
4. Reference other Aggregates only by ID

**2. Aggregate Split/Merge Decisions:**
1. Concurrency conflicts, change frequency imbalance, invariant independence -> consider splitting
2. Always changed together, mutual invariant dependency, eventual consistency not possible -> consider merging

### Key Concepts

| Concept | Description |
|------|------|
| Consistency boundary | Protects invariants within the Aggregate in a single transaction |
| Transaction principle | One transaction = one Aggregate change |
| ID reference | No direct object references between Aggregates, store only EntityId |
| Eventual consistency | Cross-Aggregate changes are handled asynchronously via domain events |
| Small Aggregates | Include only the minimum data needed for invariant protection |

---

## Why Aggregates

### Purpose of This Guide

The most important decision in DDD tactical design is **where to place the Aggregate boundary.** If this decision is wrong:

- Concurrency conflicts due to large Aggregates
- Performance degradation from overly broad transaction scope
- Difficulty making changes due to tight coupling between Aggregates

This guide maps DDD design principles to Functorium framework implementation, providing **the rationale for design decisions.**

For example, if the product catalog and inventory are placed in a single Aggregate, concurrency conflicts occur whenever an admin's product name edit and a customer's order processing happen simultaneously. Separating them into separate Aggregates allows each to be changed independently, eliminating conflicts. This illustrates how a single Aggregate boundary decision determines the stability of the production environment.

### Consistency Boundary

An Aggregate is **a group of objects that guarantees consistency as a single unit.** All invariants within the Aggregate are protected within a single transaction.

```
┌─────────────────────────────────┐
│          Aggregate              │
│                                 │
│  ┌──────────────┐               │
│  │ Aggregate    │  invariant protection    │  ← transaction boundary
│  │ Root         │───────────    │
│  └──────┬───────┘               │
│         │                       │
│    ┌────┴────┐                  │
│    │         │                  │
│  Child    Value                 │
│  Entity   Object                │
│                                 │
└─────────────────────────────────┘
```

### Invariant Protection

Invariants are **business rules that must always hold true.** Aggregates protect these invariants internally without exposing them externally.

The key point to note in the following code is that the `DeductStock()` method returns failure as `Fin<Unit>` instead of throwing an exception when stock is insufficient.

```csharp
// Inventory Aggregate invariant: stock cannot be negative
// Error type definition: public sealed record InsufficientStock : DomainErrorKind.Custom;
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

### Aggregate as a Transaction Boundary

**One transaction = one Aggregate change** is the principle.

```
One Transaction changes one Aggregate
┌─────────────────────────┐
│ Transaction             │
│  Inventory.DeductStock  │
│  Repository.Save        │
└─────────────────────────┘

One Transaction changes multiple Aggregates
┌──────────────────────────────────┐
│ Transaction                      │
│  Inventory.DeductStock           │
│  Order.Create                    │  <- concurrency conflict risk
│  Customer.UpdateCreditLimit      │
└──────────────────────────────────┘
```

### Components of an Aggregate

| Component | Role | Functorium Mapping |
|----------|------|----------------|
| **Aggregate Root** | Single entry point for external access | `AggregateRoot<TId>` |
| **Child Entity** | Internal Entity managed by Root | `Entity<TId>` |
| **Value Object** | Immutable value | `SimpleValueObject<T>`, `ValueObject` |

### Entity vs Value Object

| Aspect | Entity | Value Object |
|------|--------|--------------|
| **Identifier** | ID-based equality | Value-based equality |
| **Mutability** | Mutable (state can change) | Immutable |
| **Lifecycle** | Long-lived (Repository tracked) | Short-lived (ephemeral) |
| **Domain events** | Can publish (AggregateRoot) | Cannot publish |
| **Examples** | Order, User, Product | Money, Email, Address |

### Base Class Selection

| Usage Scenario | Base Class | Characteristics |
|--------------|------------|------|
| General Entity | `Entity<TId>` | ID-based equality |
| Aggregate Root | `AggregateRoot<TId>` | Domain event management |

### Why Use Entities?

Without Entities, the following problems occur:

```csharp
// Problem 1: Identifier is unclear
public class Order
{
    public Guid Id { get; set; }  // Guid? int? string?
    public decimal Amount { get; set; }
}

// Problem 2: Can be confused with IDs of other types
void ProcessOrder(Guid orderId, Guid customerId);
ProcessOrder(customerId, orderId);  // Order mistake - no compile error!

// Problem 3: Equality comparison is unclear
var order1 = GetOrder(id);
var order2 = GetOrder(id);
order1 == order2;  // false? (reference comparison)
```

Entities solve these problems:

```csharp
// Solution: Type-safe ID and ID-based equality
[GenerateEntityId]
public class Order : Entity<OrderId>
{
    public Money Amount { get; private set; }

    private Order(OrderId id, Money amount) : base(id)
    {
        Amount = amount;
    }
}

// Prevent mistakes with compile errors
void ProcessOrder(OrderId orderId, CustomerId customerId);
ProcessOrder(customerId, orderId);  // Compile error!

// ID-based equality
var order1 = GetOrder(id);
var order2 = GetOrder(id);
order1 == order2;  // true (same ID)
```

### Core Pattern

```csharp
using Functorium.Domains.Entities;

[GenerateEntityId]  // Auto-generates OrderId
public class Order : AggregateRoot<OrderId>
{
    public Money Amount { get; private set; }
    public CustomerId CustomerId { get; private set; }

    // Default constructor for ORM
#pragma warning disable CS8618
    private Order() { }
#pragma warning restore CS8618

    // Internal constructor
    private Order(OrderId id, Money amount, CustomerId customerId) : base(id)
    {
        Amount = amount;
        CustomerId = customerId;
    }

    // Create: Receives already validated Value Objects directly
    public static Order Create(Money amount, CustomerId customerId)
    {
        var id = OrderId.New();
        return new Order(id, amount, customerId);
    }

    // CreateFromValidated: Direct pass-through of already validated/normalized data
    // Restores the Aggregate from data read from the DB.
    // Validation/normalization is skipped since the data already passed validation at save time.
    public static Order CreateFromValidated(OrderId id, Money amount, CustomerId customerId)
        => new(id, amount, customerId);

    // Domain operation
    public Fin<Unit> UpdateAmount(Money newAmount)
    {
        Amount = newAmount;
        AddDomainEvent(new OrderAmountUpdatedEvent(Id, newAmount));
        return unit;
    }
}
```

We have examined the Aggregate concept and its components. In the next section, we will learn the four core rules to follow when implementing these concepts in code.

---

## Aggregate Design Rules

### Rule 1: Protect Invariants Within Aggregate Boundaries

All invariants within an Aggregate are protected through the Aggregate Root. Child Entities cannot be directly modified from outside.

```csharp
// ✅ Manage Tags through Aggregate Root (Product)
public sealed class Product : AggregateRoot<ProductId>
{
    private readonly List<Tag> _tags = [];
    public IReadOnlyList<Tag> Tags => _tags.AsReadOnly();

    public Product AddTag(Tag tag)
    {
        // Invariant: prevent duplicate Tags
        if (_tags.Any(t => t.Id == tag.Id))
            return this;

        _tags.Add(tag);
        AddDomainEvent(new TagAssignedEvent(tag.Id, tag.Name));
        return this;
    }

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

```csharp
// ❌ Directly modifying child Entity from outside
product.Tags.Add(newTag);  // Compile error because IReadOnlyList
```

### Rule 2: Design Small Aggregates

Aggregates should include **only the minimum data needed for invariant protection.**

```csharp
// ✅ Small Aggregate: includes only what is needed
public sealed class Customer : AggregateRoot<CustomerId>
{
    public CustomerName Name { get; private set; }
    public Email Email { get; private set; }
    public Money CreditLimit { get; private set; }
}
```

```csharp
// ❌ Large Aggregate: includes everything related
public class Customer : AggregateRoot<CustomerId>
{
    public CustomerName Name { get; private set; }
    public Email Email { get; private set; }
    public List<Order> Orders { get; }         // No invariant for Customer to protect
    public List<Address> Addresses { get; }    // Can be separated into its own Aggregate
    public List<PaymentMethod> Payments { get; } // Can be separated into its own Aggregate
}
```

**Why should it be small?**

| Problem | Large Aggregate | Small Aggregate |
|------|-------------|---------------|
| Concurrency | Frequent conflicts | Minimal conflicts |
| Performance | Full load required | Load only what is needed |
| Memory | High usage | Low usage |
| Transaction | Wide scope | Narrow scope |

### Rule 3: Reference Other Aggregates Only by ID

Between Aggregates, **only EntityId is stored.** Direct object references are not used.

```csharp
// ✅ Reference by ID only (Order → Product)
public sealed class Order : AggregateRoot<OrderId>
{
    // Cross-Aggregate reference (references Product by ID value)
    public ProductId ProductId { get; private set; }
    public Quantity Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money TotalAmount { get; private set; }
}
```

```csharp
// ❌ Direct object reference
public class Order : AggregateRoot<OrderId>
{
    public Product Product { get; private set; }  // Tight coupling!
}
```

**Why reference only by ID?**

1. **Aggregate independence**: Each Aggregate is loaded/saved independently
2. **Loose coupling**: Avoids direct references between Entities
3. **Performance**: Loads related Aggregates only when needed

### Rule 4: Use Eventual Consistency Outside Boundaries

Business rules that span multiple Aggregates are handled through **domain events** via eventual consistency.

```csharp
// Stock deduction on order creation requires changing a separate Aggregate (Product)
// → Handled asynchronously via domain events

// Event publishing from Order Aggregate
public static Order Create(
    ProductId productId,
    Quantity quantity,
    Money unitPrice,
    ShippingAddress shippingAddress)
{
    var totalAmount = unitPrice.Multiply(quantity);
    var order = new Order(OrderId.New(), productId, quantity, unitPrice, totalAmount, shippingAddress);
    order.AddDomainEvent(new CreatedEvent(order.Id, productId, quantity, totalAmount));
    return order;
}

// Updating Inventory Aggregate in Event Handler (separate transaction)
// public class OnOrderCreated : IDomainEventHandler<Order.CreatedEvent>
// {
//     public async ValueTask Handle(Order.CreatedEvent @event, CancellationToken ct)
//     {
//         // Call Inventory.DeductStock
//     }
// }
```

> **Note**: Since multiple Aggregates cannot be changed simultaneously in a single transaction, Cross-Aggregate side effects are handled via event handlers (eventual consistency). For practical exceptions such as simultaneously **creating** related Aggregates within the same Bounded Context, see [Section 4: Transaction Boundary Practical Guidelines](#transaction-boundary-practical-guidelines).

Now that we understand the design rules, let us learn the criteria for classifying domain concepts as Value Object, Entity, or Aggregate Root.

---

## Distinguishing Aggregate vs Entity vs Value Object

### Decision Flowchart

```
Does this domain concept need a unique identifier?
│
├── No → Value Object
│            (Money, Email, Address, Quantity...)
│
└── Yes → Entity
         │
         Is this Entity independently stored/queried?
         │
         ├── Yes → Aggregate Root
         │        (Customer, Product, Order...)
         │
         └── No → Child Entity (inside Aggregate)
                      (Tag, OrderItem...)
```

### Judgment Criteria Table

The following table compares the three building blocks across seven criteria. The key differences are the presence of a unique identifier and the ability to be independently queried.

| Criterion | Value Object | Entity (Child) | Aggregate Root |
|------|-------------|--------------|---------------|
| Unique identifier | None | Present | Present |
| Equality | Value-based | ID-based | ID-based |
| Mutability | Immutable | Mutable | Mutable |
| Independent query | Not possible | Not possible (via Root) | Possible |
| Repository | None | None | Present |
| Domain events | Cannot publish | Cannot publish | Can publish |
| Lifecycle | Depends on owning Entity | Depends on Root | Independent |
| Functorium | `SimpleValueObject<T>` | `Entity<TId>` | `AggregateRoot<TId>` |

### Practical Example Classification

| Domain Concept | Classification | Rationale |
|------------|------|------|
| **Customer** | Aggregate Root | Independent lifecycle, own invariants (Email validity, CreditLimit), has Repository |
| **Product** | Aggregate Root | Independent lifecycle, own invariants (Tag duplication prevention), manages child Entity (Tag) |
| **Inventory** | Aggregate Root | Independent lifecycle, own invariants (stock >= 0), IConcurrencyAware concurrency control |
| **Order** | Aggregate Root | Independent lifecycle, Cross-Aggregate reference (ProductId), own invariants (TotalAmount calculation) |
| **Tag** | Child Entity | Has own ID, but accessed only through Aggregate Root (Product). No independent Repository |
| **Money** | Value Object | No identifier, value-based equality, immutable |
| **Email** | Value Object | No identifier, value-based equality, immutable |
| **Quantity** | Value Object | No identifier, value-based equality, immutable |
| **ShippingAddress** | Value Object | No identifier, value-based equality, immutable |

We have confirmed the classification criteria and decision flow. In the next section, we will analyze actual Aggregates in LayeredArch.Domain and examine practical examples of boundary setting.

---

## Practical Examples of Aggregate Boundary Setting

We analyze three Aggregates in LayeredArch.Domain.

### Customer Aggregate: Simple Aggregate with Root Only

```
┌─────────────────────────────────┐
│  Customer Aggregate             │
│                                 │
│  ┌──────────────────┐           │
│  │ Customer (Root)  │           │
│  │  - CustomerName  │ ← VO      │
│  │  - Email         │ ← VO      │
│  │  - Money         │ ← VO      │
│  └──────────────────┘           │
│                                 │
└─────────────────────────────────┘
```

**Invariants:**
- CustomerName, Email, CreditLimit are each self-validated by their Value Objects

**Boundary Rationale:**
- Customer has an independent lifecycle
- The simplest form of Aggregate with no child Entities
- Connected to Order only via ID reference (Order does not own `CustomerId` -- in this example, Order references `ProductId`)

```csharp
[GenerateEntityId]
public sealed class Customer : AggregateRoot<CustomerId>, IAuditable
{
    public CustomerName Name { get; private set; }
    public Email Email { get; private set; }
    public Money CreditLimit { get; private set; }

    public static Customer Create(
        CustomerName name,
        Email email,
        Money creditLimit)
    {
        var customer = new Customer(CustomerId.New(), name, email, creditLimit);
        customer.AddDomainEvent(new CreatedEvent(customer.Id, name, email));
        return customer;
    }
}
```

### Product + Inventory Aggregate: Separating Catalog and Stock

This is a case where stock (high-frequency changes) was separated into its own Aggregate to reduce concurrency conflicts.

```
┌──────────────────────────────────────┐  ┌─────────────────────────────┐
│  Product Aggregate (Catalog)             │  │  Inventory Aggregate (Stock)  │
│                                      │  │                             │
│  ┌────────────────────┐              │  │  ┌──────────────────────┐   │
│  │ Product (Root)     │              │  │  │ Inventory (Root)     │   │
│  │  - ProductName     │ <- VO         │  │  │  - ProductId         │ ID ref│
│  │  - ProductDesc     │ ← VO         │  │  │  - Quantity          │ ← VO │
│  │  - Money (Price)   │ <- VO         │  │  │  - RowVersion        │ concur│
│  └────────┬───────────┘              │  │  └──────────────────────┘   │
│           │ 1:N                      │  │                             │
│  ┌────────┴───────────┐              │  └─────────────────────────────┘
│  │ Tag (Child Entity) │              │
│  │  - TagName         │ ← VO         │
│  └────────────────────┘              │
│                                      │
└──────────────────────────────────────┘
```

**Product Invariants:**
- Tag duplication prevention (checked by ID in `AddTag`)

**Inventory Invariants:**
- Stock quantity >= 0 (protected in `DeductStock`, `IConcurrencyAware` optimistic concurrency)

**Boundary Rationale:**
- Product manages the lifecycle of Tags (Tags cannot exist without Product)
- Stock changes with every order (high frequency) but catalog changes are infrequent -> separate Aggregates
- Inventory references Product by `ProductId` (ID reference, not object reference)

```csharp
// Product: Catalog information management
[GenerateEntityId]
public sealed class Product : AggregateRoot<ProductId>, IAuditable
{
    private readonly List<Tag> _tags = [];
    public IReadOnlyList<Tag> Tags => _tags.AsReadOnly();

    // Invariant protection: prevent Tag duplication
    public Product AddTag(Tag tag)
    {
        if (_tags.Any(t => t.Id == tag.Id))
            return this;

        _tags.Add(tag);
        AddDomainEvent(new TagAssignedEvent(tag.Id, tag.Name));
        return this;
    }
}

// Inventory: Stock management (optimistic concurrency control)
[GenerateEntityId]
public sealed class Inventory : AggregateRoot<InventoryId>, IAuditable, IConcurrencyAware
{
    #region Error Types

    public sealed record InsufficientStock : DomainErrorKind.Custom;

    #endregion

    public ProductId ProductId { get; private set; }
    public Quantity StockQuantity { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    // Invariant protection: stock >= 0
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
}
```

#### Aggregate Split/Merge Decisions

When signals appear that Aggregate boundaries are not appropriate in an operating system, consider splitting or merging.

**Split Signals** -- Consider splitting if any of the following apply. The most common signal is frequent concurrency conflicts.

| Signal | Symptom | Example |
|------|------|------|
| Frequent concurrency conflicts | Repeated `DbUpdateConcurrencyException` | Full Product lock on every order |
| Change frequency imbalance | Only some attributes change frequently | Catalog (low freq) vs Stock (high freq) |
| Invariant independence | No interdependent invariants between attribute groups | Price changes do not affect stock rules |

**Merge Signals** -- Consider merging if **all** of the following conditions apply:

| Signal | Symptom | Example |
|------|------|------|
| Always changed together | Two Aggregates always modified simultaneously in same Usecase | When A is modified, B must be too |
| Mutual invariant dependency | A invariant depends on B state | Aggregate constraint |
| Separate transactions impossible | Eventual consistency cannot meet business needs | Immediate consistency required |

#### Split Case: Product -> Product + Inventory

**Before** -- Single Product Aggregate:

```
┌────────────────────────────────────┐
│  Product Aggregate                 │
│                                    │
│  ProductName, Description, Price   │  <- Low-freq changes (admin)
│  StockQuantity                     │  <- High-freq changes (every order)
│  DeductStock(), HasLowStock()      │
│                                    │
│  Problem: Full Product concurrency  │
│  conflicts during order processing   │
└────────────────────────────────────┘
```

The Product + Inventory diagram above shows the result after splitting.

**Split Rationale:**
- Catalog info (Name, Description, Price) and stock (StockQuantity) are **invariant-independent** -- price changes do not affect stock rules
- Stock changes every order (high freq), catalog only by admins (low freq) -- **change frequency imbalance**
- After separation, `IConcurrencyAware` (RowVersion) applied only to Inventory -- detects only stock conflicts

**Connection Method:**
- Inventory references Product by `ProductId` via **ID reference** (not object reference, see [Cross-Aggregate Relationships](../06c-entity-aggregate-advanced#cross-aggregate-relationships))
- When creating Product in Application Layer, Inventory is also created (same Usecase)
- Stock deduction is requested directly to Inventory Aggregate

#### Transaction Boundary Practical Guidelines

The principle from [Section 1](#aggregate-as-a-transaction-boundary) is **one transaction = one Aggregate change.** In practice, patterns are classified as follows.

**Pattern Classification:**

| Pattern | Allowed | Example | Rationale |
|------|------|------|------|
| Single Aggregate change | ✅ | `DeductStockCommand`: Changes only Inventory | Follows principle |
| Read + single Aggregate change | ✅ | `CreateOrderCommand`: Read Product -> Create Order | Reads cause no contention |
| Concurrent creation (same BC) | Exception allowed | `CreateProductCommand`: Create Product + Inventory simultaneously | See conditions below |
| Concurrent change (existing) | ❌ | Order creation + Inventory deduction during order processing | Concurrency conflict risk |

**Conditions for Allowing Concurrent Creation Exception** -- **All** of the following must be met:

1. **Within the same Bounded Context**: Do not create Aggregates from different BCs simultaneously
2. **Only at creation time**: New Aggregate creation, not existing Aggregate state change
3. **No mutual invariants**: No invariants between the two Aggregates that depend on each other's state

The key point to note in the following code is that while Product and Inventory can be created simultaneously, changing the state of an existing Aggregate while simultaneously creating another Aggregate is prohibited.

```csharp
// ✅ Concurrent creation allowed: Product + Inventory (CreateProductCommand)
// - Same BC, creation time, no mutual invariants
FinT<IO, Response> usecase =
    from exists in _productRepository.Exists(new ProductNameUniqueSpec(productName))
    from _ in guard(!exists, /* ... */)
    from createdProduct in _productRepository.Create(product)
    from createdInventory in _inventoryRepository.Create(
        Inventory.Create(createdProduct.Id, stockQuantity))
    select new Response(/* ... */);
```

```csharp
// ❌ Concurrent change prohibited: Order creation + Inventory deduction
// - Inventory is an existing Aggregate state change -> must be handled in separate transaction
FinT<IO, Response> usecase =
    from inventory in _inventoryRepository.GetByProductId(productId)
    from _1 in inventory.DeductStock(quantity)        // Existing Aggregate change!
    from updated in _inventoryRepository.Update(inventory)
    from order in _orderRepository.Create(
        Order.Create(productId, quantity, unitPrice, shippingAddress))  // Simultaneously creating another Aggregate
    select new Response(/* ... */);
```

#### Concurrency Considerations

The `IConcurrencyAware` interface is selectively applied to high-contention Aggregates.

```csharp
// Implementing IConcurrencyAware on Aggregate Root
public sealed class Inventory : AggregateRoot<InventoryId>, IAuditable, IConcurrencyAware
{
    public byte[] RowVersion { get; private set; } = [];
    // ...
}
// See 13-adapters.md for EF Core Configuration and Mapper mapping
```

**Application Decision Criteria:**

| Situation | IConcurrencyAware Applied | Reason |
|------|----------------------|------|
| Stock deduction (order processing) | **Applied** | Multiple users deducting simultaneously |
| Catalog info modification | Not needed | Only admins, low frequency |
| Order status change | Depends | Evaluate concurrent state change possibility |
| Customer info modification | Not needed | Only self-modified, low conflict risk |

#### Concurrency Conflict Handling Strategy

When a concurrency conflict occurs in an Aggregate with `IConcurrencyAware` applied, it is handled with the following flow.

**Error Flow:**

```
Request -> Handler -> UoW.SaveChanges()
                        │
                        ├─ Success -> Normal response
                        │
                        └─ DbUpdateConcurrencyException
                              → AdapterError("ConcurrencyConflict")
                              → Pipeline
                              -> Error response (delegated to client)
```

**Current Strategy: Fail-Fast**

```csharp
// EfCoreUnitOfWork: Converts concurrency exception to AdapterError, returns without retry
// Error type definition: public sealed record ConcurrencyConflict : AdapterErrorKind.Custom;
public virtual FinT<IO, Unit> SaveChanges(CancellationToken cancellationToken = default)
{
    return IO.liftAsync(async () =>
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Fin.Succ(unit);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return AdapterError.FromException<EfCoreUnitOfWork>(
                new ConcurrencyConflict(), ex);
        }
    });
}
```

**Strategy Comparison:**

| Strategy | Implementation | Suitable Situations |
|------|------|-------------|
| **Fail-Fast** (current) | Immediately returns error on conflict, client decides retry | Low conflict frequency, client has retry logic |
| **Application retry** (not implemented) | Auto-retry N times in Handler then fail | High conflict frequency, retry always safe for idempotent operations (e.g., operations with same side effects like query-then-update) |

**Fail-Fast Selection Rationale:**

- Handlers **focus on business logic** -- retry policy is an infrastructure concern
- Whether retry is safe (idempotency) differs per Usecase -- blanket auto-retry is risky
- If conflict frequency increases, consider Aggregate splitting first (resolving root cause)

### Order Aggregate: Cross-Aggregate Reference + Value Calculation

```
┌──────────────────────────────────────┐
│  Order Aggregate                     │
│                                      │
│  ┌───────────────────┐               │
│  │ Order (Root)      │               │
│  │  - ProductId ─────────→ Product Aggregate (ID ref)
│  │  - Quantity       │ ← VO          │
│  │  - Money (Unit)   │ ← VO          │
│  │  - Money (Total)  │ <- VO (calculated)  │
│  │  - ShippingAddr   │ ← VO          │
│  └───────────────────┘               │
│                                      │
└──────────────────────────────────────┘
```

**Invariants:**
- TotalAmount = UnitPrice x Quantity (calculated at creation)

**Boundary Rationale:**
- Order has an independent lifecycle
- References Product Aggregate only by `ProductId` (no object reference)
- Product validation (`IProductCatalog`) is performed in Application Layer before Order creation

```csharp
[GenerateEntityId]
public sealed class Order : AggregateRoot<OrderId>, IAuditable
{
    // Cross-Aggregate reference: store only ID
    public ProductId ProductId { get; private set; }

    public Quantity Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money TotalAmount { get; private set; }
    public ShippingAddress ShippingAddress { get; private set; }

    public static Order Create(
        ProductId productId,
        Quantity quantity,
        Money unitPrice,
        ShippingAddress shippingAddress)
    {
        // Invariant: TotalAmount = UnitPrice × Quantity
        var totalAmount = unitPrice.Multiply(quantity);
        var order = new Order(OrderId.New(), productId, quantity, unitPrice, totalAmount, shippingAddress);
        order.AddDomainEvent(new CreatedEvent(order.Id, productId, quantity, totalAmount));
        return order;
    }
}
```

---

## Anti-Patterns

### God Aggregate

The mistake of putting everything related into a single Aggregate.

```csharp
// ❌ God Aggregate
public class Customer : AggregateRoot<CustomerId>
{
    public CustomerName Name { get; private set; }
    public List<Order> Orders { get; }           // Should be a separate Aggregate
    public List<Product> WishList { get; }       // Should be a separate Aggregate
    public List<Review> Reviews { get; }         // Should be a separate Aggregate
    public List<PaymentMethod> Payments { get; } // Should be a separate Aggregate
}
```

```csharp
// ✅ Small Aggregate + ID reference
public sealed class Customer : AggregateRoot<CustomerId>
{
    public CustomerName Name { get; private set; }
    public Email Email { get; private set; }
    public Money CreditLimit { get; private set; }
    // Order, WishList etc. are each independent Aggregates
}
```

**Decision Criteria**: "Is this data absolutely necessary to protect the Aggregate Root's invariants?"

### Direct Entity References Between Aggregates

```csharp
// ❌ Direct Entity reference between Aggregates
public class Order : AggregateRoot<OrderId>
{
    public Product Product { get; private set; }    // Direct reference
    public Customer Customer { get; private set; }  // Direct reference
}
```

```csharp
// ✅ Reference by ID only
public sealed class Order : AggregateRoot<OrderId>
{
    public ProductId ProductId { get; private set; }   // ID reference
    // Use Domain Port when Customer info is needed
}
```

### Invariant Validation Outside Aggregates

```csharp
// ❌ Stock validation in Application Layer
public class DeductStockUsecase
{
    public async Task Handle(DeductStockCommand cmd)
    {
        var inventory = await _inventoryRepo.GetByProductId(cmd.ProductId);

        // Invariant validation is outside the Aggregate!
        if (inventory.StockQuantity < cmd.Quantity)
            throw new InsufficientStockException();

        inventory.StockQuantity -= cmd.Quantity;  // Direct modification!
    }
}
```

```csharp
// ✅ Invariant protection inside Aggregate Root
public class DeductStockUsecase
{
    public async Task Handle(DeductStockCommand cmd)
    {
        var inventory = await _inventoryRepo.GetByProductId(cmd.ProductId);

        // State change through Aggregate Root method
        var result = inventory.DeductStock(cmd.Quantity);
        // Handle error if result is Fail
    }
}
```

### Making Everything an Aggregate Root

```csharp
// ❌ Unnecessarily making Tag an Aggregate Root
public class Tag : AggregateRoot<TagId>
{
    public TagName Name { get; private set; }
    // Tag does not need independent query/save
    // Accessing through Product is sufficient
}
```

```csharp
// ✅ Tag is sufficient as a child Entity
public sealed class Tag : Entity<TagId>
{
    public TagName Name { get; private set; }
}
```

**Decision Criteria**: "Does this Entity need an independent Repository?"

---

## Troubleshooting

### Frequent `DbUpdateConcurrencyException` Occurrences

**Cause:** A single Aggregate contains too much data, causing unrelated changes to lock the same Aggregate.

**Resolution:** Consider Aggregate splitting. Separating attribute groups with different change frequencies (e.g., catalog info vs stock) into separate Aggregates can reduce concurrency conflicts. Apply `IConcurrencyAware` selectively only to high-contention Aggregates.

### Attempting to Change Multiple Aggregates in a Single Transaction

**Cause:** Violating the "one transaction = one Aggregate change" principle. Changing multiple Aggregates simultaneously creates concurrency conflict risk and transaction scope expansion problems.

**Resolution:** Handle Cross-Aggregate changes via eventual consistency through domain events. Concurrent creation is exceptionally allowed only within the same BC, only at creation time, and only when there are no mutual invariants.

### Directly Modifying Child Entities Without Going Through the Aggregate Root

**Cause:** The Aggregate's invariants are being bypassed from outside. This occurs when child Entity collections are exposed as `public` or mutable types.

**Resolution:** Expose collections as `IReadOnlyList<T>` and ensure state changes are only performed through Aggregate Root methods. Refer to the `_tags.AsReadOnly()` pattern.

---

## FAQ

### Q1. What is the difference between Aggregate Root and regular Entity?

Aggregate Root inherits `AggregateRoot<TId>`, can publish domain events, and has an independent Repository. Regular Entity inherits `Entity<TId>`, is accessible only through the Aggregate Root, and has no independent Repository.

| Characteristic | Aggregate Root | Regular Entity |
|------|---------------|------------|
| Base class | `AggregateRoot<TId>` | `Entity<TId>` |
| Domain events | Can publish | Cannot |
| Repository | Present | None |
| External access | Direct | Through Root only |

### Q2. How do you determine Aggregate boundaries?

Key question: "Is this Entity independently stored/queried?" If an independent lifecycle is needed, it is an Aggregate Root; if it depends on another Root, it is a child Entity. Additionally, ask "Is this data absolutely necessary to protect the Root's invariants?" to determine inclusion.

### Q3. Does using eventual consistency via domain events cause data inconsistency?

Eventual consistency, unlike immediate consistency, allows temporary inconsistency. Consistency is guaranteed once the event handler completes processing. Consider Aggregate merging only when business requirements absolutely require immediate consistency.

### Q4. Should `IConcurrencyAware` be applied to all Aggregates?

No. Apply it only to high-contention Aggregates where multiple users change simultaneously (e.g., stock deduction). It is unnecessary for Aggregates that only admins change infrequently (e.g., catalog info, customer info).

### Q5. Under what conditions is the concurrent creation exception allowed?

It is allowed within the same Bounded Context, only at new Aggregate creation time, and only when there are no mutual invariants between the two Aggregates. Simultaneously changing existing Aggregate state and creating/changing another Aggregate is prohibited.

---

## References

- [Entity/Aggregate Core Patterns (HOW)](../06b-entity-aggregate-core)
- [Entity/Aggregate Advanced Patterns](../06c-entity-aggregate-advanced)
