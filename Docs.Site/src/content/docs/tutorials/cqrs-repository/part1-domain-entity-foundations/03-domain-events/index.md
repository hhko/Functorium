---
title: "Domain Events"
---
## Overview

When an order is confirmed, the payment system needs to be notified and inventory needs to be deducted. **What happens if the Aggregate Root directly calls these systems?** The order domain becomes tightly coupled to payment and inventory, making changes difficult.

A Domain Event is **an immutable object representing a meaningful occurrence in the domain**. The Aggregate Root publishes domain events when state changes, and the infrastructure layer collects them for delivery to other Aggregates or external systems. This chapter practices `IDomainEvent`, `DomainEvent` record, and the event management mechanism of `AggregateRoot<TId>`.

---

## Learning Objectives

After completing this chapter, you will be able to:

1. **Explain** the role of each property in `IDomainEvent`: EventId, OccurredAt, CorrelationId, CausationId
2. **Define** immutable events by inheriting from the `DomainEvent` record
3. **Apply** the pattern of registering events with `AddDomainEvent()` and cleaning up with `ClearDomainEvents()`

### What You Will Verify Through Hands-on Practice
- **OrderCreatedEvent**: A domain event published when an order is created
- **OrderConfirmedEvent**: A domain event published when an order is confirmed
- **ClearDomainEvents()**: Infrastructure pattern for cleaning up events after publishing

---

## Core Concepts

### Why Is This Needed?

When an Aggregate directly calls other systems, coupling increases. Instead, by **publishing the fact "the order was confirmed" as an event**, interested systems subscribe and handle it themselves. The publisher doesn't need to know about subscribers, creating loose coupling.

### Domain Event Structure

What information does an event need to be properly tracked? Look at the properties defined by `IDomainEvent`.

```csharp
public interface IDomainEvent : INotification
{
    DateTimeOffset OccurredAt { get; }   // When the event occurred
    Ulid EventId { get; }               // Unique event ID (idempotency guarantee)
    string? CorrelationId { get; }       // Request tracking ID
    string? CausationId { get; }         // Causing event ID
}
```

A summary of each property's role:

- **EventId**: Prevents duplicate event processing (idempotency)
- **CorrelationId**: Groups events from the same request
- **CausationId**: Tracks causal relationships between events

### DomainEvent Base Record

It would be tedious if every event had to define these properties each time, right? The `DomainEvent` base record provides a convenience constructor.

```csharp
public abstract record DomainEvent(...) : IDomainEvent
{
    protected DomainEvent() : this(DateTimeOffset.UtcNow, Ulid.NewUlid(), null, null) { }
}
```

Derived events are defined simply using the parameterless constructor:

```csharp
public sealed record OrderCreatedEvent(
    OrderId OrderId,
    string CustomerName,
    decimal TotalAmount) : DomainEvent;
```

Just declare the business-relevant data, and EventId and OccurredAt are populated automatically.

### Event Publishing Pattern

Inside the Aggregate Root, call `AddDomainEvent()` when state changes:

```csharp
public static Order Create(string customerName, decimal totalAmount)
{
    var order = new Order(OrderId.New(), customerName, totalAmount);
    order.AddDomainEvent(new OrderCreatedEvent(order.Id, customerName, totalAmount));
    return order;
}
```

The infrastructure layer (e.g., SaveChanges) collects and publishes events, then calls `ClearDomainEvents()`. This ensures events are only delivered after transaction commit, guaranteeing data consistency.

---

## Project Description

### Project Structure
```
DomainEvents/
├── Program.cs                  # Event publishing demo
├── OrderId.cs                  # Order ID
├── OrderCreatedEvent.cs        # Order created event
├── OrderConfirmedEvent.cs      # Order confirmed event
├── Order.cs                    # Aggregate Root that publishes events
└── DomainEvents.csproj

DomainEvents.Tests.Unit/
├── OrderDomainEventTests.cs    # Event publishing/cleanup tests
├── Using.cs
├── xunit.runner.json
└── DomainEvents.Tests.Unit.csproj
```

### Core Code

#### OrderCreatedEvent.cs

An event published when an order is created. Since it inherits from `DomainEvent`, EventId, OccurredAt, etc. are automatically set.

```csharp
public sealed record OrderCreatedEvent(
    OrderId OrderId,
    string CustomerName,
    decimal TotalAmount) : DomainEvent;
```

#### Order.cs (Event Publishing Section)

Each state change registers the corresponding domain event. Notice how `Create()` publishes an `OrderCreatedEvent` and `Confirm()` publishes an `OrderConfirmedEvent`.

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

State transitions and event publishing always occur together, so there is never a situation where state changes without an event or vice versa.

---

## Summary at a Glance

### IDomainEvent Properties

Check the role of each property in distributed systems.

| Property | Type | Purpose |
|----------|------|---------|
| `EventId` | Ulid | Unique event identification (idempotency) |
| `OccurredAt` | DateTimeOffset | Event occurrence time |
| `CorrelationId` | string? | Request tracking |
| `CausationId` | string? | Causal relationship tracking |

### Event Lifecycle

A summary of the flow from event registration to cleanup.

| Stage | Location | Method |
|-------|----------|--------|
| Registration | Inside Aggregate Root | `AddDomainEvent()` |
| Retrieval | Infrastructure layer | `DomainEvents` property |
| Publishing | Infrastructure layer | Mediator/MediatR Publish |
| Cleanup | Infrastructure layer | `ClearDomainEvents()` |

---

## FAQ

### Q1: Why collect events instead of publishing immediately?
**A**: If events are published before the transaction commits, the events are already processed even if the transaction rolls back. By collecting events and publishing after transaction commit, data consistency is guaranteed.

### Q2: How are CorrelationId and CausationId used?
**A**: CorrelationId tracks all events from a single user request. CausationId indicates "what event caused this event." They are useful for debugging event chains in distributed systems.

### Q3: Why is DomainEvent a record?
**A**: Domain Events must be **immutable**. A fact that has occurred cannot be changed. C#'s `record` provides immutability and value-based equality by default, making it suitable for event modeling.

### Q4: What happens if ClearDomainEvents() is not called?
**A**: Events keep accumulating on the same Aggregate instance. The same events may be published multiple times, so the infrastructure layer must clean up after event publishing.

---

You've learned how to create loose coupling between systems through domain events. But should you manually implement "when was it created" and "who deleted it" for every Entity? In the next chapter, we'll look at how to declaratively express these common concerns through Entity interfaces.

-> [Chapter 4: Entity Interfaces](../04-Entity-Interfaces/)
