---
title: "DomainError Helper"
---

:::note[Naming convention note]
This tutorial walks through a **self-contained mini-framework** that uses the pre-1.0.0-alpha.4 names (e.g., `DomainErrors.X.Y`, `ErrorCodeFactory`, `DomainErrorType`). The Functorium production framework now uses shorter names — see the [Error System Spec](/spec/04-error-system/) for the full rename map. The patterns and design rationale shown here are unchanged; only the identifiers differ.
:::

## Overview

Wasn't it cumbersome to repeatedly define `DomainErrors` nested classes for each value object and manually assemble error codes with `ErrorCodeFactory.Create()`? In this chapter, we cover a pattern that replaces error creation with a single line `DomainError.For<T>()` to reduce code volume by approximately 60%, and guarantees type-safe error codes through `DomainErrorType` records.

## Learning Objectives

Upon completing this chapter, you will be able to:

1. Create errors concisely using the `DomainError.For<T>()` method
2. Prevent typos and inconsistencies at compile time using `DomainErrorType` records
3. Define errors inline without `DomainErrors` nested classes
4. Choose the appropriate type-specific overload (`For<T>()`, `For<T, TValue>()`, `For<T, T1, T2>()`) for the situation

## Why Is This Needed?

In the previous `13-Error-Code` project, we introduced a structured error code system, but boilerplate remained in defining `DomainErrors` nested classes for each value object. The same pattern of `internal static class DomainErrors` had to be repeatedly defined in every value object, and error codes had to be manually assembled in the format `$"{nameof(DomainErrors)}.{nameof(Denominator)}.{nameof(Zero)}"`. Moreover, developers could use different names for the same concept such as `"Empty"`, `"IsEmpty"`, `"EmptyValue"`, making it easy to break consistency.

**The DomainError helper and DomainErrorType records** resolve these problems with type-safe error types and automatic error code generation.

## Core Concepts

### DomainErrorType Record

`DomainErrorType` is an abstract record that defines standardized error types. Using types instead of strings guarantees compile-time safety. Standard errors use predefined types, and domain-specific errors are explicitly defined by inheriting from `DomainErrorType.Custom`.

```csharp
// Standard error types (type-safe)
new DomainErrorType.Empty()           // Empty value
new DomainErrorType.Null()            // Null value
new DomainErrorType.TooShort(8)       // Below minimum length
new DomainErrorType.TooLong(100)      // Exceeds maximum length
new DomainErrorType.WrongLength(5)    // Exact length mismatch
new DomainErrorType.InvalidFormat()   // Format error
new DomainErrorType.Negative()        // Negative value
new DomainErrorType.NotPositive()     // Non-positive value
new DomainErrorType.OutOfRange("0", "1000")  // Out of range
new DomainErrorType.BelowMinimum("0")        // Below minimum
new DomainErrorType.AboveMaximum("100")      // Above maximum
new DomainErrorType.NotFound()        // Not found
new DomainErrorType.AlreadyExists()   // Already exists
new DomainErrorType.NotUpperCase()    // Not uppercase
new DomainErrorType.NotLowerCase()    // Not lowercase
new DomainErrorType.Duplicate()       // Duplicate
new DomainErrorType.Mismatch()        // Mismatch

// Custom errors (for non-standard cases) - sealed record derived definition
// public sealed record Unsupported : DomainErrorType.Custom;
new Unsupported()    // Domain-specific error
```

### DomainError Helper

The DomainError helper combines `typeof(T).Name` and `DomainErrorType` to automatically generate error codes in the format `DomainErrors.{ValueObjectName}.{ErrorType}`.

