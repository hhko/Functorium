---
title: "값 비교"
---
## 개요

두 `Denominator` 객체가 같은지는 판단할 수 있게 되었습니다. 그런데 어떤 분모가 더 큰지, 분모 목록을 오름차순으로 정렬하려면 어떻게 해야 할까요? 이 장에서는 `IComparable<T>`를 통해 값 객체에 순서 비교 기능을 부여하고, `IEqualityComparer<T>`를 통해 대소문자 무시 같은 커스텀 비교 전략을 구현합니다.

## 학습 목표

1. `IComparable<T>` 인터페이스를 구현하여 값 객체에 정렬과 범위 검색 기능을 추가할 수 있습니다
2. `<`, `>`, `<=`, `>=` 비교 연산자를 `CompareTo` 기반으로 일관되게 오버로딩할 수 있습니다
3. `IEqualityComparer<T>`를 활용하여 기본 동등성과 다른 커스텀 비교 전략을 별도 클래스로 분리할 수 있습니다

## 왜 필요한가?

이전 단계인 `ValueEquality`에서는 값 객체의 동등성을 구현하여 두 객체가 같은지 다른지만 판단할 수 있었습니다. 하지만 실제 애플리케이션에서는 순서 비교와 컬렉션 최적화가 함께 필요합니다.

`IComparable<T>`를 구현하지 않으면 `List<T>.Sort()`나 `Array.BinarySearch()` 같은 정렬/검색 API를 사용할 수 없습니다. 또한 기본 동등성 비교만으로는 대소문자 무시, 특수 규칙 등 다양한 비교 요구사항을 충족할 수 없습니다. `IEqualityComparer<T>`를 도입하면 값 객체 자체를 수정하지 않고 비교 전략을 외부에서 주입할 수 있습니다.

## 핵심 개념

### `IComparable<T>` 인터페이스

`IComparable<T>`는 값 객체에 순서 비교(Ordering Comparison) 기능을 제공합니다. `CompareTo` 메서드는 두 값을 비교하여 -1(작음), 0(같음), 1(큼) 중 하나를 반환합니다.

이전에는 두 분모가 같은지만 확인할 수 있었지만, `IComparable<T>`를 구현하면 크기 비교와 컬렉션 정렬이 가능해집니다.

```csharp
// 이전 방식 (순서 비교 불가능)
var a = Denominator.Create(5);
var b = Denominator.Create(10);
// a < b 같은 비교가 불가능했음

// 개선된 방식 (IComparable<T> 구현)
public int CompareTo(Denominator? other)
{
    if (other is null) return 1;
    return _value.CompareTo(other._value);
}

// 이제 자연스러운 비교가 가능
Console.WriteLine($"a < b: {a < b}"); // True
Console.WriteLine($"a.CompareTo(b): {a.CompareTo(b)}"); // -1
```

`List<T>.Sort()`, `Array.BinarySearch()`, `Min()`, `Max()` 등의 메서드들이 자동으로 `CompareTo`를 사용합니다.

### 비교 연산자 오버로딩

`CompareTo` 메서드를 기반으로 `<`, `>`, `<=`, `>=` 연산자를 구현하면 `a < b` 같은 수학적 표현을 자연스럽게 사용할 수 있습니다.

```csharp
// CompareTo 기반 연산자 구현
public static bool operator <(Denominator? left, Denominator? right) =>
    left is null ? right is not null : left.CompareTo(right) < 0;

public static bool operator >(Denominator? left, Denominator? right) =>
    left is not null && left.CompareTo(right) > 0;

// 자연스러운 비교 표현
if (denominator1 < denominator2)
{
    Console.WriteLine("첫 번째 분모가 더 작습니다");
}
```

### `IEqualityComparer<T>` 인터페이스

`IEqualityComparer<T>`는 값 객체의 기본 `Equals` 메서드를 변경하지 않고 커스텀 비교 전략을 제공합니다. 예를 들어 `EmailAddress`에서 대소문자를 무시한 비교가 필요할 때 별도의 비교자 클래스로 분리할 수 있습니다.

