---
title: "PropertyMap"
---
## Overview

What if the `Price` property in your domain model is stored as `UnitPrice` in the database table? Or what if a price represented as a `Money` Value Object in the domain is simply a `decimal` in the DB? If you pass a Specification's Expression Tree directly to EF Core, you'll get mapping errors. `PropertyMap` is the translation layer that bridges this gap.

`PropertyMap` resolves these name mismatches by **automatically converting Expressions written in domain terms into Expressions based on the DB model.**

## Learning Objectives

### Core Learning Objectives
1. **Understanding entity-model mismatch problems** - Issues that arise when property names differ between domain models and persistence models
2. **PropertyMap mapping registration** - The `Map(p => p.Name, m => m.ProductName)` pattern
3. **Automatic Expression conversion** - Using the `Translate` method to convert domain Expressions to model Expressions

### What you will verify through exercises
- Field name conversion (`TranslateFieldName`)
- Single Expression conversion
- Composite Expression conversion (And/Or combinations)
- Filtering a DbModel collection with converted Expressions

## Core Concepts

### Entity vs Persistence Model

```csharp
// Domain entity: business language
public record Product(string Name, decimal Price, int Stock, string Category);

// Persistence model: DB table structure
public record ProductDbModel(string ProductName, decimal UnitPrice, int StockQuantity, string CategoryCode);
```

Domain Specifications are written as `p => p.Stock > 0`, but the DB has a `StockQuantity` column. PropertyMap automatically bridges this gap.

### PropertyMap Registration

```csharp
var map = new PropertyMap<Product, ProductDbModel>();
map.Map(p => p.Name, m => m.ProductName);
map.Map(p => p.Price, m => m.UnitPrice);
map.Map(p => p.Stock, m => m.StockQuantity);
map.Map(p => p.Category, m => m.CategoryCode);
```

### TranslatingVisitor Internal Behavior

`PropertyMap.Translate` internally uses an `ExpressionVisitor`.

1. Traverses the Expression Tree to find `ParameterExpression` and `MemberExpression` nodes
2. Replaces entity parameters with model parameters
3. Replaces property accesses (`p.Stock`) with the mapped model properties (`m.StockQuantity`)
4. Result: `p => p.Stock > 0` is transformed into `m => m.StockQuantity > 0`

### Value Object Cast Pattern Support

In Part 2, Chapter 3, we learned the `(decimal)product.Price` cast pattern. PropertyMap's `Map` method directly supports this pattern:

```csharp
// Direct access: p => p.Name
map.Map(p => p.Name, m => m.ProductName);

// VO cast: p => (decimal)p.Price
map.Map(p => (decimal)p.Price, m => m.UnitPrice);

// ToString conversion: p => p.Id.ToString()
map.Map(p => p.Id.ToString(), m => m.ProductId);
```

The `TranslatingVisitor` traverses the Expression Tree and recognizes `Convert` nodes (casts) and `ToString()` calls, automatically replacing them with the mapped model properties. This is the point where Part 2.3's VO conversion pattern connects with PropertyMap.

> The full implementation of TranslatingVisitor (VisitMember, VisitUnary, VisitMethodCall) can be found in `Src/Functorium/Domains/Specifications/Expressions/PropertyMap.cs`.

Now that we understand the core concepts, let's look at how PropertyMap is structured in an actual project.

## Project Description

### Project Structure
```
PropertyMapDemo/                         # Main project
├── Product.cs                           # Domain model
├── ProductDbModel.cs                    # Persistence model
├── ProductPropertyMap.cs                # PropertyMap definition
├── Specifications/
│   ├── ProductInStockSpec.cs               # In stock (Expression-based)
│   └── ProductPriceRangeSpec.cs            # Price range (Expression-based)
├── Program.cs                           # Conversion demo
└── PropertyMapDemo.csproj
PropertyMapDemo.Tests.Unit/              # Test project
├── PropertyMapTests.cs                  # Field name/Expression conversion tests
└── ...
```

## At a Glance

### Mapping Table
| Domain (Product) | DB Model (ProductDbModel) |
|------------------|-------------------------|
| `Name` | `ProductName` |
| `Price` | `UnitPrice` |
| `Stock` | `StockQuantity` |
| `Category` | `CategoryCode` |

### PropertyMap Core Methods
| Method | Description |
|--------|-------------|
| `Map(entity, model)` | Register property mapping |
| `TranslateFieldName(name)` | Convert field name |
| `Translate(expression)` | Convert entire Expression Tree |

## FAQ

### Q1: EF Core already has mapping features, so why is PropertyMap needed?
**A**: EF Core's `HasColumnName` only works internally within EF Core. Since Specifications belong to the domain layer and EF Core belongs to the infrastructure layer, Expressions need to be converted **without the domain layer depending on EF Core**. PropertyMap performs this conversion in the infrastructure layer.

### Q2: Is PropertyMap always required?
**A**: If the property names of your domain model and DB model are identical, it's not needed. However, in practice, names often differ due to DB column naming conventions, legacy table structures, and other reasons.

### Q3: Are composite Expressions (And/Or/Not) also converted?
**A**: Yes. `SpecificationExpressionResolver.TryResolve` composes composite Specifications into a single Expression, and then `PropertyMap.Translate` converts that entire Expression. Since TranslatingVisitor recursively visits all nodes of the Expression Tree, composite Expressions are correctly converted.

---

We've resolved property mapping between domain and DB models using PropertyMap. In the next chapter, we'll combine everything we've learned so far -- Expression extraction, PropertyMap conversion, and Queryable application -- to implement an EF Core adapter.

→ [Chapter 4: EF Core Implementation](../04-EfCore-Implementation/)
