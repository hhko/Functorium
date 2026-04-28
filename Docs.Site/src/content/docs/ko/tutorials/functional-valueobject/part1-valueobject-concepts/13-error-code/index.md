---
title: "에러 체계화"
---

## 개요

`Error.New("Invalid denominator value: 0")`라는 에러 메시지만으로 어떤 도메인에서, 어떤 이유로, 어떤 값이 문제를 일으켰는지 파악할 수 있나요? `"DomainErrors.클래스.이유"` 형식의 구조화된 에러 코드와 실패 당시의 값 정보를 함께 관리하면, 디버깅과 모니터링의 효율성이 크게 향상됩니다.

## 학습 목표

이 장을 마치면 다음을 할 수 있습니다.

1. `DomainErrors.클래스.이유` 형식의 **구조화된 에러 코드 시스템을** 설계할 수 있습니다
2. 실패 당시의 값과 에러 코드를 함께 관리하는 **타입 안전한 에러 처리 시스템을** 구축할 수 있습니다
3. 기존 LanguageExt의 `Error` 타입과 완전히 호환되는 **에러 처리 프레임워크를** 설계할 수 있습니다

## 왜 필요한가?

이전 단계 `11-ValueObject-Framework`에서는 프레임워크를 통해 값 객체의 생성과 검증을 체계화했습니다. 그러나 실제 운영 환경에서 에러가 발생했을 때 세 가지 문제가 있었습니다. 기존 `Error.New` 방식은 단순한 문자열 메시지만 제공하여 에러의 출처를 체계적으로 파악하기 어렵고, 실패한 값 정보가 메시지에 하드코딩되어 동적 분석이 불가능하며, 값 객체마다 다른 형식의 에러 메시지를 사용하여 일관성이 부족했습니다.

**구조화된 에러 코드 시스템은** 에러 발생 시점의 도메인 정보, 실패 이유, 실패한 값을 체계적으로 분리하여 관리합니다.

## 핵심 개념

### 구조화된 에러 코드 시스템

에러를 `"DomainErrors.클래스.이유"` 형식의 계층적 코드로 분류합니다. 도메인 영역, 구체적인 클래스, 실패 이유가 코드에 명시되어 에러의 출처와 성격을 즉시 식별할 수 있습니다.

기존 방식과 구조화된 방식의 에러 생성을 비교합니다.

```csharp
// 이전 방식 (구조화되지 않은 방식) - 디버깅과 모니터링이 어려움
var error = Error.New("Invalid denominator value: 0");

// 개선된 방식 (구조화된 방식) - 체계적인 에러 관리
var error = ErrorFactory.Create(
    errorCode: $"{nameof(DomainErrors)}.{nameof(Denominator)}.{nameof(Zero)}",
    errorCurrentValue: 0,
    errorMessage: $"Denominator cannot be zero. Current value: '0'");
```

### 타입 안전한 에러 정보 관리

`Create<T>`, `Create<T1, T2>` 등 제네릭 오버로딩을 통해 실패한 값의 타입 정보를 보존합니다. 컴파일 타임에 타입 안전성이 보장되고, 런타임에 정확한 값 정보를 활용할 수 있습니다.

```csharp
// 다양한 타입의 에러 정보를 타입 안전하게 관리
var stringError = ErrorFactory.Create(
    errorCode: $"{nameof(DomainErrors)}.{nameof(Name)}.{nameof(TooShort)}",
    errorCurrentValue: "i@name",
    errorMessage: $"Name is too short. Current value: 'i@name'");
var intError = ErrorFactory.Create(
    errorCode: $"{nameof(DomainErrors)}.{nameof(Age)}.{nameof(Invalid)}",
    errorCurrentValue: 150,
    errorMessage: $"Age is out of range. Current value: '150'");
var multiValueError = ErrorFactory.Create(
    errorCode: $"{nameof(DomainErrors)}.{nameof(Coordinate)}.{nameof(OutOfRange)}",
    errorCurrentValue1: 1500,
    errorCurrentValue2: 2000,
    errorMessage: $"Coordinate is out of range. Current values: '1500', '2000'");
```

### 내부 DomainErrors 클래스 패턴

값 객체와 관련된 에러 정의를 같은 파일 내에 위치시켜 높은 응집도를 달성합니다. 새 값 객체를 생성할 때 에러 정의도 함께 작성하므로 개발 생산성이 향상됩니다.