```csharp
// DomainError helper usage

// 1. String value validation
DomainError.For<Currency>(new DomainErrorType.Empty(), currencyCode ?? "",
    $"Currency code cannot be empty. Current value: '{currencyCode}'")
// Generated error code: "DomainErrors.Currency.Empty"

// 2. Generic value validation (custom error: sealed record Zero : DomainErrorType.Custom;)
DomainError.For<Denominator, int>(new Zero(), value,
    $"Denominator cannot be zero. Current value: '{value}'")
// Generated error code: "DomainErrors.Denominator.Zero"

// 3. Range validation
DomainError.For<Coordinate, int>(new DomainErrorType.OutOfRange("0", "1000"), x,
    $"X coordinate must be between 0 and 1000. Current value: '{x}'")
// Generated error code: "DomainErrors.Coordinate.OutOfRange"
```

### Inline Error Definitions

Using the DomainError helper, errors can be created directly at the point of validation failure, making a separate `DomainErrors` nested class unnecessary. Since validation and error definition are located together, code cohesion is improved.

```csharp
// Inline error definition example (custom error: sealed record Zero : DomainErrorType.Custom;)
public static Validation<Error, int> Validate(int value) =>
    value == 0
        ? DomainError.For<Denominator, int>(new Zero(), value,
            $"Denominator cannot be zero. Current value: '{value}'")
        : value;
```

## Before/After Comparison

### Before (Previous Approach - 40+ lines)
```csharp
public sealed class Denominator : ComparableSimpleValueObject<int>
{
    private Denominator(int value) : base(value) { }

    public static Fin<Denominator> Create(int value) =>
        CreateFromValidation(Validate(value), validValue => new Denominator(validValue));

    public static Denominator CreateFromValidated(int validatedValue) =>
        new Denominator(validatedValue);

    public static Validation<Error, int> Validate(int value)
    {
        if (value == 0)
            return DomainErrors.Zero(value);
        return value;
    }

    // DomainErrors nested class - repeated in every value object
    internal static class DomainErrors
    {
        public static Error Zero(int value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Denominator)}.{nameof(Zero)}",
                errorCurrentValue: value,
                errorMessage: $"Denominator cannot be zero. Current value: '{value}'");
    }
}
```

### After (DomainError + DomainErrorType - 15 lines)
```csharp
public sealed class Denominator : ComparableSimpleValueObject<int>
{
    private Denominator(int value) : base(value) { }

    public static Fin<Denominator> Create(int value) =>
        CreateFromValidation(Validate(value), validValue => new Denominator(validValue));

    public static Denominator CreateFromValidated(int validatedValue) =>
        new Denominator(validatedValue);

    // Custom error type definition
    public sealed record Zero : DomainErrorType.Custom;

    public static Validation<Error, int> Validate(int value) =>
        value == 0
            ? DomainError.For<Denominator, int>(new Zero(), value,
                $"Denominator cannot be zero. Current value: '{value}'")
            : value;
}
```

**Code reduction: ~60%**

## Practical Guidelines

