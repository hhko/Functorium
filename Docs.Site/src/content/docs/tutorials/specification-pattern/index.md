---
title: "Specification Pattern"
---

**A practical guide to implementing composable business rules with C# Functorium**

---

## About This Tutorial

If your Repository methods keep growing endlessly and business rules are scattered throughout your service code, the Specification pattern may be the answer.

This tutorial is a comprehensive course designed for step-by-step learning of **domain rule implementation using the Specification pattern**. From a basic Specification class to Expression Tree-based Repository integration, you will systematically learn every aspect of the Specification pattern through **18 hands-on projects**.

> **Experience the journey from simple conditional branching to composable business rules.**

### Target Audience

| Level | Audience | Recommended Scope |
|-------|----------|-------------------|
| **Beginner** | Developers who know basic C# syntax and want to get started with the Specification pattern | Part 1 |
| **Intermediate** | Developers who understand the pattern and want practical application | Parts 1--3 |
| **Advanced** | Developers interested in architecture design and domain modeling | Parts 4--5 + Appendix |

### Learning Objectives

After completing this tutorial, you will be able to:

1. Understand and explain the **concept and necessity of the Specification pattern**
2. Implement complex business rules using **And, Or, Not composition**
3. Implement ORM-compatible Specifications using **Expression Trees**
4. Build flexible data retrieval through **Repository and Specification integration**
5. Apply **testing strategies** for reliable domain rule verification

---

### Part 0: Introduction

The introduction covers the Specification pattern concept and environment setup.

- [0.1 Why You Should Read This Tutorial](Part0-Introduction/01-why-this-tutorial.md)
- [0.2 Prerequisites and Environment Setup](Part0-Introduction/02-prerequisites-and-setup.md)
- [0.3 Specification Pattern Overview](Part0-Introduction/03-specification-pattern-overview.md)

### Part 1: Specification Basics

Learn from basic Specifications through operator overloading to the identity element.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 1 | [First Specification](Part1-Specification-Basics/01-First-Specification/) | Inheriting Specification<T>, implementing IsSatisfiedBy |
| 2 | [Composition](Part1-Specification-Basics/02-Composition/) | And, Or, Not method composition |
| 3 | [Operators](Part1-Specification-Basics/03-Operators/) | &, |, ! operator overloading |
| 4 | [All Identity Element](Part1-Specification-Basics/04-All-Identity/) | All identity element, dynamic filter chaining |

### Part 2: Expression Specification

Prepare for ORM integration with Expression Tree-based Specifications.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 1 | [Expression Introduction](Part2-Expression-Specification/01-Expression-Introduction/) | Expression Tree concept and necessity |
| 2 | [ExpressionSpecification Class](Part2-Expression-Specification/02-ExpressionSpecification-Class/) | sealed IsSatisfiedBy, delegate caching |
| 3 | [Value Object Conversion Pattern](Part2-Expression-Specification/03-ValueObject-Primitive-Conversion/) | Value Object to primitive conversion |
| 4 | [Expression Resolver](Part2-Expression-Specification/04-Expression-Resolver/) | TryResolve, recursive composition |

### Part 3: Repository Integration

Integrate Specifications with Repositories for flexible data retrieval.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 1 | [Repository with Specification](Part3-Repository-Integration/01-Repository-With-Specification/) | Preventing Repository method explosion |
| 2 | [InMemory Implementation](Part3-Repository-Integration/02-InMemory-Implementation/) | InMemory adapter |
| 3 | [PropertyMap](Part3-Repository-Integration/03-PropertyMap/) | PropertyMap, TranslatingVisitor |
| 4 | [EF Core Implementation](Part3-Repository-Integration/04-EfCore-Implementation/) | TryResolve + Translate combination |

### Part 4: Real-World Patterns

