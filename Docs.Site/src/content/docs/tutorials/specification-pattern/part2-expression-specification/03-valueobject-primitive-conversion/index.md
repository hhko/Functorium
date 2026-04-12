---
title: "Primitive Conversion"
---
## Overview

Suppose the domain model represents prices as a `Money` Value Object rather than `decimal`. In an Expression Tree, expressions like `product.Price.Value > 1000` cannot be converted to SQL by EF Core -- because EF Core does not know the internal structure of Value Objects. In this chapter, you will learn the pattern of converting Value Objects to primitive types.

> **To use Value Objects in Expression Trees, they must be converted to primitive types.**

## Learning Objectives

### Key Learning Objectives
1. **Understanding why Value Objects are problematic in Expression Trees**
   - If the closure directly captures a Value Object, the VO type is included in the Expression Tree
   - ORMs cannot map VO types to SQL columns
   - If VOs are inside the Expression Tree, SQL conversion fails

2. **Learning the local variable extraction pattern**
   - Convert VO to primitive local variable within the method
   - Expression lambda only references primitive local variables
   - Entity VO properties are converted to primitives via explicit casts

3. **Understanding the cast pattern `(string)product.Name`**
   - Convert VO to primitive through implicit operator
   - Represented as a Convert node in the Expression Tree
   - ORM's PropertyMap maps this to the actual DB column

### What You Will Verify Through Practice
- Defining Specifications for Product with Value Object properties
- Each Specification works correctly via IsSatisfiedBy
- ToExpression results can be used for AsQueryable filtering

## Key Concepts

### Problem: When Value Objects Are Directly Captured

```csharp
// Problematic code (VO is directly included in the Expression Tree)
public override Expression<Func<Product, bool>> ToExpression()
    => product => product.Name == Name;  // Name is of type ProductName
    // ORM cannot convert ProductName type to SQL!
```

### Solution: Local Variable Extraction + Cast Pattern

```csharp
public override Expression<Func<Product, bool>> ToExpression()
{
    // 1. Extract Value Object to local variable, converting to primitive
    string nameStr = Name;  // implicit operator invoked

    // 2. Expression lambda only references primitives + entity properties also cast
    return product => (string)product.Name == nameStr;
}
```

Why this pattern is needed:
1. **`string nameStr = Name`**: The value captured by the closure becomes string (not VO)
2. **`(string)product.Name`**: A Convert node is created in the Expression Tree that the ORM can interpret

### Value Object Definitions

```csharp
public sealed record ProductName(string Value)
{
    public static implicit operator string(ProductName name) => name.Value;
}

public sealed record Money(decimal Amount)
{
    public static implicit operator decimal(Money money) => money.Amount;
}

public sealed record Quantity(int Value)
{
    public static implicit operator int(Quantity qty) => qty.Value;
}
```

The `implicit operator` supports implicit conversion from VO to primitive.

## Project Description

### Project Structure
```
ValueObjectConversion/                           # Main project
├── Program.cs                                   # Value Object Spec demo
├── Product.cs                                   # VO-based product record
├── Specifications/
│   ├── ProductNameSpec.cs                       # Name Specification
│   ├── ProductPriceRangeSpec.cs                 # Price range Specification
│   └── ProductLowStockSpec.cs                   # Low stock Specification
├── ValueObjectConversion.csproj                 # Project file
ValueObjectConversion.Tests.Unit/                # Test project
├── ValueObjectConversionTests.cs                # VO conversion tests
├── Using.cs                                     # Global using
├── xunit.runner.json                            # xUnit configuration
├── ValueObjectConversion.Tests.Unit.csproj      # Test project file
index.md                                         # This document
```

### Core Code

#### ProductPriceRangeSpec.cs
```csharp
public sealed class ProductPriceRangeSpec : ExpressionSpecification<Product>
{
    public Money MinPrice { get; }
    public Money MaxPrice { get; }

    public ProductPriceRangeSpec(Money min, Money max) { MinPrice = min; MaxPrice = max; }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        decimal min = MinPrice;  // Money -> decimal
        decimal max = MaxPrice;  // Money -> decimal
        return product => (decimal)product.Price >= min && (decimal)product.Price <= max;
    }
}
```

## Summary at a Glance

### Conversion Pattern Summary
| Step | Code | Description |
|------|------|------|
| **Parameter Conversion** | `string nameStr = Name;` | Convert VO to primitive local variable |
| **Property Cast** | `(string)product.Name` | Cast entity VO property to primitive |
| **Expression Generation** | `product => (string)product.Name == nameStr` | Expression containing only primitives |

### Conversion Examples by VO Type
| Value Object | Primitive | Parameter Conversion | Property Cast |
|-------------|-----------|--------------|------------|
| `ProductName` | `string` | `string nameStr = Name;` | `(string)product.Name` |
| `Money` | `decimal` | `decimal min = MinPrice;` | `(decimal)product.Price` |
| `Quantity` | `int` | `int threshold = Threshold;` | `(int)product.Stock` |

## FAQ

### Q1: Why isn't the implicit operator alone sufficient?
**A**: The C# compiler automatically inserts implicit conversions within Expression lambdas, but it does not convert the type of the object captured by the closure. Without extracting parameters to local variables, the closure directly captures the VO instance, leaving the VO type in the Expression Tree.

### Q2: Does this pattern actually work with EF Core?
**A**: Yes, it works when used with EF Core's ValueConverter. EF Core recognizes Convert nodes in the Expression Tree and maps them to the corresponding DB columns. Functorium's PropertyMap adapter handles this conversion automatically.

### Q3: Do all Value Objects need an implicit operator?
**A**: Only VOs used in Expression Trees need it. VOs used only in memory are fine with explicit casts or `.Value` property access. The implicit operator is a convenience for code readability.

### Q4: Can VOs be defined as classes instead of records?
**A**: Yes, that is possible. This example uses records for brevity, but in real projects, Functorium's ValueObject base class is used to automatically handle validation and equality.

---

You have learned how to extract Expressions from individual ExpressionSpecifications. But how can you get a single Expression from composed Specifications like `inStock & affordable`? The next chapter covers the Expression Resolver that solves this problem.

-> [Chapter 4: Expression Resolver](../04-Expression-Resolver/)
