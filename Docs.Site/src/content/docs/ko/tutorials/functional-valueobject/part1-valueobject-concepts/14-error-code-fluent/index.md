---
title: "DomainError 헬퍼"
---

## 개요

값 객체마다 `Domain` 중첩 클래스를 반복 정의하고, `ErrorFactory.Create()`로 에러 코드를 수동 조합하는 작업이 번거롭지 않았나요? 이 장에서는 `DomainError.For<T>()` 한 줄로 에러 생성을 대체하여 코드량을 약 60% 줄이고, `DomainErrorKind` 레코드로 타입 안전한 에러 코드를 보장하는 패턴을 다룹니다.

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다.

1. `DomainError.For<T>()` 메서드를 사용하여 간결하게 에러를 생성할 수 있습니다
2. `DomainErrorKind` 레코드로 오타와 불일치를 컴파일 타임에 방지할 수 있습니다
3. `Domain` 중첩 클래스 없이 인라인으로 에러를 정의할 수 있습니다
4. 타입별 오버로딩(`For<T>()`, `For<T, TValue>()`, `For<T, T1, T2>()`)을 상황에 맞게 선택할 수 있습니다

## 왜 필요한가?

이전 `13-Error-Code` 프로젝트에서 구조화된 에러 코드 시스템을 도입했지만, 각 값 객체마다 `Domain` 중첩 클래스를 정의해야 하는 보일러플레이트가 남아 있었습니다. 모든 값 객체에서 동일한 패턴의 `internal static class Domain`를 반복 정의해야 했고, `$"{nameof(Domain)}.{nameof(Denominator)}.{nameof(Zero)}"` 형태로 에러 코드를 수동 조합해야 했습니다. 게다가 `"Empty"`, `"IsEmpty"`, `"EmptyValue"`처럼 동일한 개념에 대해 개발자마다 다른 이름을 사용할 수 있어 일관성이 깨지기 쉬웠습니다.

**DomainError 헬퍼와 DomainErrorKind 레코드는** 타입 안전한 에러 유형과 자동 에러 코드 생성으로 이 문제들을 해결합니다.

## 핵심 개념

### DomainErrorKind 레코드

`DomainErrorKind`은 표준화된 에러 유형을 정의하는 추상 레코드입니다. 문자열 대신 타입을 사용하여 컴파일 타임 안전성을 보장합니다. 표준 에러는 미리 정의된 타입을 사용하고, 도메인 특화 에러는 `DomainErrorKind.Custom`을 상속하여 명시적으로 정의합니다.

```csharp
// 표준 에러 타입들 (타입 안전)
new DomainErrorKind.Empty()           // 빈 값
new DomainErrorKind.Null()            // null 값
new DomainErrorKind.TooShort(8)       // 최소 길이 미달
new DomainErrorKind.TooLong(100)      // 최대 길이 초과
new DomainErrorKind.WrongLength(5)    // 정확한 길이 불일치
new DomainErrorKind.InvalidFormat()   // 형식 오류
new DomainErrorKind.Negative()        // 음수 값
new DomainErrorKind.NotPositive()     // 양수가 아닌 값
new DomainErrorKind.OutOfRange("0", "1000")  // 범위 초과
new DomainErrorKind.BelowMinimum("0")        // 최솟값 미달
new DomainErrorKind.AboveMaximum("100")      // 최댓값 초과
new DomainErrorKind.NotFound()        // 찾을 수 없음
new DomainErrorKind.AlreadyExists()   // 이미 존재
new DomainErrorKind.NotUpperCase()    // 대문자 아님
new DomainErrorKind.NotLowerCase()    // 소문자 아님
new DomainErrorKind.Duplicate()       // 중복
new DomainErrorKind.Mismatch()        // 불일치

// 커스텀 에러 (비표준 케이스용) - sealed record 파생 정의
// public sealed record Unsupported : DomainErrorKind.Custom;
new Unsupported()    // 도메인 특화 에러
```

### DomainError 헬퍼

DomainError 헬퍼는 `typeof(T).Name`과 `DomainErrorKind`을 조합하여 `Domain.{ValueObjectName}.{ErrorType}` 형식의 에러 코드를 자동으로 생성합니다.

