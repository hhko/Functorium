---
title: "Functorium 프레임워크 통합"
---
## 개요

Part 1~3에서 값 객체의 개념, 검증 패턴, 프레임워크 타입을 모두 학습했습니다. 이제 이 값 객체들을 실제 애플리케이션에 통합할 차례입니다.

매번 `Equals()`, `GetHashCode()`, 비교 연산자를 직접 구현하면 중복 코드가 쌓이고 미묘한 버그가 생기기 쉽습니다. Functorium 프레임워크는 DDD 값 객체 패턴과 함수형 프로그래밍 원칙을 통합한 기본 클래스 계층을 제공하여, 개발자가 비즈니스 로직에만 집중할 수 있도록 합니다.

## 학습 목표

- Functorium 타입 계층 구조(IValueObject, AbstractValueObject, ValueObject, SimpleValueObject, ComparableValueObject 등)의 관계를 파악할 수 있습니다.
- 프레임워크 기본 클래스를 상속하여 도메인 값 객체를 구현할 수 있습니다.
- `DomainError.For<T>()` 패턴으로 구조화된 에러 코드를 생성할 수 있습니다.
- `Fin<T>`와 `ValidationRules<T>`를 연동하여 값 객체 생성 패턴을 구현할 수 있습니다.

## 왜 필요한가?

Part 1~3에서 값 객체의 개념, 검증 패턴, 다양한 값 객체 타입을 학습했습니다. 하지만 실제 프로젝트에서 매번 이러한 기능들을 직접 구현하는 것은 비효율적이며 실수의 여지가 있습니다.

값 동등성, 해시코드 계산, 비교 연산 등 공통 기능을 매번 구현하면 중복 코드가 발생합니다. 프레임워크의 기본 클래스를 활용하면 이러한 반복 작업을 제거하고, 프로젝트 전체에서 동일한 패턴을 사용하여 코드의 예측 가능성과 유지보수성을 높일 수 있습니다. 또한 Functorium의 기본 클래스들은 DDD 원칙과 함수형 프로그래밍 패러다임을 결합한 검증된 구현이므로, 설계 단계에서 발생할 수 있는 실수를 방지합니다.

## 핵심 개념

### 프레임워크 타입 계층 구조

Functorium은 다음과 같은 계층 구조로 값 객체 기본 클래스를 제공합니다.

```
IValueObject (인터페이스 — 명명 규칙 상수)
    └── AbstractValueObject (기본 클래스 — 동등성, 해시코드, ORM 프록시)
        ├── ValueObject (CreateFromValidation<TVO, TValue> 헬퍼)
        │   └── SimpleValueObject<T> (단일 값 래퍼, protected T Value)
        └── ComparableValueObject (IComparable, 비교 연산자)
            └── ComparableSimpleValueObject<T> (단일 비교 가능 값 래퍼, protected T Value)
```

필요한 기능에 따라 적절한 기본 클래스를 선택합니다. 단일 값을 래핑하는 경우 `SimpleValueObject<T>`를, 비교가 필요하면 `ComparableSimpleValueObject<T>`를, 여러 속성을 가진 복합 객체는 `ValueObject`를 사용합니다.

### SimpleValueObject\<T\>

`SimpleValueObject<T>`는 단일 값을 래핑하는 가장 기본적인 값 객체 타입입니다. `Value` 속성은 `protected`이므로, 외부에서 값에 접근하려면 명시적 변환 연산자(`explicit operator T`)를 사용하거나 별도의 public 속성을 정의합니다.

```csharp
public abstract class SimpleValueObject<T> : ValueObject
    where T : notnull
{
    protected T Value { get; }

    protected SimpleValueObject(T value) { Value = value; }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static explicit operator T(SimpleValueObject<T>? valueObject) => ...;
}
```

`GetEqualityComponents()`를 통해 값 기반 동등성 비교가 자동으로 구현되며, 개발자는 비즈니스 로직에만 집중할 수 있습니다.

### ComparableSimpleValueObject\<T\>

비교 연산이 필요한 값 객체는 `ComparableSimpleValueObject<T>`를 상속합니다. `ComparableValueObject`를 상속하므로 `SimpleValueObject<T>`와는 별도의 계층입니다.

```csharp
public abstract class ComparableSimpleValueObject<T> : ComparableValueObject
    where T : notnull, IComparable
{
    protected T Value { get; }

    protected ComparableSimpleValueObject(T value) { Value = value; }

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return Value;
    }

    public static explicit operator T(ComparableSimpleValueObject<T>? valueObject) => ...;
}
```

