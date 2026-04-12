---
title: "Dynamic Filter Builder"
---

## Overview

In Part 1, Chapter 4, we introduced the `Specification<T>.All` identity element and the dynamic filter pattern. In this chapter, we evolve that pattern into an independent builder class. The builder composes optional filter conditions through a fluent API, cleanly encapsulating the filter logic for complex search screens.

## Learning Objectives

1. **Pattern using All as the initial value** - Understanding the identity property of `Specification<T>.All`
2. **Conditional `&=` chaining** - Progressive composition after null/empty checks
3. **Filter Builder separation** - Extracting filter composition logic into an independent class

## Core Concepts

### Using All as the Seed Value

`Specification<T>.All` is the identity element of the `&` operation. Since `All & X = X`, if no filters are applied, `All` is returned as-is, functioning as a full query.

```csharp
var spec = Specification<Product>.All;  // Initial value

if (!string.IsNullOrWhiteSpace(request.Name))
    spec &= new ProductNameContainsSpec(request.Name);
if (!string.IsNullOrWhiteSpace(request.Category))
    spec &= new ProductCategorySpec(request.Category);

return spec;  // Returns All if no filters
```

### Filter Builder Pattern

Separating filter composition logic into a `static` method keeps Usecase code clean and allows filter logic to be tested independently.

```csharp
public static class ProductFilterBuilder
{
    public static Specification<Product> Build(SearchProductsRequest request)
    {
        var spec = Specification<Product>.All;
        // Conditional &= chaining
        return spec;
    }
}
```

## Project Description

### Project Structure

```
DynamicFilter/
├── Product.cs                          # Product record
├── SampleProducts.cs                   # Sample data
├── SearchProductsRequest.cs            # Search request DTO
├── ProductFilterBuilder.cs             # Dynamic filter builder
├── Specifications/
│   ├── ProductNameContainsSpec.cs             # Name contains search
│   ├── ProductCategorySpec.cs                 # Category filter
│   ├── ProductPriceRangeSpec.cs               # Price range
│   └── ProductInStockSpec.cs                  # In stock
└── Program.cs                          # Demo execution
```

## At a Glance

| Request State | `Build()` Return Value | `IsAll` | Behavior |
|---------------|------------------------|---------|----------|
| No filters | `All` | `true` | Full query |
| 1 filter | That Spec | `false` | Single filter |
| N filters | `And` composition | `false` | Composite filter |

### null Fallback vs All Initial Value Comparison

| Item | `null` Fallback | `All` Initial Value |
|------|----------------|---------------------|
| **null check** | Required every time | Not needed |
| **Composition syntax** | `spec = spec is not null ? spec & x : x` | `spec &= x` |
| **Empty filter handling** | Separate branch needed | Automatic (returns All) |

## FAQ

### Q1: Does All affect performance?
**A**: `All`'s `IsSatisfiedBy()` always returns `true`, so the overhead is negligible. Additionally, the `&` operator performs identity optimization (`All & X = X`), preventing unnecessary `And` wrapping.

### Q2: Should the Filter Builder be an instance class instead of a static method?
**A**: If external dependencies (e.g., current user info, configuration values) are needed, it can be an instance class. However, for pure filter composition, a static method is sufficient.

### Q3: Can `|=` (OR chaining) be used with the same pattern?
**A**: `All` is the identity element for the `&` operation, but not for the `|` operation (`All | X = All` -- OR-ing with a condition that satisfies everything always results in everything). A separate initial value strategy is needed for OR composition.

---

We've cleanly separated filter composition logic with a builder. But how can we ensure these Specifications express the correct business rules? The next chapter covers systematic testing strategies for Specifications.

→ [Chapter 3: Testing Strategies](../03-Testing-Strategies/)
