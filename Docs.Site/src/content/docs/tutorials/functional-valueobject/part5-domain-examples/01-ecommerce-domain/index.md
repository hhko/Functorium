---
title: "E-Commerce Domain"
---
## Overview

`decimal price = 10000;` -- is this amount in KRW or USD? What happens if product code `"invalid"` is included in an order? When business concepts are expressed as primitive types in e-commerce systems, currency confusion, format errors, and invalid state transitions go undetected until runtime.

In this chapter, Part 1~4에서 학습한 Pattern과 기법을 실제 이커머스 도메인에 적용하여, 이러한 문제를 타입 시스템으로 방지하는 5가지 value object를 implements.

- **Money**: Composite value object that manages amount and currency together
- **ProductCode**: Single value object that validates product code format
- **Quantity**: Comparable value object expressing quantities with sorting and arithmetic capabilities
- **OrderStatus**: Type-safe enumeration expressing order status and transition rules
- **ShippingAddress**: Composite value object expressing shipping address

## Learning Objectives

### **Core Learning Objectives**
- Money처럼 여러 속성을 가진 value object에서 Add, Subtract 같은 **Domain Operations을 구현할 수** 있습니다.
- You can **validate business formats** using regular expressions like ProductCode.
- You can **implement a state machine** using SmartEnum in OrderStatus.
- You can **sequentially validate** multiple fields like ShippingAddress.

### **What You Will Verify Through Practice**
- Money's per-currency operation restrictions and amount calculation
- ProductCode's category-number parsing
- Quantity's arithmetic operator overloading
- OrderStatus's valid state transition verification
- ShippingAddress's multi-field validation

## Why Is This Needed?

E-commerce systems deal with various business concepts such as amounts, quantities, and product codes. Expressing these concepts as primitive types causes several problems.

Expressing it as `decimal price = 10000;` makes it impossible to know whether this is KRW or USD. The Money value object manages amount and currency together to prevent incorrect operations between different currencies. If product codes are `string`, values like `"invalid"` can be assigned anywhere, but the ProductCode value object validates format at creation time ensuring only valid formats exist. Managing order status with only `string` or `enum` allows abnormal transitions like changing from "Delivered" to "Pending", but OrderStatus implements a state machine that only allows valid transitions.

## Core Concepts

### Money (Amount)

Money is a composite value object that manages Amount and Currency together. Operations are only possible between the same currency.

```csharp
public sealed class Money : ValueObject, IComparable<Money>
{
    public sealed record CurrencyEmpty : DomainErrorType.Custom;
    public sealed record CurrencyNotThreeCharacters : DomainErrorType.Custom;

    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Fin<Money> Create(decimal amount, string? currency) =>
        CreateFromValidation(
            Validate(amount, currency ?? ""),
            validValues => new Money(validValues.Amount, validValues.Currency.ToUpperInvariant()));

    public static Validation<Error, (decimal Amount, string Currency)> Validate(decimal amount, string currency) =>
        (ValidateAmountNotNegative(amount), ValidateCurrencyNotEmpty(currency), ValidateCurrencyLength(currency))
            .Apply((validAmount, validCurrency, _) => (validAmount, validCurrency));

    private static Validation<Error, decimal> ValidateAmountNotNegative(decimal amount) =>
        amount >= 0
            ? amount
            : DomainError.For<Money, decimal>(new DomainErrorType.Negative(), amount,
                $"Amount cannot be negative. Current value: '{amount}'");

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add amounts of different currencies.");
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot subtract amounts of different currencies.");
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor) => new(Amount * factor, Currency);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

Operations like Add, Subtract verify currency match first. An attempt to add USD and KRW raises an exception at runtime. Generic currency types could be used for compile-time prevention, but runtime validation is more flexible in practice.

### ProductCode (Product Code)

ProductCode validates product codes in `"EL-001234"` format. It also provides the ability to parse category (2-letter alpha) and number (6-digit numeric).

```csharp
public sealed class ProductCode : SimpleValueObject<string>
{
    private ProductCode(string value) : base(value) { }

    public string Code => Value;  // Public accessor for protected Value
    public string Category => Value[..2];   // "EL"
    public string Number => Value[3..];      // "001234"

    public static Fin<ProductCode> Create(string? value) =>
        CreateFromValidation(
            Validate(value ?? ""),
            validValue => new ProductCode(validValue));

