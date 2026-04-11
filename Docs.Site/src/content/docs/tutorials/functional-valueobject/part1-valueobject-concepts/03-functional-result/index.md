---
title: "Functional Result Types"
---
## Overview

In the previous step, we compared two implementation approaches of defensive programming, but the exception-based Divide interrupted program flow and TryDivide modified external state via the `out` parameter. Is there no way to express success and failure without exceptions or `out` parameters?

**Functional Result Types** are the answer to this question. By embedding success/failure information in the return type itself, results are expressed explicitly without side effects.

## Learning Objectives

- You can explain which limitations of the exception-based approach are resolved by functional result types.
- You can use the `Fin<T>` type and the Match pattern to explicitly handle success/failure.
- You can understand and apply the conditions for pure functions (same input-same output, no side effects).

## Core Concepts

### Functional Result Type

Functional result types solve three problems remaining from defensive programming. Even after pre-validation, throwing an `ArgumentException` to interrupt program flow treats a predictable domain rule violation as an exceptional situation, which is not appropriate by design. Functions that throw exceptions have side effects and are not pure, and if the caller forgets the try-catch, the program crashes.

Functional result types are a C# implementation of the **Either type**, expressing both success and failure as types. The following code shows the difference between the exception-based approach and the functional result type approach.

```csharp
// Previous approach (problematic) - program crash from exception
public int Divide(int x, int y)
{
    if (y == 0)
        throw new ArgumentException("Cannot divide by zero");  // Exception thrown!

    return x / y;
}

// Improved approach (functional result type) - explicit success/failure expression
public Fin<int> Divide(int x, int y)
{
    if (y == 0)
        return Error.New("Zero is not allowed");  // Explicit failure

    return x / y;  // Explicit success
}
```

In the improved approach, the function operates safely without exceptions, and the caller can explicitly handle success/failure.

### `Fin<T>` Type and Match Pattern

`Fin<T>` is a result type provided by the LanguageExt library with two states: success (`Succ`) and failure (`Fail`). Using the Match pattern forces handling of both states, preventing developers from forgetting to handle failures.

```csharp
// Using the Fin<T> type
var result = Divide(10, 0);  // Returns Fin<int> type

// Match pattern for success/failure handling (enforced)
result.Match(
    Succ: value => Console.WriteLine($"Result: {value}"),      // Success handling
    Fail: error => Console.WriteLine($"Error: {error.Message}") // Failure handling
);
```

Unlike try-catch, the Match pattern enforces success/failure handling at compile time. This ensures type safety and reduces runtime errors.

### Completing Pure Functions

A pure function always returns the same output for the same input and must have no side effects. Functions that throw exceptions violate this condition, but functions returning functional result types satisfy the conditions for pure functions.

```csharp
// Exception-based function (not pure) - side effects occur
public int Divide(int x, int y)
{
    if (y == 0)
        throw new ArgumentException("Cannot divide by zero");  // Side effect!

    return x / y;
}

// Functional result type function (pure function) - no side effects
public Fin<int> Divide(int x, int y)
{
    if (y == 0)
        return Error.New("Zero is not allowed");  // No side effect

    return x / y;
}
```

Pure functions are easy to test, easy to compose, and guarantee **referential transparency**, allowing function calls to be replaced with their result values.

## Practical Guidelines

### Expected Output
```
=== Functional Result Types ===

Success case:
10 / 2 = 5

Failure case:
10 / 0 = Error: Zero is not allowed
```

### Key Implementation Points
1. **Using the LanguageExt library**: `using LanguageExt;` and `using LanguageExt.Common;`
2. **`Fin<T>` return type**: Explicitly expresses success/failure
3. **Using Error.New()**: Creates an Error object on failure
4. **Using Match pattern**: Explicitly handles success/failure

## Project Description

### Project Structure
```
FunctionalResult/                       # Main project
├── Program.cs                          # Main entry file
├── MathOperations.cs                   # Functional result type function implementation
├── FunctionalResult.csproj             # Project file
└── README.md                           # Main documentation
```

### Core Code

