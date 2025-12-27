# 비교 가능한 복합 값 객체
> `ComparableValueObject`

## 목차
- [개요](#개요)
- [학습 목표](#학습-목표)
- [왜 필요한가?](#왜-필요한가)
- [핵심 개념](#핵심-개념)
- [실전 지침](#실전-지침)
- [프로젝트 설명](#프로젝트-설명)
- [한눈에 보는 정리](#한눈에 보는 정리)
- [FAQ](#faq)

## 개요

이 프로젝트는 값 객체(Value Object) 패턴의 완성된 형태인 비교 가능한 복합 값 객체(Comparable Composite Value Object)를 이해하고 실습하는 것을 목표로 합니다. 여러 값 객체들을 조합하면서도 비교 기능을 제공하여 가장 강력하고 실용적인 값 객체 패턴을 구현합니다.

## 학습 목표

### **핵심 학습 목표**
1. **완성된 값 객체 패턴 이해**: 비교 가능한 복합 값 객체의 모든 특징을 학습합니다.
2. **GetComparableEqualityComponents() 구현**: 비교 순서를 고려한 동등성 정의 방법을 실습합니다.
3. **LINQ 정렬 활용**: 복합 값 객체의 자연스러운 정렬을 체험합니다.
4. **실무적 적용**: 실제 애플리케이션에서 값 객체를 효과적으로 사용하는 방법을 학습합니다.

### **실습을 통해 확인할 내용**
- `ComparableValueObject`의 완전한 기능
- 복합 데이터의 비교 순서 정의
- LINQ에서의 자연스러운 정렬
- 값 객체 패턴의 실무적 활용

## 왜 필요한가?

이전 단계인 `05-ValueObject-Composite`에서는 여러 값 객체들을 조합하여 복합적인 도메인 개념을 표현할 수 있었습니다. 하지만 이러한 복합 값 객체들을 정렬하거나 비교할 때 여전히 문제가 발생했습니다.

**첫 번째 문제는 정렬 불가능성입니다.** 주소나 복합 데이터들을 정렬할 때 기준이 되는 값이 무엇인지 명확하지 않아 수동으로 비교 로직을 구현해야 했습니다. 이는 마치 데이터베이스에서 복합 키로 정렬할 때 순서를 명시적으로 지정해야 하는 것처럼 여전히 복잡했습니다.

**두 번째 문제는 LINQ 활용의 제한입니다.** `OrderBy()`, `Min()`, `Max()`와 같은 LINQ 메서드에서 복합 값 객체를 자연스럽게 사용할 수 없었습니다. 이는 마치 컬렉션에서 기본 타입이 아닌 객체를 정렬할 때마다 비교 함수를 제공해야 하는 것처럼 여전히 불편했습니다.

**세 번째 문제는 비교 연산자의 부재입니다.** 주소가 다른 주소보다 "더 이전"인지 "더 이후"인지 비교할 수 없었습니다. 이는 마치 숫자를 비교할 수 없게 되는 것처럼 여전히 직관적이지 않은 사용 경험을 제공했습니다.

이러한 문제들을 해결하기 위해 `ComparableValueObject`를 도입했습니다. `ComparableValueObject`는 복합 값 객체의 자연스러운 순서를 정의할 수 있게 합니다. 이는 마치 데이터베이스에서 복합 인덱스를 정의하는 것처럼, 복합 데이터의 정렬 기준을 명시적으로 지정할 수 있습니다.

## 핵심 개념

이 프로젝트의 핵심은 값 객체 패턴의 완성된 형태인 비교 가능한 복합 값 객체입니다. 크게 세 가지 개념으로 나눌 수 있습니다.

### 첫 번째 개념: 완전한 값 객체 컴포지션

`ComparableValueObject`는 여러 개별 값 객체들을 조합하면서도 완전한 비교 기능을 제공합니다. 이는 값 객체 패턴의 가장 강력하고 실용적인 형태입니다.

**핵심 아이디어는 "완전한 도메인 모델링"입니다.** 주소나 복합 데이터처럼 복잡한 도메인 개념을 값 객체로 표현하면서도 정렬과 비교가 자연스럽게 가능합니다.

예를 들어, 주소는 도로명, 도시, 우편번호로 구성됩니다. 각 부분은 독립적인 값 객체이지만, 전체 주소는 비교 가능한 하나의 단위로 작동합니다. 이는 마치 수학에서 복합 수를 다루는 것처럼, 복잡한 구조를 하나의 개념으로 취급하는 것입니다.

```csharp
// 완전한 값 객체 컴포지션
Address address = Address.Create("강남대로 123", "서울시", "12345");
// 비교 가능
bool isEarlier = address1 < address2;
```

이러한 완전한 컴포지션은 도메인 모델링의 완성도를 높여줍니다. 복합 데이터가 마치 기본 타입처럼 자연스럽게 작동합니다.

### 두 번째 개념: 비교 순서 정의

`ComparableValueObject`는 `GetComparableEqualityComponents()`를 통해 비교 순서를 명시적으로 정의합니다. 이는 복합 데이터의 자연스러운 정렬을 가능하게 합니다.

**핵심 아이디어는 "의미 있는 순서"입니다.** 주소의 경우 도시 → 우편번호 → 도로명 순으로 비교하는 것이 자연스럽습니다.

예를 들어, 주소 비교에서 먼저 도시를 비교하고, 같은 도시라면 우편번호로, 같은 우편번호라면 도로명으로 비교합니다. 이는 마치 사전에서 단어를 비교하는 것처럼 계층적 비교를 구현하는 것입니다.

```csharp
protected override IEnumerable<IComparable> GetComparableEqualityComponents()
{
    yield return (string)City;        // 도시 우선 비교
    yield return (string)PostalCode;  // 우편번호 두 번째
    yield return (string)Street;      // 도로명 마지막
}
```

이러한 순서 정의는 도메인에 맞는 직관적인 정렬을 가능하게 합니다. 주소록을 도시별로 정렬하거나, 같은 도시 내에서 우편번호별로 정렬하는 것이 자연스럽습니다.

### 세 번째 개념: LINQ 완전 통합

`ComparableValueObject`는 LINQ와 완전히 통합되어 모든 정렬과 비교 연산을 자연스럽게 사용할 수 있습니다.

**핵심 아이디어는 ".NET 생태계 완전 통합"입니다.** 값 객체가 마치 기본 타입처럼 LINQ에서 작동합니다.

예를 들어, 주소 목록을 도시별로 정렬하거나, 날짜 범위를 시간순으로 정렬하는 것이 매우 직관적입니다. 별도의 비교 함수를 제공하지 않아도 됩니다.

```csharp
// LINQ 완전 통합
var sortedAddresses = addresses
    .OrderBy(a => a)      // 자연스러운 정렬
    .Where(a => a < someAddress)  // 자연스러운 비교
    .ToList();
```

이러한 완전한 통합은 코드의 가독성과 생산성을 크게 향상시킵니다. 값 객체가 기존 코드에 자연스럽게 녹아들 수 있습니다.

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
1. **ComparableValueObject 상속**: 완전한 비교 기능 상속
2. **GetComparableEqualityComponents() 구현**: 의미 있는 비교 순서 정의
3. **LINQ 완전 통합**: OrderBy, Where 등에서 자연스러운 사용
4. **모든 비교 연산자 지원**: `<`, `<=`, `>`, `>=` 자동 지원

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

### 비교 표
| 구분 | ValueObject-Composite | ComparableValueObject-Composite |
|------|----------------------|-------------------------------|
| **비교 기능** | ❌ 미지원 | ✅ 자동 지원 |
| **LINQ 정렬** | ❌ 수동 구현 | ✅ 자동 지원 |
| **연산자 오버로딩** | ❌ 미지원 | ✅ 자동 지원 |
| **`IComparable<T>`** | ❌ 미구현 | ✅ 자동 구현 |
| **실용성** | 보통 | 높음 (완전한 통합) |

### 장단점 표
| 장점 | 단점 |
|------|------|
| **완전한 LINQ 통합** | 구현 복잡도 가장 높음 |
| **자연스러운 비교** | 비교 순서 명시적 정의 필요 |
| **.NET 생태계 완전 통합** | 모든 기능 학습 필요 |
| **최고의 실용성** | 초기 학습 투자 필요 |

## FAQ

### Q1: 왜 Address에서 City → PostalCode → Street 순으로 비교하나요?
**A**: 주소 비교에서 이 순서는 도메인적으로 가장 자연스럽고 실용적입니다. 도시부터 비교하는 것은 지리적 계층 구조를 반영합니다.

도시는 가장 큰 지리적 단위이므로 첫 번째 비교 기준이 됩니다. 같은 도시 내에서는 우편번호로 지역을 구분하고, 같은 지역 내에서는 도로명으로 세부 위치를 구분하는 것이 직관적입니다.

이는 마치 전화번호부를 도시별로 분류하고, 같은 도시 내에서 지역번호별로, 같은 지역 내에서 이름별로 정렬하는 것과 유사합니다. 이러한 순서는 실제 주소록이나 지도 서비스에서 널리 사용되는 방식입니다.

### Q2: GetComparableEqualityComponents()와 GetEqualityComponents()의 차이는 무엇인가요?
**A**: `GetEqualityComponents()`는 동등성 비교만을 위한 반면, `GetComparableEqualityComponents()`는 동등성 비교와 정렬 비교 모두를 위한 것입니다. 후자는 `IComparable` 타입의 요소들을 반환해야 합니다.

예를 들어, 주소의 동등성 비교에서는 세 가지 요소 모두를 확인하지만, 정렬에서는 요소들의 순서와 타입이 중요합니다. `GetComparableEqualityComponents()`는 정렬 알고리즘이 사용할 수 있는 `IComparable` 요소들을 올바른 순서로 제공합니다.

이는 마치 데이터베이스에서 동등성 비교와 정렬에 모두 사용되는 인덱스를 정의하는 것처럼, 두 가지 목적을 동시에 충족하는 것입니다.

### Q3: 복합 값 객체의 정렬 성능은 어떠한가요?
**A**: `ComparableValueObject`의 정렬 성능은 기본 타입과 유사합니다. 비교 연산이 각 구성 요소의 비교로 대체되므로 효율적입니다.

예를 들어, 주소 정렬은 최대 세 번의 문자열 비교로 완료됩니다. 이는 매우 효율적이며, 대부분의 애플리케이션에서 성능 bottleneck이 되지 않습니다.

성능이 중요한 경우라도 `ComparableValueObject`의 가독성과 유지보수성 이점이 더 큽니다. 정렬 성능은 대개 데이터 양과 비교 횟수에 더 큰 영향을 받습니다.

### Q4: 모든 복합 값 객체에 ComparableValueObject를 사용해야 하나요?
**A**: 아닙니다. 비교가 필요하지 않은 복합 데이터에는 `ValueObject`로 충분합니다. `ComparableValueObject`는 정렬이나 크기 비교가 필요한 경우에만 선택해야 합니다.

예를 들어, 사람의 이름과 주소로 구성된 데이터가 있다고 할 때, 이름 순 정렬이 필요하다면 `ComparableValueObject`를 사용하고, 그렇지 않다면 `ValueObject`로 충분합니다.

이는 마치 프로그래밍에서 필요한 인터페이스만 구현하는 인터페이스 분리 원칙을 따르는 것과 유사합니다. 불필요한 비교 기능은 코드의 복잡도를 증가시킬 뿐입니다.

### Q5: LINQ에서 OrderBy()를 사용할 때 추가 고려사항은 무엇인가요?
**A**: `OrderBy()` 사용 시 비교 순서가 의도한 대로 정의되어 있는지 확인해야 합니다. 또한 복합 비교에서 null 값이나 특수 케이스를 적절히 처리해야 합니다.

예를 들어, 주소 정렬에서 도시명이 null인 경우를 처리하거나, 국제 주소의 경우 현지화된 정렬 규칙을 고려해야 할 수 있습니다.

이는 마치 SQL에서 ORDER BY 절을 사용할 때 COLLATE이나 NULLS FIRST 같은 옵션을 고려하는 것과 유사합니다. 복합 데이터의 정렬은 단순한 경우보다 더 신중한 설계가 필요합니다.

### Q6: 값 객체 패턴의 진정한 가치는 무엇인가요?
**A**: 값 객체 패턴의 진정한 가치는 도메인 모델링의 정확성과 코드의 표현력입니다. 복합 데이터를 값 객체로 표현함으로써 비즈니스 규칙을 코드에 명확히 반영할 수 있습니다.

예를 들어, 주소가 값 객체이면 주소 관련 비즈니스 로직을 한 곳에 집중할 수 있고, 유효성 검증, 비교, 정렬 등의 기능을 일관되게 제공할 수 있습니다.

이는 마치 도메인 주도 설계에서 유비쿼터스 언어를 코드에 반영하는 것처럼, 비즈니스 요구사항을 정확히 모델링하는 것입니다. 값 객체는 단순한 데이터 구조가 아닌, 의미 있는 비즈니스 개념을 표현합니다.



