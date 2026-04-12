---
title: "Comparable Simple Value Object"
---

> `ComparableSimpleValueObject<T>`

## Overview

`SimpleValueObject<T>` only supports equality comparison. But what if you need to sort user IDs or compare priorities? `ComparableSimpleValueObject<T>` automatically adds `IComparable<T>` implementation and comparison operator overloading to the basic value object functionality, allowing sorting and size comparison to be used just like primitive types.

## Learning Objectives

- Explain how `ComparableSimpleValueObject<T>` differs from `SimpleValueObject<T>`
- Understand the principle by which the `IComparable<T>` interface is automatically implemented
- Naturally sort value objects in collections
- Apply `<`, `<=`, `>`, `>=` operators to value objects

## Why Is This Needed?

When creating value objects with `SimpleValueObject<T>`, equality comparison works but you hit a wall when sorting or size comparison is needed.

To sort value objects in a collection, you must implement separate comparison logic each time. `<`, `>` comparison operators are unavailable, making conditional expressions unintuitive. In data structures where comparison is required, like SortedSet or priority queues, using value objects as keys is impossible.

`ComparableSimpleValueObject<T>` resolves all these problems through base class inheritance alone. By leveraging the natural ordering of the internal value, you can compare and sort just like C# primitive types.

## Core Concepts

### Automatic Comparison Functionality

`ComparableSimpleValueObject<T>` automatically inherits comparison functionality from the parent class. Size comparison based on the internal value is available without any separate implementation.

```csharp
// Comparable value object
UserId id1 = UserId.Create(123);
UserId id2 = UserId.Create(456);

// Natural comparison operations
bool isLess = id1 < id2;      // true
bool isGreater = id1 > id2;   // false
bool isEqual = id1 == id2;    // false

// This works just like primitive types
int num1 = 123;
int num2 = 456;
bool numLess = num1 < num2;   // true
```

### Automatic IComparable\<T> Implementation

`ComparableSimpleValueObject<T>` automatically implements the `IComparable<T>` interface in the parent class. Thanks to this, it can be used directly in .NET standard sorting APIs like `List<T>.Sort()` or LINQ's `OrderBy()` without a separate comparison function.

```csharp
// Automatic sorting possible
List<UserId> userIds = new List<UserId>
{
    UserId.Create(456),
    UserId.Create(123),
    UserId.Create(789)
};

userIds.Sort(); // No separate comparison function needed

// Also naturally usable in LINQ
var sorted = userIds.OrderBy(id => id); // Possible thanks to IComparable<T>
```

### All Comparison Operator Overloading

Not only `IComparable<T>` implementation but also `<`, `<=`, `>`, `>=` operators are automatically overloaded. Intuitive expressions are possible in conditional statements and range checks.

```csharp
// Natural comparison expressions
UserId currentId = UserId.Create(500);
UserId minId = UserId.Create(100);
UserId maxId = UserId.Create(1000);

// Range check
bool isValid = minId <= currentId && currentId <= maxId;

// This reads like mathematics
// 100 <= 500 <= 1000
```

## Practical Guidelines

### Expected Output
```
=== 2. Comparable Primitive Value Object - ComparableSimpleValueObject<T> ===
Parent class: ComparableSimpleValueObject<int>
Example: UserId (user ID)

Features:
   Automatically implements IComparable<UserId>
   All comparison operators overloaded (<, <=, >, >=)
   Explicit type conversion supported
   Equality comparison and hash code automatically provided

Success Cases:
   UserId(123): 123
   UserId(456): 456
   UserId(123): 123

Equality Comparison:
   123 == 456 = False
   123 == 123  =  True

Comparison Functionality (IComparable<T>):
   123 < 456 = True
   123 <= 456 = True
   123 > 456 = False
   123 >= 456 = False

Type Conversion:
   (int)123 = 123

Hash Code:
   123.GetHashCode() = 123
   123.GetHashCode() = 123
   Same value hash codes equal? True

Failure Cases:
   UserId(0): DomainErrors.UserId.NotPositive
   UserId(-1): DomainErrors.UserId.NotPositive

Sorting Demo:
   Sorted UserId list:
     123
     234
     456
     567
     789

Demo completed successfully!
```

