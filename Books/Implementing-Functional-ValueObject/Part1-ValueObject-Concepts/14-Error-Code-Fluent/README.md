# DomainError 헬퍼를 사용한 간결한 에러 처리

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
- [타입 안전 테스트 Assertion](#타입-안전-테스트-assertion)

## 개요

이 프로젝트는 Functorium 프레임워크의 `DomainError` 헬퍼와 `DomainErrorType` 레코드를 활용하여 값 객체의 에러 처리 코드를 대폭 간소화하는 패턴을 시연합니다. 기존의 `DomainErrors` 중첩 클래스와 `ErrorCodeFactory.Create()` 호출을 `DomainError.For<T>()` 한 줄로 대체하여 **코드량을 약 60% 감소**시키고, **타입 안전한 에러 코드**를 보장합니다.

## 학습 목표

### **핵심 학습 목표**
1. **DomainError 헬퍼 활용**: `DomainError.For<T>()` 메서드를 사용하여 간결하게 에러를 생성할 수 있다
2. **DomainErrorType 활용**: 타입 안전한 에러 유형을 사용하여 오타와 불일치를 컴파일 타임에 방지할 수 있다
3. **보일러플레이트 제거**: `DomainErrors` 중첩 클래스 없이 인라인으로 에러를 정의할 수 있다
4. **자동 에러 코드 생성**: `DomainErrors.{ValueObjectName}.{ErrorType}` 형식의 에러 코드가 자동으로 생성됨을 이해한다

### **실습을 통해 확인할 내용**
- **에러 생성 간소화**: `DomainErrors.Zero(value)` → `DomainError.For<Denominator, int>(new Zero(), value, "message")` (커스텀 에러는 `sealed record Zero : DomainErrorType.Custom;`으로 정의)
- **표준 에러 타입 활용**: `Empty`, `TooShort`, `OutOfRange` 등 표준화된 에러 타입 사용
- **타입별 오버로딩**: 문자열용 `For<T>()`, 제네릭용 `For<T, TValue>()`, 다중 값용 `For<T, T1, T2>()`

## 왜 필요한가?

이전 `13-Error-Code` 프로젝트에서는 구조화된 에러 코드 시스템을 도입했지만, 각 값 객체마다 `DomainErrors` 중첩 클래스를 정의해야 하는 보일러플레이트 코드가 발생했습니다.

**첫 번째 문제는 반복적인 에러 클래스 정의입니다.** 모든 값 객체에서 동일한 패턴의 `internal static class DomainErrors`를 반복 정의해야 했습니다. 이는 코드 중복을 야기하고 유지보수 비용을 증가시킵니다.

**두 번째 문제는 에러 코드의 수동 조합입니다.** `$"{nameof(DomainErrors)}.{nameof(Denominator)}.{nameof(Zero)}"` 형태로 매번 수동으로 에러 코드를 조합해야 했습니다. 이는 실수의 여지가 있고 일관성을 해칩니다.

**세 번째 문제는 문자열 기반 에러 이름입니다.** `"Empty"`, `"IsEmpty"`, `"EmptyValue"` 처럼 동일한 개념에 대해 개발자마다 다른 이름을 사용할 수 있습니다. 이는 에러 코드의 일관성을 해칩니다.

이러한 문제들을 해결하기 위해 **DomainError 헬퍼**와 **DomainErrorType 레코드**를 도입했습니다. 타입 안전한 에러 유형과 자동 에러 코드 생성으로 코드량이 대폭 감소하고 일관성이 보장됩니다.

## 핵심 개념

이 프로젝트의 핵심은 크게 3가지 개념으로 나눌 수 있습니다.

### 첫 번째 개념: DomainErrorType 레코드

`DomainErrorType`은 표준화된 에러 유형을 정의하는 추상 레코드입니다. 문자열 대신 타입을 사용하여 컴파일 타임 안전성을 보장합니다.

**핵심 아이디어는 "표준 에러는 enum처럼, 커스텀 에러는 명시적으로"입니다.**

```csharp
// 표준 에러 타입들 (타입 안전)
new DomainErrorType.Empty()           // 빈 값
new DomainErrorType.Null()            // null 값
new DomainErrorType.TooShort(8)       // 최소 길이 미달
new DomainErrorType.TooLong(100)      // 최대 길이 초과
new DomainErrorType.WrongLength(5)    // 정확한 길이 불일치
new DomainErrorType.InvalidFormat()   // 형식 오류
new DomainErrorType.Negative()        // 음수 값
new DomainErrorType.NotPositive()     // 양수가 아닌 값
new DomainErrorType.OutOfRange("0", "1000")  // 범위 초과
new DomainErrorType.BelowMinimum("0")        // 최솟값 미달
new DomainErrorType.AboveMaximum("100")      // 최댓값 초과
new DomainErrorType.NotFound()        // 찾을 수 없음
new DomainErrorType.AlreadyExists()   // 이미 존재
new DomainErrorType.NotUpperCase()    // 대문자 아님
new DomainErrorType.NotLowerCase()    // 소문자 아님
new DomainErrorType.Duplicate()       // 중복
new DomainErrorType.Mismatch()        // 불일치

// 커스텀 에러 (비표준 케이스용) - sealed record 파생 정의
// public sealed record Unsupported : DomainErrorType.Custom;
new Unsupported()    // 도메인 특화 에러
```

### 두 번째 개념: DomainError 헬퍼

DomainError 헬퍼는 Functorium 프레임워크에서 제공하는 에러 생성 유틸리티입니다. 타입 정보와 에러 유형을 조합하여 에러 코드를 자동으로 생성합니다.

**핵심 아이디어는 "타입 기반 에러 코드 자동 생성"입니다.** `typeof(T).Name`과 `DomainErrorType`을 조합하여 일관된 에러 코드를 자동으로 생성합니다.

```csharp
// DomainError 헬퍼 사용법

// 1. 문자열 값 검증 시
DomainError.For<Currency>(new DomainErrorType.Empty(), currencyCode ?? "",
    $"Currency code cannot be empty. Current value: '{currencyCode}'")
// 생성되는 에러 코드: "DomainErrors.Currency.Empty"

// 2. 제네릭 값 검증 시 (커스텀 에러: sealed record Zero : DomainErrorType.Custom;)
DomainError.For<Denominator, int>(new Zero(), value,
    $"Denominator cannot be zero. Current value: '{value}'")
// 생성되는 에러 코드: "DomainErrors.Denominator.Zero"

// 3. 범위 검증 시
DomainError.For<Coordinate, int>(new DomainErrorType.OutOfRange("0", "1000"), x,
    $"X coordinate must be between 0 and 1000. Current value: '{x}'")
// 생성되는 에러 코드: "DomainErrors.Coordinate.OutOfRange"
```

### 세 번째 개념: 인라인 에러 정의

DomainError 헬퍼를 사용하면 검증 로직과 에러 정의를 한 곳에 작성할 수 있습니다. 별도의 `DomainErrors` 중첩 클래스가 불필요합니다.

**핵심 아이디어는 "검증과 에러의 동시 정의"입니다.** 검증 실패 시점에서 바로 에러를 생성하여 코드 응집도를 높입니다.

```csharp
// 인라인 에러 정의 예시 (커스텀 에러: sealed record Zero : DomainErrorType.Custom;)
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
            return DomainErrors.Zero(value);
        return value;
    }

    // DomainErrors 중첩 클래스 - 모든 값 객체에서 반복됨
    internal static class DomainErrors
    {
        public static Error Zero(int value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Denominator)}.{nameof(Zero)}",
                errorCurrentValue: value,
                errorMessage: $"Denominator cannot be zero. Current value: '{value}'");
    }
}
```

### After (DomainError + DomainErrorType - 15줄)
```csharp
public sealed class Denominator : ComparableSimpleValueObject<int>
{
    private Denominator(int value) : base(value) { }

    public static Fin<Denominator> Create(int value) =>
        CreateFromValidation(Validate(value), validValue => new Denominator(validValue));

    public static Denominator CreateFromValidated(int validatedValue) =>
        new Denominator(validatedValue);

    // 커스텀 에러 타입 정의
    public sealed record Zero : DomainErrorType.Custom;

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
빈 통화 코드: [DomainErrors.Currency.Empty] Currency code cannot be empty. Current value: ''
3자리가 아닌 형식: [DomainErrors.Currency.WrongLength] Currency code must be exactly 3 letters. Current value: 'AB'
지원하지 않는 통화: [DomainErrors.Currency.Unsupported] Currency code is not supported. Current value: 'XYZ'

  --- Price 에러 테스트 ---
음수 가격: [DomainErrors.MoneyAmount.OutOfRange] Money amount must be between 0 and 999999.99. Current value: '-100'

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
빈 거리명: [DomainErrors.Street.Empty] Street name cannot be empty.
빈 도시명: [DomainErrors.City.Empty] City name cannot be empty.
잘못된 우편번호: [DomainErrors.PostalCode.WrongLength] Postal code must be exactly 5 digits.

  --- Street 에러 테스트 ---
빈 거리명: [DomainErrors.Street.Empty] Street name cannot be empty.

  --- City 에러 테스트 ---
빈 도시명: [DomainErrors.City.Empty] City name cannot be empty.

  --- PostalCode 에러 테스트 ---
빈 우편번호: [DomainErrors.PostalCode.Empty] Postal code cannot be empty.
5자리 숫자가 아닌 형식: [DomainErrors.PostalCode.WrongLength] Postal code must be exactly 5 digits.

--- PrimitiveValueObjects 하위 폴더 ---
  === PrimitiveValueObjects 에러 테스트 ===

  --- BinaryData 에러 테스트 ---
null 바이너리 데이터: [DomainErrors.BinaryData.Empty] Binary data cannot be empty.
빈 바이너리 데이터: [DomainErrors.BinaryData.Empty] Binary data cannot be empty.

--- CompositePrimitiveValueObjects 하위 폴더 ---
  === CompositePrimitiveValueObjects 에러 테스트 ===

  --- Coordinate 에러 테스트 ---
범위를 벗어난 X 좌표: [DomainErrors.Coordinate.OutOfRange] X coordinate must be between 0 and 1000.
범위를 벗어난 Y 좌표: [DomainErrors.Coordinate.OutOfRange] Y coordinate must be between 0 and 1000.
```

### 핵심 구현 포인트
1. **DomainErrorType 활용**: 표준 에러는 `new DomainErrorType.Empty()` 등 미리 정의된 타입 사용
2. **DomainError.For<T>() 활용**: 타입 정보에서 에러 코드 자동 생성
3. **인라인 에러 정의**: 검증 로직 내에서 직접 에러 정의
4. **Custom 에러 타입**: 표준 에러로 표현하기 어려운 도메인 특화 에러는 `sealed record` 파생 정의 후 사용
5. **LanguageExt 완전 호환**: 기존 `Validation<Error, T>`, `Fin<T>` 타입과 완벽 호환

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
    └── ErrorCodeFactoryTests.cs           # DomainError + Assertion 종합 테스트
```

### 핵심 코드

#### DomainErrorType (Functorium 프레임워크 제공)
```csharp
/// <summary>
/// 도메인 에러 유형을 정의하는 추상 레코드
/// 타입 안전한 에러 코드 생성을 위해 사용
/// </summary>
public abstract record DomainErrorType
{
    // 값 존재 검증
    public sealed record Empty : DomainErrorType;
    public sealed record Null : DomainErrorType;

    // 문자열 길이 검증
    public sealed record TooShort(int Minimum = 0) : DomainErrorType;
    public sealed record TooLong(int Maximum = 0) : DomainErrorType;
    public sealed record WrongLength(int Expected = 0) : DomainErrorType;

    // 형식 검증
    public sealed record InvalidFormat : DomainErrorType;

    // 숫자 범위 검증
    public sealed record Negative : DomainErrorType;
    public sealed record NotPositive : DomainErrorType;
    public sealed record OutOfRange(string? Minimum = null, string? Maximum = null) : DomainErrorType;
    public sealed record BelowMinimum(string? Minimum = null) : DomainErrorType;
    public sealed record AboveMaximum(string? Maximum = null) : DomainErrorType;

    // 존재 여부 검증
    public sealed record NotFound : DomainErrorType;
    public sealed record AlreadyExists : DomainErrorType;

    // 대소문자 검증
    public sealed record NotUpperCase : DomainErrorType;
    public sealed record NotLowerCase : DomainErrorType;

    // 비즈니스 규칙 검증
    public sealed record Duplicate : DomainErrorType;
    public sealed record Mismatch : DomainErrorType;

    // 커스텀 에러 (도메인 특화) - abstract record, 파생하여 사용
    public abstract record Custom : DomainErrorType;
}

// 커스텀 에러 정의 예시 (값 객체 내부에 nested record로 정의)
// public sealed record Unsupported : DomainErrorType.Custom;
// public sealed record Zero : DomainErrorType.Custom;
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
        DomainErrorType errorType, string currentValue, string message)
        where TValueObject : class =>
        ErrorCodeFactory.Create(
            errorCode: $"DomainErrors.{typeof(TValueObject).Name}.{GetErrorName(errorType)}",
            errorCurrentValue: currentValue,
            errorMessage: message);

    /// <summary>
    /// 제네릭 값에 대한 도메인 에러 생성
    /// </summary>
    public static Error For<TValueObject, TValue>(
        DomainErrorType errorType, TValue currentValue, string message)
        where TValueObject : class
        where TValue : notnull =>
        ErrorCodeFactory.Create(
            errorCode: $"DomainErrors.{typeof(TValueObject).Name}.{GetErrorName(errorType)}",
            errorCurrentValue: currentValue,
            errorMessage: message);

    private static string GetErrorName(DomainErrorType errorType) =>
        errorType.GetType().Name;
}
```

#### Denominator - 가장 간단한 예시
```csharp
public sealed class Denominator : ComparableSimpleValueObject<int>
{
    // 커스텀 에러 타입 정의
    public sealed record Zero : DomainErrorType.Custom;

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
    public sealed record Unsupported : DomainErrorType.Custom;

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
            ? DomainError.For<Currency>(new DomainErrorType.Empty(), currencyCode ?? "",
                $"Currency code cannot be empty. Current value: '{currencyCode}'")
            : currencyCode;