    public static Validation<Error, string> Validate(string value) =>
        ValidateNotEmpty(value)
            .Bind(_ => ValidateFormat(value));

    private static Validation<Error, string> ValidateNotEmpty(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainError.For<ProductCode>(new DomainErrorType.Empty(), value,
                $"Product code cannot be empty. Current value: '{value}'");

    private static Validation<Error, string> ValidateFormat(string value)
    {
        var normalized = value.ToUpperInvariant().Trim();
        return Regex.IsMatch(normalized, @"^[A-Z]{2}-\d{6}$")
            ? normalized
            : DomainError.For<ProductCode>(new DomainErrorType.InvalidFormat(), value,
                $"Product code must match 'XX-NNNNNN' pattern. Current value: '{value}'");
    }

    public static implicit operator string(ProductCode code) => code.Value;
}
```

유효한 ProductCode만 존재할 수 있으므로, `Category`와 `Number` 속성은 항상 안전하게 접근할 수 있습니다. 형식 검증과 파싱이 하나의 value object에 결합된 Pattern입니다.

### Quantity

Quantity is a comparable value object capable of arithmetic operations. It validates negative values and maximum limits.

```csharp
public sealed class Quantity : ComparableSimpleValueObject<int>
{
    private Quantity(int value) : base(value) { }

    public int Amount => Value;  // Public accessor for protected Value

    public static Quantity Zero => new(0);
    public static Quantity One => new(1);

    public static Fin<Quantity> Create(int value) =>
        CreateFromValidation(
            Validate(value),
            validValue => new Quantity(validValue));

    public static Validation<Error, int> Validate(int value) =>
        ValidateNotNegative(value)
            .Bind(_ => ValidateNotExceedsLimit(value))
            .Map(_ => value);

    private static Validation<Error, int> ValidateNotNegative(int value) =>
        value >= 0
            ? value
            : DomainError.For<Quantity, int>(new DomainErrorType.Negative(), value,
                $"Quantity cannot be negative. Current value: '{value}'");

    public Quantity Add(Quantity other) => new(Value + other.Value);
    public Quantity Subtract(Quantity other) => new(Math.Max(0, Value - other.Value));

    public static Quantity operator +(Quantity a, Quantity b) => a.Add(b);
    public static Quantity operator -(Quantity a, Quantity b) => a.Subtract(b);

    public static implicit operator int(Quantity quantity) => quantity.Value;
}
```

Thanks to operator overloading, expressions like `qty1 + qty2`, `qty1 > qty2` are possible, making domain logic intuitive.

### OrderStatus (Order Status)

OrderStatus is a type-safe enumeration using SmartEnum. It encapsulates each status properties (cancellability) and transition rules.

```csharp
public sealed class OrderStatus : SmartEnum<OrderStatus, string>
{
    public sealed record AlreadyCancelled : DomainErrorType.Custom;
    public sealed record AlreadyDelivered : DomainErrorType.Custom;
    public sealed record CannotRevertToPending : DomainErrorType.Custom;

    public static readonly OrderStatus Pending = new("PENDING", "Pending", canCancel: true);
    public static readonly OrderStatus Confirmed = new("CONFIRMED", "Confirmed", canCancel: true);
    public static readonly OrderStatus Shipped = new("SHIPPED", "Shipping", canCancel: false);
    public static readonly OrderStatus Delivered = new("DELIVERED", "Delivered", canCancel: false);
    public static readonly OrderStatus Cancelled = new("CANCELLED", "Cancelled", canCancel: false);

    public string DisplayName { get; }
    public bool CanCancel { get; }

    public Fin<OrderStatus> TransitionTo(OrderStatus next)
    {
        return (this, next) switch
        {
            (var s, _) when s == Cancelled => DomainError.For<OrderStatus>(
                new AlreadyCancelled(), $"{Value}->{next.Value}",
                $"Cannot change status of a cancelled order. Current: '{Value}', Target: '{next.Value}'"),
            (var s, _) when s == Delivered => DomainError.For<OrderStatus>(
                new AlreadyDelivered(), $"{Value}->{next.Value}",
                $"Cannot change status of a delivered order. Current: '{Value}', Target: '{next.Value}'"),
            (_, var n) when n == Pending => DomainError.For<OrderStatus>(
                new CannotRevertToPending(), $"{Value}->{next.Value}",
                $"Cannot revert to pending status. Current: '{Value}', Target: '{next.Value}'"),
            _ => next
        };
    }
}
```

Business rules such as cancelled orders cannot change status and cannot revert to pending status are defined within the value object. Since state transition rules are encapsulated, attempting an invalid transition from outside returns a domain error.

### ShippingAddress (Shipping Address)

ShippingAddress is a composite value object containing recipient, street, city, postal code, and country.

```csharp
public sealed class ShippingAddress : ValueObject
{
    public sealed record RecipientNameEmpty : DomainErrorType.Custom;
    public sealed record StreetEmpty : DomainErrorType.Custom;
    public sealed record CityEmpty : DomainErrorType.Custom;
    public sealed record CountryEmpty : DomainErrorType.Custom;

