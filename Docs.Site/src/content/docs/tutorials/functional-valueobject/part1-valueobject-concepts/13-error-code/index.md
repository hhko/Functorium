---
title: "Structured Error Codes"
---

## Overview

Can you determine from the error message `Error.New("Invalid denominator value: 0")` alone which domain, what reason, and which value caused the problem? By managing structured error codes in the format `"Domain.ClassName.Reason"` together with the value at the time of failure, debugging and monitoring efficiency are greatly improved.

## Learning Objectives

Upon completing this chapter, you will be able to:

1. Design a **structured error code system** in the format `Domain.ClassName.Reason`
2. Build a **type-safe error handling system** that manages the failed value alongside the error code
3. Design an **error handling framework** that is fully compatible with LanguageExt's `Error` type

## Why Is This Needed?

In the previous step `11-ValueObject-Framework`, we systematized value object creation and validation through the framework. However, when errors occurred in production environments, there were three problems. The existing `Error.New` approach provided only simple string messages, making it difficult to systematically identify the source of errors. Failed value information was hardcoded into messages, making dynamic analysis impossible. And each value object used different error message formats, lacking consistency.

**A structured error code system** systematically separates and manages the domain information, failure reason, and failed value at the time of error occurrence.

## Core Concepts

### Structured Error Code System

Errors are classified using hierarchical codes in the format `"Domain.ClassName.Reason"`. The domain area, specific class, and failure reason are explicitly stated in the code, enabling immediate identification of the source and nature of the error.

Comparing error creation between the existing approach and the structured approach.

```csharp
// Previous approach (unstructured) - difficult to debug and monitor
var error = Error.New("Invalid denominator value: 0");

// Improved approach (structured) - systematic error management
var error = ErrorFactory.Create(
    errorCode: $"{nameof(DomainErrors)}.{nameof(Denominator)}.{nameof(Zero)}",
    errorCurrentValue: 0,
    errorMessage: $"Denominator cannot be zero. Current value: '0'");
```

### Type-Safe Error Information Management

Through generic overloads such as `Create<T>` and `Create<T1, T2>`, type information of the failed value is preserved. Type safety is guaranteed at compile time, and accurate value information can be utilized at runtime.

```csharp
// Managing various types of error information in a type-safe manner
var stringError = ErrorFactory.Create(
    errorCode: $"{nameof(DomainErrors)}.{nameof(Name)}.{nameof(TooShort)}",
    errorCurrentValue: "i@name",
    errorMessage: $"Name is too short. Current value: 'i@name'");
var intError = ErrorFactory.Create(
    errorCode: $"{nameof(DomainErrors)}.{nameof(Age)}.{nameof(Invalid)}",
    errorCurrentValue: 150,
    errorMessage: $"Age is out of range. Current value: '150'");
var multiValueError = ErrorFactory.Create(
    errorCode: $"{nameof(DomainErrors)}.{nameof(Coordinate)}.{nameof(OutOfRange)}",
    errorCurrentValue1: 1500,
    errorCurrentValue2: 2000,
    errorMessage: $"Coordinate is out of range. Current values: '1500', '2000'");
```

### Internal DomainErrors Class Pattern

Error definitions related to a value object are placed in the same file to achieve high cohesion. When creating a new value object, error definitions are written together, improving development productivity.

```csharp
public sealed class Denominator : SimpleValueObject<int>
{
    // ... existing code ...

    internal static class DomainErrors
    {
        public static Error Zero(int value) =>
            ErrorFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Denominator)}.{nameof(Zero)}",
                errorCurrentValue: value,
                errorMessage: $"Denominator cannot be zero. Current value: '{value}'");
    }
}
```

In the next chapter, we apply a Fluent API to this error code system to implement a more concise error definition approach.

## Practical Guidelines