### Expected Output
```
=== Concise Error Handling Patterns Using DomainError Helper ===

=== Comparable Tests ===

--- CompositeValueObjects Subfolder ---
  === CompositeValueObjects Error Tests ===

  --- Currency Error Tests ---
Empty currency code: [DomainErrors.Currency.Empty] Currency code cannot be empty. Current value: ''
Non-3-character format: [DomainErrors.Currency.WrongLength] Currency code must be exactly 3 letters. Current value: 'AB'
Unsupported currency: [DomainErrors.Currency.Unsupported] Currency code is not supported. Current value: 'XYZ'

  --- Price Error Tests ---
Negative price: [DomainErrors.MoneyAmount.OutOfRange] Money amount must be between 0 and 999999.99. Current value: '-100'

  --- PriceRange Error Tests ---
Price range where min exceeds max: [DomainErrors.PriceRange.MinExceedsMax] Minimum price cannot exceed maximum price.

--- PrimitiveValueObjects Subfolder ---
  === PrimitiveValueObjects Error Tests ===

  --- Denominator Error Tests ---
Zero value: [DomainErrors.Denominator.Zero] Denominator cannot be zero. Current value: '0'

--- CompositePrimitiveValueObjects Subfolder ---
  === CompositePrimitiveValueObjects Error Tests ===

  --- DateRange Error Tests ---
Date range where start is after end: [DomainErrors.DateRange.StartAfterEnd] Start date cannot be after end date.

=== ComparableNot Folder Tests ===

--- CompositeValueObjects Subfolder ---
  === CompositeValueObjects Error Tests ===

  --- Address Error Tests ---
Empty street name: [DomainErrors.Street.Empty] Street name cannot be empty.
Empty city name: [DomainErrors.City.Empty] City name cannot be empty.
Invalid postal code: [DomainErrors.PostalCode.WrongLength] Postal code must be exactly 5 digits.

  --- Street Error Tests ---
Empty street name: [DomainErrors.Street.Empty] Street name cannot be empty.

  --- City Error Tests ---
Empty city name: [DomainErrors.City.Empty] City name cannot be empty.

  --- PostalCode Error Tests ---
Empty postal code: [DomainErrors.PostalCode.Empty] Postal code cannot be empty.
Non-5-digit format: [DomainErrors.PostalCode.WrongLength] Postal code must be exactly 5 digits.

--- PrimitiveValueObjects Subfolder ---
  === PrimitiveValueObjects Error Tests ===

  --- BinaryData Error Tests ---
Null binary data: [DomainErrors.BinaryData.Empty] Binary data cannot be empty.
Empty binary data: [DomainErrors.BinaryData.Empty] Binary data cannot be empty.

--- CompositePrimitiveValueObjects Subfolder ---
  === CompositePrimitiveValueObjects Error Tests ===

  --- Coordinate Error Tests ---
Out-of-range X coordinate: [DomainErrors.Coordinate.OutOfRange] X coordinate must be between 0 and 1000.
Out-of-range Y coordinate: [DomainErrors.Coordinate.OutOfRange] Y coordinate must be between 0 and 1000.
```

### Key Implementation Points

Standard errors use predefined types like `new DomainErrorType.Empty()`, and domain-specific errors that are difficult to express with standard types are defined as derived `sealed record` types. Since `DomainError.For<T>()` automatically generates error codes from type information, errors can be defined inline directly within validation logic. It is fully compatible with existing `Validation<Error, T>` and `Fin<T>` types.

## Project Description

### Project Structure
```
14-Error-Code-Fluent/
├── README.md                              # This document
├── ErrorCodeFluent/                       # Main project
│   ├── Program.cs                         # Main entry file
│   ├── ErrorCodeFluent.csproj             # Project file
│   └── ValueObjects/                      # Value object implementation
│       ├── 01-ComparableNot/              # Non-comparable value objects
│       │   ├── 01-PrimitiveValueObjects/
│       │   │   └── BinaryData.cs          # Binary data
│       │   ├── 02-CompositePrimitiveValueObjects/
│       │   │   └── Coordinate.cs          # Coordinates (x, y)
│       │   └── 03-CompositeValueObjects/
│       │       ├── Street.cs              # Street name
│       │       ├── City.cs                # City name
│       │       ├── PostalCode.cs          # Postal code
│       │       └── Address.cs             # Address (composite)
│       └── 02-Comparable/                 # Comparable value objects
│           ├── 01-PrimitiveValueObjects/
│           │   └── Denominator.cs         # Denominator (non-zero integer)
│           ├── 02-CompositePrimitiveValueObjects/
│           │   └── DateRange.cs           # Date range
│           └── 03-CompositeValueObjects/
│               ├── Currency.cs            # Currency (SmartEnum-based)
│               ├── MoneyAmount.cs         # Money amount
│               ├── Price.cs               # Price (amount + currency)
│               └── PriceRange.cs          # Price range
└── ErrorCodeFluent.Tests.Unit/            # Unit tests
    ├── Using.cs                           # Global using definitions
    ├── DenominatorTests.cs                # Denominator type-safe tests
    └── ErrorCodeFactoryTests.cs           # DomainError + Assertion comprehensive tests
```

