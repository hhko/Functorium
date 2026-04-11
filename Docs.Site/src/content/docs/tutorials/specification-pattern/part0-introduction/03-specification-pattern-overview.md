---
title: "Specification Pattern Overview"
---
## Overview

If business rules are scattered throughout your service code, how can they be managed systematically? The Specification pattern is a Domain-Driven Design (DDD) pattern that **encapsulates business rules as independent objects**, making them easy to reuse, compose, and test. Defined by Eric Evans and Martin Fowler, this pattern is a powerful tool for clearly expressing complex conditional logic.

---

## The Repository Method Explosion Problem

### Problem: Methods Grow with Each Condition

Every time a new business requirement is added, a new Repository method is needed:

```csharp
// Bad: Method explosion - a new method for each condition combination
public interface IProductRepository
{
    Task<List<Product>> GetActiveProductsAsync();
    Task<List<Product>> GetActiveProductsByCategoryAsync(string category);
    Task<List<Product>> GetActiveProductsByPriceRangeAsync(decimal min, decimal max);
    Task<List<Product>> GetActiveProductsByCategoryAndPriceAsync(string category, decimal min, decimal max);
    Task<List<Product>> GetPremiumProductsAsync();
    Task<List<Product>> GetPremiumActiveProductsAsync();
    Task<List<Product>> GetDiscountedProductsAsync();
    // ... methods grow as combinations increase
}
```

The following table outlines the problems this approach causes.

| Problem | Description |
|---------|-------------|
| **Method explosion** | N conditions = up to 2^N methods |
| **Duplicate code** | Condition logic repeated across multiple methods |
| **Fragile to change** | Rule changes require modifying multiple methods |
| **Testing burden** | Tests needed for all combinations |

---

### Solution: Specification Pattern

The Specification pattern solves this problem by **separating conditions into objects**:

```csharp
// Good: Specification pattern - one method handles all conditions
public interface IProductRepository
{
    Task<IReadOnlyList<Product>> FindAsync(Specification<Product> spec);
    Task<int> CountAsync(Specification<Product> spec);
}

// Freely compose conditions
var spec = new ActiveProductSpec() & new ProductCategorySpec("Electronics");
var products = await repository.FindAsync(spec);
```

---

## What Specification Solves

The following table shows how the Specification pattern solves each of the problems presented above.

| Problem | How Specification Solves It |
|---------|---------------------------|
| Method explosion | Unified into a single `FindAsync(spec)` method |
| Duplicate code | Each rule is an independent Specification class |
| Fragile to change | Only the relevant Specification needs modification |
| Testing burden | Each Specification tested independently |
| Dynamic conditions | Runtime composition with And, Or, Not |

---

## Position in DDD

The Specification pattern resides in the **domain layer** of DDD:

```
Application Architecture
├── Presentation Layer
├── Application Layer
│   ├── UseCase / Handler
│   └── Specification composition (dynamic filters)
├── Domain Layer
│   ├── Entity / Aggregate
│   ├── Value Object
│   ├── Specification  <- Here
│   └── Domain Service
└── Infrastructure Layer
    ├── Repository implementation
    └── Specification->Expression conversion
```

**Core Principles:**
- Specifications are defined in the **domain layer**
- Repository **interfaces** (Ports) are defined in the domain layer
- Repository **implementations** (Adapters) are defined in the infrastructure layer
- Specification Expression conversion is handled in the infrastructure layer

---

## Functorium Type Hierarchy

The Functorium Specification type hierarchy used in this tutorial:

```
Specification<T> (abstract class)
├── IsSatisfiedBy(T) : bool
├── And() / Or() / Not()
├── & / | / ! operators
└── All (identity element)

IExpressionSpec<T> (interface)
└── ToExpression() : Expression<Func<T, bool>>

ExpressionSpecification<T> : Specification<T>, IExpressionSpec<T>
├── abstract ToExpression()
├── sealed IsSatisfiedBy (compilation + caching)
└── AllSpecification<T> (internal, identity element: _ => true)

SpecificationExpressionResolver (Expression composition)
PropertyMap<TEntity, TModel> (Entity->Model conversion)
```

### Specification<T>

The most basic abstract class. Define business rules by implementing the `IsSatisfiedBy` method.

```csharp
public sealed class ActiveProductSpec : Specification<Product>
{
    public override bool IsSatisfiedBy(Product candidate) =>
        candidate.IsActive && !candidate.IsDiscontinued;
}
```

### ExpressionSpecification<T>

A Specification that supports Expression Trees. When you implement `ToExpression`, `IsSatisfiedBy` automatically provides a compiled delegate with caching.

```csharp
public sealed class ActiveProductSpec : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression() =>
        product => product.IsActive && !product.IsDiscontinued;
}
```

### SpecificationExpressionResolver

A utility that recursively composes the Expressions of multiple Specifications. It merges the Expression Trees of And, Or, Not compositions into one.

### PropertyMap<TEntity, TModel>

Defines property mappings between domain models (including Value Objects) and database entities. Used when converting a Specification's Expression to a database query in ORMs like EF Core.

---

## Learning Flow of This Tutorial

```
Part 1: Specification Basics
├── Inheriting Specification<T> and implementing IsSatisfiedBy
├── And, Or, Not method composition
├── &, |, ! operator overloading
└── All identity element and dynamic filter chaining

Part 2: Expression Specification
├── Expression Tree concept and necessity
├── ExpressionSpecification<T> implementation
├── Value Object->primitive conversion pattern
└── SpecificationExpressionResolver

Part 3: Repository Integration
├── Preventing Repository method explosion
├── InMemory adapter implementation
├── PropertyMap and TranslatingVisitor
└── EF Core implementation

Part 4: Real-World Patterns
├── Using Specification with CQRS
├── Dynamic filter builder pattern
├── Testing strategies
└── Architecture rules

Part 5: Domain-Specific Practical Examples
├── E-commerce product filtering
└── Customer management
```

---

## FAQ

### Q1: What is the difference between the Specification pattern and the Strategy pattern?
**A**: The Strategy pattern focuses on swapping entire algorithms, while the Specification pattern focuses on **encapsulating conditions (predicates) as objects**. The key difference is that Specifications can be composed with `And`, `Or`, `Not` and can be converted to ORM queries through Expression Trees.

### Q2: Why is the `Specification<T>.All` identity element needed?
**A**: It is used as a seed value in dynamic filter builders. Since `All & X = X`, if no conditions are added, all data is returned. It allows writing clean code when conditionally adding nullable filter parameters with `if` statements.

### Q3: When is `SpecificationExpressionResolver` used?
**A**: It is used to merge the Expression Trees of compound Specifications composed with `And`, `Or`, `Not` into a single Expression. It is a utility that recursively composes multiple Expressions for passing to EF Core's `Where` clause.

### Q4: In which layer of DDD architecture does Specification reside?
**A**: Specifications are defined in the **domain layer**. This is because they are domain objects that express business rules. Repository interfaces (Ports) are also in the domain layer, while Repository implementations (Adapters) and Specification Expression conversion are handled in the infrastructure layer.

---

## Next Steps

Now that you understand the overview of the Specification pattern, let's write code directly in Part 1 and create your first Specification.

-> [Part 1 Ch 1: First Specification](../Part1-Specification-Basics/01-First-Specification/)
