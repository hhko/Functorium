---
title: "Type-Safe Enumeration Value Object"
---

## Overview

If you assign an invalid value like `(Currency)999` to a C# `enum`, it is not caught at compile time. You must manage currency symbols or Korean names in a separate `Dictionary`, and methods or properties cannot be added. SmartEnum overcomes these limitations by embedding type safety and domain logic directly in the enumeration itself.

## Learning Objectives

1. Implement type-safe enumerations using the Ardalis.SmartEnum library.
2. Include business logic and domain properties directly in enumeration values.
3. Apply functional validation patterns using LanguageExt's `Validation<Error, T>`.

## Why Is This Needed?

Traditional C# enums have three fundamental limitations.

Invalid values cannot be prevented at compile time.

```csharp
// Problem with traditional enum
public enum Currency { KRW, USD, EUR }

// Error not caught at compile time
Currency currency = (Currency)999; // Invalid value
```

Domain logic like currency symbols or names cannot be expressed in the enum itself and must be managed separately.

```csharp
// Traditional enum cannot express complex logic
public enum Currency { KRW, USD, EUR }

// Currency symbols and names must be managed separately
private static readonly Dictionary<Currency, string> Symbols = new()
{
    { Currency.KRW, "₩" },
    { Currency.USD, "$" },
    { Currency.EUR, "€" }
};
```

Extensibility is limited as methods, properties, and inheritance are not possible.

```csharp
// Adding new properties requires modifying existing code
// Cannot add methods
// Cannot inherit
```

## Core Concepts

### SmartEnum Basic Structure

SmartEnum defines each enumeration value as a static instance (`public static readonly`) and prevents external creation with a private constructor. Domain-required properties are initialized together in the constructor.

```csharp
public sealed class Currency : SmartEnum<Currency, string>
{
    public static readonly Currency KRW = new(nameof(KRW), "KRW", "Korean Won", "₩");
    public static readonly Currency USD = new(nameof(USD), "USD", "US Dollar", "$");

    public string DisplayName { get; }
    public string Symbol { get; }

    private Currency(string name, string value, string displayName, string symbol)
        : base(name, value)
    {
        DisplayName = displayName;
        Symbol = symbol;
    }
}
```

### Functional Validation Pattern

Using `Bind` chaining, dependent validation stages are executed sequentially. If any stage fails, subsequent stages are not executed.

```csharp
public static Validation<Error, string> Validate(string currencyCode) =>
    ValidateNotEmpty(currencyCode)
        .Bind(ValidateFormat)
        .Bind(ValidateSupported);
```

### Embedded Domain Logic

The greatest advantage of SmartEnum is that business logic can be directly included in enumeration values. Domain-specific features like formatting and calculation are defined inside the enumeration class.

```csharp
public string FormatAmount(decimal amount) => $"{Symbol}{amount:N2}";
public string FormatAmountWithoutDecimals(decimal amount) => $"{Symbol}{amount:N0}";
```

## Practical Guidelines

### SmartEnum Implementation Patterns

Three areas to distinguish when implementing SmartEnum.

| Area | Pattern | Description |
|------|------|------|
| **Instance definitions** | `public static readonly` | Defines each enumeration value |
| **Validation logic** | `Bind` chaining | Sequential validation + specific error messages |
| **Business logic** | Instance methods | Formatting, calculation, and other domain logic |

## Project Structure

```
07-TypeSafeEnum/
├── TypeSafeEnum/
│   ├── ValueObjects/
│   │   └── Currency.cs          # SmartEnum-based currency enumeration
│   ├── Program.cs               # Demo program
│   └── TypeSafeEnum.csproj      # Project file
└── README.md                    # Project document
```

## Core Code

### Currency SmartEnum Implementation

`Currency` inherits SmartEnum and implements `IValueObject` to comply with the framework's value object rules (`Create`, `Validate`, `DomainError.For<T>()`).

