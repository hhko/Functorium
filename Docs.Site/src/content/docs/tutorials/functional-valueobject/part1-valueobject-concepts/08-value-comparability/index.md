---
title: "Value Comparability"
---
## Overview

We can now determine whether two `Denominator` objects are equal. But what if you need to know which denominator is larger, or sort a list of denominators in ascending order? In this chapter, we add ordering comparison to value objects through `IComparable<T>` and implement custom comparison strategies such as case-insensitive comparison through `IEqualityComparer<T>`.

## Learning Objectives

1. You can implement `IComparable<T>` to add sorting and range search capabilities to value objects
2. You can consistently overload `<`, `>`, `<=`, `>=` comparison operators based on `CompareTo`
3. You can use `IEqualityComparer<T>` to separate custom comparison strategies different from the default equality into separate classes

## Why Is This Needed?

In the previous step `ValueEquality`, we implemented value object equality so we could only determine whether two objects are equal or not. However, real applications also need ordering comparison and collection optimization.

Without implementing `IComparable<T>`, you cannot use sorting/searching APIs like `List<T>.Sort()` or `Array.BinarySearch()`. Additionally, default equality comparison alone cannot meet various comparison requirements such as case-insensitive comparison or special rules. By introducing `IEqualityComparer<T>`, you can inject comparison strategies externally without modifying the value object itself.

## Core Concepts

### The `IComparable<T>` Interface

`IComparable<T>` provides ordering comparison capability to value objects. The `CompareTo` method compares two values and returns one of -1 (less), 0 (equal), or 1 (greater).

Previously, we could only check whether two denominators were equal, but by implementing `IComparable<T>`, size comparison and collection sorting become possible.

```csharp
// Previous approach (ordering comparison not possible)
var a = Denominator.Create(5);
var b = Denominator.Create(10);
// Comparisons like a < b were not possible

// Improved approach (IComparable<T> implementation)
public int CompareTo(Denominator? other)
{
    if (other is null) return 1;
    return _value.CompareTo(other._value);
}

// Now natural comparison is possible
Console.WriteLine($"a < b: {a < b}"); // True
Console.WriteLine($"a.CompareTo(b): {a.CompareTo(b)}"); // -1
```

Methods such as `List<T>.Sort()`, `Array.BinarySearch()`, `Min()`, and `Max()` automatically use `CompareTo`.

### Comparison Operator Overloading

By implementing `<`, `>`, `<=`, `>=` operators based on the `CompareTo` method, you can naturally use mathematical expressions like `a < b`.

```csharp
// CompareTo-based operator implementation
public static bool operator <(Denominator? left, Denominator? right) =>
    left is null ? right is not null : left.CompareTo(right) < 0;

public static bool operator >(Denominator? left, Denominator? right) =>
    left is not null && left.CompareTo(right) > 0;

// Natural comparison expression
if (denominator1 < denominator2)
{
    Console.WriteLine("The first denominator is smaller");
}
```

### The `IEqualityComparer<T>` Interface

`IEqualityComparer<T>` provides custom comparison strategies without modifying the value object's default `Equals` method. For example, when case-insensitive comparison is needed for `EmailAddress`, it can be separated into a dedicated comparer class.

```csharp
// Default equality comparison (case-sensitive)
var email1 = EmailAddress.Create("User@Example.com");
var email2 = EmailAddress.Create("user@example.com");
Console.WriteLine($"Default comparison: {email1 == email2}"); // False

// Custom comparer (case-insensitive)
public class EmailAddressCaseInsensitiveComparer : IEqualityComparer<EmailAddress>
{
    public bool Equals(EmailAddress? x, EmailAddress? y)
    {
        if (x is null && y is null) return true;
        if (x is null || y is null) return false;

        string xValue = (string)x;
        string yValue = (string)y;
        return xValue.Equals(yValue, StringComparison.OrdinalIgnoreCase);
    }
}

// Using the custom comparer in collections
var emails = new[] { email1, email2 };
var uniqueEmails = emails.Distinct(new EmailAddressCaseInsensitiveComparer());
```

Multiple comparison strategies can be provided simultaneously for a single value object, enabling response to diverse business requirements.

## Practical Guidelines

