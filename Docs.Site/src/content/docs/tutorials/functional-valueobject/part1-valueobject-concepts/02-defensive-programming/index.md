---
title: "Defensive Programming"
---
## Overview

In `01-Basic-Divide`, we confirmed the problem of the program crashing with a `DivideByZeroException` when the basic division function attempts to divide by zero. But is "division by zero" truly an exceptional situation? Mathematically, the fact that a denominator cannot be zero is a **predictable domain rule**. Handling predictable failures with exceptions is not appropriate by design.

In this step, we compare two implementation approaches of defensive programming.

1. **Divide with defined (intentional) exception via pre-validation**: Making exceptions clearer and more intentional
2. **TryDivide using bool return without exceptions via pre-validation**: Safe failure handling without exceptions

## Learning Objectives

- You can explain the difference between defined exception handling via pre-validation and the Try pattern.
- You can compare the pros and cons of exception-based approaches and the Try pattern, and choose the appropriate method for the situation.
- You can recognize that the `out` parameter in the Try pattern is still a side effect from a functional programming perspective.
- You can understand the connection to standard .NET Framework patterns like TryParse, TryGetValue, etc.

## Core Concepts

### Defined (Intentional) Exception Handling via Pre-validation

This approach validates input beforehand and throws a clear exception if it is not valid. Instead of the system-thrown `DivideByZeroException`, the developer uses an intentional `ArgumentException` to clarify error messages and debugging information.

```csharp
// Defensive programming - defined exception via pre-validation
public static int Divide(int numerator, int denominator)
{
    if (denominator == 0)
        throw new ArgumentException("Cannot divide by zero");

    return numerator / denominator;
}
```

While this provides clear error messages and stack traces, it still throws an exception that interrupts the program flow. The caller must use a try-catch block, which constitutes a **side effect**.

### TryDivide Pattern (Try Pattern)

This approach expresses success/failure as a `bool` return value without throwing exceptions. It follows the same pattern widely used in the .NET Framework with `int.TryParse`, `Dictionary.TryGetValue`, etc.

```csharp
// Defensive programming - bool return without exceptions
public static bool TryDivide(int numerator, int denominator, out int result)
{
    if (denominator == 0)
    {
        result = default;
        return false;
    }

    result = numerator / denominator;
    return true;
}
```

Performance improves because there is no exception handling overhead, and the program does not crash on failure. However, **side effects still exist** through the `out` parameter which modifies state outside the function. Additionally, only success/failure status is known without specific error information, and combining multiple Try patterns leads to complex nested if statements.

### Common Limitations of Both Approaches

It is important that both approaches have side effects. The exception-based Divide has the side effect of interrupting program flow, while TryDivide has the side effect of modifying external state via the `out` parameter. Only the target of the side effect has changed; the fundamental problem remains.

The following table compares the characteristics of both approaches item by item.

| Aspect | Exception-based Divide | Try Pattern TryDivide |
|--------|------------------------|----------------------|
| **Approach** | Throws defined exception after pre-validation | Returns bool after pre-validation |
| **On success** | Returns result directly | Returns `true`, result via `out` parameter |
| **On failure** | Throws `ArgumentException` | Returns `false`, `out` parameter has default value |
| **Exception handling** | Requires try-catch block | Not required |
| **Performance** | Exception handling overhead exists | Fast (no exception overhead) |
| **Side effects** | Interrupts program flow | Modifies external state (`out`) |
| **Error information** | Detailed exception message, stack trace | Only success/failure status |

### Connection to Standard Patterns

The Try pattern is a standard pattern used throughout the .NET Framework. `int.TryParse`, `Dictionary.TryGetValue`, `ConcurrentDictionary.TryAdd`, `ConcurrentDictionary.TryRemove` all follow the same principle. On success, they return `true` and pass the result via the `out` parameter; on failure, they return `false` without throwing exceptions.

## Practical Guidelines

### Expected Output
```
=== Two Implementation Approaches of Defensive Programming ===

=== Attempting to calculate 10 / 2 ===

Method 1: Exception-based Divide
 Success: 5

Method 2: Try Pattern TryDivide
 Success: 5

=== Attempting to calculate 10 / 0 ===

Method 1: Exception-based Divide
 Failure: Cannot divide by zero (Parameter 'denominator') (program flow interruption side effect)

Method 2: Try Pattern TryDivide
 Failure: Cannot calculate (external state modification side effect: result = 0)
```

## Project Description

### Project Structure
```
DefensiveProgramming/                          # Main project
├── Program.cs                                  # Main entry file
├── MathOperations.cs                           # TryDivide pattern implementation
├── DefensiveProgramming.csproj                 # Project file
└── README.md                                   # Main documentation
```

### Core Code

#### MathOperations.cs
```csharp
namespace DefensiveProgramming;

public static class MathOperations
{
    /// <summary>
    /// Defensive programming division function using the TryDivide pattern.
    /// Returns false when denominator is 0, and result holds the default value.
    /// </summary>
    public static bool TryDivide(int numerator, int denominator, out int result)
    {
        // Return false when denominator is 0 (no exception thrown!)
        if (denominator == 0)
        {
            result = default; // Set default value
            return false;     // Explicitly return failure
        }

        result = numerator / denominator;
        return true;          // Explicitly return success
    }

    /// <summary>
    /// Original Divide method (kept for backward compatibility).
    /// Throws ArgumentException when denominator is 0.
    /// </summary>
    public static int Divide(int numerator, int denominator)
    {
        if (denominator == 0)
            throw new ArgumentException("Cannot divide by zero");

        return numerator / denominator;
    }
}
```

