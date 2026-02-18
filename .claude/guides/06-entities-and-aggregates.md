# Entity와 Aggregate

이 문서는 DDD 전술적 설계 관점에서 Aggregate와 Entity를 **왜** 그렇게 설계하는지(Part 1), Functorium 프레임워크로 **어떻게** 구현하는지(Part 2)를 설명합니다.

## 목차

### Part 1: Aggregate 설계 (WHY + WHAT)

- [1. 왜 Aggregate인가](#1-왜-aggregate인가)
- [2. Aggregate 설계 규칙](#2-aggregate-설계-규칙)
- [3. Aggregate vs Entity vs Value Object 구분](#3-aggregate-vs-entity-vs-value-object-구분)
- [4. Aggregate 경계 설정 실전 예제](#4-aggregate-경계-설정-실전-예제)
- [5. 안티패턴](#5-안티패턴)

### Part 2: Entity/Aggregate 구현 (HOW)

- [6. 클래스 계층](#6-클래스-계층)
- [7. Entity ID 시스템](#7-entity-id-시스템)
- [8. 생성 패턴](#8-생성-패턴)
- [9. 커맨드 메서드와 불변식 보호](#9-커맨드-메서드와-불변식-보호)
- [10. 자식 Entity 구현 패턴](#10-자식-entity-구현-패턴)
- [11. 도메인 이벤트](#11-도메인-이벤트)
- [12. Cross-Aggregate 관계](#12-cross-aggregate-관계)
- [13. 부가 인터페이스](#13-부가-인터페이스)
- [14. 실전 예제](#14-실전-예제)
- [15. 체크리스트](#15-체크리스트)
- [16. FAQ](#16-faq)
- [참고 문서](#참고-문서)

---

# Part 1: Aggregate 설계 (WHY + WHAT)

---

## 1. 왜 Aggregate인가

### 이 가이드의 목적

DDD 전술적 설계에서 가장 중요한 결정은 **Aggregate 경계를 어디에 둘 것인가**입니다. 이 결정이 잘못되면:

- 거대한 Aggregate로 인한 동시성 충돌
- 트랜잭션 범위가 너무 넓어 성능 저하
- Aggregate 간 강한 결합으로 변경이 어려움

이 가이드는 DDD 설계 원칙을 Functorium 프레임워크 구현에 매핑하여, **설계 결정의 근거**를 제공합니다.

### 일관성 경계 (Consistency Boundary)

Aggregate는 **하나의 단위로 일관성을 보장하는 객체 그룹**입니다. Aggregate 내부의 모든 불변식(invariant)은 단일 트랜잭션 내에서 보호됩니다.

```
┌─────────────────────────────────┐
│          Aggregate              │
│                                 │
│  ┌──────────────┐               │
│  │ Aggregate    │  불변식 보호    │  ← 트랜잭션 경계
│  │ Root         │───────────    │
│  └──────┬───────┘               │
│         │                       │
│    ┌────┴────┐                  │
│    │         │                  │
│  Child    Value                 │
│  Entity   Object                │
│                                 │
└─────────────────────────────────┘
```

### 불변식 (Invariant) 보호

불변식이란 **항상 참이어야 하는 비즈니스 규칙**입니다. Aggregate는 이 불변식을 외부에 노출하지 않고 내부에서 보호합니다.

```csharp
// Inventory Aggregate의 불변식: 재고는 음수가 될 수 없다
public Fin<Unit> DeductStock(Quantity quantity)
{
    if (quantity > StockQuantity)
        return DomainError.For<Inventory, int>(
            new Custom("InsufficientStock"),
            currentValue: StockQuantity,
            message: $"Insufficient stock. Current: {StockQuantity}, Requested: {quantity}");

    StockQuantity = StockQuantity.Subtract(quantity);
    UpdatedAt = DateTime.UtcNow;
    AddDomainEvent(new StockDeductedEvent(Id, ProductId, quantity));
    return unit;
}
```

### 트랜잭션 경계로서의 Aggregate

**하나의 트랜잭션 = 하나의 Aggregate 변경**이 원칙입니다.

```
✅ 트랜잭션 1개에 Aggregate 1개 변경
┌─────────────────────────┐
│ Transaction             │
│  Inventory.DeductStock  │
│  Repository.Save        │
└─────────────────────────┘

❌ 트랜잭션 1개에 Aggregate 여러 개 변경
┌──────────────────────────────────┐
│ Transaction                      │
│  Inventory.DeductStock           │
│  Order.Create                    │  ← 동시성 충돌 위험
│  Customer.UpdateCreditLimit      │
└──────────────────────────────────┘
```

### Aggregate의 구성 요소

| 구성 요소 | 역할 | Functorium 매핑 |
|----------|------|----------------|
| **Aggregate Root** | 외부 접근의 유일한 진입점 | `AggregateRoot<TId>` |
| **자식 Entity** | Root가 관리하는 내부 Entity | `Entity<TId>` |
| **Value Object** | 불변 값 | `SimpleValueObject<T>`, `ValueObject` |

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

## 2. Aggregate 설계 규칙

### 규칙 1: Aggregate 경계 안에서 불변식 보호

Aggregate 내부의 모든 불변식은 Aggregate Root를 통해 보호합니다. 외부에서 자식 Entity를 직접 수정할 수 없습니다.

```csharp
// ✅ Aggregate Root(Product)를 통해 Tag를 관리
public sealed class Product : AggregateRoot<ProductId>
{
    private readonly List<Tag> _tags = [];
    public IReadOnlyList<Tag> Tags => _tags.AsReadOnly();

    public Product AddTag(Tag tag)
    {
        // 불변식: 중복 Tag 방지
        if (_tags.Any(t => t.Id == tag.Id))
            return this;

        _tags.Add(tag);
        AddDomainEvent(new TagAssignedEvent(tag.Id, tag.Name));
        return this;
    }

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

```csharp
// ❌ 외부에서 자식 Entity를 직접 수정
product.Tags.Add(newTag);  // IReadOnlyList이므로 컴파일 오류
```

### 규칙 2: 작은 Aggregate를 설계하라

Aggregate는 **불변식 보호에 필요한 최소한의 데이터만** 포함해야 합니다.

```csharp
// ✅ 작은 Aggregate: 필요한 것만 포함
public sealed class Customer : AggregateRoot<CustomerId>
{
    public CustomerName Name { get; private set; }
    public Email Email { get; private set; }
    public Money CreditLimit { get; private set; }
}
```

```csharp
// ❌ 거대한 Aggregate: 관련된 모든 것을 포함
public class Customer : AggregateRoot<CustomerId>
{
    public CustomerName Name { get; private set; }
    public Email Email { get; private set; }
    public List<Order> Orders { get; }         // Customer가 보호할 불변식이 없음
    public List<Address> Addresses { get; }    // 별도 Aggregate로 분리 가능
    public List<PaymentMethod> Payments { get; } // 별도 Aggregate로 분리 가능
}
```

**왜 작아야 하나?**

| 문제 | 큰 Aggregate | 작은 Aggregate |
|------|-------------|---------------|
| 동시성 | 충돌 빈번 | 충돌 최소화 |
| 성능 | 전체 로드 필요 | 필요한 것만 로드 |
| 메모리 | 사용량 높음 | 사용량 낮음 |
| 트랜잭션 | 범위 넓음 | 범위 좁음 |

### 규칙 3: 다른 Aggregate는 ID로만 참조하라

Aggregate 간에는 **EntityId만 저장**합니다. 객체 참조를 직접 사용하지 않습니다.

```csharp
// ✅ ID로만 참조 (Order → Product)
public sealed class Order : AggregateRoot<OrderId>
{
    // 교차 Aggregate 참조 (Product의 ID를 값으로 참조)
    public ProductId ProductId { get; private set; }
    public Quantity Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money TotalAmount { get; private set; }
}
```

```csharp
// ❌ 객체 직접 참조
public class Order : AggregateRoot<OrderId>
{
    public Product Product { get; private set; }  // 강한 결합!
}
```

**왜 ID만 참조하나?**

1. **Aggregate 독립성**: 각 Aggregate는 독립적으로 로드/저장됩니다
2. **느슨한 결합**: Entity 간 직접 참조를 피합니다
3. **성능**: 필요할 때만 관련 Aggregate를 로드합니다

### 규칙 4: 경계 밖에서는 최종 일관성을 사용하라

여러 Aggregate에 걸친 비즈니스 규칙은 **도메인 이벤트**를 통해 최종 일관성(Eventual Consistency)으로 처리합니다.

```csharp
// 주문 생성 시 재고 차감은 별도 Aggregate(Product) 변경
// → 도메인 이벤트로 비동기 처리

// Order Aggregate에서 이벤트 발행
public static Order Create(
    ProductId productId,
    Quantity quantity,
    Money unitPrice,
    ShippingAddress shippingAddress)
{
    var totalAmount = unitPrice.Multiply(quantity);
    var order = new Order(OrderId.New(), productId, quantity, unitPrice, totalAmount, shippingAddress);
    order.AddDomainEvent(new CreatedEvent(order.Id, productId, quantity, totalAmount));
    return order;
}

// Event Handler에서 Inventory Aggregate 업데이트 (별도 트랜잭션)
// public class OnOrderCreated : IDomainEventHandler<Order.CreatedEvent>
// {
//     public async ValueTask Handle(Order.CreatedEvent @event, CancellationToken ct)
//     {
//         // Inventory.DeductStock 호출
//     }
// }
```

> **참고**: 위 이벤트 핸들러는 최종 일관성이 필요한 경우의 예시입니다. 같은 Bounded Context 내에서 관련 Aggregate를 동시에 **생성**하는 경우 등 실용적 예외는 [§4 트랜잭션 경계 실전 가이드라인](#트랜잭션-경계-실전-가이드라인)을 참조하세요.

---

## 3. Aggregate vs Entity vs Value Object 구분

### 의사결정 흐름도

```
이 도메인 개념에 고유 식별자가 필요한가?
│
├── 아니오 → Value Object
│            (Money, Email, Address, Quantity...)
│
└── 예 → Entity
         │
         이 Entity가 독립적으로 저장/조회되는가?
         │
         ├── 예 → Aggregate Root
         │        (Customer, Product, Order...)
         │
         └── 아니오 → 자식 Entity (Aggregate 내부)
                      (Tag, OrderItem...)
```

### 판단 기준 테이블

| 기준 | Value Object | Entity (자식) | Aggregate Root |
|------|-------------|--------------|---------------|
| 고유 식별자 | 없음 | 있음 | 있음 |
| 동등성 | 값 기반 | ID 기반 | ID 기반 |
| 가변성 | 불변 | 가변 | 가변 |
| 독립적 조회 | 불가 | 불가 (Root 통해) | 가능 |
| Repository | 없음 | 없음 | 있음 |
| 도메인 이벤트 | 발행 불가 | 발행 불가 | 발행 가능 |
| 생명주기 | 소유 Entity에 종속 | Root에 종속 | 독립적 |
| Functorium | `SimpleValueObject<T>` | `Entity<TId>` | `AggregateRoot<TId>` |

### 실제 예제 분류

| 도메인 개념 | 분류 | 근거 |
|------------|------|------|
| **Customer** | Aggregate Root | 독립적 생명주기, 자체 불변식(Email 유효성, CreditLimit), Repository 존재 |
| **Product** | Aggregate Root | 독립적 생명주기, 자체 불변식(Tag 중복 방지), 자식 Entity(Tag) 관리 |
| **Inventory** | Aggregate Root | 독립적 생명주기, 자체 불변식(재고 ≥ 0), IConcurrencyAware 동시성 제어 |
| **Order** | Aggregate Root | 독립적 생명주기, Cross-Aggregate 참조(ProductId), 자체 불변식(TotalAmount 계산) |
| **Tag** | 자식 Entity | 자체 ID 보유하지만, Aggregate Root(Product)를 통해서만 접근. 독립 Repository 없음 |
| **Money** | Value Object | 식별자 없음, 값 기반 동등성, 불변 |
| **Email** | Value Object | 식별자 없음, 값 기반 동등성, 불변 |
| **Quantity** | Value Object | 식별자 없음, 값 기반 동등성, 불변 |
| **ShippingAddress** | Value Object | 식별자 없음, 값 기반 동등성, 불변 |

---

## 4. Aggregate 경계 설정 실전 예제

LayeredArch.Domain의 세 가지 Aggregate를 분석합니다.

### Customer Aggregate: Root만 있는 단순 Aggregate

```
┌─────────────────────────────────┐
│  Customer Aggregate             │
│                                 │
│  ┌──────────────────┐           │
│  │ Customer (Root)  │           │
│  │  - CustomerName  │ ← VO      │
│  │  - Email         │ ← VO      │
│  │  - Money         │ ← VO      │
│  └──────────────────┘           │
│                                 │
└─────────────────────────────────┘
```

**불변식:**
- CustomerName, Email, CreditLimit은 각각 Value Object가 자체 검증

**경계 근거:**
- Customer는 독립적인 생명주기를 가짐
- 자식 Entity가 없는 가장 단순한 형태의 Aggregate
- Order와는 ID 참조로만 연결됨 (Order가 `CustomerId`를 소유하지 않음 — 이 예제에서는 Order가 `ProductId`를 참조)

```csharp
[GenerateEntityId]
public sealed class Customer : AggregateRoot<CustomerId>, IAuditable
{
    public CustomerName Name { get; private set; }
    public Email Email { get; private set; }
    public Money CreditLimit { get; private set; }

    public static Customer Create(
        CustomerName name,
        Email email,
        Money creditLimit)
    {
        var customer = new Customer(CustomerId.New(), name, email, creditLimit);
        customer.AddDomainEvent(new CreatedEvent(customer.Id, name, email));
        return customer;
    }
}
```

### Product + Inventory Aggregate: 카탈로그와 재고 분리

재고(고빈도 변경)를 별도 Aggregate로 분리하여 동시성 충돌을 줄인 사례입니다.

```
┌──────────────────────────────────────┐  ┌─────────────────────────────┐
│  Product Aggregate (카탈로그)          │  │  Inventory Aggregate (재고)  │
│                                      │  │                             │
│  ┌────────────────────┐              │  │  ┌──────────────────────┐   │
│  │ Product (Root)     │              │  │  │ Inventory (Root)     │   │
│  │  - ProductName     │ ← VO         │  │  │  - ProductId         │ ID참조│
│  │  - ProductDesc     │ ← VO         │  │  │  - Quantity          │ ← VO │
│  │  - Money (Price)   │ ← VO         │  │  │  - RowVersion        │ 동시성│
│  └────────┬───────────┘              │  │  └──────────────────────┘   │
│           │ 1:N                      │  │                             │
│  ┌────────┴───────────┐              │  └─────────────────────────────┘
│  │ Tag (Child Entity) │              │
│  │  - TagName         │ ← VO         │
│  └────────────────────┘              │
│                                      │
└──────────────────────────────────────┘
```

**Product 불변식:**
- Tag 중복 방지 (`AddTag`에서 ID로 확인)

**Inventory 불변식:**
- 재고 수량 ≥ 0 (`DeductStock`에서 보호, `IConcurrencyAware` 낙관적 동시성)

**경계 근거:**
- Product는 Tag의 생명주기를 관리 (Tag는 Product 없이 존재하지 않음)
- 재고는 주문마다 변경(고빈도)되지만 카탈로그는 저빈도 → 별도 Aggregate
- Inventory는 `ProductId`로 Product를 ID 참조 (객체 참조 아님)

```csharp
// Product: 카탈로그 정보 관리
[GenerateEntityId]
public sealed class Product : AggregateRoot<ProductId>, IAuditable
{
    private readonly List<Tag> _tags = [];
    public IReadOnlyList<Tag> Tags => _tags.AsReadOnly();

    // 불변식 보호: Tag 중복 방지
    public Product AddTag(Tag tag)
    {
        if (_tags.Any(t => t.Id == tag.Id))
            return this;

        _tags.Add(tag);
        AddDomainEvent(new TagAssignedEvent(tag.Id, tag.Name));
        return this;
    }
}

// Inventory: 재고 관리 (낙관적 동시성 제어)
[GenerateEntityId]
public sealed class Inventory : AggregateRoot<InventoryId>, IAuditable, IConcurrencyAware
{
    public ProductId ProductId { get; private set; }
    public Quantity StockQuantity { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    // 불변식 보호: 재고 ≥ 0
    public Fin<Unit> DeductStock(Quantity quantity)
    {
        if (quantity > StockQuantity)
            return DomainError.For<Inventory, int>(
                new Custom("InsufficientStock"),
                currentValue: StockQuantity,
                message: $"Insufficient stock. Current: {StockQuantity}, Requested: {quantity}");

        StockQuantity = StockQuantity.Subtract(quantity);
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new StockDeductedEvent(Id, ProductId, quantity));
        return unit;
    }
}
```

#### Aggregate 분할/병합 의사결정

운영 중인 시스템에서 Aggregate 경계가 적절하지 않다는 신호가 나타나면, 분할 또는 병합을 검토합니다.

**분할 신호** — 다음 중 하나라도 해당하면 분할을 검토:

| 신호 | 증상 | 예시 |
|------|------|------|
| 동시성 충돌 빈번 | `DbUpdateConcurrencyException` 반복 | 주문마다 Product 전체 락 |
| 변경 빈도 불균형 | 일부 속성만 고빈도 변경 | 카탈로그(저빈도) vs 재고(고빈도) |
| 불변식 독립성 | 속성 그룹 간 상호 의존 불변식 없음 | 가격 변경이 재고 규칙에 영향 없음 |

**병합 신호** — 다음 조건이 **모두** 해당하면 병합을 검토:

| 신호 | 증상 | 예시 |
|------|------|------|
| 항상 함께 변경 | 두 Aggregate가 같은 Usecase에서 항상 동시 수정 | A 수정 시 B도 반드시 수정 |
| 상호 불변식 의존 | A의 불변식이 B의 상태에 의존 | 합계 제약 조건 |
| 개별 트랜잭션 불가 | 결과적 일관성으로는 비즈니스 요구 충족 불가 | 즉시 일관성 필수 |

#### 분할 사례: Product → Product + Inventory

**Before** — 단일 Product Aggregate:

```
┌────────────────────────────────────┐
│  Product Aggregate                 │
│                                    │
│  ProductName, Description, Price   │  ← 저빈도 변경 (관리자)
│  StockQuantity                     │  ← 고빈도 변경 (주문마다)
│  DeductStock(), HasLowStock()      │
│                                    │
│  문제: 주문 처리 시 Product 전체에  │
│  동시성 충돌 발생                    │
└────────────────────────────────────┘
```

위의 Product + Inventory 다이어그램이 분할 후의 결과입니다.

**분할 근거:**
- 카탈로그 정보(Name, Description, Price)와 재고(StockQuantity)는 **불변식 독립** — 가격 변경이 재고 규칙에 영향 없음
- 재고는 주문마다 변경(고빈도), 카탈로그는 관리자만 변경(저빈도) — **변경 빈도 불균형**
- 분리 후 Inventory에만 `IConcurrencyAware`(RowVersion) 적용 — 재고 충돌만 감지

**연결 방식:**
- Inventory는 `ProductId`로 Product를 **ID 참조** (객체 참조 아님, [§12 참조](#12-cross-aggregate-관계))
- Application Layer에서 Product 생성 시 Inventory도 함께 생성 (같은 Usecase)
- 재고 차감은 Inventory Aggregate에 직접 요청

#### 트랜잭션 경계 실전 가이드라인

[§1의 원칙](#트랜잭션-경계로서의-aggregate)은 **하나의 트랜잭션 = 하나의 Aggregate 변경**입니다. 실전에서는 다음과 같이 패턴을 분류합니다.

**패턴 분류:**

| 패턴 | 허용 | 예시 | 근거 |
|------|------|------|------|
| 단일 Aggregate 변경 | ✅ | `DeductStockCommand`: Inventory만 변경 | 원칙 준수 |
| 읽기 + 단일 Aggregate 변경 | ✅ | `CreateOrderCommand`: Product 읽기 → Order 생성 | 읽기는 트랜잭션 경합 없음 |
| 동시 생성 (같은 BC) | ⚠️ 예외 허용 | `CreateProductCommand`: Product + Inventory 동시 생성 | 아래 허용 조건 참조 |
| 동시 변경 (기존 Aggregate) | ❌ | 주문 처리 시 Order 생성 + Inventory 차감 | 동시성 충돌 위험 |

**동시 생성 예외 허용 조건** — 다음을 **모두** 충족해야 합니다:

1. **같은 Bounded Context 내**: 서로 다른 BC의 Aggregate를 동시 생성하지 않음
2. **생성(Create) 시점에만**: 기존 Aggregate의 상태 변경이 아닌, 새 Aggregate 생성
3. **상호 불변식 없음**: 두 Aggregate 간에 서로의 상태에 의존하는 불변식이 없음

```csharp
// ✅ 동시 생성 허용: Product + Inventory (CreateProductCommand)
// - 같은 BC 내, 생성 시점, 상호 불변식 없음
FinT<IO, Response> usecase =
    from exists in _productRepository.Exists(new ProductNameUniqueSpec(productName))
    from _ in guard(!exists, /* ... */)
    from createdProduct in _productRepository.Create(product)
    from createdInventory in _inventoryRepository.Create(
        Inventory.Create(createdProduct.Id, stockQuantity))
    select new Response(/* ... */);
```

```csharp
// ❌ 동시 변경 금지: Order 생성 + Inventory 차감
// - Inventory는 기존 Aggregate의 상태 변경 → 별도 트랜잭션으로 처리해야 함
FinT<IO, Response> usecase =
    from inventory in _inventoryRepository.GetByProductId(productId)
    from _1 in inventory.DeductStock(quantity)        // 기존 Aggregate 변경!
    from updated in _inventoryRepository.Update(inventory)
    from order in _orderRepository.Create(
        Order.Create(productId, quantity, unitPrice, shippingAddress))  // 동시에 다른 Aggregate 생성
    select new Response(/* ... */);
```

#### 동시성 고려사항

고경합 Aggregate에는 `IConcurrencyAware` 인터페이스를 선택적으로 적용합니다.

```csharp
// Aggregate Root에 IConcurrencyAware 구현
public sealed class Inventory : AggregateRoot<InventoryId>, IAuditable, IConcurrencyAware
{
    public byte[] RowVersion { get; private set; } = [];
    // ...
}
// EF Core Configuration 및 Mapper 매핑은 13-adapters.md 참조
```

**적용 판단 기준:**

| 상황 | IConcurrencyAware 적용 | 이유 |
|------|----------------------|------|
| 재고 차감 (주문 처리) | **적용** | 다수 사용자가 동시 차감 |
| 카탈로그 정보 수정 | 불필요 | 관리자만 저빈도 변경 |
| 주문 상태 변경 | 상황에 따라 | 동시 상태 변경 가능성 평가 |
| 고객 정보 수정 | 불필요 | 본인만 수정, 충돌 가능성 낮음 |

#### 동시성 충돌 처리 전략

`IConcurrencyAware`를 적용한 Aggregate에서 동시성 충돌이 발생하면, 다음 흐름으로 처리됩니다.

**에러 흐름:**

```
요청 → Handler → UoW.SaveChanges()
                        │
                        ├─ 성공 → 정상 응답
                        │
                        └─ DbUpdateConcurrencyException
                              → AdapterError("ConcurrencyConflict")
                              → Pipeline
                              → 에러 응답 (클라이언트에 위임)
```

**현재 전략: Fail-Fast**

```csharp
// EfCoreUnitOfWork: 동시성 예외를 AdapterError로 변환, 재시도 없이 반환
public virtual FinT<IO, Unit> SaveChanges(CancellationToken cancellationToken = default)
{
    return IO.liftAsync(async () =>
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Fin.Succ(unit);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return AdapterError.FromException<EfCoreUnitOfWork>(
                new Custom("ConcurrencyConflict"), ex);
        }
    });
}
```

**전략 비교:**

| 전략 | 구현 | 적합한 상황 |
|------|------|-------------|
| **Fail-Fast** (현재) | 충돌 시 즉시 에러 반환, 클라이언트가 재시도 판단 | 충돌 빈도 낮음, 클라이언트가 재시도 로직 보유 |
| **Application 재시도** (미구현) | Handler에서 N회 자동 재시도 후 실패 | 충돌 빈도 높고, 재시도가 항상 안전한 멱등 연산 |

**Fail-Fast 선택 근거:**

- Handler는 **비즈니스 로직에 집중** — 재시도 정책은 인프라 관심사
- 재시도가 안전한지(멱등성) 여부는 Usecase마다 다름 — 일괄 자동 재시도는 위험
- 충돌 빈도가 높아지면 Aggregate 분할을 먼저 검토 (근본 원인 해결)

### Order Aggregate: Cross-Aggregate 참조 + 값 계산

```
┌──────────────────────────────────────┐
│  Order Aggregate                     │
│                                      │
│  ┌───────────────────┐               │
│  │ Order (Root)      │               │
│  │  - ProductId ─────────→ Product Aggregate (ID 참조)
│  │  - Quantity       │ ← VO          │
│  │  - Money (Unit)   │ ← VO          │
│  │  - Money (Total)  │ ← VO (계산값)  │
│  │  - ShippingAddr   │ ← VO          │
│  └───────────────────┘               │
│                                      │
└──────────────────────────────────────┘
```

**불변식:**
- TotalAmount = UnitPrice × Quantity (생성 시 계산)

**경계 근거:**
- Order는 독립적인 생명주기를 가짐
- Product Aggregate와는 `ProductId`로만 참조 (객체 참조 없음)
- 상품 검증(`IProductCatalog`)은 Application Layer에서 Order 생성 전에 수행

```csharp
[GenerateEntityId]
public sealed class Order : AggregateRoot<OrderId>, IAuditable
{
    // Cross-Aggregate 참조: ID만 저장
    public ProductId ProductId { get; private set; }

    public Quantity Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money TotalAmount { get; private set; }
    public ShippingAddress ShippingAddress { get; private set; }

    public static Order Create(
        ProductId productId,
        Quantity quantity,
        Money unitPrice,
        ShippingAddress shippingAddress)
    {
        // 불변식: TotalAmount = UnitPrice × Quantity
        var totalAmount = unitPrice.Multiply(quantity);
        var order = new Order(OrderId.New(), productId, quantity, unitPrice, totalAmount, shippingAddress);
        order.AddDomainEvent(new CreatedEvent(order.Id, productId, quantity, totalAmount));
        return order;
    }
}
```

---

## 5. 안티패턴

### 거대한 Aggregate (God Aggregate)

관련된 모든 것을 하나의 Aggregate에 넣는 실수입니다.

```csharp
// ❌ 거대한 Aggregate
public class Customer : AggregateRoot<CustomerId>
{
    public CustomerName Name { get; private set; }
    public List<Order> Orders { get; }           // 별도 Aggregate여야 함
    public List<Product> WishList { get; }       // 별도 Aggregate여야 함
    public List<Review> Reviews { get; }         // 별도 Aggregate여야 함
    public List<PaymentMethod> Payments { get; } // 별도 Aggregate여야 함
}
```

```csharp
// ✅ 작은 Aggregate + ID 참조
public sealed class Customer : AggregateRoot<CustomerId>
{
    public CustomerName Name { get; private set; }
    public Email Email { get; private set; }
    public Money CreditLimit { get; private set; }
    // Order, WishList 등은 각각 독립 Aggregate
}
```

**판단 기준**: "이 데이터가 Aggregate Root의 불변식을 보호하는 데 꼭 필요한가?"

### Aggregate 간 직접 Entity 참조

```csharp
// ❌ Aggregate 간 직접 Entity 참조
public class Order : AggregateRoot<OrderId>
{
    public Product Product { get; private set; }    // 직접 참조
    public Customer Customer { get; private set; }  // 직접 참조
}
```

```csharp
// ✅ ID로만 참조
public sealed class Order : AggregateRoot<OrderId>
{
    public ProductId ProductId { get; private set; }   // ID 참조
    // Customer 정보가 필요하면 Domain Port 사용
}
```

### Aggregate 외부에서 불변식 검증

```csharp
// ❌ Application Layer에서 재고 검증
public class DeductStockUsecase
{
    public async Task Handle(DeductStockCommand cmd)
    {
        var inventory = await _inventoryRepo.GetByProductId(cmd.ProductId);

        // 불변식 검증이 Aggregate 밖에 있음!
        if (inventory.StockQuantity < cmd.Quantity)
            throw new InsufficientStockException();

        inventory.StockQuantity -= cmd.Quantity;  // 직접 수정!
    }
}
```

```csharp
// ✅ Aggregate Root 내부에서 불변식 보호
public class DeductStockUsecase
{
    public async Task Handle(DeductStockCommand cmd)
    {
        var inventory = await _inventoryRepo.GetByProductId(cmd.ProductId);

        // Aggregate Root의 메서드를 통해 상태 변경
        var result = inventory.DeductStock(cmd.Quantity);
        // result가 Fail이면 에러 처리
    }
}
```

### 모든 것을 Aggregate Root로 만들기

```csharp
// ❌ Tag를 불필요하게 Aggregate Root로 만듦
public class Tag : AggregateRoot<TagId>
{
    public TagName Name { get; private set; }
    // Tag는 독립적으로 조회/저장할 필요가 없음
    // Product를 통해서만 접근하면 충분
}
```

```csharp
// ✅ Tag는 자식 Entity로 충분
public sealed class Tag : Entity<TagId>
{
    public TagName Name { get; private set; }
}
```

**판단 기준**: "이 Entity에 독립적인 Repository가 필요한가?"

---

# Part 2: Entity/Aggregate 구현 (HOW)

---

## 6. 클래스 계층

### 클래스 계층 구조

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
            `-- AggregateRoot<TId> : IDomainEventDrain
                +-- DomainEvents (읽기 전용, IHasDomainEvents)
                +-- AddDomainEvent() (protected)
                `-- ClearDomainEvents() (IDomainEventDrain)
```

**계층 이해하기:**

- **IEntity\<TId\>**: Entity의 계약을 정의하는 인터페이스. `Create`, `CreateFromValidated` 메서드명 상수를 포함합니다.
- **Entity\<TId\>**: ID 기반 동등성(`Equals`, `GetHashCode`, `==`, `!=`)을 자동 구현. ORM 프록시 타입도 처리합니다.
- **AggregateRoot\<TId\>**: 도메인 이벤트 관리 기능을 제공합니다. `IDomainEventDrain`(internal)을 구현하여, 이벤트 조회(`IHasDomainEvents`)와 정리(`IDomainEventDrain`)를 분리합니다.

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

> Entity 구현 예제는 [§8. 생성 패턴](#8-생성-패턴)에서 확인할 수 있습니다.

### AggregateRoot\<TId\>

도메인 이벤트 관리 기능을 제공하는 Aggregate Root의 기반 추상 클래스입니다.

**위치**: `Functorium.Domains.Entities.AggregateRoot<TId>`

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
- 도메인 이벤트는 불변의 사실(fact)이므로, 도메인 계약에서 개별 삭제 메서드를 제공하지 않습니다

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

## 7. Entity ID 시스템

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

## 8. 생성 패턴

Entity 구현의 핵심은 **검증 책임 분리**입니다. Value Object와 Entity는 서로 다른 검증 책임을 가집니다.

- **Value Object**: 원시 값을 받아 자신의 유효성을 검증
- **Entity**: 이미 검증된 Value Object를 받아 조합. Entity 레벨 비즈니스 규칙이 있을 때만 Validate 정의

### Value Object vs Entity의 역할 차이

| 구분 | Value Object | Entity |
|------|--------------|--------|
| **Validate** | 원시 값 → 검증된 값 반환 | Entity 레벨 비즈니스 규칙만 |
| **Create** | 원시 값 받음 | **Value Object를 직접 받음** |
| **검증 책임** | 자신의 값 검증 | VO 간 관계/규칙 검증 |

> **참고**: Value Object의 검증 패턴은 [값 객체 구현 가이드 - 구현 패턴](./05-value-objects.md#구현-패턴)을 참고하세요.

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
public static Product Create(ProductName name, ProductDescription description, Money price)
{
    var id = ProductId.New();  // 새 ID 생성
    var product = new Product(id, name, description, price);
    product.AddDomainEvent(new CreatedEvent(product.Id, name, price));
    return product;
}
```

**CreateFromValidated 메서드:**

ORM이나 Repository에서 Entity를 복원할 때 사용합니다. 데이터베이스에서 읽은 값은 이미 검증되었으므로 다시 검증하지 않습니다.

```csharp
public static Product CreateFromValidated(
    ProductId id,
    ProductName name,
    ProductDescription description,
    Money price,
    DateTime createdAt,
    DateTime? updatedAt)
{
    return new Product(id, name, description, price)
    {
        CreatedAt = createdAt,
        UpdatedAt = updatedAt
    };
}
```

**왜 두 가지 메서드가 필요한가요?**

1. **성능**: 데이터베이스에서 대량의 Entity를 로드할 때 검증을 건너뛰어 성능을 향상시킵니다.
2. **의미**: 새 Entity 생성과 기존 Entity 복원은 다른 의미를 가집니다.
3. **ID 관리**: Create는 새 ID를 생성하고, CreateFromValidated는 기존 ID를 사용합니다.

### 패턴 1: 정적 Create() 팩토리 메서드

Aggregate Root는 **`Create` 정적 팩토리 메서드**로 생성합니다. 생성자는 `private`으로 캡슐화합니다. 이미 검증된 Value Object를 받아 새 Aggregate를 생성하고, ID를 자동 발급하며 도메인 이벤트를 발행합니다.

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

**전체 Aggregate Root의 Create() 비교:**

| Aggregate | 매개변수 | ID 생성 | 이벤트 |
|-----------|---------|---------|--------|
| `Product.Create()` | `ProductName, ProductDescription, Money` | `ProductId.New()` | `CreatedEvent` |
| `Inventory.Create()` | `ProductId, Quantity` | `InventoryId.New()` | `CreatedEvent` |
| `Order.Create()` | `ProductId, Quantity, Money, ShippingAddress` | `OrderId.New()` | `CreatedEvent` |
| `Customer.Create()` | `CustomerName, Email, Money` | `CustomerId.New()` | `CreatedEvent` |

**공통 규칙:**
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
    DateTime? updatedAt)
{
    return new Product(id, name, description, price)
    {
        CreatedAt = createdAt,
        UpdatedAt = updatedAt
    };
}
```

**Create vs CreateFromValidated 비교:**

| 항목 | `Create()` | `CreateFromValidated()` |
|------|-----------|------------------------|
| 용도 | 새 Aggregate 생성 | ORM/Repository 복원 |
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
```csharp
// 판매가 > 원가 규칙은 Entity 레벨의 검증
[GenerateEntityId]
public class Product : Entity<ProductId>
{
    public ProductName Name { get; private set; }
    public Price SellingPrice { get; private set; }
    public Money Cost { get; private set; }

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

```csharp
// 시작일 < 종료일 규칙은 Entity 레벨
[GenerateEntityId]
public class Subscription : Entity<SubscriptionId>
{
    public Date StartDate { get; private set; }
    public Date EndDate { get; private set; }
    public CustomerId CustomerId { get; private set; }

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

### 팩토리 패턴 설계 가이드라인

| 시나리오 | 권장 방식 | 예시 |
|---------|---------|------|
| 단순 생성 (VO만 필요) | 정적 `Create()` 직접 호출 | `Customer.Create(name, email, creditLimit)` |
| 병렬 VO 검증 필요 | Apply 패턴 (Usecase 내부) | `CreateProductCommand.CreateProduct()` |
| 외부 데이터 필요 | Usecase에서 Port 조율 후 `Create()` | `CreateOrderCommand` + `IProductCatalog` |
| DB에서 복원 | `CreateFromValidated()` (검증 스킵) | Repository Mapper |

> **Apply 패턴**: Usecase에서 `(v1, v2, v3).Apply(...)` 튜플로 VO를 병렬 검증한 뒤 `Create()`를 호출합니다. 자세한 내용은 [유스케이스 구현 가이드 — Value Object 검증과 Apply 병합 패턴](./11-usecases-and-cqrs.md)을 참조하세요.
>
> **교차 Aggregate 조율**: 다른 Aggregate 데이터가 필요하면 Usecase의 LINQ 체인에서 Port를 통해 조회한 뒤 `Create()`를 호출합니다. 자세한 내용은 [유스케이스 구현 가이드](./11-usecases-and-cqrs.md)를 참조하세요.

**DDD 원칙 충족:**
- **캡슐화**: `private` 생성자로 직접 인스턴스화 차단, 팩토리 메서드만 공개
- **불변식 보호**: `Create()`에서 검증된 VO만 수용, primitive 직접 전달 불가
- **재구성 분리**: `Create()` (새 생성) vs `CreateFromValidated()` (복원) 명확 구분
- **이벤트 일관성**: 새 생성 시에만 도메인 이벤트 발행, 복원 시 이벤트 없음
- **레이어 책임**: Aggregate는 자기 생성만 담당, 외부 조율은 Usecase 책임

---

## 9. 커맨드 메서드와 불변식 보호

### 불변식을 보호하는 커맨드 메서드

상태 변경은 Aggregate Root의 메서드를 통해서만 가능합니다. 비즈니스 규칙 위반 시 `Fin<Unit>`으로 실패를 반환합니다.

```csharp
// Inventory: 재고 차감 (불변식: 재고 ≥ 0)
public Fin<Unit> DeductStock(Quantity quantity)
{
    if (quantity > StockQuantity)
        return DomainError.For<Inventory, int>(
            new Custom("InsufficientStock"),
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

### 자식 Entity 관리 (추가/제거)

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

### 쿼리 메서드 (상태 확인)

Entity의 상태를 확인하는 메서드입니다. 부작용이 없고, 상태를 변경하지 않습니다.

```csharp
// 상품이 만료되었는지 확인
public bool IsExpired() => ExpirationDate < DateTime.UtcNow;

// 주문이 배송 가능한 상태인지 확인
public bool IsShippable() => Status == OrderStatus.Confirmed;
```

### 메서드 유형별 반환 타입

| 메서드 유형 | 반환 타입 | 설명 |
|------------|----------|------|
| 쿼리 (단순 확인) | `bool`, `int`, etc. | 부작용 없는 상태 확인 |
| 쿼리 (VO 계산) | `Money`, `Quantity`, etc. | 계산된 값 객체 반환 |
| 커맨드 (항상 성공) | `void` 또는 `this` | 검증 불필요한 상태 변경 |
| 커맨드 (실패 가능) | `Fin<Unit>` | 비즈니스 규칙 위반 가능 |
| 커맨드 (결과 반환) | `Fin<T>` | 실패 가능 + 계산 결과 반환 |

---

## 10. 자식 Entity 구현 패턴

### Aggregate Root를 통해서만 접근

자식 Entity는 독립적인 Repository가 없으며, 반드시 Aggregate Root를 통해 생성/수정/삭제됩니다.

```csharp
// Tag: 자식 Entity (SharedKernel)
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

### 자체 식별자 보유

자식 Entity는 Value Object와 달리 **고유 식별자**를 가집니다. 이를 통해 컬렉션 내에서 특정 요소를 식별할 수 있습니다.

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

### 자식 Entity의 이벤트 발행

자식 Entity는 도메인 이벤트를 직접 발행하지 않습니다. 대신 **Aggregate Root가 자식 Entity 변경에 대한 이벤트를 발행**합니다.

```csharp
// ✅ Aggregate Root(Product)가 Tag 관련 이벤트 발행
public Product AddTag(Tag tag)
{
    _tags.Add(tag);
    AddDomainEvent(new TagAssignedEvent(tag.Id, tag.Name));  // Root가 발행
    return this;
}

// ❌ 자식 Entity(Tag)가 직접 이벤트 발행
// Tag는 Entity<TId>를 상속하므로 AddDomainEvent()를 사용할 수 없음
```

---

## 11. 도메인 이벤트

도메인 이벤트는 도메인에서 발생한 중요한 사건을 표현합니다. AggregateRoot에서만 발행할 수 있습니다.

> **참고**: 도메인 이벤트의 전체 설계(`IDomainEvent`/`DomainEvent` 정의, Pub/Sub, 핸들러 구독/등록, 트랜잭션 고려사항)는 [도메인 이벤트 가이드](./07-domain-events.md)를 참조하세요.

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
- **Event Handler에서 이벤트 발행 주체 명시**: Handler가 `IDomainEventHandler<Product.CreatedEvent>`를 상속받으면, 코드를 읽는 것만으로 "Product Entity가 발행한 이벤트"임을 즉시 파악 가능

**사용 예시**:
```csharp
// Entity 내부에서 (짧게)
AddDomainEvent(new CreatedEvent(Id, customerId, totalAmount));

// 외부에서 (명시적)
public void Handle(Order.CreatedEvent @event) { ... }
```

### 이벤트 발행 패턴

AggregateRoot 내에서 `AddDomainEvent()`를 사용하여 이벤트를 수집합니다. 비즈니스적으로 의미 있는 상태 변화가 발생할 때 발행합니다.

```csharp
[GenerateEntityId]
public class Order : AggregateRoot<OrderId>
{
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
                new Custom("InvalidStatus"),
                Status.ToString(),
                "Order must be confirmed before shipping");

        Status = OrderStatus.Shipped;
        AddDomainEvent(new ShippedEvent(Id, address));
        return unit;
    }
}
```

---

## 12. Cross-Aggregate 관계

### ID 참조 패턴

다른 Aggregate를 참조할 때는 **EntityId만 저장**합니다.

```csharp
// Order Aggregate가 Product Aggregate를 ID로 참조
public sealed class Order : AggregateRoot<OrderId>
{
    // 교차 Aggregate 참조 (Product의 ID를 값으로 참조)
    public ProductId ProductId { get; private set; }

    public Quantity Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money TotalAmount { get; private set; }
    public ShippingAddress ShippingAddress { get; private set; }
}
```

### Domain Port를 통한 외부 Aggregate 조회

다른 Aggregate의 정보가 필요할 때는 **Domain Port(인터페이스)**를 정의하고, Application Layer에서 구현합니다.

```csharp
// Domain Layer: Port 정의
public interface IProductCatalog : IAdapter
{
    /// <summary>
    /// 상품 존재 여부 확인
    /// </summary>
    FinT<IO, bool> ExistsById(ProductId productId);

    /// <summary>
    /// 상품 가격 조회
    /// </summary>
    FinT<IO, Money> GetPrice(ProductId productId);
}
```

Port는 **도메인이 필요한 것**을 표현합니다:
- `IProductCatalog`는 Product Aggregate 전체를 노출하지 않음
- 필요한 정보(존재 여부, 가격)만 제공
- 구현은 Application/Adapter Layer에서 담당

### 도메인 이벤트를 통한 Aggregate 간 통신

Aggregate 간 상태 동기화는 도메인 이벤트를 통해 처리합니다.

```
Order Aggregate                     Inventory Aggregate
┌──────────────────┐                ┌──────────────────┐
│ Order.Create()   │                │                  │
│   └─ 이벤트 발행  │───────────────→│ DeductStock()    │
│     CreatedEvent │  Event Handler │                  │
└──────────────────┘                └──────────────────┘
      트랜잭션 1                         트랜잭션 2
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

> Navigation Property가 필요한 경우는 [Adapter 구현 가이드](./13-adapters.md)를 참조하세요.

---

## 13. 부가 인터페이스

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

### IConcurrencyAware

낙관적 동시성 제어를 지원합니다. 고경합 Aggregate에 선택적으로 적용합니다.

**위치**: `Functorium.Domains.Entities.IConcurrencyAware`

```csharp
public interface IConcurrencyAware
{
    byte[] RowVersion { get; }
}
```

**사용 예제:**

```csharp
[GenerateEntityId]
public sealed class Inventory : AggregateRoot<InventoryId>, IAuditable, IConcurrencyAware
{
    public Quantity StockQuantity { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
}
```

`IConcurrencyAware` 적용 시기와 EF Core `IsRowVersion()` 매핑은 [§4. Aggregate 경계 설정 실전 예제 — 동시성 고려사항](#동시성-고려사항)을 참고하세요.

---

## 14. 실전 예제

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

## 15. 체크리스트

### Aggregate 경계 설정 시 확인사항

- [ ] **이 Aggregate가 보호하는 불변식은 무엇인가?**
  - 명확한 불변식이 없으면 경계가 잘못되었을 수 있음
- [ ] **Aggregate가 충분히 작은가?**
  - 불변식 보호에 필요한 최소한의 데이터만 포함하는가?
- [ ] **다른 Aggregate를 ID로만 참조하는가?**
  - 객체 직접 참조가 있으면 경계 재검토 필요
- [ ] **하나의 트랜잭션에서 하나의 Aggregate만 변경하는가?**
  - 여러 Aggregate를 동시에 변경하면 설계 재검토 필요
- [ ] **자식 Entity가 Aggregate Root 없이 의미가 있는가?**
  - 있다면 별도 Aggregate로 분리 고려
- [ ] **커맨드 메서드가 불변식을 캡슐화하는가?**
  - 외부에서 불변식을 직접 검증하고 있지 않은가?
- [ ] **도메인 이벤트가 Aggregate Root에서만 발행되는가?**
  - 자식 Entity에서 이벤트를 발행하려 하면 설계 재검토

### Functorium 구현 확인사항

- [ ] Aggregate Root는 `AggregateRoot<TId>` 상속
- [ ] 자식 Entity는 `Entity<TId>` 상속
- [ ] `[GenerateEntityId]` 속성 적용
- [ ] Cross-Aggregate 참조는 `EntityId` 타입만 사용
- [ ] 자식 Entity 컬렉션: `private List<T>` + `public IReadOnlyList<T>`
- [ ] 비즈니스 규칙 위반 시 `Fin<Unit>` 반환
- [ ] 상태 변경 시 `AddDomainEvent()` 호출
- [ ] ORM용 기본 생성자 + `#pragma warning disable CS8618`
- [ ] `Create()` 팩토리 메서드 (새 Entity 생성)
- [ ] `CreateFromValidated()` 메서드 (ORM 복원용)
- [ ] Entity 레벨 비즈니스 규칙이 있으면 `Validate()` 메서드 정의
- [ ] 도메인 이벤트는 중첩 record로 정의 (`Order.CreatedEvent`)
- [ ] EF Core 통합은 [Adapter 구현 가이드](./13-adapters.md) 참조

---

## 16. FAQ

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

// 나쁨: 너무 세부적인 이벤트
AddDomainEvent(new OrderStatusChangedEvent(Id, OldStatus, NewStatus));  // 너무 일반적
AddDomainEvent(new PropertyUpdatedEvent(Id, "Name", OldValue, NewValue));  // CRUD 수준
```

> 이벤트 핸들러 등록, 트랜잭션 고려사항 등 자세한 내용은 [도메인 이벤트 가이드](./07-domain-events.md)를 참조하세요.

### Q5. Entity에서 Validate 메서드가 필요한 경우는?

Entity 레벨 비즈니스 규칙(VO 간 관계 검증)이 있을 때만 정의합니다. [§8. 생성 패턴 — Entity.Validate](#entityvalidate가-필요한-경우-vs-불필요한-경우)를 참조하세요.

### Q6. 다른 Entity를 참조할 때 전체 Entity vs EntityId 중 무엇을 사용하나요?

항상 EntityId만 참조합니다. [§12. Cross-Aggregate 관계](#12-cross-aggregate-관계)를 참조하세요.

---

## 참고 문서

- [값 객체 구현 가이드](./05-value-objects.md) - Value Object 구현 및 검증 패턴
- [도메인 이벤트 가이드](./07-domain-events.md) - 도메인 이벤트 전체 설계 (IDomainEvent, Pub/Sub, 핸들러, 트랜잭션)
- [에러 시스템 가이드](./08-error-system.md) - 레이어별 에러 정의 및 네이밍 규칙
- [도메인 모델링 개요](./04-ddd-tactical-overview.md) - 도메인 모델링 개요
- [유스케이스 구현 가이드](./11-usecases-and-cqrs.md) - Application Layer에서의 Aggregate 사용 (Apply 패턴, 교차 Aggregate 조율)
- [Adapter 구현 가이드](./13-adapters.md) - EF Core 통합, Persistence Model 매핑
- [단위 테스트 가이드](./15-unit-testing.md)
