---
title: "InMemory Implementation"
---
## Overview

Let's see how simply the Repository interface designed in the previous chapter can be implemented. The InMemory adapter is not only useful in test environments but is also the implementation that most clearly demonstrates the core behavior of the Repository pattern.

The implementation is complete just by passing method references (`spec.IsSatisfiedBy`) to LINQ's `Where` and `Any`.

## Learning Objectives

### Key Learning Objectives
1. **Direct use of IsSatisfiedBy** - Understanding the `Where(spec.IsSatisfiedBy)` pattern
2. **Method reference syntax** - Using method reference `spec.IsSatisfiedBy` instead of lambda `p => spec.IsSatisfiedBy(p)`
3. **Simplicity of Repository implementation** - Thanks to Specification, the Repository implementation becomes extremely concise

### What You Will Verify Through Practice
- FindAll/Exists behavior of InMemoryProductRepository
- Querying with various Specification combinations
- Existence checking (Exists)

## Key Concepts

### IsSatisfiedBy Method Reference

`Specification<T>.IsSatisfiedBy` is compatible with the `Func<T, bool>` signature. Therefore, it can be passed directly to LINQ's `Where`.

```csharp
// Lambda approach (verbose)
_products.Where(p => spec.IsSatisfiedBy(p));

// Method reference approach (concise)
_products.Where(spec.IsSatisfiedBy);
```

Both approaches return identical results, but the method reference approach is more concise and intent is clearer.

### Core of InMemory Implementation

```csharp
public class InMemoryProductRepository : IProductRepository
{
    private readonly List<Product> _products;

    public InMemoryProductRepository(IEnumerable<Product> products)
        => _products = products.ToList();

    public IEnumerable<Product> FindAll(Specification<Product> spec)
        => _products.Where(spec.IsSatisfiedBy);

    public bool Exists(Specification<Product> spec)
        => _products.Any(spec.IsSatisfiedBy);
}
```

The Repository **has no knowledge of what conditions are being filtered**. It simply delegates to `IsSatisfiedBy`. Even when new Specifications are added, the Repository code does not need to change.

## Project Description

### Project Structure
```
InMemoryImpl/                            # Main project
├── Product.cs                           # Domain model
├── IProductRepository.cs                # Repository interface
├── InMemoryProductRepository.cs         # InMemory implementation
├── SampleProducts.cs                    # Sample data (8 products)
├── Specifications/
│   ├── ProductInStockSpec.cs                   # In-stock products
│   ├── ProductPriceRangeSpec.cs                # Price range products
│   └── ProductCategorySpec.cs                  # Products by category
├── Program.cs                           # FindAll/Exists demo
└── InMemoryImpl.csproj
InMemoryImpl.Tests.Unit/                 # Test project
├── InMemoryRepositoryTests.cs           # Repository behavior tests
└── ...
```

## Summary at a Glance

### InMemory Implementation Core
| Method | LINQ Method | Description |
|--------|------------|------|
| `FindAll(spec)` | `Where(spec.IsSatisfiedBy)` | Returns all items satisfying the condition |
| `Exists(spec)` | `Any(spec.IsSatisfiedBy)` | Whether an item satisfying the condition exists |

### InMemory Characteristics
| Characteristic | Description |
|------|------|
| **Pros** | Extremely simple implementation, suitable for testing, no external dependencies |
| **Limitations** | Must load all data into memory, not suitable for large datasets |
| **Use Cases** | Unit tests, prototyping, small datasets |

## FAQ

### Q1: Is the InMemory implementation useful in real projects?
**A**: Yes. Using the InMemory implementation instead of a real DB in unit tests makes tests fast and isolated. Thanks to the Repository interface, different implementations can be used in testing and production.

### Q2: Are there cases where `Where(p => spec.IsSatisfiedBy(p))` should be used instead of `Where(spec.IsSatisfiedBy)`?
**A**: They are functionally identical. Since method references are more concise, the `spec.IsSatisfiedBy` form is generally preferred.

### Q3: What should be done for large datasets?
**A**: In Chapter 3 (PropertyMap) and Chapter 4 (EF Core Implementation), you will learn how to filter at the DB level using Expression Trees. The InMemory approach loads all data into memory before filtering, so it is not suitable for large volumes.

---

The InMemory implementation calls `IsSatisfiedBy` directly, so the domain model alone was sufficient. However, in environments like EF Core that convert Expression Trees to SQL, problems arise when domain model and DB model property names differ. The next chapter covers PropertyMap, which bridges this gap.

-> [Chapter 3: PropertyMap](../03-PropertyMap/)
