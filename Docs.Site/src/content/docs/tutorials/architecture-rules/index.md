---
title: "Architecture Rule Tests"
---

**A practical guide to architecture testing with the Functorium ArchitectureRules framework**

---

## About This Tutorial

Recurring code review comments -- missing `sealed`, dependency direction violations, naming convention inconsistencies. Should a human always have to verify these by eye? What if there were **tests that automatically verify these design rules right after compilation**?

This tutorial is a comprehensive course designed for step-by-step learning of **architecture test implementation using the Functorium ArchitectureRules framework**. From basic class verification to real-world layer architecture rules, you will systematically learn every aspect of architecture testing through **16 hands-on projects**.

> **"This class should be sealed" -- let's build a world where a failing test tells you this, not a code review comment.**

### Target Audience

| Level | Audience | Recommended Scope |
|-------|----------|-------------------|
| **Beginner** | Developers with C# unit testing experience who want to get started with architecture testing | Parts 0--1 |
| **Intermediate** | Developers who understand architecture testing basics and want advanced verification | Parts 2--3 |
| **Advanced** | Developers who want to adopt architecture rules in production projects | Parts 4--5 + Appendix |

### Learning Objectives

After completing this tutorial, you will be able to:

1. **Type-level architecture rule verification**
   - Enforce visibility, modifier, naming, and inheritance rules with ClassValidator/InterfaceValidator
   - Load assemblies to automatically collect types for verification
2. **Member-level signature verification**
   - Verify method signatures, return types, and parameters with MethodValidator
   - Enforce property immutability and field access rules
3. **Compose team-specific custom rules**
   - Create reusable rule combinations with DelegateArchRule/CompositeArchRule
4. **Automate DDD layer architecture design consistency**
   - Apply rules per Domain/Application/Adapter layer
   - Verify dependency direction by combining ArchUnitNET and Functorium
5. **Use pre-built test suites**
   - Instantly apply to projects by inheriting DomainArchitectureTestSuite / ApplicationArchitectureTestSuite

---

### Part 0: Introduction

Introduce the need for architecture testing and the framework.

- [0.1 Why Architecture Testing?](Part0-Introduction/01-why-architecture-testing.md)
- [0.2 ArchUnitNET and Functorium](Part0-Introduction/02-archunitnet-and-functorium.md)
- [0.3 Environment Setup](Part0-Introduction/03-environment-setup.md)

### Part 1: ClassValidator Basics

Learn the core verification methods of ClassValidator.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 1 | [First Architecture Test](Part1-ClassValidator-Basics/01-First-Architecture-Test/) | ArchRuleDefinition, ValidateAllClasses, RequirePublic, RequireSealed |
| 2 | [Visibility and Modifiers](Part1-ClassValidator-Basics/02-Visibility-And-Modifiers/) | RequireInternal, RequireStatic, RequireAbstract, RequireRecord |
| 3 | [Naming Rules](Part1-ClassValidator-Basics/03-Naming-Rules/) | RequireNameStartsWith, RequireNameEndsWith, RequireNameMatching |
| 4 | [Inheritance and Interfaces](Part1-ClassValidator-Basics/04-Inheritance-And-Interface/) | RequireInherits, RequireImplements, RequireImplementsGenericInterface |

### Part 2: Method and Property Verification

Learn method signature verification through MethodValidator.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 1 | [Method Verification](Part2-Method-And-Property-Validation/01-Method-Validation/) | RequireMethod, RequireAllMethods, RequireVisibility, RequireExtensionMethod |
| 2 | [Return Type Verification](Part2-Method-And-Property-Validation/02-Return-Type-Validation/) | RequireReturnType, RequireReturnTypeOfDeclaringClass, RequireReturnTypeContaining |
| 3 | [Parameter Verification](Part2-Method-And-Property-Validation/03-Parameter-Validation/) | RequireParameterCount, RequireFirstParameterTypeContaining |
| 4 | [Property and Field Verification](Part2-Method-And-Property-Validation/04-Property-And-Field-Validation/) | RequireProperty, RequireNoPublicSetters, RequireNoInstanceFields |

### Part 3: Advanced Verification

Learn immutability rules, nested classes, interface verification, and custom rules.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 1 | [Immutability Rules](Part3-Advanced-Validation/01-Immutability-Rule/) | RequireImmutable, ImmutabilityRule 6-dimension verification |
| 2 | [Nested Class Verification](Part3-Advanced-Validation/02-Nested-Class-Validation/) | RequireNestedClass, RequireNestedClassIfExists |
| 3 | [Interface Verification](Part3-Advanced-Validation/03-Interface-Validation/) | ValidateAllInterfaces, InterfaceValidator |
| 4 | [Custom Rules](Part3-Advanced-Validation/04-Custom-Rules/) | DelegateArchRule, CompositeArchRule, Apply |

### Part 4: Real-World Patterns

Apply architecture tests to DDD layer architectures.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 1 | [Domain Layer Rules](Part4-Real-World-Patterns/01-Domain-Layer-Rules/) | Entity, ValueObject, DomainEvent, DomainService comprehensive verification |
| 2 | [Application Layer Rules](Part4-Real-World-Patterns/02-Application-Layer-Rules/) | Command/Query, Usecase, DTO rules |
| 3 | [Adapter Layer Rules](Part4-Real-World-Patterns/03-Adapter-Layer-Rules/) | Port Interface, Adapter Implementation, RequireVirtual rules |
| 4 | [Layer Dependency Rules](Part4-Real-World-Patterns/04-Layer-Dependency-Rules/) | ArchUnitNET dependency rules + Functorium rules integration |
| 5 | [Architecture Test Suites](Part4-Real-World-Patterns/05-Architecture-Test-Suites/) | DomainArchitectureTestSuite, ApplicationArchitectureTestSuite inheritance |

