---
title: "Implementing Success-Driven Value Objects with Functional Programming"
---

**A practical guide to implementing type-safe value objects with C# Functorium**

---

## About This Tutorial

You can assign `"not-an-email"` to `string email` and the compiler says nothing. You can assign `-1` to `int age` and the same thing happens. Only at runtime does an `ArgumentException` fire, and only then do you realize, "Oh, I needed validation here too."

This tutorial solves that problem **with the type system**. By using `Email` instead of `string` and `Age` instead of `int`, invalid values simply cannot be created. Starting from a basic division function and progressing to a complete value object framework, you will experience this journey firsthand through **29 hands-on projects**.

> **"Let's build a world where invalid values in `string email` are caught at compile time, not at runtime."**

### Target Audience

The following table provides recommended learning scopes by experience level.

| Level | Audience | Recommended Scope |
|-------|----------|-------------------|
| **Beginner** | Developers who know basic C# syntax and want to get started with functional programming | Part 1 (Chapters 1--6) |
| **Intermediate** | Developers who understand functional concepts and want practical application | All of Parts 1--3 |
| **Advanced** | Developers interested in framework design and architecture | Parts 4--5 + Appendix |

### Learning Objectives

After completing this tutorial, you will be able to:

1. Write safe code using **explicit result types instead of exceptions**
2. **Express domain rules as types** and validate them at compile time
3. Implement flexible validation logic using **Bind/Apply patterns**
4. Develop production-ready value objects using the **Functorium framework**

---

### Part 0: Introduction

Explore why exception-based code is problematic and what alternative success-driven development offers.

- [0.1 Why You Should Read This Tutorial](Part0-Introduction/01-why-this-tutorial.md)
- [0.2 What Is Success-Driven Development?](Part0-Introduction/02-success-driven-development.md)
- [0.3 Environment Setup](Part0-Introduction/03-environment-setup.md)

### Part 1: Understanding Value Object Concepts

Starting from a simple example where `10 / 0` throws, you progress one step at a time through exceptions, defensive programming, functional result types, and always-valid value objects. You verify through code why each step is necessary.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 1 | [Basic Division](Part1-ValueObject-Concepts/01-Basic-Divide/) | Exceptions vs domain types |
| 2 | [Defensive Programming](Part1-ValueObject-Concepts/02-Defensive-Programming/) | Defensive programming and precondition validation |
| 3 | [Functional Result Types](Part1-ValueObject-Concepts/03-Functional-Result/) | Functional result types (Fin, Validation) |
| 4 | [Always-Valid Value Objects](Part1-ValueObject-Concepts/04-Always-Valid/) | Implementing always-valid value objects |
| 5 | [Operator Overloading](Part1-ValueObject-Concepts/05-Operator-Overloading/) | Operator overloading and type conversion |
| 6 | [LINQ Expressions](Part1-ValueObject-Concepts/06-Linq-Expression/) | LINQ expressions and functional composition |
| 7 | [Value Equality](Part1-ValueObject-Concepts/07-Value-Equality/) | Value equality and hash codes |
| 8 | [Comparability](Part1-ValueObject-Concepts/08-Value-Comparability/) | Comparability and sorting |
| 9 | [Separating Creation and Validation](Part1-ValueObject-Concepts/09-Create-Validate-Separation/) | Separation of creation and validation |
| 10 | [Validated Value Creation](Part1-ValueObject-Concepts/10-Validated-Value-Creation/) | Validated value creation pattern |
| 11 | [Framework Types](Part1-ValueObject-Concepts/11-ValueObject-Framework/) | Framework types |
| 12 | [Type-Safe Enumerations](Part1-ValueObject-Concepts/12-Type-Safe-Enums/) | Type-safe enumerations |
| 13 | [Error Codes](Part1-ValueObject-Concepts/13-Error-Code/) | Structured error codes |
| 14 | [Fluent Error Codes](Part1-ValueObject-Concepts/14-Error-Code-Fluent/) | DomainError helpers |
| 15 | [FluentValidation](Part1-ValueObject-Concepts/15-Validation-Fluent/) | FluentValidation-based validation patterns |
| 16 | [Architecture Tests](Part1-ValueObject-Concepts/16-Architecture-Test/) | Architecture tests and rules |

### Part 2: Mastering Validation Patterns

