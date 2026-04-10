---
title: "검증 시스템 사양"
---

Functorium의 검증 시스템은 값 객체와 DTO 검증을 위한 함수형 API를 제공합니다. 도메인 레이어에서는 `TypedValidation`과 `ContextualValidation`으로 타입 안전한 검증 체인을 구성하고, 애플리케이션 레이어에서는 `FluentValidationExtensions`로 값 객체 검증 로직을 FluentValidation 규칙에 통합합니다.

## 요약

### 주요 타입

| 타입 | 네임스페이스 | 설명 |
|------|-------------|------|
| `TypedValidation<TVO, T>` | `Domains.ValueObjects.Validations.Typed` | 값 객체 타입 정보를 체이닝 중 전달하는 wrapper |
| `ValidationRules<TVO>` | `Domains.ValueObjects.Validations.Typed` | 타입 파라미터를 한 번만 지정하는 검증 시작점 |
| `TypedValidationExtensions` | `Domains.ValueObjects.Validations.Typed` | `Then*` 체이닝 확장 메서드 |
| `ContextualValidation<T>` | `Domains.ValueObjects.Validations.Contextual` | 검증 컨텍스트 이름을 체이닝 중 전달하는 wrapper |
| `ValidationRules` | `Domains.ValueObjects.Validations.Contextual` | `For(contextName)` Named Context 진입점 |
| `ValidationContext` | `Domains.ValueObjects.Validations.Contextual` | Named Context 검증 규칙 인스턴스 메서드 |
| `ContextualValidationExtensions` | `Domains.ValueObjects.Validations.Contextual` | `Then*` 체이닝 확장 메서드 (Contextual) |
| `IValidationContext` | `Domains.ValueObjects.Validations` | Application Layer 재사용 가능 검증 컨텍스트 마커 |
| `ValidationApplyExtensions` | `Domains.ValueObjects.Validations` | `Validation<Error, T>` Tuple Apply (2~5-tuple) |
| `FinApplyExtensions` | `Domains.ValueObjects.Validations` | `Fin<T>` Tuple Apply (2~5-tuple) |
| `FluentValidationExtensions` | `Applications.Validations` | FluentValidation + 값 객체 Validate 통합 |

### 주요 개념

| 개념 | 설명 |
|------|------|
| Typed Validation | `ValidationRules<TVO>.Rule(value)` 형태로 값 객체 타입을 오류 메시지에 자동 포함 |
| Contextual Validation | `ValidationRules.For("Name").Rule(value)` 형태로 문자열 컨텍스트를 오류 메시지에 포함 |
| `Then*` 체이닝 | 순차적 검증 체인 (첫 오류에서 중단, `Bind` 기반) |
| `Apply` 병합 | 독립적 검증을 병렬 수행하여 모든 오류를 누적 수집 |
| FluentValidation 통합 | 값 객체의 `Validate()` 결과를 `IRuleBuilder` 규칙으로 변환 |

---

## TypedValidation vs ContextualValidation 비교

두 검증 방식은 동일한 규칙 카탈로그를 제공하지만, 오류 메시지의 출처 식별 방식이 다릅니다.

| 구분 | TypedValidation | ContextualValidation |
|------|----------------|---------------------|
| **시작점** | `ValidationRules<TVO>.Rule(value)` | `ValidationRules.For("ctx").Rule(value)` |
| **오류 출처** | `typeof(TVO).Name` (컴파일 타임 타입) | `contextName` (런타임 문자열) |
| **Wrapper** | `TypedValidation<TVO, T>` | `ContextualValidation<T>` |
| **체이닝** | `.ThenRule()` | `.ThenRule()` |
| **Apply** | 2~4-tuple Apply 지원 | 2~4-tuple Apply 지원 |
| **권장 레이어** | Domain Layer (값 객체 내부) | Presentation Layer, 빠른 프로토타이핑 |
| **DomainError 팩토리** | `DomainError.For<TVO>(...)` | `DomainError.ForContext(...)` |

