---
title: "값 객체 사양"
---

Functorium 프레임워크가 제공하는 값 객체(Value Object) 타입의 공개 API를 정의합니다. 설계 의도와 실습은 [값 객체 가이드](../guides/domain/05a-value-objects)를, 여기서는 각 타입의 시그니처, 계약, 동작 규칙을 확인하십시오.

## 요약

### 주요 타입

| 타입 | 네임스페이스 | 설명 |
|------|-------------|------|
| `IValueObject` | `Functorium.Domains.ValueObjects` | 값 객체 마커 인터페이스 |
| `AbstractValueObject` | `Functorium.Domains.ValueObjects` | 값 기반 동등성 비교 기반 클래스 |
| `ValueObject` | `Functorium.Domains.ValueObjects` | 복합 값 객체 기반 클래스 |
| `SimpleValueObject<T>` | `Functorium.Domains.ValueObjects` | 단일 값 래핑 기반 클래스 |
| `ComparableValueObject` | `Functorium.Domains.ValueObjects` | 비교 가능한 복합 값 객체 기반 클래스 |
| `ComparableSimpleValueObject<T>` | `Functorium.Domains.ValueObjects` | 비교 가능한 단일 값 래핑 기반 클래스 |
| `IUnionValueObject` | `Functorium.Domains.ValueObjects.Unions` | Union 값 객체 마커 인터페이스 |
| `UnionValueObject` | `Functorium.Domains.ValueObjects.Unions` | Discriminated Union 기본 record |
| `UnionValueObject<TSelf>` | `Functorium.Domains.ValueObjects.Unions` | 상태 전이 지원 Union record |
| `[UnionType]` | `Functorium.Domains.ValueObjects.Unions` | Match/Switch 소스 생성기 트리거 |
| `UnreachableCaseException` | `Functorium.Domains.ValueObjects.Unions` | 도달 불가능 케이스 예외 |

### 주요 개념

| 개념 | 설명 |
|------|------|
| 값 기반 동등성 | `GetEqualityComponents()`가 반환하는 구성 요소로 동등성 판단 |
| 팩토리 메서드 패턴 | `Create()` → `Validate()` → `CreateFromValidation()` 흐름으로 불변 객체 생성 |
| 해시코드 캐싱 | 최초 계산 후 캐시하여 이후 O(1) 반환 |
| 프록시 타입 투명성 | ORM 프록시(Castle, NHibernate, EF Core)를 투명하게 처리 |
| CRTP 상태 전이 | `UnionValueObject<TSelf>`에서 Curiously Recurring Template Pattern으로 타입 안전한 전이 |

---

## 계층 구조

```
IValueObject (marker interface)
├── AbstractValueObject (abstract class, equality)
│   └── ValueObject (abstract class, CreateFromValidation)
│       ├── SimpleValueObject<T> (abstract class, single value)
│       └── ComparableValueObject (abstract class, IComparable)
│           └── ComparableSimpleValueObject<T> (abstract class, single comparable value)
│
IUnionValueObject : IValueObject (marker interface)
└── UnionValueObject (abstract record)
    └── UnionValueObject<TSelf> (abstract record, state transition)
```

값 객체 계층은 두 갈래로 나뉩니다. **클래스 기반 계층은** `AbstractValueObject`를 루트로 동등성과 팩토리 메서드를 제공하고, **record 기반 계층은** `UnionValueObject`를 루트로 Discriminated Union 패턴을 제공합니다.

---

## IValueObject

값 객체임을 나타내는 마커 인터페이스입니다. 소스 생성기와 아키텍처 규칙 검증에서 값 객체 타입을 식별하는 데 사용합니다.

```csharp
public interface IValueObject
{
    public static class ArchTestContract
    {
        public const string CreateMethodName = "Create";
        public const string CreateFromValidatedMethodName = "CreateFromValidated";
        public const string ValidateMethodName = "Validate";
        public const string NestedErrorsClassName = "Domain";
    }
}
```

### ArchTestContract 상수

아키텍처 테스트(ArchUnitNET) 스위트가 모든 ValueObject 구현체에 대해 enforce하는 네이밍 계약입니다. 프로덕션 로직은 참조하지 않습니다.

| 상수 | 값 | 용도 |
|------|----|------|
| `CreateMethodName` | `"Create"` | 팩토리 메서드 이름 규약 |
| `CreateFromValidatedMethodName` | `"CreateFromValidated"` | 사전 검증된 값의 팩토리 메서드 이름 규약 |
| `ValidateMethodName` | `"Validate"` | 검증 전용 메서드 이름 규약 |
| `NestedErrorsClassName` | `"Domain"` | 중첩 에러 클래스 이름 규약 |