Learn how to use the Specification pattern in production projects.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 1 | [Use-Case Patterns](Part4-Real-World-Patterns/01-Usecase-Patterns/) | Using Specs with CQRS |
| 2 | [Dynamic Filter Builder](Part4-Real-World-Patterns/02-Dynamic-Filter-Builder/) | All seed conditional chaining |
| 3 | [Testing Strategies](Part4-Real-World-Patterns/03-Testing-Strategies/) | Spec/composition/use-case testing |
| 4 | [Architecture Rules](Part4-Real-World-Patterns/04-Architecture-Rules/) | Naming, folder placement, ArchUnitNET |

### Part 5: Domain-Specific Practical Examples

Practical examples applying the Specification pattern across various domains.

- [5.1 E-Commerce Product Filtering](Part5-Domain-Examples/01-Ecommerce-Product-Filtering/)
- [5.2 Customer Management](Part5-Domain-Examples/02-Customer-Management/)

### [Appendix](Appendix/)

- [A. Specification vs Alternatives Comparison](Appendix/A-specification-vs-alternatives.md)
- [B. Anti-Patterns](Appendix/B-anti-patterns.md)
- [C. Glossary](Appendix/C-glossary.md)
- [D. References](Appendix/D-references.md)

---

## Core Evolution Process

```
Part 1
  Ch 1: First Spec          ->  Ch 2: And/Or/Not Composition  ->  Ch 3: Operator Overloading  ->  Ch 4: All Identity Element
     ↓
Part 2
  Ch 1: Expression Intro    ->  Ch 2: ExpressionSpec           ->  Ch 3: VO Conversion Pattern  ->  Ch 4: Expression Resolver
     ↓
Part 3
  Ch 1: Repository Integration ->  Ch 2: InMemory Implementation  ->  Ch 3: PropertyMap          ->  Ch 4: EF Core Implementation
     ↓
Part 4
  Ch 1: Use-Case Patterns      ->  Ch 2: Dynamic Filter           ->  Ch 3: Testing Strategies   ->  Ch 4: Architecture Rules
```

---

## Functorium Specification Type Hierarchy

The core type hierarchy covered in this tutorial is as follows.

```
Specification<T> (abstract class)
├── IsSatisfiedBy(T) : bool
├── And() / Or() / Not()
├── & / | / ! operators
└── All (identity element)

IExpressionSpec<T> (interface)
└── ToExpression() : Expression<Func<T, bool>>

ExpressionSpecification<T> : Specification<T>, IExpressionSpec<T>
├── abstract ToExpression()
├── sealed IsSatisfiedBy (compilation + caching)
└── AllSpecification<T> (internal, identity element: _ => true)

SpecificationExpressionResolver (Expression composition)
PropertyMap<TEntity, TModel> (Entity→Model conversion)
```

---

## Prerequisites

- .NET 10.0 SDK or later
- VS Code + C# Dev Kit extension
- Basic knowledge of C# syntax

---

## Project Structure

```
specification-pattern/
├── Part0-Introduction/              # Part 0: Introduction
├── Part1-Specification-Basics/      # Part 1: Specification Basics (4)
│   ├── 01-First-Specification/
│   ├── 02-Composition/
│   ├── 03-Operators/
│   └── 04-All-Identity/
├── Part2-Expression-Specification/  # Part 2: Expression Specification (4)
│   ├── 01-Expression-Introduction/
│   ├── 02-ExpressionSpecification-Class/
│   ├── 03-ValueObject-Primitive-Conversion/
│   └── 04-Expression-Resolver/
├── Part3-Repository-Integration/    # Part 3: Repository Integration (4)
│   ├── 01-Repository-With-Specification/
│   ├── 02-InMemory-Implementation/
│   ├── 03-PropertyMap/
│   └── 04-EfCore-Implementation/
├── Part4-Real-World-Patterns/       # Part 4: Real-World Patterns (4)
│   ├── 01-Usecase-Patterns/
│   ├── 02-Dynamic-Filter-Builder/
│   ├── 03-Testing-Strategies/
│   └── 04-Architecture-Rules/
├── Part5-Domain-Examples/           # Part 5: Domain-Specific Practical Examples (2)
│   ├── 01-Ecommerce-Product-Filtering/
│   └── 02-Customer-Management/
├── Appendix/                        # Appendix
└── index.md                         # This document
```