### Expected Output
```
=== Systematic Error Handling Patterns ===

=== Comparable Tests ===

--- CompositeValueObjects Subfolder ---
  === CompositeValueObjects Error Tests ===

  --- Currency Error Tests ---
Empty currency code: ErrorCode: Domain.Currency.Empty, ErrorCurrentValue:
Non-3-character format: ErrorCode: Domain.Currency.NotThreeLetters, ErrorCurrentValue: AB
Unsupported currency: ErrorCode: Domain.Currency.Unsupported, ErrorCurrentValue: XYZ

  --- Price Error Tests ---
Negative price: ErrorCode: Domain.MoneyAmount.OutOfRange, ErrorCurrentValue: -100

  --- PriceRange Error Tests ---
Price range where min exceeds max: ErrorCode: Domain.PriceRange.MinExceedsMax, ErrorCurrentValue: MinPrice: KRW (Korean Won) ₩ 1,000.00, MaxPrice: KRW (Korean Won) ₩ 500.00

--- PrimitiveValueObjects Subfolder ---
  === PrimitiveValueObjects Error Tests ===

  --- Denominator Error Tests ---
Zero value: ErrorCode: Domain.Denominator.Zero, ErrorCurrentValue: 0

--- CompositePrimitiveValueObjects Subfolder ---
  === CompositePrimitiveValueObjects Error Tests ===

  --- DateRange Error Tests ---
Date range where start is after end: ErrorCode: Domain.DateRange.StartAfterEnd, ErrorCurrentValue: StartDate: 2024-12-31 12:00:00 AM, EndDate: 2024-01-01 12:00:00 AM

=== ComparableNot Folder Tests ===

--- CompositeValueObjects Subfolder ---
  === CompositeValueObjects Error Tests ===

  --- Address Error Tests ---
Empty street name: ErrorCode: Domain.Street.Empty, ErrorCurrentValue:
Empty city name: ErrorCode: Domain.City.Empty, ErrorCurrentValue:
Invalid postal code: ErrorCode: Domain.PostalCode.NotFiveDigits, ErrorCurrentValue: 1234

  --- Street Error Tests ---
Empty street name: ErrorCode: Domain.Street.Empty, ErrorCurrentValue:

  --- City Error Tests ---
Empty city name: ErrorCode: Domain.City.Empty, ErrorCurrentValue:

  --- PostalCode Error Tests ---
Empty postal code: ErrorCode: Domain.PostalCode.Empty, ErrorCurrentValue:
Non-5-digit format: ErrorCode: Domain.PostalCode.NotFiveDigits, ErrorCurrentValue: 1234

--- PrimitiveValueObjects Subfolder ---
  === PrimitiveValueObjects Error Tests ===

  --- BinaryData Error Tests ---
Null binary data: ErrorCode: Domain.BinaryData.Empty, ErrorCurrentValue: null
Empty binary data: ErrorCode: Domain.BinaryData.Empty, ErrorCurrentValue: 0

--- CompositePrimitiveValueObjects Subfolder ---
  === CompositePrimitiveValueObjects Error Tests ===

  --- Coordinate Error Tests ---
Out-of-range X coordinate: ErrorCode: Domain.Coordinate.XOutOfRange, ErrorCurrentValue: -1
Out-of-range Y coordinate: ErrorCode: Domain.Coordinate.YOutOfRange, ErrorCurrentValue: 1001
```

### Key Implementation Points
1. **ErrorFactory generic overloads**: Type-safe management of various types of error information through `Create<T>` and `Create<T1, T2>` methods
2. **Internal DomainErrors class pattern**: Defining `internal static class DomainErrors` inside the value object for highly cohesive error management
3. **Specific error reason naming**: Naming conventions that exactly match validation conditions, such as `Empty`, `NotThreeLetters`, `NotFiveDigits`, `MinExceedsMax`
4. **LanguageExt compatibility**: Inheriting from the existing `Error` type to ensure full compatibility with the ecosystem

## Project Description

### Project Structure
```
ErrorCode/                                  # Main project
├── Program.cs                              # Main entry file (tests matching ValueObjects folder structure)
├── ErrorCode.csproj                        # Project file
├── Framework/                              # Error handling framework
│   ├── Abstractions/
│   │   └── Errors/
│   │       ├── ErrorFactory.cs         # Error creation factory
│   │       ├── ExpectedError.cs        # Structured error types
│   │       └── ExceptionalError.cs     # Exception-based errors
│   └── Layers/
│       └── Domains/
│           ├── ValueObject.cs              # Base value object class
│           ├── SimpleValueObject.cs        # Simple value object class
│           └── AbstractValueObject.cs      # Abstract value object class
└── ValueObjects/                           # Value object implementation (classified by folder structure)
    ├── Comparable/                         # Comparable value objects
    │   ├── CompositeValueObjects/
    │   │   ├── Currency.cs                 # Currency value object (SmartEnum-based)
    │   │   ├── MoneyAmount.cs              # Money amount value object (ComparableSimpleValueObject<decimal>)
    │   │   ├── Price.cs                    # Price value object (MoneyAmount + Currency combination)
    │   │   └── PriceRange.cs               # Price range value object (Price combination)
    │   ├── PrimitiveValueObjects/
    │   │   └── Denominator.cs              # Denominator value object
    │   └── CompositePrimitiveValueObjects/
    │       └── DateRange.cs                # Date range value object
    └── ComparableNot/                      # Non-comparable value objects
        ├── CompositeValueObjects/
        │   ├── Address.cs                  # Address value object
        │   ├── Street.cs                   # Street name value object
        │   ├── City.cs                     # City name value object
        │   └── PostalCode.cs               # Postal code value object
        ├── PrimitiveValueObjects/
        │   └── BinaryData.cs               # Binary data value object
        └── CompositePrimitiveValueObjects/
            └── Coordinate.cs               # Coordinate value object
```

