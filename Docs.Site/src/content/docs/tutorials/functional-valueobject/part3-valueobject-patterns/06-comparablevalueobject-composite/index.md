---
title: "Comparable Composite Value Object"
---

> `ComparableValueObject`

## Overview

When you need to sort an address list by city or compare the order of two addresses, `ValueObject` alone is not sufficient. `ComparableValueObject` automatically provides comparison and sorting functionality while combining multiple value objects, implementing the completed form of the value object pattern.

## Learning Objectives

1. Implement a comparable composite value object by inheriting from `ComparableValueObject`.
2. Override `GetComparableEqualityComponents()` to define a meaningful comparison order.
3. Naturally use composite value objects in LINQ's `OrderBy()`, `Where()`, etc.
4. Verify that comparison operators (`<`, `<=`, `>`, `>=`) are automatically supported.

## Why Is This Needed?

In the previous step, `05-ValueObject-Composite`, we could express complex domain concepts by combining multiple value objects. However, sorting or comparing these composite value objects required manually implementing comparison logic.

When sorting addresses or composite data, what value serves as the basis was not clear, and composite value objects could not be used directly in LINQ methods like `OrderBy()`, `Min()`, `Max()`. Comparison operators were not supported either, so there was no intuitive way to compare the order of two addresses.

`ComparableValueObject` resolves this problem. It defines natural sorting criteria through the order of `IComparable` elements returned from `GetComparableEqualityComponents()`, and automatically provides `IComparable<T>` implementation and comparison operators.

## Core Concepts

### Complete Value Object Composition

`ComparableValueObject` provides complete comparison functionality while combining multiple individual value objects. An address is composed of street, city, and postal code, where each part is an independent value object but the overall address operates as a single comparable unit.

```csharp
// Complete value object composition
Address address = Address.Create("Gangnam-daero 123", "Seoul", "12345");
// Comparable
bool isEarlier = address1 < address2;
```

### Comparison Order Definition

The comparison order is explicitly defined through `GetComparableEqualityComponents()`. For addresses, it is natural to compare by city (the largest geographic unit) first, then by postal code within the same city, and then by street within the same postal code area.

```csharp
protected override IEnumerable<IComparable> GetComparableEqualityComponents()
{
    yield return (string)City;        // Compare city first
    yield return (string)PostalCode;  // Postal code second
    yield return (string)Street;      // Street last
}
```

With just this order definition, LINQ's `OrderBy(a => a)` naturally sorts by city, postal code, and street.

### Full LINQ Integration

`ComparableValueObject` implements `IComparable<T>`, so it can be used in all LINQ sorting/comparison operations without a separate comparison function.

```csharp
// Full LINQ integration
var sortedAddresses = addresses
    .OrderBy(a => a)      // Natural sorting
    .Where(a => a < someAddress)  // Natural comparison
    .ToList();
```

## Practical Guidelines

### Expected Output
```
=== 6. Comparable Composite Value Object - ComparableValueObject ===
Parent class: ComparableValueObject
Example: Address - Street + City + PostalCode composition

Features:
   Value object with complex validation logic
   Comparison functionality automatically provided
   Expresses more complex domain concepts by combining multiple value objects
   Street + City + PostalCode = Address

Success Cases:
   Address: Gangnam-daero 123, Seoul 12345
     - Street: Gangnam-daero 123
     - City: Seoul
     - PostalCode: 12345

   Address: Teheran-ro 456, Seoul 67890
     - Street: Teheran-ro 456
     - City: Seoul
     - PostalCode: 67890

   Address: Gangnam-daero 123, Seoul 12345
     - Street: Gangnam-daero 123
     - City: Seoul
     - PostalCode: 12345

Equality Comparison:
   Gangnam-daero 123, Seoul 12345 == Teheran-ro 456, Seoul 67890 = False
   Gangnam-daero 123, Seoul 12345 == Gangnam-daero 123, Seoul 12345 = True

Comparison Functionality (IComparable<T>):
   Gangnam-daero 123, Seoul 12345 < Teheran-ro 456, Seoul 67890 = True
   Gangnam-daero 123, Seoul 12345 <= Teheran-ro 456, Seoul 67890 = True
   Gangnam-daero 123, Seoul 12345 > Teheran-ro 456, Seoul 67890 = False
   Gangnam-daero 123, Seoul 67890 >= Teheran-ro 456, Seoul 67890 = False

Hash Code:
   Gangnam-daero 123, Seoul 12345.GetHashCode() = 304805004
   Gangnam-daero 123, Seoul 12345.GetHashCode() = 304805004
   Same value hash codes equal? True

Failure Cases:
   Address("", "Seoul", "12345"):
   Address("Gangnam-daero 123", "Seoul", "1234"):
   Address("Gangnam-daero 123", "", "12345"):

Sorting Demo:
   Sorted Address list:
     Gangnam-daero 123, Seoul 12345
     Myeongdong-gil 321, Seoul 23456
     Jongno 789, Seoul 34567
     Teheran-ro 456, Seoul 67890

Comparable composite value object characteristics:
   - Street, City, PostalCode are each independent comparable value objects
   - Address expresses a more complex domain concept by combining these three value objects
   - Each component has its own validation logic and comparison functionality
   - The overall Address provides equality comparison and sorting through the combination of components

Demo completed successfully!
```

