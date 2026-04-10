---
title: "Aggregate Design (WHY + WHAT)"
---

이 문서는 일관성 경계를 올바르게 설정하여 동시성 충돌과 데이터 무결성 문제를 방지하는 Aggregate 설계 원칙을 다룹니다. Entity/Aggregate 구현 방법은 [06b-entity-aggregate-core.md](./06b-entity-aggregate-core)를 참조하세요.

## Introduction

"주문 처리마다 `DbUpdateConcurrencyException`이 발생한다."
"하나의 Entity에 모든 관련 데이터를 넣었더니 트랜잭션이 느려졌다."
"여러 Aggregate를 한 트랜잭션에서 변경하면 안 되는 건 알겠는데, 그러면 데이터 일관성은 어떻게 보장하나?"

이러한 문제들은 Aggregate 경계를 잘못 설정했을 때 나타나는 전형적인 증상입니다. Aggregate는 DDD에서 가장 중요한 설계 결정이며, 이 경계가 시스템의 동시성, 성능, 유지보수성을 좌우합니다.

### What You Will Learn

This document covers the following topics:

1. **Aggregate가 일관성 경계인 이유** - 불변식 보호와 트랜잭션 원칙
2. **Aggregate 설계 4가지 핵심 규칙** - 불변식 보호, 작은 Aggregate, ID 참조, 최종 일관성
3. **Value Object/Entity/Aggregate Root 구분 기준** - 의사결정 흐름도와 판단 기준
4. **분할/병합 의사결정** - 운영 중 경계 재설정이 필요한 신호와 판단 기준
5. **안티패턴 식별과 회피** - God Aggregate, 직접 참조, 외부 불변식 검증 등

### Prerequisites

A basic understanding of the following concepts is needed to understand this document:

- [DDD 전술적 설계 개요](./04-ddd-tactical-overview)의 빌딩블록 전체 맵
- [값 객체(Value Object)](./05a-value-objects) 개념과 불변성 원칙
- 트랜잭션과 동시성 제어의 기본 개념

> Aggregate 경계 하나의 결정이 시스템의 동시성, 성능, 유지보수성을 좌우합니다. 경계를 작게 유지하고, Aggregate 간에는 ID로만 참조하며, 경계 밖의 변경은 도메인 이벤트로 처리하는 것이 핵심 원칙입니다.

## Summary

### Key Commands

```csharp
// Aggregate Root 정의
[GenerateEntityId]
public class Order : AggregateRoot<OrderId> { }

// 불변식 보호 (Aggregate 내부)
public Fin<Unit> DeductStock(Quantity quantity) { ... }

// 도메인 이벤트 발행
AddDomainEvent(new CreatedEvent(Id, productId, quantity, totalAmount));

// Cross-Aggregate 참조 (ID만)
public ProductId ProductId { get; private set; }
```

### Key Procedures

**1. Aggregate 설계:**
1. 도메인 개념의 불변식 식별
2. 불변식을 보호하는 최소 객체 그룹으로 경계 설정
3. Aggregate Root 지정 (외부 접근의 유일한 진입점)
4. 다른 Aggregate는 ID로만 참조

**2. Aggregate 분할/병합 판단:**
1. 동시성 충돌, 변경 빈도 불균형, 불변식 독립성 → 분할 검토
2. 항상 함께 변경, 상호 불변식 의존, 결과적 일관성 불가 → 병합 검토

### Key Concepts

| Concept | Description |
|------|------|
| 일관성 경계 | Aggregate 내부의 불변식을 단일 트랜잭션으로 보호 |
| 트랜잭션 원칙 | 하나의 트랜잭션 = 하나의 Aggregate 변경 |
| ID 참조 | Aggregate 간 객체 직접 참조 금지, EntityId만 저장 |
| 최종 일관성 | Cross-Aggregate 변경은 도메인 이벤트로 비동기 처리 |
| 작은 Aggregate | 불변식 보호에 필요한 최소 데이터만 포함 |

---

## 왜 Aggregate인가

### 이 가이드의 목적

DDD 전술적 설계에서 가장 중요한 결정은 **Aggregate 경계를 어디에 둘 것인가입니다.** 이 결정이 잘못되면:

- 거대한 Aggregate로 인한 동시성 충돌
- 트랜잭션 범위가 너무 넓어 성능 저하
- Aggregate 간 강한 결합으로 변경이 어려움

이 가이드는 DDD 설계 원칙을 Functorium 프레임워크 구현에 매핑하여, **설계 결정의 근거를** 제공합니다.

