---
title: "EF Core Repository"
---
## Overview

What happens if you map domain models directly to `DbSet`?
Every time a DB column is added, you must modify the domain class, and ORM annotations invade business logic.
`EfCoreRepositoryBase<TAggregate, TId, TModel>` solves this problem.
It separates Domain Model and Persistence Model, handling conversions between the two through `ToDomain`/`ToModel` mappings.
Using `PropertyMap`, you can automatically convert domain Specification Expressions for EF Core queries.

---

## Learning Objectives

After completing this chapter, you will be able to:

1. Explain why Domain Model and Persistence Model should be separated.
2. Implement `ToDomain`/`ToModel` mappings yourself.
3. Explain how `PropertyMap` converts Specification Expressions.
4. Explain the `AsNoTracking` + Include auto-application mechanism of `ReadQuery()`.

---

## Core Concepts

### Why Separate Models?

Imagine mapping domain models directly to EF Core `DbSet`.

```csharp
// Anti-pattern: ORM annotations invading the Domain Model
[Table("Products")]
public class Product : AggregateRoot<ProductId>
{
    [Column("product_id")]
    public ProductId Id { get; }      // Ulid but DB wants string

    [MaxLength(200)]
    public string Name { get; }       // DB constraint unrelated to business logic

    public void UpdatePrice(decimal newPrice) { ... }
}
```

Every time the DB schema changes, the domain class must be modified, and business logic gets mixed with persistence concerns. Separating models allows each layer to evolve independently.

### Domain Model vs Persistence Model

Looking at the following structure, the difference between the two models is clear. The Domain Model has behavior and events, while the Persistence Model contains only pure data.

```
Product (Domain Model)          ProductModel (Persistence Model)
├── ProductId Id                ├── string Id        <- Ulid -> string
├── string Name                 ├── string Name
├── decimal Price               ├── decimal Price
├── bool IsActive               ├── bool IsActive
├── UpdatePrice()               └── (no behavior)
└── DomainEvents
```

- **Domain Model** includes business logic and domain events.
- **Persistence Model** is a pure data class matching the DB schema.
- Separation ensures DB schema changes don't affect domain logic.

### ToDomain / ToModel Mapping

How do you convert between the two models? The `ToDomain` and `ToModel` methods handle this.

```csharp
// Persistence -> Domain (when reading from DB)
Product ToDomain(ProductModel model)
{
    return new Product(
        ProductId.Create(model.Id),  // string -> Ulid-based ID
        model.Name,
        model.Price,
        model.IsActive);
}

// Domain -> Persistence (when saving to DB)
ProductModel ToModel(Product aggregate)
{
    return new ProductModel
    {
        Id = aggregate.Id.ToString(),  // Ulid-based ID -> string
        Name = aggregate.Name,
        Price = aggregate.Price,
        IsActive = aggregate.IsActive,
    };
}
```

When reading from DB, `ToDomain` restores the domain object; when saving, `ToModel` converts to DB format.

### EfCoreRepositoryBase Required Implementations

The subclass must implement the following 4 members. Compared to InMemory Repository's single `Store`, mapping logic is added.

| Member | Role |
|--------|------|
| `DbContext` | EF Core DbContext |
| `DbSet` | Entity's DbSet |
| `ToDomain(TModel)` | Persistence -> Domain conversion |
| `ToModel(TAggregate)` | Domain -> Persistence conversion |

### PropertyMap -- Specification Expression Conversion

To use Specifications written in the domain layer directly in EF Core queries, Expression Trees must be converted to Persistence Model references. `PropertyMap` handles this conversion automatically.

```
1. Specification -> Expression<Func<Product, bool>>     (domain-based)
2. PropertyMap.Translate() -> Expression<Func<ProductModel, bool>>  (model-based)
3. IQueryable.Where() -> SQL WHERE clause generation
```

This allows domain layer Specifications to be used in EF Core queries without modification.

### ReadQuery() -- N+1 Prevention

Automatically applies `AsNoTracking` and `Include` to all read queries, ensuring performance and consistency.

```csharp
protected IQueryable<TModel> ReadQuery()
    => applyIncludes(DbSet.AsNoTracking());
```

- `AsNoTracking`: Eliminates Change Tracker overhead for read-only queries.
- `applyIncludes`: Includes declared in the constructor are automatically applied to all read queries.

---

## Project Description

### Project Structure

```
03-EfCore-Repository/
├── EfCoreRepository/
│   ├── EfCoreRepository.csproj
│   ├── Program.cs              # Mapping demo
│   ├── ProductId.cs            # Ulid-based identifier
│   ├── Product.cs              # Domain Model
│   ├── ProductModel.cs         # Persistence Model
│   └── ProductMapper.cs        # ToDomain/ToModel mapping
├── EfCoreRepository.Tests.Unit/
│   ├── EfCoreRepository.Tests.Unit.csproj
│   ├── Using.cs
│   ├── xunit.runner.json
│   └── ProductMapperTests.cs
└── README.md
```

### Core Code

Let's look at the actual mapping code. Note how `ToDomain` and `ToModel` convert the ID type.

**ProductMapper** -- Conversion between Domain and Persistence models:

```csharp
public static Product ToDomain(ProductModel model)
{
    return new Product(
        ProductId.Create(model.Id),
        model.Name,
        model.Price,
        model.IsActive);
}

public static ProductModel ToModel(Product aggregate)
{
    return new ProductModel
    {
        Id = aggregate.Id.ToString(),
        Name = aggregate.Name,
        Price = aggregate.Price,
        IsActive = aggregate.IsActive,
    };
}
```

`ProductId.Create(model.Id)` restores a string to a Ulid-based ID, and `aggregate.Id.ToString()` converts to a DB-storable string.

---

## Summary at a Glance

The following table summarizes the key components of EF Core Repository.

| Item | Description |
|------|-------------|
| Base class | `EfCoreRepositoryBase<TAggregate, TId, TModel>` |
| Model separation | Domain Model + Persistence Model |
| Mapping | `ToDomain()` / `ToModel()` |
| Specification conversion | `PropertyMap.Translate()` |
| Read optimization | `ReadQuery()` = `AsNoTracking` + Include |
| ID strategy | Ulid -> string (DB compatible) |

---

## FAQ

### Q1: Why separate Domain Model and Persistence Model?
**A**: To ensure DB schema changes (column additions, type changes) don't affect domain logic. It also prevents ORM annotations from invading the domain model.

### Q2: Can Specification be used without PropertyMap?
**A**: Basic CRUD of `IRepository` works without `PropertyMap`. However, for Specification-based queries like `BuildQuery(spec)` or `ExistsBySpec(spec)`, `PropertyMap` is required.

### Q3: What is IHasStringId?
**A**: An interface for providing default implementations of `ByIdPredicate`/`ByIdsPredicate` in `EfCoreRepositoryBase`. It forces the Persistence Model to have a `string Id` property.

---

We've separated domain and persistence through EF Core Repository. But what if order creation and inventory deduction must be wrapped in a single transaction? In the next chapter, we'll look at the Unit of Work pattern for atomically committing changes across multiple Repositories.

-> [Chapter 4: Unit of Work](../04-Unit-Of-Work/)
