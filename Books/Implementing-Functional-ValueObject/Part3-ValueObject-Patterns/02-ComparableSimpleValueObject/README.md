# 비교 가능한 단순 값 객체
> `ComparableSimpleValueObject<T>`

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

이 프로젝트는 비교 가능한 값 객체 패턴인 `ComparableSimpleValueObject<T>`를 이해하고 실습하는 것을 목표로 합니다. 값 객체의 기본 기능에 더해 자동으로 비교 기능을 제공받아 정렬과 비교 연산을 사용할 수 있습니다.

## 학습 목표

### **핵심 학습 목표**
1. **비교 가능한 값 객체 이해**: `ComparableSimpleValueObject<T>`의 특징과 장점을 학습합니다.
2. **자동 비교 기능 체험**: `IComparable<T>` 인터페이스가 자동으로 구현되는 것을 확인합니다.
3. **정렬 기능 활용**: 값 객체를 컬렉션에서 자연스럽게 정렬하는 방법을 실습합니다.
4. **비교 연산자 사용**: `<`, `<=`, `>`, `>=` 연산자를 값 객체에 적용하는 방법을 학습합니다.

### **실습을 통해 확인할 내용**
- `ComparableSimpleValueObject<T>`의 자동 비교 기능
- `IComparable<T>` 인터페이스의 자동 구현
- 모든 비교 연산자의 자동 오버로딩
- 컬렉션에서의 자연스러운 정렬

## 왜 필요한가?

이전 단계인 `01-SimpleValueObject`에서는 값 객체의 기본적인 개념인 불변성, 값 기반 동등성, 타입 안전성을 학습했습니다. 하지만 실제 애플리케이션에서 이러한 값 객체들을 사용할 때 한 가지 문제가 발생했습니다.

**첫 번째 문제는 정렬과 비교의 어려움입니다.** 값 객체들을 컬렉션에 담아 정렬하거나 비교할 때, 별도의 비교 로직을 구현해야 했습니다. 이는 마치 알고리즘에서 정렬 알고리즘을 사용할 때마다 비교 함수를 직접 작성해야 하는 것처럼 비효율적입니다.

**두 번째 문제는 비교 연산자의 부재입니다.** 기본적인 값 객체에서는 `<`, `>`, `<=`, `>=`와 같은 비교 연산자를 사용할 수 없었습니다. 이는 마치 수학에서 숫자를 비교할 수 없게 되는 것처럼 직관적이지 않은 사용 경험을 제공했습니다.

**세 번째 문제는 컬렉션 활용의 제한입니다.** SortedSet이나 우선순위 큐와 같은 자료구조를 사용할 때 값 객체를 키로 사용할 수 없었습니다. 이는 마치 데이터베이스에서 인덱스를 사용할 수 없게 되는 것처럼 성능과 사용성에 큰 제약을 초래했습니다.

이러한 문제들을 해결하기 위해 `ComparableSimpleValueObject<T>`를 도입했습니다. 이 클래스는 값 객체의 기본 기능에 더해 자동으로 비교 기능을 제공합니다. 이는 마치 C#의 기본 타입들처럼 자연스럽게 비교하고 정렬할 수 있게 해줍니다.

## 핵심 개념

이 프로젝트의 핵심은 비교 가능한 값 객체인 `ComparableSimpleValueObject<T>`를 이해하는 것입니다. 크게 세 가지 개념으로 나눌 수 있습니다.

### 첫 번째 개념: 자동 비교 기능

`ComparableSimpleValueObject<T>`는 부모 클래스에서 자동으로 비교 기능을 상속받습니다. 별도의 구현 없이도 자연스럽게 비교할 수 있습니다.

**핵심 아이디어는 "값 객체도 숫자처럼 비교 가능"입니다.** 일반적으로 값 객체는 동등성 비교만 가능하지만, `ComparableSimpleValueObject<T>`는 크기 비교도 가능합니다.

예를 들어, `UserId` 값 객체들은 그 값에 따라 자연스럽게 크기를 비교할 수 있습니다. 이는 마치 숫자 타입처럼 `userId1 < userId2`와 같은 표현이 가능해집니다.

