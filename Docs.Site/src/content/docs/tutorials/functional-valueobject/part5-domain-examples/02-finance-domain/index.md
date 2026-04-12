---
title: "Finance Domain"
---
## Overview

Confusing interest rate 5% with 0.05 results in amounts differing by 100 times. If "USD/KRW = 1350" is ambiguous whether it means 1350 KRW per dollar or 1350 dollars per won, exchange errors occur. Account numbers printed in plain text in logs lead to security incidents. Primitive types in financial systems expose these risks directly.

In this chapter, we implement 4 core financial domain concepts as value objects to prevent calculation errors and security risks through the type system.

- **AccountNumber**: Value object that parses bank code and account number with masking
- **InterestRate**: Represents interest rates with simple/compound interest calculation
- **ExchangeRate**: Manages currency pairs and rates with conversion/inverse conversion
- **TransactionType**: Type-safe enumeration representing transaction types (deposit/withdrawal)

## Learning Objectives

### **Core Learning Objectives**
- You can **encapsulate** simple/compound interest calculations within the InterestRate value object.
- You can **calculate inverse exchange rates** with the Invert() method in ExchangeRate.
- You can **mask account numbers to enhance security** in AccountNumber.
- You can **implement a type-safe enumeration** with deposit/withdrawal classification properties in TransactionType.

### **What You Will Verify Through Practice**
- AccountNumber's bank code parsing and masking
- InterestRate's simple/compound interest calculation
- ExchangeRate's currency conversion and inverse rate calculation
- TransactionType's deposit/withdrawal classification

## Why Is This Needed?

Financial systems require particularly high accuracy and security. Handling financial data with primitive types creates several risks.

Confusing percentage (5%) and decimal (0.05) in interest rate calculations causes amount errors, which the InterestRate value object prevents by clearly distinguishing Percentage and Decimal properties. ExchangeRate explicitly manages BaseCurrency and QuoteCurrency, fundamentally preventing exchange rate direction confusion. The security issue of printing account numbers directly in logs is resolved by AccountNumber's Masked property supporting safe display.

## Core Concepts

### AccountNumber (Account Number)

AccountNumber validates and parses bank account numbers. It provides bank code extraction and masking functionality.

```csharp
public sealed class AccountNumber : SimpleValueObject<string>
{
    private static readonly Regex Format = new(@"^\d{3}-\d{10,14}$", RegexOptions.Compiled);

    private AccountNumber(string value) : base(value) { }

    public string FullNumber => Value;  // Public accessor for protected Value
    public string BankCode => Value[..3];            // "110"
    public string Number => Value[4..];              // "1234567890"
    public string Masked => $"{BankCode}-****{Number[^4..]}"; // "110-****7890"

    public static Fin<AccountNumber> Create(string? value) =>
        CreateFromValidation(Validate(value ?? "null"), v => new AccountNumber(v));

    public static Validation<Error, string> Validate(string value) =>
        ValidationRules<AccountNumber>.NotEmpty(value)
            .ThenNormalize(v => v.Replace(" ", "").Replace("\u2212", "-"))
            .ThenMatches(Format,
                $"Invalid account number format. Expected: 'NNN-NNNNNNNNNN'. Current value: '{value}'");

    public static implicit operator string(AccountNumber account) => account.Value;
}
```

`ToString()` returns the full account number, but `Masked` hides the middle portion for use in logs or screen display. This is a pattern for safe display of sensitive information.

### InterestRate (Interest Rate)

InterestRate stores interest rates as percentages and provides simple/compound interest calculation functionality.

```csharp
public sealed class InterestRate : ComparableSimpleValueObject<decimal>
{
    private InterestRate(decimal value) : base(value) { }

    public decimal Percentage => Value;          // 5.5 (%)
    public decimal Decimal => Value / 100m;      // 0.055

    public static Fin<InterestRate> Create(decimal percentValue) =>
        CreateFromValidation(Validate(percentValue), v => new InterestRate(v));

    public static Validation<Error, decimal> Validate(decimal value) =>
        ValidationRules<InterestRate>.NonNegative(value)
            .ThenAtMost(100m);

    // Simple interest: principal x rate x period
    public decimal CalculateSimpleInterest(decimal principal, int years) =>
        principal * Decimal * years;

    // Compound interest: principal x ((1 + rate)^period - 1)
    public decimal CalculateCompoundInterest(decimal principal, int years) =>
        principal * ((decimal)Math.Pow((double)(1 + Decimal), years) - 1);

    public static implicit operator decimal(InterestRate rate) => rate.Decimal;
}
```

Since interest calculation formulas are within the value object, consistent calculations are guaranteed everywhere. Separating Percentage and Decimal properties prevents percentage/decimal confusion.

### ExchangeRate (Exchange Rate)

ExchangeRate manages currency pairs (USD/KRW) and exchange rates. It provides conversion and inverse rate calculation functionality.

