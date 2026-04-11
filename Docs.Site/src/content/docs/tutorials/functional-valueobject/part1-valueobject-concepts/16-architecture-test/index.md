---
title: "Architecture Tests"
---

## Overview

As you create multiple value objects, design rules start to gradually break down -- such as a Create method being private or forgetting to add sealed. C#'s generic constraints and interfaces alone cannot enforce these rules at compile time. In this chapter, we cover how to use ArchUnitNET to automatically verify the structural rules of value objects through runtime tests.

## Learning Objectives

Upon completing this chapter, you will be able to:

1. Build an architecture rule verification system using ArchUnitNET
2. Guarantee value object design rules that cannot be enforced at compile time through runtime tests
3. Automatically verify consistent design patterns for all classes implementing IValueObject

## Why Is This Needed?

C#'s generic constraints and interfaces alone cannot enforce that all value objects have the same method signatures. For example, while a Create method can be defined in the IValueObject interface, enforcing that it must be public static is difficult. Developers may accidentally make a Create method private, omit sealed, or write the DomainErrors class structure differently, and catching such issues through code review is cumbersome and easily missed.

By introducing architecture tests, design rules that cannot be enforced at compile time can be automatically verified in the CI pipeline.

## Core Concepts

### Architecture Tests

Architecture tests verify structural rules of code, not functional behavior. While unit tests verify "Does this method return the correct result?", architecture tests verify "Do all value objects follow the same design pattern?"

```csharp
// Previous approach (manual verification) - possibility of omission and mistakes
public class Price : ComparableSimpleValueObject<decimal>
{
    // Create method could be private or missing
    private static Price Create(decimal value) { ... } // Incorrect implementation
}

// Improved approach (automatic verification) - enforced by architecture tests
public class Price : ComparableSimpleValueObject<decimal>
{
    // Architecture test verifies existence of public static Create method
    public static Fin<Price> Create(decimal value) { ... } // Correct implementation
}
```

### Value Object Design Rule Verification

Guarantees that all classes implementing the IValueObject interface have specific structures and methods. Verifies fine-grained rules that cannot be enforced by interfaces, such as access modifiers, sealed status, constructor visibility, and method return types.

```csharp
// Architecture test rule definition
@class
    .RequirePublic()                            // public class
    .RequireSealed()                            // sealed class
    .RequireAllPrivateConstructors()            // All constructors are private
    .RequireMethod("Create", method => method
        .RequireVisibility(Visibility.Public)    // public method
        .RequireStatic()                         // static method
        .RequireReturnType(typeof(Fin<>)))       // Returns Fin<T>
```

### Domain Error Rule Verification

For value objects that have a DomainErrors nested class, verifies that the class has the correct structure. Since DomainErrors is not required for all value objects, it is verified optionally with `RequireNestedClassIfExists`.

```csharp
// DomainErrors nested class rule verification
@class
    .RequireNestedClassIfExists("DomainErrors", domainErrors =>
    {
        domainErrors
            .RequireInternal()                          // internal class
            .RequireSealed()                            // sealed class
            .RequireAllMethods(method => method
                .RequireVisibility(Visibility.Public)   // public method
                .RequireStatic()                        // static method
                .RequireReturnType(typeof(Error)));     // Returns Error
    });
```

## Practical Guidelines

### Key Implementation Points

Load the target assembly with ArchUnitNET's architecture loader, define design rules for IValueObject implementation classes, and automatically apply rules to all value object classes. Even when adding new value objects, existing tests automatically provide coverage.

## Project Description

### Project Structure
```
ArchitectureTest.Tests.Unit/                 # Architecture test project
├── ArchitectureTestBase.cs                  # Architecture test base class
├── DomainRuleTests.cs                       # Domain rule tests
├── ArchitectureTest.Tests.Unit.csproj       # Project file
└── README.md                                # Main documentation
```

### Core Code

