# ValueObject Framework

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

이 프로젝트는 **값 객체 구현의 중복 코드를 최소화**하기 위해 **베이스 클래스 기반 프레임워크**를 도입하는 방법을 학습합니다. 이전 프로젝트들에서 매번 반복적으로 구현했던 동등성 비교, 해시코드, 연산자 오버로딩, 비교 기능 등의 공통 기능을 프레임워크로 추상화하여 **6가지 값 객체 타입**을 효율적으로 구현합니다.

## 학습 목표

### **핵심 학습 목표**
1. **6가지 프레임워크 타입 이해**: `IComparable<T>` 지원 유무와 값의 복잡성에 따른 프레임워크 선택 기준
2. **프레임워크 기반 값 객체 구현**: 6가지 베이스 클래스를 활용한 효율적인 값 객체 구현
3. **중복 코드 제거**: 동등성 비교, 해시코드, 연산자 오버로딩, 비교 기능 등 공통 기능의 프레임워크화

### **실습을 통해 확인할 내용**
- **6가지 프레임워크 타입**: `ComparableSimpleValueObject<T>`, `SimpleValueObject<T>`, `ComparableValueObject`, `ValueObject`의 각각 다른 특징
- **프레임워크 활용**: 베이스 클래스의 `CreateFromValidation` 헬퍼 메서드를 통한 간결한 구현
- **코드 중복 제거**: 이전 프로젝트 대비 90% 이상 코드 감소와 완전히 일관된 구현 패턴

## 왜 필요한가?

이전 단계인 `ValidatedValueCreation`에서는 3가지 메서드 패턴(Create, CreateFromValidated, Validate)을 통해 효율적인 값 객체 생성을 구현했습니다. 하지만 실제 프로젝트에서 다양한 타입의 값 객체를 구현하려고 할 때 몇 가지 문제가 발생했습니다.

**첫 번째 문제는 반복적인 보일러플레이트 코드입니다.** 마치 매번 새로운 컨트롤러를 만들 때마다 기본적인 CRUD 메서드를 반복 구현하는 것과 같은 비효율성입니다. 동등성 비교, 해시코드 생성, 연산자 오버로딩 등은 모든 값 객체에서 동일한 패턴으로 구현되지만, 매번 새로 작성해야 했습니다. 이는 개발 시간을 늘리고 실수 가능성을 높입니다.

**두 번째 문제는 일관성 부족입니다.** 마치 여러 개발자가 각자 다른 방식으로 API를 설계하듯이, 값 객체마다 다른 구현 방식과 네이밍 컨벤션을 사용하게 됩니다. 이는 코드 리뷰 시간을 늘리고 유지보수성을 저하시킵니다. 특히 동등성 비교나 연산자 오버로딩에서 미묘한 차이가 발생할 수 있습니다.

**세 번째 문제는 확장성과 유지보수성의 한계입니다.** 마치 하드코딩된 설정값들을 매번 수정해야 하는 것처럼, 공통 기능에 변경이 필요할 때 모든 값 객체를 개별적으로 수정해야 합니다. 이는 버그 수정이나 성능 개선 시 큰 부담이 됩니다.

이러한 문제들을 해결하기 위해 **베이스 클래스 기반 프레임워크**를 도입했습니다. 이 프레임워크를 사용하면 공통 기능을 한 곳에서 관리하고, 값 객체 구현에 집중할 수 있어 개발 생산성과 코드 품질을 크게 향상시킬 수 있습니다.

## 핵심 개념

이 프로젝트의 핵심은 **`IComparable<T>` 지원 유무**에 따라 크게 6가지 개념으로 나눌 수 있습니다. 각각이 어떻게 작동하는지 쉽게 설명해드리겠습니다.

### 첫 번째 개념: `SimpleValueObject<T>` 프레임워크 (비교 불가능한 primitive 값 객체)

**핵심 아이디어는 "비교가 필요하지 않은 단일 값을 래핑하는 값 객체의 공통 기능을 추상화"입니다.** 마치 `List<T>`와 `SortedList<T>`의 차이처럼, 비교 기능이 필요하지 않은 경우에 사용하는 경량화된 프레임워크입니다.