---

## AbstractValueObject

모든 클래스 기반 값 객체의 루트 추상 클래스입니다. 값 기반 동등성 비교, 해시코드 캐싱, ORM 프록시 투명 처리를 제공합니다.

```csharp
[Serializable]
public abstract class AbstractValueObject
    : IValueObject
    , IEquatable<AbstractValueObject>
```

### 추상 멤버

| 멤버 | 시그니처 | 설명 |
|------|---------|------|
| `GetEqualityComponents()` | `protected abstract IEnumerable<object> GetEqualityComponents()` | 동등성 비교에 사용할 구성 요소 반환 |

### 공개 멤버

| 멤버 | 시그니처 | 설명 |
|------|---------|------|
| `Equals(object?)` | `public override bool Equals(object? obj)` | 값 기반 동등성 비교 |
| `Equals(AbstractValueObject?)` | `public bool Equals(AbstractValueObject? other)` | 타입 안전한 동등성 비교 (`IEquatable<T>`) |
| `GetHashCode()` | `public override int GetHashCode()` | 캐시된 해시코드 반환 |
| `operator ==` | `public static bool operator ==(AbstractValueObject?, AbstractValueObject?)` | 동등성 연산자 |
| `operator !=` | `public static bool operator !=(AbstractValueObject?, AbstractValueObject?)` | 부등성 연산자 |

### 보호 멤버

| 멤버 | 시그니처 | 설명 |
|------|---------|------|
| `GetUnproxiedType(object)` | `protected static Type GetUnproxiedType(object obj)` | ORM 프록시 타입을 제거하고 실제 타입 반환 |

### 동등성 계약

1. **구성 요소 비교**: `GetEqualityComponents()`가 반환하는 시퀀스를 `SequenceEqual`로 비교합니다.
2. **타입 일치 필수**: 비교 대상이 동일한 타입(프록시 제거 후)이어야 동등할 수 있습니다.
3. **배열 내용 비교**: 내부 `ValueObjectEqualityComparer`가 배열을 요소별로 비교합니다. 대용량 배열(100KB 이상)에는 적합하지 않습니다.
4. **해시코드 캐싱**: 최초 `GetHashCode()` 호출 시 계산하여 캐시합니다. 값 객체가 불변이므로 안전합니다.
5. **프록시 투명성**: Castle.Proxies, NHibernate.Proxy, EF Core Proxies 네임스페이스의 프록시 타입을 자동 감지하여 `BaseType`으로 대체합니다.

---

## ValueObject

복합 값 객체(여러 필드를 가진 값 객체)의 기반 클래스입니다. `CreateFromValidation` 팩토리 메서드 템플릿을 제공합니다.

```csharp
[Serializable]
public abstract class ValueObject : AbstractValueObject
```

### 정적 메서드

| 메서드 | 시그니처 | 설명 |
|--------|---------|------|
| `CreateFromValidation<TValueObject, TValue>` | `public static Fin<TValueObject> CreateFromValidation<TValueObject, TValue>(Validation<Error, TValue> validation, Func<TValue, TValueObject> factory) where TValueObject : ValueObject` | Validation 결과를 Fin으로 변환하여 값 객체 생성 |

### 사용 예시

```csharp
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Fin<Money> Create(decimal amount, string currency)
    {
        var validation = (ValidateAmount(amount), ValidateCurrency(currency))
            .Apply((a, c) => new Money(a, c));
        return CreateFromValidation<Money, Money>(validation, x => x);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

---

## SimpleValueObject\<T\>

단일 값을 래핑하는 값 객체의 기반 클래스입니다. `Value` 속성, 명시적 변환 연산자, 단순화된 `CreateFromValidation`을 제공합니다.

```csharp
[Serializable]
public abstract class SimpleValueObject<T> : ValueObject
    where T : notnull
