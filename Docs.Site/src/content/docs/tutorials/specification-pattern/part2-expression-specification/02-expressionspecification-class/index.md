---
title: "ExpressionSpecification Class"
---
## Overview

Using Expression Trees, Specifications can be made into a form that ORMs can understand. However, manually compiling and caching Expressions each time is cumbersome. `ExpressionSpecification<T>` automates this process -- just implement `ToExpression()` and `IsSatisfiedBy` is automatically provided by caching the compiled delegate.

> **Just implement ToExpression() and IsSatisfiedBy() is automatically provided.**

## Learning Objectives

### Key Learning Objectives
1. **Can explain the design intent of ExpressionSpecification**
   - Override `ToExpression()` to define conditions as an Expression Tree
   - `IsSatisfiedBy()` is sealed and cannot be overridden in subclasses
   - Pattern of compiling the Expression once and caching it

2. **Can distinguish the differences from Specification**
   - `Specification<T>`: Directly implement `IsSatisfiedBy()`
   - `ExpressionSpecification<T>`: Implement `ToExpression()` and the rest is automatic
   - Expression-based, so SQL conversion is possible in ORMs

3. **Can define concrete Specifications by inheriting ExpressionSpecification**
   - Specification without parameters (ProductInStockSpec)
   - Specification with constructor parameters (ProductPriceRangeSpec, ProductCategorySpec)

### What You Will Verify Through Practice
- IsSatisfiedBy correctly returns the Expression compilation result
- ToExpression can be used directly with IQueryable
- Caching ensures consistent results across repeated calls

## Key Concepts

### sealed IsSatisfiedBy

The core design of `ExpressionSpecification<T>` is declaring `IsSatisfiedBy()` as sealed. This ensures:

1. **Consistency guarantee**: ToExpression() and IsSatisfiedBy() always evaluate the same condition
2. **Automatic caching**: Compiled delegates are cached internally
3. **Mistake prevention**: Prevents subclasses from defining conditions in two places

```csharp
public abstract class ExpressionSpecification<T> : Specification<T>, IExpressionSpec<T>
{
    private Func<T, bool>? _compiled;

    public abstract Expression<Func<T, bool>> ToExpression();

    public sealed override bool IsSatisfiedBy(T entity)
    {
        _compiled ??= ToExpression().Compile();
        return _compiled(entity);
    }
}
```

### IExpressionSpec<T> Interface

`ExpressionSpecification<T>` implements the `IExpressionSpec<T>` interface. This interface defines only the `ToExpression()` method and is the key for `SpecificationExpressionResolver` (covered in Chapter 4) to extract Expressions via pattern matching (`spec is IExpressionSpec<T>`).

```csharp
public interface IExpressionSpec<T>
{
    Expression<Func<T, bool>> ToExpression();
}
```

### Delegate Caching Pattern

`_compiled ??= ToExpression().Compile()` uses the null-coalescing assignment operator, so Compile() is only executed on the first call and the cached delegate is reused thereafter.

### ExpressionSpecification Implementation

```csharp
// Specification without parameters
public sealed class ProductInStockSpec : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
        => product => product.Stock > 0;
}

// Specification with constructor parameters
public sealed class ProductPriceRangeSpec(decimal min, decimal max)
    : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
        => product => product.Price >= min && product.Price <= max;
}
```

## Project Description

Let's verify these concepts with code.

### Project Structure
```
ExpressionSpec/                           # Main project
├── Program.cs                            # ExpressionSpecification demo
├── Product.cs                            # Product record
├── Specifications/
│   ├── ProductInStockSpec.cs                # Stock Specification
│   ├── ProductPriceRangeSpec.cs             # Price range Specification
│   └── ProductCategorySpec.cs               # Category Specification
├── ExpressionSpec.csproj                 # Project file
ExpressionSpec.Tests.Unit/                # Test project
├── ExpressionSpecTests.cs                # ExpressionSpecification tests
├── Using.cs                              # Global using
├── xunit.runner.json                     # xUnit configuration
├── ExpressionSpec.Tests.Unit.csproj      # Test project file
index.md                                  # This document
```

## Summary at a Glance

### Specification vs ExpressionSpecification
| Aspect | `Specification<T>` | `ExpressionSpecification<T>` |
|------|--------------------|-----------------------------|
| **Implementation Target** | `IsSatisfiedBy()` | `ToExpression()` |
| **IsSatisfiedBy** | Directly implemented | sealed (auto-compiled) |
| **Expression** | None | Provided via `ToExpression()` |
| **SQL Conversion** | Impossible | ORM adapter can convert |
| **Caching** | None | Compiled result auto-cached |
| **When to Use** | Memory-only conditions | Conditions requiring DB query conversion |

### ExpressionSpecification Selection Criteria
- Conditions that can be executed in DB -> `ExpressionSpecification<T>`
- Complex logic that runs only in memory -> `Specification<T>`

## FAQ

### Q1: Why is IsSatisfiedBy sealed?
**A**: To guarantee that ToExpression() and IsSatisfiedBy() always evaluate the same condition. If it were not sealed, subclasses could override IsSatisfiedBy() with a different condition than ToExpression(), breaking consistency.

### Q2: Is the caching thread-safe?
**A**: The `??=` operator itself is not atomic, but since the Compile() result is always the same, concurrent initialization from multiple threads does not cause practical issues. In the worst case, Compile() may be called multiple times, but the result is identical.

### Q3: How does And/Or/Not composition work with ExpressionSpecification?
**A**: You can compose using Specification's `&`, `|`, `!` operators. To extract Expressions from composed Specifications, `SpecificationExpressionResolver` is needed. This is covered in [Chapter 4: Expression Resolver](../04-Expression-Resolver/).

### Q4: When should I use regular Specification instead of ExpressionSpecification?
**A**: Use regular Specification for complex logic that cannot be expressed as an Expression Tree (e.g., external service calls, complex string processing). Expression Trees are suited for simple condition expressions that can be converted to SQL.

### Q5: Is Compile() caching per-instance or per-type?

**A**: The `_compiled` field is **per-instance**. Each call to `new ProductInStockSpec()` creates a new instance, and `Compile()` runs on the first `IsSatisfiedBy` call. `Compile()` itself takes microseconds, so it is not a performance issue in most scenarios. In high-frequency paths, reusing Specification instances (static fields, caching) can avoid unnecessary recompilation.

---

ExpressionSpecification expresses conditions with primitive types. But what if the domain model uses Value Objects? In the next chapter, we cover the primitive conversion pattern for safely using Value Objects in Expression Trees.

-> [Chapter 3: Value Object Conversion Pattern](../03-ValueObject-Primitive-Conversion/)