예를 들어, 바이너리 데이터나 복잡한 객체를 래핑하는 값 객체의 경우 정렬이나 비교가 의미가 없을 수 있습니다. 이때는 `SimpleValueObject<T>`를 사용하여 동등성 비교와 해시코드만 제공받을 수 있습니다.

```csharp
// 비교 기능이 필요 없는 단일값 객체
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

    // 비교 기능은 제공되지 않음 (의도적으로)
    // 동등성 비교와 해시코드만 자동 제공
}
```

이 방식의 장점은 **필요한 기능만 제공**하여 **성능과 메모리 효율성**을 최적화한다는 것입니다. 비교 기능이 필요하지 않은 경우 불필요한 오버헤드를 제거할 수 있습니다.

### 두 번째 개념: `ComparableSimpleValueObject<T>` 프레임워크 (비교 가능한 primitive 값 객체)

**핵심 아이디어는 "비교 가능한 단일 값을 래핑하는 값 객체의 공통 기능을 완전히 추상화"입니다.** 마치 제네릭 컬렉션 클래스가 다양한 타입을 처리하듯이, `ComparableSimpleValueObject<T>`는 어떤 비교 가능한 타입의 단일 값이든 래핑할 수 있는 범용 프레임워크입니다.

예를 들어, `Denominator`는 단순히 `int` 값을 래핑하는 값 객체입니다. 이전 방식에서는 동등성 비교, 해시코드, 연산자 오버로딩, 비교 가능성을 모두 직접 구현해야 했지만, 이제는 `ComparableSimpleValueObject<int>`를 상속받기만 하면 됩니다. 마치 인터페이스를 구현하듯이 필요한 메서드만 오버라이드하면 됩니다.

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

이 방식의 장점은 **코드 중복을 90% 이상 제거**하고 **완전히 일관된 구현 패턴**을 보장한다는 것입니다. 특히 비교 가능성 관련 코드가 완전히 자동화되어 실수할 가능성이 없어집니다. 또한 프레임워크에서 공통 기능을 개선하면 모든 값 객체가 자동으로 혜택을 받습니다.

### 세 번째 개념: ValueObject 프레임워크 (비교 불가능한 복합 primitive 값 객체)

**핵심 아이디어는 "비교가 필요하지 않은 복합 값 객체의 공통 기능과 Validation 조합 헬퍼를 제공"입니다.** 마치 함수형 프로그래밍에서 모나드 체이닝을 통해 여러 연산을 조합하듯이, `ValueObject`는 여러 값의 검증을 조합하는 헬퍼 메서드를 제공합니다.

예를 들어, `Coordinate`는 X, Y 두 개의 좌표를 조합한 값 객체입니다. 각 좌표의 검증이 성공했을 때만 Coordinate 객체를 생성해야 합니다. 이전 방식에서는 복잡한 에러 처리와 조합 로직을 직접 구현해야 했지만, 이제는 `CreateFromValidation` 헬퍼를 사용하면 됩니다.

```csharp
// 비교 불가능한 복합값 객체
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
```

이 방식의 장점은 **함수형 프로그래밍의 모나드 체이닝**을 활용하여 **선언적이고 읽기 쉬운 코드**를 작성할 수 있다는 것입니다. 또한 에러 처리가 자동화되어 실수 가능성이 크게 줄어듭니다. 비교 기능이 필요하지 않은 경우 경량화된 구현을 제공합니다.

**핵심은 `GetEqualityComponents()` 메서드 오버라이드입니다.** 이 메서드는 동등성 비교에 사용될 구성 요소들을 정의합니다. 프레임워크는 이 메서드가 반환하는 값들을 기반으로 자동으로 `Equals`, `GetHashCode`, `==`, `!=` 연산자를 구현합니다. 마치 인터페이스의 추상 메서드를 구현하듯이, 개발자는 동등성 비교에 필요한 구성 요소만 정의하면 됩니다.

### 네 번째 개념: ComparableValueObject 프레임워크 (비교 가능한 복합 primitive 값 객체)