```csharp
// 비교 가능한 값 객체
UserId id1 = UserId.Create(123);
UserId id2 = UserId.Create(456);

// 자연스러운 비교 연산
bool isLess = id1 < id2;      // true
bool isGreater = id1 > id2;   // false
bool isEqual = id1 == id2;    // false

// 이는 마치 기본 타입처럼 작동
int num1 = 123;
int num2 = 456;
bool numLess = num1 < num2;   // true
```

이러한 자동 비교 기능은 컬렉션에서의 정렬이나 검색에서 특히 유용합니다. 값 객체를 마치 기본 타입처럼 다룰 수 있습니다.

### 두 번째 개념: `IComparable<T>` 자동 구현

`ComparableSimpleValueObject<T>`는 부모 클래스에서 `IComparable<T>` 인터페이스를 자동으로 구현합니다. 이는 .NET의 표준 비교 인터페이스입니다.

**핵심 아이디어는 ".NET 표준 준수"입니다.** `IComparable<T>`를 구현함으로써 값 객체가 .NET의 모든 정렬 알고리즘과 컬렉션에서 자연스럽게 작동할 수 있습니다.

예를 들어, `List<T>.Sort()` 메서드나 `LINQ`의 `OrderBy()` 메서드를 사용할 때 별도의 비교 함수를 제공하지 않아도 됩니다. 값 객체가 스스로 자신의 비교 방법을 알고 있습니다.

```csharp
// 자동 정렬 가능
List<UserId> userIds = new List<UserId>
{
    UserId.Create(456),
    UserId.Create(123),
    UserId.Create(789)
};

userIds.Sort(); // 별도의 비교 함수 필요 없음

// LINQ에서도 자연스럽게 사용
var sorted = userIds.OrderBy(id => id); // IComparable<T> 덕분에 가능
```

이러한 표준 인터페이스 준수는 코드의 재사용성과 호환성을 크게 향상시킵니다. 마치 플러그 앤 플레이처럼 값 객체를 기존 코드에 쉽게 통합할 수 있습니다.

### 세 번째 개념: 모든 비교 연산자 오버로딩

`ComparableSimpleValueObject<T>`는 모든 비교 연산자를 자동으로 오버로딩합니다. `<`, `<=`, `>`, `>=` 연산자를 모두 사용할 수 있습니다.

**핵심 아이디어는 "완전한 비교 지원"입니다.** `IComparable<T>`뿐만 아니라 연산자 오버로딩까지 제공하여 직관적인 비교 표현을 가능하게 합니다.

예를 들어, 조건문이나 계산식에서 자연스러운 비교 표현을 사용할 수 있습니다. 이는 수학적 표현을 코드로 그대로 옮길 수 있게 해줍니다.

```csharp
// 자연스러운 비교 표현
UserId currentId = UserId.Create(500);
UserId minId = UserId.Create(100);
UserId maxId = UserId.Create(1000);

// 범위 체크
bool isValid = minId <= currentId && currentId <= maxId;

// 이는 마치 수학처럼 읽힘
// 100 <= 500 <= 1000
```

이러한 연산자 오버로딩은 코드의 가독성을 크게 향상시킵니다. 복잡한 비교 로직도 직관적으로 표현할 수 있습니다.

## 실전 지침

### 예상 출력
```
=== 2. 비교 가능한 primitive 값 객체 - ComparableSimpleValueObject<T> ===
부모 클래스: ComparableSimpleValueObject<int>
예시: UserId (사용자 ID)

📋 특징:
   ✅ 자동으로 IComparable<UserId> 구현
   ✅ 모든 비교 연산자 오버로딩 (<, <=, >, >=)
   ✅ 명시적 타입 변환 지원
   ✅ 동등성 비교와 해시코드 자동 제공

🔍 성공 케이스:
   ✅ UserId(123): 123
   ✅ UserId(456): 456
   ✅ UserId(123): 123

📊 동등성 비교:
   123 == 456 = False
   123 == 123  =  True

📊 비교 기능 (IComparable<T>):
   123 < 456 = True
   123 <= 456 = True
   123 > 456 = False
   123 >= 456 = False

🔄 타입 변환:
   (int)123 = 123

🔢 해시코드:
   123.GetHashCode() = 123
   123.GetHashCode() = 123
   동일한 값의 해시코드가 같은가? True

❌ 실패 케이스:
   UserId(0): DomainErrors.UserId.NotPositive
   UserId(-1): DomainErrors.UserId.NotPositive

📈 정렬 데모:
   정렬된 UserId 목록:
     123
     234
     456
     567
     789

✅ 데모가 성공적으로 완료되었습니다!
```

