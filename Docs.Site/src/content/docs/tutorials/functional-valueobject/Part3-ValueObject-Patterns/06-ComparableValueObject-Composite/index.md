---
title: "비교 가능한 복합 값 객체"
---

> `ComparableValueObject`

## 개요

주소 목록을 도시별로 정렬하거나, 두 주소의 순서를 비교해야 할 때 `ValueObject`만으로는 충분하지 않습니다. `ComparableValueObject`는 여러 값 객체를 조합하면서도 비교와 정렬 기능을 자동으로 제공하여, 값 객체 패턴의 완성된 형태를 구현합니다.

## 학습 목표

1. `ComparableValueObject`를 상속하여 비교 가능한 복합 값 객체를 구현할 수 있습니다.
2. `GetComparableEqualityComponents()`를 오버라이드하여 의미 있는 비교 순서를 정의할 수 있습니다.
3. LINQ의 `OrderBy()`, `Where()` 등에서 복합 값 객체를 자연스럽게 사용할 수 있습니다.
4. 비교 연산자(`<`, `<=`, `>`, `>=`)가 자동으로 지원됨을 확인할 수 있습니다.

## 왜 필요한가?

이전 단계인 `05-ValueObject-Composite`에서는 여러 값 객체들을 조합하여 복합적인 도메인 개념을 표현할 수 있었습니다. 하지만 이러한 복합 값 객체들을 정렬하거나 비교하려면 수동으로 비교 로직을 구현해야 했습니다.

주소나 복합 데이터들을 정렬할 때 기준이 되는 값이 무엇인지 명확하지 않고, `OrderBy()`, `Min()`, `Max()` 같은 LINQ 메서드에서 복합 값 객체를 직접 사용할 수 없었습니다. 비교 연산자도 지원되지 않아 두 주소의 순서를 직관적으로 비교할 방법이 없었습니다.

`ComparableValueObject`는 이 문제를 해결합니다. `GetComparableEqualityComponents()`에서 반환하는 `IComparable` 요소들의 순서로 자연스러운 정렬 기준을 정의하며, `IComparable<T>` 구현과 비교 연산자를 자동으로 제공합니다.

## 핵심 개념

### 완전한 값 객체 컴포지션

`ComparableValueObject`는 여러 개별 값 객체들을 조합하면서도 완전한 비교 기능을 제공합니다. 주소는 도로명, 도시, 우편번호로 구성되며, 각 부분은 독립적인 값 객체이지만 전체 주소는 비교 가능한 하나의 단위로 작동합니다.

```csharp
// 완전한 값 객체 컴포지션
Address address = Address.Create("강남대로 123", "서울시", "12345");
// 비교 가능
bool isEarlier = address1 < address2;
```

### 비교 순서 정의

`GetComparableEqualityComponents()`를 통해 비교 순서를 명시적으로 정의합니다. 주소의 경우 도시(가장 큰 지리적 단위)를 먼저 비교하고, 같은 도시라면 우편번호, 같은 우편번호라면 도로명으로 비교하는 것이 자연스럽습니다.

```csharp
protected override IEnumerable<IComparable> GetComparableEqualityComponents()
{
    yield return (string)City;        // 도시 우선 비교
    yield return (string)PostalCode;  // 우편번호 두 번째
    yield return (string)Street;      // 도로명 마지막
}
```

이 순서 정의만으로 LINQ의 `OrderBy(a => a)`가 도시별, 우편번호별, 도로명별로 자연스럽게 정렬됩니다.

### LINQ 완전 통합

`ComparableValueObject`는 `IComparable<T>`를 구현하므로 LINQ의 모든 정렬/비교 연산에서 별도의 비교 함수 없이 사용할 수 있습니다.

```csharp
// LINQ 완전 통합
var sortedAddresses = addresses
    .OrderBy(a => a)      // 자연스러운 정렬
    .Where(a => a < someAddress)  // 자연스러운 비교
    .ToList();
```

## 실전 지침