#### MathOperations.cs
```csharp
using LanguageExt;
using LanguageExt.Common;

namespace FunctionalResult;

public static class MathOperations
{
    /// <summary>
    /// Division function using functional result types.
    /// Returns Fin<int>.Succ(result) on success, Fin<int>.Fail(error) on failure.
    /// </summary>
    /// <param name="numerator">Numerator</param>
    /// <param name="denominator">Denominator</param>
    /// <returns>Fin<int> type explicitly expressing success/failure</returns>
    public static Fin<int> Divide(int numerator, int denominator)
    {
        if (denominator == 0)
            return Error.New("Zero is not allowed");

        return numerator / denominator;
    }
}
```

#### Program.cs
```csharp
namespace FunctionalResult;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Functional Result Type Test ===\n");

        // Success case
        Console.WriteLine("Success case:");
        var successResult = MathOperations.Divide(10, 2);
        successResult.Match(
            Succ: value => Console.WriteLine($"10 / 2 = {value}"),
            Fail: error => Console.WriteLine($"Error: {error.Message}")
        );

        Console.WriteLine();

        // Failure case
        Console.WriteLine("Failure case:");
        var failureResult = MathOperations.Divide(10, 0);
        failureResult.Match(
            Succ: value => Console.WriteLine($"10 / 0 = {value}"),
            Fail: error => Console.WriteLine($"10 / 0 = Error: {error.Message}")
        );
    }
}
```

### Key Packages
- **LanguageExt.Core**: Functional programming library
  - `Fin<T>`: Result type expressing success/failure
  - `Error`: Type carrying error information
  - `Match`: Result handling via pattern matching

## Summary at a Glance

### Exception-Based vs Functional Result Type Comparison

The following table compares the characteristics of exception-based approaches and functional result types item by item.

| Aspect | Exception-based | Functional Result Type |
|--------|----------------|----------------------|
| **Success/failure expression** | Unclear in function signature | Clear in function signature |
| **Handling enforcement** | Optional (try-catch) | Required (Match) |
| **Side effects** | Present (exception throwing) | None |
| **Predictability** | Low (exceptions possible) | High (always returns value) |
| **Type safety** | Low (runtime exceptions) | High (compile-time verification) |

### Improvement Direction
1. **Introduce value objects**: Create a domain type that represents "non-zero integer"
2. **Ensure type safety**: Validate at compile time
3. **Domain-centric design**: Express business rules as types

## FAQ

### Q1: Should all functions be converted to functional result types?
**A**: No. Use functional result types for **predictable domain rule violations** like division by zero or input validation, and use exceptions for **unpredictable system errors** like network failures or memory exhaustion. The general distinction is to use result types in the domain layer and exceptions in the infrastructure layer.

### Q2: What is the `Fin<T>` type?
**A**: A functional result type provided by the LanguageExt library with two states: `Succ(T value)` and `Fail(Error error)`. The Match method forces handling of both states, and it is immutable and can be combined with other functional types.

### Q3: Can it be used without LanguageExt?
**A**: You can implement your own result type. A simple `Result<T>` like the one below can be created and is suitable for learning or small projects. However, for production, using the proven LanguageExt is recommended.

```csharp
public class Result<T>
{
    private readonly T? _value;
    private readonly string? _error;
    private readonly bool _isSucc;

    private Result(T value)
    {
        _value = value;
        _isSucc= true;
    }

    private Result(string error)
    {
        _error = error;
        _isSucc = false;
    }

    public static Result<T> Succ(T value) => new Result<T>(value);
    public static Result<T> Fail(string error) => new Result<T>(error);

    public R Match<R>(Func<T, R> onSucc, Func<string, R> onFail)
    {
        return _isSucc ? onSucc(_value!) : onFail(_error!);
    }
}
```

---

Functional result types eliminated all side effects of exceptions and `out` parameters, and presented a way to explicitly express success and failure as types. However, calling `Divide(10, 0)` itself could not be blocked at compile time. In the next chapter, **Always-Valid Types**, we examine how to define "non-zero integer" as a domain type to block invalid inputs from reaching the function entirely through the type system.

→ [Chapter 4: Always-Valid Value Objects](../04-Always-Valid/)