**핵심 아이디어는 "비교 가능한 복합 값 객체의 공통 기능과 Validation 조합 헬퍼를 제공"입니다.** 마치 함수형 프로그래밍에서 모나드 체이닝을 통해 여러 연산을 조합하듯이, `ComparableValueObject`는 여러 비교 가능한 값의 검증을 조합하는 헬퍼 메서드를 제공합니다.

예를 들어, 날짜 범위를 나타내는 값 객체는 시작일과 종료일을 조합하며, 날짜는 비교 가능한 타입입니다. 이때 `ComparableValueObject`를 사용하면 자동으로 비교 기능을 제공받을 수 있습니다.

```csharp
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

이 방식의 장점은 **복합 객체에서도 비교 기능을 자동으로 제공**받을 수 있다는 것입니다. 각 구성 요소가 비교 가능하다면 전체 객체도 자동으로 비교 가능해집니다.

**핵심은 `GetComparableEqualityComponents()` 메서드 오버라이드입니다.** 이 메서드는 동등성 비교와 비교 기능에 사용될 `IComparable` 구현 구성 요소들을 정의합니다. 프레임워크는 이 메서드가 반환하는 값들을 기반으로 자동으로 `Equals`, `GetHashCode`, `==`, `!=` 연산자뿐만 아니라 `IComparable<T>` 구현과 모든 비교 연산자(`<`, `<=`, `>`, `>=`)도 구현합니다. 마치 `GetEqualityComponents()`의 비교 가능한 버전처럼, 개발자는 비교에 필요한 구성 요소만 정의하면 됩니다.

### 다섯 번째 개념: ValueObject 프레임워크 (비교 불가능한 복합 값 객체)

**핵심 아이디어는 "여러 값 객체를 조합하여 더 복잡한 도메인 개념을 표현하는 값 객체의 공통 기능을 제공"입니다.** 마치 컴포지트 패턴처럼, 여러 값 객체를 조합하여 더 큰 개념을 만들어냅니다.

예를 들어, `Address`는 `Street`, `City`, `PostalCode`라는 세 개의 값 객체를 조합한 복합 값 객체입니다. 각각의 값 객체가 독립적으로 검증되고, 모든 검증이 성공했을 때만 Address 객체가 생성됩니다.

```csharp
// 비교 불가능한 복합 값 객체
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

이 방식의 장점은 **도메인 개념의 계층적 구조를 자연스럽게 표현**할 수 있다는 것입니다. 각 구성 요소가 독립적으로 검증되고 재사용 가능하며, 전체 객체의 일관성도 보장됩니다.

### 여섯 번째 개념: ComparableValueObject 프레임워크 (비교 가능한 복합 값 객체)

**핵심 아이디어는 "여러 비교 가능한 값 객체를 조합하여 더 복잡한 도메인 개념을 표현하는 값 객체의 공통 기능을 제공"입니다.** 마치 컴포지트 패턴처럼, 여러 비교 가능한 값 객체를 조합하여 더 큰 개념을 만들어냅니다.

예를 들어, `PriceRange`는 `Price`, `Currency`라는 두 개의 비교 가능한 값 객체를 조합한 복합 값 객체입니다. 각각의 값 객체가 독립적으로 검증되고, 모든 검증이 성공했을 때만 PriceRange 객체가 생성됩니다. 또한 각 구성 요소가 비교 가능하므로 전체 객체도 자동으로 비교 가능해집니다.

```csharp
// 비교 가능한 복합 값 객체
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

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return MinPrice;
        yield return MaxPrice;
        yield return Currency;
    }
}
```

이 방식의 장점은 **도메인 개념의 계층적 구조를 자연스럽게 표현**하면서도 **비교 기능을 자동으로 제공**받을 수 있다는 것입니다. 각 구성 요소가 독립적으로 검증되고 재사용 가능하며, 전체 객체의 일관성과 비교 가능성도 보장됩니다.

**핵심은 `GetComparableEqualityComponents()` 메서드 오버라이드입니다.** 이 메서드는 동등성 비교와 비교 기능에 사용될 `IComparable` 구현 구성 요소들을 정의합니다. 프레임워크는 이 메서드가 반환하는 값들을 기반으로 자동으로 모든 동등성 비교와 비교 기능을 구현합니다. 마치 다섯 번째 개념의 `GetEqualityComponents()`와 유사하지만, 비교 가능한 값 객체들을 조합할 때 사용됩니다.

