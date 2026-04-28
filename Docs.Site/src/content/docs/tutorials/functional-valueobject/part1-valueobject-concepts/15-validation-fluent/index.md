---
title: "Validate Fluent API"
---

:::note[Naming convention note]
This tutorial walks through a **self-contained mini-framework** that uses the pre-1.0.0-alpha.4 names (e.g., `DomainErrors.X.Y`, `ErrorCodeFactory`, `DomainErrorType`). The Functorium production framework now uses shorter names — see the [Error System Spec](/spec/04-error-system/) for the full rename map. The patterns and design rationale shown here are unchanged; only the identifiers differ.
:::

## Overview

Error creation became concise with `DomainError.For<T>()`, but when applying multiple validation rules, didn't you find it inconvenient that ternary operators nest and types must be repeatedly specified? In this chapter, we introduce the `Validate<T>` Fluent API to write validation code as linear chaining and cover a pattern that reduces code volume by approximately 70%.

## Learning Objectives

Upon completing this chapter, you will be able to:

1. Start validation from a single entry point `Validate<T>` specifying the type parameter only once
2. Connect multiple validation rules linearly with `Then*()` methods
3. Include post-validation value transformation (normalization) in the chain with `ThenNormalize()`
4. Add custom validation conditions to the Fluent chain with `ThenMust()`

## Why Is This Needed?

In the previous `14-Error-Code-Fluent` project, we simplified error handling with the `DomainError.For<T>()` helper, but there was still room for improvement.

When applying multiple validation rules, nested ternary operators reduce readability.

```csharp
// Previous approach: Nested ternary operators
public static Validation<Error, string> Validate(string currencyCode) =>
    string.IsNullOrWhiteSpace(currencyCode)
        ? DomainError.For<Currency>(new DomainErrorType.Empty(), currencyCode ?? "", "...")
        : currencyCode.Length != 3
            ? DomainError.For<Currency>(new DomainErrorType.WrongLength(3), currencyCode, "...")
            : currencyCode.ToUpperInvariant();
```

The type must be repeatedly specified as `DomainError.For<Currency>(...)` each time, and value transformation (`ToUpperInvariant()`) is buried between validation logic, making the intent unclear.

**The Validate&lt;T&gt; Fluent API** improves readability through linear chaining, specifies the type only once, and expresses transformation explicitly with `ThenNormalize()`.

## Core Concepts

### The Validate&lt;T&gt; Static Class

`Validate<T>` is the single entry point for all validation. Once the type parameter is specified, it is automatically carried through the subsequent chain.

```csharp
// String validation methods
Validate<Currency>.NotEmpty(value)        // Empty value check
Validate<Currency>.MinLength(value, 3)    // Minimum length check
Validate<Currency>.MaxLength(value, 100)  // Maximum length check
Validate<Currency>.ExactLength(value, 3)  // Exact length check
Validate<Currency>.Matches(value, regex)  // Pattern check

// Numeric validation methods
Validate<MoneyAmount>.NonNegative(value)         // Non-negative check
Validate<MoneyAmount>.Positive(value)            // Positive check
Validate<MoneyAmount>.Between(value, 0, 1000)    // Range check
Validate<MoneyAmount>.AtMost(value, 999999.99m)  // Maximum value check
Validate<MoneyAmount>.AtLeast(value, 0)          // Minimum value check

// Custom validation methods
Validate<Denominator>.Must(value, v => v != 0, new Zero(), "message")  // sealed record Zero : DomainErrorType.Custom;
```

### The TypedValidation&lt;TValueObject, T&gt; Wrapper

`TypedValidation<TValueObject, T>` is a `readonly struct` that wraps `Validation<Error, T>`. It carries type information throughout the entire chain, and thanks to implicit conversion, it is fully compatible with existing code.

```csharp
public readonly struct TypedValidation<TValueObject, T>
{
    public Validation<Error, T> Value { get; }

    // Implicit conversion to Validation<Error, T>
    public static implicit operator Validation<Error, T>(TypedValidation<TValueObject, T> typed)
        => typed.Value;
}
```

You can return TypedValidation as-is from methods with a return type of `Validation<Error, string>`.

```csharp
// Return type is Validation<Error, string> but returning TypedValidation is fine
public static Validation<Error, string> Validate(string value) =>
    Validate<Currency>.NotEmpty(value)  // Returns TypedValidation<Currency, string>
        .ThenExactLength(3);            // Implicitly converts to Validation<Error, string>
```

### Fluent Chaining Extension Methods