```csharp
// DomainError 헬퍼 사용법

// 1. 문자열 값 검증 시
DomainError.For<Currency>(new DomainErrorKind.Empty(), currencyCode ?? "",
    $"Currency code cannot be empty. Current value: '{currencyCode}'")
// 생성되는 에러 코드: "Domain.Currency.Empty"

// 2. 제네릭 값 검증 시 (커스텀 에러: sealed record Zero : DomainErrorKind.Custom;)
DomainError.For<Denominator, int>(new Zero(), value,
    $"Denominator cannot be zero. Current value: '{value}'")
// 생성되는 에러 코드: "Domain.Denominator.Zero"

// 3. 범위 검증 시
DomainError.For<Coordinate, int>(new DomainErrorKind.OutOfRange("0", "1000"), x,
    $"X coordinate must be between 0 and 1000. Current value: '{x}'")
// 생성되는 에러 코드: "Domain.Coordinate.OutOfRange"
```

### 인라인 에러 정의

DomainError 헬퍼를 사용하면 검증 실패 시점에서 바로 에러를 생성할 수 있어, 별도의 `Domain` 중첩 클래스가 불필요합니다. 검증과 에러 정의가 한 곳에 위치하므로 코드 응집도가 높아집니다.

```csharp
// 인라인 에러 정의 예시 (커스텀 에러: sealed record Zero : DomainErrorKind.Custom;)
public static Validation<Error, int> Validate(int value) =>
    value == 0
        ? DomainError.For<Denominator, int>(new Zero(), value,
            $"Denominator cannot be zero. Current value: '{value}'")
        : value;
```

## Before/After 비교

### Before (기존 방식 - 40줄+)
```csharp
public sealed class Denominator : ComparableSimpleValueObject<int>
{
    private Denominator(int value) : base(value) { }

    public static Fin<Denominator> Create(int value) =>
        CreateFromValidation(Validate(value), validValue => new Denominator(validValue));

    public static Denominator CreateFromValidated(int validatedValue) =>
        new Denominator(validatedValue);

    public static Validation<Error, int> Validate(int value)
    {
        if (value == 0)
            return Domain.Zero(value);
        return value;
    }

    // Domain 중첩 클래스 - 모든 값 객체에서 반복됨
    internal static class Domain
    {
        public static Error Zero(int value) =>
            ErrorFactory.Create(
                errorCode: $"{nameof(Domain)}.{nameof(Denominator)}.{nameof(Zero)}",
                errorCurrentValue: value,
                errorMessage: $"Denominator cannot be zero. Current value: '{value}'");
    }
}
```

### After (DomainError + DomainErrorKind - 15줄)
```csharp
public sealed class Denominator : ComparableSimpleValueObject<int>
{
    private Denominator(int value) : base(value) { }

    public static Fin<Denominator> Create(int value) =>
        CreateFromValidation(Validate(value), validValue => new Denominator(validValue));

    public static Denominator CreateFromValidated(int validatedValue) =>
        new Denominator(validatedValue);

    // 커스텀 에러 타입 정의
    public sealed record Zero : DomainErrorKind.Custom;

    public static Validation<Error, int> Validate(int value) =>
        value == 0
            ? DomainError.For<Denominator, int>(new Zero(), value,
                $"Denominator cannot be zero. Current value: '{value}'")
            : value;
}
```

**코드 감소율: ~60%**

## 실전 지침

