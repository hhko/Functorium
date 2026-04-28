---
title: "Simple Value Object"
---

> `SimpleValueObject<T>`

## Overview

In Parts 1-2, we learned the concepts of value objects and validation patterns. Now we learn how to quickly implement value objects by inheriting from framework base classes, without repeating boilerplate each time.

`SimpleValueObject<T>` is the most basic form of the value object pattern, providing immutability, value-based equality, and type safety simply through base class inheritance.

## Learning Objectives

- Implement a value object by inheriting from `SimpleValueObject<T>`
- Explain the behavior of value-based equality (`==`, `!=`) and hash code (`GetHashCode()`)
- Extract the internal value using explicit type conversion
- Understand and apply the immutability guarantee mechanism of value objects

## Why Is This Needed?

Three practical problems arise when representing domain values with primitive types.

If both user ID and order ID are `int`, an assignment like `userId = orderId` passes compilation. The type system fails to catch logical errors. Also, primitive types only store data without including validation or business logic, so related logic gets scattered across multiple locations, making maintenance difficult. Finally, primitive types allow values to be changed at any time, which can cause unexpected side effects.

The value object pattern resolves all three problems at once. It secures compile time safety with meaningful types, encapsulates data and validation logic in one place, and prevents value changes after creation.

## Core Concepts

### Value-Based Equality

Regular classes treat two instances as different objects when their memory addresses differ. Value objects, on the other hand, consider instances equal if their internal values are the same.

If two `BinaryData` objects have the same byte array, they are treated as the same object. Thanks to this characteristic, deduplication and searching in collections work intuitively.

```csharp
// Regular class: reference equality
var obj1 = new SomeClass { Value = 42 };
var obj2 = new SomeClass { Value = 42 };
Console.WriteLine(obj1 == obj2); // false (different references)

// Value object: value equality
var data1 = BinaryData.Create(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F });
var data2 = BinaryData.Create(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F });
Console.WriteLine(data1 == data2); // true (same values)
```

### Immutability Guarantee

Value objects cannot be changed after creation. The constructor is declared `private` and the design allows creation only through a static factory method. Thanks to this structure, race conditions do not occur even when multiple threads read the value object simultaneously.

```csharp
// Value object: immutability guaranteed
var data = BinaryData.Create(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F });
var data2 = (BinaryData)data;

// Cannot change the value of data
// data.Value = new byte[] { 0x57, 0x6F, 0x72, 0x6C, 0x64 }; // Compilation error

// Must create a new value object
var newData = BinaryData.Create(new byte[] { 0x57, 0x6F, 0x72, 0x6C, 0x64 });
```

### Type Safety

Using domain-appropriate types instead of primitive types allows the compiler to detect type mismatches early. This effect grows in larger applications.

```csharp
// Primitive types: lack of type safety
int userId = 123;
int productId = 456;
userId = productId; // Compiles but is a logical error

// Value objects: type safety guaranteed
UserId userId = UserId.Create(123);
ProductId productId = ProductId.Create(456);
// userId = productId; // Compilation error - type mismatch
```

## Practical Guidelines

### Expected Output
```
=== 1. Non-Comparable Primitive Value Object - SimpleValueObject<T> ===
Parent class: SimpleValueObject<byte[]>
Example: BinaryData (binary data)

Features:
   Basic equality comparison and hash code provided
   Comparison operators not supported (IComparable<T> not implemented)
   Explicit type conversion supported
   Suitable for simple value wrapping

Success Cases:
   BinaryData(Hello): BinaryData[5 bytes: 48 65 6C 6C 6F]
   BinaryData(World): BinaryData[5 bytes: 57 6F 72 6C 64]
   BinaryData(Hello): BinaryData[5 bytes: 48 65 6C 6C 6F]

Equality Comparison:
   BinaryData[5 bytes: 48 65 6C 6C 6F] == BinaryData[5 bytes: 57 6F 72 6C 64] = False
   BinaryData[5 bytes: 48 65 6C 6C 6F] == BinaryData[5 bytes: 48 65 6C 6C 6F] = True

Type Conversion:
   (byte[])BinaryData[5 bytes: 48 65 6C 6C 6F] = [0x48, 0x65, 0x6C, 0x6C, 0x6F]

Hash Code:
   BinaryData[5 bytes: 48 65 6C 6C 6F].GetHashCode() = -1711187277
   BinaryData[5 bytes: 48 65 6C 6C 6F].GetHashCode() = -1711187277
   Same value hash codes equal? True

Failure Cases:
   BinaryData(null): Domain.BinaryData.Empty
   BinaryData(empty): Domain.BinaryData.Empty

Demo completed successfully!
```

