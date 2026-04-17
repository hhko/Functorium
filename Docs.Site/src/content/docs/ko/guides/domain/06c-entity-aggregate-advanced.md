---
title: "Entity와 Aggregate 구현 — 고급 패턴"
---

이 문서는 Entity/Aggregate의 고급 구현 패턴을 다룹니다. 핵심 패턴(클래스 계층, ID 시스템, 생성 패턴, 커맨드 메서드, 도메인 이벤트)은 [06b-entity-aggregate-core.md](../06b-entity-aggregate-core)를 참조하세요.

## 들어가며

Aggregate의 기본 구조를 잡았다면, 실무에서는 곧바로 다음과 같은 질문이 이어집니다:

- 다른 Aggregate를 참조해야 할 때 객체를 직접 들고 있어도 되는가?
- 생성/수정 시각이나 소프트 삭제 같은 공통 관심사는 어디에 구현하는가?
- 동시에 같은 데이터를 수정하면 어떻게 보호하는가?

### 이 문서에서 배우는 내용

1. Cross-Aggregate 참조를 EntityId로 제한하고 도메인 이벤트로 통신하는 방법
2. `IAuditable`, `ISoftDeletable`, `IConcurrencyAware` 부가 인터페이스의 구현 패턴
3. 부가 인터페이스별 인프라(Mapper, EF Core, Repository) 연동 체크리스트

### 사전 지식

- [Entity/Aggregate 핵심 패턴](../06b-entity-aggregate-core) — 클래스 계층, ID 시스템, 생성 패턴, 커맨드 메서드, 도메인 이벤트
- [Aggregate 설계 원칙](../06a-aggregate-design) — 불변식과 경계 설정 개념

> Aggregate 간 참조는 항상 EntityId만 사용하고, 감사(Audit), 소프트 삭제, 동시성 제어 같은 공통 관심사는 부가 인터페이스로 선언하여 도메인이 명시적으로 필요성을 표현합니다. 인프라 구현은 각 인터페이스의 체크리스트를 따릅니다.

## 요약

### 주요 개념

| 개념 | 설명 |
|------|------|
| Cross-Aggregate 참조 | EntityId로만 참조, 도메인 이벤트로 Aggregate 간 통신 |
| IAuditable | 생성/수정 시각 추적, 도메인이 직접 관리 |
| ISoftDeletable | 소프트 삭제 지원, `Option<DateTime>` 기반 단일 진실 원천 |
| IConcurrencyAware | 낙관적 동시성 제어, RowVersion 기반 Lost Update 방지 |

### 주요 절차

1. Cross-Aggregate 참조는 EntityId만 사용, Domain Port로 외부 Aggregate 조회
2. 필요 시 부가 인터페이스 적용 (`IAuditable`, `ISoftDeletable`, `IConcurrencyAware`)
3. 각 인터페이스의 체크리스트에 따라 도메인 모델 + 인프라 구현

---

## Cross-Aggregate 관계

### ID 참조 패턴

다른 Aggregate를 참조할 때는 **EntityId만 저장합니다.**

아래 코드에서 `ProductId`는 Product Aggregate 자체가 아닌 ID 값만 보유한다는 점을 주목하세요.

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

다른 Aggregate의 정보가 필요할 때는 **Domain Port(인터페이스)를** 정의하고, Application Layer에서 구현합니다.

```csharp
// Domain Layer: Port 정의
public interface IProductCatalog : IObservablePort
{
    /// <summary>
    /// 여러 상품의 가격을 배치로 조회
    /// </summary>
    FinT<IO, Map<ProductId, Money>> GetPricesForProducts(IReadOnlyList<ProductId> productIds);
}
```

Port는 **도메인이 필요한 것을** 표현합니다:
- `IProductCatalog`는 Product Aggregate 전체를 노출하지 않음
- 배치 API로 필요한 정보(가격)를 효율적으로 제공 (N+1 문제 방지)
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

Entity가 다른 Entity를 참조할 때는 **EntityId만 참조합니다** (외래 키 패턴).

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

> Navigation Property가 필요한 경우는 [Adapter 구현 가이드](../adapter/13-adapters)를 참조하세요.

