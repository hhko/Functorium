---
title: "타입 안전 열거형"
---

## 개요

통화 코드마다 고유한 기호($, EUR, ₩)와 포맷팅 규칙이 필요한데, 기존 C# enum으로는 각 값에 속성이나 동작을 정의할 수 없습니다. `Ardalis.SmartEnum`을 사용하면 각 열거형 값이 독립적인 객체로 동작하여 타입 안전성과 도메인 표현력을 동시에 확보할 수 있습니다.

## 학습 목표

이 장을 마치면 다음을 할 수 있습니다.

1. `Ardalis.SmartEnum`을 활용하여 **타입 안전한 열거형을** 구현할 수 있습니다
2. 컴파일 타임 타입 검증과 풍부한 도메인 로직으로 **타입 안전성과 도메인 표현력을** 향상시킬 수 있습니다
3. SmartEnum을 활용한 복합 값 객체 구현으로 **도메인 모델의 일관성과 확장성을** 확보할 수 있습니다

## 왜 필요한가?

이전 단계 `ValueObject-Framework`에서는 기본적인 값 객체 프레임워크를 도입했습니다. 그러나 복잡한 도메인 개념을 모델링하면서 기존 C# enum의 한계가 드러났습니다. enum은 각 값에 추가 속성이나 동작을 정의할 수 없고, 문자열/정수 변환 시 타입 안전성을 보장하지 못하며, 도메인 로직이 enum 외부로 분산됩니다.

**SmartEnum은** 각 열거형 값을 독립적인 객체로 만들어, 고유한 속성과 메서드를 갖게 합니다. 컴파일 타임 타입 안전성을 보장하면서도 도메인 지식을 해당 객체 내부에 캡슐화할 수 있습니다.

## 핵심 개념

### SmartEnum 기반 타입 안전한 열거형

기존 C# enum은 단순한 상수 집합입니다. SmartEnum은 각 값을 독립적인 클래스 인스턴스로 만들어 추가 속성과 메서드를 정의할 수 있게 합니다.

기존 enum과 SmartEnum의 구현 차이를 비교합니다.

```csharp
// 기존 방식 (문제가 있는 방식) - 추가 속성 정의 불가
public enum Currency
{
    USD, EUR, KRW  // 기호나 포맷팅 규칙을 정의할 수 없음
}

// 개선된 방식 (현재 방식) - 각 값이 독립적인 객체
public sealed class Currency : SmartEnum<Currency, string>
{
    public static readonly Currency USD = new(nameof(USD), "USD", "$", "미국 달러");
    public static readonly Currency EUR = new(nameof(EUR), "EUR", "€", "유로");

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

### 컴파일 타임 타입 안전성

SmartEnum은 컴파일 시점에 타입 검증을 수행합니다. 기존 enum은 문자열이나 정수 변환 시 유효하지 않은 값이 런타임에만 발견되지만, SmartEnum은 정적 인스턴스를 통해 유효한 값만 사용하도록 강제합니다.

```csharp
// 기존 방식 (문제가 있는 방식) - 런타임에만 오류 발견
public enum Currency { USD, EUR, KRW }
string invalidCurrency = "INVALID"; // 컴파일 시점에 오류를 잡을 수 없음

// 개선된 방식 (현재 방식) - 컴파일 타임 타입 안전성
public sealed class Currency : SmartEnum<Currency, string>
{
    public static readonly Currency USD = new(nameof(USD), "USD", "$", "미국 달러");
    // 유효하지 않은 값은 컴파일 시점에 차단됨
}

// 타입 안전한 사용
Currency validCurrency = Currency.USD; // 컴파일 시점에 타입 검증
```

### 도메인 로직의 캡슐화

SmartEnum을 사용하면 각 열거형 값에 고유한 동작을 캡슐화할 수 있습니다. 도메인 로직이 외부 switch 문에 분산되지 않고 해당 객체 내부에 집중됩니다.

기존 switch 기반 분산 로직과 SmartEnum 캡슐화 방식의 차이를 보여줍니다.

```csharp
// 기존 방식 (문제가 있는 방식) - 도메인 로직이 분산됨
public enum Currency { USD, EUR, KRW }
public string FormatCurrency(Currency currency, decimal amount)
{
    return currency switch
    {
        Currency.USD => $"${amount:N2}",
        Currency.EUR => $"€{amount:N2}",
        Currency.KRW => $"₩{amount:N0}",
        _ => throw new ArgumentException("Unknown currency")
    };
}

