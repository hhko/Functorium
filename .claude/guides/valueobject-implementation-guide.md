# 값 객체(ValueObject) 구현 가이드

이 문서는 Functorium 프레임워크의 `Functorium.Domains` 네임스페이스를 사용하여 DDD 값 객체를 구현하는 방법을 설명합니다.

## 목차

- [개요](#개요)
- [클래스 계층 구조](#클래스-계층-구조)
- [기반 클래스](#기반-클래스)
  - [ValueObject](#valueobject)
  - [SimpleValueObject\<T\>](#simplevalueobjectt)
  - [ComparableValueObject](#comparablevalueobject)
  - [ComparableSimpleValueObject\<T\>](#comparablesimplevalueobjectt)
- [검증 시스템](#검증-시스템)
  - [ValidationRules\<T\> 시작점](#validationrulest-시작점)
  - [TypedValidation 체이닝](#typedvalidation-체이닝)
- [오류 시스템](#오류-시스템)
  - [DomainErrorType 계층](#domainerrortype-계층)
  - [DomainError.For\<T\>() 헬퍼](#domainerrorfort-헬퍼)
- [구현 패턴](#구현-패턴)
- [FluentValidation 통합](#fluentvalidation-통합)
  - [MustSatisfyValidation (입력 타입 == 출력 타입)](#mustsatisfyvalidation-입력-타입--출력-타입)
  - [MustSatisfyValidationOf (입력 타입 != 출력 타입)](#mustsatisfyvalidationof-입력-타입--출력-타입)
- [실전 예제](#실전-예제)
- [SmartEnum 열거형](#smartenum-열거형)
- [FAQ](#faq)

---

## 개요

### 핵심 특징

| 특성 | 설명 |
|------|------|
| **불변성** | 생성 후 변경 불가 |
| **값 기반 동등성** | 속성 값으로 동등성 판단 |
| **자기 검증** | 생성 시 유효성 검증 |
| **도메인 로직 캡슐화** | 관련 연산 포함 |

### 기반 클래스 선택

| 사용 시나리오 | 기반 클래스 | 특징 |
|--------------|------------|------|
| 복합 속성 | `ValueObject` | 여러 속성으로 동등성 판단 |
| 단일 값 래핑 | `SimpleValueObject<T>` | 단일 값으로 동등성 판단 |
| 복합 속성 + 비교 | `ComparableValueObject` | 정렬, 비교 연산 지원 |
| 단일 값 + 비교 | `ComparableSimpleValueObject<T>` | 정렬, 비교 연산 지원 |
| 타입 안전한 열거형 | `SmartEnum<T, TValue>` | 도메인 로직 내장 열거형 |

### 핵심 패턴

```csharp
using Functorium.Domains.ValueObjects;
using static Functorium.Domains.Errors.DomainErrorType;

public sealed class Email : SimpleValueObject<string>
{
    private static readonly Regex EmailPattern = new(@"^[^@]+@[^@]+\.[^@]+$");

    private Email(string value) : base(value) { }

    // Create: CreateFromValidation 헬퍼 사용
    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new Email(v));

    // Validate: 원시 타입 반환, 타입 파라미터 한 번만 지정
    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<Email>.NotEmpty(value ?? "")
            .ThenMatches(EmailPattern)
            .ThenMaxLength(254)
            .ThenNormalize(v => v.ToLowerInvariant());

    public static implicit operator string(Email email) => email.Value;
}
```

---

## 클래스 계층 구조

```
IValueObject (인터페이스)
│
AbstractValueObject (추상 클래스)
├── GetEqualityComponents() - 동등성 컴포넌트
├── Equals() / GetHashCode() - 값 기반 동등성
├── == / != 연산자
└── 프록시 타입 처리 (ORM 지원)
    │
    └── ValueObject
        ├── CreateFromValidation<TValueObject, TValue>() 헬퍼
        │
        ├── SimpleValueObject<T>
        │   ├── protected T Value
        │   ├── CreateFromValidation<TValueObject>() 헬퍼
        │   └── explicit operator T
        │
        └── ComparableValueObject
            ├── GetComparableEqualityComponents()
            ├── IComparable<ComparableValueObject>
            ├── < / <= / > / >= 연산자
            │
            └── ComparableSimpleValueObject<T>
                ├── protected T Value
                ├── CreateFromValidation<TValueObject>() 헬퍼
                └── explicit operator T
```

---

## 기반 클래스

### ValueObject

복합 속성으로 구성된 값 객체의 기반 클래스입니다.

**위치**: `Functorium.Domains.ValueObjects.ValueObject`

```csharp
public abstract class ValueObject : AbstractValueObject
{
    // 팩토리 헬퍼 메서드
    public static Fin<TValueObject> CreateFromValidation<TValueObject, TValue>(
        Validation<Error, TValue> validation,
        Func<TValue, TValueObject> factory)
        where TValueObject : ValueObject;
}
```

**구현 필수 항목:**

| 항목 | 설명 |
|------|------|
| `GetEqualityComponents()` | 동등성 비교 컴포넌트 반환 |
| Private 생성자 | 외부 생성 차단 |
| `Create()` / `Validate()` | 팩토리 및 검증 메서드 |

**예제:**

```csharp
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    // Create: CreateFromValidation 헬퍼 사용
    public static Fin<Money> Create(decimal amount, string currency) =>
        CreateFromValidation(Validate(amount, currency), v => new Money(v.Amount, v.Currency));

    // Validate: 검증된 원시값 튜플 반환 (ValueObject 생성은 Create에서)
    public static Validation<Error, (decimal Amount, string Currency)> Validate(decimal amount, string currency) =>
        (ValidateAmount(amount), ValidateCurrency(currency))
            .Apply((a, c) => (Amount: a, Currency: c));
}
```

### SimpleValueObject\<T\>

단일 값을 래핑하는 값 객체의 기반 클래스입니다.

**위치**: `Functorium.Domains.ValueObjects.SimpleValueObject<T>`

```csharp
public abstract class SimpleValueObject<T> : ValueObject
    where T : notnull
{
    protected T Value { get; }

    protected SimpleValueObject(T value);

    // 팩토리 헬퍼 메서드
    public static Fin<TValueObject> CreateFromValidation<TValueObject>(
        Validation<Error, T> validation,
        Func<T, TValueObject> factory)
        where TValueObject : SimpleValueObject<T>;

    // 명시적 변환
    public static explicit operator T(SimpleValueObject<T>? valueObject);
}
```

**특징:**
- `Value` 속성은 `protected` - 외부에서 직접 접근 불가
- `GetEqualityComponents()`가 자동 구현됨 (`Value` 반환)
- 명시적 변환 연산자 제공

**예제:**

```csharp
public sealed class ProductName : SimpleValueObject<string>
{
    private ProductName(string value) : base(value) { }

    public static Fin<ProductName> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new ProductName(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<ProductName>.NotEmpty(value ?? "")
            .ThenMaxLength(100);

    // 암시적 변환 (선택적)
    public static implicit operator string(ProductName name) => name.Value;
}
```

### ComparableValueObject

비교 가능한 복합 값 객체의 기반 클래스입니다.

**위치**: `Functorium.Domains.ValueObjects.ComparableValueObject`

```csharp
public abstract class ComparableValueObject : ValueObject, IComparable<ComparableValueObject>
{
    protected abstract IEnumerable<IComparable> GetComparableEqualityComponents();

    public virtual int CompareTo(ComparableValueObject? other);

    // 비교 연산자
    public static bool operator <(ComparableValueObject? left, ComparableValueObject? right);
    public static bool operator <=(ComparableValueObject? left, ComparableValueObject? right);
    public static bool operator >(ComparableValueObject? left, ComparableValueObject? right);
    public static bool operator >=(ComparableValueObject? left, ComparableValueObject? right);
}
```

**구현 필수 항목:**
- `GetComparableEqualityComponents()` - `IComparable` 구현 타입만 반환

### ComparableSimpleValueObject\<T\>

비교 가능한 단일 값 객체의 기반 클래스입니다.

**위치**: `Functorium.Domains.ValueObjects.ComparableSimpleValueObject<T>`

```csharp
public abstract class ComparableSimpleValueObject<T> : ComparableValueObject
    where T : notnull, IComparable
{
    protected T Value { get; }

    protected ComparableSimpleValueObject(T value);

    public static Fin<TValueObject> CreateFromValidation<TValueObject>(
        Validation<Error, T> validation,
        Func<T, TValueObject> factory)
        where TValueObject : ComparableSimpleValueObject<T>;
}
```

**예제:**

```csharp
public sealed class Price : ComparableSimpleValueObject<decimal>
{
    private Price(decimal value) : base(value) { }

    public static Fin<Price> Create(decimal value) =>
        CreateFromValidation(Validate(value), v => new Price(v));

    public static Validation<Error, decimal> Validate(decimal value) =>
        ValidationRules<Price>.Positive(value)
            .ThenAtMost(1_000_000);

    public static implicit operator decimal(Price price) => price.Value;
}
```

---

## 검증 시스템

### 검증 카테고리 요약

검증 클래스(DomainErrorType, ValidationRules, TypedValidationExtensions)는 다음과 같은 일관된 범주 구조를 따릅니다:

| DomainErrorType | ValidationRules | TypedValidationExtensions |
|-----------------|-----------------|---------------------------|
| Presence | Presence | Presence |
| Length | Length | Length |
| Format | Format | Format |
| DateTime | DateTime | DateTime |
| Numeric | Numeric | Numeric |
| Range | Range | Range |
| Existence | (Must 사용) | (ThenMust 사용) |
| Custom | Custom | Generic |
| - | Collection | Collection |

#### 범주별 메서드 및 ErrorType

| 범주 | 메서드 | ErrorType | 설명 |
|------|--------|-----------|------|
| **Presence** | `NotNull` | `Null` | null 검증 |
| **Length** | `NotEmpty`, `MinLength`, `MaxLength`, `ExactLength` | `Empty`, `TooShort`, `TooLong`, `WrongLength` | 문자열/컬렉션 길이 검증 |
| **Format** | `Matches`, `IsUpperCase`, `IsLowerCase` | `InvalidFormat`, `NotUpperCase`, `NotLowerCase` | 형식 및 대소문자 검증 |
| **DateTime** | `NotDefault`, `InPast`, `InFuture`, `Before`, `After`, `DateBetween` | `DefaultDate`, `NotInPast`, `NotInFuture`, `TooLate`, `TooEarly`, `OutOfRange` | 날짜 검증 |
| **Numeric** | `Positive`, `NonNegative`, `NotZero`, `Between`, `AtMost`, `AtLeast` | `NotPositive`, `Negative`, `Zero`, `OutOfRange`, `AboveMaximum`, `BelowMinimum` | 숫자 값/범위 검증 |
| **Range** | `ValidRange`, `ValidStrictRange` | `RangeInverted`, `RangeEmpty` | min/max 쌍 검증 |
| **Collection** | `NotEmptyArray` | `Empty` | 배열 검증 |
| **Custom** | `Must`, `ThenMust` | `Custom(Name)` | 사용자 정의 검증 |

### ValidationRules\<T\> 시작점

**위치**: `Functorium.Domains.ValueObjects.Validations.ValidationRules<TValueObject>`

타입 파라미터를 한 번만 지정하면 체이닝에서 반복하지 않아도 됩니다.

#### Presence 검증 메서드

```csharp
ValidationRules<User>.NotNull(value)                // null이 아님 (참조 타입)
ValidationRules<User>.NotNull(nullableValue)        // null이 아님 (nullable 값 타입)
```

| 메서드 | ErrorType | 오류 메시지 |
|--------|-----------|------------|
| `NotNull` | `Null` | `{Type} cannot be null.` |

#### Length 검증 메서드

```csharp
ValidationRules<Email>.NotEmpty(value)              // 비어있지 않음
ValidationRules<Email>.MinLength(value, 8)          // 최소 길이
ValidationRules<Email>.MaxLength(value, 100)        // 최대 길이
ValidationRules<Email>.ExactLength(value, 10)       // 정확한 길이
```

| 메서드 | ErrorType | 오류 메시지 |
|--------|-----------|------------|
| `NotEmpty` | `Empty` | `{Type} cannot be empty. Current value: '{v}'` |
| `MinLength` | `TooShort(n)` | `{Type} must be at least {n} characters. Current length: {len}` |
| `MaxLength` | `TooLong(n)` | `{Type} must not exceed {n} characters. Current length: {len}` |
| `ExactLength` | `WrongLength(n)` | `{Type} must be exactly {n} characters. Current length: {len}` |

#### Format 검증 메서드

```csharp
ValidationRules<Email>.Matches(value, regex)        // 정규식 패턴
ValidationRules<Email>.Matches(value, regex, msg)   // 정규식 + 커스텀 메시지
ValidationRules<Code>.IsUpperCase(value)            // 대문자 검증
ValidationRules<Code>.IsLowerCase(value)            // 소문자 검증
```

| 메서드 | ErrorType | 오류 메시지 |
|--------|-----------|------------|
| `Matches` | `InvalidFormat(pattern)` | `Invalid {Type} format. Current value: '{v}'` |
| `IsUpperCase` | `NotUpperCase` | `{Type} must be uppercase. Current value: '{v}'` |
| `IsLowerCase` | `NotLowerCase` | `{Type} must be lowercase. Current value: '{v}'` |

#### Numeric 검증 메서드

`INumber<T>` 제약으로 모든 숫자 타입(int, decimal, double 등)에서 동작합니다:

```csharp
ValidationRules<Price>.Positive(value)              // > 0
ValidationRules<Age>.NonNegative(value)             // >= 0
ValidationRules<Denominator>.NotZero(value)         // != 0
ValidationRules<Age>.Between(value, 0, 150)         // min <= value <= max
ValidationRules<Age>.AtMost(value, 150)             // <= max
ValidationRules<Age>.AtLeast(value, 0)              // >= min
```

| 메서드 | ErrorType | 오류 메시지 |
|--------|-----------|------------|
| `Positive` | `NotPositive` | `{Type} must be positive. Current value: '{v}'` |
| `NonNegative` | `Negative` | `{Type} cannot be negative. Current value: '{v}'` |
| `NotZero` | `Zero` | `{Type} cannot be zero. Current value: '{v}'` |
| `Between` | `OutOfRange(min, max)` | `{Type} must be between {min} and {max}. Current value: '{v}'` |
| `AtMost` | `AboveMaximum(max)` | `{Type} cannot exceed {max}. Current value: '{v}'` |
| `AtLeast` | `BelowMinimum(min)` | `{Type} must be at least {min}. Current value: '{v}'` |

#### Collection 검증 메서드

```csharp
ValidationRules<BinaryData>.NotEmptyArray(value)    // 배열이 null이 아니고 길이 > 0
```

| 메서드 | ErrorType | 오류 메시지 |
|--------|-----------|------------|
| `NotEmptyArray` | `Empty` | `{Type} array cannot be empty or null. Current length: '{len}'` |

#### Range 검증 메서드

```csharp
ValidationRules<PriceRange>.ValidRange(minValue, maxValue)        // min <= max 검증, (min, max) 튜플 반환
ValidationRules<DateRange>.ValidStrictRange(minValue, maxValue)   // min < max 검증, (min, max) 튜플 반환
```

| 메서드 | ErrorType | 오류 메시지 |
|--------|-----------|------------|
| `ValidRange` | `RangeInverted(min, max)` | `{Type} range is invalid. Minimum ({min}) cannot exceed maximum ({max}).` |
| `ValidStrictRange` | `RangeInverted(min, max)` | `{Type} range is invalid. Minimum ({min}) cannot exceed maximum ({max}).` |
| `ValidStrictRange` | `RangeEmpty(value)` | `{Type} range is empty. Start ({value}) equals end ({value}).` |

#### DateTime 검증 메서드

```csharp
ValidationRules<Birthday>.NotDefault(value)         // != DateTime.MinValue
ValidationRules<Birthday>.InPast(value)             // < DateTime.Now
ValidationRules<ExpiryDate>.InFuture(value)         // > DateTime.Now
ValidationRules<EndDate>.Before(value, boundary)    // < boundary
ValidationRules<StartDate>.After(value, boundary)   // > boundary
ValidationRules<EventDate>.DateBetween(value, min, max)  // min <= value <= max
```

| 메서드 | ErrorType | 오류 메시지 |
|--------|-----------|------------|
| `NotDefault` | `DefaultDate` | `{Type} date cannot be default. Current value: '{v}'` |
| `InPast` | `NotInPast` | `{Type} must be in the past. Current value: '{v}'` |
| `InFuture` | `NotInFuture` | `{Type} must be in the future. Current value: '{v}'` |
| `Before` | `TooLate(boundary)` | `{Type} must be before {boundary}. Current value: '{v}'` |
| `After` | `TooEarly(boundary)` | `{Type} must be after {boundary}. Current value: '{v}'` |
| `DateBetween` | `OutOfRange(min, max)` | `{Type} must be between {min} and {max}. Current value: '{v}'` |

#### 커스텀 검증 메서드

```csharp
ValidationRules<Currency>.Must(
    value,
    v => SupportedCurrencies.Contains(v),
    new Custom("Unsupported"),
    $"Currency '{value}' is not supported")
```

### TypedValidation 체이닝

**위치**: `Functorium.Domains.ValueObjects.TypedValidationExtensions`

`ValidationRules<T>`가 반환하는 `TypedValidation<TValueObject, T>`에 대한 체이닝 메서드입니다.

#### Presence 체이닝

| 메서드 | 설명 |
|--------|------|
| `ThenNotNull()` | null이 아닌지 검증 |

#### Length 체이닝

| 메서드 | 설명 |
|--------|------|
| `ThenNotEmpty()` | 비어있지 않은지 검증 |
| `ThenMinLength(n)` | 최소 길이 검증 |
| `ThenMaxLength(n)` | 최대 길이 검증 |
| `ThenExactLength(n)` | 정확한 길이 검증 |
| `ThenNormalize(func)` | 값 변환 (Map) |

#### Format 체이닝

| 메서드 | 설명 |
|--------|------|
| `ThenMatches(regex)` | 정규식 패턴 검증 |
| `ThenMatches(regex, message)` | 정규식 + 커스텀 메시지 |
| `ThenIsUpperCase()` | 대문자 검증 |
| `ThenIsLowerCase()` | 소문자 검증 |

#### Numeric 체이닝

| 메서드 | 설명 |
|--------|------|
| `ThenPositive()` | 양수 검증 |
| `ThenNonNegative()` | 0 이상 검증 |
| `ThenNotZero()` | 0이 아닌지 검증 |
| `ThenBetween(min, max)` | 범위 검증 |
| `ThenAtMost(max)` | 최대값 이하 검증 |
| `ThenAtLeast(min)` | 최소값 이상 검증 |

#### DateTime 체이닝

| 메서드 | 설명 |
|--------|------|
| `ThenNotDefault()` | 기본값(DateTime.MinValue)이 아닌지 검증 |
| `ThenInPast()` | 과거 날짜인지 검증 |
| `ThenInFuture()` | 미래 날짜인지 검증 |
| `ThenBefore(boundary)` | 기준 날짜 이전인지 검증 |
| `ThenAfter(boundary)` | 기준 날짜 이후인지 검증 |
| `ThenDateBetween(min, max)` | 날짜 범위 내인지 검증 |

#### Range 체이닝

| 메서드 | 설명 |
|--------|------|
| `ThenValidRange()` | 범위가 유효한지 검증 (min <= max) |
| `ThenValidStrictRange()` | 엄격한 범위 검증 (min < max) |

#### Collection 체이닝

| 메서드 | 설명 |
|--------|------|
| `ThenNotEmptyArray()` | 배열이 비어있지 않은지 검증 |

#### Generic/커스텀 체이닝

| 메서드 | 설명 |
|--------|------|
| `ThenMust(predicate, errorType, message)` | 커스텀 조건 (고정 메시지) |
| `ThenMust(predicate, errorType, messageFactory)` | 커스텀 조건 (메시지 생성 함수) |

```csharp
// 메시지 생성 함수 사용
.ThenMust(
    v => SupportedCurrencies.Contains(v),
    new Custom("Unsupported"),
    v => $"Currency '{v}' is not supported")  // 값 포함 동적 메시지
```

### TypedValidation\<TValueObject, T\>

**위치**: `Functorium.Domains.ValueObjects.TypedValidation<TValueObject, T>`

타입 정보를 체이닝 중 전달하는 wrapper struct입니다.

```csharp
public readonly struct TypedValidation<TValueObject, T>
{
    public Validation<Error, T> Value { get; }

    // Validation<Error, T>로 암시적 변환
    public static implicit operator Validation<Error, T>(TypedValidation<TValueObject, T> typed);
}
```

**성능 특성:**
- 8바이트 readonly struct (스택 할당)
- 모든 메서드에 `AggressiveInlining` 적용
- `TValueObject`는 팬텀 타입 파라미터 (런타임에 사용되지 않음)

#### LINQ 지원 (SelectMany, Select)

TypedValidation은 LINQ query expression을 지원합니다. 명시적 캐스팅 없이 `from...in` 구문을 사용할 수 있습니다.

```csharp
// 캐스팅 없이 LINQ query expression 사용
public static Validation<Error, (DateTime Min, DateTime Max)> Validate(DateTime startDate, DateTime endDate) =>
    from validStartDate in ValidationRules<DateRange>.NotDefault(startDate)
    from validEndDate in ValidationRules<DateRange>.NotDefault(endDate)
    from validRange in ValidationRules<DateRange>.ValidStrictRange(validStartDate, validEndDate)
    select validRange;
```

| 메서드 | 설명 |
|--------|------|
| `SelectMany` | TypedValidation → Validation 또는 TypedValidation → TypedValidation 체이닝 |
| `Select` | 값 변환 (Map) |
| `ToValidation()` | TypedValidation을 Validation으로 명시적 변환 |

#### Tuple Apply 지원

`Validation<Error, T>` 또는 `TypedValidation<TValueObject, T>` 튜플에 대한 Apply 오버로드를 제공합니다. `.As()` 없이 사용할 수 있습니다.

```csharp
// Validation 튜플 - .As() 불필요
(ValidateAmount(amount), ValidateCurrency(currency))
    .Apply((a, c) => new Money(a, c));  // Validation<Error, Money> 직접 반환

// TypedValidation 포함 튜플 - .As() 불필요
(ValidateCurrency(baseCurrency), 
 ValidateCurrency(quoteCurrency),
 ValidationRules<ExchangeRate>.Positive(rate))  // TypedValidation
    .Apply((b, q, r) => (b, q, r));  // Validation<Error, T> 직접 반환
```

---

## 오류 시스템

### DomainErrorType 계층

**위치**: `Functorium.Domains.Errors.DomainErrorType`

sealed record 계층으로 타입 안전한 에러 정의를 제공합니다. partial 클래스로 범주별 파일에 분리되어 있습니다.

```csharp
using static Functorium.Domains.Errors.DomainErrorType;
```

#### 범주 구조

| 범주 | 파일 | 설명 |
|------|------|------|
| Presence | `DomainErrorType.Presence.cs` | 값 존재 검증 |
| Length | `DomainErrorType.Length.cs` | 문자열/컬렉션 길이 검증 |
| Format | `DomainErrorType.Format.cs` | 형식 및 대소문자 검증 |
| DateTime | `DomainErrorType.DateTime.cs` | 날짜 검증 |
| Numeric | `DomainErrorType.Numeric.cs` | 숫자 값/범위 검증 |
| Range | `DomainErrorType.Range.cs` | min/max 쌍 검증 |
| Existence | `DomainErrorType.Existence.cs` | 존재 여부/비즈니스 규칙 검증 |
| Custom | `DomainErrorType.Custom.cs` | 커스텀 에러 |

#### Presence (값 존재 검증)

| ErrorType | 설명 |
|-----------|------|
| `Empty` | 비어있음 (null, empty string, empty collection) |
| `Null` | null임 |

#### Length (문자열/컬렉션 길이 검증)

| ErrorType | 설명 |
|-----------|------|
| `TooShort(MinLength)` | 최소 길이 미만 |
| `TooLong(MaxLength)` | 최대 길이 초과 |
| `WrongLength(Expected)` | 정확한 길이 불일치 |

#### Format (형식 검증)

| ErrorType | 설명 |
|-----------|------|
| `InvalidFormat(Pattern?)` | 형식 불일치 |
| `NotUpperCase` | 대문자가 아님 |
| `NotLowerCase` | 소문자가 아님 |

#### DateTime (날짜 검증)

| ErrorType | 설명 |
|-----------|------|
| `DefaultDate` | 날짜가 기본값(DateTime.MinValue)임 |
| `NotInPast` | 날짜가 과거여야 하는데 미래임 |
| `NotInFuture` | 날짜가 미래여야 하는데 과거임 |
| `TooLate(Boundary?)` | 날짜가 기준보다 늦음 (이전이어야 함) |
| `TooEarly(Boundary?)` | 날짜가 기준보다 이름 (이후여야 함) |

#### Numeric (숫자 검증)

| ErrorType | 설명 |
|-----------|------|
| `Zero` | 0임 |
| `Negative` | 음수임 |
| `NotPositive` | 양수가 아님 (0 포함) |
| `OutOfRange(Min?, Max?)` | 범위 밖 |
| `BelowMinimum(Minimum?)` | 최소값 미만 |
| `AboveMaximum(Maximum?)` | 최대값 초과 |

#### Range (범위 쌍 검증)

| ErrorType | 설명 |
|-----------|------|
| `RangeInverted(Min?, Max?)` | 범위가 역전됨 (최소값이 최대값보다 큼) |
| `RangeEmpty(Value?)` | 범위가 비어있음 (최소값과 최대값이 같음, 엄격한 범위에서 유효한 값이 없음) |

#### Existence (존재 여부/비즈니스 규칙 검증)

| ErrorType | 설명 |
|-----------|------|
| `NotFound` | 찾을 수 없음 |
| `AlreadyExists` | 이미 존재함 |
| `Duplicate` | 중복됨 |
| `Mismatch` | 값 불일치 |

#### Custom (커스텀 에러)

| ErrorType | 설명 |
|-----------|------|
| `Custom(Name)` | 도메인 특화 에러 |

### DomainError.For\<T\>() 헬퍼

**위치**: `Functorium.Domains.Errors.DomainError`

에러 코드를 `DomainErrors.{TypeName}.{ErrorName}` 형식으로 자동 생성합니다.

```csharp
// 단일 값
DomainError.For<Email>(new Empty(), value, "Email cannot be empty");
// -> ErrorCode: "DomainErrors.Email.Empty"

// 제네릭 값
DomainError.For<Age, int>(new Negative(), -5, "Age cannot be negative");

// 두 값
DomainError.For<DateRange, DateTime, DateTime>(
    new Custom("InvalidRange"), startDate, endDate, "Start must be before end");

// 세 값
DomainError.For<Triangle, double, double, double>(
    new Custom("InvalidTriangle"), a, b, c, "Invalid triangle sides");
```

---

## 구현 패턴

### Create/Validate 패턴

| 기반 클래스 | Create 패턴 | Validate 반환 |
|------------|-------------|---------------|
| `SimpleValueObject<T>` | `CreateFromValidation(Validate(value), factory)` | `Validation<Error, T>` |
| `ComparableSimpleValueObject<T>` | `CreateFromValidation(Validate(value), factory)` | `Validation<Error, T>` |
| `ValueObject` (Apply) | `CreateFromValidation(Validate(...), factory)` | `Validation<Error, (T1, T2, ...)>` |

### 순차 검증 (Bind/Then)

첫 오류에서 중단. 의존 관계가 있는 검증에 적합:

```csharp
public static Validation<Error, string> Validate(string? value) =>
    ValidationRules<Email>.NotEmpty(value ?? "")  // 1. 빈 값 검증
        .ThenMatches(EmailPattern)          // 2. 형식 검증 (1 통과 시)
        .ThenMaxLength(254);                // 3. 길이 검증 (2 통과 시)
```

### 병렬 검증 (Apply)

모든 오류 수집. 독립적인 검증에 적합:

```csharp
public static Validation<Error, (decimal Amount, string Currency)> Validate(decimal amount, string currency) =>
    (ValidateAmount(amount), ValidateCurrency(currency))
        .Apply((a, c) => (Amount: a, Currency: c));
```

> **참고**: Functorium은 `Validation<Error, T>` 및 `TypedValidation<TValueObject, T>` 튜플에 대한
> Apply 오버로드를 제공하여 `.As()` 호출 없이 사용할 수 있습니다.

### 혼합 패턴 (Apply + Bind)

병렬 검증 후 의존 검증:

```csharp
// 튜플에 TypedValidation이 포함되면 .As() 불필요
public static Validation<Error, (string BaseCurrency, string QuoteCurrency, decimal Rate)> Validate(
    string baseCurrency, string quoteCurrency, decimal rate) =>
    (ValidateCurrency(baseCurrency),
     ValidateCurrency(quoteCurrency),
     ValidationRules<ExchangeRate>.Positive(rate))  // TypedValidation
        .Apply((b, q, r) => (BaseCurrency: b, QuoteCurrency: q, Rate: r))
        .Bind(v => ValidateDifferentCurrencies(v.BaseCurrency, v.QuoteCurrency)
            .Map(_ => (v.BaseCurrency, v.QuoteCurrency, v.Rate)));
```

---

## FluentValidation 통합

Value Object의 `Validate` 메서드를 FluentValidation과 통합하여 `Application Layer` `UsecaseValidationPipeline`에서 재사용할 수 있습니다.

**위치**: `Functorium.Applications.Validations.FluentValidationExtensions`

### MustSatisfyValidation (입력 타입 == 출력 타입)

검증 메서드의 입력 타입과 출력 타입이 동일한 경우 사용합니다. C# 14 extension members를 활용하여 타입 추론이 작동합니다.

```csharp
// decimal → Validation<Error, decimal>
public static Validation<Error, decimal> ValidateAmount(decimal amount) =>
    ValidationRules<Money>.NonNegative(amount);

// Application Layer UsecaseValidationPipeline
// FluentValidation에서 사용 (타입 추론 작동)
public sealed class Validator : AbstractValidator<Request>
{
    public Validator()
    {
        RuleFor(x => x.Price)
            .MustSatisfyValidation(Money.ValidateAmount);

        RuleFor(x => x.Currency)
            .MustSatisfyValidation(Money.ValidateCurrency);

        RuleFor(x => x.ProductId)
            .MustSatisfyValidation(ProductId.Validate);
    }
}
```

### MustSatisfyValidationOf (입력 타입 != 출력 타입)

검증 메서드의 입력 타입과 출력 타입이 다른 경우 사용합니다. 모든 타입 파라미터를 명시해야 합니다.

```csharp
// string → Validation<Error, int> (입력 타입 != 출력 타입)
public sealed class Age : ComparableSimpleValueObject<int>
{
    public static Validation<Error, int> Validate(string value) =>
        int.TryParse(value, out var parsed)
            ? ValidationRules<Age>.Between(parsed, 0, 150)
            : DomainError.For<Age>(new Custom("InvalidFormat"), value,
                $"'{value}'은(는) 유효한 숫자가 아닙니다");
}

// Application Layer UsecaseValidationPipeline
// FluentValidation에서 사용 (모든 타입 파라미터 명시 필요)
public sealed class Validator : AbstractValidator<Request>
{
    public Validator()
    {
        // MustSatisfyValidationOf<TRequest, TProperty, TValueObject>
        RuleFor(x => x.Age)
            .MustSatisfyValidationOf<Request, string, int>(Age.Validate);
    }
}
```

### 메서드 선택 가이드

| 검증 메서드 시그니처 | 사용 메서드 | 타입 명시 |
|---------------------|-------------|----------|
| `Func<T, Validation<Error, T>>` | `MustSatisfyValidation` | 불필요 (타입 추론) |
| `Func<TIn, Validation<Error, TOut>>` | `MustSatisfyValidationOf` | 필요 (`<TRequest, TIn, TOut>`) |

### C# 14 Extension Members 제한 사항

`MustSatisfyValidationOf`에서 모든 타입 파라미터를 명시해야 하는 이유:

| 문제 | 설명 |
|------|------|
| **C# 14 제한** | Extension members는 추가 제네릭 타입 파라미터가 있을 때 파생 인터페이스에서 타입 추론 불가 |
| **RuleFor 반환 타입** | `RuleFor()`는 `IRuleBuilderInitial<T, TProperty>` 반환 |
| **해결책** | `IRuleBuilderInitial`을 직접 대상으로 하는 전통적인 확장 메서드 제공 |

---

## 실전 예제

### Email (SimpleValueObject)

```csharp
using Functorium.Domains.ValueObjects;
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

```csharp
using Functorium.Domains.ValueObjects;

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

```csharp
using Functorium.Domains.ValueObjects;
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

## SmartEnum 열거형

`Ardalis.SmartEnum`을 사용한 타입 안전한 열거형입니다. `SimpleValueObject`를 상속받지 않으므로 별도 패턴을 사용합니다.

### 기본 구조

```csharp
using Ardalis.SmartEnum;
using Functorium.Domains.ValueObjects;

public sealed class Currency : SmartEnum<Currency, string>, IValueObject
{
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
            return DomainError.For<Currency>(new Custom("Unsupported"), currencyCode,
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

## FAQ

### Q1. 기반 클래스 선택 기준은?

| 조건 | 선택 |
|------|------|
| 단일 값 래핑 | `SimpleValueObject<T>` |
| 단일 값 + 비교/정렬 필요 | `ComparableSimpleValueObject<T>` |
| 복합 속성 | `ValueObject` |
| 복합 속성 + 비교/정렬 필요 | `ComparableValueObject` |
| 열거형 + 도메인 로직 | `SmartEnum<T, TValue>` |

### Q2. ValidationRules<T>와 DomainError.For<T>() 사용 기준은?

| 상황 | 권장 |
|------|------|
| 일반적인 검증 | `ValidationRules<T>` + 체이닝 |
| 커스텀 비즈니스 규칙 | `ThenMust` 또는 `DomainError.For<T>()` |
| 도메인 연산 중 오류 | `DomainError.For<T>()` |

### Q3. Bind(Then)와 Apply 사용 기준은?

| 전략 | 사용 시점 | 특징 |
|------|----------|------|
| `Bind` / `Then*` | 검증 간 의존 관계 | 첫 오류에서 중단 |
| `Apply` | 독립적인 검증 | 모든 오류 수집 |

### Q4. Value 속성에 접근하려면?

`Value`는 `protected`입니다. 외부 접근이 필요하면:

```csharp
// 방법 1: 암시적 변환
public static implicit operator string(Email email) => email.Value;
string raw = email;

// 방법 2: 파생 속성
public string LocalPart { get; }  // 생성자에서 계산

// 방법 3: ToString()
public override string ToString() => Value;
```

### Q5. 언제 SmartEnum을 사용하나요?

| 상황 | 선택 |
|------|------|
| 단순한 상태/플래그 | 기존 C# enum |
| 값마다 고유 속성 필요 | SmartEnum |
| 값마다 다른 동작 필요 | SmartEnum |
| 런타임 타입 안전성 중요 | SmartEnum |

---

## 참고 문서

- [레이어별 에러 코드 정의 가이드](./layered-error-definition-guide.md)
- [단위 테스트 가이드](./unit-testing-guide.md)
- [LanguageExt](https://github.com/louthy/language-ext)
- [Ardalis.SmartEnum](https://github.com/ardalis/SmartEnum)