---

## Testing

All example projects in every Part include unit tests. Tests follow the [Unit Testing Guide](../../guides/testing/15a-unit-testing.md).

### Running Tests

```bash
# Build the entire tutorial
dotnet build specification-pattern.slnx

# Test the entire tutorial
dotnet test --solution specification-pattern.slnx
```

### Test Project Structure

**Part 1: Specification Basics** (4)

| Ch | Test Project | Key Test Content |
|:---:|-------------|-----------------|
| 1 | `FirstSpecification.Tests.Unit` | IsSatisfiedBy behavior verification |
| 2 | `Composition.Tests.Unit` | And, Or, Not composition verification |
| 3 | `Operators.Tests.Unit` | Operator overloading verification |
| 4 | `AllIdentity.Tests.Unit` | All identity element, dynamic chaining |

**Part 2: Expression Specification** (4)

| Ch | Test Project | Key Test Content |
|:---:|-------------|-----------------|
| 1 | `ExpressionIntro.Tests.Unit` | Expression Tree basics |
| 2 | `ExpressionSpec.Tests.Unit` | sealed IsSatisfiedBy, caching |
| 3 | `ValueObjectConversion.Tests.Unit` | VO to primitive conversion |
| 4 | `ExpressionResolver.Tests.Unit` | TryResolve, recursive composition |

**Part 3: Repository Integration** (4)

| Ch | Test Project | Key Test Content |
|:---:|-------------|-----------------|
| 1 | `RepositorySpec.Tests.Unit` | Repository + Spec integration |
| 2 | `InMemoryImpl.Tests.Unit` | InMemory adapter |
| 3 | `PropertyMapDemo.Tests.Unit` | PropertyMap, TranslatingVisitor |
| 4 | `EfCoreImpl.Tests.Unit` | EF Core TryResolve + Translate |

**Part 4: Real-World Patterns** (4)

| Ch | Test Project | Key Test Content |
|:---:|-------------|-----------------|
| 1 | `UsecasePatterns.Tests.Unit` | CQRS + Spec usage |
| 2 | `DynamicFilter.Tests.Unit` | Dynamic filter chaining |
| 3 | `TestingStrategies.Tests.Unit` | Spec testing patterns |
| 4 | `ArchitectureRules.Tests.Unit` | Architecture rule verification |

**Part 5: Domain-Specific Practical Examples** (2)

| Ch | Test Project | Key Test Content |
|:---:|-------------|-----------------|
| 1 | `EcommerceFiltering.Tests.Unit` | Product filtering Spec |
| 2 | `CustomerManagement.Tests.Unit` | Customer management Spec |

### Test Naming Convention

Follows the T1_T2_T3 naming convention:

```csharp
// Method_ExpectedResult_Scenario
[Fact]
public void IsSatisfiedBy_ReturnsTrue_WhenProductIsActive()
{
    // Arrange
    var spec = new ActiveProductSpec();
    var product = new Product { IsActive = true };
    // Act
    var actual = spec.IsSatisfiedBy(product);
    // Assert
    actual.ShouldBeTrue();
}
```

---

## Source Code

All example code for this tutorial can be found in the Functorium project:

- Framework types: `Src/Functorium/Domains/Specifications/`
- Tutorial projects: `Docs.Site/src/content/docs/tutorials/specification-pattern/`

### Related Tutorials

This tutorial is more effective when studied together with:

- **[Separating Commands and Queries with the CQRS Pattern](../cqrs-repository/)**: The IQueryPort and IRepository in the CQRS pattern use Specifications as parameters.

---

This tutorial was written based on real-world experience developing the Specification framework in the Functorium project.