```csharp
// TypedValidation: 값 객체 내부
public static Validation<Error, ProductName> Validate(string value) =>
    ValidationRules<ProductName>.NotEmpty(value)
        .ThenMinLength(3)
        .ThenMaxLength(100)
        .Value;

// ContextualValidation: Named Context
ValidationRules.For("ProductName")
    .NotEmpty(name)
    .ThenMinLength(3)
    .ThenMaxLength(100);
```

`IValidationContext` 마커 인터페이스를 구현하면 Application Layer에서 재사용 가능한 검증 컨텍스트 클래스를 정의할 수 있습니다.

```csharp
// Application Layer 검증 컨텍스트 클래스
public sealed class ProductValidation : IValidationContext;

// 사용: TypedValidation과 동일한 API
ValidationRules<ProductValidation>.Positive(amount);
// Error: DomainErrors.ProductValidation.NotPositive
```

---

## TypedValidation\<TVO, T\>

### 구조체 정의

```csharp
public readonly struct TypedValidation<TValueObject, T>
{
    public Validation<Error, T> Value { get; }

    // Validation<Error, T>로 암시적 변환
    public static implicit operator Validation<Error, T>(
        TypedValidation<TValueObject, T> typed) => typed.Value;
}
```

- `TValueObject`: 값 객체 타입 (오류 메시지에 타입 이름 포함)
- `T`: 검증 대상 값의 타입
- `Value` 속성 또는 암시적 변환으로 `Validation<Error, T>` 추출

### ValidationRules\<TVO\> 시작점 메서드

`ValidationRules<TValueObject>` 정적 클래스가 검증 체인의 시작점을 제공합니다. 모든 메서드는 `TypedValidation<TValueObject, T>`를 반환합니다.

---

## 규칙 카탈로그

### Presence (존재성)

시작점 (`ValidationRules<TVO>`):

| 메서드 | 시그니처 | 설명 |
|--------|---------|------|
| `NotNull` | `NotNull<T>(T? value) where T : class` | 참조 타입 null 검사 |
| `NotNull` | `NotNull<T>(T? value) where T : struct` | nullable 값 타입 null 검사 |

체이닝 (`TypedValidationExtensions`):

| 메서드 | 시그니처 | 설명 |
|--------|---------|------|
| `ThenNotNull` | `ThenNotNull<TVO, T>(this TypedValidation<TVO, T?>) where T : class` | 참조 타입 null 검사 |
| `ThenNotNull` | `ThenNotNull<TVO, T>(this TypedValidation<TVO, T?>) where T : struct` | nullable 값 타입 null 검사 |

**DomainErrorType:** `Null()`

### Length (문자열 길이)

시작점:

| 메서드 | 시그니처 | 설명 |
|--------|---------|------|
| `NotEmpty` | `NotEmpty(string value)` | 공백 문자열 검사 (`IsNullOrWhiteSpace`) |
| `MinLength` | `MinLength(string value, int minLength)` | 최소 길이 |
| `MaxLength` | `MaxLength(string value, int maxLength)` | 최대 길이 |
| `ExactLength` | `ExactLength(string value, int length)` | 정확한 길이 |

체이닝:

| 메서드 | 시그니처 | 설명 |
|--------|---------|------|
| `ThenNotEmpty` | `ThenNotEmpty<TVO>(this TypedValidation<TVO, string>)` | 공백 문자열 검사 |
| `ThenMinLength` | `ThenMinLength<TVO>(this TypedValidation<TVO, string>, int)` | 최소 길이 |
| `ThenMaxLength` | `ThenMaxLength<TVO>(this TypedValidation<TVO, string>, int)` | 최대 길이 |
| `ThenExactLength` | `ThenExactLength<TVO>(this TypedValidation<TVO, string>, int)` | 정확한 길이 |
| `ThenNormalize` | `ThenNormalize<TVO>(this TypedValidation<TVO, string>, Func<string, string>)` | 문자열 변환 (정규화) |

**DomainErrorType:** `Empty()`, `TooShort(minLength)`, `TooLong(maxLength)`, `WrongLength(length)`

### Numeric (숫자)

시작점 (`where T : notnull, INumber<T>`):