```csharp
public sealed class Denominator : SimpleValueObject<int>
{
    // ... 기존 코드 ...

    internal static class DomainErrors
    {
        public static Error Zero(int value) =>
            ErrorFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Denominator)}.{nameof(Zero)}",
                errorCurrentValue: value,
                errorMessage: $"Denominator cannot be zero. Current value: '{value}'");
    }
}
```

다음 장에서는 이 에러 코드 시스템에 Fluent API를 적용하여 더 간결한 에러 정의 방식을 구현합니다.

## 실전 지침

### 예상 출력
```
=== 체계적인 에러 처리 패턴 ===

=== Comparable 테스트 ===

--- CompositeValueObjects 하위 폴더 ---
  === CompositeValueObjects 에러 테스트 ===

  --- Currency 에러 테스트 ---
빈 통화 코드: ErrorCode: Domain.Currency.Empty, ErrorCurrentValue:
3자리가 아닌 형식: ErrorCode: Domain.Currency.NotThreeLetters, ErrorCurrentValue: AB
지원하지 않는 통화: ErrorCode: Domain.Currency.Unsupported, ErrorCurrentValue: XYZ

  --- Price 에러 테스트 ---
음수 가격: ErrorCode: Domain.MoneyAmount.OutOfRange, ErrorCurrentValue: -100

  --- PriceRange 에러 테스트 ---
최솟값이 최댓값을 초과하는 가격 범위: ErrorCode: Domain.PriceRange.MinExceedsMax, ErrorCurrentValue: MinPrice: KRW (한국 원화) ₩ 1,000.00, MaxPrice: KRW (한국 원화) ₩ 500.00

--- PrimitiveValueObjects 하위 폴더 ---
  === PrimitiveValueObjects 에러 테스트 ===

  --- Denominator 에러 테스트 ---
0 값: ErrorCode: Domain.Denominator.Zero, ErrorCurrentValue: 0

--- CompositePrimitiveValueObjects 하위 폴더 ---
  === CompositePrimitiveValueObjects 에러 테스트 ===

  --- DateRange 에러 테스트 ---
시작일이 종료일 이후인 날짜 범위: ErrorCode: Domain.DateRange.StartAfterEnd, ErrorCurrentValue: StartDate: 2024-12-31 오전 12:00:00, EndDate: 2024-01-01 오전 12:00:00

=== ComparableNot 폴더 테스트 ===

--- CompositeValueObjects 하위 폴더 ---
  === CompositeValueObjects 에러 테스트 ===

  --- Address 에러 테스트 ---
빈 거리명: ErrorCode: Domain.Street.Empty, ErrorCurrentValue:
빈 도시명: ErrorCode: Domain.City.Empty, ErrorCurrentValue:
잘못된 우편번호: ErrorCode: Domain.PostalCode.NotFiveDigits, ErrorCurrentValue: 1234

  --- Street 에러 테스트 ---
빈 거리명: ErrorCode: Domain.Street.Empty, ErrorCurrentValue:

  --- City 에러 테스트 ---
빈 도시명: ErrorCode: Domain.City.Empty, ErrorCurrentValue:

  --- PostalCode 에러 테스트 ---
빈 우편번호: ErrorCode: Domain.PostalCode.Empty, ErrorCurrentValue:
5자리 숫자가 아닌 형식: ErrorCode: Domain.PostalCode.NotFiveDigits, ErrorCurrentValue: 1234

--- PrimitiveValueObjects 하위 폴더 ---
  === PrimitiveValueObjects 에러 테스트 ===

  --- BinaryData 에러 테스트 ---
null 바이너리 데이터: ErrorCode: Domain.BinaryData.Empty, ErrorCurrentValue: null
빈 바이너리 데이터: ErrorCode: Domain.BinaryData.Empty, ErrorCurrentValue: 0

--- CompositePrimitiveValueObjects 하위 폴더 ---
  === CompositePrimitiveValueObjects 에러 테스트 ===

  --- Coordinate 에러 테스트 ---
범위를 벗어난 X 좌표: ErrorCode: Domain.Coordinate.XOutOfRange, ErrorCurrentValue: -1
범위를 벗어난 Y 좌표: ErrorCode: Domain.Coordinate.YOutOfRange, ErrorCurrentValue: 1001
```

