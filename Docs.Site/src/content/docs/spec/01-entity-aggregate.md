---
title: "Entity and Aggregate Specification"
---

This is the API specification for Entity and Aggregate related public types provided by the Functorium framework. For design principles and implementation patterns, see the [Entity and Aggregate Implementation Guide](../guides/domain/06b-entity-aggregate-core).

## Summary

### Key Types

| Type | Namespace | Description |
|------|-------------|------|
| `IEntity` | `Functorium.Domains.Entities` | Entity naming convention constant definitions |
| `IEntity<TId>` | `Functorium.Domains.Entities` | Entity base interface (ID-based equality contract) |
| `IEntityId<T>` | `Functorium.Domains.Entities` | Ulid-based Entity ID interface |
| `Entity<TId>` | `Functorium.Domains.Entities` | Entity base abstract class (equality, proxy support) |
| `AggregateRoot<TId>` | `Functorium.Domains.Entities` | Aggregate Root base abstract class (domain event management) |
| `GenerateEntityIdAttribute` | `Functorium.Domains.Entities` | EntityId source generator trigger attribute |
| `IAuditable` | `Functorium.Domains.Entities` | Created/modified timestamp tracking mixin |
| `IAuditableWithUser` | `Functorium.Domains.Entities` | Created/modified timestamp + user tracking mixin |
| `IConcurrencyAware` | `Functorium.Domains.Entities` | Optimistic concurrency control mixin |
| `ISoftDeletable` | `Functorium.Domains.Entities` | Soft delete mixin |
| `ISoftDeletableWithUser` | `Functorium.Domains.Entities` | Soft delete + deleter tracking mixin |
| `IDomainService` | `Functorium.Domains.Services` | Domain service marker interface |

---

## IEntity / IEntity\<TId\>

Interfaces defining the Entity contract.

### IEntity (Non-generic)

```csharp
public interface IEntity
{
    const string CreateMethodName = "Create";
    const string CreateFromValidatedMethodName = "CreateFromValidated";
}
```

| Constant | Value | Description |
|------|----|------|
| `CreateMethodName` | `"Create"` | Factory method name for creating a new Entity |
| `CreateFromValidatedMethodName` | `"CreateFromValidated"` | Method name for restoring an Entity from validated data (for Repository/ORM) |

### IEntity\<TId\>

```csharp
public interface IEntity<TId> : IEntity
    where TId : struct, IEntityId<TId>
{
    TId Id { get; }
}
```

| Property | Type | Description |
|------|------|------|
| `Id` | `TId` | Unique identifier of the Entity |

**Generic constraint:** `TId` must be a `struct` and implement `IEntityId<TId>`.

---

## IEntityId\<T\>

Interface for Ulid-based Entity IDs. Supports time-ordered sorting and inherits `IEquatable<T>`, `IComparable<T>`, `IParsable<T>`.

```csharp
public interface IEntityId<T> : IEquatable<T>, IComparable<T>, IParsable<T>
    where T : struct, IEntityId<T>
{
    Ulid Value { get; }

    static abstract T New();
    static abstract T Create(Ulid id);
    static abstract T Create(string id);
}
```

| Member | Return Type | Description |
|------|----------|------|
| `Value` | `Ulid` | The Ulid value |
| `New()` | `T` | Creates a new EntityId (static abstract) |
| `Create(Ulid id)` | `T` | Creates an EntityId from a Ulid (static abstract) |
| `Create(string id)` | `T` | Creates an EntityId from a string (static abstract). Throws `FormatException` for invalid formats |

---

## Entity\<TId\>

Base abstract class for Entity providing ID-based equality comparison. Also handles ORM proxy types (Castle, NHibernate, EF Core Proxies).

```csharp
[Serializable]
public abstract class Entity<TId> : IEntity<TId>, IEquatable<Entity<TId>>
    where TId : struct, IEntityId<TId>
```

### Properties

| Property | Type | Accessor | Description |
|------|------|--------|------|
| `Id` | `TId` | `public get; protected init` | Unique identifier of the Entity |

### Constructors

| Signature | Access Level | Description |
|----------|----------|------|
| `Entity()` | `protected` | Default constructor (for ORM/serialization) |
| `Entity(TId id)` | `protected` | Creates an Entity with a specified ID |

### Methods

| Method | Return Type | Access Level | Description |
|--------|----------|----------|------|
| `Equals(object? obj)` | `bool` | `public` | ID-based equality comparison (proxy-type aware) |
| `Equals(Entity<TId>? other)` | `bool` | `public` | Type-safe equality comparison |
| `GetHashCode()` | `int` | `public` | ID-based hash code |
| `operator ==(Entity<TId>?, Entity<TId>?)` | `bool` | `public static` | Equality operator |
| `operator !=(Entity<TId>?, Entity<TId>?)` | `bool` | `public static` | Inequality operator |
| `CreateFromValidation<TEntity, TValue>(Validation<Error, TValue>, Func<TValue, TEntity>)` | `Fin<TEntity>` | `public static` | Factory helper using LanguageExt Validation |
| `GetUnproxiedType(object obj)` | `Type` | `protected static` | Strips ORM proxy and returns the actual type |

