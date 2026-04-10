---
title: "Entity and Aggregate Implementation — Core Patterns"
---

This document covers the core methods for implementing Entities and Aggregates with the Functorium framework. 설계 원칙과 개념은 [06a-aggregate-design.md](./06a-aggregate-design)를 참조하세요. 고급 패턴(Cross-Aggregate 관계, 부가 인터페이스, 실전 예제)은 [06c-entity-aggregate-advanced.md](./06c-entity-aggregate-advanced)를 참조하세요.

## Introduction

"Which base class should be used to implement the Aggregate Root and child Entities?"
"Who is responsible for validation during Entity creation, and how is ORM restoration distinguished?"
"How is a business rule violation expressed in a method signature?"

Entity and Aggregate implementation is the core of domain modeling. This document covers patterns needed for actual implementation, from the base class hierarchy provided by the Functorium framework to creation patterns, command methods, and child Entity management.

### What You Will Learn

Through this document, you will learn:

1. **Entity\<TId\>와 AggregateRoot\<TId\>의 클래스 계층** — 기반 클래스가 제공하는 기능과 역할
2. **Ulid 기반 Entity ID 시스템** — 소스 생성기를 통한 타입 안전한 식별자 자동 생성
3. **Create / CreateFromValidated 생성 패턴** — 새 Entity 생성과 ORM 복원의 분리
4. **커맨드 메서드와 Immutable식 보호** — `Fin<T>` 반환으로 비즈니스 규칙 위반을 타입으로 표현
5. **자식 Entity 구현과 이벤트 발행** — Aggregate Root를 통한 자식 관리와 도메인 이벤트

### Prerequisites

A basic understanding of the following concepts is required to understand this document:

- [Aggregate 설계 원칙](./06a-aggregate-design) — Aggregate 경계와 설계 원칙 (WHY)
- [값 객체 구현 가이드](./05a-value-objects) — Value Object 구현 패턴
- [에러 시스템: 기초와 네이밍](./08a-error-system) — `Fin<T>`와 에러 반환 패턴

> The core of Entity and Aggregate implementation is **separation of validation responsibilities.** Value Object가 원시 값의 유효성을 보장하고, Entity는 이미 검증된 Value Object를 받아 조합합니다. 비즈니스 규칙 위반은 `Fin<T>` 반환으로 타입 시스템에 명시하여, 호출자가 반드시 실패를 처리하도록 강제합니다.

## Summary

### Key Commands

```csharp
// Entity ID 생성 (Ulid 기반)
var productId = ProductId.New();

// Aggregate Root 생성 (검증된 VO를 직접 받음)
var product = Product.Create(name, description, price, stockQuantity);

// ORM 복원용 팩토리
var product = Product.CreateFromValidated(id, name, ..., createdAt, updatedAt);

// 커맨드 메서드 (불변식 보호, Fin<T> 반환)
Fin<Unit> result = order.Confirm(updatedBy);

// 도메인 이벤트 발행
AddDomainEvent(new CreatedEvent(Id, customerId, totalAmount));
```

### Key Procedures

1. `[GenerateEntityId]` 속성 적용하여 EntityId 소스 생성
2. `AggregateRoot<TId>` (또는 `Entity<TId>`) 상속
3. `Create()` 팩토리 메서드 구현 - 검증된 VO를 받아 Entity 생성 + 도메인 이벤트 발행
4. `CreateFromValidated()` 메서드 구현 - ORM 복원용 (검증 없음)
5. 커맨드 메서드 구현 - Immutable식 검사 후 `Fin<T>` 반환
6. 도메인 이벤트를 중첩 record로 정의하고 상태 변경 시 발행

### Key Concepts

| Concept | Description |
|------|------|
| Entity vs AggregateRoot | Entity는 ID 기반 동등성, AggregateRoot는 트랜잭션 경계 + 이벤트 발행 |
| Create / CreateFromValidated | Create는 새 Entity 생성(검증), CreateFromValidated는 DB 복원(검증 없음) |
| 커맨드 메서드 | Immutable식 위반 시 `Fin.Fail` 반환, 성공 시 상태 변경 + 이벤트 발행 |
| Ulid 기반 ID | 분산 생성 가능, 시간 순서 보장, 인덱스 성능 우수 |

---

## Class Hierarchy

### Class Hierarchy

Functorium provides a base class hierarchy for Entity implementation.

```
IEntity<TId> (인터페이스)
+-- TId Id
+-- CreateMethodName 상수
`-- CreateFromValidatedMethodName 상수
    |
    `-- Entity<TId> (추상 클래스)
        +-- Id 속성 (protected init)
        +-- Equals() / GetHashCode() - ID 기반 동등성
        +-- == / != 연산자
        +-- CreateFromValidation<TEntity, TValue>() 헬퍼
        `-- GetUnproxiedType() - ORM 프록시 지원
            |
            `-- AggregateRoot<TId> : IDomainEventDrain
                +-- DomainEvents (읽기 전용, IHasDomainEvents)
                +-- AddDomainEvent() (protected)
                `-- ClearDomainEvents() (IDomainEventDrain)
