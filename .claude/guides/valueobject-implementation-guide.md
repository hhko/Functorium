# 값 객체(ValueObject) 구현 가이드

이 문서는 Functorium 프레임워크를 사용하여 DDD 값 객체를 구현하는 방법을 설명합니다.

## 목차
- [요약](#요약)
- [값 객체 개요](#값-객체-개요)
- [Functorium 기반 클래스](#functorium-기반-클래스)
- [구현 단계](#구현-단계)
- [유효성 검사 분리 패턴](#유효성-검사-분리-패턴)
- [오류 처리](#오류-처리)
- [실전 예제](#실전-예제)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)

---

## 요약

### 기반 클래스 선택

| 사용 시나리오 | 기반 클래스 | 특징 |
|--------------|------------|------|
| 복합 속성 값 객체 | `ValueObject` | 여러 속성으로 동등성 판단 |
| 단일 값 래핑 | `SimpleValueObject<T>` | 단일 값으로 동등성 판단 |
| 단일 값 + 비교 필요 | `ComparableSimpleValueObject<T>` | 정렬, 비교 연산 지원 |

### 핵심 패턴

```csharp
// 1. 기반 클래스 상속
public sealed class Email : SimpleValueObject<string>
{
    // 2. Private 생성자
    private Email(string value) : base(value) { }

    // 3. 파생 속성 (의미 있는 정보만 노출)
    public string LocalPart => Value.Split('@')[0];
    public string Domain => Value.Split('@')[1];

    // 4. Create 메서드 - 검증과 생성 연결
    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(
            Validate(value ?? "null"),
            validValue => new Email(validValue));

    // 5. Validate 메서드 - 검증 로직 분리
    public static Validation<Error, string> Validate(string value) =>
        (ValidateNotEmpty(value), ValidateFormat(value))
            .Apply((_, validFormat) => validFormat.ToLowerInvariant())
            .As();

    // 6. 개별 검증 규칙
    private static Validation<Error, string> ValidateNotEmpty(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainErrors.Empty(value);

    private static Validation<Error, string> ValidateFormat(string value) =>
        value.Contains('@')
            ? value
            : DomainErrors.InvalidFormat(value);

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Email)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: $"Email cannot be empty. Current value: '{value}'");

        public static Error InvalidFormat(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Email)}.{nameof(InvalidFormat)}",
                errorCurrentValue: value,
                errorMessage: $"Invalid email format. Current value: '{value}'");
    }
}
```

### 검증 전략 비교

| 전략 | 연산자 | 동작 | 사용 시점 |
|------|--------|------|----------|
| 순차 검증 | `Bind` | 첫 오류에서 중단 | 의존 관계가 있는 검증 |
| 병렬 검증 | `Apply` | 모든 오류 수집 | 독립적인 검증 |

---

## 값 객체 개요

### DDD에서의 값 객체

값 객체(Value Object)는 도메인 주도 설계에서 중요한 빌딩 블록입니다:

| 특성 | 설명 | 예시 |
|------|------|------|
| 불변성 | 생성 후 변경 불가 | 금액, 이메일 |
| 값 기반 동등성 | 속성 값으로 동등성 판단 | Money(100, "KRW") == Money(100, "KRW") |
| 자기 검증 | 생성 시 유효성 검증 | 유효하지 않은 이메일은 생성 불가 |
| 도메인 로직 캡슐화 | 관련 연산 포함 | Money.Add(Money other) |

### 왜 Functorium 기반 클래스를 사용하는가?

**직접 구현 시 문제점:**
- 매번 `Equals`, `GetHashCode` 구현 필요
- `IEquatable<T>`, `IComparable<T>` 반복 구현
- 동등성 로직의 일관성 유지 어려움

**Functorium 기반 클래스의 장점:**
- 동등성/해시코드 자동 처리
- `CreateFromValidation` 헬퍼로 검증-생성 연결
- 타입 안전한 값 래핑
- 보일러플레이트 코드 제거

---

## Functorium 기반 클래스

### ValueObject (복합 값 객체)

여러 속성으로 구성된 값 객체에 사용합니다:

```csharp
using Functorium.Domains.ValueObjects;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    // 동등성 컴포넌트 구현 (필수)
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public static Fin<Money> Create(decimal amount, string currency) =>
        CreateFromValidation(
            Validate(amount, currency),
            values => new Money(values.Amount, values.Currency));

    // ...
}
```

### SimpleValueObject\<T\> (단일 값 래핑)

단일 값을 래핑하는 값 객체에 사용합니다:

```csharp
using Functorium.Domains.ValueObjects;

public sealed class Email : SimpleValueObject<string>
{
    private Email(string value) : base(value) { }

    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(
            Validate(value ?? "null"),
            validValue => new Email(validValue));

    // 파생 속성 (의미 있는 정보만 노출)
    public string LocalPart => Value.Split('@')[0];
    public string Domain => Value.Split('@')[1];

    // 필요 시 암시적 변환
    public static implicit operator string(Email email) => email.Value;
}
```

### ComparableSimpleValueObject\<T\> (비교 가능 단일 값)

정렬, 비교가 필요한 단일 값 객체에 사용합니다:

```csharp
using Functorium.Domains.ValueObjects;

public sealed class Quantity : ComparableSimpleValueObject<int>
{
    private Quantity(int value) : base(value) { }

    // 정적 상수
    public static Quantity Zero => new(0);
    public static Quantity One => new(1);

    public static Fin<Quantity> Create(int value) =>
        CreateFromValidation(
            Validate(value),
            validValue => new Quantity(validValue));

    // 도메인 연산 (원시 값 노출 대신 의미 있는 메서드 제공)
    public Quantity Add(Quantity other) => new(Value + other.Value);
    public Quantity Subtract(Quantity other) => new(Math.Max(0, Value - other.Value));
    public bool IsZero => Value == 0;
    public bool IsPositive => Value > 0;

    // 필요 시 암시적 변환
    public static implicit operator int(Quantity quantity) => quantity.Value;

    // IComparable<Quantity> 자동 구현
    // quantity1 < quantity2, quantity1.CompareTo(quantity2) 등 사용 가능
}
```

---

## 구현 단계

### 7단계 구현 절차

| 단계 | 구성 요소 | 필수 | 설명 |
|------|----------|------|------|
| 1 | 속성 선언 | O | 불변 속성 정의 |
| 2 | Private 생성자 | O | 외부 생성 차단 |
| 3 | Create 메서드 | O | 검증과 생성 연결 |
| 4 | 파생 속성/도메인 메서드 | △ | 의미 있는 정보/동작만 노출 |
| 5 | Validate 메서드 | O | 검증 로직 분리 |
| 6 | 동등성 컴포넌트 | △ | ValueObject만 필수 |
| 7 | DomainErrors | O | 오류 정의 |

### 상세 구현

#### 1. 속성 선언

```csharp
// ValueObject - 여러 속성
public sealed class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string ZipCode { get; }
    public string Country { get; }
}

// SimpleValueObject - 단일 값 (protected Value 상속)
public sealed class Email : SimpleValueObject<string>
{
    // Value는 기반 클래스에서 상속
}
```

#### 2. Private 생성자

```csharp
// ValueObject
private Address(string street, string city, string zipCode, string country)
{
    Street = street;
    City = city;
    ZipCode = zipCode;
    Country = country;
}

// SimpleValueObject
private Email(string value) : base(value) { }
```

#### 3. Create 메서드

```csharp
public static Fin<Email> Create(string? value) =>
    CreateFromValidation(
        Validate(value ?? "null"),
        validValue => new Email(validValue));
```

#### 4. 파생 속성/도메인 메서드

원시 값을 직접 노출하지 않고, **의미 있는 파생 속성**이나 **도메인 메서드**만 노출합니다:

```csharp
// ✗ 비권장 - 원시 값 직접 노출 (값 객체 의도 훼손)
public string Address => Value;

// ✓ 권장 - 의미 있는 파생 속성만 노출
public string LocalPart => Value.Split('@')[0];
public string Domain => Value.Split('@')[1];
public string Masked => $"{LocalPart[0]}***@{Domain}";

// ✓ 권장 - 도메인 메서드로 동작 제공
public bool IsCorporate() => Domain.EndsWith(".com") || Domain.EndsWith(".co.kr");
public bool BelongsTo(string domain) => Domain.Equals(domain, StringComparison.OrdinalIgnoreCase);
```

**외부에서 원시 값이 필요한 경우**: 암시적 변환 연산자를 사용합니다:

```csharp
// 암시적 변환으로 필요 시에만 원시 값 추출
public static implicit operator string(Email email) => email.Value;

// 사용
Email email = Email.Create("test@example.com").Match(e => e, _ => null!);
string rawValue = email;  // 암시적 변환
```

#### 5. Validate 메서드

```csharp
public static Validation<Error, string> Validate(string value) =>
    ValidateNotEmpty(value)
        .Bind(_ => ValidateFormat(value))
        .Map(v => v.ToLowerInvariant());
```

#### 6. 동등성 컴포넌트 (ValueObject만)

```csharp
protected override IEnumerable<object> GetEqualityComponents()
{
    yield return Street;
    yield return City;
    yield return ZipCode;
    yield return Country;
}
```

#### 7. DomainErrors 중첩 클래스

```csharp
internal static class DomainErrors
{
    public static Error Empty(string value) =>
        ErrorCodeFactory.Create(
            errorCode: $"{nameof(DomainErrors)}.{nameof(Email)}.{nameof(Empty)}",
            errorCurrentValue: value,
            errorMessage: $"Email cannot be empty. Current value: '{value}'");
}
```

---

## 유효성 검사 분리 패턴

### 왜 검증을 분리하는가?

**기존 방식의 문제점:**

```csharp
// ✗ 비권장 - Create 내부에서 검증
public static Fin<Email> Create(string value)
{
    if (string.IsNullOrWhiteSpace(value))
        return DomainErrors.Empty(value);
    if (!value.Contains('@'))
        return DomainErrors.InvalidFormat(value);
    return new Email(value.ToLowerInvariant());
}
```

**문제점:**
- 여러 오류 발생 시 첫 번째 오류만 반환
- 검증 로직 재사용 불가
- 테스트하기 어려움

**분리 패턴의 장점:**

```csharp
// ✓ 권장 - Validate 메서드 분리
public static Fin<Email> Create(string? value) =>
    CreateFromValidation(
        Validate(value ?? "null"),
        validValue => new Email(validValue));

public static Validation<Error, string> Validate(string value) =>
    (ValidateNotEmpty(value), ValidateFormat(value))
        .Apply((_, validFormat) => validFormat.ToLowerInvariant())
        .As();
```

**장점:**
- 모든 오류를 한 번에 수집 가능 (Apply)
- 검증 로직만 별도 테스트 가능
- 다른 곳에서 재사용 가능

### 순차 검증 (Bind)

첫 번째 오류에서 중단합니다. **의존 관계가 있는 검증**에 적합합니다:

```csharp
// 빈 값 검증 → 형식 검증 (의존 관계)
public static Validation<Error, string> Validate(string value) =>
    ValidateNotEmpty(value)                           // 먼저 빈 값 검증
        .Bind(_ => ValidateFormat(value))             // 통과 시 형식 검증
        .Bind(_ => ValidateLength(value))             // 통과 시 길이 검증
        .Map(v => v.ToLowerInvariant());              // 정규화
```

**동작:**
1. `ValidateNotEmpty` 실패 → 첫 번째 오류 반환, 중단
2. `ValidateNotEmpty` 성공 → `ValidateFormat` 실행
3. `ValidateFormat` 실패 → 두 번째 오류 반환, 중단
4. 모두 성공 → 정규화된 값 반환

### 병렬 검증 (Apply)

모든 오류를 수집합니다. **독립적인 검증**에 적합합니다.

#### 방법 1: 튜플 기반 Apply (권장)

여러 Validation을 튜플로 묶어서 한 번에 Apply를 호출합니다:

```csharp
public static Validation<Error, (string Street, string City, string ZipCode, string Country)>
    Validate(string street, string city, string zipCode, string country) =>
    (ValidateStreet(street), ValidateCity(city), ValidateZipCode(zipCode), ValidateCountry(country))
        .Apply((validStreet, validCity, validZipCode, validCountry) =>
            (validStreet, validCity, validZipCode, validCountry))
        .As();
```

**장점:**
- 간결하고 직관적인 코드
- 검증 개수가 명확하게 드러남
- 대부분의 상황에서 권장

#### 방법 2: fun 기반 개별 Apply

`fun` 함수를 사용하여 Currying 방식으로 개별 Apply를 체이닝합니다:

```csharp
using static LanguageExt.Prelude;

public static Validation<Error, (string Email, string Password, string Name, int Age)>
    Validate(string email, string password, string name, string ageInput) =>
    fun((string e, string p, string n, int a) => (Email: e, Password: p, Name: n, Age: a))
        .Map(f => Success<Error, Func<string, string, string, int, (string, string, string, int)>>(f))
        .Apply(ValidateEmailFormat(email))
        .Apply(ValidatePasswordStrength(password))
        .Apply(ValidateNameFormat(name))
        .Apply(ValidateAgeFormat(ageInput));
```

또는 `Pure`를 사용하여 더 간결하게:

```csharp
public static Validation<Error, (string Email, string Password, string Name, int Age)>
    Validate(string email, string password, string name, string ageInput) =>
    Pure<Validation<Error>, Func<string, string, string, int, (string, string, string, int)>>(
        fun((string e, string p, string n, int a) => (Email: e, Password: p, Name: n, Age: a)))
        .Apply(ValidateEmailFormat(email))
        .Apply(ValidatePasswordStrength(password))
        .Apply(ValidateNameFormat(name))
        .Apply(ValidateAgeFormat(ageInput));
```

**장점:**
- Currying을 통한 단계적 적용으로 유연성 확보
- 동적으로 검증 개수를 조절할 때 유용
- 함수형 프로그래밍의 Applicative Functor 패턴에 충실

#### 두 방법 비교

| 구분 | 튜플 기반 Apply | fun 기반 개별 Apply |
|------|----------------|---------------------|
| **코드 간결성** | 간결하고 직관적 | 상대적으로 장황함 |
| **타입 추론** | 자동 추론 | `fun`이 타입 추론 지원 |
| **유연성** | 고정된 검증 개수 | 동적 검증 개수 가능 |
| **사용 시기** | 대부분의 경우 | 고급 합성, 동적 파라미터 |
| **학습 곡선** | 낮음 | Currying 이해 필요 |

> **권장사항**: 일반적인 경우 **튜플 기반 Apply**를 사용하세요. fun 기반 개별 Apply는 동적으로 검증을 조합해야 하거나 함수형 프로그래밍 패턴을 깊이 활용할 때 고려하세요.

**동작:**
1. 모든 검증을 병렬로 실행
2. 실패한 모든 오류를 `Seq<Error>`로 수집
3. 모두 성공 시 튜플로 결과 반환

### 혼합 패턴 (Apply + Bind)

독립 검증 후 의존 검증을 수행합니다:

```csharp
public static Validation<Error, (string Base, string Quote, decimal Rate)>
    Validate(string baseCurrency, string quoteCurrency, decimal rate) =>
    // 1단계: 독립적인 검증 (병렬)
    (ValidateBaseCurrency(baseCurrency), ValidateQuoteCurrency(quoteCurrency), ValidateRate(rate))
        .Apply((validBase, validQuote, validRate) => (validBase, validQuote, validRate))
        .As()
    // 2단계: 의존 검증 (순차)
        .Bind(values => ValidateDifferentCurrencies(values.validBase, values.validQuote)
            .Map(_ => (values.validBase, values.validQuote, values.validRate)));
```

---

## 오류 처리

### ErrorCodeFactory 패턴

일관된 오류 형식을 사용합니다:

```csharp
internal static class DomainErrors
{
    public static Error Empty(string value) =>
        ErrorCodeFactory.Create(
            errorCode: $"{nameof(DomainErrors)}.{nameof(Email)}.{nameof(Empty)}",
            errorCurrentValue: value,
            errorMessage: $"Email cannot be empty. Current value: '{value}'");

    public static Error InvalidFormat(string value) =>
        ErrorCodeFactory.Create(
            errorCode: $"{nameof(DomainErrors)}.{nameof(Email)}.{nameof(InvalidFormat)}",
            errorCurrentValue: value,
            errorMessage: $"Invalid email format. Current value: '{value}'");

    public static Error TooLong(int length) =>
        ErrorCodeFactory.Create(
            errorCode: $"{nameof(DomainErrors)}.{nameof(Email)}.{nameof(TooLong)}",
            errorCurrentValue: length,
            errorMessage: $"Email cannot exceed 254 characters. Current length: '{length}'");
}
```

### 오류 메시지 표준화

| 구성 요소 | 형식 | 예시 |
|----------|------|------|
| errorCode | `DomainErrors.{타입}.{오류명}` | `DomainErrors.Email.InvalidFormat` |
| errorCurrentValue | 현재 값 | `"invalid-email"` |
| errorMessage | `{설명}. Current value: '{값}'` | `"Invalid email format. Current value: 'invalid-email'"` |

### 여러 값의 오류

```csharp
public static Error CurrencyMismatch(string currency1, string currency2) =>
    ErrorCodeFactory.Create(
        errorCode: $"{nameof(DomainErrors)}.{nameof(Money)}.{nameof(CurrencyMismatch)}",
        currency1, currency2,  // 여러 값
        errorMessage: $"Cannot operate on different currencies. Currency1: '{currency1}', Currency2: '{currency2}'");
```

---

## 실전 예제

### Email (SimpleValueObject)

```csharp
using Functorium.Abstractions.Errors;
using Functorium.Domains.ValueObjects;
using LanguageExt;
using LanguageExt.Common;
using System.Text.RegularExpressions;

public sealed class Email : SimpleValueObject<string>
{
    private static readonly Regex Pattern = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private Email(string value) : base(value) { }

    // 파생 속성 (의미 있는 정보만 노출)
    public string LocalPart => Value.Split('@')[0];
    public string Domain => Value.Split('@')[1];
    public string Masked => LocalPart.Length <= 2
        ? $"**@{Domain}"
        : $"{LocalPart[0]}***{LocalPart[^1]}@{Domain}";

    // 도메인 메서드
    public bool BelongsTo(string domain) =>
        Domain.Equals(domain, StringComparison.OrdinalIgnoreCase);

    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(
            Validate(value ?? "null"),
            validValue => new Email(validValue));

    public static Validation<Error, string> Validate(string value) =>
        ValidateNotEmpty(value)
            .Bind(_ => ValidateNotTooLong(value.Trim()))
            .Bind(normalized => ValidateFormat(normalized));

    private static Validation<Error, string> ValidateNotEmpty(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainErrors.Empty(value);

    private static Validation<Error, string> ValidateNotTooLong(string value)
    {
        var normalized = value.ToLowerInvariant();
        return normalized.Length <= 254
            ? normalized
            : DomainErrors.TooLong(normalized.Length);
    }

    private static Validation<Error, string> ValidateFormat(string value) =>
        Pattern.IsMatch(value)
            ? value
            : DomainErrors.InvalidFormat(value);

    public static implicit operator string(Email email) => email.Value;

    internal static class DomainErrors
    {
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Email)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: $"Email cannot be empty. Current value: '{value}'");

        public static Error TooLong(int length) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Email)}.{nameof(TooLong)}",
                errorCurrentValue: length,
                errorMessage: $"Email cannot exceed 254 characters. Current length: '{length}'");

        public static Error InvalidFormat(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Email)}.{nameof(InvalidFormat)}",
                errorCurrentValue: value,
                errorMessage: $"Invalid email format. Current value: '{value}'");
    }
}
```

### Money (ValueObject)

```csharp
using Functorium.Abstractions.Errors;
using Functorium.Domains.ValueObjects;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

public sealed class Money : ValueObject, IComparable<Money>
{
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
            values => new Money(values.Amount, values.Currency.ToUpperInvariant()));

    public static Validation<Error, (decimal Amount, string Currency)>
        Validate(decimal amount, string currency) =>
        (ValidateAmountNotNegative(amount), ValidateCurrencyNotEmpty(currency), ValidateCurrencyLength(currency))
            .Apply((validAmount, validCurrency, _) => (validAmount, validCurrency))
            .As();

    private static Validation<Error, decimal> ValidateAmountNotNegative(decimal amount) =>
        amount >= 0
            ? amount
            : DomainErrors.NegativeAmount(amount);

    private static Validation<Error, string> ValidateCurrencyNotEmpty(string currency) =>
        !string.IsNullOrWhiteSpace(currency)
            ? currency
            : DomainErrors.EmptyCurrency(currency);

    private static Validation<Error, string> ValidateCurrencyLength(string currency) =>
        currency.Length == 3
            ? currency
            : DomainErrors.InvalidCurrencyLength(currency);

    // 도메인 연산
    public Fin<Money> Add(Money other) =>
        Currency == other.Currency
            ? new Money(Amount + other.Amount, Currency)
            : DomainErrors.CurrencyMismatch(Currency, other.Currency);

    public Fin<Money> Subtract(Money other) =>
        Currency == other.Currency
            ? Amount >= other.Amount
                ? new Money(Amount - other.Amount, Currency)
                : DomainErrors.InsufficientAmount(Amount, other.Amount)
            : DomainErrors.CurrencyMismatch(Currency, other.Currency);

    // IComparable<Money>
    public int CompareTo(Money? other)
    {
        if (other is null) return 1;
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot compare {Currency} with {other.Currency}");
        return Amount.CompareTo(other.Amount);
    }

    // 동등성 컴포넌트
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:N0} {Currency}";

    internal static class DomainErrors
    {
        public static Error NegativeAmount(decimal amount) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Money)}.{nameof(NegativeAmount)}",
                errorCurrentValue: amount,
                errorMessage: $"Amount cannot be negative. Current value: '{amount}'");

        public static Error EmptyCurrency(string currency) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Money)}.{nameof(EmptyCurrency)}",
                errorCurrentValue: currency,
                errorMessage: $"Currency cannot be empty. Current value: '{currency}'");

        public static Error InvalidCurrencyLength(string currency) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Money)}.{nameof(InvalidCurrencyLength)}",
                errorCurrentValue: currency,
                errorMessage: $"Currency must be 3 characters. Current value: '{currency}'");

        public static Error CurrencyMismatch(string currency1, string currency2) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Money)}.{nameof(CurrencyMismatch)}",
                currency1, currency2,
                errorMessage: $"Cannot operate on different currencies. Currency1: '{currency1}', Currency2: '{currency2}'");

        public static Error InsufficientAmount(decimal current, decimal required) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Money)}.{nameof(InsufficientAmount)}",
                current, required,
                errorMessage: $"Insufficient amount. Current: '{current}', Required: '{required}'");
    }
}
```

### Quantity (ComparableSimpleValueObject)

```csharp
using Functorium.Abstractions.Errors;
using Functorium.Domains.ValueObjects;
using LanguageExt;
using LanguageExt.Common;

public sealed class Quantity : ComparableSimpleValueObject<int>
{
    public const int MaxValue = 10000;

    private Quantity(int value) : base(value) { }

    // 정적 상수
    public static Quantity Zero => new(0);
    public static Quantity One => new(1);

    // 파생 속성 (의미 있는 정보)
    public bool IsZero => Value == 0;
    public bool IsPositive => Value > 0;

    public static Fin<Quantity> Create(int value) =>
        CreateFromValidation(
            Validate(value),
            validValue => new Quantity(validValue));

    public static Validation<Error, int> Validate(int value) =>
        ValidateNotNegative(value)
            .Bind(_ => ValidateNotExceedsMax(value))
            .Map(_ => value);

    private static Validation<Error, int> ValidateNotNegative(int value) =>
        value >= 0
            ? value
            : DomainErrors.Negative(value);

    private static Validation<Error, int> ValidateNotExceedsMax(int value) =>
        value <= MaxValue
            ? value
            : DomainErrors.ExceedsMaximum(value);

    // 도메인 연산
    public Quantity Add(Quantity other) => new(Value + other.Value);
    public Quantity Subtract(Quantity other) => new(Math.Max(0, Value - other.Value));

    // 연산자 오버로딩
    public static Quantity operator +(Quantity a, Quantity b) => a.Add(b);
    public static Quantity operator -(Quantity a, Quantity b) => a.Subtract(b);
    public static bool operator <(Quantity a, Quantity b) => a.CompareTo(b) < 0;
    public static bool operator >(Quantity a, Quantity b) => a.CompareTo(b) > 0;
    public static bool operator <=(Quantity a, Quantity b) => a.CompareTo(b) <= 0;
    public static bool operator >=(Quantity a, Quantity b) => a.CompareTo(b) >= 0;

    public static implicit operator int(Quantity quantity) => quantity.Value;

    internal static class DomainErrors
    {
        public static Error Negative(int value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Quantity)}.{nameof(Negative)}",
                errorCurrentValue: value,
                errorMessage: $"Quantity cannot be negative. Current value: '{value}'");

        public static Error ExceedsMaximum(int value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Quantity)}.{nameof(ExceedsMaximum)}",
                errorCurrentValue: value,
                errorMessage: $"Quantity cannot exceed {MaxValue}. Current value: '{value}'");
    }
}
```

---

## 트러블슈팅

### "보호 수준 때문에 Value에 액세스할 수 없습니다"

**원인**: `SimpleValueObject<T>`.`Value`는 `protected`입니다.

**해결:** 원시 값을 직접 노출하지 않고, 의미 있는 파생 속성이나 암시적 변환을 사용합니다:

```csharp
public sealed class Email : SimpleValueObject<string>
{
    // ✗ 비권장 - 원시 값 직접 노출
    public string Address => Value;

    // ✓ 권장 - 파생 속성
    public string LocalPart => Value.Split('@')[0];
    public string Domain => Value.Split('@')[1];

    // ✓ 권장 - 필요 시 암시적 변환
    public static implicit operator string(Email email) => email.Value;
}
```

### "Validation<E,A>에서 Fin<A>로 변환 오류"

**원인**: `CreateFromValidation` 없이 직접 반환

**해결:** `CreateFromValidation` 헬퍼 사용:

```csharp
// ✗ 잘못됨
public static Fin<Email> Create(string value) =>
    Validate(value);  // Validation<Error, string> 반환

// ✓ 올바름
public static Fin<Email> Create(string value) =>
    CreateFromValidation(
        Validate(value),
        validValue => new Email(validValue));
```

### "Apply 결과에서 .As() 호출 필요"

**원인**: `Apply` 결과가 다른 타입으로 래핑됨

**해결:** `.As()` 메서드로 변환:

```csharp
// ✗ 잘못됨 - 컴파일 오류
public static Validation<Error, (string, int)> Validate(string s, int n) =>
    (ValidateString(s), ValidateNumber(n))
        .Apply((validS, validN) => (validS, validN));

// ✓ 올바름
public static Validation<Error, (string, int)> Validate(string s, int n) =>
    (ValidateString(s), ValidateNumber(n))
        .Apply((validS, validN) => (validS, validN))
        .As();
```

### "HashSet 모호한 참조"

**원인**: `LanguageExt.HashSet`과 `System.Collections.Generic.HashSet` 충돌

**해결:** 전체 경로 사용:

```csharp
// ✗ 모호함
private static readonly HashSet<string> ReservedNames = new();

// ✓ 명시적
private static readonly System.Collections.Generic.HashSet<string> ReservedNames = new();
```

### GetEqualityComponents 누락

**원인**: `ValueObject` 상속 시 필수 메서드 누락

**해결:** `GetEqualityComponents` 구현:

```csharp
public sealed class Address : ValueObject
{
    // ✗ 누락됨 - 컴파일 오류

    // ✓ 필수 구현
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return ZipCode;
    }
}
```

---

## FAQ

### Q1. ValueObject와 SimpleValueObject 중 어느 것을 사용해야 하나요?

**A:** 속성 개수로 결정합니다:

| 속성 개수 | 권장 클래스 | 예시 |
|----------|------------|------|
| 1개 | `SimpleValueObject<T>` | Email, UserId, ProductCode |
| 2개 이상 | `ValueObject` | Money(amount, currency), Address |
| 1개 + 비교 필요 | `ComparableSimpleValueObject<T>` | Quantity, Age |

### Q2. Create와 Validate를 분리해야 하는 이유는?

**A:**
- **오류 수집**: `Validate`에서 `Apply`로 모든 오류 수집 가능
- **재사용**: 다른 곳에서 `Validate`만 호출 가능
- **테스트**: 검증 로직만 별도 테스트 가능

### Q3. Bind와 Apply를 언제 사용하나요?

**A:**

| 전략 | 사용 시점 | 예시 |
|------|----------|------|
| Bind | 검증 간 의존 관계가 있을 때 | 빈 값 → 형식 → 길이 |
| Apply | 독립적인 검증을 병렬로 할 때 | 이름, 이메일, 나이 동시 검증 |
| Bind + Apply | 먼저 병렬 검증 후 의존 검증 | (기준통화, 견적통화) → 같은 통화 아님 |

### Q4. 튜플 기반 Apply와 fun 기반 Apply 중 어떤 것을 사용하나요?

**A:**

| 상황 | 권장 방법 |
|------|----------|
| 일반적인 경우 (대부분) | **튜플 기반 Apply** |
| 동적 검증 개수 | fun 기반 개별 Apply |
| Applicative Functor 학습 | fun 기반 개별 Apply |

```csharp
// ✓ 대부분의 경우 - 튜플 기반
(ValidateA(a), ValidateB(b), ValidateC(c))
    .Apply((validA, validB, validC) => (validA, validB, validC))
    .As();

// 동적 검증이 필요한 경우 - fun 기반
fun((string a, string b) => (a, b))
    .Map(f => Success<Error, Func<string, string, (string, string)>>(f))
    .Apply(ValidateA(a))
    .Apply(ValidateB(b));
```

### Q5. 암시적 변환(implicit operator)은 언제 추가하나요?

**A:** 자주 사용되는 원시 타입 변환에 추가합니다:

```csharp
// 자주 사용되는 경우 추가
public static implicit operator string(Email email) => email.Value;

// 사용
string emailString = email;  // 암시적 변환
```

### Q6. 도메인 연산은 어디에 구현하나요?

**A:** 값 객체 내부에 메서드로 구현합니다:

```csharp
public sealed class Money : ValueObject
{
    // 도메인 연산
    public Fin<Money> Add(Money other) =>
        Currency == other.Currency
            ? new Money(Amount + other.Amount, Currency)
            : DomainErrors.CurrencyMismatch(Currency, other.Currency);
}
```

### Q7. 오류 메시지는 한글과 영어 중 어느 것으로 작성하나요?

**A:** 영어로 작성하고 `Current value: '{값}'` 패턴을 사용합니다:

```csharp
// ✓ 권장
errorMessage: $"Email cannot be empty. Current value: '{value}'"

// ✗ 비권장
errorMessage: $"이메일이 비어있습니다. 현재 값: '{value}'"
```

### Q8. 외부에서 원시 값이 필요할 때는 어떻게 하나요?

**A:** 원시 값을 직접 노출하지 않고, **암시적 변환**이나 **파생 속성**을 사용합니다:

```csharp
// ✗ 비권장 - 원시 값 직접 노출 (값 객체 의도 훼손)
public string Address => Value;

// ✓ 권장 방법 1 - 암시적 변환
public static implicit operator string(Email email) => email.Value;

// 사용
string rawValue = email;  // 필요할 때만 변환

// ✓ 권장 방법 2 - 명시적 ToString()
public override string ToString() => Value;

// ✓ 권장 방법 3 - 의미 있는 파생 속성
public string LocalPart => Value.Split('@')[0];
public string Domain => Value.Split('@')[1];
```

**원칙**: 값 객체는 "원시 타입 집착(Primitive Obsession)"을 피하기 위한 것이므로, 가능한 한 값 객체 타입으로 사용하고 원시 타입으로의 변환은 최소화합니다.

### Q9. 동등성 비교는 어떻게 테스트하나요?

**A:**

```csharp
[Fact]
public void SameValues_AreEqual()
{
    var email1 = Email.Create("test@example.com").Match(e => e, _ => null!);
    var email2 = Email.Create("TEST@example.com").Match(e => e, _ => null!);

    // 정규화 후 동일하므로 equal
    email1.Equals(email2).ShouldBeTrue();
    (email1 == email2).ShouldBeTrue();
    email1.GetHashCode().ShouldBe(email2.GetHashCode());
}
```

## 참고 문서

- [유스케이스 구현 가이드](./usecase-implementation-guide.md) - CQRS 패턴에서 값 객체 활용
- [단위 테스트 가이드](./unit-testing-guide.md) - 값 객체 테스트 작성
- [LanguageExt](https://github.com/louthy/language-ext) - Fin, Validation 타입 제공

