# Entity 구현 가이드

이 문서는 Functorium 프레임워크의 `Functorium.Domains.Entities` 네임스페이스를 사용하여 DDD Entity를 구현하는 방법을 설명합니다.

## 목차

- [개요](#개요)
- [클래스 계층 구조](#클래스-계층-구조)
- [기반 클래스](#기반-클래스)
  - [Entity\<TId\>](#entitytid)
  - [AggregateRoot\<TId\>](#aggregateroottid)
- [Entity ID 시스템](#entity-id-시스템)
  - [IEntityId\<T\> 인터페이스](#ientityidt-인터페이스)
  - [EntityIdGenerator (소스 생성기)](#entityidgenerator-소스-생성기)
  - [생성되는 코드](#생성되는-코드)
- [도메인 이벤트](#도메인-이벤트)
  - [IDomainEvent / DomainEvent](#idomainevent--domainevent)
  - [이벤트 발행 패턴](#이벤트-발행-패턴)
- [부가 인터페이스](#부가-인터페이스)
  - [IAuditable](#iauditable)
  - [ISoftDeletable](#isoftdeletable)
- [구현 패턴](#구현-패턴)
  - [Create / CreateFromValidated 패턴](#create--createfromvalidated-패턴)
  - [EF Core 통합](#ef-core-통합)
  - [Entity.Validate가 필요한 경우 vs 불필요한 경우](#entityvalidate가-필요한-경우-vs-불필요한-경우)
- [속성 구성 패턴](#속성-구성-패턴)
  - [Value Object vs Entity의 역할 차이](#value-object-vs-entity의-역할-차이)
  - [Value Object 속성을 가진 Entity](#value-object-속성을-가진-entity)
  - [다른 Entity를 참조하는 Entity](#다른-entity를-참조하는-entity)
  - [복합 검증 패턴](#복합-검증-패턴)
  - [Application Layer에서의 호출 패턴](#application-layer에서의-호출-패턴)
- [실전 예제](#실전-예제)
- [FAQ](#faq)
- [참고 문서](#참고-문서)

---

## 개요

Entity는 도메인 주도 설계(DDD)의 핵심 전술 패턴 중 하나입니다. "주문", "사용자", "제품" 같은 **고유한 식별자를 가진 도메인 개념**을 표현합니다.

### 왜 Entity를 사용하나요?

Entity가 없으면 다음과 같은 문제가 발생합니다:

```csharp
// 문제점 1: 식별자가 명확하지 않음
public class Order
{
    public Guid Id { get; set; }  // Guid? int? string?
    public decimal Amount { get; set; }
}

// 문제점 2: 다른 타입의 ID와 혼동 가능
void ProcessOrder(Guid orderId, Guid customerId);
ProcessOrder(customerId, orderId);  // 순서 착각 - 컴파일 오류 없음!

// 문제점 3: 동등성 비교가 명확하지 않음
var order1 = GetOrder(id);
var order2 = GetOrder(id);
order1 == order2;  // false? (참조 비교)
```

Entity는 이 문제들을 해결합니다:

```csharp
// 해결책: 타입 안전한 ID와 ID 기반 동등성
[GenerateEntityId]
public class Order : Entity<OrderId>
{
    public Money Amount { get; private set; }

    private Order(OrderId id, Money amount) : base(id)
    {
        Amount = amount;
    }
}

// 컴파일 오류로 실수 방지
void ProcessOrder(OrderId orderId, CustomerId customerId);
ProcessOrder(customerId, orderId);  // 컴파일 오류!

// ID 기반 동등성
var order1 = GetOrder(id);
var order2 = GetOrder(id);
order1 == order2;  // true (같은 ID)
```

### Entity vs Value Object

| 관점 | Entity | Value Object |
|------|--------|--------------|
| **식별자** | ID 기반 동등성 | 값 기반 동등성 |
| **가변성** | 가변 (상태 변경 가능) | 불변 |
| **생명주기** | 장기 (Repository 추적) | 단기 (일회성) |
| **도메인 이벤트** | 발행 가능 (AggregateRoot) | 발행 없음 |
| **예시** | Order, User, Product | Money, Email, Address |

### 기반 클래스 선택

| 사용 시나리오 | 기반 클래스 | 특징 |
|--------------|------------|------|
| 일반 Entity | `Entity<TId>` | ID 기반 동등성 |
| Aggregate Root | `AggregateRoot<TId>` | 도메인 이벤트 관리 |

### 핵심 패턴

```csharp
using Functorium.Domains.Entities;
using Functorium.Domains.SourceGenerator;

[GenerateEntityId]  // OrderId 자동 생성
public class Order : AggregateRoot<OrderId>
{
    public Money Amount { get; private set; }
    public CustomerId CustomerId { get; private set; }

    // ORM용 기본 생성자
#pragma warning disable CS8618
    private Order() { }
#pragma warning restore CS8618

    // 내부 생성자
    private Order(OrderId id, Money amount, CustomerId customerId) : base(id)
    {
        Amount = amount;
        CustomerId = customerId;
    }

    // Create: 이미 검증된 Value Object를 직접 받음
    public static Order Create(Money amount, CustomerId customerId)
    {
        var id = OrderId.New();
        return new Order(id, amount, customerId);
    }

    // CreateFromValidated: ORM/Repository 복원용 (검증 없음)
    public static Order CreateFromValidated(OrderId id, Money amount, CustomerId customerId)
        => new(id, amount, customerId);

    // 도메인 연산
    public Fin<Unit> UpdateAmount(Money newAmount)
    {
        Amount = newAmount;
        AddDomainEvent(new OrderAmountUpdatedEvent(Id, newAmount));
        return unit;
    }
}
```

---

## 클래스 계층 구조

Functorium은 Entity 구현을 위한 기반 클래스 계층을 제공합니다.

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
            `-- AggregateRoot<TId>
                +-- DomainEvents (읽기 전용)
                +-- AddDomainEvent()
                +-- RemoveDomainEvent()
                `-- ClearDomainEvents()
```

**계층 이해하기:**

- **IEntity\<TId\>**: Entity의 계약을 정의하는 인터페이스. `Create`, `CreateFromValidated` 메서드명 상수를 포함합니다.
- **Entity\<TId\>**: ID 기반 동등성(`Equals`, `GetHashCode`, `==`, `!=`)을 자동 구현. ORM 프록시 타입도 처리합니다.
- **AggregateRoot\<TId\>**: 도메인 이벤트 관리 기능을 제공합니다.

---

## 기반 클래스

Entity를 구현할 때 가장 먼저 할 일은 **어떤 기반 클래스를 상속받을지** 결정하는 것입니다.

**질문: 도메인 이벤트를 발행하나요?**
- 아니오 → `Entity<TId>` (일반 Entity)
- 예 → `AggregateRoot<TId>` (Aggregate Root)

```
도메인 이벤트를 발행하나요?
    |
    +-- 예 --> AggregateRoot<TId>
    |
    `-- 아니오 --> Entity<TId>
```

### Entity\<TId\>

ID 기반 동등성을 제공하는 Entity의 기반 추상 클래스입니다.

**위치**: `Functorium.Domains.Entities.Entity<TId>`

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

**구현 필수 항목:**

| 항목 | 설명 |
|------|------|
| `[GenerateEntityId]` 속성 | EntityId 자동 생성 |
| Private 생성자 (ORM용) | 파라미터 없는 기본 생성자 + `#pragma warning disable CS8618` |
| Private 생성자 (내부용) | ID를 받는 생성자 |
| `Create()` | Entity 생성 팩토리 메서드 |
| `CreateFromValidated()` | ORM 복원용 메서드 |

**예제:**

```csharp
using Functorium.Domains.Entities;
using Functorium.Domains.SourceGenerator;

[GenerateEntityId]
public class Product : Entity<ProductId>
{
    public ProductName Name { get; private set; }
    public Price Price { get; private set; }

    // ORM용 기본 생성자
    // CS8618: Non-nullable 속성이 생성자에서 초기화되지 않음 경고 억제
    // ORM이 리플렉션으로 속성을 설정하므로 안전함
#pragma warning disable CS8618
    private Product() { }
#pragma warning restore CS8618

    // 내부 생성자
    private Product(ProductId id, ProductName name, Price price) : base(id)
    {
        Name = name;
        Price = price;
    }

    // Create: 이미 검증된 Value Object를 직접 받음
    public static Product Create(ProductName name, Price price)
    {
        var id = ProductId.New();
        return new Product(id, name, price);
    }

    public static Product CreateFromValidated(ProductId id, ProductName name, Price price)
        => new(id, name, price);

    public void UpdatePrice(Price newPrice)
    {
        Price = newPrice;
    }
}
```

### AggregateRoot\<TId\>

도메인 이벤트 관리 기능을 제공하는 Aggregate Root의 기반 추상 클래스입니다.

**위치**: `Functorium.Domains.Entities.AggregateRoot<TId>`

```csharp
[Serializable]
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : struct, IEntityId<TId>
{
    // 도메인 이벤트 목록 (읽기 전용)
    public IReadOnlyList<IDomainEvent> DomainEvents { get; }

    // 기본 생성자 (ORM/직렬화용)
    protected AggregateRoot();

    // ID를 지정하여 Aggregate Root 생성
    protected AggregateRoot(TId id);

    // 도메인 이벤트 추가
    protected void AddDomainEvent(IDomainEvent domainEvent);

    // 도메인 이벤트 제거
    protected void RemoveDomainEvent(IDomainEvent domainEvent);

    // 모든 도메인 이벤트 제거
    public void ClearDomainEvents();
}
```

**예제:**

```csharp
[GenerateEntityId]
public class Order : AggregateRoot<OrderId>
{
    public Money TotalAmount { get; private set; }
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

    public Fin<Unit> Confirm()
    {
        if (Status != OrderStatus.Pending)
            return DomainError.For<Order>(
                new Custom("InvalidStatus"),
                Status.ToString(),
                "Order can only be confirmed when pending");

        Status = OrderStatus.Confirmed;
        AddDomainEvent(new OrderConfirmedEvent(Id));
        return unit;
    }
}
```

---

## Entity ID 시스템

Functorium은 타입 안전한 Entity ID 시스템을 제공합니다. Ulid 기반으로 시간 순서 정렬이 가능하며, 소스 생성기를 통해 자동으로 생성됩니다.

### IEntityId\<T\> 인터페이스

**위치**: `Functorium.Domains.Entities.IEntityId<T>`

```csharp
public interface IEntityId<T> : IEquatable<T>, IComparable<T>
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

| 특성 | GUID | Ulid |
|------|------|------|
| 크기 | 128bit | 128bit |
| 정렬 | 무작위 | 시간 순서 |
| 가독성 | 36자 (하이픈 포함) | 26자 |
| 인덱스 성능 | 낮음 (무작위) | 높음 (순차) |

Ulid는 시간 순서로 정렬되므로 데이터베이스 인덱스 성능이 좋고, 생성 시간을 추출할 수 있습니다.

### EntityIdGenerator (소스 생성기)

`[GenerateEntityId]` 속성을 Entity 클래스에 붙이면 해당 Entity의 ID 타입이 자동으로 생성됩니다.

**위치**: `Functorium.Domains.SourceGenerator.GenerateEntityIdAttribute`

```csharp
using Functorium.Domains.SourceGenerator;

[GenerateEntityId]  // ProductId, ProductIdComparer, ProductIdConverter 자동 생성
public class Product : Entity<ProductId>
{
    // ...
}
```

### 생성되는 코드

`[GenerateEntityId]`는 다음 타입들을 자동 생성합니다:

| 생성 타입 | 용도 |
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

---

## 도메인 이벤트

도메인 이벤트는 도메인에서 발생한 중요한 사건을 표현합니다. AggregateRoot에서만 발행할 수 있습니다.

### IDomainEvent / DomainEvent

**위치**: `Functorium.Domains.Events`

```csharp
// 인터페이스
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}

// 기반 record
public abstract record DomainEvent(DateTimeOffset OccurredAt) : IDomainEvent
{
    // 현재 시각으로 이벤트 생성
    protected DomainEvent() : this(DateTimeOffset.UtcNow) { }
}
```

### 이벤트 정의 위치

도메인 이벤트는 해당 Entity의 **중첩 클래스**로 정의합니다:

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

**장점**:
- 이벤트 소유권이 타입 시스템에서 명확 (`Order.CreatedEvent`)
- IntelliSense에서 `Order.`만 치면 관련 이벤트 모두 표시
- Entity 이름 중복 제거 (`OrderCreatedEvent` → `Order.CreatedEvent`)

**사용 예시**:
```csharp
// Entity 내부에서 (짧게)
AddDomainEvent(new CreatedEvent(Id, customerId, totalAmount));

// 외부에서 (명시적)
public void Handle(Order.CreatedEvent @event) { ... }
```

### 이벤트 발행 패턴

AggregateRoot 내에서 `AddDomainEvent()`를 사용하여 이벤트를 발행합니다.

```csharp
[GenerateEntityId]
public class Order : AggregateRoot<OrderId>
{
    #region Domain Events

    public sealed record CreatedEvent(OrderId OrderId, Money TotalAmount) : DomainEvent;
    public sealed record ShippedEvent(OrderId OrderId, Address ShippingAddress) : DomainEvent;

    #endregion

    public Money TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }

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
        // 생성 이벤트 발행 (내부에서는 짧게)
        order.AddDomainEvent(new CreatedEvent(id, totalAmount));
        return order;
    }

    public Fin<Unit> Ship(Address address)
    {
        if (Status != OrderStatus.Confirmed)
            return DomainError.For<Order>(
                new Custom("InvalidStatus"),
                Status.ToString(),
                "Order must be confirmed before shipping");

        Status = OrderStatus.Shipped;
        // 배송 이벤트 발행
        AddDomainEvent(new ShippedEvent(Id, address));
        return unit;
    }
}
```

**이벤트 처리 (Application Layer):**

이벤트는 일반적으로 Application Layer에서 처리됩니다:

```csharp
public class OrderCommandHandler
{
    private readonly IOrderRepository _repository;
    private readonly IEventDispatcher _eventDispatcher;

    public async Task<Fin<Unit>> ConfirmOrderAsync(OrderId orderId)
    {
        var order = await _repository.GetByIdAsync(orderId);
        var result = order.Confirm();

        if (result.IsSucc)
        {
            await _repository.SaveAsync(order);
            // 이벤트 발행
            await _eventDispatcher.DispatchAsync(order.DomainEvents);
            order.ClearDomainEvents();
        }

        return result;
    }
}
```

---

## 부가 인터페이스

Entity에 추가 기능을 부여하는 인터페이스들입니다.

### IAuditable

생성/수정 시각을 추적합니다.

**위치**: `Functorium.Domains.Entities.IAuditable`

```csharp
// 시각만 추적
public interface IAuditable
{
    DateTime CreatedAt { get; }
    DateTime? UpdatedAt { get; }
}

// 시각 + 사용자 추적
public interface IAuditableWithUser : IAuditable
{
    string? CreatedBy { get; }
    string? UpdatedBy { get; }
}
```

**사용 예제:**

```csharp
[GenerateEntityId]
public class Product : Entity<ProductId>, IAuditableWithUser
{
    public ProductName Name { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public string? UpdatedBy { get; private set; }

    private Product(ProductId id, ProductName name, string createdBy) : base(id)
    {
        Name = name;
        CreatedAt = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    public void UpdateName(ProductName name, string updatedBy)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }
}
```

### ISoftDeletable

소프트 삭제를 지원합니다. 실제로 레코드를 삭제하지 않고 삭제됨으로 표시합니다.

**위치**: `Functorium.Domains.Entities.ISoftDeletable`

```csharp
// 삭제 여부만 추적
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
}

// 삭제 여부 + 삭제자 추적
public interface ISoftDeletableWithUser : ISoftDeletable
{
    string? DeletedBy { get; }
}
```

**사용 예제:**

```csharp
[GenerateEntityId]
public class Product : Entity<ProductId>, ISoftDeletableWithUser
{
    public ProductName Name { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public string? DeletedBy { get; private set; }

    public void Delete(string deletedBy)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }
}
```

---

## 구현 패턴

Entity 구현의 핵심은 **검증 책임 분리**입니다. Value Object와 Entity는 서로 다른 검증 책임을 가집니다.

- **Value Object**: 원시 값을 받아 자신의 유효성을 검증
- **Entity**: 이미 검증된 Value Object를 받아 조합. Entity 레벨 비즈니스 규칙이 있을 때만 Validate 정의

### Value Object vs Entity의 역할 차이

| 구분 | Value Object | Entity |
|------|--------------|--------|
| **Validate** | 원시 값 → 검증된 값 반환 | Entity 레벨 비즈니스 규칙만 |
| **Create** | 원시 값 받음 | **Value Object를 직접 받음** |
| **검증 책임** | 자신의 값 검증 | VO 간 관계/규칙 검증 |

> **참고**: Value Object의 검증 패턴은 [값 객체 구현 가이드 - 구현 패턴](./valueobject-guide.md#구현-패턴)을 참고하세요.

### Create / CreateFromValidated 패턴

Entity는 두 가지 생성 경로를 제공합니다:

| 메서드 | 용도 | 검증 | ID 생성 |
|--------|------|------|---------|
| `Create()` | 새 Entity 생성 | VO가 이미 검증됨 | 새로 생성 |
| `CreateFromValidated()` | ORM/Repository 복원 | 없음 | 기존 ID 사용 |

**Create 메서드:**

새로운 Entity를 생성할 때 사용합니다. **이미 검증된 Value Object를 직접 받습니다.**

```csharp
// Create: 이미 검증된 Value Object를 직접 받음
public static Product Create(ProductName name, Price price)
{
    var id = ProductId.New();  // 새 ID 생성
    return new Product(id, name, price);
}
```

**CreateFromValidated 메서드:**

ORM이나 Repository에서 Entity를 복원할 때 사용합니다. 데이터베이스에서 읽은 값은 이미 검증되었으므로 다시 검증하지 않습니다.

```csharp
public static Product CreateFromValidated(ProductId id, ProductName name, Price price)
    => new(id, name, price);  // 검증 없음, 기존 ID 사용
```

**왜 두 가지 메서드가 필요한가요?**

1. **성능**: 데이터베이스에서 대량의 Entity를 로드할 때 검증을 건너뛰어 성능을 향상시킵니다.
2. **의미**: 새 Entity 생성과 기존 Entity 복원은 다른 의미를 가집니다.
3. **ID 관리**: Create는 새 ID를 생성하고, CreateFromValidated는 기존 ID를 사용합니다.

### Entity.Validate가 필요한 경우 vs 불필요한 경우

**불필요한 경우** - VO 단순 조합:
```csharp
// Value Object가 이미 검증됨 → Entity.Validate 불필요
public static Order Create(Money amount, CustomerId customerId)
{
    var id = OrderId.New();
    return new Order(id, amount, customerId);
}
```

**필요한 경우** - Entity 레벨 비즈니스 규칙:
```csharp
// 판매가 > 원가 규칙은 Entity 레벨의 검증
[GenerateEntityId]
public class Product : Entity<ProductId>
{
    public ProductName Name { get; private set; }
    public Price SellingPrice { get; private set; }
    public Money Cost { get; private set; }

    private Product(ProductId id, ProductName name, Price sellingPrice, Money cost) : base(id)
    {
        Name = name;
        SellingPrice = sellingPrice;
        Cost = cost;
    }

    // Validate: Entity 레벨 비즈니스 규칙 (판매가 > 원가)
    public static Validation<Error, Unit> Validate(Price sellingPrice, Money cost) =>
        sellingPrice.Value > cost.Amount
            ? Success<Error, Unit>(unit)
            : DomainError.For<Product>(
                new Custom("SellingPriceBelowCost"),
                sellingPrice.Value,
                $"Selling price must be greater than cost. Price: {sellingPrice.Value}, Cost: {cost.Amount}");

    // Create: Validate 호출 후 Entity 생성
    public static Fin<Product> Create(ProductName name, Price sellingPrice, Money cost) =>
        Validate(sellingPrice, cost)
            .Map(_ => new Product(ProductId.New(), name, sellingPrice, cost))
            .ToFin();
}
```

### EF Core 통합

EntityIdGenerator가 생성하는 `ValueConverter`와 `ValueComparer`를 사용하여 EF Core와 통합합니다.

**DbContext 설정:**

```csharp
public class AppDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Product 설정
        modelBuilder.Entity<Product>(builder =>
        {
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasConversion(new ProductIdConverter())
                .HasMaxLength(26);  // Ulid 문자열 길이

            builder.Property(e => e.Id)
                .Metadata
                .SetValueComparer(new ProductIdComparer());

            // Value Object 속성
            builder.OwnsOne(e => e.Name, name =>
            {
                name.Property(v => v.Value)
                    .HasColumnName("Name")
                    .HasMaxLength(100);
            });
        });

        // Order 설정
        modelBuilder.Entity<Order>(builder =>
        {
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasConversion(new OrderIdConverter())
                .HasMaxLength(26);

            // 다른 Entity 참조 (외래 키)
            builder.Property(e => e.CustomerId)
                .HasConversion(new CustomerIdConverter())
                .HasMaxLength(26);
        });
    }
}
```

---

## 속성 구성 패턴

Entity는 Value Object와 다른 Entity를 속성으로 가질 수 있습니다.

### Value Object 속성을 가진 Entity

Entity가 Value Object를 속성으로 가질 때의 패턴입니다.

**Entity 레벨 비즈니스 규칙이 없는 경우:**

```csharp
[GenerateEntityId]
public class Product : Entity<ProductId>
{
    public ProductName Name { get; private set; }
    public Price Price { get; private set; }

#pragma warning disable CS8618
    private Product() { }
#pragma warning restore CS8618

    private Product(ProductId id, ProductName name, Price price) : base(id)
    {
        Name = name;
        Price = price;
    }

    // Create: 이미 검증된 Value Object를 직접 받음
    public static Product Create(ProductName name, Price price)
    {
        var id = ProductId.New();
        return new Product(id, name, price);
    }

    // CreateFromValidated: ORM 복원용
    public static Product CreateFromValidated(ProductId id, ProductName name, Price price)
        => new(id, name, price);
}
```

**Entity 레벨 비즈니스 규칙이 있는 경우:**

```csharp
[GenerateEntityId]
public class Product : Entity<ProductId>
{
    public ProductName Name { get; private set; }
    public Price SellingPrice { get; private set; }
    public Money Cost { get; private set; }

#pragma warning disable CS8618
    private Product() { }
#pragma warning restore CS8618

    private Product(ProductId id, ProductName name, Price sellingPrice, Money cost) : base(id)
    {
        Name = name;
        SellingPrice = sellingPrice;
        Cost = cost;
    }

    // Validate: Entity 레벨 비즈니스 규칙 (판매가 > 원가)
    public static Validation<Error, Unit> Validate(Price sellingPrice, Money cost) =>
        sellingPrice.Value > cost.Amount
            ? Success<Error, Unit>(unit)
            : DomainError.For<Product>(
                new Custom("SellingPriceBelowCost"),
                sellingPrice.Value,
                $"Selling price must be greater than cost. Price: {sellingPrice.Value}, Cost: {cost.Amount}");

    // Create: Validate 호출 후 Entity 생성
    public static Fin<Product> Create(ProductName name, Price sellingPrice, Money cost) =>
        Validate(sellingPrice, cost)
            .Map(_ => new Product(ProductId.New(), name, sellingPrice, cost))
            .ToFin();

    // CreateFromValidated: ORM 복원용
    public static Product CreateFromValidated(
        ProductId id,
        ProductName name,
        Price sellingPrice,
        Money cost)
        => new(id, name, sellingPrice, cost);
}
```

### 다른 Entity를 참조하는 Entity

Entity가 다른 Entity를 참조할 때는 **EntityId만 참조**합니다 (외래 키 패턴).

```csharp
[GenerateEntityId]
public class OrderItem : Entity<OrderItemId>
{
    public OrderId OrderId { get; private set; }      // Order Entity 참조
    public ProductId ProductId { get; private set; }  // Product Entity 참조
    public Quantity Quantity { get; private set; }
    public Price UnitPrice { get; private set; }

#pragma warning disable CS8618
    private OrderItem() { }
#pragma warning restore CS8618

    private OrderItem(
        OrderItemId id,
        OrderId orderId,
        ProductId productId,
        Quantity quantity,
        Price unitPrice) : base(id)
    {
        OrderId = orderId;
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    // Create: 이미 검증된 Value Object를 직접 받음, EntityId도 그대로 전달
    public static OrderItem Create(
        OrderId orderId,
        ProductId productId,
        Quantity quantity,
        Price unitPrice)
    {
        var id = OrderItemId.New();
        return new OrderItem(id, orderId, productId, quantity, unitPrice);
    }

    // CreateFromValidated: ORM 복원용
    public static OrderItem CreateFromValidated(
        OrderItemId id,
        OrderId orderId,
        ProductId productId,
        Quantity quantity,
        Price unitPrice)
        => new(id, orderId, productId, quantity, unitPrice);
}
```

**왜 Entity 전체가 아닌 EntityId만 참조하나요?**

1. **느슨한 결합**: Entity 간 직접 참조를 피하여 결합도를 낮춥니다.
2. **성능**: 필요할 때만 관련 Entity를 로드합니다 (Lazy Loading).
3. **Aggregate 경계**: 각 Aggregate는 독립적으로 로드/저장됩니다.

**Navigation Property가 필요한 경우:**

EF Core에서 Navigation Property가 필요하면 별도로 추가할 수 있습니다:

```csharp
[GenerateEntityId]
public class OrderItem : Entity<OrderItemId>
{
    public OrderId OrderId { get; private set; }
    public ProductId ProductId { get; private set; }

    // Navigation Property (EF Core용, 도메인 로직에서는 사용 금지)
    public Order? Order { get; private set; }
    public Product? Product { get; private set; }
}
```

### 복합 검증 패턴

Entity 생성 시 검증이 필요한 경우와 불필요한 경우를 구분합니다.

**Entity 레벨 규칙이 없는 경우 - Validate 불필요:**

```csharp
// VO가 이미 검증됨 → Entity는 단순 조합만
public static Order Create(Money amount, Address shippingAddress, CustomerId customerId)
{
    var id = OrderId.New();
    var order = new Order(id, amount, shippingAddress, customerId);
    order.AddDomainEvent(new OrderCreatedEvent(id, customerId, amount));
    return order;
}
```

**Entity 레벨 규칙이 있는 경우 - Validate 필요:**

```csharp
// 시작일 < 종료일 규칙은 Entity 레벨
[GenerateEntityId]
public class Subscription : Entity<SubscriptionId>
{
    public Date StartDate { get; private set; }
    public Date EndDate { get; private set; }
    public CustomerId CustomerId { get; private set; }

    private Subscription(SubscriptionId id, Date startDate, Date endDate, CustomerId customerId) : base(id)
    {
        StartDate = startDate;
        EndDate = endDate;
        CustomerId = customerId;
    }

    // Validate: Entity 레벨 비즈니스 규칙 (시작일 < 종료일)
    public static Validation<Error, Unit> Validate(Date startDate, Date endDate) =>
        startDate < endDate
            ? Success<Error, Unit>(unit)
            : DomainError.For<Subscription>(
                new Custom("StartAfterEnd"),
                startDate.Value,
                $"Start date must be before end date. Start: {startDate.Value}, End: {endDate.Value}");

    // Create: Validate 호출 후 Entity 생성
    public static Fin<Subscription> Create(Date startDate, Date endDate, CustomerId customerId) =>
        Validate(startDate, endDate)
            .Map(_ => new Subscription(SubscriptionId.New(), startDate, endDate, customerId))
            .ToFin();
}
```

### Application Layer에서의 호출 패턴

원시 값에서 Entity를 생성하는 것은 Application Layer의 책임입니다.

```csharp
// Application Layer: 원시 값 → Value Object 생성 → Entity 생성
public async Task<Fin<Order>> CreateOrderAsync(CreateOrderCommand cmd)
{
    // 1. Value Object 생성 (검증 포함)
    var amount = Money.Create(cmd.Amount, cmd.Currency);
    var address = Address.Create(cmd.City, cmd.Street, cmd.PostalCode);
    var customerId = CustomerId.Create(cmd.CustomerId);

    // 2. 모든 VO 검증 성공 시 Entity 생성
    return (amount, address, customerId)
        .Apply((amount, address, customerId) => Order.Create(amount, address, customerId));
}
```

#### Value Object가 없는 필드의 검증

모든 필드가 Value Object로 정의되지 않을 수 있습니다. 예를 들어, `Note`나 `Description` 같은 단순 문자열 필드는 VO로 만들기에 과할 수 있습니다. 이때 **Contextual 검증**을 사용합니다.

**Named Context (일회성 검증):**

```csharp
using Functorium.Domains.ValueObjects.Validations.Contextual;

public async Task<Fin<Order>> CreateOrderAsync(CreateOrderCommand cmd)
{
    // VO가 있는 필드
    var amount = Money.Create(cmd.Amount, cmd.Currency);
    var address = Address.Create(cmd.City, cmd.Street, cmd.PostalCode);

    // VO가 없는 필드: Named Context 사용
    var note = ValidationRules.For("Note")
        .NotEmpty(cmd.Note)
        .ThenMaxLength(500);
    // Error Code: DomainErrors.Note.Empty 또는 DomainErrors.Note.TooLong

    // 모든 검증 결과 병합
    return (amount, address, note)
        .Apply((amount, address, note) => Order.Create(amount, address, note));
}
```

**Context Class (재사용 가능한 검증):**

검증 컨텍스트를 여러 곳에서 재사용하려면 `IValidationContext`를 구현한 클래스를 사용합니다.

```csharp
using Functorium.Domains.ValueObjects.Validations;
using Functorium.Domains.ValueObjects.Validations.Typed;

// Application Layer에 정의
public sealed class OrderValidation : IValidationContext;

public async Task<Fin<Order>> CreateOrderAsync(CreateOrderCommand cmd)
{
    var amount = Money.Create(cmd.Amount, cmd.Currency);

    // Context Class 사용: 타입 안전 + 재사용 가능
    var note = ValidationRules<OrderValidation>.NotEmpty(cmd.Note)
        .ThenMaxLength(500);
    // Error Code: DomainErrors.OrderValidation.Empty

    return (amount, note)
        .Apply((amount, note) => Order.Create(amount, note));
}
```

**여러 Primitive 필드 검증:**

VO 없는 필드가 여러 개일 때도 동일하게 튜플과 `Apply()`로 병합합니다.

```csharp
// VO 1개 + Primitive 여러 개
public async Task<Fin<Order>> CreateOrderAsync(CreateOrderCommand cmd)
{
    // VO가 있는 필드
    var amount = Money.Create(cmd.Amount, cmd.Currency);

    // VO가 없는 필드 여러 개: 각각 Named Context 사용
    var note = ValidationRules.For("Note")
        .NotEmpty(cmd.Note)
        .ThenMaxLength(500);

    var tag = ValidationRules.For("Tag")
        .NotEmpty(cmd.Tag)
        .ThenMaxLength(50);

    var priority = ValidationRules.For("Priority")
        .InRange(cmd.Priority, 1, 5);

    // 모두 튜플로 병합
    return (amount, note, tag, priority)
        .Apply((amount, note, tag, priority) =>
            Order.Create(amount, note, tag, priority));
}
```

**검증 방식 선택 가이드:**

| 상황 | 권장 방식 |
|------|----------|
| 필드에 대응하는 VO가 있음 | `VO.Create()` 또는 `VO.Validate()` |
| VO 없음 + 일회성 검증 | `ValidationRules.For("FieldName")` |
| VO 없음 + 재사용 필요 | `ValidationRules<TContext>` (Context Class) |

> **참고**: Contextual 검증에 대한 자세한 내용은 [값 객체 구현 가이드 - Contextual 검증](./valueobject-guide.md#contextual-검증-named-context)을 참조하세요.

---

## 도메인 로직 메서드

Entity는 단순한 데이터 저장소가 아닌 **도메인 로직의 중심**입니다. 비즈니스 규칙과 상태 변경 로직을 Entity 내부에 캡슐화합니다.

### 쿼리 메서드 (상태 확인)

Entity의 상태를 확인하는 메서드입니다. 부작용이 없고, 상태를 변경하지 않습니다.

```csharp
// 재고가 임계값 미만인지 확인
public bool HasLowStock(Quantity threshold) => StockQuantity < threshold;

// 상품이 만료되었는지 확인
public bool IsExpired() => ExpirationDate < DateTime.UtcNow;

// 총 가치 계산
public Money CalculateTotalValue() => Price.Multiply((decimal)StockQuantity);
```

### 커맨드 메서드 (상태 변경)

Entity의 상태를 변경하는 메서드입니다. 비즈니스 규칙을 검증하고, 도메인 이벤트를 발행합니다.

```csharp
/// <summary>
/// 재고를 차감합니다.
/// </summary>
public Fin<Unit> DeductStock(Quantity quantity)
{
    // 1. 비즈니스 규칙 검증
    if ((int)quantity > (int)StockQuantity)
        return Fin.Fail<Unit>(DomainError.For<Product, int>(
            new Custom("InsufficientStock"),
            currentValue: (int)StockQuantity,
            message: $"Insufficient stock. Current: {(int)StockQuantity}, Requested: {(int)quantity}"));

    // 2. 상태 변경
    StockQuantity = Quantity.Create((int)StockQuantity - (int)quantity).ThrowIfFail();

    // 3. 도메인 이벤트 발행
    AddDomainEvent(new StockDeductedEvent(Id, quantity));

    return unit;  // using static LanguageExt.Prelude; 필요
}
```

### 메서드 유형별 반환 타입

| 메서드 유형 | 반환 타입 | 설명 |
|------------|----------|------|
| 쿼리 (단순 확인) | `bool`, `int`, etc. | 부작용 없는 상태 확인 |
| 쿼리 (VO 계산) | `Money`, `Quantity`, etc. | 계산된 값 객체 반환 |
| 커맨드 (항상 성공) | `void` 또는 `this` | 검증 불필요한 상태 변경 |
| 커맨드 (실패 가능) | `Fin<Unit>` | 비즈니스 규칙 위반 가능 |
| 커맨드 (결과 반환) | `Fin<T>` | 실패 가능 + 계산 결과 반환 |

### 도메인 이벤트와 커맨드 메서드

상태 변경 시 도메인 이벤트를 발행하는 것이 권장됩니다:

```csharp
// 주문 확정 - 상태 변경 + 이벤트 발행
public Fin<Unit> Confirm(string updatedBy)
{
    if (Status != OrderStatus.Pending)
        return DomainError.For<Order>(
            new Custom("InvalidStatus"),
            Status.ToString(),
            "Order can only be confirmed when pending");

    Status = OrderStatus.Confirmed;
    UpdatedAt = DateTime.UtcNow;
    UpdatedBy = updatedBy;

    AddDomainEvent(new OrderConfirmedEvent(Id));

    return unit;
}
```

---

## 실전 예제

### Order Aggregate (복합 예제)

Value Object 속성, Entity 참조, 도메인 이벤트를 모두 포함하는 완전한 예제입니다.

```csharp
using Functorium.Domains.Entities;
using Functorium.Domains.Events;
using Functorium.Domains.SourceGenerator;
using static Functorium.Domains.Errors.DomainErrorType;

// Order Aggregate Root
[GenerateEntityId]
public class Order : AggregateRoot<OrderId>, IAuditableWithUser
{
    #region Domain Events

    public sealed record CreatedEvent(OrderId OrderId, CustomerId CustomerId, Money TotalAmount) : DomainEvent;
    public sealed record ConfirmedEvent(OrderId OrderId) : DomainEvent;
    public sealed record CancelledEvent(OrderId OrderId, string Reason) : DomainEvent;

    #endregion

    private readonly List<OrderItem> _items = [];

    // Value Object 속성
    public Money TotalAmount { get; private set; }
    public Address ShippingAddress { get; private set; }

    // 다른 Entity 참조 (EntityId)
    public CustomerId CustomerId { get; private set; }

    // 상태
    public OrderStatus Status { get; private set; }

    // 감사 정보
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public string? UpdatedBy { get; private set; }

    // 컬렉션
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    // ORM용 기본 생성자
#pragma warning disable CS8618
    private Order() { }
#pragma warning restore CS8618

    // 내부 생성자
    private Order(
        OrderId id,
        CustomerId customerId,
        Money totalAmount,
        Address shippingAddress,
        string createdBy) : base(id)
    {
        CustomerId = customerId;
        TotalAmount = totalAmount;
        ShippingAddress = shippingAddress;
        Status = OrderStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    // Create: 이미 검증된 Value Object를 직접 받음
    public static Order Create(
        CustomerId customerId,
        Money totalAmount,
        Address shippingAddress,
        string createdBy)
    {
        var id = OrderId.New();
        var order = new Order(id, customerId, totalAmount, shippingAddress, createdBy);
        order.AddDomainEvent(new CreatedEvent(id, customerId, totalAmount));
        return order;
    }

    // CreateFromValidated: ORM 복원용
    public static Order CreateFromValidated(
        OrderId id,
        CustomerId customerId,
        Money totalAmount,
        Address shippingAddress,
        OrderStatus status,
        DateTime createdAt,
        string? createdBy,
        DateTime? updatedAt,
        string? updatedBy)
    {
        return new Order
        {
            Id = id,
            CustomerId = customerId,
            TotalAmount = totalAmount,
            ShippingAddress = shippingAddress,
            Status = status,
            CreatedAt = createdAt,
            CreatedBy = createdBy,
            UpdatedAt = updatedAt,
            UpdatedBy = updatedBy
        };
    }

    // 도메인 연산: 주문 확정
    public Fin<Unit> Confirm(string updatedBy)
    {
        if (Status != OrderStatus.Pending)
            return DomainError.For<Order>(
                new Custom("InvalidStatus"),
                Status.ToString(),
                "Order can only be confirmed when pending");

        Status = OrderStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        AddDomainEvent(new ConfirmedEvent(Id));
        return unit;
    }

    // 도메인 연산: 주문 취소
    public Fin<Unit> Cancel(string reason, string updatedBy)
    {
        if (Status == OrderStatus.Shipped || Status == OrderStatus.Delivered)
            return DomainError.For<Order>(
                new Custom("CannotCancel"),
                Status.ToString(),
                "Cannot cancel shipped or delivered orders");

        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        AddDomainEvent(new CancelledEvent(Id, reason));
        return unit;
    }

    // 도메인 연산: 배송지 변경 - Value Object를 직접 받음
    public Fin<Unit> UpdateShippingAddress(Address newAddress, string updatedBy)
    {
        if (Status != OrderStatus.Pending)
            return DomainError.For<Order>(
                new Custom("InvalidStatus"),
                Status.ToString(),
                "Shipping address can only be changed for pending orders");

        ShippingAddress = newAddress;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        return unit;
    }

    // 주문 항목 추가 (내부용)
    internal void AddItem(OrderItem item)
    {
        _items.Add(item);
        RecalculateTotalAmount();
    }

    private void RecalculateTotalAmount()
    {
        var total = _items.Sum(i => (decimal)i.UnitPrice * (int)i.Quantity);
        TotalAmount = Money.CreateFromValidated(total, TotalAmount.Currency);
    }
}

// 주문 상태
public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}
```

---

## FAQ

### Q1. Entity vs AggregateRoot 선택 기준은?

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

| 특성 | GUID | Auto-increment | Ulid |
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

데이터베이스에 저장된 값은 이미 검증을 통과했으므로, 복원 시 다시 검증할 필요가 없습니다.

```csharp
// Repository 구현 예시
public async Task<Product> GetByIdAsync(ProductId id)
{
    var row = await _dbContext.Products.FindAsync(id);

    // CreateFromValidated 사용 - 검증 건너뛰기
    return Product.CreateFromValidated(
        row.Id,
        ProductName.CreateFromValidated(row.Name),
        Price.CreateFromValidated(row.Price));
}
```

| 상황 | 사용 메서드 | 이유 |
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
AddDomainEvent(new PaymentReceivedEvent(Id, Amount));

// 나쁨: 너무 세부적인 이벤트
AddDomainEvent(new OrderStatusChangedEvent(Id, OldStatus, NewStatus));  // 너무 일반적
AddDomainEvent(new PropertyUpdatedEvent(Id, "Name", OldValue, NewValue));  // CRUD 수준
```

**이벤트 발행 위치:**

```csharp
// Entity 레벨 규칙이 없으면 Order를 직접 반환
public static Order Create(...)
{
    var order = new Order(...);
    order.AddDomainEvent(new OrderCreatedEvent(...));  // 생성 시 발행
    return order;
}

public Fin<Unit> Confirm(...)
{
    Status = OrderStatus.Confirmed;
    AddDomainEvent(new OrderConfirmedEvent(Id));  // 상태 변경 시 발행
    return unit;
}
```

### Q5. Entity에서 Validate 메서드가 필요한 경우는?

**Entity.Validate는 Entity 레벨 비즈니스 규칙이 있을 때만 정의합니다.**

Value Object는 각자 자신의 값을 검증하므로, Entity는 VO를 단순 조합할 때 Validate가 필요 없습니다.

```csharp
// Entity 레벨 규칙이 없으면 Validate 불필요
public static Order Create(Money amount, CustomerId customerId)
{
    var id = OrderId.New();
    return new Order(id, amount, customerId);
}
```

**Entity 레벨 비즈니스 규칙이 있을 때만 Validate를 정의합니다:**

```csharp
// Entity 레벨 규칙: 판매가 > 원가
public static Validation<Error, Unit> Validate(Price sellingPrice, Money cost) =>
    sellingPrice.Value > cost.Amount
        ? Success<Error, Unit>(unit)
        : DomainError.For<Product>(
            new Custom("SellingPriceBelowCost"),
            sellingPrice.Value,
            $"Selling price must be greater than cost");

public static Fin<Product> Create(ProductName name, Price sellingPrice, Money cost) =>
    Validate(sellingPrice, cost)
        .Map(_ => new Product(ProductId.New(), name, sellingPrice, cost))
        .ToFin();
```

**Entity 레벨 비즈니스 규칙 예시:**

| 규칙 | 설명 |
|------|------|
| 판매가 > 원가 | Product의 SellingPrice와 Cost 관계 |
| 시작일 < 종료일 | Subscription의 StartDate와 EndDate 관계 |
| 최소 주문 금액 | Order의 TotalAmount 제약 |
| 재고 수량 ≥ 0 | Inventory의 Quantity 제약 |

### Q6. 다른 Entity를 참조할 때 전체 Entity vs EntityId 중 무엇을 사용하나요?

**항상 EntityId만 참조하세요.**

```csharp
// 좋음: EntityId만 참조
public class OrderItem : Entity<OrderItemId>
{
    public OrderId OrderId { get; private set; }      // EntityId만 저장
    public ProductId ProductId { get; private set; }  // EntityId만 저장
}

// 나쁨: Entity 전체 참조
public class OrderItem : Entity<OrderItemId>
{
    public Order Order { get; private set; }      // 강한 결합!
    public Product Product { get; private set; }  // 강한 결합!
}
```

**이유:**

1. **Aggregate 경계 유지**: 각 Aggregate는 독립적으로 로드/저장됩니다.
2. **느슨한 결합**: Entity 간 직접 참조를 피합니다.
3. **성능**: 필요할 때만 관련 Entity를 로드합니다.

**관련 Entity가 필요할 때:**

```csharp
// Application Layer에서 필요한 Entity를 로드
public async Task<OrderDto> GetOrderWithProductsAsync(OrderId orderId)
{
    var order = await _orderRepository.GetByIdAsync(orderId);

    // 필요한 Product들만 로드
    var productIds = order.Items.Select(i => i.ProductId).ToList();
    var products = await _productRepository.GetByIdsAsync(productIds);

    return new OrderDto(order, products);
}
```

---

## 참고 문서

- [값 객체 구현 가이드](./valueobject-guide.md) - Value Object 구현 및 검증 패턴
- [에러 시스템 가이드](./error-guide.md) - 레이어별 에러 정의 및 네이밍 규칙
- [에러 테스트 가이드](./error-testing-guide.md) - 에러 테스트 패턴
- [단위 테스트 가이드](./unit-testing-guide.md)