### 핵심 구현 포인트
1. **ErrorFactory의 제네릭 오버로딩**: `Create<T>`, `Create<T1, T2>` 메서드를 통해 다양한 타입의 에러 정보를 타입 안전하게 관리
2. **내부 DomainErrors 클래스 패턴**: 값 객체 내부에 `internal static class DomainErrors`를 정의하여 응집도 높은 에러 관리
3. **구체적인 에러 이유 명명**: `Empty`, `NotThreeLetters`, `NotFiveDigits`, `MinExceedsMax` 등 검증 조건과 정확히 일치하는 명명 규칙
4. **LanguageExt 호환성**: 기존 `Error` 타입을 상속받아 생태계와 완전한 호환성 보장

## 프로젝트 설명

### 프로젝트 구조
```
ErrorCode/                                  # 메인 프로젝트
├── Program.cs                              # 메인 실행 파일 (ValueObjects 폴더 구조와 일치하는 테스트)
├── ErrorCode.csproj                        # 프로젝트 파일
├── Framework/                              # 에러 처리 프레임워크
│   ├── Abstractions/
│   │   └── Errors/
│   │       ├── ErrorFactory.cs         # 에러 생성 팩토리
│   │       ├── ExpectedError.cs        # 구조화된 에러 타입들
│   │       └── ExceptionalError.cs     # 예외 기반 에러
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

#### ErrorFactory -- 에러 생성 팩토리
```csharp
public static class ErrorFactory
{
    // 기본 에러 생성
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error Create(string errorCode, string errorCurrentValue, string errorMessage) =>
        new ExpectedError(errorCode, errorCurrentValue, errorMessage);

    // 제네릭 단일 값 에러 생성
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error Create<T>(string errorCode, T errorCurrentValue, string errorMessage) where T : notnull =>
        new ExpectedError<T>(errorCode, errorCurrentValue, errorMessage);

    // 제네릭 다중 값 에러 생성
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error Create<T1, T2>(string errorCode, T1 errorCurrentValue1, T2 errorCurrentValue2, string errorMessage)
        where T1 : notnull where T2 : notnull =>
        new ExpectedError<T1, T2>(errorCode, errorCurrentValue1, errorCurrentValue2, errorMessage);

    // 예외 기반 에러 생성
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error CreateFromException(string errorCode, Exception exception) =>
        new ExceptionalError(errorCode, exception);

    // 에러 코드 포맷팅
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Format(params string[] parts) =>
        string.Join('.', parts);
}
```

#### Denominator -- 내부 DomainErrors 패턴 적용
```csharp
public sealed class Denominator : SimpleValueObject<int>, IComparable<Denominator>
{
    // ... 기존 구현 ...

    public static Validation<Error, int> Validate(int value)
    {
        if (value == 0)
            return Domain.Zero(value);

        return value;
    }

    // 내부 DomainErrors 클래스 - 응집도 높은 에러 정의
    internal static class DomainErrors
    {
        public static Error Zero(int value) =>
            ErrorFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Denominator)}.{nameof(Zero)}",
                errorCurrentValue: value,
                errorMessage: $"Denominator cannot be zero. Current value: '{value}'");
    }
}
```

#### Currency -- SmartEnum 기반 에러 정의

SmartEnum에서도 동일한 내부 DomainErrors 패턴을 적용합니다.

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
            ? Domain.Empty(currencyCode)
            : currencyCode;

    private static Validation<Error, string> ValidateFormat(string currencyCode) =>
        currencyCode.Length != 3 || !currencyCode.All(char.IsLetter)
            ? Domain.NotThreeLetters(currencyCode)
            : currencyCode.ToUpperInvariant();

    // 내부 DomainErrors 클래스 - SmartEnum 특화 에러 정의
    internal static class DomainErrors
    {
        public static Error Empty(string value) =>
            ErrorFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Currency)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: $"Currency code cannot be empty. Current value: '{value}'");

        public static Error NotThreeLetters(string value) =>
            ErrorFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Currency)}.{nameof(NotThreeLetters)}",
                errorCurrentValue: value,
                errorMessage: $"Currency code must be exactly 3 letters. Current value: '{value}'");

        public static Error Unsupported(string value) =>
            ErrorFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Currency)}.{nameof(Unsupported)}",
                errorCurrentValue: value,
                errorMessage: $"Currency code is not supported. Current value: '{value}'");
    }
}
```

#### PriceRange -- 다중 값 에러 정의