### 프레임워크 구조 (Framework Architecture)

**핵심 아이디어는 "`IComparable<T>` 지원 유무와 값의 복잡성에 따른 계층적 추상화"입니다.** 마치 객체지향 프로그래밍에서 상속을 통해 기능을 확장하듯이, 프레임워크도 계층적으로 구성되어 있습니다.

프레임워크 구조는 다음과 같습니다:
- `AbstractValueObject`: 가장 기본적인 동등성 비교와 해시코드 기능
- `ValueObject`: 복합 값 객체를 위한 Validation 조합 헬퍼 추가
- `ComparableValueObject`: 비교 가능한 복합 값 객체를 위한 비교 기능 추가
- `SimpleValueObject<T>`: 단일 값을 래핑하는 특화된 기능 추가
- `ComparableSimpleValueObject<T>`: 비교 가능한 단일 값을 래핑하는 완전한 기능 제공

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

이 구조의 장점은 **`IComparable<T>` 지원 유무와 값의 복잡성에 따라 적절한 추상화 레벨을 선택**할 수 있다는 것입니다. 각 상황에 맞는 최적화된 프레임워크를 선택할 수 있습니다.

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

#### 1. Denominator (비교 가능한 primitive 값 객체) - `ComparableSimpleValueObject<T>` 프레임워크
```csharp
public sealed class Denominator : ComparableSimpleValueObject<int>
{
    private Denominator(int value) : base(value) { }

    public static Fin<Denominator> Create(int value) =>
        CreateFromValidation(
            Validate(value),
            validValue => new Denominator(validValue));

    public static Validation<Error, int> Validate(int value) =>
        value == 0 ? Error.New("0은 허용되지 않습니다") : value;

    // 비교 가능성은 ComparableSimpleValueObject<int>에서 자동으로 제공됨
    // - IComparable<Denominator> 구현
    // - 모든 비교 연산자 오버로딩 (<, <=, >, >=)
    // - GetComparableEqualityComponents() 자동 구현
}
```

#### 2. BinaryData (비교 불가능한 primitive 값 객체) - `SimpleValueObject<T>` 프레임워크
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

    // 비교 기능은 제공되지 않음 (의도적으로)
    // - byte[]는 IComparable을 구현하지 않음
    // - 동등성 비교와 해시코드만 자동 제공
}
```

#### 3. Coordinate (비교 불가능한 복합 primitive 값 객체) - ValueObject 프레임워크
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

    public override string ToString() =>
        $"({X}, {Y})";
}
```

#### 4. DateRange (비교 가능한 복합 primitive 값 객체) - ComparableValueObject 프레임워크
```csharp
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
        from validStartDate in ValidateStartDate(startDate)
        from validEndDate in ValidateEndDate(endDate)
        from validRange in ValidateDateRange(validStartDate, validEndDate)
        select (StartDate: validStartDate, EndDate: validEndDate);

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return StartDate;
        yield return EndDate;
    }

    public override string ToString() =>
        $"{StartDate:yyyy-MM-dd} ~ {EndDate:yyyy-MM-dd}";
}
```

#### 5. Address (비교 불가능한 복합 값 객체) - ValueObject 프레임워크
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

#### 6. PriceRange (비교 가능한 복합 값 객체) - ComparableValueObject 프레임워크
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

### 비교 표
| 구분 | 이전 방식 | 프레임워크 방식 |
|------|-----------|-----------------|
| **코드량** | 50-100줄 | 15-25줄 |
| **보일러플레이트** | 매번 직접 구현 | 프레임워크에서 제공 |
| **비교 기능** | 수동 구현 필요 | 자동으로 완전 제공 |
| **일관성** | 구현자마다 다름 | 프레임워크로 표준화 |
| **유지보수** | 개별 수정 필요 | 프레임워크 수정으로 일괄 적용 |
| **에러 처리** | 직접 구현 | 헬퍼 메서드로 자동화 |

