---
title: "값 객체 (Value Object)"
---

이 문서는 Functorium 프레임워크의 값 객체(Value Object) 핵심 개념, 클래스 계층 구조, 검증 시스템, 구현 패턴을 다룹니다. 열거형 패턴, Application 검증, FAQ는 [05b-value-objects-validation.md](./05b-value-objects-validation.md)을 참고하세요.

## 목차

- [요약](#요약)
- [왜 값 객체인가](#왜-값-객체인가)
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
  - [Contextual 검증 (Named Context)](#contextual-검증-named-context)
  - [IValidationContext를 이용한 검증 (Context Class)](#ivalidationcontext를-이용한-검증-context-class)
- [유스케이스 파이프라인 검증 시스템](#유스케이스-파이프라인-검증-시스템)
  - [MustSatisfyValidation (입력 타입 == 출력 타입)](#mustsatisfyvalidation-입력-타입--출력-타입)
  - [MustSatisfyValidationOf (입력 타입 != 출력 타입)](#mustsatisfyvalidationof-입력-타입--출력-타입)
- [오류 시스템](#오류-시스템)
  - [DomainErrorType 개요](#domainerrortype-개요)
  - [DomainError.For\<T\>() 헬퍼](#domainerrorfort-헬퍼)
- [구현 패턴](#구현-패턴)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)
- [참고 문서](#참고-문서)

---

## 요약

### 주요 명령

```csharp
// 값 객체 생성 (검증 포함)
Fin<Email> email = Email.Create("user@example.com");

// 값 객체 검증만 (객체 생성 없음)
Validation<Error, string> result = Email.Validate("user@example.com");

// 검증 체이닝
ValidationRules<Email>.NotEmpty(value).ThenMatches(pattern).ThenMaxLength(254);

// FluentValidation 통합
RuleFor(x => x.Price).MustSatisfyValidation(Money.ValidateAmount);
```

### 주요 절차

1. 기반 클래스 선택 (`SimpleValueObject<T>`, `ComparableSimpleValueObject<T>`, `ValueObject`)
2. `Validate()` 메서드 구현 - `ValidationRules<T>`로 검증 로직 정의, `Validation<Error, T>` 반환
3. `Create()` 메서드 구현 - `CreateFromValidation(Validate(value), factory)` 호출, `Fin<T>` 반환
4. 필요 시 FluentValidation에서 `MustSatisfyValidation`으로 검증 재사용

### 주요 개념

| 개념 | 설명 |
|------|------|
| Create/Validate 분리 | `Validate`는 검증만, `Create`는 검증 + 객체 생성. 검증 로직 재사용 가능 |
| 순차 검증 (Bind) | 첫 오류에서 중단. 의존 관계가 있는 검증에 사용 |
| 병렬 검증 (Apply) | 모든 오류를 수집. 독립적인 검증에 사용 |
| 세 가지 검증 방식 | Typed(`ValidationRules<T>`), Context Class(`IValidationContext`), Named Context(`ValidationRules.For()`) |
| 결과 타입 | `Fin<T>` (단일 에러), `Validation<Error, T>` (에러 누적) |

---

## 왜 값 객체인가

DDD 전술적 설계에서 값 객체는 **도메인 개념을 명시적으로 표현하는 가장 기본적인 빌딩블록**입니다.

### Primitive Obsession 방지

원시 타입(string, int, decimal)만 사용하면 도메인 지식이 코드에 드러나지 않습니다. 값 객체는 도메인 개념에 이름과 규칙을 부여합니다.

| 원시 타입 | 값 객체 | 효과 |
|----------|--------|------|
| `string email` | `Email email` | 컴파일 타임 타입 안전성 |
| `decimal price` | `Price price` | 음수 불가, 최대값 제한 자동 적용 |
| `string currency` | `Currency currency` | 지원 통화만 허용 |

### "잘못된 상태는 표현 불가" (Make Illegal States Unrepresentable)

값 객체는 생성 시점에 유효성을 검증하여, 시스템 내에서 유효하지 않은 값이 존재할 수 없도록 보장합니다. 한 번 생성된 값 객체는 항상 유효합니다.

### 불변성으로 부수효과 제거

값 객체는 생성 후 변경할 수 없으므로 스레드 안전하고 예측 가능합니다. 값을 변경해야 할 때는 새로운 값 객체를 생성합니다.

### 판단 기준: 언제 값 객체를 만드는가

- 도메인에서 특별한 의미를 가진 값 (이메일, 가격, 수량)
- 유효성 검증이 필요한 값
- 여러 곳에서 동일한 규칙으로 사용되는 값
- 두 개 이상의 원시 값이 함께 의미를 형성하는 경우 (금액+통화 → Money)

---

## 개요

값 객체(Value Object)는 도메인 주도 설계(DDD)의 핵심 전술 패턴 중 하나입니다. "이메일 주소", "가격", "금액" 같은 도메인 개념을 **원시 타입(string, decimal) 대신 전용 타입으로 표현**합니다.

### 왜 값 객체를 사용하나요?

원시 타입만 사용하면 다음과 같은 문제가 발생합니다:

```csharp
// 문제점 1: 의미가 불명확함
public void ProcessOrder(string email, decimal price, string currency);

// 문제점 2: 잘못된 값 전달 가능 (컴파일 오류 없음)
ProcessOrder(currency, price, email);  // 순서 착각 - 런타임에야 발견

// 문제점 3: 유효하지 않은 값이 시스템 전체로 퍼짐
var email = "not-an-email";  // 아무 문자열이나 이메일로 사용 가능
```

값 객체는 이 문제들을 해결합니다:

```csharp
// 해결책: 타입으로 의미를 표현
public void ProcessOrder(Email email, Price price, Currency currency);

// 컴파일 오류로 실수 방지
ProcessOrder(currency, price, email);  // 컴파일 오류!

// 생성 시점에 유효성 검증
var email = Email.Create("not-an-email");  // Fin<Email> - 실패 결과 반환
```

### 핵심 특징

| 특성 | 설명 |
|------|------|
| **불변성** | 생성 후 변경 불가. 스레드 안전하고 부작용 없음 |
| **값 기반 동등성** | 속성 값이 같으면 같은 객체. 참조가 아닌 내용으로 비교 |
| **자기 검증** | 생성 시 유효성 검증. 잘못된 상태의 객체는 존재할 수 없음 |
| **도메인 로직 캡슐화** | 관련 연산(비교, 변환, 계산)을 타입 내부에 포함 |

### 기반 클래스 선택

| 사용 시나리오 | 기반 클래스 | 특징 |
|--------------|------------|------|
| 복합 속성 | `ValueObject` | 여러 속성으로 동등성 판단 |
| 단일 값 래핑 | `SimpleValueObject<T>` | 단일 값으로 동등성 판단 |
| 복합 속성 + 비교 | `ComparableValueObject` | 정렬, 비교 연산 지원 |
| 단일 값 + 비교 | `ComparableSimpleValueObject<T>` | 정렬, 비교 연산 지원 |
| 타입 안전한 열거형 | `SmartEnum<T, TValue>` (Ardalis.SmartEnum) | 도메인 로직 내장 열거형. IValueObject 수동 구현 필요 |

### 핵심 패턴

```csharp
using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations.Typed;
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

Functorium은 다양한 값 객체 유형에 대응하는 기반 클래스 계층을 제공합니다. 각 클래스는 특정 시나리오에 최적화되어 있으며, 상위 클래스의 기능을 상속받습니다.

```
IValueObject (인터페이스)
|
AbstractValueObject (추상 클래스)
+-- GetEqualityComponents() - 동등성 컴포넌트
+-- Equals() / GetHashCode() - 값 기반 동등성
+-- == / != 연산자
`-- 프록시 타입 처리 (ORM 지원)
    |
    `-- ValueObject
        +-- CreateFromValidation<TValueObject, TValue>() 헬퍼
        |
        +-- SimpleValueObject<T>
        |   +-- protected T Value
        |   +-- CreateFromValidation<TValueObject>() 헬퍼
        |   `-- explicit operator T
        |
        `-- ComparableValueObject
            +-- GetComparableEqualityComponents()
            +-- IComparable<ComparableValueObject>
            +-- < / <= / > / >= 연산자
            |
            `-- ComparableSimpleValueObject<T>
                +-- protected T Value
                +-- CreateFromValidation<TValueObject>() 헬퍼
                `-- explicit operator T
```

**계층 이해하기:**

- **IValueObject**: 모든 값 객체가 구현하는 마커 인터페이스. SmartEnum은 IValueObject를 자동으로 구현하지 않으므로, SmartEnum 기반 값 객체에서는 IValueObject를 명시적으로 구현해야 합니다.
- **AbstractValueObject**: 동등성 비교(`Equals`, `GetHashCode`, `==`, `!=`)를 자동 구현. ORM 프록시 타입도 처리합니다.
- **ValueObject**: 복합 속성 값 객체의 기반. `CreateFromValidation` 헬퍼 메서드를 제공합니다.
- **SimpleValueObject\<T\>**: 단일 값 래핑용. `GetEqualityComponents()`가 자동 구현됩니다.
- **ComparableValueObject / ComparableSimpleValueObject\<T\>**: 비교 연산자(`<`, `>`, `<=`, `>=`)와 정렬을 지원합니다.

---

## 기반 클래스

값 객체를 구현할 때 가장 먼저 할 일은 **어떤 기반 클래스를 상속받을지** 결정하는 것입니다. 아래 두 가지 질문으로 쉽게 선택할 수 있습니다:

**질문 1: 몇 개의 값으로 구성되나요?**
- 하나의 값 → `SimpleValueObject<T>` 계열 (이메일, 가격, ID 등)
- 여러 속성 → `ValueObject` 계열 (금액+통화, 주소, 좌표 등)

**질문 2: 크기 비교/정렬이 필요한가요?**
- 필요 없음 → 기본 클래스 (`SimpleValueObject<T>`, `ValueObject`)
- 필요함 → Comparable 클래스 (`ComparableSimpleValueObject<T>`, `ComparableValueObject`)

```
단일 값인가요?
    |
    +-- 예 --> 비교/정렬이 필요한가요?
    |              |
    |              +-- 예 --> ComparableSimpleValueObject<T>
    |              |
    |              `-- 아니오 --> SimpleValueObject<T>
    |
    `-- 아니오 --> 비교/정렬이 필요한가요?
                       |
                       +-- 예 --> ComparableValueObject
                       |
                       `-- 아니오 --> ValueObject
```

### ValueObject

복합 속성으로 구성된 값 객체의 기반 클래스입니다. 여러 속성의 조합이 하나의 개념을 표현할 때 사용합니다 (예: 금액 + 통화 = Money).

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

단일 값을 래핑하는 값 객체의 기반 클래스입니다. **가장 많이 사용되는 기반 클래스**로, 하나의 원시 타입에 도메인 의미를 부여할 때 사용합니다. `GetEqualityComponents()`가 자동 구현되어 `Value`를 반환합니다.

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

비교 가능한 복합 값 객체의 기반 클래스입니다. 복합 속성이면서 정렬이나 크기 비교가 필요할 때 사용합니다 (예: 시작일+종료일로 구성된 DateRange의 기간 비교).

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

비교 가능한 단일 값 객체의 기반 클래스입니다. 단일 값이면서 "더 크다/작다" 비교가 의미 있을 때 사용합니다 (예: 가격 비교, 수량 정렬, 나이 범위 검증).

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

값 객체의 **자기 검증(Self-Validation)** 원칙을 구현하는 시스템입니다. 모든 값 객체는 생성 시점에 유효성을 검증하여 **잘못된 상태의 객체가 존재할 수 없도록** 보장합니다.

### 검증의 핵심 개념

Functorium 검증 시스템은 **Railway Oriented Programming** 패턴을 따릅니다. 검증은 두 트랙(성공/실패)을 따라 진행됩니다:

```
Input ──┬── [Validate 1] ──┬── [Validate 2] ──┬── [Validate 3] ──┬── Success (Valid Value)
        │                  │                  │                  │
        └── Fail ──────────┴── Fail ──────────┴── Fail ──────────┴── Fail (Error)
```

**순차 검증 (Bind/Then)**: 이전 검증이 통과해야 다음 검증을 실행합니다. 의존 관계가 있는 검증에 적합합니다.
```csharp
ValidationRules<Email>.NotEmpty(value)    // 1. 먼저 비어있는지 확인
    .ThenMatches(EmailPattern)            // 2. 비어있지 않아야 패턴 검증 가능
    .ThenMaxLength(254);                  // 3. 형식이 맞아야 길이 검증 의미 있음
```

**병렬 검증 (Apply)**: 모든 검증을 독립적으로 실행하고 모든 오류를 수집합니다. 독립적인 필드 검증에 적합합니다.
```csharp
(ValidateAmount(amount), ValidateCurrency(currency))
    .Apply((a, c) => (a, c));  // 두 검증 모두 실행, 모든 오류 수집
```

### 네임스페이스 구조

검증 시스템은 세 개의 네임스페이스로 구성됩니다:

| 네임스페이스 | 용도 | 주요 클래스 |
|-------------|------|------------|
| `Functorium.Domains.ValueObjects.Validations` | 공통 인프라 | `ValidationApplyExtensions`, `IValidationContext` |
| `Functorium.Domains.ValueObjects.Validations.Typed` | Value Object / Context Class 검증 | `ValidationRules<T>`, `TypedValidation<T,V>`, `TypedValidationExtensions` |
| `Functorium.Domains.ValueObjects.Validations.Contextual` | Named Context 검증 | `ValidationRules.For()`, `ValidationContext`, `ContextualValidation<T>` |

**using 문 가이드:**
- 순차 검증만 사용: `Validations.Typed` 네임스페이스만 필요
- Apply 패턴 사용: `Validations` + `Validations.Typed` 둘 다 필요
- Context Class 사용: `Validations` (IValidationContext) + `Validations.Typed` 둘 다 필요
- Named Context 검증: `Validations.Contextual` 네임스페이스 사용

```csharp
// 순차 검증만 사용하는 경우 (Value Object)
using Functorium.Domains.ValueObjects.Validations.Typed;

// Apply 패턴 (병렬 검증)을 사용하는 경우
using Functorium.Domains.ValueObjects.Validations;
using Functorium.Domains.ValueObjects.Validations.Typed;

// Named Context 검증 (DTO, API 입력 등)
using Functorium.Domains.ValueObjects.Validations.Contextual;
```

**DDD 레이어별 권장 사용:**

| 레이어 | 권장 방식 | 예시 |
|--------|----------|------|
| Domain Layer | Value Object (Typed) | `ValidationRules<Price>.Positive(amount)` |
| Application Layer | Context Class (IValidationContext) | `ValidationRules<ProductValidation>.NotEmpty(name)` |
| Presentation Layer | Named Context (Contextual) | `ValidationRules.For("ProductName").NotEmpty(name)` |

> **Context Class**는 `IValidationContext`를 구현한 빈 클래스입니다. Application Layer에서 검증 컨텍스트를 재사용할 때 사용합니다. 자세한 내용은 [IValidationContext를 이용한 검증](#ivalidationcontext를-이용한-검증-context-class) 섹션을 참조하세요.

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
| **Custom** | `Must`, `ThenMust` | `Custom` (abstract record, 사용자 정의 파생) | 사용자 정의 검증 |

### ValidationRules\<T\> 시작점

**위치**: `Functorium.Domains.ValueObjects.Validations.Typed.ValidationRules<TValueObject>`

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
// Error type definition: public sealed record Unsupported : DomainErrorType.Custom;
ValidationRules<Currency>.Must(
    value,
    v => SupportedCurrencies.Contains(v),
    new Unsupported(),
    $"Currency '{value}' is not supported")
```

### TypedValidation 체이닝

**위치**: `Functorium.Domains.ValueObjects.Validations.Typed.TypedValidationExtensions`

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
// Error type definition: public sealed record Unsupported : DomainErrorType.Custom;
// 메시지 생성 함수 사용
.ThenMust(
    v => SupportedCurrencies.Contains(v),
    new Unsupported(),
    v => $"Currency '{v}' is not supported")  // 값 포함 동적 메시지
```

### TypedValidation\<TValueObject, T\>

**위치**: `Functorium.Domains.ValueObjects.Validations.Typed.TypedValidation<TValueObject, T>`

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

**위치**: `Functorium.Domains.ValueObjects.Validations.ValidationApplyExtensions`

`Validation<Error, T>` 또는 `TypedValidation<TValueObject, T>` 튜플에 대한 Apply 오버로드를 제공합니다. `.As()` 없이 사용할 수 있습니다.

> **참고**: Apply 확장 메서드는 루트 `Validations` 네임스페이스에 있으므로, Apply 패턴 사용 시 해당 네임스페이스를 추가해야 합니다.

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

### Contextual 검증 (Named Context)

**위치**: `Functorium.Domains.ValueObjects.Validations.Contextual`

Value Object 없이 primitive 타입을 검증할 때 사용합니다. DTO 검증, API 입력 검증, 빠른 프로토타이핑에 적합합니다.

#### ValidationRules.For() 시작점

```csharp
using Functorium.Domains.ValueObjects.Validations.Contextual;

// Named Context 검증 시작
ValidationRules.For("ProductName").NotEmpty(name);
// Error: DomainErrors.ProductName.Empty

// 체이닝
ValidationRules.For("OrderValidation")
    .NotEmpty(name)
    .ThenMinLength(3)
    .ThenMaxLength(100);
```

#### ValidationContext 검증 메서드

`ValidationRules.For()`가 반환하는 `ValidationContext`는 `ValidationRules<T>`와 동일한 검증 메서드를 제공합니다:

| 범주 | 메서드 |
|------|--------|
| Presence | `NotNull()` |
| Length | `NotEmpty()`, `MinLength()`, `MaxLength()`, `ExactLength()` |
| Format | `Matches()` |
| Numeric | `Positive()`, `NonNegative()`, `NotZero()`, `Between()`, `AtMost()`, `AtLeast()` |
| DateTime | `NotDefault()`, `InPast()`, `InFuture()`, `Before()`, `After()`, `DateBetween()` |
| Custom | `Must()` |

#### ContextualValidation\<T\> 체이닝

`ValidationContext` 메서드가 반환하는 `ContextualValidation<T>`에 대한 체이닝 메서드입니다. `TypedValidationExtensions`와 동일한 메서드를 제공합니다:

| 범주 | 메서드 |
|------|--------|
| Presence | `ThenNotNull()` |
| Length | `ThenNotEmpty()`, `ThenMinLength()`, `ThenMaxLength()`, `ThenExactLength()`, `ThenNormalize()` |
| Numeric | `ThenPositive()`, `ThenNonNegative()`, `ThenNotZero()`, `ThenBetween()`, `ThenAtMost()`, `ThenAtLeast()` |
| Apply | `Apply()` - ContextualValidation 튜플에 대한 Apply 지원 |

#### 사용 예시

```csharp
using Functorium.Domains.ValueObjects.Validations.Contextual;

// DTO 검증 예시
public Validation<Error, CreateProductRequest> ValidateRequest(CreateProductRequest request) =>
    (ValidationRules.For("ProductName").NotEmpty(request.Name).ThenMaxLength(100),
     ValidationRules.For("Price").Positive(request.Price),
     ValidationRules.For("Category").NotEmpty(request.Category))
        .Apply((name, price, category) => request);

// API 입력 검증 예시
public Validation<Error, decimal> ValidateAmount(decimal amount) =>
    ValidationRules.For("Amount")
        .Positive(amount)
        .ThenAtMost(1_000_000m);
```

#### IValidationContext를 이용한 검증 (Context Class)

**위치**: `Functorium.Domains.ValueObjects.Validations.IValidationContext`

Value Object 없이 `ValidationRules<T>` 패턴을 사용하고 싶을 때, `IValidationContext` 마커 인터페이스를 구현한 클래스를 만들어 사용할 수 있습니다. 이 방식은 Named Context(`ValidationRules.For()`)와 Typed(`ValidationRules<T>`) 방식의 중간입니다.

**언제 사용하나요?**

- Application Layer에서 **재사용 가능한 검증 컨텍스트**가 필요할 때
- Named Context의 **문자열 오타 위험**을 피하고 싶을 때
- 하지만 Value Object를 만들기에는 **과한 경우**

```csharp
using Functorium.Domains.ValueObjects.Validations;
using Functorium.Domains.ValueObjects.Validations.Typed;

// 1. IValidationContext를 구현한 빈 클래스 정의
public sealed class ProductValidation : IValidationContext;
public sealed class OrderValidation : IValidationContext;

// 2. ValidationRules<T>에서 Value Object 대신 사용
public Validation<Error, decimal> ValidatePrice(decimal price) =>
    ValidationRules<ProductValidation>.Positive(price);
// Error Code: DomainErrors.ProductValidation.NotPositive

public Validation<Error, string> ValidateOrderId(string orderId) =>
    ValidationRules<OrderValidation>.NotEmpty(orderId)
        .ThenMinLength(10);
// Error Code: DomainErrors.OrderValidation.Empty 또는 TooShort
```

**장점:**
- 컴파일 타임 타입 안전성 (오타 방지)
- 검증 컨텍스트를 여러 곳에서 재사용 가능
- IDE 자동완성 지원

#### 세 가지 검증 방식 비교

| 특성 | Typed | Context Class | Named Context |
|------|-------|---------------|---------------|
| **사용법** | `ValidationRules<Price>` | `ValidationRules<ProductValidation>` | `ValidationRules.For("Price")` |
| **타입 소스** | Value Object | IValidationContext 구현 클래스 | 문자열 |
| **타입 안전성** | 컴파일 타임 | 컴파일 타임 | 런타임 |
| **네임스페이스** | `Validations.Typed` | `Validations.Typed` | `Validations.Contextual` |
| **권장 레이어** | Domain | Application | Presentation |
| **권장 대상** | Value Object | 재사용 가능한 검증 | 일회성 검증, 프로토타이핑 |
| **Error Code** | `DomainErrors.Price.NotPositive` | `DomainErrors.ProductValidation.NotPositive` | `DomainErrors.Price.NotPositive` |

**선택 가이드:**

```
Value Object가 있나요?
    |
    +-- 예 --> ValidationRules<Price> (Typed)
    |
    `-- 아니오 --> 검증을 여러 곳에서 재사용하나요?
                      |
                      +-- 예 --> ValidationRules<ProductValidation> (Context Class)
                      |
                      `-- 아니오 --> ValidationRules.For("Price") (Named Context)
```

---

## 유스케이스 파이프라인 검증 시스템

Application Layer에서 Request DTO를 검증할 때 FluentValidation을 많이 사용합니다. 이때 **값 객체에 이미 정의된 검증 로직을 재사용**하면 중복을 피하고 일관성을 유지할 수 있습니다.

Functorium은 FluentValidation과의 통합을 위해 `MustSatisfyValidation` 확장 메서드를 제공합니다. 이를 통해 **검증 로직을 Domain Layer(Value Object)에서 한 번만 정의**하고, Application Layer에서 그대로 재사용할 수 있습니다.

### 왜 검증을 재사용하나요?

일반적인 레이어드 아키텍처에서 검증은 두 곳에서 발생합니다:

1. **Application Layer (유스케이스 진입점)**: Request DTO가 들어오면 FluentValidation으로 검증
2. **Domain Layer (값 객체 생성)**: `Price.Create(value)`를 호출할 때 검증

검증 로직을 각각 따로 작성하면 **중복**이 발생하고, 규칙이 변경될 때 **두 곳을 모두 수정**해야 합니다.

```
+-------------------------------------------------------------------+
|  Application Layer                                                |
|  +-------------------------------------------------------------+  |
|  |  UsecaseValidationPipeline (FluentValidation)               |  |
|  |  - RuleFor(x => x.Price).MustSatisfyValidation(...)    <----+--+-- Value Object's
|  +-------------------------------------------------------------+  |   Validate reuse
+-------------------------------------------------------------------+
                                |
                                v
+-------------------------------------------------------------------+
|  Domain Layer                                                     |
|  +-------------------------------------------------------------+  |
|  |  Value Object (Price)                                       |  |
|  |  - Validate(): Single source of validation logic       <----+--+-- Define validation
|  |  - Create(): Create object after Validate call              |  |
|  +-------------------------------------------------------------+  |
+-------------------------------------------------------------------+
```

**해결책**: Value Object의 `Validate` 메서드를 FluentValidation에서 직접 호출하여 재사용합니다.

**위치**: `Functorium.Applications.Validations.FluentValidationExtensions`

### MustSatisfyValidation (입력 타입 == 출력 타입)

검증 메서드의 입력 타입과 출력 타입이 동일한 경우 사용합니다. 대부분의 경우 이 메서드를 사용합니다.

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

드물지만, 검증 메서드가 입력 타입과 다른 타입을 반환하는 경우가 있습니다. 예를 들어 문자열을 받아서 정수로 파싱하고 검증하는 경우입니다.

```csharp
// string → Validation<Error, int> (입력: string, 출력: int)
public sealed class Age : ComparableSimpleValueObject<int>
{
    public sealed record InvalidFormat : DomainErrorType.Custom;

    // 문자열을 받아서 정수로 변환 후 검증
    public static Validation<Error, int> Validate(string value) =>
        int.TryParse(value, out var parsed)
            ? ValidationRules<Age>.Between(parsed, 0, 150)
            : DomainError.For<Age>(new InvalidFormat(), value,
                $"'{value}' is not a valid number");
}

// Application Layer UsecaseValidationPipeline
public sealed class Validator : AbstractValidator<Request>
{
    public Validator()
    {
        // 타입이 다르므로 MustSatisfyValidationOf 사용
        // 타입 파라미터: <Request타입, 입력타입(string), 출력타입(int)>
        RuleFor(x => x.Age)
            .MustSatisfyValidationOf<Request, string, int>(Age.Validate);
    }
}
```

### 어떤 메서드를 사용해야 하나요?

대부분의 경우 **MustSatisfyValidation**을 사용합니다. Value Object의 `Validate` 메서드는 보통 같은 타입을 입력받고 반환하기 때문입니다.

```csharp
// 대부분의 경우: decimal → Validation<Error, decimal>
public static Validation<Error, decimal> Validate(decimal value) => ...

// 드문 경우: string → Validation<Error, int> (파싱 포함)
public static Validation<Error, int> Validate(string value) => ...
```

| 검증 메서드 시그니처 | 사용 메서드 | 타입 명시 |
|---------------------|-------------|----------|
| `Func<T, Validation<Error, T>>` | `MustSatisfyValidation` | 불필요 (타입 추론) |
| `Func<TIn, Validation<Error, TOut>>` | `MustSatisfyValidationOf` | 필요 (`<TRequest, TIn, TOut>`) |

> **참고**: `MustSatisfyValidationOf`에서 타입을 명시해야 하는 이유는 C# 14의 extension members가 추가 제네릭 타입 파라미터가 있을 때 타입 추론을 지원하지 않기 때문입니다.

---

## 오류 시스템

Functorium은 **예외 대신 결과 타입**을 사용하여 오류를 처리합니다. 검증 실패는 예외를 던지지 않고 `Validation<Error, T>` 또는 `Fin<T>`로 표현됩니다. 이 방식은 함수형 프로그래밍의 "실패는 예외가 아닌 정상적인 결과"라는 철학을 따릅니다.

**결과 타입의 장점:**
- 호출자가 실패 가능성을 **명시적으로** 처리해야 함 (컴파일러가 강제)
- try-catch 없이 **함수 체이닝**으로 오류 처리 가능
- 여러 오류를 **수집**하여 한 번에 반환 가능

> **상세 내용은 [에러 시스템 가이드](./08a-error-system.md)를 참조하세요.**

### DomainErrorType 개요

**위치**: `Functorium.Domains.Errors.DomainErrorType`

sealed record 계층으로 타입 안전한 에러 정의를 제공합니다.

```csharp
using static Functorium.Domains.Errors.DomainErrorType;
```

#### 범주 구조

| 범주 | 설명 | 대표 ErrorType |
|------|------|---------------|
| Presence | 값 존재 검증 | `Empty`, `Null` |
| Length | 길이 검증 | `TooShort`, `TooLong`, `WrongLength` |
| Format | 형식 검증 | `InvalidFormat`, `NotUpperCase`, `NotLowerCase` |
| DateTime | 날짜 검증 | `DefaultDate`, `NotInPast`, `NotInFuture`, `TooLate`, `TooEarly` |
| Numeric | 숫자 검증 | `Zero`, `Negative`, `NotPositive`, `OutOfRange`, `BelowMinimum`, `AboveMaximum` |
| Range | 범위 쌍 검증 | `RangeInverted`, `RangeEmpty` |
| Existence | 존재 여부 검증 | `NotFound`, `AlreadyExists`, `Duplicate`, `Mismatch` |
| Custom | 커스텀 에러 | `Custom` (abstract record, 사용자 정의 파생) |

### DomainError.For\<T\>() 헬퍼

**위치**: `Functorium.Domains.Errors.DomainError`

`ValidationRules<T>`가 제공하지 않는 커스텀 비즈니스 규칙 검증 실패 시 에러를 생성합니다. 에러 코드를 `DomainErrors.{TypeName}.{ErrorName}` 형식으로 자동 생성합니다.

#### 메서드 시그니처

```csharp
// 단일 값 (문자열)
public static Error For<TContext>(
    DomainErrorType errorType,
    string currentValue,
    string message);

// 단일 값 (제네릭)
public static Error For<TContext, TValue>(
    DomainErrorType errorType,
    TValue currentValue,
    string message);

// 두 값
public static Error For<TContext, TValue1, TValue2>(
    DomainErrorType errorType,
    TValue1 value1,
    TValue2 value2,
    string message);

// 세 값
public static Error For<TContext, TValue1, TValue2, TValue3>(
    DomainErrorType errorType,
    TValue1 value1,
    TValue2 value2,
    TValue3 value3,
    string message);
```

#### 파라미터 설명

| 파라미터 | 설명 |
|----------|------|
| `TContext` | 에러 컨텍스트 타입 (Value Object 또는 IValidationContext). 에러 코드의 `{TypeName}` 부분 |
| `errorType` | `DomainErrorType` 인스턴스. 에러 코드의 `{ErrorName}` 부분 |
| `currentValue` | 검증 실패한 현재 값. 디버깅 및 에러 메시지에 포함 |
| `message` | 사용자/개발자에게 표시할 에러 메시지 |

#### 사용 예시 및 출력

각 오버로드는 내부적으로 다른 Error 타입을 생성합니다:

| 오버로드 | 내부 타입 | 값 필드 |
|----------|----------|---------|
| `For<TContext>` | `ErrorCodeExpected` | `ErrorCurrentValue: string` |
| `For<TContext, TValue>` | `ErrorCodeExpected<TValue>` | `ErrorCurrentValue: TValue` |
| `For<TContext, T1, T2>` | `ErrorCodeExpected<T1, T2>` | `ErrorCurrentValue1: T1`, `ErrorCurrentValue2: T2` |
| `For<TContext, T1, T2, T3>` | `ErrorCodeExpected<T1, T2, T3>` | `ErrorCurrentValue1: T1`, `ErrorCurrentValue2: T2`, `ErrorCurrentValue3: T3` |

**단일 값 (문자열) → `ErrorCodeExpected`**

```csharp
var error = DomainError.For<Email>(new Empty(), "", "Email cannot be empty");

// 타입 검증
error.ShouldBeOfType<ErrorCodeExpected>();
var typed = (ErrorCodeExpected)error;
typed.ErrorCode.ShouldBe("DomainErrors.Email.Empty");
typed.ErrorCurrentValue.ShouldBe("");
typed.Message.ShouldBe("Email cannot be empty");
```

```json
{
  "ErrorCode": "DomainErrors.Email.Empty",
  "ErrorCurrentValue": "",
  "Message": "Email cannot be empty"
}
```

**단일 값 (제네릭) → `ErrorCodeExpected<TValue>`**

```csharp
var error = DomainError.For<Age, int>(new Negative(), -5, "Age cannot be negative");

// 타입 검증
error.ShouldBeOfType<ErrorCodeExpected<int>>();
var typed = (ErrorCodeExpected<int>)error;
typed.ErrorCode.ShouldBe("DomainErrors.Age.Negative");
typed.ErrorCurrentValue.ShouldBe(-5);  // int 타입 유지
typed.Message.ShouldBe("Age cannot be negative");
```

```json
{
  "ErrorCode": "DomainErrors.Age.Negative",
  "ErrorCurrentValue": -5,
  "Message": "Age cannot be negative"
}
```

**두 값 → `ErrorCodeExpected<T1, T2>`**

```csharp
// Error type definition: public sealed record InvalidRange : DomainErrorType.Custom;
var startDate = new DateTime(2024, 12, 31);
var endDate = new DateTime(2024, 1, 1);
var error = DomainError.For<DateRange, DateTime, DateTime>(
    new InvalidRange(), startDate, endDate, "Start must be before end");

// 타입 검증
error.ShouldBeOfType<ErrorCodeExpected<DateTime, DateTime>>();
var typed = (ErrorCodeExpected<DateTime, DateTime>)error;
typed.ErrorCode.ShouldBe("DomainErrors.DateRange.InvalidRange");
typed.ErrorCurrentValue1.ShouldBe(startDate);  // DateTime 타입 유지
typed.ErrorCurrentValue2.ShouldBe(endDate);    // DateTime 타입 유지
typed.Message.ShouldBe("Start must be before end");
```

```json
{
  "ErrorCode": "DomainErrors.DateRange.InvalidRange",
  "ErrorCurrentValue1": "2024-12-31T00:00:00",
  "ErrorCurrentValue2": "2024-01-01T00:00:00",
  "Message": "Start must be before end"
}
```

**세 값 → `ErrorCodeExpected<T1, T2, T3>`**

```csharp
// Error type definition: public sealed record InvalidTriangle : DomainErrorType.Custom;
var error = DomainError.For<Triangle, double, double, double>(
    new InvalidTriangle(), 1.0, 2.0, 10.0, "Invalid triangle sides");

// 타입 검증
error.ShouldBeOfType<ErrorCodeExpected<double, double, double>>();
var typed = (ErrorCodeExpected<double, double, double>)error;
typed.ErrorCode.ShouldBe("DomainErrors.Triangle.InvalidTriangle");
typed.ErrorCurrentValue1.ShouldBe(1.0);   // double 타입 유지
typed.ErrorCurrentValue2.ShouldBe(2.0);   // double 타입 유지
typed.ErrorCurrentValue3.ShouldBe(10.0);  // double 타입 유지
typed.Message.ShouldBe("Invalid triangle sides");
```

```json
{
  "ErrorCode": "DomainErrors.Triangle.InvalidTriangle",
  "ErrorCurrentValue1": 1.0,
  "ErrorCurrentValue2": 2.0,
  "ErrorCurrentValue3": 10.0,
  "Message": "Invalid triangle sides"
}
```

**튜플 값 예시 → `ErrorCodeExpected<(T1, T2)>`**

```csharp
var range = (Min: 100m, Max: 50m);
var error = DomainError.For<PriceRange, (decimal Min, decimal Max)>(
    new RangeInverted(Min: "100", Max: "50"),
    range,
    "Price range is invalid. Minimum cannot exceed maximum.");

// 타입 검증
error.ShouldBeOfType<ErrorCodeExpected<(decimal Min, decimal Max)>>();
var typed = (ErrorCodeExpected<(decimal Min, decimal Max)>)error;
typed.ErrorCode.ShouldBe("DomainErrors.PriceRange.RangeInverted");
typed.ErrorCurrentValue.ShouldBe((100m, 50m));  // 튜플 타입 유지
typed.Message.ShouldBe("Price range is invalid. Minimum cannot exceed maximum.");
```

```json
{
  "ErrorCode": "DomainErrors.PriceRange.RangeInverted",
  "ErrorCurrentValue": {
    "Item1": 100.0,
    "Item2": 50.0
  },
  "Message": "Price range is invalid. Minimum cannot exceed maximum."
}
```

#### ValidationRules\<T\> vs DomainError.For\<T\>()

| 상황 | 권장 |
|------|------|
| 일반적인 검증 (빈 값, 길이, 범위 등) | `ValidationRules<T>` + 체이닝 |
| 커스텀 조건 검증 | `ValidationRules<T>.Must()` 또는 `.ThenMust()` |
| 도메인 연산 중 비즈니스 규칙 위반 | `DomainError.For<T>()` |
| 두 값 비교 실패 (통화 불일치 등) | `DomainError.For<T, V1, V2>()` |

```csharp
// ValidationRules<T>: 일반적인 검증
public static Validation<Error, decimal> ValidateAmount(decimal amount) =>
    ValidationRules<Money>.NonNegative(amount);

// DomainError.For<T>(): 도메인 연산 중 비즈니스 규칙 위반
public Fin<Money> Add(Money other) =>
    Currency == other.Currency
        ? new Money(Amount + other.Amount, Currency)
        : DomainError.For<Money, string, string>(
            new Mismatch(), Currency, other.Currency,
            $"Cannot add different currencies: {Currency} vs {other.Currency}");
```

---

## 구현 패턴

값 객체 구현의 핵심은 **Create/Validate 분리 패턴**입니다. 이 패턴은 검증 로직과 객체 생성을 분리하여 재사용성과 테스트 용이성을 높입니다.

- **Validate**: 원시 값을 받아 검증 결과(`Validation<Error, T>`)를 반환. 객체를 생성하지 않음
- **Create**: Validate를 호출하고 성공 시 객체를 생성하여 `Fin<T>`를 반환

이렇게 분리하면 **Validate 메서드를 다른 곳(FluentValidation 파이프라인 등)에서 재사용**할 수 있습니다.

> **참고**: Entity도 동일한 Create/Validate 분리 패턴을 따릅니다. 자세한 내용은 [Entity 구현 가이드 - 생성 패턴](./06b-entity-aggregate-core.md#생성-패턴)을 참고하세요.

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

> **참고**: Apply 패턴 사용 시 `Functorium.Domains.ValueObjects.Validations` 네임스페이스가 필요합니다.
> (`ValidationApplyExtensions`가 해당 네임스페이스에 위치)

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

## 트러블슈팅

### Apply 패턴에서 `.As()` 호출 누락으로 컴파일 오류 발생
**원인:** `Validation<Error, T>` 튜플에 Apply를 사용할 때 LanguageExt의 타입 추론이 실패하는 경우가 있습니다.
**해결:** 튜플에 `TypedValidation`이 포함되어 있으면 `.As()` 없이 사용 가능합니다. 그 외의 경우 `ValidationApplyExtensions` 네임스페이스(`Functorium.Domains.ValueObjects.Validations`)를 추가하세요. 이 네임스페이스의 Apply 오버로드는 `.As()` 없이 직접 동작합니다.

### MustSatisfyValidation에서 타입 추론 실패
**원인:** `MustSatisfyValidationOf`는 입력 타입과 출력 타입이 다를 때 사용하는데, C#14 extension members의 타입 추론 제한으로 제네릭 파라미터를 명시해야 합니다.
**해결:** 입력/출력 타입이 동일하면 `MustSatisfyValidation`을 사용하세요. 다를 경우 `MustSatisfyValidationOf<TRequest, TIn, TOut>(Validate)` 형태로 타입을 명시하세요.

### ValidationRules 네임스페이스를 찾을 수 없음
**원인:** 검증 방식(Typed, Contextual, Context Class)에 따라 네임스페이스가 다릅니다.
**해결:**
- Typed: `using Functorium.Domains.ValueObjects.Validations.Typed;`
- Contextual: `using Functorium.Domains.ValueObjects.Validations.Contextual;`
- Apply 확장: `using Functorium.Domains.ValueObjects.Validations;`

---

## FAQ

### Q1. SimpleValueObject와 ValueObject 중 어떤 것을 사용해야 하나요?
단일 원시 값을 감싸는 경우 `SimpleValueObject<T>` (또는 비교가 필요하면 `ComparableSimpleValueObject<T>`)를 사용합니다. 여러 속성이 함께 의미를 형성하는 경우(예: Money = Amount + Currency) `ValueObject`를 사용합니다.

### Q2. Create와 Validate를 왜 분리하나요?
`Validate`는 `Validation<Error, T>`를 반환하여 FluentValidation 파이프라인에서 재사용할 수 있습니다. `Create`는 `Fin<T>`를 반환하여 실제 객체를 생성합니다. 분리하면 검증 로직을 한 곳에서 정의하고 여러 곳에서 재사용할 수 있습니다.

### Q3. 순차 검증과 병렬 검증은 언제 사용하나요?
**순차 검증(Bind/Then)**: 이전 검증이 통과해야 다음 검증이 의미 있는 경우. 예: 빈 값 체크 → 형식 체크 → 길이 체크. **병렬 검증(Apply)**: 각 검증이 독립적이어서 모든 오류를 한 번에 수집하고 싶은 경우. 예: Amount 검증 + Currency 검증.

### Q4. Named Context와 IValidationContext 중 어떤 것을 사용해야 하나요?
일회성 검증이나 프로토타이핑에는 `ValidationRules.For("Name")` (Named Context)를 사용합니다. 여러 곳에서 재사용하거나 오타를 방지하려면 `IValidationContext`를 구현한 클래스를 만들어 `ValidationRules<MyContext>`로 사용하세요.

### Q5. DomainError.For와 ValidationRules.Must의 차이는 무엇인가요?
`ValidationRules<T>.Must()`는 값 객체 생성 시 검증 체이닝에서 사용하며 `Validation<Error, T>`를 반환합니다. `DomainError.For<T>()`는 Entity의 도메인 연산 중 비즈니스 규칙 위반 시 사용하며, 에러를 직접 생성하여 `Fin<T>`로 반환합니다.

---

## 참고 문서

- [값 객체: 열거형·검증·실전 패턴](./05b-value-objects-validation.md)