```

### 보호 멤버

| 멤버 | 시그니처 | 설명 |
|------|---------|------|
| `Value` | `protected T Value { get; }` | 래핑된 값 |
| 생성자 | `protected SimpleValueObject(T value)` | `null` 전달 시 `ArgumentNullException` 발생 |

### 공개 멤버

| 멤버 | 시그니처 | 설명 |
|------|---------|------|
| `ToString()` | `public override string ToString()` | `Value.ToString()` 반환 |
| `explicit operator T` | `public static explicit operator T(SimpleValueObject<T>? valueObject)` | 명시적 변환 (`null` 시 `InvalidOperationException`) |
| `operator ==` | `public static bool operator ==(SimpleValueObject<T>?, SimpleValueObject<T>?)` | 동등성 연산자 |
| `operator !=` | `public static bool operator !=(SimpleValueObject<T>?, SimpleValueObject<T>?)` | 부등성 연산자 |

### 정적 메서드

| 메서드 | 시그니처 | 설명 |
|--------|---------|------|
| `CreateFromValidation<TValueObject>` | `public static Fin<TValueObject> CreateFromValidation<TValueObject>(Validation<Error, T> validation, Func<T, TValueObject> factory) where TValueObject : SimpleValueObject<T>` | 단일 값 Validation을 Fin으로 변환 |

### 동등성 봉인

`Equals(object?)`와 `GetHashCode()`는 `sealed override`로 선언되어 파생 클래스에서 재정의할 수 없습니다. 동등성 로직은 반드시 `GetEqualityComponents()`를 통해 정의해야 합니다.

### 사용 예시

```csharp
public sealed class Email : SimpleValueObject<string>
{
    private Email(string value) : base(value) { }

    public static Validation<Error, string> Validate(string value) =>
        ValidationRules<Email>.NotEmpty(value)
            .ThenMatches(EmailRegex())
            .ThenMaxLength(254);

    public static Fin<Email> Create(string value) =>
        CreateFromValidation(Validate(value), v => new Email(v));
}
```

---

## ComparableValueObject

비교 가능한 복합 값 객체의 기반 클래스입니다. `IComparable<ComparableValueObject>`를 구현하여 정렬 연산자(`<`, `<=`, `>`, `>=`)를 지원합니다.

```csharp
[Serializable]
public abstract class ComparableValueObject : ValueObject, IComparable<ComparableValueObject>
```

### 추상 멤버

| 멤버 | 시그니처 | 설명 |
|------|---------|------|
| `GetComparableEqualityComponents()` | `protected abstract IEnumerable<IComparable> GetComparableEqualityComponents()` | 비교에 사용할 `IComparable` 구성 요소 반환 |

### 공개 멤버

| 멤버 | 시그니처 | 설명 |
|------|---------|------|
| `CompareTo(ComparableValueObject?)` | `public virtual int CompareTo(ComparableValueObject? other)` | 구성 요소 순서대로 비교 |
| `operator <` | `public static bool operator <(ComparableValueObject?, ComparableValueObject?)` | 미만 비교 |
| `operator <=` | `public static bool operator <=(ComparableValueObject?, ComparableValueObject?)` | 이하 비교 |
| `operator >` | `public static bool operator >(ComparableValueObject?, ComparableValueObject?)` | 초과 비교 |
| `operator >=` | `public static bool operator >=(ComparableValueObject?, ComparableValueObject?)` | 이상 비교 |

### 비교 계약

1. **구성 요소 순차 비교**: `GetComparableEqualityComponents()`가 반환하는 시퀀스를 앞에서부터 순서대로 비교하며, 차이가 발생한 첫 번째 요소에서 결과를 결정합니다.
2. **null 처리**: `other`가 `null`이면 `1`(this가 큼)을 반환합니다.
3. **타입 불일치**: 타입이 다르면 타입 이름의 문자열 비교 결과를 반환합니다.
4. **동등성 위임**: `GetEqualityComponents()`는 `GetComparableEqualityComponents()`를 래핑하므로 동등성과 비교가 동일한 구성 요소를 사용합니다.

---

## ComparableSimpleValueObject\<T\>

비교 가능한 단일 값을 래핑하는 값 객체의 기반 클래스입니다. `T`는 `IComparable`을 구현해야 합니다.

```csharp
[Serializable]
public abstract class ComparableSimpleValueObject<T> : ComparableValueObject
    where T : notnull, IComparable