예를 들어, 상품 카탈로그와 재고를 하나의 Aggregate에 넣으면 관리자의 상품명 수정과 고객의 주문 처리가 동시에 발생할 때마다 동시성 충돌이 일어납니다. 이를 별도 Aggregate로 분리하면 각각 독립적으로 변경할 수 있어 충돌이 사라집니다. 이처럼 Aggregate 경계 하나의 결정이 운영 환경의 안정성을 좌우합니다.

### 일관성 경계 (Consistency Boundary)

Aggregate는 **하나의 단위로 일관성을 보장하는 객체 그룹입니다.** Aggregate 내부의 모든 불변식(invariant)은 단일 트랜잭션 내에서 보호됩니다.

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

불변식이란 **항상 참이어야 하는 비즈니스 규칙입니다.** Aggregate는 이 불변식을 외부에 노출하지 않고 내부에서 보호합니다.

The key point to note in the following code is `DeductStock()` 메서드가 재고 부족 시 예외를 던지지 않고 `Fin<Unit>`으로 실패를 반환한다는 것입니다.

```csharp
// Inventory Aggregate의 불변식: 재고는 음수가 될 수 없다
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

### 트랜잭션 경계로서의 Aggregate

**하나의 트랜잭션 = 하나의 Aggregate 변경이** 원칙입니다.

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

    // CreateFromValidated: 이미 검증/정규화된 데이터를 직접 pass-through
    // DB에서 읽어온 데이터로 Aggregate를 복원합니다.
    // 저장 시점에 이미 검증을 통과한 데이터이므로 검증/정규화를 생략합니다.
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

We have examined the Aggregate concept and its components. In the next section, we will 이 개념을 코드로 구현할 때 따라야 할 4가지 핵심 규칙을 알아봅니다.

---

## Aggregate 설계 규칙

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

**Why should it be small?**

| 문제 | 큰 Aggregate | 작은 Aggregate |
|------|-------------|---------------|
| 동시성 | 충돌 빈번 | 충돌 최소화 |
| 성능 | 전체 로드 필요 | 필요한 것만 로드 |
| 메모리 | 사용량 높음 | 사용량 낮음 |
| 트랜잭션 | 범위 넓음 | 범위 좁음 |

### 규칙 3: 다른 Aggregate는 ID로만 참조하라

Aggregate 간에는 **EntityId만 저장합니다.** 객체 참조를 직접 사용하지 않습니다.

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

**Why reference only by ID?**

1. **Aggregate 독립성**: 각 Aggregate는 독립적으로 로드/저장됩니다
2. **느슨한 결합**: Entity 간 직접 참조를 피합니다
3. **성능**: 필요할 때만 관련 Aggregate를 로드합니다

### 규칙 4: 경계 밖에서는 최종 일관성을 사용하라

여러 Aggregate에 걸친 비즈니스 규칙은 **도메인 이벤트를** 통해 최종 일관성(Eventual Consistency)으로 처리합니다.

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

> **Note**: 하나의 트랜잭션에서 여러 Aggregate를 동시에 변경할 수 없으므로, Cross-Aggregate 부수 효과는 이벤트 핸들러(최종 일관성)로 처리합니다. 같은 Bounded Context 내에서 관련 Aggregate를 동시에 **생성하는** 경우 등 실용적 예외는 [§4 트랜잭션 경계 실전 가이드라인](#트랜잭션-경계-실전-가이드라인)을 참조하세요.

Now that we understand the design rules, let us learn the criteria for classifying domain concepts as Value Object, Entity, or Aggregate Root.

---

## Aggregate vs Entity vs Value Object 구분

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

다음 표는 세 가지 빌딩블록을 7개 기준으로 비교합니다. 핵심 차이는 고유 식별자의 유무와 독립적 조회 가능 여부입니다.

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

We have confirmed the classification criteria and decision flow. In the next section, we will LayeredArch.Domain의 실제 Aggregate를 분석하며 경계 설정의 실전 사례를 살펴봅니다.

---

## Aggregate 경계 설정 실전 예제

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

**Invariants:**
- CustomerName, Email, CreditLimit은 각각 Value Object가 자체 검증

**Boundary Rationale:**
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

**Boundary Rationale:**
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
    #region Error Types

    public sealed record InsufficientStock : DomainErrorType.Custom;

    #endregion

    public ProductId ProductId { get; private set; }
    public Quantity StockQuantity { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    // 불변식 보호: 재고 ≥ 0
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
}
```

#### Aggregate 분할/병합 의사결정

운영 중인 시스템에서 Aggregate 경계가 적절하지 않다는 신호가 나타나면, 분할 또는 병합을 검토합니다.

**Split Signals** — 다음 중 하나라도 해당하면 분할을 검토합니다. 가장 흔한 신호는 동시성 충돌의 빈번한 발생입니다.

