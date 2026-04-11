---
title: "Domain Event Flow"
---
## Overview

How do you propagate events externally while maintaining domain layer purity? When an order is cancelled, inventory must be restored and payment refunded. But if the Aggregate directly calls other services, the domain layer becomes dependent on infrastructure. This chapter builds the complete flow of creating events inside the Aggregate, tracking through Repository, and publishing after SaveChanges.

---

## Learning Objectives

After completing this chapter, you will be able to:

1. Create events inside the Aggregate with **AggregateRoot.AddDomainEvent()**
2. Explain the mechanism by which Repository tracks Aggregates with **IDomainEventCollector**
3. Implement each stage of the **event lifecycle** (creation -> tracking -> publishing -> cleanup)
4. Explain the roles of **DomainEvent base properties** (EventId, OccurredAt, CorrelationId)

---

## Core Concepts

### Domain Event Lifecycle

Events are created inside the Aggregate, collected through the Repository, and published after transaction commit. Examine each stage in order.

```
1. Aggregate.Create() / UpdatePrice()
   └── AddDomainEvent(new XxxEvent(...))

2. Repository.Create(aggregate)
   └── eventCollector.Track(aggregate)

3. After SaveChanges completes
   └── eventPublisher.PublishTrackedEvents()
       ├── Iterate aggregate.DomainEvents
       ├── Mediator.Publish(event)
       └── aggregate.ClearDomainEvents()
```

### Event Generation in Aggregate

When the Aggregate's state changes, that fact is recorded as an event. `AddDomainEvent()` is a protected method of `AggregateRoot<TId>`, so events can only be created inside the Aggregate.

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

When Repository saves an Aggregate, it registers that Aggregate with the Collector. The Collector is registered with Scoped lifetime, so all Repositories within a single request share the same Collector.

```csharp
public interface IDomainEventCollector
{
    void Track(IHasDomainEvents aggregate);
    void TrackRange(IEnumerable<IHasDomainEvents> aggregates);
    IReadOnlyList<IHasDomainEvents> GetTrackedAggregates();
}
```

---

## Project Description

You can verify the flow from event creation to collection in the files below.

| File | Description |
|------|-------------|
| `ProductId.cs` | Ulid-based Product identifier |
| `Product.cs` | AggregateRoot + product entity that generates events |
| `ProductCreatedEvent.cs` | Product creation domain event |
| `ProductPriceChangedEvent.cs` | Price change domain event |
| `SimpleDomainEventCollector.cs` | IDomainEventCollector implementation |
| `Program.cs` | Event flow demo |

---

## Summary at a Glance

A summary of the core components of domain event flow.

| Concept | Description |
|---------|-------------|
| `DomainEvent` | Base domain event record (includes EventId, OccurredAt) |
| `AddDomainEvent()` | AggregateRoot's protected method for event registration |
| `ClearDomainEvents()` | Cleanup after event publishing |
| `IDomainEventCollector` | Collector for Repository to track Aggregates |
| `IHasDomainEvents` | Read-only marker for Aggregates with events |

---

## FAQ

### Q1: When are events published?
**A**: In the UsecaseTransactionPipeline, after SaveChanges succeeds and the transaction is committed, `IDomainEventPublisher.PublishTrackedEvents()` is called to publish events.

### Q2: Who calls ClearDomainEvents()?
**A**: `DomainEventPublisher` automatically calls it after publishing events. There's no need to call it directly from the Usecase.

### Q3: What happens if the same Aggregate is tracked multiple times?
**A**: `ReferenceEqualityComparer` is used, so the same instance is tracked only once. Duplicate Track calls are ignored.

---

We've built the domain event collection and publishing flow. But what if every Usecase has to repeat SaveChanges and event publishing? In the next chapter, we'll look at automating these cross-cutting concerns with a transaction pipeline.

-> [Chapter 5: Transaction Pipeline](../05-Transaction-Pipeline/)
