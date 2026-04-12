---
title: "Value Object Architecture Test"
---

## Overview

If a developer accidentally omits the `sealed` keyword or implements the `Create` method signature differently, it is easy to miss in code review. Architecture tests using ArchUnitNET automatically verify that all value objects in projects 01-07 correctly comply with the rules on every build.

## Learning Objectives

1. Automatically verify structural rules (sealed, private constructor, Create/Validate methods, etc.) of value object classes with ArchUnitNET.
2. Verify multiple assemblies in a single test using the AssemblyReference pattern.
3. Ensure continuous quality by integrating architecture tests into the CI/CD pipeline.

## Why Is This Needed?

In large projects, manually reviewing all value object implementations is impractical. Even if a developer accidentally violates ValueObject rules, it can be missed in code review. Team members may implement in different ways, or new developers may implement differently without knowing existing rules. Architecture rules may be unintentionally violated during refactoring, and such changes may not be discovered immediately.

Architecture tests resolve this problem. They automatically verify architecture rules on every code change, so rule violations are discovered before commits.

## Core Concepts

### Architecture Testing

Architecture testing verifies the structural characteristics of code. It automatically checks class accessibility, method signatures, inheritance relationships, etc. to guarantee architecture rule compliance.

Verification rules are expressed declaratively using ArchUnitNET's Fluent API.

```csharp
// Architecture test example
ArchRuleDefinition
    .Classes()
    .That()
    .ImplementInterface(typeof(IValueObject))
    .Should()
    .BeSealed()                    // Must be a sealed class
    .And()
    .HaveMethod("Create")          // Must have Create method
    .And()
    .HaveMethod("Validate");       // Must have Validate method
```

### Multi-Assembly Validation

Multiple assembly classes can be verified simultaneously in a single test. All value objects from 7 projects are verified at once to guarantee overall consistency.

`BuildArchitecture()` loads all target assemblies.

```csharp
// Multi-assembly architecture configuration
protected static readonly Architecture Architecture = BuildArchitecture();

private static Architecture BuildArchitecture()
{
    List<System.Reflection.Assembly> assemblies = [];

    assemblies.AddRange([
        SimpleValueObject.AssemblyReference.Assembly,
        ComparableSimpleValueObject.AssemblyReference.Assembly,
        ValueObjectPrimitive.AssemblyReference.Assembly,
        // ... all 7 projects included
    ]);

    return new ArchLoader()
        .LoadAssemblies(assemblies.ToArray())
        .Build();
}
```

When a new project is added, simply add the assembly to this array.

### AssemblyReference Pattern

Each project has an `AssemblyReference` class so that the test project can safely reference the assembly at compile time.

```csharp
// AssemblyReference.cs in each project
public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
```

## Practical Guidelines

### How to Run Each Project

```bash
# Run architecture tests
cd 03-Patterns/08-Architecture-Test/ArchitectureTest
dotnet test

# Run specific test only
dotnet test --filter "ValueObject_ShouldSatisfy_Rules"
```

### Expected Output Example

```
=== Architecture Test Execution ===

Validating 7 assemblies:
  - SimpleValueObject
  - ComparableSimpleValueObject
  - ValueObjectPrimitive
  - ComparableValueObjectPrimitive
  - ValueObjectComposite
  - ComparableValueObjectComposite
  - TypeSafeEnum

Test passed: ValueObject Rule
```

## Project Description

### Project Structure

```
08-Architecture-Test/
├── ArchitectureTest/                    # Test project
│   ├── ArchitectureTestBase.cs         # Architecture test base class
│   ├── DomainRuleTests.cs              # Domain rule tests
│   └── ArchitectureTest.csproj         # Project file
└── README.md                           # This document
```

### Core Code

`ArchitectureTestBase` loads assemblies from 7 projects to configure the `Architecture` instance.

#### ArchitectureTestBase.cs
```csharp
public abstract class ArchitectureTestBase
{
    protected static readonly Architecture Architecture = BuildArchitecture();

    private static Architecture BuildArchitecture()
    {
        List<System.Reflection.Assembly> assemblies = [];

        assemblies.AddRange([
            SimpleValueObject.AssemblyReference.Assembly,
            ComparableSimpleValueObject.AssemblyReference.Assembly,
            ValueObjectPrimitive.AssemblyReference.Assembly,
            ComparableValueObjectPrimitive.AssemblyReference.Assembly,
            ValueObjectComposite.AssemblyReference.Assembly,
            ComparableValueObjectComposite.AssemblyReference.Assembly,
            TypeSafeEnum.AssemblyReference.Assembly
        ]);

        return new ArchLoader()
            .LoadAssemblies(assemblies.ToArray())
            .Build();
    }
}
```

