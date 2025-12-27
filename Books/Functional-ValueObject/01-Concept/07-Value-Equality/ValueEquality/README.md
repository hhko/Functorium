# 07. 값 기반 동등성 비교하기

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

이 프로젝트는 값 객체의 핵심 특성인 **값 기반 동등성(Value Equality)**을 구현하고, 참조 동등성과의 차이점을 학습합니다. `IEquatable<T>` 인터페이스를 통해 타입 안전한 동등성 비교를 구현하고, 컬렉션에서의 올바른 동작을 보장합니다.

## 학습 목표

### **핵심 학습 목표**
1. **값 기반 동등성 구현**: `IEquatable<T>` 인터페이스를 활용하여 값 객체의 동등성을 올바르게 구현할 수 있다
2. **참조 동등성 vs 값 동등성 구분**: 두 동등성 개념의 차이점을 이해하고 적절한 상황에서 올바른 동등성을 선택할 수 있다
3. **컬렉션에서의 동등성 활용**: `HashSet<T>`, `Dictionary<TKey, TValue>` 등에서 값 기반 동등성이 올바르게 동작하는 것을 확인할 수 있다

### **실습을 통해 확인할 내용**
- **기본 동등성**: `a == b: True`, `a.Equals(b): True` (값이 같은 경우)
- **참조 vs 값 동등성**: `ReferenceEquals(a, b): False`, `a == b: True` (다른 객체, 같은 값)
- **컬렉션 동작**: `HashSet`에서 중복 제거, `Dictionary`에서 키 검색이 값 기반으로 동작
- **성능 최적화**: `IEquatable<T>` 사용 시 박싱/언박싱 오버헤드 방지로 성능 향상

## 왜 필요한가?

이전 단계인 `LinqExpression`에서는 **모나드 체이닝(Monadic Chaining)**을 통해 함수형 프로그래밍의 합성성을 구현했습니다. 하지만 실제로 값 객체들을 **컬렉션에서 사용**하거나 **비교 연산**을 수행하려고 할 때 몇 가지 문제가 발생했습니다.

**첫 번째 문제는 참조 동등성(Reference Equality)의 한계입니다.** 마치 **데이터베이스의 기본키(Primary Key)와 비즈니스 키(Business Key)**의 차이처럼, 객체의 메모리 주소가 아닌 실제 값으로 비교해야 하는 상황이 빈번합니다. 기본적으로 C#의 모든 객체는 참조 동등성을 사용하므로, 값이 같아도 다른 객체 인스턴스라면 `false`를 반환합니다.

**두 번째 문제는 컬렉션에서의 예측 불가능한 동작입니다.** 마치 **해시 테이블(Hash Table)의 충돌 처리**처럼, `HashSet<T>`나 `Dictionary<TKey, TValue>`에서 값이 같은 객체들이 중복으로 저장되거나 키 검색이 실패하는 문제가 발생합니다. 이는 `GetHashCode()`와 `Equals()` 메서드가 일관되지 않게 구현되었기 때문입니다.

**세 번째 문제는 성능상의 비효율성입니다.** 마치 **타입 캐스팅의 박싱/언박싱 오버헤드**처럼, `Object.Equals()`를 사용할 때마다 값 타입이 참조 타입으로 변환되는 과정에서 성능 저하가 발생합니다. 특히 대량의 데이터를 처리할 때 이 오버헤드는 무시할 수 없습니다.

이러한 문제들을 해결하기 위해 **값 기반 동등성(Value Equality)**을 도입했습니다. `IEquatable<T>` 인터페이스를 구현하면 **타입 안전성**, **성능 최적화**, **컬렉션 호환성**을 모두 얻을 수 있습니다.

## 핵심 개념

이 프로젝트의 핵심은 크게 3가지 개념으로 나눌 수 있습니다. 각각이 어떻게 작동하는지 쉽게 설명해드리겠습니다.

### 값 기반 동등성 (Value Equality)

값 기반 동등성은 마치 **수학의 등식(Equality)**처럼, 두 객체의 내부 값이 같으면 동일한 것으로 취급하는 개념입니다. 이는 **도메인 주도 설계(DDD)의 값 객체(Value Object)** 패턴의 핵심 원칙입니다.

