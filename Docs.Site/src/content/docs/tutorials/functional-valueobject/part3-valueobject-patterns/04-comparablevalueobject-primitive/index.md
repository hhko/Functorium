---
title: "Comparable Value Object"
---

> `ComparableValueObject`

## Overview

What if you represented a date range with `ValueObject` but need to sort multiple ranges chronologically? `ComparableValueObject` supports sorting and comparison operations naturally by defining the comparison order through `GetComparableEqualityComponents()`, while still combining multiple primitive types.

## Learning Objectives

- Explain how `ComparableValueObject` differs from `ValueObject`
- Define comparison order with `GetComparableEqualityComponents()`
- Sort objects composed of multiple values in collections
- Use composite value objects directly in LINQ's `OrderBy()`

## Why Is This Needed?

`ValueObject` only supports equality comparison. When sorting composite data like date ranges or coordinates, you must specify what value serves as the basis, and must provide a separate comparison function each time for LINQ methods like `OrderBy()` or `Min()`. `<`, `>` comparison operators are also unavailable, making it unintuitive to compare the order of two items.

`ComparableValueObject` supports natural sorting and comparison simply by defining the return order of components in `GetComparableEqualityComponents()`.

## Core Concepts

### GetComparableEqualityComponents() Implementation

`ComparableValueObject` returns the components used for comparison in order via the `GetComparableEqualityComponents()` method. It follows lexicographic order, comparing from the first element and proceeding to the next when values are equal.

For date ranges, it compares start dates first, and if start dates are equal, compares end dates.

```csharp
protected override IEnumerable<IComparable> GetComparableEqualityComponents()
{
    yield return StartDate;  // Compare by start date first
    yield return EndDate;    // Compare by end date if start dates are equal
}
```

### Automatic Comparison Functionality Inheritance

The parent class automatically implements the `IComparable<T>` interface, so it can be used directly in `List<T>.Sort()` or LINQ's `OrderBy()` without a separate comparison function.

```csharp
// Automatic sorting possible
List<DateRange> ranges = new List<DateRange>
{
    new DateRange(start1, end1),
    new DateRange(start2, end2),
    new DateRange(start3, end3)
};

ranges.Sort(); // No separate comparison function needed

// Also naturally usable in LINQ
var sorted = ranges.OrderBy(r => r); // Possible thanks to IComparable<T>
```

### All Comparison Operators Supported

`<`, `<=`, `>`, `>=` operators are all automatically overloaded. Intuitive comparison expressions are possible even for composite data.

```csharp
// Natural comparison expressions
DateRange range1 = DateRange.Create(start1, end1);
DateRange range2 = DateRange.Create(start2, end2);

// Intuitive comparison
bool isEarlier = range1 < range2;    // Is range1 earlier than range2?
bool isLater = range1 > range2;      // Is range1 later than range2?
bool overlaps = range1 <= range2;    // Does range1 overlap with range2?
```

## Practical Guidelines

### Expected Output
```
=== 4. Comparable Composite Primitive Value Object - ComparableValueObject ===
Parent class: ComparableValueObject
Example: DateRange (date range)

Features:
   Combines multiple primitive values
   Comparison functionality automatically provided
   Date range validity validation

Success Cases:
   DateRange: 2024-01-01 ~ 2024-06-30
     - StartDate: 2024-01-01
     - EndDate: 2024-06-30

   DateRange: 2024-07-01 ~ 2024-12-31
     - StartDate: 2024-07-01
     - EndDate: 2024-12-31

   DateRange: 2024-01-01 ~ 2024-06-30
     - StartDate: 2024-01-01
     - EndDate: 2024-06-30

Equality Comparison:
   2024-01-01 ~ 2024-06-30 == 2024-07-01 ~ 2024-12-31 = False
   2024-01-01 ~ 2024-06-30 == 2024-01-01 ~ 2024-06-30 = True

Comparison Functionality (IComparable<T>):
   2024-01-01 ~ 2024-06-30 < 2024-07-01 ~ 2024-12-31 = True
   2024-01-01 ~ 2024-06-30 <= 2024-07-01 ~ 2024-12-31 = True
   2024-01-01 ~ 2024-06-30 > 2024-07-01 ~ 2024-12-31 = False
   2024-01-01 ~ 2024-06-30 >= 2024-07-01 ~ 2024-12-31 = False

Hash Code:
   2024-01-01 ~ 2024-06-30.GetHashCode() = -1711187277
   2024-01-01 ~ 2024-06-30.GetHashCode() = -1711187277
   Same value hash codes equal? True

Failure Cases:
   DateRange(2024-12-31, 2024-01-01): StartAfterEnd

Sorting Demo:
   Sorted DateRange list:
     2024-01-01 ~ 2024-03-31
     2024-04-01 ~ 2024-05-31
     2024-06-01 ~ 2024-06-30
     2024-09-01 ~ 2024-12-31

Comparable primitive composition value object characteristics:
   - Combines multiple primitive types (DateTime, etc.)
   - Individual validation logic for each primitive value
   - Provides both equality comparison and comparison functionality
   - Expresses complex domain concepts with sorting and size comparison

Demo completed successfully!
```

