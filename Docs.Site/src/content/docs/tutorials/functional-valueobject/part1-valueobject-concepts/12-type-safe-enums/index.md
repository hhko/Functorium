---
title: "Type-Safe Enumerations"
---

## Overview

Each currency code needs a unique symbol ($, EUR, ₩) and formatting rules, but traditional C# enums cannot define properties or behaviors for each value. Using `Ardalis.SmartEnum`, each enumeration value acts as an independent object, simultaneously securing type safety and domain expressiveness.

## Learning Objectives

Upon completing this chapter, you will be able to:

1. Implement **type-safe enumerations** using `Ardalis.SmartEnum`
2. Enhance **type safety and domain expressiveness** through compile-time type verification and rich domain logic
3. Secure **domain model consistency and extensibility** through composite value object implementation using SmartEnum

## Why Is This Needed?

In the previous step `ValueObject-Framework`, we introduced the basic value object framework. However, the limitations of existing C# enums became apparent when modeling complex domain concepts. Enums cannot define additional properties or behaviors for each value, do not guarantee type safety during string/integer conversion, and domain logic is scattered outside the enum.

**SmartEnum** makes each enumeration value an independent object, allowing them to have unique properties and methods. It guarantees compile-time type safety while also enabling domain knowledge to be encapsulated within the object.

## Core Concepts

### Type-Safe Enumerations Based on SmartEnum

Existing C# enums are simple constant sets. SmartEnum makes each value an independent class instance, enabling additional properties and methods to be defined.

Comparing the implementation differences between existing enums and SmartEnum.

```csharp
// Previous approach (problematic) - cannot define additional properties
public enum Currency
{
    USD, EUR, KRW  // Cannot define symbols or formatting rules
}

// Improved approach (current) - each value is an independent object
public sealed class Currency : SmartEnum<Currency, string>
{
    public static readonly Currency USD = new(nameof(USD), "USD", "$", "US Dollar");
    public static readonly Currency EUR = new(nameof(EUR), "EUR", "EUR", "Euro");

    public string Symbol { get; }
    public string Description { get; }

    private Currency(string name, string value, string symbol, string description)
        : base(name, value)
    {
        Symbol = symbol;
        Description = description;
    }

    public string FormatAmount(decimal amount) => $"{Symbol}{amount:N2}";
}
```

### Compile-Time Type Safety

SmartEnum performs type verification at compile time. Existing enums only discover invalid values at runtime during string or integer conversion, but SmartEnum forces only valid values to be used through static instances.

```csharp
// Previous approach (problematic) - errors discovered only at runtime
public enum Currency { USD, EUR, KRW }
string invalidCurrency = "INVALID"; // Cannot catch errors at compile time

// Improved approach (current) - compile-time type safety
public sealed class Currency : SmartEnum<Currency, string>
{
    public static readonly Currency USD = new(nameof(USD), "USD", "$", "US Dollar");
    // Invalid values are blocked at compile time
}

// Type-safe usage
Currency validCurrency = Currency.USD; // Type verification at compile time
```

### Domain Logic Encapsulation

Using SmartEnum, unique behaviors can be encapsulated in each enumeration value. Domain logic is concentrated within the object rather than being scattered in external switch statements.

Showing the difference between existing switch-based scattered logic and the SmartEnum encapsulation approach.

```csharp
// Previous approach (problematic) - domain logic is scattered
public enum Currency { USD, EUR, KRW }
public string FormatCurrency(Currency currency, decimal amount)
{
    return currency switch
    {
        Currency.USD => $"${amount:N2}",
        Currency.EUR => $"EUR{amount:N2}",
        Currency.KRW => $"₩{amount:N0}",
        _ => throw new ArgumentException("Unknown currency")
    };
}

// Improved approach (current) - domain logic is encapsulated
public sealed class Currency : SmartEnum<Currency, string>
{
    public static readonly Currency USD = new(nameof(USD), "USD", "$", "US Dollar");
    public static readonly Currency EUR = new(nameof(EUR), "EUR", "EUR", "Euro");
    public static readonly Currency KRW = new(nameof(KRW), "KRW", "₩", "Korean Won");

    public string Symbol { get; }
    public string Description { get; }

    private Currency(string name, string value, string symbol, string description)
        : base(name, value)
    {
        Symbol = symbol;
        Description = description;
    }

    public string FormatAmount(decimal amount) => $"{Symbol}{amount:N2}";
    public bool IsMajorCurrency() => Symbol == "$" || Symbol == "EUR";
    public decimal ConvertToBaseUnit(decimal amount) => amount; // Exchange rate conversion logic
}
```