**핵심 아이디어는 "값이 같으면 동일한 객체"입니다.** 마치 **함수형 프로그래밍의 참조 투명성(Referential Transparency)**처럼, 동일한 입력에 대해 항상 동일한 결과를 보장합니다.

예를 들어, `Denominator(5)`와 `Denominator(5)`를 생각해보세요. 이전 방식에서는 **참조 동등성(Reference Equality)**을 사용하여 메모리 주소가 다르면 다른 객체로 취급했습니다. 하지만 값 객체의 관점에서는 내부 값이 같으므로 동일한 객체여야 합니다.

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

이 방식의 장점은 **예측 가능한 동작**과 **도메인 모델의 정확성**을 보장한다는 것입니다.

### `IEquatable<T>` 인터페이스

`IEquatable<T>`는 마치 **제네릭 타입 시스템(Generic Type System)**의 타입 안전성을 보장하는 것처럼, 동등성 비교에서 타입 안전성을 제공하는 인터페이스입니다. 이는 **객체지향 프로그래밍의 다형성(Polymorphism)**과 **인터페이스 분리 원칙(Interface Segregation Principle)**을 활용한 설계입니다.

**핵심 아이디어는 "타입 안전한 동등성 비교"입니다.** 마치 **컴파일 타임 타입 검사(Compile-time Type Checking)**처럼, 런타임 오류를 방지하고 성능을 최적화합니다.

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

이 방식의 장점은 **박싱/언박싱 오버헤드 방지**와 **타입 안전성 보장**입니다.

### GetHashCode와 Equals의 일관성

`GetHashCode()`와 `Equals()`는 마치 **해시 테이블(Hash Table)의 충돌 처리**처럼, 항상 함께 구현되어야 하는 쌍을 이루는 메서드입니다. 이는 **데이터 구조의 일관성(Data Structure Consistency)**과 **알고리즘의 정확성(Algorithm Correctness)**을 보장하는 원칙입니다.

**핵심 아이디어는 "해시 코드와 동등성의 일관성"입니다.** 마치 **분산 시스템의 일관성(Consistency)**처럼, 두 메서드가 항상 같은 결과를 반환해야 합니다.

```csharp
// 일관된 구현
public override int GetHashCode() => _value.GetHashCode();

public bool Equals(Denominator? other) =>
    other is not null && _value == other._value;
```

이 방식의 장점은 **컬렉션에서의 올바른 동작**과 **성능 최적화**입니다.

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
1. **`IEquatable<T>` 인터페이스 구현**: 타입 안전한 동등성 비교를 위한 `Equals(Denominator? other)` 메서드 구현
2. **Object.Equals 오버라이드**: 참조 타입과의 호환성을 위한 `Equals(object? obj)` 메서드 구현
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

### 비교 표
| 구분 | 참조 동등성 | 값 기반 동등성 |
|------|-------------|----------------|
| **비교 기준** | 메모리 주소 | 내부 값 |
| **동일한 값, 다른 인스턴스** | `false` | `true` |
| **컬렉션 동작** | 예측 불가능 | 올바른 동작 |
| **성능** | 박싱/언박싱 오버헤드 | 최적화됨 |
| **타입 안전성** | 부족 | 보장됨 |

### 구현 체크리스트
| 메서드 | 구현 여부 | 목적 |
|--------|-----------|------|
| **`IEquatable<T>`.Equals** | ✅ | 타입 안전한 동등성 비교 |
| **Object.Equals** | ✅ | 참조 타입과의 호환성 |
| **GetHashCode** | ✅ | 해시 기반 컬렉션 지원 |
| **== 연산자** | ✅ | 자연스러운 비교 구문 |
| **!= 연산자** | ✅ | 자연스러운 비교 구문 |

### 장단점 표
| 장점 | 단점 |
|------|------|
| **타입 안전성** | **구현 복잡성** |
| **성능 최적화** | **메모리 사용량** |
| **컬렉션 호환성** | **학습 곡선** |
| **예측 가능한 동작** | **디버깅 어려움** |

## FAQ