```csharp
public sealed class Currency : SmartEnum<Currency, string>, IValueObject
{
    public sealed record Unsupported : DomainErrorKind.Custom;

    // Static instances
    public static readonly Currency KRW = new(nameof(KRW), "KRW", "Korean Won", "₩");
    public static readonly Currency USD = new(nameof(USD), "USD", "US Dollar", "$");

    // Domain properties
    public string DisplayName { get; }
    public string Symbol { get; }

    // Private constructor
    private Currency(string name, string value, string displayName, string symbol)
        : base(name, value)
    {
        DisplayName = displayName;
        Symbol = symbol;
    }

    // Factory method
    public static Fin<Currency> Create(string currencyCode) =>
        Validate(currencyCode).Map(FromValue).ToFin();

    // Validation logic - DomainError.For<T>() pattern
    public static Validation<Error, string> Validate(string currencyCode) =>
        ValidateNotEmpty(currencyCode)
            .Bind(ValidateFormat)
            .Bind(ValidateSupported);

    // Individual validation methods - DomainError.For<T>() pattern applied
    private static Validation<Error, string> ValidateNotEmpty(string currencyCode) =>
        !string.IsNullOrWhiteSpace(currencyCode)
            ? currencyCode
            : DomainError.For<Currency>(new DomainErrorKind.Empty(), currencyCode,
                $"Currency code cannot be empty. Current value: '{currencyCode}'");

    private static Validation<Error, string> ValidateFormat(string currencyCode) =>
        currencyCode.Length == 3 && currencyCode.All(char.IsLetter)
            ? currencyCode.ToUpperInvariant()
            : DomainError.For<Currency>(new DomainErrorKind.WrongLength(), currencyCode,
                $"Currency code must be exactly 3 letters. Current value: '{currencyCode}'");

    // Business logic
    public string FormatAmount(decimal amount) => $"{Symbol}{amount:N2}";
}
```

### Alternative: SimpleValueObject\<string> + HashMap Pattern

In addition to SmartEnum, type-safe enumerations can be implemented by combining the framework's `SimpleValueObject<string>` and LanguageExt's `HashMap`. This pattern requires no external library dependencies and uses only the framework.

> Reference: `OrderStatus` implementation in `Tests.Hosts/01-SingleHost`

```csharp
public sealed class OrderStatus : SimpleValueObject<string>
{
    public sealed record InvalidValue : DomainErrorKind.Custom;

    // Static instances
    public static readonly OrderStatus Pending = new("Pending");
    public static readonly OrderStatus Confirmed = new("Confirmed");
    public static readonly OrderStatus Shipped = new("Shipped");
    public static readonly OrderStatus Delivered = new("Delivered");
    public static readonly OrderStatus Cancelled = new("Cancelled");

    // Valid values list using HashMap
    private static readonly HashMap<string, OrderStatus> All = HashMap(
        ("Pending", Pending),
        ("Confirmed", Confirmed),
        ("Shipped", Shipped),
        ("Delivered", Delivered),
        ("Cancelled", Cancelled));

    private OrderStatus(string value) : base(value) { }

    public static Fin<OrderStatus> Create(string value) =>
        Validate(value).ToFin();

    public static OrderStatus CreateFromValidated(string validatedValue) =>
        All[validatedValue];

    public static Validation<Error, OrderStatus> Validate(string value) =>
        All.Find(value)
            .ToValidation(DomainError.For<OrderStatus>(
                new InvalidValue(), currentValue: value,
                message: $"Invalid order status: '{value}'"));
}
```

Summary of the differences between the two approaches.

**SmartEnum vs SimpleValueObject+HashMap Comparison:**
| Feature | SmartEnum | SimpleValueObject+HashMap |
|------|-----------|---------------------------|
| **External dependency** | Requires Ardalis.SmartEnum | Uses framework only |
| **Domain properties** | Freely addable | Limited (separate property management) |
| **ValueObject compatibility** | Manual IValueObject implementation | Automatic inheritance |
| **HashMap lookup** | Built-in FromValue/FromName | Uses LanguageExt HashMap |

### Demo Program
```csharp
// Basic usage
var krw = Currency.KRW;
var usd = Currency.FromValue("USD");

// Validation
var result = Currency.Create("INVALID");
result.Match(
    Succ: currency => Console.WriteLine($"Success: {currency}"),
    Fail: error => Console.WriteLine($"Failure: {error.Message}")
);

// Business logic
Console.WriteLine(Currency.USD.FormatAmount(1000)); // $1,000.00
```