// 개선된 방식 (현재 방식) - 도메인 로직이 캡슐화됨
public sealed class Currency : SmartEnum<Currency, string>
{
    public static readonly Currency USD = new(nameof(USD), "USD", "$", "미국 달러");
    public static readonly Currency EUR = new(nameof(EUR), "EUR", "€", "유로");
    public static readonly Currency KRW = new(nameof(KRW), "KRW", "₩", "한국 원화");

    public string Symbol { get; }
    public string Description { get; }

    private Currency(string name, string value, string symbol, string description)
        : base(name, value)
    {
        Symbol = symbol;
        Description = description;
    }

    public string FormatAmount(decimal amount) => $"{Symbol}{amount:N2}";
    public bool IsMajorCurrency() => Symbol == "$" || Symbol == "€";
    public decimal ConvertToBaseUnit(decimal amount) => amount; // 환율 변환 로직
}
```

새로운 통화를 추가할 때 기존 코드를 수정하지 않고 새 정적 인스턴스만 추가하면 됩니다.

다음 장에서는 단순한 문자열 에러 메시지를 넘어서, 구조화된 에러 코드 시스템을 구현합니다.

## 실전 지침

### 예상 출력
```
=== ValueObject Framework 데모 ===

   SmartEnum 기반 Currency와 PriceRange (가격 범위)
   SmartEnum을 사용한 타입 안전한 통화 처리와 PriceRange 조합

   📋 지원되는 통화 목록:
      - AUD (호주 달러) A$ (코드: AUD)
      - CAD (캐나다 달러) C$ (코드: CAD)
      - CHF (스위스 프랑) CHF (코드: CHF)
      - CNY (중국 위안) ¥ (코드: CNY)
      - EUR (유로) € (코드: EUR)
      - GBP (영국 파운드) £ (코드: GBP)
      - JPY (일본 엔) ¥ (코드: JPY)
      - KRW (한국 원화) ₩ (코드: KRW)
      - SGD (싱가포르 달러) S$ (코드: SGD)
      - USD (미국 달러) $ (코드: USD)

   ✅ 성공 (KRW): KRW (한국 원화) ₩ 10,000.00 ~ KRW (한국 원화) ₩ 50,000.00
   ✅ 성공 (USD): USD (미국 달러) $ 100.00 ~ USD (미국 달러) $ 500.00
   ✅ 성공 (EUR): EUR (유로) € 80.00 ~ EUR (유로) € 400.00

   🚫 실패 케이스들:
   ❌ 실패: 금액은 0 이상 999,999.99 이하여야 합니다: -1000
   ❌ 실패: 금액은 0 이상 999,999.99 이하여야 합니다: -5000
   ❌ 실패: 최소 가격은 최대 가격보다 작거나 같아야 합니다: KRW (한국 원화) ₩ 50,000.00 > KRW (한국 원화) ₩ 10,000.00
   ❌ 실패: 통화 코드는 3자리 영문자여야 합니다: INVALID

   💰 SmartEnum Currency 직접 사용:
      KRW: KRW (한국 원화) ₩ - ₩12,345.67
      USD: USD (미국 달러) $ - $123.45
      EUR: EUR (유로) € - €89.12

   🔍 통화 지원 여부 확인:
      KRW 지원: True
      USD 지원: True
      INVALID 지원: False

   📊 비교 기능 데모:
   - KRW (한국 원화) ₩ 10,000.00 ~ KRW (한국 원화) ₩ 30,000.00 < KRW (한국 원화) ₩ 20,000.00 ~ KRW (한국 원화) ₩ 40,000.00 = True
   - KRW (한국 원화) ₩ 10,000.00 ~ KRW (한국 원화) ₩ 30,000.00 == KRW (한국 원화) ₩ 10,000.00 ~ KRW (한국 원화) ₩ 30,000.00 = True
   - KRW (한국 원화) ₩ 10,000.00 ~ KRW (한국 원화) ₩ 30,000.00 > KRW (한국 원화) ₩ 20,000.00 ~ KRW (한국 원화) ₩ 40,000.00 = False
   - KRW (한국 원화) ₩ 10,000.00 ~ KRW (한국 원화) ₩ 30,000.00 <= KRW (한국 원화) ₩ 10,000.00 ~ KRW (한국 원화) ₩ 30,000.00 = True
   - KRW (한국 원화) ₩ 10,000.00 ~ KRW (한국 원화) ₩ 30,000.00 >= KRW (한국 원화) ₩ 10,000.00 ~ KRW (한국 원화) ₩ 30,000.00 = True
   - KRW (한국 원화) ₩ 10,000.00 ~ KRW (한국 원화) ₩ 30,000.00 != KRW (한국 원화) ₩ 20,000.00 ~ KRW (한국 원화) ₩ 40,000.00 = True

   📋 개별 값 객체 생성:
   - MinPrice: USD (미국 달러) $ 15,000.00 (금액: 15000)
   - MaxPrice: USD (미국 달러) $ 35,000.00 (금액: 35000)
   - Currency: USD (미국 달러) $ (값: USD)
   - PriceRange from validated: USD (미국 달러) $ 15,000.00 ~ USD (미국 달러) $ 35,000.00

   🔄 Price 비교 기능 데모:
   📊 같은 통화 (USD) 비교:
      - USD (미국 달러) $ 100.00 < USD (미국 달러) $ 200.00 = True
      - USD (미국 달러) $ 100.00 == USD (미국 달러) $ 100.00 = True
      - USD (미국 달러) $ 100.00 > USD (미국 달러) $ 200.00 = False
      - CanCompareWith: True = True

   🌍 다른 통화 비교:
      - USD vs KRW: USD (미국 달러) $ 100.00 vs KRW (한국 원화) ₩ 100,000.00
      - CanCompareWith: False = False
      - 비교 결과: False (통화 우선 비교)
      - USD vs EUR: USD (미국 달러) $ 100.00 vs EUR (유로) € 80.00
      - CanCompareWith: False = False
      - 비교 결과: False (통화 우선 비교)

   🛡️ 안전한 비교 유틸리티:
      - USD (미국 달러) $ 100.00 < USD (미국 달러) $ 200.00
      - 서로 다른 통화는 비교할 수 없습니다: USD (미국 달러) $ vs KRW (한국 원화) ₩
      - 서로 다른 통화는 비교할 수 없습니다: KRW (한국 원화) ₩ vs EUR (유로) €

   📈 가격 정렬 데모 (통화 우선, 금액 순):
      1. EUR (유로) € 80.00
      2. KRW (한국 원화) ₩ 100,000.00
      3. USD (미국 달러) $ 100.00
      4. USD (미국 달러) $ 100.00
      5. USD (미국 달러) $ 200.00
      - CanCompareWith: False = False
      - 비교 결과: False (통화 우선 비교)

   🛡️ 안전한 비교 유틸리티:
      - USD (미국 달러) $ 100.00 < USD (미국 달러) $ 200.00
      - 서로 다른 통화는 비교할 수 없습니다: USD (미국 달러) $ vs KRW (한국 원화) ₩

   📈 가격 정렬 데모 (통화 우선, 금액 순):
      1. EUR (유로) € 80.00
      2. KRW (한국 원화) ₩ 100,000.00
      3. USD (미국 달러) $ 100.00
      4. USD (미국 달러) $ 100.00
      5. USD (미국 달러) $ 200.00