The `TypedValidationExtensions` class provides linear chaining methods for `TypedValidation`.

```csharp
// String chaining methods
.ThenNotEmpty()           // Empty value check
.ThenMinLength(8)         // Minimum length check
.ThenMaxLength(100)       // Maximum length check
.ThenExactLength(3)       // Exact length check
.ThenMatches(regex)       // Pattern check
.ThenNormalize(fn)        // Value transformation (uses Map)

// Numeric chaining methods
.ThenNonNegative()        // Non-negative check
.ThenPositive()           // Positive check
.ThenBetween(0, 1000)     // Range check
.ThenAtMost(max)          // Maximum value check
.ThenAtLeast(min)         // Minimum value check

// Custom chaining methods
.ThenMust(predicate, errorType, message)  // Custom condition check
```

#### ThenNormalize vs ThenMust

`ThenNormalize` **transforms** a value (uses `Map` internally, always succeeds). `ThenMust` **validates** a value (uses `Bind` internally, returns an error on condition failure).

```csharp
// ThenNormalize: Value transformation (always succeeds)
.ThenNormalize(v => v.ToUpperInvariant())

// ThenMust: Conditional validation (can fail)
.ThenMust(v => SupportedCurrencies.Contains(v),
    new Unsupported(),  // sealed record Unsupported : DomainErrorType.Custom;
    v => $"Currency '{v}' is not supported")
```

### Automatic Error Code Generation

`Validate<T>` automatically generates error codes in the format `DomainErrors.{ValueObjectName}.{ErrorTypeName}`.

```csharp
// Validation code -> generated error code
Validate<Currency>.NotEmpty(value)        -> "DomainErrors.Currency.Empty"
Validate<Currency>.ExactLength(value, 3)  -> "DomainErrors.Currency.WrongLength"
Validate<MoneyAmount>.NonNegative(value)  -> "DomainErrors.MoneyAmount.Negative"
Validate<Coordinate>.Between(x, 0, 1000)  -> "DomainErrors.Coordinate.OutOfRange"
```

## Before/After Comparison

### Before (Previous Approach - Direct DomainError.For Usage)
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

### After (Validate&lt;T&gt; Fluent - Much More Concise)
```csharp
public sealed class PostalCode : SimpleValueObject<string>
{
    private static readonly Regex DigitsPattern = new(@"^\d+$", RegexOptions.Compiled);

    private PostalCode(string value) : base(value) { }

    public static Fin<PostalCode> Create(string value) =>
        CreateFromValidation(Validate(value), validValue => new PostalCode(validValue));

    public static PostalCode CreateFromValidated(string validatedValue) =>
        new PostalCode(validatedValue);

    public static Validation<Error, string> Validate(string value) =>
        Validate<PostalCode>.NotEmpty(value ?? "")
            .ThenExactLength(5)
            .ThenMatches(DigitsPattern, "Postal code must contain only digits");
}
```

**Code reduction: ~70%** (validation methods 6 lines -> 2 lines)

## Practical Guidelines

### Expected Output
```
=== Concise Validation Patterns Using Validate<T> Fluent API ===

=== Comparable Tests ===

--- CompositeValueObjects Subfolder ---
  === CompositeValueObjects Error Tests ===

  --- Currency Error Tests ---
Empty currency code: [DomainErrors.Currency.Empty] Currency cannot be empty. Current value: ''
Non-3-character format: [DomainErrors.Currency.WrongLength] Currency must be exactly 3 characters. Current length: 2
Unsupported currency: [DomainErrors.Currency.Unsupported] Currency 'XYZ' is not supported

  --- Price Error Tests ---
Negative price: [DomainErrors.MoneyAmount.Negative] MoneyAmount cannot be negative. Current value: '-100'

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
Empty street name: [DomainErrors.Street.Empty] Street cannot be empty. Current value: ''
Empty city name: [DomainErrors.City.Empty] City cannot be empty. Current value: ''
Invalid postal code: [DomainErrors.PostalCode.WrongLength] PostalCode must be exactly 5 characters. Current length: 4

  --- Street Error Tests ---
Empty street name: [DomainErrors.Street.Empty] Street cannot be empty. Current value: ''

  --- City Error Tests ---
Empty city name: [DomainErrors.City.Empty] City cannot be empty. Current value: ''

  --- PostalCode Error Tests ---
Empty postal code: [DomainErrors.PostalCode.Empty] PostalCode cannot be empty. Current value: ''
Non-5-digit format: [DomainErrors.PostalCode.WrongLength] PostalCode must be exactly 5 characters. Current length: 4

--- PrimitiveValueObjects Subfolder ---
  === PrimitiveValueObjects Error Tests ===

  --- BinaryData Error Tests ---
Null binary data: [DomainErrors.BinaryData.Empty] BinaryData cannot be empty. Current value: 'null'
Empty binary data: [DomainErrors.BinaryData.Empty] BinaryData cannot be empty. Current value: 'null'

--- CompositePrimitiveValueObjects Subfolder ---
  === CompositePrimitiveValueObjects Error Tests ===

  --- Coordinate Error Tests ---
Out-of-range X coordinate: [DomainErrors.Coordinate.OutOfRange] Coordinate must be between 0 and 1000. Current value: '-1'
Out-of-range Y coordinate: [DomainErrors.Coordinate.OutOfRange] Coordinate must be between 0 and 1000. Current value: '1001'
```

