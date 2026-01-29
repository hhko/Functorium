# 도메인 모델링 개요

이 문서는 Functorium 프레임워크의 도메인 모델링 개념을 설명합니다.

## 1. 소개

### DDD와 함수형 프로그래밍 결합

Functorium은 Domain-Driven Design(DDD)의 전술적 패턴과 함수형 프로그래밍을 결합합니다:

| 개념 | DDD | 함수형 프로그래밍 | Functorium |
|------|-----|-----------------|------------|
| 값 객체 | 불변 객체, 값 기반 동등성 | 불변 데이터 구조 | `ValueObject`, `SimpleValueObject<T>` |
| 검증 | 자기 검증 객체 | 타입 안전 검증 | `ValidationRules<T>`, `TypedValidation<T,V>` |
| 에러 처리 | 예외 vs 결과 | Railway Oriented Programming | `Fin<T>`, `Validation<Error, T>` |

### Functorium 프레임워크의 철학

1. **타입 안전성**: 컴파일 타임에 오류 방지
2. **불변성**: 모든 값 객체는 생성 후 변경 불가
3. **자기 검증**: 잘못된 상태의 객체는 생성 불가
4. **명시적 오류 처리**: 예외 대신 결과 타입 사용

## 2. 핵심 개념

### 값 객체란?

값 객체(Value Object)는 속성 값으로 동등성을 판단하는 불변 객체입니다.

```csharp
// 값 객체 예시: 이메일
public sealed class Email : SimpleValueObject<string>
{
    private Email(string value) : base(value) { }

    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new Email(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<Email>.NotEmpty(value ?? "")
            .ThenMatches(EmailPattern)
            .ThenMaxLength(254);
}
```

**값 객체의 특성:**

| 특성 | 설명 |
|------|------|
| 불변성 | 생성 후 변경 불가 |
| 값 기반 동등성 | 속성 값으로 동등성 판단 |
| 자기 검증 | 생성 시 유효성 검증 |
| 도메인 로직 캡슐화 | 관련 연산 포함 |

### Entity란?

Entity는 고유한 식별자(ID)를 가진 도메인 객체입니다. ID가 같으면 동일한 Entity입니다.

```csharp
// Entity 예시: 주문
[GenerateEntityId]  // OrderId 자동 생성
public class Order : AggregateRoot<OrderId>
{
    public Money TotalAmount { get; private set; }
    public CustomerId CustomerId { get; private set; }

    private Order(OrderId id, Money totalAmount, CustomerId customerId) : base(id)
    {
        TotalAmount = totalAmount;
        CustomerId = customerId;
    }

    public static Fin<Order> Create(decimal amount, string currency, CustomerId customerId) =>
        CreateFromValidation(
            Money.Validate(amount, currency),
            money => new Order(OrderId.New(), money, customerId));
}
```

**Entity의 특성:**

| 특성 | 설명 |
|------|------|
| ID 기반 동등성 | ID가 같으면 동일한 Entity |
| 가변성 | 상태 변경 가능 |
| 생명주기 | Repository를 통해 영속화 |
| 도메인 이벤트 | AggregateRoot에서 발행 가능 |

**Entity vs Value Object:**

| 관점 | Entity | Value Object |
|------|--------|--------------|
| 식별자 | ID 기반 동등성 | 값 기반 동등성 |
| 가변성 | 가변 | 불변 |
| 생명주기 | 장기 (Repository) | 단기 (일회성) |
| 예시 | Order, User, Product | Money, Email, Address |

### 불변성과 자기 검증

값 객체는 항상 유효한 상태로만 존재합니다:

```csharp
// 유효하지 않은 이메일은 생성 불가
var result = Email.Create("invalid");  // Fin<Email> - 실패
var result = Email.Create("user@example.com");  // Fin<Email> - 성공
```

### 에러 처리 전략 (Railway Oriented Programming)

Functorium은 예외 대신 결과 타입을 사용합니다:

```
입력 → [검증1] → [검증2] → [검증3] → 성공
         ↓         ↓         ↓
        실패      실패      실패
```

**두 가지 결과 타입:**

| 타입 | 용도 | 특징 |
|------|------|------|
| `Fin<T>` | 최종 결과 | 성공 또는 단일 에러 |
| `Validation<Error, T>` | 검증 결과 | 성공 또는 여러 에러 |

## 3. 타입 계층 구조 다이어그램

### IValueObject 계층

```
IValueObject (인터페이스)
│
AbstractValueObject (추상 클래스)
├── GetEqualityComponents() - 동등성 컴포넌트
├── Equals() / GetHashCode() - 값 기반 동등성
└── == / != 연산자
    │
    └── ValueObject
        ├── CreateFromValidation<TValueObject, TValue>() 헬퍼
        │
        ├── SimpleValueObject<T>
        │   ├── protected T Value
        │   ├── CreateFromValidation<TValueObject>() 헬퍼
        │   └── explicit operator T
        │
        └── ComparableValueObject
            ├── GetComparableEqualityComponents()
            ├── IComparable<ComparableValueObject>
            ├── < / <= / > / >= 연산자
            │
            └── ComparableSimpleValueObject<T>
                ├── protected T Value
                ├── CreateFromValidation<TValueObject>() 헬퍼
                └── explicit operator T
```

### IEntity 계층