### Minimal Usage Example

```csharp
[GenerateEntityId]
public class Product : Entity<ProductId>
{
#pragma warning disable CS8618
    private Product() { }
#pragma warning restore CS8618

    private Product(ProductId id, ProductName name) : base(id)
    {
        Name = name;
    }

    public ProductName Name { get; private set; }

    public static Product Create(ProductName name)
        => new(ProductId.New(), name);

    public static Product CreateFromValidated(ProductId id, ProductName name)
        => new(id, name);
}
```

---

## AggregateRoot\<TId\>

Base abstract class for Aggregate Root providing domain event management. Inherits **Entity\<TId\>** and implements `IDomainEventDrain` (internal).

```csharp
public abstract class AggregateRoot<TId> : Entity<TId>, IDomainEventDrain
    where TId : struct, IEntityId<TId>
```

### Properties

| Property | Type | Accessor | Description |
|------|------|--------|------|
| `DomainEvents` | `IReadOnlyList<IDomainEvent>` | `public get` | Domain event list (read-only) |

### Constructors

| Signature | Access Level | Description |
|----------|----------|------|
| `AggregateRoot()` | `protected` | Default constructor (for ORM/serialization) |
| `AggregateRoot(TId id)` | `protected` | Creates an Aggregate Root with a specified ID |

### Methods

| Method | Return Type | Access Level | Description |
|--------|----------|----------|------|
| `AddDomainEvent(IDomainEvent domainEvent)` | `void` | `protected` | Adds a domain event |
| `ClearDomainEvents()` | `void` | `public` | Removes all domain events (`IDomainEventDrain` implementation) |

### Interface Separation

**AggregateRoot\<TId\>** separates two interfaces for domain events.

| Interface | Access Level | Role |
|-----------|----------|------|
| `IHasDomainEvents` | `public` | Event read-only access (`DomainEvents` property) |
| `IDomainEventDrain` | `internal` | Event cleanup (`ClearDomainEvents()`) -- infrastructure concern |

### Minimal Usage Example

```csharp
[GenerateEntityId]
public class Order : AggregateRoot<OrderId>
{
#pragma warning disable CS8618
    private Order() { }
#pragma warning restore CS8618

    private Order(OrderId id, Money totalAmount) : base(id)
    {
        TotalAmount = totalAmount;
        Status = OrderStatus.Pending;
    }

    public Money TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }

    public static Order Create(Money totalAmount)
    {
        var id = OrderId.New();
        var order = new Order(id, totalAmount);
        order.AddDomainEvent(new OrderCreatedEvent(id, totalAmount));
        return order;
    }

    public Fin<Unit> Confirm()
    {
        if (!Status.CanTransitionTo(OrderStatus.Confirmed))
            return Fin<Unit>.Fail(Error.New("Cannot confirm order"));

        Status = OrderStatus.Confirmed;
        AddDomainEvent(new OrderConfirmedEvent(Id));
        return unit;
    }
}
```

---

## GenerateEntityIdAttribute

When applied to an Entity class, the source generator automatically generates EntityId-related types.

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GenerateEntityIdAttribute : Attribute;
```

### Generated Types

Applying `[GenerateEntityId]` to the `Product` class generates the following types.

| Generated Type | Kind | Description |
|----------|------|------|
| `ProductId` | `readonly partial record struct` | Ulid-based EntityId (implements `IEntityId<ProductId>`) |
| `ProductIdComparer` | `sealed class` | EF Core `ValueComparer<ProductId>` (for change tracking) |
| `ProductIdConverter` | `sealed class` | EF Core `ValueConverter<ProductId, string>` (string conversion for DB storage) |

### Generated ProductId Members

| Member | Type/Return | Description |
|------|----------|------|
| `Name` | `const string` | Type name constant (`"ProductId"`) |
| `Namespace` | `const string` | Namespace constant |
| `Empty` | `static readonly ProductId` | Empty value (based on `Ulid.Empty`) |
| `Value` | `Ulid { get; init; }` | The Ulid value |
| `New()` | `static ProductId` | Creates a new ID |
| `Create(Ulid id)` | `static ProductId` | Creates from a Ulid |
| `Create(string id)` | `static ProductId` | Creates from a string (`FormatException` possible) |
| `CompareTo(ProductId other)` | `int` | Ulid-based comparison |
| `<`, `>`, `<=`, `>=` | `bool` | Comparison operators |
| `Parse(string, IFormatProvider?)` | `static ProductId` | `IParsable<T>` implementation |
| `TryParse(string?, IFormatProvider?, out ProductId)` | `static bool` | `IParsable<T>` implementation |
| `ToString()` | `string` | Ulid string representation |

Generated EntityIds automatically have `[JsonConverter]` and `[TypeConverter]` attributes applied, supporting JSON serialization and type conversion.

### Usage Example

```csharp
// Create a new ID
var productId = ProductId.New();