| 메서드 | 시그니처 | 설명 |
|--------|---------|------|
| `NotZero` | `NotZero<T>(T value)` | 0이 아닌지 검사 |
| `NonNegative` | `NonNegative<T>(T value)` | 음수가 아닌지 검사 (>= 0) |
| `Positive` | `Positive<T>(T value)` | 양수인지 검사 (> 0) |
| `Between` | `Between<T>(T value, T min, T max)` | 범위 내 검사 |
| `AtMost` | `AtMost<T>(T value, T max)` | 최대값 이하 검사 |
| `AtLeast` | `AtLeast<T>(T value, T min)` | 최소값 이상 검사 |

체이닝 (`where T : notnull, INumber<T>`):

| 메서드 | 시그니처 | 설명 |
|--------|---------|------|
| `ThenNotZero` | `ThenNotZero<TVO, T>(this TypedValidation<TVO, T>)` | 0이 아닌지 검사 |
| `ThenNonNegative` | `ThenNonNegative<TVO, T>(this TypedValidation<TVO, T>)` | 음수가 아닌지 검사 |
| `ThenPositive` | `ThenPositive<TVO, T>(this TypedValidation<TVO, T>)` | 양수인지 검사 |
| `ThenBetween` | `ThenBetween<TVO, T>(this TypedValidation<TVO, T>, T min, T max)` | 범위 내 검사 |
| `ThenAtMost` | `ThenAtMost<TVO, T>(this TypedValidation<TVO, T>, T max)` | 최대값 이하 검사 |
| `ThenAtLeast` | `ThenAtLeast<TVO, T>(this TypedValidation<TVO, T>, T min)` | 최소값 이상 검사 |

**DomainErrorType:** `Zero()`, `Negative()`, `NotPositive()`, `OutOfRange(min, max)`, `AboveMaximum(max)`, `BelowMinimum(min)`

### Format (형식)

시작점:

| 메서드 | 시그니처 | 설명 |
|--------|---------|------|
| `Matches` | `Matches(string value, Regex pattern, string? message = null)` | 정규식 패턴 일치 |
| `IsUpperCase` | `IsUpperCase(string value)` | 대문자 검사 |
| `IsLowerCase` | `IsLowerCase(string value)` | 소문자 검사 |

체이닝:

| 메서드 | 시그니처 | 설명 |
|--------|---------|------|
| `ThenMatches` | `ThenMatches<TVO>(this TypedValidation<TVO, string>, Regex, string?)` | 정규식 패턴 일치 |
| `ThenIsUpperCase` | `ThenIsUpperCase<TVO>(this TypedValidation<TVO, string>)` | 대문자 검사 |
| `ThenIsLowerCase` | `ThenIsLowerCase<TVO>(this TypedValidation<TVO, string>)` | 소문자 검사 |

**DomainErrorType:** `InvalidFormat(pattern)`, `NotUpperCase()`, `NotLowerCase()`

> `ThenMatches`의 `pattern` 파라미터는 `Regex` 타입입니다. 성능을 위해 `[GeneratedRegex]` 패턴 사용을 권장합니다.

### DateTime (날짜/시간)

시작점:

| 메서드 | 시그니처 | 설명 |
|--------|---------|------|
| `NotDefault` | `NotDefault(DateTime value)` | `DateTime.MinValue`가 아닌지 검사 |
| `InPast` | `InPast(DateTime value)` | 과거 날짜 검사 |
| `InFuture` | `InFuture(DateTime value)` | 미래 날짜 검사 |
| `Before` | `Before(DateTime value, DateTime boundary)` | 기준 날짜 이전 검사 |
| `After` | `After(DateTime value, DateTime boundary)` | 기준 날짜 이후 검사 |
| `DateBetween` | `DateBetween(DateTime value, DateTime min, DateTime max)` | 날짜 범위 내 검사 |

체이닝:

| 메서드 | 시그니처 | 설명 |
|--------|---------|------|
| `ThenNotDefault` | `ThenNotDefault<TVO>(this TypedValidation<TVO, DateTime>)` | 기본값 검사 |
| `ThenInPast` | `ThenInPast<TVO>(this TypedValidation<TVO, DateTime>)` | 과거 날짜 검사 |
| `ThenInFuture` | `ThenInFuture<TVO>(this TypedValidation<TVO, DateTime>)` | 미래 날짜 검사 |
| `ThenBefore` | `ThenBefore<TVO>(this TypedValidation<TVO, DateTime>, DateTime)` | 기준 이전 검사 |
| `ThenAfter` | `ThenAfter<TVO>(this TypedValidation<TVO, DateTime>, DateTime)` | 기준 이후 검사 |
| `ThenDateBetween` | `ThenDateBetween<TVO>(this TypedValidation<TVO, DateTime>, DateTime, DateTime)` | 날짜 범위 검사 |