### Key Implementation Points

All validation starts with `Validate<ValueObjectType>.Method()` and additional rules are connected with `Then*()` methods. Value transformation is expressed explicitly with `ThenNormalize()`, and validations that are difficult to express with standard methods use `ThenMust()`. Thanks to implicit conversion from `TypedValidation` to `Validation<Error, T>`, it is compatible with existing code.

## Project Description

### Project Structure
```
15-Validation-Fluent/
├── README.md                              # This document
├── ValidationFluent/                      # Main project
│   ├── Program.cs                         # Main entry file
│   ├── ValidationFluent.csproj            # Project file
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
└── ValidationFluent.Tests.Unit/           # Unit tests
    ├── Using.cs                           # Global using definitions
    ├── PostalCodeTests.cs                 # PostalCode Fluent validation tests
    └── CurrencyTests.cs                   # Currency Fluent validation tests
```

### Core Code

#### Validate&lt;T&gt; Static Class (Provided by Functorium Framework)
```csharp
/// <summary>
/// Single entry point for validation with type parameter specified only once
/// </summary>
public static class Validate<TValueObject>
{
    // String validation
    public static TypedValidation<TValueObject, string> NotEmpty(string value);
    public static TypedValidation<TValueObject, string> MinLength(string value, int minLength);
    public static TypedValidation<TValueObject, string> MaxLength(string value, int maxLength);
    public static TypedValidation<TValueObject, string> ExactLength(string value, int length);
    public static TypedValidation<TValueObject, string> Matches(string value, Regex pattern, string? message = null);

    // Numeric validation
    public static TypedValidation<TValueObject, T> NonNegative<T>(T value) where T : INumber<T>;
    public static TypedValidation<TValueObject, T> Positive<T>(T value) where T : INumber<T>;
    public static TypedValidation<TValueObject, T> Between<T>(T value, T min, T max) where T : INumber<T>;
    public static TypedValidation<TValueObject, T> AtMost<T>(T value, T max) where T : INumber<T>;
    public static TypedValidation<TValueObject, T> AtLeast<T>(T value, T min) where T : INumber<T>;

    // Custom validation
    public static TypedValidation<TValueObject, T> Must<T>(
        T value, Func<T, bool> predicate, DomainErrorType errorType, string message);
}
```

#### TypedValidationExtensions (Provided by Functorium Framework)
```csharp
/// <summary>
/// Extension methods for TypedValidation chaining
/// </summary>
public static class TypedValidationExtensions
{
    // String chaining
    public static TypedValidation<TVO, string> ThenNotEmpty<TVO>(this TypedValidation<TVO, string> v);
    public static TypedValidation<TVO, string> ThenMinLength<TVO>(this TypedValidation<TVO, string> v, int min);
    public static TypedValidation<TVO, string> ThenMaxLength<TVO>(this TypedValidation<TVO, string> v, int max);
    public static TypedValidation<TVO, string> ThenExactLength<TVO>(this TypedValidation<TVO, string> v, int len);
    public static TypedValidation<TVO, string> ThenMatches<TVO>(this TypedValidation<TVO, string> v, Regex pattern);
    public static TypedValidation<TVO, string> ThenNormalize<TVO>(this TypedValidation<TVO, string> v, Func<string, string> fn);

    // Numeric chaining
    public static TypedValidation<TVO, T> ThenNonNegative<TVO, T>(this TypedValidation<TVO, T> v) where T : INumber<T>;
    public static TypedValidation<TVO, T> ThenPositive<TVO, T>(this TypedValidation<TVO, T> v) where T : INumber<T>;
    public static TypedValidation<TVO, T> ThenBetween<TVO, T>(this TypedValidation<TVO, T> v, T min, T max) where T : INumber<T>;
    public static TypedValidation<TVO, T> ThenAtMost<TVO, T>(this TypedValidation<TVO, T> v, T max) where T : INumber<T>;
    public static TypedValidation<TVO, T> ThenAtLeast<TVO, T>(this TypedValidation<TVO, T> v, T min) where T : INumber<T>;

    // Custom chaining
    public static TypedValidation<TVO, T> ThenMust<TVO, T>(this TypedValidation<TVO, T> v,
        Func<T, bool> predicate, DomainErrorType errorType, string message);
    public static TypedValidation<TVO, T> ThenMust<TVO, T>(this TypedValidation<TVO, T> v,
        Func<T, bool> predicate, DomainErrorType errorType, Func<T, string> messageFactory);
}
```