Cross-Aggregate 참조 규칙을 이해했다면, 이제 Entity에 공통적으로 필요한 부가 기능(감사, 소프트 삭제, 동시성 제어)을 인터페이스로 적용하는 방법을 살펴봅니다.

---

## 부가 인터페이스

Entity에 추가 기능을 부여하는 인터페이스들입니다.

### IAuditable

생성/수정 시각을 추적합니다. Cross-Cutting Concern이지만, 엔티티가 자신의 상태를 직접 관리하는 원칙에 따라 도메인 레이어에 배치합니다. EF Core `SaveChanges` 인터셉터 등 인프라에 위임하지 않고, 비즈니스 메서드가 명시적으로 시각을 설정합니다.

**위치**: `Functorium.Domains.Entities.IAuditable`

#### 인터페이스 정의

```csharp
// 시각만 추적
public interface IAuditable
{
    DateTime CreatedAt { get; }
    Option<DateTime> UpdatedAt { get; }
}

// 시각 + 사용자 추적
public interface IAuditableWithUser : IAuditable
{
    Option<string> CreatedBy { get; }
    Option<string> UpdatedBy { get; }
}
```

**설계 포인트:** `Option<T>`을 사용하여 값의 존재/부재를 명시적으로 표현합니다. `null` 대신 `Option.None`으로 "아직 수정되지 않음"을 타입 안전하게 나타냅니다.

#### 구현 패턴 — 도메인이 직접 관리

SingleHost의 5개 엔티티 모두 `IAuditable`을 구현하며, 동일한 패턴을 따릅니다.

| Entity | CreatedAt 설정 위치 | UpdatedAt 설정 위치 |
|--------|-------------------|-------------------|
| Product | 생성자 | `Update()` |
| Order | 생성자 | `TransitionTo()` |
| Tag | 생성자 | `Rename()` |
| Customer | 생성자 | `UpdateCreditLimit()`, `ChangeEmail()` |
| Inventory | 생성자 | `DeductStock()`, `AddStock()` |

**사용 예제 (Product.cs 발췌):**

생성자에서 `CreatedAt`을 설정하고, 비즈니스 메서드에서 `UpdatedAt`을 갱신하는 패턴을 주목하세요.

```csharp
[GenerateEntityId]
public sealed class Product : AggregateRoot<ProductId>, IAuditable, ISoftDeletableWithUser
{
    public DateTime CreatedAt { get; private set; }
    public Option<DateTime> UpdatedAt { get; private set; }

    // 생성자: CreatedAt 설정
    private Product(ProductId id, ProductName name, ...) : base(id)
    {
        Name = name;
        CreatedAt = DateTime.UtcNow;
    }

    // 비즈니스 메서드: UpdatedAt 설정
    public Fin<Product> Update(ProductName name, ...)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
        return this;
    }

    // ORM 복원용: createdAt, updatedAt을 파라미터로 수신
    public static Product CreateFromValidated(
        ProductId id, ...,
        DateTime createdAt,
        Option<DateTime> updatedAt, ...)
    {
        var product = new Product(id, ...) { CreatedAt = createdAt, UpdatedAt = updatedAt };
        return product;
    }
}
```

#### 인프라 전략 — Mapper 변환

| 관점 | 현재 구현 | 대안 (미사용) |
|------|----------|-------------|
| 감사 필드 설정 | 도메인 모델이 직접 설정 | EF Core `SaveChanges` 인터셉터로 자동 주입 |
| Mapper 변환 | `Option<DateTime>.ToNullable()` / `Optional()` | — |
| Persistence Model | `DateTime?` (nullable) | — |

```csharp
// Domain → Persistence Model (ToModel)
CreatedAt = product.CreatedAt,
UpdatedAt = product.UpdatedAt.ToNullable(),    // Option<DateTime> → DateTime?

// Persistence Model → Domain (ToDomain)
Product.CreateFromValidated(
    ...,
    model.CreatedAt,
    Optional(model.UpdatedAt),                  // DateTime? → Option<DateTime>
    ...);
```

#### IAuditableWithUser 참고

`IAuditableWithUser`는 사용자 추적이 필요한 경우를 위해 제공됩니다. SingleHost에서는 아직 사용되지 않으며, 멀티테넌트 등 사용자 식별이 필요한 시나리오에서 적용합니다.