**DomainErrorType:** `DefaultDate()`, `NotInPast()`, `NotInFuture()`, `TooLate(boundary)`, `TooEarly(boundary)`, `OutOfRange(min, max)`

### Range (범위 쌍)

시작점 (`where TValue : notnull, IComparable<TValue>`):

| 메서드 | 시그니처 | 설명 |
|--------|---------|------|
| `ValidRange` | `ValidRange<TValue>(TValue min, TValue max)` | min <= max 검사 |
| `ValidStrictRange` | `ValidStrictRange<TValue>(TValue min, TValue max)` | min < max 검사 (빈 범위 불허) |

체이닝:

| 메서드 | 시그니처 | 설명 |
|--------|---------|------|
| `ThenValidRange` | `ThenValidRange<TVO, TValue>(this TypedValidation<TVO, (TValue, TValue)>)` | min <= max 검사 |
| `ThenValidStrictRange` | `ThenValidStrictRange<TVO, TValue>(this TypedValidation<TVO, (TValue, TValue)>)` | min < max 검사 |

반환 타입은 `TypedValidation<TVO, (TValue Min, TValue Max)>`입니다.

**DomainErrorType:** `RangeInverted(min, max)`, `RangeEmpty(value)` (StrictRange 전용)

### Collection (컬렉션)

시작점:

| 메서드 | 시그니처 | 설명 |
|--------|---------|------|
| `NotEmptyArray` | `NotEmptyArray<TElement>(TElement[]? value)` | 배열이 null이 아니고 비어있지 않은지 검사 |

체이닝:

| 메서드 | 시그니처 | 설명 |
|--------|---------|------|
| `ThenNotEmptyArray` | `ThenNotEmptyArray<TVO, TElement>(this TypedValidation<TVO, TElement[]>)` | 배열 비어있지 않은지 검사 |

**DomainErrorType:** `Empty()`

### Generic (사용자 정의)

시작점:

| 메서드 | 시그니처 | 설명 |
|--------|---------|------|
| `Must` | `Must<T>(T value, Func<T, bool> predicate, DomainErrorType errorType, string message) where T : notnull` | 사용자 정의 조건 검증 |

체이닝:

| 메서드 | 시그니처 | 설명 |
|--------|---------|------|
| `ThenMust` | `ThenMust<TVO, T>(this TypedValidation<TVO, T>, Func<T, bool>, DomainErrorType, string)` | 사용자 정의 조건 |
| `ThenMust` | `ThenMust<TVO, T>(this TypedValidation<TVO, T>, Func<T, bool>, DomainErrorType, Func<T, string>)` | 메시지 팩토리 오버로드 |

```csharp
ValidationRules<Discount>.Must(
    rate,
    r => r <= 100m,
    new DomainErrorType.BusinessRule("MaxDiscount"),
    $"Discount rate must not exceed 100%. Current: {rate}%");
```

### LINQ 지원

`TypedValidation`은 LINQ query expression을 지원합니다.

| 메서드 | 설명 |
|--------|------|
| `Select` | 값 변환 (`Map`) |
| `SelectMany` (TypedValidation -> Validation) | `from...in` 구문으로 체이닝 |
| `SelectMany` (TypedValidation -> TypedValidation) | 동일 TVO 타입 내 체이닝 |
| `ToValidation` | 명시적 `Validation<Error, T>` 변환 |

```csharp
// LINQ query expression
from validStart in ValidationRules<DateRange>.NotDefault(startDate)
from validEnd in ValidationRules<DateRange>.NotDefault(endDate)
select (validStart, validEnd);
```

---

## ContextualValidation\<T\>

### 구조체 정의

