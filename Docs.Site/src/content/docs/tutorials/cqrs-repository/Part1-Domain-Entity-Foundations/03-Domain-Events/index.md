---
title: "도메인 이벤트"
---
## 개요

Domain Event는 **도메인에서 발생한 의미 있는 사건을 나타내는 불변 객체**입니다. Aggregate Root는 상태가 변경될 때 도메인 이벤트를 발행하고, 인프라 계층에서 이를 수집하여 다른 Aggregate나 외부 시스템에 전달합니다. 이 장에서는 `IDomainEvent`, `DomainEvent` record, 그리고 `AggregateRoot<TId>`의 이벤트 관리 메커니즘을 실습합니다.

---

## 학습 목표

### 핵심 학습 목표
1. **IDomainEvent 인터페이스** - EventId, OccurredAt, CorrelationId, CausationId의 역할
2. **DomainEvent record** - 불변 이벤트의 기본 구현과 편의 생성자
3. **AddDomainEvent / ClearDomainEvents** - Aggregate Root에서 이벤트를 등록하고 정리하는 패턴

### 실습을 통해 확인할 내용
- **OrderCreatedEvent**: 주문 생성 시 발행되는 도메인 이벤트
- **OrderConfirmedEvent**: 주문 확인 시 발행되는 도메인 이벤트
- **ClearDomainEvents()**: 이벤트 발행 후 정리하는 인프라 패턴

---

## 핵심 개념

### Domain Event의 구조

```csharp
public interface IDomainEvent : INotification
{
    DateTimeOffset OccurredAt { get; }   // 이벤트 발생 시각
    Ulid EventId { get; }               // 이벤트 고유 ID (멱등성 보장)
    string? CorrelationId { get; }       // 요청 추적 ID
    string? CausationId { get; }         // 원인 이벤트 ID
}
```

- **EventId**: 이벤트 중복 처리 방지 (멱등성)
- **CorrelationId**: 같은 요청에서 발생한 이벤트를 그룹핑
- **CausationId**: 이벤트 간의 인과 관계 추적

### DomainEvent 기본 record

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

인프라 계층(예: SaveChanges)에서 이벤트를 수집하고 발행한 후 `ClearDomainEvents()`를 호출합니다.

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
```csharp
public sealed record OrderCreatedEvent(
    OrderId OrderId,
    string CustomerName,
    decimal TotalAmount) : DomainEvent;
```

#### Order.cs (이벤트 발행 부분)
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

---

## 한눈에 보는 정리

### IDomainEvent 속성
| 속성 | 타입 | 용도 |
|------|------|------|
| `EventId` | Ulid | 이벤트 고유 식별 (멱등성) |
| `OccurredAt` | DateTimeOffset | 이벤트 발생 시각 |
| `CorrelationId` | string? | 요청 추적 |
| `CausationId` | string? | 인과 관계 추적 |

### 이벤트 생명주기
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