#### 체크리스트 — 새 엔티티에 IAuditable 적용 시

- [ ] `IAuditable` 구현 (`CreatedAt`, `UpdatedAt` 속성)
- [ ] 생성자에서 `CreatedAt = DateTime.UtcNow`
- [ ] 상태 변경 메서드에서 `UpdatedAt = DateTime.UtcNow`
- [ ] `CreateFromValidated()`에 `createdAt`, `updatedAt` 파라미터
- [ ] Persistence Model: `DateTime?` 타입
- [ ] Mapper: `Option<DateTime>.ToNullable()` / `Optional()` 변환

### ISoftDeletable

소프트 삭제를 지원합니다. 실제로 레코드를 삭제하지 않고 삭제됨으로 표시합니다.

**위치**: `Functorium.Domains.Entities.ISoftDeletable`

#### 왜 Soft Delete인가 — 5가지 원칙

| # | 가치 | 설명 |
|---|------|------|
| 1 | **참조 무결성** | Cross-Aggregate 참조 보존. 예: `OrderLine → ProductId` 참조가 존재하므로 물리 삭제 불가 |
| 2 | **비즈니스 의미 분리** | "단종"은 도메인 개념이지 데이터 소멸이 아님. `Delete()`/`Restore()` + 도메인 이벤트로 명시적 모델링 |
| 3 | **복원 가능성** | `Restore()` 메서드로 복구 가능. 멱등성 보장 |
| 4 | **감사 추적** | `ISoftDeletableWithUser`의 `DeletedBy`로 삭제자 추적 |
| 5 | **인프라 관심사 분리** | EF Core Global Query Filter + Dapper `WHERE DeletedAt IS NULL` 자동 필터링 |

#### 인터페이스 정의

```csharp
// 삭제 여부 추적 — Option<DateTime>이 단일 진실 원천
public interface ISoftDeletable
{
    Option<DateTime> DeletedAt { get; }
    bool IsDeleted => DeletedAt.IsSome;  // default interface member (파생 속성)
}

// 삭제 여부 + 삭제자 추적
public interface ISoftDeletableWithUser : ISoftDeletable
{
    Option<string> DeletedBy { get; }
}
```

**설계 포인트:** `bool IsDeleted`는 `DeletedAt`에서 파생되는 default interface member입니다. `Option<DateTime>`이 단일 진실 원천(Single Source of Truth)이므로 상태 불일치가 불가능합니다.

#### 도메인 모델 구현 패턴

**참조**: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/AggregateRoots/Products/Product.cs`

`Delete()`와 `Restore()`의 멱등성 보장 패턴, 그리고 `Update()`에서 삭제된 엔티티 수정을 방지하는 가드 패턴을 주목하세요.

```csharp
[GenerateEntityId]
public sealed class Product : AggregateRoot<ProductId>, ISoftDeletableWithUser
{
    // --- Error Type ---
    public sealed record AlreadyDeleted : DomainErrorType.Custom;

    // --- Domain Events ---
    public sealed record DeletedEvent(ProductId ProductId, string DeletedBy) : DomainEvent;
    public sealed record RestoredEvent(ProductId ProductId) : DomainEvent;

    // --- SoftDelete 속성 ---
    public Option<DateTime> DeletedAt { get; private set; }
    public Option<string> DeletedBy { get; private set; }

    // --- Delete: 멱등성 보장 ---
    public Product Delete(string deletedBy)
    {
        if (DeletedAt.IsSome)           // 이미 삭제됨 → 아무것도 하지 않음
            return this;

        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        AddDomainEvent(new DeletedEvent(Id, deletedBy));
        return this;
    }

    // --- Restore: 멱등성 보장 ---
    public Product Restore()
    {
        if (DeletedAt.IsNone)           // 삭제되지 않음 → 아무것도 하지 않음
            return this;

        DeletedAt = Option<DateTime>.None;
        DeletedBy = Option<string>.None;
        AddDomainEvent(new RestoredEvent(Id));
        return this;
    }

