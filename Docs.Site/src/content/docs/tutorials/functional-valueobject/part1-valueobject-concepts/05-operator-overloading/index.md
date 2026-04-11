---
title: "Operator Overloading"
---
## Overview

`numerator / denominator.Value` -- even after introducing value objects, if you have to extract `.Value` every time, the gap between domain language and code still remains. By leveraging operator overloading, you can use natural mathematical expressions like `15 / denominator` directly in code without `.Value`.

## Learning Objectives

Upon completing this chapter, you will be able to:

- Implement **operator overloading** for user-defined types in C#.
- Make **natural operations possible without `.Value`** when using value objects.
- Implement safe type conversions through **explicit conversion operators**.

### **What You Will Verify Through Practice**
- **Natural division**: Intuitive operations in the form `15 / denominator`
- **Type conversion**: Explicit conversion like `(Denominator)15` and `int value = (int)denominator`
- **Improved usability**: No need to access `.Value` property compared to the previous step

## Why Is This Needed?

In the previous step `04-Always-Valid`, we introduced value objects to guarantee validity at compile time, but there was still the constraint of having to access the internal value through the `.Value` property.

The expression `numerator / denominator.Value` does not match mathematical intuition. In mathematics, we write "15 / 5", not "15 / the value of 5". This mismatch makes code difficult for domain experts to read and violates the **Ubiquitous Language** principle. Additionally, having to access `.Value` every time a value object is used makes code unnecessarily verbose and fails to properly leverage the advantages of encapsulation.

Introducing **operator overloading** enables natural mathematical expressions like `15 / denominator` directly in code and allows more intuitive expression of domain language.

## Core Concepts

### Operator Overloading

Operator overloading is a C# feature that **redefines the behavior of existing operators for user-defined types**. Instead of `numerator / denominator.Value`, you can write `numerator / denominator`.

Comparing the previous approach with the improved approach:

```csharp
// Previous approach (problematic) - requires .Value property access
public static int Divide(int numerator, Denominator denominator)
{
    return numerator / denominator.Value;  // .Value needed
}

// Improved approach (operator overloading) - natural operations
public static int operator /(int numerator, Denominator denominator)
{
    return numerator / denominator._value;  // .Value not needed
}
```

The code becomes identical to mathematical expressions, making it easily understandable by domain experts.

### Conversion Operators

Conversion operators implement **safe conversions between types**. The conversion from `int` to `Denominator` requires validity checking, so it is defined as an explicit conversion.

```csharp
// Explicit conversion - safe conversion
public static explicit operator Denominator(int value)
{
    return Denominator.Create(value).Match(
        Succ: x => x,
        Fail: _ => throw new InvalidCastException("0 cannot be converted to Denominator")
    );
}

// Explicit conversion - automatic conversion
public static explicit operator int(Denominator denominator)
{
    return denominator._value;  // Safe conversion
}
```

Using explicit conversion forces the compiler to clearly mark type conversion points, preventing unintended conversions that could occur with implicit conversion.

### Natural Expression of Domain Language

The ultimate goal of operator overloading is to make code match the domain expert's language.

```csharp
// Previous approach - differs from domain language
var result = numerator / denominator.Value;  // "15 divided by the value of 5"

// Improved approach - matches domain language
var result = numerator / denominator;        // "15 divided by 5"
```

## Practical Guidelines

### Expected Output
```
=== Natural Division Operations Through Operator Overloading ===

1. Key improvement: Natural division operations
  Before (04-Always-Valid): numerator / denominator.Value
  After  (05-Operator-Overloading): numerator / denominator
  15 / OperatorOverloading.ValueObjects.Denominator = 3
  15 / OperatorOverloading.ValueObjects.Denominator = 3 (direct operator)

2. Conversion operators:
  int to Denominator conversion:
    15 -> Denominator: OperatorOverloading.ValueObjects.Denominator
    Denominator -> int: 15
    0 -> Denominator(conversion failed): 0 cannot be converted to Denominator

3. Error handling:
  Error handling during operations:
    Denominator creation failed: Zero is not allowed
```

### **Key Implementation Points**
1. **Operator overloading**: Implement `public static int operator /(int numerator, Denominator denominator)`
2. **Conversion operators**: Implement `explicit operator Denominator(int value)` and `implicit operator int(Denominator value)`
3. **Error handling**: `InvalidCastException` thrown when attempting to convert 0

## Project Description