```

### 핵심 구현 포인트
1. **SmartEnum Currency 구현**: 각 통화 코드를 독립적인 객체로 정의하고 고유한 기호와 포맷팅 규칙을 구현
2. **타입 안전성 보장**: 컴파일 타임에 유효하지 않은 통화 코드 사용을 방지하는 타입 시스템 구축
3. **도메인 로직 캡슐화**: 각 통화의 고유한 특성과 동작을 해당 객체 내부에 캡슐화하여 응집도 향상

## 프로젝트 설명

### 프로젝트 구조
```
TypeSafeEnums/                    # 메인 프로젝트
├── Program.cs                    # 메인 실행 파일
├── TypeSafeEnums.csproj         # 프로젝트 파일
├── README.md                    # 메인 문서
└── ValueObjects/                # 값 객체 구현
    └── Comparable/
        └── CompositeValueObjects/
            ├── Currency.cs      # SmartEnum 기반 통화
            ├── MoneyAmount.cs   # 금액 값 객체
            ├── Price.cs         # 가격 복합 값 객체
            └── PriceRange.cs    # 가격 범위 복합 값 객체
```

### 핵심 코드

#### SmartEnum 기반 Currency 구현
```csharp
public sealed class Currency : SmartEnum<Currency, string>
{
    public static readonly Currency USD = new(nameof(USD), "USD", "$", "미국 달러");
    public static readonly Currency EUR = new(nameof(EUR), "EUR", "€", "유로");
    public static readonly Currency KRW = new(nameof(KRW), "KRW", "₩", "한국 원화");
    // ... 10개 통화 정의

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
            : DomainErrors.Empty(currencyCode);

