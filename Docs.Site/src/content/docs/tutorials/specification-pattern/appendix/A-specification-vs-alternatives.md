---
title: "Specification vs Alternatives"
---
## Overview

The Specification pattern is not the only way to manage business rules. You can also pass lambda expressions directly, use the Query Object pattern, or combine LINQ extension methods. So when should you choose Specification? This appendix compares the major alternatives with the Specification pattern and summarizes the appropriate use scenarios for each.

---

## Approach Comparison

A quick comparison of the pros, cons, and suitable scenarios for five approaches.

| Approach | Pros | Cons | Suitable For |
|--------|------|------|-------------|
| **Inline Predicate** | Simple, no extra classes needed | Not reusable, hard to test | One-off filters, simple conditions |
| **Strategy Pattern** | Algorithm swapping | No composition, overkill for bool returns | When algorithm swapping is the goal |
| **Repository-per-Query** | Intuitive, type-safe | Method explosion, duplicate code | When condition combinations are few |
| **Dynamic LINQ** | Flexible string-based queries | No compile-time validation, security risks | Admin ad-hoc queries |
| **Specification Pattern** | Composable, reusable, easy to test | Initial implementation cost, learning curve | Complex business rules, DDD |

---

## Detailed Comparison

### 1. Inline Predicate

```csharp
// Inline predicate
var activeProducts = products.Where(p => p.IsActive && !p.IsDiscontinued);
```

**Pros:**
- Immediate use without extra classes
- Suitable for simple conditions
- No learning cost

**Cons:**
- Same condition repeated in multiple places
- All usages must be modified when rules change
- Cannot assign a name to the condition
- Cannot independently verify just the condition via unit tests

---

### 2. Strategy Pattern

```csharp
public interface IProductFilter
{
    IEnumerable<Product> Filter(IEnumerable<Product> products);
}

public class ActiveProductFilter : IProductFilter
{
    public IEnumerable<Product> Filter(IEnumerable<Product> products) =>
        products.Where(p => p.IsActive);
}
```

**Pros:**
- Encapsulates filter logic
- Filters can be swapped at runtime

**Cons:**
- Operates on the entire collection (not individual item evaluation)
- No standard way to combine two Strategies with And/Or
- Cannot convert to Expression Tree

---

### 3. Repository-per-Query

```csharp
public interface IProductRepository
{
    Task<List<Product>> GetActiveProductsAsync();
    Task<List<Product>> GetActiveProductsByCategoryAsync(string category);
    Task<List<Product>> GetPremiumActiveProductsByCategoryAsync(string category);
    // ... new method for each combination
}
```

**Pros:**
- Intuitive and type-safe
- IDE auto-completion support

**Cons:**
- N conditions -> up to 2^N methods
- Condition logic scattered across Repository implementations
- Interface changes required when adding new conditions

---

### 4. Dynamic LINQ

```csharp
// String-based dynamic query (System.Linq.Dynamic.Core)
var result = products.AsQueryable()
    .Where("IsActive == true && Price > @0", minPrice);
```

**Pros:**
- Very flexible runtime query construction
- Suitable for user-defined filters

**Cons:**
- No compile-time type validation
- Runtime errors from typos
- Security risks similar to SQL injection
- Difficult to track strings during refactoring

---

### 5. Specification Pattern

```csharp
var spec = new ActiveProductSpec() & new ProductCategorySpec("Electronics");
var products = await repository.FindAsync(spec);
```

**Pros:**
- Assigns names to business rules (ubiquitous language)
- Free composition with And, Or, Not
- Individual Specifications can be tested independently
- ORM integration through Expression Trees
- Prevents Repository method explosion

**Cons:**
- Initial framework implementation cost (resolved by using Functorium)
- Can be overkill for simple conditions
- Requires understanding Expression Trees (covered in Part 2)

---

## Selection Guide

```
Is the condition used in only one place?
├── Yes -> Inline Predicate
└── No -> Is condition composition needed?
    ├── No -> Strategy Pattern or Repository-per-Query
    └── Yes -> Is ORM integration needed?
        ├── No -> Specification (memory-based)
        └── Yes -> ExpressionSpecification (Expression Tree)
```

---

## Next Steps

Check the anti-patterns of the Specification pattern.

-> [B. Anti-Patterns](B-anti-patterns.md)