### 예상 출력
```
=== DomainError 헬퍼를 사용한 간결한 에러 처리 패턴 ===

=== Comparable 테스트 ===

--- CompositeValueObjects 하위 폴더 ---
  === CompositeValueObjects 에러 테스트 ===

  --- Currency 에러 테스트 ---
빈 통화 코드: [Domain.Currency.Empty] Currency code cannot be empty. Current value: ''
3자리가 아닌 형식: [Domain.Currency.WrongLength] Currency code must be exactly 3 letters. Current value: 'AB'
지원하지 않는 통화: [Domain.Currency.Unsupported] Currency code is not supported. Current value: 'XYZ'

  --- Price 에러 테스트 ---
음수 가격: [Domain.MoneyAmount.OutOfRange] Money amount must be between 0 and 999999.99. Current value: '-100'

  --- PriceRange 에러 테스트 ---
최솟값이 최댓값을 초과하는 가격 범위: [Domain.PriceRange.MinExceedsMax] Minimum price cannot exceed maximum price.

--- PrimitiveValueObjects 하위 폴더 ---
  === PrimitiveValueObjects 에러 테스트 ===

  --- Denominator 에러 테스트 ---
0 값: [Domain.Denominator.Zero] Denominator cannot be zero. Current value: '0'

--- CompositePrimitiveValueObjects 하위 폴더 ---
  === CompositePrimitiveValueObjects 에러 테스트 ===

  --- DateRange 에러 테스트 ---
시작일이 종료일 이후인 날짜 범위: [Domain.DateRange.StartAfterEnd] Start date cannot be after end date.

=== ComparableNot 폴더 테스트 ===

--- CompositeValueObjects 하위 폴더 ---
  === CompositeValueObjects 에러 테스트 ===

  --- Address 에러 테스트 ---
빈 거리명: [Domain.Street.Empty] Street name cannot be empty.
빈 도시명: [Domain.City.Empty] City name cannot be empty.
잘못된 우편번호: [Domain.PostalCode.WrongLength] Postal code must be exactly 5 digits.

  --- Street 에러 테스트 ---
빈 거리명: [Domain.Street.Empty] Street name cannot be empty.

  --- City 에러 테스트 ---
빈 도시명: [Domain.City.Empty] City name cannot be empty.

  --- PostalCode 에러 테스트 ---
빈 우편번호: [Domain.PostalCode.Empty] Postal code cannot be empty.
5자리 숫자가 아닌 형식: [Domain.PostalCode.WrongLength] Postal code must be exactly 5 digits.

--- PrimitiveValueObjects 하위 폴더 ---
  === PrimitiveValueObjects 에러 테스트 ===

  --- BinaryData 에러 테스트 ---
null 바이너리 데이터: [Domain.BinaryData.Empty] Binary data cannot be empty.
빈 바이너리 데이터: [Domain.BinaryData.Empty] Binary data cannot be empty.

--- CompositePrimitiveValueObjects 하위 폴더 ---
  === CompositePrimitiveValueObjects 에러 테스트 ===

  --- Coordinate 에러 테스트 ---
범위를 벗어난 X 좌표: [Domain.Coordinate.OutOfRange] X coordinate must be between 0 and 1000.
범위를 벗어난 Y 좌표: [Domain.Coordinate.OutOfRange] Y coordinate must be between 0 and 1000.
```

### 핵심 구현 포인트

표준 에러는 `new DomainErrorKind.Empty()` 등 미리 정의된 타입을 사용하고, 표준 에러로 표현하기 어려운 도메인 특화 에러는 `sealed record` 파생 정의 후 사용합니다. `DomainError.For<T>()`가 타입 정보에서 에러 코드를 자동 생성하므로, 검증 로직 내에서 직접 에러를 인라인 정의할 수 있습니다. 기존 `Validation<Error, T>`, `Fin<T>` 타입과 완벽 호환됩니다.

## 프로젝트 설명

### 프로젝트 구조
```
14-Error-Code-Fluent/
├── README.md                              # 이 문서
├── ErrorCodeFluent/                       # 메인 프로젝트
│   ├── Program.cs                         # 메인 실행 파일
│   ├── ErrorCodeFluent.csproj             # 프로젝트 파일
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
└── ErrorCodeFluent.Tests.Unit/            # 단위 테스트
    ├── Using.cs                           # 전역 using 정의
    ├── DenominatorTests.cs                # Denominator 타입 안전 테스트
    └── ErrorFactoryTests.cs           # DomainError + Assertion 종합 테스트
```

### 핵심 코드

