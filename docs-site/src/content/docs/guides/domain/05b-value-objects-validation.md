---
title: "값 객체: 열거형·검증·실전 패턴"
---

이 문서는 값 객체의 열거형 패턴, 실전 예제, Application Layer 검증 병합, FAQ를 다룹니다. 핵심 개념과 기반 클래스는 [05a-value-objects.md](./05a-value-objects)을 참고하세요.

## 목차

- [요약](#요약)
- [열거형 구현 패턴](#열거형-구현-패턴)
- [실전 예제](#실전-예제)
- [Application Layer에서 VO 검증 병합](#application-layer에서-vo-검증-병합)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)
- [참고 문서](#참고-문서)

---

## 요약

### 주요 명령

```csharp
// SmartEnum Create 패턴
public static Fin<Currency> Create(string currencyCode) =>
    Validate(currencyCode).Map(FromValue).ToFin();

// SimpleValueObject Create 패턴
public static Fin<Email> Create(string? value) =>
    CreateFromValidation(Validate(value), v => new Email(v));

// Application Layer에서 Apply 병합
(name, description, price, stockQuantity)
    .Apply((n, d, p, s) => Product.Create(...))
    .As().ToFin();
```

### 주요 절차

**1. 값 객체 생성:**
1. 기반 클래스 선택 (`SimpleValueObject<T>`, `ValueObject`, `SmartEnum` 등)
2. `Validate()` 메서드로 검증 규칙 정의
3. `Create()` 메서드로 검증 + 생성 조합

**2. Application Layer 검증 병합:**
1. 각 필드의 `VO.Validate()` 호출 (Validation<Error, T> 반환)
2. `Apply`로 모든 검증 결과를 병렬 병합
3. 성공 시 Entity 생성, 실패 시 모든 오류 수집

### 주요 개념

| 개념 | 설명 |
|------|------|
| `SmartEnum` | 값마다 고유 속성/동작이 필요한 열거형 패턴 |
| `ValidationRules<T>` | Domain Layer에서 타입 기반 검증 규칙 체이닝 |
| `ValidationRules.For()` | VO 없는 필드의 문자열 기반 검증 (Named Context) |
| `Apply` 병합 | 독립적 검증을 병렬 수행하여 모든 오류를 수집 |
| `Bind`/`Then` 체이닝 | 의존적 검증을 순차 수행 (첫 오류에서 중단) |

---

## 열거형 구현 패턴

도메인에서 **고정된 선택지**(통화 종류, 주문 상태, 회원 등급 등)를 표현할 때 C# 기본 `enum` 대신 `Ardalis.SmartEnum`을 사용합니다.

**왜 SmartEnum인가요?**

C# 기본 `enum`은 단순한 정수 상수에 불과합니다:

- 값에 추가 속성(표시 이름, 기호 등)을 붙일 수 없음
- 값마다 다른 동작을 정의할 수 없음
- 유효하지 않은 값 캐스팅이 가능함 (`(Currency)999`)

SmartEnum은 이를 해결합니다:
- 각 값에 고유 속성과 동작 부여 가능
- 런타임 타입 안전성 보장
- Value Object처럼 검증 로직 포함 가능

`SmartEnum`은 `SimpleValueObject`를 상속받지 않으므로 Create 패턴이 약간 다릅니다.

### 기본 구조

```csharp
using Ardalis.SmartEnum;
using Functorium.Domains.ValueObjects;

public sealed class Currency : SmartEnum<Currency, string>, IValueObject
{
    public sealed record Unsupported : DomainErrorType.Custom;

    public static readonly Currency KRW = new(nameof(KRW), "KRW", "한국 원화", "₩");
    public static readonly Currency USD = new(nameof(USD), "USD", "미국 달러", "$");
    public static readonly Currency EUR = new(nameof(EUR), "EUR", "유로", "€");

    public string KoreanName { get; }
    public string Symbol { get; }

    private Currency(string name, string value, string koreanName, string symbol)
        : base(name, value)
    {
        KoreanName = koreanName;
        Symbol = symbol;
    }

    // SmartEnum 패턴: .Map(FromValue).ToFin()
    public static Fin<Currency> Create(string currencyCode) =>
        Validate(currencyCode)
            .Map(FromValue)
            .ToFin();

    public static Currency CreateFromValidated(string currencyCode) =>
        FromValue(currencyCode);

    public static Validation<Error, string> Validate(string currencyCode) =>
        ValidateNotEmpty(currencyCode)
            .Bind(ValidateFormat)
            .Bind(ValidateSupported);

    private static Validation<Error, string> ValidateNotEmpty(string currencyCode) =>
        string.IsNullOrWhiteSpace(currencyCode)
            ? DomainError.For<Currency>(new Empty(), currencyCode ?? "",
                $"Currency code cannot be empty")
            : currencyCode;

    private static Validation<Error, string> ValidateFormat(string currencyCode) =>
        currencyCode.Length != 3 || !currencyCode.All(char.IsLetter)
            ? DomainError.For<Currency>(new WrongLength(3), currencyCode,
                $"Currency code must be exactly 3 letters")
            : currencyCode.ToUpperInvariant();

    private static Validation<Error, string> ValidateSupported(string currencyCode)
    {
        try { FromValue(currencyCode); return currencyCode; }
        catch (SmartEnumNotFoundException)
        {
            return DomainError.For<Currency>(new Unsupported(), currencyCode,
                $"Currency code is not supported");
        }
    }

    public string FormatAmount(decimal amount) => $"{Symbol}{amount:N2}";
    public static IEnumerable<Currency> GetAllSupportedCurrencies() => List;
}
```

### Create 패턴 차이

| 기반 클래스 | Create 패턴 |
|------------|-------------|
| `SimpleValueObject<T>` | `CreateFromValidation(Validate(value), factory)` |
| `ComparableSimpleValueObject<T>` | `CreateFromValidation(Validate(value), factory)` |
| `ValueObject` | `CreateFromValidation(Validate(...), factory)` |
| `SmartEnum<T, TValue>` | `Validate(value).Map(FromValue).ToFin()` |

---

## 실전 예제

지금까지 설명한 패턴들을 적용한 완전한 예제입니다. 각 예제는 실제 프로젝트에서 그대로 사용할 수 있는 수준의 구현을 보여줍니다.

### Email (SimpleValueObject)

가장 흔한 패턴인 `SimpleValueObject<string>`의 완전한 예제입니다. 정규식 검증, 정규화(소문자 변환), 파생 속성(LocalPart, Domain)을 모두 포함합니다.

```csharp
using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations.Typed;
using System.Text.RegularExpressions;

public sealed class Email : SimpleValueObject<string>
{
    private static readonly Regex EmailPattern = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled);
    private const int MaxLength = 254;

    private Email(string value) : base(value)
    {
        var atIndex = value.IndexOf('@');
        LocalPart = value[..atIndex];
        Domain = value[(atIndex + 1)..];
    }

    public string LocalPart { get; }
    public string Domain { get; }

    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new Email(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<Email>.NotEmpty(value ?? "")
            .ThenMatches(EmailPattern)
            .ThenMaxLength(MaxLength)
            .ThenNormalize(v => v.ToLowerInvariant());

    public static implicit operator string(Email email) => email.Value;
}
```

### Quantity (ComparableSimpleValueObject)

비교 가능한 단일 값 객체의 예제입니다. 수량 비교(`q1 > q2`)와 정렬이 필요하며, 도메인 연산(Add, Subtract)과 편의 속성(IsZero, IsPositive)을 포함합니다.

```csharp
using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations.Typed;

public sealed class Quantity : ComparableSimpleValueObject<int>
{
    public const int MaxValue = 10000;

    private Quantity(int value) : base(value) { }

    public static Quantity Zero => new(0);
    public static Quantity One => new(1);

    public bool IsZero => Value == 0;
    public bool IsPositive => Value > 0;

    public static Fin<Quantity> Create(int value) =>
        CreateFromValidation(Validate(value), v => new Quantity(v));

    public static Validation<Error, int> Validate(int value) =>
        ValidationRules<Quantity>.NonNegative(value)
            .ThenAtMost(MaxValue);

    public Quantity Add(Quantity other) => new(Value + other.Value);
    public Quantity Subtract(Quantity other) => new(Math.Max(0, Value - other.Value));

    public static implicit operator int(Quantity q) => q.Value;
}
```

### Money (ValueObject with Apply)

복합 속성(Amount + Currency)으로 구성된 값 객체의 예제입니다. Apply 패턴으로 두 속성을 **병렬 검증**하고, 도메인 연산(Add)에서 **비즈니스 규칙 위반**(다른 통화 더하기)을 `DomainError.For<T>()`로 처리합니다.

```csharp
using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations;
using Functorium.Domains.ValueObjects.Validations.Typed;
using static Functorium.Domains.Errors.DomainErrorType;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    // Create: CreateFromValidation 헬퍼 사용
    public static Fin<Money> Create(decimal amount, string currency) =>
        CreateFromValidation(Validate(amount, currency), v => new Money(v.Amount, v.Currency));

    // Validate: 검증된 원시값 튜플 반환 (ValueObject 생성은 Create에서)
    public static Validation<Error, (decimal Amount, string Currency)> Validate(decimal amount, string currency) =>
        (ValidateAmount(amount), ValidateCurrency(currency))
            .Apply((a, c) => (Amount: a, Currency: c));

    private static Validation<Error, decimal> ValidateAmount(decimal amount) =>
        ValidationRules<Money>.NonNegative(amount);

    private static Validation<Error, string> ValidateCurrency(string currency) =>
        ValidationRules<Money>.NotEmpty(currency)
            .ThenExactLength(3)
            .ThenNormalize(v => v.ToUpperInvariant());

    public Fin<Money> Add(Money other) =>
        Currency == other.Currency
            ? new Money(Amount + other.Amount, Currency)
            : DomainError.For<Money, string, string>(
                new Mismatch(), Currency, other.Currency,
                $"Cannot add different currencies: {Currency} vs {other.Currency}");

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

---

## Application Layer에서 VO 검증 병합

Usecase에서 여러 ValueObject를 동시에 검증하고 Entity를 생성할 때 Apply 패턴을 사용합니다.

### Apply 병합 패턴 (Usecase 내부)

```csharp
private static Fin<Product> CreateProduct(Request request)
{
    // 1. 모든 필드: VO Validate() 호출 (Validation<Error, T> 반환)
    var name = ProductName.Validate(request.Name);
    var description = ProductDescription.Validate(request.Description);
    var price = Money.Validate(request.Price);
    var stockQuantity = Quantity.Validate(request.StockQuantity);

    // 2. Apply로 병렬 검증 후 Entity 생성
    return (name, description, price, stockQuantity)
        .Apply((n, d, p, s) => Product.Create(
            ProductName.Create(n).ThrowIfFail(),
            ProductDescription.Create(d).ThrowIfFail(),
            Money.Create(p).ThrowIfFail(),
            Quantity.Create(s).ThrowIfFail()))
        .As()
        .ToFin();
}
```

### 패턴 설명

| 단계 | 설명 |
|------|------|
| Validate() 호출 | 모든 필드의 검증을 Validation<Error, T>로 수집 |
| Apply 병합 | 모든 검증이 성공해야 Entity 생성 진행 |
| ThrowIfFail() | 이미 검증된 값이므로 안전하게 VO 변환 |

### VO가 없는 필드의 검증 (Named Context)

모든 필드가 Value Object로 정의되지 않을 경우 Named Context 검증을 사용합니다:

```csharp
private static Fin<Product> CreateProduct(Request request)
{
    // VO가 있는 필드
    var name = ProductName.Validate(request.Name);
    var price = Money.Validate(request.Price);

    // VO가 없는 필드: Named Context 사용
    var note = ValidationRules.For("Note")
        .NotEmpty(request.Note)
        .ThenMaxLength(500);

    // 모두 튜플로 병합 - Apply로 병렬 검증
    return (name, price, note.Value)
        .Apply((n, p, noteValue) =>
            Product.Create(
                ProductName.Create(n).ThrowIfFail(),
                noteValue,
                Money.Create(p).ThrowIfFail()))
        .As()
        .ToFin();
}
```

> **참고**: 자주 사용되는 필드는 Named Context 대신 별도의 ValueObject로 정의하는 것을 권장합니다.

---

## 트러블슈팅

### SmartEnum의 Create에서 `SmartEnumNotFoundException` 발생

**원인:** `FromValue()`에 등록되지 않은 값을 전달한 경우입니다. SmartEnum은 `static readonly` 필드로 등록된 값만 허용합니다.

**해결:** `Validate()` 메서드를 통해 지원 여부를 먼저 검증하세요. `ValidateSupported`에서 `try-catch`로 `SmartEnumNotFoundException`을 잡아 `DomainError`로 변환하는 패턴을 사용합니다.

```csharp
private static Validation<Error, string> ValidateSupported(string currencyCode)
{
    try { FromValue(currencyCode); return currencyCode; }
    catch (SmartEnumNotFoundException)
    {
        return DomainError.For<Currency>(new Unsupported(), currencyCode,
            $"Currency code is not supported");
    }
}
```

### Apply 병합 시 일부 검증 오류만 반환됨

**원인:** `Apply` 대신 `Bind`를 사용했거나, 검증 체인 내부에서 `Bind`(`Then*`)가 순차 실행되어 첫 오류에서 중단된 경우입니다.

**해결:** 독립적인 필드 간 검증은 반드시 `Apply`를 사용하세요. 각 필드 내부의 순차 검증(`NotEmpty → Matches → MaxLength`)은 `Then*`을 사용하되, 필드 간 병합은 튜플 + `Apply`로 처리합니다.

```csharp
// 필드 간 검증은 Apply (병렬)
(ValidateAmount(amount), ValidateCurrency(currency))
    .Apply((a, c) => new Money(a, c));
```

### ThrowIfFail()에서 예외 발생

**원인:** Apply 병합 후 Entity 생성 시 `ThrowIfFail()`을 호출하는 구간에서, Apply가 실패했는데 내부 팩토리 함수가 실행된 경우입니다. 이는 Apply가 성공했을 때만 팩토리 함수가 실행되므로 정상적으로는 발생하지 않습니다.

**해결:** `ThrowIfFail()`은 Apply 내부의 팩토리 함수에서만 사용하세요. Apply 외부에서 개별 `Fin<T>`에 대해 `ThrowIfFail()`을 직접 호출하면 검증 실패 시 예외가 발생합니다.

---

## FAQ

값 객체 구현 시 자주 받는 질문들입니다. 위 내용을 읽고도 헷갈리는 부분이 있다면 이 섹션을 참고하세요.

### Q1. 기반 클래스 선택 기준은?

값 객체를 만들 때 어떤 기반 클래스를 상속받을지 결정해야 합니다. 핵심 질문 두 가지로 쉽게 선택할 수 있습니다.

**첫 번째 질문: 값이 하나인가, 여러 개인가?**
- **하나의 값**만 감싸는 경우 → `SimpleValueObject<T>` 계열
  - 예: 이메일 주소(string 하나), 가격(decimal 하나), 사용자 ID(int 하나)
- **여러 속성**으로 구성된 경우 → `ValueObject` 계열
  - 예: 금액(amount + currency), 주소(city + street + postalCode), 좌표(x + y)

**두 번째 질문: 크기 비교가 필요한가?**
- **비교 필요 없음** → `SimpleValueObject<T>` 또는 `ValueObject`
  - 예: 이메일은 "어떤 게 더 크다"가 의미 없음
- **비교/정렬 필요** → `ComparableSimpleValueObject<T>` 또는 `ComparableValueObject`
  - 예: 가격은 "더 비싸다/싸다" 비교가 필요, 날짜 범위는 정렬이 필요

| 조건 | 선택 |
|------|------|
| 단일 값 래핑 | `SimpleValueObject<T>` |
| 단일 값 + 비교/정렬 필요 | `ComparableSimpleValueObject<T>` |
| 복합 속성 | `ValueObject` |
| 복합 속성 + 비교/정렬 필요 | `ComparableValueObject` |
| 열거형 + 도메인 로직 | `SmartEnum<T, TValue>` |

### Q2. ValidationRules\<T\>와 DomainError.For\<T\>() 사용 기준은?

두 가지 모두 검증 오류를 생성하지만, 용도가 다릅니다.

**ValidationRules\<T\>는 "일반적인 검증 규칙"에 사용합니다.**

"비어있으면 안 됨", "양수여야 함", "최대 100자" 같은 흔한 검증은 이미 구현되어 있어서 체이닝으로 간단히 사용할 수 있습니다.

```csharp
// 좋음: 일반적인 검증은 ValidationRules 사용
ValidationRules<Email>.NotEmpty(value)
    .ThenMaxLength(254)
    .ThenMatches(EmailPattern);
```

**DomainError.For\<T\>()는 "특수한 비즈니스 규칙"에 사용합니다.**

"통화가 서로 달라서 더할 수 없음", "재고가 부족함" 같은 도메인 특화 오류는 직접 생성해야 합니다.

```csharp
// 좋음: 비즈니스 규칙 위반은 DomainError.For 사용
return Currency == other.Currency
    ? new Money(Amount + other.Amount, Currency)
    : DomainError.For<Money, string, string>(
        new Mismatch(), Currency, other.Currency,
        $"Cannot add different currencies: {Currency} vs {other.Currency}");
```

| 상황 | 권장 |
|------|------|
| 일반적인 검증 | `ValidationRules<T>` + 체이닝 |
| 커스텀 비즈니스 규칙 | `ThenMust` 또는 `DomainError.For<T>()` |
| 도메인 연산 중 오류 | `DomainError.For<T>()` |

### Q3. Bind(Then)와 Apply 사용 기준은?

검증이 여러 개일 때, **오류를 어떻게 보여줄지**에 따라 선택합니다.

**Bind/Then은 "순차 검증"입니다. 첫 오류에서 멈춥니다.**

앞 검증이 실패하면 뒤 검증은 실행되지 않습니다. 검증 간에 의존 관계가 있을 때 사용합니다.

```csharp
// "비어있지 않아야" 통과해야 "이메일 형식 검사"가 의미 있음
ValidationRules<Email>.NotEmpty(value)    // 1. 빈 값이면 여기서 중단
    .ThenMatches(EmailPattern)            // 2. 1 통과 시에만 실행
    .ThenMaxLength(254);                  // 3. 2 통과 시에만 실행
```

**Apply는 "병렬 검증"입니다. 모든 오류를 수집합니다.**

각 검증이 독립적일 때 사용합니다. 사용자에게 한 번에 모든 문제를 알려줄 수 있어서 UX가 좋습니다.

```csharp
// amount와 currency 검증은 서로 독립적
// 둘 다 틀리면 두 오류 모두 반환
(ValidateAmount(amount), ValidateCurrency(currency))
    .Apply((a, c) => new Money(a, c));
```

**실제 예시로 비교:**

| 입력 | Bind 결과 | Apply 결과 |
|------|----------|------------|
| amount=-100, currency="" | "금액은 양수여야 합니다" (1개) | "금액은 양수여야 합니다", "통화 코드는 비어있을 수 없습니다" (2개) |

| 전략 | 사용 시점 | 특징 |
|------|----------|------|
| `Bind` / `Then*` | 검증 간 의존 관계 | 첫 오류에서 중단 |
| `Apply` | 독립적인 검증 | 모든 오류 수집 |

### Q4. Value 속성에 접근하려면?

`SimpleValueObject<T>`의 `Value` 속성은 `protected`로 선언되어 있어서 외부에서 직접 접근할 수 없습니다. 이는 의도적인 설계입니다 - 값 객체를 "원시 값처럼" 쓰는 것을 방지하고, 타입 안전성을 유지합니다.

외부에서 내부 값이 필요한 경우 세 가지 방법이 있습니다:

```csharp
// 방법 1: 암시적 변환 연산자 정의 (권장)
// Email을 string이 필요한 곳에 바로 전달 가능
public static implicit operator string(Email email) => email.Value;

string emailString = email;  // 암시적 변환
SendEmail(email);            // string 매개변수에 직접 전달

// 방법 2: 의미 있는 파생 속성 제공
// 단순히 Value를 노출하는 것보다 도메인 의미를 담은 속성이 좋음
public string LocalPart { get; }   // user@example.com에서 "user" 부분
public string Domain { get; }      // user@example.com에서 "example.com" 부분

// 방법 3: ToString() 오버라이드
// 디버깅이나 로깅에 유용
public override string ToString() => Value;
```

**참고**: 방법 1의 암시적 변환은 편리하지만, 남용하면 값 객체의 타입 안전성이 약해질 수 있습니다. 꼭 필요한 경우에만 사용하세요.

### Q5. 언제 SmartEnum을 사용하나요?

C#의 기본 `enum`은 단순한 정수 상수에 불과합니다. **값마다 다른 속성이나 동작이 필요하면** `SmartEnum`을 사용합니다.

**기본 enum으로 충분한 경우:**

```csharp
// 단순한 상태 구분만 필요
public enum OrderStatus { Pending, Confirmed, Shipped, Delivered }
```

**SmartEnum이 필요한 경우:**

```csharp
// 각 통화마다 고유한 속성(기호, 이름)과 동작(포맷팅)이 필요
public sealed class Currency : SmartEnum<Currency, string>
{
    public static readonly Currency KRW = new("KRW", "KRW", "₩", "한국 원화");
    public static readonly Currency USD = new("USD", "USD", "$", "미국 달러");

    public string Symbol { get; }
    public string DisplayName { get; }

    // 값마다 다른 동작
    public string Format(decimal amount) => $"{Symbol}{amount:N2}";
}
```

| 상황 | 선택 |
|------|------|
| 단순한 상태/플래그 | 기존 C# enum |
| 값마다 고유 속성 필요 | SmartEnum |
| 값마다 다른 동작 필요 | SmartEnum |
| 런타임 타입 안전성 중요 | SmartEnum |

### Q6. ValidationRules\<T\>와 ValidationRules.For() 차이점은?

둘 다 같은 검증 메서드(`NotEmpty`, `Positive` 등)를 제공하지만, **타입 정보를 어디서 가져오는지**가 다릅니다.

**ValidationRules\<T\>는 "타입"에서 컨텍스트를 가져옵니다.**

Value Object 클래스 내부에서 사용하며, 컴파일 타임에 타입이 결정됩니다.

```csharp
// Price 클래스 내부에서
public static Validation<Error, decimal> Validate(decimal value) =>
    ValidationRules<Price>.Positive(value);
// 오류 코드: DomainErrors.Price.NotPositive
```

**ValidationRules.For()는 "문자열"에서 컨텍스트를 가져옵니다.**

Value Object가 없는 상황(DTO 검증, API 입력 검증)에서 사용합니다.

```csharp
// DTO 검증에서
var result = ValidationRules.For("ProductPrice").Positive(request.Price);
// 오류 코드: DomainErrors.ProductPrice.NotPositive
```

**언제 어떤 것을 사용하나요?**

```csharp
// Domain Layer: 항상 ValidationRules<T> 사용
public sealed class Price : ComparableSimpleValueObject<decimal>
{
    public static Validation<Error, decimal> Validate(decimal value) =>
        ValidationRules<Price>.Positive(value);  // 타입 안전
}

// Application/Presentation Layer: ValidationRules.For() 사용 가능
public class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        // DTO 검증 - Value Object 없이 직접 검증
        RuleFor(x => x.Price)
            .Must(v => ValidationRules.For("Price").Positive(v).IsSuccess);
    }
}
```

| 특성 | `ValidationRules<T>` (Typed) | `ValidationRules.For()` (Contextual) |
|------|------------------------------|--------------------------------------|
| 네임스페이스 | `Validations.Typed` | `Validations.Contextual` |
| 타입 소스 | 컴파일 타임 (제네릭) | 런타임 (문자열) |
| 권장 레이어 | Domain Layer | Presentation/Application Layer |
| 사용 대상 | Value Object | DTO, API 입력, 프로토타이핑 |
| 예시 | `ValidationRules<Price>.Positive(v)` | `ValidationRules.For("Price").Positive(v)` |

**권장 사항:**
- Domain Layer에서는 항상 `ValidationRules<T>` 사용 (타입 안전성)
- DTO나 API 입력 검증에서는 `ValidationRules.For()` 사용 가능

---

## 참고 문서

- [에러 시스템: 기초와 네이밍](./08a-error-system) - 에러 처리 기본 원칙과 네이밍 규칙
- [에러 시스템: Domain/Application 에러](./08b-error-system-domain-app) - Domain/Application 에러 정의 및 테스트 패턴
- [단위 테스트 가이드](../testing/15a-unit-testing)
- [LanguageExt](https://github.com/louthy/language-ext)
- [Ardalis.SmartEnum](https://github.com/ardalis/SmartEnum)