### 예상 출력
```
=== 6. 비교 가능한 복합 값 객체 - ComparableValueObject ===
부모 클래스: ComparableValueObject
예시: Address (주소) - Street + City + PostalCode 조합

📋 특징:
   ✅ 복잡한 검증 로직을 가진 값 객체
   ✅ 비교 기능 자동 제공
   ✅ 여러 값 객체를 조합하여 더 복잡한 도메인 개념 표현
   ✅ Street + City + PostalCode = Address

🔍 성공 케이스:
   ✅ Address: 강남대로 123, 서울시 12345
     - Street: 강남대로 123
     - City: 서울시
     - PostalCode: 12345

   ✅ Address: 테헤란로 456, 서울시 67890
     - Street: 테헤란로 456
     - City: 서울시
     - PostalCode: 67890

   ✅ Address: 강남대로 123, 서울시 12345
     - Street: 강남대로 123
     - City: 서울시
     - PostalCode: 12345

📊 동등성 비교:
   강남대로 123, 서울시 12345 == 테헤란로 456, 서울시 67890 = False
   강남대로 123, 서울시 12345 == 강남대로 123, 서울시 12345 = True

📊 비교 기능 (IComparable<T>):
   강남대로 123, 서울시 12345 < 테헤란로 456, 서울시 67890 = True
   강남대로 123, 서울시 12345 <= 테헤란로 456, 서울시 67890 = True
   강남대로 123, 서울시 12345 > 테헤란로 456, 서울시 67890 = False
   강남대로 123, 서울시 67890 >= 테헤란로 456, 서울시 67890 = False

🔢 해시코드:
   강남대로 123, 서울시 12345.GetHashCode() = 304805004
   강남대로 123, 서울시 12345.GetHashCode() = 304805004
   동일한 값의 해시코드가 같은가? True

❌ 실패 케이스:
   Address("", "서울시", "12345"):
   Address("강남대로 123", "서울시", "1234"):
   Address("강남대로 123", "", "12345"):

📈 정렬 데모:
   정렬된 Address 목록:
     강남대로 123, 서울시 12345
     명동길 321, 서울시 23456
     종로 789, 서울시 34567
     테헤란로 456, 서울시 67890

💡 비교 가능한 복합 값 객체의 특징:
   - Street, City, PostalCode는 각각 독립적인 비교 가능한 값 객체
   - Address는 이 세 값 객체를 조합하여 더 복잡한 도메인 개념 표현
   - 각 구성 요소는 자체적인 검증 로직과 비교 기능을 가짐
   - 전체 Address는 구성 요소들의 조합으로 동등성 비교와 정렬 기능 제공

✅ 데모가 성공적으로 완료되었습니다!
```

### 핵심 구현 포인트

비교 가능한 복합 값 객체 구현의 핵심 네 가지입니다.

| 포인트 | 설명 |
|--------|------|
| **ComparableValueObject 상속** | 완전한 비교 기능 상속 |
| **GetComparableEqualityComponents() 구현** | 의미 있는 비교 순서 정의 |
| **LINQ 완전 통합** | OrderBy, Where 등에서 자연스러운 사용 |
| **모든 비교 연산자 지원** | `<`, `<=`, `>`, `>=` 자동 지원 |

## 프로젝트 설명

### 프로젝트 구조
```
06-ComparableValueObject-Composite/
├── Program.cs                              # 메인 실행 파일
├── ComparableValueObjectComposite.csproj  # 프로젝트 파일
├── ValueObjects/
│   ├── Address.cs                         # 비교 가능한 복합 주소 값 객체
│   ├── City.cs                           # 도시 값 객체
│   ├── PostalCode.cs                     # 우편번호 값 객체
│   └── Street.cs                         # 도로명 값 객체
└── README.md                             # 프로젝트 문서
```

### 핵심 코드

`Address`는 Street, City, PostalCode 세 값 객체를 조합하고, 비교 순서를 City -> PostalCode -> Street로 정의합니다.