```
IEntity<TId> (인터페이스)
├── TId Id - Entity 식별자
├── CreateMethodName 상수
└── CreateFromValidatedMethodName 상수
    │
    └── Entity<TId> (추상 클래스)
        ├── Id 속성 (protected init)
        ├── Equals() / GetHashCode() - ID 기반 동등성
        ├── == / != 연산자
        ├── CreateFromValidation<TEntity, TValue>() 헬퍼
        └── GetUnproxiedType() - ORM 프록시 지원
            │
            └── AggregateRoot<TId>
                ├── DomainEvents (읽기 전용)
                ├── AddDomainEvent()
                ├── RemoveDomainEvent()
                └── ClearDomainEvents()

IEntityId<T> (인터페이스) - Ulid 기반
├── Ulid Value
├── static T New()
├── static T Create(Ulid)
└── static T Create(string)

IDomainEvent (인터페이스)
└── DateTimeOffset OccurredAt
    │
    └── DomainEvent (abstract record)
        └── UTC 시간 자동 설정
```

### Error 계층

```
Error (LanguageExt)
│
├── DomainError
│   └── DomainErrorType (Presence, Length, Format, DateTime, Numeric, Range, Existence, Custom)
│
├── ApplicationError
│   └── ApplicationErrorType (공통, 권한, 검증, 비즈니스 규칙, 커스텀)
│
└── AdapterError
    └── AdapterErrorType (공통, Pipeline, 외부 서비스, 데이터, 커스텀)
```

### 관계도

```
+-------------------+         +-------------------+
|   ValueObject     |         |   Validation      |
|                   |◄────────|  ValidationRules  |
+-------------------+         +-------------------+
         │                             │
         │                             │
         ▼                             ▼
+-------------------+         +-------------------+
|   Fin<T> /        |         |   DomainError     |
|   Validation<E,T> |◄────────|                   |
+-------------------+         +-------------------+
```

## 4. 목적별 가이드

| 목적 | 참조 문서 | 주요 내용 |
|------|----------|----------|
| 값 객체 구현 | [valueobject-guide.md](./valueobject-guide.md) §개요, §기반 클래스 | 기반 클래스 선택, Create/Validate 패턴 |
| Entity 구현 | [entity-guide.md](./entity-guide.md) §기반 클래스 | Entity vs AggregateRoot, ID 시스템 |
| 검증 로직 작성 | [valueobject-guide.md](./valueobject-guide.md) §검증 시스템 | `ValidationRules<T>`, 순차/병렬 검증 |
| 도메인 이벤트 | [entity-guide.md](./entity-guide.md) §도메인 이벤트 | 이벤트 정의, 발행 패턴 |
| 에러 타입 정의 | [error-guide.md](./error-guide.md) | 레이어별 에러 타입, 네이밍 규칙 |
| 에러 테스트 작성 | [error-testing-guide.md](./error-testing-guide.md) | 어설션 메서드, 테스트 패턴 |
| 검증 재사용 | [valueobject-guide.md](./valueobject-guide.md) §FluentValidation | 유스케이스 파이프라인 통합 |

## 5. 빠른 시작 예제

### 간단한 Email 값 객체

```csharp
using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations.Typed;
using System.Text.RegularExpressions;

public sealed class Email : SimpleValueObject<string>
{
    private static readonly Regex EmailPattern = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled);
    private const int MaxLength = 254;

    // private 생성자 - 외부 생성 차단
    private Email(string value) : base(value) { }

    // 팩토리 메서드
    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new Email(v));

    // 검증 메서드 (원시 타입 반환)
    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<Email>.NotEmpty(value ?? "")
            .ThenMatches(EmailPattern)
            .ThenMaxLength(MaxLength)
            .ThenNormalize(v => v.ToLowerInvariant());

    // 암시적 변환 (선택적)
    public static implicit operator string(Email email) => email.Value;
}
```

### 사용 예시

```csharp
// 성공
var email = Email.Create("User@Example.COM");
email.IfSucc(e => Console.WriteLine(e));  // user@example.com

// 실패
var invalid = Email.Create("invalid-email");
invalid.IfFail(e => Console.WriteLine(e.Code));  // DomainErrors.Email.InvalidFormat
```

### 테스트 예시

```csharp
using Functorium.Testing.Assertions.Errors;
using static Functorium.Domains.Errors.DomainErrorType;

[Fact]
public void Create_ShouldFail_WhenEmailIsEmpty()
{
    // Arrange
    var emptyEmail = "";

    // Act
    var result = Email.Create(emptyEmail);

    // Assert
    result.ShouldBeDomainError<Email, Email>(new Empty());
}

[Fact]
public void Create_ShouldSucceed_WhenEmailIsValid()
{
    // Arrange
    var validEmail = "user@example.com";

    // Act
    var result = Email.Create(validEmail);

    // Assert
    result.IsSucc.ShouldBeTrue();
}
```

## 6. 가이드 문서 색인

| 문서 | 설명 | 주요 내용 |
|------|------|----------|
| [valueobject-guide.md](./valueobject-guide.md) | 값 객체 구현 | 기반 클래스, 검증 시스템, 구현 패턴, 실전 예제 |
| [entity-guide.md](./entity-guide.md) | Entity 구현 | Entity/AggregateRoot, ID 시스템, 도메인 이벤트 |
| [error-guide.md](./error-guide.md) | 에러 시스템 | 에러 정의, 네이밍 규칙, 레이어별 에러 타입 |
| [error-testing-guide.md](./error-testing-guide.md) | 에러 테스트 | 어설션 메서드, 테스트 패턴, 모범 사례 |
| [unit-testing-guide.md](./unit-testing-guide.md) | 단위 테스트 | 테스트 규칙, 네이밍 규칙, 체크리스트 |

## 7. 참고 자료

- [LanguageExt](https://github.com/louthy/language-ext) - 함수형 프로그래밍 라이브러리
- [Ardalis.SmartEnum](https://github.com/ardalis/SmartEnum) - 타입 안전한 열거형