Validating a single value object is straightforward. However, when you need to validate multiple fields simultaneously and collect all errors at once, you must understand the difference between Bind and Apply.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 1 | [Sequential Validation (Bind)](Part2-Validation-Patterns/01-Bind-Sequential-Validation/) | Sequential validation with Bind |
| 2 | [Parallel Validation (Apply)](Part2-Validation-Patterns/02-Apply-Parallel-Validation/) | Parallel validation with Apply |
| 3 | [Combining Apply and Bind](Part2-Validation-Patterns/03-Apply-Bind-Combined-Validation/) | Combining Apply and Bind |
| 4 | [Inner Bind, Outer Apply](Part2-Validation-Patterns/04-Apply-Internal-Bind-Validation/) | Inner Bind with outer Apply |
| 5 | [Inner Apply, Outer Bind](Part2-Validation-Patterns/05-Bind-Internal-Apply-Validation/) | Inner Apply with outer Bind |
| 6 | [Contextual Validation](Part2-Validation-Patterns/06-Contextual-Validation/) | ContextualValidation -- field-name-based validation |

### Part 3: Completing Value Object Patterns

Assemble the concepts learned in Parts 1--2 into the Functorium framework's base classes. Complete production-ready patterns from single-value wrappers to composite value objects and type-safe enumerations.

| Ch | Topic | Framework Type |
|:---:|-------|----------------|
| 1 | [SimpleValueObject](Part3-ValueObject-Patterns/01-SimpleValueObject/) | `SimpleValueObject<T>` |
| 2 | [ComparableSimpleValueObject](Part3-ValueObject-Patterns/02-ComparableSimpleValueObject/) | `ComparableSimpleValueObject<T>` |
| 3 | [ValueObject (Primitive)](Part3-ValueObject-Patterns/03-ValueObject-Primitive/) | `ValueObject` |
| 4 | [ComparableValueObject (Primitive)](Part3-ValueObject-Patterns/04-ComparableValueObject-Primitive/) | `ComparableValueObject` |
| 5 | [ValueObject (Composite)](Part3-ValueObject-Patterns/05-ValueObject-Composite/) | `ValueObject` |
| 6 | [ComparableValueObject (Composite)](Part3-ValueObject-Patterns/06-ComparableValueObject-Composite/) | `ComparableValueObject` |
| 7 | [TypeSafeEnum](Part3-ValueObject-Patterns/07-TypeSafeEnum/) | `SmartEnum + IValueObject` |
| 8 | [Architecture Tests](Part3-ValueObject-Patterns/08-Architecture-Test/) | `ArchUnitNET` |
| 9 | [UnionValueObject](Part3-ValueObject-Patterns/09-UnionValueObject/) | `UnionValueObject` (Discriminated Union) |

### Part 4: Practical Guide

Covers real-world issues that arise when integrating value objects with infrastructure like EF Core and CQRS.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 1 | [Functorium Framework Integration](Part4-Practical-Guide/01-Functorium-Framework/) | Integrating value objects with the Functorium framework |
| 2 | [ORM Integration Patterns](Part4-Practical-Guide/02-ORM-Integration/) | Integrating value objects with EF Core |
| 3 | [CQRS and Value Objects](Part4-Practical-Guide/03-CQRS-Integration/) | Using value objects in CQRS patterns |
| 4 | [Testing Strategies](Part4-Practical-Guide/04-Testing-Strategies/) | Value object testing strategies |

### Part 5: Domain-Specific Practical Examples

See how value objects are used in real domains including e-commerce, finance, user management, and scheduling.

| Ch | Topic | Value Object Examples |
|:---:|-------|---------------------|
| 1 | [E-Commerce Domain](Part5-Domain-Examples/01-Ecommerce-Domain/) | Money, ProductCode, Quantity, OrderStatus |
| 2 | [Finance Domain](Part5-Domain-Examples/02-Finance-Domain/) | AccountNumber, InterestRate, ExchangeRate |
| 3 | [User Management Domain](Part5-Domain-Examples/03-User-Management-Domain/) | Email, Password, PhoneNumber |
| 4 | [Scheduling Domain](Part5-Domain-Examples/04-Scheduling-Domain/) | DateRange, TimeSlot, Duration |

### [Appendix](Appendix/)

- [A. LanguageExt Key Type Reference](Appendix/A-languageext-reference.md)
- [B. Framework Type Selection Guide](Appendix/B-type-selection-guide.md)
- [C. Glossary](Appendix/C-glossary.md)
- [D. References](Appendix/D-references.md)
- [E. FAQ](Appendix/E-faq.md)

---

## Core Evolution Process