### 6가지 값 객체 타입 비교
| 타입 | 베이스 클래스 | `IComparable<T>` | 특징 | 예시 |
|------|---------------|----------------|------|------|
| **비교 불가능한 primitive 값 객체** | `SimpleValueObject<T>` | ❌ 미지원 | 단일 값을 래핑, 동등성만 제공 | `BinaryData` |
| **비교 불가능한 복합 primitive 값 객체** | `ValueObject` | ❌ 미지원 | 여러 primitive 값 조합, 동등성만 제공 | `Coordinate` |
| **비교 불가능한 복합 값 객체** | `ValueObject` | ❌ 미지원 | 여러 값 객체 조합, 동등성만 제공 | `Address` |
| **비교 가능한 primitive 값 객체** | `ComparableSimpleValueObject<T>` | ✅ 지원 | 단일 값을 래핑, 비교 기능 자동 제공 | `Denominator` |
| **비교 가능한 복합 primitive 값 객체** | `ComparableValueObject` | ✅ 지원 | 여러 primitive 값 조합, 비교 기능 제공 | `DateRange` |
| **비교 가능한 복합 값 객체** | `ComparableValueObject` | ✅ 지원 | 여러 값 객체 조합, 비교 기능 자동 제공 | `Price`, `Currency`, `PriceRange` |

### 장단점 표
| 장점 | 단점 |
|------|------|
| **코드 중복 90% 감소** | **프레임워크 학습 필요** |
| **완전히 일관된 구현 패턴** | **프레임워크 의존성** |
| **비교 기능 자동화** | **초기 설정 복잡성** |
| **자동화된 에러 처리** | **과도한 추상화 위험** |
| **유지보수성 향상** | **타입 제약 조건** |

## FAQ

### Q1: 4가지 프레임워크 타입의 차이점은 무엇인가요?
**A**: 프레임워크는 **`IComparable<T>` 지원 유무**와 **값의 복잡성(단일/복합)**에 따라 4가지로 구분됩니다. 이는 마치 컬렉션 타입을 선택하는 것과 같습니다. 각각 특정 용도에 최적화되어 있습니다.

**비교 가능한 단일값**: `ComparableSimpleValueObject<T>`는 비교 가능한 단일 값을 래핑할 때 사용하며, 자동으로 동등성 비교, 해시코드, 타입 변환, 비교 가능성 기능을 모두 제공합니다. 예를 들어 `Denominator`는 `int` 값을 래핑하므로 `ComparableSimpleValueObject<int>`를 상속받습니다.

**비교 불가능한 단일값**: `SimpleValueObject<T>`는 비교가 필요하지 않은 단일 값을 래핑할 때 사용하며, 동등성 비교와 해시코드만 제공합니다. 예를 들어 `BinaryData`는 `byte[]`를 래핑하므로 `SimpleValueObject<byte[]>`를 상속받습니다.

**비교 가능한 복합값**: `ComparableValueObject`는 여러 비교 가능한 값을 조합할 때 사용하며, `GetComparableEqualityComponents()` 메서드를 오버라이드하여 비교 기능을 정의합니다. 예를 들어 `DateRange`는 시작일과 종료일을 조합하므로 `ComparableValueObject`를 상속받습니다.

**비교 불가능한 복합값**: `ValueObject`는 여러 값을 조합할 때 사용하며, `GetEqualityComponents()` 메서드를 오버라이드하여 동등성 비교를 정의해야 합니다. 예를 들어 `Coordinate`는 X, Y 두 값을 조합하므로 `ValueObject`를 상속받습니다.