### Part 5: Conclusion

Provide best practices and guidance for next steps.

- [5.1 Best Practices](Part5-Conclusion/01-best-practices.md)
- [5.2 Next Steps](Part5-Conclusion/02-next-steps.md)

### [Appendix](Appendix/)

- [A. API Reference](Appendix/A-api-reference.md)
- [B. ArchUnitNET Cheat Sheet](Appendix/B-archunitnet-cheatsheet.md)
- [C. FAQ](Appendix/C-faq.md)

---

## Core Evolution Process

[Part 1] ClassValidator Basics
Ch 1: First Architecture Test  ->  Ch 2: Visibility and Modifiers  ->  Ch 3: Naming Rules  ->  Ch 4: Inheritance and Interfaces

[Part 2] Method and Property Verification
Ch 1: Method Verification  ->  Ch 2: Return Type Verification  ->  Ch 3: Parameter Verification  ->  Ch 4: Property and Field Verification

[Part 3] Advanced Verification
Ch 1: Immutability Rules  ->  Ch 2: Nested Classes  ->  Ch 3: Interface Verification  ->  Ch 4: Custom Rules

[Part 4] Real-World Patterns
Ch 1: Domain Layer  ->  Ch 2: Application Layer  ->  Ch 3: Adapter Layer  ->  Ch 4: Layer Dependencies  ->  Ch 5: Test Suites

---

## Prerequisites

- .NET 10.0 SDK or later
- VS Code + C# Dev Kit extension
- Basic experience with C# unit testing

---

## Project Structure

```txt
architecture-rules/
├── index.md
├── Part0-Introduction/
│   ├── 01-why-architecture-testing.md
│   ├── 02-archunitnet-and-functorium.md
│   └── 03-environment-setup.md
├── Part1-ClassValidator-Basics/
│   ├── 01-First-Architecture-Test/
│   ├── 02-Visibility-And-Modifiers/
│   ├── 03-Naming-Rules/
│   └── 04-Inheritance-And-Interface/
├── Part2-Method-And-Property-Validation/
│   ├── 01-Method-Validation/
│   ├── 02-Return-Type-Validation/
│   ├── 03-Parameter-Validation/
│   └── 04-Property-And-Field-Validation/
├── Part3-Advanced-Validation/
│   ├── 01-Immutability-Rule/
│   ├── 02-Nested-Class-Validation/
│   ├── 03-Interface-Validation/
│   └── 04-Custom-Rules/
├── Part4-Real-World-Patterns/
│   ├── 01-Domain-Layer-Rules/
│   ├── 02-Application-Layer-Rules/
│   ├── 03-Adapter-Layer-Rules/
│   ├── 04-Layer-Dependency-Rules/
│   └── 05-Architecture-Test-Suites/
├── Part5-Conclusion/
│   ├── 01-best-practices.md
│   └── 02-next-steps.md
└── Appendix/
    ├── A-api-reference.md
    ├── B-archunitnet-cheatsheet.md
    └── C-faq.md
```

---

## Testing

All example projects in every Part include unit tests. Tests follow the [Unit Testing Guide](../../guides/testing/15a-unit-testing.md).

### Running Tests

```bash
# Test an individual chapter
dotnet test --project Docs.Site/src/content/docs/tutorials/architecture-rules/Part1-ClassValidator-Basics/01-First-Architecture-Test/FirstArchitectureTest.Tests.Unit

# Test the entire solution
dotnet test --solution architecture-rules.slnx
```

### Test Project Structure

| Part | Ch | Test Project |
|:----:|:---:|-------------|
| 1 | 1 | `FirstArchitectureTest.Tests.Unit` |
| 1 | 2 | `VisibilityAndModifiers.Tests.Unit` |
| 1 | 3 | `NamingRules.Tests.Unit` |
| 1 | 4 | `InheritanceAndInterface.Tests.Unit` |
| 2 | 1 | `MethodValidation.Tests.Unit` |
| 2 | 2 | `ReturnTypeValidation.Tests.Unit` |
| 2 | 3 | `ParameterValidation.Tests.Unit` |
| 2 | 4 | `PropertyAndFieldValidation.Tests.Unit` |
| 3 | 1 | `ImmutabilityRule.Tests.Unit` |
| 3 | 2 | `NestedClassValidation.Tests.Unit` |
| 3 | 3 | `InterfaceValidation.Tests.Unit` |
| 3 | 4 | `CustomRules.Tests.Unit` |
| 4 | 1 | `DomainLayerRules.Tests.Unit` |
| 4 | 2 | `ApplicationLayerRules.Tests.Unit` |
| 4 | 3 | `AdapterLayerRules.Tests.Unit` |
| 4 | 4 | `LayerDependencyRules.Tests.Unit` |
| 4 | 5 | `ArchitectureTestSuites.Tests.Unit` |

### Test Naming Convention

```txt
T1_T2_T3
│  │  └─ T3: Condition/Scenario
│  └──── T2: Expected behavior (ShouldBe, ShouldHave, ShouldNotDependOn)
└─────── T1: Verification target (DomainClasses, ValueObject, Entity)

Example: DomainClasses_ShouldBe_PublicAndSealed
```

---

## Source Code

All example code for this tutorial can be found in the Functorium project:

- Framework types: `Src/Functorium.Testing/Assertions/ArchitectureRules/`
- Tutorial projects: `Docs.Site/src/content/docs/tutorials/architecture-rules/`

### Related Tutorials

- **[Implementing Success-Driven Value Objects with Functional Programming](../functional-valueobject/)**: Learn the ValueObject implementation patterns that architecture rules verify.

---

This tutorial was written based on real-world experience developing the architecture testing framework in the Functorium project.
