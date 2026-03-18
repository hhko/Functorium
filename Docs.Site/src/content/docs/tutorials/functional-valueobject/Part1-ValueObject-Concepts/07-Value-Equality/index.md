---
title: "값 동등성"
---
## 개요

`Denominator(5)`와 `Denominator(5)`는 같은 객체일까요? C#의 기본 동작에서는 메모리 주소가 다르므로 `false`를 반환합니다. 하지만 값 객체라면 내부 값이 같을 때 같은 객체로 취급되어야 합니다. 이 장에서는 `IEquatable<T>` 인터페이스를 통해 타입 안전한 값 기반 동등성을 구현하고, `HashSet<T>`이나 `Dictionary<TKey, TValue>` 같은 컬렉션에서도 올바르게 동작하도록 보장합니다.

## 학습 목표

1. `IEquatable<T>` 인터페이스를 활용하여 값 객체의 동등성을 올바르게 구현할 수 있습니다
2. 참조 동등성과 값 동등성의 차이점을 이해하고 적절한 상황에서 올바른 동등성을 선택할 수 있습니다
3. `HashSet<T>`, `Dictionary<TKey, TValue>` 등 해시 기반 컬렉션에서 값 기반 동등성이 올바르게 동작하는 것을 확인할 수 있습니다

## 왜 필요한가?

이전 단계인 `LinqExpression`에서는 모나드 체이닝을 통해 함수형 합성성을 구현했습니다. 하지만 값 객체들을 컬렉션에서 사용하거나 비교 연산을 수행할 때 문제가 드러납니다.

C#의 모든 참조 타입은 기본적으로 참조 동등성을 사용합니다. 값이 같아도 다른 인스턴스라면 `false`를 반환하므로, `HashSet<T>`이나 `Dictionary<TKey, TValue>`에서 값이 같은 객체들이 중복 저장되거나 키 검색이 실패합니다. 이는 `GetHashCode()`와 `Equals()`가 일관되지 않게 구현되었기 때문입니다. 또한 `Object.Equals()`를 사용하면 값 타입에서 박싱/언박싱 오버헤드가 발생하여 대량 데이터 처리 시 성능이 저하됩니다.

`IEquatable<T>` 인터페이스를 구현하면 타입 안전성, 성능 최적화, 컬렉션 호환성을 모두 확보할 수 있습니다.

## 핵심 개념

### 값 기반 동등성 (Value Equality)

두 객체의 내부 값이 같으면 동일한 것으로 취급하는 개념입니다. DDD에서 값 객체(Value Object) 패턴의 핵심 원칙이며, 동일한 입력에 대해 항상 동일한 결과를 보장합니다.

다음 코드는 참조 동등성과 값 기반 동등성의 차이를 보여줍니다.

```csharp
// 이전 방식 (참조 동등성) - 문제가 있는 방식
var a = new Denominator(5);
var b = new Denominator(5);
Console.WriteLine(a == b); // false (다른 메모리 주소)

// 개선된 방식 (값 기반 동등성) - 올바른 방식
var a = Denominator.Create(5).Match(Succ: x => x, Fail: _ => throw new Exception());
var b = Denominator.Create(5).Match(Succ: x => x, Fail: _ => throw new Exception());
Console.WriteLine(a == b); // true (같은 값)
```

### `IEquatable<T>` 인터페이스

`IEquatable<T>`는 동등성 비교에서 타입 안전성을 제공합니다. `Object.Equals(object?)`와 달리 박싱/언박싱 오버헤드가 없고, 컴파일 타임에 타입 검사가 이루어집니다.

```csharp
// IEquatable<T> 구현
public sealed class Denominator : IEquatable<Denominator>
{
    public bool Equals(Denominator? other) =>
        other is not null && _value == other._value;

    public override bool Equals(object? obj) =>
        obj is Denominator other && Equals(other);
}
```

### GetHashCode와 Equals의 일관성

`Equals()`가 `true`를 반환하는 두 객체는 반드시 같은 `GetHashCode()` 값을 가져야 합니다. 이 규칙이 깨지면 `HashSet<T>`이나 `Dictionary<TKey, TValue>` 같은 해시 기반 컬렉션이 올바르게 동작하지 않습니다.

