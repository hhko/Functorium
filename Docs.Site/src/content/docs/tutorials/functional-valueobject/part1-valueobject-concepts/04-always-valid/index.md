---
title: "Always-Valid Types"
---
## Overview

If you have to check whether `denominator` is 0 every time you call the `Divide` function, isn't there a way to skip that check entirely? By using **Always-Valid Types**, you can guarantee validity at compile time, making runtime validation unnecessary.

> **Let's guarantee validity at compile time instead of runtime validation!**

## Learning Objectives

Upon completing this chapter, you will be able to:

- Express domain concepts as **value objects** and guarantee validity at creation time.
- Implement always-valid types by combining **private constructors and static factory methods**.
- Combine functional result types (`Fin<T>`) with value objects to achieve **compile-time safety**.
- Prevent **runtime errors** by reflecting business rules in the type system.

### **What You Will Verify Through Practice**
- **Value object creation**: `Denominator.Create(5)` → returns `Fin<Denominator>.Succ(denominator)`
- **Invalid value**: `Denominator.Create(0)` → returns `Fin<Denominator>.Fail(Error)`
- **Safe function**: `Divide(10, denominator)` → no validation needed, always safe
- **Compile-time guarantee**: Invalid values are rejected at compile time

## Why Is This Needed?

In the previous step `03-Functional-Result`, we were able to handle failures safely without exceptions using functional result types, but there was still the limitation of having to perform validity checks at runtime.