```csharp
// 기본 동등성 비교 (대소문자 구분)
var email1 = EmailAddress.Create("User@Example.com");
var email2 = EmailAddress.Create("user@example.com");
Console.WriteLine($"기본 비교: {email1 == email2}"); // False

// 커스텀 비교자 (대소문자 무시)
public class EmailAddressCaseInsensitiveComparer : IEqualityComparer<EmailAddress>
{
    public bool Equals(EmailAddress? x, EmailAddress? y)
    {
        if (x is null && y is null) return true;
        if (x is null || y is null) return false;

        string xValue = (string)x;
        string yValue = (string)y;
        return xValue.Equals(yValue, StringComparison.OrdinalIgnoreCase);
    }
}

// 컬렉션에서 커스텀 비교자 사용
var emails = new[] { email1, email2 };
var uniqueEmails = emails.Distinct(new EmailAddressCaseInsensitiveComparer());
```

하나의 값 객체에 대해 여러 비교 전략을 동시에 제공할 수 있어, 다양한 비즈니스 요구사항에 대응할 수 있습니다.

## 실전 지침

### 예상 출력
```
=== 값 객체의 비교 가능성 ===

=== 기본 비교 기능 테스트 ===
a = 5, b = 10, c = 5

CompareTo 테스트:
a.CompareTo(b) = -1
b.CompareTo(a) = 1
a.CompareTo(c) = 0

연산자 테스트:
a < b: True
a <= b: True
a > b: False
a >= b: False
a == c: True
a != b: True

=== null 비교 테스트 ===
a = 5, nullValue = null

null과의 비교:
a.CompareTo(null) = 1
a > null: True
a >= null: True
a < null: False
a <= null: False
a == null: False
a != null: True

null과 null 비교:
null == null: True
null != null: False

=== 정렬 테스트 ===
정렬 전:
10 3 7 1 15
오름차순 정렬 후:
1 3 7 10 15
내림차순 정렬 후:
15 10 7 3 1

=== 컬렉션에서의 비교 테스트 ===
원본 리스트:
5 2 8 1 3
최소값: 1
최대값: 8
범위: 7

=== 성능 비교 테스트 ===
10,000개 Denominator 정렬 시간: 1ms
이진 검색 시간: 0ms
찾은 인덱스: 4999

=== 경계값 테스트 ===
최소값: -2147483648
최대값: 2147483647
음수값: -100
양수값: 100

음수와 양수 비교:
음수 < 양수: True
음수 > 양수: False

최소값과 최대값 비교:
최소값 < 최대값: True
최소값 > 최대값: False


==================================================

=== IEqualityComparer<T> 사용 예제 테스트 ===

=== 기본 IEqualityComparer<T> 테스트 ===
email1 = user@example.com
email2 = user@example.com
email3 = admin@example.com

기본 비교 테스트:
comparer.Equals(email1, email2) = True
comparer.Equals(email1, email3) = False
comparer.Equals(email1, null) = False
comparer.Equals(null, null) = True

해시 코드 테스트:
email1.GetHashCode() = 650831702
email2.GetHashCode() = 650831702
email3.GetHashCode() = -1837482715
같은 값의 해시 코드가 같은가? True

=== 컬렉션에서 IEqualityComparer<T> 사용 테스트 ===
원본 이메일 리스트:
user1@example.com user2@example.com user1@example.com admin@example.com user2@example.com test@example.com
기본 Distinct 후 (중복 제거):
user1@example.com user2@example.com admin@example.com test@example.com
HashSet 사용 후 (중복 제거):
user1@example.com user2@example.com admin@example.com test@example.com
커스텀 EqualityComparer 사용 후:
user1@example.com user2@example.com admin@example.com test@example.com

=== 대소문자 무시 비교자 테스트 ===
원본 이메일 리스트 (대소문자 혼재):
user@example.com user@example.com admin@example.com admin@example.com test@example.com test@example.com
대소문자 구분 비교자 사용 후:
user@example.com admin@example.com test@example.com
대소문자 무시 비교자 사용 후:
user@example.com admin@example.com test@example.com

=== Dictionary에서 IEqualityComparer<T> 사용 테스트 ===
기본 Dictionary 결과:
  user1@example.com -> User One
  user2@example.com -> User Two
  admin@example.com -> Admin
커스텀 EqualityComparer 사용 Dictionary 결과:
  user1@example.com -> User One
  user2@example.com -> User Two
  admin@example.com -> Admin

=== 성능 비교 테스트 ===
기본 Distinct 성능: 0ms
커스텀 EqualityComparer 성능: 0ms
HashSet 성능: 0ms
결과 개수: 10000 (기본), 10000 (커스텀), 10000 (HashSet)
대소문자 무시 비교자 사용 후:
user@example.com admin@example.com test@example.com
```

