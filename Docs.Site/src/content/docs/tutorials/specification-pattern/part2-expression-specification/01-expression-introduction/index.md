---
title: "Expression Tree Basics"
---
## Overview

The Specifications created in Part 1 filter in-memory collections using the `IsSatisfiedBy` method. However, in real applications, data resides in databases. ORMs like EF Core convert C# lambdas to SQL, and for that, they need **Expression Trees** rather than methods that return `bool`. In this chapter, we explore what Expression Trees are and why they are needed for Specifications.

> **Func is an executable black box, while Expression is an inspectable tree structure.**

## Learning Objectives

### Key Learning Objectives
1. **Can explain the fundamental difference between Func and Expression**
   - `Func<T, bool>` is a compiled delegate whose internal structure cannot be examined
   - `Expression<Func<T, bool>>` preserves the code structure in tree form
   - Expression is inspectable through Body, Parameters, NodeType, etc.

2. **Can explain why ORMs need Expression Trees**
   - EF Core must translate LINQ queries to SQL
   - Func is opaque and cannot be converted to SQL
   - Expression can be traversed and converted to SQL clauses

3. **Can compile and cache Expressions** for in-memory execution
   - Can be converted to Func via `Expression.Compile()` for in-memory execution
   - Caching the compiled result for reuse is efficient

### What You Will Verify Through Practice
- Accessing Expression's Body, Parameters properties
- Compiling an Expression to Func and executing it
- Using Expression-based Where with AsQueryable()

## Key Concepts

### Func: An Opaque Black Box

`Func<Product, bool>` is a compiled delegate. It can be invoked at runtime to get results, but "what condition it represents" cannot be determined programmatically.

```csharp
Func<Product, bool> func = p => p.Price > 1000;
// No way to inspect the internals of func
// EF Core cannot translate this to SQL
```

### Expression: An Inspectable Tree Structure

`Expression<Func<Product, bool>>` preserves the same lambda expression as a tree structure. The compiler converts the lambda into a data structure rather than code.

```csharp
Expression<Func<Product, bool>> expr = p => p.Price > 1000;

// Tree structure is inspectable
Console.WriteLine(expr.Body);        // (p.Price > 1000)
Console.WriteLine(expr.Parameters);  // p
Console.WriteLine(expr.Body.NodeType); // GreaterThan
```

### Expression -> Func Compilation

An Expression can be converted to an executable Func through the `Compile()` method. This process has a cost, so caching the result is recommended.

```csharp
var compiled = expr.Compile();
var result = compiled(product); // true/false
```

### Why ORMs Need Expressions

| Aspect | Func | Expression |
|------|------|------------|
| **Internal Structure** | Opaque (black box) | Inspectable (tree) |
| **SQL Conversion** | Impossible | Possible |
| **IQueryable** | Not supported | Can be used in Where clause |
| **Execution Location** | Always in memory | DB server or memory |

## Project Description

### Project Structure
```
ExpressionIntro/                          # Main project
├── Program.cs                            # Expression Tree demo
├── Product.cs                            # Product record
├── ExpressionIntro.csproj                # Project file
ExpressionIntro.Tests.Unit/               # Test project
├── ExpressionBasicsTests.cs              # Expression basics tests
├── Using.cs                              # Global using
├── xunit.runner.json                     # xUnit configuration
├── ExpressionIntro.Tests.Unit.csproj     # Test project file
index.md                                  # This document
```

### Core Code

#### Product.cs
```csharp
public record Product(string Name, decimal Price, int Stock, string Category);
```

#### Expression Creation and Inspection
```csharp
// Func - opaque black box
Func<Product, bool> func = p => p.Price > 1000;

// Expression - inspectable tree
Expression<Func<Product, bool>> expr = p => p.Price > 1000;
Console.WriteLine($"Body: {expr.Body}");
Console.WriteLine($"Parameters: {string.Join(", ", expr.Parameters)}");

// Expression -> Func compilation
var compiled = expr.Compile();
var product = new Product("Laptop", 1_500_000, 10, "Electronics");
Console.WriteLine($"Result: {compiled(product)}");
```

## Summary at a Glance

### Func vs Expression Comparison
| Aspect | `Func<T, bool>` | `Expression<Func<T, bool>>` |
|------|-----------------|------------------------------|
| **Essence** | Compiled code | Data representation of code |
| **Inspection** | Impossible | Body, Parameters, etc. accessible |
| **SQL Conversion** | Impossible | ORM traverses tree to convert |
| **Execution** | Direct invocation | Invoke after Compile() |
| **IEnumerable** | Can be used with Where | Needs Compile |
| **IQueryable** | Cannot be used | Can be used directly with Where |

### Key Points
1. **Expression represents code as data**, allowing programmatic analysis and transformation.
2. **ORMs need Expressions to generate SQL**. With Func alone, all data must be loaded into memory.
3. **Compile() has a cost, so caching** is recommended.

## FAQ

### Q1: Where are Expression Trees used?
**A**: Primarily in ORMs (Entity Framework Core), LINQ to SQL, dynamic query builders, etc. Expression Trees are essential when condition expressions written in C# code need to be converted to SQL or other query languages.

### Q2: Should Expression always be used instead of Func?
**A**: No. When filtering in-memory collections, Func is more efficient. Expression should only be used when SQL conversion is needed. The Specification pattern uses Expression-based approaches to support both scenarios.

### Q3: How much is the performance cost of Expression.Compile()?
**A**: Compile() converts the Expression Tree to IL code, which is relatively expensive. Therefore, it is recommended to cache and reuse the compiled result. Functorium's `ExpressionSpecification` automatically performs this caching internally.

### Q4: What is AsQueryable()?
**A**: `AsQueryable()` converts `IEnumerable<T>` to `IQueryable<T>`. Since `IQueryable` supports Expression-based Where, you can test Expression-based filtering even on in-memory collections. In real projects, EF Core's `DbSet<T>` implements `IQueryable<T>`.

---

Now that you understand the concept of Expression Trees, in the next chapter we will implement the `ExpressionSpecification<T>` class that integrates them into Specifications.

-> [Chapter 2: ExpressionSpecification Class](../02-ExpressionSpecification-Class/)
