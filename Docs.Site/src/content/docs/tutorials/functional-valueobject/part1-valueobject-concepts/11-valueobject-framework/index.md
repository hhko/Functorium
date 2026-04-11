---
title: "ValueObject Framework"
---

## Overview

If you have to repeatedly implement equality comparison, hash codes, and operator overloading every time you create a value object, a base class framework eliminates this boilerplate and lets you efficiently implement 6 types based on whether `IComparable<T>` is supported and the complexity of the value.

## Learning Objectives

Upon completing this chapter, you will be able to:

1. Explain the **selection criteria for the 6 framework types** based on `IComparable<T>` support and value complexity
2. Implement **framework-based value objects** using the 6 base classes
3. Use the framework to eliminate **duplicate code** for common functionality such as equality comparison, hash codes, and operator overloading

## Why Is This Needed?

In the previous step `ValidatedValueCreation`, we implemented value object creation through the three-method pattern (Create, CreateFromValidated, Validate). However, when implementing various types of value objects in real projects, common functionality (equality comparison, hash codes, operator overloading) had to be written from scratch each time, consistency suffered because each implementer used different approaches, and when changes to common functionality were needed, every value object had to be modified individually.

**A base class framework** manages this common functionality in one place, improving both development productivity and code quality simultaneously.

## Core Concepts

### Simple Value Objects: `SimpleValueObject<T>` / `ComparableSimpleValueObject<T>`

Value objects that wrap a single value choose their base class based on whether comparison is needed. `SimpleValueObject<T>` provides only equality comparison and hash codes, while `ComparableSimpleValueObject<T>` automatically provides `IComparable<T>` and comparison operators as well.

The difference in code volume between the previous approach and the framework approach is stark.

```csharp
// Previous approach (implementing all common functionality manually)
public sealed class Denominator : IEquatable<Denominator>, IComparable<Denominator>
{
    private readonly int _value;

    public Denominator(int value) => _value = value;

    public override bool Equals(object? obj) => /* complex equality comparison logic */
    public override int GetHashCode() => /* hash code generation logic */
    public static bool operator ==(Denominator? left, Denominator? right) => /* operator overloading */
    public int CompareTo(Denominator? other) => /* comparison logic */
    public static bool operator <(Denominator? left, Denominator? right) => /* comparison operator */
    // ... dozens of lines of boilerplate code
}

// Improved approach (using the framework)
public sealed class Denominator : ComparableSimpleValueObject<int>
{
    private Denominator(int value) : base(value) { }

    public static Fin<Denominator> Create(int value) =>
        CreateFromValidation(Validate(value), validValue => new Denominator(validValue));

    public static Validation<Error, int> Validate(int value) =>
        value == 0 ? Error.New("Zero is not allowed") : value;

    // All comparison functionality is automatically provided!
    // - IComparable<Denominator> implementation
    // - All comparison operator overloading (<, <=, >, >=)
    // - GetComparableEqualityComponents() automatic implementation
}
```

### Composite Value Objects: `ValueObject` / `ComparableValueObject`

Composite objects that combine multiple values define which components are used for equality/comparison by overriding `GetEqualityComponents()` or `GetComparableEqualityComponents()`. The framework automatically implements `Equals`, `GetHashCode`, `==`, `!=` operators, and `ComparableValueObject` additionally provides `IComparable<T>` and comparison operators.

Implementation patterns for a non-comparable composite value object (Coordinate) and a comparable composite value object (DateRange).

```csharp
// Non-comparable composite value object
public sealed class Coordinate : ValueObject
{
    public int X { get; }
    public int Y { get; }

    private Coordinate(int x, int y) { X = x; Y = y; }

    public static Fin<Coordinate> Create(int x, int y) =>
        CreateFromValidation(
            Validate(x, y),
            validValues => new Coordinate(validValues.X, validValues.Y));

    public static Validation<Error, (int X, int Y)> Validate(int x, int y) =>
        from validX in ValidateX(x)
        from validY in ValidateY(y)
        select (X: validX, Y: validY);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return X;
        yield return Y;
    }
}

// Comparable composite value object
public sealed class DateRange : ComparableValueObject
{
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    private DateRange(DateTime startDate, DateTime endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }

    public static Fin<DateRange> Create(DateTime startDate, DateTime endDate) =>
        CreateFromValidation(
            Validate(startDate, endDate),
            validValues => new DateRange(validValues.StartDate, validValues.EndDate));

    public static Validation<Error, (DateTime StartDate, DateTime EndDate)> Validate(DateTime startDate, DateTime endDate) =>
        startDate >= endDate
            ? Error.New("Start date must be before end date")
            : (StartDate: startDate, EndDate: endDate);

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return StartDate;
        yield return EndDate;
    }
}
```