```csharp
public readonly struct ContextualValidation<T>
{
    public Validation<Error, T> Value { get; }
    public string ContextName { get; }

    // Validation<Error, T>로 암시적 변환
    public static implicit operator Validation<Error, T>(
        ContextualValidation<T> contextual) => contextual.Value;
}
```

### ValidationRules.For() 진입점

```csharp
public static class ValidationRules
{
    public static ValidationContext For(string contextName) => new(contextName);
}
```

`ValidationContext`는 `ValidationRules<TVO>`와 동일한 규칙 카탈로그를 인스턴스 메서드로 제공합니다. 모든 규칙의 오류 메시지에 `ContextName`이 `typeof(TVO).Name` 대신 사용됩니다.

### ValidationContext 인스턴스 메서드

`ValidationContext`가 제공하는 시작점 메서드 목록입니다. 카테고리별 규칙은 TypedValidation과 동일합니다.

| 카테고리 | 메서드 |
|---------|--------|
| Presence | `NotNull<T>` |
| Length | `NotEmpty`, `MinLength`, `MaxLength`, `ExactLength` |
| Numeric | `NotZero<T>`, `NonNegative<T>`, `Positive<T>`, `Between<T>`, `AtMost<T>`, `AtLeast<T>` |
| Format | `Matches`, `IsUpperCase`, `IsLowerCase` |
| DateTime | `NotDefault`, `InPast`, `InFuture`, `Before`, `After`, `DateBetween` |
| Generic | `Must<T>` |

### ContextualValidationExtensions 체이닝

`ContextualValidationExtensions`가 제공하는 `Then*` 체이닝 메서드 목록입니다. 컨텍스트 이름이 자동으로 전파됩니다.

| 카테고리 | 메서드 |
|---------|--------|
| Presence | `ThenNotNull<T>` |
| Length | `ThenNotEmpty`, `ThenMinLength`, `ThenMaxLength`, `ThenExactLength`, `ThenNormalize` |
| Numeric | `ThenNotZero<T>`, `ThenNonNegative<T>`, `ThenPositive<T>`, `ThenBetween<T>`, `ThenAtMost<T>`, `ThenAtLeast<T>` |

```csharp
// Named Context 체이닝 예시
ValidationRules.For("Price")
    .Positive(amount)
    .ThenAtMost(1_000_000m);

// Named Context Apply 예시
(ValidationRules.For("Amount").Positive(amount),
 ValidationRules.For("Currency").NotEmpty(currency))
    .Apply((a, c) => new Money(a, c));
```

---

## Apply 조합

### Validation\<Error, T\> Tuple Apply

`ValidationApplyExtensions`는 `Validation<Error, T>` 튜플에 대한 `Apply` 확장 메서드를 제공합니다. LanguageExt의 generic Apply가 `K<Validation<Error>, T>`를 반환하는 문제를 해결하여 `.As()` 호출 없이 concrete `Validation<Error, R>`를 반환합니다.

```csharp
// 시그니처 (2-tuple 예시)
public static Validation<Error, R> Apply<T1, T2, R>(
    this (Validation<Error, T1> v1, Validation<Error, T2> v2) tuple,
    Func<T1, T2, R> f)
```

| Tuple 크기 | 지원 |
|-----------|------|
| 2-tuple | `(v1, v2).Apply((a, b) => ...)` |
| 3-tuple | `(v1, v2, v3).Apply((a, b, c) => ...)` |
| 4-tuple | `(v1, v2, v3, v4).Apply((a, b, c, d) => ...)` |
| 5-tuple | `(v1, v2, v3, v4, v5).Apply((a, b, c, d, e) => ...)` |

### TypedValidation Tuple Apply

`TypedValidationExtensions.Apply`는 `TypedValidation`과 `Validation`을 자유롭게 혼합할 수 있는 오버로드를 제공합니다.

| Tuple 크기 | 조합 패턴 |
|-----------|---------|
| 2-tuple | TT, TV, VT |
| 3-tuple | TTT, VVT, TVV, VTV, TTV, TVT, VTT |
| 4-tuple | TTTT, TVVV, VTVV, VVTV, VVVT |

> T = TypedValidation, V = Validation

