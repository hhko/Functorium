# DDD 전술적 설계 개요

이 문서는 DDD 전술적 설계의 전체 그림과 Functorium 프레임워크의 매핑을 설명합니다.

## 1. 왜 DDD 전술적 설계인가 (WHY)

### 도메인 복잡성 관리

소프트웨어의 본질적 복잡성은 도메인에서 비롯됩니다. DDD 전술적 설계는 이 복잡성을 **명확한 빌딩블록**으로 분해하여 관리합니다. 각 빌딩블록은 역할과 책임이 명확하여, 개발자가 "이 코드는 어디에 두어야 하는가?"라는 질문에 일관된 답을 제공합니다.

### 유비쿼터스 언어와 코드의 일치

DDD는 도메인 전문가와 개발자가 동일한 언어를 사용할 것을 강조합니다. 코드에서 `Email`, `Order`, `Product`와 같은 도메인 용어를 직접 타입으로 표현하면, 코드가 곧 도메인 모델이 됩니다.

### 비즈니스 규칙의 명시적 표현

"이메일 형식이 올바른가?", "재고가 충분한가?", "주문 상태 전이가 유효한가?" 같은 비즈니스 규칙이 특정 빌딩블록(Value Object, Entity, Aggregate)에 배치되어, 규칙의 위치와 책임이 명확합니다.

## 2. DDD 전술적 설계 빌딩블록 (WHAT)

### 빌딩블록 전체 맵

```
┌─────────────────────────────────────────────────────────┐
│                      Domain Layer                       │
│                                                         │
│  ┌──────────┐  ┌──────────┐  ┌─────────────────────┐    │
│  │ Value    │  │ Entity   │  │ Aggregate           │    │
│  │ Object   │  │          │  │ ┌─────────────────┐ │    │
│  │          │  │          │  │ │ Aggregate Root  │ │    │
│  │ Email    │  │ Tag      │  │ │ (Order)         │ │    │
│  │ Money    │  │ OrderItem│  │ │  ├─ Child Entity│ │    │
│  │ Quantity │  │          │  │ │  └─ Value Object│ │    │
│  └──────────┘  └──────────┘  │ └─────────────────┘ │    │
│                              └─────────────────────┘    │
│                                                         │
│  ┌──────────────┐  ┌──────────────────┐                 │
│  │ Domain       │  │ Domain Error     │                 │
│  │ Event        │  │ (DomainError)    │                 │
│  └──────────────┘  └──────────────────┘                 │
│  ┌──────────────────┐                                   │
│  │ Domain Service   │                                   │
│  │ (IDomainService) │                                   │
│  └──────────────────┘                                   │
│                                                         │
├─────────────────────────────────────────────────────────┤
│                    Application Layer                    │
│                                                         │
│  ┌──────────────┐  ┌──────────────┐                     │
│  │ Command      │  │ Query        │                     │
│  │ (Use Case)   │  │ (Use Case)   │                     │
│  └──────────────┘  └──────────────┘                     │
│  ┌──────────────┐  ┌──────────────────┐                 │
│  │ Event Handler│  │ Application Error│                 │
│  └──────────────┘  └──────────────────┘                 │
│                                                         │
├─────────────────────────────────────────────────────────┤
│                      Adapter Layer                      │
│                                                         │
│  ┌──────────────┐  ┌──────────────┐                     │
│  │ Port         │  │ Adapter      │                     │
│  │ (Interface)  │  │ (구현체)      │                     │
│  └──────────────┘  └──────────────┘                     │
│  ┌──────────────────┐                                   │
│  │ Adapter Error    │                                   │
│  └──────────────────┘                                   │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### 각 빌딩블록의 역할과 관계

| 빌딩블록 | 역할 | 특성 |
|----------|------|------|
| **Value Object** | 도메인 개념의 값 표현 | 불변, 값 동등성, 자기 검증 |
| **Entity** | 식별자를 가진 도메인 객체 | ID 동등성, 가변, 생명주기 |
| **Aggregate** | 일관성 경계를 가진 객체 그룹 | 트랜잭션 단위, 불변식 보호 |
| **Domain Event** | 도메인에서 발생한 중요한 사건 | 과거형, 불변, Aggregate 간 통신 |
| **Domain Service** | 교차 Aggregate 순수 도메인 로직 | 상태 없음, I/O 없음, IDomainService 마커 |
| **Factory** | Aggregate 생성/복원 | 정적 `Create()`, `CreateFromValidated()` 메서드 |
| **Repository** | Aggregate의 영속화 | Aggregate 단위로 저장/조회 |
| **Application Service** | 유스케이스 조율 | Command/Query, 도메인 객체 위임 |

### Functorium 타입 매핑 테이블

| DDD 빌딩블록 | Functorium 타입 | 위치 |
|-------------|----------------|------|
| Value Object | `SimpleValueObject<T>`, `ValueObject`, `ComparableSimpleValueObject<T>` | `Functorium.Domains.ValueObjects` |
| Entity | `Entity<TId>` | `Functorium.Domains.Entities` |
| Aggregate Root | `AggregateRoot<TId>` | `Functorium.Domains.Entities` |
| Entity ID | `IEntityId<T>` + `[GenerateEntityId]` | `Functorium.Domains.Entities` |
| Domain Event | `IDomainEvent`, `DomainEvent` | `Functorium.Domains.Events` |
| Domain Service | `IDomainService` | `Functorium.Domains.Services` |
| Specification | `Specification<T>` | `Functorium.Domains.Specifications` |
| Domain Error | `DomainError`, `DomainErrorType` | `Functorium.Domains.Errors` |
| Command | `ICommandRequest<T>`, `ICommandUsecase<T,R>` | `Functorium.Applications.Usecases` |
| Query | `IQueryRequest<T>`, `IQueryUsecase<T,R>` | `Functorium.Applications.Usecases` |
| Event Handler | `IDomainEventHandler<T>` | `Functorium.Applications.Events` |
| Application Error | `ApplicationError`, `ApplicationErrorType` | `Functorium.Applications.Errors` |
| Port | `IAdapter` | `Functorium.Applications.Observabilities` |
| Repository | `IRepository<TAggregate, TId>` | `Functorium.Domains.Repositories` |
| Adapter | `[GeneratePipeline]` | Adapter Layer 프로젝트 |
| Adapter Error | `AdapterError`, `AdapterErrorType` | `Functorium.Adapters.Errors` |
| 검증 | `ValidationRules<T>`, `TypedValidation<T,V>` | `Functorium.Domains.ValueObjects.Validations` |
| 결과 타입 | `Fin<T>`, `Validation<Error, T>`, `FinResponse<T>` | LanguageExt / Functorium |

## 3. Functorium의 설계 철학

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

### 핵심 개념

#### 값 객체 (Value Object)

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

#### Entity

Entity는 고유한 식별자(ID)를 가진 도메인 객체입니다. ID가 같으면 동일한 Entity입니다.

```csharp
// Entity 예시: 주문 (검증된 VO를 받아 Aggregate 생성)
[GenerateEntityId]  // OrderId 자동 생성
public sealed class Order : AggregateRoot<OrderId>
{
    public ProductId ProductId { get; private set; }
    public Quantity Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money TotalAmount { get; private set; }