```csharp
public sealed class ExchangeRate : ValueObject
{
    public sealed record InvalidBaseCurrency : DomainErrorType.Custom;
    public sealed record InvalidQuoteCurrency : DomainErrorType.Custom;
    public sealed record SameCurrency : DomainErrorType.Custom;

    public string BaseCurrency { get; }    // "USD"
    public string QuoteCurrency { get; }   // "KRW"
    public decimal Rate { get; }           // 1350.50

    private ExchangeRate(string baseCurrency, string quoteCurrency, decimal rate)
    {
        BaseCurrency = baseCurrency; QuoteCurrency = quoteCurrency; Rate = rate;
    }

    public static Fin<ExchangeRate> Create(string? baseCurrency, string? quoteCurrency, decimal rate) =>
        CreateFromValidation(
            Validate(baseCurrency ?? "null", quoteCurrency ?? "null", rate),
            v => new ExchangeRate(v.BaseCurrency.ToUpperInvariant(), v.QuoteCurrency.ToUpperInvariant(), v.Rate));

    public static Validation<Error, (string BaseCurrency, string QuoteCurrency, decimal Rate)> Validate(
        string baseCurrency, string quoteCurrency, decimal rate) =>
        (ValidateCurrency(baseCurrency, new InvalidBaseCurrency(), "basecurrency"),
         ValidateCurrency(quoteCurrency, new InvalidQuoteCurrency(), "quotecurrency"),
         ValidationRules<ExchangeRate>.Positive(rate))
            .Apply((b, q, r) => (BaseCurrency: b, QuoteCurrency: q, Rate: r))
            .Bind(v => ValidateDifferentCurrencies(v.BaseCurrency, v.QuoteCurrency)
                .Map(_ => (v.BaseCurrency, v.QuoteCurrency, v.Rate)));

    public decimal Convert(decimal amount) => amount * Rate;       // 100 USD -> 135,050 KRW
    public decimal ConvertBack(decimal amount) => amount / Rate;   // 135,050 KRW -> 100 USD
    public ExchangeRate Invert() => new(QuoteCurrency, BaseCurrency, 1m / Rate);
    public string Pair => $"{BaseCurrency}/{QuoteCurrency}";       // "USD/KRW"

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return BaseCurrency; yield return QuoteCurrency; yield return Rate;
    }
}
```

`Convert()` converts from base to quote currency, `ConvertBack()` in the reverse direction, and `Invert()` returns an inverse exchange rate object. Bidirectional conversion is explicitly expressed.

### TransactionType (Transaction Type)

TransactionType uses SmartEnum to distinguish deposits from withdrawals.

```csharp
public sealed class TransactionType : SmartEnum<TransactionType, string>
{
    public static readonly TransactionType Deposit = new("DEPOSIT", "Deposit", isCredit: true);
    public static readonly TransactionType Withdrawal = new("WITHDRAWAL", "Withdrawal", isCredit: false);
    public static readonly TransactionType Transfer = new("TRANSFER", "Transfer", isCredit: false);
    public static readonly TransactionType Interest = new("INTEREST", "Interest", isCredit: true);
    public static readonly TransactionType Fee = new("FEE", "Fee", isCredit: false);

    public string DisplayName { get; }
    public bool IsCredit { get; }    // Deposit category
    public bool IsDebit => !IsCredit;  // Withdrawal category
}
```

The `IsCredit` property can determine addition/subtraction for balance calculations. The following code is an example of balance update utilizing this classification property.

```csharp
decimal UpdateBalance(decimal balance, TransactionType type, decimal amount) =>
    type.IsCredit ? balance + amount : balance - amount;
```

## Practical Guidelines

### Expected Output
```
=== Finance Domain Value Objects ===

1. AccountNumber (Account Number)
────────────────────────────────────────
   Account number: 110-1234567890
   Bank code: 110
   Masked: 110-****7890

2. InterestRate
────────────────────────────────────────
   Annual rate: 5.50%
   Principal: 1,000,000 KRW
   Period: 3 years
   Simple interest: 165,000 KRW
   Compound interest: 174,241 KRW

3. ExchangeRate (Exchange Rate)
────────────────────────────────────────
   Exchange rate: USD/KRW = 1350.5000
   100 USD = 135,050 KRW
   Inverse rate: KRW/USD = 0.0007

4. TransactionType (Transaction Type)
────────────────────────────────────────
   All transaction types:
      - DEPOSIT: Deposit (Deposit)
      - WITHDRAWAL: Withdrawal (Withdrawal)
      - TRANSFER: Transfer (Withdrawal)
      - INTEREST: Interest (Deposit)
      - FEE: Fee (Withdrawal)
```

## Project Description

### Project Structure
```
02-Finance-Domain/
├── FinanceDomain/
│   ├── Program.cs              # Main executable (4 value object implementations)
│   └── FinanceDomain.csproj    # Project file
└── README.md                   # Project documentation
```

