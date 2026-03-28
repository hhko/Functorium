---
title: "도메인 이벤트"
---
## 개요

주문이 확인되면 결제 시스템에 알리고, 재고를 차감해야 합니다. Aggregate Root가 이 시스템들을 **직접 호출하면 어떻게 될까요?** 주문 도메인이 결제, 재고에 강하게 결합되어 변경이 어려워집니다.

Domain Event는 **도메인에서 발생한 의미 있는 사건을 나타내는 불변 객체**입니다. Aggregate Root는 상태가 변경될 때 도메인 이벤트를 발행하고, 인프라 계층에서 이를 수집하여 다른 Aggregate나 외부 시스템에 전달합니다. 이 장에서는 `IDomainEvent`, `DomainEvent` record, 그리고 `AggregateRoot<TId>`의 이벤트 관리 메커니즘을 실습합니다.

---

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. `IDomainEvent`의 EventId, OccurredAt, CorrelationId, CausationId 각 속성의 역할을 **설명할 수 있습니다**
2. `DomainEvent` record를 상속하여 불변 이벤트를 **정의할 수 있습니다**
3. `AddDomainEvent()`와 `ClearDomainEvents()`로 이벤트를 등록하고 정리하는 패턴을 **적용할 수 있습니다**

### 실습을 통해 확인할 내용
- **OrderCreatedEvent**: 주문 생성 시 발행되는 도메인 이벤트
- **OrderConfirmedEvent**: 주문 확인 시 발행되는 도메인 이벤트
- **ClearDomainEvents()**: 이벤트 발행 후 정리하는 인프라 패턴

---

## 핵심 개념

### 왜 필요한가?

Aggregate가 다른 시스템을 직접 호출하면 결합도가 높아집니다. 대신 "주문이 확인됐다"는 **사실을 이벤트로 발행**하면, 관심 있는 시스템이 각자 구독하여 처리합니다. 발행자는 구독자를 알 필요가 없으므로 느슨한 결합이 만들어집니다.

### Domain Event의 구조

이벤트가 올바르게 추적되려면 어떤 정보가 필요할까요? `IDomainEvent`가 정의하는 속성을 보세요.

```csharp
public interface IDomainEvent : INotification
{
    DateTimeOffset OccurredAt { get; }   // 이벤트 발생 시각
    Ulid EventId { get; }               // 이벤트 고유 ID (멱등성 보장)
    string? CorrelationId { get; }       // 요청 추적 ID
    string? CausationId { get; }         // 원인 이벤트 ID
}
```

각 속성의 역할을 정리하면 다음과 같습니다:

- **EventId**: 이벤트 중복 처리 방지 (멱등성)
- **CorrelationId**: 같은 요청에서 발생한 이벤트를 그룹핑
- **CausationId**: 이벤트 간의 인과 관계 추적

### DomainEvent 기본 record

모든 이벤트가 이 속성들을 매번 정의하면 번거롭겠죠? `DomainEvent` 기본 record가 편의 생성자를 제공합니다.

```csharp
public abstract record DomainEvent(...) : IDomainEvent
{
    protected DomainEvent() : this(DateTimeOffset.UtcNow, Ulid.NewUlid(), null, null) { }
}
```

파생 이벤트는 매개변수 없는 생성자를 사용하여 간단하게 정의합니다:

```csharp
public sealed record OrderCreatedEvent(
    OrderId OrderId,
    string CustomerName,
    decimal TotalAmount) : DomainEvent;
```

비즈니스에 필요한 데이터만 선언하면, EventId와 OccurredAt은 자동으로 채워집니다.

### 이벤트 발행 패턴

Aggregate Root 내부에서 상태 변경 시 `AddDomainEvent()`를 호출합니다:

```csharp
public static Order Create(string customerName, decimal totalAmount)
{
    var order = new Order(OrderId.New(), customerName, totalAmount);
    order.AddDomainEvent(new OrderCreatedEvent(order.Id, customerName, totalAmount));
    return order;
}
```

인프라 계층(예: SaveChanges)에서 이벤트를 수집하고 발행한 후 `ClearDomainEvents()`를 호출합니다. 이렇게 하면 트랜잭션 커밋 후에만 이벤트가 전달되어 데이터 일관성을 보장할 수 있습니다.

---

## 프로젝트 설명

