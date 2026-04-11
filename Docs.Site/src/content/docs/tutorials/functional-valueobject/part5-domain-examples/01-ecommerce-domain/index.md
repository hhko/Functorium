---
title: "이커머스 도메인"
---
## Overview

`decimal price = 10000;` -- 이 금액은 원화인가, 달러인가? 상품 코드 `"invalid"`가 주문에 포함되면 어떻게 되는가? 이커머스 시스템에서 원시 타입으로 비즈니스 개념을 표현하면 통화 혼동, 형식 오류, 잘못된 상태 전이가 runtime까지 발견되지 않습니다.

In this chapter, Part 1~4에서 학습한 패턴과 기법을 실제 이커머스 도메인에 적용하여, 이러한 문제를 타입 시스템으로 방지하는 5가지 value object를 implements.

- **Money**: 금액과 통화를 함께 관리하는 복합 value object
- **ProductCode**: 상품 코드 형식을 검증하는 단일 value object
- **Quantity**: 수량을 표현하며 정렬과 연산이 가능한 비교 가능 value object
- **OrderStatus**: 주문 상태와 전이 규칙을 표현하는 타입 안전 enumeration
- **ShippingAddress**: 배송 주소를 표현하는 복합 value object

## Learning Objectives

### **핵심 학습 목표**
- Money처럼 여러 속성을 가진 value object에서 Add, Subtract 같은 **도메인 연산을 구현할 수** 있습니다.
- ProductCode처럼 정규식을 사용하여 **비즈니스 형식을 검증할 수** 있습니다.
- OrderStatus에서 SmartEnum을 활용한 **상태 머신을 구현할 수** 있습니다.
- ShippingAddress처럼 여러 필드를 **순차적으로 검증할 수** 있습니다.

### **실습을 통해 확인할 내용**
- Money의 통화별 연산 제한과 금액 계산
- ProductCode의 카테고리-번호 파싱
- Quantity의 산술 operator overloading
- OrderStatus의 유효한 상태 전이 검증
- ShippingAddress의 다중 필드 검증

## Why Is This Needed?

이커머스 시스템은 금액, 수량, 상품 코드 등 다양한 비즈니스 개념을 다룹니다. 이러한 개념들을 원시 타입으로 표현하면 여러 문제가 발생합니다.

`decimal price = 10000;`으로 표현하면 이것이 원화인지 달러인지 알 수 없습니다. Money value object는 금액과 통화를 함께 관리하여 다른 통화 간의 잘못된 연산을 방지합니다. 상품 코드가 `string`이면 어디서든 `"invalid"` 같은 값이 할당될 수 있는데, ProductCode value object는 생성 시점에 형식을 검증하여 항상 유효한 형식만 존재하게 합니다. 주문 상태를 `string`이나 `enum`으로만 관리하면 "배송 완료" 상태에서 "대기 중"으로 변경되는 비정상적 전이가 가능한데, OrderStatus는 유효한 전이만 허용하는 상태 머신을 implements.

## Core Concepts

### Money (금액)

Money는 금액(Amount)과 통화(Currency)를 함께 관리하는 복합 value object입니다. 같은 통화끼리만 연산이 가능합니다.

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
            throw new InvalidOperationException("다른 통화끼리 합산할 수 없습니다.");
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("다른 통화끼리 뺄 수 없습니다.");
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

Add, Subtract 같은 연산은 통화 일치를 먼저 검증합니다. USD와 KRW를 더하려는 시도는 runtime에 예외를 발생시킵니다. compile time 방지를 위해 제네릭 통화 타입을 사용할 수도 있지만, 실무에서는 runtime 검증이 더 유연합니다.

### ProductCode (상품 코드)

ProductCode는 `"EL-001234"` 형식의 상품 코드를 검증합니다. 카테고리(2자리 영문)와 번호(6자리 숫자)를 파싱하는 기능도 provides.

```csharp
public sealed class ProductCode : SimpleValueObject<string>
{
    private ProductCode(string value) : base(value) { }

    public string Code => Value;  // protected Value에 대한 public 접근자
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

유효한 ProductCode만 존재할 수 있으므로, `Category`와 `Number` 속성은 항상 안전하게 접근할 수 있습니다. 형식 검증과 파싱이 하나의 value object에 결합된 패턴입니다.

### Quantity (수량)

Quantity는 비교 가능하고 산술 연산이 가능한 value object입니다. 음수와 최대 한계를 검증합니다.

```csharp
public sealed class Quantity : ComparableSimpleValueObject<int>
{
    private Quantity(int value) : base(value) { }