### **Project Structure**
```
OperatorOverloading/                        # Main project
├── Program.cs                              # Main entry file
├── MathOperations.cs                       # Math operations using operator overloading
├── ValueObjects/
│   └── Denominator.cs                      # Denominator value object with operator overloading
├── OperatorOverloading.csproj              # Project file
└── README.md                               # Main documentation
```

### **Core Code**

#### **Denominator Class - Operator Overloading**
```csharp
public sealed class Denominator
{
    private readonly int _value;

    // Core: Division operator between int and Denominator
    public static int operator /(int numerator, Denominator denominator) =>
        numerator / denominator._value;

    // Conversion operators
    // Explicit conversion operator
    public static explicit operator Denominator(int value) =>
        Create(value).Match(
            Succ: x => x,
            Fail: _ => throw new InvalidCastException("0 cannot be converted to Denominator")
        );

    // Explicit conversion operator
    public static explicit operator int(Denominator value) =>
        value._value;
}
```

#### **MathOperations - Natural Operations**
```csharp
public static class MathOperations
{
    public static int Divide(int numerator, Denominator denominator)
    {
        // Key improvement: natural operations without .Value
        return numerator / denominator;
    }
}
```

#### **Test - Verifying Operator Behavior**
```csharp
// Natural division operations
int result = MathOperations.Divide(15, denom);
int directResult = 15 / denom;  // Direct operator usage

// Conversion operator tests
var nonZero = (Denominator)15;  // Explicit conversion: Denominator <- int
int intValue = (int)nonZero;    // Explicit conversion: int         <- Denominator
```

## Summary at a Glance

### **Improvement Comparison**

Showing the usability difference between the previous and current steps.

| Aspect | Previous (04-Always-Valid) | Current (05-Operator-Overloading) |
|--------|----------------------------|-----------------------------------|
| **Operation expression** | `numerator / denominator.Value` | `numerator / denominator` |
| **Readability** | Complex with `.Value` property access | Natural mathematical expression |
| **Domain language** | Programming language-centric | Domain-centric intuitive expression |
| **Usability** | Internal value extraction needed | Direct operations possible |

### **Pros and Cons**

Benefits and considerations when introducing operator overloading.

| Pros | Cons |
|------|------|
| **Natural domain language** | **Increased implementation complexity** |
| **Improved readability** | **Limited internal value access during debugging** |
| **Intuitive operations** | **Requires redefining operator semantics** |
| **Maintained type safety** | **Confusion from incorrect operator overloading** |

### **Key Techniques**
- **Operator overloading**: Redefining the `/` operator
- **Conversion operators**: `explicit`/`implicit` conversion support
- **Value object pattern**: Maintaining immutability and validity verification
- **Functional programming**: Using LanguageExt's `Fin<T>`

## FAQ

### Q1: Does operator overloading affect performance?
**A**: Almost none. Operator overloading is converted to method calls at compile time, and the JIT compiler can apply inline optimizations, so runtime overhead is at the same level as regular method calls.

### Q2: Can all operators be overloaded?
**A**: Most arithmetic/comparison operators (`+`, `-`, `*`, `/`, `==`, `!=`, etc.) can be overloaded, but the assignment operator (`=`) and member access operator (`.`) cannot be overloaded.

```csharp
// Overloadable operators
public static T operator +(T a, T b)     // Addition
public static T operator -(T a, T b)     // Subtraction
public static T operator *(T a, T b)     // Multiplication
public static T operator /(T a, T b)     // Division
public static bool operator ==(T a, T b) // Equality comparison
public static bool operator !=(T a, T b) // Inequality comparison

// Non-overloadable operators
// public static T operator =(T a, T b)  // Assignment operator
// public static T operator .(T a, T b)  // Member access operator
```

### Q3: What is the difference between operator overloading and method overloading?
**A**: Operator overloading redefines the behavior of operators like `+`, `/` to use mathematical expressions directly in code. Method overloading defines methods with the same name but different parameters to handle various inputs. When domain operations need to be expressed intuitively, operator overloading is more natural.

```csharp
// Operator overloading
public static int operator /(int a, Denominator b) => a / b._value;

// Method overloading
public int Divide(int a) => a / _value;
public int Divide(double a) => (int)(a / _value);
```

---

Operator overloading gave us natural mathematical expressions, but the inconvenience of nesting `Match` methods for compound operations with multiple `Fin<T>` values remains. In the next chapter, we introduce **LINQ expressions** to handle compound operations and error propagation concisely using `from`/`select` syntax.

→ [Chapter 6: LINQ Expressions](../06-Linq-Expression/)
