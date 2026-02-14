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
- [8. Aggregate Root 구현 패턴](#8-aggregate-root-구현-패턴)
- [9. 자식 Entity 구현 패턴](#9-자식-entity-구현-패턴)
- [10. Cross-Aggregate 관계](#10-cross-aggregate-관계)
- [11. 속성 구성 패턴](#11-속성-구성-패턴)
- [12. 부가 인터페이스](#12-부가-인터페이스)
- [13. EF Core 통합](#13-ef-core-통합)
- [14. 체크리스트](#14-체크리스트)
- [15. FAQ](#15-faq)
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
// Product Aggregate의 불변식: 재고는 음수가 될 수 없다
public Fin<Unit> DeductStock(Quantity quantity)
{
    if (quantity > StockQuantity)
        return DomainError.For<Product, int>(
            new Custom("InsufficientStock"),
            currentValue: StockQuantity,
            message: $"Insufficient stock. Current: {StockQuantity}, Requested: {quantity}");

    StockQuantity = StockQuantity.Subtract(quantity);
    AddDomainEvent(new StockDeductedEvent(Id, quantity));
    return unit;
}
```

### 트랜잭션 경계로서의 Aggregate

**하나의 트랜잭션 = 하나의 Aggregate 변경**이 원칙입니다.

```
✅ 트랜잭션 1개에 Aggregate 1개 변경
┌──────────────────────┐
│ Transaction          │
│  Product.DeductStock │
│  Repository.Save     │
└──────────────────────┘

❌ 트랜잭션 1개에 Aggregate 여러 개 변경
┌──────────────────────────────────┐
│ Transaction                      │
│  Product.DeductStock             │
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

// Event Handler에서 Product Aggregate 업데이트 (별도 트랜잭션)
// public class OnOrderCreated : IDomainEventHandler<Order.CreatedEvent>
// {
//     public async ValueTask Handle(Order.CreatedEvent @event, CancellationToken ct)
//     {
//         // Product.DeductStock 호출
//     }
// }
```

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
| **Product** | Aggregate Root | 독립적 생명주기, 자체 불변식(재고 ≥ 0), 자식 Entity(Tag) 관리 |
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

### Product Aggregate: Root + 자식 Entity + 비즈니스 규칙

```
┌──────────────────────────────────────┐
│  Product Aggregate                   │
│                                      │
│  ┌────────────────────┐              │
│  │ Product (Root)     │              │
│  │  - ProductName     │ ← VO         │
│  │  - ProductDesc     │ ← VO         │
│  │  - Money (Price)   │ ← VO         │
│  │  - Quantity        │ ← VO         │
│  └────────┬───────────┘              │
│           │ 1:N                      │
│  ┌────────┴───────────┐              │
│  │ Tag (Child Entity) │              │
│  │  - TagName         │ ← VO         │
│  └────────────────────┘              │
│                                      │
└──────────────────────────────────────┘
```

**불변식:**
- 재고 수량 ≥ 0 (`DeductStock`에서 보호)
- Tag 중복 방지 (`AddTag`에서 ID로 확인)

**경계 근거:**
- Product는 Tag의 생명주기를 관리 (Tag는 Product 없이 존재하지 않음)
- `DeductStock` 불변식은 Product 내부에서 보호되어야 함
- Tag는 Product를 통해서만 접근 가능 → 자식 Entity

```csharp
[GenerateEntityId]
public sealed class Product : AggregateRoot<ProductId>, IAuditable
{
    private readonly List<Tag> _tags = [];
    public IReadOnlyList<Tag> Tags => _tags.AsReadOnly();

    // 불변식 보호: 재고 ≥ 0
    public Fin<Unit> DeductStock(Quantity quantity)
    {
        if (quantity > StockQuantity)
            return DomainError.For<Product, int>(
                new Custom("InsufficientStock"),
                currentValue: StockQuantity,
                message: $"Insufficient stock. Current: {StockQuantity}, Requested: {quantity}");

        StockQuantity = StockQuantity.Subtract(quantity);
        AddDomainEvent(new StockDeductedEvent(Id, quantity));
        return unit;
    }

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
```

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
public class CreateOrderUsecase
{
    public async Task Handle(CreateOrderCommand cmd)
    {
        var product = await _productRepo.GetById(cmd.ProductId);

        // 불변식 검증이 Aggregate 밖에 있음!
        if (product.StockQuantity < cmd.Quantity)
            throw new InsufficientStockException();

        product.StockQuantity -= cmd.Quantity;  // 직접 수정!
    }
}
```

```csharp
// ✅ Aggregate Root 내부에서 불변식 보호
public class CreateOrderUsecase
{
    public async Task Handle(CreateOrderCommand cmd)
    {
        var product = await _productRepo.GetById(cmd.ProductId);

        // Aggregate Root의 메서드를 통해 상태 변경
        var result = product.DeductStock(cmd.Quantity);
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

## 8. Aggregate Root 구현 패턴

Entity 구현의 핵심은 **검증 책임 분리**입니다. Value Object와 Entity는 서로 다른 검증 책임을 가집니다.

- **Value Object**: 원시 값을 받아 자신의 유효성을 검증
- **Entity**: 이미 검증된 Value Object를 받아 조합. Entity 레벨 비즈니스 규칙이 있을 때만 Validate 정의

### Value Object vs Entity의 역할 차이

| 구분 | Value Object | Entity |
|------|--------------|--------|
| **Validate** | 원시 값 → 검증된 값 반환 | Entity 레벨 비즈니스 규칙만 |
| **Create** | 원시 값 받음 | **Value Object를 직접 받음** |
| **검증 책임** | 자신의 값 검증 | VO 간 관계/규칙 검증 |

> **참고**: Value Object의 검증 패턴은 [값 객체 구현 가이드 - 구현 패턴](./02-value-objects.md#구현-패턴)을 참고하세요.

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

### 팩토리 메서드를 통한 생성

Aggregate Root는 **`Create` 정적 팩토리 메서드**로 생성합니다. 생성자는 `private`으로 캡슐화합니다.

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
    Money price,
    Quantity stockQuantity)
{
    var product = new Product(ProductId.New(), name, description, price, stockQuantity);
    product.AddDomainEvent(new CreatedEvent(product.Id, name, price));
    return product;
}
```

> **참조:** 팩토리 패턴의 전체 가이드(Apply 패턴, Port 조율, EFCore 통합 등)는 [ddd-tactical-improvements.md §8](./ddd-tactical-improvements.md#8-factories-) 참조

### 불변식을 보호하는 커맨드 메서드

상태 변경은 Aggregate Root의 메서드를 통해서만 가능합니다. 비즈니스 규칙 위반 시 `Fin<Unit>`으로 실패를 반환합니다.

```csharp
// Product: 재고 차감 (불변식: 재고 ≥ 0)
public Fin<Unit> DeductStock(Quantity quantity)
{
    if (quantity > StockQuantity)
        return DomainError.For<Product, int>(
            new Custom("InsufficientStock"),
            currentValue: StockQuantity,
            message: $"Insufficient stock. Current: {StockQuantity}, Requested: {quantity}");

    StockQuantity = StockQuantity.Subtract(quantity);
    AddDomainEvent(new StockDeductedEvent(Id, quantity));
    return unit;
}
```

```csharp
// Product: 정보 업데이트 (항상 성공하는 커맨드)
public Product Update(
    ProductName name,
    ProductDescription description,
    Money price,
    Quantity stockQuantity)
{
    var oldPrice = Price;

    Name = name;
    Description = description;
    Price = price;
    StockQuantity = stockQuantity;
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

### 도메인 이벤트

도메인 이벤트는 도메인에서 발생한 중요한 사건을 표현합니다. AggregateRoot에서만 발행할 수 있습니다.

#### IDomainEvent / DomainEvent

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

#### 이벤트 정의 위치

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

#### 이벤트 발행 패턴

AggregateRoot 내에서 `AddDomainEvent()`를 사용하여 이벤트를 수집합니다.

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

Aggregate Root는 비즈니스적으로 의미 있는 상태 변화가 발생할 때 도메인 이벤트를 발행합니다.

```csharp
// Functorium 매핑: AggregateRoot<TId>.AddDomainEvent()
public sealed class Product : AggregateRoot<ProductId>
{
    // 이벤트 정의 (중첩 record)
    public sealed record CreatedEvent(ProductId ProductId, ProductName Name, Money Price) : DomainEvent;
    public sealed record UpdatedEvent(ProductId ProductId, ProductName Name, Money OldPrice, Money NewPrice) : DomainEvent;
    public sealed record StockDeductedEvent(ProductId ProductId, Quantity Quantity) : DomainEvent;

    // 상태 변경 시 이벤트 발행
    public static Product Create(...)
    {
        var product = new Product(...);
        product.AddDomainEvent(new CreatedEvent(product.Id, name, price));
        return product;
    }
}
```

#### 이벤트 Pub/Sub (발행/구독)

`IDomainEvent`는 Mediator의 `INotification`을 확장하여 Pub/Sub 통합을 지원합니다.

**이벤트 발행 (Application Layer에서):**

`IDomainEventPublisher`를 사용하여 Repository 저장 후 이벤트를 발행합니다. `IDomainEventPublisher`는 `FinT<IO, Unit>`을 반환하므로 Repository/Port와 동일한 LINQ 체이닝 패턴으로 사용할 수 있습니다:

```csharp
public sealed class Usecase(
    IProductRepository productRepository,
    IDomainEventPublisher eventPublisher)
    : ICommandUsecase<Request, Response>
{
    public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken ct)
    {
        // 1. Entity 생성/수정
        var product = Product.Create(...);

        // 2. Repository 저장 + 이벤트 발행 (LINQ 체이닝)
        FinT<IO, Response> usecase =
            from savedProduct in _productRepository.Create(product)
            from _ in _eventPublisher.PublishEvents(savedProduct, ct)  // 저장 성공 후 이벤트 발행
            select new Response(...);

        Fin<Response> response = await usecase.Run().RunAsync();
        return response.ToFinResponse();
    }
}
```

**이벤트 핸들러 (구독):**

`IDomainEventHandler<TEvent>`를 구현하여 이벤트를 구독합니다:

```csharp
using Functorium.Applications.Events;

public sealed class OnProductCreated : IDomainEventHandler<Product.CreatedEvent>
{
    private readonly ILogger<OnProductCreated> _logger;

    public OnProductCreated(ILogger<OnProductCreated> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(Product.CreatedEvent notification, CancellationToken ct)
    {
        // 사이드 이펙트 처리: 로깅, 알림, 캐시 무효화 등
        _logger.LogInformation(
            "[DomainEvent] Product created: {ProductId}, Name: {Name}",
            notification.ProductId,
            notification.Name);

        return ValueTask.CompletedTask;
    }
}
```

**핸들러 명명 규칙:**

| 핸들러 유형 | 명명 패턴 | 예시 |
|------------|----------|------|
| Command/Query Handler | `{Command/Query}Handler` | `CreateProductHandler`, `GetProductHandler` |
| Domain Event Handler | `On{EventName}` | `OnProductCreated`, `OnOrderConfirmed` |

Domain Event Handler는 `On` 접두사만 사용합니다:
- `On` 접두사가 이미 이벤트 핸들러임을 나타내므로 `Handler` 접미사는 중복
- Command/Query Handler와 자연스럽게 구분됨
- 간결하고 가독성 향상

**핸들러 등록:**

> **주의**: `Mediator.SourceGenerator`는 해당 패키지가 참조된 프로젝트 내의 핸들러만 자동 등록합니다.
> 다른 어셈블리(예: Application 레이어)의 핸들러는 명시적으로 등록해야 합니다.

Scrutor를 사용하여 어셈블리에서 핸들러를 스캔하고 등록합니다:

```csharp
services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
services.RegisterDomainEventPublisher();  // IDomainEventPublisher 등록

// Application 레이어의 도메인 이벤트 핸들러 등록
services.RegisterDomainEventHandlersFromAssembly(
    YourApp.Application.AssemblyReference.Assembly);
```

`RegisterDomainEventHandlersFromAssembly`는 Scrutor의 `Scan()` API를 사용하여 지정된 어셈블리에서 `IDomainEventHandler<T>` 구현체를 자동으로 검색하고 `INotificationHandler<T>`로 등록합니다.

**트랜잭션 고려사항:**

- 이벤트 발행은 트랜잭션 외부에서 실행됩니다
- 발행 실패 시 비즈니스 로직은 이미 커밋됨 (eventual consistency)
- 강한 일관성이 필요하면 Outbox 패턴을 고려하세요

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

## 9. 자식 Entity 구현 패턴

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

## 10. Cross-Aggregate 관계

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
Order Aggregate                     Product Aggregate
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

---

## 11. 속성 구성 패턴

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

> **참고**: Contextual 검증에 대한 자세한 내용은 [값 객체 구현 가이드 - Contextual 검증](./02-value-objects.md#contextual-검증-named-context)을 참조하세요.

---

## 12. 부가 인터페이스

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

## 13. EF Core 통합

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

## 14. 체크리스트

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

---

## 15. FAQ

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

## 참고 문서

- [값 객체 구현 가이드](./02-value-objects.md) - Value Object 구현 및 검증 패턴
- [에러 시스템 가이드](./05-error-system.md) - 레이어별 에러 정의 및 네이밍 규칙
- [에러 테스트 가이드](./05-error-system.md) - 에러 테스트 패턴
- [단위 테스트 가이드](./09-unit-testing.md)
- [도메인 모델링 개요](./01-ddd-tactical-overview.md) - 도메인 모델링 개요
- [유스케이스 구현 가이드](./07-usecases-and-cqrs.md) - Application Layer에서의 Aggregate 사용
