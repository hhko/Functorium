---
title: "ValueObject Framework"
---

## 개요

값 객체를 만들 때마다 동등성 비교, 해시코드, 연산자 오버로딩을 반복 구현해야 한다면? 베이스 클래스 기반 프레임워크를 도입하면 이 보일러플레이트를 제거하고, `IComparable<T>` 지원 유무와 값의 복잡성에 따라 6가지 타입을 효율적으로 구현할 수 있습니다.

## 학습 목표

이 장을 마치면 다음을 할 수 있습니다.

1. `IComparable<T>` 지원 유무와 값의 복잡성에 따른 **6가지 프레임워크 타입의 선택 기준을** 설명할 수 있습니다
2. 6가지 베이스 클래스를 활용하여 **프레임워크 기반 값 객체를** 구현할 수 있습니다
3. 동등성 비교, 해시코드, 연산자 오버로딩 등 공통 기능의 **중복 코드를** 프레임워크로 제거할 수 있습니다

## 왜 필요한가?

이전 단계 `ValidatedValueCreation`에서는 3가지 메서드 패턴(Create, CreateFromValidated, Validate)을 통해 값 객체 생성을 구현했습니다. 그러나 실제 프로젝트에서 다양한 타입의 값 객체를 구현하면 공통 기능(동등성 비교, 해시코드, 연산자 오버로딩)을 매번 새로 작성해야 했고, 구현자마다 다른 방식을 사용하여 일관성이 떨어졌으며, 공통 기능에 변경이 필요할 때 모든 값 객체를 개별 수정해야 했습니다.

**베이스 클래스 기반 프레임워크는** 이 공통 기능을 한 곳에서 관리하여 개발 생산성과 코드 품질을 동시에 향상시킵니다.

## 핵심 개념

### 단일값 객체: `SimpleValueObject<T>` / `ComparableSimpleValueObject<T>`

단일 값을 래핑하는 값 객체는 비교 필요 여부에 따라 베이스 클래스를 선택합니다. `SimpleValueObject<T>`는 동등성 비교와 해시코드만 제공하고, `ComparableSimpleValueObject<T>`는 `IComparable<T>`와 비교 연산자까지 자동 제공합니다.

이전 방식과 프레임워크 방식의 코드량 차이가 극명합니다.

```csharp
// 이전 방식 (모든 공통 기능을 직접 구현)
public sealed class Denominator : IEquatable<Denominator>, IComparable<Denominator>
{
    private readonly int _value;

    public Denominator(int value) => _value = value;

    public override bool Equals(object? obj) => /* 복잡한 동등성 비교 로직 */
    public override int GetHashCode() => /* 해시코드 생성 로직 */
    public static bool operator ==(Denominator? left, Denominator? right) => /* 연산자 오버로딩 */
    public int CompareTo(Denominator? other) => /* 비교 로직 */
    public static bool operator <(Denominator? left, Denominator? right) => /* 비교 연산자 */
    // ... 수십 줄의 보일러플레이트 코드
}

// 개선된 방식 (프레임워크 활용)
public sealed class Denominator : ComparableSimpleValueObject<int>
{
    private Denominator(int value) : base(value) { }

    public static Fin<Denominator> Create(int value) =>
        CreateFromValidation(Validate(value), validValue => new Denominator(validValue));

    public static Validation<Error, int> Validate(int value) =>
        value == 0 ? Error.New("0은 허용되지 않습니다") : value;

    // 모든 비교 기능이 자동으로 제공됨!
    // - IComparable<Denominator> 구현
    // - 모든 비교 연산자 오버로딩 (<, <=, >, >=)
    // - GetComparableEqualityComponents() 자동 구현
}
```

### 복합값 객체: `ValueObject` / `ComparableValueObject`

여러 값을 조합하는 복합 객체는 `GetEqualityComponents()` 또는 `GetComparableEqualityComponents()` 메서드를 오버라이드하여 동등성/비교에 사용할 구성 요소를 정의합니다. 프레임워크가 `Equals`, `GetHashCode`, `==`, `!=` 연산자를 자동 구현하며, `ComparableValueObject`는 `IComparable<T>`와 비교 연산자도 추가로 제공합니다.