### Framework Architecture

The framework is hierarchically abstracted based on `IComparable<T>` support and value complexity.

```csharp
// Hierarchical framework structure
AbstractValueObject (basic equality, hash code)
    |
ValueObject (Validation composition helper)
    |                       |
SimpleValueObject<T>    ComparableValueObject
                            |
                        ComparableSimpleValueObject<T> (full functionality)
```

In the next chapter, we implement type-safe enumerations using SmartEnum to overcome the limitations of existing C# enums.

## Practical Guidelines

### Expected Output
```
=== ValueObject Framework Demo ===

1. Non-comparable primitive value object - BinaryData (binary data)
   Concisely implemented based on SimpleValueObject<byte[]>

   Success: BinaryData[5 bytes: 48 65 6C 6C 6F]
   Failure: Binary data cannot be empty
   Failure: Binary data cannot be empty
   Equality: BinaryData[3 bytes: 01 02 03] == BinaryData[3 bytes: 01 02 03] = True
   Equality: BinaryData[3 bytes: 01 02 03] == BinaryData[3 bytes: 04 05 06] = False
   Comparison: Not provided (intentionally)

2. Comparable primitive value object - Denominator (non-zero integer)
   Concisely implemented based on ComparableSimpleValueObject<int>

   Success: 5 (value: 5)
   Failure: Zero is not allowed
   Comparison: 3 < 5 = True
   Comparison: 3 == 5 = False

3. Non-comparable composite primitive value object - Coordinate (X, Y coordinates)
   Based on ValueObject, combining 2 Validations

   Success: (100, 200) (X: 100, Y: 200)
   Failure: X coordinate must be in the range 0-1000
   Failure: Y coordinate must be in the range 0-1000
   Equality: (100, 200) == (100, 200) = True

4. Comparable composite primitive value object - DateRange (date range)
   Based on ComparableValueObject, combining 2 DateTime values

   Success: 2024-01-01 ~ 2024-12-31 (start: 2024-01-01, end: 2024-12-31)
   Failure: Start date must be before end date
   Failure: Start date must be before end date
   Comparison: 2024-01-01 ~ 2024-06-30 < 2024-07-01 ~ 2024-12-31 = True
   Comparison: 2024-01-01 ~ 2024-06-30 == 2024-01-01 ~ 2024-06-30 = True
   Comparison: 2024-01-01 ~ 2024-06-30 > 2024-07-01 ~ 2024-12-31 = False

5. Non-comparable composite value object - Address (Street, City, PostalCode)
   Based on ValueObject, combining 3 value objects

   Success: 123 Main St, Seoul 12345
   Failure: Street name cannot be empty
   Failure: Postal code must be 5 digits

   Individual value object creation:
   - Street: Broadway (value: Broadway)
   - City: New York (value: New York)
   - PostalCode: 10001 (value: 10001)
   - Address from validated: Broadway, New York 10001

6. Comparable composite value object - PriceRange (Price, Currency)
   Based on ComparableValueObject, combining Price and Currency value objects

   Success: KRW10,000 ~ KRW50,000 (min: 10,000, max: 50,000, currency: KRW)
   Failure: Price must be 0 or greater
   Failure: Price must be 0 or greater
   Failure: Minimum price must be less than or equal to maximum price
   Failure: Currency code must be 3 characters

   Comparison demo:
   - KRW10,000 ~ KRW30,000 < KRW20,000 ~ KRW40,000 = True
   - KRW10,000 ~ KRW30,000 == KRW10,000 ~ KRW30,000 = True
   - KRW10,000 ~ KRW30,000 > KRW20,000 ~ KRW40,000 = False

   Individual value object creation:
   - MinPrice: 15,000 (value: 15000)
   - MaxPrice: 35,000 (value: 35000)
   - Currency: USD (value: USD)
   - PriceRange from validated: USD15,000 ~ USD35,000
```

### Key Implementation Points
1. **Framework inheritance**: Selecting the appropriate base class (`SimpleValueObject<T>` vs `ValueObject`)
2. **Using CreateFromValidation**: Concise factory method implementation through the framework's helper methods
3. **Validation logic separation**: Clearly separating validation responsibility into the `Validate` method