```csharp
// TypedValidation + Validation 혼합
(ValidationRules<Money>.NonNegative(amount),
 ValidationRules<Money>.NotEmpty(currency))
    .Apply((a, c) => new Money(a, c));
```

### ContextualValidation Tuple Apply

`ContextualValidationExtensions.Apply`는 `ContextualValidation`과 `Validation`을 혼합할 수 있는 동일한 패턴의 오버로드를 제공합니다. Tuple 크기와 조합 패턴은 TypedValidation Apply와 동일합니다 (2~4-tuple).

### Fin\<T\> Tuple Apply

`FinApplyExtensions`는 `Fin<T>` 튜플을 내부적으로 `Validation<Error, T>`로 변환한 뒤 Apply를 수행하고 결과를 다시 `Fin<R>`로 변환합니다.

```csharp
// 시그니처 (2-tuple 예시)
public static Fin<R> Apply<T1, T2, R>(
    this (Fin<T1> v1, Fin<T2> v2) tuple,
    Func<T1, T2, R> f)
```

| Tuple 크기 | 지원 |
|-----------|------|
| 2-tuple | `(fin1, fin2).Apply((a, b) => ...)` |
| 3-tuple | `(fin1, fin2, fin3).Apply((a, b, c) => ...)` |
| 4-tuple | `(fin1, fin2, fin3, fin4).Apply((a, b, c, d) => ...)` |
| 5-tuple | `(fin1, fin2, fin3, fin4, fin5).Apply((a, b, c, d, e) => ...)` |

```csharp
// Fin Apply 예시
(PersonalName.Create("HyungHo", "Ko"),
 EmailAddress.Create("user@example.com"))
    .Apply((name, email) => Contact.Create(name, email, now));
```

---

## FluentValidation 통합

`FluentValidationExtensions`는 값 객체의 `Validate()` 메서드를 FluentValidation 규칙으로 통합하는 확장 메서드를 제공합니다. 검증 실패 시 `IHasErrorCode` 인터페이스가 구현된 에러는 `[ErrorCode] Message` 형식으로 오류 메시지를 생성합니다.

### MustSatisfyValidation

입력 타입과 검증 결과 타입이 동일한 경우 사용합니다. C# 14 extension members 문법으로 타입 추론이 자동 작동합니다.

```csharp
public IRuleBuilderOptions<TRequest, TProperty> MustSatisfyValidation(
    Func<TProperty, Validation<Error, TProperty>> validationMethod)
```

```csharp
RuleFor(x => x.Price)
    .MustSatisfyValidation(Money.ValidateAmount);

RuleFor(x => x.Currency)
    .MustSatisfyValidation(Money.ValidateCurrency);
```

### MustSatisfyValidationOf\<TValueObject\>

입력 타입과 검증 결과 타입이 다른 경우 사용합니다. `TValueObject` 타입만 명시하면 됩니다.

```csharp
public IRuleBuilderOptions<TRequest, TProperty> MustSatisfyValidationOf<TValueObject>(
    Func<TProperty, Validation<Error, TValueObject>> validationMethod)
```

```csharp
// string -> Validation<Error, ProductName>
RuleFor(x => x.Name)
    .MustSatisfyValidationOf<ProductName>(ProductName.Validate);
```

> `IRuleBuilderInitial`에서 추가 제네릭 파라미터가 있는 메서드를 호출할 때 C# 14 extension members의 타입 추론 제한이 발생할 수 있습니다. 이 경우 전통적 확장 메서드 오버로드(`MustSatisfyValidationOf<TRequest, TProperty, TValueObject>`)를 사용하십시오.

### MustBeEntityId\<TEntityId\>

`IEntityId<TEntityId>`를 구현하는 EntityId 타입에 대한 문자열 검증입니다. `NotEmpty` + `TryParse`를 하나의 규칙으로 통합합니다.

```csharp
public static IRuleBuilderOptions<TRequest, string> MustBeEntityId<TRequest, TEntityId>(
    this IRuleBuilder<TRequest, string> ruleBuilder)
    where TEntityId : struct, IEntityId<TEntityId>
```

```csharp
RuleFor(x => x.ProductId)
    .MustBeEntityId<CreateProductRequest, ProductId>();
```

