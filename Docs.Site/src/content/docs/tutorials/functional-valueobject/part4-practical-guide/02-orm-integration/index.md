---
title: "ORM Integration Patterns"
---
## Overview

In the domain model, `Email` is a strongly-typed object, but it must be stored as a `VARCHAR` column in the database. How do you map a composite value object like `Address(City, Street, PostalCode)`? What about collections like `List<OrderLineItem>` within an order?

In this chapter, we cover how to persist value objects while maintaining domain model purity, using three patterns provided by Entity Framework Core: `OwnsOne`, `OwnsMany`, and `Value Converter`.

## Learning Objectives

- Map composite value objects (Address, Money, etc.) as part of an entity using the `OwnsOne` pattern.
- Convert single value objects (Email, ProductCode, etc.) to database columns using the `Value Converter` pattern.
- Map value object collections (OrderLineItem, etc.) using the `OwnsMany` pattern.
- Design a structure that integrates with EF Core while maintaining domain model purity.

## Why Is This Needed?

There are several technical challenges when persisting value objects to a database.

In the domain, `Email` is a strongly-typed object, but it is stored as a `VARCHAR` column in the database. Manually handling this type conversion every time causes code duplication and mistakes. Separating composite value objects like `Address(City, Street, PostalCode)` into separate tables causes unnecessary joins, while storing them in the same table requires explicit column mapping. Additionally, value object collections like `List<OrderLineItem>` require a separate table but must be managed as owned types rather than entities.

EF Core's Owned Entity feature and Value Converters can transparently solve these challenges.

## Core Concepts

### OwnsOne Pattern

`OwnsOne` maps a value object as part of an entity. Each property of the value object is stored as a column in the parent table.

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Email value object: stored as Email_Value column in the User table
    modelBuilder.Entity<User>()
        .OwnsOne(u => u.Email);

    // Address composite value object: stored as Address_City, Address_Street, Address_PostalCode columns in the User table
    modelBuilder.Entity<User>()
        .OwnsOne(u => u.Address);
}
```

The data is stored as columns in the same table rather than a separate table, and is loaded together with the parent entity. The resulting table structure is as follows.

```
Users table
├── Id (PK)
├── Name
├── Email_Value          # Email mapped via OwnsOne
├── Address_City         # Address mapped via OwnsOne
├── Address_Street
└── Address_PostalCode
```

### Value Converter Pattern

`HasConversion` converts a value object into a single column. It defines bidirectional conversion from object to primitive value and from primitive value back to object.

```csharp
modelBuilder.Entity<Product>()
    .Property(p => p.Code)
    .HasConversion(
        code => code.Value,                           // On save: ProductCode -> string
        value => ProductCode.CreateFromValidated(value) // On load: string -> ProductCode
    );
```

Domain code works with the `ProductCode` type while the database stores it as a string. This conversion process is handled automatically at the ORM level. While `OwnsOne` stores each property of a value object as a separate column, `HasConversion` stores the entire value object as a single column.

### OwnsMany Pattern

`OwnsMany` maps value object collections. They are stored in a separate table but managed as owned types rather than entities.

```csharp
modelBuilder.Entity<Order>()
    .OwnsMany(o => o.LineItems);
```

`OrderLineItem` is stored in a separate table, but is deleted together when the `Order` is deleted. It has no independent lifecycle. The resulting table structure is as follows.

```
Orders table
├── Id (PK)
└── CustomerName

OrderLineItem table
├── OrderId (FK, part of PK)
├── Id (part of PK)
├── ProductName
├── Quantity
└── UnitPrice
```

### Private Constructors and EF Core Compatibility

Value objects use private constructors for immutability. A parameterless private constructor is needed to maintain compatibility with EF Core.

```csharp
public sealed class Email
{
    public string Value { get; private set; }

    // Private constructor for EF Core mapping
    private Email() => Value = string.Empty;

    // Private constructor for actual creation
    private Email(string value) => Value = value;

    public static Fin<Email> Create(string value) { ... }
}
```

EF Core creates objects using the parameterless constructor and then sets properties. Used with `private setter`, it blocks external modification while allowing ORM mapping.

## Practical Guidelines

### Expected Output
```
=== ORM Integration Patterns ===

1. OwnsOne Pattern - Composite Value Object Mapping
────────────────────────────────────────
   Saved user: Hong Gildong
   Email: hong@example.com
   Address: Seoul Gangnam-gu Teheran-ro 123 (06234)

2. Value Converter Pattern - Single Value Object Conversion
────────────────────────────────────────
   Product code: EL-001234
   Price: 50,000 KRW

3. OwnsMany Pattern - Collection Value Object Mapping
────────────────────────────────────────
   Customer: Kim Cheolsu
   Order items:
      - Product A: 2 x 10,000 won
      - Product B: 1 x 25,000 won