    private Order(OrderId id, ProductId productId, Quantity quantity,
        Money unitPrice, Money totalAmount) : base(id) { /* ... */ }

    // Create: 검증된 VO를 받아 새 Aggregate 생성
    public static Order Create(
        ProductId productId, Quantity quantity,
        Money unitPrice, ShippingAddress shippingAddress)
    {
        var totalAmount = unitPrice.Multiply(quantity);
        var order = new Order(OrderId.New(), productId, quantity, unitPrice, totalAmount);
        order.AddDomainEvent(new CreatedEvent(order.Id, productId, quantity, totalAmount));
        return order;
    }
}
```

**Entity vs Value Object:**

| 관점 | Entity | Value Object |
|------|--------|--------------|
| 식별자 | ID 기반 동등성 | 값 기반 동등성 |
| 가변성 | 가변 | 불변 |
| 생명주기 | 장기 (Repository) | 단기 (일회성) |
| 예시 | Order, User, Product | Money, Email, Address |

#### 불변성과 자기 검증

값 객체는 항상 유효한 상태로만 존재합니다:

```csharp
// 유효하지 않은 이메일은 생성 불가
var result = Email.Create("invalid");  // Fin<Email> - 실패
var result = Email.Create("user@example.com");  // Fin<Email> - 성공
```

#### 에러 처리 전략 (Railway Oriented Programming)

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

## 4. 타입 계층 구조

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
            └── AggregateRoot<TId> : IDomainEventDrain
                ├── DomainEvents (읽기 전용, IHasDomainEvents)
                ├── AddDomainEvent() (protected)
                └── ClearDomainEvents() (IDomainEventDrain)

IEntityId<T> (인터페이스) - Ulid 기반
├── Ulid Value
├── static T New()
├── static T Create(Ulid)
└── static T Create(string)

IDomainEvent : INotification (인터페이스)
├── DateTimeOffset OccurredAt
├── Ulid EventId
├── string? CorrelationId
└── string? CausationId
    │
    └── DomainEvent (abstract record)
        ├── 기본 생성자: OccurredAt, EventId 자동 설정
        └── CorrelationId, CausationId 선택적 지정

IHasDomainEvents (읽기 전용 이벤트 조회)
└── IDomainEventDrain (internal, 이벤트 정리)
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

### Specification 계층

```
Specification<T> (추상 클래스)
├── abstract bool IsSatisfiedBy(T entity)
├── And() / Or() / Not() 조합 메서드
├── & / | / ! 연산자 오버로드
│
├── AndSpecification<T> (internal sealed)
├── OrSpecification<T>  (internal sealed)
└── NotSpecification<T> (internal sealed)
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