## Project Description

### Project Structure
```
ValueObjectFramework/                       # Main project
├── Program.cs                              # 6 scenario demo
├── ValueObjects/                           # Value object implementation
│   ├── Comparable/                         # Comparable value objects
│   │   ├── PrimitiveValueObjects/          # Comparable primitive value objects
│   │   │   └── Denominator.cs              # Non-zero integer
│   │   ├── CompositePrimitiveValueObjects/ # Comparable composite primitive value objects
│   │   │   └── DateRange.cs                # Date range
│   │   └── CompositeValueObjects/          # Comparable composite value objects
│   │       ├── Price.cs                    # Price
│   │       ├── Currency.cs                 # Currency
│   │       └── PriceRange.cs               # Price range (Price + Currency combination)
│   └── ComparableNot/                      # Non-comparable value objects
│       ├── PrimitiveValueObjects/          # Non-comparable primitive value objects
│       │   └── BinaryData.cs               # Binary data
│       ├── CompositePrimitiveValueObjects/ # Non-comparable composite primitive value objects
│       │   └── Coordinate.cs               # X, Y coordinates
│       └── CompositeValueObjects/          # Non-comparable composite value objects
│           ├── Address.cs                  # Address (Street, City, PostalCode)
│           ├── Street.cs                   # Street name
│           ├── City.cs                     # City name
│           └── PostalCode.cs               # Postal code
├── ValueObjectFramework.csproj             # Project file
└── README.md                               # Main documentation
```

### Core Code

#### 1. BinaryData -- `SimpleValueObject<T>` Framework

A single value object where comparison is not needed. Since `byte[]` does not implement `IComparable`, `SimpleValueObject` is used.

```csharp
public sealed class BinaryData : SimpleValueObject<byte[]>
{
    private BinaryData(byte[] value) : base(value) { }

    public static Fin<BinaryData> Create(byte[] value) =>
        CreateFromValidation(
            Validate(value),
            validValue => new BinaryData(validValue));

    public static Validation<Error, byte[]> Validate(byte[] value) =>
        value == null || value.Length == 0
            ? Error.New("Binary data cannot be empty")
            : value;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        // Convert to string for content comparison of byte[] arrays
        yield return Convert.ToBase64String(Value);
    }

    public override string ToString() =>
        $"BinaryData[{Value.Length} bytes: {BitConverter.ToString(Value).Replace("-", " ")}]";
}
```

#### 2. Address -- ValueObject Framework

A composite value object combining multiple value objects (Street, City, PostalCode).

```csharp
public sealed class Address : ValueObject
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

    public static Fin<Address> Create(string streetValue, string cityValue, string postalCodeValue) =>
        CreateFromValidation(
            Validate(streetValue, cityValue, postalCodeValue),
            validValues => new Address(
                validValues.Street,
                validValues.City,
                validValues.PostalCode));

    public static Validation<Error, (Street Street, City City, PostalCode PostalCode)> Validate(
            string street, string city, string postalCode) =>
        from validStreet in Street.Validate(street)
        from validCity in City.Validate(city)
        from validPostalCode in PostalCode.Validate(postalCode)
        select (
            Street: Street.CreateFromValidated(validStreet),
            City: City.CreateFromValidated(validCity),
            PostalCode: PostalCode.CreateFromValidated(validPostalCode)
        );

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return PostalCode;
    }
}
```

#### 3. PriceRange -- ComparableValueObject Framework

A composite value object combining comparable value objects (Price, Currency).

