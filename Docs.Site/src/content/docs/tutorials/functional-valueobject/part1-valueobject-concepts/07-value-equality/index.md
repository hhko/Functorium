---
title: "Value Equality"
---
## Overview

Are `Denominator(5)` and `Denominator(5)` the same object? In C#'s default behavior, they return `false` because they have different memory addresses. However, if they are value objects, they should be treated as the same object when their internal values are equal. In this chapter, we implement type-safe value-based equality through the `IEquatable<T>` interface and guarantee correct behavior in collections such as `HashSet<T>` and `Dictionary<TKey, TValue>`.

## Learning Objectives

1. You can correctly implement value object equality using the `IEquatable<T>` interface
2. You can understand the difference between reference equality and value equality and choose the correct equality for the situation
3. You can verify that value-based equality works correctly in hash-based collections such as `HashSet<T>` and `Dictionary<TKey, TValue>`

## Why Is This Needed?

In the previous step `LinqExpression`, we implemented functional composability through monadic chaining. However, problems emerge when using value objects in collections or performing comparison operations.

All reference types in C# use reference equality by default. Even if the values are the same, different instances return `false`, so in `HashSet<T>` or `Dictionary<TKey, TValue>`, objects with the same value are stored as duplicates or key lookups fail. This happens because `GetHashCode()` and `Equals()` are not implemented consistently. Additionally, using `Object.Equals()` causes boxing/unboxing overhead for value types, degrading performance when processing large volumes of data.

By implementing the `IEquatable<T>` interface, you can secure type safety, performance optimization, and collection compatibility all at once.

## Core Concepts

### Value-Based Equality

This is the concept of treating two objects as identical when their internal values are the same. It is a core principle of the Value Object pattern in DDD, and it guarantees that the same input always produces the same result.

The following code shows the difference between reference equality and value-based equality.

```csharp
// Previous approach (reference equality) - problematic approach
var a = new Denominator(5);
var b = new Denominator(5);
Console.WriteLine(a == b); // false (different memory addresses)

// Improved approach (value-based equality) - correct approach
var a = Denominator.Create(5).Match(Succ: x => x, Fail: _ => throw new Exception());
var b = Denominator.Create(5).Match(Succ: x => x, Fail: _ => throw new Exception());
Console.WriteLine(a == b); // true (same value)
```

### The `IEquatable<T>` Interface

`IEquatable<T>` provides type safety in equality comparisons. Unlike `Object.Equals(object?)`, there is no boxing/unboxing overhead, and type checking is performed at compile time.

```csharp
// IEquatable<T> implementation
public sealed class Denominator : IEquatable<Denominator>
{
    public bool Equals(Denominator? other) =>
        other is not null && _value == other._value;

    public override bool Equals(object? obj) =>
        obj is Denominator other && Equals(other);
}
```

### Consistency Between GetHashCode and Equals

Two objects for which `Equals()` returns `true` must have the same `GetHashCode()` value. If this rule is broken, hash-based collections such as `HashSet<T>` and `Dictionary<TKey, TValue>` will not work correctly.

```csharp
// Consistent implementation
public override int GetHashCode() => _value.GetHashCode();

public bool Equals(Denominator? other) =>
    other is not null && _value == other._value;
```

## Practical Guidelines

### Expected Output
```
=== Value Object Equality ===

=== Basic Equality Test ===
a = 5, b = 5, c = 10
a == b: True
a == c: False
a.Equals(b): True
a.Equals(c): False

=== Reference Equality (ReferenceEquals) vs Value Equality (Equals) ===
a = 5, b = 5
ReferenceEquals(a, b): False
a == b: True
a.Equals(b): True

=== Null Equality Test ===
a = 5
a == null: False
null == a: False
a.Equals(null): False
null == null: True

=== Hash Code Test ===
a = 5, b = 5, c = 10
a.GetHashCode(): 5
b.GetHashCode(): 5
c.GetHashCode(): 10
a.GetHashCode() == b.GetHashCode(): True
a.GetHashCode() == c.GetHashCode(): False

=== Equality in Collections Test ===
Original values: [5, 10, 5, 15, 10]
Denominator values: [5, 10, 5, 15, 10]
HashSet (duplicates removed): [5, 10, 15]
Dictionary key count: 3
Value found by key 5: Value_5

=== Performance Comparison Test (1,000,000 items) ===
Using IEquatable<T>: 4ms
Using Object.Equals: 8ms
Performance difference: 4ms

=== Equality in Collections Test ===
Original values: [5, 10, 5, 15, 10]
Denominator values: [5, 10, 5, 15, 10]
HashSet (duplicates removed): [5, 10, 15]
Dictionary key count: 3
Value found by key 5: Value_5

=== Performance Comparison Test (1,000,000 items) ===
Using IEquatable<T>: 4ms
Using Object.Equals: 8ms
Performance difference: 4ms
```