### 핵심 구현 포인트
1. **`ComparableSimpleValueObject<T>` 상속**: 자동 비교 기능 상속
2. **`IComparable<T>` 자동 구현**: 부모 클래스에서 제공
3. **모든 비교 연산자 자동 오버로딩**: `<`, `<=`, `>`, `>=` 사용 가능
4. **컬렉션 정렬 지원**: Sort(), OrderBy() 등에서 별도 비교 함수 불필요

## 프로젝트 설명

### 프로젝트 구조
```
02-ComparableSimpleValueObject/
├── Program.cs                          # 메인 실행 파일
├── ComparableSimpleValueObject.csproj # 프로젝트 파일
├── ValueObjects/
│   └── UserId.cs                      # 사용자 ID 값 객체
└── README.md                          # 프로젝트 문서
```

### 핵심 코드

**UserId.cs - 비교 가능한 값 객체 구현**
```csharp
public sealed class UserId : ComparableSimpleValueObject<int>
{
    private UserId(int value) : base(value) { }

    public int Id => Value; // public 접근자 제공

    public static Fin<UserId> Create(int value) =>
        CreateFromValidation(Validate(value), val => new UserId(val));

    internal static UserId CreateFromValidated(int validatedValue) =>
        new UserId(validatedValue);

    public static Validation<Error, int> Validate(int value) =>
        value > 0
            ? value
            : DomainErrors.NotPositive(value);

    internal static class DomainErrors
    {
        public static Error NotPositive(int value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(UserId)}.{nameof(NotPositive)}",
                errorCurrentValue: value,
                errorMessage: $"User ID must be a positive number. Current value: '{value}'");
    }
}
```

**Program.cs - 비교 기능 데모**
```csharp
// 비교 연산자 사용
var userId1 = (UserId)id1;
var userId2 = (UserId)id2;
Console.WriteLine($"   {userId1} < {userId2} = {userId1 < userId2}");
Console.WriteLine($"   {userId1} <= {userId2} = {userId1 <= userId2}");

// 자동 정렬
userIds.Sort(); // IComparable<T> 덕분에 자동 정렬 가능

Console.WriteLine("   정렬된 UserId 목록:");
foreach (var userId in userIds)
{
    Console.WriteLine($"     {userId}");
}
```

## 한눈에 보는 정리

### 비교 표
| 구분 | `SimpleValueObject<T>` | `ComparableSimpleValueObject<T>` |
|------|---------------------|-------------------------------|
| **동등성 비교** | ✅ 지원 | ✅ 지원 |
| **비교 연산자** | ❌ 미지원 | ✅ 자동 지원 (`<`, `<=`, `>`, `>=`) |
| **`IComparable<T>`** | ❌ 미구현 | ✅ 자동 구현 |
| **컬렉션 정렬** | ❌ 수동 구현 필요 | ✅ 자동 지원 |
| **LINQ 정렬** | ❌ 별도 키 필요 | ✅ 자연스러운 정렬 |

### 장단점 표
| 장점 | 단점 |
|------|------|
| **자동 비교 기능** | 구현 복잡도 약간 증가 |
| **표준 인터페이스 준수** | 비교 의미가 항상 명확하지 않음 |
| **직관적인 연산자 사용** | 불필요한 경우 오버헤드 발생 |
| **컬렉션 호환성 우수** | 비교 로직 커스터마이징 불가 |

## FAQ

### Q1: 언제 `ComparableSimpleValueObject<T>`를 사용해야 하나요?
**A**: `ComparableSimpleValueObject<T>`는 값 객체에 자연스러운 순서 관계가 있을 때 사용합니다. 예를 들어, ID, 버전 번호, 우선순위와 같이 크기 비교가 의미 있는 도메인 개념을 표현할 때 적합합니다.