    public string RecipientName { get; }
    public string Street { get; }
    public string City { get; }
    public string PostalCode { get; }
    public string Country { get; }

    private ShippingAddress(string recipientName, string street, string city, string postalCode, string country)
    {
        RecipientName = recipientName; Street = street; City = city;
        PostalCode = postalCode; Country = country;
    }

    public static Fin<ShippingAddress> Create(
        string? recipientName, string? street, string? city, string? postalCode, string? country) =>
        CreateFromValidation(
            Validate(recipientName ?? "", street ?? "", city ?? "", postalCode ?? "", country ?? ""),
            v => new ShippingAddress(v.RecipientName.Trim(), v.Street.Trim(), v.City.Trim(),
                v.PostalCode, v.Country.Trim().ToUpperInvariant()));

    public static Validation<Error, (string RecipientName, string Street, string City, string PostalCode, string Country)>
        Validate(string recipientName, string street, string city, string postalCode, string country) =>
        (ValidateRecipientName(recipientName), ValidateStreet(street), ValidateCity(city), ValidateCountry(country))
            .Apply((r, s, c, co) => (r, s, c, co))
            .Bind(values => ValidatePostalCode(postalCode)
                .Map(p => (values.r, values.s, values.c, p, values.co)));

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return RecipientName; yield return Street; yield return City;
        yield return PostalCode; yield return Country;
    }
}
```

각 필드를 순서대로 검증하고, 첫 번째 오류에서 즉시 returns. 다중 필드의 순차 검증 Pattern을 보여주는 대표적인 예시입니다.

## Practical Guidelines

### Expected Output
```
=== E-Commerce Domain Value Objects ===

1. Money (Amount) - ComparableValueObject
────────────────────────────────────────
   Product price: 10,000 KRW
   Discount amount: 1,000 KRW
   Final price: 9,000 KRW
   Different currency addition attempt: Cannot add amounts of different currencies.

2. ProductCode (Product Code) - SimpleValueObject
────────────────────────────────────────
   Product code: EL-001234
   Category: EL
   Number: 001234
   Invalid format: Product code format is invalid. (e.g., EL-001234)

3. Quantity - ComparableSimpleValueObject
────────────────────────────────────────
   Quantity 1: 5
   Quantity 2: 3
   Total: 8
   Comparison: 5 > 3 = True
   Sorting: [1, 3, 5]

4. OrderStatus (Order Status) - SmartEnum
────────────────────────────────────────
   Current status: Pending
   Cancellable: True
   After transition: Confirmed
   Shipping: Shipping, Cancellable: False

5. ShippingAddress (Shipping Address) - ValueObject
────────────────────────────────────────
   Recipient: Hong Gildong
   Address: 123 Teheran-ro, Seoul
   우편Number: 06234
   Country: KR

   Empty address validation result: Recipient name is empty.
```

## Project Description

### Project Structure
```
01-Ecommerce-Domain/
├── EcommerceDomain/
│   ├── Program.cs                  # Main executable (5개 값 객체 구현)
│   └── EcommerceDomain.csproj      # Project file
└── README.md                       # Project documentation
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

### value object별 Framework Type

The following table 각 value object가 어떤 프레임워크 기반 타입을 상속하고 어떤 Characteristics을 갖는지 정리한 것입니다.

| value object | Framework Type | Characteristics |
|--------|---------------|------|
| Money | ValueObject + IComparable | Composite value, same currency operations |
| ProductCode | SimpleValueObject\<string\> | Format validation, parsing |
| Quantity | ComparableSimpleValueObject\<int\> | operator overloading |
| OrderStatus | SmartEnum | State transition rules |
| ShippingAddress | ValueObject | Multi-field validation |