```csharp
// 일관된 구현
public override int GetHashCode() => _value.GetHashCode();

public bool Equals(Denominator? other) =>
    other is not null && _value == other._value;
```

## 실전 지침

### 예상 출력
```
=== 값 객체의 동등성 ===

=== 기본 동등성 테스트 ===
a = 5, b = 5, c = 10
a == b: True
a == c: False
a.Equals(b): True
a.Equals(c): False

=== 참조 동등성(ReferenceEquals) vs 값 동등성(Equals) ===
a = 5, b = 5
ReferenceEquals(a, b): False
a == b: True
a.Equals(b): True

=== null과의 동등성 테스트 ===
a = 5
a == null: False
null == a: False
a.Equals(null): False
null == null: True

=== 해시 코드 테스트 ===
a = 5, b = 5, c = 10
a.GetHashCode(): 5
b.GetHashCode(): 5
c.GetHashCode(): 10
a.GetHashCode() == b.GetHashCode(): True
a.GetHashCode() == c.GetHashCode(): False

=== 컬렉션에서의 동등성 테스트 ===
원본 값들: [5, 10, 5, 15, 10]
Denominator 값들: [5, 10, 5, 15, 10]
HashSet (중복 제거): [5, 10, 15]
Dictionary 키 개수: 3
키 5로 검색된 값: Value_5

=== 성능 비교 테스트(1,000,000개) ===
IEquatable<T> 사용: 4ms
Object.Equals 사용: 8ms
성능 차이: 4ms

=== 컬렉션에서의 동등성 테스트 ===
원본 값들: [5, 10, 5, 15, 10]
Denominator 값들: [5, 10, 5, 15, 10]
HashSet (중복 제거): [5, 10, 15]
Dictionary 키 개수: 3
키 5로 검색된 값: Value_5

=== 성능 비교 테스트(1,000,000개) ===
IEquatable<T> 사용: 4ms
Object.Equals 사용: 8ms
성능 차이: 4ms
```

### 핵심 구현 포인트

다음 다섯 가지 요소를 함께 구현해야 값 기반 동등성이 완전하게 동작한다.

1. **`IEquatable<T>` 인터페이스 구현**: 타입 안전한 `Equals(Denominator? other)` 메서드
2. **Object.Equals 오버라이드**: 참조 타입과의 호환성을 위한 `Equals(object? obj)` 메서드
3. **GetHashCode 오버라이드**: 해시 기반 컬렉션에서의 올바른 동작을 위한 일관된 해시 코드 생성
4. **연산자 오버로딩**: `==`와 `!=` 연산자를 통한 자연스러운 비교 구문 지원
5. **null 안전성**: null 참조와의 비교에서 예외 발생 방지

## 프로젝트 설명

### 프로젝트 구조
```
ValueEquality/
├── ValueObjects/
│   └── Denominator.cs          # 값 기반 동등성이 구현된 값 객체
├── Program.cs                  # 메인 실행 파일
├── ValueEquality.csproj        # 프로젝트 파일
└── README.md                   # 프로젝트 문서
```

### 핵심 코드

#### Denominator 값 객체 (값 기반 동등성 구현)
```csharp
public sealed class Denominator : IEquatable<Denominator>
{
    private readonly int _value;

    // IEquatable<T> 구현 - 타입 안전한 동등성 비교
    public bool Equals(Denominator? other) =>
        other is not null && _value == other._value;

    // Object.Equals 오버라이드 - 참조 동등성이 아닌 값 동등성 사용
    public override bool Equals(object? obj) =>
        obj is Denominator other && Equals(other);

    // 동등성 연산자 오버로딩
    public static bool operator ==(Denominator? left, Denominator? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Denominator? left, Denominator? right) =>
        !(left == right);

    // GetHashCode 오버라이드 - 값 기반 해시 코드 생성
    public override int GetHashCode() => _value.GetHashCode();
}
```