### Key Implementation Points

The following five elements must be implemented together for value-based equality to work completely.

1. **`IEquatable<T>` interface implementation**: Type-safe `Equals(Denominator? other)` method
2. **Object.Equals override**: `Equals(object? obj)` method for compatibility with reference types
3. **GetHashCode override**: Consistent hash code generation for correct behavior in hash-based collections
4. **Operator overloading**: Supporting natural comparison syntax through `==` and `!=` operators
5. **Null safety**: Preventing exceptions when comparing with null references

## Project Description

### Project Structure
```
ValueEquality/
├── ValueObjects/
│   └── Denominator.cs          # Value object with value-based equality
├── Program.cs                  # Main entry file
├── ValueEquality.csproj        # Project file
└── README.md                   # Project documentation
```

### Core Code

#### Denominator Value Object (Value-Based Equality Implementation)
```csharp
public sealed class Denominator : IEquatable<Denominator>
{
    private readonly int _value;

    // IEquatable<T> implementation - type-safe equality comparison
    public bool Equals(Denominator? other) =>
        other is not null && _value == other._value;

    // Object.Equals override - using value equality instead of reference equality
    public override bool Equals(object? obj) =>
        obj is Denominator other && Equals(other);

    // Equality operator overloading
    public static bool operator ==(Denominator? left, Denominator? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Denominator? left, Denominator? right) =>
        !(left == right);

    // GetHashCode override - value-based hash code generation
    public override int GetHashCode() => _value.GetHashCode();
}
```

#### Test Using LINQ Expressions (Monadic Chaining)
```csharp
public static void DemonstrateBasicEquality()
{
    var result = from a in Denominator.Create(5)
                 from b in Denominator.Create(5)
                 from c in Denominator.Create(10)
                 select (a, b, c);

    result.Match(
        Succ: values =>
        {
            var (a, b, c) = values;
            Console.WriteLine($"a == b: {a == b}"); // true (same value)
            Console.WriteLine($"a == c: {a == c}"); // false (different value)
        },
        Fail: error => Console.WriteLine($"Creation failed: {error}")
    );
}
```

## Summary at a Glance

The following table compares the differences between reference equality and value-based equality.

| Aspect | Reference Equality | Value-Based Equality |
|------|-------------|----------------|
| **Comparison criterion** | Memory address | Internal value |
| **Same value, different instance** | `false` | `true` |
| **Collection behavior** | Unpredictable | Correct behavior |
| **Performance** | Boxing/unboxing overhead | Optimized |
| **Type safety** | Insufficient | Guaranteed |

To fully implement value-based equality, all of the following methods must be implemented.

| Method | Implemented | Purpose |
|--------|-----------|------|
| **`IEquatable<T>`.Equals** | Yes | Type-safe equality comparison |
| **Object.Equals** | Yes | Compatibility with reference types |
| **GetHashCode** | Yes | Hash-based collection support |
| **== operator** | Yes | Natural comparison syntax |
| **!= operator** | Yes | Natural comparison syntax |

## FAQ

### Q1: Why must GetHashCode also be overridden?
**A**: There is a contract that two objects for which `Equals` returns `true` must have the same hash code. If this rule is broken, search failures or duplicate storage occur in hash-based collections such as `HashSet<T>` and `Dictionary<TKey, TValue>`.

```csharp
// Incorrect implementation - inconsistent
public override int GetHashCode() => 1; // Always the same hash code
public bool Equals(Denominator? other) => _value == other?._value;

// Correct implementation - consistent
public override int GetHashCode() => _value.GetHashCode();
public bool Equals(Denominator? other) => _value == other?._value;
```

### Q2: Should I use reference equality or value equality?
**A**: Value objects use value equality, while entities use reference equality (or identifier-based equality). Value objects are characterized by immutability and value-based comparison, while entities are distinguished by unique identifiers.

```csharp
// Value object - value-based equality
public class Money : IEquatable<Money>
{
    public bool Equals(Money? other) =>
        Amount == other?.Amount && Currency == other?.Currency;
}

// Entity - reference equality (default behavior)
public class User
{
    public Guid Id { get; set; }
    // Uses reference equality (default behavior)
}
```

### Q3: Why is null checking necessary?
**A**: To prevent `NullReferenceException` when comparing with null references. In the `==` operator, `ReferenceEquals` handles the case where both sides are null first, and returns `false` when only one side is null.

```csharp
// Safe null check
public static bool operator ==(Denominator? left, Denominator? right)
{
    if (ReferenceEquals(left, right)) return true;  // Both null case
    if (left is null || right is null) return false; // One null case
    return left.Equals(right); // Neither null case
}
```

With value equality secured, the next chapter covers implementing order comparison for value objects to enable sorting and range verification through comparability.

---

→ [Chapter 8: Value Comparability](../08-Value-Comparability/)