### Dependencies
```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\..\..\Src\Functorium\Functorium.csproj" />
</ItemGroup>

<ItemGroup>
  <PackageReference Include="Ardalis.SmartEnum" />
</ItemGroup>
```

### Framework Type per Value Object

Summarizes the framework type each value object inherits and its key characteristics.

| value object | Framework Type | Characteristics |
|--------|---------------|------|
| AccountNumber | SimpleValueObject\<string\> | ValidationRules chain, parsing, masking |
| InterestRate | ComparableSimpleValueObject\<decimal\> | ValidationRules chain, simple/compound calculation |
| ExchangeRate | ValueObject | Parallel validation + Bind, currency pair management, conversion |
| TransactionType | SmartEnum | Deposit/withdrawal classification |

## Summary at a Glance

### Finance Value Object Summary

You can compare the properties, validation rules, and domain operations of each value object at a glance.

| value object | Key Properties | Validation Rules | Domain Operations |
|--------|----------|----------|------------|
| AccountNumber | Value | NNN-NNNNNNNNNN format | BankCode, Masked |
| InterestRate | Value | 0-100% range | Simple/compound calculation |
| ExchangeRate | Base, Quote, Rate | 3-character currency, positive rate | Convert, Invert |
| TransactionType | Value, IsCredit | Defined types only | None |

### Finance Domain Patterns

The following classifies the design patterns used in the finance domain by type.

| Pattern | value object | Description |
|------|--------|------|
| Sensitive information masking | AccountNumber | Safely display by hiding partial information |
| Domain calculation encapsulation | InterestRate | Interest calculation formulas implemented within value object |
| Bidirectional conversion | ExchangeRate | Provides forward/reverse conversion methods |
| Classification properties | TransactionType | Determines behavior via IsCredit/IsDebit |

## FAQ

### Q1: How to calculate monthly or daily compound interest in InterestRate?

Add compounding period as a parameter or provide separate methods.

```csharp
public decimal CalculateCompoundInterest(
    decimal principal,
    int years,
    CompoundingFrequency frequency = CompoundingFrequency.Annual)
{
    int n = frequency switch
    {
        CompoundingFrequency.Annual => 1,
        CompoundingFrequency.SemiAnnual => 2,
        CompoundingFrequency.Quarterly => 4,
        CompoundingFrequency.Monthly => 12,
        CompoundingFrequency.Daily => 365,
        _ => 1
    };

    return principal * ((decimal)Math.Pow((double)(1 + Decimal / n), n * years) - 1);
}
```

### Q2: How to perform chain conversion between multiple currencies in ExchangeRate?

Create a separate ExchangeRateService to manage exchange rate chains.

```csharp
public class ExchangeRateService
{
    private readonly Dictionary<string, ExchangeRate> _rates;

    public decimal Convert(decimal amount, string from, string to)
    {
        if (_rates.TryGetValue($"{from}/{to}", out var directRate))
            return directRate.Convert(amount);

        // Convert using USD as intermediate currency
        var toUsd = _rates[$"{from}/USD"];
        var fromUsd = _rates[$"USD/{to}"];
        return fromUsd.Convert(toUsd.Convert(amount));
    }
}
```

### Q3: How to support different formats per country in AccountNumber?

Accept the country code as a parameter and apply different validation patterns.

```csharp
public static Fin<AccountNumber> Create(string value, string countryCode = "KR")
{
    var pattern = countryCode switch
    {
        "KR" => @"^\d{3}-\d{10,14}$",
        "US" => @"^\d{9}-\d{12}$",  // Routing number + account number
        "GB" => @"^\d{6}-\d{8}$",   // Sort code + account number
        _ => throw new ArgumentException("Unsupported country code")
    };

    // Validation logic...
}
```

We have explored the value object implementation for the finance domain. In the next chapter, we implement value objects for the user management domain where security and data quality are particularly important, including email, password, and phone number.

---

## Tests

This project includes unit tests.

### Running Tests
```bash
cd FinanceDomain.Tests.Unit
dotnet test
```

### Test Structure
```
FinanceDomain.Tests.Unit/
├── AccountNumberTests.cs     # Account number format validation tests
├── InterestRateTests.cs      # Interest rate range validation tests
├── ExchangeRateTests.cs      # Exchange rate conversion tests
└── TransactionTypeTests.cs   # Transaction type SmartEnum tests
```

### Key Test Cases

| Test Class | Test Content |
|-------------|-----------|
| AccountNumberTests | Format validation, bank code/account number parsing |
| InterestRateTests | Range validation, decimal conversion, comparison operations |
| ExchangeRateTests | Exchange rate validation, currency conversion calculation |
| TransactionTypeTests | Deposit/withdrawal classification, IsCredit/IsDebit |

---

We have implemented the finance domain value objects. In the next chapter, we cover value objects in the user management domain where personal information protection is important, including email, password, and phone number.

→ [Chapter 3: User Management Domain](../03-User-Management-Domain/)
