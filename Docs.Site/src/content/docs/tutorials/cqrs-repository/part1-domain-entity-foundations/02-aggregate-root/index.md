---
title: "Aggregate Root"
---
## Overview

An Order contains OrderLines. What happens if external code directly deletes an OrderLine? The order total stays the same but the item is gone, and **data consistency is broken.**

An Aggregate Root is **the root Entity that defines the consistency boundary for related Entities and Value Objects**. External access can only modify internal state through the Aggregate Root, and the Aggregate Root protects business invariants. This chapter practices state transitions and invariant protection through an Order Aggregate.

---

## Learning Objectives

After completing this chapter, you will be able to:

1. **Explain** how `AggregateRoot<TId>` extends `Entity<TId>` to provide domain event management and consistency protection
2. **Implement** the pattern of rejecting disallowed transitions with `Fin<Unit>` during state transitions
3. **Design** safe state machines using enum-based states and method-based transitions

### What You Will Verify Through Hands-on Practice
- **Order**: Pending -> Confirmed -> Shipped -> Delivered state transitions
- **Fin<Unit>**: Functional result type expressing success/failure

---

## Core Concepts

### Why Is This Needed?

If internal Entities within an Aggregate are modified directly from outside, business rules break down. When the Aggregate Root serves as the sole entry point, this problem is prevented.

```
         External
           |
           v
    +-- Aggregate Root --+
    |   (Order)          |
    |    +-- OrderItem   |   <- Internal Entity
    |    +-- ShippingInfo|   <- Value Object
    +--------------------+
```

Let's look at the three responsibilities that the Aggregate Root guarantees:

- **Consistency boundary**: All changes within the Aggregate are processed as a single transaction
- **Invariant protection**: Rejects invalid state transitions
- **Entry point**: External code only calls Aggregate Root methods

### State Transitions with Fin<Unit>

Since state transitions can fail, they return `Fin<Unit>`. Instead of throwing exceptions, this approach makes the caller **explicitly** handle success and failure.

```csharp
public Fin<Unit> Confirm()
{
    if (Status != OrderStatus.Pending)
        return Error.New($"Can only confirm from Pending status. Current status: {Status}");

    Status = OrderStatus.Confirmed;
    return unit;
}
```

The caller branches on success/failure with `Match`:

```csharp
var result = order.Confirm();
result.Match(
    Succ: _ => Console.WriteLine("Confirmation successful"),
    Fail: err => Console.WriteLine($"Failed: {err.Message}"));
```

Since invalid state transitions are expressed as **values** rather than exceptions, the caller cannot ignore failures.

---

## Project Description

### Project Structure
```
AggregateRoot/
├── Program.cs               # State transition demo
├── OrderId.cs               # Order ID
├── OrderStatus.cs           # Order status enum
├── Order.cs                 # Order Aggregate Root
└── AggregateRoot.csproj

AggregateRoot.Tests.Unit/
├── OrderTests.cs            # State transition success/failure tests
├── Using.cs
├── xunit.runner.json
└── AggregateRoot.Tests.Unit.csproj
```

### Core Code

#### OrderStatus.cs

Defines the order lifecycle as an enum. Transition rules between states are protected by methods in the Order class.

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

Inherits `AggregateRoot<OrderId>` to form a consistency boundary. Note the pattern where each state transition method validates the current state and returns an `Error` if invalid.

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
            return Error.New($"Can only confirm from Pending status. Current status: {Status}");

        Status = OrderStatus.Confirmed;
        return unit;
    }

    // Ship(), Deliver(), Cancel() follow the same pattern...
}
```

Since the constructor is `private`, external code can only use the `Create()` factory method, and state changes must go through explicit methods like `Confirm()` and `Ship()`.

---

## Summary at a Glance

### Order State Transition Rules

Check at a glance which transitions are allowed from each state.

| Current State | Confirm | Ship | Deliver | Cancel |
|--------------|---------|------|---------|--------|
| Pending | O | X | X | O |
| Confirmed | X | O | X | O |
| Shipped | X | X | O | O |
| Delivered | X | X | X | X |
| Cancelled | X | X | X | X |

### Entity vs AggregateRoot

`AggregateRoot<TId>` extends `Entity<TId>`. Check the added responsibilities in the table below.

| Aspect | Entity<TId> | AggregateRoot<TId> |
|--------|-------------|-------------------|
| **ID-based equality** | O | O (inherited) |
| **Domain events** | X | O |
| **Consistency boundary** | X | O |
| **Repository target** | X | O |

---

## FAQ

### Q1: Why use Fin<Unit> instead of exceptions for state transitions?
**A**: Invalid state transitions are **business rule violations, not programming errors**. Exceptions are used for unexpected situations, while expected failures are explicitly returned with `Fin<T>`. This way, the caller gets a compile warning if they don't handle the failure.

### Q2: How are other Entities inside an AggregateRoot managed?
**A**: The Aggregate Root manages internal Entity collections as private, and external code can only modify them through the Aggregate Root's methods. Example: `order.AddItem(product, quantity)`. This chapter focuses on state transitions; internal Entity management is covered in Part 5.

### Q3: Why is Cancel allowed from multiple states?
**A**: Order cancellation is a business rule allowing cancellation at any time before delivery completion. It can be cancelled from Pending, Confirmed, and Shipped states, but not from Delivered and Cancelled states.

---

You've learned how to protect consistency boundaries with Aggregate Root. But how do you notify the payment system and inventory system when an order is confirmed? If the Aggregate calls them directly, tight coupling occurs. In the next chapter, we'll look at how to create loose coupling between systems through domain events.

-> [Chapter 3: Domain Events](../03-Domain-Events/)