### Core Code

#### ErrorFactory -- Error Creation Factory
```csharp
public static class ErrorFactory
{
    // Basic error creation
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error Create(string errorCode, string errorCurrentValue, string errorMessage) =>
        new ExpectedError(errorCode, errorCurrentValue, errorMessage);

    // Generic single-value error creation
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error Create<T>(string errorCode, T errorCurrentValue, string errorMessage) where T : notnull =>
        new ExpectedError<T>(errorCode, errorCurrentValue, errorMessage);

    // Generic multi-value error creation
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error Create<T1, T2>(string errorCode, T1 errorCurrentValue1, T2 errorCurrentValue2, string errorMessage)
        where T1 : notnull where T2 : notnull =>
        new ExpectedError<T1, T2>(errorCode, errorCurrentValue1, errorCurrentValue2, errorMessage);

    // Exception-based error creation
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error CreateFromException(string errorCode, Exception exception) =>
        new ExceptionalError(errorCode, exception);

    // Error code formatting
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Format(params string[] parts) =>
        string.Join('.', parts);
}
```

#### Denominator -- Internal DomainErrors Pattern Applied
```csharp
public sealed class Denominator : SimpleValueObject<int>, IComparable<Denominator>
{
    // ... existing implementation ...

    public static Validation<Error, int> Validate(int value)
    {
        if (value == 0)
            return DomainErrors.Zero(value);

        return value;
    }

    // Internal DomainErrors class - highly cohesive error definitions
    internal static class DomainErrors
    {
        public static Error Zero(int value) =>
            ErrorFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Denominator)}.{nameof(Zero)}",
                errorCurrentValue: value,
                errorMessage: $"Denominator cannot be zero. Current value: '{value}'");
    }
}
```

#### Currency -- SmartEnum-Based Error Definitions

The same internal DomainErrors pattern is applied in SmartEnum as well.

```csharp
public sealed class Currency : SmartEnum<Currency, string>, IValueObject
{
    public static readonly Currency KRW = new(nameof(KRW), "KRW", "Korean Won", "₩");
    public static readonly Currency USD = new(nameof(USD), "USD", "US Dollar", "$");
    // ... other currencies ...

    public static Validation<Error, string> Validate(string currencyCode) =>
        ValidateNotEmpty(currencyCode)
            .Bind(ValidateFormat)
            .Bind(ValidateSupported);

    private static Validation<Error, string> ValidateNotEmpty(string currencyCode) =>
        string.IsNullOrWhiteSpace(currencyCode)
            ? DomainErrors.Empty(currencyCode)
            : currencyCode;

    private static Validation<Error, string> ValidateFormat(string currencyCode) =>
        currencyCode.Length != 3 || !currencyCode.All(char.IsLetter)
            ? DomainErrors.NotThreeLetters(currencyCode)
            : currencyCode.ToUpperInvariant();

    // Internal DomainErrors class - SmartEnum-specific error definitions
    internal static class DomainErrors
    {
        public static Error Empty(string value) =>
            ErrorFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Currency)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: $"Currency code cannot be empty. Current value: '{value}'");

        public static Error NotThreeLetters(string value) =>
            ErrorFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Currency)}.{nameof(NotThreeLetters)}",
                errorCurrentValue: value,
                errorMessage: $"Currency code must be exactly 3 letters. Current value: '{value}'");

        public static Error Unsupported(string value) =>
            ErrorFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Currency)}.{nameof(Unsupported)}",
                errorCurrentValue: value,
                errorMessage: $"Currency code is not supported. Current value: '{value}'");
    }
}
```

#### PriceRange -- Multi-Value Error Definitions

A pattern for defining multi-value errors in composite value objects.