제네릭 제약 조건 `where T : notnull, IComparable`를 통해 비교 가능한 타입만 허용하며, 정렬과 범위 검사에 활용할 수 있습니다.

### DomainError.For\<T\>() 패턴

Functorium은 `DomainError.For<T>()` 헬퍼를 통해 구조화된 에러를 간결하게 생성합니다.

```csharp
using static Functorium.Domains.Errors.DomainErrorKind;

DomainError.For<Email>(new Empty(), value, "Email cannot be empty");
DomainError.For<Password>(new TooShort(MinLength: 8), value, "Password too short");
// 커스텀 에러 타입 정의
public sealed record Unsupported : DomainErrorKind.Custom;
DomainError.For<Currency>(new Unsupported(), value, "Currency not supported");
```

에러 코드가 `Domain.{타입명}.{에러명}` 형식으로 자동 생성되어 로깅, 국제화, API 응답 등에서 일관되게 활용할 수 있습니다.

### ValidationRules\<T\> 체이닝 시스템

`ValidationRules<T>`는 타입 파라미터를 한 번만 지정하고, 검증 규칙을 체인으로 연결합니다.

```csharp
public const int MaxLength = 320;

public static Validation<Error, string> Validate(string? value) =>
    ValidationRules<Email>
        .NotNull(value)
        .ThenNotEmpty()
        .ThenNormalize(v => v.Trim().ToLowerInvariant())
        .ThenMaxLength(MaxLength)
        .ThenMatches(EmailRegex(), "Invalid email format");
```

`Then*` 메서드들이 순차적으로 실행되며, 실패 시 즉시 단락됩니다. 검증 로직을 선언적으로 표현할 수 있습니다.

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

   IValueObject (인터페이스 — 명명 규칙 상수)
       └── AbstractValueObject (기본 클래스 — 동등성, 해시코드, ORM 프록시)
           ├── ValueObject (CreateFromValidation<TVO, TValue> 헬퍼)
           │   └── SimpleValueObject<T> (단일 값 래퍼, protected T Value)
           └── ComparableValueObject (IComparable, 비교 연산자)
               └── ComparableSimpleValueObject<T> (단일 비교 가능 값 래퍼, protected T Value)
```

### 값 객체 구현 패턴

다음은 `SimpleValueObject<T>`를 상속하여 Email 값 객체를 구현하는 전체 패턴입니다.

```csharp
using static Functorium.Domains.Errors.DomainErrorKind;

// 1. SimpleValueObject<T> 상속
public sealed class Email : SimpleValueObject<string>
{
    // 2. 도메인 제약 조건을 상수로 선언
    public const int MaxLength = 320;

    // 3. private 생성자
    private Email(string value) : base(value) { }

    // 4. Fin<T> 반환하는 Create 메서드
    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new Email(v));

    // 5. ValidationRules<T> 체이닝으로 검증
    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<Email>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenNormalize(v => v.Trim().ToLowerInvariant())
            .ThenMaxLength(MaxLength)
            .ThenMatches(EmailRegex(), "Invalid email format");

    // 5. 암시적 타입 변환 (선택적)
    public static implicit operator string(Email email) => email.Value;
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
> `Value` 속성은 `protected`로 선언되어 있으므로, 외부에서 접근이 필요한 경우 `implicit operator`나 별도의 public 속성을 정의합니다.

**Email 값 객체 (SimpleValueObject)**
```csharp
public sealed class Email : SimpleValueObject<string>
{
    private Email(string value) : base(value) { }

    public string Address => Value;  // protected Value에 대한 public 접근자

    public static Fin<Email> Create(string value) =>
        CreateFromValidation(Validate(value), v => new Email(v));

    public static Validation<Error, string> Validate(string value) =>
        (ValidateNotEmpty(value), ValidateFormat(value))
            .Apply((_, validFormat) => validFormat.ToLowerInvariant());

    public static implicit operator string(Email email) => email.Value;
}
```

**Age 값 객체 (ComparableSimpleValueObject)**
```csharp
public sealed class Age : ComparableSimpleValueObject<int>
{
    private Age(int value) : base(value) { }

    public int Id => Value;  // protected Value에 대한 public 접근자

    public static Fin<Age> Create(int value) =>
        CreateFromValidation(Validate(value), v => new Age(v));

    public static Age CreateFromValidated(int value) => new(value);

    public static Validation<Error, int> Validate(int value) =>
        ValidateNotNegative(value)
            .Bind(_ => ValidateNotTooOld(value))
            .Map(_ => value);

    public static implicit operator int(Age age) => age.Value;
}
```

