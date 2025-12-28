# 에러 쳬계화하기

## 목차
- [개요](#개요)
- [학습 목표](#학습-목표)
- [왜 필요한가?](#왜-필요한가)
- [핵심 개념](#핵심-개념)
- [실전 지침](#실전-지침)
- [프로젝트 설명](#프로젝트-설명)
- [한눈에 보는 정리](#한눈에-보는-정리)
- [FAQ](#faq)

## 개요

이 프로젝트는 기존의 단순한 `Error.New` 방식을 넘어서 체계적이고 구조화된 에러 코드 처리 패턴을 구현합니다. `"DomainErrors.클래스.이유"` 형식의 에러 코드와 실패 당시의 값 정보를 포함하는 타입 안전한 에러 처리 시스템을 제공합니다.

## 학습 목표

### **핵심 학습 목표**
1. **구조화된 에러 코드 시스템 설계**: `DomainErrors.클래스.이유` 형식의 체계적인 에러 코드 구조를 이해하고 구현할 수 있다
2. **타입 안전한 에러 정보 관리**: 실패 당시의 값과 에러 코드를 함께 관리하는 타입 안전한 에러 처리 시스템을 구축할 수 있다
3. **LanguageExt 생태계 통합**: 기존 LanguageExt의 `Error` 타입과 완전히 호환되는 에러 처리 프레임워크를 설계할 수 있다

### **실습을 통해 확인할 내용**
- **기본 에러 코드 구조**: `Error.New("Invalid value")` → `ErrorCodeFactory.Create($"{nameof(DomainErrors)}.{nameof(Denominator)}.{nameof(Zero)}", 0, "Denominator cannot be zero. Current value: '0'")`
- **다양한 타입 지원**: `Create<T>`, `Create<T1, T2>`, `Create<T1, T2, T3>` 메서드를 통한 타입 안전한 에러 생성
- **ValueObject 통합**: 값 객체 내부의 `DomainErrors` 클래스를 통한 응집도 높은 에러 정의

## 왜 필요한가?

이전 단계인 `11-ValueObject-Framework`에서는 프레임워크를 통해 값 객체의 생성과 검증을 체계화했습니다. 하지만 실제 운영 환경에서 에러가 발생했을 때 디버깅과 모니터링을 위한 구조화된 정보가 부족했습니다.

**첫 번째 문제는 에러 정보의 구조화 부족입니다.** 기존의 `Error.New("Invalid denominator value: 0")` 방식은 단순한 문자열 메시지만 제공하여, 어떤 도메인에서 어떤 이유로 실패했는지 체계적으로 파악하기 어렵습니다. 이는 마치 로그 시스템에서 구조화된 로그 대신 단순한 텍스트만 남기는 것과 같습니다.

**두 번째 문제는 실패 당시 값 정보의 손실입니다.** 에러가 발생했을 때 어떤 값이 문제를 일으켰는지에 대한 정보가 메시지에 하드코딩되어 있어, 동적으로 에러 정보를 분석하거나 모니터링 시스템에서 활용하기 어렵습니다. 이는 마치 함수형 프로그래밍에서 부작용을 포함한 함수 대신 순수 함수를 사용하는 것처럼, 에러 정보도 순수하고 구조화된 형태로 관리해야 합니다.

**세 번째 문제는 에러 코드의 일관성 부족입니다.** 각 값 객체마다 다른 형식의 에러 메시지를 사용하여 전체 시스템의 에러 처리 패턴이 일관되지 않습니다. 이는 마치 아키텍처에서 계층별로 다른 인터페이스를 사용하는 것처럼, 시스템 전체의 일관성을 해치는 문제입니다.

이러한 문제들을 해결하기 위해 **구조화된 에러 코드 시스템**을 도입했습니다. 이 시스템을 사용하면 에러 발생 시점의 정확한 도메인 정보, 실패 이유, 그리고 실패한 값까지 체계적으로 관리할 수 있어 디버깅과 모니터링의 효율성이 크게 향상됩니다.

## 핵심 개념

이 프로젝트의 핵심은 크게 3가지 개념으로 나눌 수 있습니다. 각각이 어떻게 작동하는지 쉽게 설명해드리겠습니다.

### 첫 번째 개념: 구조화된 에러 코드 시스템

구조화된 에러 코드 시스템은 마치 네임스페이스 계층 구조처럼 계층적으로 에러를 분류하고 관리하는 시스템입니다. `"DomainErrors.클래스.이유"` 형식을 통해 에러의 출처와 성격을 명확히 식별할 수 있습니다.

**핵심 아이디어는 "계층적 에러 분류(Hierarchical Error Classification)"입니다.** 이는 마치 파일 시스템의 디렉토리 구조처럼, 에러를 도메인별, 클래스별, 이유별로 체계적으로 분류하여 관리하는 것입니다.

예를 들어, `DomainErrors.Denominator.Invalid`라는 에러 코드를 생각해보세요. 이는 마치 `System.IO.FileNotFoundException`처럼 계층적으로 구조화된 예외 타입과 같은 원리입니다. 첫 번째 부분은 도메인 영역을, 두 번째 부분은 구체적인 클래스를, 세 번째 부분은 실패 이유를 나타냅니다.

```csharp
// 이전 방식 (구조화되지 않은 방식) - 디버깅과 모니터링이 어려움
var error = Error.New("Invalid denominator value: 0");

// 개선된 방식 (구조화된 방식) - 체계적인 에러 관리
var error = ErrorCodeFactory.Create(
    errorCode: $"{nameof(DomainErrors)}.{nameof(Denominator)}.{nameof(Zero)}",
    errorCurrentValue: 0,
    errorMessage: $"Denominator cannot be zero. Current value: '0'");
```

이 방식의 장점은 에러 발생 시점에서 정확한 도메인 정보와 실패 이유를 즉시 파악할 수 있고, 모니터링 시스템에서 에러를 도메인별로 집계하고 분석할 수 있다는 점입니다.

### 두 번째 개념: 타입 안전한 에러 정보 관리

타입 안전한 에러 정보 관리는 마치 제네릭을 활용한 타입 안전성 보장처럼, 에러와 함께 실패한 값의 타입 정보를 보존하는 시스템입니다. 이를 통해 컴파일 타임에 타입 안전성을 보장하고 런타임에 정확한 값 정보를 활용할 수 있습니다.

**핵심 아이디어는 "제네릭 기반 에러 래핑(Generic-based Error Wrapping)"입니다.** 이는 마치 `Option<T>`나 `Fin<T>`처럼 제네릭을 활용하여 타입 정보를 보존하는 함수형 프로그래밍 패턴과 같은 원리입니다.

```csharp
// 다양한 타입의 에러 정보를 타입 안전하게 관리
var stringError = ErrorCodeFactory.Create(
    errorCode: $"{nameof(DomainErrors)}.{nameof(Name)}.{nameof(TooShort)}",
    errorCurrentValue: "i@name",
    errorMessage: $"Name is too short. Current value: 'i@name'");
var intError = ErrorCodeFactory.Create(
    errorCode: $"{nameof(DomainErrors)}.{nameof(Age)}.{nameof(Invalid)}",
    errorCurrentValue: 150,
    errorMessage: $"Age is out of range. Current value: '150'");
var multiValueError = ErrorCodeFactory.Create(
    errorCode: $"{nameof(DomainErrors)}.{nameof(Coordinate)}.{nameof(OutOfRange)}",
    errorCurrentValue1: 1500,
    errorCurrentValue2: 2000,
    errorMessage: $"Coordinate is out of range. Current values: '1500', '2000'");
```

이 방식의 장점은 실패한 값의 타입 정보가 보존되어 디버깅 시 정확한 값 정보를 확인할 수 있고, 타입별로 다른 처리 로직을 적용할 수 있다는 점입니다.

### 세 번째 개념: 내부 DomainErrors 클래스 패턴

내부 DomainErrors 클래스 패턴은 마치 캡슐화를 통한 응집도 향상처럼, 값 객체와 관련된 에러 정의를 같은 파일 내에 위치시켜 높은 응집도를 달성하는 패턴입니다.

**핵심 아이디어는 "응집도 기반 에러 정의(Cohesion-based Error Definition)"입니다.** 이는 마치 클래스의 private 멤버를 같은 클래스 내에 정의하는 것처럼, 관련된 에러 정의를 값 객체와 함께 관리하는 것입니다.

```csharp
public sealed class Denominator : SimpleValueObject<int>
{
    // ... 기존 코드 ...

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

이 방식의 장점은 값 객체와 관련된 에러 정의가 한 곳에 모여 있어 유지보수가 용이하고, 새로운 값 객체를 생성할 때 에러 정의도 함께 작성하는 개발 생산성이 향상된다는 점입니다.

## 실전 지침

### 예상 출력
```
=== 체계적인 에러 처리 패턴 ===

=== Comparable 테스트 ===

--- CompositeValueObjects 하위 폴더 ---
  === CompositeValueObjects 에러 테스트 ===

  --- Currency 에러 테스트 ---
빈 통화 코드: ErrorCode: DomainErrors.Currency.Empty, ErrorCurrentValue: 
3자리가 아닌 형식: ErrorCode: DomainErrors.Currency.NotThreeLetters, ErrorCurrentValue: AB
지원하지 않는 통화: ErrorCode: DomainErrors.Currency.Unsupported, ErrorCurrentValue: XYZ

  --- Price 에러 테스트 ---
음수 가격: ErrorCode: DomainErrors.MoneyAmount.OutOfRange, ErrorCurrentValue: -100

  --- PriceRange 에러 테스트 ---
최솟값이 최댓값을 초과하는 가격 범위: ErrorCode: DomainErrors.PriceRange.MinExceedsMax, ErrorCurrentValue: MinPrice: KRW (한국 원화) ₩ 1,000.00, MaxPrice: KRW (한국 원화) ₩ 500.00

--- PrimitiveValueObjects 하위 폴더 ---
  === PrimitiveValueObjects 에러 테스트 ===

  --- Denominator 에러 테스트 ---
0 값: ErrorCode: DomainErrors.Denominator.Zero, ErrorCurrentValue: 0

--- CompositePrimitiveValueObjects 하위 폴더 ---
  === CompositePrimitiveValueObjects 에러 테스트 ===

  --- DateRange 에러 테스트 ---
시작일이 종료일 이후인 날짜 범위: ErrorCode: DomainErrors.DateRange.StartAfterEnd, ErrorCurrentValue: StartDate: 2024-12-31 오전 12:00:00, EndDate: 2024-01-01 오전 12:00:00

=== ComparableNot 폴더 테스트 ===

--- CompositeValueObjects 하위 폴더 ---
  === CompositeValueObjects 에러 테스트 ===

  --- Address 에러 테스트 ---
빈 거리명: ErrorCode: DomainErrors.Street.Empty, ErrorCurrentValue:
빈 도시명: ErrorCode: DomainErrors.City.Empty, ErrorCurrentValue:
잘못된 우편번호: ErrorCode: DomainErrors.PostalCode.NotFiveDigits, ErrorCurrentValue: 1234

  --- Street 에러 테스트 ---
빈 거리명: ErrorCode: DomainErrors.Street.Empty, ErrorCurrentValue:

  --- City 에러 테스트 ---
빈 도시명: ErrorCode: DomainErrors.City.Empty, ErrorCurrentValue:

  --- PostalCode 에러 테스트 ---
빈 우편번호: ErrorCode: DomainErrors.PostalCode.Empty, ErrorCurrentValue: 
5자리 숫자가 아닌 형식: ErrorCode: DomainErrors.PostalCode.NotFiveDigits, ErrorCurrentValue: 1234

--- PrimitiveValueObjects 하위 폴더 ---
  === PrimitiveValueObjects 에러 테스트 ===

  --- BinaryData 에러 테스트 ---
null 바이너리 데이터: ErrorCode: DomainErrors.BinaryData.Empty, ErrorCurrentValue: null
빈 바이너리 데이터: ErrorCode: DomainErrors.BinaryData.Empty, ErrorCurrentValue: 0

--- CompositePrimitiveValueObjects 하위 폴더 ---
  === CompositePrimitiveValueObjects 에러 테스트 ===

  --- Coordinate 에러 테스트 ---
범위를 벗어난 X 좌표: ErrorCode: DomainErrors.Coordinate.XOutOfRange, ErrorCurrentValue: -1
범위를 벗어난 Y 좌표: ErrorCode: DomainErrors.Coordinate.YOutOfRange, ErrorCurrentValue: 1001

=== ComparableNot 폴더 테스트 ===

--- CompositeValueObjects 하위 폴더 ---
  === CompositeValueObjects 에러 테스트 ===

  --- Address 에러 테스트 ---
빈 거리명: ErrorCode: DomainErrors.Street.Empty, ErrorCurrentValue: 
빈 도시명: ErrorCode: DomainErrors.City.Empty, ErrorCurrentValue: 
5자리 숫자가 아닌 우편번호: ErrorCode: DomainErrors.PostalCode.NotFiveDigits, ErrorCurrentValue: 1234

  --- Street 에러 테스트 ---
빈 거리명: ErrorCode: DomainErrors.Street.Empty, ErrorCurrentValue: 

  --- City 에러 테스트 ---
빈 도시명: ErrorCode: DomainErrors.City.Empty, ErrorCurrentValue: 

  --- PostalCode 에러 테스트 ---
빈 우편번호: ErrorCode: DomainErrors.PostalCode.Empty, ErrorCurrentValue: 
5자리 숫자가 아닌 형식: ErrorCode: DomainErrors.PostalCode.NotFiveDigits, ErrorCurrentValue: 1234

--- PrimitiveValueObjects 하위 폴더 ---
  === PrimitiveValueObjects 에러 테스트 ===

  --- BinaryData 에러 테스트 ---
null 바이너리 데이터: ErrorCode: DomainErrors.BinaryData.Empty, ErrorCurrentValue: null
빈 바이너리 데이터: ErrorCode: DomainErrors.BinaryData.Empty, ErrorCurrentValue: 0

--- CompositePrimitiveValueObjects 하위 폴더 ---
  === CompositePrimitiveValueObjects 에러 테스트 ===

  --- Coordinate 에러 테스트 ---
범위를 벗어난 X 좌표: ErrorCode: DomainErrors.Coordinate.XOutOfRange, ErrorCurrentValue: -1
범위를 벗어난 Y 좌표: ErrorCode: DomainErrors.Coordinate.YOutOfRange, ErrorCurrentValue: 1001
```

### 핵심 구현 포인트
1. **ErrorCodeFactory의 제네릭 오버로딩**: `Create<T>`, `Create<T1, T2>`, `Create<T1, T2, T3>` 메서드를 통해 다양한 타입의 에러 정보를 타입 안전하게 관리
2. **내부 DomainErrors 클래스 패턴**: 값 객체 내부에 `internal static class DomainErrors`를 정의하여 응집도 높은 에러 관리
3. **구체적인 에러 이유 명명**: `Empty`, `NotThreeDigits`, `NotFiveDigits`, `MinExceedsMax`, `StartAfterEnd`, `XOutOfRange`, `YOutOfRange` 등 검증 조건과 정확히 일치하는 구체적인 명명 규칙 적용
4. **LanguageExt 호환성**: 기존 `Error` 타입을 상속받아 LanguageExt 생태계와 완전한 호환성 보장
5. **ValueObjects 폴더 구조 기반 테스트**: Comparable/ComparableNot 폴더 구조와 일치하는 체계적인 테스트 구성
6. **InternalsVisibleTo 활용**: `PrintError` 메서드에서 패턴 매칭을 통한 타입 안전한 에러 정보 추출

## 프로젝트 설명

### 프로젝트 구조
```
ErrorCode/                                  # 메인 프로젝트
├── Program.cs                              # 메인 실행 파일 (ValueObjects 폴더 구조와 일치하는 테스트)
├── ErrorCode.csproj                        # 프로젝트 파일
├── Framework/                              # 에러 처리 프레임워크
│   ├── Abstractions/
│   │   └── Errors/
│   │       ├── ErrorCodeFactory.cs         # 에러 생성 팩토리
│   │       ├── ErrorCodeExpected.cs        # 구조화된 에러 타입들
│   │       └── ErrorCodeExceptional.cs     # 예외 기반 에러
│   └── Layers/
│       └── Domains/
│           ├── ValueObject.cs              # 기본 값 객체 클래스
│           ├── SimpleValueObject.cs        # 단일 값 객체 클래스
│           └── AbstractValueObject.cs      # 추상 값 객체 클래스
└── ValueObjects/                           # 값 객체 구현 (폴더 구조별 분류)
    ├── Comparable/                         # 비교 가능한 값 객체들
    │   ├── CompositeValueObjects/
    │   │   ├── Currency.cs                 # 통화 값 객체 (SmartEnum 기반)
    │   │   ├── MoneyAmount.cs              # 금액 값 객체 (ComparableSimpleValueObject<decimal>)
    │   │   ├── Price.cs                    # 가격 값 객체 (MoneyAmount + Currency 조합)
    │   │   └── PriceRange.cs               # 가격 범위 값 객체 (Price 조합)
    │   ├── PrimitiveValueObjects/
    │   │   └── Denominator.cs              # 분모 값 객체
    │   └── CompositePrimitiveValueObjects/
    │       └── DateRange.cs                # 날짜 범위 값 객체
    └── ComparableNot/                      # 비교 불가능한 값 객체들
        ├── CompositeValueObjects/
        │   ├── Address.cs                  # 주소 값 객체
        │   ├── Street.cs                   # 거리명 값 객체
        │   ├── City.cs                     # 도시명 값 객체
        │   └── PostalCode.cs               # 우편번호 값 객체
        ├── PrimitiveValueObjects/
        │   └── BinaryData.cs               # 바이너리 데이터 값 객체
        └── CompositePrimitiveValueObjects/
            └── Coordinate.cs               # 좌표 값 객체
```

### 핵심 코드

#### ErrorCodeFactory - 에러 생성 팩토리
```csharp
public static class ErrorCodeFactory
{
    // 기본 에러 생성
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error Create(string errorCode, string errorCurrentValue, string errorMessage) =>
        new ErrorCodeExpected(errorCode, errorCurrentValue, errorMessage);

    // 제네릭 단일 값 에러 생성
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error Create<T>(string errorCode, T errorCurrentValue, string errorMessage) where T : notnull =>
        new ErrorCodeExpected<T>(errorCode, errorCurrentValue, errorMessage);

    // 제네릭 다중 값 에러 생성
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error Create<T1, T2>(string errorCode, T1 errorCurrentValue1, T2 errorCurrentValue2, string errorMessage)
        where T1 : notnull where T2 : notnull =>
        new ErrorCodeExpected<T1, T2>(errorCode, errorCurrentValue1, errorCurrentValue2, errorMessage);

    // 예외 기반 에러 생성
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error CreateFromException(string errorCode, Exception exception) =>
        new ErrorCodeExceptional(errorCode, exception);

    // 에러 코드 포맷팅
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Format(params string[] parts) =>
        string.Join('.', parts);
}
```

#### Denominator - 내부 DomainErrors 패턴 적용
```csharp
public sealed class Denominator : SimpleValueObject<int>, IComparable<Denominator>
{
    // ... 기존 구현 ...

    public static Validation<Error, int> Validate(int value)
    {
        if (value == 0)
            return DomainErrors.Zero(value);

        return value;
    }

    // 내부 DomainErrors 클래스 - 응집도 높은 에러 정의
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

#### Currency - SmartEnum 기반 통화 값 객체
```csharp
public sealed class Currency : SmartEnum<Currency, string>, IValueObject
{
    public static readonly Currency KRW = new(nameof(KRW), "KRW", "한국 원화", "₩");
    public static readonly Currency USD = new(nameof(USD), "USD", "미국 달러", "$");
    // ... 기타 통화들 ...

    public static Validation<Error, string> Validate(string currencyCode) =>
        ValidateNotEmpty(currencyCode)
            .Bind(ValidateFormat)
            .Bind(ValidateSupported);

    private static Validation<Error, string> ValidateNotEmpty(string currencyCode) =>
        string.IsNullOrWhiteSpace(currencyCode)
            ? DomainErrors.Empty(currencyCode)
            : currencyCode;

    private static Validation<Error, string> ValidateFormat(string currencyCode) =>
        currencyCode.Length != 3 || !currencyCode.All(char.IsLetter)
            ? DomainErrors.NotThreeLetters(currencyCode)
            : currencyCode.ToUpperInvariant();

    // 내부 DomainErrors 클래스 - SmartEnum 특화 에러 정의
    internal static class DomainErrors
    {
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Currency)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: $"Currency code cannot be empty. Current value: '{value}'");

        public static Error NotThreeLetters(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Currency)}.{nameof(NotThreeLetters)}",
                errorCurrentValue: value,
                errorMessage: $"Currency code must be exactly 3 letters. Current value: '{value}'");

        public static Error Unsupported(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Currency)}.{nameof(Unsupported)}",
                errorCurrentValue: value,
                errorMessage: $"Currency code is not supported. Current value: '{value}'");
    }
}
```

#### MoneyAmount - 금액 값 객체
```csharp
public sealed class MoneyAmount : ComparableSimpleValueObject<decimal>
{
    private MoneyAmount(decimal value) : base(value) { }

    public static Fin<MoneyAmount> Create(decimal value) =>
        CreateFromValidation(
            Validate(value),
            validValue => new MoneyAmount(validValue));

    public static Validation<Error, decimal> Validate(decimal value) =>
        value >= 0 && value <= 999999.99m
            ? value
            : DomainErrors.OutOfRange(value);

    // 내부 DomainErrors 클래스 - 금액 범위 검증 에러
    internal static class DomainErrors
    {
        public static Error OutOfRange(decimal value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(MoneyAmount)}.{nameof(OutOfRange)}",
                errorCurrentValue: value,
                errorMessage: $"Money amount must be between 0 and 999999.99. Current value: '{value}'");
    }
}
```

#### Price - 복합 가격 값 객체
```csharp
public sealed class Price : ComparableValueObject
{
    public MoneyAmount Amount { get; }
    public Currency Currency { get; }

    private Price(MoneyAmount amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Fin<Price> Create(decimal amount, string currency) =>
        CreateFromValidation(
            Validate(amount, currency),
            validValues => new Price(validValues.Amount, validValues.Currency));

    public static Validation<Error, (MoneyAmount Amount, Currency Currency)> Validate(decimal amount, string currency) =>
        from validAmount in MoneyAmount.Validate(amount)
        from validCurrency in Currency.Validate(currency)
        select (Amount: MoneyAmount.CreateFromValidated(validAmount), 
                Currency: Currency.CreateFromValidated(validCurrency));

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return Currency.Value;    // 통화를 먼저 비교
        yield return (decimal)Amount;   // 금액을 나중에 비교
    }
}
```

#### PriceRange - 가격 범위 값 객체
```csharp
public sealed class PriceRange : ComparableValueObject
{
    public Price MinPrice { get; }
    public Price MaxPrice { get; }

    public static Fin<PriceRange> Create(decimal minPriceValue, decimal maxPriceValue, string currencyCode) =>
        CreateFromValidation(
            Validate(minPriceValue, maxPriceValue, currencyCode),
            validValues => new PriceRange(validValues.MinPrice, validValues.MaxPrice));

    public static Validation<Error, (Price MinPrice, Price MaxPrice)> Validate(
        decimal minPriceValue, decimal maxPriceValue, string currencyCode) =>
        from validMinPriceTuple in Price.Validate(minPriceValue, currencyCode)
        from validMaxPriceTuple in Price.Validate(maxPriceValue, currencyCode)
        from validPriceRange in ValidatePriceRange(
            Price.CreateFromValidated(validMinPriceTuple),
            Price.CreateFromValidated(validMaxPriceTuple))
        select validPriceRange;

    private static Validation<Error, (Price MinPrice, Price MaxPrice)> ValidatePriceRange(Price minPrice, Price maxPrice) =>
        (decimal)minPrice.Amount > (decimal)maxPrice.Amount
            ? DomainErrors.MinExceedsMax(minPrice, maxPrice)
            : (MinPrice: minPrice, MaxPrice: maxPrice);

    // 내부 DomainErrors 클래스 - 가격 범위 검증 에러
    internal static class DomainErrors
    {
        public static Error MinExceedsMax(Price minPrice, Price maxPrice) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(PriceRange)}.{nameof(MinExceedsMax)}",
                errorCurrentValue: $"MinPrice: {minPrice}, MaxPrice: {maxPrice}",
                errorMessage: $"Minimum price cannot exceed maximum price. Min: '{minPrice}', Max: '{maxPrice}'");
    }
}
```

#### Program.cs - 에러 처리 시연
```csharp
class Program
{
    static void Main()
    {
        Console.WriteLine("=== 체계적인 에러 처리 패턴 ===\n");

        Console.WriteLine("=== Comparable 테스트 ===");
        
        // 1. CompositeValueObjects 하위 폴더 테스트
        Console.WriteLine("\n--- CompositeValueObjects 하위 폴더 ---");
        DemonstrateComparableCompositeValueObjects();
        
        // 2. PrimitiveValueObjects 하위 폴더 테스트
        Console.WriteLine("\n--- PrimitiveValueObjects 하위 폴더 ---");
        DemonstrateComparablePrimitiveValueObjects();
        
        // 3. CompositePrimitiveValueObjects 하위 폴더 테스트
        Console.WriteLine("\n--- CompositePrimitiveValueObjects 하위 폴더 ---");
        DemonstrateComparableCompositePrimitiveValueObjects();
        
        Console.WriteLine("\n=== ComparableNot 폴더 테스트 ===");
        
        // 4. ComparableNot 폴더 테스트
        DemonstrateComparableNotCompositeValueObjects();
        DemonstrateComparableNotPrimitiveValueObjects();
        DemonstrateComparableNotCompositePrimitiveValueObjects();
    }

    static string PrintError(Error error)
    {
        // InternalsVisibleTo를 통해 ErrorCodeExpected 클래스에 직접 접근
        return error switch
        {
            ErrorCodeExpected<string> errorCodeExpectedString => 
                $"ErrorCode: {errorCodeExpectedString.ErrorCode}, ErrorCurrentValue: {errorCodeExpectedString.ErrorCurrentValue}",
            
            ErrorCodeExpected<int> errorCodeExpectedInt => 
                $"ErrorCode: {errorCodeExpectedInt.ErrorCode}, ErrorCurrentValue: {errorCodeExpectedInt.ErrorCurrentValue}",
            
            ErrorCodeExpected<Price, Price> errorCodeExpectedPriceRange => 
                $"ErrorCode: {errorCodeExpectedPriceRange.ErrorCode}, ErrorCurrentValue: MinPrice: {errorCodeExpectedPriceRange.ErrorCurrentValue1}, MaxPrice: {errorCodeExpectedPriceRange.ErrorCurrentValue2}",
            
            _ => $"Message: {error.Message}"
        };
    }
}
```

## 한눈에 보는 정리

### 비교 표
| 구분 | 이전 방식 (Error.New) | 현재 방식 (ErrorCodeFactory) |
|------|----------------------|------------------------------|
| **에러 코드 구조** | 단순한 문자열 메시지 | `DomainErrors.클래스.이유` 형식의 구조화된 코드 |
| **값 정보 관리** | 메시지에 하드코딩 | 타입 안전한 별도 필드로 관리 |
| **디버깅 지원** | 제한적 (메시지 파싱 필요) | 체계적 (구조화된 정보 제공) |
| **모니터링 지원** | 어려움 (일관성 부족) | 용이함 (표준화된 형식) |
| **타입 안전성** | 없음 | 제네릭을 통한 타입 안전성 보장 |
| **확장성** | 제한적 | 다양한 타입과 개수의 값 지원 |

### 장단점 표
| 장점 | 단점 |
|------|------|
| **구조화된 에러 관리** | **초기 설정 복잡성** |
| **타입 안전한 에러 정보** | **메모리 사용량 증가** |
| **LanguageExt 완전 호환** | **학습 곡선 존재** |
| **디버깅 및 모니터링 향상** | **코드 볼륨 증가** |
| **응집도 높은 에러 정의** | **프레임워크 의존성** |

### 핵심 개선사항
- **체계적인 에러 코드 구조**: `"DomainErrors.Entity.ErrorReason"` 형식으로 에러 분류
- **타입 안전한 에러 정보**: 실패 당시 값과 설명을 타입 안전하게 관리
- **LanguageExt 완전 호환**: 기존 생태계와 무결합으로 동작
- **내부 DomainErrors 클래스 방식**: 응집도와 개발 생산성 향상
- **구체적인 에러 명명**: 검증 조건과 정확히 일치하는 구체적인 에러 이름으로 디버깅 효율성 극대화
- **디버깅 및 로깅 향상**: 구조화된 에러 정보로 문제 해결 시간 단축
- **폴더 구조 기반 테스트**: ValueObjects 폴더 구조와 일치하는 체계적인 테스트 구성
- **InternalsVisibleTo 패턴 매칭**: 타입 안전한 에러 정보 추출로 성능과 안정성 향상

## FAQ

### Q1: 기존 Error.New 방식과 비교했을 때 어떤 장점이 있나요?
**A**: 구조화된 에러 코드 시스템의 가장 큰 장점은 디버깅과 모니터링의 효율성 향상입니다.

기존의 `Error.New("Invalid denominator value: 0")` 방식은 단순한 문자열 메시지만 제공하여, 어떤 도메인에서 어떤 이유로 실패했는지 체계적으로 파악하기 어렵습니다. 에러 정보를 순수하고 구조화된 형태로 관리해야 합니다.

반면 새로운 `ErrorCodeFactory.Create($"{nameof(DomainErrors)}.{nameof(Denominator)}.{nameof(Zero)}", 0, "Denominator cannot be zero. Current value: '0'")` 방식은 에러의 출처, 성격, 실패한 값, 그리고 에러 메시지를 체계적으로 분리하여 관리합니다. 이는 마치 네임스페이스 계층 구조처럼 계층적으로 에러를 분류하고 관리하는 시스템으로, 모니터링 시스템에서 에러를 도메인별로 집계하고 분석할 수 있습니다.

**실제 예시:**
```csharp
// 기존 방식 - 구조화되지 않은 정보
var oldError = Error.New("Invalid denominator value: 0");
// 디버깅 시: 메시지를 파싱해야 함, 모니터링 시: 일관성 없는 형식

// 새로운 방식 - 구조화된 정보
var newError = ErrorCodeFactory.Create(
    errorCode: $"{nameof(DomainErrors)}.{nameof(Denominator)}.{nameof(Zero)}",
    errorCurrentValue: 0,
    errorMessage: $"Denominator cannot be zero. Current value: '0'");
// 디버깅 시: 즉시 도메인과 이유 파악 가능, 모니터링 시: 표준화된 형식으로 집계 가능
```

### Q2: 내부 DomainErrors 클래스를 사용하는 이유는 무엇인가요?
**A**: 내부 DomainErrors 클래스 패턴은 응집도 기반 에러 정의를 통해 코드의 유지보수성과 개발 생산성을 크게 향상시킵니다.

값 객체와 관련된 에러 정의를 같은 파일 내에 위치시킴으로써, 관련된 코드가 한 곳에 모여 있어 유지보수가 용이해집니다. 이는 마치 클래스의 private 멤버를 같은 클래스 내에 정의하는 것처럼, 관련된 에러 정의를 값 객체와 함께 관리하는 것입니다.

또한 새로운 값 객체를 생성할 때 에러 정의도 함께 작성하는 개발 생산성이 향상됩니다. 값 객체 생성과 에러 정의를 하나의 패턴으로 묶어서 관리하는 것입니다.

이러한 접근은 코드의 응집도를 높이고 결합도를 낮추어 전체 시스템의 안정성과 확장성을 크게 향상시킵니다. 각 값 객체가 자신의 에러 정의를 내부적으로 관리함으로써, 외부에서의 의존성을 최소화하고 독립적인 모듈로 동작할 수 있습니다.

**실제 예시:**
```csharp
// 응집도 높은 에러 정의 - Denominator.cs 파일 내부
public sealed class Denominator : SimpleValueObject<int>
{
    // ... 값 객체 구현 ...

    internal static class DomainErrors
    {
        public static Error Zero(int value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Denominator)}.{nameof(Zero)}",
                errorCurrentValue: value,
                errorMessage: $"Denominator cannot be zero. Current value: '{value}'");
    }
}

// 사용 시 - 같은 파일 내에서 자연스럽게 접근
public static Validation<Error, int> Validate(int value)
{
    if (value == 0)
        return DomainErrors.Zero(value);  // 내부 클래스로 응집도 향상

    return value;
}
```

### Q3: 제네릭을 사용한 에러 생성의 장점은 무엇인가요?
**A**: 제네릭을 사용한 에러 생성은 타입 안전성과 확장성을 동시에 보장하는 함수형 프로그래밍 패턴의 핵심입니다. 이는 마치 `Option<T>`나 `Result<T, E>`처럼 제네릭을 활용하여 타입 정보를 보존하는 것과 같은 원리입니다.

`Create<T>`, `Create<T1, T2>`, `Create<T1, T2, T3>` 메서드를 통해 다양한 타입의 에러 정보를 타입 안전하게 관리할 수 있습니다. 타입 정보를 보존하면서도 다양한 시나리오를 지원하는 것입니다.

컴파일 타임에 타입 안전성을 보장하고 런타임에 정확한 값 정보를 활용할 수 있습니다. 타입 시스템의 이점을 최대한 활용하는 것입니다.

또한 확장성 측면에서 새로운 타입이나 개수의 값이 필요할 때 기존 코드를 수정하지 않고도 쉽게 대응할 수 있습니다. 이는 마치 개방-폐쇄 원칙(Open-Closed Principle)처럼, 확장에는 열려있고 수정에는 닫혀있는 설계를 에러 처리 영역에 적용한 것입니다.

**실제 예시:**
```csharp
// 타입 안전한 단일 값 에러
var stringError = ErrorCodeFactory.Create(
    errorCode: $"{nameof(DomainErrors)}.{nameof(Name)}.{nameof(TooShort)}",
    errorCurrentValue: "a",
    errorMessage: $"Name is too short. Current value: 'a'");
var intError = ErrorCodeFactory.Create(
    errorCode: $"{nameof(DomainErrors)}.{nameof(Age)}.{nameof(OutOfRange)}",
    errorCurrentValue: 150,
    errorMessage: $"Age is out of range. Current value: '150'");

// 타입 안전한 다중 값 에러
var coordinateError = ErrorCodeFactory.Create(
    errorCode: $"{nameof(DomainErrors)}.{nameof(Coordinate)}.{nameof(XOutOfRange)}",
    errorCurrentValue1: 1500,
    errorCurrentValue2: 2000,
    errorMessage: $"Coordinate is out of range. Current values: '1500', '2000'");
var addressError = ErrorCodeFactory.Create(
    errorCode: $"{nameof(DomainErrors)}.{nameof(Address)}.{nameof(Empty)}",
    errorCurrentValue1: "Empty Street",
    errorCurrentValue2: "Empty City",
    errorCurrentValue3: "12345",
    errorMessage: $"Address is empty. Street: 'Empty Street', City: 'Empty City', PostalCode: '12345'");

// 타입 정보가 보존되어 디버깅 시 정확한 값 확인 가능
// 런타임에 타입별로 다른 처리 로직 적용 가능
```

### Q4: LanguageExt와의 호환성은 어떻게 보장되나요?
**A**: LanguageExt와의 호환성은 기존 `Error` 타입을 상속받아 구현함으로써 완전한 호환성을 보장합니다. 이는 마치 어댑터 패턴처럼, 새로운 기능을 기존 인터페이스와 호환되도록 래핑하는 것입니다.

`ErrorCodeExpected`, `ErrorCodeExpected<T>`, `ErrorCodeExceptional` 클래스들이 모두 LanguageExt의 `Error` 클래스를 상속받아 구현되어 있습니다. 기존 계약을 준수하면서 새로운 기능을 추가하는 것입니다.

이러한 설계를 통해 기존 LanguageExt 생태계의 모든 기능을 그대로 사용할 수 있습니다. `Match`, `Map`, `Bind` 등의 함수형 연산자들과 완전히 호환되며, 기존 코드를 수정하지 않고도 새로운 에러 처리 시스템을 도입할 수 있습니다.

또한 기존 LanguageExt의 에러 처리 패턴과 자연스럽게 통합되어, 개발자들이 새로운 학습 곡선 없이도 체계적인 에러 처리를 적용할 수 있습니다. 기존 사용자 경험을 해치지 않으면서 새로운 기능을 제공하는 것입니다.

**실제 예시:**
```csharp
// LanguageExt의 기존 패턴과 완전 호환
var result = Denominator.Create(0);
result.Match(
    Succ: d => Console.WriteLine($"성공: {d}"),
    Fail: error => Console.WriteLine($"실패: {GetErrorDetails(error)}")  // 새로운 구조화된 에러 정보 활용
);

// 기존 LanguageExt 연산자들과 호환
var processedResult = result
    .Map(d => d * 2)
    .Bind(d => SomeOtherOperation(d));
```

### Q5: 에러 코드와 에러 메시지를 모두 사용하는 이유는 무엇인가요?
**A**: 에러 코드와 에러 메시지를 모두 사용하는 것은 구조화된 정보와 사용자 친화적인 메시지를 동시에 제공하기 위한 설계 결정입니다.

**에러 코드의 역할**은 프로그래밍적 처리와 분류입니다. `DomainErrors.Currency.NotThreeLetters`와 같은 계층적 에러 코드는 모니터링 시스템에서 에러를 도메인별로 집계하고, 코드에서 패턴 매칭을 통해 특정 에러 유형을 처리할 수 있게 합니다.

**에러 메시지의 역할**은 디버깅과 로깅입니다. `"Currency code must be exactly 3 letters. Current value: 'AB'"`와 같은 상세한 메시지는 개발자가 문제를 즉시 파악하고, 로그에서 에러 원인을 빠르게 추적할 수 있게 합니다.

이 두 가지를 분리함으로써 각각의 목적에 맞게 활용할 수 있습니다. 에러 코드는 시스템 간 통신이나 API 응답에서 일관된 식별자로 사용되고, 에러 메시지는 내부 로깅과 디버깅에 활용됩니다.

향후 다국어 지원이 필요한 경우, 에러 코드를 키로 사용하여 언어별 메시지를 매핑할 수 있습니다. 도메인 계층의 에러 메시지는 개발자용 영어 메시지로 유지하고, 인프라 계층에서 에러 코드를 기반으로 사용자 언어에 맞는 메시지로 변환하는 패턴을 적용할 수 있습니다.

### Q6: 실제 운영 환경에서 어떤 이점을 얻을 수 있나요?
**A**: 실제 운영 환경에서는 구조화된 에러 정보를 통한 디버깅 시간 단축과 모니터링 시스템의 효율성 향상이라는 실질적인 이점을 얻을 수 있습니다.

디버깅 측면에서는 에러 발생 시점에서 정확한 도메인 정보와 실패 이유를 즉시 파악할 수 있습니다. 이는 마치 스택 트레이스가 정확한 호출 경로를 제공하는 것처럼, 에러의 출처와 성격을 명확히 알려주는 것입니다.

모니터링 시스템에서는 에러를 도메인별로 집계하고 분석할 수 있습니다. 이는 마치 메트릭 시스템에서 카운터를 태그별로 분류하는 것처럼, 에러를 체계적으로 분류하여 트렌드 분석과 예방적 대응이 가능합니다.

또한 실패한 값 정보를 통해 입력 데이터의 품질 문제를 사전에 파악할 수 있습니다. 이는 마치 데이터 파이프라인에서 데이터 품질 모니터링을 수행하는 것처럼, 시스템의 안정성을 사전에 보장하는 역할을 합니다.

이러한 운영 환경에서의 이점은 개발 생산성 향상과 시스템 안정성 증대로 이어져, 전체적인 비즈니스 가치를 크게 향상시킵니다.

**실제 예시:**
```csharp
// 운영 환경에서의 에러 로깅
var error = ErrorCodeFactory.Create(
    errorCode: $"{nameof(DomainErrors)}.{nameof(Payment)}.{nameof(Declined)}",
    errorCurrentValue: "CardNumber: 1234-5678-9012-3456",
    errorMessage: $"Payment was declined. Card: '1234-5678-9012-3456'");

// 구조화된 에러 정보로 모니터링 시스템에 전송
logger.LogError("Payment processing failed", new {
    ErrorCode = error.ErrorCode,            // "DomainErrors.Payment.Declined"
    CurrentValue = error.ErrorCurrentValue, // "CardNumber: 1234-5678-9012-3456"
    Timestamp = DateTime.UtcNow,
    TraceId = Guid.NewGuid()
});

// 모니터링 시스템에서 도메인별 집계 및 분석 가능
// - Payment 도메인의 에러 빈도 분석
// - 특정 카드 번호 패턴의 문제 파악
// - 에러 트렌드 분석을 통한 예방적 대응

// 구체적인 에러 명명의 장점
var currencyError = ErrorCodeFactory.Create(
    errorCode: $"{nameof(DomainErrors)}.{nameof(Currency)}.{nameof(NotThreeLetters)}",
    errorCurrentValue: "AB",
    errorMessage: $"Currency code must be exactly 3 letters. Current value: 'AB'");
// 에러 코드만 봐도 "통화 코드가 3자리 영문자가 아니다"는 것을 즉시 파악 가능
```

### Q7: DomainErrors 중첩 클래스에서 "이유" 부분의 메서드 이름을 어떻게 정의해야 하나요?
**A**: 에러의 구체적인 원인을 명확하고 일관성 있게 표현하는 것이 핵심입니다. **"에러 코드만 봐도 무엇이 잘못되었는지 즉시 알 수 있어야 한다"**는 원칙을 따르세요.

**🎯 핵심 원칙 (5가지):**

1. **명확성**: `Bad` ❌ → `Empty` ✅ (무엇이 잘못되었는지 즉시 파악)
2. **일관성**: `WrongFormat` ❌ → `NotThreeDigits` ✅ (프로젝트 전체에서 동일한 패턴)
3. **간결성**: `ValueIsEmptyAndNull` ❌ → `Empty` ✅ (핵심만 표현)
4. **표준화**: `NotGood` ❌ → `MinExceedsMax` ✅ (구체적인 도메인 용어 사용)
5. **구체성**: `Invalid` ❌ → `XOutOfRange` ✅ (검증 조건에 맞는 구체적 이유)

**📋 표준 에러 이유 명명 규칙 (ErrorCode 프로젝트 실제 구현):**

| 에러 상황 | 메서드 이름 | 실제 예시 | 검증 조건 | 적용 클래스 |
|-----------|-------------|-----------|-----------|-------------|
| **빈 값** | `Empty` | `Empty(string value)` | `string.IsNullOrWhiteSpace(value)` | `Currency`, `PostalCode`, `Street`, `City` |
| **3자리 영문자 아님** | `NotThreeLetters` | `NotThreeLetters(string value)` | `value.Length != 3 \|\| !value.All(char.IsLetter)` | `Currency` |
| **5자리 아님** | `NotFiveDigits` | `NotFiveDigits(string value)` | `value.Length != 5 \|\| !value.All(char.IsDigit)` | `PostalCode` |
| **X 좌표 범위 초과** | `XOutOfRange` | `XOutOfRange(int value)` | `value < 0 \|\| value > 1000` | `Coordinate` |
| **Y 좌표 범위 초과** | `YOutOfRange` | `YOutOfRange(int value)` | `value < 0 \|\| value > 1000` | `Coordinate` |
| **금액 범위 초과** | `OutOfRange` | `OutOfRange(decimal value)` | `value < 0 \|\| value > 999999.99m` | `MoneyAmount` |
| **0 값** | `Zero` | `Zero(int value)` | `value == 0` | `Denominator` |
| **지원 안함** | `Unsupported` | `Unsupported(string value)` | `!supportedValues.Contains(value)` | `Currency` |
| **최솟값이 최댓값 초과** | `MinExceedsMax` | `MinExceedsMax(Price min, Price max)` | `(decimal)min > (decimal)max` | `PriceRange` |
| **시작일이 종료일 이후** | `StartAfterEnd` | `StartAfterEnd(DateTime start, DateTime end)` | `start >= end` | `DateRange` |

**💡 실제 프로젝트 예시 (E**

```csharp
// Currency.cs - 통화 코드 검증
internal static class DomainErrors
{
    public static Error Empty(string value) =>           // 빈 통화 코드
        ErrorCodeFactory.Create(
            errorCode: $"{nameof(DomainErrors)}.{nameof(Currency)}.{nameof(Empty)}",
            errorCurrentValue: value,
            errorMessage: $"Currency code cannot be empty. Current value: '{value}'");

    public static Error NotThreeLetters(string value) =>  // 3자리 영문자가 아님
        ErrorCodeFactory.Create(
            errorCode: $"{nameof(DomainErrors)}.{nameof(Currency)}.{nameof(NotThreeLetters)}",
            errorCurrentValue: value,
            errorMessage: $"Currency code must be exactly 3 letters. Current value: '{value}'");

    public static Error Unsupported(string value) =>     // 지원하지 않는 통화
        ErrorCodeFactory.Create(
            errorCode: $"{nameof(DomainErrors)}.{nameof(Currency)}.{nameof(Unsupported)}",
            errorCurrentValue: value,
            errorMessage: $"Currency code is not supported. Current value: '{value}'");
}

// MoneyAmount.cs - 금액 검증
internal static class DomainErrors
{
    public static Error OutOfRange(decimal value) =>     // 금액 범위 초과
        ErrorCodeFactory.Create(
            errorCode: $"{nameof(DomainErrors)}.{nameof(MoneyAmount)}.{nameof(OutOfRange)}",
            errorCurrentValue: value,
            errorMessage: $"Money amount must be between 0 and 999999.99. Current value: '{value}'");
}

// PostalCode.cs - 우편번호 검증
internal static class DomainErrors
{
    public static Error Empty(string value) =>           // 빈 우편번호
        ErrorCodeFactory.Create(
            errorCode: $"{nameof(DomainErrors)}.{nameof(PostalCode)}.{nameof(Empty)}",
            errorCurrentValue: value,
            errorMessage: $"Postal code cannot be empty. Current value: '{value}'");

    public static Error NotFiveDigits(string value) =>   // 5자리 숫자가 아님
        ErrorCodeFactory.Create(
            errorCode: $"{nameof(DomainErrors)}.{nameof(PostalCode)}.{nameof(NotFiveDigits)}",
            errorCurrentValue: value,
            errorMessage: $"Postal code must be exactly 5 digits. Current value: '{value}'");
}

// PriceRange.cs - 가격 범위 검증
internal static class DomainErrors
{
    public static Error MinExceedsMax(Price minPrice, Price maxPrice) =>  // 최솟값이 최댓값을 초과
        ErrorCodeFactory.Create(
            errorCode: $"{nameof(DomainErrors)}.{nameof(PriceRange)}.{nameof(MinExceedsMax)}",
            errorCurrentValue: $"MinPrice: {minPrice}, MaxPrice: {maxPrice}",
            errorMessage: $"Minimum price cannot exceed maximum price. Min: '{minPrice}', Max: '{maxPrice}'");
}

// DateRange.cs - 날짜 범위 검증
internal static class DomainErrors
{
    public static Error StartAfterEnd(DateTime startDate, DateTime endDate) =>  // 시작일이 종료일 이후
        ErrorCodeFactory.Create(
            errorCode: $"{nameof(DomainErrors)}.{nameof(DateRange)}.{nameof(StartAfterEnd)}",
            errorCurrentValue: $"StartDate: {startDate}, EndDate: {endDate}",
            errorMessage: $"Start date cannot be after or equal to end date. Start: '{startDate}', End: '{endDate}'");
}

// Coordinate.cs - 좌표 검증
internal static class DomainErrors
{
    public static Error XOutOfRange(int value) =>        // X 좌표가 범위를 벗어남
        ErrorCodeFactory.Create(
            errorCode: $"{nameof(DomainErrors)}.{nameof(Coordinate)}.{nameof(XOutOfRange)}",
            errorCurrentValue: value,
            errorMessage: $"X coordinate must be non-negative. Current value: '{value}'");

    public static Error YOutOfRange(int value) =>        // Y 좌표가 범위를 벗어남
        ErrorCodeFactory.Create(
            errorCode: $"{nameof(DomainErrors)}.{nameof(Coordinate)}.{nameof(YOutOfRange)}",
            errorCurrentValue: value,
            errorMessage: $"Y coordinate must be between 0 and 1000. Current value: '{value}'");
}

// Price.cs - 가격 검증
internal static class DomainErrors
{
    public static Error Negative(decimal value) =>       // 음수 가격
        ErrorCodeFactory.Create(
            errorCode: $"{nameof(DomainErrors)}.{nameof(Price)}.{nameof(Negative)}",
            errorCurrentValue: value,
            errorMessage: $"Price cannot be negative. Current value: '{value}'");
}

// DateRange.cs - 날짜 범위 검증
internal static class DomainErrors
{
    public static Error InvalidStartDate(DateTime value) =>  // 잘못된 시작일
        ErrorCodeFactory.Create(
            errorCode: $"{nameof(DomainErrors)}.{nameof(DateRange)}.{nameof(InvalidStartDate)}",
            errorCurrentValue: value,
            errorMessage: $"Start date is invalid. Current value: '{value}'");

    public static Error InvalidRange(DateTime start, DateTime end) =>  // 잘못된 범위 (시작일 >= 종료일)
        ErrorCodeFactory.Create(
            errorCode: $"{nameof(DomainErrors)}.{nameof(DateRange)}.{nameof(InvalidRange)}",
            errorCurrentValue: $"StartDate: {start}, EndDate: {end}",
            errorMessage: $"Date range is invalid. Start date must be before end date. Start: '{start}', End: '{end}'");
}
```

**⚠️ 주의사항 (5가지):**

1. **동사 금지**: `Validate` ❌ → `NotThreeDigits` ✅
2. **과거형 금지**: `Invalidated` ❌ → `XOutOfRange` ✅  
3. **일관된 접두사**: `BadFormat` ❌ → `NotFiveDigits` ✅
4. **표준 용어**: `NotGood` ❌ → `MinExceedsMax` ✅
5. **복수형 고려**: `InvalidDate` ❌ → `StartAfterEnd` ✅

**🎯 결과: 에러 코드만으로 문제 파악 가능**

```csharp
// 에러 코드: "DomainErrors.Currency.NotThreeLetters"
// 즉시 파악 가능한 정보:
// - 도메인: Currency (통화 관련)
// - 클래스: Currency (통화 값 객체)  
// - 이유: NotThreeLetters (3자리 영문자가 아님)
// - 값: "AB" (실패한 값)

// 에러 코드: "DomainErrors.MoneyAmount.OutOfRange"
// 즉시 파악 가능한 정보:
// - 도메인: MoneyAmount (금액 관련)
// - 클래스: MoneyAmount (금액 값 객체)
// - 이유: OutOfRange (범위를 벗어남)
// - 값: -100 (실패한 값)

// 에러 코드: "DomainErrors.PostalCode.NotFiveDigits"
// 즉시 파악 가능한 정보:
// - 도메인: PostalCode (우편번호 관련)
// - 클래스: PostalCode (우편번호 값 객체)
// - 이유: NotFiveDigits (5자리 숫자가 아님)
// - 값: "1234" (실패한 값)

// 에러 코드: "DomainErrors.PriceRange.MinExceedsMax"
// 즉시 파악 가능한 정보:
// - 도메인: PriceRange (가격 범위 관련)
// - 클래스: PriceRange (가격 범위 값 객체)
// - 이유: MinExceedsMax (최솟값이 최댓값을 초과)
// - 값: "MinPrice: ₩1,000, MaxPrice: ₩500" (실패한 값)
```

**🚀 핵심 개선 효과:**

1. **완전한 명확성**: 모든 에러 이름이 대상과 구체적인 이유를 명시
   - `NotThreeLetters` → "통화 코드가 3자리 영문자가 아니다"
   - `OutOfRange` → "금액이 범위를 벗어났다"
   - `MinExceedsMax` → "최솟값이 최댓값을 초과한다"
   - `XOutOfRange` → "X 좌표가 범위를 벗어났다"

2. **사용자 경험 향상**: 모든 에러에서 사용자가 즉시 무엇을 수정해야 하는지 알 수 있음
   - `NotFiveDigits` → "5자리 숫자로 입력하세요"
   - `StartAfterEnd` → "시작일이 종료일보다 이전이어야 합니다"

3. **DDD 원칙 준수**: 유비쿼터스 언어와 비즈니스 규칙을 완벽히 표현
   - 도메인 전문가가 이해할 수 있는 비즈니스 용어 사용
   - 검증 조건과 에러 이름이 1:1 매핑

4. **일관된 명명 규칙**: 동일한 관점에서 모든 에러 이름 설계
   - ErrorCode 프로젝트 전체에서 동일한 패턴 적용
   - 새로운 ValueObject 추가 시 기존 규칙을 그대로 활용 가능

이렇게 명명하면 디버깅과 모니터링의 효율성이 크게 향상됩니다!