#### Program.cs
```csharp
namespace DefensiveProgramming;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Defensive Programming TryDivide Pattern Test ===\n");

        // Using TryDivide pattern (recommended approach)
        Console.WriteLine("1. Using TryDivide pattern (recommended):");
        DemonstrateTryDividePattern();

        Console.WriteLine();

        // Comparison with traditional Divide method
        Console.WriteLine("2. Comparison with traditional Divide method:");
        DemonstrateTraditionalDivideMethod();

        Console.WriteLine();

        // Demonstrating benefits of defensive programming
        Console.WriteLine("3. Benefits of defensive programming:");
        DemonstrateDefensiveProgrammingBenefits();
    }

    static void DemonstrateTryDividePattern()
    {
        // Normal case
        if (MathOperations.TryDivide(10, 2, out int result1))
        {
            Console.WriteLine($"✓ 10 / 2 = {result1} (success)");
        }
        else
        {
            Console.WriteLine("✗ 10 / 2 = failure");
        }

        // Exception case (no exception thrown!)
        if (MathOperations.TryDivide(10, 0, out int result2))
        {
            Console.WriteLine($"✓ 10 / 0 = {result2} (success)");
        }
        else
        {
            Console.WriteLine("✗ 10 / 0 = failure (handled safely without exception)");
        }
    }
}
```

### Try Pattern Usage
```csharp
// 1. Basic usage
if (MathOperations.TryDivide(10, 2, out int result))
{
    Console.WriteLine($"Result: {result}");  // On success
}
else
{
    Console.WriteLine("Cannot calculate");   // On failure
}

// 2. Using with variable declaration
int result;
if (MathOperations.TryDivide(10, 0, out result))
{
    Console.WriteLine($"Result: {result}");
}
else
{
    Console.WriteLine($"Failed, result value: {result}");  // default(int) = 0
}

// 3. When you want to discard
if (MathOperations.TryDivide(10, 2, out _))  // Using _
{
    Console.WriteLine("Calculation succeeded");
}
else
{
    Console.WriteLine("Calculation failed");
}
```

## Summary at a Glance

### Core Principles of the Try Pattern
1. **On success**: Returns `true`, returns result value via `out` parameter
2. **On failure**: Returns `false`, `out` parameter holds default or meaningless value
3. **No exceptions**: Never throws an exception under any circumstances
4. **Explicit handling**: The caller must explicitly handle success/failure

### Limitations of Each Approach and Next Steps

The following table summarizes the limitations of both approaches and the improvement direction toward functional result types.

| Aspect | Exception-based Divide | Try Pattern TryDivide |
|--------|------------------------|----------------------|
| **Limitation** | Program flow interruption from exceptions (side effect) | Lack of type safety, side effect via `out` parameter |
| **Side effect type** | Program flow interruption side effect | External state modification side effect |
| **Improvement direction** | Safe failure handling without exceptions | Achieve type safety and complete purity |
| **Next step** | Naturally connects to Functional Result | Functional Result type resolves all side effects |

### Five Limitations of the Try Pattern

The following table contrasts the limitations of the Try pattern with the solutions proposed by functional programming.

| Limitation | Problem | Functional Solution |
|-----------|---------|---------------------|
| **Lack of type safety** | Runtime validation, cannot validate at compile time | Compile-time blocking with domain-specific types |
| **Side effects exist** | Modifies external state via `out` parameter | Resolves all side effects by returning immutable objects |
| **Insufficient explicit error handling** | Only boolean return, lacking specific error information | Provides specific error information via result types |
| **Limited composability** | Increased complexity from nested if statements | Concise composition via monad chaining |
| **Insufficient type system utilization** | Depends on primitive types, cannot leverage domain-specific types | Enhanced expressiveness through a strong type system |

## FAQ

### Q1: Is the Try pattern always better than exception-based approaches?
**A**: No. The Try pattern is suitable for **predictable failures** like division by zero, but exception-based approaches are suitable for **unpredictable system errors** like missing files or network errors. Choose the Try pattern when failure frequency is high and the program needs to continue executing; choose exceptions for serious, unrecoverable errors.

### Q2: Are there alternatives to the `out` parameter?
**A**: Tuple return `(bool Success, int Result)` or result object return approaches are also possible. However, the `out` parameter approach is the .NET Framework standard and has minimal memory allocation, making it advantageous for performance. In the next step, we explore the **Functional Result type**, which is the common direction of these alternatives.

### Q3: What is the fundamental limitation of the Try pattern?
**A**: While it eliminated the side effect of throwing exceptions, the side effect of modifying external state via the `out` parameter still exists. Additionally, a `bool` return value alone cannot convey the specific cause of an error, and combining multiple Try patterns leads to complex nested if statements. These limitations are resolved in the next step with functional result types.

---

Defensive programming presented ways to handle exceptions more safely, but both implementation approaches cannot escape the fundamental limitation of side effects. In the next chapter, **Functional Result Types**, we examine how to express success and failure as a single type without either exceptions or `out` parameters.

→ [Chapter 3: Functional Result Types](../03-Functional-Result/)