    // --- Update 가드: 삭제된 엔티티 수정 방지 ---
    public Fin<Product> Update(ProductName name, ProductDescription description, Money price)
    {
        if (DeletedAt.IsSome)
            return DomainError.For<Product>(
                new AlreadyDeleted(), Id.ToString(),
                "Cannot update a deleted product");
        // ... 업데이트 로직
        return this;
    }

    // --- ORM 복원용 팩토리: deletedAt, deletedBy 파라미터 포함 ---
    public static Product CreateFromValidated(
        ProductId id, ...,
        Option<DateTime> deletedAt, Option<string> deletedBy)
    {
        var product = new Product(id, ...) { DeletedAt = deletedAt, DeletedBy = deletedBy };
        return product;
    }
}
```

**핵심 패턴 요약:**

| 패턴 | 구현 |
|------|------|
| 멱등성 | `Delete()` — `DeletedAt.IsSome` → early return |
| 멱등성 | `Restore()` — `DeletedAt.IsNone` → early return |
| 에러 가드 | `Update()` — `DeletedAt.IsSome` → `Fin.Fail(AlreadyDeleted)` |
| 도메인 이벤트 | 상태 변경 시 `DeletedEvent`/`RestoredEvent` 발행 |
| 초기화 | `Option<T>.None`으로 복원 (null이 아님) |

#### Repository Port 패턴

**참조**: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/AggregateRoots/Products/IProductRepository.cs`

```csharp
public interface IProductRepository : IRepository<Product, ProductId>
{
    FinT<IO, Product> GetByIdIncludingDeleted(ProductId id);
}
```

`GetByIdIncludingDeleted()`가 필요한 이유: Delete/Restore 커맨드가 삭제된 엔티티에 접근해야 하므로 Global Query Filter를 우회하는 별도 메서드가 필요합니다.

#### 인프라 필터링 전략

| Adapter | 필터 전략 | 우회 방법 |
|---------|-----------|-----------|
| EF Core | `HasQueryFilter(p => p.DeletedAt == null)` | `IgnoreQueryFilters()` |
| Dapper | `WHERE DeletedAt IS NULL` (BuildWhereClause) | 별도 쿼리 작성 |
| InMemory | `p.DeletedAt.IsNone` 조건 | 조건 제거 |

**Mapper 변환:**
- Domain → Model: `Option<DateTime>.ToNullable()` (DB에 `NULL`로 저장)
- Model → Domain: `Optional(model.DeletedAt)` (`NULL` → `Option.None`)

> 인프라 구현 상세는 [Adapter 구현 가이드](../adapter/13-adapters)를 참조하세요.

#### 체크리스트

Soft Delete를 새 Aggregate에 적용할 때 확인할 항목:

- [ ] 도메인 모델: `ISoftDeletableWithUser` 구현
- [ ] 도메인 모델: `Delete()`/`Restore()` 멱등성 메서드
- [ ] 도메인 모델: 상태 변경 메서드에 `DeletedAt.IsSome` 가드
- [ ] 도메인 모델: 도메인 이벤트 (`DeletedEvent`, `RestoredEvent`) 발행
- [ ] 도메인 모델: `CreateFromValidated()`에 `deletedAt`/`deletedBy` 파라미터
- [ ] Repository Port: `GetByIdIncludingDeleted()` 메서드
- [ ] EF Core: `HasQueryFilter(e => e.DeletedAt == null)` 설정
- [ ] Dapper: `WHERE DeletedAt IS NULL` 자동 필터링
- [ ] Mapper: `Option<DateTime>` ↔ `DateTime?` 변환

### IConcurrencyAware

낙관적 동시성 제어를 지원합니다. Aggregate의 불변식(Invariant)은 단일 트랜잭션 안에서만 보호되므로, 동시 트랜잭션 간에는 도메인 로직만으로 불변식 보호가 불가능합니다(Lost Update). 도메인이 "나는 동시성 보호가 필요하다"고 명시적으로 선언하는 인터페이스이며, 고경합 Aggregate에 선택적으로 적용합니다.

**위치**: `Functorium.Domains.Entities.IConcurrencyAware`

#### 인터페이스 정의

```csharp
public interface IConcurrencyAware
{
    byte[] RowVersion { get; }
}
```

#### 왜 필요한가 — Lost Update 시나리오