    public int Amount => Value;  // protected Value에 대한 public 접근자

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

operator overloading 덕분에 `qty1 + qty2`, `qty1 > qty2` 같은 표현이 가능하여 도메인 로직이 직관적입니다.

### OrderStatus (주문 상태)

OrderStatus는 SmartEnum을 사용한 타입 안전 enumeration입니다. 각 상태의 속성(취소 가능 여부)과 전이 규칙을 캡슐화합니다.

```csharp
public sealed class OrderStatus : SmartEnum<OrderStatus, string>
{
    public sealed record AlreadyCancelled : DomainErrorType.Custom;
    public sealed record AlreadyDelivered : DomainErrorType.Custom;
    public sealed record CannotRevertToPending : DomainErrorType.Custom;

    public static readonly OrderStatus Pending = new("PENDING", "대기중", canCancel: true);
    public static readonly OrderStatus Confirmed = new("CONFIRMED", "확인됨", canCancel: true);
    public static readonly OrderStatus Shipped = new("SHIPPED", "배송중", canCancel: false);
    public static readonly OrderStatus Delivered = new("DELIVERED", "배송완료", canCancel: false);
    public static readonly OrderStatus Cancelled = new("CANCELLED", "취소됨", canCancel: false);

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

취소된 주문은 상태를 변경할 수 없고, 대기 중 상태로 되돌릴 수 없는 등의 business rule이 value object 내부에 정의됩니다. 상태 전이 규칙이 캡슐화되어 있으므로, 외부에서 잘못된 전이를 시도하면 도메인 오류가 반환됩니다.

### ShippingAddress (배송 주소)

ShippingAddress는 수령인, 도로명, 도시, 우편번호, 국가를 포함하는 복합 value object입니다.

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

각 필드를 순서대로 검증하고, 첫 번째 오류에서 즉시 returns. 다중 필드의 순차 검증 패턴을 보여주는 대표적인 예시입니다.

## Practical Guidelines

### Expected Output
```
=== 이커머스 도메인 값 객체 ===

1. Money (금액) - ComparableValueObject
────────────────────────────────────────
   상품 가격: 10,000 KRW
   할인 금액: 1,000 KRW
   최종 가격: 9,000 KRW
   다른 통화 합산 시도: 다른 통화끼리 합산할 수 없습니다.

2. ProductCode (상품 코드) - SimpleValueObject
────────────────────────────────────────
   상품 코드: EL-001234
   카테고리: EL
   번호: 001234
   잘못된 형식: 상품 코드 형식이 올바르지 않습니다. (예: EL-001234)

3. Quantity (수량) - ComparableSimpleValueObject
────────────────────────────────────────
   수량 1: 5
   수량 2: 3
   합계: 8
   비교: 5 > 3 = True
   정렬: [1, 3, 5]

4. OrderStatus (주문 상태) - SmartEnum
────────────────────────────────────────
   현재 상태: 대기중
   취소 가능: True
   전이 후: 확인됨
   배송 중: 배송중, 취소 가능: False

5. ShippingAddress (배송 주소) - ValueObject
────────────────────────────────────────
   수령인: 홍길동
   주소: 테헤란로 123, 서울
   우편번호: 06234
   국가: KR

