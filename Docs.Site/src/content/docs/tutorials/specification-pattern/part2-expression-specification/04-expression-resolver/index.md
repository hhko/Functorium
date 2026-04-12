---
title: "Expression Resolver"
---
## Overview

So far you have learned how to extract Expressions from individual `ExpressionSpecification`s. But how can you get a single Expression from composed Specifications like `inStock & affordable`? `SpecificationExpressionResolver` recursively traverses the Specification tree composed with And, Or, Not and produces a single synthesized Expression Tree.

> **TryResolve extracts an Expression from a Specification. If extraction is not possible, it returns null.**

## Learning Objectives

### Key Learning Objectives
1. **Can explain how TryResolve extracts Expressions by Specification type**
   - `IExpressionSpec<T>` -> Direct call to `ToExpression()`
   - `AndSpecification<T>` -> Synthesize left/right Expressions with AndAlso
   - `OrSpecification<T>` -> Synthesize left/right Expressions with OrElse
   - `NotSpecification<T>` -> Wrap inner Expression with Not
   - Others -> Return null (graceful fallback)

2. **Can explain why ParameterReplacer is essential for Expression synthesis**
   - An ExpressionVisitor that unifies parameters from different Expressions
   - When synthesizing two Expressions, replaces parameters to reference the same one
   - Transforms the tree while maintaining Expression Tree immutability

3. **Can design fallback strategies when null is returned**
   - Non-expression Specifications cannot be converted to SQL
   - When null is returned, the adapter falls back to full load + in-memory filtering
   - Mixed composites (Expression + Non-expression) also return null

### What You Will Verify Through Practice
- Extracting Expressions from a single ExpressionSpec
- Extracting synthesized Expressions from And/Or/Not composites
- Confirming null return when mixed with Non-expression Specs

## Key Concepts

### TryResolve Pattern Matching

```csharp
public static Expression<Func<T, bool>>? TryResolve<T>(Specification<T> spec)
{
    return spec switch
    {
        IExpressionSpec<T> e => e.ToExpression(),
        AndSpecification<T> a => CombineAnd(a),
        OrSpecification<T> o => CombineOr(o),
        NotSpecification<T> n => CombineNot(n),
        _ => null
    };
}
```

Pattern matching performs appropriate processing based on the Specification type. `IExpressionSpec<T>` is checked first to directly extract the Expression. `AndSpecification` and `OrSpecification` access internal Specifications through `Left`/`Right` properties, and `NotSpecification` through the `Inner` property, recursively synthesizing Expressions.

### ParameterReplacer

When synthesizing two Expressions, each Expression has different parameter instances. ParameterReplacer uses an ExpressionVisitor to replace all parameters with a single unified parameter.

```csharp
// left: p => p.Stock > 0       (parameter: p)
// right: q => q.Price <= 50000  (parameter: q)
// synthesized: x => x.Stock > 0 && x.Price <= 50000  (unified parameter: x)
```

### null Fallback Strategy

Since partial conversion risks changing query semantics, an all-or-nothing strategy is adopted: either convert everything or process everything in memory. For example, if only one side of an And composite is converted to SQL, a large volume of insufficiently filtered data could flow into memory from the DB.

```
Repository.FindAll(spec) called
    ↓
Adapter attempts TryResolve(spec)
    ↓
Expression extraction succeeds -> DbContext.Set<T>().Where(expr) (SQL conversion)
Expression extraction fails -> Full load then in-memory filtering with IsSatisfiedBy
```

## Project Description

### Project Structure
```
ExpressionResolver/                          # Main project
├── Program.cs                               # Resolver demo
├── Product.cs                               # Product record
├── Specifications/
│   ├── ProductInStockSpec.cs                   # Expression-based stock Spec
│   ├── ProductPriceRangeSpec.cs                # Expression-based price Spec
│   ├── ProductCategorySpec.cs                  # Expression-based category Spec
│   └── ProductInStockPlainSpec.cs            # Non-expression stock Spec (fallback demo)
├── ExpressionResolver.csproj                # Project file
ExpressionResolver.Tests.Unit/               # Test project
├── ExpressionResolverTests.cs               # Resolver tests
├── Using.cs                                 # Global using
├── xunit.runner.json                        # xUnit configuration
├── ExpressionResolver.Tests.Unit.csproj     # Test project file
index.md                                     # This document
```

## Summary at a Glance

### TryResolve Behavior Summary
| Specification Type | Processing Method | Result |
|-------------------|----------|------|
| `IExpressionSpec<T>` | Direct call to `ToExpression()` | `Expression<Func<T, bool>>` |
| `AndSpecification<T>` | Recursive left/right extraction + AndAlso synthesis | Synthesized Expression or null |
| `OrSpecification<T>` | Recursive left/right extraction + OrElse synthesis | Synthesized Expression or null |
| `NotSpecification<T>` | Recursive inner extraction + Not wrapping | Negated Expression or null |
| Others | Cannot process | null |

### null Return Conditions
- Non-expression Specification used standalone
- In And/Or composites, if either left or right is non-expression
- If the inner part of Not is non-expression

## FAQ

### Q1: Why TryResolve and not Resolve?
**A**: Because there are Specifications from which Expressions cannot be extracted. For non-expression Specifications or mixed composites, Expressions cannot be created, so instead of throwing exceptions, null is returned so the caller can gracefully fall back.

### Q2: Can't mixed composites (Expression + Non-expression) be partially converted?
**A**: Theoretically possible, but partial conversion risks changing query semantics. For example, if only one side of an And composite is converted to SQL, unfiltered data from the DB can flow into memory, causing performance issues. Therefore, the all-or-nothing strategy is adopted: either convert everything or process everything in memory.

### Q3: Can't Expressions be synthesized without ParameterReplacer?
**A**: No. Each Expression lambda has a unique ParameterExpression instance. Without unifying them, the synthesized Expression would reference different parameters, causing runtime errors.

### Q4: How is it used in actual EF Core adapters?
**A**: It is used in Repository adapters as follows:
```csharp
var expr = SpecificationExpressionResolver.TryResolve(spec);
if (expr is not null)
    return await dbContext.Set<T>().Where(expr).ToListAsync();
else
    return (await dbContext.Set<T>().ToListAsync()).Where(spec.IsSatisfiedBy).ToList();
```
This pattern is covered in detail in [Part 3: Repository Integration](../../Part3-Repository-Integration/01-Repository-With-Specification/).

---

In Part 2, we built all the foundations for connecting Specifications with ORMs -- from Expression Tree concepts to the Resolver. In Part 3, we design Repositories on this foundation and implement InMemory and EF Core adapters.

-> [Part 3 Chapter 1: Repository with Specification](../../Part3-Repository-Integration/01-Repository-With-Specification/)
