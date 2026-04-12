---
title: "E-Commerce Product Filtering"
---
## Overview

From Part 1 through Part 4, we progressively learned the Specification pattern fundamentals, Expression Trees, Repository integration, and real-world patterns. Now let's apply all of this to a single, complete domain scenario. Through a practical example of filtering products by various conditions on an e-commerce platform, we'll see how the patterns we've learned are actually combined.

> **Implement type-safe domain filtering through the combination of Value Objects and Specifications.**

## Learning Objectives

1. **Domain modeling with Value Objects**: Express domain concepts as types such as `ProductName`, `Money`, `Quantity`, `Category`
2. **ExpressionSpecification implementation**: Encapsulate various filtering conditions into individual Specifications
3. **Specification composition**: Build composite filtering conditions using `&`, `|`, `!` operators
4. **Repository pattern integration**: Utilize Specifications in Repository for product search

## Core Concepts

### Expressing the Domain with Value Objects

Using Value Objects instead of primitive types makes domain meaning explicit in the code.

```csharp
// Primitive types: meaning is unclear
Product(string name, decimal price, int stock, string category)

// Value Objects: domain meaning is clear
Product(ProductName name, Money price, Quantity stock, Category category)
```

### ExpressionSpecification and Value Objects

When using Value Objects inside Expressions, they must be converted to primitive types through implicit operators. This is because Expression Trees do not directly support comparisons of custom types.

```csharp
public override Expression<Func<Product, bool>> ToExpression()
{
    decimal min = MinPrice;  // implicit conversion
    decimal max = MaxPrice;
    return product => (decimal)product.Price >= min && (decimal)product.Price <= max;
}
```

### Composite Filtering Through Specification Composition

Compose individual Specifications with `&`, `|`, `!` operators to express complex business conditions.

```csharp
// Electronics AND in stock AND under 1,000,000
var spec = new ProductCategorySpec(new Category("Electronics"))
    & new ProductInStockSpec()
    & new ProductPriceRangeSpec(new Money(0m), new Money(1_000_000m));
```

## Project Description

### Project Structure
```
EcommerceFiltering/
├── Domain/
│   ├── ValueObjects/
│   │   ├── ProductName.cs
│   │   ├── Money.cs
│   │   ├── Quantity.cs
│   │   └── Category.cs
│   ├── Product.cs
│   ├── IProductRepository.cs
│   └── Specifications/
│       ├── ProductNameUniqueSpec.cs
│       ├── ProductPriceRangeSpec.cs
│       ├── ProductLowStockSpec.cs
│       ├── ProductCategorySpec.cs
│       └── ProductInStockSpec.cs
├── Infrastructure/
│   └── InMemoryProductRepository.cs
├── SampleProducts.cs
└── Program.cs
```

### Specification List

| Specification | Description | Parameters |
|---------------|-------------|------------|
| `ProductNameUniqueSpec` | Product name match check | `ProductName` |
| `ProductPriceRangeSpec` | Price range filtering | `Money min`, `Money max` |
| `ProductLowStockSpec` | Low stock products | `Quantity threshold` |
| `ProductCategorySpec` | Category filtering | `Category` |
| `ProductInStockSpec` | Products in stock | None |

## At a Glance

| Aspect | Details |
|--------|---------|
| **Domain** | E-commerce products (Product) |
| **Value Objects** | ProductName, Money, Quantity, Category |
| **Specification base** | All ExpressionSpecification |
| **Core pattern** | Specification composition (`&`, `|`, `!`) |
| **Repository** | Specification-based FindAll, Exists |

## FAQ

### Q1: Why can't Value Objects be directly compared in Expressions?
**A**: Expression Trees must be translatable to SQL and similar formats at runtime. Custom type `==` operators cannot be translated, so values must be converted to primitive types through implicit conversion before comparison.

### Q2: Why are all Specifications ExpressionSpecifications?
**A**: In this example, all filtering conditions can be expressed as Expression Trees. ExpressionSpecification is used to support automatic SQL translation when integrating with ORMs like EF Core. The next chapter covers an example that also uses non-Expression Specifications.

### Q3: Why are Korean variable names used in SampleProducts?
**A**: In domain examples, Korean variable names reflect domain terms directly in code, improving readability. In real projects, this is decided based on team conventions.

---

Product filtering is the most intuitive application of the Specification pattern. The next chapter applies the same pattern to the customer management domain, exploring the versatility of Specifications and mixed Expression/non-Expression usage.

→ [Customer Management](../02-Customer-Management/)