#### LINQ 표현식을 활용한 테스트 (모나드 체이닝)
```csharp
public static void DemonstrateBasicEquality()
{
    var result = from a in Denominator.Create(5)
                 from b in Denominator.Create(5)
                 from c in Denominator.Create(10)
                 select (a, b, c);

    result.Match(
        Succ: values =>
        {
            var (a, b, c) = values;
            Console.WriteLine($"a == b: {a == b}"); // true (값이 같음)
            Console.WriteLine($"a == c: {a == c}"); // false (값이 다름)
        },
        Fail: error => Console.WriteLine($"생성 실패: {error}")
    );
}
```

## 한눈에 보는 정리

다음 표는 참조 동등성과 값 기반 동등성의 차이를 비교합니다.

| 구분 | 참조 동등성 | 값 기반 동등성 |
|------|-------------|----------------|
| **비교 기준** | 메모리 주소 | 내부 값 |
| **동일한 값, 다른 인스턴스** | `false` | `true` |
| **컬렉션 동작** | 예측 불가능 | 올바른 동작 |
| **성능** | 박싱/언박싱 오버헤드 | 최적화됨 |
| **타입 안전성** | 부족 | 보장됨 |

값 기반 동등성을 완전히 구현하려면 다음 메서드들을 모두 구현해야 합니다.

| 메서드 | 구현 여부 | 목적 |
|--------|-----------|------|
| **`IEquatable<T>`.Equals** | ✅ | 타입 안전한 동등성 비교 |
| **Object.Equals** | ✅ | 참조 타입과의 호환성 |
| **GetHashCode** | ✅ | 해시 기반 컬렉션 지원 |
| **== 연산자** | ✅ | 자연스러운 비교 구문 |
| **!= 연산자** | ✅ | 자연스러운 비교 구문 |

## FAQ

### Q1: 왜 GetHashCode도 오버라이드해야 하나요?
**A**: `Equals`가 `true`인 두 객체는 반드시 같은 해시 코드를 가져야 한다는 계약(Contract)이 있습니다. 이 규칙이 깨지면 `HashSet<T>`, `Dictionary<TKey, TValue>` 등 해시 기반 컬렉션에서 검색 실패나 중복 저장이 발생합니다.

```csharp
// 잘못된 구현 - 일관성 없음
public override int GetHashCode() => 1; // 항상 같은 해시 코드
public bool Equals(Denominator? other) => _value == other?._value;

// 올바른 구현 - 일관성 있음
public override int GetHashCode() => _value.GetHashCode();
public bool Equals(Denominator? other) => _value == other?._value;
```

### Q2: 참조 동등성과 값 동등성 중 어떤 것을 사용해야 하나요?
**A**: 값 객체(Value Object)는 값 동등성을, 엔티티(Entity)는 참조 동등성(또는 식별자 기반 동등성)을 사용합니다. 값 객체는 불변성과 값 기반 비교가 특징이고, 엔티티는 고유한 식별자로 구분됩니다.

```csharp
// 값 객체 - 값 기반 동등성
public class Money : IEquatable<Money>
{
    public bool Equals(Money? other) =>
        Amount == other?.Amount && Currency == other?.Currency;
}

// 엔티티 - 참조 동등성 (기본 동작)
public class User
{
    public Guid Id { get; set; }
    // 참조 동등성 사용 (기본 동작)
}
```

### Q3: null 체크는 왜 필요한가요?
**A**: null 참조와의 비교에서 `NullReferenceException`을 방지하기 위해서입니다. `==` 연산자에서 `ReferenceEquals`로 양쪽 다 null인 경우를 먼저 처리하고, 한쪽만 null인 경우 `false`를 반환합니다.

```csharp
// 안전한 null 체크
public static bool operator ==(Denominator? left, Denominator? right)
{
    if (ReferenceEquals(left, right)) return true;  // 둘 다 null인 경우
    if (left is null || right is null) return false; // 하나만 null인 경우
    return left.Equals(right); // 둘 다 null이 아닌 경우
}
```

값 동등성이 확보되면, 다음 장에서는 값 객체의 순서 비교를 구현하여 정렬과 범위 검증이 가능한 비교 가능성(Comparability)을 다룹니다.
