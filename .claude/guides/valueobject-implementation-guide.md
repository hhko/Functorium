# 값 객체(ValueObject) 구현 가이드

이 문서는 Functorium 프레임워크를 사용하여 DDD 값 객체를 구현하는 방법을 설명합니다.

## 목차
- [요약](#요약)
- [값 객체 개요](#값-객체-개요)
- [Functorium 기반 클래스](#functorium-기반-클래스)
- [구현 단계](#구현-단계)
- [유효성 검사](#유효성-검사)
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
using Functorium.Domains.Errors;
using Functorium.Domains.ValueObjects;
using static Functorium.Domains.Errors.DomainErrorType;

// 1. 기반 클래스 상속
public sealed class Email : SimpleValueObject<string>
{
    private static readonly Regex EmailPattern = new(@"^[^@]+@[^@]+\.[^@]+$");

    // 2. Private 생성자 - 파생 속성도 여기서 계산
    private Email(string value) : base(value)
    {
        var atIndex = value.IndexOf('@');
        LocalPart = value[..atIndex];
        Domain = value[(atIndex + 1)..];
    }

    // 3. 파생 속성 (생성자에서 한 번만 계산)
    public string LocalPart { get; }
    public string Domain { get; }

    // 4. Create 메서드 - CreateFromValidation 헬퍼 사용
    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new Email(v));

    // 5. Validate 메서드 - nullable 입력 처리 후 원시 타입 반환
    public static Validation<Error, string> Validate(string? value) =>
        Validate<Email>.NotEmpty(value ?? "")
            .ThenMatches(EmailPattern)
            .ThenMaxLength(254)
            .ThenNormalize(v => v.ToLowerInvariant());

    // 6. 암시적 변환 (선택적)
    public static implicit operator string(Email email) => email.Value;
}
```

### Validate 메서드 반환 타입 규칙

`Validation<Error, T>`에서 **T는 값 객체 타입이 아니라 값 객체가 래핑하는 원시 데이터 타입**입니다:

| 값 객체 | 래핑 타입 | Validate 반환 | Create 반환 |
|---------|----------|--------------|-------------|
| `Email` | `string` | `Validation<Error, string>` | `Fin<Email>` |
| `Price` | `decimal` | `Validation<Error, decimal>` | `Fin<Price>` |
| `Quantity` | `int` | `Validation<Error, int>` | `Fin<Quantity>` |
| `Money` | `(decimal, string)` | `Validation<Error, (decimal, string)>` | `Fin<Money>` |

**역할 분리:**
- **Validate**: 검증만 담당, 원시 타입 반환
- **Create**: 객체 생성 담당, `CreateFromValidation(Validate(), factory)` 호출

**CreateFromValidation 헬퍼**: 부모 클래스에서 제공하는 메서드로 `.Map(factory).ToFin()` 패턴을 캡슐화합니다.

### 오류 정의 비교

| 방식 | 설명 | 사용 시점 |
|------|------|----------|
| `Validate<T>` | 공통 검증 규칙 라이브러리 | 일반적인 검증 (권장) |
| `DomainError.For<T>()` | 타입 안전한 오류 생성 | 커스텀 검증 |

### 검증 전략 비교

| 전략 | 연산자 | 동작 | 사용 시점 |
|------|--------|------|----------|
| 순차 검증 | `Bind` / `Then*` | 첫 오류에서 중단 | 의존 관계가 있는 검증 |
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
- `Validate<T>`로 일관된 검증 패턴
- `DomainError.For<T>()`로 타입 안전한 오류 생성
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
        Validate(amount, currency).ToFin();

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
        Validate(value).ToFin();

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
        Validate(value).ToFin();

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

### 6단계 구현 절차

| 단계 | 구성 요소 | 필수 | 설명 |
|------|----------|------|------|
| 1 | 속성 선언 | O | 불변 속성 정의 |
| 2 | Private 생성자 | O | 외부 생성 차단 |
| 3 | Create 메서드 | O | `Validate().ToFin()` 호출 |
| 4 | Validate 메서드 | O | `Validate<T>` 체이닝 |
| 5 | 파생 속성/도메인 메서드 | △ | 의미 있는 정보/동작만 노출 |
| 6 | 동등성 컴포넌트 | △ | ValueObject만 필수 |

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

부모 클래스의 `CreateFromValidation` 헬퍼를 사용하여 객체 생성:

```csharp
// SimpleValueObject<T>용 - 부모 클래스에 정의된 CreateFromValidation 사용
public static Fin<Email> Create(string? value) =>
    CreateFromValidation(Validate(value), v => new Email(v));

// ValueObject용 - 부모 클래스에 정의된 CreateFromValidation 사용
public static Fin<Money> Create(decimal amount, string? currency) =>
    CreateFromValidation(Validate(amount, currency), tuple => new Money(tuple.amount, tuple.currency));
```

#### 4. Validate 메서드

nullable 입력을 처리하고 원시 타입을 반환하며 검증만 담당:

```csharp
// Validate는 nullable 입력을 받아 null을 빈 문자열로 변환 후 검증
public static Validation<Error, string> Validate(string? value) =>
    Validate<Email>.NotEmpty(value ?? "")
        .ThenMatches(EmailPattern)
        .ThenMaxLength(254)
        .ThenNormalize(v => v.ToLowerInvariant());
```

#### 5. 파생 속성/도메인 메서드

원시 값을 직접 노출하지 않고, **의미 있는 파생 속성**이나 **도메인 메서드**만 노출합니다.
**성능**: 파생 속성은 생성자에서 한 번만 계산하여 캐싱합니다:

```csharp
// X 비권장 - 원시 값 직접 노출 (값 객체 의도 훼손)
public string Address => Value;

// X 비권장 - 매번 계산 (성능 저하)
public string LocalPart => Value.Split('@')[0];
public string Domain => Value.Split('@')[1];

// O 권장 - 생성자에서 한 번만 계산
private Email(string value) : base(value)
{
    var atIndex = value.IndexOf('@');
    LocalPart = value[..atIndex];
    Domain = value[(atIndex + 1)..];
}
public string LocalPart { get; }
public string Domain { get; }
public string Masked => $"{LocalPart[0]}***@{Domain}";  // 이미 캐싱된 값 사용

// O 권장 - 도메인 메서드로 동작 제공
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

---

## 유효성 검사

### Validate\<T\> 라이브러리

`Validate<T>`는 값 객체 검증을 위한 공통 규칙 라이브러리입니다. 타입 파라미터를 한 번만 지정하면 체이닝에서 반복하지 않아도 됩니다:

```csharp
using Functorium.Domains.ValueObjects;

// 시작 메서드 - 타입 파라미터 한 번만 지정
Validate<Email>.NotEmpty(value)           // 문자열 비어있지 않음
Validate<Email>.MinLength(value, 8)       // 최소 길이
Validate<Email>.MaxLength(value, 100)     // 최대 길이
Validate<Email>.ExactLength(value, 10)    // 정확한 길이
Validate<Email>.Matches(value, regex)     // 정규식 패턴
Validate<Price>.Positive(value)           // 양수
Validate<Age>.NonNegative(value)          // 0 이상
Validate<Age>.Between(value, 0, 150)      // 범위
Validate<Age>.AtMost(value, 150)          // 최대값 이하
Validate<Age>.AtLeast(value, 0)           // 최소값 이상
Validate<T>.Must(value, predicate, errorType, message)  // 커스텀 조건
```

### TypedValidationExtensions (체이닝)

`Then*` 확장 메서드로 검증을 체이닝합니다. **타입 파라미터 반복이 필요 없습니다**:

```csharp
using Functorium.Domains.ValueObjects;

// Validate: nullable 입력을 받아 null 처리 후 검증 (타입 파라미터 한 번만 지정)
public static Validation<Error, string> Validate(string? value) =>
    Validate<Email>.NotEmpty(value ?? "")
        .ThenMatches(EmailPattern)
        .ThenMaxLength(254)
        .ThenNormalize(v => v.ToLowerInvariant());

// Create: value를 직접 전달 (null 처리는 Validate 내부에서)
public static Fin<Email> Create(string? value) =>
    CreateFromValidation(Validate(value), v => new Email(v));
```

**사용 가능한 체이닝 메서드:**

| 메서드 | 설명 |
|--------|------|
| `ThenNotEmpty()` | 비어있지 않은지 검증 |
| `ThenMinLength(n)` | 최소 길이 검증 |
| `ThenMaxLength(n)` | 최대 길이 검증 |
| `ThenExactLength(n)` | 정확한 길이 검증 |
| `ThenMatches(regex)` | 정규식 패턴 검증 |
| `ThenNormalize(func)` | 값 정규화 (소문자 변환 등) |
| `ThenPositive()` | 양수 검증 |
| `ThenNonNegative()` | 0 이상 검증 |
| `ThenBetween(min, max)` | 범위 검증 |
| `ThenAtMost(max)` | 최대값 이하 검증 |
| `ThenAtLeast(min)` | 최소값 이상 검증 |
| `ThenMust(predicate, errorType, message)` | 커스텀 조건 검증 |

### 병렬 검증 (Apply)

모든 오류를 수집합니다. **독립적인 검증**에 적합합니다:

```csharp
public static Validation<Error, Money> Validate(decimal amount, string currency) =>
    (ValidateAmount(amount), ValidateCurrency(currency))
        .Apply((validAmount, validCurrency) => new Money(validAmount, validCurrency))
        .As();

private static Validation<Error, decimal> ValidateAmount(decimal amount) =>
    Validate<Money>.NonNegative(amount);

private static Validation<Error, string> ValidateCurrency(string currency) =>
    Validate<Money>.NotEmpty(currency)
        .ThenExactLength(3)
        .ThenNormalize(v => v.ToUpperInvariant());
```

**동작:**
1. 모든 검증을 병렬로 실행
2. 실패한 모든 오류를 `Seq<Error>`로 수집
3. 모두 성공 시 결과 반환

### 혼합 패턴 (Apply + Bind)

독립 검증 후 의존 검증을 수행합니다:

```csharp
public static Validation<Error, ExchangeRate> Validate(
    string baseCurrency, string quoteCurrency, decimal rate) =>
    // 1단계: 독립적인 검증 (병렬)
    (ValidateCurrency(baseCurrency), ValidateCurrency(quoteCurrency), ValidateRate(rate))
        .Apply((validBase, validQuote, validRate) => (validBase, validQuote, validRate))
        .As()
    // 2단계: 의존 검증 (순차)
        .Bind(values => ValidateDifferentCurrencies(values.validBase, values.validQuote)
            .Map(_ => new ExchangeRate(values.validBase, values.validQuote, values.validRate)));
```

---

## 오류 처리

### DomainErrorType 계층

`DomainErrorType`은 타입 안전한 에러 정의를 제공하는 sealed record 계층입니다:

```csharp
using static Functorium.Domains.Errors.DomainErrorType;

// 값 존재 검증
new Empty()                      // 비어있음
new Null()                       // null

// 문자열/컬렉션 길이 검증
new TooShort(MinLength: 8)       // 최소 길이 미만
new TooLong(MaxLength: 100)      // 최대 길이 초과
new WrongLength(Expected: 10)    // 정확한 길이 불일치

// 형식 검증
new InvalidFormat(Pattern: @"^\d+$")  // 형식 불일치

// 대소문자 검증
new NotUpperCase()               // 대문자가 아님
new NotLowerCase()               // 소문자가 아님

// 숫자 범위 검증
new Negative()                   // 음수
new NotPositive()                // 양수가 아님 (0 포함)
new OutOfRange(Min: "1", Max: "100")  // 범위 밖
new BelowMinimum(Minimum: "0")   // 최소값 미만
new AboveMaximum(Maximum: "1000") // 최대값 초과

// 존재 여부 검증
new NotFound()                   // 찾을 수 없음
new AlreadyExists()              // 이미 존재함

// 비즈니스 규칙 검증
new Duplicate()                  // 중복됨
new Mismatch()                   // 값 불일치

// 커스텀 에러
new Custom("AlreadyShipped")     // 도메인 특화 에러
```

### DomainError.For<T>() 패턴

`DomainError.For<T>()`는 에러 코드를 자동으로 `DomainErrors.{TypeName}.{ErrorName}` 형식으로 생성합니다:

```csharp
using Functorium.Domains.Errors;
using static Functorium.Domains.Errors.DomainErrorType;

// 기본 사용법
DomainError.For<Email>(
    new Empty(),
    currentValue: "",
    message: "Email cannot be empty");
// -> ErrorCode: "DomainErrors.Email.Empty"

// 제네릭 값 타입
DomainError.For<Age, int>(
    new Negative(),
    currentValue: -5,
    message: "Age cannot be negative");
// -> ErrorCode: "DomainErrors.Age.Negative"

// 두 개의 값 포함
DomainError.For<DateRange, DateTime, DateTime>(
    new Custom("InvalidRange"),
    startDate,
    endDate,
    message: "Start date must be before end date");
// -> ErrorCode: "DomainErrors.DateRange.InvalidRange"

// 세 개의 값 포함
DomainError.For<Triangle, double, double, double>(
    new Custom("InvalidTriangle"),
    sideA, sideB, sideC,
    message: "Invalid triangle sides");
```

### 커스텀 검증 with DomainError.For<T>()

`Validate<T>`에 없는 커스텀 검증은 `ThenMust` 또는 직접 `DomainError.For<T>()`를 사용합니다:

```csharp
// 방법 1: ThenMust 사용 (Validate는 nullable 입력 처리 후 원시 타입 반환)
public static Validation<Error, string> Validate(string? value) =>
    Validate<Currency>.NotEmpty(value ?? "")
        .ThenExactLength(3)
        .ThenMust(
            v => SupportedCurrencies.Contains(v),
            new Custom("Unsupported"),
            v => $"Currency '{v}' is not supported")
        .ThenNormalize(v => v.ToUpperInvariant());

public static Fin<Currency> Create(string? value) =>
    CreateFromValidation(Validate(value), v => new Currency(v));

// 방법 2: 직접 DomainError.For<T>() 사용
private static Validation<Error, string> ValidateSupportedCurrency(string value) =>
    SupportedCurrencies.Contains(value.ToUpperInvariant())
        ? value
        : DomainError.For<Currency>(
            new Custom("Unsupported"),
            value,
            $"Currency '{value}' is not supported");
```

---

## 실전 예제

### Email (SimpleValueObject)

```csharp
using Functorium.Domains.Errors;
using Functorium.Domains.ValueObjects;
using LanguageExt;
using LanguageExt.Common;
using System.Text.RegularExpressions;
using static Functorium.Domains.Errors.DomainErrorType;

public sealed class Email : SimpleValueObject<string>
{
    private static readonly Regex EmailPattern = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private const int MaxLength = 254;

    private Email(string value) : base(value)
    {
        // 파생 속성은 생성자에서 한 번만 계산
        var atIndex = value.IndexOf('@');
        LocalPart = value[..atIndex];
        Domain = value[(atIndex + 1)..];
    }

    // 파생 속성 (생성자에서 캐싱)
    public string LocalPart { get; }
    public string Domain { get; }
    public string Masked => LocalPart.Length <= 2
        ? $"**@{Domain}"
        : $"{LocalPart[0]}***{LocalPart[^1]}@{Domain}";

    // 도메인 메서드
    public bool BelongsTo(string domain) =>
        Domain.Equals(domain, StringComparison.OrdinalIgnoreCase);

    // Create: CreateFromValidation 헬퍼 사용
    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new Email(v));

    // Validate: nullable 입력 처리 후 원시 타입 반환
    public static Validation<Error, string> Validate(string? value) =>
        Validate<Email>.NotEmpty(value ?? "")
            .ThenMatches(EmailPattern)
            .ThenMaxLength(MaxLength)
            .ThenNormalize(v => v.ToLowerInvariant());

    public static implicit operator string(Email email) => email.Value;
}
```

### Money (ValueObject)

```csharp
using Functorium.Domains.Errors;
using Functorium.Domains.ValueObjects;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Domains.Errors.DomainErrorType;

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
        Validate(amount, currency ?? "").ToFin();

    public static Validation<Error, Money> Validate(decimal amount, string currency) =>
        (ValidateAmount(amount), ValidateCurrency(currency))
            .Apply((validAmount, validCurrency) => new Money(validAmount, validCurrency))
            .As();

    private static Validation<Error, decimal> ValidateAmount(decimal amount) =>
        Validate<Money>.NonNegative(amount);

    private static Validation<Error, string> ValidateCurrency(string currency) =>
        Validate<Money>.NotEmpty(currency)
            .ThenExactLength(3)
            .ThenNormalize(v => v.ToUpperInvariant());

    // 도메인 연산
    public Fin<Money> Add(Money other) =>
        Currency == other.Currency
            ? new Money(Amount + other.Amount, Currency)
            : DomainError.For<Money, string, string>(
                new Mismatch(),
                Currency, other.Currency,
                $"Cannot operate on different currencies: {Currency} vs {other.Currency}");

    public Fin<Money> Subtract(Money other) =>
        Currency == other.Currency
            ? Amount >= other.Amount
                ? new Money(Amount - other.Amount, Currency)
                : DomainError.For<Money, decimal, decimal>(
                    new Custom("InsufficientAmount"),
                    Amount, other.Amount,
                    $"Insufficient amount. Current: {Amount}, Required: {other.Amount}")
            : DomainError.For<Money, string, string>(
                new Mismatch(),
                Currency, other.Currency,
                $"Cannot operate on different currencies: {Currency} vs {other.Currency}");

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
}
```

### Quantity (ComparableSimpleValueObject)

```csharp
using Functorium.Domains.Errors;
using Functorium.Domains.ValueObjects;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Domains.Errors.DomainErrorType;

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

    // Create: CreateFromValidation 헬퍼 사용
    public static Fin<Quantity> Create(int value) =>
        CreateFromValidation(Validate(value), v => new Quantity(v));

    // Validate: 원시 타입 반환 (검증만 담당)
    public static Validation<Error, int> Validate(int value) =>
        Validate<Quantity>.NonNegative(value)
            .ThenAtMost(MaxValue);

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
    // X 비권장 - 원시 값 직접 노출
    public string Address => Value;

    // O 권장 - 생성자에서 계산한 파생 속성
    private Email(string value) : base(value)
    {
        var atIndex = value.IndexOf('@');
        LocalPart = value[..atIndex];
        Domain = value[(atIndex + 1)..];
    }
    public string LocalPart { get; }
    public string Domain { get; }

    // O 권장 - 필요 시 암시적 변환
    public static implicit operator string(Email email) => email.Value;
}
```

### "Validation<E,A>에서 Fin<A>로 변환 오류"

**원인**: `Validation`을 `Fin`으로 변환하지 않음

**해결:** 부모 클래스의 `CreateFromValidation` 헬퍼 사용:

```csharp
// X 잘못됨
public static Fin<Email> Create(string? value) =>
    Validate(value);  // Validation<Error, string> 반환

// O 올바름 - CreateFromValidation 헬퍼 사용
public static Fin<Email> Create(string? value) =>
    CreateFromValidation(Validate(value), v => new Email(v));
```

### "Apply 결과에서 .As() 호출 필요"

**원인**: `Apply` 결과가 다른 타입으로 래핑됨

**해결:** `.As()` 메서드로 변환:

```csharp
// X 잘못됨 - 컴파일 오류
public static Validation<Error, Money> Validate(decimal amount, string currency) =>
    (ValidateAmount(amount), ValidateCurrency(currency))
        .Apply((a, c) => new Money(a, c));

// O 올바름
public static Validation<Error, Money> Validate(decimal amount, string currency) =>
    (ValidateAmount(amount), ValidateCurrency(currency))
        .Apply((a, c) => new Money(a, c))
        .As();
```

### "HashSet 모호한 참조"

**원인**: `LanguageExt.HashSet`과 `System.Collections.Generic.HashSet` 충돌

**해결:** 전체 경로 사용:

```csharp
// X 모호함
private static readonly HashSet<string> ReservedNames = new();

// O 명시적
private static readonly System.Collections.Generic.HashSet<string> ReservedNames = new();
```

### GetEqualityComponents 누락

**원인**: `ValueObject` 상속 시 필수 메서드 누락

**해결:** `GetEqualityComponents` 구현:

```csharp
public sealed class Address : ValueObject
{
    // X 누락됨 - 컴파일 오류

    // O 필수 구현
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

### Q2. Validate\<T\>와 DomainError.For<T>() 중 어느 것을 사용해야 하나요?

**A:**

| 상황 | 권장 방법 |
|------|----------|
| 일반적인 검증 | `Validate<T>` + 체이닝 |
| 커스텀 비즈니스 규칙 | `ThenMust` 또는 `DomainError.For<T>()` |
| 도메인 연산 중 오류 | `DomainError.For<T>()` |

### Q3. Bind와 Apply를 언제 사용하나요?

**A:**

| 전략 | 사용 시점 | 예시 |
|------|----------|------|
| Bind (Then*) | 검증 간 의존 관계가 있을 때 | 빈 값 -> 형식 -> 길이 |
| Apply | 독립적인 검증을 병렬로 할 때 | 이름, 이메일, 나이 동시 검증 |
| Bind + Apply | 먼저 병렬 검증 후 의존 검증 | (기준통화, 견적통화) -> 같은 통화 아님 |

### Q4. 암시적 변환(implicit operator)은 언제 추가하나요?

**A:** 자주 사용되는 원시 타입 변환에 추가합니다:

```csharp
// 자주 사용되는 경우 추가
public static implicit operator string(Email email) => email.Value;

// 사용
string emailString = email;  // 암시적 변환
```

### Q5. 도메인 연산은 어디에 구현하나요?

**A:** 값 객체 내부에 메서드로 구현합니다:

```csharp
public sealed class Money : ValueObject
{
    // 도메인 연산
    public Fin<Money> Add(Money other) =>
        Currency == other.Currency
            ? new Money(Amount + other.Amount, Currency)
            : DomainError.For<Money, string, string>(
                new Mismatch(),
                Currency, other.Currency,
                $"Cannot operate on different currencies");
}
```

### Q6. 오류 메시지는 한글과 영어 중 어느 것으로 작성하나요?

**A:** 영어로 작성하고 현재 값을 포함합니다:

```csharp
// O 권장
$"Email cannot be empty. Current value: '{value}'"

// X 비권장
$"이메일이 비어있습니다. 현재 값: '{value}'"
```

### Q7. 외부에서 원시 값이 필요할 때는 어떻게 하나요?

**A:** 원시 값을 직접 노출하지 않고, **암시적 변환**이나 **파생 속성**을 사용합니다:

```csharp
// X 비권장 - 원시 값 직접 노출 (값 객체 의도 훼손)
public string Address => Value;

// O 권장 방법 1 - 암시적 변환
public static implicit operator string(Email email) => email.Value;

// 사용
string rawValue = email;  // 필요할 때만 변환

// O 권장 방법 2 - 명시적 ToString()
public override string ToString() => Value;

// O 권장 방법 3 - 생성자에서 계산한 파생 속성 (성능 최적화)
private Email(string value) : base(value)
{
    var atIndex = value.IndexOf('@');
    LocalPart = value[..atIndex];
    Domain = value[(atIndex + 1)..];
}
public string LocalPart { get; }
public string Domain { get; }
```

**원칙**: 값 객체는 "원시 타입 집착(Primitive Obsession)"을 피하기 위한 것이므로, 가능한 한 값 객체 타입으로 사용하고 원시 타입으로의 변환은 최소화합니다.

### Q8. 동등성 비교는 어떻게 테스트하나요?

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

- [레이어별 에러 코드 정의 가이드](./layered-error-definition-guide.md) - DomainError, ApplicationError, AdapterError 사용법
- [유스케이스 구현 가이드](./usecase-implementation-guide.md) - CQRS 패턴에서 값 객체 활용
- [단위 테스트 가이드](./unit-testing-guide.md) - 값 객체 테스트 작성
- [LanguageExt](https://github.com/louthy/language-ext) - Fin, Validation 타입 제공