#### DomainErrorKind (Functorium 프레임워크 제공)
```csharp
/// <summary>
/// 도메인 에러 유형을 정의하는 추상 레코드
/// 타입 안전한 에러 코드 생성을 위해 사용
/// </summary>
public abstract record DomainErrorKind
{
    // 값 존재 검증
    public sealed record Empty : DomainErrorKind;
    public sealed record Null : DomainErrorKind;

    // 문자열 길이 검증
    public sealed record TooShort(int Minimum = 0) : DomainErrorKind;
    public sealed record TooLong(int Maximum = 0) : DomainErrorKind;
    public sealed record WrongLength(int Expected = 0) : DomainErrorKind;

    // 형식 검증
    public sealed record InvalidFormat : DomainErrorKind;

    // 숫자 범위 검증
    public sealed record Negative : DomainErrorKind;
    public sealed record NotPositive : DomainErrorKind;
    public sealed record OutOfRange(string? Minimum = null, string? Maximum = null) : DomainErrorKind;
    public sealed record BelowMinimum(string? Minimum = null) : DomainErrorKind;
    public sealed record AboveMaximum(string? Maximum = null) : DomainErrorKind;

    // 존재 여부 검증
    public sealed record NotFound : DomainErrorKind;
    public sealed record AlreadyExists : DomainErrorKind;

    // 대소문자 검증
    public sealed record NotUpperCase : DomainErrorKind;
    public sealed record NotLowerCase : DomainErrorKind;

    // 비즈니스 규칙 검증
    public sealed record Duplicate : DomainErrorKind;
    public sealed record Mismatch : DomainErrorKind;

    // 커스텀 에러 (도메인 특화) - abstract record, 파생하여 사용
    public abstract record Custom : DomainErrorKind;
}

// 커스텀 에러 정의 예시 (값 객체 내부에 nested record로 정의)
// public sealed record Unsupported : DomainErrorKind.Custom;
// public sealed record Zero : DomainErrorKind.Custom;
```

#### DomainError 헬퍼 (Functorium 프레임워크 제공)
```csharp
/// <summary>
/// 도메인 에러를 간결하게 생성하기 위한 헬퍼 클래스
/// 에러 코드를 타입 정보에서 자동으로 생성합니다.
/// </summary>
public static class DomainError
{
    /// <summary>
    /// 문자열 값에 대한 도메인 에러 생성
    /// </summary>
    public static Error For<TValueObject>(
        DomainErrorKind errorType, string currentValue, string message)
        where TValueObject : class =>
        ErrorFactory.Create(
            errorCode: $"Domain.{typeof(TValueObject).Name}.{GetErrorName(errorType)}",
            errorCurrentValue: currentValue,
            errorMessage: message);

    /// <summary>
    /// 제네릭 값에 대한 도메인 에러 생성
    /// </summary>
    public static Error For<TValueObject, TValue>(
        DomainErrorKind errorType, TValue currentValue, string message)
        where TValueObject : class
        where TValue : notnull =>
        ErrorFactory.Create(
            errorCode: $"Domain.{typeof(TValueObject).Name}.{GetErrorName(errorType)}",
            errorCurrentValue: currentValue,
            errorMessage: message);

    private static string GetErrorName(DomainErrorKind errorType) =>
        errorType.GetType().Name;
}
```

#### Denominator - 가장 간단한 예시
```csharp
public sealed class Denominator : ComparableSimpleValueObject<int>
{
    // 커스텀 에러 타입 정의
    public sealed record Zero : DomainErrorKind.Custom;

    private Denominator(int value) : base(value) { }

    public static Fin<Denominator> Create(int value) =>
        CreateFromValidation(Validate(value), validValue => new Denominator(validValue));

    public static Denominator CreateFromValidated(int validatedValue) =>
        new Denominator(validatedValue);

    public static Validation<Error, int> Validate(int value) =>
        value == 0
            ? DomainError.For<Denominator, int>(new Zero(), value,
                $"Denominator cannot be zero. Current value: '{value}'")
            : value;
}
```

