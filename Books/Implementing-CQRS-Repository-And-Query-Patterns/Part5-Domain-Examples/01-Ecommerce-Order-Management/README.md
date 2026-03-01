# Part 5 - Chapter 19: E-commerce 주문 관리

> **Part 5: 도메인별 실전 예제** | [← 이전: 18장 Transaction Pipeline →](../../Part4-CQRS-Usecase-Integration/05-Transaction-Pipeline) | [다음: 20장 Customer Management →](../02-Customer-Management)

---

## 개요

E-commerce 주문 도메인을 통해 CQRS 패턴의 Command 측을 완전한 예제로 구현합니다. Order Aggregate Root, OrderLine 자식 Entity, 상태 전이 규칙, 도메인 이벤트, Repository 패턴을 하나의 예제에서 종합적으로 다룹니다.

---

## 학습 목표

- **Aggregate Root + 자식 Entity** 구조 설계
- **상태 전이(State Transition)** 규칙과 `Fin<Unit>` 반환 패턴
- **도메인 이벤트** 발행과 수집
- **InMemoryRepository** 기반 CRUD 구현
- **팩토리 메서드**에서의 비즈니스 규칙 검증

---

## 핵심 개념

### 주문 상태 전이 다이어그램

```
Pending ──→ Confirmed ──→ Shipped ──→ Delivered
  │             │
  └──→ Cancelled ←──┘
```

- `Confirm()`: Pending → Confirmed
- `Ship()`: Confirmed → Shipped
- `Deliver()`: Shipped → Delivered
- `Cancel()`: Pending 또는 Confirmed → Cancelled (Delivered 불가)

### Aggregate Root와 자식 Entity

```csharp
// Order (Aggregate Root) → OrderLine (자식 Entity)
var order = Order.Create("홍길동", orderLines).ThrowIfFail();
order.Confirm();      // Fin<Unit> 반환
order.Ship();         // 상태 전이 실패 시 Error 반환
```

### 도메인 이벤트 흐름

```csharp
Order.Create(...)     → OrderCreatedEvent
order.Confirm()       → OrderConfirmedEvent
order.Ship()          → OrderShippedEvent
order.Deliver()       → OrderDeliveredEvent
order.Cancel()        → OrderCancelledEvent
```

---

## 프로젝트 설명

### 파일 구조

| 파일 | 역할 |
|------|------|
| `OrderId.cs` | Ulid 기반 주문 식별자 |
| `OrderLineId.cs` | Ulid 기반 주문 항목 식별자 |
| `OrderStatus.cs` | 주문 상태 열거형 |
| `OrderLine.cs` | 주문 항목 자식 Entity |
| `Order.cs` | 주문 Aggregate Root (상태 전이 + 도메인 이벤트) |
| `OrderDto.cs` | Query 측 DTO |
| `IOrderRepository.cs` | Repository 인터페이스 |
| `InMemoryOrderRepository.cs` | InMemory 구현 |

### Order Aggregate 설계 포인트

1. **팩토리 검증**: `Create()`에서 빈 고객명, 빈 주문 항목을 `Fin<Order>`로 검증
2. **금액 자동 계산**: `TotalAmount`는 OrderLine들의 `LineTotal` 합계로 계산
3. **불변식 보호**: 상태 전이 메서드가 `Fin<Unit>`을 반환하여 실패를 명시적으로 처리
4. **이벤트 추적**: 각 상태 전이에서 `AddDomainEvent()` 호출

---

## 한눈에 보는 정리

| 개념 | 구현 |
|------|------|
| Aggregate Root | `Order : AggregateRoot<OrderId>` |
| 자식 Entity | `OrderLine : Entity<OrderLineId>` |
| 상태 전이 | `Confirm()`, `Ship()`, `Deliver()`, `Cancel()` → `Fin<Unit>` |
| 도메인 이벤트 | `OrderCreatedEvent`, `OrderConfirmedEvent`, etc. |
| Repository | `IOrderRepository : IRepository<Order, OrderId>` |
| InMemory 구현 | `InMemoryOrderRepository : InMemoryRepositoryBase<Order, OrderId>` |

---

## FAQ

**Q: OrderLine을 별도 Aggregate로 만들지 않는 이유는?**
A: OrderLine은 Order 없이는 존재 의미가 없는 자식 Entity입니다. Aggregate 경계는 "함께 변경되어야 하는 단위"로 결정하며, OrderLine은 항상 Order와 함께 생성/변경됩니다.

**Q: TotalAmount를 매번 계산하는 대신 캐싱하면 안 되나요?**
A: 이 예제에서는 생성 시점에 계산하여 저장합니다. 실무에서는 OrderLine 변경 시 재계산 로직이 필요하며, 이는 Aggregate Root가 불변식을 보호하는 좋은 예입니다.

**Q: Cancel()이 Shipped 상태에서는 불가능한 이유는?**
A: 이 예제에서는 Confirmed까지만 취소를 허용합니다. 실무에서는 Shipped 상태에서도 반품 프로세스를 통한 취소가 가능할 수 있으며, 이는 별도의 도메인 이벤트(ReturnRequestedEvent)로 처리합니다.