When adding a new currency, you simply add a new static instance without modifying existing code.

In the next chapter, we implement a structured error code system that goes beyond simple string error messages.

## Practical Guidelines

### Expected Output
```
=== ValueObject Framework Demo ===

   SmartEnum-based Currency and PriceRange (price range)
   Type-safe currency handling and PriceRange composition using SmartEnum

   Supported currency list:
      - AUD (Australian Dollar) A$ (code: AUD)
      - CAD (Canadian Dollar) C$ (code: CAD)
      - CHF (Swiss Franc) CHF (code: CHF)
      - CNY (Chinese Yuan) ¥ (code: CNY)
      - EUR (Euro) EUR (code: EUR)
      - GBP (British Pound) £ (code: GBP)
      - JPY (Japanese Yen) ¥ (code: JPY)
      - KRW (Korean Won) ₩ (code: KRW)
      - SGD (Singapore Dollar) S$ (code: SGD)
      - USD (US Dollar) $ (code: USD)

   Success (KRW): KRW (Korean Won) ₩ 10,000.00 ~ KRW (Korean Won) ₩ 50,000.00
   Success (USD): USD (US Dollar) $ 100.00 ~ USD (US Dollar) $ 500.00
   Success (EUR): EUR (Euro) EUR 80.00 ~ EUR (Euro) EUR 400.00

   Failure cases:
   Failure: Amount must be between 0 and 999,999.99: -1000
   Failure: Amount must be between 0 and 999,999.99: -5000
   Failure: Minimum price must be less than or equal to maximum price: KRW (Korean Won) ₩ 50,000.00 > KRW (Korean Won) ₩ 10,000.00
   Failure: Currency code must be 3 alphabetic characters: INVALID

   SmartEnum Currency direct usage:
      KRW: KRW (Korean Won) ₩ - ₩12,345.67
      USD: USD (US Dollar) $ - $123.45
      EUR: EUR (Euro) EUR - EUR89.12

   Currency support check:
      KRW supported: True
      USD supported: True
      INVALID supported: False

   Comparison demo:
   - KRW (Korean Won) ₩ 10,000.00 ~ KRW (Korean Won) ₩ 30,000.00 < KRW (Korean Won) ₩ 20,000.00 ~ KRW (Korean Won) ₩ 40,000.00 = True
   - KRW (Korean Won) ₩ 10,000.00 ~ KRW (Korean Won) ₩ 30,000.00 == KRW (Korean Won) ₩ 10,000.00 ~ KRW (Korean Won) ₩ 30,000.00 = True
   - KRW (Korean Won) ₩ 10,000.00 ~ KRW (Korean Won) ₩ 30,000.00 > KRW (Korean Won) ₩ 20,000.00 ~ KRW (Korean Won) ₩ 40,000.00 = False
   - KRW (Korean Won) ₩ 10,000.00 ~ KRW (Korean Won) ₩ 30,000.00 <= KRW (Korean Won) ₩ 10,000.00 ~ KRW (Korean Won) ₩ 30,000.00 = True
   - KRW (Korean Won) ₩ 10,000.00 ~ KRW (Korean Won) ₩ 30,000.00 >= KRW (Korean Won) ₩ 10,000.00 ~ KRW (Korean Won) ₩ 30,000.00 = True
   - KRW (Korean Won) ₩ 10,000.00 ~ KRW (Korean Won) ₩ 30,000.00 != KRW (Korean Won) ₩ 20,000.00 ~ KRW (Korean Won) ₩ 40,000.00 = True

   Individual value object creation:
   - MinPrice: USD (US Dollar) $ 15,000.00 (amount: 15000)
   - MaxPrice: USD (US Dollar) $ 35,000.00 (amount: 35000)
   - Currency: USD (US Dollar) $ (value: USD)
   - PriceRange from validated: USD (US Dollar) $ 15,000.00 ~ USD (US Dollar) $ 35,000.00

   Price comparison demo:
   Same currency (USD) comparison:
      - USD (US Dollar) $ 100.00 < USD (US Dollar) $ 200.00 = True
      - USD (US Dollar) $ 100.00 == USD (US Dollar) $ 100.00 = True
      - USD (US Dollar) $ 100.00 > USD (US Dollar) $ 200.00 = False
      - CanCompareWith: True = True

   Different currency comparison:
      - USD vs KRW: USD (US Dollar) $ 100.00 vs KRW (Korean Won) ₩ 100,000.00
      - CanCompareWith: False = False
      - Comparison result: False (currency-first comparison)
      - USD vs EUR: USD (US Dollar) $ 100.00 vs EUR (Euro) EUR 80.00
      - CanCompareWith: False = False
      - Comparison result: False (currency-first comparison)

   Safe comparison utility:
      - USD (US Dollar) $ 100.00 < USD (US Dollar) $ 200.00
      - Different currencies cannot be compared: USD (US Dollar) $ vs KRW (Korean Won) ₩
      - Different currencies cannot be compared: KRW (Korean Won) ₩ vs EUR (Euro) EUR

   Price sorting demo (currency-first, then by amount):
      1. EUR (Euro) EUR 80.00
      2. KRW (Korean Won) ₩ 100,000.00
      3. USD (US Dollar) $ 100.00
      4. USD (US Dollar) $ 100.00
      5. USD (US Dollar) $ 200.00
      - CanCompareWith: False = False
      - Comparison result: False (currency-first comparison)

   Safe comparison utility:
      - USD (US Dollar) $ 100.00 < USD (US Dollar) $ 200.00
      - Different currencies cannot be compared: USD (US Dollar) $ vs KRW (Korean Won) ₩

   Price sorting demo (currency-first, then by amount):
      1. EUR (Euro) EUR 80.00
      2. KRW (Korean Won) ₩ 100,000.00
      3. USD (US Dollar) $ 100.00
      4. USD (US Dollar) $ 100.00
      5. USD (US Dollar) $ 200.00
```

