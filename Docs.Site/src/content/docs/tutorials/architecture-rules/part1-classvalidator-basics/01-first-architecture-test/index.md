---
title: "First Architecture Test"
---
## Overview

Are you manually checking in every code review whether the `Employee` class is `public sealed`? When there are 10 classes it is manageable, but as the number grows to 50 or 100, missing something is just a matter of time. In this chapter, you will learn how to turn this manual checking into **executable tests**.

> **"The first step of architecture testing is simple. Just express 'this class should be public sealed' as code."**

## Learning Objectives

### Core Learning Goals
1. **Understanding the basic structure of architecture tests**
   - Load assemblies with `ArchLoader` to create an `Architecture` object
   - Select classes for verification with `ArchRuleDefinition.Classes()`
2. **Class rule verification with ClassValidator**
   - `RequirePublic()`: Verify that a class is `public`
   - `RequireSealed()`: Verify that a class is `sealed`
3. **Chaining multiple rules**
   - Combine multiple rules in a single verification like `RequirePublic().RequireSealed()`

### What You Will Verify Through Practice
- Automatically verify the visibility (`public`) and modifier (`sealed`) of the `Employee` class
- The difference between single-rule tests and compound rule chaining

## Project Structure

```
01-First-Architecture-Test/
├── FirstArchitectureTest/                    # Main project
│   ├── Domains/
│   │   └── Employee.cs                       # Target domain class for verification
│   ├── Program.cs
│   └── FirstArchitectureTest.csproj
├── FirstArchitectureTest.Tests.Unit/         # Test project
│   ├── ArchitectureTests.cs                  # Architecture tests
│   ├── FirstArchitectureTest.Tests.Unit.csproj
│   └── xunit.runner.json
└── README.md
```

## Code Under Verification

### Employee.cs

```csharp
namespace FirstArchitectureTest.Domains;

public sealed class Employee
{
    public string Name { get; }
    public string Department { get; }

    private Employee(string name, string department)
    {
        Name = name;
        Department = department;
    }

    public static Employee Create(string name, string department)
        => new(name, department);
}
```

The `Employee` class follows these design principles:
- **`public sealed`**: Exposed publicly but prohibits inheritance to protect the immutability contract.
- **`private` constructor + static factory method**: Controls object creation.

## Test Code Walkthrough

### Loading the Architecture

```csharp
public abstract class ArchitectureTestBase
{
    protected static readonly Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(typeof(FirstArchitectureTest.Domains.Employee).Assembly)
            .Build();

    protected static readonly string DomainNamespace =
        typeof(FirstArchitectureTest.Domains.Employee).Namespace!;
}
```

`ArchLoader` is the core class of **ArchUnitNET** that analyzes type information from a given assembly to produce an `Architecture` object. This object serves as the foundation for all architecture verification.

### Single Rule Verification

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
```

Verification consists of four steps:

1. **Select targets** -- Start class verification with `ArchRuleDefinition.Classes()`
2. **Filter** -- Select only classes in a specific namespace with `.That().ResideInNamespace(...)`
3. **Apply rules** -- Use **ClassValidator** in `.ValidateAllClasses(...)` to apply the `RequirePublic()` rule to each class
4. **Check results** -- Throw an exception if any violations exist with `.ThrowIfAnyFailures(...)`

### Rule Chaining

```csharp
[Fact]
public void DomainClasses_ShouldBe_PublicAndSealed()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .ValidateAllClasses(Architecture, @class => @class
            .RequirePublic()
            .RequireSealed(),
            verbose: true)
        .ThrowIfAnyFailures("Domain Class Public Sealed Rule");
}
```

**ClassValidator** provides a fluent API that allows chaining multiple rules. `RequirePublic().RequireSealed()` verifies both conditions within a single verification.

## Summary at a Glance

The following table summarizes the core components used in this chapter.

### Architecture Test Components

| Component | Role |
|-----------|------|
| **ArchLoader** | Analyzes assemblies to create an Architecture object |
| **ArchRuleDefinition** | Entry point for selecting verification targets (classes, interfaces, etc.) |
| **ValidateAllClasses** | Applies ClassValidator rules to all selected classes |
| **ClassValidator** | Fluent API for defining per-class rules |
| **ThrowIfAnyFailures** | Throws an exception with detailed messages if violations exist |
| **verbose: true** | Outputs the list of classes being verified to the console |

## FAQ

### Q1: What does `verbose: true` do?
**A**: It outputs the list of classes selected for verification to the console. This is useful during debugging to confirm "is the expected class included in the verification targets?" In production CI, you can set it to `false` to reduce output.

### Q2: What message is output when a rule is violated?
**A**: `ThrowIfAnyFailures` outputs a detailed message including the violating class name, the violated rule, and the rule name (`"Domain Class Public Sealed Rule"`). For example, if `Employee` is not `sealed`, a message like "`Employee` failed: RequireSealed" is displayed.

### Q3: What is the difference between chaining and separate tests?
**A**: `RequirePublic().RequireSealed()` chaining bundles two rules into **a single verification unit**. If any fails, all violations for that class are reported together. In contrast, separating into individual tests allows tracking the success/failure of each rule independently. Generally, related rules are chained together, and independent rules are separated into individual tests.

### Q4: Why is `@` needed in `@class`?
**A**: `class` is a reserved keyword in C#. To use it as a variable name, the `@` prefix must be added to tell the compiler "this is an identifier, not a reserved word". You could use `c` or `cls` instead of `@class`, but `@class` communicates the intent more clearly.

---

The next chapter covers how to verify various class attributes including `public`/`internal` visibility, `sealed`/`abstract`/`static` modifiers, and `record` types.

-> [Ch 2: Visibility and Modifiers](../02-Visibility-And-Modifiers/)
