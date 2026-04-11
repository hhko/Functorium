---
title: "Entity Interfaces"
---
## Overview

What happens if you manually implement "when was it created," "when was it modified," and "who deleted it" in every Entity? The same code repeats across all Entities, making omissions and inconsistencies likely.

Functorium provides these common concerns as **separated interfaces**. `IAuditable` provides creation/modification timestamps, `ISoftDeletable` provides logical deletion, and `ISoftDeletableWithUser` adds deletion actor tracking. This chapter creates a Product Entity implementing these interfaces to practice each interface's role.

---

## Learning Objectives

After completing this chapter, you will be able to:

1. **Implement** creation/modification timestamp tracking with `IAuditable` using `CreatedAt` and `UpdatedAt`
2. **Apply** the logical deletion pattern with `ISoftDeletable` / `ISoftDeletableWithUser` instead of physical deletion
3. **Declaratively express** an Entity's concerns by combining multiple interfaces

### What You Will Verify Through Hands-on Practice
- **Product**: An Entity implementing both `IAuditable` and `ISoftDeletableWithUser`
- **UpdatePrice()**: Automatic `UpdatedAt` refresh on price changes
- **Delete() / Restore()**: Soft delete and restore behavior

---

## Core Concepts

### Why Is This Needed?

Creation time, modification time, and soft delete are common concerns needed by most Entities. Separating them into interfaces allows declaring "this Entity supports audit tracking" in the type system and enables automatic handling in the infrastructure layer.

### IAuditable Interface

```csharp
public interface IAuditable
{
    DateTime CreatedAt { get; }
    Option<DateTime> UpdatedAt { get; }
}
```

- `CreatedAt`: Entity creation time (set only once)
- `UpdatedAt`: Last modification time (`Option<DateTime>` to express unmodified state)

Wondering why `UpdatedAt` is `Option<DateTime>` instead of `DateTime?`? It prevents null reference errors and enables safe handling through pattern matching.

### ISoftDeletable / ISoftDeletableWithUser Interfaces

A pattern that marks records as "deleted" instead of physical DELETE. Data is preserved while behaving as if deleted.

```csharp
public interface ISoftDeletable
{
    Option<DateTime> DeletedAt { get; }
    bool IsDeleted => DeletedAt.IsSome;  // Default implementation
}

public interface ISoftDeletableWithUser : ISoftDeletable
{
    Option<string> DeletedBy { get; }
}
```

- `DeletedAt`: Deletion time (None if not deleted)
- `IsDeleted`: Convenience property derived from `DeletedAt`
- `DeletedBy`: Identifier of the user who performed the deletion

### Soft Delete Pattern

Here's how it works in practice:

```csharp
// Delete
product.Delete("admin@example.com");
// DeletedAt = Some(2025-01-01T12:00:00), DeletedBy = Some("admin@example.com")

// Restore
product.Restore();
// DeletedAt = None, DeletedBy = None, UpdatedAt = Some(...)
```

Since data remains after deletion, it can be restored at any time, and the deletion actor can be tracked.

---

## Project Description

### Project Structure
```
EntityInterfaces/
├── Program.cs                  # IAuditable, ISoftDeletableWithUser demo
├── ProductId.cs                # Ulid-based identifier
├── Product.cs                  # IAuditable + ISoftDeletableWithUser implementation
└── EntityInterfaces.csproj

EntityInterfaces.Tests.Unit/
├── ProductTests.cs             # Timestamp tracking, soft delete tests
├── Using.cs
├── xunit.runner.json
└── EntityInterfaces.Tests.Unit.csproj
```

### Core Code

#### Product.cs

Implements both `IAuditable` and `ISoftDeletableWithUser`, supporting both timestamp tracking and soft delete in a single Entity. See how each method updates the related properties.

```csharp
public sealed class Product : Entity<ProductId>, IAuditable, ISoftDeletableWithUser
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Option<DateTime> UpdatedAt { get; private set; }
    public Option<DateTime> DeletedAt { get; private set; }
    public Option<string> DeletedBy { get; private set; }
    public bool IsDeleted => DeletedAt.IsSome;

    public static Product Create(string name, decimal price) =>
        new(ProductId.New(), name, price);

    public void UpdatePrice(decimal newPrice)
    {
        Price = newPrice;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete(string deletedBy)
    {
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    public void Restore()
    {
        DeletedAt = None;
        DeletedBy = None;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

`UpdatePrice()` refreshes `UpdatedAt` along with the price change, and `Restore()` also refreshes `UpdatedAt` since it's a state change. This ensures "the last modification time" is always accurately maintained.

---

## Summary at a Glance

### Entity Interface Hierarchy

A summary of the complete Entity interface hierarchy provided by Functorium.

| Interface | Properties | Purpose |
|-----------|-----------|---------|
| `IAuditable` | `CreatedAt`, `UpdatedAt` | Creation/modification timestamp tracking |
| `IAuditableWithUser` | + `CreatedBy`, `UpdatedBy` | Creator/modifier tracking |
| `ISoftDeletable` | `DeletedAt`, `IsDeleted` | Soft delete |
| `ISoftDeletableWithUser` | + `DeletedBy` | Deletion actor tracking |

### Why Use Option<DateTime>

Compare why `Option<DateTime>` is used instead of `DateTime?`.

| Expression | Meaning |
|-----------|---------|
| `None` | Has not occurred yet (unmodified, not deleted) |
| `Some(DateTime)` | Occurred at that point in time |
| Advantage over `DateTime?` | Prevents `null` reference errors, supports pattern matching |

---

## FAQ

### Q1: Why use soft delete instead of physical deletion?
**A**: Soft delete preserves data, which is advantageous for audit tracking, recovering from accidental deletion, and maintaining referential integrity of related data. When the Repository layer automatically applies an `IsDeleted` filter, application code doesn't need to be aware of deleted data.

### Q2: IsDeleted is a default interface implementation, so why re-declare it in Product?
**A**: C#'s default interface members (DIM) are only accessible when **cast to the interface type**. To access directly as `product.IsDeleted`, it must be explicitly declared in the class. This is a convenience choice.

### Q3: Why refresh UpdatedAt on Restore()?
**A**: Restore is also a state change on the Entity, so `UpdatedAt` is refreshed. This allows accurately tracking "the last time this Entity was modified."

### Q4: When should IAuditableWithUser be used?
**A**: Use it when you need to track "who created/modified" in multi-tenant environments or systems where audit logs are important. This chapter uses only `IAuditable` for simplicity.

---

The domain model foundation is now complete. Entity, Aggregate Root, domain events, and common interfaces -- now it's time to save and retrieve these models. Should every Repository repeatedly define the same CRUD methods? In Part 2, we solve this problem through Repository abstraction.

-> [Chapter 1: Repository Interface](../../Part2-Command-Repository/01-Repository-Interface/)
