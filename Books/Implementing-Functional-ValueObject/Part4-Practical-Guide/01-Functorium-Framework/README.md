# Functorium 프레임워크 통합

> **Part 4: 실전 가이드** | [목차](../../README.md) | [다음: ORM 통합 →](../02-ORM-Integration/README.md)

---

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

이 프로젝트는 Functorium 프레임워크에서 제공하는 값 객체 기본 클래스 계층 구조를 이해하고 활용하는 방법을 학습합니다. Functorium은 DDD의 값 객체 패턴과 함수형 프로그래밍 원칙을 통합한 프레임워크로, 실전에서 바로 사용할 수 있는 값 객체 기본 클래스들을 제공합니다.

Part 1~3에서 학습한 개념들이 실제 프레임워크에서 어떻게 구현되어 있는지 확인하고, 이를 활용하여 도메인 모델을 구축하는 방법을 익힙니다.

## 학습 목표

### **핵심 학습 목표**
1. **Functorium 타입 계층 구조 이해**: IValueObject, AbstractValueObject, ValueObject, SimpleValueObject, ComparableValueObject 등의 계층 관계를 파악합니다.
2. **프레임워크 기본 클래스 활용**: 제공되는 기본 클래스를 상속하여 도메인 값 객체를 구현하는 방법을 학습합니다.
3. **ErrorCodeFactory 패턴 적용**: 구조화된 에러 코드를 생성하여 명확한 오류 처리를 구현합니다.
4. **Fin<T>와 Validation 연동**: 함수형 결과 타입과 값 객체 생성 패턴을 통합합니다.

### **실습을 통해 확인할 내용**
- `SimpleValueObject<T>`: 단일 값을 래핑하는 기본 값 객체
- `ComparableSimpleValueObject<T>`: 비교 가능한 단일 값 객체
- `AbstractValueObject`: 복합 값 객체의 기본 클래스
- 프레임워크 계층 구조의 설계 원칙

## 왜 필요한가?

Part 1~3에서 값 객체의 개념, 검증 패턴, 다양한 값 객체 타입을 학습했습니다. 하지만 실제 프로젝트에서 매번 이러한 기능들을 직접 구현하는 것은 비효율적이며 실수의 여지가 있습니다.

**첫 번째 이유는 코드 재사용성입니다.** 값 동등성, 해시코드 계산, 비교 연산 등 값 객체에 필요한 공통 기능을 매번 구현하면 중복 코드가 발생합니다. 프레임워크의 기본 클래스를 활용하면 이러한 반복 작업을 제거할 수 있습니다.

**두 번째 이유는 일관성 확보입니다.** 프로젝트 전체에서 동일한 패턴을 사용하면 코드의 예측 가능성이 높아지고 유지보수가 용이해집니다. 새로운 팀원도 프레임워크의 규칙만 이해하면 쉽게 값 객체를 구현할 수 있습니다.

**세 번째 이유는 검증된 구현 활용입니다.** Functorium의 값 객체 기본 클래스들은 DDD 원칙과 함수형 프로그래밍 패러다임을 결합한 검증된 구현입니다. 이를 활용하면 설계 단계에서 발생할 수 있는 실수를 방지할 수 있습니다.

## 핵심 개념

### 첫 번째 개념: 프레임워크 타입 계층 구조

Functorium은 다음과 같은 계층 구조로 값 객체 기본 클래스를 제공합니다.

```
IValueObject (인터페이스)
    │
    └── AbstractValueObject (기본 클래스)
        │
        ├── ValueObject (복합 값 객체)
        │   │
        │   ├── SimpleValueObject<T>
        │   │   └── ComparableSimpleValueObject<T>
        │   │
        │   └── ComparableValueObject
        │
        └── SmartEnum + IValueObject (열거형)
```

**핵심 아이디어는 "필요한 기능에 따라 적절한 기본 클래스를 선택"하는 것입니다.** 단일 값을 래핑하는 경우 `SimpleValueObject<T>`를, 비교가 필요하면 `ComparableSimpleValueObject<T>`를, 여러 속성을 가진 복합 객체는 `ValueObject`를 사용합니다.

### 두 번째 개념: SimpleValueObject<T>

`SimpleValueObject<T>`는 단일 값을 래핑하는 가장 기본적인 값 객체 타입입니다.

```csharp
public abstract class SimpleValueObject<T> : AbstractValueObject
{
    public T Value { get; }

    protected SimpleValueObject(T value) => Value = value;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value!;
    }

    public override string ToString() => Value?.ToString() ?? string.Empty;
}
```

