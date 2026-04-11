---
title: "E-commerce Order Management"
---
## Overview

So far, we've learned Entity, Aggregate Root, domain events, and Repository individually. What does it look like when these patterns are **integrated into one** in a real order domain?

This chapter implements the Command side of the CQRS pattern as a complete example through the E-commerce order domain. It comprehensively covers Order Aggregate Root, OrderLine child Entity, state transition rules, domain events, and Repository pattern in a single example.

---

## Learning Objectives

After completing this chapter, you will be able to:

1. Design **Aggregate Root + child Entity** structures
2. Apply **state transition** rules and the `Fin<Unit>` return pattern
3. Implement **domain event** publishing and collection
4. Implement **InMemoryRepository**-based CRUD
5. Validate business rules in **factory methods**

---

## Core Concepts

### Order State Transition Diagram

An order follows a clear state flow from creation to delivery. When business rules are violated at each transition, `Fin<Unit>` returns an error.

```
Pending --> Confirmed --> Shipped --> Delivered
  |             |
  +--> Cancelled <--+
```

- `Confirm()`: Pending -> Confirmed
- `Ship()`: Confirmed -> Shipped
- `Deliver()`: Shipped -> Delivered
- `Cancel()`: Pending or Confirmed -> Cancelled (not from Delivered)

### Aggregate Root and Child Entity

Order is the Aggregate Root, OrderLine is the child Entity. External access to OrderLine is only possible through Order.

```csharp
// Order (Aggregate Root) -> OrderLine (child Entity)
var order = Order.Create("John Doe", orderLines).ThrowIfFail();
order.Confirm();      // Returns Fin<Unit>
order.Ship();         // Returns Error on state transition failure
```

### Domain Event Flow

Each state transition publishes a corresponding domain event. Other systems like payment, inventory, and notifications subscribe to these events to react.

```csharp
Order.Create(...)     -> OrderCreatedEvent
order.Confirm()       -> OrderConfirmedEvent
order.Ship()          -> OrderShippedEvent
order.Deliver()       -> OrderDeliveredEvent
order.Cancel()        -> OrderCancelledEvent
```

---

## Project Description

### File Structure

Check each file's role in the CQRS architecture.

| File | Role |
|------|------|
| `OrderId.cs` | Ulid-based order identifier |
| `OrderLineId.cs` | Ulid-based order line identifier |
| `OrderStatus.cs` | Order status enumeration |
| `OrderLine.cs` | Order line child Entity |
| `Order.cs` | Order Aggregate Root (state transitions + domain events) |
| `OrderDto.cs` | Query-side DTO |
| `IOrderRepository.cs` | Repository interface |
| `InMemoryOrderRepository.cs` | InMemory implementation |

### Order Aggregate Design Points

1. **Factory validation**: `Create()` validates empty customer name and empty order lines with `Fin<Order>`
2. **Auto amount calculation**: `TotalAmount` is calculated from the sum of OrderLines' `LineTotal`
3. **Invariant protection**: State transition methods return `Fin<Unit>` for explicit failure handling
4. **Event tracking**: `AddDomainEvent()` called at each state transition

---

## Summary at a Glance

A summary of the CQRS pattern elements used in this example.

| Concept | Implementation |
|---------|---------------|
| Aggregate Root | `Order : AggregateRoot<OrderId>` |
| Child Entity | `OrderLine : Entity<OrderLineId>` |
| State transitions | `Confirm()`, `Ship()`, `Deliver()`, `Cancel()` -> `Fin<Unit>` |
| Domain events | `OrderCreatedEvent`, `OrderConfirmedEvent`, etc. |
| Repository | `IOrderRepository : IRepository<Order, OrderId>` |
| InMemory implementation | `InMemoryOrderRepository : InMemoryRepositoryBase<Order, OrderId>` |

---

## FAQ

### Q1: Why not make OrderLine a separate Aggregate?
**A**: OrderLine is a child Entity that has no meaning without Order. Aggregate boundaries are determined by "units that must change together," and OrderLine is always created/changed together with Order.

### Q2: Can't TotalAmount be cached instead of calculated each time?
**A**: In this example, it's calculated and stored at creation time. In practice, recalculation logic is needed when OrderLines change, which is a good example of Aggregate Root protecting invariants.

### Q3: Why can't Cancel() be called from Shipped state?
**A**: This example only allows cancellation up to Confirmed. In practice, cancellation from Shipped state may be possible through a return process, handled as a separate domain event (ReturnRequestedEvent).

---

Order management CQRS is complete. Next is customer management. You shouldn't be able to register duplicate customers with the same email -- how do you implement email duplicate checking? The next chapter solves this problem with the Specification pattern.

-> [Chapter 2: Customer Management](../02-Customer-Management/)
