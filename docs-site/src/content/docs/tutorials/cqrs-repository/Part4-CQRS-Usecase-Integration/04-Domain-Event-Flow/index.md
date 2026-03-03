---
title: "Part 4 - Chapter 17: Domain Event Flow"
---

> **Part 4: CQRS Usecase 통합** | [← 이전: 16장 FinT to FinResponse](../03-FinT-To-FinResponse/) | [다음: 18장 Transaction Pipeline →](../05-Transaction-Pipeline/)

---

## 개요

Domain Event는 Aggregate 내에서 발생한 비즈니스 사실을 나타냅니다. Aggregate가 상태를 변경할 때 이벤트를 생성하고, Repository가 Aggregate를 추적하며, SaveChanges 이후 이벤트가 발행됩니다. 이 장에서는 이벤트의 생성부터 수집, 발행, 정리까지의 전체 흐름을 학습합니다.

---

## 학습 목표

- **AggregateRoot.AddDomainEvent()**: Aggregate 내부에서 이벤트를 생성하는 방법
- **IDomainEventCollector**: Repository가 Aggregate를 추적하는 메커니즘
- **이벤트 라이프사이클**: 생성 -> 추적 -> 발행 -> 정리
- **DomainEvent 기본 속성**: EventId, OccurredAt, CorrelationId

---

## 핵심 개념

### Domain Event 라이프사이클

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

```csharp
public static Product Create(string name, decimal price)
{
    var product = new Product(ProductId.New(), name, price);
    product.AddDomainEvent(new ProductCreatedEvent(
        product.Id.ToString(), product.Name, product.Price));
    return product;
}
```

`AddDomainEvent()`는 `AggregateRoot<TId>`의 protected 메서드입니다. 이벤트는 Aggregate 내부에서만 생성할 수 있습니다.

### IDomainEventCollector

```csharp
public interface IDomainEventCollector
{
    void Track(IHasDomainEvents aggregate);
    void TrackRange(IEnumerable<IHasDomainEvents> aggregates);
    IReadOnlyList<IHasDomainEvents> GetTrackedAggregates();
}
```

Collector는 Scoped 라이프타임으로 등록되며, 하나의 요청 안에서 모든 Repository가 같은 Collector를 공유합니다.

---

## 프로젝트 설명

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