### 핵심 구현 포인트
1. **`IComparable<T>` 구현**: `CompareTo` 메서드에서 null 처리와 값 비교 로직을 명확히 구현
2. **비교 연산자 오버로딩**: `CompareTo` 메서드를 기반으로 `<`, `>`, `<=`, `>=` 연산자를 일관되게 구현
3. **`IEqualityComparer<T>` 전략 패턴**: 기본 동등성과 다른 비교 전략을 별도 클래스로 분리하여 유연성 확보

## 프로젝트 설명

### 프로젝트 구조
```
ValueComparability/                             # 메인 프로젝트
├── Program.cs                                  # 메인 실행 파일
├── ValueObjects/                               # 값 객체 구현
│   ├── Denominator.cs                          # IComparable<T> 구현 예제
│   └── EmailAddress.cs                         # IEquatable<T>만 구현 예제
├── Comparers/                                  # 커스텀 비교자 구현
│   ├── EmailAddressComparer.cs                 # 기본 비교자
│   └── EmailAddressCaseInsensitiveComparer.cs  # 대소문자 무시 비교자
├── Tests/                                      # 테스트 코드
│   ├── ComparabilityTests.cs                   # IComparable<T> 테스트
│   └── EqualityComparerTests.cs                # IEqualityComparer<T> 테스트
├── ValueComparability.csproj                   # 프로젝트 파일
└── README.md                                   # 메인 문서
```

### 핵심 코드

#### Denominator - `IComparable<T>` 구현
```csharp
public sealed class Denominator : IEquatable<Denominator>, IComparable<Denominator>
{
    private readonly int _value;

    // IComparable<T> 구현 - 순서 비교
    public int CompareTo(Denominator? other)
    {
        if (other is null) return 1;  // null보다는 모든 값이 큼
        return _value.CompareTo(other._value);
    }

    // 비교 연산자 오버로딩
    public static bool operator <(Denominator? left, Denominator? right) =>
        left is null ? right is not null : left.CompareTo(right) < 0;

    public static bool operator >(Denominator? left, Denominator? right) =>
        left is not null && left.CompareTo(right) > 0;

    public static bool operator <=(Denominator? left, Denominator? right) =>
        left is null || left.CompareTo(right) <= 0;

    public static bool operator >=(Denominator? left, Denominator? right) =>
        left is null ? right is null : left.CompareTo(right) >= 0;
}
```

#### EmailAddressCaseInsensitiveComparer - 커스텀 비교 전략
```csharp
public class EmailAddressCaseInsensitiveComparer : IEqualityComparer<EmailAddress>
{
    public bool Equals(EmailAddress? x, EmailAddress? y)
    {
        if (x is null && y is null) return true;
        if (x is null || y is null) return false;

        // 명시적 캐스팅을 통한 문자열 비교
        string xValue = (string)x;
        string yValue = (string)y;
        return xValue.Equals(yValue, StringComparison.OrdinalIgnoreCase);
    }

    public int GetHashCode(EmailAddress obj)
    {
        if (obj is null) return 0;
        string value = (string)obj;
        return value.ToLowerInvariant().GetHashCode();
    }
}
```

#### LINQ 표현식을 활용한 테스트 코드
```csharp
// 여러 값 객체를 안전하게 생성하고 비교
var result = from a in Denominator.Create(5)
             from b in Denominator.Create(10)
             from c in Denominator.Create(5)
             select (a, b, c);

result.Match(
    Succ: values =>
    {
        var (a, b, c) = values;
        Console.WriteLine($"a < b: {a < b}");   // True
        Console.WriteLine($"a == c: {a == c}"); // True
    },
    Fail: error => Console.WriteLine($"생성 실패: {error.Message}")
);
```

## 한눈에 보는 정리