### Core Code

#### DomainErrorType (Provided by Functorium Framework)
```csharp
/// <summary>
/// Abstract record defining domain error types
/// Used for type-safe error code generation
/// </summary>
public abstract record DomainErrorType
{
    // Value existence validation
    public sealed record Empty : DomainErrorType;
    public sealed record Null : DomainErrorType;

    // String length validation
    public sealed record TooShort(int Minimum = 0) : DomainErrorType;
    public sealed record TooLong(int Maximum = 0) : DomainErrorType;
    public sealed record WrongLength(int Expected = 0) : DomainErrorType;

    // Format validation
    public sealed record InvalidFormat : DomainErrorType;

    // Numeric range validation
    public sealed record Negative : DomainErrorType;
    public sealed record NotPositive : DomainErrorType;
    public sealed record OutOfRange(string? Minimum = null, string? Maximum = null) : DomainErrorType;
    public sealed record BelowMinimum(string? Minimum = null) : DomainErrorType;
    public sealed record AboveMaximum(string? Maximum = null) : DomainErrorType;

    // Existence validation
    public sealed record NotFound : DomainErrorType;
    public sealed record AlreadyExists : DomainErrorType;

    // Case validation
    public sealed record NotUpperCase : DomainErrorType;
    public sealed record NotLowerCase : DomainErrorType;

    // Business rule validation
    public sealed record Duplicate : DomainErrorType;
    public sealed record Mismatch : DomainErrorType;

    // Custom errors (domain-specific) - abstract record, derive to use
    public abstract record Custom : DomainErrorType;
}

// Custom error definition example (defined as nested record inside value object)
// public sealed record Unsupported : DomainErrorType.Custom;
// public sealed record Zero : DomainErrorType.Custom;
```

#### DomainError Helper (Provided by Functorium Framework)
```csharp
/// <summary>
/// Helper class for concise domain error creation
/// Automatically generates error codes from type information.
/// </summary>
public static class DomainError
{
    /// <summary>
    /// Create a domain error for a string value
    /// </summary>
    public static Error For<TValueObject>(
        DomainErrorType errorType, string currentValue, string message)
        where TValueObject : class =>
        ErrorCodeFactory.Create(
            errorCode: $"DomainErrors.{typeof(TValueObject).Name}.{GetErrorName(errorType)}",
            errorCurrentValue: currentValue,
            errorMessage: message);

    /// <summary>
    /// Create a domain error for a generic value
    /// </summary>
    public static Error For<TValueObject, TValue>(
        DomainErrorType errorType, TValue currentValue, string message)
        where TValueObject : class
        where TValue : notnull =>
        ErrorCodeFactory.Create(
            errorCode: $"DomainErrors.{typeof(TValueObject).Name}.{GetErrorName(errorType)}",
            errorCurrentValue: currentValue,
            errorMessage: message);

    private static string GetErrorName(DomainErrorType errorType) =>
        errorType.GetType().Name;
}
```

#### Denominator - Simplest Example
```csharp
public sealed class Denominator : ComparableSimpleValueObject<int>
{
    // Custom error type definition
    public sealed record Zero : DomainErrorType.Custom;

    private Denominator(int value) : base(value) { }

    public static Fin<Denominator> Create(int value) =>
        CreateFromValidation(Validate(value), validValue => new Denominator(validValue));

    public static Denominator CreateFromValidated(int validatedValue) =>
        new Denominator(validatedValue);

    public static Validation<Error, int> Validate(int value) =>
        value == 0
            ? DomainError.For<Denominator, int>(new Zero(), value,
                $"Denominator cannot be zero. Current value: '{value}'")
            : value;
}
```