## 5. 레이어 아키텍처와 빌딩블록 배치

### Domain Layer

도메인의 핵심 비즈니스 로직을 담당합니다. 외부 의존성이 없습니다.

- **배치되는 빌딩블록**: Value Object, Entity, Aggregate Root, Domain Event, Domain Error, Domain Service, Repository Interface
- **의존성**: 없음 (가장 안쪽 레이어)

### Application Layer

유스케이스를 조율합니다. 도메인 객체에 작업을 위임합니다.

- **배치되는 빌딩블록**: Command/Query (Use Case), Event Handler, Application Error, Port Interface
- **의존성**: Domain Layer만 의존

### Adapter Layer

외부 시스템과의 통신을 담당합니다.

- **배치되는 빌딩블록**: Adapter 구현체, Pipeline (자동 생성), Adapter Error
- **의존성**: Domain Layer, Application Layer 의존

### 의존성 규칙

```
Adapter Layer → Application Layer → Domain Layer
(바깥)           (중간)              (안쪽)
```

안쪽 레이어는 바깥 레이어를 절대 참조하지 않습니다. Application Layer가 Adapter 기능이 필요할 때는 Port(인터페이스)를 정의하고, Adapter Layer가 이를 구현합니다.

## 6. 빠른 시작 예제

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

## 7. 가이드 문서 색인

| 문서 | 설명 | 주요 내용 |
|------|------|----------|
| [05-value-objects.md](./05-value-objects.md) | 값 객체 구현 | 기반 클래스, 검증 시스템, 구현 패턴, 실전 예제 |
| [06-entities-and-aggregates.md](./06-entities-and-aggregates.md) | Entity/Aggregate 구현 | 설계 원칙, 클래스 계층, ID 시스템, 도메인 이벤트 |
| [07-domain-events.md](./07-domain-events.md) | 도메인 이벤트 | 이벤트 정의, 발행, 핸들러 구현 |
| [08-error-system.md](./08-error-system.md) | 에러 시스템 | 에러 정의, 네이밍, 테스트 패턴 |
| [11-usecases-and-cqrs.md](./11-usecases-and-cqrs.md) | Usecase 구현 | CQRS 패턴, Apply 병합 |
| [12-ports-and-adapters.md](./12-ports-and-adapters.md) | Adapter 구현 | Port 정의, Adapter 구현, Pipeline |
| [09-domain-services.md](./09-domain-services.md) | 도메인 서비스 | IDomainService, 교차 Aggregate 로직, Usecase 통합 |
| [10-specifications.md](./10-specifications.md) | Specification 패턴 | 비즈니스 규칙 캡슐화, And/Or/Not 조합, Repository 통합 |
| [13-unit-testing.md](./13-unit-testing.md) | 단위 테스트 | 테스트 규칙, 네이밍, 체크리스트 |
| [14-testing-library.md](./14-testing-library.md) | 테스트 라이브러리 | 로그/아키텍처/소스생성기/Job 테스트 |
| [ddd-tactical-improvements.md](./ddd-tactical-improvements.md) | DDD 전술적 설계 갭 분석 | 에릭 에반스 DDD 관점 갭 분석 및 개선 로드맵 |

## 8. 실전 예제 프로젝트

LayeredArch 샘플 프로젝트에서 실제 구현을 확인할 수 있습니다:

| 개념 | 예제 파일 |
|------|----------|
| 값 객체 | `Tests.Hosts/01-SingleHost/LayeredArch.Domain/ValueObjects/` |
| Entity | `Tests.Hosts/01-SingleHost/LayeredArch.Domain/Entities/Product.cs` |
| Repository | `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/AggregateRoots/Products/Ports/IProductRepository.cs` |
| Repository (공통) | `Src/Functorium/Domains/Repositories/IRepository.cs` |
| Usecase | `Tests.Hosts/01-SingleHost/LayeredArch.Application/Usecases/Products/` |
| Domain Service | `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/Services/OrderCreditCheckService.cs` |
| Specification | `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/AggregateRoots/Products/Specifications/` |
| Adapter | `Tests.Hosts/01-SingleHost/LayeredArch.Adapters.Persistence/Repositories/` |

## 9. 참고 자료

- [LanguageExt](https://github.com/louthy/language-ext) - 함수형 프로그래밍 라이브러리
- [Ardalis.SmartEnum](https://github.com/ardalis/SmartEnum) - 타입 안전한 열거형