비교가 불필요한 복합값 객체(Coordinate)와 비교가 필요한 복합값 객체(DateRange)의 구현 패턴입니다.

```csharp
// 비교 불가능한 복합값 객체
public sealed class Coordinate : ValueObject
{
    public int X { get; }
    public int Y { get; }

    private Coordinate(int x, int y) { X = x; Y = y; }

    public static Fin<Coordinate> Create(int x, int y) =>
        CreateFromValidation(
            Validate(x, y),
            validValues => new Coordinate(validValues.X, validValues.Y));

    public static Validation<Error, (int X, int Y)> Validate(int x, int y) =>
        from validX in ValidateX(x)
        from validY in ValidateY(y)
        select (X: validX, Y: validY);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return X;
        yield return Y;
    }
}

// 비교 가능한 복합값 객체
public sealed class DateRange : ComparableValueObject
{
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    private DateRange(DateTime startDate, DateTime endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }

    public static Fin<DateRange> Create(DateTime startDate, DateTime endDate) =>
        CreateFromValidation(
            Validate(startDate, endDate),
            validValues => new DateRange(validValues.StartDate, validValues.EndDate));

    public static Validation<Error, (DateTime StartDate, DateTime EndDate)> Validate(DateTime startDate, DateTime endDate) =>
        startDate >= endDate
            ? Error.New("시작일은 종료일보다 이전이어야 합니다")
            : (StartDate: startDate, EndDate: endDate);

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return StartDate;
        yield return EndDate;
    }
}
```

### 프레임워크 구조 (Framework Architecture)

`IComparable<T>` 지원 유무와 값의 복잡성에 따라 계층적으로 추상화되어 있습니다.

```csharp
// 계층적 프레임워크 구조
AbstractValueObject (기본 동등성, 해시코드)
    ↓
ValueObject (Validation 조합 헬퍼)
    ↓                       ↓
SimpleValueObject<T>    ComparableValueObject
                            ↓
                        ComparableSimpleValueObject<T> (완전한 기능)
```

다음 장에서는 기존 C# enum의 한계를 극복하는 타입 안전한 열거형을 SmartEnum으로 구현합니다.

## 실전 지침