**Address 값 객체 (ValueObject)**
```csharp
public sealed class Address : ValueObject
{
    public sealed record CityEmpty : DomainErrorKind.Custom;
    public sealed record StreetEmpty : DomainErrorKind.Custom;
    public sealed record PostalCodeEmpty : DomainErrorKind.Custom;

    public string City { get; }
    public string Street { get; }
    public string PostalCode { get; }

    private Address(string city, string street, string postalCode)
    {
        City = city; Street = street; PostalCode = postalCode;
    }

    public static Fin<Address> Create(string city, string street, string postalCode) =>
        CreateFromValidation(
            Validate(city, street, postalCode),
            v => new Address(v.City, v.Street, v.PostalCode));

    public static Validation<Error, (string City, string Street, string PostalCode)> Validate(
        string city, string street, string postalCode) =>
        (ValidateCityNotEmpty(city), ValidateStreetNotEmpty(street), ValidatePostalCodeNotEmpty(postalCode))
            .Apply((c, s, p) => (c, s, p));

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

다음 표는 값 객체의 요구사항에 따라 어떤 기본 클래스를 상속해야 하는지 안내합니다.

| 기본 클래스 | 용도 | 특징 |
|------------|------|------|
| `SimpleValueObject<T>` | 단일 값 래핑 | 값 동등성, 해시코드 자동 제공 |
| `ComparableSimpleValueObject<T>` | 비교 가능한 단일 값 | 정렬, 범위 검사 지원 |
| `ValueObject` | 복합 값 객체 | 다중 속성, `GetEqualityComponents()` 구현 필요 |
| `ComparableValueObject` | 비교 가능한 복합 값 | 다중 속성 + 정렬 지원 |

### 구현 체크리스트

값 객체를 구현할 때 아래 항목들을 순서대로 확인합니다.

| 항목 | 설명 |
|------|------|
| private 생성자 | 외부에서 직접 생성 방지 |
| `Create()` 메서드 | `Fin<T>` 반환으로 검증과 생성 통합 |
| `Validate()` 메서드 | `Validation<Error, T>` 반환으로 독립 검증 |
| `CreateFromValidated()` 메서드 | 검증 없이 생성 (ORM, 테스트 용도) |
| `DomainError.For<T>()` | 구조화된 에러 코드 자동 생성 |
| `ValidationRules<T>` | 체이닝 검증 규칙 |
| 암시적/명시적 타입 변환 | 선택적으로 원시 타입 변환 제공 |

### 프레임워크 활용의 이점

직접 구현했을 때와 프레임워크를 활용했을 때의 차이를 비교합니다.

| 직접 구현 | 프레임워크 활용 |
|----------|----------------|
| 매번 동등성 로직 작성 | 상속만으로 자동 제공 |
| 해시코드 계산 실수 가능 | 검증된 구현 재사용 |
| 비교 연산자 반복 구현 | 제네릭 기본 클래스 활용 |
| 프로젝트마다 다른 패턴 | 일관된 구현 패턴 |

## FAQ

### Q1: 언제 SimpleValueObject\<T\>를 사용하고 언제 ValueObject를 사용하나요?
**A**: 단일 값을 래핑하는 경우(Email, UserId, ProductCode 등) `SimpleValueObject<T>`를, 비교가 필요한 단일 값(Age, Money 등)은 `ComparableSimpleValueObject<T>`를, 여러 속성을 가진 경우(Address, ExchangeRate 등) `ValueObject`를 사용합니다.

### Q2: CreateFromValidated() 메서드는 왜 필요한가요?
**A**: 이미 검증된 값으로 객체를 생성할 때 사용합니다. ORM이 데이터베이스에서 로드하거나 테스트 코드에서 빠르게 객체를 만들 때 유용합니다. 사용자 입력이나 외부 API 응답에는 항상 `Create()` 메서드를 사용해야 합니다.

### Q3: 암시적 타입 변환(implicit operator)을 언제 사용해야 하나요?
**A**: 문자열 보간이나 API 직렬화 시 값 객체를 원시 타입처럼 자연스럽게 사용해야 할 때 제공합니다. 다만 암시적 변환은 타입 안전성을 일부 포기하는 것이므로, 명시적 변환(`explicit operator`)을 기본으로 하고 꼭 필요한 경우에만 사용합니다.

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

다음 장에서는 이렇게 구현한 값 객체를 Entity Framework Core와 통합하여 데이터베이스에 영속화하는 패턴을 다룹니다.

---

→ [2장: ORM 통합 패턴](../02-ORM-Integration/)