### MustBeEnum (SmartEnum)

`Ardalis.SmartEnum` 타입에 대한 검증입니다. Value, Name, string Value 세 가지 오버로드를 제공합니다.

| 메서드 | 시그니처 | 설명 |
|--------|---------|------|
| `MustBeEnum<TSmartEnum, TValue>` | `IRuleBuilder<TReq, TValue>` | Value로 검증 |
| `MustBeEnum<TSmartEnum>` (int) | `IRuleBuilder<TReq, int>` | int Value 간소화 오버로드 |
| `MustBeEnumName<TSmartEnum, TValue>` | `IRuleBuilder<TReq, string>` | Name으로 검증 |
| `MustBeEnumValue<TSmartEnum>` (string) | `IRuleBuilder<TReq, string>` | string Value로 검증 (대소문자 무시) |

```csharp
RuleFor(x => x.CurrencyCode)
    .MustBeEnumValue<CreateMoneyRequest, Currency>();

RuleFor(x => x.Status)
    .MustBeEnum<UpdateOrderRequest, OrderStatus>();
```

### MustBeOneOf

허용된 문자열 목록 중 하나인지 검증합니다. 대소문자를 무시하며, null 또는 빈 문자열은 검증을 건너뜁니다.

```csharp
public static IRuleBuilderOptions<TRequest, string> MustBeOneOf<TRequest>(
    this IRuleBuilder<TRequest, string> ruleBuilder,
    string[] allowedValues)
```

```csharp
RuleFor(x => x.SortBy)
    .MustBeOneOf(["name", "price", "date"]);
```

### Option\<T\> 검증

`Option<TProperty>` 속성에 대한 검증입니다. `None`이면 검증을 건너뛰고, `Some`이면 내부 값을 추출하여 검증합니다.

| 메서드 | 설명 |
|--------|------|
| `MustSatisfyValidation` | 입력 타입 == 결과 타입 |
| `MustSatisfyValidationOf<TValueObject>` | 입력 타입 != 결과 타입 |

```csharp
// Option<decimal> -> None이면 skip, Some(100m) -> validate
RuleFor(x => x.MinPrice)
    .MustSatisfyValidation(Money.Validate);

// Option<string> -> None이면 skip, Some("name") -> validate
RuleFor(x => x.Name)
    .MustSatisfyValidationOf<ProductName>(ProductName.Validate);
```

### MustBePairedRange

두 `Option` 필드가 반드시 함께 제공되어야 하는 쌍 범위 필터를 단일 호출로 검증합니다.

```csharp
public static void MustBePairedRange<TRequest, T>(
    this AbstractValidator<TRequest> validator,
    Expression<Func<TRequest, Option<T>>> minExpr,
    Expression<Func<TRequest, Option<T>>> maxExpr,
    Func<T, Validation<Error, T>> validate,
    bool inclusive = false)
    where T : IComparable<T>
```

**검증 로직:**

1. 둘 다 `None` -- 통과 (필터 미적용)
2. 하나만 `Some` -- 실패 ("MaxPrice is required when MinPrice is specified")
3. 둘 다 `Some` -- 각각 `validate` 실행 + 범위 비교

```csharp
// 기본: max > min (exclusive)
this.MustBePairedRange(
    x => x.MinPrice,
    x => x.MaxPrice,
    Money.Validate);

// 커스텀: max >= min (inclusive)
this.MustBePairedRange(
    x => x.MinPrice,
    x => x.MaxPrice,
    Money.Validate,
    inclusive: true);
```

---

## 관련 문서

| 문서 | 설명 |
|------|------|
| [값 객체: 열거형/검증/실전 패턴](../guides/domain/05b-value-objects-validation) | Apply 병합, 체이닝 패턴, SmartEnum Create 가이드 |
| [값 객체 기반 클래스](../guides/domain/05a-value-objects) | `SimpleValueObject<T>`, `ValueObject`, Create 패턴 |
| [에러 시스템 사양](./04-error-system) | `DomainErrorType`, `DomainError.For<T>()`, `DomainError.ForContext()` |
| [값 객체 사양](./02-value-object) | `ValueObject`, `SimpleValueObject<T>`, Union 타입 |