**실제 예시:**
```csharp
// 1. ComparableSimpleValueObject<T> - 비교 가능한 단일값 래핑
public sealed class Denominator : ComparableSimpleValueObject<int>
{
    // Value 속성, 타입 변환, 비교 기능이 모두 자동으로 제공됨
    // (int)denominator로 직접 변환 가능
    // denominator1 < denominator2 비교 가능
}

// 2. SimpleValueObject<T> - 비교 불가능한 단일값 래핑
public sealed class BinaryData : SimpleValueObject<byte[]>
{
    // Value 속성, 타입 변환만 제공됨
    // 비교 기능은 제공되지 않음 (의도적으로)
}

// 3. ComparableValueObject - 비교 가능한 복합값 조합
public sealed class DateRange : ComparableValueObject
{
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    // GetComparableEqualityComponents()를 오버라이드해야 함
    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return StartDate;
        yield return EndDate;
    }
}

// 4. ValueObject - 비교 불가능한 복합값 조합
public sealed class Coordinate : ValueObject
{
    public int X { get; }
    public int Y { get; }

    // GetEqualityComponents()를 오버라이드해야 함
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return X;
        yield return Y;
    }
}
```

### Q2: 프레임워크를 사용하면 성능에 영향을 주나요?
**A**: 프레임워크 사용은 오히려 성능을 향상시킵니다. 이는 마치 JIT 컴파일러가 최적화된 코드를 생성하듯이, 프레임워크의 공통 기능이 최적화되어 있기 때문입니다.

프레임워크의 `CreateFromValidation` 헬퍼는 LanguageExt의 최적화된 Validation 모나드를 활용하여 효율적인 에러 처리를 제공합니다. 또한 동등성 비교와 해시코드 생성도 최적화된 알고리즘을 사용합니다.

개별 구현에서는 매번 다른 방식으로 구현할 수 있어 일관성이 떨어지고, 때로는 비효율적인 구현이 될 수 있습니다. 하지만 프레임워크를 사용하면 검증된 최적화된 구현을 사용할 수 있습니다.

**실제 예시:**
```csharp
// 프레임워크 방식 - 최적화된 Validation 체이닝
public static Fin<Address> Create(string street, string city, string postalCode) =>
    CreateFromValidation(
        Validate(street, city, postalCode),  // 최적화된 Validation 조합
        validValues => new Address(...));    // 효율적인 객체 생성

// 개별 구현 방식 - 비효율적인 에러 처리 가능성
public static Fin<Address> Create(string street, string city, string postalCode)
{
    // 매번 다른 방식으로 구현할 수 있어 비효율적일 수 있음
    var streetResult = Street.Create(street);
    if (streetResult.IsFail) return streetResult.ToFin<Address>();
    // ... 반복적인 에러 체크
}
```

### Q3: 프레임워크 없이도 값 객체를 구현할 수 있는데 왜 프레임워크가 필요한가요?
**A**: 프레임워크는 값 객체 구현 자체를 위한 것이 아니라, **일관성과 유지보수성**을 위한 것입니다. 이는 마치 ORM 없이도 데이터베이스에 접근할 수 있지만, ORM을 사용하는 이유와 같습니다.

프레임워크 없이 구현하면 각 개발자가 다른 방식으로 동등성 비교, 해시코드, 연산자 오버로딩을 구현할 수 있습니다. 이는 코드 리뷰 시간을 늘리고, 미묘한 버그를 발생시킬 수 있습니다. 특히 동등성 비교에서 실수하면 예상치 못한 동작을 할 수 있습니다.

프레임워크를 사용하면 모든 값 객체가 동일한 패턴으로 구현되어 일관성이 보장됩니다. 또한 프레임워크에서 공통 기능을 개선하면 모든 값 객체가 자동으로 혜택을 받습니다.

**실제 예시:**
```csharp
// 프레임워크 없이 구현 - 일관성 부족
public class UserId1 : IEquatable<UserId1>
{
    public override bool Equals(object? obj) => /* 개발자 A의 구현 */
}

public class UserId2 : IEquatable<UserId2>
{
    public override bool Equals(object? obj) => /* 개발자 B의 다른 구현 */
}

// 프레임워크 사용 - 일관성 보장
public class UserId1 : SimpleValueObject<string> { /* 자동으로 일관된 구현 */ }
public class UserId2 : SimpleValueObject<string> { /* 자동으로 일관된 구현 */ }
```

### Q4: 프레임워크의 CreateFromValidation 헬퍼는 어떻게 작동하나요?
**A**: `CreateFromValidation` 헬퍼는 함수형 프로그래밍의 모나드 체이닝을 활용하여 Validation 결과를 Fin으로 변환하는 역할을 합니다. 이는 마치 LINQ의 Select 메서드가 IEnumerable을 변환하듯이, Validation을 Fin으로 변환합니다.