#### Currency - SmartEnum 기반 값 객체
```csharp
public sealed class Currency
    : SmartEnum<Currency, string>
    , IValueObject
{
    // 커스텀 에러 타입 정의
    public sealed record Unsupported : DomainErrorKind.Custom;

    public static readonly Currency KRW = new(nameof(KRW), "KRW", "한국 원화", "₩");
    public static readonly Currency USD = new(nameof(USD), "USD", "미국 달러", "$");
    // ... 기타 통화들 ...

    public static Fin<Currency> Create(string currencyCode) =>
        Validate(currencyCode)
            .Map(FromValue)
            .ToFin();

    public static Validation<Error, string> Validate(string currencyCode) =>
        ValidateNotEmpty(currencyCode)
            .Bind(ValidateFormat)
            .Bind(ValidateSupported);

    private static Validation<Error, string> ValidateNotEmpty(string currencyCode) =>
        string.IsNullOrWhiteSpace(currencyCode)
            ? DomainError.For<Currency>(new DomainErrorKind.Empty(), currencyCode ?? "",
                $"Currency code cannot be empty. Current value: '{currencyCode}'")
            : currencyCode;

    private static Validation<Error, string> ValidateFormat(string currencyCode) =>
        currencyCode.Length != 3 || !currencyCode.All(char.IsLetter)
            ? DomainError.For<Currency>(new DomainErrorKind.WrongLength(3), currencyCode,
                $"Currency code must be exactly 3 letters. Current value: '{currencyCode}'")
            : currencyCode.ToUpperInvariant();

    private static Validation<Error, string> ValidateSupported(string currencyCode)
    {
        try
        {
            FromValue(currencyCode);
            return currencyCode;
        }
        catch (SmartEnumNotFoundException)
        {
            return DomainError.For<Currency>(new Unsupported(), currencyCode,
                $"Currency code is not supported. Current value: '{currencyCode}'");
        }
    }
}
```

#### PostalCode - 다단계 검증
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
            ? DomainError.For<PostalCode>(new DomainErrorKind.Empty(), value ?? "",
                $"Postal code cannot be empty. Current value: '{value}'")
            : value;

    private static Validation<Error, string> ValidateFormat(string value) =>
        value.Length != 5 || !value.All(char.IsDigit)
            ? DomainError.For<PostalCode>(new DomainErrorKind.WrongLength(5), value,
                $"Postal code must be exactly 5 digits. Current value: '{value}'")
            : value;
}
```

#### Coordinate - 복합 기본형 값 객체
```csharp
public sealed class Coordinate : ValueObject
{
    public int X { get; }
    public int Y { get; }

    private Coordinate(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static Fin<Coordinate> Create(int x, int y) =>
        CreateFromValidation(Validate(x, y), validValues => new Coordinate(validValues.X, validValues.Y));

    public static Validation<Error, (int X, int Y)> Validate(int x, int y) =>
        from validX in ValidateX(x)
        from validY in ValidateY(y)
        select (X: validX, Y: validY);

    private static Validation<Error, int> ValidateX(int x) =>
        x < 0 || x > 1000
            ? DomainError.For<Coordinate, int>(new DomainErrorKind.OutOfRange("0", "1000"), x,
                $"X coordinate must be between 0 and 1000. Current value: '{x}'")
            : x;

    private static Validation<Error, int> ValidateY(int y) =>
        y < 0 || y > 1000
            ? DomainError.For<Coordinate, int>(new DomainErrorKind.OutOfRange("0", "1000"), y,
                $"Y coordinate must be between 0 and 1000. Current value: '{y}'")
            : y;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return X;
        yield return Y;
    }