Every time the `Divide` function is called, we must check whether `denominator` is 0, and this validation logic is repeated in every function. Even if `Denominator.Create` validates once, the `Divide` function must validate again, violating the **DRY principle (Don't Repeat Yourself)**. Additionally, the business rule "non-zero integer" is not expressed at all in the simple `int` type, making it difficult for code readers to identify this constraint.

To solve these problems, we introduced **value objects**. With value objects, **validity is guaranteed at compile time**, **validation logic is centralized in one place**, and **domain concepts are clearly expressed in the code**.

## Core Concepts

### Value Object
- **Expression of domain concepts**: Express business rules as types
- **Immutability**: Values do not change after creation
- **Encapsulation**: Protect internal state from outside access
- **Validity guarantee**: Verify all business rules at creation time

### Always-Valid Type Pattern

Private constructors prevent direct instance creation, and the static factory method validates before creating.

```csharp
public sealed class Denominator
{
    private readonly int _value;

    // Private constructor - prevent direct instance creation
    private Denominator(int value) =>
        _value = value;

    // Static factory method - validate before creation
    public static Fin<Denominator> Create(int value)
    {
        if (value == 0)
            return Error.New("Zero is not allowed");

        return new Denominator(value);
    }

    // Safe value access
    public int Value =>
        _value;
}
```

### Compile-Time vs Runtime Validation

Comparing the previous approach with the current approach.

```csharp
// Runtime validation (previous approach)
public static Fin<int> Divide(int numerator, int denominator)
{
    if (denominator == 0) // Validated at runtime
        return Error.New("Zero is not allowed");

    return numerator / denominator;
}

// Compile-time guarantee (current approach)
public static int Divide(int numerator, Denominator denominator)
{
    return numerator / denominator.Value; // No validation needed!
}
```

Since the compiler requires the `Denominator` type, `int` values cannot be passed directly. The possibility of invalid values reaching the function is entirely eliminated.

### Domain-Driven Design (DDD) Perspective
- **Clear expression of domain concepts**: Use `Denominator` instead of `int`
- **Reflecting business rules in the type system**: Express non-zero integers as a type
- **Clarifying intent**: The function signature alone guarantees safety
- **Communication with domain experts**: Express business language in code

When `Denominator` appears in the function signature, code readers can immediately understand the rule "this parameter must be a non-zero integer" without separate documentation.

## Practical Guidelines

### Expected Output
```
=== Always-Valid Types ===

Valid value: AlwaysValid.ValueObjects.Denominator
Invalid value: Error: Zero is not allowed
Division function test:
10 / 5 = 2
```

### Value Object Usage Pattern

The basic flow of creating a value object and then calling a safe function.

```csharp
// 1. Create value object (includes validity verification)
var denominatorResult = Denominator.Create(5);
var denominator = denominatorResult.Match(
    Succ: value => value,
    Fail: error => throw new Exception($"Invalid denominator: {error.Message}")
);

// 2. Call safe function (no validation needed)
var result = MathOperations.Divide(10, denominator);
Console.WriteLine($"Result: {result}"); // Always safe!
```

## Project Description

### Project Structure
```
AlwaysValid/                          # Main project
├── Program.cs                        # Main entry file
├── MathOperations.cs                 # Safe division function
├── ValueObjects/                     # Value objects directory
│   └── Denominator.cs                # Denominator value object
├── AlwaysValid.csproj                # Project file
└── README.md                         # Project description
```

### Core Code

#### Denominator.cs (Value Object)
```csharp
using LanguageExt;
using LanguageExt.Common;

namespace AlwaysValid.ValueObjects;

/// <summary>
/// A denominator value object representing a non-zero integer.
/// Performs validity verification at creation time to guarantee only valid values.
/// </summary>
public sealed class Denominator
{
    private readonly int _value;

    // Private constructor - prevent direct instance creation
    private Denominator(int value) =>
        _value = value;

    /// <summary>
    /// Creates a Denominator. Returns failure if the value is 0.
    /// </summary>
    /// <param name="value">A non-zero integer value</param>
    /// <returns>Denominator on success, Error on failure</returns>
    public static Fin<Denominator> Create(int value)
    {
        if (value == 0)
            return Error.New("Zero is not allowed");

        return new Denominator(value);
    }

    /// <summary>
    /// Safely returns the internal value.
    /// </summary>
    public int Value =>
        _value;
}
```

#### MathOperations.cs (Safe Function)
```csharp
using AlwaysValid.ValueObjects;

namespace AlwaysValid;

public static class MathOperations
{
    /// <summary>
    /// Safe division function using value objects.
    /// Since denominator is always a valid Denominator, no validation is needed.
    /// </summary>
    /// <param name="numerator">Numerator</param>
    /// <param name="denominator">Denominator (guaranteed to be non-zero)</param>
    /// <returns>Division result</returns>
    public static int Divide(int numerator, Denominator denominator)
    {
        // No validation needed! Always valid!
        return numerator / denominator.Value;
    }
}
```

#### Program.cs (Usage Example)
```csharp
using AlwaysValid.ValueObjects;

namespace AlwaysValid;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Always-Valid Type Test ===\n");

        // Denominator creation test
        Console.WriteLine("Denominator creation cases:");

        var validResult = Denominator.Create(5);
        validResult.Match(
            Succ: value => Console.WriteLine($"Valid value: {value}"),
            Fail: error => Console.WriteLine($"Error: {error.Message}")
        );

        var invalidResult = Denominator.Create(0);
        invalidResult.Match(
            Succ: value => Console.WriteLine($"Valid value: {value}"),
            Fail: error => Console.WriteLine($"Invalid value: Error: {error.Message}")
        );

        Console.WriteLine();

        // Division function test
        Console.WriteLine("Division function test:");
        var denominator = Denominator.Create(5);
        var result = MathOperations.Divide(10, (Denominator)denominator);
        Console.WriteLine($"10 / 5 = {result}");
    }
}
```

### Key Elements of the Value Object Pattern

Four elements combine to form an always-valid type.

```csharp
// 1. Private constructor
private Denominator(int value) => _value = value;

// 2. Static factory method
public static Fin<Denominator> Create(int value)
{
    if (value == 0)
        return Error.New("Zero is not allowed");

    return new Denominator(value);
}

// 3. Immutability guarantee
public int Value => _value; // Read-only property

// 4. Domain concept expression
public static int Divide(int numerator, Denominator denominator)
{
    return numerator / denominator.Value; // No validation needed!
}
```

## Summary at a Glance

### Pros and Cons of Value Objects

Benefits and costs of introducing value objects.

| Pros | Cons |
|------|------|
| **Compile-time guarantee** | **Additional type definitions needed** |
| **No validation needed** | **Initial learning curve** |
| **Domain expression** | **Excessive complexity for simple cases** |
| **Type safety** | **Memory overhead** |
| **Clear intent** | **Refactoring cost** |

### Evolution Comparison

How the validation approach has evolved from Chapters 1 through 4.

| Step | Approach | Validation Timing | Safety | Complexity |
|------|----------|-------------------|--------|------------|
| **01-Basic-Divide** | Basic function | Runtime exception | Low | Low |
| **02-Exception-Handling** | Defensive programming | Runtime validation | Medium | Medium |
| **03-Functional-Result** | Functional result type | Runtime validation | High | High |
| **04-Always-Valid** | Value object | Compile time | Highest | Highest |

### Type Expression of Domain Concepts

Beyond denominators, various domain concepts can be expressed as value objects.

| Domain Concept | Primitive Type | Value Object | Business Rule |
|---------------|---------------|-------------|---------------|
| **Denominator** | `int` | `Denominator` | Non-zero |
| **Email** | `string` | `EmailAddress` | Valid format |
| **Age** | `int` | `Age` | 0 to 150 |
| **Amount** | `decimal` | `Money` | Positive, 2 decimal places |

## FAQ

### Q1: Why are value objects better than functional result types?
**A**: Functional result types still require validation at runtime every time, and developers can forget to validate. In contrast, value objects secure validity at the creation stage, so validation logic is unnecessary in all subsequent functions. The compiler enforces safety at the type level.

### Q2: When should I use value objects?
**A**: Judge by asking "Does this value have business rules?" Values with specific format or range constraints, like email addresses, ages, and monetary amounts, are appropriate to express as value objects. On the other hand, for simple calculation results or temporary data without business rules, using primitive types is better.

### Q3: Why use value objects and functional result types together?
**A**: At creation time, `Fin<T>` explicitly expresses the validation result, and at usage time, since it is already a valid value object, no validation is needed. Thanks to this combination, failures can be safely handled at the creation stage, while values can be trusted without validation in subsequent business logic.

---

We secured compile-time validity through value objects, but the inconvenience of having to extract the internal value like `denominator.Value` remains. In the next chapter, we introduce **operator overloading** to implement natural mathematical expressions like `15 / denominator`.

→ [Chapter 5: Operator Overloading](../05-Operator-Overloading/)