**Address.cs - 비교 가능한 복합 값 객체**
```csharp
public sealed class Address : ComparableValueObject
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

    // LINQ Expression 복합 검증
    public static Validation<Error, (Street, City, PostalCode)> Validate(
        string street, string city, string postalCode) =>
        from validStreet in Street.Validate(street)
        from validCity in City.Validate(city)
        from validPostalCode in PostalCode.Validate(postalCode)
        select (Street: Street.CreateFromValidated(validStreet),
                City: City.CreateFromValidated(validCity),
                PostalCode: PostalCode.CreateFromValidated(validPostalCode));

    // 비교 순서 정의
    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return (string)City;        // 도시를 먼저 비교 (가장 큰 단위)
        yield return (string)PostalCode;  // 우편번호를 두 번째로 비교 (지역 구분)
        yield return (string)Street;      // 도로명을 마지막에 비교 (세부 주소)
    }
}
```

**Program.cs - 완전한 값 객체 데모**
```csharp
// 비교 연산자 자연스러운 사용
var a1 = address1.Match(Succ: x => x, Fail: _ => default!);
var a2 = address2.Match(Succ: x => x, Fail: _ => default!);
Console.WriteLine($"   {a1} < {a2} = {a1 < a2}");

// LINQ 정렬
var sortedAddresses = addresses.OrderBy(a => a).ToArray();
```

## 한눈에 보는 정리

`ValueObject` 기반 복합 값 객체와 `ComparableValueObject` 기반 복합 값 객체의 기능 차이를 비교합니다.

### 비교 표
| 구분 | ValueObject-Composite | ComparableValueObject-Composite |
|------|----------------------|-------------------------------|
| **비교 기능** | 미지원 | 자동 지원 |
| **LINQ 정렬** | 수동 구현 | 자동 지원 |
| **연산자 오버로딩** | 미지원 | 자동 지원 |
| **`IComparable<T>`** | 미구현 | 자동 구현 |
| **실용성** | 보통 | 높음 (완전한 통합) |

### 장단점 표
| 장점 | 단점 |
|------|------|
| **완전한 LINQ 통합** | 구현 복잡도 가장 높음 |
| **자연스러운 비교** | 비교 순서 명시적 정의 필요 |
| **.NET 생태계 완전 통합** | 모든 기능 학습 필요 |
| **최고의 실용성** | 초기 학습 투자 필요 |

## FAQ

### Q1: 왜 Address에서 City -> PostalCode -> Street 순으로 비교하나요?
**A**: 도시는 가장 큰 지리적 단위이므로 첫 번째 비교 기준이 됩니다. 같은 도시 내에서는 우편번호로 지역을 구분하고, 같은 지역 내에서는 도로명으로 세부 위치를 구분합니다. 실제 주소록이나 지도 서비스에서도 이 순서를 따릅니다.

### Q2: GetComparableEqualityComponents()와 GetEqualityComponents()의 차이는 무엇인가요?
**A**: `GetEqualityComponents()`는 동등성 비교만을 위한 반면, `GetComparableEqualityComponents()`는 동등성과 정렬 비교 모두를 위한 것입니다. 후자는 `IComparable` 타입의 요소들을 반환해야 하며, 요소들의 순서가 정렬 우선순위를 결정합니다.

### Q3: 모든 복합 값 객체에 ComparableValueObject를 사용해야 하나요?
**A**: 아닙니다. 정렬이나 크기 비교가 필요하지 않은 복합 데이터에는 `ValueObject`로 충분합니다. 불필요한 비교 기능은 코드의 복잡도만 증가시키므로, 실제로 정렬이 필요한 경우에만 `ComparableValueObject`를 선택하세요.

지금까지 프레임워크 기본 클래스를 활용한 값 객체 패턴을 모두 살펴보았습니다. 다음 장에서는 SmartEnum을 사용하여 도메인 로직을 내장한 타입 안전 열거형을 구현하는 방법을 다룹니다.

---

→ [7장: TypeSafeEnum](../07-TypeSafeEnum/)