#### PostalCode - Most Concise Example
```csharp
public sealed class PostalCode : SimpleValueObject<string>
{
    private static readonly Regex DigitsPattern = new(@"^\d+$", RegexOptions.Compiled);

    private PostalCode(string value) : base(value) { }

    public static Fin<PostalCode> Create(string value) =>
        CreateFromValidation(Validate(value), validValue => new PostalCode(validValue));

    public static PostalCode CreateFromValidated(string validatedValue) =>
        new PostalCode(validatedValue);

    public static Validation<Error, string> Validate(string value) =>
        Validate<PostalCode>.NotEmpty(value ?? "")
            .ThenExactLength(5)
            .ThenMatches(DigitsPattern, "Postal code must contain only digits");
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

    private static readonly HashSet<string> SupportedCodes =
        new(List.Select(c => c.Value), StringComparer.OrdinalIgnoreCase);

    public static Fin<Currency> Create(string currencyCode) =>
        Validate(currencyCode)
            .Map(FromValue)
            .ToFin();

    public static Validation<Error, string> Validate(string currencyCode) =>
        Validate<Currency>.NotEmpty(currencyCode ?? "")
            .ThenNormalize(v => v.ToUpperInvariant())
            .ThenExactLength(3)
            .ThenMust(
                v => SupportedCodes.Contains(v),
                new Unsupported(),  // sealed record Unsupported : DomainErrorType.Custom;
                v => $"Currency '{v}' is not supported");
}
```

#### MoneyAmount - Numeric Range Validation
```csharp
public sealed class MoneyAmount : ComparableSimpleValueObject<decimal>
{
    private MoneyAmount(decimal value) : base(value) { }

    public static Fin<MoneyAmount> Create(decimal value) =>
        CreateFromValidation(Validate(value), validValue => new MoneyAmount(validValue));

    public static MoneyAmount CreateFromValidated(decimal validatedValue) =>
        new MoneyAmount(validatedValue);

    public static Validation<Error, decimal> Validate(decimal value) =>
        Validate<MoneyAmount>.NonNegative(value)
            .ThenAtMost(999999.99m);
}
```

#### Coordinate - Composite Primitive Value Object
```csharp
public sealed class Coordinate : ValueObject
{
    public int X { get; }
    public int Y { get; }

    private Coordinate(int x, int y) { X = x; Y = y; }

    public static Fin<Coordinate> Create(int x, int y) =>
        CreateFromValidation(Validate(x, y), validValues => new Coordinate(validValues.X, validValues.Y));

    public static Validation<Error, (int X, int Y)> Validate(int x, int y) =>
        from validX in ValidateX(x)
        from validY in ValidateY(y)
        select (X: validX, Y: validY);

    private static Validation<Error, int> ValidateX(int x) =>
        Validate<Coordinate>.Between(x, 0, 1000);

    private static Validation<Error, int> ValidateY(int y) =>
        Validate<Coordinate>.Between(y, 0, 1000);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return X;
        yield return Y;
    }
}
```

## Summary at a Glance

Comparing the differences between the previous DomainError.For direct usage approach and the Validate&lt;T&gt; Fluent approach.

### Comparison Table
| Aspect | Previous Approach (Direct DomainError.For) | Current Approach (Validate&lt;T&gt; Fluent) |
|------|---------------------------------------|--------------------------------------|
| **Type specification** | Every time with `DomainError.For<T>(...)` | Once with `Validate<T>.Method()` |
| **Validation flow** | Nested ternary operators/Bind chains | Linear `Then*()` chaining |
| **Value transformation** | Buried in validation logic | Explicit with `ThenNormalize()` |
| **Error messages** | Manually written each time | Auto-generated (customizable) |
| **Code volume** | 5-10 lines per validation method | 1-3 lines per validation method |