#### Currency - SmartEnum-Based Value Object
```csharp
public sealed class Currency
    : SmartEnum<Currency, string>
    , IValueObject
{
    // Custom error type definition
    public sealed record Unsupported : DomainErrorType.Custom;

    public static readonly Currency KRW = new(nameof(KRW), "KRW", "Korean Won", "₩");
    public static readonly Currency USD = new(nameof(USD), "USD", "US Dollar", "$");
    // ... other currencies ...

    public static Fin<Currency> Create(string currencyCode) =>
        Validate(currencyCode)
            .Map(FromValue)
            .ToFin();

    public static Validation<Error, string> Validate(string currencyCode) =>
        ValidateNotEmpty(currencyCode)
            .Bind(ValidateFormat)
            .Bind(ValidateSupported);

    private static Validation<Error, string> ValidateNotEmpty(string currencyCode) =>
        string.IsNullOrWhiteSpace(currencyCode)
            ? DomainError.For<Currency>(new DomainErrorType.Empty(), currencyCode ?? "",
                $"Currency code cannot be empty. Current value: '{currencyCode}'")
            : currencyCode;

    private static Validation<Error, string> ValidateFormat(string currencyCode) =>
        currencyCode.Length != 3 || !currencyCode.All(char.IsLetter)
            ? DomainError.For<Currency>(new DomainErrorType.WrongLength(3), currencyCode,
                $"Currency code must be exactly 3 letters. Current value: '{currencyCode}'")
            : currencyCode.ToUpperInvariant();

    private static Validation<Error, string> ValidateSupported(string currencyCode)
    {
        try
        {
            FromValue(currencyCode);
            return currencyCode;
        }
        catch (SmartEnumNotFoundException)
        {
            return DomainError.For<Currency>(new Unsupported(), currencyCode,
                $"Currency code is not supported. Current value: '{currencyCode}'");
        }
    }
}
```

#### PostalCode - Multi-Step Validation
```csharp
public sealed class PostalCode : SimpleValueObject<string>
{
    private PostalCode(string value) : base(value) { }

    public static Fin<PostalCode> Create(string value) =>
        CreateFromValidation(Validate(value), validValue => new PostalCode(validValue));

    public static PostalCode CreateFromValidated(string validatedValue) =>
        new PostalCode(validatedValue);

    public static Validation<Error, string> Validate(string value) =>
        ValidateNotEmpty(value).Bind(ValidateFormat);

    private static Validation<Error, string> ValidateNotEmpty(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? DomainError.For<PostalCode>(new DomainErrorType.Empty(), value ?? "",
                $"Postal code cannot be empty. Current value: '{value}'")
            : value;

    private static Validation<Error, string> ValidateFormat(string value) =>
        value.Length != 5 || !value.All(char.IsDigit)
            ? DomainError.For<PostalCode>(new DomainErrorType.WrongLength(5), value,
                $"Postal code must be exactly 5 digits. Current value: '{value}'")
            : value;
}
```