| 신호 | 증상 | 예시 |
|------|------|------|
| 동시성 충돌 빈번 | `DbUpdateConcurrencyException` 반복 | 주문마다 Product 전체 락 |
| 변경 빈도 불균형 | 일부 속성만 고빈도 변경 | 카탈로그(저빈도) vs 재고(고빈도) |
| 불변식 독립성 | 속성 그룹 간 상호 의존 불변식 없음 | 가격 변경이 재고 규칙에 영향 없음 |

**Merge Signals** — 다음 조건이 **모두** 해당하면 병합을 검토:

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

**Split Rationale:**
- 카탈로그 정보(Name, Description, Price)와 재고(StockQuantity)는 **불변식 독립** — 가격 변경이 재고 규칙에 영향 없음
- 재고는 주문마다 변경(고빈도), 카탈로그는 관리자만 변경(저빈도) — **변경 빈도 불균형**
- 분리 후 Inventory에만 `IConcurrencyAware`(RowVersion) 적용 — 재고 충돌만 감지

**Connection Method:**
- Inventory는 `ProductId`로 Product를 **ID 참조** (객체 참조 아님, [§Cross-Aggregate 관계](./06c-entity-aggregate-advanced#cross-aggregate-관계))
- Application Layer에서 Product 생성 시 Inventory도 함께 생성 (같은 Usecase)
- 재고 차감은 Inventory Aggregate에 직접 요청

#### 트랜잭션 경계 실전 가이드라인

[§1의 원칙](#트랜잭션-경계로서의-aggregate)은 **하나의 트랜잭션 = 하나의 Aggregate 변경입니다.** 실전에서는 다음과 같이 패턴을 분류합니다.

**Pattern Classification:**

| 패턴 | 허용 | 예시 | 근거 |
|------|------|------|------|
| 단일 Aggregate 변경 | ✅ | `DeductStockCommand`: Inventory만 변경 | 원칙 준수 |
| 읽기 + 단일 Aggregate 변경 | ✅ | `CreateOrderCommand`: Product 읽기 → Order 생성 | 읽기는 트랜잭션 경합 없음 |
| 동시 생성 (같은 BC) | 예외 허용 | `CreateProductCommand`: Product + Inventory 동시 생성 | 아래 허용 조건 참조 |
| 동시 변경 (기존 Aggregate) | ❌ | 주문 처리 시 Order 생성 + Inventory 차감 | 동시성 충돌 위험 |

**Conditions for Allowing Concurrent Creation Exception** — 다음을 **모두** 충족해야 합니다:

1. **같은 Bounded Context 내**: 서로 다른 BC의 Aggregate를 동시 생성하지 않음
2. **생성(Create) 시점에만**: 기존 Aggregate의 상태 변경이 아닌, 새 Aggregate 생성
3. **상호 불변식 없음**: 두 Aggregate 간에 서로의 상태에 의존하는 불변식이 없음

The key point to note in the following code is Product와 Inventory를 동시에 생성하되, 기존 Aggregate의 상태 변경과 다른 Aggregate 생성을 동시에 하는 것은 금지된다는 차이입니다.

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

**Application Decision Criteria:**

| 상황 | IConcurrencyAware 적용 | 이유 |
|------|----------------------|------|
| 재고 차감 (주문 처리) | **적용** | 다수 사용자가 동시 차감 |
| 카탈로그 정보 수정 | 불필요 | 관리자만 저빈도 변경 |
| 주문 상태 변경 | 상황에 따라 | 동시 상태 변경 가능성 평가 |
| 고객 정보 수정 | 불필요 | 본인만 수정, 충돌 가능성 낮음 |

#### 동시성 충돌 처리 전략

`IConcurrencyAware`를 적용한 Aggregate에서 동시성 충돌이 발생하면, 다음 흐름으로 처리됩니다.

**Error Flow:**

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

**Current Strategy: Fail-Fast**

```csharp
// EfCoreUnitOfWork: 동시성 예외를 AdapterError로 변환, 재시도 없이 반환
// Error type definition: public sealed record ConcurrencyConflict : AdapterErrorType.Custom;
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
                new ConcurrencyConflict(), ex);
        }
    });
}
```

**Strategy Comparison:**

| 전략 | 구현 | 적합한 상황 |
|------|------|-------------|
| **Fail-Fast** (현재) | 충돌 시 즉시 에러 반환, 클라이언트가 재시도 판단 | 충돌 빈도 낮음, 클라이언트가 재시도 로직 보유 |
| **Application 재시도** (미구현) | Handler에서 N회 자동 재시도 후 실패 | 충돌 빈도 높고, 재시도가 항상 안전한 멱등 연산 (예: 조회 후 상태 갱신처럼 부수 효과가 동일한 연산) |

**Fail-Fast Selection Rationale:**

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

**Invariants:**
- TotalAmount = UnitPrice × Quantity (생성 시 계산)

**Boundary Rationale:**
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

## Anti-Patterns

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

**Decision Criteria**: "이 데이터가 Aggregate Root의 불변식을 보호하는 데 꼭 필요한가?"

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

**Decision Criteria**: "이 Entity에 독립적인 Repository가 필요한가?"

---

## Troubleshooting

### `DbUpdateConcurrencyException`이 빈번하게 발생

**Cause:** 하나의 Aggregate가 너무 많은 데이터를 포함하여, 서로 무관한 변경이 동일 Aggregate를 잠그는 경우입니다.

**Resolution:** Aggregate 분할을 검토하세요. 변경 빈도가 다른 속성 그룹(예: 카탈로그 정보 vs 재고)을 별도 Aggregate로 분리하면 동시성 충돌을 줄일 수 있습니다. `IConcurrencyAware`는 고경합 Aggregate에만 선택적으로 적용합니다.

### 여러 Aggregate를 하나의 트랜잭션에서 변경하려 함

**Cause:** "하나의 트랜잭션 = 하나의 Aggregate 변경" 원칙을 위반하고 있습니다. 여러 Aggregate를 동시에 변경하면 동시성 충돌 위험과 트랜잭션 범위 확대 문제가 발생합니다.

**Resolution:** Cross-Aggregate 변경은 도메인 이벤트를 통한 최종 일관성으로 처리하세요. 동시 생성은 같은 BC 내에서, 생성 시점에만, 상호 불변식이 없을 때 예외적으로 허용됩니다.

### Aggregate Root를 거치지 않고 자식 Entity를 직접 수정

**Cause:** Aggregate의 불변식이 외부에서 우회되고 있습니다. 자식 Entity의 컬렉션이 `public` 또는 가변 타입으로 노출된 경우 발생합니다.

**Resolution:** 컬렉션은 `IReadOnlyList<T>`로 노출하고, 상태 변경은 반드시 Aggregate Root의 메서드를 통해서만 수행하세요. `_tags.AsReadOnly()` 패턴을 참고하세요.

---

## FAQ

### Q1. Aggregate Root와 일반 Entity의 차이점은?

Aggregate Root는 `AggregateRoot<TId>`를 상속하며 도메인 이벤트를 발행할 수 있고, 독립적인 Repository를 가집니다. 일반 Entity는 `Entity<TId>`를 상속하며 Aggregate Root를 통해서만 접근 가능하고, 독립 Repository가 없습니다.

| 특성 | Aggregate Root | 일반 Entity |
|------|---------------|------------|
| 기반 클래스 | `AggregateRoot<TId>` | `Entity<TId>` |
| 도메인 이벤트 | 발행 가능 | 불가 |
| Repository | 있음 | 없음 |
| 외부 접근 | 직접 가능 | Root를 통해서만 |

### Q2. Aggregate 경계를 어떻게 판단하나요?

핵심 질문: "이 Entity가 독립적으로 저장/조회되는가?" 독립 생명주기가 필요하면 Aggregate Root, 다른 Root에 종속되면 자식 Entity입니다. 추가로 "이 데이터가 Root의 불변식을 보호하는 데 꼭 필요한가?"를 질문하여 포함 여부를 결정합니다.

### Q3. 도메인 이벤트로 최종 일관성을 사용하면 데이터 불일치가 발생하지 않나요?

최종 일관성은 즉시 일관성과 달리 일시적 불일치를 허용합니다. 이벤트 핸들러가 처리를 완료하면 일관성이 보장됩니다. 비즈니스 요구사항이 즉시 일관성을 필수로 요구하는 경우에만 Aggregate 병합을 검토하세요.

### Q4. `IConcurrencyAware`는 모든 Aggregate에 적용해야 하나요?

아닙니다. 다수 사용자가 동시에 변경하는 고경합 Aggregate(예: 재고 차감)에만 적용합니다. 관리자만 저빈도로 변경하는 Aggregate(예: 카탈로그 정보, 고객 정보)에는 불필요합니다.

### Q5. 동시 생성 예외는 어떤 조건에서 허용되나요?

같은 Bounded Context 내에서, 새 Aggregate 생성 시점에만, 두 Aggregate 간 상호 불변식이 없을 때 허용됩니다. 기존 Aggregate의 상태 변경과 다른 Aggregate 생성/변경을 동시에 하는 것은 금지됩니다.

---

## References

- [Entity/Aggregate 핵심 패턴 (HOW)](./06b-entity-aggregate-core)
- [Entity/Aggregate 고급 패턴](./06c-entity-aggregate-advanced)