### Expected Output
```
=== Value Object Comparability ===

=== Basic Comparison Test ===
a = 5, b = 10, c = 5

CompareTo test:
a.CompareTo(b) = -1
b.CompareTo(a) = 1
a.CompareTo(c) = 0

Operator test:
a < b: True
a <= b: True
a > b: False
a >= b: False
a == c: True
a != b: True

=== Null Comparison Test ===
a = 5, nullValue = null

Comparison with null:
a.CompareTo(null) = 1
a > null: True
a >= null: True
a < null: False
a <= null: False
a == null: False
a != null: True

Null-to-null comparison:
null == null: True
null != null: False

=== Sorting Test ===
Before sorting:
10 3 7 1 15
After ascending sort:
1 3 7 10 15
After descending sort:
15 10 7 3 1

=== Collection Comparison Test ===
Original list:
5 2 8 1 3
Minimum value: 1
Maximum value: 8
Range: 7

=== Performance Comparison Test ===
Sort time for 10,000 Denominators: 1ms
Binary search time: 0ms
Found index: 4999

=== Boundary Value Test ===
Minimum value: -2147483648
Maximum value: 2147483647
Negative value: -100
Positive value: 100

Negative vs positive comparison:
Negative < Positive: True
Negative > Positive: False

Minimum vs maximum comparison:
Minimum < Maximum: True
Minimum > Maximum: False


==================================================

=== IEqualityComparer<T> Usage Example Test ===

=== Basic IEqualityComparer<T> Test ===
email1 = user@example.com
email2 = user@example.com
email3 = admin@example.com

Default comparison test:
comparer.Equals(email1, email2) = True
comparer.Equals(email1, email3) = False
comparer.Equals(email1, null) = False
comparer.Equals(null, null) = True

Hash code test:
email1.GetHashCode() = 650831702
email2.GetHashCode() = 650831702
email3.GetHashCode() = -1837482715
Same value has same hash code? True

=== IEqualityComparer<T> in Collections Test ===
Original email list:
user1@example.com user2@example.com user1@example.com admin@example.com user2@example.com test@example.com
After default Distinct (duplicates removed):
user1@example.com user2@example.com admin@example.com test@example.com
After HashSet (duplicates removed):
user1@example.com user2@example.com admin@example.com test@example.com
After custom EqualityComparer:
user1@example.com user2@example.com admin@example.com test@example.com

=== Case-Insensitive Comparer Test ===
Original email list (mixed case):
user@example.com user@example.com admin@example.com admin@example.com test@example.com test@example.com
After case-sensitive comparer:
user@example.com admin@example.com test@example.com
After case-insensitive comparer:
user@example.com admin@example.com test@example.com

=== IEqualityComparer<T> in Dictionary Test ===
Default Dictionary result:
  user1@example.com -> User One
  user2@example.com -> User Two
  admin@example.com -> Admin
Custom EqualityComparer Dictionary result:
  user1@example.com -> User One
  user2@example.com -> User Two
  admin@example.com -> Admin

=== Performance Comparison Test ===
Default Distinct performance: 0ms
Custom EqualityComparer performance: 0ms
HashSet performance: 0ms
Result count: 10000 (default), 10000 (custom), 10000 (HashSet)
After case-insensitive comparer:
user@example.com admin@example.com test@example.com
```

### Key Implementation Points
1. **`IComparable<T>` implementation**: Clearly implementing null handling and value comparison logic in the `CompareTo` method
2. **Comparison operator overloading**: Consistently implementing `<`, `>`, `<=`, `>=` operators based on the `CompareTo` method
3. **`IEqualityComparer<T>` strategy pattern**: Separating comparison strategies different from the default equality into separate classes for flexibility

## Project Description

### Project Structure
```
ValueComparability/                             # Main project
├── Program.cs                                  # Main entry file
├── ValueObjects/                               # Value object implementation
│   ├── Denominator.cs                          # IComparable<T> implementation example
│   └── EmailAddress.cs                         # IEquatable<T> only implementation example
├── Comparers/                                  # Custom comparer implementation
│   ├── EmailAddressComparer.cs                 # Default comparer
│   └── EmailAddressCaseInsensitiveComparer.cs  # Case-insensitive comparer
├── Tests/                                      # Test code
│   ├── ComparabilityTests.cs                   # IComparable<T> tests
│   └── EqualityComparerTests.cs                # IEqualityComparer<T> tests
├── ValueComparability.csproj                   # Project file
└── README.md                                   # Main documentation
```

### Core Code

#### Denominator - `IComparable<T>` Implementation
```csharp
public sealed class Denominator : IEquatable<Denominator>, IComparable<Denominator>
{
    private readonly int _value;

    // IComparable<T> implementation - ordering comparison
    public int CompareTo(Denominator? other)
    {
        if (other is null) return 1;  // All values are greater than null
        return _value.CompareTo(other._value);
    }

    // Comparison operator overloading
    public static bool operator <(Denominator? left, Denominator? right) =>
        left is null ? right is not null : left.CompareTo(right) < 0;

    public static bool operator >(Denominator? left, Denominator? right) =>
        left is not null && left.CompareTo(right) > 0;

    public static bool operator <=(Denominator? left, Denominator? right) =>
        left is null || left.CompareTo(right) <= 0;

    public static bool operator >=(Denominator? left, Denominator? right) =>
        left is null ? right is null : left.CompareTo(right) >= 0;
}
```

