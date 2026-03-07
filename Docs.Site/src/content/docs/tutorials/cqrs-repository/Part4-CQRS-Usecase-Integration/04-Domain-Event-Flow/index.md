---
title: "도메인 이벤트 흐름"
---
## 개요

도메인 계층의 순수성을 유지하면서 이벤트를 외부에 전파하려면 어떻게 해야 할까요? 주문이 취소되면 재고를 복원하고 결제를 환불해야 합니다. 하지만 Aggregate가 직접 다른 서비스를 호출하면 도메인 계층이 인프라에 의존하게 됩니다. 이 장에서는 Aggregate 내부에서 이벤트를 생성하고, Repository가 추적하며, SaveChanges 이후에 발행하는 전체 흐름을 만들어봅시다.

---

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. **AggregateRoot.AddDomainEvent()로** Aggregate 내부에서 이벤트를 생성할 수 있습니다
2. **IDomainEventCollector로** Repository가 Aggregate를 추적하는 메커니즘을 설명할 수 있습니다
3. **이벤트 라이프사이클** (생성 → 추적 → 발행 → 정리)의 각 단계를 구현할 수 있습니다
4. **DomainEvent 기본 속성** (EventId, OccurredAt, CorrelationId)의 역할을 설명할 수 있습니다

---

## 핵심 개념

### Domain Event 라이프사이클

이벤트는 Aggregate 내부에서 생성되어, Repository를 거쳐 수집되고, 트랜잭션 커밋 후에 발행됩니다. 각 단계를 순서대로 살펴보세요.

```
1. Aggregate.Create() / UpdatePrice()
   └── AddDomainEvent(new XxxEvent(...))

2. Repository.Create(aggregate)
   └── eventCollector.Track(aggregate)

3. SaveChanges 완료 후
   └── eventPublisher.PublishTrackedEvents()
       ├── aggregate.DomainEvents 순회
       ├── Mediator.Publish(event)
       └── aggregate.ClearDomainEvents()
```

### Aggregate에서 이벤트 발생

Aggregate의 상태가 변경될 때, 그 사실을 이벤트로 기록합니다. `AddDomainEvent()`는 `AggregateRoot<TId>`의 protected 메서드이므로 이벤트는 Aggregate 내부에서만 생성할 수 있습니다.

```csharp
public static Product Create(string name, decimal price)
{
    var product = new Product(ProductId.New(), name, price);
    product.AddDomainEvent(new ProductCreatedEvent(
        product.Id.ToString(), product.Name, product.Price));
    return product;
}
```

### IDomainEventCollector

Repository가 Aggregate를 저장할 때, Collector에 해당 Aggregate를 등록합니다. Collector는 Scoped 라이프타임으로 등록되므로, 하나의 요청 안에서 모든 Repository가 같은 Collector를 공유합니다.

```csharp
public interface IDomainEventCollector
{
    void Track(IHasDomainEvents aggregate);
    void TrackRange(IEnumerable<IHasDomainEvents> aggregates);
    IReadOnlyList<IHasDomainEvents> GetTrackedAggregates();
}
```

---

## 프로젝트 설명

아래 파일들에서 이벤트의 생성부터 수집까지의 흐름을 확인할 수 있습니다.

| 파일 | 설명 |
|------|------|
| `ProductId.cs` | Ulid 기반 Product 식별자 |
| `Product.cs` | AggregateRoot + 이벤트를 발생시키는 상품 엔티티 |
| `ProductCreatedEvent.cs` | 상품 생성 도메인 이벤트 |
| `ProductPriceChangedEvent.cs` | 가격 변경 도메인 이벤트 |
| `SimpleDomainEventCollector.cs` | IDomainEventCollector 구현 |
| `Program.cs` | 이벤트 흐름 데모 |

---

## 한눈에 보는 정리

도메인 이벤트 흐름의 핵심 구성 요소를 정리합니다.

| 개념 | 설명 |
|------|------|
| `DomainEvent` | 도메인 이벤트 기본 record (EventId, OccurredAt 포함) |
| `AddDomainEvent()` | AggregateRoot의 protected 메서드로 이벤트 등록 |
| `ClearDomainEvents()` | 이벤트 발행 후 정리 |
| `IDomainEventCollector` | Repository가 Aggregate를 추적하는 수집기 |
| `IHasDomainEvents` | 이벤트를 가진 Aggregate의 읽기 전용 마커 |

---

## FAQ

**Q: 이벤트는 언제 발행되나요?**
A: UsecaseTransactionPipeline에서 SaveChanges 성공 후, 트랜잭션 커밋 후에 `IDomainEventPublisher.PublishTrackedEvents()`가 호출되어 이벤트가 발행됩니다.

**Q: ClearDomainEvents()는 누가 호출하나요?**
A: `DomainEventPublisher`가 이벤트 발행 후 자동으로 호출합니다. Usecase에서 직접 호출할 필요가 없습니다.

**Q: 같은 Aggregate를 여러 번 Track하면 어떻게 되나요?**
A: `ReferenceEqualityComparer`를 사용하므로 동일 인스턴스는 한 번만 추적됩니다. 중복 Track은 무시됩니다.

---

도메인 이벤트 수집과 발행 흐름을 만들었습니다. 그런데 모든 Usecase에서 SaveChanges와 이벤트 발행을 반복해야 한다면? 다음 장에서는 트랜잭션 파이프라인으로 이 횡단 관심사를 자동화하는 방법을 살펴봅니다.