### 예상 출력
```
=== ValueObject Framework 데모 ===

1. 비교 불가능한 primitive 값 객체 - BinaryData (바이너리 데이터)
   SimpleValueObject<byte[]> 기반으로 간결하게 구현

   ✅ 성공: BinaryData[5 bytes: 48 65 6C 6C 6F]
   ❌ 실패: 바이너리 데이터는 비어있을 수 없습니다
   ❌ 실패: 바이너리 데이터는 비어있을 수 없습니다
   📊 동등성: BinaryData[3 bytes: 01 02 03] == BinaryData[3 bytes: 01 02 03] = True
   📊 동등성: BinaryData[3 bytes: 01 02 03] == BinaryData[3 bytes: 04 05 06] = False
   📊 비교 기능: 제공되지 않음 (의도적으로)

2. 비교 가능한 primitive 값 객체 - Denominator (0이 아닌 정수)
   ComparableSimpleValueObject<int> 기반으로 간결하게 구현

   ✅ 성공: 5 (값: 5)
   ❌ 실패: 0은 허용되지 않습니다
   📊 비교: 3 < 5 = True
   📊 비교: 3 == 5 = False

3. 비교 불가능한 복합 primitive 값 객체 - Coordinate (X, Y 좌표)
   ValueObject 기반으로 2개 Validation 조합

   ✅ 성공: (100, 200) (X: 100, Y: 200)
   ❌ 실패: X 좌표는 0-1000 범위여야 합니다
   ❌ 실패: Y 좌표는 0-1000 범위여야 합니다
   📊 동등성: (100, 200) == (100, 200) = True

4. 비교 가능한 복합 primitive 값 객체 - DateRange (날짜 범위)
   ComparableValueObject 기반으로 2개 DateTime 조합

   ✅ 성공: 2024-01-01 ~ 2024-12-31 (시작: 2024-01-01, 종료: 2024-12-31)
   ❌ 실패: 시작일은 종료일보다 이전이어야 합니다
   ❌ 실패: 시작일은 종료일보다 이전이어야 합니다
   📊 비교: 2024-01-01 ~ 2024-06-30 < 2024-07-01 ~ 2024-12-31 = True
   📊 비교: 2024-01-01 ~ 2024-06-30 == 2024-01-01 ~ 2024-06-30 = True
   📊 비교: 2024-01-01 ~ 2024-06-30 > 2024-07-01 ~ 2024-12-31 = False

5. 비교 불가능한 복합 값 객체 - Address (Street, City, PostalCode)
   ValueObject 기반으로 3개 값 객체 조합

   ✅ 성공: 123 Main St, Seoul 12345
   ❌ 실패: 거리명은 비어있을 수 없습니다
   ❌ 실패: 우편번호는 5자리 숫자여야 합니다

   📋 개별 값 객체 생성:
   - Street: Broadway (값: Broadway)
   - City: New York (값: New York)
   - PostalCode: 10001 (값: 10001)
   - Address from validated: Broadway, New York 10001

6. 비교 가능한 복합 값 객체 - PriceRange (Price, Currency)
   ComparableValueObject 기반으로 Price, Currency 값 객체 조합

   ✅ 성공: KRW10,000 ~ KRW50,000 (최소: ₩10,000, 최대: ₩50,000, 통화: KRW)
   ❌ 실패: 가격은 0 이상이어야 합니다
   ❌ 실패: 가격은 0 이상이어야 합니다
   ❌ 실패: 최소 가격은 최대 가격보다 작거나 같아야 합니다
   ❌ 실패: 통화 코드는 3자리여야 합니다

   📊 비교 기능 데모:
   - KRW10,000 ~ KRW30,000 < KRW20,000 ~ KRW40,000 = True
   - KRW10,000 ~ KRW30,000 == KRW10,000 ~ KRW30,000 = True
   - KRW10,000 ~ KRW30,000 > KRW20,000 ~ KRW40,000 = False

   📋 개별 값 객체 생성:
   - MinPrice: ₩15,000 (값: 15000)
   - MaxPrice: ₩35,000 (값: 35000)
   - Currency: USD (값: USD)
   - PriceRange from validated: USD15,000 ~ USD35,000
```

### 핵심 구현 포인트
1. **프레임워크 상속**: 적절한 베이스 클래스 선택 (`SimpleValueObject<T>` vs `ValueObject`)
2. **CreateFromValidation 활용**: 프레임워크의 헬퍼 메서드를 통한 간결한 팩토리 메서드 구현
3. **검증 로직 분리**: `Validate` 메서드로 검증 책임을 명확히 분리

## 프로젝트 설명

### 프로젝트 구조
```
ValueObjectFramework/                       # 메인 프로젝트
├── Program.cs                              # 6가지 시나리오 데모
├── ValueObjects/                           # 값 객체 구현
│   ├── Comparable/                         # 비교 가능한 값 객체
│   │   ├── PrimitiveValueObjects/          # 비교 가능한 primitive 값 객체
│   │   │   └── Denominator.cs              # 0이 아닌 정수
│   │   ├── CompositePrimitiveValueObjects/ # 비교 가능한 복합 primitive 값 객체
│   │   │   └── DateRange.cs                # 날짜 범위
│   │   └── CompositeValueObjects/          # 비교 가능한 복합 값 객체
│   │       ├── Price.cs                    # 가격
│   │       ├── Currency.cs                 # 통화
│   │       └── PriceRange.cs               # 가격 범위 (Price, Currency 조합)
│   └── ComparableNot/                      # 비교 불가능한 값 객체
│       ├── PrimitiveValueObjects/          # 비교 불가능한 primitive 값 객체
│       │   └── BinaryData.cs               # 바이너리 데이터
│       ├── CompositePrimitiveValueObjects/ # 비교 불가능한 복합 primitive 값 객체
│       │   └── Coordinate.cs               # X, Y 좌표
│       └── CompositeValueObjects/          # 비교 불가능한 복합 값 객체
│           ├── Address.cs                  # 주소 (Street, City, PostalCode)
│           ├── Street.cs                   # 거리명
│           ├── City.cs                     # 도시명
│           └── PostalCode.cs               # 우편번호
├── ValueObjectFramework.csproj             # 프로젝트 파일
└── README.md                               # 메인 문서
```