다음 표는 두 비교 인터페이스의 목적과 사용 시나리오를 비교합니다.

| 구분 | `IComparable<T>` | `IEqualityComparer<T>` |
|------|----------------|---------------------|
| **목적** | 순서 비교 (정렬, 검색) | 커스텀 동등성 비교 |
| **구현 위치** | 값 객체 내부 | 별도 비교자 클래스 |
| **주요 메서드** | `CompareTo(T other)` | `Equals(T x, T y)`, `GetHashCode(T obj)` |
| **반환 타입** | `int` (-1, 0, 1) | `bool` (true, false) |
| **사용 시나리오** | 정렬, Min/Max, BinarySearch | Distinct, HashSet, Dictionary |
| **유연성** | 고정된 비교 로직 | 동적 비교 전략 교체 |

모든 값 객체에 두 인터페이스가 다 필요한 것은 아닙니다. 도메인에 의미 있는 비교만 구현합니다.

| 값 객체 | `IEquatable<T>` | `IComparable<T>` | `IEqualityComparer<T>` |
|---------|---------------|----------------|---------------------|
| **Denominator** | ✅ (기본 동등성) | ✅ (수치 비교) | -- (불필요) |
| **EmailAddress** | ✅ (기본 동등성) | -- (의미 없음) | ✅ (대소문자 무시) |

## FAQ

### Q1: `IComparable<T>`와 `IEquatable<T>`의 차이점은 무엇인가요?
**A**: `IEquatable<T>`는 두 객체가 같은지 다른지만 판단하는 동등성 비교(`bool` 반환)를 제공하고, `IComparable<T>`는 크기 관계를 판단하는 순서 비교(`int` 반환, -1/0/1)를 제공합니다.

```csharp
var a = Denominator.Create(5);
var b = Denominator.Create(10);

// IEquatable<T> - 동등성 비교
Console.WriteLine($"a == b: {a == b}"); // False

// IComparable<T> - 순서 비교
Console.WriteLine($"a < b: {a < b}"); // True
Console.WriteLine($"a.CompareTo(b): {a.CompareTo(b)}"); // -1
```

### Q2: 왜 EmailAddress는 `IComparable<T>`를 구현하지 않았나요?
**A**: 이메일 주소에는 의미 있는 순서가 없기 때문입니다. `Denominator`는 수치적 크기가 비즈니스 의미를 가지지만(5 < 10), 이메일 주소의 문자열 순서는 비즈니스 로직에서 의미가 없습니다. 도메인에 실제로 필요한 비교만 구현해야 합니다.

```csharp
// Denominator - 순서가 의미 있음
var small = Denominator.Create(5);
var large = Denominator.Create(10);
Console.WriteLine($"small < large: {small < large}"); // True

// EmailAddress - 순서가 의미 없음
var email1 = EmailAddress.Create("admin@company.com");
var email2 = EmailAddress.Create("user@company.com");
// email1 < email2 같은 비교는 비즈니스적으로 의미 없음
```

### Q3: `IEqualityComparer<T>`를 사용하는 이유는 무엇인가요?
**A**: 값 객체의 기본 `Equals` 메서드를 변경하지 않고, 상황에 따라 다른 비교 로직을 적용하기 위해서입니다. 예를 들어 이메일 주소의 기본 비교는 대소문자를 구분하지만, 중복 제거 시에는 대소문자를 무시해야 할 수 있습니다.

```csharp
var emails = new[] {
    EmailAddress.Create("User@Example.com"),
    EmailAddress.Create("user@example.com"),
    EmailAddress.Create("ADMIN@EXAMPLE.COM")
};

// 기본 비교 (대소문자 구분)
var distinct1 = emails.Distinct().ToList(); // 3개 (모두 다름)

// 커스텀 비교 (대소문자 무시)
var comparer = new EmailAddressCaseInsensitiveComparer();
var distinct2 = emails.Distinct(comparer).ToList(); // 2개 (중복 제거)
```

동등성과 비교 가능성을 모두 갖춘 값 객체가 완성되었습니다. 다음 장에서는 값 객체의 생성(Create)과 검증(Validate) 책임을 분리하여 단일 책임 원칙을 적용하는 방법을 다룹니다.