    private static Validation<Error, string> ValidateFormat(string currencyCode) =>
        currencyCode.Length != 3 || !currencyCode.All(char.IsLetter)
            ? DomainError.For<Currency>(new DomainErrorType.WrongLength(3), currencyCode,
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
            ? DomainError.For<Coordinate, int>(new DomainErrorType.OutOfRange("0", "1000"), x,
                $"X coordinate must be between 0 and 1000. Current value: '{x}'")
            : x;

    private static Validation<Error, int> ValidateY(int y) =>
        y < 0 || y > 1000
            ? DomainError.For<Coordinate, int>(new DomainErrorType.OutOfRange("0", "1000"), y,
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

### 비교 표
| 구분 | 이전 방식 (DomainErrors 중첩 클래스) | 현재 방식 (DomainError + DomainErrorType) |
|------|--------------------------------------|-------------------------------------------|
| **에러 정의 위치** | 별도 중첩 클래스 | 검증 로직 내 인라인 |
| **에러 코드 생성** | 수동 조합 (nameof 사용) | 자동 생성 (타입 정보 + DomainErrorType) |
| **에러 이름 안전성** | 문자열 기반 (오타 가능) | 타입 기반 (컴파일 타임 체크) |
| **코드량** | 약 40줄 | 약 15줄 |
| **유지보수** | 여러 위치에서 수정 필요 | 한 곳에서 관리 |
| **일관성** | 개발자마다 다른 이름 가능 | 표준 에러 타입으로 강제 |
| **LanguageExt 호환성** | 완전 호환 | 완전 호환 |

### DomainErrorType 선택 가이드
| 검증 조건 | DomainErrorType | 생성되는 에러 코드 |
|-----------|-----------------|-------------------|
| 빈 값 | `new DomainErrorType.Empty()` | `DomainErrors.{Type}.Empty` |
| null 값 | `new DomainErrorType.Null()` | `DomainErrors.{Type}.Null` |
| 최소 길이 미달 | `new DomainErrorType.TooShort(8)` | `DomainErrors.{Type}.TooShort` |
| 정확한 길이 불일치 | `new DomainErrorType.WrongLength(5)` | `DomainErrors.{Type}.WrongLength` |
| 형식 오류 | `new DomainErrorType.InvalidFormat()` | `DomainErrors.{Type}.InvalidFormat` |
| 음수 값 | `new DomainErrorType.Negative()` | `DomainErrors.{Type}.Negative` |
| 범위 초과 | `new DomainErrorType.OutOfRange("0", "100")` | `DomainErrors.{Type}.OutOfRange` |
| 찾을 수 없음 | `new DomainErrorType.NotFound()` | `DomainErrors.{Type}.NotFound` |
| 도메인 특화 | `new Zero()` (`sealed record Zero : DomainErrorType.Custom;`) | `DomainErrors.{Type}.Zero` |

### 장단점 표
| 장점 | 단점 |
|------|------|
| **코드량 60% 감소** | Functorium 프레임워크 의존성 |
| **타입 안전한 에러 타입** | - |
| **인라인 에러 정의** | - |
| **자동 에러 코드 생성** | - |
| **높은 코드 응집도** | - |
| **표준화된 에러 이름** | - |
| **빠른 개발 속도** | - |

### 핵심 개선사항
- **DomainErrors 중첩 클래스 제거**: 모든 에러 정의가 검증 로직 내에 인라인으로 위치
- **DomainErrorType 도입**: 타입 안전한 표준 에러 유형으로 일관성 보장
- **자동 에러 코드 생성**: `DomainErrors.{ValueObjectName}.{ErrorType}` 형식 자동 생성
- **타입 기반 오버로딩**: 문자열, 단일 제네릭, 다중 제네릭 값 지원
- **코드 응집도 향상**: 검증과 에러 정의가 같은 위치에 존재
- **개발 생산성 향상**: 새 값 객체 생성 시 에러 처리 코드 최소화

## FAQ

### Q1: DomainErrorType이 왜 필요한가요?
**A**: 기존 문자열 기반 에러 이름은 오타와 불일치 문제가 있었습니다.

문자열 기반 문제:
```csharp
// 개발자 A
DomainError.For<Email>("Empty", value, message);
// 개발자 B
DomainError.For<UserName>("IsEmpty", value, message);
// 개발자 C
DomainError.For<PostalCode>("EmptyValue", value, message);
```

DomainErrorType으로 해결:
```csharp
// 모든 개발자가 동일한 타입 사용
DomainError.For<Email>(new DomainErrorType.Empty(), value, message);
DomainError.For<UserName>(new DomainErrorType.Empty(), value, message);
DomainError.For<PostalCode>(new DomainErrorType.Empty(), value, message);
```

### Q2: 언제 Custom을 사용해야 하나요?
**A**: 표준 DomainErrorType으로 표현하기 어려운 도메인 특화 에러에 사용합니다. `sealed record`를 파생 정의하여 타입 안전하게 사용합니다.

```csharp
// 표준 타입으로 표현 가능 → 표준 타입 사용
DomainError.For<Currency>(new DomainErrorType.Empty(), value, "...");          // Empty 사용
DomainError.For<Password>(new DomainErrorType.TooShort(8), value, "...");      // TooShort 사용

// 도메인 특화 에러 → sealed record 파생 정의 후 사용
// public sealed record Zero : DomainErrorType.Custom;
// public sealed record Unsupported : DomainErrorType.Custom;
// public sealed record StartAfterEnd : DomainErrorType.Custom;
DomainError.For<Denominator, int>(new Zero(), value, "...");
DomainError.For<Currency>(new Unsupported(), value, "...");
DomainError.For<DateRange>(new StartAfterEnd(), start, "...");
```

### Q3: 언제 어떤 DomainError.For 오버로딩을 사용해야 하나요?
**A**: 검증 실패 시 저장할 값의 타입에 따라 선택합니다.

1. **문자열 값** → `DomainError.For<T>(errorType, stringValue, message)`
   ```csharp
   DomainError.For<Currency>(new DomainErrorType.Empty(), currencyCode ?? "", "...")
   ```

2. **제네릭 값 (int, decimal 등)** → `DomainError.For<T, TValue>(errorType, value, message)`
   ```csharp
   // sealed record Zero : DomainErrorType.Custom;
   DomainError.For<Denominator, int>(new Zero(), value, "...")
   DomainError.For<MoneyAmount, decimal>(new DomainErrorType.OutOfRange(), amount, "...")
   ```

3. **두 개의 값** → `DomainError.For<T, T1, T2>(errorType, v1, v2, message)`
   ```csharp
   // sealed record StartAfterEnd : DomainErrorType.Custom;
   DomainError.For<DateRange, DateTime, DateTime>(new StartAfterEnd(), start, end, "...")
   ```

### Q4: 기존 코드를 마이그레이션하는 방법은?
**A**: 3단계로 마이그레이션합니다.

1. **DomainErrors 호출을 DomainError.For로 교체**:
   ```csharp
   // Before
   return DomainErrors.Zero(value);

   // After (sealed record Zero : DomainErrorType.Custom;)
   return DomainError.For<Denominator, int>(new Zero(), value,
       $"Denominator cannot be zero. Current value: '{value}'");
   ```

2. **표준 에러 타입으로 변환 (가능한 경우)**:
   ```csharp
   // Before (문자열 기반 Custom - 구버전)
   new DomainErrorType.Custom("Empty")

   // After (표준 타입)
   new DomainErrorType.Empty()
   ```

3. **DomainErrors 중첩 클래스 삭제**: 더 이상 필요하지 않음

### Q5: 성능에 영향이 있나요?
**A**: 런타임 성능 차이는 미미합니다. `typeof(T).Name`과 `DomainErrorType` 패턴 매칭은 JIT 컴파일 시 최적화됩니다. 에러 코드 문자열이 실패 시점에만 생성되므로 성공 경로의 성능은 완전히 동일합니다.

### Q6: 단위 테스트에서 에러를 어떻게 검증하나요?
**A**: `Functorium.Testing.Assertions`의 타입 안전 확장 메서드를 사용합니다. `DomainError.For<T>()`로 생성한 에러를 `ShouldBeDomainError<T>()`로 검증하여 일관성 있는 패턴을 유지합니다.

```csharp
// Before (문자열 기반) - 오타 가능, 리팩토링 위험
result.IsFail.ShouldBeTrue();
result.IfFail(error => error.Message.ShouldContain("DomainErrors.Denominator.Zero"));

// After (타입 안전) - 컴파일 타임 검증, 리팩토링 안전
result.ShouldBeDomainError<Denominator, int>(new Zero());
```

---

## 타입 안전 테스트 Assertion

Functorium 프레임워크는 `DomainError` 생성 패턴과 대칭되는 타입 안전 테스트 Assertion을 제공합니다.

### 설계 원칙

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
streetResult.ShouldBeDomainError<Street, string>(new DomainErrorType.Empty());

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
postalValidation.ShouldHaveOnlyDomainError<PostalCode, string>(new DomainErrorType.Empty());

// 3. 여러 에러 검증 (Apply 패턴 사용 시)
var combined = (validation1, validation2).Apply((a, b) => a + b).As();
combined.ShouldHaveDomainErrors<PostalCode, string>(
    new DomainErrorType.Empty(),
    new DomainErrorType.WrongLength(5));

// 4. 현재 값까지 검증
validation.ShouldHaveDomainError<Denominator, int, int>(
    new Zero(),
    expectedCurrentValue: 0);
```

### Before/After 비교 (테스트 코드)

#### Before (문자열 기반)
```csharp
[Fact]
public void Create_ShouldFail_WhenEmpty()
{
    // Act
    var actual = Street.Create("");

    // Assert - 문자열 기반, 오타 가능
    actual.IsFail.ShouldBeTrue();
    actual.IfFail(error =>
    {
        error.Message.ShouldContain("DomainErrors.Street.Empty");
    });
}
```

#### After (타입 안전)
```csharp
[Fact]
public void Create_ShouldReturnDomainError_WhenEmpty()
{
    // Act
    var actual = Street.Create("");

    // Assert - 타입 안전, 컴파일 타임 검증
    actual.ShouldBeDomainError<Street, string>(new DomainErrorType.Empty());
}
```

### Assertion 메서드 선택 가이드

| 시나리오 | Assertion 메서드 |
|----------|------------------|
| Fin 실패 확인 | `fin.ShouldBeDomainError<TVO, T>(errorType)` |
| Fin 실패 + 값 확인 | `fin.ShouldBeDomainError<TVO, T, TValue>(errorType, value)` |
| Validation 에러 포함 확인 | `validation.ShouldHaveDomainError<TVO, T>(errorType)` |
| Validation 정확히 1개 에러 | `validation.ShouldHaveOnlyDomainError<TVO, T>(errorType)` |
| Validation 여러 에러 확인 | `validation.ShouldHaveDomainErrors<TVO, T>(types...)` |
| Validation 에러 + 값 확인 | `validation.ShouldHaveDomainError<TVO, T, TValue>(errorType, value)` |

### 타입 안전 테스트의 장점

| 장점 | 설명 |
|------|------|
| **컴파일 타임 안전성** | 값 객체 이름 변경 시 테스트 자동 갱신 |
| **IntelliSense 지원** | `DomainErrorType` 자동 완성 |
| **일관된 패턴** | 생성 (`DomainError.For<T>()`) ↔ 검증 (`ShouldBeDomainError<T>()`) |
| **간결한 코드** | 기존 4-6줄 → 1줄로 축약 |
| **오타 방지** | 문자열 대신 타입 사용 |