**핵심 아이디어는 "값 동등성을 자동으로 제공"하는 것입니다.** `GetEqualityComponents()`를 통해 값 기반 동등성 비교가 자동으로 구현되며, 개발자는 비즈니스 로직에만 집중할 수 있습니다.

### 세 번째 개념: ComparableSimpleValueObject<T>

비교 연산이 필요한 값 객체는 `ComparableSimpleValueObject<T>`를 상속합니다.

```csharp
public abstract class ComparableSimpleValueObject<T> : SimpleValueObject<T>, IComparable<ComparableSimpleValueObject<T>>
    where T : IComparable<T>
{
    protected ComparableSimpleValueObject(T value) : base(value) { }

    public int CompareTo(ComparableSimpleValueObject<T>? other)
    {
        if (other is null) return 1;
        return Value.CompareTo(other.Value);
    }

    public static bool operator <(ComparableSimpleValueObject<T> left, ComparableSimpleValueObject<T> right)
        => left.CompareTo(right) < 0;

    public static bool operator >(ComparableSimpleValueObject<T> left, ComparableSimpleValueObject<T> right)
        => left.CompareTo(right) > 0;
}
```

**핵심 아이디어는 "비교 연산을 타입 안전하게 제공"하는 것입니다.** 제네릭 제약 조건 `where T : IComparable<T>`를 통해 비교 가능한 타입만 허용하며, 정렬과 범위 검사에 활용할 수 있습니다.

### 네 번째 개념: ErrorCodeFactory 패턴

Functorium은 구조화된 에러 코드 생성을 위한 `ErrorCodeFactory`를 제공합니다.

```csharp
public static Error Empty(string value) =>
    ErrorCodeFactory.Create(
        errorCode: $"{nameof(DomainErrors)}.{nameof(Email)}.{nameof(Empty)}",
        errorCurrentValue: value,
        errorMessage: $"Email address cannot be empty. Current value: '{value}'");
```

**핵심 아이디어는 "에러 정보를 구조화"하는 것입니다.** 에러 코드, 현재 값, 메시지를 명확히 구분하여 로깅, 국제화, API 응답 등에서 일관되게 활용할 수 있습니다.

## 실전 지침

### 예상 출력
```
=== Functorium 프레임워크 통합 ===

1. SimpleValueObject<T> 사용 예시
────────────────────────────────────────
   유효한 이메일: user@example.com
   오류: Email.InvalidFormat

2. ComparableSimpleValueObject<T> 사용 예시
────────────────────────────────────────
   정렬 전: 30, 25, 35
   정렬 후: 25, 30, 35

3. ValueObject (복합) 사용 예시
────────────────────────────────────────
   주소: 서울 강남구 테헤란로 123 (06234)

4. 프레임워크 타입 계층 구조
────────────────────────────────────────

   IValueObject (인터페이스)
       │
       └── AbstractValueObject (기본 클래스)
           │
           ├── ValueObject (복합 값 객체)
           │   │
           │   ├── SimpleValueObject<T>
           │   │   └── ComparableSimpleValueObject<T>
           │   │
           │   └── ComparableValueObject
           │
           └── SmartEnum + IValueObject (열거형)
```

### 값 객체 구현 패턴

```csharp
// 1. SimpleValueObject<T> 상속
public sealed class Email : SimpleValueObject<string>
{
    // 2. private 생성자
    private Email(string value) : base(value) { }

    // 3. Fin<T> 반환하는 Create 메서드
    public static Fin<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DomainErrors.Empty(value ?? "null");

        if (!value.Contains('@'))
            return DomainErrors.InvalidFormat(value);

        return new Email(value.ToLowerInvariant());
    }

    // 4. 암시적 타입 변환 (선택적)
    public static implicit operator string(Email email) => email.Value;

    // 5. 구조화된 도메인 에러
    internal static class DomainErrors
    {
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Email)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: $"Email address cannot be empty. Current value: '{value}'");

        public static Error InvalidFormat(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Email)}.{nameof(InvalidFormat)}",
                errorCurrentValue: value,
                errorMessage: $"Invalid email format. Current value: '{value}'");
    }
}
```

## 프로젝트 설명

### 프로젝트 구조
```
01-Functorium-Framework/
├── FunctoriumFramework/
│   ├── Program.cs                  # 메인 실행 파일
│   └── FunctoriumFramework.csproj  # 프로젝트 파일
└── README.md                       # 프로젝트 문서
```

### 의존성
```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\..\..\Src\Functorium\Functorium.csproj" />
</ItemGroup>
```

### 핵심 코드

