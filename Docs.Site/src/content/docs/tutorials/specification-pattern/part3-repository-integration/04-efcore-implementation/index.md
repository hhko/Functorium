---
title: "EF Core Implementation"
---
## Overview

We combine everything learned so far to implement an **EF Core adapter simulation**. Without actual EF Core dependencies, we use `AsQueryable()` to reproduce the full pipeline: extracting Expressions from Specifications, converting them with PropertyMap, and applying them to Queryable.

The key takeaway is that **adding a new Specification requires no changes to the Repository code at all**. This is the Open-Closed Principle in action.

## Learning Objectives

### Core Learning Objectives
1. **Understanding the full pipeline** - Specification -> Expression extraction -> PropertyMap conversion -> Queryable execution
2. **BuildQuery pattern** - Combining `TryResolve` + `Translate` + `Where`
3. **Open-Closed Principle** - No Repository changes needed when adding new conditions

### What you will verify through exercises
- Full behavior of SimulatedEfCoreProductRepository
- Using the same interface as InMemory Repository
- Confirming that adding new Specifications requires no Repository changes

## Core Concepts

### Full Pipeline

```
Specification<Product>
    |
    v
SpecificationExpressionResolver.TryResolve(spec)
    |
    v
Expression<Func<Product, bool>>     (Domain Expression)
    |
    v
PropertyMap.Translate(expression)
    |
    v
Expression<Func<ProductDbModel, bool>>  (Model Expression)
    |
    v
dbModels.AsQueryable().Where(translated)
    |
    v
IQueryable<ProductDbModel>          (EF Core translates to SQL)
```

### BuildQuery Pattern

```csharp
private IQueryable<ProductDbModel> BuildQuery(Specification<Product> spec)
{
    // 1) Extract Expression from Specification
    var expression = SpecificationExpressionResolver.TryResolve(spec);
    if (expression is null)
        throw new InvalidOperationException(
            "Specification does not support expression resolution.");

    // 2) Convert domain Expression -> model Expression
    var translated = _propertyMap.Translate(expression);

    // 3) Apply to Queryable (translated to SQL in EF Core)
    return _dbModels.AsQueryable().Where(translated);
}
```

### Open-Closed Principle

When a new condition is needed:
1. Create a new `ExpressionSpecification<Product>`
2. Done. No Repository code changes required.

```csharp
// Adding a new Specification - no Repository changes!
var newSpec = new ProductCategorySpec("Electronics") & new ProductPriceRangeSpec(50_000, decimal.MaxValue);
var results = repository.FindAll(newSpec);  // Just works
```

## Project Description

### Project Structure
```
EfCoreImpl/                              # Main project
├── Product.cs                           # Domain model
├── ProductDbModel.cs                    # Persistence model
├── IProductRepository.cs                # Repository interface
├── InMemoryProductRepository.cs         # InMemory implementation (for comparison)
├── SimulatedEfCoreProductRepository.cs  # EF Core simulation implementation
├── ProductPropertyMap.cs                # PropertyMap definition
├── Specifications/
│   ├── ProductInStockSpec.cs               # In stock (Expression-based)
│   ├── ProductPriceRangeSpec.cs            # Price range (Expression-based)
│   └── ProductCategorySpec.cs              # Category (Expression-based)
├── Program.cs                           # Full pipeline demo
└── EfCoreImpl.csproj
EfCoreImpl.Tests.Unit/                   # Test project
├── EfCoreRepositoryTests.cs             # Full pipeline tests
└── ...
```

## At a Glance

### InMemory vs EF Core Implementation Comparison

Comparing the key differences between the two implementation approaches.

| Aspect | InMemory | EF Core (Simulation) |
|--------|----------|---------------------|
| **Filtering location** | Application memory | DB (Queryable/SQL) |
| **What it uses** | `IsSatisfiedBy` (method) | Expression Tree |
| **PropertyMap** | Not needed | Required (name conversion) |
| **Large datasets** | Not suitable | Suitable |
| **Interface** | Same (`IProductRepository`) | Same |

### Pipeline Stages

Each stage showing how data flows from Specification to SQL execution.

| Stage | Input | Output | Responsible |
|-------|-------|--------|-------------|
| 1 | `Specification<Product>` | `Expression<Func<Product, bool>>` | `TryResolve` |
| 2 | Domain Expression | Model Expression | `PropertyMap.Translate` |
| 3 | Model Expression | `IQueryable<ProductDbModel>` | `AsQueryable().Where()` |

## FAQ

### Q1: How does this differ with actual EF Core?
**A**: `_dbModels.AsQueryable()` becomes `dbContext.Set<ProductDbModel>().AsQueryable()`. Since EF Core's LINQ Provider translates the Expression Tree to SQL, filtering is executed at the DB level. The rest of the pipeline remains the same.

### Q2: What happens if a Specification that doesn't support Expression is passed?
**A**: `TryResolve` returns `null`, and `BuildQuery` throws an `InvalidOperationException`. When using the EF Core adapter, you must use `ExpressionSpecification`.

### Q3: Can the InMemory and EF Core implementations be used simultaneously?
**A**: Yes. Since both implement `IProductRepository`, you can inject different implementations via the DI container depending on the environment. It's common to use InMemory for testing and EF Core for production.

> **In real projects,** you would use the adapter base classes provided by Functorium: `EfCoreRepositoryBase`, `DapperQueryBase`, and `InMemoryQueryBase`. These classes already have the `BuildQuery` pattern built in. For more details, see the [CQRS Repository tutorial](../../cqrs-repository/).

---

In Part 3, we completed the integration of Specifications with the data layer -- from Repository interface design through InMemory, PropertyMap, and EF Core implementation. In Part 4, we'll apply real-world patterns on top of this infrastructure: usage in CQRS, dynamic filter builders, testing strategies, and architecture rules.

→ [Part 4, Chapter 1: Usecase Patterns](../../Part4-Real-World-Patterns/01-Usecase-Patterns/)
