---
title: "First Specification"
---
## Overview

The core of the Specification pattern is **encapsulating business conditions as independent objects**. In this chapter, you will inherit from the `Specification<T>` abstract class and create your first Specification.

> **"Is it in stock?", "Is the price within range?" -- Each of these questions becomes a Specification object.**

## Learning Objectives

### Key Learning Objectives
1. **Understanding the structure of Specification\<T\> abstract class**
   - The role and implementation of the `IsSatisfiedBy(T entity)` method
   - The pattern of inheriting from the abstract class to define concrete conditions

2. **Objectifying domain conditions**
   - Why separate conditional statements (`if`) into Specification objects
   - Reusable and testable condition expressions

3. **Importance of boundary value testing**
   - Verifying the exact behavior of Specifications using boundary values

### What You Will Verify Through Practice
- **ProductInStockSpec**: Filtering products with stock greater than 0
- **ProductPriceRangeSpec**: Filtering products within min/max price range

## Key Concepts

### Specification\<T\> Abstract Class

Functorium's `Specification<T>` is an abstract base class that encapsulates domain conditions. The core is a single method:

```csharp
public abstract class Specification<T>
{
    public abstract bool IsSatisfiedBy(T entity);
}
```

The reason this design uses an **abstract class** rather than an interface is to provide composition methods like `And()`, `Or()`, `Not()` and operator overloading from the base class. This ensures all Specifications automatically have composition capabilities.

### Why Separate Conditions into Objects

In typical code, conditions are written directly as `if` statements or lambdas:

```csharp
// Inline condition - not reusable, hard to test
var inStock = products.Where(p => p.Stock > 0);
```

When separated into a Specification:

```csharp
// Specification object - reusable, unit testable
var spec = new ProductInStockSpec();
var inStock = products.Where(p => spec.IsSatisfiedBy(p));
```

The value of this separation grows as conditions become more complex.

## Project Description

### Project Structure
```
FirstSpecification/
├── Program.cs                          # Demo execution
├── Product.cs                          # Domain model
├── Specifications/
│   ├── ProductInStockSpec.cs                  # Stock check Specification
│   └── ProductPriceRangeSpec.cs               # Price range Specification
└── FirstSpecification.csproj

FirstSpecification.Tests.Unit/
├── ProductInStockSpecTests.cs                 # Stock boundary value tests
├── ProductPriceRangeSpecTests.cs              # Price range boundary value tests
├── Using.cs
├── xunit.runner.json
└── FirstSpecification.Tests.Unit.csproj
```

### Core Code

#### Product.cs
```csharp
public record Product(string Name, decimal Price, int Stock, string Category);
```

#### ProductInStockSpec.cs
```csharp
public sealed class ProductInStockSpec : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity) => entity.Stock > 0;
}
```

#### ProductPriceRangeSpec.cs
```csharp
public sealed class ProductPriceRangeSpec(decimal min, decimal max) : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity) =>
        entity.Price >= min && entity.Price <= max;
}
```

## Summary at a Glance

The following table summarizes the design of `Specification<T>`.

### Specification\<T\> Design Summary
| Aspect | Description |
|------|------|
| **Base Type** | `abstract class Specification<T>` |
| **Core Method** | `IsSatisfiedBy(T entity)` -- returns whether the condition is satisfied |
| **Why Abstract Class** | Provides composition methods (`And`, `Or`, `Not`) and operator overloading from the base |
| **Implementation** | Inherit and override `IsSatisfiedBy` |

The following table compares inline conditions with the Specification approach.

### Inline Conditions vs Specification
| Aspect | Inline Conditions | Specification |
|------|-------------|---------------|
| **Reusability** | Low (copy/paste) | High (shared objects) |
| **Testability** | Difficult | Easy (unit tests) |
| **Readability** | Degrades with complexity | Conveys intent through names |
| **Composition** | Manual (`&&`, `\|\|`) | Automatic (`And`, `Or`, `Not`) |

## FAQ

### Q1: Why an abstract class instead of an interface?
**A**: `Specification<T>` implements `And()`, `Or()`, `Not()` composition methods and `&`, `|`, `!` operator overloading in the base class. Interfaces cannot provide operator overloading, and composition logic would be duplicated each time. Using an abstract class ensures all Specifications automatically inherit composition capabilities.

### Q2: Is there a reason for using the `sealed` keyword?
**A**: Concrete Specification classes (`ProductInStockSpec`, `ProductPriceRangeSpec`) are final implementations that do not need further inheritance. Adding `sealed` prevents unintended inheritance and allows the JIT compiler to optimize virtual calls.

### Q3: Is it okay to pass state (constructor parameters) to a Specification?
**A**: Yes, receiving condition parameters at construction time like `ProductPriceRangeSpec(min, max)` is a natural pattern. A Specification should be immutable after creation and should not depend on external state when `IsSatisfiedBy` is called.

### Q4: Why are boundary value tests important?
**A**: For `ProductPriceRangeSpec(100, 500)`, the behavior when the price is exactly 100 or 500 differs depending on whether `>=` or `>` is used. Boundary value tests clearly verify these subtle differences to guarantee the exact semantics of the Specification.

---

In the next chapter, you will learn how to compose multiple Specifications with `And`, `Or`, `Not` to create complex business rules.

-> [Chapter 2: Composition](../02-Composition/)