   빈 주소 검증 결과: 수령인 이름이 비어있습니다.
```

## Project Description

### Project Structure
```
01-Ecommerce-Domain/
├── EcommerceDomain/
│   ├── Program.cs                  # 메인 실행 파일 (5개 값 객체 구현)
│   └── EcommerceDomain.csproj      # 프로젝트 파일
└── README.md                       # 프로젝트 문서
```

### 의존성
```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\..\..\Src\Functorium\Functorium.csproj" />
</ItemGroup>

<ItemGroup>
  <PackageReference Include="Ardalis.SmartEnum" />
</ItemGroup>
```

### value object별 프레임워크 타입

The following table 각 value object가 어떤 프레임워크 기반 타입을 상속하고 어떤 특징을 갖는지 정리한 것입니다.

| value object | 프레임워크 타입 | 특징 |
|--------|---------------|------|
| Money | ValueObject + IComparable | 복합 값, 동일 통화 연산 |
| ProductCode | SimpleValueObject\<string\> | 형식 검증, 파싱 |
| Quantity | ComparableSimpleValueObject\<int\> | operator overloading |
| OrderStatus | SmartEnum | 상태 전이 규칙 |
| ShippingAddress | ValueObject | 다중 필드 검증 |

## Summary at a Glance

### 이커머스 value object 요약

각 value object의 속성, 검증 규칙, 도메인 연산을 한눈에 비교할 수 있습니다.

| value object | 주요 속성 | 검증 규칙 | 도메인 연산 |
|--------|----------|----------|------------|
| Money | Amount, Currency | 음수 금액 불가, 3자리 통화 코드 | Add, Subtract, Multiply |
| ProductCode | Value | XX-NNNNNN 형식 | Category, Number 파싱 |
| Quantity | Value | 0~10000 범위 | +, -, 비교 연산 |
| OrderStatus | Value, DisplayName | 유효 상태만 | TransitionTo |
| ShippingAddress | 5개 필드 | 모든 필드 필수 | 없음 |

### 검증 패턴 비교

이커머스 도메인에서 사용된 검증 패턴을 유형별로 분류하면 다음과 같습니다.

| 패턴 | value object | Description |
|------|--------|------|
| 단일 조건 검증 | Quantity | 범위 체크 |
| 정규식 검증 | ProductCode | 형식 패턴 매칭 |
| 다중 필드 순차 검증 | ShippingAddress | 각 필드 순서대로 검증 |
| 복합 조건 검증 | Money | 금액과 통화 각각 검증 |
| 상태 전이 검증 | OrderStatus | 현재-목표 상태 조합 검증 |

## FAQ

### Q1: Money에서 다른 통화 간 연산을 지원하려면?

환율 변환 서비스를 주입받아 변환 후 연산하는 방법을 uses. 또는 별도의 MoneyConverter 도메인 서비스를 만들어 두 Money 객체를 같은 통화로 변환한 후 연산하도록 설계할 수 있습니다.

```csharp
public Money ConvertTo(string targetCurrency, IExchangeRateService rateService)
{
    if (Currency == targetCurrency)
        return this;

    var rate = rateService.GetRate(Currency, targetCurrency);
    return new Money(Amount * rate, targetCurrency);
}
```

### Q2: OrderStatus의 상태 전이를 더 복잡하게 관리하려면?

상태 머신 라이브러리(Stateless 등)를 사용하거나, 별도의 OrderStatusTransition value object를 만들어 전이 규칙을 명시적으로 관리할 수 있습니다.

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

### Q3: Quantity에서 음수 결과를 허용하려면?

현재 구현은 Subtract에서 `Math.Max(0, ...)`로 음수를 방지합니다. 음수를 허용하려면 별도의 `SignedQuantity` 타입을 만들거나, 결과를 `Fin<T>`로 반환하는 방법이 있습니다.

```csharp
// 방법 1: 음수 허용 버전
public Quantity SubtractAllowNegative(Quantity other) =>
    new(Value - other.Value);

// 방법 2: 결과를 Fin<T>로 반환
public Fin<Quantity> SafeSubtract(Quantity other)
{
    var result = Value - other.Value;
    return result >= 0
        ? new Quantity(result)
        : DomainError.For<Quantity, int>(new DomainErrorType.Negative(), result,
            $"Result would be negative. Current: '{Value}', Other: '{other.Value}'");
}
```

이커머스 도메인의 value object 구현을 살펴보았습니다. Next chapter에서는 계좌번호, 이자율, 환율 등 정확성과 보안이 특히 중요한 금융 도메인의 value object를 implements.

---

## 테스트

이 프로젝트에는 단위 테스트가 포함되어 있습니다.

### 테스트 실행
```bash
cd EcommerceDomain.Tests.Unit
dotnet test
```

### 테스트 구조
```
EcommerceDomain.Tests.Unit/
├── MoneyTests.cs           # 복합 값 객체, 통화 연산 테스트
├── ProductCodeTests.cs     # 형식 검증, 파싱 테스트
├── QuantityTests.cs        # 산술 연산, 비교 테스트
├── OrderStatusTests.cs     # 상태 전이 규칙 테스트
└── ShippingAddressTests.cs # 다중 필드 검증 테스트
```

### 주요 테스트 케이스

| 테스트 클래스 | 테스트 내용 |
|-------------|-----------|
| MoneyTests | 생성 검증, 동일 통화 연산, 다른 통화 연산 금지 |
| ProductCodeTests | 형식 검증, 카테고리/번호 파싱 |
| QuantityTests | 범위 검증, +/- 연산, 비교 연산자 |
| OrderStatusTests | 상태 전이 규칙, 취소 가능 여부 |
| ShippingAddressTests | 필수 필드 검증, 동등성 |

---

이커머스 도메인의 value object를 구현했습니다. Next chapter에서는 금융 도메인에서 계좌번호, 이자율, 환율 등 정밀한 계산이 필요한 value object를 다룹니다.

→ [2장: 금융 도메인](../02-Finance-Domain/)