```

### DbContext Configuration Example

A `DbContext` configuration with all three mapping patterns applied.

```csharp
public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1. OwnsOne: Email value object
        modelBuilder.Entity<User>()
            .OwnsOne(u => u.Email);

        // 2. OwnsOne: Address composite value object
        modelBuilder.Entity<User>()
            .OwnsOne(u => u.Address);

        // 3. Value Converter: ProductCode
        modelBuilder.Entity<Product>()
            .Property(p => p.Code)
            .HasConversion(
                code => code.Value,
                value => ProductCode.CreateFromValidated(value));

        // 4. OwnsOne: Money
        modelBuilder.Entity<Product>()
            .OwnsOne(p => p.Price);

        // 5. OwnsMany: OrderLineItem collection
        modelBuilder.Entity<Order>()
            .OwnsMany(o => o.LineItems);
    }
}
```

## Project Description

### Project Structure
```
02-ORM-Integration/
├── OrmIntegration/
│   ├── Program.cs                # Main executable (includes value objects, entities, DbContext)
│   └── OrmIntegration.csproj     # Project file
└── README.md                     # Project documentation
```

### Dependencies
```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\..\..\Src\Functorium\Functorium.csproj" />
</ItemGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" />
</ItemGroup>
```

### Core Code

**Entity Definitions**
```csharp
public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Email Email { get; set; } = null!;       // Single value object
    public Address Address { get; set; } = null!;   // Composite value object
}

public class Product
{
    public Guid Id { get; set; }
    public ProductCode Code { get; set; } = null!;  // Uses Value Converter
    public Money Price { get; set; } = null!;       // Uses OwnsOne
}

public class Order
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<OrderLineItem> LineItems { get; set; } = new();  // Uses OwnsMany
}
```

**value object Definitions**
```csharp
// EF Core compatible value object
public sealed class Email
{
    public string Value { get; private set; }

    private Email() => Value = string.Empty;  // For EF Core
    private Email(string value) => Value = value;

    public static Fin<Email> Create(string value) { ... }
    public static Email CreateFromValidated(string value) => new(value.ToLowerInvariant());
}

// Composite value object
public sealed class Address
{
    public string City { get; private set; }
    public string Street { get; private set; }
    public string PostalCode { get; private set; }

    private Address()  // For EF Core
    {
        City = string.Empty;
        Street = string.Empty;
        PostalCode = string.Empty;
    }

    public Address(string city, string street, string postalCode)
    {
        City = city;
        Street = street;
        PostalCode = postalCode;
    }
}
```

## Summary at a Glance

### ORM Mapping Pattern Comparison

Compares the storage approaches and suitable value object types for the three patterns.

| Pattern | Storage Approach | Suitable value objects | Table Structure |
|---------|-----------------|----------------------|----------------|
| `OwnsOne` | Parent table columns | Email, Address, Money | Same table |
| `HasConversion` | Single column | ProductCode, UserId | Same table, 1 column |
| `OwnsMany` | Separate table | OrderLineItem | Child table |

### Pattern Selection Guide

Choose the appropriate mapping pattern based on the value object's structure.

| Situation | Recommended Pattern |
|-----------|-------------------|
| Single property value object | `HasConversion` or `OwnsOne` |
| Multi-property value object | `OwnsOne` |
| value object collection | `OwnsMany` |
| JSON serialization needed | `HasConversion` + JSON |

### EF Core Compatibility Checklist

Items to verify when integrating value objects with EF Core.

| Item | Description |
|------|-------------|
| Parameterless private constructor | Allows EF Core to create objects |
| Private setter | Allows EF Core mapping while maintaining immutability |
| `CreateFromValidated()` method | Used by Value Converter |
| Default value initialization | Prevents nullable warnings |

## FAQ

### Q1: How do I choose between OwnsOne and HasConversion?
**A**: `HasConversion` is suitable for single-property value objects that need conversion logic on load. Use `OwnsOne` for multi-property objects. With `OwnsOne`, a column is created per property, allowing individual properties to be used as query conditions.

### Q2: How do I make value objects with private constructors compatible with EF Core?
**A**: EF Core can invoke private constructors via Reflection. Provide a parameterless private constructor and use `private set` so EF Core can set values while blocking modifications from external code.

### Q3: How do I sort collections mapped with OwnsMany?
**A**: `OwnsMany` does not guarantee sort order by default. If ordering matters, explicitly add a sort column.

```csharp
modelBuilder.Entity<Order>()
    .OwnsMany(o => o.LineItems, builder =>
    {
        builder.Property<int>("Sequence");
        builder.HasKey("OrderId", "Sequence");
    });
```

---

## Tests

This project includes unit tests.

### Running Tests
```bash
cd OrmIntegration.Tests.Unit
dotnet test
```

### Test Structure
```
OrmIntegration.Tests.Unit/
├── OwnsOnePatternTests.cs       # OwnsOne mapping pattern tests
├── ValueConverterPatternTests.cs # Value Converter pattern tests
└── OwnsManyPatternTests.cs      # OwnsMany collection mapping tests
```

### Key Test Cases

| Test Class | Test Content |
|------------|-------------|
| OwnsOnePatternTests | Address, Email composite value object persistence |
| ValueConverterPatternTests | ProductCode single value conversion |
| OwnsManyPatternTests | OrderLineItem collection persistence |

Now that we have learned patterns for persisting value objects to the database, the next chapter covers how to integrate value objects with Commands/Queries in a CQRS architecture.

---

→ [3장: CQRS와 value object](../03-CQRS-Integration/)
