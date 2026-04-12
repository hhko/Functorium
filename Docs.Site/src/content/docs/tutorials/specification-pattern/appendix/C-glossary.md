---
title: "Glossary"
---
Definitions and code examples of key terms used in this tutorial.

## A

### All (Identity Element)
The identity element of Specification. Returns `true` for all candidates and is used as the starting point for And composition. Serves as the initial seed in dynamic filter chaining.

```csharp
var spec = Specification<Product>.All;
spec &= new ActiveProductSpec();
```

### AllSpecification<T> (internal)
A Specification that satisfies all candidates. Inherits from `ExpressionSpecification<T>` and provides `ToExpression() => _ => true`, enabling Expression extraction via `SpecificationExpressionResolver.TryResolve()`. Accessed through the `Specification<T>.All` property, it is an internal class implemented with the singleton pattern.

### And (Logical Conjunction)
A composition operation that combines two Specifications, passing only candidates that satisfy both.

```csharp
var spec = new ActiveProductSpec().And(new ProductInStockSpec());
// Or using operator: new ActiveProductSpec() & new ProductInStockSpec()
```

---

## C

### Composition
Combining multiple Specifications with And, Or, Not to create complex rules. The core value of the Specification pattern.

---

## D

### Delegate Caching
An optimization technique in ExpressionSpecification that compiles the Expression Tree only once and caches the resulting delegate.

---

## E

### Expression Tree
A representation of code as a data structure. In the form of `Expression<Func<T, bool>>`, it can be converted by ORMs to SQL and other query languages.

```csharp
Expression<Func<Product, bool>> expr = p => p.IsActive;
```

### ExpressionSpecification<T>
An abstract Specification class that supports Expression Trees. Implementing `ToExpression` causes `IsSatisfiedBy` to automatically cache the compiled delegate.

### IExpressionSpec<T>
An interface indicating the ability to provide an Expression Tree. `ExpressionSpecification<T>` implements this, and `SpecificationExpressionResolver.TryResolve()` checks for this interface via pattern matching.

---

## I

### Identity Element
An element in a composition operation that does not change the other operand's value. `Specification<T>.All` is the identity element for the And operation.

### IsSatisfiedBy
The core method of Specification. Determines whether a candidate object satisfies the rule and returns `bool`.

```csharp
public override bool IsSatisfiedBy(Product candidate) =>
    candidate.IsActive;
```

---

## N

### Not (Logical Negation)
A composition operation that inverts the result of a Specification.

```csharp
var spec = new ActiveProductSpec().Not();
// Or using operator: !new ActiveProductSpec()
```

---

## O

### Or (Logical Disjunction)
A composition operation that passes if at least one of two Specifications is satisfied.

```csharp
var spec = new PremiumSpec().Or(new DiscountedSpec());
// Or using operator: new PremiumSpec() | new DiscountedSpec()
```

---

## P

### ParameterReplacer
An ExpressionVisitor that unifies different parameter expressions into one when composing Expression Trees. Used internally in And and Or compositions.

### PropertyMap<TEntity, TModel>
A class that defines property mappings between domain models and database entities. Used to convert domain properties to entity properties in Expression Trees.

---

## R

### Repository Pattern
A pattern that abstracts data access logic. When combined with Specification, a single `FindAsync(spec)` method enables querying with various conditions.

---

## S

### Specification<T>
An abstract class that encapsulates business rules. Implements the `IsSatisfiedBy` method to determine whether a candidate object satisfies the criteria.

### SpecificationExpressionResolver
A utility that recursively synthesizes Expression Trees from multiple Specifications. Merges And, Or, Not composition Expressions into a single Expression Tree.

---

## T

### ToExpression
The core method of ExpressionSpecification. Returns the rule in `Expression<Func<T, bool>>` form, which ORMs can convert to SQL.

### TranslatingVisitor
An ExpressionVisitor that transforms property accesses in Expression Trees based on PropertyMap. Converts domain model-based Expressions to database entity-based Expressions.

### TryResolve
The core method of SpecificationExpressionResolver. Extracts an Expression Tree from a Specification and recursively synthesizes them for composed Specifications.

---

## Next Steps

Check the references.

-> [D. References](D-references.md)
