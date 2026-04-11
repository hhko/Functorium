---
title: "Basic Division"
---
## Overview

What happens when you execute `10 / 0`? A `DivideByZeroException` is thrown and the program crashes. But the fact that you cannot divide by zero is not an exceptional situation -- it is a mathematical rule everyone knows. In this chapter, we examine **why handling predictable failures with exceptions is problematic** and understand the need for value objects.

## Learning Objectives

Upon completing this chapter, you will be able to:

1. Explain the problem of `DivideByZeroException` occurring in a basic division function
2. Distinguish between exceptional situations and predictable failures
3. Recognize the limitation that domain rules cannot be expressed in the `int` type

## Why Is This Needed?

Consider the following division function.

```csharp
public int Divide(int numerator, int denominator)
{
    return numerator / denominator;
}
```

This function has two problems.

If 0 is passed as `denominator`, a `DivideByZeroException` occurs. Exceptions are tools for unpredictable situations like network errors or memory exhaustion, but "cannot divide by zero" is a predictable domain rule. Throwing an exception creates a side effect that interrupts the program flow, which also violates the principle of pure functions.

Additionally, the domain rule "non-zero integer" is not expressed in the `int` type at all. A person reading the code cannot tell from the signature alone how this function behaves with 0.

To solve these problems, value objects that express domain rules as types are needed.

## Core Concepts

### Pure Functions and Side Effects

A pure function **always returns the same output for the same input** and has **no side effects**. Throwing an exception creates a side effect that interrupts the program flow, so it is not a pure function.

```csharp
// Not pure - side effect of throwing an exception
public int Divide(int x, int y)
{
    // Throws exception if y is 0!
    return x / y;
}

// Pure function - safe operations with always-valid values
public int Divide(int x, Denominator y)
{
    // y is guaranteed to be always valid (no side effects)
    return x / y.Value;
}
```

### Exceptions vs Domain Types

Exceptions should be used for system-level unpredictable errors (network failures, memory exhaustion). Predictable failures like user input errors or business rule violations are better expressed as domain types.

```csharp
// Using exceptions (inappropriate) - handling predictable failure with exceptions
var result = Divide(10, 0);  // Exception at runtime!

// Using domain types (appropriate) - preventing errors at compile time
var result = Divide(10, new Denominator(2));  // Safe operation
```

When the type system enforces constraints, developers no longer need to remember the rules, and the compiler blocks incorrect usage in advance.

## Practical Guidelines

### Expected Output
```
=== Basic Division Function ===

Normal case:
10 / 2 = 5

Exception case:
10 / 0 = System.DivideByZeroException: Attempted to divide by zero.
   at BasicDivide.MathOperations.Divide(Int32 numerator, Int32 denominator) in ...\MathOperations.cs:line 16
   at BasicDivide.Program.DemonstrateExceptionalDivide() in ...\Program.cs:line 43
```

## Project Description

### Project Structure
```
BasicDivide/                        # Main project
├── Program.cs                      # Main entry file
├── MathOperations.cs               # Division function implementation
├── BasicDivide.csproj              # Project file
└── README.md                       # Main documentation
```

### Core Code

#### MathOperations.cs
```csharp
public static class MathOperations
{
    // Problematic basic division function
    public static int Divide(int numerator, int denominator)
    {
        // Throws exception if denominator is 0!
        return numerator / denominator;
    }
}
```

#### Program.cs
```csharp
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Basic Division Function Test ===\n");

        // Normal case
        Console.WriteLine("Normal case:");
        try
        {
            var result = MathOperations.Divide(10, 2);
            Console.WriteLine($"10 / 2 = {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
        }

        Console.WriteLine();

        // Exception case
        Console.WriteLine("Exception case:");
        try
        {
            var result = MathOperations.Divide(10, 0);
            Console.WriteLine($"10 / 0 = {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"10 / 0 = {ex}");
        }
    }
}
```

## Summary at a Glance

The following table summarizes the problems with the basic division function.

### Problems with the Basic Division Function
| Problem | Description | Impact |
|---------|-------------|--------|
| **Exception-based error handling** | Handling predictable failures with exceptions interrupts program flow | Reduced program stability |
| **Side effects** | Throwing exceptions interrupts program flow | Unpredictable behavior |
| **Caller responsibility** | The caller must handle exceptions | Increased usage complexity |
| **Insufficient domain expression** | Business rules are not visible in the code | Reduced code readability |
| **Lack of type safety** | Cannot validate invalid inputs at compile time | Runtime error risk |

The following table compares exceptions and domain types.

### Exceptions vs Domain Types Comparison
| Aspect | Exceptions | Domain Types |
|--------|-----------|--------------|
| **Discovery time** | Runtime | Compile time |
| **Debugging cost** | High (execute → error → analyze → fix) | Low (explicit failure handling) |
| **Deployment risk** | High (can be missed in testing) | Low (no crashes from exceptions) |
| **Developer experience** | Poor (sudden crashes) | Good (predictable handling) |

## FAQ

### Q1: Aren't exceptions not always bad?
**A**: Exceptions are tools for handling **exceptional situations** (file deletion, network disconnection, memory exhaustion). For **predictable failures** like division by zero, explicit handling is more appropriate than exceptions.

### Q2: How do we solve this problem?
**A**: We solve it step by step in the following chapters. Starting with defensive programming in Chapter 2, we introduce functional result types in Chapter 3 and value objects in Chapter 4.

### Q3: Does this kind of problem occur frequently in real projects?
**A**: Yes. When dealing with values that have business rules -- such as email addresses, ages, and monetary amounts -- using only `string` or `int` causes the same problems. Validation code is scattered across multiple places, and forgetting it somewhere leads to runtime errors.

---

In the next chapter, we examine **defensive programming** as the first attempt to solve this problem. We can make exceptions clearer with pre-validation, but we also identify the fundamental limitations.

→ [Chapter 2: Defensive Programming](../02-Defensive-Programming/)