    private static Validation<Error, string> ValidateThreeLetters(string currencyCode) =>
        currencyCode.Length == 3 && currencyCode.All(char.IsLetter)
            ? ValidateSupported(currencyCode)
            : DomainErrors.NotThreeLetters(currencyCode);

    private static Validation<Error, string> ValidateSupported(string currencyCode) =>
        GetAllSupportedCurrencies().Any(c => c.GetCode() == currencyCode)
            ? currencyCode
            : DomainErrors.Unsupported(currencyCode);
}
```

#### 개선된 Price 비교 로직
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
        yield return Currency.Value;    // 통화를 먼저 비교
        yield return (decimal)Amount;   // 금액을 나중에 비교
    }

    public bool CanCompareWith(Price other) => Currency.Equals(other.Currency);

    public override string ToString() => $"{Currency} {Amount}";
}
```

#### LINQ Expression 기반 PriceRange 검증
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
            : DomainErrors.MinExceedsMax(minPrice, maxPrice);

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return (decimal)MinPrice.Amount;
        yield return (decimal)MaxPrice.Amount;
    }

    public override string ToString() => $"{MinPrice} ~ {MaxPrice}";
}
```

## 한눈에 보는 정리

### 비교 표

기존 C# enum과 SmartEnum 방식의 차이를 요약합니다.

| 구분 | 이전 방식 | 현재 방식 |
|------|-----------|-----------|
| **열거형 구현** | 기본 C# enum (속성 정의 불가) | SmartEnum (각 값이 독립 객체) |
| **타입 안전성** | 런타임에만 오류 발견 | 컴파일 타임 타입 검증 |
| **도메인 표현력** | 단순한 상수만 제공 | 풍부한 속성과 동작 정의 |
| **확장성** | 새 값 추가 시 기존 코드 수정 필요 | 개방-폐쇄 원칙 준수 |
| **캡슐화** | 도메인 로직이 외부에 분산 | 도메인 지식이 객체 내부에 집중 |

### 장단점 표

SmartEnum 도입의 트레이드오프를 정리합니다.

| 장점 | 단점 |
|------|------|
| 컴파일 타임 타입 검증 | 새로운 라이브러리 학습 필요 |
| 각 값의 고유 속성과 동작 정의 | 외부 라이브러리(Ardalis.SmartEnum) 의존 |
| 개방-폐쇄 원칙으로 확장 용이 | 단순한 enum 대비 구현 복잡도 증가 |
| 도메인 지식이 객체 내부에 집중 | 객체 생성 오버헤드 (미미한 수준) |

## FAQ

### Q1: SmartEnum과 기존 enum을 어떻게 구분해서 사용하나요?
**A**: 단순한 상태나 플래그(주문 상태, 사용자 권한 등)는 기존 enum으로 충분합니다. 각 값마다 고유한 속성이나 동작이 필요한 경우(통화별 기호/포맷팅, 주문 타입별 계산 로직 등)에 SmartEnum을 사용합니다.

### Q2: SmartEnum의 성능 오버헤드가 있나요?
**A**: 각 통화 코드마다 하나의 정적 인스턴스만 생성되므로 메모리 오버헤드는 미미합니다. 대부분의 애플리케이션에서 타입 안전성과 도메인 표현력 향상의 이점이 성능 비용보다 훨씬 큽니다.

### Q3: SmartEnum 도입 시 주의사항은?
**A**: 팀의 학습 곡선, 외부 라이브러리 의존성 관리, 기존 enum 대비 구현 복잡도 증가를 고려해야 합니다. 도메인 복잡성이 높아질수록 SmartEnum의 이점이 더 명확해지므로, 단순한 열거형에는 기존 enum을 유지하는 것이 합리적입니다.

---

값 객체의 검증이 실패할 때 "왜 실패했는지"를 구조화된 에러 코드로 전달하면 디버깅과 모니터링이 크게 개선됩니다. 다음 장에서는 구조화된 에러 코드 시스템을 구현합니다.

→ [13장: 에러 코드](../13-Error-Code/)