[Part 1] Understanding Value Object Concepts
Ch 1: Basic Division  ->  Ch 2: Defensive Programming  ->  Ch 3: Functional Result Types  ->  Ch 4: Always-Valid Value Objects  ->  Ch 5: Operator Overloading  ->  Ch 6: LINQ Expressions  ->  Ch 7: Value Equality  ->  Ch 8: Comparability  ->  Ch 9: Separating Creation and Validation  ->  Ch 10: Validated Value Creation  ->  Ch 11: Framework Types  ->  Ch 12: Type-Safe Enumerations  ->  Ch 13: Error Codes  ->  Ch 14: Fluent Error Codes  ->  Ch 15: FluentValidation  ->  Ch 16: Architecture Tests

[Part 2] Mastering Validation Patterns
Ch 1: Sequential Validation (Bind)  ->  Ch 2: Parallel Validation (Apply)  ->  Ch 3: Combining Apply and Bind  ->  Ch 4: Inner Bind, Outer Apply  ->  Ch 5: Inner Apply, Outer Bind  ->  Ch 6: Contextual Validation

[Part 3] Completing Value Object Patterns
Ch 1: SimpleValueObject  ->  Ch 2: ComparableSimpleValueObject  ->  Ch 3: ValueObject (Primitive)  ->  Ch 4: ComparableValueObject (Primitive)  ->  Ch 5: ValueObject (Composite)  ->  Ch 6: ComparableValueObject (Composite)  ->  Ch 7: TypeSafeEnum  ->  Ch 8: Architecture Tests  ->  Ch 9: UnionValueObject

[Part 4] Practical Guide
Ch 1: Functorium Framework Integration  ->  Ch 2: ORM Integration Patterns  ->  Ch 3: CQRS and Value Objects  ->  Ch 4: Testing Strategies

[Part 5] Domain-Specific Practical Examples
Ch 1: E-Commerce Domain  ->  Ch 2: Finance Domain  ->  Ch 3: User Management Domain  ->  Ch 4: Scheduling Domain

---

## Prerequisites

- .NET 10.0 SDK or later
- VS Code + C# Dev Kit extension
- Basic knowledge of C# syntax

---

## Project Structure

```
functional-valueobject/
├── Part0-Introduction/                # Part 0: Introduction
├── Part1-ValueObject-Concepts/        # Part 1: Understanding Value Object Concepts (16)
│   ├── 01-Basic-Divide/
│   ├── 02-Defensive-Programming/
│   ├── ...
│   └── 16-Architecture-Test/
├── Part2-Validation-Patterns/         # Part 2: Mastering Validation Patterns (6)
│   ├── 01-Bind-Sequential-Validation/
│   ├── 02-Apply-Parallel-Validation/
│   ├── ...
│   └── 06-Contextual-Validation/
├── Part3-ValueObject-Patterns/        # Part 3: Completing Value Object Patterns (9)
│   ├── 01-SimpleValueObject/
│   ├── 02-ComparableSimpleValueObject/
│   ├── ...
│   └── 09-UnionValueObject/
├── Part4-Practical-Guide/             # Part 4: Practical Guide (4)
│   ├── 01-Functorium-Framework/
│   ├── 02-ORM-Integration/
│   ├── 03-CQRS-Integration/
│   └── 04-Testing-Strategies/
├── Part5-Domain-Examples/             # Part 5: Domain-Specific Practical Examples (4)
│   ├── 01-Ecommerce-Domain/
│   ├── 02-Finance-Domain/
│   ├── 03-User-Management-Domain/
│   └── 04-Scheduling-Domain/
├── Appendix/                          # Appendix
└── index.md                           # This document
```

---

## Testing

All example projects in every Part include unit tests. Tests follow the [Unit Testing Guide](../../guides/testing/15a-unit-testing.md).

### Running Tests

```bash
# Build the entire tutorial
dotnet build functional-valueobject.slnx

# Test the entire tutorial
dotnet test --solution functional-valueobject.slnx
```

### Test Project Structure

**Part 1: Understanding Value Object Concepts** (16)

