---
title: "Value Object"
---

> `ValueObject`

## Overview

A 2D coordinate is meaningful only when X and Y values always travel together. The `SimpleValueObject<T>` we have learned so far can only wrap a single value, so how do we represent domain concepts that combine multiple primitive types? `ValueObject` bundles multiple values into a single immutable unit and provides component-based equality comparison through `GetEqualityComponents()`.

## Learning Objectives

- Implement a composite value object by combining multiple primitive types
- Define equality comparison criteria with the `GetEqualityComponents()` method
- Implement composite validation logic using LINQ Expressions
- Distinguish and apply individual validation and integrated validation for each component

## Why Is This Needed?

In real domains, many concepts cannot be represented by a single value. If the X and Y of a 2D coordinate are managed as separate variables, one side can be updated alone, easily breaking data consistency. When multiple values are related and must be validated together, guaranteeing validity with individual variables becomes complex. Also, manually implementing equality comparison for composite data is prone to errors like missing components.

`ValueObject` encapsulates related values into a single immutable object and allows managing equality comparison and validation logic in one place.

## Core Concepts

### Primitive Type Composition

`ValueObject` combines multiple base types into a single meaningful unit. It groups related dispersed data into a single object, increasing cohesion and concentrating related logic.

```csharp
// Dispersed data (problematic)
int x = 100;
int y = 200;

// Composed data (resolved)
Coordinate coord = Coordinate.Create(100, 200);
```

### GetEqualityComponents() Implementation

`ValueObject` must implement the `GetEqualityComponents()` method for equality comparison. All components returned by this method must be equal for two instances to be considered identical.

For `Coordinate`, both X and Y values must be the same to be treated as the same coordinate. If you want to exclude a specific field from equality comparison, simply do not return that field.

```csharp
protected override IEnumerable<object> GetEqualityComponents()
{
    yield return X;  // X coordinate as comparison element
    yield return Y;  // Y coordinate as comparison element
}
```

### Composite Validation Logic

`ValueObject` can perform both individual validation for each component and overall validity validation. Using the `from-in-select` pattern of LINQ Expressions, sequential validation can be expressed declaratively.

```csharp
public static Validation<Error, (int x, int y)> Validate(int x, int y) =>
    from validX in ValidateX(x)      // Individual X coordinate validation
    from validY in ValidateY(y)      // Individual Y coordinate validation
    select (x: validX, y: validY);   // Combine validated values
```

## Practical Guidelines

### Expected Output
```
=== 3. Non-Comparable Composite Primitive Value Object - ValueObject ===
Parent class: ValueObject
Example: Coordinate (2D coordinate)

Features:
   Combines multiple primitive values
   Provides equality comparison only
   Comparison functionality intentionally not provided

Success Cases:
   Coordinate: (100, 200) (X: 100, Y: 200)
   Coordinate: (100, 200) (X: 100, Y: 200)
   Coordinate: (300, 400) (X: 300, Y: 400)

Equality Comparison:
   (100, 200) == (100, 200) = True
   (100, 200) == (300, 400) = False

Hash Code:
   (100, 200).GetHashCode() = -1711187277
   (100, 200).GetHashCode() = -1711187277
   Same value hash codes equal? True

Comparison Functionality:
   Comparison functionality intentionally not provided
   Use ComparableValueObject when sorting or size comparison is needed

Failure Cases:
   Coordinate(-1, 200): XOutOfRange
   Coordinate(100, 2000): YOutOfRange

Primitive composition value object characteristics:
   - Combines multiple primitive types (int, string, decimal, etc.)
   - Individual validation logic for each primitive value
   - Provides equality comparison only (no comparison functionality)
   - Expresses complex domain concepts as simple primitive compositions

Demo completed successfully!
```

### Key Implementation Points

Summarizes the essential elements of composite `ValueObject`-based value object implementation.