#### Coordinate - Composite Primitive Value Object
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
        CreateFromValidation(Validate(x, y), validValues => new Coordinate(validValues.X, validValues.Y));

    public static Validation<Error, (int X, int Y)> Validate(int x, int y) =>
        from validX in ValidateX(x)
        from validY in ValidateY(y)
        select (X: validX, Y: validY);

    private static Validation<Error, int> ValidateX(int x) =>
        x < 0 || x > 1000
            ? DomainError.For<Coordinate, int>(new DomainErrorType.OutOfRange("0", "1000"), x,
                $"X coordinate must be between 0 and 1000. Current value: '{x}'")
            : x;

    private static Validation<Error, int> ValidateY(int y) =>
        y < 0 || y > 1000
            ? DomainError.For<Coordinate, int>(new DomainErrorType.OutOfRange("0", "1000"), y,
                $"Y coordinate must be between 0 and 1000. Current value: '{y}'")
            : y;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return X;
        yield return Y;
    }

    public override string ToString() => $"({X}, {Y})";
}
```

## Summary at a Glance

Comparing the differences between the previous approach and the DomainError helper approach.

### Comparison Table
| Aspect | Previous Approach (DomainErrors Nested Class) | Current Approach (DomainError + DomainErrorType) |
|------|--------------------------------------|-------------------------------------------|
| **Error definition location** | Separate nested class | Inline within validation logic |
| **Error code generation** | Manual assembly (using nameof) | Automatic generation (type info + DomainErrorType) |
| **Error name safety** | String-based (typos possible) | Type-based (compile-time check) |
| **Code volume** | ~40 lines | ~15 lines |
| **Consistency** | Different names per developer possible | Enforced through standard error types |

### DomainErrorType Selection Guide

Choose the appropriate DomainErrorType based on the validation condition.

| Validation Condition | DomainErrorType | Generated Error Code |
|-----------|-----------------|-------------------|
| Empty value | `new DomainErrorType.Empty()` | `DomainErrors.{Type}.Empty` |
| Null value | `new DomainErrorType.Null()` | `DomainErrors.{Type}.Null` |
| Below minimum length | `new DomainErrorType.TooShort(8)` | `DomainErrors.{Type}.TooShort` |
| Exact length mismatch | `new DomainErrorType.WrongLength(5)` | `DomainErrors.{Type}.WrongLength` |
| Format error | `new DomainErrorType.InvalidFormat()` | `DomainErrors.{Type}.InvalidFormat` |
| Negative value | `new DomainErrorType.Negative()` | `DomainErrors.{Type}.Negative` |
| Out of range | `new DomainErrorType.OutOfRange("0", "100")` | `DomainErrors.{Type}.OutOfRange` |
| Not found | `new DomainErrorType.NotFound()` | `DomainErrors.{Type}.NotFound` |
| Domain-specific | `new Zero()` (`sealed record Zero : DomainErrorType.Custom;`) | `DomainErrors.{Type}.Zero` |

### Pros and Cons
| Pros | Cons |
|------|------|
| **60% code reduction** | Functorium framework dependency |
| **Type-safe error types** | - |
| **High cohesion through inline error definitions** | - |
| **Automatic error code generation** | - |
| **Standardized error names** | - |

## FAQ

### Q1: When should Custom be used?

Use standard types when a standard DomainErrorType can express the error, and define derived `sealed record` types only for domain-specific errors.

```csharp
// Can be expressed with standard type -> use standard type
DomainError.For<Currency>(new DomainErrorType.Empty(), value, "...");          // Use Empty
DomainError.For<Password>(new DomainErrorType.TooShort(8), value, "...");      // Use TooShort

// Domain-specific error -> define derived sealed record then use
// public sealed record Zero : DomainErrorType.Custom;
// public sealed record Unsupported : DomainErrorType.Custom;
// public sealed record StartAfterEnd : DomainErrorType.Custom;
DomainError.For<Denominator, int>(new Zero(), value, "...");
DomainError.For<Currency>(new Unsupported(), value, "...");
DomainError.For<DateRange>(new StartAfterEnd(), start, "...");
```

### Q2: Which DomainError.For overload should I use?

Choose based on the type of value to store when validation fails.

1. **String value** -> `DomainError.For<T>(errorType, stringValue, message)`
   ```csharp
   DomainError.For<Currency>(new DomainErrorType.Empty(), currencyCode ?? "", "...")
   ```

2. **Generic value (int, decimal, etc.)** -> `DomainError.For<T, TValue>(errorType, value, message)`
   ```csharp
   // sealed record Zero : DomainErrorType.Custom;
   DomainError.For<Denominator, int>(new Zero(), value, "...")
   DomainError.For<MoneyAmount, decimal>(new DomainErrorType.OutOfRange(), amount, "...")
   ```

3. **Two values** -> `DomainError.For<T, T1, T2>(errorType, v1, v2, message)`
   ```csharp
   // sealed record StartAfterEnd : DomainErrorType.Custom;
   DomainError.For<DateRange, DateTime, DateTime>(new StartAfterEnd(), start, end, "...")
   ```

### Q3: How do you verify errors in unit tests?

Use the type-safe extension methods from `Functorium.Testing.Assertions`. Verify errors created with `DomainError.For<T>()` using `ShouldBeDomainError<T>()`.

```csharp
// Before (string-based) - typo-prone, refactoring risk
result.IsFail.ShouldBeTrue();
result.IfFail(error => error.Message.ShouldContain("DomainErrors.Denominator.Zero"));