### Key Implementation Points

Summarizes the key elements added compared to `ValueObject`.

| Point | Description |
|--------|------|
| **Inherit ComparableValueObject** | Inherits automatic comparison functionality |
| **Implement GetComparableEqualityComponents()** | Defines comparison order |
| **Automatic `IComparable<T>` implementation** | Provided by parent class |
| **Automatic comparison operator support** | `<`, `<=`, `>`, `>=` available |

## Project Description

### Project Structure
```
04-ComparableValueObject-Primitive/
├── Program.cs                              # Main entry point
├── ComparableValueObjectPrimitive.csproj  # Project file
├── ValueObjects/
│   └── DateRange.cs                       # Date range value object
└── README.md                              # Project document
```

### Core Code

`DateRange` inherits from `ComparableValueObject` to represent start and end dates as a single comparable date range.

**DateRange.cs - comparable composite primitive value object implementation**
```csharp
public sealed class DateRange : ComparableValueObject
{
    public sealed record StartAfterEnd : DomainErrorType.Custom;
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    private DateRange(DateTime startDate, DateTime endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }

    public static Fin<DateRange> Create(DateTime startDate, DateTime endDate) =>
        CreateFromValidation(Validate(startDate, endDate), v => new DateRange(v.startDate, v.endDate));

    public static DateRange CreateFromValidated((DateTime startDate, DateTime endDate) validatedValues) =>
        new(validatedValues.startDate, validatedValues.endDate);

    // Date range validation
    public static Validation<Error, (DateTime startDate, DateTime endDate)> Validate(
        DateTime startDate, DateTime endDate) =>
        startDate <= endDate
            ? (startDate, endDate)
            : DomainError.For<DateRange, DateTime, DateTime>(new StartAfterEnd(), startDate, endDate,
                $"Start date cannot be after end date. Start: '{startDate:yyyy-MM-dd}', End: '{endDate:yyyy-MM-dd}'");

    // Comparison order definition
    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return StartDate;  // Compare start date first
        yield return EndDate;    // Compare end date if start dates are equal
    }

    public override string ToString() => $"{StartDate:yyyy-MM-dd} ~ {EndDate:yyyy-MM-dd}";
}
```

Demo code that verifies comparison operators and automatic sorting.

**Program.cs - comparable composite value object demo**
```csharp
// Using comparison operators
var r1 = range1.Match(Succ: x => x, Fail: _ => default!);
var r2 = range2.Match(Succ: x => x, Fail: _ => default!);
Console.WriteLine($"   {r1} < {r2} = {r1 < r2}");
Console.WriteLine($"   {r1} <= {r2} = {r1 <= r2}");

// Automatic sorting
var ranges = new[] { ... }
    .Select(r => DateRange.Create(r.Item1, r.Item2))
    .Where(result => result.IsSucc)
    .Select(result => result.Match(Succ: x => x, Fail: _ => default!))
    .OrderBy(r => r)  // Natural sorting
    .ToArray();
```

## Summary at a Glance

Compares the difference between `ValueObject` and `ComparableValueObject`.

### Comparison Table
| Aspect | ValueObject | ComparableValueObject |
|------|-------------|----------------------|
| **Comparison functionality** | Not supported | Automatically supported |
| **GetComparableEqualityComponents()** | N/A | Required implementation |
| **`IComparable<T>`** | Not implemented | Automatically implemented |
| **LINQ sorting** | Manual implementation | Automatically supported |
| **operator overloading** | Not supported | Automatically supported |

## FAQ

### Q1: What is the difference between GetComparableEqualityComponents() and GetEqualityComponents()?
**A**: `GetEqualityComponents()` is used only for equality comparison, while `GetComparableEqualityComponents()` is used for both equality and sort comparison. The latter must return elements of `IComparable` type, and the order of elements determines the sorting priority.

### Q2: How is the comparison order determined?
**A**: It is determined by the order in which elements are returned from `GetComparableEqualityComponents()`. If the first element is different, that determines the result; if equal, it proceeds to the next element. Specify the order to match the domain -- start date first for date ranges, X-axis first for coordinates, etc.

### Q3: Should ComparableValueObject always be used?
**A**: No. For composite data that does not need sorting or size comparison, `ValueObject` is sufficient. Unnecessary comparison functionality only adds the burden of implementing `GetComparableEqualityComponents()`, so choose `ComparableValueObject` only when sorting is actually needed.

The next chapter covers the composite value object pattern that combines other value objects rather than primitive types. It examines how to build richer domain models by including value objects within value objects.

---

-> [Chapter 5: ValueObject (Composite)](../05-ValueObject-Composite/)
