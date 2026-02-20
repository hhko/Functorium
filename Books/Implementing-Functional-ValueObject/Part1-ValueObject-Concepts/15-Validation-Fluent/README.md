# Validate&lt;T&gt; Fluent API로 검증 간소화하기

## 목차
- [개요](#개요)
- [학습 목표](#학습-목표)
- [왜 필요한가?](#왜-필요한가)
- [핵심 개념](#핵심-개념)
- [Before/After 비교](#beforeafter-비교)
- [실전 지침](#실전-지침)
- [프로젝트 설명](#프로젝트-설명)
- [한눈에 보는 정리](#한눈에-보는-정리)
- [FAQ](#faq)

## 개요

이 프로젝트는 Functorium 프레임워크의 `Validate<T>` Fluent API를 활용하여 값 객체의 검증 코드를 대폭 간소화하는 패턴을 시연합니다. 기존의 `DomainError.For<T>()` 직접 호출과 중첩된 삼항 연산자를 `Validate<T>.NotEmpty()` 체이닝으로 대체하여 **코드량을 약 70% 감소**시키고, **선형적이고 읽기 쉬운 검증 흐름**을 제공합니다.

## 학습 목표

### **핵심 학습 목표**
1. **Validate&lt;T&gt; 진입점 활용**: 단일 진입점에서 타입 파라미터를 한 번만 지정하여 검증을 시작할 수 있다
2. **Fluent 체이닝 활용**: `Then*()` 메서드로 여러 검증 규칙을 선형적으로 연결할 수 있다
3. **자동 에러 코드 생성**: 에러 타입과 메시지가 자동으로 생성됨을 이해한다
4. **ThenNormalize 활용**: 검증 후 값 변환(정규화)을 체인에 포함시킬 수 있다
5. **ThenMust 활용**: 커스텀 검증 조건을 Fluent 체인에 추가할 수 있다

### **실습을 통해 확인할 내용**
- **검증 시작**: `Validate<Currency>.NotEmpty(value)` → 타입 안전한 검증 시작점
- **체이닝**: `.ThenExactLength(3).ThenNormalize(v => v.ToUpperInvariant())` → 선형적 흐름
- **자동 변환**: `TypedValidation<TValueObject, T>` → `Validation<Error, T>` 암시적 변환

## 왜 필요한가?

이전 `14-Error-Code-Fluent` 프로젝트에서는 `DomainError.For<T>()` 헬퍼로 에러 처리를 간소화했지만, 여전히 개선의 여지가 있었습니다.

**첫 번째 문제는 중첩된 조건문입니다.** 여러 검증 규칙을 적용할 때 삼항 연산자가 중첩되어 가독성이 떨어집니다.

```csharp
// 이전 방식: 중첩된 삼항 연산자
public static Validation<Error, string> Validate(string currencyCode) =>
    string.IsNullOrWhiteSpace(currencyCode)
        ? DomainError.For<Currency>(new DomainErrorType.Empty(), currencyCode ?? "", "...")
        : currencyCode.Length != 3
            ? DomainError.For<Currency>(new DomainErrorType.WrongLength(3), currencyCode, "...")
            : currencyCode.ToUpperInvariant();
```

**두 번째 문제는 반복적인 타입 지정입니다.** 매번 `DomainError.For<Currency>(...)` 형태로 타입을 반복 지정해야 합니다.

**세 번째 문제는 검증과 변환의 혼재입니다.** 값 변환(`ToUpperInvariant()`)이 검증 로직 사이에 묻혀 의도가 명확하지 않습니다.

이러한 문제들을 해결하기 위해 **Validate&lt;T&gt; Fluent API**를 도입했습니다. 선형적인 체이닝으로 가독성이 향상되고, 타입을 한 번만 지정하며, 변환은 `ThenNormalize()`로 명시적으로 표현합니다.

## 핵심 개념

이 프로젝트의 핵심은 크게 4가지 개념으로 나눌 수 있습니다.

### 첫 번째 개념: Validate&lt;T&gt; 정적 클래스

`Validate<T>`는 모든 검증의 **단일 진입점**입니다. 타입 파라미터를 한 번만 지정하면 이후 체인에서 자동으로 전달됩니다.

**핵심 아이디어는 "한 번 타입 지정, 전체 체인 적용"입니다.**

```csharp
// 문자열 검증 메서드
Validate<Currency>.NotEmpty(value)        // 빈 값 검증
Validate<Currency>.MinLength(value, 3)    // 최소 길이 검증
Validate<Currency>.MaxLength(value, 100)  // 최대 길이 검증
Validate<Currency>.ExactLength(value, 3)  // 정확한 길이 검증
Validate<Currency>.Matches(value, regex)  // 패턴 검증

// 숫자 검증 메서드
Validate<MoneyAmount>.NonNegative(value)         // 0 이상 검증
Validate<MoneyAmount>.Positive(value)            // 양수 검증
Validate<MoneyAmount>.Between(value, 0, 1000)    // 범위 검증
Validate<MoneyAmount>.AtMost(value, 999999.99m)  // 최대값 검증
Validate<MoneyAmount>.AtLeast(value, 0)          // 최소값 검증

// 커스텀 검증 메서드
Validate<Denominator>.Must(value, v => v != 0, new Zero(), "message")  // sealed record Zero : DomainErrorType.Custom;
```

### 두 번째 개념: TypedValidation&lt;TValueObject, T&gt; 래퍼

`TypedValidation<TValueObject, T>`는 `Validation<Error, T>`를 감싸는 **readonly struct**입니다. 이 래퍼의 목적은 **타입 정보를 체인 전체에 전달**하는 것입니다.

**핵심 아이디어는 "타입 정보 전달을 위한 투명한 래퍼"입니다.**

```csharp
public readonly struct TypedValidation<TValueObject, T>
{
    public Validation<Error, T> Value { get; }

    // Validation<Error, T>로 암시적 변환
    public static implicit operator Validation<Error, T>(TypedValidation<TValueObject, T> typed)
        => typed.Value;
}
```

암시적 변환 덕분에 기존 코드와 완벽하게 호환됩니다:

```csharp
// 반환 타입이 Validation<Error, string>이지만 TypedValidation을 반환해도 됨
public static Validation<Error, string> Validate(string value) =>
    Validate<Currency>.NotEmpty(value)  // TypedValidation<Currency, string> 반환
        .ThenExactLength(3);            // 암시적으로 Validation<Error, string>으로 변환
```

### 세 번째 개념: Fluent 체이닝 확장 메서드

`TypedValidationExtensions` 클래스는 `TypedValidation`에 체이닝 메서드를 제공합니다.

**핵심 아이디어는 "선형적이고 읽기 쉬운 검증 흐름"입니다.**

```csharp
// 문자열 체이닝 메서드
.ThenNotEmpty()           // 빈 값 검증
.ThenMinLength(8)         // 최소 길이 검증
.ThenMaxLength(100)       // 최대 길이 검증
.ThenExactLength(3)       // 정확한 길이 검증
.ThenMatches(regex)       // 패턴 검증
.ThenNormalize(fn)        // 값 변환 (Map 사용)

// 숫자 체이닝 메서드
.ThenNonNegative()        // 0 이상 검증
.ThenPositive()           // 양수 검증
.ThenBetween(0, 1000)     // 범위 검증
.ThenAtMost(max)          // 최대값 검증
.ThenAtLeast(min)         // 최소값 검증

// 커스텀 체이닝 메서드
.ThenMust(predicate, errorType, message)  // 커스텀 조건 검증
```

#### ThenNormalize vs ThenMust

- **`ThenNormalize`**: 값을 **변환**합니다. 내부적으로 `Map`을 사용하여 항상 성공합니다.
- **`ThenMust`**: 값을 **검증**합니다. 내부적으로 `Bind`를 사용하여 조건 실패 시 에러를 반환합니다.

```csharp
// ThenNormalize: 값 변환 (항상 성공)
.ThenNormalize(v => v.ToUpperInvariant())

// ThenMust: 조건부 검증 (실패할 수 있음)
.ThenMust(v => SupportedCurrencies.Contains(v),
    new Unsupported(),  // sealed record Unsupported : DomainErrorType.Custom;
    v => $"Currency '{v}' is not supported")
```

### 네 번째 개념: 자동 에러 코드 생성

`Validate<T>`는 에러 코드를 자동으로 생성합니다. 패턴은 `DomainErrors.{ValueObjectName}.{ErrorTypeName}` 형식입니다.

**핵심 아이디어는 "타입 기반 일관된 에러 코드"입니다.**

```csharp
// 검증 코드 → 생성되는 에러 코드
Validate<Currency>.NotEmpty(value)        → "DomainErrors.Currency.Empty"
Validate<Currency>.ExactLength(value, 3)  → "DomainErrors.Currency.WrongLength"
Validate<MoneyAmount>.NonNegative(value)  → "DomainErrors.MoneyAmount.Negative"
Validate<Coordinate>.Between(x, 0, 1000)  → "DomainErrors.Coordinate.OutOfRange"
```

## Before/After 비교

### Before (기존 방식 - DomainError.For 직접 사용)
```csharp
public sealed class PostalCode : SimpleValueObject<string>
{
    private PostalCode(string value) : base(value) { }

    public static Fin<PostalCode> Create(string value) =>
        CreateFromValidation(Validate(value), validValue => new PostalCode(validValue));

    public static PostalCode CreateFromValidated(string validatedValue) =>
        new PostalCode(validatedValue);

    public static Validation<Error, string> Validate(string value) =>
        ValidateNotEmpty(value).Bind(ValidateFormat);

    private static Validation<Error, string> ValidateNotEmpty(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? DomainError.For<PostalCode>(new DomainErrorType.Empty(), value ?? "",
                $"Postal code cannot be empty. Current value: '{value}'")
            : value;

    private static Validation<Error, string> ValidateFormat(string value) =>
        value.Length != 5 || !value.All(char.IsDigit)
            ? DomainError.For<PostalCode>(new DomainErrorType.WrongLength(5), value,
                $"Postal code must be exactly 5 digits. Current value: '{value}'")
            : value;
}
```

### After (Validate&lt;T&gt; Fluent - 훨씬 간결)
```csharp
public sealed class PostalCode : SimpleValueObject<string>
{
    private static readonly Regex DigitsPattern = new(@"^\d+$", RegexOptions.Compiled);

    private PostalCode(string value) : base(value) { }

    public static Fin<PostalCode> Create(string value) =>
        CreateFromValidation(Validate(value), validValue => new PostalCode(validValue));

    public static PostalCode CreateFromValidated(string validatedValue) =>
        new PostalCode(validatedValue);

    public static Validation<Error, string> Validate(string value) =>
        Validate<PostalCode>.NotEmpty(value ?? "")
            .ThenExactLength(5)
            .ThenMatches(DigitsPattern, "Postal code must contain only digits");
}
```

**코드 감소율: ~70%** (검증 메서드 6줄 → 2줄)

## 실전 지침

### 예상 출력
```
=== Validate<T> Fluent API를 사용한 간결한 검증 패턴 ===

=== Comparable 테스트 ===

--- CompositeValueObjects 하위 폴더 ---
  === CompositeValueObjects 에러 테스트 ===

  --- Currency 에러 테스트 ---
빈 통화 코드: [DomainErrors.Currency.Empty] Currency cannot be empty. Current value: ''
3자리가 아닌 형식: [DomainErrors.Currency.WrongLength] Currency must be exactly 3 characters. Current length: 2
지원하지 않는 통화: [DomainErrors.Currency.Unsupported] Currency 'XYZ' is not supported

  --- Price 에러 테스트 ---
음수 가격: [DomainErrors.MoneyAmount.Negative] MoneyAmount cannot be negative. Current value: '-100'

  --- PriceRange 에러 테스트 ---
최솟값이 최댓값을 초과하는 가격 범위: [DomainErrors.PriceRange.MinExceedsMax] Minimum price cannot exceed maximum price.

--- PrimitiveValueObjects 하위 폴더 ---
  === PrimitiveValueObjects 에러 테스트 ===

  --- Denominator 에러 테스트 ---
0 값: [DomainErrors.Denominator.Zero] Denominator cannot be zero. Current value: '0'

--- CompositePrimitiveValueObjects 하위 폴더 ---
  === CompositePrimitiveValueObjects 에러 테스트 ===

  --- DateRange 에러 테스트 ---
시작일이 종료일 이후인 날짜 범위: [DomainErrors.DateRange.StartAfterEnd] Start date cannot be after end date.

=== ComparableNot 폴더 테스트 ===

--- CompositeValueObjects 하위 폴더 ---
  === CompositeValueObjects 에러 테스트 ===

  --- Address 에러 테스트 ---
빈 거리명: [DomainErrors.Street.Empty] Street cannot be empty. Current value: ''
빈 도시명: [DomainErrors.City.Empty] City cannot be empty. Current value: ''
잘못된 우편번호: [DomainErrors.PostalCode.WrongLength] PostalCode must be exactly 5 characters. Current length: 4

  --- Street 에러 테스트 ---
빈 거리명: [DomainErrors.Street.Empty] Street cannot be empty. Current value: ''

  --- City 에러 테스트 ---
빈 도시명: [DomainErrors.City.Empty] City cannot be empty. Current value: ''

  --- PostalCode 에러 테스트 ---
빈 우편번호: [DomainErrors.PostalCode.Empty] PostalCode cannot be empty. Current value: ''
5자리 숫자가 아닌 형식: [DomainErrors.PostalCode.WrongLength] PostalCode must be exactly 5 characters. Current length: 4

--- PrimitiveValueObjects 하위 폴더 ---
  === PrimitiveValueObjects 에러 테스트 ===

  --- BinaryData 에러 테스트 ---
null 바이너리 데이터: [DomainErrors.BinaryData.Empty] BinaryData cannot be empty. Current value: 'null'
빈 바이너리 데이터: [DomainErrors.BinaryData.Empty] BinaryData cannot be empty. Current value: 'null'

--- CompositePrimitiveValueObjects 하위 폴더 ---
  === CompositePrimitiveValueObjects 에러 테스트 ===

  --- Coordinate 에러 테스트 ---
범위를 벗어난 X 좌표: [DomainErrors.Coordinate.OutOfRange] Coordinate must be between 0 and 1000. Current value: '-1'
범위를 벗어난 Y 좌표: [DomainErrors.Coordinate.OutOfRange] Coordinate must be between 0 and 1000. Current value: '1001'
```

### 핵심 구현 포인트
1. **Validate&lt;T&gt;로 시작**: 모든 검증은 `Validate<ValueObjectType>.메서드()`로 시작
2. **체이닝으로 연결**: `Then*()` 메서드로 추가 검증 규칙 연결
3. **ThenNormalize로 변환**: 값 변환은 검증 체인 마지막에 `ThenNormalize()` 사용
4. **ThenMust로 커스텀 검증**: 표준 메서드로 표현하기 어려운 검증은 `ThenMust()` 사용
5. **암시적 변환 활용**: `TypedValidation` → `Validation<Error, T>` 자동 변환

## 프로젝트 설명

### 프로젝트 구조
```
15-Validation-Fluent/
├── README.md                              # 이 문서
├── ValidationFluent/                      # 메인 프로젝트
│   ├── Program.cs                         # 메인 실행 파일
│   ├── ValidationFluent.csproj            # 프로젝트 파일
│   └── ValueObjects/                      # 값 객체 구현
│       ├── 01-ComparableNot/              # 비교 불가능한 값 객체
│       │   ├── 01-PrimitiveValueObjects/
│       │   │   └── BinaryData.cs          # 바이너리 데이터
│       │   ├── 02-CompositePrimitiveValueObjects/
│       │   │   └── Coordinate.cs          # 좌표 (x, y)
│       │   └── 03-CompositeValueObjects/
│       │       ├── Street.cs              # 거리명
│       │       ├── City.cs                # 도시명
│       │       ├── PostalCode.cs          # 우편번호
│       │       └── Address.cs             # 주소 (복합)
│       └── 02-Comparable/                 # 비교 가능한 값 객체
│           ├── 01-PrimitiveValueObjects/
│           │   └── Denominator.cs         # 분모 (0이 아닌 정수)
│           ├── 02-CompositePrimitiveValueObjects/
│           │   └── DateRange.cs           # 날짜 범위
│           └── 03-CompositeValueObjects/
│               ├── Currency.cs            # 통화 (SmartEnum 기반)
│               ├── MoneyAmount.cs         # 금액
│               ├── Price.cs               # 가격 (금액 + 통화)
│               └── PriceRange.cs          # 가격 범위
└── ValidationFluent.Tests.Unit/           # 단위 테스트
    ├── Using.cs                           # 전역 using 정의
    ├── PostalCodeTests.cs                 # PostalCode Fluent 검증 테스트
    └── CurrencyTests.cs                   # Currency Fluent 검증 테스트
```

### 핵심 코드

#### Validate&lt;T&gt; 정적 클래스 (Functorium 프레임워크 제공)
```csharp
/// <summary>
/// 타입 파라미터를 한 번만 지정하는 검증 시작점
/// </summary>
public static class Validate<TValueObject>
{
    // 문자열 검증
    public static TypedValidation<TValueObject, string> NotEmpty(string value);
    public static TypedValidation<TValueObject, string> MinLength(string value, int minLength);
    public static TypedValidation<TValueObject, string> MaxLength(string value, int maxLength);
    public static TypedValidation<TValueObject, string> ExactLength(string value, int length);
    public static TypedValidation<TValueObject, string> Matches(string value, Regex pattern, string? message = null);

    // 숫자 검증
    public static TypedValidation<TValueObject, T> NonNegative<T>(T value) where T : INumber<T>;
    public static TypedValidation<TValueObject, T> Positive<T>(T value) where T : INumber<T>;
    public static TypedValidation<TValueObject, T> Between<T>(T value, T min, T max) where T : INumber<T>;
    public static TypedValidation<TValueObject, T> AtMost<T>(T value, T max) where T : INumber<T>;
    public static TypedValidation<TValueObject, T> AtLeast<T>(T value, T min) where T : INumber<T>;

    // 커스텀 검증
    public static TypedValidation<TValueObject, T> Must<T>(
        T value, Func<T, bool> predicate, DomainErrorType errorType, string message);
}
```

#### TypedValidationExtensions (Functorium 프레임워크 제공)
```csharp
/// <summary>
/// TypedValidation 체이닝을 위한 확장 메서드
/// </summary>
public static class TypedValidationExtensions
{
    // 문자열 체이닝
    public static TypedValidation<TVO, string> ThenNotEmpty<TVO>(this TypedValidation<TVO, string> v);
    public static TypedValidation<TVO, string> ThenMinLength<TVO>(this TypedValidation<TVO, string> v, int min);
    public static TypedValidation<TVO, string> ThenMaxLength<TVO>(this TypedValidation<TVO, string> v, int max);
    public static TypedValidation<TVO, string> ThenExactLength<TVO>(this TypedValidation<TVO, string> v, int len);
    public static TypedValidation<TVO, string> ThenMatches<TVO>(this TypedValidation<TVO, string> v, Regex pattern);
    public static TypedValidation<TVO, string> ThenNormalize<TVO>(this TypedValidation<TVO, string> v, Func<string, string> fn);

    // 숫자 체이닝
    public static TypedValidation<TVO, T> ThenNonNegative<TVO, T>(this TypedValidation<TVO, T> v) where T : INumber<T>;
    public static TypedValidation<TVO, T> ThenPositive<TVO, T>(this TypedValidation<TVO, T> v) where T : INumber<T>;
    public static TypedValidation<TVO, T> ThenBetween<TVO, T>(this TypedValidation<TVO, T> v, T min, T max) where T : INumber<T>;
    public static TypedValidation<TVO, T> ThenAtMost<TVO, T>(this TypedValidation<TVO, T> v, T max) where T : INumber<T>;
    public static TypedValidation<TVO, T> ThenAtLeast<TVO, T>(this TypedValidation<TVO, T> v, T min) where T : INumber<T>;

    // 커스텀 체이닝
    public static TypedValidation<TVO, T> ThenMust<TVO, T>(this TypedValidation<TVO, T> v,
        Func<T, bool> predicate, DomainErrorType errorType, string message);
    public static TypedValidation<TVO, T> ThenMust<TVO, T>(this TypedValidation<TVO, T> v,
        Func<T, bool> predicate, DomainErrorType errorType, Func<T, string> messageFactory);
}
```

#### PostalCode - 가장 간결한 예시
```csharp
public sealed class PostalCode : SimpleValueObject<string>
{
    private static readonly Regex DigitsPattern = new(@"^\d+$", RegexOptions.Compiled);

    private PostalCode(string value) : base(value) { }

    public static Fin<PostalCode> Create(string value) =>
        CreateFromValidation(Validate(value), validValue => new PostalCode(validValue));

    public static PostalCode CreateFromValidated(string validatedValue) =>
        new PostalCode(validatedValue);

    public static Validation<Error, string> Validate(string value) =>
        Validate<PostalCode>.NotEmpty(value ?? "")
            .ThenExactLength(5)
            .ThenMatches(DigitsPattern, "Postal code must contain only digits");
}
```

#### Currency - SmartEnum 기반 값 객체
```csharp
public sealed class Currency
    : SmartEnum<Currency, string>
    , IValueObject
{
    // 커스텀 에러 타입 정의
    public sealed record Unsupported : DomainErrorType.Custom;

    public static readonly Currency KRW = new(nameof(KRW), "KRW", "한국 원화", "₩");
    public static readonly Currency USD = new(nameof(USD), "USD", "미국 달러", "$");
    // ... 기타 통화들 ...

    private static readonly HashSet<string> SupportedCodes =
        new(List.Select(c => c.Value), StringComparer.OrdinalIgnoreCase);

    public static Fin<Currency> Create(string currencyCode) =>
        Validate(currencyCode)
            .Map(FromValue)
            .ToFin();

    public static Validation<Error, string> Validate(string currencyCode) =>
        Validate<Currency>.NotEmpty(currencyCode ?? "")
            .ThenExactLength(3)
            .ThenNormalize(v => v.ToUpperInvariant())
            .ThenMust(
                v => SupportedCodes.Contains(v),
                new Unsupported(),  // sealed record Unsupported : DomainErrorType.Custom;
                v => $"Currency '{v}' is not supported");
}
```

#### MoneyAmount - 숫자 범위 검증
```csharp
public sealed class MoneyAmount : ComparableSimpleValueObject<decimal>
{
    private MoneyAmount(decimal value) : base(value) { }

    public static Fin<MoneyAmount> Create(decimal value) =>
        CreateFromValidation(Validate(value), validValue => new MoneyAmount(validValue));

    public static MoneyAmount CreateFromValidated(decimal validatedValue) =>
        new MoneyAmount(validatedValue);

    public static Validation<Error, decimal> Validate(decimal value) =>
        Validate<MoneyAmount>.NonNegative(value)
            .ThenAtMost(999999.99m);
}
```

#### Coordinate - 복합 기본형 값 객체
```csharp
public sealed class Coordinate : ValueObject
{
    public int X { get; }
    public int Y { get; }

    private Coordinate(int x, int y) { X = x; Y = y; }

    public static Fin<Coordinate> Create(int x, int y) =>
        CreateFromValidation(Validate(x, y), validValues => new Coordinate(validValues.X, validValues.Y));

    public static Validation<Error, (int X, int Y)> Validate(int x, int y) =>
        from validX in ValidateX(x)
        from validY in ValidateY(y)
        select (X: validX, Y: validY);

    private static Validation<Error, int> ValidateX(int x) =>
        Validate<Coordinate>.Between(x, 0, 1000);

    private static Validation<Error, int> ValidateY(int y) =>
        Validate<Coordinate>.Between(y, 0, 1000);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return X;
        yield return Y;
    }
}
```

## 한눈에 보는 정리

### 비교 표
| 구분 | 이전 방식 (DomainError.For 직접 사용) | 현재 방식 (Validate&lt;T&gt; Fluent) |
|------|---------------------------------------|--------------------------------------|
| **타입 지정** | 매번 `DomainError.For<T>(...)` | 처음 한 번 `Validate<T>.메서드()` |
| **검증 흐름** | 중첩된 삼항 연산자/Bind 체인 | 선형적인 `Then*()` 체이닝 |
| **값 변환** | 검증 로직에 묻혀 있음 | `ThenNormalize()`로 명시적 |
| **에러 메시지** | 매번 수동 작성 | 자동 생성 (커스텀 가능) |
| **코드량** | 검증 메서드당 5-10줄 | 검증 메서드당 1-3줄 |
| **가독성** | 중첩 구조로 파악 어려움 | 선형 구조로 한눈에 파악 |

### Validate&lt;T&gt; 메서드 선택 가이드
| 검증 조건 | 시작 메서드 | 체이닝 메서드 |
|-----------|-------------|---------------|
| 빈 값 | `Validate<T>.NotEmpty(value)` | `.ThenNotEmpty()` |
| 최소 길이 | `Validate<T>.MinLength(value, min)` | `.ThenMinLength(min)` |
| 최대 길이 | `Validate<T>.MaxLength(value, max)` | `.ThenMaxLength(max)` |
| 정확한 길이 | `Validate<T>.ExactLength(value, len)` | `.ThenExactLength(len)` |
| 패턴 매칭 | `Validate<T>.Matches(value, regex)` | `.ThenMatches(regex)` |
| 0 이상 | `Validate<T>.NonNegative(value)` | `.ThenNonNegative()` |
| 양수 | `Validate<T>.Positive(value)` | `.ThenPositive()` |
| 범위 | `Validate<T>.Between(value, min, max)` | `.ThenBetween(min, max)` |
| 최대값 | `Validate<T>.AtMost(value, max)` | `.ThenAtMost(max)` |
| 최소값 | `Validate<T>.AtLeast(value, min)` | `.ThenAtLeast(min)` |
| 커스텀 | `Validate<T>.Must(value, pred, type, msg)` | `.ThenMust(pred, type, msg)` |

### 장단점 표
| 장점 | 단점 |
|------|------|
| **코드량 70% 감소** | Functorium 프레임워크 의존성 |
| **선형적이고 읽기 쉬운 흐름** | 학습 곡선 (새로운 API) |
| **타입 한 번 지정** | - |
| **자동 에러 메시지 생성** | - |
| **명시적 값 변환 (ThenNormalize)** | - |
| **기존 코드 완벽 호환** | - |

### 핵심 개선사항
- **단일 진입점**: `Validate<T>`로 모든 검증 시작
- **Fluent 체이닝**: `Then*()` 메서드로 선형적 검증 흐름
- **타입 정보 자동 전달**: `TypedValidation` 래퍼로 체인 전체에 타입 전달
- **명시적 변환**: `ThenNormalize()`로 값 변환 의도 명확화
- **커스텀 검증 통합**: `ThenMust()`로 표준 체인에 커스텀 로직 추가
- **암시적 변환**: 기존 `Validation<Error, T>` 반환 타입과 호환

## FAQ

### Q1: 언제 Validate&lt;T&gt;를 사용하고 언제 DomainError.For&lt;T&gt;를 사용하나요?
**A**: 대부분의 경우 `Validate<T>` Fluent API를 사용하면 됩니다. `DomainError.For<T>()`는 다음 경우에만 사용합니다:

```csharp
// Validate<T> 사용 (권장) - 표준 검증 패턴
public static Validation<Error, string> Validate(string value) =>
    Validate<PostalCode>.NotEmpty(value)
        .ThenExactLength(5);

// DomainError.For<T>() 사용 - 복잡한 비즈니스 로직
// sealed record MinExceedsMax : DomainErrorType.Custom;
private static Validation<Error, (Price Min, Price Max)> ValidatePriceRange(Price min, Price max) =>
    (decimal)min.Amount > (decimal)max.Amount
        ? DomainError.For<PriceRange>(new MinExceedsMax(),
            $"Min: {min}, Max: {max}",
            $"Minimum price cannot exceed maximum price.")
        : (Min: min, Max: max);
```

### Q2: ThenNormalize는 언제 사용하나요?
**A**: 검증 후 값을 변환(정규화)할 때 사용합니다. 항상 검증 체인의 마지막에 배치합니다:

```csharp
// Good: 검증 후 변환
Validate<Currency>.NotEmpty(value)
    .ThenExactLength(3)               // 먼저 검증
    .ThenNormalize(v => v.ToUpperInvariant());  // 마지막에 변환

// Bad: 변환 후 검증 (의도치 않은 결과 가능)
Validate<Currency>.NotEmpty(value)
    .ThenNormalize(v => v.ToUpperInvariant())
    .ThenExactLength(3);  // 이미 대문자로 변환된 값을 검증
```

### Q3: ThenMust의 messageFactory 오버로드는 언제 사용하나요?
**A**: 에러 메시지에 현재 값을 포함시키고 싶을 때 사용합니다:

```csharp
// 정적 메시지 (값 정보 없음)
.ThenMust(v => SupportedCodes.Contains(v),
    new Unsupported(),  // sealed record Unsupported : DomainErrorType.Custom;
    "Currency is not supported")

// 동적 메시지 (값 정보 포함) - 권장
.ThenMust(v => SupportedCodes.Contains(v),
    new Unsupported(),  // sealed record Unsupported : DomainErrorType.Custom;
    v => $"Currency '{v}' is not supported")
```

### Q4: 여러 필드를 동시에 검증하려면?
**A**: 개별 필드를 `Validate<T>`로 검증하고, LINQ 쿼리 구문이나 `Apply`로 결합합니다:

```csharp
// LINQ 쿼리 구문 (권장)
public static Validation<Error, (int X, int Y)> Validate(int x, int y) =>
    from validX in Validate<Coordinate>.Between(x, 0, 1000)
    from validY in Validate<Coordinate>.Between(y, 0, 1000)
    select (X: validX, Y: validY);

// Apply 패턴 (모든 에러 수집)
public static Validation<Error, (string Street, string City)> Validate(string street, string city) =>
    (Validate<Address>.NotEmpty(street), Validate<Address>.NotEmpty(city))
        .Apply((s, c) => (Street: s, City: c));
```

### Q5: 기존 14-Error-Code-Fluent 코드를 마이그레이션하는 방법은?
**A**: 3단계로 마이그레이션합니다.

1. **단일 검증을 Validate&lt;T&gt;로 변경**:
   ```csharp
   // Before
   public static Validation<Error, string> Validate(string value) =>
       string.IsNullOrWhiteSpace(value)
           ? DomainError.For<Street>(new DomainErrorType.Empty(), value ?? "", "...")
           : value;

   // After
   public static Validation<Error, string> Validate(string value) =>
       Validate<Street>.NotEmpty(value ?? "");
   ```

2. **다단계 검증을 체이닝으로 변경**:
   ```csharp
   // Before
   ValidateNotEmpty(value).Bind(ValidateFormat)

   // After
   Validate<PostalCode>.NotEmpty(value ?? "")
       .ThenExactLength(5)
       .ThenMatches(DigitsPattern)
   ```

3. **불필요한 private 메서드 제거**: `ValidateNotEmpty`, `ValidateFormat` 등 분리된 검증 메서드 제거

### Q6: 성능에 영향이 있나요?
**A**: 런타임 성능 차이는 미미합니다. `TypedValidation`은 `readonly struct`로 힙 할당이 없고, 체이닝 메서드는 JIT 컴파일 시 인라인됩니다. 검증 실패 시에만 에러 객체가 생성되므로 성공 경로의 성능은 기존과 동일합니다.