// After (type-safe) - compile-time verification, refactoring safe
result.ShouldBeDomainError<Denominator, int>(new Zero());
```

---

## Type-Safe Test Assertions

The Functorium framework provides type-safe test assertions that are symmetric with the `DomainError` creation pattern.

### Design Principle

Error creation and verification are designed to form a symmetric structure.

| Error Creation | Error Verification |
|-----------|-----------|
| `DomainError.For<T>(...)` | `ShouldBeDomainError<T>(...)` |
| `DomainError.For<T, TValue>(...)` | `ShouldBeDomainError<T, TValue>(...)` |
| `Validation<Error, T>` | `ShouldHaveDomainError<T>(...)` |

### Fin&lt;T&gt; Result Verification

```csharp
using Functorium.Testing.Assertions;

// 1. Basic verification - check error type only
var result = Denominator.Create(0);
result.ShouldBeDomainError<Denominator, int>(new Zero());

// 2. Strict verification - check error type + current value
result.ShouldBeDomainError<Denominator, int, int>(
    new Zero(),
    expectedCurrentValue: 0);

// 3. Standard error type verification
var streetResult = Street.Create("");
streetResult.ShouldBeDomainError<Street, string>(new DomainErrorType.Empty());

var currencyResult = Currency.Create("XYZ");
currencyResult.ShouldBeDomainError<Currency, string>(new Unsupported());
```

### Validation&lt;Error, T&gt; Result Verification

```csharp
// 1. Single error verification
Validation<Error, int> validation = Denominator.Validate(0);
validation.ShouldHaveDomainError<Denominator, int>(new Zero());

// 2. Verify exactly one error exists
Validation<Error, string> postalValidation = PostalCode.Validate("");
postalValidation.ShouldHaveOnlyDomainError<PostalCode, string>(new DomainErrorType.Empty());

// 3. Multiple error verification (when using Apply pattern)
var combined = (validation1, validation2).Apply((a, b) => a + b).As();
combined.ShouldHaveDomainErrors<PostalCode, string>(
    new DomainErrorType.Empty(),
    new DomainErrorType.WrongLength(5));

// 4. Verify including current value
validation.ShouldHaveDomainError<Denominator, int, int>(
    new Zero(),
    expectedCurrentValue: 0);
```

### Assertion Method Selection Guide

Choose the appropriate assertion method for each scenario.

| Scenario | Assertion Method |
|----------|------------------|
| Fin failure check | `fin.ShouldBeDomainError<TVO, T>(errorType)` |
| Fin failure + value check | `fin.ShouldBeDomainError<TVO, T, TValue>(errorType, value)` |
| Validation contains error | `validation.ShouldHaveDomainError<TVO, T>(errorType)` |
| Validation exactly 1 error | `validation.ShouldHaveOnlyDomainError<TVO, T>(errorType)` |
| Validation multiple errors | `validation.ShouldHaveDomainErrors<TVO, T>(types...)` |
| Validation error + value check | `validation.ShouldHaveDomainError<TVO, T, TValue>(errorType, value)` |

Error handling code has become concise, but the validation logic itself still needs to be written with ternary operators or Bind chains. In the next chapter, we introduce the `Validate<T>` Fluent API to improve the validation flow into a linear structure as well.

→ [Chapter 15: FluentValidation](../15-Validation-Fluent/)