### 핵심 코드

#### 1. BinaryData -- `SimpleValueObject<T>` 프레임워크

비교가 불필요한 단일값 객체입니다. `byte[]`는 `IComparable`을 구현하지 않으므로 `SimpleValueObject`를 사용합니다.

```csharp
public sealed class BinaryData : SimpleValueObject<byte[]>
{
    private BinaryData(byte[] value) : base(value) { }

    public static Fin<BinaryData> Create(byte[] value) =>
        CreateFromValidation(
            Validate(value),
            validValue => new BinaryData(validValue));

    public static Validation<Error, byte[]> Validate(byte[] value) =>
        value == null || value.Length == 0
            ? Error.New("바이너리 데이터는 비어있을 수 없습니다")
            : value;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        // byte[] 배열의 내용을 비교하기 위해 문자열로 변환
        yield return Convert.ToBase64String(Value);
    }

    public override string ToString() =>
        $"BinaryData[{Value.Length} bytes: {BitConverter.ToString(Value).Replace("-", " ")}]";
}
```

#### 2. Address -- ValueObject 프레임워크

여러 값 객체(Street, City, PostalCode)를 조합한 복합 값 객체입니다.

```csharp
public sealed class Address : ValueObject
{
    public Street Street { get; }
    public City City { get; }
    public PostalCode PostalCode { get; }

    private Address(Street street, City city, PostalCode postalCode)
    {
        Street = street;
        City = city;
        PostalCode = postalCode;
    }

    public static Fin<Address> Create(string streetValue, string cityValue, string postalCodeValue) =>
        CreateFromValidation(
            Validate(streetValue, cityValue, postalCodeValue),
            validValues => new Address(
                validValues.Street,
                validValues.City,
                validValues.PostalCode));

    public static Validation<Error, (Street Street, City City, PostalCode PostalCode)> Validate(
            string street, string city, string postalCode) =>
        from validStreet in Street.Validate(street)
        from validCity in City.Validate(city)
        from validPostalCode in PostalCode.Validate(postalCode)
        select (
            Street: Street.CreateFromValidated(validStreet),
            City: City.CreateFromValidated(validCity),
            PostalCode: PostalCode.CreateFromValidated(validPostalCode)
        );

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return PostalCode;
    }
}
```

#### 3. PriceRange -- ComparableValueObject 프레임워크

비교 가능한 값 객체(Price, Currency)를 조합한 복합 값 객체입니다.

```csharp
public sealed class PriceRange : ComparableValueObject
{
    public Price MinPrice { get; }
    public Price MaxPrice { get; }
    public Currency Currency { get; }

    private PriceRange(Price minPrice, Price maxPrice, Currency currency)
    {
        MinPrice = minPrice;
        MaxPrice = maxPrice;
        Currency = currency;
    }

    public static Fin<PriceRange> Create(decimal minPriceValue, decimal maxPriceValue, string currencyCode) =>
        CreateFromValidation(
            Validate(minPriceValue, maxPriceValue, currencyCode),
            validValues => new PriceRange(validValues.MinPrice, validValues.MaxPrice, validValues.Currency));

    public static Fin<PriceRange> CreateFromValidated(Price minPrice, Price maxPrice, Currency currency) =>
        CreateFromValidation(
            ValidatePriceRange(minPrice, maxPrice),
            validValues => new PriceRange(validValues.MinPrice, validValues.MaxPrice, currency));

    public static Validation<Error, (Price MinPrice, Price MaxPrice, Currency Currency)> Validate(
        decimal minPriceValue, decimal maxPriceValue, string currencyCode) =>
        from validMinPrice in Price.Validate(minPriceValue)
        from validMaxPrice in Price.Validate(maxPriceValue)
        from validCurrency in Currency.Validate(currencyCode)
        from validPriceRange in ValidatePriceRange(
            Price.CreateFromValidated(validMinPrice),
            Price.CreateFromValidated(validMaxPrice))
        select (
            MinPrice: validPriceRange.MinPrice,
            MaxPrice: validPriceRange.MaxPrice,
            Currency: Currency.CreateFromValidated(validCurrency)
        );

    private static Validation<Error, (Price MinPrice, Price MaxPrice)> ValidatePriceRange(Price minPrice, Price maxPrice) =>
        minPrice.Value > maxPrice.Value
            ? Error.New("최소 가격은 최대 가격보다 작거나 같아야 합니다")
            : (MinPrice: minPrice, MaxPrice: maxPrice);

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return MinPrice;
        yield return MaxPrice;
        yield return Currency;
    }

    public override string ToString() =>
        $"{Currency}{MinPrice.Value:N0} ~ {Currency}{MaxPrice.Value:N0}";
}
```


