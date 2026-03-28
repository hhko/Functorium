---
title: "유형 값 객체"
---

## 개요

C#의 기존 `enum`에 `(Currency)999` 같은 유효하지 않은 값을 대입하면 컴파일 타임에 잡히지 않습니다. 통화별 기호나 한글 이름을 별도의 `Dictionary`로 관리해야 하고, 메서드나 속성을 추가할 수도 없습니다. SmartEnum은 이러한 한계를 극복하여 타입 안전성과 도메인 로직을 열거형 자체에 내장합니다.

## 학습 목표

1. Ardalis.SmartEnum 라이브러리를 활용하여 타입 안전한 열거형을 구현할 수 있습니다.
2. 열거형 값에 비즈니스 로직과 도메인 속성을 직접 포함시킬 수 있습니다.
3. LanguageExt의 `Validation<Error, T>`를 사용하여 함수형 검증 패턴을 적용할 수 있습니다.

## 왜 필요한가?

기존 C# enum은 세 가지 근본적인 한계가 있습니다.

유효하지 않은 값을 컴파일 타임에 방지할 수 없습니다.

```csharp
// 기존 enum의 문제점
public enum Currency { KRW, USD, EUR }

// 컴파일 타임에 잡히지 않는 오류
Currency currency = (Currency)999; // 유효하지 않은 값
```

통화별 기호나 이름 같은 도메인 로직을 enum 자체에 표현할 수 없어 별도로 관리해야 합니다.

```csharp
// 기존 enum으로는 복잡한 로직 표현이 어려움
public enum Currency { KRW, USD, EUR }

// 통화별 기호나 이름을 별도로 관리해야 함
private static readonly Dictionary<Currency, string> Symbols = new()
{
    { Currency.KRW, "₩" },
    { Currency.USD, "$" },
    { Currency.EUR, "€" }
};
```

메서드나 속성 추가, 상속이 불가능하여 확장성이 제한됩니다.

```csharp
// 새로운 속성 추가 시 기존 코드 수정 필요
// 메서드 추가 불가능
// 상속 불가능
```

## 핵심 개념

### SmartEnum 기본 구조

SmartEnum은 정적 인스턴스(`public static readonly`)로 각 열거형 값을 정의하고, private 생성자로 외부 생성을 방지합니다. 도메인에 필요한 속성들을 생성자에서 함께 초기화합니다.

```csharp
public sealed class Currency : SmartEnum<Currency, string>
{
    public static readonly Currency KRW = new(nameof(KRW), "KRW", "한국 원화", "₩");
    public static readonly Currency USD = new(nameof(USD), "USD", "미국 달러", "$");

    public string KoreanName { get; }
    public string Symbol { get; }

    private Currency(string name, string value, string koreanName, string symbol)
        : base(name, value)
    {
        KoreanName = koreanName;
        Symbol = symbol;
    }
}
```

### 함수형 검증 패턴

`Bind`를 사용한 체이닝으로 의존성이 있는 검증 단계들을 순차적으로 실행합니다. 각 단계에서 실패하면 이후 단계는 실행되지 않습니다.

```csharp
public static Validation<Error, string> Validate(string currencyCode) =>
    ValidateNotEmpty(currencyCode)
        .Bind(ValidateFormat)
        .Bind(ValidateSupported);
```

### 도메인 로직 내장

SmartEnum의 가장 큰 장점은 열거형 값에 비즈니스 로직을 직접 포함할 수 있다는 점입니다. 포맷팅, 계산, 검증 같은 도메인 특화 기능을 열거형 클래스 안에 정의합니다.

```csharp
public string FormatAmount(decimal amount) => $"{Symbol}{amount:N2}";
public string FormatAmountWithoutDecimals(decimal amount) => $"{Symbol}{amount:N0}";
```

## 실전 지침

### SmartEnum 구현 패턴

SmartEnum 구현 시 다음 세 가지 영역을 구분합니다.

| 영역 | 패턴 | 설명 |
|------|------|------|
| **인스턴스 정의** | `public static readonly` | 각 열거형 값 정의 |
| **검증 로직** | `Bind` 체이닝 | 순차 검증 + 구체적 에러 메시지 |
| **비즈니스 로직** | 인스턴스 메서드 | 포맷팅, 계산 등 도메인 로직 |

## 프로젝트 구조

```
07-TypeSafeEnum/
├── TypeSafeEnum/
│   ├── ValueObjects/
│   │   └── Currency.cs          # SmartEnum 기반 통화 열거형
│   ├── Program.cs               # 데모 프로그램
│   └── TypeSafeEnum.csproj      # 프로젝트 파일
└── README.md                    # 프로젝트 문서
```