이는 마치 숫자 타입을 사용할 때처럼, `id1 < id2`와 같은 표현이 자연스러운 경우에 사용합니다. 반대로, 이메일 주소나 전화번호처럼 순서가 크게 중요하지 않은 값에는 `SimpleValueObject<T>`를 사용하는 것이 좋습니다.

이러한 선택은 마치 데이터베이스에서 인덱스를 생성할지 말지 결정하는 것과 유사합니다. 비교가 필요한 경우에만 비교 기능을 추가하여 불필요한 복잡도를 피할 수 있습니다.

### Q2: `IComparable<T>`는 어떻게 자동으로 구현되나요?
**A**: `ComparableSimpleValueObject<T>`는 부모 클래스에서 `IComparable<T>` 인터페이스를 자동으로 구현합니다. 이는 기본 타입의 값(`Value`)을 사용하여 비교를 수행합니다.

예를 들어, `int` 기반의 값 객체라면 `int`의 자연스러운 순서를 따르고, `string` 기반이라면 알파벳 순서를 따릅니다. 이는 마치 기본 타입의 비교 연산을 값 객체에 위임하는 것과 유사합니다.

이러한 자동 구현은 코드 중복을 방지하고 표준 인터페이스를 준수하여 .NET 생태계와의 호환성을 보장합니다. 이는 마치 디자인 패턴에서 템플릿 메서드 패턴을 사용하는 것처럼, 공통적인 비교 로직을 부모 클래스에서 제공하는 역할을 합니다.

### Q3: 모든 비교 연산자가 자동으로 오버로딩되나요?
**A**: 네, `ComparableSimpleValueObject<T>`는 `<`, `<=`, `>`, `>=` 연산자를 모두 자동으로 오버로딩합니다. 이는 `IComparable<T>.CompareTo()` 메서드를 기반으로 구현됩니다.

예를 들어, `userId1 < userId2`는 내부적으로 `userId1.CompareTo(userId2) < 0`으로 변환되어 실행됩니다. 이는 마치 컴파일러가 연산자를 메서드 호출로 변환하는 것과 유사한 방식입니다.

이러한 연산자 오버로딩은 코드의 가독성을 향상시키고 수학적 표현을 자연스럽게 코드로 옮길 수 있게 해줍니다. 이는 마치 연산자 오버로딩이 복소수 계산을 직관적으로 만드는 것처럼, 값 객체의 비교를 수학처럼 표현할 수 있게 합니다.

### Q4: 컬렉션 정렬 시 성능에 영향이 있나요?
**A**: `ComparableSimpleValueObject<T>`는 기본 타입의 비교를 사용하므로 성능 오버헤드가 거의 없습니다. 이는 마치 기본 타입을 직접 비교하는 것과 유사한 성능을 제공합니다.

예를 들어, `List<UserId>.Sort()`는 값 객체의 `CompareTo()` 메서드를 호출하지만, 이는 단순히 내부 `int` 값의 비교로 수행됩니다. 이는 박싱/언박싱 오버헤드 없이 효율적으로 작동합니다.

이러한 성능 특징은 값 객체가 기본 타입의 성능을 유지하면서도 타입 안전성과 의미 있는 이름을 제공할 수 있게 합니다. 이는 마치 제네릭이 타입 안전성을 제공하면서도 기본 타입의 성능을 유지하는 것과 유사합니다.

### Q5: 비교 로직을 커스터마이징할 수 있나요?
**A**: `ComparableSimpleValueObject<T>`는 기본 타입의 자연스러운 순서를 따르므로 비교 로직을 커스터마이징할 수 없습니다. 만약 특별한 비교 로직이 필요한 경우 `ValueObject`를 직접 상속받아 `IComparable<T>`를 수동으로 구현해야 합니다.

예를 들어, 버전 번호에서 "1.10"이 "1.2"보다 큰 것으로 비교해야 하는 경우, 기본 문자열 비교로는 부족합니다. 이러한 경우에는 사용자 정의 비교 로직을 구현해야 합니다.

이는 마치 정렬 알고리즘에서 기본 비교 함수 대신 사용자 정의 비교 함수를 사용하는 것과 유사합니다. 복잡한 비교 로직이 필요한 경우에는 유연성을 위해 수동 구현을 선택하는 것이 좋습니다.