    public override string ToString() => $"({X}, {Y})";
}
```

## 한눈에 보는 정리

이전 방식과 DomainError 헬퍼 방식의 차이를 비교합니다.

### 비교 표
| 구분 | 이전 방식 (Domain 중첩 클래스) | 현재 방식 (DomainError + DomainErrorKind) |
|------|--------------------------------------|-------------------------------------------|
| **에러 정의 위치** | 별도 중첩 클래스 | 검증 로직 내 인라인 |
| **에러 코드 생성** | 수동 조합 (nameof 사용) | 자동 생성 (타입 정보 + DomainErrorKind) |
| **에러 이름 안전성** | 문자열 기반 (오타 가능) | 타입 기반 (컴파일 타임 체크) |
| **코드량** | 약 40줄 | 약 15줄 |
| **일관성** | 개발자마다 다른 이름 가능 | 표준 에러 타입으로 강제 |

### DomainErrorKind 선택 가이드

검증 조건에 따라 적절한 DomainErrorKind을 선택합니다.

| 검증 조건 | DomainErrorKind | 생성되는 에러 코드 |
|-----------|-----------------|-------------------|
| 빈 값 | `new DomainErrorKind.Empty()` | `Domain.{Type}.Empty` |
| null 값 | `new DomainErrorKind.Null()` | `Domain.{Type}.Null` |
| 최소 길이 미달 | `new DomainErrorKind.TooShort(8)` | `Domain.{Type}.TooShort` |
| 정확한 길이 불일치 | `new DomainErrorKind.WrongLength(5)` | `Domain.{Type}.WrongLength` |
| 형식 오류 | `new DomainErrorKind.InvalidFormat()` | `Domain.{Type}.InvalidFormat` |
| 음수 값 | `new DomainErrorKind.Negative()` | `Domain.{Type}.Negative` |
| 범위 초과 | `new DomainErrorKind.OutOfRange("0", "100")` | `Domain.{Type}.OutOfRange` |
| 찾을 수 없음 | `new DomainErrorKind.NotFound()` | `Domain.{Type}.NotFound` |
| 도메인 특화 | `new Zero()` (`sealed record Zero : DomainErrorKind.Custom;`) | `Domain.{Type}.Zero` |

### 장단점 표
| 장점 | 단점 |
|------|------|
| **코드량 60% 감소** | Functorium 프레임워크 의존성 |
| **타입 안전한 에러 타입** | - |
| **인라인 에러 정의로 높은 응집도** | - |
| **자동 에러 코드 생성** | - |
| **표준화된 에러 이름** | - |

## FAQ

### Q1: 언제 Custom을 사용해야 하나요?

표준 DomainErrorKind으로 표현할 수 있으면 표준 타입을 사용하고, 도메인 특화 에러만 `sealed record`를 파생 정의합니다.

```csharp
// 표준 타입으로 표현 가능 → 표준 타입 사용
DomainError.For<Currency>(new DomainErrorKind.Empty(), value, "...");          // Empty 사용
DomainError.For<Password>(new DomainErrorKind.TooShort(8), value, "...");      // TooShort 사용

// 도메인 특화 에러 → sealed record 파생 정의 후 사용
// public sealed record Zero : DomainErrorKind.Custom;
// public sealed record Unsupported : DomainErrorKind.Custom;
// public sealed record StartAfterEnd : DomainErrorKind.Custom;
DomainError.For<Denominator, int>(new Zero(), value, "...");
DomainError.For<Currency>(new Unsupported(), value, "...");
DomainError.For<DateRange>(new StartAfterEnd(), start, "...");
```

### Q2: 어떤 DomainError.For 오버로딩을 사용해야 하나요?

검증 실패 시 저장할 값의 타입에 따라 선택합니다.

1. **문자열 값** → `DomainError.For<T>(errorType, stringValue, message)`
   ```csharp
   DomainError.For<Currency>(new DomainErrorKind.Empty(), currencyCode ?? "", "...")
   ```

2. **제네릭 값 (int, decimal 등)** → `DomainError.For<T, TValue>(errorType, value, message)`
   ```csharp
   // sealed record Zero : DomainErrorKind.Custom;
   DomainError.For<Denominator, int>(new Zero(), value, "...")
   DomainError.For<MoneyAmount, decimal>(new DomainErrorKind.OutOfRange(), amount, "...")
   ```

3. **두 개의 값** → `DomainError.For<T, T1, T2>(errorType, v1, v2, message)`
   ```csharp
   // sealed record StartAfterEnd : DomainErrorKind.Custom;
   DomainError.For<DateRange, DateTime, DateTime>(new StartAfterEnd(), start, end, "...")
   ```

### Q3: 단위 테스트에서 에러를 어떻게 검증하나요?

`Functorium.Testing.Assertions`의 타입 안전 확장 메서드를 사용합니다. `DomainError.For<T>()`로 생성한 에러를 `ShouldBeDomainError<T>()`로 검증합니다.

```csharp
// Before (문자열 기반) - 오타 가능, 리팩토링 위험
result.IsFail.ShouldBeTrue();
result.IfFail(error => error.Message.ShouldContain("Domain.Denominator.Zero"));