### Key Implementation Points

Summarizes the key elements added compared to `SimpleValueObject<T>`.

| Point | Description |
|--------|------|
| **Inherit `ComparableSimpleValueObject<T>`** | Inherits automatic comparison functionality |
| **Automatic `IComparable<T>` implementation** | Provided by parent class |
| **Automatic comparison operator overloading** | `<`, `<=`, `>`, `>=` available |
| **Collection sorting support** | No separate comparison function needed for Sort(), OrderBy() etc. |

## Project Description

### Project Structure
```
02-ComparableSimpleValueObject/
├── Program.cs                          # Main entry point
├── ComparableSimpleValueObject.csproj # Project file
├── ValueObjects/
│   └── UserId.cs                      # User ID value object
└── README.md                          # Project document
```

### Core Code

`UserId` inherits from `ComparableSimpleValueObject<int>` to represent a comparable user ID.

**UserId.cs - comparable value object implementation**
```csharp
public sealed class UserId : ComparableSimpleValueObject<int>
{
    private UserId(int value) : base(value) { }

    public int Id => Value; // public accessor provided

    public static Fin<UserId> Create(int value) =>
        CreateFromValidation(Validate(value), v => new UserId(v));

    public static UserId CreateFromValidated(int validatedValue) =>
        new(validatedValue);

    public static Validation<Error, int> Validate(int value) =>
        ValidationRules<UserId>.Positive(value);

    public static implicit operator int(UserId userId) => userId.Value;
}
```

Demo code that verifies comparison operators and automatic sorting.

**Program.cs - comparison functionality demo**
```csharp
// Using comparison operators
var userId1 = (UserId)id1;
var userId2 = (UserId)id2;
Console.WriteLine($"   {userId1} < {userId2} = {userId1 < userId2}");
Console.WriteLine($"   {userId1} <= {userId2} = {userId1 <= userId2}");

// Automatic sorting
userIds.Sort(); // Automatic sorting possible thanks to IComparable<T>

Console.WriteLine("   Sorted UserId list:");
foreach (var userId in userIds)
{
    Console.WriteLine($"     {userId}");
}
```

## Summary at a Glance

Compares the comparison features added from `SimpleValueObject<T>`.

### Comparison Table
| Aspect | `SimpleValueObject<T>` | `ComparableSimpleValueObject<T>` |
|------|---------------------|-------------------------------|
| **Equality comparison** | Supported | Supported |
| **Comparison operators** | Not supported | Automatically supported (`<`, `<=`, `>`, `>=`) |
| **`IComparable<T>`** | Not implemented | Automatically implemented |
| **Collection sorting** | Manual implementation needed | Automatically supported |
| **LINQ sorting** | Separate key needed | Natural sorting |

## FAQ

### Q1: When should `ComparableSimpleValueObject<T>` be used?
**A**: Use it when the value object has a natural ordering relationship. It is suitable for cases like IDs, version numbers, and priorities where size comparison is meaningful. For values where ordering is not important, such as email addresses or phone numbers, `SimpleValueObject<T>` is appropriate.

### Q2: How is `IComparable<T>` automatically implemented?
**A**: The parent class delegates comparison to the internal `Value`. For `int`-based types it follows the natural order of integers, and for `string`-based types it follows alphabetical order.

### Q3: Can comparison logic be customized?
**A**: `ComparableSimpleValueObject<T>` only supports the natural ordering of the base type. If custom comparison is needed, such as "1.10" > "1.2" for version numbers, you must directly inherit from `ValueObject` and manually implement `IComparable<T>`.

The next chapter covers the `ValueObject` pattern that combines multiple primitive types rather than a single value. It examines how to represent composite data like 2D coordinates as a single value object.

---

-> [Chapter 3: ValueObject (Primitive)](../03-ValueObject-Primitive/)