> **참고**: 아래 예시들은 `Functorium.Domains.ValueObjects` 네임스페이스의 기본 클래스를 상속합니다.
> `Value` 속성은 `protected`로 선언되어 있으므로, 외부에서 접근이 필요한 경우 public 접근자를 별도로 정의합니다.

**Email 값 객체 (SimpleValueObject)**
```csharp
using Functorium.Domains.ValueObjects;

public sealed class Email : SimpleValueObject<string>
{
    private Email(string value) : base(value) { }

    /// <summary>
    /// 이메일 주소 값에 대한 public 접근자
    /// </summary>
    public string Address => Value;

    public static Fin<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DomainErrors.Empty(value ?? "null");

        if (!value.Contains('@'))
            return DomainErrors.InvalidFormat(value);

        return new Email(value.ToLowerInvariant());
    }

    public static implicit operator string(Email email) => email.Value;
}
```

**Age 값 객체 (ComparableSimpleValueObject)**
```csharp
using Functorium.Domains.ValueObjects;

public sealed class Age : ComparableSimpleValueObject<int>
{
    private Age(int value) : base(value) { }

    /// <summary>
    /// 나이 값에 대한 public 접근자
    /// </summary>
    public int Id => Value;

    public static Fin<Age> Create(int value)
    {
        if (value < 0)
            return DomainErrors.Negative(value);
        if (value > 150)
            return DomainErrors.TooOld(value);
        return new Age(value);
    }

    internal static Age CreateFromValidated(int value) => new(value);

    public static implicit operator int(Age age) => age.Value;
}
```

**Address 값 객체 (ValueObject)**
```csharp
using Functorium.Domains.ValueObjects;

public sealed class Address : ValueObject
{
    public string City { get; }
    public string Street { get; }
    public string PostalCode { get; }

    private Address(string city, string street, string postalCode)
    {
        City = city;
        Street = street;
        PostalCode = postalCode;
    }

    public static Fin<Address> Create(string city, string street, string postalCode)
    {
        if (string.IsNullOrWhiteSpace(city))
            return DomainErrors.CityEmpty(city ?? "null");
        if (string.IsNullOrWhiteSpace(street))
            return DomainErrors.StreetEmpty(street ?? "null");
        if (string.IsNullOrWhiteSpace(postalCode))
            return DomainErrors.PostalCodeEmpty(postalCode ?? "null");

        return new Address(city, street, postalCode);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return City;
        yield return Street;
        yield return PostalCode;
    }

    public override string ToString() => $"{City} {Street} ({PostalCode})";
}
```

## 한눈에 보는 정리

### 기본 클래스 선택 가이드

| 기본 클래스 | 용도 | 특징 |
|------------|------|------|
| `SimpleValueObject<T>` | 단일 값 래핑 | 값 동등성, 해시코드 자동 제공 |
| `ComparableSimpleValueObject<T>` | 비교 가능한 단일 값 | 정렬, 범위 검사 지원 |
| `ValueObject` | 복합 값 객체 | 다중 속성, `GetEqualityComponents()` 구현 필요 |
| `ComparableValueObject` | 비교 가능한 복합 값 | 다중 속성 + 정렬 지원 |

### 구현 체크리스트

| 항목 | 설명 |
|------|------|
| private 생성자 | 외부에서 직접 생성 방지 |
| `Create()` 메서드 | `Fin<T>` 반환으로 검증과 생성 통합 |
| `CreateFromValidated()` 메서드 | 검증 없이 생성 (내부 용도) |
| `DomainErrors` 내부 클래스 | 구조화된 에러 코드 정의 |
| 암시적 타입 변환 | 선택적으로 원시 타입 변환 제공 |

### 프레임워크 활용의 이점

| 직접 구현 | 프레임워크 활용 |
|----------|----------------|
| 매번 동등성 로직 작성 | 상속만으로 자동 제공 |
| 해시코드 계산 실수 가능 | 검증된 구현 재사용 |
| 비교 연산자 반복 구현 | 제네릭 기본 클래스 활용 |
| 프로젝트마다 다른 패턴 | 일관된 구현 패턴 |

## FAQ

### Q1: 언제 SimpleValueObject<T>를 사용하고 언제 AbstractValueObject를 사용하나요?
**A**: 단일 값을 래핑하는 경우 `SimpleValueObject<T>`를 사용합니다. 예를 들어 Email, UserId, ProductCode 등 하나의 원시 값을 감싸는 경우입니다. 반면 Address(City, Street, PostalCode)나 Money(Amount, Currency)처럼 여러 속성을 가진 경우 `AbstractValueObject`를 직접 상속합니다.