## Summary at a Glance

### E-Commerce Value Object Summary

각 value object의 속성, Validation Rules, Domain Operations을 한눈에 비교할 수 있습니다.

| value object | Key Properties | Validation Rules | Domain Operations |
|--------|----------|----------|------------|
| Money | Amount, Currency | No negative amount, 3-character currency code | Add, Subtract, Multiply |
| ProductCode | Value | XX-NNNNNN format | Category, Number parsing |
| Quantity | Value | 0-10000 range | +, -, comparison |
| OrderStatus | Value, DisplayName | Valid states only | TransitionTo |
| ShippingAddress | 5 fields | All fields required | None |

### 검증 Pattern 비교

이커머스 도메인에서 사용된 검증 Pattern을 유형별로 minutes류하면 다음과 같습니다.

| Pattern | value object | Description |
|------|--------|------|
| Single condition validation | Quantity | Range check |
| Regex validation | ProductCode | 형식 Pattern 매칭 |
| Multi-field sequential validation | ShippingAddress | Validate each field in order |
| Composite condition validation | Money | Validate amount and currency separately |
| State transition validation | OrderStatus | Current-target status combination verification |

## FAQ

### Q1: How to support operations between different currencies in Money?

One approach is to inject an exchange rate conversion service and operate after conversion. Alternatively, you can create a separate MoneyConverter domain service that converts two Money objects to the same currency before operating.

```csharp
public Money ConvertTo(string targetCurrency, IExchangeRateService rateService)
{
    if (Currency == targetCurrency)
        return this;

    var rate = rateService.GetRate(Currency, targetCurrency);
    return new Money(Amount * rate, targetCurrency);
}
```

### Q2: How to manage more complex state transitions in OrderStatus?

You can use a state machine library (like Stateless) or create a separate OrderStatusTransition value object to explicitly manage transition rules.

```csharp
public static readonly Dictionary<(OrderStatus From, OrderStatus To), bool> AllowedTransitions = new()
{
    { (Pending, Confirmed), true },
    { (Confirmed, Shipped), true },
    { (Shipped, Delivered), true },
    { (Pending, Cancelled), true },
    { (Confirmed, Cancelled), true }
};
```

### Q3: How to allow negative results in Quantity?

The current implementation prevents negatives in Subtract with `Math.Max(0, ...)`. To allow negatives, you can create a separate `SignedQuantity` type or return the result as `Fin<T>`.

```csharp
// Method 1: Version allowing negatives
public Quantity SubtractAllowNegative(Quantity other) =>
    new(Value - other.Value);

// Method 2: Return result as Fin<T>
public Fin<Quantity> SafeSubtract(Quantity other)
{
    var result = Value - other.Value;
    return result >= 0
        ? new Quantity(result)
        : DomainError.For<Quantity, int>(new DomainErrorType.Negative(), result,
            $"Result would be negative. Current: '{Value}', Other: '{other.Value}'");
}
```

We have explored the value object implementation for the e-commerce domain. In the next chapter, we implement value objects for the finance domain where accuracy and security are particularly important, including account numbers, interest rates, and exchange rates.

---

## Tests

This project includes unit tests.

### Tests 실행
```bash
cd EcommerceDomain.Tests.Unit
dotnet test
```

### Tests 구조
```
EcommerceDomain.Tests.Unit/
├── MoneyTests.cs           # Composite value object, currency operation tests
├── ProductCodeTests.cs     # Format validation, parsing tests
├── QuantityTests.cs        # Arithmetic operation, comparison tests
├── OrderStatusTests.cs     # State transition rule tests
└── ShippingAddressTests.cs # Multi-field validation tests
```

### Key Test Cases

| Test Class | Test Content |
|-------------|-----------|
| MoneyTests | Creation validation, same currency operations, different currency operation prohibition |
| ProductCodeTests | Format validation, category/number parsing |
| QuantityTests | Range validation, +/- operations, comparison operators |
| OrderStatusTests | State transition rules, cancellability |
| ShippingAddressTests | Required field validation, equality |

---

We have implemented the e-commerce domain value objects. In the next chapter, we cover value objects in the finance domain requiring precise calculations such as account numbers, interest rates, and exchange rates.

→ [2장: 금융 도메인](../02-Finance-Domain/)