```csharp
public sealed class PriceRange : ComparableValueObject
{
    public Price MinPrice { get; }
    public Price MaxPrice { get; }

    public static Fin<PriceRange> Create(decimal minPriceValue, decimal maxPriceValue, string currencyCode) =>
        CreateFromValidation(
            Validate(minPriceValue, maxPriceValue, currencyCode),
            validValues => new PriceRange(validValues.MinPrice, validValues.MaxPrice));

    public static Validation<Error, (Price MinPrice, Price MaxPrice)> Validate(
        decimal minPriceValue, decimal maxPriceValue, string currencyCode) =>
        from validMinPriceTuple in Price.Validate(minPriceValue, currencyCode)
        from validMaxPriceTuple in Price.Validate(maxPriceValue, currencyCode)
        from validPriceRange in ValidatePriceRange(
            Price.CreateFromValidated(validMinPriceTuple),
            Price.CreateFromValidated(validMaxPriceTuple))
        select validPriceRange;

    private static Validation<Error, (Price MinPrice, Price MaxPrice)> ValidatePriceRange(Price minPrice, Price maxPrice) =>
        (decimal)minPrice.Amount > (decimal)maxPrice.Amount
            ? DomainErrors.MinExceedsMax(minPrice, maxPrice)
            : (MinPrice: minPrice, MaxPrice: maxPrice);

    // Internal DomainErrors class - price range validation errors
    internal static class DomainErrors
    {
        public static Error MinExceedsMax(Price minPrice, Price maxPrice) =>
            ErrorFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(PriceRange)}.{nameof(MinExceedsMax)}",
                errorCurrentValue: $"MinPrice: {minPrice}, MaxPrice: {maxPrice}",
                errorMessage: $"Minimum price cannot exceed maximum price. Min: '{minPrice}', Max: '{maxPrice}'");
    }
}
```

## Summary at a Glance

### Comparison Table

The following table summarizes the differences between the existing Error.New approach and the ErrorFactory approach.

| Aspect | Previous Approach (Error.New) | Current Approach (ErrorFactory) |
|------|----------------------|------------------------------|
| **Error code structure** | Simple string messages | `Domain.ClassName.Reason` format |
| **Value information management** | Hardcoded in message | Type-safe separate fields |
| **Debugging support** | Requires message parsing | Structured information immediately available |
| **Monitoring support** | Lacks consistency | Aggregatable in standardized format |
| **Type safety** | None | Guaranteed through generics |

### Pros and Cons

Trade-offs of the structured error code system.

| Pros | Cons |
|------|------|
| **Structured error management** | **Initial setup complexity** |
| **Type-safe error information** | **Increased code volume** |
| **Full LanguageExt compatibility** | **Learning curve** |
| **Improved debugging and monitoring** | **Framework dependency** |

### Error Reason Naming Conventions

Error method names must exactly match the validation condition. Just by looking at the error code, it should be immediately clear what went wrong.

| Error Situation | Method Name | Applied Class |
|-----------|-------------|-------------|
| **Empty value** | `Empty` | `Currency`, `PostalCode`, `Street`, `City` |
| **Not 3 alphabetic characters** | `NotThreeLetters` | `Currency` |
| **Not 5 digits** | `NotFiveDigits` | `PostalCode` |
| **Coordinate out of range** | `XOutOfRange`, `YOutOfRange` | `Coordinate` |
| **Amount out of range** | `OutOfRange` | `MoneyAmount` |
| **Zero value** | `Zero` | `Denominator` |
| **Not supported** | `Unsupported` | `Currency` |
| **Min > Max** | `MinExceedsMax` | `PriceRange` |
| **Start >= End** | `StartAfterEnd` | `DateRange` |

## FAQ

### Q1: What are the advantages over the existing Error.New approach?
**A**: Through structured error codes (`Domain.Denominator.Zero`), the source and reason of an error can be immediately identified, and with type-safe value fields, domain-specific aggregation is possible in monitoring systems. The previous approach required parsing message strings.

### Q2: Why use an internal DomainErrors class?
**A**: Placing the value object and error definitions in the same file increases cohesion. When modifying a value object, related errors can be checked together, and when creating a new value object, error definitions are naturally written alongside it.

### Q3: How is compatibility with LanguageExt guaranteed?
**A**: `ExpectedError`, `ExpectedError<T>`, etc. are all implemented by inheriting from LanguageExt's `Error` class. They are fully compatible with functional operators such as `Match`, `Map`, and `Bind`, so the new error handling system can be introduced without modifying existing code.

---

The error code structure is in place, but calling `ErrorFactory.Create` directly each time makes the code verbose. In the next chapter, we introduce `DomainError` helpers and `DomainErrorKind` to make error creation concise.

→ [Chapter 14: DomainError Helper](../14-Error-Code-Fluent/)