```

### 보호 멤버

| 멤버 | 시그니처 | 설명 |
|------|---------|------|
| `Value` | `protected T Value { get; }` | 래핑된 값 |
| 생성자 | `protected ComparableSimpleValueObject(T value)` | `null` 전달 시 `ArgumentNullException` 발생 |

### 공개 멤버

| 멤버 | 시그니처 | 설명 |
|------|---------|------|
| `ToString()` | `public override string ToString()` | `Value.ToString()` 반환 |
| `explicit operator T` | `public static explicit operator T(ComparableSimpleValueObject<T>? valueObject)` | 명시적 변환 (`null` 시 `InvalidOperationException`) |
| `operator ==` | `public static bool operator ==(ComparableSimpleValueObject<T>?, ComparableSimpleValueObject<T>?)` | 동등성 연산자 |
| `operator !=` | `public static bool operator !=(ComparableSimpleValueObject<T>?, ComparableSimpleValueObject<T>?)` | 부등성 연산자 |

### 정적 메서드

| 메서드 | 시그니처 | 설명 |
|--------|---------|------|
| `CreateFromValidation<TValueObject>` | `public static Fin<TValueObject> CreateFromValidation<TValueObject>(Validation<Error, T> validation, Func<T, TValueObject> factory) where TValueObject : ComparableSimpleValueObject<T>` | 단일 비교 가능 값 Validation을 Fin으로 변환 |

### 동등성 봉인

`SimpleValueObject<T>`와 동일하게 `Equals(object?)`와 `GetHashCode()`가 `sealed override`로 선언됩니다.

### 사용 예시

```csharp
public sealed class Priority : ComparableSimpleValueObject<int>
{
    private Priority(int value) : base(value) { }

    public static Fin<Priority> Create(int value) =>
        CreateFromValidation(
            ValidationRules<Priority>.GreaterThanOrEqual(value, 1)
                .ThenLessThanOrEqual(10),
            v => new Priority(v));
}

// 비교 연산자 사용
Priority high = Priority.Create(9).ThrowIfFail();
Priority low = Priority.Create(1).ThrowIfFail();
bool result = high > low; // true
```

---

## 팩토리 메서드 패턴

모든 값 객체는 동일한 생성 흐름을 따릅니다.

### Create/Validate 분리

```
Create(rawValue)
  └── Validate(rawValue) → Validation<Error, T>
       └── CreateFromValidation(validation, factory) → Fin<TValueObject>
```

| 메서드 | 반환 타입 | 책임 |
|--------|----------|------|
| `Validate()` | `Validation<Error, T>` | 검증 로직만 수행. 에러 누적 가능. Application Layer에서 재사용 |
| `Create()` | `Fin<TValueObject>` | `Validate()` 호출 후 `CreateFromValidation()`으로 객체 생성 |
| `CreateFromValidation()` | `Fin<TValueObject>` | `Validation`을 `Fin`으로 변환하고 팩토리 함수 적용 |

### CreateFromValidation 변형

| 기반 클래스 | 시그니처 | 제약 조건 |
|------------|---------|----------|
| `ValueObject` | `CreateFromValidation<TValueObject, TValue>(Validation<Error, TValue>, Func<TValue, TValueObject>)` | `TValueObject : ValueObject` |
| `SimpleValueObject<T>` | `CreateFromValidation<TValueObject>(Validation<Error, T>, Func<T, TValueObject>)` | `TValueObject : SimpleValueObject<T>` |
| `ComparableSimpleValueObject<T>` | `CreateFromValidation<TValueObject>(Validation<Error, T>, Func<T, TValueObject>)` | `TValueObject : ComparableSimpleValueObject<T>` |

`SimpleValueObject<T>`와 `ComparableSimpleValueObject<T>`의 `CreateFromValidation`은 타입 파라미터가 하나(`TValueObject`)로 단순화되어 있습니다. `ValueObject`의 버전은 복합 값 객체의 다양한 검증 값 타입을 수용하기 위해 `TValue`를 추가로 받습니다.

---

## Union 값 객체

Discriminated Union(구별된 합집합) 패턴을 record 기반으로 구현합니다. 클래스 기반 값 객체 계층과 별도의 record 계층입니다.

### IUnionValueObject

Union 값 객체의 마커 인터페이스입니다. `IValueObject`를 상속합니다.

```csharp
public interface IUnionValueObject : IValueObject;
```

### UnionValueObject

순수 데이터 유니온(상태 전이 없음)의 기본 abstract record입니다.

```csharp
[Serializable]
public abstract record UnionValueObject : IUnionValueObject;
```

### UnionValueObject\<TSelf\>

CRTP(Curiously Recurring Template Pattern)으로 상태 전이를 지원하는 Union record입니다.

```csharp
[Serializable]
public abstract record UnionValueObject<TSelf> : UnionValueObject
    where TSelf : UnionValueObject<TSelf>