### Validate&lt;T&gt; Method Selection Guide

Choose starting methods and chaining methods based on the validation condition.

| Validation Condition | Starting Method | Chaining Method |
|-----------|-------------|---------------|
| Empty value | `Validate<T>.NotEmpty(value)` | `.ThenNotEmpty()` |
| Minimum length | `Validate<T>.MinLength(value, min)` | `.ThenMinLength(min)` |
| Maximum length | `Validate<T>.MaxLength(value, max)` | `.ThenMaxLength(max)` |
| Exact length | `Validate<T>.ExactLength(value, len)` | `.ThenExactLength(len)` |
| Pattern matching | `Validate<T>.Matches(value, regex)` | `.ThenMatches(regex)` |
| Non-negative | `Validate<T>.NonNegative(value)` | `.ThenNonNegative()` |
| Positive | `Validate<T>.Positive(value)` | `.ThenPositive()` |
| Range | `Validate<T>.Between(value, min, max)` | `.ThenBetween(min, max)` |
| Maximum value | `Validate<T>.AtMost(value, max)` | `.ThenAtMost(max)` |
| Minimum value | `Validate<T>.AtLeast(value, min)` | `.ThenAtLeast(min)` |
| Custom | `Validate<T>.Must(value, pred, type, msg)` | `.ThenMust(pred, type, msg)` |

### Pros and Cons
| Pros | Cons |
|------|------|
| **70% code reduction** | Functorium framework dependency |
| **Linear and readable flow** | New API learning required |
| **Type specified once** | - |
| **Auto-generated error messages** | - |
| **Explicit value transformation (ThenNormalize)** | - |
| **Full backward compatibility** | - |

## FAQ

### Q1: When should I use Validate&lt;T&gt; vs DomainError.For&lt;T&gt;?

In most cases, use the `Validate<T>` Fluent API. `DomainError.For<T>()` is used only in cases that are difficult to express with standard chaining, such as complex business logic.

```csharp
// Use Validate<T> (recommended) - standard validation patterns
public static Validation<Error, string> Validate(string value) =>
    Validate<PostalCode>.NotEmpty(value)
        .ThenExactLength(5);

// Use DomainError.For<T>() - complex business logic
// sealed record MinExceedsMax : DomainErrorType.Custom;
private static Validation<Error, (Price Min, Price Max)> ValidatePriceRange(Price min, Price max) =>
    (decimal)min.Amount > (decimal)max.Amount
        ? DomainError.For<PriceRange>(new MinExceedsMax(),
            $"Min: {min}, Max: {max}",
            $"Minimum price cannot exceed maximum price.")
        : (Min: min, Max: max);
```

### Q2: When should ThenNormalize be used?

Use it to transform (normalize) a value after existence validation (NotEmpty) and before structural validation (ExactLength, Matches, etc.). Follow the order of performing structural validation on the normalized value.

```csharp
// Good: Existence validation -> Normalization -> Structural validation
Validate<Currency>.NotEmpty(value)
    .ThenNormalize(v => v.ToUpperInvariant())  // Normalize first
    .ThenExactLength(3);                       // Validate normalized value

// Bad: Structural validation then normalization (value at validation time may differ from final value)
Validate<Currency>.NotEmpty(value)
    .ThenExactLength(3)                        // Validates pre-normalization value
    .ThenNormalize(v => v.ToUpperInvariant());  // Late transformation
```

### Q3: How do you validate multiple fields simultaneously?

Validate individual fields with `Validate<T>`, then combine them with LINQ query syntax or `Apply`.

```csharp
// LINQ query syntax (recommended)
public static Validation<Error, (int X, int Y)> Validate(int x, int y) =>
    from validX in Validate<Coordinate>.Between(x, 0, 1000)
    from validY in Validate<Coordinate>.Between(y, 0, 1000)
    select (X: validX, Y: validY);

// Apply pattern (collects all errors)
public static Validation<Error, (string Street, string City)> Validate(string street, string city) =>
    (Validate<Address>.NotEmpty(street), Validate<Address>.NotEmpty(city))
        .Apply((s, c) => (Street: s, City: c));
```

Validation code has become concise with the Fluent API, but how do you guarantee that these value objects consistently follow design rules? In the next chapter, we use ArchUnitNET-based architecture tests to automatically verify the structural rules of value objects.

→ [Chapter 16: Architecture Tests](../16-Architecture-Test/)