### 프로젝트 구조
```
DomainEvents/
├── Program.cs                  # 이벤트 발행 데모
├── OrderId.cs                  # 주문 ID
├── OrderCreatedEvent.cs        # 주문 생성 이벤트
├── OrderConfirmedEvent.cs      # 주문 확인 이벤트
├── Order.cs                    # 이벤트를 발행하는 Aggregate Root
└── DomainEvents.csproj

DomainEvents.Tests.Unit/
├── OrderDomainEventTests.cs    # 이벤트 발행/정리 테스트
├── Using.cs
├── xunit.runner.json
└── DomainEvents.Tests.Unit.csproj
```

### 핵심 코드

#### OrderCreatedEvent.cs

주문 생성 시 발행되는 이벤트입니다. `DomainEvent`를 상속하므로 EventId, OccurredAt 등은 자동으로 설정됩니다.

```csharp
public sealed record OrderCreatedEvent(
    OrderId OrderId,
    string CustomerName,
    decimal TotalAmount) : DomainEvent;
```

#### Order.cs (이벤트 발행 부분)

상태가 변경될 때마다 해당하는 도메인 이벤트를 등록합니다. `Create()`에서는 `OrderCreatedEvent`를, `Confirm()`에서는 `OrderConfirmedEvent`를 발행하는 것을 확인하세요.

```csharp
public static Order Create(string customerName, decimal totalAmount)
{
    var order = new Order(OrderId.New(), customerName, totalAmount);
    order.AddDomainEvent(new OrderCreatedEvent(order.Id, customerName, totalAmount));
    return order;
}

public Fin<Unit> Confirm()
{
    if (Status != OrderStatus.Pending)
        return Error.New(...);

    Status = OrderStatus.Confirmed;
    AddDomainEvent(new OrderConfirmedEvent(Id));
    return unit;
}
```

상태 전이와 이벤트 발행이 항상 함께 이루어지므로, 상태 변경 없이 이벤트만 발행되거나 그 반대 상황이 발생하지 않습니다.

---

## 한눈에 보는 정리

### IDomainEvent 속성

각 속성이 분산 시스템에서 어떤 역할을 하는지 확인하세요.

| 속성 | 타입 | 용도 |
|------|------|------|
| `EventId` | Ulid | 이벤트 고유 식별 (멱등성) |
| `OccurredAt` | DateTimeOffset | 이벤트 발생 시각 |
| `CorrelationId` | string? | 요청 추적 |
| `CausationId` | string? | 인과 관계 추적 |

### 이벤트 생명주기

이벤트가 등록에서 정리까지 어떤 흐름을 거치는지 정리하면 다음과 같습니다.

| 단계 | 위치 | 메서드 |
|------|------|--------|
| 등록 | Aggregate Root 내부 | `AddDomainEvent()` |
| 조회 | 인프라 계층 | `DomainEvents` 속성 |
| 발행 | 인프라 계층 | Mediator/MediatR Publish |
| 정리 | 인프라 계층 | `ClearDomainEvents()` |

---

## FAQ

### Q1: 왜 이벤트를 즉시 발행하지 않고 수집하나요?
**A**: 트랜잭션이 커밋되기 전에 이벤트를 발행하면, 트랜잭션이 롤백되어도 이벤트는 이미 처리된 상태가 됩니다. 이벤트를 수집했다가 트랜잭션 커밋 후 발행하면 데이터 일관성을 보장할 수 있습니다.

### Q2: CorrelationId와 CausationId는 어떻게 사용하나요?
**A**: CorrelationId는 하나의 사용자 요청에서 발생한 모든 이벤트를 추적합니다. CausationId는 "이 이벤트가 어떤 이벤트 때문에 발생했는가"를 나타냅니다. 분산 시스템에서 이벤트 체인을 디버깅할 때 유용합니다.

### Q3: DomainEvent가 record인 이유는?
**A**: Domain Event는 **불변**이어야 합니다. 발생한 사실은 변경될 수 없기 때문입니다. C#의 `record`는 불변성과 값 기반 동등성을 기본 제공하므로 이벤트 모델링에 적합합니다.

### Q4: ClearDomainEvents()를 호출하지 않으면 어떻게 되나요?
**A**: 같은 Aggregate 인스턴스에서 이벤트가 계속 누적됩니다. 동일한 이벤트가 중복 발행될 수 있으므로, 인프라 계층에서 이벤트 발행 후 반드시 정리해야 합니다.

---

도메인 이벤트로 시스템 간 느슨한 결합을 만드는 방법을 배웠습니다. 그런데 "언제 생성됐는지", "누가 삭제했는지"를 매번 Entity마다 수동으로 구현해야 할까요? 다음 장에서는 엔티티 인터페이스를 통해 이런 공통 관심사를 선언적으로 표현하는 방법을 살펴봅니다.

→ [4장: 엔티티 인터페이스](../04-Entity-Interfaces/)