// Convert from string
var parsed = ProductId.Create("01ARZ3NDEKTSV4RRFFQ69G5FAV");

// Comparison
bool isNewer = productId > parsed;

// EF Core configuration
builder.Property(x => x.Id)
    .HasConversion(new ProductIdConverter())
    .Metadata.SetValueComparer(new ProductIdComparer());
```

---

## Mixin Interfaces

Interfaces that can be optionally mixed into Entity or Aggregate Root to add cross-cutting concerns.

### IAuditable

Tracks creation/modification timestamps.

```csharp
public interface IAuditable
{
    DateTime CreatedAt { get; }
    Option<DateTime> UpdatedAt { get; }
}
```

| Property | Type | Description |
|------|------|------|
| `CreatedAt` | `DateTime` | Creation timestamp |
| `UpdatedAt` | `Option<DateTime>` | Last modification timestamp (`None` if never modified) |

### IAuditableWithUser

Extends **IAuditable** to additionally track user information.

```csharp
public interface IAuditableWithUser : IAuditable
{
    Option<string> CreatedBy { get; }
    Option<string> UpdatedBy { get; }
}
```

| Property | Type | Description |
|------|------|------|
| `CreatedBy` | `Option<string>` | Creator identifier |
| `UpdatedBy` | `Option<string>` | Last modifier identifier |

### IConcurrencyAware

Manages row version for optimistic concurrency control. Maps to EF Core's `[Timestamp]`/`IsRowVersion()`.

```csharp
public interface IConcurrencyAware
{
    byte[] RowVersion { get; }
}
```

| Property | Type | Description |
|------|------|------|
| `RowVersion` | `byte[]` | Row version for optimistic concurrency control |

### ISoftDeletable

Supports soft delete. `IsDeleted` provides a default implementation (default interface method) derived from `DeletedAt`.

```csharp
public interface ISoftDeletable
{
    Option<DateTime> DeletedAt { get; }
    bool IsDeleted => DeletedAt.IsSome;
}
```

| Property | Type | Description |
|------|------|------|
| `DeletedAt` | `Option<DateTime>` | Deletion timestamp (`None` if not deleted) |
| `IsDeleted` | `bool` | Whether deleted (derived from `DeletedAt.IsSome`, default implementation) |

### ISoftDeletableWithUser

Extends **ISoftDeletable** to additionally track deleter information.

```csharp
public interface ISoftDeletableWithUser : ISoftDeletable
{
    Option<string> DeletedBy { get; }
}
```

| Property | Type | Description |
|------|------|------|
| `DeletedBy` | `Option<string>` | Deleter identifier |

### Mixin Application Example

```csharp
[GenerateEntityId]
public class Product : AggregateRoot<ProductId>, IAuditableWithUser, ISoftDeletable, IConcurrencyAware
{
    public DateTime CreatedAt { get; private set; }
    public Option<DateTime> UpdatedAt { get; private set; }
    public Option<string> CreatedBy { get; private set; }
    public Option<string> UpdatedBy { get; private set; }
    public Option<DateTime> DeletedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
}
```

---

## IDomainService

A marker interface for expressing domain logic that spans multiple Aggregates.

```csharp
public interface IDomainService { }
```

### Design Rules

| Rule | Description |
|------|------|
| Stateless | Does not maintain mutable state between calls (Evans Blue Book Ch.9) |
| Default pattern | Implemented as pure functions (no external I/O) |
| Repository dependency allowed | May depend on Repository interfaces for large-scale cross-data queries |
| Port/Adapter forbidden | No `IObservablePort` dependency (Port/Adapter is used in Usecases) |
| Location | Domain Layer |

### Usage Example

```csharp
public sealed class PricingService : IDomainService
{
    public static Fin<Money> CalculateDiscount(
        Money originalPrice,
        DiscountRate rate,
        CustomerGrade grade)
    {
        // Pure function logic that references values from multiple Aggregates
        var discount = originalPrice.Value * rate.Value * grade.Multiplier;
        return Money.Create(originalPrice.Value - discount);
    }
}
```

---

## Related Documents

- [Entity and Aggregate Implementation -- Core Patterns](../guides/domain/06b-entity-aggregate-core) -- Creation patterns, command methods, child Entity management
- [Aggregate Design Principles](../guides/domain/06a-aggregate-design) -- Aggregate boundaries and design principles
- [Entity and Aggregate Implementation -- Advanced Patterns](../guides/domain/06c-entity-aggregate-advanced) -- Cross-Aggregate relationships, mixin practical examples
- [Domain Events Specification](./09-domain-events) -- `IDomainEvent`, `DomainEvent`, Publisher/Collector
- [Source Generators Specification](./10-source-generators) -- Detailed EntityId generator specification