// After (타입 안전) - 컴파일 타임 검증, 리팩토링 안전
result.ShouldBeDomainError<Denominator, int>(new Zero());
```

---

## 타입 안전 테스트 Assertion

Functorium 프레임워크는 `DomainError` 생성 패턴과 대칭되는 타입 안전 테스트 Assertion을 제공합니다.

### 설계 원칙

에러 생성과 검증이 대칭 구조를 이루도록 설계되어 있습니다.

| 에러 생성 | 에러 검증 |
|-----------|-----------|
| `DomainError.For<T>(...)` | `ShouldBeDomainError<T>(...)` |
| `DomainError.For<T, TValue>(...)` | `ShouldBeDomainError<T, TValue>(...)` |
| `Validation<Error, T>` | `ShouldHaveDomainError<T>(...)` |

### Fin&lt;T&gt; 결과 검증

```csharp
using Functorium.Testing.Assertions;

// 1. 기본 검증 - 에러 타입만 확인
var result = Denominator.Create(0);
result.ShouldBeDomainError<Denominator, int>(new Zero());

// 2. 엄격한 검증 - 에러 타입 + 현재 값 확인
result.ShouldBeDomainError<Denominator, int, int>(
    new Zero(),
    expectedCurrentValue: 0);

// 3. 표준 에러 타입 검증
var streetResult = Street.Create("");
streetResult.ShouldBeDomainError<Street, string>(new DomainErrorKind.Empty());

var currencyResult = Currency.Create("XYZ");
currencyResult.ShouldBeDomainError<Currency, string>(new Unsupported());
```

### Validation&lt;Error, T&gt; 결과 검증

```csharp
// 1. 단일 에러 검증
Validation<Error, int> validation = Denominator.Validate(0);
validation.ShouldHaveDomainError<Denominator, int>(new Zero());

// 2. 정확히 하나의 에러만 있는지 검증
Validation<Error, string> postalValidation = PostalCode.Validate("");
postalValidation.ShouldHaveOnlyDomainError<PostalCode, string>(new DomainErrorKind.Empty());

// 3. 여러 에러 검증 (Apply 패턴 사용 시)
var combined = (validation1, validation2).Apply((a, b) => a + b).As();
combined.ShouldHaveDomainErrors<PostalCode, string>(
    new DomainErrorKind.Empty(),
    new DomainErrorKind.WrongLength(5));

// 4. 현재 값까지 검증
validation.ShouldHaveDomainError<Denominator, int, int>(
    new Zero(),
    expectedCurrentValue: 0);
```

### Assertion 메서드 선택 가이드

시나리오별로 적절한 Assertion 메서드를 선택합니다.

| 시나리오 | Assertion 메서드 |
|----------|------------------|
| Fin 실패 확인 | `fin.ShouldBeDomainError<TVO, T>(errorType)` |
| Fin 실패 + 값 확인 | `fin.ShouldBeDomainError<TVO, T, TValue>(errorType, value)` |
| Validation 에러 포함 확인 | `validation.ShouldHaveDomainError<TVO, T>(errorType)` |
| Validation 정확히 1개 에러 | `validation.ShouldHaveOnlyDomainError<TVO, T>(errorType)` |
| Validation 여러 에러 확인 | `validation.ShouldHaveDomainErrors<TVO, T>(types...)` |
| Validation 에러 + 값 확인 | `validation.ShouldHaveDomainError<TVO, T, TValue>(errorType, value)` |

에러 처리 코드가 간결해졌지만, 검증 로직 자체는 여전히 삼항 연산자나 Bind 체인으로 작성해야 합니다. 다음 장에서는 `Validate<T>` Fluent API를 도입하여 검증 흐름까지 선형적으로 개선합니다.

→ [15장: FluentValidation 검증](../15-Validation-Fluent/)
