---
title: "Specification Composition"
---
## Overview

Creating individual Specifications alone cannot express complex business conditions. In this chapter, you will learn how to **logically compose** multiple Specifications using the `And()`, `Or()`, `Not()` methods.

> **Compose small conditions like LEGO blocks to build complex business rules.**

## Learning Objectives

### Key Learning Objectives
1. **How to use And/Or/Not composition methods**
   - `And()`: True only when both conditions are true
   - `Or()`: True if at least one is true
   - `Not()`: Inverts the condition

2. **Understanding the internal composition class structure**
   - Roles of `AndSpecification<T>`, `OrSpecification<T>`, `NotSpecification<T>`
   - Provided by the base class, so users do not need to implement them

3. **Chain composition pattern**
   - Chained composition like `spec1.And(spec2.Not())`

### What You Will Verify Through Practice
- **And composition**: Products in stock and affordable
- **Or composition**: Products that are electronics or affordable
- **Not composition**: Products that are not electronics
- **Complex composition**: And + Not combination

## Key Concepts

### Internal Structure of Composition

The `Specification<T>` base class provides three composition methods:

```csharp
public Specification<T> And(Specification<T> other) => new AndSpecification<T>(this, other);
public Specification<T> Or(Specification<T> other)  => new OrSpecification<T>(this, other);
public Specification<T> Not()                        => new NotSpecification<T>(this);
```

Each composition internally creates `AndSpecification`, `OrSpecification`, or `NotSpecification` classes. These classes are declared as `internal`, so they are not exposed to library users.

### How AndSpecification Works

```csharp
internal sealed class AndSpecification<T>(Specification<T> left, Specification<T> right)
    : Specification<T>
{
    public override bool IsSatisfiedBy(T entity)
        => left.IsSatisfiedBy(entity) && right.IsSatisfiedBy(entity);
}
```

It holds two Specifications, `left` and `right`, and checks whether both are satisfied when `IsSatisfiedBy` is called. `OrSpecification` and `NotSpecification` work on the same principle.

### Composition Returns a New Specification

The important point is that `And()`, `Or()`, `Not()` **return a new Specification object without modifying the original**. This guarantees immutability.

```csharp
var inStock = new ProductInStockSpec();
var affordable = new ProductPriceRangeSpec(10_000m, 100_000m);

// The original is not modified - a new Specification is created
var combined = inStock.And(affordable);
```

## Project Description

### Project Structure
```
Composition/
├── Program.cs
├── Product.cs
├── Specifications/
│   ├── ProductInStockSpec.cs
│   ├── ProductPriceRangeSpec.cs
│   └── ProductCategorySpec.cs
└── Composition.csproj

Composition.Tests.Unit/
├── CompositionTests.cs
├── Using.cs
├── xunit.runner.json
└── Composition.Tests.Unit.csproj
```

### Core Code

#### ProductCategorySpec.cs
```csharp
public sealed class ProductCategorySpec(string category) : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity) =>
        entity.Category.Equals(category, StringComparison.OrdinalIgnoreCase);
}
```

#### Composition Usage Examples
```csharp
var inStock = new ProductInStockSpec();
var affordable = new ProductPriceRangeSpec(10_000m, 100_000m);
var electronics = new ProductCategorySpec("Electronics");

// And: Products in stock and affordable
var spec1 = inStock.And(affordable);

// Or: Products that are electronics or affordable
var spec2 = electronics.Or(affordable);

// Not: Products that are not electronics
var spec3 = electronics.Not();

// Complex: Products in stock and not electronics
var spec4 = inStock.And(electronics.Not());
```

## Summary at a Glance

The following table summarizes the behavior of the three composition methods.

### Composition Method Summary
| Method | Internal Class | Behavior |
|--------|------------|------|
| `And(other)` | `AndSpecification<T>` | True when both conditions are true |
| `Or(other)` | `OrSpecification<T>` | True if at least one is true |
| `Not()` | `NotSpecification<T>` | Inverts the condition |

The following table lists the key characteristics of composition.

### Composition Characteristics
| Characteristic | Description |
|------|------|
| **Immutability** | Returns a new object without modifying the original Specification |
| **Chainable** | Chained composition possible like `a.And(b.Not())` |
| **Internal Implementation** | Composition classes are encapsulated as `internal` |
| **Automatically Provided** | No manual implementation needed as it is provided by the base class |

## FAQ

### Q1: Is there a reason the composition classes are internal?
**A**: Users only need to use the `And()`, `Or()`, `Not()` methods. Exposing the implementation details of internal composition classes would break library encapsulation and make future implementation changes difficult.

### Q2: How deeply can compositions be nested?
**A**: There is no technical limit, but overly deep nesting harms readability. If complex compositions are needed, it is better to assign names to intermediate variables or create new Specification classes with meaningful names.

### Q3: Does the evaluation order of And and Or matter?
**A**: `AndSpecification` uses the `&&` operator, so **short-circuit evaluation** applies. That is, if the left side is false, the right side is not evaluated. Similarly, `OrSpecification` does not evaluate the right side if the left side is true.

---

In the next chapter, we will look at how to compose more concisely using `&`, `|`, `!` operators instead of method calls.

-> [Chapter 3: Operators](../03-Operators/)