#### ArchitectureTestBase.cs
```csharp
public abstract class ArchitectureTestBase
{
    protected static readonly Architecture Architecture = BuildArchitecture();

    private static Architecture BuildArchitecture()
    {
        List<System.Reflection.Assembly> assemblies = [];

        assemblies.AddRange([
            ArchitectureTest.AssemblyReference.Assembly,
        ]);

        return new ArchLoader()
            .LoadAssemblies(assemblies.ToArray())
            .Build();
    }
}
```

#### DomainRuleTests.cs

Applies design rules in bulk to all non-abstract classes implementing IValueObject.

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
            // Value object class rules
            @class
                .RequirePublic()
                .RequireSealed()
                .RequireAllPrivateConstructors()
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

            // DomainErrors nested class rules
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

Comparing the differences between manual code review and the architecture test approach.

### Comparison Table
| Aspect | Previous Approach | Current Approach |
|------|-----------|-----------|
| **Rule verification** | Manual code review | Automated architecture tests |
| **Consistency guarantee** | Developer-dependent | System-enforced |
| **Error detection** | Runtime or manual discovery | Detected immediately after compilation |
| **Maintenance** | Manual updates when rules change | Only tests need modification when rules change |

### Pros and Cons
| Pros | Cons |
|------|------|
| **Automated verification** | Initial setup complexity |
| **Consistent design guarantee** | ArchUnitNET dependency |
| **Immediate rule violation detection** | Increased execution time due to reflection |
| **New value objects automatically covered** | - |

## FAQ

### Q1: How do architecture tests differ from unit tests?

While unit tests verify "Does this method return the correct result?", architecture tests verify "Do all value objects follow the same design pattern?"

```csharp
// Unit test: Functional verification
[Fact]
public void Create_ShouldReturnSuccess_WhenValidValue()
{
    var result = Price.Create(100m);
    result.IsSucc.ShouldBeTrue();
}

// Architecture test: Structural verification
[Fact]
public void ValueObject_ShouldSatisfy_Rules()
{
    // Verifies that all value objects implement Create as public static
    ArchRuleDefinition.Classes()
        .That().ImplementInterface(typeof(IValueObject))
        .Should().HaveMethod("Create", method => method
            .BePublic().And().BeStatic());
}
```

### Q2: Why is the DomainErrors nested class verified optionally (IfExists)?

Not every value object needs to have DomainErrors. Simple value objects may not need complex validation logic and thus do not require DomainErrors. `RequireNestedClassIfExists` enforces the correct structure only on value objects that have DomainErrors, and skips verification for those that do not.

```csharp
// Value object requiring complex validation
public sealed class Price : ComparableSimpleValueObject<decimal>
{
    internal static class DomainErrors  // DomainErrors exists
    {
        public static Error Negative(decimal value) => ...;
    }
}

// Simple value object
public sealed class Currency : SmartEnum<Currency, string>
{
    // No DomainErrors - verification skipped
}

// Architecture test: Optional verification
@class.RequireNestedClassIfExists("DomainErrors", domainErrors =>
{
    // Apply these rules if DomainErrors exists
    domainErrors.RequireInternal().RequireSealed();
});
```

### Q3: How do you add architecture test rules?

Add calls to `RequireMethod`, `RequireImplements`, etc. in DomainRuleTests.cs. When adding a new rule, if existing value objects violate the rule, the test will fail, so it is safe to gradually modify the code before activating the rule.

```csharp
// Adding a new rule
@class
    .RequireMethod("Create", method => method
        .RequireVisibility(Visibility.Public)
        .RequireStatic())
    .RequireMethod("Validate", method => method  // New rule added
        .RequireVisibility(Visibility.Public)
        .RequireStatic()
        .RequireReturnType(typeof(Validation<,>)));

// Modifying existing rules
@class
    .RequireMethod("Create", method => method
        .RequireVisibility(Visibility.Public)
        .RequireStatic()
        .RequireReturnType(typeof(Fin<>)));  // Return type changed
```

---

Part 1 covered everything from the basics of value objects through architecture tests. In Part 2, we learn the Bind/Apply patterns for validating multiple value objects simultaneously.

→ [Part 2, Chapter 1: Sequential Validation (Bind)](../../Part2-Validation-Patterns/01-Bind-Sequential-Validation/)