헬퍼의 작동 원리는 다음과 같습니다:
1. `Validation<Error, TValue>`를 받아서
2. 성공 시 `factory` 함수를 적용하여 값 객체를 생성하고
3. 실패 시 Error를 그대로 전달하여
4. 최종적으로 `Fin<TValueObject>`를 반환합니다

이 과정에서 LanguageExt의 최적화된 모나드 연산을 활용하여 효율적인 에러 처리를 제공합니다.

**실제 예시:**
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

// 사용 예시
public static Fin<Denominator> Create(int value) =>
    CreateFromValidation(
        Validate(value),                    // Validation<Error, int>
        validValue => new Denominator(validValue)); // int -> Denominator
```

### Q5: 언제 어떤 값 객체 부모 클래스를 선택해야 하나요?
**A**: 프레임워크 선택은 **`IComparable<T>` 지원 유무**와 **값의 복잡성**에 따라 결정됩니다. 이는 마치 컬렉션 타입을 선택하는 것과 같습니다. 각 상황에 맞는 최적화된 선택을 할 수 있습니다.

**선택 가이드라인:**
1. **단일값 + 비교 필요**: `ComparableSimpleValueObject<T>` (예: `Denominator`, `UserId`)
2. **단일값 + 비교 불필요**: `SimpleValueObject<T>` (예: `BinaryData`, `ImageData`)
3. **복합값 + 비교 필요**: `ComparableValueObject` (예: `DateRange`, `PriceRange`)
4. **복합값 + 비교 불필요**: `ValueObject` (예: `Coordinate`, `Address`)

**실제 예시:**
```csharp
// 1. 정렬이 필요한 ID → ComparableSimpleValueObject<T>
public sealed class UserId : ComparableSimpleValueObject<int> { }

// 2. 정렬이 불필요한 바이너리 데이터 → SimpleValueObject<T>
public sealed class ImageData : SimpleValueObject<byte[]> { }

// 3. 정렬이 필요한 날짜 범위 → ComparableValueObject
public sealed class DateRange : ComparableValueObject { }

// 4. 정렬이 불필요한 좌표 → ValueObject
public sealed class Coordinate : ValueObject { }
```

이러한 선택 기준을 따르면 각 상황에 맞는 최적화된 프레임워크를 사용할 수 있어 성능과 기능의 균형을 맞출 수 있습니다.

### Q6: `ComparableSimpleValueObject<T>`를 사용할 때 타입 제약 조건이 있나요?
**A**: 네, `ComparableSimpleValueObject<T>`는 `T`가 `IComparable`을 구현해야 한다는 제약 조건이 있습니다. 이는 마치 제네릭 컬렉션에서 `T`가 특정 인터페이스를 구현해야 하는 것과 같습니다.

이 제약 조건은 비교 기능을 자동으로 제공하기 위해 필요합니다. `int`, `string`, `DateTime` 등 .NET의 기본 타입들은 모두 `IComparable`을 구현하므로 대부분의 경우 문제가 없습니다.

만약 비교 기능이 필요하지 않은 단일값 객체라면 `SimpleValueObject<T>`를 사용할 수 있습니다. 하지만 대부분의 값 객체는 정렬이나 비교가 필요하므로 `ComparableSimpleValueObject<T>`를 사용하는 것이 권장됩니다.

**실제 예시:**
```csharp
// 올바른 사용 - int는 IComparable을 구현함
public sealed class UserId : ComparableSimpleValueObject<int>
{
    // 모든 비교 기능이 자동으로 제공됨
}

// 올바른 사용 - string도 IComparable을 구현함
public sealed class Email : ComparableSimpleValueObject<string>
{
    // 문자열 비교가 자동으로 제공됨
}

// 비교 기능이 필요 없는 경우 - SimpleValueObject 사용
public sealed class BinaryData : SimpleValueObject<byte[]>
{
    // byte[]는 IComparable을 구현하지 않으므로 SimpleValueObject 사용
}
```