### Key Implementation Points

The following table summarizes the four essential elements when implementing a `SimpleValueObject<T>`-based value object.

| Point | Description |
|--------|------|
| **Inherit `SimpleValueObject<T>`** | Inherits basic value object functionality |
| **private constructor** | Restricts direct creation from outside |
| **Static Create method** | Handles validation and object creation |
| **DomainError.For\<T>()** | Static method for structured error handling |

## Project Description

### Project Structure
```
01-SimpleValueObject/
├── Program.cs                    # Main entry point
├── SimpleValueObject.csproj     # Project file
├── ValueObjects/
│   └── BinaryData.cs           # Binary data value object
└── README.md                   # Project document
```

### Core Code

`BinaryData` inherits from `SimpleValueObject<byte[]>` to represent binary data as a value object.

**BinaryData.cs - value object implementation**
```csharp
public sealed class BinaryData : SimpleValueObject<byte[]>
{
    private BinaryData(byte[] value) : base(value) { }

    public static Fin<BinaryData> Create(byte[] value) =>
        CreateFromValidation(Validate(value), v => new BinaryData(v));

    public static BinaryData CreateFromValidated(byte[] validatedValue) =>
        new(validatedValue);

    public static Validation<Error, byte[]> Validate(byte[] value) =>
        value != null && value.Length > 0
            ? value
            : DomainError.For<BinaryData, byte[]>(new DomainErrorKind.Empty(), value!,
                $"Binary data cannot be empty or null. Current value: '{(value == null ? "null" : $"{value.Length} bytes")}'");
}
```

Demo code that verifies equality comparison, type conversion, and failure cases.

**Program.cs - demo code**
```csharp
// Success cases
var data1 = BinaryData.Create(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F });
var data2 = BinaryData.Create(new byte[] { 0x57, 0x6F, 0x72, 0x6C, 0x64 });
var data3 = BinaryData.Create(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F });

// Equality comparison
Console.WriteLine($"   {(BinaryData)data1} == {(BinaryData)data2} = {(BinaryData)data1 == (BinaryData)data2}");
Console.WriteLine($"   {(BinaryData)data1} == {(BinaryData)data3} = {(BinaryData)data1 == (BinaryData)data3}");

// Type conversion
var binaryData = (BinaryData)data1;
var bytes = (byte[])binaryData;
```

## Summary at a Glance

Compares the difference between a regular class and a `SimpleValueObject<T>`-based value object.

### Comparison Table
| Aspect | Regular class | value object (`SimpleValueObject<T>`) |
|------|------------|-------------------------------|
| **Equality** | reference equality | value equality |
| **Immutability** | Mutable | Immutable |
| **Type safety** | Primitive types | Meaningful types |
| **Comparison operators** | N/A | Not supported (IComparable not implemented) |
| **Usage** | General objects | Value representation |

## FAQ

### Q1: When should `SimpleValueObject<T>` be used?
**A**: It is suitable for wrapping a single primitive type to represent a domain concept where size comparison is not needed. Typical examples include user IDs, email addresses, and phone numbers.

### Q2: What is the difference between a value object and a regular class?
**A**: Regular classes use reference equality and values can be changed. Value objects treat instances as identical if their internal values are the same, and values cannot be changed after creation.

### Q3: Why are comparison operators not supported?
**A**: In binary data, "greater than/less than" can be interpreted differently depending on the domain, which can cause confusion. If comparison is needed, use `ComparableSimpleValueObject<T>`.

The next chapter covers `ComparableSimpleValueObject<T>`, which adds comparison functionality to `SimpleValueObject<T>`. It examines how to support sorting and comparison operations when a value object has a natural ordering.

---

-> [Chapter 2: ComparableSimpleValueObject](../02-ComparableSimpleValueObject/)
