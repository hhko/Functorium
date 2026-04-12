---
title: "Usecase Patterns"
---

## Overview

We've learned how to define Specifications, convert them to Expressions, and integrate them with Repositories. So where and how are Specifications actually used in real applications? This chapter explores two key scenarios where Specifications are utilized in the CQRS pattern -- existence checks in Commands and dynamic filters in Queries.

## Learning Objectives

1. **Using Specifications in Commands** - You can apply the duplicate check pattern using `Exists(spec)`
2. **Using Specifications in Queries** - You can implement dynamic filter composition using `Specification<T>.All` as the initial value
3. **Role separation in CQRS** - You can explain the difference in Specification roles between Commands (existence checks) and Queries (search filters)

## Core Concepts

### Command: Existence Check

Let's start with the Command side. In Command Usecases, Specifications are used for business rule validation. For example, when creating a product, we check whether a product with the same name already exists.

```csharp
var uniqueSpec = new ProductNameUniqueSpec(command.Name);
if (_repository.Exists(uniqueSpec))
    return false; // Already exists
```

### Query: Search Filter

Now for the Query side. In Query Usecases, `Specification<T>.All` is used as the initial value to progressively compose optional filters.

```csharp
var spec = Specification<Product>.All;

if (query.Category is not null)
    spec &= new ProductCategorySpec(query.Category);
if (query.MinPrice.HasValue && query.MaxPrice.HasValue)
    spec &= new ProductPriceRangeSpec(query.MinPrice.Value, query.MaxPrice.Value);

return _repository.FindAll(spec);
```

## Project Description

### Project Structure

```
UsecasePatterns/
├── Product.cs                              # Product record
├── IProductRepository.cs                   # Repository interface
├── InMemoryProductRepository.cs            # InMemory implementation
├── Specifications/
│   ├── ProductNameUniqueSpec.cs             # Name uniqueness check
│   ├── ProductInStockSpec.cs                      # In stock
│   ├── ProductPriceRangeSpec.cs                   # Price range
│   └── ProductCategorySpec.cs                     # Category filter
├── Usecases/
│   ├── CreateProductCommand.cs             # Command: Create product
│   └── SearchProductsQuery.cs              # Query: Search products
└── Program.cs                              # Demo execution
```

## At a Glance

Comparing how Specifications are used in Commands vs Queries.

| Aspect | Command | Query |
|--------|---------|-------|
| **Purpose** | Business rule validation | Data search/filtering |
| **Repository method** | `Exists(spec)` | `FindAll(spec)` |
| **Specification usage** | Single Spec | `All` + `&=` composition |
| **Example** | Name duplicate check | Category + price + stock filter |

## FAQ

### Q1: Why use a Specification instead of directly comparing the name in a Command?
**A**: Encapsulating it as a Specification allows the same duplicate check logic to be reused across different Usecases. It also eliminates the need to add specialized methods like `ExistsByName` to the Repository interface, preventing Repository method explosion.

### Q2: Why use `Specification<T>.All` instead of `null` as the initial value in a Query?
**A**: Using `All` enables progressive composition with the `&=` operator without null checks. Thanks to the identity property `All & X = X`, if no filters are applied, `All` is returned as-is, functioning as a full query.

### Q3: Can the same Specification be shared between Commands and Queries?
**A**: Yes, if it's the same domain condition, it can be shared. Specifications belong to the domain layer, and both Commands and Queries reference the same domain conditions.

---

The `All &= ...` pattern from the Query in this chapter works well for simple cases, but as filter conditions grow, the Usecase code becomes complex. In the next chapter, we'll evolve this pattern into an independent builder class that cleanly encapsulates filter logic.

→ [Chapter 2: Dynamic Filter Builder](../02-Dynamic-Filter-Builder/)