## 한눈에 보는 정리

### 이전 방식 vs 프레임워크 방식

| 구분 | 이전 방식 | 프레임워크 방식 |
|------|-----------|-----------------|
| **코드량** | 50-100줄 | 15-25줄 |
| **보일러플레이트** | 매번 직접 구현 | 프레임워크에서 제공 |
| **비교 기능** | 수동 구현 필요 | 자동으로 완전 제공 |
| **일관성** | 구현자마다 다름 | 프레임워크로 표준화 |
| **유지보수** | 개별 수정 필요 | 프레임워크 수정으로 일괄 적용 |

### 타입 선택 가이드

`IComparable<T>` 지원 유무와 값의 복잡성에 따라 적절한 베이스 클래스를 선택합니다.

| 타입 | 베이스 클래스 | `IComparable<T>` | 예시 |
|------|---------------|----------------|------|
| **단일값, 비교 불필요** | `SimpleValueObject<T>` | 미지원 | `BinaryData` |
| **단일값, 비교 필요** | `ComparableSimpleValueObject<T>` | 지원 | `Denominator` |
| **복합값, 비교 불필요** | `ValueObject` | 미지원 | `Coordinate`, `Address` |
| **복합값, 비교 필요** | `ComparableValueObject` | 지원 | `DateRange`, `PriceRange` |

### 장단점

| 장점 | 단점 |
|------|------|
| **코드 중복 90% 감소** | **프레임워크 학습 필요** |
| **완전히 일관된 구현 패턴** | **프레임워크 의존성** |
| **비교 기능 자동화** | **과도한 추상화 위험** |
| **유지보수성 향상** | **타입 제약 조건** |

## FAQ

### Q1: 프레임워크 타입은 어떻게 선택하나요?
**A**: 두 가지 기준으로 결정합니다. (1) 정렬/비교가 필요한가? 필요하면 `Comparable` 접두사가 붙은 타입을 선택합니다. (2) 단일 값인가, 복합 값인가? 단일 값이면 `SimpleValueObject<T>` 계열, 복합 값이면 `ValueObject` 계열을 선택합니다.

### Q2: `ComparableSimpleValueObject<T>`의 타입 제약 조건은?
**A**: `T`가 `IComparable`을 구현해야 합니다. `int`, `string`, `DateTime` 등 .NET 기본 타입은 모두 충족하므로 대부분 문제없습니다. 비교가 불필요한 타입(`byte[]` 등)은 `SimpleValueObject<T>`를 사용합니다.

### Q3: CreateFromValidation 헬퍼는 어떻게 작동하나요?
**A**: `Validation<Error, TValue>`를 받아 성공 시 팩토리 함수를 적용하여 값 객체를 생성하고, 실패 시 Error를 그대로 전달하여 `Fin<TValueObject>`를 반환합니다.

```csharp
// CreateFromValidation 헬퍼의 내부 동작
public static Fin<TValueObject> CreateFromValidation<TValueObject, TValue>(
    Validation<Error, TValue> validation,
    Func<TValue, TValueObject> factory)
    where TValueObject : ValueObject
{
    return validation
        .Map(factory)        // 성공 시 factory 함수 적용
        .ToFin();           // Validation을 Fin으로 변환
}
```