```

The following 각 계층이 어떤 역할을 담당하는지 정리한 것입니다.

- **IEntity\<TId\>**: Entity의 계약을 정의하는 인터페이스. `Create`, `CreateFromValidated` 메서드명 상수를 포함합니다.
- **Entity\<TId\>**: ID 기반 동등성(`Equals`, `GetHashCode`, `==`, `!=`)을 자동 구현. ORM 프록시 타입도 처리합니다.
- **AggregateRoot\<TId\>**: 도메인 이벤트 관리 기능을 제공합니다. `IDomainEventDrain`(internal)을 구현하여, 이벤트 조회(`IHasDomainEvents`)와 정리(`IDomainEventDrain`)를 분리합니다.

### Entity\<TId\>

ID 기반 동등성을 제공하는 Entity의 기반 추상 클래스입니다.

**Location**: `Functorium.Domains.Entities.Entity<TId>`

```csharp
[Serializable]
public abstract class Entity<TId> : IEntity<TId>, IEquatable<Entity<TId>>
    where TId : struct, IEntityId<TId>
{
    // Entity의 고유 식별자
    public TId Id { get; protected init; }

    // 기본 생성자 (ORM/직렬화용)
    protected Entity();

    // ID를 지정하여 Entity 생성
    protected Entity(TId id);

    // ID 기반 동등성 비교
    public override bool Equals(object? obj);
    public bool Equals(Entity<TId>? other);
    public override int GetHashCode();

    // 동등성 연산자
    public static bool operator ==(Entity<TId>? a, Entity<TId>? b);
    public static bool operator !=(Entity<TId>? a, Entity<TId>? b);

    // 팩토리 헬퍼 메서드
    public static Fin<TEntity> CreateFromValidation<TEntity, TValue>(
        Validation<Error, TValue> validation,
        Func<TValue, TEntity> factory)
        where TEntity : Entity<TId>;
}
```

The items that must be included when implementing an Entity are as follows.

| Item | Description |
|------|------|
| `[GenerateEntityId]` 속성 | EntityId 자동 생성 |
| Private 생성자 (ORM용) | 파라미터 없는 기본 생성자 + `#pragma warning disable CS8618` |
| Private 생성자 (내부용) | ID를 받는 생성자 |
| `Create()` | Entity 생성 팩토리 메서드 |
| `CreateFromValidated()` | ORM 복원용 메서드 |