#### EmailAddressCaseInsensitiveComparer - Custom Comparison Strategy
```csharp
public class EmailAddressCaseInsensitiveComparer : IEqualityComparer<EmailAddress>
{
    public bool Equals(EmailAddress? x, EmailAddress? y)
    {
        if (x is null && y is null) return true;
        if (x is null || y is null) return false;

        // String comparison through explicit casting
        string xValue = (string)x;
        string yValue = (string)y;
        return xValue.Equals(yValue, StringComparison.OrdinalIgnoreCase);
    }

    public int GetHashCode(EmailAddress obj)
    {
        if (obj is null) return 0;
        string value = (string)obj;
        return value.ToLowerInvariant().GetHashCode();
    }
}
```

#### Test Code Using LINQ Expressions
```csharp
// Safely create and compare multiple value objects
var result = from a in Denominator.Create(5)
             from b in Denominator.Create(10)
             from c in Denominator.Create(5)
             select (a, b, c);

result.Match(
    Succ: values =>
    {
        var (a, b, c) = values;
        Console.WriteLine($"a < b: {a < b}");   // True
        Console.WriteLine($"a == c: {a == c}"); // True
    },
    Fail: error => Console.WriteLine($"Creation failed: {error.Message}")
);
```

## Summary at a Glance

The following table compares the purpose and usage scenarios of the two comparison interfaces.

| Aspect | `IComparable<T>` | `IEqualityComparer<T>` |
|------|----------------|---------------------|
| **Purpose** | Ordering comparison (sorting, searching) | Custom equality comparison |
| **Implementation location** | Inside the value object | Separate comparer class |
| **Key methods** | `CompareTo(T other)` | `Equals(T x, T y)`, `GetHashCode(T obj)` |
| **Return type** | `int` (-1, 0, 1) | `bool` (true, false) |
| **Usage scenarios** | Sorting, Min/Max, BinarySearch | Distinct, HashSet, Dictionary |
| **Flexibility** | Fixed comparison logic | Dynamic comparison strategy replacement |

Not every value object needs both interfaces. Implement only comparisons that are meaningful in the domain.

| Value Object | `IEquatable<T>` | `IComparable<T>` | `IEqualityComparer<T>` |
|---------|---------------|----------------|---------------------|
| **Denominator** | Yes (default equality) | Yes (numeric comparison) | -- (not needed) |
| **EmailAddress** | Yes (default equality) | -- (not meaningful) | Yes (case-insensitive) |

## FAQ

### Q1: What is the difference between `IComparable<T>` and `IEquatable<T>`?
**A**: `IEquatable<T>` provides equality comparison (`bool` return) that only determines whether two objects are equal or not, while `IComparable<T>` provides ordering comparison (`int` return, -1/0/1) that determines the size relationship.

```csharp
var a = Denominator.Create(5);
var b = Denominator.Create(10);

// IEquatable<T> - equality comparison
Console.WriteLine($"a == b: {a == b}"); // False

// IComparable<T> - ordering comparison
Console.WriteLine($"a < b: {a < b}"); // True
Console.WriteLine($"a.CompareTo(b): {a.CompareTo(b)}"); // -1
```

### Q2: Why doesn't EmailAddress implement `IComparable<T>`?
**A**: Because there is no meaningful ordering for email addresses. `Denominator` has numeric magnitude that carries business meaning (5 < 10), but the alphabetical order of email address strings has no meaning in business logic. You should only implement comparisons that are actually needed in the domain.

```csharp
// Denominator - ordering is meaningful
var small = Denominator.Create(5);
var large = Denominator.Create(10);
Console.WriteLine($"small < large: {small < large}"); // True

// EmailAddress - ordering is not meaningful
var email1 = EmailAddress.Create("admin@company.com");
var email2 = EmailAddress.Create("user@company.com");
// Comparisons like email1 < email2 have no business meaning
```

### Q3: Why use `IEqualityComparer<T>`?
**A**: To apply different comparison logic depending on the situation without modifying the value object's default `Equals` method. For example, the default comparison for email addresses is case-sensitive, but when removing duplicates, you may need to ignore case.

```csharp
var emails = new[] {
    EmailAddress.Create("User@Example.com"),
    EmailAddress.Create("user@example.com"),
    EmailAddress.Create("ADMIN@EXAMPLE.COM")
};

// Default comparison (case-sensitive)
var distinct1 = emails.Distinct().ToList(); // 3 items (all different)

// Custom comparison (case-insensitive)
var comparer = new EmailAddressCaseInsensitiveComparer();
var distinct2 = emails.Distinct(comparer).ToList(); // 2 items (duplicates removed)
```

With both equality and comparability in place, the value object is now complete. In the next chapter, we cover how to separate the creation (Create) and validation (Validate) responsibilities of value objects to apply the single responsibility principle.

---

→ [Chapter 9: Create/Validate Separation](../09-Create-Validate-Separation/)