복합 값 객체에서 다중 값 에러를 정의하는 패턴입니다.

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
            ? Domain.MinExceedsMax(minPrice, maxPrice)
            : (MinPrice: minPrice, MaxPrice: maxPrice);

    // 내부 DomainErrors 클래스 - 가격 범위 검증 에러
    internal static class DomainErrors
    {
        public static Error MinExceedsMax(Price minPrice, Price maxPrice) =>
            ErrorFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(PriceRange)}.{nameof(MinExceedsMax)}",
                errorCurrentValue: $"MinPrice: {minPrice}, MaxPrice: {maxPrice}",
                errorMessage: $"Minimum price cannot exceed maximum price. Min: '{minPrice}', Max: '{maxPrice}'");
    }
}
```

## 한눈에 보는 정리

### 비교 표

기존 Error.New 방식과 ErrorFactory 방식의 차이를 요약합니다.

| 구분 | 이전 방식 (Error.New) | 현재 방식 (ErrorFactory) |
|------|----------------------|------------------------------|
| **에러 코드 구조** | 단순한 문자열 메시지 | `DomainErrors.클래스.이유` 형식 |
| **값 정보 관리** | 메시지에 하드코딩 | 타입 안전한 별도 필드 |
| **디버깅 지원** | 메시지 파싱 필요 | 구조화된 정보 즉시 제공 |
| **모니터링 지원** | 일관성 부족 | 표준화된 형식으로 집계 가능 |
| **타입 안전성** | 없음 | 제네릭으로 보장 |

### 장단점 표

구조화된 에러 코드 시스템의 트레이드오프를 정리합니다.

| 장점 | 단점 |
|------|------|
| **구조화된 에러 관리** | **초기 설정 복잡성** |
| **타입 안전한 에러 정보** | **코드 볼륨 증가** |
| **LanguageExt 완전 호환** | **학습 곡선 존재** |
| **디버깅 및 모니터링 향상** | **프레임워크 의존성** |

### 에러 이유 명명 규칙

에러 메서드 이름은 검증 조건과 정확히 일치해야 합니다. 에러 코드만 봐도 무엇이 잘못되었는지 즉시 파악할 수 있어야 합니다.

| 에러 상황 | 메서드 이름 | 적용 클래스 |
|-----------|-------------|-------------|
| **빈 값** | `Empty` | `Currency`, `PostalCode`, `Street`, `City` |
| **3자리 영문자 아님** | `NotThreeLetters` | `Currency` |
| **5자리 숫자 아님** | `NotFiveDigits` | `PostalCode` |
| **좌표 범위 초과** | `XOutOfRange`, `YOutOfRange` | `Coordinate` |
| **금액 범위 초과** | `OutOfRange` | `MoneyAmount` |
| **0 값** | `Zero` | `Denominator` |
| **지원 안 함** | `Unsupported` | `Currency` |
| **최솟값 > 최댓값** | `MinExceedsMax` | `PriceRange` |
| **시작일 >= 종료일** | `StartAfterEnd` | `DateRange` |

## FAQ

### Q1: 기존 Error.New 방식 대비 어떤 장점이 있나요?
**A**: 구조화된 에러 코드(`Domain.Denominator.Zero`)를 통해 에러의 출처와 이유를 즉시 파악할 수 있고, 타입 안전한 값 필드로 모니터링 시스템에서 도메인별 집계가 가능합니다. 기존 방식은 메시지 문자열을 파싱해야 했습니다.

### Q2: 내부 DomainErrors 클래스를 사용하는 이유는?
**A**: 값 객체와 에러 정의를 같은 파일에 두어 응집도를 높입니다. 값 객체를 수정할 때 관련 에러도 함께 확인할 수 있고, 새 값 객체 생성 시 에러 정의도 자연스럽게 함께 작성합니다.

### Q3: LanguageExt와의 호환성은 어떻게 보장되나요?
**A**: `ExpectedError`, `ExpectedError<T>` 등이 모두 LanguageExt의 `Error` 클래스를 상속받아 구현됩니다. `Match`, `Map`, `Bind` 등의 함수형 연산자와 완전히 호환되므로, 기존 코드를 수정하지 않고도 새 에러 처리 시스템을 도입할 수 있습니다.

---

에러 코드 구조가 갖춰졌지만, 매번 `ErrorFactory.Create`를 직접 호출하면 코드가 장황해집니다. 다음 장에서는 `DomainError` 헬퍼와 `DomainErrorKind`을 도입하여 에러 생성을 간결하게 만듭니다.

→ [14장: 에러 코드 Fluent](../14-Error-Code-Fluent/)
