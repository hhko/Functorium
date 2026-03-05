---
title: "애그리거트 루트"
---
## 개요

Aggregate Root는 **관련된 Entity와 Value Object의 일관성 경계(Consistency Boundary)를 정의하는 루트 Entity**입니다. 외부에서는 반드시 Aggregate Root를 통해서만 내부 상태를 변경할 수 있으며, Aggregate Root가 비즈니스 불변 규칙(Invariant)을 보호합니다. 이 장에서는 주문(Order) Aggregate를 통해 상태 전이와 불변 규칙 보호를 실습합니다.

---

## 학습 목표

### 핵심 학습 목표
1. **AggregateRoot<TId>의 역할** - Entity<TId>를 확장하여 도메인 이벤트 관리와 일관성 보호를 제공
2. **비즈니스 불변 규칙 보호** - 상태 전이 시 허용되지 않는 전이를 `Fin<Unit>`으로 거부
3. **상태 머신 패턴** - enum 기반 상태와 메서드 기반 전이로 안전한 상태 관리

### 실습을 통해 확인할 내용
- **Order**: Pending -> Confirmed -> Shipped -> Delivered 상태 전이
- **Fin<Unit>**: 성공/실패를 표현하는 함수형 결과 타입

---

## 핵심 개념

### Aggregate Root의 책임

```
         외부
           │
           ▼
    ┌─ Aggregate Root ─┐
    │   (Order)         │
    │    ├─ OrderItem   │   ← 내부 Entity
    │    └─ ShippingInfo│   ← Value Object
    └───────────────────┘
```

- **일관성 경계**: Aggregate 내부의 모든 변경은 하나의 트랜잭션으로 처리
- **불변 규칙 보호**: 잘못된 상태 전이를 거부
- **진입점**: 외부에서는 Aggregate Root의 메서드만 호출

### Fin<Unit>을 활용한 상태 전이

상태 전이가 실패할 수 있으므로 `Fin<Unit>`을 반환합니다:

```csharp
public Fin<Unit> Confirm()
{
    if (Status != OrderStatus.Pending)
        return Error.New($"Pending 상태에서만 확인할 수 있습니다. 현재 상태: {Status}");

    Status = OrderStatus.Confirmed;
    return unit;
}
```

호출자는 성공/실패를 명시적으로 처리합니다:

```csharp
var result = order.Confirm();
result.Match(
    Succ: _ => Console.WriteLine("확인 성공"),
    Fail: err => Console.WriteLine($"실패: {err.Message}"));
```

---

## 프로젝트 설명

### 프로젝트 구조
```
AggregateRoot/
├── Program.cs               # 상태 전이 데모
├── OrderId.cs               # 주문 ID
├── OrderStatus.cs           # 주문 상태 enum
├── Order.cs                 # 주문 Aggregate Root
└── AggregateRoot.csproj

AggregateRoot.Tests.Unit/
├── OrderTests.cs            # 상태 전이 성공/실패 테스트
├── Using.cs
├── xunit.runner.json
└── AggregateRoot.Tests.Unit.csproj
```

### 핵심 코드

#### OrderStatus.cs
```csharp
public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}
```

#### Order.cs
```csharp
public sealed class Order : AggregateRoot<OrderId>
{
    public string CustomerName { get; private set; }
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }

    private Order(OrderId id, string customerName, decimal totalAmount)
    {
        Id = id;
        CustomerName = customerName;
        TotalAmount = totalAmount;
        Status = OrderStatus.Pending;
    }

    public static Order Create(string customerName, decimal totalAmount)
    {
        return new Order(OrderId.New(), customerName, totalAmount);
    }

    public Fin<Unit> Confirm()
    {
        if (Status != OrderStatus.Pending)
            return Error.New($"Pending 상태에서만 확인할 수 있습니다. 현재 상태: {Status}");

        Status = OrderStatus.Confirmed;
        return unit;
    }

    // Ship(), Deliver(), Cancel() 동일한 패턴...
}
```

---

## 한눈에 보는 정리

### 주문 상태 전이 규칙
| 현재 상태 | Confirm | Ship | Deliver | Cancel |
|-----------|---------|------|---------|--------|
| Pending | O | X | X | O |
| Confirmed | X | O | X | O |
| Shipped | X | X | O | O |
| Delivered | X | X | X | X |
| Cancelled | X | X | X | X |

### Entity vs AggregateRoot
| 구분 | Entity<TId> | AggregateRoot<TId> |
|------|-------------|-------------------|
| **ID 기반 동등성** | O | O (상속) |
| **도메인 이벤트** | X | O |
| **일관성 경계** | X | O |
| **Repository 대상** | X | O |

---

## FAQ

### Q1: 왜 상태 전이에서 예외 대신 Fin<Unit>을 사용하나요?
**A**: 잘못된 상태 전이는 **프로그래밍 오류가 아닌 비즈니스 규칙 위반**입니다. 예외는 예상치 못한 상황에 사용하고, 예상 가능한 실패는 `Fin<T>`으로 명시적으로 반환합니다. 이렇게 하면 호출자가 실패를 처리하지 않으면 컴파일 경고가 발생합니다.

### Q2: AggregateRoot 내부의 다른 Entity는 어떻게 관리하나요?
**A**: Aggregate Root가 내부 Entity 컬렉션을 private으로 관리하고, 외부에서는 Aggregate Root의 메서드를 통해서만 변경합니다. 예: `order.AddItem(product, quantity)`. 이 장에서는 상태 전이에 집중하고, 내부 Entity 관리는 Part 5에서 다룹니다.

### Q3: Cancel은 왜 여러 상태에서 허용되나요?
**A**: 주문 취소는 배달 완료 전까지는 언제든 가능한 비즈니스 규칙입니다. Pending, Confirmed, Shipped 상태에서 모두 취소할 수 있지만, Delivered와 Cancelled 상태에서는 불가합니다.