### Key Implementation Points
1. **SmartEnum Currency implementation**: Define each currency code as an independent object and implement unique symbols and formatting rules
2. **Type safety guarantee**: Build a type system that prevents usage of invalid currency codes at compile time
3. **Domain logic encapsulation**: Encapsulate each currency's unique characteristics and behaviors within the object to improve cohesion

## Project Description

### Project Structure
```
TypeSafeEnums/                    # Main project
├── Program.cs                    # Main entry file
├── TypeSafeEnums.csproj         # Project file
├── README.md                    # Main documentation
└── ValueObjects/                # Value object implementation
    └── Comparable/
        └── CompositeValueObjects/
            ├── Currency.cs      # SmartEnum-based currency
            ├── MoneyAmount.cs   # Money amount value object
            ├── Price.cs         # Price composite value object
            └── PriceRange.cs    # Price range composite value object
```

### Core Code

#### SmartEnum-Based Currency Implementation
```csharp
public sealed class Currency : SmartEnum<Currency, string>
{
    public static readonly Currency USD = new(nameof(USD), "USD", "$", "US Dollar");
    public static readonly Currency EUR = new(nameof(EUR), "EUR", "EUR", "Euro");
    public static readonly Currency KRW = new(nameof(KRW), "KRW", "₩", "Korean Won");
    // ... 10 currencies defined

    public string Symbol { get; }
    public string Description { get; }

    private Currency(string name, string value, string symbol, string description)
        : base(name, value)
    {
        Symbol = symbol;
        Description = description;
    }

    public string FormatAmount(decimal amount) => $"{Symbol}{amount:N2}";

    public static Validation<Error, string> Validate(string currencyCode) =>
        !string.IsNullOrWhiteSpace(currencyCode)
            ? ValidateThreeLetters(currencyCode)
            : Domain.Empty(currencyCode);

    private static Validation<Error, string> ValidateThreeLetters(string currencyCode) =>
        currencyCode.Length == 3 && currencyCode.All(char.IsLetter)
            ? ValidateSupported(currencyCode)
            : Domain.NotThreeLetters(currencyCode);

    private static Validation<Error, string> ValidateSupported(string currencyCode) =>
        GetAllSupportedCurrencies().Any(c => c.GetCode() == currencyCode)
            ? currencyCode
            : Domain.Unsupported(currencyCode);
}
```