```csharp
public sealed class PriceRange : ComparableValueObject
{
    public Price MinPrice { get; }
    public Price MaxPrice { get; }
    public Currency Currency { get; }

    private PriceRange(Price minPrice, Price maxPrice, Currency currency)
    {
        MinPrice = minPrice;
        MaxPrice = maxPrice;
        Currency = currency;
    }

    public static Fin<PriceRange> Create(decimal minPriceValue, decimal maxPriceValue, string currencyCode) =>
        CreateFromValidation(
            Validate(minPriceValue, maxPriceValue, currencyCode),
            validValues => new PriceRange(validValues.MinPrice, validValues.MaxPrice, validValues.Currency));

    public static Fin<PriceRange> CreateFromValidated(Price minPrice, Price maxPrice, Currency currency) =>
        CreateFromValidation(
            ValidatePriceRange(minPrice, maxPrice),
            validValues => new PriceRange(validValues.MinPrice, validValues.MaxPrice, currency));

    public static Validation<Error, (Price MinPrice, Price MaxPrice, Currency Currency)> Validate(
        decimal minPriceValue, decimal maxPriceValue, string currencyCode) =>
        from validMinPrice in Price.Validate(minPriceValue)
        from validMaxPrice in Price.Validate(maxPriceValue)
        from validCurrency in Currency.Validate(currencyCode)
        from validPriceRange in ValidatePriceRange(
            Price.CreateFromValidated(validMinPrice),
            Price.CreateFromValidated(validMaxPrice))
        select (
            MinPrice: validPriceRange.MinPrice,
            MaxPrice: validPriceRange.MaxPrice,
            Currency: Currency.CreateFromValidated(validCurrency)
        );

    private static Validation<Error, (Price MinPrice, Price MaxPrice)> ValidatePriceRange(Price minPrice, Price maxPrice) =>
        minPrice.Value > maxPrice.Value
            ? Error.New("Minimum price must be less than or equal to maximum price")
            : (MinPrice: minPrice, MaxPrice: maxPrice);

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return MinPrice;
        yield return MaxPrice;
        yield return Currency;
    }

    public override string ToString() =>
        $"{Currency}{MinPrice.Value:N0} ~ {Currency}{MaxPrice.Value:N0}";
}
```


## Summary at a Glance

### Previous Approach vs Framework Approach

| Aspect | Previous Approach | Framework Approach |
|------|-----------|-----------------|
| **Code volume** | 50-100 lines | 15-25 lines |
| **Boilerplate** | Manually implemented each time | Provided by the framework |
| **Comparison functionality** | Manual implementation required | Fully provided automatically |
| **Consistency** | Varies by implementer | Standardized through the framework |
| **Maintainability** | Individual modifications needed | Bulk application through framework modification |

### Type Selection Guide

Select the appropriate base class based on `IComparable<T>` support and value complexity.

| Type | Base Class | `IComparable<T>` | Example |
|------|---------------|----------------|------|
| **Single value, no comparison needed** | `SimpleValueObject<T>` | Not supported | `BinaryData` |
| **Single value, comparison needed** | `ComparableSimpleValueObject<T>` | Supported | `Denominator` |
| **Composite value, no comparison needed** | `ValueObject` | Not supported | `Coordinate`, `Address` |
| **Composite value, comparison needed** | `ComparableValueObject` | Supported | `DateRange`, `PriceRange` |

### Pros and Cons

| Pros | Cons |
|------|------|
| **90% reduction in code duplication** | **Framework learning required** |
| **Completely consistent implementation pattern** | **Framework dependency** |
| **Comparison functionality automation** | **Risk of over-abstraction** |
| **Improved maintainability** | **Type constraint requirements** |

## FAQ

### Q1: How do you choose the framework type?
**A**: It is determined by two criteria. (1) Is sorting/comparison needed? If so, choose a type with the `Comparable` prefix. (2) Is it a single value or a composite value? For single values, choose the `SimpleValueObject<T>` family; for composite values, choose the `ValueObject` family.

### Q2: What are the type constraints for `ComparableSimpleValueObject<T>`?
**A**: `T` must implement `IComparable`. .NET basic types such as `int`, `string`, and `DateTime` all satisfy this, so there are no issues in most cases. Types that do not need comparison (such as `byte[]`) should use `SimpleValueObject<T>`.

### Q3: How does the CreateFromValidation helper work?
**A**: It receives a `Validation<Error, TValue>`, applies a factory function on success to create the value object, and passes the Error through as-is on failure, returning `Fin<TValueObject>`.

```csharp
// Internal operation of the CreateFromValidation helper
public static Fin<TValueObject> CreateFromValidation<TValueObject, TValue>(
    Validation<Error, TValue> validation,
    Func<TValue, TValueObject> factory)
    where TValueObject : ValueObject
{
    return validation
        .Map(factory)        // Apply factory function on success
        .ToFin();           // Convert Validation to Fin
}
```

---

We eliminated boilerplate with framework types, but business domains also have cases where a fixed set of choices needs to be expressed. In the next chapter, we implement type-safe enumerations using SmartEnum.

→ [Chapter 12: Type-Safe Enumerations](../12-Type-Safe-Enums/)