### Q1: 왜 GetHashCode도 오버라이드해야 하나요?
**A**: `Equals`와 `GetHashCode`는 항상 함께 오버라이드해야 합니다. 이는 마치 데이터베이스의 복합 인덱스처럼, 해시 기반 컬렉션에서 일관된 동작을 보장하기 때문입니다.

**해시 테이블의 일관성**은 `HashSet<T>`, `Dictionary<TKey, TValue>` 등에서 올바른 동작을 위해 필요합니다. 이는 마치 데이터베이스의 해시 인덱스가 일관된 해시 함수를 사용해야 하는 것처럼, 해시 기반 컬렉션에서도 일관된 해시 코드가 필요합니다.

**성능 최적화**는 해시 코드를 통한 빠른 검색과 충돌 처리를 가능하게 합니다. 이는 마치 데이터베이스의 B-tree 인덱스가 빠른 검색을 제공하는 것처럼, 해시 코드를 통해 O(1) 시간 복잡도의 검색을 제공합니다.

**계약(Contract) 준수**는 두 메서드가 항상 같은 결과를 반환해야 하는 원칙입니다. 이는 마치 데이터베이스의 트랜잭션 ACID 속성처럼, 객체의 동등성과 해시 코드가 일관된 관계를 유지해야 합니다.

**실제 예시:**
```csharp
// 잘못된 구현 - 일관성 없음
public override int GetHashCode() => 1; // 항상 같은 해시 코드
public bool Equals(Denominator? other) => _value == other?._value;

// 올바른 구현 - 일관성 있음
public override int GetHashCode() => _value.GetHashCode();
public bool Equals(Denominator? other) => _value == other?._value;
```

### Q2: 참조 동등성과 값 동등성 중 어떤 것을 사용해야 하나요?
**A**: 값 객체는 값 동등성을, 엔티티는 참조 동등성을 사용합니다. 이는 마치 데이터베이스에서 기본키와 외래키의 관계처럼, 도메인 모델의 특성에 따라 적절한 동등성을 선택해야 합니다.

**값 객체(Value Object)**는 불변성, 값 기반 동등성, 도메인 개념 표현을 특징으로 합니다. 이는 마치 데이터베이스의 값 타입처럼, 값이 같으면 동일한 것으로 취급되어야 하는 개념입니다.

**엔티티(Entity)**는 식별성, 참조 동등성, 생명주기 관리를 특징으로 합니다. 이는 마치 데이터베이스의 테이블 레코드처럼, 고유한 식별자를 통해 구분되는 개념입니다.

**도메인 주도 설계(DDD)**는 도메인 모델의 특성에 따른 적절한 동등성 선택을 강조합니다. 이는 마치 데이터베이스 설계에서 엔티티와 값 타입을 구분하는 것처럼, 도메인의 본질적 특성에 따라 동등성 전략을 결정해야 합니다.

**실제 예시:**
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
**A**: null 참조와의 비교에서 `NullReferenceException`을 방지하고, null은 null과만 같다고 정의하기 때문입니다. 이는 마치 데이터베이스에서 NULL 값 처리를 위한 방어적 프로그래밍처럼, 예외 상황에 대한 안전한 처리가 필요합니다.

**방어적 프로그래밍**은 예외 상황에 대한 안전한 처리를 제공합니다. 이는 마치 데이터베이스의 CHECK 제약 조건처럼, 잘못된 데이터나 예외 상황을 미리 방지하는 역할을 합니다.

**일관된 동작**은 null과의 비교에서 예측 가능한 결과를 보장합니다. 이는 마치 데이터베이스에서 NULL과의 비교가 일관된 결과를 반환하는 것처럼, null과의 비교에서도 항상 동일한 결과를 보장해야 합니다.

**도메인 규칙**은 null은 null과만 같다는 명확한 정의를 제공합니다. 이는 마치 데이터베이스의 NULL 처리 규칙처럼, null 값에 대한 명확하고 일관된 처리 방식을 정의해야 합니다.

**실제 예시:**
```csharp
// 안전한 null 체크
public static bool operator ==(Denominator? left, Denominator? right)
{
    if (ReferenceEquals(left, right)) return true;  // 둘 다 null인 경우
    if (left is null || right is null) return false; // 하나만 null인 경우
    return left.Equals(right); // 둘 다 null이 아닌 경우
}
```