#### Improved Price Comparison Logic
```csharp
public sealed class Price : ComparableValueObject
{
    public MoneyAmount Amount { get; }
    public Currency Currency { get; }

    private Price(MoneyAmount amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Fin<Price> Create(decimal amount, string currency) =>
        CreateFromValidation(
            Validate(amount, currency),
            validValues => new Price(validValues.Amount, validValues.Currency));

    public static Validation<Error, (MoneyAmount Amount, Currency Currency)> Validate(decimal amount, string currency) =>
        from validAmount in MoneyAmount.Validate(amount)
        from validCurrency in Currency.Validate(currency)
        select (Amount: MoneyAmount.CreateFromValidated(validAmount),
                Currency: Currency.CreateFromValidated(validCurrency));

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return Currency.Value;    // Compare currency first
        yield return (decimal)Amount;   // Compare amount second
    }

    public bool CanCompareWith(Price other) => Currency.Equals(other.Currency);

    public override string ToString() => $"{Currency} {Amount}";
}
```

#### LINQ Expression-Based PriceRange Validation
```csharp
public sealed class PriceRange : ComparableValueObject
{
    public Price MinPrice { get; }
    public Price MaxPrice { get; }

    private PriceRange(Price minPrice, Price maxPrice)
    {
        MinPrice = minPrice;
        MaxPrice = maxPrice;
    }

    public static Fin<PriceRange> Create(decimal minPriceValue, decimal maxPriceValue, string currencyCode) =>
        CreateFromValidation(
            Validate(minPriceValue, maxPriceValue, currencyCode),
            validValues => new PriceRange(validValues.MinPrice, validValues.MaxPrice));

    public static Validation<Error, (Price MinPrice, Price MaxPrice)> Validate(
    decimal minPriceValue,
    decimal maxPriceValue,
    string currencyCode) =>
        from validMinPriceTuple in Price.Validate(minPriceValue, currencyCode)
        from validMaxPriceTuple in Price.Validate(maxPriceValue, currencyCode)
    from validPriceRange in ValidatePriceRange(
            Price.CreateFromValidated(validMinPriceTuple),
            Price.CreateFromValidated(validMaxPriceTuple))
        select validPriceRange;

    private static Validation<Error, (Price MinPrice, Price MaxPrice)> ValidatePriceRange(
        Price minPrice, Price maxPrice) =>
        (decimal)minPrice.Amount <= (decimal)maxPrice.Amount
            ? (minPrice, maxPrice)
            : Domain.MinExceedsMax(minPrice, maxPrice);

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return (decimal)MinPrice.Amount;
        yield return (decimal)MaxPrice.Amount;
    }

    public override string ToString() => $"{MinPrice} ~ {MaxPrice}";
}
```

## Summary at a Glance

### Comparison Table

The following table summarizes the differences between existing C# enums and the SmartEnum approach.

| Aspect | Previous Approach | Current Approach |
|------|-----------|-----------|
| **Enumeration implementation** | Basic C# enum (cannot define properties) | SmartEnum (each value is an independent object) |
| **Type safety** | Errors discovered only at runtime | Compile-time type verification |
| **Domain expressiveness** | Only simple constants | Rich properties and behavior definition |
| **Extensibility** | Existing code modification required when adding new values | Adheres to the open-closed principle |
| **Encapsulation** | Domain logic scattered externally | Domain knowledge concentrated within the object |

### Pros and Cons

Trade-offs of introducing SmartEnum.

| Pros | Cons |
|------|------|
| Compile-time type verification | New library learning required |
| Unique properties and behaviors per value | External library dependency (Ardalis.SmartEnum) |
| Easy extension via open-closed principle | Increased implementation complexity compared to simple enums |
| Domain knowledge concentrated within the object | Object creation overhead (negligible level) |

## FAQ

### Q1: How do you distinguish when to use SmartEnum vs existing enums?
**A**: Simple states or flags (order status, user permissions, etc.) are sufficient with existing enums. SmartEnum is used when each value needs unique properties or behaviors (currency-specific symbols/formatting, order-type-specific calculation logic, etc.).

### Q2: Is there a performance overhead with SmartEnum?
**A**: Since only one static instance is created for each currency code, memory overhead is negligible. In most applications, the benefits of improved type safety and domain expressiveness far outweigh the performance cost.

### Q3: What are the considerations when introducing SmartEnum?
**A**: You should consider the team's learning curve, external library dependency management, and increased implementation complexity compared to existing enums. As domain complexity increases, the benefits of SmartEnum become more apparent, so it is reasonable to maintain existing enums for simple enumerations.

---

When value object validation fails, communicating "why it failed" through structured error codes greatly improves debugging and monitoring. In the next chapter, we implement a structured error code system.

→ [Chapter 13: Error Codes](../13-Error-Code/)
