---
title: "LINQ Expressions"
---
## Overview

To combine two `Fin<T>` values, you must nest `Match` calls, and as the number of steps increases, the code nests deeper to the right. By using LINQ expression `from`/`select` syntax, you can flatten nested `Match` chains while automatically propagating errors.

## Learning Objectives

Upon completing this chapter, you will be able to:

- Simplify **`Fin<T>` type chaining operations using the `from` keyword.**
- Implement **various operations between `Denominator` types** through explicit operator overloading.
- Automate **error propagation in compound operations** through LINQ expressions.

### **What You Will Verify Through Practice**
- **Using LINQ expressions**: Simplifying complex `Match` chains with the `from` keyword
- **Enhanced operator overloading**: Supporting various operators between `Denominator` types
- **Improved error handling**: Performing safe type conversions and operations without implicit conversions

## Why Is This Needed?

In the previous step `05-Operator-Overloading`, we implemented natural mathematical operations through operator overloading. However, several problems arose when attempting to perform complex operations in practice.

To perform chained operations using two denominators, you must nest `Match` methods. As the number of steps increases, the code continues to nest deeper, causing readability to drop sharply. The process of checking success/failure at each step and passing errors to the next step is cumbersome and error-prone, and the intent of the operation you are actually trying to perform gets buried in nested `Match` calls.

Introducing **LINQ expressions** resolves all of these problems. With `from`/`select` syntax, you can write code in a flat structure while the framework automatically handles error propagation.

## Core Concepts

### Functional Error Handling Through LINQ Expressions

LINQ expressions are a C# feature that implements **monadic chaining**. When you chain multiple steps of operations using the `from` keyword, success/failure handling at each step is performed automatically.

Comparing the previous approach's nested `Match` with LINQ expressions:

```csharp
// Previous approach (Match chain) - complex and hard to read
var result = Denominator.Create(5).Match(
    Succ: denom => Denominator.Create(3).Match(
        Succ: denom2 => denom / denom2,
        Fail: error => error
    ),
    Fail: error => error
);

// Improved approach (LINQ expression) - intuitive and easy to read
var result = from denom in Denominator.Create(5)
             from denom2 in Denominator.Create(3)
             select denom / denom2;
```

In the LINQ expression, the intent -- "create a denominator from 5, create another denominator from 3, and divide them" -- is directly expressed in the code. This is the essence of **declarative programming**.

### Explicit Type Conversion Through Operator Overloading

Previously, relying on implicit conversions could cause type safety issues. Now we explicitly define the necessary operators so that `Denominator` types can also perform operations with each other.

```csharp
// Operator overloading between Denominators
public static int operator /(Denominator numerator, Denominator denominator) =>
    numerator._value / denominator._value;
```

### Automated Error Propagation

In LINQ expressions, when an error occurs, the remaining steps are skipped and the error is automatically reflected in the final result. Developers do not need to check and pass errors at each step.

For example, if an error occurs at the second step of a three-step operation, the third step is not executed, and the error from the second step is propagated as the final result. Thanks to this, the code becomes much simpler and repetitive error handling logic disappears.

## Practical Guidelines

### Expected Output
```
=== Code Simplification Through LINQ Expressions ===

1. Key improvement: Simplification through LINQ expressions
  Before (05-Operator-Overloading): Using Match
  After  (06-Linq-Expression): Using from keyword
  15 / 5 = 3 (LINQ expression)

2. Using LINQ expressions in compound operations:
  (10 / 5) * 2 = 1

3. Conversion operators and LINQ expressions:
    Conversion succeeded: LinqExpression.ValueObjects.Denominator
    Conversion failed: Zero is not allowed

4. Error handling:
  Error handling through LINQ expressions:
    Error: Zero is not allowed
    Chained operation error: Zero is not allowed
```

### Key Implementation Points
1. **LINQ expression syntax**: Monadic operation chaining using the `from` keyword
2. **Extended operator overloading**: Implementing various operators between `Denominator` types (int/Denominator, Denominator/Denominator)
3. **Error handling pattern**: Processing success/failure cases through the `Match` method
4. **Explicit conversion operators**: Safe type conversion through `explicit operator`
5. **Test-driven implementation**: Verifying functionality through `LinqExpressionBasicTests` and `LinqExpressionAdvancedTests`

## Project Description

### Project Structure
```
LinqExpression/                         # Main project
├── Program.cs                          # Main entry file
├── MathOperations.cs                   # Math operations class (using LINQ expressions)
├── ValueObjects/                       # Value objects directory
│   └── Denominator.cs                  # Denominator value object (with operator overloading)
├── LinqExpression.csproj               # Project file
└── README.md                           # Main documentation
```

### Core Code