### Key Implementation Points

Four key elements for implementing a comparable composite value object.

| Point | Description |
|--------|------|
| **Inherit ComparableValueObject** | Inherits complete comparison functionality |
| **Implement GetComparableEqualityComponents()** | Defines meaningful comparison order |
| **Full LINQ integration** | Natural usage in OrderBy, Where, etc. |
| **All comparison operators supported** | `<`, `<=`, `>`, `>=` automatically supported |

## Project Description

### Project Structure
```
06-ComparableValueObject-Composite/
├── Program.cs                              # Main entry point
├── ComparableValueObjectComposite.csproj  # Project file
├── ValueObjects/
│   ├── Address.cs                         # Comparable composite address value object
│   ├── City.cs                           # City value object
│   ├── PostalCode.cs                     # Postal code value object
│   └── Street.cs                         # Street value object
└── README.md                             # Project document
```

### Core Code

`Address` combines Street, City, and PostalCode, three value objects, and defines the comparison order as City -> PostalCode -> Street.

**Address.cs - comparable composite value object**
```csharp
public sealed class Address : ComparableValueObject
{
    public Street Street { get; }
    public City City { get; }
    public PostalCode PostalCode { get; }

    private Address(Street street, City city, PostalCode postalCode)
    {
        Street = street;
        City = city;
        PostalCode = postalCode;
    }

    // LINQ Expression composite validation
    public static Validation<Error, (Street, City, PostalCode)> Validate(
        string street, string city, string postalCode) =>
        from validStreet in Street.Validate(street)
        from validCity in City.Validate(city)
        from validPostalCode in PostalCode.Validate(postalCode)
        select (Street: Street.CreateFromValidated(validStreet),
                City: City.CreateFromValidated(validCity),
                PostalCode: PostalCode.CreateFromValidated(validPostalCode));

    // Comparison order definition
    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return (string)City;        // Compare city first (largest unit)
        yield return (string)PostalCode;  // Compare postal code second (area distinction)
        yield return (string)Street;      // Compare street last (detailed address)
    }
}
```

**Program.cs - complete value object demo**
```csharp
// Natural use of comparison operators
var a1 = address1.Match(Succ: x => x, Fail: _ => default!);
var a2 = address2.Match(Succ: x => x, Fail: _ => default!);
Console.WriteLine($"   {a1} < {a2} = {a1 < a2}");

// LINQ sorting
var sortedAddresses = addresses.OrderBy(a => a).ToArray();
```

## Summary at a Glance

Compares the functional differences between `ValueObject`-based and `ComparableValueObject`-based composite value objects.

### Comparison Table
| Aspect | ValueObject-Composite | ComparableValueObject-Composite |
|------|----------------------|-------------------------------|
| **Comparison functionality** | Not supported | Automatically supported |
| **LINQ sorting** | Manual implementation | Automatically supported |
| **operator overloading** | Not supported | Automatically supported |
| **`IComparable<T>`** | Not implemented | Automatically implemented |
| **Practicality** | Average | High (full integration) |

### Pros and Cons
| Pros | Cons |
|------|------|
| **Full LINQ integration** | Highest implementation complexity |
| **Natural comparison** | Explicit comparison order definition required |
| **Full .NET ecosystem integration** | All features need to be learned |
| **Maximum practicality** | Initial learning investment required |

## FAQ

### Q1: Why does Address compare in the order City -> PostalCode -> Street?
**A**: City is the largest geographic unit, making it the first comparison criterion. Within the same city, postal code distinguishes the area, and within the same area, street distinguishes the detailed location. Actual address books and map services also follow this order.

### Q2: What is the difference between GetComparableEqualityComponents() and GetEqualityComponents()?
**A**: `GetEqualityComponents()` is for equality comparison only, while `GetComparableEqualityComponents()` is for both equality and sort comparison. The latter must return elements of `IComparable` type, and the order of elements determines the sorting priority.

### Q3: Should ComparableValueObject be used for all composite value objects?
**A**: No. For composite data that does not need sorting or size comparison, `ValueObject` is sufficient. Unnecessary comparison functionality only increases code complexity, so choose `ComparableValueObject` only when sorting is actually needed.

We have now covered all value object patterns using framework base classes. The next chapter covers how to implement type-safe enumerations with embedded domain logic using SmartEnum.

---

-> [Chapter 7: TypeSafeEnum](../07-TypeSafeEnum/)