이는 객체지향 설계에서 단일 책임 원칙을 따르는 것과 유사합니다. 각 기본 클래스는 특정 용도에 최적화되어 있으므로 목적에 맞는 클래스를 선택하면 불필요한 복잡성을 피할 수 있습니다.

### Q2: CreateFromValidated() 메서드는 왜 필요한가요?
**A**: `CreateFromValidated()`는 이미 검증된 값으로 객체를 생성할 때 사용합니다. 주로 데이터베이스에서 읽어온 값이나 테스트 코드에서 활용됩니다.

예를 들어 ORM이 데이터베이스에서 값을 로드할 때, 해당 값은 저장 시점에 이미 검증되었으므로 다시 검증할 필요가 없습니다. 이 경우 `CreateFromValidated()`를 사용하면 불필요한 검증 오버헤드를 피할 수 있습니다.

단, 이 메서드는 신뢰할 수 있는 소스의 데이터에만 사용해야 합니다. 사용자 입력이나 외부 API 응답에는 항상 `Create()` 메서드를 사용하여 검증을 수행해야 합니다.

### Q3: ErrorCodeFactory의 세 가지 매개변수는 각각 어떤 역할을 하나요?
**A**: `ErrorCodeFactory.Create()`는 세 가지 정보를 구조화합니다.

- **errorCode**: 에러를 식별하는 고유 코드입니다. `"Email.InvalidFormat"` 형식으로 도메인과 에러 유형을 명확히 표현합니다. 국제화(i18n)나 에러 추적에 활용됩니다.
- **errorCurrentValue**: 에러를 발생시킨 실제 입력값입니다. 디버깅과 로깅에 유용하며, 사용자에게 "입력한 값 'abc'는 올바른 이메일 형식이 아닙니다"와 같은 메시지를 보여줄 때 활용됩니다.
- **errorMessage**: 사람이 읽을 수 있는 오류 설명입니다. 기본 언어의 메시지를 제공하며, errorCode를 키로 사용하여 다국어 메시지로 대체할 수 있습니다.

### Q4: 암시적 타입 변환(implicit operator)을 언제 사용해야 하나요?
**A**: 암시적 타입 변환은 값 객체를 원시 타입처럼 자연스럽게 사용해야 할 때 제공합니다. 예를 들어 `string greeting = $"안녕하세요, {email}님";`처럼 문자열 보간에서 직접 사용하거나, API 직렬화 시 자동 변환이 필요한 경우입니다.

그러나 암시적 변환은 타입 안전성을 일부 포기하는 것이므로 신중하게 사용해야 합니다. 값 객체의 주요 목적 중 하나가 타입 혼동을 방지하는 것이므로, 명시적 변환(`explicit operator`)을 기본으로 사용하고 꼭 필요한 경우에만 암시적 변환을 제공하는 것이 좋습니다.

### Q5: Functorium 프레임워크 없이도 값 객체를 구현할 수 있나요?
**A**: 물론입니다. Part 1~3에서 학습한 내용만으로도 완전한 값 객체를 구현할 수 있습니다. 프레임워크는 반복 작업을 줄이고 일관성을 확보하기 위한 도구입니다.

프레임워크를 사용하면 `GetEqualityComponents()`, `Equals()`, `GetHashCode()`, 비교 연산자 등의 보일러플레이트 코드를 직접 작성하지 않아도 됩니다. 또한 팀 전체가 동일한 패턴을 따르므로 코드 리뷰와 유지보수가 수월해집니다.

소규모 프로젝트나 학습 목적이라면 직접 구현하는 것도 좋은 경험입니다. 대규모 프로젝트나 팀 환경에서는 프레임워크 활용이 생산성 향상에 도움이 됩니다.

---

## 테스트

이 프로젝트에는 단위 테스트가 포함되어 있습니다.

### 테스트 실행
```bash
cd FunctoriumFramework.Tests.Unit
dotnet test
```

### 테스트 구조
```
FunctoriumFramework.Tests.Unit/
├── EmailTests.cs      # SimpleValueObject 패턴 테스트
├── AgeTests.cs        # ComparableSimpleValueObject 패턴 테스트
└── AddressTests.cs    # AbstractValueObject 패턴 테스트
```

### 주요 테스트 케이스

| 테스트 클래스 | 테스트 내용 |
|-------------|-----------|
| EmailTests | 생성 검증, 형식 검증, 정규화, 동등성 |
| AgeTests | 범위 검증, 비교 연산, 정렬 |
| AddressTests | 다중 필드 검증, 복합 동등성 |
