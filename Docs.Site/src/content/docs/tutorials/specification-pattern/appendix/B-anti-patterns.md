---
title: "Anti-Patterns"
---
## Overview

Learning the correct pattern does not mean incorrect usage disappears. In practice, it is surprisingly common to over-apply the Specification pattern or use it in the wrong places. This appendix summarizes frequently occurring anti-patterns and provides guidance on how to avoid each one.

---

## 1. non-Expression Spec + EF Core

This is the most common mistake when first using an ORM like EF Core. Passing an in-memory-only Specification directly to an ORM query causes a runtime error.

### Problem

Using `Specification<T>` (non-Expression) directly with an ORM like EF Core causes runtime errors.

```csharp
// ❌ Anti-pattern: Using non-Expression Specification with EF Core
public sealed class ActiveProductSpec : Specification<Product>
{
    public override bool IsSatisfiedBy(Product candidate) =>
        candidate.IsActive;
}

// Runtime error when used with EF Core
var products = await dbContext.Products
    .Where(spec.IsSatisfiedBy)  // 💥 Cannot convert to SQL because it's not an Expression Tree
    .ToListAsync();
```

### Cause

EF Core's `Where` requires `Expression<Func<T, bool>>`. `Specification<T>`'s `IsSatisfiedBy` is a regular method and cannot be converted to an Expression Tree.

### Solution

Use `ExpressionSpecification<T>` when ORM integration is needed.

```csharp
// ✅ Use Expression-based Specification
public sealed class ActiveProductSpec : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression() =>
        product => product.IsActive;
}
```

---

## 2. Missing Value Object Closure Conversion

This frequently occurs in DDD projects that actively use Value Objects. Directly referencing a Value Object inside an Expression Tree prevents the ORM from interpreting it.

### Problem

Directly referencing a Value Object within an Expression Tree prevents EF Core from converting it to SQL.

```csharp
// ❌ Anti-pattern: Using Value Object directly in Expression
public sealed class MinPriceSpec : ExpressionSpecification<Product>
{
    private readonly Money _minPrice;

    public MinPriceSpec(Money minPrice) => _minPrice = minPrice;

    public override Expression<Func<Product, bool>> ToExpression() =>
        product => product.Price >= _minPrice;  // 💥 Cannot convert Money type to SQL
}
```

### Cause

Expression Trees can only use values that are convertible to database types. Value Objects are domain concepts that the database cannot understand.

### Solution

Extract the primitive value from the Value Object.

```csharp
// ✅ Convert to primitive value before use
public sealed class MinPriceSpec : ExpressionSpecification<Product>
{
    private readonly decimal _minPrice;

    public MinPriceSpec(Money minPrice) => _minPrice = minPrice.Value;

    public override Expression<Func<Product, bool>> ToExpression() =>
        product => product.Price >= _minPrice;
}
```

---

## 3. Stateful Specification

This is an easy trap to fall into when you want to filter and aggregate simultaneously. Adding counters or state to a Specification breaks the pure function principle.

### Problem

When a Specification mutates its internal state, unpredictable behavior occurs.

```csharp
// ❌ Anti-pattern: Specification with state
public sealed class CountingSpec : Specification<Product>
{
    private int _matchCount;

    public int MatchCount => _matchCount;

    public override bool IsSatisfiedBy(Product candidate)
    {
        if (candidate.IsActive)
        {
            _matchCount++;  // 💥 Side effect: state mutation
            return true;
        }
        return false;
    }
}
```

### Cause

A Specification should be a **pure function**. Side effects can produce different results for the same input and are not thread-safe.

### Solution

The Specification should only perform evaluation; aggregation should be handled externally.

```csharp
// ✅ Pure function Specification
public sealed class ActiveProductSpec : Specification<Product>
{
    public override bool IsSatisfiedBy(Product candidate) =>
        candidate.IsActive;
}

// Aggregation handled externally
var spec = new ActiveProductSpec();
var matchCount = products.Count(spec.IsSatisfiedBy);
```

---

## 4. God Specification