```

#### 보호 멤버

| 멤버 | 시그니처 | 설명 |
|------|---------|------|
| `TransitionFrom<TSource, TTarget>` | `protected Fin<TTarget> TransitionFrom<TSource, TTarget>(Func<TSource, TTarget> transition, string? message = null) where TTarget : notnull` | 타입 안전한 상태 전이. `this`가 `TSource`이면 전이 함수 적용, 아니면 `InvalidTransition` 에러 반환 |

#### 전이 실패 에러

전이 실패 시 `DomainError.For<TSelf>(new DomainErrorKind.InvalidTransition(FromState, ToState), ...)`를 반환합니다. `FromState`는 현재 케이스의 타입 이름, **`ToState`는** 대상 케이스의 타입 이름입니다.

### [UnionType] 속성

`abstract partial record`에 적용하면 소스 생성기가 패턴 매칭 메서드를 자동 생성합니다.

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class UnionTypeAttribute : Attribute;
```

#### 생성되는 멤버

소스 생성기는 내부 `sealed record` 케이스를 분석하여 다음 멤버를 생성합니다.

| 생성 멤버 | 시그니처 | 설명 |
|----------|---------|------|
| `Match<TResult>` | `TResult Match<TResult>(Func<Case1, TResult> case1, ...)` | 모든 케이스에 대한 함수를 받아 결과 반환 (exhaustive) |
| `Switch` | `void Switch(Action<Case1> case1, ...)` | 모든 케이스에 대한 액션 실행 (exhaustive) |
| `Is{CaseName}` | `bool Is{CaseName}` | 특정 케이스인지 확인하는 속성 |
| `As{CaseName}()` | `{CaseName}? As{CaseName}()` | 특정 케이스로 안전하게 캐스팅 (`null` 가능) |

#### 생성기 조건

- 대상 타입이 `abstract partial record`여야 합니다.
- 내부에 하나 이상의 `sealed record` 케이스가 직접 상속해야 합니다.
- 케이스가 없으면 코드를 생성하지 않습니다.

### UnreachableCaseException

생성된 `Match`/`Switch`의 기본 분기(default case)에서 발생하는 예외입니다. sealed 계층이 완전하면 런타임에 도달하지 않습니다.

```csharp
public sealed class UnreachableCaseException(object value)
    : InvalidOperationException($"Unreachable case: {value.GetType().FullName}");
```

### 사용 예시

```csharp
[UnionType]
public abstract partial record OrderStatus : UnionValueObject<OrderStatus>
{
    public sealed record Pending : OrderStatus;
    public sealed record Confirmed(DateTime ConfirmedAt) : OrderStatus;
    public sealed record Shipped(string TrackingNumber) : OrderStatus;
    public sealed record Cancelled(string Reason) : OrderStatus;

    // 상태 전이 메서드
    public Fin<Confirmed> Confirm(DateTime confirmedAt) =>
        TransitionFrom<Pending, Confirmed>(_ => new Confirmed(confirmedAt));
}

// 소스 생성기가 자동 생성하는 멤버 사용
OrderStatus status = new OrderStatus.Pending();

// Match - exhaustive 패턴 매칭
string label = status.Match(
    pending:   _ => "대기 중",
    confirmed: c => $"확인됨 ({c.ConfirmedAt:d})",
    shipped:   s => $"배송됨 ({s.TrackingNumber})",
    cancelled: c => $"취소됨 ({c.Reason})");

// Is 속성
bool isPending = status.IsPending; // true

// As 메서드
OrderStatus.Pending? asPending = status.AsPending(); // non-null
```

---

## 관련 문서

| 문서 | 설명 |
|------|------|
| [값 객체 가이드](../guides/domain/05a-value-objects) | 값 객체 설계 원칙, 기반 클래스 선택 기준, Create/Validate 분리 패턴 |
| [값 객체 검증 가이드](../guides/domain/05b-value-objects-validation) | 열거형 패턴, Application 검증, FluentValidation 통합 |
| [Union 값 객체 가이드](../guides/domain/05c-union-value-objects) | Discriminated Union 설계, 상태 전이, 소스 생성기 사용법 |
| [검증 시스템 사양](../03-validation) | `TypedValidation`, `ContextualValidation`, `ValidationRules<T>` API |
| [에러 시스템 사양](../04-error-system) | `DomainErrorKind.InvalidTransition`, 에러 팩토리 API |
| [소스 생성기 사양](../10-source-generators) | `UnionTypeGenerator` 상세 동작, 생성 코드 형식 |