#### Denominator Class Operator Overloading
```csharp
public sealed class Denominator
{
    private readonly int _value;

    // Private constructor - prevent direct instance creation
    private Denominator(int value) => _value = value;

    /// <summary>
    /// Creates a Denominator. Returns failure if the value is 0.
    /// </summary>
    public static Fin<Denominator> Create(int value)
    {
        if (value == 0)
            return Error.New("Zero is not allowed");
        return new Denominator(value);
    }

    // Basic operators
    public static int operator /(int numerator, Denominator denominator) =>
        numerator / denominator._value;

    public static int operator /(Denominator denominator, int divisor) =>
        denominator._value / divisor;

    public static int operator /(Denominator numerator, Denominator denominator) =>
        numerator._value / denominator._value;

    // Conversion operators
    public static explicit operator Denominator(int value) =>
        Create(value).Match(
            Succ: x => x,
            Fail: _ => throw new InvalidCastException("0 cannot be converted to Denominator")
        );
}
```

#### Compound Operations Through LINQ Expressions

From single operations to compound operations and error handling, this demonstrates the range of LINQ expression usage.

```csharp
// Natural division operation using LINQ expression
var result = from denominator in Denominator.Create(5)
             select MathOperations.Divide(15, denominator);

// Using LINQ expressions in compound operations
var complexResult = from a in Denominator.Create(10)
                    from b in Denominator.Create(5)
                    from c in Denominator.Create(2)
                    select a / b / c;

// Conversion operators and LINQ expressions
var successResult = from value in Denominator.Create(15)
                    select $"Conversion succeeded: {value}";

var failureResult = from value in Denominator.Create(0)
                    select $"Conversion succeeded: {value}";
```

## Summary at a Glance

### Comparison Table

The following table summarizes the differences in error handling approaches and code structure between the previous step and the current step.

| Aspect | Previous approach (05-Operator-Overloading) | Current approach (06-Linq-Expression) |
|------|-------------------------------------|--------------------------------|
| **Error handling** | Nested Match chains | Simplification through LINQ expressions |
| **Code readability** | Complex nested structure | Intuitive from-select syntax |
| **Error propagation** | Explicit error handling | Automatic error propagation |
| **Operator support** | Basic operators only | Extended operator set (int/Denominator, Denominator/Denominator) |
| **Type conversion** | Relies on implicit conversion | Explicit operator overloading |

### Pros and Cons

The following table summarizes the benefits and considerations when introducing LINQ expressions.

| Pros | Cons |
|------|------|
| **Improved code readability** | Learning curve for LINQ expressions |
| **Automated error handling** | Debugging trace complexity |
| **Enhanced type safety** | Operator overloading complexity |
| **Functional programming** | Differences from existing imperative code |

## FAQ

### Q1: When should I use LINQ expressions vs the Match method?
**A**: When branching on success/failure from a single `Fin<T>`, `Match` is appropriate. In compound operations combining multiple `Fin<T>` values, LINQ expressions are more concise because they eliminate nesting and automate error propagation.

```csharp
// Single handling - use Match
var result = Denominator.Create(5).Match(
    Succ: value => $"Success: {value}",
    Fail: error => $"Failure: {error}"
);

// Compound handling - use LINQ expression
var result = from a in Denominator.Create(10)
             from b in Denominator.Create(5)
             select a / b;

// Example used in an actual project
var result = from denominator in Denominator.Create(5)
             select MathOperations.Divide(15, denominator);
```

### Q2: How are errors handled when they occur in a LINQ expression?
**A**: When a failure occurs midway through chaining, the remaining steps are skipped and the error is reflected in the final `Fin<T>` result. The original error message is preserved as-is, so you just need to process the final result with `Match`.

```csharp
// Error handling example from an actual project
var divisionResult = from ten in Denominator.Create(10)
                     from zero in Denominator.Create(0)  // Failure occurs
                     select ten / zero;

divisionResult.Match(
    Succ: value => Console.WriteLine($"Result: {value}"),
    Fail: error => Console.WriteLine($"Error: {error}")  // Error handling
);

// Chained error handling example
var chainResult = from a in Denominator.Create(20)
                  from b in Denominator.Create(4)
                  from c in Denominator.Create(0)  // Failure occurs
                  select a / b / c;

chainResult.Match(
    Succ: value => Console.WriteLine($"Chained operation result: {value}"),
    Fail: error => Console.WriteLine($"Chained operation error: {error}")
);
```

### Q3: Does adding many operator overloads affect performance?
**A**: The performance impact is negligible. Operator overloads are converted to method calls by the compiler, and the JIT compiler can apply inline optimizations, so only the same level of overhead as regular method calls is incurred.

---

Up to this point, we have covered all the basic concepts of Part 1 (validity guarantee, operator overloading, LINQ expressions). These three concepts combine to form the foundation of a value object that "secures validity at creation time, performs natural operations, and automatically propagates errors." In the next chapter, we implement **value equality** to learn how to treat two objects with the same value as equal.

→ [Chapter 7: Value Equality](../07-Value-Equality/)