As filtering conditions increase, the temptation to "put everything in one class" grows. A God Specification built this way defeats the core values of the Specification pattern: composition and reuse.

### Problem

Putting all conditions into a single Specification makes reuse and composition impossible.

```csharp
// ❌ Anti-pattern: All conditions in one Specification
public sealed class ProductFilterSpec : Specification<Product>
{
    private readonly string? _category;
    private readonly decimal? _minPrice;
    private readonly decimal? _maxPrice;
    private readonly bool? _isActive;
    private readonly bool? _isPremium;

    public ProductFilterSpec(
        string? category = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? isActive = null,
        bool? isPremium = null)
    {
        _category = category;
        _minPrice = minPrice;
        _maxPrice = maxPrice;
        _isActive = isActive;
        _isPremium = isPremium;
    }

    public override bool IsSatisfiedBy(Product candidate)
    {
        if (_category is not null && candidate.Category != _category)
            return false;
        if (_minPrice is not null && candidate.Price < _minPrice)
            return false;
        if (_maxPrice is not null && candidate.Price > _maxPrice)
            return false;
        if (_isActive is not null && candidate.IsActive != _isActive)
            return false;
        if (_isPremium is not null && candidate.IsPremium != _isPremium)
            return false;
        return true;
    }
}
```

### Cause

The core value of the Specification pattern is **encapsulation and composition of individual rules**. Putting all conditions in one place eliminates this value.

### Solution

Separate each rule into an independent Specification and compose them.

```csharp
// ✅ Single Responsibility Specification + Composition
var spec = Specification<Product>.All;

if (filter.Category is not null)
    spec &= new ProductCategorySpec(filter.Category);
if (filter.MinPrice is not null)
    spec &= new MinPriceSpec(filter.MinPrice.Value);
if (filter.IsActive is not null)
    spec &= new ActiveProductSpec();

var products = await repository.FindAsync(spec);
```

---

## 5. Specification in Presentation Layer

Sometimes when you want quick results in a Controller or Razor Page, you skip the Application layer and use Specifications directly. While convenient, this breaks layer boundaries.

### Problem

Using Specifications directly in the presentation layer breaks layer boundaries.

```csharp
// ❌ Anti-pattern: Using Specification directly in Controller
[ApiController]
public class ProductController : ControllerBase
{
    private readonly IProductRepository _repository;

    [HttpGet]
    public async Task<IActionResult> GetProducts(string? category)
    {
        // 💥 Composing domain Specifications directly in the presentation layer
        var spec = new ActiveProductSpec();
        if (category is not null)
            spec &= new ProductCategorySpec(category);

        var products = await _repository.FindAsync(spec);
        return Ok(products);
    }
}
```

### Cause

Specifications are domain layer concepts. When the presentation layer depends directly on domain Specifications, coupling between layers increases.

### Solution

Compose Specifications in the Application layer (UseCase/Handler).

```csharp
// ✅ Compose Specifications in the Application layer
public sealed class GetProductsQueryHandler
{
    private readonly IProductRepository _repository;

    public async Task<IReadOnlyList<ProductDto>> Handle(GetProductsQuery query)
    {
        var spec = Specification<Product>.All;
        if (query.Category is not null)
            spec &= new ProductCategorySpec(query.Category);

        var products = await _repository.FindAsync(spec);
        return products.Select(p => p.ToDto()).ToList();
    }
}
```

---

## Anti-Pattern Summary

| # | Anti-Pattern | Core Problem | Solution |
|---|----------|----------|----------|
| 1 | non-Expression + EF Core | Cannot convert to SQL | Use ExpressionSpecification |
| 2 | Missing VO Closure Conversion | Expression Tree failure | Extract primitive value |
| 3 | Stateful Specification | Side effects, thread-unsafe | Maintain pure functions |
| 4 | God Specification | Cannot reuse/compose | Single responsibility + composition |
| 5 | Presentation Layer usage | Layer violation | Compose in Application layer |

---

## Next Steps

Check the glossary.

-> [C. Glossary](C-glossary.md)