> Entity 구현 예제는 [§생성 패턴](#생성-패턴)에서 확인할 수 있습니다.

### AggregateRoot\<TId\>

도메인 이벤트 관리 기능을 제공하는 Aggregate Root의 기반 추상 클래스입니다.

**Location**: `Functorium.Domains.Entities.AggregateRoot<TId>`

```csharp
public abstract class AggregateRoot<TId> : Entity<TId>, IDomainEventDrain
    where TId : struct, IEntityId<TId>
{
    // 도메인 이벤트 목록 (읽기 전용, IHasDomainEvents)
    public IReadOnlyList<IDomainEvent> DomainEvents { get; }

    // 기본 생성자 (ORM/직렬화용)
    protected AggregateRoot();

    // ID를 지정하여 Aggregate Root 생성
    protected AggregateRoot(TId id);

    // 도메인 이벤트 추가
    protected void AddDomainEvent(IDomainEvent domainEvent);

    // 모든 도메인 이벤트 제거 (IDomainEventDrain)
    public void ClearDomainEvents();
}
```

**인터페이스 분리 원칙**:
- `IHasDomainEvents`: 도메인 계층의 읽기 전용 계약 (이벤트 조회만 허용)
- `IDomainEventDrain` (internal): 이벤트 발행 후 정리를 위한 인프라 인터페이스
- 도메인 이벤트는 Immutable의 사실(fact)이므로, 도메인 계약에서 개별 삭제 메서드를 제공하지 않습니다

다음 예제에서 The key point is `AddDomainEvent()`로 이벤트를 발행하고, 커맨드 메서드가 `Fin<Unit>`을 반환하여 Immutable식 위반을 표현하는 구조입니다.

```csharp
[GenerateEntityId]
public class Order : AggregateRoot<OrderId>
{
    #region Error Types

    // 상태 전이 위반 에러 타입
    public sealed record InvalidOrderStatusTransition : DomainErrorType.Custom;

    #endregion

    public Money TotalAmount { get; private set; }
    // OrderStatus: SimpleValueObject<string> 기반 Smart Enum (상세: §6c 실전 예제)
    public OrderStatus Status { get; private set; }

#pragma warning disable CS8618
    private Order() { }
#pragma warning restore CS8618

    private Order(OrderId id, Money totalAmount) : base(id)
    {
        TotalAmount = totalAmount;
        Status = OrderStatus.Pending;
    }

    // Create: 이미 검증된 Value Object를 직접 받음
    public static Order Create(Money totalAmount)
    {
        var id = OrderId.New();
        var order = new Order(id, totalAmount);
        order.AddDomainEvent(new OrderCreatedEvent(id, totalAmount));
        return order;
    }

    // 상태 전이 — TransitionTo()에 위임하여 전이 규칙을 중앙화
    public Fin<Unit> Confirm() => TransitionTo(OrderStatus.Confirmed, new OrderConfirmedEvent(Id));

    private Fin<Unit> TransitionTo(OrderStatus target, DomainEvent domainEvent)
    {
        if (!Status.CanTransitionTo(target))
            return DomainError.For<Order, string, string>(
                new InvalidOrderStatusTransition(),
                value1: Status,
                value2: target,
                message: $"Cannot transition from '{Status}' to '{target}'");

        Status = target;
        AddDomainEvent(domainEvent);
        return unit;
    }
}
```

### Supplementary Interface Summary

Aggregate/Entity에 혼합하여 사용하는 부가 인터페이스입니다. 상세 구현과 사용 예제는 [06c-entity-aggregate-advanced.md](./06c-entity-aggregate-advanced)를 참조하세요.

| 인터페이스 | 속성 | Purpose |
|-----------|------|------|
| `IAuditable` | `DateTime CreatedAt`, `Option<DateTime> UpdatedAt` | 생성/수정 시각 추적 |
| `IAuditableWithUser` | + `Option<string> CreatedBy/UpdatedBy` | + 사용자 추적 |
| `ISoftDeletable` | `Option<DateTime> DeletedAt`, `bool IsDeleted` | 소프트 삭제 |
| `ISoftDeletableWithUser` | + `Option<string> DeletedBy` | + 삭제자 추적 |
| `IConcurrencyAware` | `byte[] RowVersion` | 낙관적 동시성 제어 |

클래스 계층을 이해했으니, 이제 Entity를 고유하게 식별하는 ID 시스템을 살펴보겠습니다.

---

## Entity ID System

Functorium provides a type-safe Entity ID system. Ulid 기반으로 시간 순서 정렬이 가능하며, 소스 생성기를 통해 자동으로 생성됩니다.

### IEntityId\<T\> 인터페이스

**Location**: `Functorium.Domains.Entities.IEntityId<T>`

```csharp
public interface IEntityId<T> : IEquatable<T>, IComparable<T>, IParsable<T>
    where T : struct, IEntityId<T>
{
    // Ulid 값
    Ulid Value { get; }

    // 새로운 EntityId 생성
    static abstract T New();

    // Ulid로부터 EntityId 생성
    static abstract T Create(Ulid id);

    // 문자열로부터 EntityId 생성
    static abstract T Create(string id);
}
```

**왜 Ulid인가요?**

Functorium이 GUID 대신 Ulid를 Optional한 이유를 비교하면 다음과 같습니다.

| Characteristics | GUID | Ulid |
|------|------|------|
| 크기 | 128bit | 128bit |
| 정렬 | 무작위 | 시간 순서 |
| 가독성 | 36자 (하이픈 포함) | 26자 |
| 인덱스 성능 | 낮음 (무작위) | 높음 (순차) |

The key difference is 정렬과 인덱스 성능입니다. Ulid는 시간 순서로 정렬되므로 데이터베이스 인덱스 성능이 좋고, 생성 시간을 추출할 수 있습니다.

### EntityIdGenerator (소스 생성기)

When the `[GenerateEntityId]` attribute is applied to an Entity class, the ID type for that Entity is automatically generated.

**Location**: `Functorium.Domains.Entities.GenerateEntityIdAttribute`

```csharp
using Functorium.Domains.Entities;

[GenerateEntityId]  // ProductId, ProductIdComparer, ProductIdConverter 자동 생성
public class Product : Entity<ProductId>
{
    // ...
}
```

### 생성되는 코드

`[GenerateEntityId]`는 다음 타입들을 자동 생성합니다. ID 자체뿐 아니라 EF Core 통합과 직렬화에 필요한 보조 타입까지 모두 포함됩니다.

| 생성 타입 | Purpose |
|----------|------|
| `{Entity}Id` struct | Entity 식별자 (Ulid 기반) |
| `{Entity}IdComparer` | EF Core ValueComparer |
| `{Entity}IdConverter` | EF Core ValueConverter (string ↔ EntityId) |
| `{Entity}IdJsonConverter` | System.Text.Json 직렬화 (내장) |
| `{Entity}IdTypeConverter` | TypeConverter 지원 (내장) |

**생성된 EntityId 구조:**

```csharp
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(ProductIdJsonConverter))]
[TypeConverter(typeof(ProductIdTypeConverter))]
public readonly partial record struct ProductId :
    IEntityId<ProductId>,
    IParsable<ProductId>
{
    // 타입 이름 상수
    public const string Name = "ProductId";

    // 빈 값 상수
    public static readonly ProductId Empty = new(Ulid.Empty);

    // Ulid 값
    public Ulid Value { get; init; }

    // 팩토리 메서드
    public static ProductId New();              // 새 ID 생성
    public static ProductId Create(Ulid id);    // Ulid에서 생성
    public static ProductId Create(string id);  // 문자열에서 생성

    // 비교 연산자
    public int CompareTo(ProductId other);
    public static bool operator <(ProductId left, ProductId right);
    public static bool operator >(ProductId left, ProductId right);
    public static bool operator <=(ProductId left, ProductId right);
    public static bool operator >=(ProductId left, ProductId right);

    // IParsable 구현
    public static ProductId Parse(string s, IFormatProvider? provider);
    public static bool TryParse(string? s, IFormatProvider? provider, out ProductId result);

    // 내장 JsonConverter, TypeConverter
    // ...
}
```

While the ID system provides the means to identify Entities, creation patterns define how to safely create them.

---

## Creation Patterns

The core of Entity implementation is **separation of validation responsibilities.** Value Object와 Entity는 서로 다른 검증 책임을 가집니다.

- **Value Object**: 원시 값을 받아 자신의 유효성을 검증
- **Entity**: 이미 검증된 Value Object를 받아 조합. Entity 레벨 비즈니스 규칙이 있을 때만 Validate 정의

### Value Object vs Entity의 역할 차이

| Category | Value Object | Entity |
|------|--------------|--------|
| **Validate** | 원시 값 → 검증된 값 반환 | Entity 레벨 비즈니스 규칙만 |
| **Create** | 원시 값 받음 | **Value Object를 직접 받음** |
| **검증 책임** | 자신의 값 검증 | VO 간 관계/규칙 검증 |

> **참고**: Value Object의 검증 패턴은 [값 객체 구현 가이드 - 구현 패턴](./05a-value-objects#구현-패턴)을 참고하세요.

### Create / CreateFromValidated 패턴

Entities provide two creation paths. 각 경로의 용도와 동작 차이를 확인하세요.

| Method | Purpose | 검증 | ID 생성 |
|--------|------|------|---------|
| `Create()` | 새 Entity 생성 | VO가 이미 검증됨 | 새로 생성 |
| `CreateFromValidated()` | ORM/Repository 복원 | 없음 | 기존 ID 사용 |

**Create 메서드:**

Used when creating a new Entity. **이미 검증된 Value Object를 직접 받습니다.**

```csharp
// Create: 이미 검증된 Value Object를 직접 받음
public static Product Create(ProductName name, ProductDescription description, Money price)
{
    var id = ProductId.New();  // 새 ID 생성
    var product = new Product(id, name, description, price);
    product.AddDomainEvent(new CreatedEvent(product.Id, name, price));
    return product;
}
```

**CreateFromValidated 메서드:**

Used when restoring an Entity from ORM or Repository. Values read from the database are already validated, so they are not validated again.

```csharp
public static Product CreateFromValidated(
    ProductId id,
    ProductName name,
    ProductDescription description,
    Money price,
    DateTime createdAt,
    Option<DateTime> updatedAt)
{
    return new Product(id, name, description, price)
    {
        CreatedAt = createdAt,
        UpdatedAt = updatedAt
    };
}
```

**Why are two methods needed?**

1. **성능**: 데이터베이스에서 대량의 Entity를 로드할 때 검증을 건너뛰어 성능을 향상시킵니다.
2. **의미**: 새 Entity 생성과 기존 Entity 복원은 다른 의미를 가집니다.
3. **ID 관리**: Create는 새 ID를 생성하고, CreateFromValidated는 기존 ID를 사용합니다.

### 패턴 1: 정적 Create() 팩토리 메서드

Aggregate Root는 **`Create` 정적 팩토리 메서드로** 생성합니다. 생성자는 `private`으로 캡슐화합니다. 이미 검증된 Value Object를 받아 새 Aggregate를 생성하고, ID를 자동 발급하며 도메인 이벤트를 발행합니다.

```csharp
// Customer Aggregate: 단순 생성
public static Customer Create(
    CustomerName name,
    Email email,
    Money creditLimit)
{
    var customer = new Customer(CustomerId.New(), name, email, creditLimit);
    customer.AddDomainEvent(new CreatedEvent(customer.Id, name, email));
    return customer;
}
```

```csharp
// Product Aggregate: 생성 + 초기 상태 설정
public static Product Create(
    ProductName name,
    ProductDescription description,
    Money price)
{
    var product = new Product(ProductId.New(), name, description, price);
    product.AddDomainEvent(new CreatedEvent(product.Id, name, price));
    return product;
}
```

**Create() Comparison Across All Aggregate Roots:**

| Aggregate | 매개변수 | ID 생성 | 이벤트 |
|-----------|---------|---------|--------|
| `Product.Create()` | `ProductName, ProductDescription, Money` | `ProductId.New()` | `CreatedEvent` |
| `Inventory.Create()` | `ProductId, Quantity` | `InventoryId.New()` | `CreatedEvent` |
| `Order.Create()` | `ProductId, Quantity, Money, ShippingAddress` | `OrderId.New()` | `CreatedEvent` |
| `Customer.Create()` | `CustomerName, Email, Money` | `CustomerId.New()` | `CreatedEvent` |

**Common Rules:**
- `private` 생성자 + `public static Create()` 조합
- 매개변수는 **이미 검증된 Value Object** (primitive 아님)
- ID는 `XxxId.New()`로 내부에서 자동 생성
- 도메인 이벤트를 생성 시점에 발행

### 패턴 2: CreateFromValidated() ORM 복원

DB에서 읽어온 데이터로 Aggregate를 복원합니다. 이미 한 번 검증을 통과한 데이터이므로 검증을 생략합니다.

```csharp
// Product.cs
public static Product CreateFromValidated(
    ProductId id,
    ProductName name,
    ProductDescription description,
    Money price,
    DateTime createdAt,
    Option<DateTime> updatedAt)
{
    return new Product(id, name, description, price)
    {
        CreatedAt = createdAt,
        UpdatedAt = updatedAt
    };
}
```

**Create vs CreateFromValidated 비교:**

| Item | `Create()` | `CreateFromValidated()` |
|------|-----------|------------------------|
| Purpose | 새 Aggregate 생성 | ORM/Repository 복원 |
| ID 생성 | `XxxId.New()` 자동 발급 | 외부에서 전달 |
| 검증 | VO가 이미 검증됨 | 검증 스킵 (DB 데이터 신뢰) |
| 이벤트 발행 | `AddDomainEvent()` 호출 | 이벤트 없음 |
| Audit 필드 | 자동 설정 (`DateTime.UtcNow`) | 외부에서 전달 |

### Entity.Validate가 필요한 경우 vs 불필요한 경우

**불필요한 경우** — VO 단순 조합:
```csharp
// Value Object가 이미 검증됨 → Entity.Validate 불필요
public static Order Create(Money amount, CustomerId customerId)
{
    var id = OrderId.New();
    return new Order(id, amount, customerId);
}
```

**필요한 경우** — Entity 레벨 비즈니스 규칙 (VO 간 관계):

다음 예제에서 The key point is `Validate`가 `Validation<Error, Unit>`을 반환하고, `Create`가 이를 호출한 뒤 `ToFin()`으로 변환하는 흐름입니다.

```csharp
// 판매가 > 원가 규칙은 Entity 레벨의 검증
[GenerateEntityId]
public class Product : Entity<ProductId>
{
    #region Error Types

    public sealed record SellingPriceBelowCost : DomainErrorType.Custom;

    #endregion

    public ProductName Name { get; private set; }
    public Price SellingPrice { get; private set; }
    public Money Cost { get; private set; }

    // Validate: Entity 레벨 비즈니스 규칙 (판매가 > 원가)
    public static Validation<Error, Unit> Validate(Price sellingPrice, Money cost) =>
        sellingPrice.Value > cost.Amount
            ? Success<Error, Unit>(unit)
            : DomainError.For<Product>(
                new SellingPriceBelowCost(),
                sellingPrice.Value,
                $"Selling price must be greater than cost. Price: {sellingPrice.Value}, Cost: {cost.Amount}");

    // Create: Validate 호출 후 Entity 생성
    public static Fin<Product> Create(ProductName name, Price sellingPrice, Money cost) =>
        Validate(sellingPrice, cost)
            .Map(_ => new Product(ProductId.New(), name, sellingPrice, cost))
            .ToFin();
}
```

```csharp
// 시작일 < 종료일 규칙은 Entity 레벨
[GenerateEntityId]
public class Subscription : Entity<SubscriptionId>
{
    #region Error Types

    public sealed record StartAfterEnd : DomainErrorType.Custom;

    #endregion

    public Date StartDate { get; private set; }
    public Date EndDate { get; private set; }
    public CustomerId CustomerId { get; private set; }

    // Validate: Entity 레벨 비즈니스 규칙 (시작일 < 종료일)
    public static Validation<Error, Unit> Validate(Date startDate, Date endDate) =>
        startDate < endDate
            ? Success<Error, Unit>(unit)
            : DomainError.For<Subscription>(
                new StartAfterEnd(),
                startDate.Value,
                $"Start date must be before end date. Start: {startDate.Value}, End: {endDate.Value}");

    // Create: Validate 호출 후 Entity 생성
    public static Fin<Subscription> Create(Date startDate, Date endDate, CustomerId customerId) =>
        Validate(startDate, endDate)
            .Map(_ => new Subscription(SubscriptionId.New(), startDate, endDate, customerId))
            .ToFin();
}
```

### Factory Pattern Design Guidelines

| Scenario | 권장 방식 | Example |
|---------|---------|------|
| 단순 생성 (VO만 필요) | 정적 `Create()` 직접 호출 | `Customer.Create(name, email, creditLimit)` |
| 병렬 VO 검증 필요 | Apply 패턴 (Usecase 내부) | `CreateProductCommand.CreateProduct()` |
| 외부 데이터 필요 | Usecase에서 Port 조율 후 `Create()` | `CreateOrderCommand` + `IProductCatalog` |
| DB에서 복원 | `CreateFromValidated()` (검증 스킵) | Repository Mapper |

> **Apply 패턴**: Usecase에서 `(v1, v2, v3).Apply(...)` 튜플로 VO를 병렬 검증한 뒤 `Create()`를 호출합니다. 자세한 내용은 [유스케이스 구현 가이드 — Value Object 검증과 Apply 병합 패턴](../application/11-usecases-and-cqrs)을 참조하세요.
>
> **교차 Aggregate 조율**: 다른 Aggregate 데이터가 필요하면 Usecase의 LINQ 체인에서 Port를 통해 조회한 뒤 `Create()`를 호출합니다. 자세한 내용은 [유스케이스 구현 가이드](../application/11-usecases-and-cqrs)를 참조하세요.

**DDD Principle Compliance:**
- **캡슐화**: `private` 생성자로 직접 인스턴스화 차단, 팩토리 메서드만 공개
- **Immutable식 보호**: `Create()`에서 검증된 VO만 수용, primitive 직접 전달 불가
- **재구성 분리**: `Create()` (새 생성) vs `CreateFromValidated()` (복원) 명확 구분
- **이벤트 일관성**: 새 생성 시에만 도메인 이벤트 발행, 복원 시 이벤트 없음
- **레이어 책임**: Aggregate는 자기 생성만 담당, 외부 조율은 Usecase 책임

Having covered how to create Entities, let us now examine command methods that safely change the state of created Entities.

---

## Command Methods and Invariant Protection

### Command Methods That Protect Invariants

State changes are only possible through Aggregate Root methods. 비즈니스 규칙 위반 시 `Fin<Unit>`으로 실패를 반환합니다.

The key point in the following code is Immutable식 검사 후 실패 시 `DomainError`를 반환하고, 성공 시 상태 변경과 이벤트 발행을 수행하는 패턴입니다.

```csharp
// Inventory: 재고 차감 (불변식: 재고 ≥ 0)
// Error type definition: public sealed record InsufficientStock : DomainErrorType.Custom;
public Fin<Unit> DeductStock(Quantity quantity)
{
    if (quantity > StockQuantity)
        return DomainError.For<Inventory, int>(
            new InsufficientStock(),
            currentValue: StockQuantity,
            message: $"Insufficient stock. Current: {StockQuantity}, Requested: {quantity}");

    StockQuantity = StockQuantity.Subtract(quantity);
    UpdatedAt = DateTime.UtcNow;
    AddDomainEvent(new StockDeductedEvent(Id, ProductId, quantity));
    return unit;
}
```

```csharp
// Product: 정보 업데이트 (항상 성공하는 커맨드)
public Product Update(
    ProductName name,
    ProductDescription description,
    Money price)
{
    var oldPrice = Price;

    Name = name;
    Description = description;
    Price = price;
    UpdatedAt = DateTime.UtcNow;

    AddDomainEvent(new UpdatedEvent(Id, name, oldPrice, price));

    return this;
}
```

### Child Entity Management (Add/Remove)

자식 Entity 컬렉션은 `private List<T>` + `public IReadOnlyList<T>` 패턴으로 캡슐화합니다.

```csharp
public sealed class Product : AggregateRoot<ProductId>
{
    // private 변경 가능 컬렉션
    private readonly List<Tag> _tags = [];

    // public 읽기 전용 뷰
    public IReadOnlyList<Tag> Tags => _tags.AsReadOnly();

    // Root를 통해서만 자식 Entity 추가
    public Product AddTag(Tag tag)
    {
        if (_tags.Any(t => t.Id == tag.Id))
            return this;

        _tags.Add(tag);
        AddDomainEvent(new TagAssignedEvent(tag.Id, tag.Name));
        return this;
    }

    // Root를 통해서만 자식 Entity 제거
    public Product RemoveTag(TagId tagId)
    {
        var tag = _tags.FirstOrDefault(t => t.Id == tagId);
        if (tag is null)
            return this;

        _tags.Remove(tag);
        AddDomainEvent(new TagRemovedEvent(tagId));
        return this;
    }
}
```

### Query Methods (State Inspection)

Entity의 상태를 확인하는 메서드입니다. 부작용이 없고, 상태를 변경하지 않습니다.

```csharp
// 상품이 만료되었는지 확인
public bool IsExpired() => ExpirationDate < DateTime.UtcNow;

// 주문이 배송 가능한 상태인지 확인
public bool IsShippable() => Status == OrderStatus.Confirmed;
```

### Return Types by Method Type

메서드의 성격에 따라 적절한 반환 타입을 Optional하세요.

| Method Type | Return Type | Description |
|------------|----------|------|
| Query (simple check) | `bool`, `int`, etc. | Side-effect-free state check |
| Query (VO calculation) | `Money`, `Quantity`, etc. | Returns calculated value object |
| Command (always succeeds) | `void` 또는 `this` | State change without validation |
| Command (can fail) | `Fin<Unit>` | Possible business rule violation |
| Command (returns result) | `Fin<T>` | Can fail + returns calculated result |

An Aggregate Root's command methods only change its own state. 그렇다면 Aggregate 내부의 자식 Entity는 어떻게 관리할까요?

---

## Child Entity Implementation Patterns

### Access Only Through the Aggregate Root

Child Entities do not have independent Repositories and must be created/modified/deleted through the Aggregate Root.

```csharp
// Tag: 자식 Entity (SharedModels)
[GenerateEntityId]
public sealed class Tag : Entity<TagId>
{
    public TagName Name { get; private set; }

#pragma warning disable CS8618
    private Tag() { }
#pragma warning restore CS8618

    private Tag(TagId id, TagName name) : base(id)
    {
        Name = name;
    }

    public static Tag Create(TagName name) =>
        new(TagId.New(), name);

    public static Tag CreateFromValidated(TagId id, TagName name) =>
        new(id, name);
}
```

### 검증이 필요한 자식 Entity (OrderLine 예시)

자식 Entity가 도메인 Immutable식을 가질 때는 `Create()`가 `Fin<T>`를 반환합니다:

```csharp
// OrderLine: Order Aggregate의 자식 Entity
[GenerateEntityId]
public sealed class OrderLine : Entity<OrderLineId>
{
    public sealed record InvalidQuantity : DomainErrorType.Custom;

    public ProductId ProductId { get; private set; }
    public Quantity Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money LineTotal { get; private set; }

    private OrderLine(OrderLineId id, ProductId productId, Quantity quantity, Money unitPrice, Money lineTotal)
        : base(id)
    {
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        LineTotal = lineTotal;
    }

    // Create: 수량 > 0 불변식 검증, LineTotal 자동 계산
    public static Fin<OrderLine> Create(ProductId productId, Quantity quantity, Money unitPrice)
    {
        if ((int)quantity <= 0)
            return DomainError.For<OrderLine, int>(
                new InvalidQuantity(), currentValue: quantity,
                message: "Order line quantity must be greater than 0");

        var lineTotal = unitPrice.Multiply(quantity);
        return new OrderLine(OrderLineId.New(), productId, quantity, unitPrice, lineTotal);
    }

    // CreateFromValidated: ORM/Repository 복원용 (검증 없음)
    public static OrderLine CreateFromValidated(
        OrderLineId id, ProductId productId, Quantity quantity, Money unitPrice, Money lineTotal)
        => new(id, productId, quantity, unitPrice, lineTotal);
}
```

> **참고**: 실전 코드는 `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/AggregateRoots/Orders/OrderLine.cs`를 참조하세요.

### Having Its Own Identifier

자식 Entity는 Value Object와 달리 **고유 식별자를** 가집니다. 이를 통해 컬렉션 내에서 특정 요소를 식별할 수 있습니다.

```csharp
// Aggregate Root에서 TagId로 특정 Tag를 찾아 제거
public Product RemoveTag(TagId tagId)
{
    var tag = _tags.FirstOrDefault(t => t.Id == tagId);
    if (tag is null)
        return this;

    _tags.Remove(tag);
    AddDomainEvent(new TagRemovedEvent(tagId));
    return this;
}
```

### Event Publishing from Child Entities

Child Entities do not directly publish domain events. 대신 **Aggregate Root가 자식 Entity 변경에 대한 이벤트를 발행합니다.**

```csharp
// Aggregate Root(Product)가 Tag 관련 이벤트 발행
public Product AddTag(Tag tag)
{
    _tags.Add(tag);
    AddDomainEvent(new TagAssignedEvent(tag.Id, tag.Name));  // Root가 발행
    return this;
}

// 자식 Entity(Tag)가 직접 이벤트 발행
// Tag는 Entity<TId>를 상속하므로 AddDomainEvent()를 사용할 수 없음
```

---

## Domain Events

Domain events represent significant occurrences in the domain. AggregateRoot에서만 발행할 수 있습니다.

> **참고**: 도메인 이벤트의 전체 설계(`IDomainEvent`/`DomainEvent` 정의, Pub/Sub, 핸들러 구독/등록, 트랜잭션 고려사항)는 [도메인 이벤트 가이드](./07-domain-events)를 참조하세요.

### Event Definition Location

도메인 이벤트는 해당 Entity의 **중첩 클래스로** 정의합니다:

```csharp
[GenerateEntityId]
public class Order : AggregateRoot<OrderId>
{
    #region Domain Events

    // 도메인 이벤트 (중첩 클래스)
    public sealed record CreatedEvent(OrderId OrderId, CustomerId CustomerId, Money TotalAmount) : DomainEvent;
    public sealed record ConfirmedEvent(OrderId OrderId) : DomainEvent;
    public sealed record CancelledEvent(OrderId OrderId, string Reason) : DomainEvent;

    #endregion

    // Entity 구현...
}
```

**Advantages**:
- 이벤트 소유권이 타입 시스템에서 명확 (`Order.CreatedEvent`)
- IntelliSense에서 `Order.`만 치면 관련 이벤트 모두 표시
- Entity 이름 중복 제거 (`OrderCreatedEvent` → `Order.CreatedEvent`)
- **Event Handler에서 이벤트 발행 주체 명시**: Handler가 `IDomainEventHandler<Product.CreatedEvent>`를 상속받으면, 코드를 읽는 것만으로 "Product Entity가 발행한 이벤트"임을 즉시 파악 가능

**사용 예시**:
```csharp
// Entity 내부에서 (짧게)
AddDomainEvent(new CreatedEvent(Id, customerId, totalAmount));

// 외부에서 (명시적)
public void Handle(Order.CreatedEvent @event) { ... }
```

### Event Publishing Pattern

AggregateRoot 내에서 `AddDomainEvent()`를 사용하여 이벤트를 수집합니다. 비즈니스적으로 의미 있는 상태 변화가 발생할 때 발행합니다.

```csharp
[GenerateEntityId]
public class Order : AggregateRoot<OrderId>
{
    #region Error Types

    public sealed record InvalidStatus : DomainErrorType.Custom;

    #endregion

    #region Domain Events

    public sealed record CreatedEvent(OrderId OrderId, Money TotalAmount) : DomainEvent;
    public sealed record ShippedEvent(OrderId OrderId, Address ShippingAddress) : DomainEvent;

    #endregion

    // Create: 생성 이벤트 발행
    public static Order Create(Money totalAmount)
    {
        var id = OrderId.New();
        var order = new Order(id, totalAmount);
        order.AddDomainEvent(new CreatedEvent(id, totalAmount));
        return order;
    }

    // Ship: 상태 변경 시 이벤트 발행
    public Fin<Unit> Ship(Address address)
    {
        if (Status != OrderStatus.Confirmed)
            return DomainError.For<Order>(
                new InvalidStatus(),
                Status.ToString(),
                "Order must be confirmed before shipping");

        Status = OrderStatus.Shipped;
        AddDomainEvent(new ShippedEvent(Id, address));
        return unit;
    }
}
```

---

## Checklist

### Functorium Implementation Checklist

- [ ] Aggregate Root는 `AggregateRoot<TId>` 상속
- [ ] 자식 Entity는 `Entity<TId>` 상속
- [ ] `[GenerateEntityId]` 속성 적용
- [ ] 자식 Entity 컬렉션: `private List<T>` + `public IReadOnlyList<T>`
- [ ] 비즈니스 규칙 위반 시 `Fin<Unit>` 반환
- [ ] 상태 변경 시 `AddDomainEvent()` 호출
- [ ] ORM용 기본 생성자 + `#pragma warning disable CS8618`
- [ ] `Create()` 팩토리 메서드 (새 Entity 생성)
- [ ] `CreateFromValidated()` 메서드 (ORM 복원용)
- [ ] Entity 레벨 비즈니스 규칙이 있으면 `Validate()` 메서드 정의
- [ ] 도메인 이벤트는 중첩 record로 정의 (`Order.CreatedEvent`)

---

## Troubleshooting

### `[GenerateEntityId]` 적용 후 EntityId 타입이 생성되지 않음
**Cause:** Source Generator가 빌드 시점에 실행되지 않았거나, IDE 캐시가 오래된 상태일 수 있습니다.
**Solution:** `dotnet build`로 전체 빌드를 실행하세요. IDE에서 인식되지 않으면 솔루션을 닫고 다시 열거나, `dotnet clean` 후 빌드하세요.

### ORM 복원 시 `#pragma warning disable CS8618` 누락으로 경고 발생
**Cause:** EF Core 등 ORM은 파라미터 없는 private 생성자를 요구하는데, 이 생성자에서 non-nullable 속성이 초기화되지 않아 CS8618 경고가 발생합니다.
**Solution:** ORM용 기본 생성자에 `#pragma warning disable CS8618` / `#pragma warning restore CS8618`를 적용하세요. 이는 ORM 프록시 생성을 위한 관례적 패턴입니다.

---

## FAQ

### Q1. Entity vs AggregateRoot Optional 기준은?

**AggregateRoot는 "트랜잭션 경계"입니다.**

Aggregate Root는:
- 외부에서 직접 접근할 수 있는 유일한 Entity입니다.
- 트랜잭션의 일관성 경계를 정의합니다.
- 도메인 이벤트를 발행할 수 있습니다.

```csharp
// Order는 AggregateRoot - 외부에서 직접 접근
[GenerateEntityId]
public class Order : AggregateRoot<OrderId> { }

// OrderItem은 Entity - Order를 통해서만 접근
[GenerateEntityId]
public class OrderItem : Entity<OrderItemId> { }
```

| 질문 | 예 | 아니오 |
|------|-----|--------|
| 외부에서 직접 접근하나요? | AggregateRoot | Entity |
| 도메인 이벤트를 발행하나요? | AggregateRoot | Entity |
| 독립적으로 저장/조회하나요? | AggregateRoot | Entity |

### Q2. 왜 Ulid를 사용하나요?

**Ulid는 GUID의 장점 + 시간 순서를 제공합니다.**

| Characteristics | GUID | Auto-increment | Ulid |
|------|------|----------------|------|
| 분산 생성 | O | X | O |
| 시간 순서 | X | O | O |
| 인덱스 성능 | 낮음 | 높음 | 높음 |
| 추측 가능성 | 낮음 | 높음 | 낮음 |

```csharp
var id1 = ProductId.New();  // 01ARZ3NDEKTSV4RRFFQ69G5FAV
var id2 = ProductId.New();  // 01ARZ3NDEKTSV4RRFFQ69G5FAW

// Ulid는 시간 순서를 보장
id1 < id2  // true
```

### Q3. CreateFromValidated는 언제 사용하나요?

**데이터베이스에서 Entity를 복원할 때 사용합니다.**

| 상황 | 사용 메서드 | Reason |
|------|------------|------|
| 새 Entity 생성 | `Create()` | 입력값 검증 필요 |
| DB에서 복원 | `CreateFromValidated()` | 이미 검증된 데이터 |
| API 요청 처리 | `Create()` | 외부 입력 검증 필요 |

### Q4. 도메인 이벤트는 언제 발행하나요?

**비즈니스적으로 의미 있는 상태 변화가 발생했을 때 발행합니다.**

```csharp
// 좋음: 비즈니스 의미가 있는 이벤트
AddDomainEvent(new OrderCreatedEvent(Id, CustomerId, TotalAmount));
AddDomainEvent(new OrderConfirmedEvent(Id));

// 나쁨: 너무 세부적인 이벤트
AddDomainEvent(new OrderStatusChangedEvent(Id, OldStatus, NewStatus));  // 너무 일반적
AddDomainEvent(new PropertyUpdatedEvent(Id, "Name", OldValue, NewValue));  // CRUD 수준
```

> 이벤트 핸들러 등록, 트랜잭션 고려사항 등 자세한 내용은 [도메인 이벤트 가이드](./07-domain-events)를 참조하세요.

### Q5. Entity에서 Validate 메서드가 필요한 경우는?

Entity 레벨 비즈니스 규칙(VO 간 관계 검증)이 있을 때만 정의합니다. [§생성 패턴 — Entity.Validate](#entityvalidate가-필요한-경우-vs-불필요한-경우)를 참조하세요.

---

## Reference Documents

- [Aggregate 설계 원칙 (WHY)](./06a-aggregate-design) - Aggregate 설계 원칙과 개념
- [Entity/Aggregate 고급 패턴](./06c-entity-aggregate-advanced) - Cross-Aggregate 관계, 부가 인터페이스, 실전 예제
- [값 객체 구현 가이드](./05a-value-objects) - Value Object 구현 패턴, [검증·열거형 가이드](./05b-value-objects-validation) - 열거형·Application 검증·FAQ
- [도메인 이벤트 가이드](./07-domain-events) - 도메인 이벤트 전체 설계 (IDomainEvent, Pub/Sub, 핸들러, 트랜잭션)
- [에러 시스템: 기초와 네이밍](./08a-error-system) - 에러 처리 기본 원칙과 네이밍 규칙
- [에러 시스템: Domain/Application 에러](./08b-error-system-domain-app) - Domain/Application 에러 정의 및 테스트 패턴
- [도메인 모델링 개요](./04-ddd-tactical-overview) - 도메인 모델링 개요
- [유스케이스 구현 가이드](../application/11-usecases-and-cqrs) - Application Layer에서의 Aggregate 사용 (Apply 패턴, 교차 Aggregate 조율)
- [Adapter 구현 가이드](../adapter/13-adapters) - EF Core 통합, Persistence Model 매핑
- [단위 테스트 가이드](../testing/15a-unit-testing)
