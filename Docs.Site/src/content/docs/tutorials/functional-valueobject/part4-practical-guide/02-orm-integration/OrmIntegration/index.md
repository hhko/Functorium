---
title: "ORM Integration Patterns"
---
## Overview

Learn patterns for integrating Entity Framework Core with value objects. Covers OwnsOne, OwnsMany, and Value Converter patterns.

---

## Learning Objectives

- Map value objects with EF Core's `OwnsOne` pattern
- Map collection value objects with the `OwnsMany` pattern
- Simple conversion with `ValueConverter`
- Saving and loading value objects to/from database

---

## How to Run

```bash
cd Docs/tutorials/Functional-ValueObject/04-practical-guide/02-ORM-Integration/OrmIntegration
dotnet run
```

---

## Expected Output

```
=== ORM Integration Patterns ===

1. OwnsOne Pattern - Composite Value Object Mapping
────────────────────────────────────────
   Saved user: John Doe
   Email: john@example.com
   Address: Seoul Gangnam-gu Teheran-ro 123 (06234)

2. Value Converter Pattern - Single Value Object Conversion
────────────────────────────────────────
   Product code: EL-001234
   Price: 50,000 KRW

3. OwnsMany Pattern - Collection Value Object Mapping
────────────────────────────────────────
   Customer: Jane Smith
   Order items:
      - Product A: 2 x 10,000
      - Product B: 1 x 25,000
```

---

## Core Code Explanation

### 1. OwnsOne Pattern

```csharp
modelBuilder.Entity<User>()
    .OwnsOne(u => u.Address, address =>
    {
        address.Property(a => a.City).HasColumnName("City");
        address.Property(a => a.Street).HasColumnName("Street");
        address.Property(a => a.PostalCode).HasColumnName("PostalCode");
    });
```

**Table structure:**
```
Users
├── Id (PK)
├── Name
├── Email       ← OwnsOne (single column)
├── City        ← OwnsOne Address
├── Street      ← OwnsOne Address
└── PostalCode  ← OwnsOne Address
```

### 2. Value Converter Pattern

```csharp
modelBuilder.Entity<Product>()
    .Property(p => p.Code)
    .HasConversion(
        code => code.Value,              // When saving
        value => ProductCode.CreateFromValidated(value)  // When loading
    );
```

### 3. OwnsMany Pattern

```csharp
modelBuilder.Entity<Order>()
    .OwnsMany(o => o.LineItems, lineItem =>
    {
        lineItem.Property(l => l.ProductName);
        lineItem.Property(l => l.Quantity);
        lineItem.Property(l => l.UnitPrice);
    });
```

**Table structure:**
```
Orders                  OrderLineItems
├── Id (PK)            ├── OrderId (FK)
└── CustomerName       ├── ProductName
                       ├── Quantity
                       └── UnitPrice
```

---

## Pattern Selection Guide

| Pattern | When to Use | Pros | Cons |
|------|----------|------|------|
| OwnsOne | Composite value object | Stored in same table | Column count increases |
| OwnsMany | Collection value object | Normalized structure | Separate table required |
| ValueConverter | Single value object | Simple conversion | Cannot handle composite types |

## FAQ

### Q1: Should I use `OwnsOne` or `ValueConverter`?
**A**: `ValueConverter` is simpler for value objects with a single property (`Email`, `ProductCode`, etc.). Use `OwnsOne` for composite value objects with multiple properties (`Address`, `Money`, etc.) to map each property to a separate column.

### Q2: Why is `CreateFromValidated` used when loading with `ValueConverter`?
**A**: Values stored in the database have already passed validation, so there is no need to validate again. Using `Create` would incur unnecessary validation cost and require `Fin<T>` unwrapping. `CreateFromValidated` skips validation and creates the instance directly.

### Q3: Are there performance issues since `OwnsMany` creates a separate table?
**A**: Query performance may be slightly lower than a single table since JOINs are required. However, EF Core automatically manages the relationships, and in practice it rarely becomes a problem if the collection size is not large. For performance-critical queries, it is common to use a separate read model.

---

## Next Steps

Learn CQRS integration patterns.

-> [4.3 CQRS Integration](../../03-CQRS-Integration/CqrsIntegration/)