| Point | Description |
|--------|------|
| **Inherit ValueObject** | Inherits basic composite value object functionality |
| **Implement GetEqualityComponents()** | Defines components for equality comparison |
| **LINQ Expression validation** | Composite validation using from-in-select pattern |
| **Individual validation methods** | Independent validation for each primitive value |

## Project Description

### Project Structure
```
03-ValueObject-Primitive/
├── Program.cs                    # Main entry point
├── ValueObjectPrimitive.csproj  # Project file
├── ValueObjects/
│   └── Coordinate.cs            # 2D coordinate value object
└── README.md                    # Project document
```

### Core Code

`Coordinate` inherits from `ValueObject` to represent two integers X and Y as a single 2D coordinate.

**Coordinate.cs - composite primitive value object implementation**
```csharp
public sealed class Coordinate : ValueObject
{
    public int X { get; }
    public int Y { get; }

    private Coordinate(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static Fin<Coordinate> Create(int x, int y) =>
        CreateFromValidation(Validate(x, y), v => new Coordinate(v.x, v.y));

    public static Coordinate CreateFromValidated((int x, int y) validatedValues) =>
        new(validatedValues.x, validatedValues.y);

    // Composite validation using LINQ Expression
    public static Validation<Error, (int x, int y)> Validate(int x, int y) =>
        from validX in ValidateX(x)
        from validY in ValidateY(y)
        select (x: validX, y: validY);

    // Individual validation using ValidationRules<T>
    private static Validation<Error, int> ValidateX(int x) =>
        ValidationRules<Coordinate>.NonNegative(x);

    private static Validation<Error, int> ValidateY(int y) =>
        ValidationRules<Coordinate>.Between(y, 0, 1000);

    // Components for equality comparison
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return X;
        yield return Y;
    }

    public override string ToString() => $"({X}, {Y})";
}
```

Demo code that verifies equality comparison and hash code.

**Program.cs - composite value object demo**
```csharp
// Create composite value objects
var coord1 = Coordinate.Create(100, 200);
var coord2 = Coordinate.Create(100, 200);
var coord3 = Coordinate.Create(300, 400);

// Equality comparison
var c1 = coord1.Match(Succ: x => x, Fail: _ => default!);
var c2 = coord2.Match(Succ: x => x, Fail: _ => default!);
Console.WriteLine($"   {c1} == {c2} = {c1 == c2}");

// Hash code verification
Console.WriteLine($"   {c1}.GetHashCode() = {c1.GetHashCode()}");
Console.WriteLine($"   {c2}.GetHashCode() = {c2.GetHashCode()}");
```

## Summary at a Glance

Compares the difference between single value wrapping and composite value composition.

### Comparison Table
| Aspect | `SimpleValueObject<T>` | ValueObject |
|------|---------------------|-------------|
| **Number of values** | Single primitive | Composite primitive |
| **GetEqualityComponents()** | Automatically implemented | Manual implementation required |
| **Validation logic** | Simple validation | Composite validation possible |
| **LINQ usage** | Not needed | Useful for composite validation |
| **Usage** | Simple value wrapping | Composite domain concepts |

## FAQ

### Q1: Why is GetEqualityComponents() necessary?
**A**: It is needed to define equality for composite value objects. For coordinates, both X and Y must be equal for the same coordinate, so both values are returned. To exclude a specific field from comparison, simply do not return that field.

### Q2: Why is LINQ Expression used?
**A**: The `from-in-select` pattern allows expressing composite validation declaratively. Short-circuit evaluation where Y validation is skipped if X validation fails is naturally implemented, and it is more readable than if-else chains.

### Q3: When should a regular class be used instead of ValueObject?
**A**: When the value needs to change or reference equality is required. Data that changes frequently, like a bank account balance, is suitable for regular classes. Use `ValueObject` for values that do not change after creation, such as events and configuration values.

The next chapter covers `ComparableValueObject`, which adds comparison functionality to `ValueObject`. It covers cases where composite data like date ranges also needs natural ordering.

---

-> [Chapter 4: ComparableValueObject (Primitive)](../04-ComparableValueObject-Primitive/)