## 핵심 코드

### Currency SmartEnum 구현

`Currency`는 SmartEnum을 상속하고 `IValueObject`를 구현하여, 프레임워크의 값 객체 규칙(`Create`, `Validate`, `DomainError.For<T>()`)을 준수합니다.

```csharp
public sealed class Currency : SmartEnum<Currency, string>, IValueObject
{
    public sealed record Unsupported : DomainErrorType.Custom;

    // 정적 인스턴스들
    public static readonly Currency KRW = new(nameof(KRW), "KRW", "한국 원화", "₩");
    public static readonly Currency USD = new(nameof(USD), "USD", "미국 달러", "$");

    // 도메인 속성
    public string KoreanName { get; }
    public string Symbol { get; }

    // Private 생성자
    private Currency(string name, string value, string koreanName, string symbol)
        : base(name, value)
    {
        KoreanName = koreanName;
        Symbol = symbol;
    }

    // 팩토리 메서드
    public static Fin<Currency> Create(string currencyCode) =>
        Validate(currencyCode).Map(FromValue).ToFin();

    // 검증 로직 - DomainError.For<T>() 패턴
    public static Validation<Error, string> Validate(string currencyCode) =>
        ValidateNotEmpty(currencyCode)
            .Bind(ValidateFormat)
            .Bind(ValidateSupported);

    // 개별 검증 메서드들 - DomainError.For<T>() 패턴 적용
    private static Validation<Error, string> ValidateNotEmpty(string currencyCode) =>
        !string.IsNullOrWhiteSpace(currencyCode)
            ? currencyCode
            : DomainError.For<Currency>(new DomainErrorType.Empty(), currencyCode,
                $"Currency code cannot be empty. Current value: '{currencyCode}'");

    private static Validation<Error, string> ValidateFormat(string currencyCode) =>
        currencyCode.Length == 3 && currencyCode.All(char.IsLetter)
            ? currencyCode.ToUpperInvariant()
            : DomainError.For<Currency>(new DomainErrorType.WrongLength(), currencyCode,
                $"Currency code must be exactly 3 letters. Current value: '{currencyCode}'");

    // 비즈니스 로직
    public string FormatAmount(decimal amount) => $"{Symbol}{amount:N2}";
}
```

### 대안: SimpleValueObject\<string> + HashMap 패턴

SmartEnum 외에도 프레임워크의 `SimpleValueObject<string>`과 LanguageExt의 `HashMap`을 조합하여 타입 안전한 열거형을 구현할 수 있습니다. 이 패턴은 외부 라이브러리 의존성 없이 프레임워크만으로 구현 가능합니다.

> 참고: `Tests.Hosts/01-SingleHost`의 `OrderStatus` 구현

```csharp
public sealed class OrderStatus : SimpleValueObject<string>
{
    public sealed record InvalidValue : DomainErrorType.Custom;

    // 정적 인스턴스들
    public static readonly OrderStatus Pending = new("Pending");
    public static readonly OrderStatus Confirmed = new("Confirmed");
    public static readonly OrderStatus Shipped = new("Shipped");
    public static readonly OrderStatus Delivered = new("Delivered");
    public static readonly OrderStatus Cancelled = new("Cancelled");

    // HashMap을 사용한 유효값 목록
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

두 접근 방식의 차이를 정리합니다.

**SmartEnum vs SimpleValueObject+HashMap 비교:**
| 특징 | SmartEnum | SimpleValueObject+HashMap |
|------|-----------|---------------------------|
| **외부 의존성** | Ardalis.SmartEnum 필요 | 프레임워크만 사용 |
| **도메인 속성** | 자유롭게 추가 가능 | 제한적 (별도 속성 관리) |
| **ValueObject 호환** | IValueObject 수동 구현 | 자동 상속 |
| **HashMap 조회** | 내장 FromValue/FromName | LanguageExt HashMap 사용 |

### 데모 프로그램
```csharp
// 기본 사용법
var krw = Currency.KRW;
var usd = Currency.FromValue("USD");

// 검증 기능
var result = Currency.Create("INVALID");
result.Match(
    Succ: currency => Console.WriteLine($"성공: {currency}"),
    Fail: error => Console.WriteLine($"실패: {error.Message}")
);

// 비즈니스 로직
Console.WriteLine(Currency.USD.FormatAmount(1000)); // $1,000.00
```

## 예상 출력

```
=== Type Safe Enum 데모 ===

1. 기본 사용법
================
KRW: KRW (한국 원화) ₩
USD: USD (미국 달러) $
EUR: EUR (유로) €
JPY from value: JPY (일본 엔) ¥
GBP from name: GBP (영국 파운드) £