| Ch | Test Project | Key Test Content |
|:---:|-------------|-----------------|
| 1 | `BasicDivide.Tests.Unit` | Exception vs domain type division |
| 2 | `DefensiveProgramming.Tests.Unit` | Defensive programming, precondition validation |
| 3 | `FunctionalResult.Tests.Unit` | Fin, Validation result types |
| 4 | `AlwaysValid.Tests.Unit` | Always-valid value object creation |
| 5 | `OperatorOverloading.Tests.Unit` | Operator overloading, type conversion |
| 6 | `LinqExpression.Tests.Unit` | LINQ expressions, functional composition |
| 7 | `ValueEquality.Tests.Unit` | Value equality, hash codes |
| 8 | `ValueComparability.Tests.Unit` | Comparability, sorting |
| 9 | `CreateValidateSeparation.Tests.Unit` | Separation of creation and validation |
| 10 | `ValidatedValueCreation.Tests.Unit` | Validated value creation pattern |
| 11 | `ValueObjectFramework.Tests.Unit` | Framework type verification |
| 12 | `TypeSafeEnums.Tests.Unit` | Type-safe enumerations |
| 13 | `ErrorCode.Tests.Unit` | Structured error codes |
| 14 | `ErrorCodeFluent.Tests.Unit` | DomainError helpers |
| 15 | `ValidationFluent.Tests.Unit` | FluentValidation-based validation |
| 16 | `ArchitectureTest.Tests.Unit` | Architecture rule verification |

**Part 2: Mastering Validation Patterns** (6)

| Ch | Test Project | Key Test Content |
|:---:|-------------|-----------------|
| 1 | `BindSequentialValidation.Tests.Unit` | Bind sequential validation |
| 2 | `ApplyParallelValidation.Tests.Unit` | Apply parallel validation |
| 3 | `ApplyBindCombinedValidation.Tests.Unit` | Apply+Bind combined validation |
| 4 | `ApplyInternalBindValidation.Tests.Unit` | Inner Bind, outer Apply |
| 5 | `BindInternalApplyValidation.Tests.Unit` | Inner Apply, outer Bind |
| 6 | `ContextualValidation.Tests.Unit` | Contextual validation |

**Part 3: Completing Value Object Patterns** (8)

| Ch | Test Project | Key Test Content |
|:---:|-------------|-----------------|
| 1 | `SimpleValueObject.Tests.Unit` | SimpleValueObject verification |
| 2 | `ComparableSimpleValueObject.Tests.Unit` | ComparableSimpleValueObject verification |
| 3 | `ValueObjectPrimitive.Tests.Unit` | ValueObject (Primitive) verification |
| 4 | `ComparableValueObjectPrimitive.Tests.Unit` | ComparableValueObject (Primitive) verification |
| 5 | `ValueObjectComposite.Tests.Unit` | ValueObject (Composite) verification |
| 6 | `ComparableValueObjectComposite.Tests.Unit` | ComparableValueObject (Composite) verification |
| 7 | `TypeSafeEnum.Tests.Unit` | SmartEnum + IValueObject verification |
| 9 | `UnionValueObject.Tests.Unit` | UnionValueObject (Discriminated Union) verification |

**Part 4: Practical Guide** (4)

| Ch | Test Project | Key Test Content |
|:---:|-------------|-----------------|
| 1 | `FunctoriumFramework.Tests.Unit` | Functorium framework integration |
| 2 | `OrmIntegration.Tests.Unit` | EF Core value object integration |
| 3 | `CqrsIntegration.Tests.Unit` | CQRS pattern value object usage |
| 4 | `TestingStrategies.Tests.Unit` | Value object testing strategies |

**Part 5: Domain-Specific Practical Examples** (4)

| Ch | Test Project | Key Test Content |
|:---:|-------------|-----------------|
| 1 | `EcommerceDomain.Tests.Unit` | Money, ProductCode, Quantity, OrderStatus |
| 2 | `FinanceDomain.Tests.Unit` | AccountNumber, InterestRate, ExchangeRate |
| 3 | `UserManagementDomain.Tests.Unit` | Email, Password, PhoneNumber |
| 4 | `SchedulingDomain.Tests.Unit` | DateRange, TimeSlot, Duration |

### Test Naming Convention

Follows the T1_T2_T3 naming convention:

```csharp
// Method_ExpectedResult_Scenario
[Fact]
public void Create_ReturnsSuccess_WhenValueIsValid()
{
    // Arrange
    var input = "user@example.com";
    // Act
    var actual = Email.Create(input);
    // Assert
    actual.IsSucc.ShouldBeTrue();
}
```

---

## Source Code

All example code for this tutorial can be found in the Functorium project:

- Framework types: `Src/Functorium/Domains/ValueObjects/`
- Tutorial projects: `Docs.Site/src/content/docs/tutorials/functional-valueobject/`

### Related Tutorials

- **[Specification Pattern](../specification-pattern/)**: Learn how to combine ValueObject and Specification to express domain rules.
- **[Architecture Rule Tests](../architecture-rules/)**: Learn how to automate architecture rule verification for ValueObjects.

---

This tutorial was written based on real-world experience developing the value object framework in the Functorium project.