다음 시나리오는 RowVersion 없이 두 트랜잭션이 동시에 재고를 차감할 때 발생하는 Lost Update 문제를 보여줍니다.

Inventory의 `DeductStock` 예시로 동시성 문제를 설명합니다:

```
초기 상태: 재고 = 10개

1. [트랜잭션 A] 재고를 읽음 → 10개
2. [트랜잭션 B] 재고를 읽음 → 10개  (A가 아직 저장 전이므로 같은 값)
3. [트랜잭션 A] DeductStock(7): 7 ≤ 10 ✓ → 재고 = 3 → DB 저장
4. [트랜잭션 B] DeductStock(7): 7 ≤ 10 ✓ → 재고 = 3 → DB 저장 (A의 결과를 덮어씀!)

최종 결과: 재고 = 3개
기대 결과: B는 거부되어야 함 (A 반영 후 실제 재고 = 3, 7개 차감 불가)
```

핵심: `DeductStock()`의 `if (quantity > StockQuantity)` 가드는 **읽은 시점의 값으로만** 판단합니다. 트랜잭션 B는 A가 저장하기 전의 값(10)을 읽었기 때문에 검증을 통과하지만, 실제로는 재고가 이미 3개로 줄어든 상태입니다. 이것이 **Lost Update** 문제이며, 도메인 로직만으로는 방지할 수 없습니다.

#### 왜 도메인 레이어에 두는가

| 관점 | 설명 |
|------|------|
| 도메인 모델링 결정 | 어떤 Aggregate가 고경합인지는 도메인 지식. Inventory(주문마다 차감)는 고경합, Product(관리자 저빈도 수정)는 저경합 |
| 명시적 선언 | 인프라가 추측하는 것이 아니라, 도메인이 선언 |
| 인프라 분리 | 인터페이스는 도메인, `IsRowVersion()` 매핑은 인프라. 도메인은 DB를 모름 |

#### 도메인 모델 구현 패턴

**참조**: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/AggregateRoots/Inventories/Inventory.cs`

```csharp
[GenerateEntityId]
public sealed class Inventory : AggregateRoot<InventoryId>, IAuditable, IConcurrencyAware
{
    public sealed record InsufficientStock : DomainErrorType.Custom;

    // Value Object 속성
    public Quantity StockQuantity { get; private set; }

    // 낙관적 동시성 제어
    public byte[] RowVersion { get; private set; } = [];

    // Audit 속성
    public DateTime CreatedAt { get; private set; }
    public Option<DateTime> UpdatedAt { get; private set; }

    // 비즈니스 메서드: RowVersion은 DB가 자동 갱신
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