`DomainRuleTests` verifies sealed, private constructor, Create/Validate method signatures, immutability, etc. for all classes implementing `IValueObject`.

#### DomainRuleTests.cs
```csharp
[Fact]
public void ValueObject_ShouldSatisfy_Rules()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ImplementInterface(typeof(IValueObject))
        .And()
        .AreNotAbstract()
        .ValidateAllClasses(Architecture, @class =>
        {
            // Value object class rule verification
            @class
                .RequirePublic()
                .RequireSealed()
                .RequireAllPrivateConstructors()
                .RequireImmutable()
                .RequireMethod(IValueObject.CreateMethodName, method => method
                    .RequireVisibility(Visibility.Public)
                    .RequireStatic()
                    .RequireReturnType(typeof(Fin<>)))
                .RequireMethod(IValueObject.CreateFromValidatedMethodName, method => method
                    .RequireVisibility(Visibility.Public)
                    .RequireStatic()
                    .RequireReturnTypeOfDeclaringClass())
                .RequireMethod(IValueObject.ValidateMethodName, method => method
                    .RequireVisibility(Visibility.Public)
                    .RequireStatic()
                    .RequireReturnType(typeof(Validation<,>)))
                .RequireImplements(typeof(IEquatable<>));

            // DomainErrors nested class rule verification
            @class
                .RequireNestedClassIfExists(IValueObject.DomainErrorsNestedClassName, domainErrors =>
                {
                    domainErrors
                        .RequireInternal()
                        .RequireSealed()
                        .RequireAllMethods(method => method
                            .RequireVisibility(Visibility.Public)
                            .RequireStatic()
                            .RequireReturnType(typeof(Error)));
                });
        }, _output)
        .ThrowIfAnyFailures("ValueObject Rule");
}
```

## Summary at a Glance

### Projects Under Verification

The 7 projects verified by architecture tests and their respective verification content.

| Order | Project | Verification Content | AssemblyReference |
|------|----------|-----------|-------------------|
| **01** | `SimpleValueObject` | Non-comparable single value object | Included |
| **02** | `ComparableSimpleValueObject` | Comparable single value object | Included |
| **03** | `ValueObjectPrimitive` | Non-comparable composite primitive type | Included |
| **04** | `ComparableValueObjectPrimitive` | Comparable composite primitive type | Included |
| **05** | `ValueObjectComposite` | Non-comparable composite value object | Included |
| **06** | `ComparableValueObjectComposite` | Comparable composite value object | Included |
| **07** | `TypeSafeEnum` | Type-safe enumeration | Included |

### Verification Rules

The list of rules applied to each value object class.

| Rule | Description |
|------|------|
| **Public class** | All value objects must be public |
| **Sealed class** | Must be sealed to prevent inheritance |
| **Private constructors** | All constructors must be private |
| **Immutability** | All properties must be read-only |
| **Create method** | public static, returns `Fin<T>` |
| **Validate method** | public static, returns `Validation<,>` |
| **IEquatable implementation** | Required for value equality |

## FAQ

### Q1: What should I do if an architecture test fails?
**A**: Check the failure message to identify which rule was violated, then fix the class. For example, if the `sealed` keyword is missing, add `sealed` to the class and re-run the test.

### Q2: How do I add a new project?
**A**: Add an `AssemblyReference.cs` file to the new project and add the new assembly to the `BuildArchitecture()` method in `ArchitectureTestBase`. When you run the test, the new project is automatically verified.

### Q3: What is the difference between architecture tests and unit tests?
**A**: Unit tests verify the behavioral correctness of individual methods or classes. Architecture tests verify that the structure of the code (class accessibility, method signatures, inheritance relationships, etc.) complies with rules. Using both together ensures both functional correctness and structural consistency.

In Part 3, we have completed all value object patterns using framework base classes. In Part 4, we cover how to integrate these value objects with real infrastructure like EF Core and CQRS.

-> [Chapter 9: UnionValueObject](../09-UnionValueObject/)
