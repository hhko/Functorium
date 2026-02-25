# DDD 전술적 설계 개요

이 문서는 DDD 전술적 설계의 전체 그림과 Functorium 프레임워크의 매핑을 설명합니다.

## 목차

- [요약](#요약)
- [왜 DDD 전술적 설계인가](#왜-ddd-전술적-설계인가)
- [DDD 전술적 설계 빌딩블록 (WHAT)](#ddd-전술적-설계-빌딩블록-what)
- [Functorium의 설계 철학](#functorium의-설계-철학)
- [타입 계층 구조](#타입-계층-구조)
- [레이어 아키텍처와 빌딩블록 배치](#레이어-아키텍처와-빌딩블록-배치)
- [모듈과 프로젝트 구조 매핑](#모듈과-프로젝트-구조-매핑)
- [유비쿼터스 언어와 네이밍 가이드](#유비쿼터스-언어와-네이밍-가이드)
- [Bounded Context와 Context Map](#bounded-context와-context-map)
- [빠른 시작 예제](#빠른-시작-예제)
- [가이드 문서 색인](#가이드-문서-색인)
- [실전 예제 프로젝트](#실전-예제-프로젝트)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)
- [참고 문서](#참고-문서)

---

## 요약

### 주요 명령

```csharp
// Value Object 생성
var email = Email.Create("user@example.com");

// Entity/Aggregate 생성
var order = Order.Create(productId, quantity, unitPrice, shippingAddress);

// 도메인 이벤트 발행
order.AddDomainEvent(new CreatedEvent(order.Id, productId, quantity, totalAmount));

// Specification 조합
var spec = priceRange & !lowStock;

// Domain Service 사용 (Usecase 내)
private readonly OrderCreditCheckService _creditCheckService = new();
```

### 주요 절차

1. **Value Object 정의**: `SimpleValueObject<T>` 상속, `Create()` + `Validate()` 구현
2. **Entity/Aggregate 정의**: `AggregateRoot<TId>` 상속, `[GenerateEntityId]` 어트리뷰트 적용
3. **도메인 이벤트 정의**: Aggregate 내 중첩 `sealed record`로 `DomainEvent` 상속
4. **Specification 정의**: `ExpressionSpecification<T>` 상속, `ToExpression()` 구현
5. **Domain Service 정의**: `IDomainService` 마커 인터페이스 구현, 순수 함수로 작성
6. **Usecase 구현**: `ICommandUsecase<T,R>` / `IQueryUsecase<T,R>` 상속, `FinT<IO, T>` LINQ 체인으로 조율

### 주요 개념

| 개념 | 설명 | Functorium 타입 |
|------|------|----------------|
| Value Object | 불변, 값 동등성, 자기 검증 | `SimpleValueObject<T>`, `ValueObject` |
| Entity / Aggregate | ID 동등성, 일관성 경계 | `Entity<TId>`, `AggregateRoot<TId>` |
| Domain Event | 과거형, 불변, Aggregate 간 통신 | `IDomainEvent`, `DomainEvent` |
| Domain Service | 교차 Aggregate 순수 로직 | `IDomainService` |
| Specification | 비즈니스 규칙 캡슐화, 조합 | `Specification<T>`, `ExpressionSpecification<T>` |
| Error 처리 | Railway Oriented Programming | `Fin<T>`, `Validation<Error, T>` |
| Layer 구조 | Domain → Application → Adapter | 의존성 규칙: 안쪽 → 바깥 참조 금지 |

---

## 왜 DDD 전술적 설계인가

### 도메인 복잡성 관리

소프트웨어의 본질적 복잡성은 도메인에서 비롯됩니다. DDD 전술적 설계는 이 복잡성을 **명확한 빌딩블록**으로 분해하여 관리합니다. 각 빌딩블록은 역할과 책임이 명확하여, 개발자가 "이 코드는 어디에 두어야 하는가?"라는 질문에 일관된 답을 제공합니다.

### 유비쿼터스 언어와 코드의 일치

DDD는 도메인 전문가와 개발자가 동일한 언어를 사용할 것을 강조합니다. 코드에서 `Email`, `Order`, `Product`와 같은 도메인 용어를 직접 타입으로 표현하면, 코드가 곧 도메인 모델이 됩니다.

### 비즈니스 규칙의 명시적 표현

"이메일 형식이 올바른가?", "재고가 충분한가?", "주문 상태 전이가 유효한가?" 같은 비즈니스 규칙이 특정 빌딩블록(Value Object, Entity, Aggregate)에 배치되어, 규칙의 위치와 책임이 명확합니다.

## DDD 전술적 설계 빌딩블록 (WHAT)

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
| Command | `ICommandRequest<T>`, `ICommandUsecase<T,R>` | `Functorium.Applications.Cqrs` |
| Query | `IQueryRequest<T>`, `IQueryUsecase<T,R>` | `Functorium.Applications.Cqrs` |
| Event Handler | `IDomainEventHandler<T>` | `Functorium.Applications.Events` |
| Application Error | `ApplicationError`, `ApplicationErrorType` | `Functorium.Applications.Errors` |
| Port | `IObservablePort` | `Functorium.Domains.Observabilities` |
| Repository | `IRepository<TAggregate, TId>` | `Functorium.Domains.Repositories` |
| Adapter | `[GenerateObservablePort]` | Adapter Layer 프로젝트 |
| Adapter Error | `AdapterError`, `AdapterErrorType` | `Functorium.Adapters.Errors` |
| 검증 | `ValidationRules<T>`, `TypedValidation<T,V>` | `Functorium.Domains.ValueObjects.Validations` |
| 결과 타입 | `Fin<T>`, `Validation<Error, T>`, `FinResponse<T>` | LanguageExt / Functorium |

## Functorium의 설계 철학

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
// 값 객체 예시: 이메일 (전체 구현은 §빠른 시작 예제 참조)
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

## 타입 계층 구조

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
├── NotSpecification<T> (internal sealed)
├── ExpressionSpecification<T> (public sealed)
└── AllSpecification<T> (public sealed)
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

## 레이어 아키텍처와 빌딩블록 배치

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

## 모듈과 프로젝트 구조 매핑

### Evans의 Module 개념

에릭 에반스는 Module을 **도메인 개념의 응집도**를 기준으로 그룹화하는 단위로 정의합니다. Module은 패키지나 네임스페이스가 아니라 **의미론적 경계**입니다.

| 원칙 | 설명 |
|------|------|
| 높은 응집도 | 같은 Module 안의 요소는 하나의 도메인 개념을 표현 |
| 낮은 결합도 | Module 간 의존은 최소화하고, 필요 시 Port/Interface로 소통 |
| 커뮤니케이션 | Module 이름이 유비쿼터스 언어를 반영하여 코드 구조만으로 도메인 경계 전달 |

### 이중 축: Layer × Module

Functorium는 **Layer(수평 축)** 와 **Module(수직 축)** 의 이중 축으로 코드를 배치합니다.

- **Layer** — .csproj 단위. 기술적 관심사(Domain, Application, Adapter)를 분리
- **Module** — 폴더/네임스페이스 단위. 도메인 개념(Products, Orders 등)의 응집도를 유지

```
              │ Products  │ Inventories │ Orders  │ Customers │ SharedModels │
──────────────┼───────────┼─────────────┼─────────┼───────────┼──────────────┤
Domain        │ Aggregate │ Aggregate   │Aggregate│ Aggregate │ VO, Entity,  │
(.csproj)     │ VO, Spec  │ Spec        │ VO      │ VO, Spec  │ Event        │
              │ Port      │ Port        │ Port    │ Port      │              │
──────────────┼───────────┼─────────────┼─────────┼───────────┼──────────────┤
Application   │ Command   │ Command     │ Command │ Command   │              │
(.csproj)     │ Query     │ Query       │ Query   │ Query     │              │
              │ EventHdlr │ EventHdlr   │EventHdlr│ EventHdlr │              │
──────────────┼───────────┼─────────────┼─────────┼───────────┼──────────────┤
Adapter       │ Endpoint  │ Endpoint    │Endpoint │ Endpoint  │              │
(.csproj ×3)  │ Repo      │ Repo        │ Repo    │ Repo      │              │
              │ QueryAdpt │ QueryAdpt   │         │           │              │
──────────────┴───────────┴─────────────┴─────────┴───────────┴──────────────┘
```

**매핑 규칙:**

| 축 | 단위 | 분리 기준 | 예시 |
|----|------|----------|------|
| Layer (수평) | .csproj | 기술적 관심사, 의존성 방향 | `LayeredArch.Domain`, `LayeredArch.Application` |
| Module (수직) | 폴더/네임스페이스 | 도메인 개념 응집도 | `AggregateRoots/Products/`, `Usecases/Products/` |

### SingleHost 모듈 경계

SingleHost 프로젝트의 실제 모듈 구성입니다.

| Module | Domain | Application | Adapter |
|--------|--------|-------------|---------|
| **Products** | `AggregateRoots/Products/` (Aggregate, Ports, Specs, VOs) | `Usecases/Products/` (Commands, Queries, Dtos, Ports) | Endpoints, Repository, QueryAdapter |
| **Inventories** | `AggregateRoots/Inventories/` (Aggregate, Ports, Specs) | `Usecases/Inventories/` (Commands, Queries, Dtos, Ports) | Endpoints, Repository, QueryAdapter |
| **Orders** | `AggregateRoots/Orders/` (Aggregate, Ports, VOs) | `Usecases/Orders/` (Commands, Queries) | Endpoints, Repository |
| **Customers** | `AggregateRoots/Customers/` (Aggregate, Ports, Specs, VOs) | `Usecases/Customers/` (Commands, Queries) | Endpoints, Repository |
| **SharedModels** | `SharedModels/` (공유 VO, Entity, Event) | — | — |

> **패턴**: 각 Module은 Domain → Application → Adapter 전 Layer를 관통하는 **수직 슬라이스**입니다. 폴더 이름이 곧 Module 이름이고, Module 이름이 곧 유비쿼터스 언어입니다.

### 모듈 응집도 규칙

**Module 내부 배치 (기본)**

- 특정 Aggregate 전용 타입 → 해당 Aggregate 폴더 내부
- 예: `ProductName` → `AggregateRoots/Products/ValueObjects/`

**SharedModels 이동 기준**

- 2개 이상 Aggregate에서 공유하는 타입 → `SharedModels/`
- 예: `Money`, `Quantity` → `SharedModels/ValueObjects/`

**프로젝트 루트 이동 기준**

- 교차 Aggregate Port → `Domain/Ports/` (예: `IProductCatalog` — Order에서 Product 검증용)
- Domain Service → `Domain/Services/` (예: `OrderCreditCheckService` — 교차 Aggregate 순수 로직)

> 처음에는 Aggregate 전용으로 배치하고, 공유가 필요해지면 SharedModels로 이동합니다. 이 규칙의 상세 판단 기준은 [01-project-structure.md FAQ §3](./01-project-structure.md)을 참조하세요.

### Multi-Aggregate 확장 가이드

서비스가 성장할 때 모듈 구조의 3단계 진화 경로입니다.

| 단계 | 구조 | 설명 |
|------|------|------|
| 1단계 | **단일 Aggregate** | 하나의 Aggregate가 하나의 Module. SingleHost 초기 Product 구조 |
| 2단계 | **Multi-Aggregate 동일 서비스** | 여러 Aggregate가 폴더로 분리되지만 동일 서비스(프로세스) 내 배치. SingleHost 현재 구조 |
| 3단계 | **별도 Bounded Context** | Module이 독립 서비스(.sln)로 분리. Context Map 패턴 필요 |

**2단계 → 3단계 분리 판단 기준:**

| 기준 | 동일 서비스 유지 | 별도 서비스 분리 |
|------|----------------|----------------|
| 배포 주기 | 동일 | Module별 독립 배포 필요 |
| 트랜잭션 경계 | Aggregate 간 같은 DB 공유 가능 | 독립 DB/스키마 필요 |
| 팀 소유권 | 같은 팀 | 다른 팀이 독립적으로 개발 |
| 유비쿼터스 언어 | 용어 충돌 없음 | 같은 용어가 다른 의미 |
| 데이터 저장소 | 동종 (예: 모두 PostgreSQL) | 이종 (예: SQL + NoSQL) |

> **참고**: 3단계 Bounded Context 분리 패턴(Context Map, ACL 등)은 아래 §8 Bounded Context 경계 정의에서 다룹니다.

## 유비쿼터스 언어와 네이밍 가이드

각 빌딩블록의 상세 네이밍 규칙은 개별 가이드에 기술되어 있습니다. 이 섹션은 **모든 빌딩블록의 네이밍 패턴을 한 곳에서 참조**할 수 있는 중앙 색인 역할을 합니다.

### 네이밍 패턴 참조 테이블

| 빌딩블록 | 네이밍 패턴 | 예시 | 상세 참조 |
|----------|-----------|------|----------|
| Value Object | `{Concept}` | `ProductName`, `Email` | [05a-value-objects.md](./05a-value-objects.md) |
| Entity | `{EntityName}` | `Tag` | [06b-entity-aggregate-implementation.md](./06b-entity-aggregate-implementation.md) |
| Aggregate Root | `{Aggregate}` | `Product`, `Order` | [06b-entity-aggregate-implementation.md](./06b-entity-aggregate-implementation.md) |
| Entity ID | `{Aggregate}Id` + `[GenerateEntityId]` | `ProductId`, `OrderId` | [06b-entity-aggregate-implementation.md](./06b-entity-aggregate-implementation.md) |
| Domain Event | `{Aggregate}.{PastTense}Event` (nested record) | `Product.CreatedEvent` | [07-domain-events.md](./07-domain-events.md) |
| Domain Error | `DomainError.For<{Type}>()` | `DomainError.For<Email>()` | [08b-error-system-layers.md](./08b-error-system-layers.md) §4 |
| Domain Service | `{DomainConcept}Service` : `IDomainService` | `OrderCreditCheckService` | [09-domain-services.md](./09-domain-services.md) |
| Specification | `{Aggregate}{Concept}Spec` | `ProductNameUniqueSpec` | [10-specifications.md](./10-specifications.md) |
| Command | `{Verb}{Aggregate}Command` (nested Request/Response/Usecase) | `CreateProductCommand` | [11-usecases-and-cqrs.md](./11-usecases-and-cqrs.md) |
| Query | `{Get/Search}{Description}Query` (nested Request/Response/Usecase) | `SearchProductsQuery` | [11-usecases-and-cqrs.md](./11-usecases-and-cqrs.md) |
| Event Handler | `On{DomainEvent}` | `OnProductCreated` | [01-project-structure.md](./01-project-structure.md) |
| Repository Interface | `I{Aggregate}Repository` | `IProductRepository` | [12-ports.md](./12-ports.md) |
| Repository Impl | `{Technology}{Aggregate}Repository` | `EfCoreProductRepository` | [13-adapters.md](./13-adapters.md) |
| Query Adapter Interface | `I{Aggregate}QueryAdapter` | `IProductQueryAdapter` | [12-ports.md](./12-ports.md) |
| Query Adapter Impl | `{Technology}{Aggregate}QueryAdapter` | `DapperProductQueryAdapter` | [13-adapters.md](./13-adapters.md) |
| Cross-Aggregate Port | `I{Concept}` | `IProductCatalog` | [12-ports.md](./12-ports.md) |
| Endpoint | `{Verb}{Aggregate}Endpoint` | `CreateProductEndpoint` | [01-project-structure.md](./01-project-structure.md) |
| Persistence Model | `{Aggregate}Model` | `ProductModel` | [13-adapters.md](./13-adapters.md) |
| Mapper | `{Aggregate}Mapper` | `ProductMapper` | [13-adapters.md](./13-adapters.md) |
| Module (폴더) | 복수 명사 (유비쿼터스 언어) | `Products/`, `Orders/` | §6 |

### 용어집 템플릿

도메인 전문가와 개발자가 공유하는 용어집을 유지하면, 코드 네이밍과 비즈니스 용어의 괴리를 방지할 수 있습니다.

| 도메인 용어 | 정의 | 코드 타입 | 비고 |
|------------|------|----------|------|
| 상품 | 판매 카탈로그의 개별 항목 | `Product` (Aggregate) | |
| 재고 | 상품의 가용 수량 | `Inventory` (Aggregate) | Product와 1:1 |
| 주문 | 고객의 구매 요청 | `Order` (Aggregate) | |
| 금액 | 통화 + 수치 조합 | `Money` (Value Object) | SharedModels |
| 수량 | 0 이상의 정수 값 | `Quantity` (Value Object) | SharedModels |

> **활용**: 프로젝트별 용어집을 위 형식으로 작성하여 도메인 전문가와 공유합니다. 용어가 변경되면 코드 타입명도 함께 변경합니다.

### 도메인 전문가 협업

- 용어집은 도메인 전문가와 개발자가 **반복적으로 합의**하여 유지합니다.
- 코드에서 도메인 용어와 다른 이름을 사용하면 커뮤니케이션 비용이 증가합니다. 용어 충돌 발견 시 즉시 용어집을 갱신하고 코드를 리네이밍합니다.
- 새 빌딩블록 추가 시 위의 네이밍 패턴 테이블을 참조하여 일관된 이름을 부여합니다.

## Bounded Context와 Context Map

현재 SingleHost 프로젝트는 **단일 Bounded Context** 내에서 여러 Module(Products, Orders 등)을 운영합니다. 이 섹션은 서비스가 성장하여 다중 Bounded Context로 분리될 때 적용할 **Context Map 패턴**을 정의하고, 기존 코드에서 이미 존재하는 선행 패턴을 식별합니다.

### Context Map 패턴

| 패턴 | 설명 | Functorium 매핑 |
|------|------|----------------|
| **Shared Kernel** | 두 BC가 공유하는 도메인 모델 부분집합 | `SharedModels/` 폴더 (`Money`, `Quantity`) |
| **Customer-Supplier** | 상류 BC가 하류 BC에 API 제공 | 미구현 (향후 서비스 간 REST API) |
| **Anti-Corruption Layer (ACL)** | 외부 모델 오염 방지 변환 계층 | `IProductCatalog` Port + EF Core Mapper |
| **Open Host Service** | 표준 프로토콜로 공개 API 제공 | REST Endpoints |
| **Published Language** | BC 간 공유 언어 (이벤트/스키마) | Domain Events (향후 Integration Event) |
| **Conformist** | 하류가 상류 모델을 그대로 수용 | 미구현 |
| **Separate Ways** | BC 간 통합 없이 독립 운영 | 미구현 |

### SingleHost에서의 선행 패턴 인식

기존 코드에는 이미 Context Map 패턴의 **단일 서비스 내 선행 구현**이 존재합니다. 서비스 분리 시 이 패턴들이 BC 간 통합 지점이 됩니다.

**Shared Kernel → `SharedModels/ValueObjects/`**

`Money`, `Quantity` 등 여러 Module이 공유하는 Value Object가 `SharedModels/` 폴더에 배치되어 있습니다. 서비스 분리 시 NuGet 패키지로 추출하거나 각 BC에 복제하는 결정이 필요합니다.

**ACL (mini) → `IProductCatalog` Port + Adapter**

Order Module이 Product 데이터를 조회할 때 `IProductCatalog` Port를 통해 접근합니다. 현재는 동일 프로세스 내 EF Core 구현이지만, 서비스 분리 시 원격 API 호출 + 응답 변환 계층(ACL)으로 교체됩니다.

**Domain Events as Published Language**

현재 Domain Event는 in-process Mediator로 발행됩니다. 서비스 분리 시 메시지 브로커(RabbitMQ, Kafka 등)를 통한 Integration Event로 전환되며, 이때 Domain Event와 Integration Event의 분리가 필요합니다.

### Multi-Context 프로젝트 구조

다중 Bounded Context로 분리될 때의 개념적 프로젝트 구조입니다.

```
Services/
├── ProductCatalog/                ← BC 1 (기존 3-Layer 구조 동일)
│   ├── ProductCatalog.Domain/
│   ├── ProductCatalog.Application/
│   └── ProductCatalog.Adapters.*/
├── OrderManagement/               ← BC 2
│   ├── OrderManagement.Domain/
│   ├── OrderManagement.Application/
│   └── OrderManagement.Adapters.*/
SharedModels/                      ← 공유 NuGet 패키지
IntegrationEvents/                 ← Published Language (BC 간 공유 이벤트 스키마)
```

> 각 BC는 §5의 3-Layer 구조(Domain → Application → Adapter)를 그대로 유지합니다. BC 간 통신만 Cross-Aggregate Port 대신 Integration Event 또는 REST API로 교체됩니다.

### 진화 경로와의 관계

§6의 Multi-Aggregate 확장 가이드에서 **3단계 분리 판단 기준(WHEN)** 을 제시했습니다. 이 섹션의 Context Map 패턴은 분리를 결정한 후 **어떻게(HOW)** 구현할지를 안내합니다.

- **WHEN**: 배포 주기, 트랜잭션 경계, 팀 소유권, 유비쿼터스 언어 충돌, 데이터 저장소 이종성 → §6 판단 기준 테이블
- **HOW**: Shared Kernel, ACL, Published Language, Open Host Service → 이 섹션의 Context Map 패턴

## 빠른 시작 예제

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

## 가이드 문서 색인

| 문서 | 설명 | 주요 내용 |
|------|------|----------|
| [05a-value-objects.md](./05a-value-objects.md) | 값 객체 구현 | 기반 클래스, 검증 시스템, 구현 패턴, 실전 예제 |
| [05b-value-objects-validation.md](./05b-value-objects-validation.md) | 값 객체 검증·열거형 | 열거형 구현, Application 검증, FAQ |
| [06a-aggregate-design.md](./06a-aggregate-design.md) | Aggregate 설계 | 설계 원칙, 경계 설정, 안티패턴 |
| [06b-entity-aggregate-implementation.md](./06b-entity-aggregate-implementation.md) | Entity/Aggregate 구현 | 클래스 계층, ID 시스템, 도메인 이벤트 |
| [07-domain-events.md](./07-domain-events.md) | 도메인 이벤트 | 이벤트 정의, 발행, 핸들러 구현 |
| [08a-error-system.md](./08a-error-system.md) | 에러 시스템: 기초와 네이밍 | 에러 처리 원칙, Fin 패턴, 네이밍 규칙 |
| [08b-error-system-layers.md](./08b-error-system-layers.md) | 에러 시스템: 레이어별 구현과 테스트 | 레이어별 에러 정의, 테스트 패턴, 체크리스트 |
| [11-usecases-and-cqrs.md](./11-usecases-and-cqrs.md) | Usecase 구현 | CQRS 패턴, Apply 병합 |
| [12-ports.md](./12-ports.md) | Port 아키텍처 | Port 정의, IObservablePort 계층 |
| [13-adapters.md](./13-adapters.md) | Adapter 구현 | Repository, External API, Messaging, Query |
| [14-adapter-wiring.md](./14-adapter-wiring.md) | Adapter 연결 | Pipeline, DI, Options, 테스트 |
| [09-domain-services.md](./09-domain-services.md) | 도메인 서비스 | IDomainService, 교차 Aggregate 로직, Usecase 통합 |
| [10-specifications.md](./10-specifications.md) | Specification 패턴 | 비즈니스 규칙 캡슐화, And/Or/Not 조합, Repository 통합 |
| [15a-unit-testing.md](./15a-unit-testing.md) | 단위 테스트 | 테스트 규칙, 네이밍, 체크리스트 |
| [16-testing-library.md](./16-testing-library.md) | 테스트 라이브러리 | 로그/아키텍처/소스생성기/Job 테스트 |


## 실전 예제 프로젝트

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

## 트러블슈팅

### Value Object의 Create()가 항상 실패한다

**원인:** `Validate()` 메서드에서 `null`이나 빈 문자열을 처리하지 않거나, 정규식 패턴이 잘못되었을 수 있습니다.

**해결:** `Validate()` 메서드에서 `null` 처리를 확인하고, `ValidationRules<T>.NotEmpty(value ?? "")` 패턴을 사용하세요. 정규식 패턴은 별도 단위 테스트로 검증하세요.

### Entity의 ID가 비교되지 않는다 (동등성 실패)

**원인:** `[GenerateEntityId]` 어트리뷰트 없이 직접 ID 타입을 정의했거나, `IEntityId<T>`를 구현하지 않았을 수 있습니다.

**해결:** Entity ID는 반드시 `[GenerateEntityId]` 소스 생성기를 사용하세요. 소스 생성기가 `Equals()`, `GetHashCode()`, `==`, `!=` 연산자를 자동 생성합니다.

### 도메인 로직을 어디에 배치해야 할지 모르겠다

**원인:** 빌딩블록 간 역할 구분이 명확하지 않을 때 발생합니다.

**해결:** 다음 판단 기준을 따르세요:
1. 단일 Aggregate 내부 → Entity 메서드 또는 Value Object
2. 여러 Aggregate 참조 + I/O 없음 → Domain Service
3. I/O 필요 (Repository, 외부 API) → Usecase에서 조율
4. 상태 변경 후 부수 효과 → Domain Event + Event Handler

---

## FAQ

### Q1. Value Object와 Entity의 선택 기준은?

식별자(ID)가 필요한지 여부가 핵심입니다. `Money`, `Email`처럼 값 자체로 동등성을 판단하면 Value Object, `Order`, `Product`처럼 고유 ID로 추적해야 하면 Entity입니다. 일반적으로 Value Object가 더 많고, Entity가 소수여야 합니다.

### Q2. Aggregate 경계를 어떻게 설정하나요?

하나의 트랜잭션에서 일관성을 보장해야 하는 범위가 Aggregate 경계입니다. Aggregate를 작게 유지하고, Aggregate 간 참조는 ID만 사용하세요. 상세 설계 원칙은 [06a-aggregate-design.md](./06a-aggregate-design.md)를 참조하세요.

### Q3. SharedModels에 배치해야 할 타입의 기준은?

2개 이상의 Aggregate에서 공유하는 Value Object나 Entity가 대상입니다. 처음에는 특정 Aggregate 내부에 배치하고, 실제로 공유가 필요해진 시점에 `SharedModels/`로 이동하세요.

### Q4. `Fin<T>`와 `Validation<Error, T>`는 언제 사용하나요?

`Fin<T>`는 최종 결과(성공 또는 단일 에러)에, `Validation<Error, T>`는 검증 결과(여러 에러 누적)에 사용합니다. Value Object의 `Create()`는 `Fin<T>`를, `Validate()`는 `Validation<Error, T>`를 반환합니다.

### Q5. 다중 Bounded Context로 분리하는 시점은?

배포 주기가 다르거나, 팀 소유권이 분리되거나, 동일 용어가 다른 의미로 쓰이거나, 이종 데이터 저장소가 필요한 경우 분리를 검토하세요. 분리 방법은 Context Map 패턴(Shared Kernel, ACL, Published Language 등)을 적용합니다.

---

## 참고 문서

- [LanguageExt](https://github.com/louthy/language-ext) - 함수형 프로그래밍 라이브러리
- [Ardalis.SmartEnum](https://github.com/ardalis/SmartEnum) - 타입 안전한 열거형