## Expected Output

```
=== Type Safe Enum Demo ===

1. Basic Usage
================
KRW: KRW (Korean Won) ₩
USD: USD (US Dollar) $
EUR: EUR (Euro) €
JPY from value: JPY (Japanese Yen) ¥
GBP from name: GBP (British Pound) £

2. Validation
=============
Valid currency: USD (US Dollar) $
Error: Currency code must be exactly 3 letters: US
Error: Unsupported currency code: XYZ
Error: Currency code cannot be empty:

3. Comparison
=============
KRW == KRW: True
KRW == USD: False
KRW < USD: True
USD > EUR: False
KRW HashCode: 1234567890
USD HashCode: 9876543210

4. Business Logic
=================
KRW: ₩1,000.00
KRW: ₩1,000
USD: $1,000.00
USD: $1,000
EUR: €1,000.00
EUR: €1,000
JPY: ¥1,000.00
JPY: ¥1,000

5. Error Handling
=============
USD -> USD (US Dollar) $
INVALID -> Unsupported currency code: INVALID
KR -> Currency code must be exactly 3 letters: KR
XYZ -> Unsupported currency code: XYZ
EUR -> EUR (Euro) €

6. All Supported Currencies
==================
10 currencies supported:
  - KRW (Korean Won) ₩
  - USD (US Dollar) $
  - EUR (Euro) €
  - JPY (Japanese Yen) ¥
  - CNY (Chinese Yuan) ¥
  - GBP (British Pound) £
  - AUD (Australian Dollar) A$
  - CAD (Canadian Dollar) C$
  - CHF (Swiss Franc) CHF
  - SGD (Singapore Dollar) S$

=== Demo Complete ===
```

## Summary at a Glance

Compares the functional differences between traditional enum and SmartEnum.

### SmartEnum vs Traditional Enum Comparison
| Feature | Traditional Enum | SmartEnum |
|------|-----------|-----------|
| **Type safety** | Limited | Complete type safety |
| **Domain logic** | Requires separate management | Can be directly embedded |
| **Property addition** | Not possible | Freely addable |
| **Method addition** | Not possible | Freely addable |
| **Inheritance** | Not possible | Possible |
| **Validation** | Manual implementation required | Auto-provided + customizable |
| **Comparison** | Basic provided | Advanced comparison provided |

### Pros and Cons
| Pros | Cons |
|------|------|
| **Complete type safety** | **External library dependency** |
| **Embedded domain logic** | **More complex than traditional enum** |
| **Powerful validation** | **Performance overhead (negligible)** |
| **Extensibility and flexibility** | **Learning curve** |

## FAQ

### Q1: What is the biggest difference between SmartEnum and traditional enum?
**A**: SmartEnum can directly embed domain logic. Traditional enums can only store simple integer/string values, but SmartEnum can include properties, methods, and business logic.

### Q2: When should SmartEnum be used?
**A**: It is suitable for enumerations that need domain logic like formatting, calculation, or validation, that need to prevent invalid value input at the type level, or where new properties or methods are expected to be added.

### Q3: How does SmartEnum comply with ValueObject rules?
**A**: Implement the `IValueObject` interface and handle structured errors with the `DomainError.For<T>()` pattern. Custom error types are defined in the form `sealed record Unsupported : DomainErrorKind.Custom`.

```csharp
// ValueObject rule compliance example
private static Validation<Error, string> ValidateNotEmpty(string currencyCode) =>
    !string.IsNullOrWhiteSpace(currencyCode)
        ? currencyCode                    // Success: return value
        : DomainError.For<Currency>(      // Failure: use DomainError.For<T>()
            new DomainErrorKind.Empty(), currencyCode,
            $"Currency code cannot be empty. Current value: '{currencyCode}'");
```

The next chapter covers how to use ArchUnitNET to automatically verify that all value objects implemented so far correctly comply with architectural rules.

---

-> [Chapter 8: Architecture Test](../08-Architecture-Test/)
