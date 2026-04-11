---
title: "Visibility and Modifiers"
---
## Overview

What happens when a cache class intended for internal implementation accidentally becomes `public`? External modules start referencing it directly, and when you later try to change the internal implementation, it is already depended upon in many places and becomes impossible to modify. In this chapter, you will learn how to prevent such problems by enforcing **visibility and modifiers** through architecture tests.

> **"A class's visibility and modifiers are expressions of design intent. Architecture tests guarantee that this intent is consistently maintained across the entire codebase."**

## Learning Objectives

### Core Learning Goals
1. **Visibility verification**
   - `RequirePublic()`: Verify classes that must be publicly exposed
   - `RequireInternal()`: Verify internal implementation classes
2. **Modifier verification**
   - `RequireSealed()` / `RequireNotSealed()`: Verify sealed status
   - `RequireAbstract()` / `RequireNotAbstract()`: Verify abstract status
   - `RequireStatic()` / `RequireNotStatic()`: Verify static status
3. **Type kind verification**
   - `RequireRecord()` / `RequireNotRecord()`: Verify record type status

### What You Will Verify Through Practice
- Apply visibility rules separately by namespace
- Verify `abstract`, `sealed`, `static`, and `record` modifiers individually

## Project Structure

```
02-Visibility-And-Modifiers/
├── VisibilityAndModifiers/                   # Main project
│   ├── Domains/
│   │   ├── Order.cs                          # public sealed class
│   │   ├── OrderSummary.cs                   # public sealed record
│   │   ├── DomainEvent.cs                    # public abstract class
│   │   └── OrderCreatedEvent.cs              # sealed class (inherits DomainEvent)
│   ├── Services/
│   │   └── OrderFormatter.cs                 # public static class
│   ├── Internal/
│   │   └── OrderCache.cs                     # internal sealed class
│   ├── Program.cs
│   └── VisibilityAndModifiers.csproj
├── VisibilityAndModifiers.Tests.Unit/        # Test project
│   ├── ArchitectureTests.cs
│   ├── VisibilityAndModifiers.Tests.Unit.csproj
│   └── xunit.runner.json
└── README.md
```

## Code Under Verification

The project consists of classes with various visibility levels and modifiers:

| Class | Namespace | Visibility | Modifier | Type |
|-------|-----------|------------|----------|------|
| `Order` | Domains | public | sealed | class |
| `OrderSummary` | Domains | public | sealed | record |
| `DomainEvent` | Domains | public | abstract | class |
| `OrderCreatedEvent` | Domains | public | sealed | class |
| `OrderFormatter` | Services | public | static | class |
| `OrderCache` | Internal | internal | sealed | class |

## Test Code Walkthrough

### Visibility Verification

```csharp
[Fact]
public void DomainClasses_ShouldBe_Public()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .ValidateAllClasses(Architecture, @class => @class
            .RequirePublic(),
            verbose: true)
        .ThrowIfAnyFailures("Domain Class Visibility Rule");
}

[Fact]
public void InternalClasses_ShouldBe_Internal()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(InternalNamespace)
        .ValidateAllClasses(Architecture, @class => @class
            .RequireInternal(),
            verbose: true)
        .ThrowIfAnyFailures("Internal Class Visibility Rule");
}
```

`RequirePublic()` and `RequireInternal()` verify class visibility. By separating rules by namespace, you enforce that domain classes remain public and internal implementation classes remain private.

### Abstract vs Sealed Verification

```csharp
[Fact]
public void AbstractClasses_ShouldBe_Abstract()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .And()
        .AreAbstract()
        .ValidateAllClasses(Architecture, @class => @class
            .RequireAbstract(),
            verbose: true)
        .ThrowIfAnyFailures("Abstract Class Rule");
}
```

`AreAbstract()` is an ArchUnitNET filtering method that narrows the verification targets first, then applies the rule with `RequireAbstract()`.

### Static Class Verification

```csharp
[Fact]
public void ServiceClasses_ShouldBe_Static()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(ServiceNamespace)
        .ValidateAllClasses(Architecture, @class => @class
            .RequireStatic(),
            verbose: true)
        .ThrowIfAnyFailures("Service Class Static Rule");
}
```

In C#, a `static` class is represented as `abstract sealed` at the IL level. From the CLR's perspective, a `static` class is one that cannot be inherited (`sealed`) and cannot be instantiated (`abstract`). **ClassValidator** handles this difference internally, correctly distinguishing `RequireStatic()` from `RequireAbstract()`.

> **"A C# `static` class is `abstract sealed` in IL. ClassValidator automatically handles this IL-level difference, so `RequireStatic()` and `RequireAbstract()` do not interfere with each other."**

### Record Type Verification

```csharp
[Fact]
public void RecordTypes_ShouldBe_Record()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .And()
        .HaveNameEndingWith("Summary")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireRecord(),
            verbose: true)
        .ThrowIfAnyFailures("Record Type Rule");
}
```

`RequireRecord()` verifies whether a type is a C# `record` type. This is useful when enforcing the use of `record` for DTOs or immutable data transfer objects.

## Summary at a Glance

The following table organizes ClassValidator's visibility and modifier verification methods.

### ClassValidator Visibility/Modifier Verification Methods

| Method | Verifies | Use Scenario |
|--------|----------|-------------|
| `RequirePublic()` | public visibility | Domain models, API contracts |
| `RequireInternal()` | internal visibility | Internal implementations, infrastructure code |
| `RequireSealed()` | sealed modifier | Prevent inheritance, protect immutability contracts |
| `RequireNotSealed()` | not sealed | Base classes, extensible classes |
| `RequireAbstract()` | abstract modifier | Base classes, template pattern |
| `RequireNotAbstract()` | not abstract | Concrete implementation classes |
| `RequireStatic()` | static class | Utilities, extension methods |
| `RequireNotStatic()` | not static | Instance classes |
| `RequireRecord()` | record type | DTOs, value objects |
| `RequireNotRecord()` | not record | Regular classes |

## FAQ

### Q1: Are there `protected` or `private` verifications beyond `RequirePublic()` and `RequireInternal()`?
**A**: Top-level classes in C# can only be `public` or `internal`. Nested classes can be `protected` or `private`, but since ArchUnitNET primarily handles top-level types, `RequirePublic()` and `RequireInternal()` are the core methods.

### Q2: What happens if `RequireSealed()` is applied to a `static` class?
**A**: Since a `static` class is `abstract sealed` at the IL level, `RequireSealed()` passes, but `RequireAbstract()` also passes. **ClassValidator** recognizes `static` as a separate category, so using `RequireStatic()` expresses the intent more accurately.

### Q3: Does `RequireRecord()` verify both `record class` and `record struct`?
**A**: `RequireRecord()` used in `ValidateAllClasses` verifies only `record class`. Since `record struct` is a value type, it is not included in ArchUnitNET's class filter.

---

The next chapter covers how to verify naming rules for classes and interfaces using suffixes, prefixes, and regular expression patterns.

-> [Ch 3: Naming Rules](../03-Naming-Rules/)
