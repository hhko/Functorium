---
title: "Specification-Based Repository"
---
## Overview

In Part 2, we introduced Expression Trees to Specifications. Now it is time to leverage these Expressions for actual database queries. By designing Repository methods that accept Specifications, you can fundamentally solve the 'method explosion' problem of adding new methods for every condition combination.

In the traditional Repository pattern, a new method must be created every time a query condition is added. `FindByCategory`, `FindByPriceRange`, `FindInStock`, `FindByCategoryAndPriceRange`... As condition combinations grow, the number of methods explodes exponentially.

The Specification pattern fundamentally solves this problem. The Repository has only a single method `FindAll(Specification<T> spec)`, and **WHAT to find** is delegated to the Specification.

## Learning Objectives

### Key Learning Objectives
1. **Recognizing the method explosion problem** - Understanding the limitations of adding methods for every condition combination
2. **Separation of concerns** - Repository handles HOW (where to find), Specification handles WHAT (what to find)
3. **IProductRepository interface design** - A generic Repository interface that accepts Specifications as parameters

### What You Will Verify Through Practice
- Concrete examples of the method explosion problem
- Handling all query conditions with a single `FindAll(Specification<Product> spec)` method
- Checking existence with the `Exists(Specification<Product> spec)` method

## Key Concepts

### Method Explosion

In a traditional Repository, even with just 3 conditions (category, price, stock), the number of combinable methods increases rapidly.

```csharp
// Before: Adding a method for each condition
public interface IProductRepository
{
    IEnumerable<Product> FindByCategory(string category);
    IEnumerable<Product> FindByPriceRange(decimal min, decimal max);
    IEnumerable<Product> FindInStock();
    IEnumerable<Product> FindByCategoryAndPriceRange(string category, decimal min, decimal max);
    IEnumerable<Product> FindInStockByCategory(string category);
    // ... methods increase exponentially as conditions grow!
}
```

### Solving with Specification

```csharp
// After: Handling all conditions with just two methods
public interface IProductRepository
{
    IEnumerable<Product> FindAll(Specification<Product> spec);
    bool Exists(Specification<Product> spec);
}
```

When a new condition is added, **you only need to create a new Specification class**. The Repository interface does not need to change.

## Project Description

### Project Structure
```
RepositorySpec/                          # Main project
├── Product.cs                           # Domain model
├── IProductRepository.cs                # Repository interface
├── Specifications/
│   ├── ProductInStockSpec.cs                   # In-stock products
│   ├── ProductPriceRangeSpec.cs                # Price range products
│   └── ProductCategorySpec.cs                  # Products by category
├── Program.cs                           # Before/After comparison demo
└── RepositorySpec.csproj
RepositorySpec.Tests.Unit/               # Test project
├── RepositorySpecTests.cs               # Interface contract + Spec tests
└── ...
```

### Core Code

#### IProductRepository.cs
```csharp
public interface IProductRepository
{
    IEnumerable<Product> FindAll(Specification<Product> spec);
    bool Exists(Specification<Product> spec);
}
```

The Repository does not need to know what condition the Specification expresses. It simply delegates to the Specification's `IsSatisfiedBy` method.

## Summary at a Glance

### Before vs After Comparison

Comparing how the Repository changes before and after introducing Specification.

| Aspect | Before (Traditional) | After (Specification) |
|------|----------------|----------------------|
| **Adding New Conditions** | Add method to Repository | Add Specification class |
| **Condition Combinations** | Separate method per combination | Compose with operators (`&`, `\|`, `!`) |
| **Repository Changes** | Changes needed per condition | No changes needed |
| **Testing** | Test per method | Specification unit tests |
| **Open-Closed** | Violated (modification needed) | Adhered (only extension needed) |

### Separation of Concerns

Summary of the roles handled by Repository and Specification respectively.

| Role | Responsibility | Examples |
|------|------|------|
| **Repository** | HOW (where to find) | InMemory, DB, API |
| **Specification** | WHAT (what to find) | In-stock products, under 10,000 won |

## FAQ

### Q1: Aren't methods other than FindAll and Exists needed in the Repository?
**A**: In real projects, you can add `Count(spec)`, `FindFirst(spec)`, etc. The key point is that **query conditions are expressed as Specification objects rather than method signatures**. Whatever methods you add, the parameter is always `Specification<T>`.

### Q2: Can this be used alongside existing Repository patterns?
**A**: Yes. It is practical to keep simple lookups like `FindById(int id)` as-is and only convert queries requiring complex condition combinations to Specification.

### Q3: When does the Specification pattern become over-engineering?
**A**: If there are only 1-2 query conditions and no composition is needed, the traditional method approach is simpler. The value of Specification becomes apparent when there are 3 or more conditions or when composition is needed.

---

Now that the Repository interface is complete, in the next chapter we implement this interface in its simplest form. Through the InMemory adapter, we will see how Specifications actually work within a Repository.

-> [Chapter 2: InMemory Implementation](../02-InMemory-Implementation/)