    // ORM 복원용: byte[] rowVersion 파라미터 포함
    public static Inventory CreateFromValidated(
        InventoryId id, ProductId productId, Quantity stockQuantity,
        byte[] rowVersion, DateTime createdAt, Option<DateTime> updatedAt)
    {
        return new Inventory(id, productId, stockQuantity)
        {
            RowVersion = rowVersion,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }
}
```

**핵심 패턴 요약:**

| 패턴 | 구현 |
|------|------|
| RowVersion 선언 | `byte[] RowVersion { get; private set; } = []` |
| 초기값 | 빈 배열 `[]` — DB 저장 시 EF Core가 자동 생성 |
| 비즈니스 메서드 | `RowVersion`을 직접 변경하지 않음 — DB가 자동 갱신 |
| ORM 복원 | `CreateFromValidated()`에 `byte[] rowVersion` 파라미터로 전달 |

#### 인프라 구현 — 전체 흐름

`IConcurrencyAware`를 인프라에서 지원하려면 4개 파일이 협력합니다:

```
Domain Model ──→ Mapper ──→ Persistence Model ──→ DB 저장 (UoW)
(byte[] RowVersion)  (직접 전달)  (byte[] RowVersion)     │
                                       ↑                   │
                              EF Core Configuration        │
                              (.IsRowVersion())            │
                                                           ↓
                                                  UPDATE ... WHERE RowVersion = @original
                                                           │
                                              ┌────────────┴────────────┐
                                              │                         │
                                         행 갱신 성공              행 갱신 0건
                                              │                         │
                                         정상 응답         DbUpdateConcurrencyException
                                                                        │
                                                              ConcurrencyConflict 에러
```

**Step 1. Persistence Model** — `byte[] RowVersion` 속성 정의

```csharp
// InventoryModel.cs
public class InventoryModel
{
    public string Id { get; set; } = default!;
    public string ProductId { get; set; } = default!;
    public int StockQuantity { get; set; }
    public byte[] RowVersion { get; set; } = [];    // ← 동시성 토큰
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

**Step 2. EF Core Configuration** — `.IsRowVersion()`으로 SQL Server ROWVERSION 매핑

```csharp
// InventoryConfiguration.cs
builder.Property(i => i.RowVersion)
    .IsRowVersion();    // SQL Server: 자동 증가하는 8바이트 타임스탬프
```

**Step 3. Mapper** — Domain ↔ Persistence Model 양방향 `byte[]` 직접 전달

```csharp
// InventoryMapper.cs — Domain → Persistence Model
public static InventoryModel ToModel(this Inventory inventory) => new()
{
    // ...
    RowVersion = inventory.RowVersion,
};

// InventoryMapper.cs — Persistence Model → Domain
public static Inventory ToDomain(this InventoryModel model) =>
    Inventory.CreateFromValidated(
        // ...
        model.RowVersion,       // byte[] 직접 전달
        // ...);
```

**Step 4. UoW 충돌 처리** — `DbUpdateConcurrencyException` → `ConcurrencyConflict` 에러 변환

```csharp
// EfCoreUnitOfWork.SaveChanges() 내부
catch (DbUpdateConcurrencyException ex)
{
    return AdapterError.FromException<EfCoreUnitOfWork>(
        new ConcurrencyConflict(), ex);
}
```

**동작 원리:** EF Core는 `IsRowVersion()`으로 설정된 속성을 UPDATE/DELETE 쿼리의 `WHERE` 조건에 자동 추가합니다. DB에 저장된 RowVersion이 읽어온 시점의 값과 다르면 갱신 행이 0건이 되고, EF Core가 `DbUpdateConcurrencyException`을 발생시킵니다. UoW는 이를 `ConcurrencyConflict` 에러로 변환하여 반환합니다.

> 적용 시기, 충돌 처리 전략(Fail-Fast), 전체 UoW 코드는 [§4. Aggregate 경계 설정 실전 예제 — 동시성 고려사항](../06a-aggregate-design#동시성-고려사항)을 참고하세요.

**참조 파일:**
- `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/EfCore/Models/InventoryModel.cs`
- `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/EfCore/Configurations/InventoryConfiguration.cs`
- `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/EfCore/Mappers/InventoryMapper.cs`
- `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/EfCore/EfCoreUnitOfWork.cs`

#### 체크리스트 — 새 Aggregate에 IConcurrencyAware 적용 시

- [ ] 도메인 모델: `IConcurrencyAware` 구현 (`byte[] RowVersion` 속성)
- [ ] 도메인 모델: `CreateFromValidated()`에 `byte[] rowVersion` 파라미터
- [ ] Persistence Model: `byte[] RowVersion` 속성
- [ ] EF Core Configuration: `.IsRowVersion()` 설정
- [ ] Mapper: `RowVersion` 양방향 직접 전달
- [ ] 적용 판단: [§4 적용 기준표](../06a-aggregate-design#동시성-고려사항) 참고

부가 인터페이스의 개별 패턴을 익혔다면, 이제 이들을 모두 조합한 완전한 Aggregate 예제를 살펴봅니다.

---

## 실전 예제

### Order Aggregate (복합 예제)

Value Object 속성, Entity 참조, 도메인 이벤트를 모두 포함하는 완전한 예제입니다.

```csharp
// 참조: samples/ecommerce-ddd/.../OrderStatus.cs, Order.cs
using Functorium.Domains.Entities;
using Functorium.Domains.Events;
using Functorium.Domains.Errors;
using static Functorium.Domains.Errors.DomainErrorType;
using static LanguageExt.Prelude;

// OrderStatus: SimpleValueObject<string> 기반 Smart Enum + 상태 전이 규칙
public sealed class OrderStatus : SimpleValueObject<string>
{
    public sealed record InvalidValue : DomainErrorType.Custom;

    public static readonly OrderStatus Pending = new("Pending");
    public static readonly OrderStatus Confirmed = new("Confirmed");
    public static readonly OrderStatus Shipped = new("Shipped");
    public static readonly OrderStatus Delivered = new("Delivered");
    public static readonly OrderStatus Cancelled = new("Cancelled");

    private static readonly HashMap<string, OrderStatus> All = HashMap(
        ("Pending", Pending), ("Confirmed", Confirmed), ("Shipped", Shipped),
        ("Delivered", Delivered), ("Cancelled", Cancelled));

    // 허용된 전이를 데이터로 선언 — 메서드별 하드코딩 제거
    private static readonly HashMap<string, Seq<string>> AllowedTransitions = HashMap(
        ("Pending", Seq("Confirmed", "Cancelled")),
        ("Confirmed", Seq("Shipped", "Cancelled")),
        ("Shipped", Seq("Delivered")));

    private OrderStatus(string value) : base(value) { }

    public static Fin<OrderStatus> Create(string value) =>
        Validate(value).ToFin();

    public static Validation<Error, OrderStatus> Validate(string value) =>
        All.Find(value)
            .ToValidation(DomainError.For<OrderStatus>(
                new InvalidValue(),
                currentValue: value,
                message: $"Invalid order status: '{value}'"));

    public bool CanTransitionTo(OrderStatus target) =>
        AllowedTransitions.Find(Value)
            .Map(allowed => allowed.Any(v => v == target.Value))
            .IfNone(false);
}

// Order Aggregate Root — 중앙화된 TransitionTo() 패턴
[GenerateEntityId]
public class Order : AggregateRoot<OrderId>, IAuditableWithUser
{
    #region Error Types

    public sealed record InvalidOrderStatusTransition : DomainErrorType.Custom;

    #endregion

    #region Domain Events

    public sealed record CreatedEvent(OrderId OrderId, CustomerId CustomerId, Money TotalAmount) : DomainEvent;
    public sealed record ConfirmedEvent(OrderId OrderId) : DomainEvent;
    public sealed record CancelledEvent(OrderId OrderId) : DomainEvent;

    #endregion

    private readonly List<OrderItem> _items = [];

    // Value Object 속성
    public Money TotalAmount { get; private set; }
    public Address ShippingAddress { get; private set; }

    // 다른 Entity 참조 (EntityId)
    public CustomerId CustomerId { get; private set; }

    // 상태 — OrderStatus는 SimpleValueObject<string> 기반 Smart Enum
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

    // 도메인 연산: 각 메서드가 TransitionTo()에 위임
    public Fin<Unit> Confirm(string updatedBy) =>
        TransitionTo(OrderStatus.Confirmed, new ConfirmedEvent(Id), updatedBy);

    public Fin<Unit> Cancel(string updatedBy) =>
        TransitionTo(OrderStatus.Cancelled, new CancelledEvent(Id), updatedBy);

    // 배송지 변경 — 상태 전이가 아닌 불변식 체크는 CanTransitionTo()와 별개
    public Fin<Unit> UpdateShippingAddress(Address newAddress, string updatedBy)
    {
        if (Status != OrderStatus.Pending)
            return DomainError.For<Order, string, string>(
                new InvalidOrderStatusTransition(),
                value1: Status,
                value2: "UpdateShippingAddress",
                message: "Shipping address can only be changed for pending orders");

        ShippingAddress = newAddress;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        return unit;
    }

    // 중앙화된 상태 전이 — 전이 규칙은 OrderStatus.CanTransitionTo()에 위임
    private Fin<Unit> TransitionTo(OrderStatus target, DomainEvent domainEvent, string updatedBy)
    {
        if (!Status.CanTransitionTo(target))
            return DomainError.For<Order, string, string>(
                new InvalidOrderStatusTransition(),
                value1: Status,
                value2: target,
                message: $"Cannot transition from '{Status}' to '{target}'");

        Status = target;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        AddDomainEvent(domainEvent);
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
```

---

## 체크리스트

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

- [ ] Cross-Aggregate 참조는 `EntityId` 타입만 사용
- [ ] 부가 인터페이스 적용 여부 결정 (`IAuditable`, `ISoftDeletable`, `IConcurrencyAware`)
- [ ] EF Core 통합은 [Adapter 구현 가이드](../adapter/13-adapters) 참조

---

## 트러블슈팅

### Aggregate 간 직접 객체 참조로 인한 트랜잭션 경계 위반
**원인:** 다른 Aggregate의 Entity를 직접 참조(Navigation Property)하면 하나의 트랜잭션에서 여러 Aggregate를 변경하게 되어 설계 원칙에 위배됩니다.
**해결:** Cross-Aggregate 참조는 항상 EntityId만 사용하세요. 다른 Aggregate의 정보가 필요하면 Domain Port를 정의하고, Aggregate 간 상태 동기화는 도메인 이벤트로 처리하세요.

---

## FAQ

### Q1. 다른 Entity를 참조할 때 전체 Entity vs EntityId 중 무엇을 사용하나요?

항상 EntityId만 참조합니다. [§Cross-Aggregate 관계](#cross-aggregate-관계)를 참조하세요.

---

## 참고 문서

- [Entity/Aggregate 핵심 패턴](../06b-entity-aggregate-core) - 클래스 계층, ID 시스템, 생성 패턴, 커맨드 메서드, 도메인 이벤트
- [Aggregate 설계 원칙 (WHY)](../06a-aggregate-design) - Aggregate 설계 원칙과 개념
- [값 객체 구현 가이드](../05a-value-objects) - Value Object 구현 패턴, [검증·열거형 가이드](../05b-value-objects-validation) - 열거형·Application 검증·FAQ
- [도메인 이벤트 가이드](../07-domain-events) - 도메인 이벤트 전체 설계 (IDomainEvent, Pub/Sub, 핸들러, 트랜잭션)
- [에러 시스템: 기초와 네이밍](../08a-error-system) - 에러 처리 기본 원칙과 네이밍 규칙
- [에러 시스템: Domain/Application 에러](../08b-error-system-domain-app) - Domain/Application 에러 정의 및 테스트 패턴
- [도메인 모델링 개요](../04-ddd-tactical-overview) - 도메인 모델링 개요
- [유스케이스 구현 가이드](../application/11-usecases-and-cqrs) - Application Layer에서의 Aggregate 사용 (Apply 패턴, 교차 Aggregate 조율)
- [Adapter 구현 가이드](../adapter/13-adapters) - EF Core 통합, Persistence Model 매핑
- [단위 테스트 가이드](../testing/15a-unit-testing)

---

## Dictionary 조회 성능 팁

Entity/Aggregate 구현에서 Dictionary 기반 캐시나 조회 로직을 사용할 때, `ContainsKey` + 인덱서 조합은 동일한 키를 두 번 조회합니다. `TryGetValue`를 사용하면 단일 조회로 존재 여부와 값을 동시에 확인합니다.

### 값 조회 패턴

```csharp
// 변경 전: 키를 2번 조회
if (_cache.ContainsKey(id))
{
    return _cache[id];
}

// 변경 후: 키를 1번만 조회
if (_cache.TryGetValue(id, out var cachedValue))
{
    return cachedValue;
}
```

### GetOrAdd 패턴

```csharp
// 변경 전
if (!_factories.ContainsKey(type))
{
    _factories[type] = CreateFactory(type);
}
return _factories[type];

// 변경 후
if (!_factories.TryGetValue(type, out var factory))
{
    factory = CreateFactory(type);
    _factories[type] = factory;
}
return factory;
```

### 성능 비교

| 패턴 | 해시 계산 | 버킷 조회 | 총 연산 |
|------|----------|----------|--------|
| `ContainsKey` + `[key]` | 2회 | 2회 | 4 |
| `TryGetValue` | 1회 | 1회 | 2 |

읽기 집약적 워크로드에서는 `ConcurrentDictionary`의 `GetOrAdd`도 고려합니다:

```csharp
private readonly ConcurrentDictionary<string, MetricsSet> _metrics = new();

public MetricsSet GetMetrics(string category)
{
    return _metrics.GetOrAdd(category, key => CreateMetrics(key));
}
```

> **코드 분석 도구**: .NET 분석기 `CA1854`가 `ContainsKey` + 인덱서 패턴을 감지합니다.