2. 검증 기능
=============
✅ 유효한 통화: USD (미국 달러) $
❌ 에러: 통화 코드는 3자리 영문자여야 합니다: US
❌ 에러: 지원하지 않는 통화 코드입니다: XYZ
❌ 에러: 통화 코드는 비어있을 수 없습니다:

3. 비교 기능
=============
KRW == KRW: True
KRW == USD: False
KRW < USD: True
USD > EUR: False
KRW HashCode: 1234567890
USD HashCode: 9876543210

4. 비즈니스 로직
=================
KRW: ₩1,000.00
KRW: ₩1,000
USD: $1,000.00
USD: $1,000
EUR: €1,000.00
EUR: €1,000
JPY: ¥1,000.00
JPY: ¥1,000

5. 에러 처리
=============
✅ USD → USD (미국 달러) $
❌ INVALID → 지원하지 않는 통화 코드입니다: INVALID
❌ KR → 통화 코드는 3자리 영문자여야 합니다: KR
❌ XYZ → 지원하지 않는 통화 코드입니다: XYZ
✅ EUR → EUR (유로) €

6. 모든 지원 통화
==================
총 10개 통화 지원:
  - KRW (한국 원화) ₩
  - USD (미국 달러) $
  - EUR (유로) €
  - JPY (일본 엔) ¥
  - CNY (중국 위안) ¥
  - GBP (영국 파운드) £
  - AUD (호주 달러) A$
  - CAD (캐나다 달러) C$
  - CHF (스위스 프랑) CHF
  - SGD (싱가포르 달러) S$

=== 데모 완료 ===
```

## 한눈에 보는 정리

기존 enum과 SmartEnum의 기능 차이를 비교합니다.

### SmartEnum vs 기존 Enum 비교
| 특징 | 기존 Enum | SmartEnum |
|------|-----------|-----------|
| **타입 안전성** | 제한적 | 완전한 타입 안전성 |
| **도메인 로직** | 별도 관리 필요 | 직접 내장 가능 |
| **속성 추가** | 불가능 | 자유롭게 추가 가능 |
| **메서드 추가** | 불가능 | 자유롭게 추가 가능 |
| **상속** | 불가능 | 가능 |
| **검증** | 수동 구현 필요 | 자동 제공 + 커스텀 가능 |
| **비교 기능** | 기본 제공 | 고급 비교 기능 제공 |

### 장단점 표
| 장점 | 단점 |
|------|------|
| **완전한 타입 안전성** | **외부 라이브러리 의존성** |
| **도메인 로직 내장** | **기존 enum 대비 복잡성** |
| **강력한 검증 기능** | **성능 오버헤드 (미미)** |
| **확장성과 유연성** | **학습 곡선** |

## FAQ

### Q1: SmartEnum과 기존 enum의 가장 큰 차이점은 무엇인가요?
**A**: SmartEnum은 도메인 로직을 직접 내장할 수 있습니다. 기존 enum은 단순한 정수/문자열 값만 저장할 수 있지만, SmartEnum은 속성, 메서드, 비즈니스 로직을 포함할 수 있습니다.

### Q2: 언제 SmartEnum을 사용해야 하나요?
**A**: 포맷팅, 계산, 검증 같은 도메인 로직이 필요하거나, 잘못된 값 입력을 타입 수준에서 방지해야 하거나, 새로운 속성이나 메서드 추가가 예상되는 열거형에 적합합니다.

### Q3: SmartEnum에서 ValueObject 규칙을 어떻게 준수하나요?
**A**: `IValueObject` 인터페이스를 구현하고, `DomainError.For<T>()` 패턴으로 구조화된 에러를 처리합니다. 커스텀 에러 타입은 `sealed record Unsupported : DomainErrorType.Custom` 형식으로 정의합니다.

```csharp
// ValueObject 규칙 준수 예시
private static Validation<Error, string> ValidateNotEmpty(string currencyCode) =>
    !string.IsNullOrWhiteSpace(currencyCode)
        ? currencyCode                    // 성공: 값 반환
        : DomainError.For<Currency>(      // 실패: DomainError.For<T>() 사용
            new DomainErrorType.Empty(), currencyCode,
            $"Currency code cannot be empty. Current value: '{currencyCode}'");
```

다음 장에서는 ArchUnitNET을 활용하여 지금까지 구현한 모든 값 객체가 아키텍처 규칙을 올바르게 준수하는지 자동으로 검증하는 방법을 다룹니다.

---

→ [8장: 아키텍처 테스트](../08-Architecture-Test/)
