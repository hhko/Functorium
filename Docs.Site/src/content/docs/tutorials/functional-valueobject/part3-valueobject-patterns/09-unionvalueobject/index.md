---
title: "UnionValueObject"
---

## Overview

What if `OrderStatus` must be **exactly one** of `Pending`, `Confirmed`, `Shipped`, `Delivered`? What if `Shape` must be one of `Circle`, `Rectangle`, `Triangle`? Regular inheritance is an "open hierarchy" where new subtypes can be added at any time. A Discriminated Union is a "closed hierarchy" where **all cases are known at compile time** and pattern matching can handle them exhaustively.

This chapter implements `UnionValueObject` -- a Discriminated Union value object based on abstract records.

> **"When a value object must be exactly one of several variants, an abstract record hierarchy with Match/Switch guarantees type-safe branching."**

## Learning Objectives

1. **`UnionValueObject`** -- Discriminated Union pattern based on abstract records
2. **`IUnionValueObject`** -- Marker interface extending IValueObject
3. **Match/Switch** -- Manual implementation for understanding principles + `[UnionType]` source generator introduction
4. **`UnreachableCaseException`** -- Safety guard for unreachable default cases
5. **`UnionValueObject<TSelf>`** -- CRTP pattern supporting state transitions via `TransitionFrom`

### What you will verify through practice
- **Shape**: Circle | Rectangle | Triangle -- area and perimeter calculation
- **PaymentMethod**: CreditCard | BankTransfer | Cash -- fee calculation
- **OrderStatus**: Pending -> Confirmed -- Functorium `UnionValueObject<TSelf>` state transition

## Core Type Structure

```
IUnionValueObject (marker interface)
    ├── UnionValueObject (abstract record) — pure data union
    │       ├── Shape (abstract record)
    │       │   ├── Circle(Radius)       — sealed record
    │       │   ├── Rectangle(W, H)      — sealed record
    │       │   └── Triangle(Base, H)    — sealed record
    │       └── PaymentMethod (abstract record)
    │           ├── CreditCard(CardNo, Expiry) — sealed record
    │           ├── BankTransfer(AccNo, Bank)  — sealed record
    │           └── Cash()                     — sealed record
    └── UnionValueObject<TSelf> (abstract record) — state transition union
            └── OrderStatus (abstract record)
                ├── Pending(OrderId)              — sealed record
                └── Confirmed(OrderId, ConfirmedAt) — sealed record
```

## Match and Switch Patterns

### Match -- Branching That Returns a Value

Provides a transformation function for every case and returns a result:

```csharp
public TResult Match<TResult>(
    Func<Circle, TResult> circle,
    Func<Rectangle, TResult> rectangle,
    Func<Triangle, TResult> triangle) => this switch
{
    Circle c => circle(c),
    Rectangle r => rectangle(r),
    Triangle t => triangle(t),
    _ => throw new UnreachableCaseException(this)
};

// Usage
double area = shape.Match(
    circle: c => Math.PI * c.Radius * c.Radius,
    rectangle: r => r.Width * r.Height,
    triangle: t => 0.5 * t.Base * t.Height);
```

### Switch -- Branching for Side Effects

Executes an action for each case without a return value:

```csharp
shape.Switch(
    circle: c => Console.WriteLine($"Circle: radius={c.Radius}"),
    rectangle: r => Console.WriteLine($"Rectangle: {r.Width}x{r.Height}"),
    triangle: t => Console.WriteLine($"Triangle: base={t.Base}"));
```

### UnreachableCaseException

A safety guard for the `default` branch. Since only sealed records are used, it is theoretically unreachable, but it is specified for runtime safety:

```csharp
_ => throw new UnreachableCaseException(this)
// Message in the form: "Unreachable case: Shape+Circle"
```

## Domain Logic Examples

### Shape -- Area and Perimeter

```csharp
public abstract record Shape : UnionValueObject
{
    public sealed record Circle(double Radius) : Shape;
    public sealed record Rectangle(double Width, double Height) : Shape;
    public sealed record Triangle(double Base, double Height) : Shape;

    public double Area => Match(
        circle: c => Math.PI * c.Radius * c.Radius,
        rectangle: r => r.Width * r.Height,
        triangle: t => 0.5 * t.Base * t.Height);
}
```

### PaymentMethod -- Fee Calculation

```csharp
public abstract record PaymentMethod : UnionValueObject
{
    public sealed record CreditCard(string CardNumber, string ExpiryDate) : PaymentMethod;
    public sealed record BankTransfer(string AccountNumber, string BankCode) : PaymentMethod;
    public sealed record Cash() : PaymentMethod;

    public decimal CalculateFee(decimal amount) => Match(
        creditCard: _ => amount * 0.03m,
        bankTransfer: _ => 1000m,
        cash: _ => 0m);
}
```

## Record-Based Equality

`UnionValueObject` is an abstract record, so **value-based equality** is automatically guaranteed:

```csharp
Shape a = new Shape.Circle(5.0);
Shape b = new Shape.Circle(5.0);
a == b  // true — same shape if same Radius

Shape c = new Shape.Rectangle(5.0, 5.0);
a == c  // false — different cases are different values
```

## Functorium's Source Generator

In this tutorial, Match/Switch was manually implemented, but in Functorium the `[UnionType]` attribute causes the **source generator to generate them automatically**:

```csharp
// When using the Functorium framework
[UnionType]
public abstract partial record Shape : UnionValueObject
{
    public sealed record Circle(double Radius) : Shape;
    public sealed record Rectangle(double Width, double Height) : Shape;
    public sealed record Triangle(double Base, double Height) : Shape;
    // Match and Switch methods are automatically generated
}
```

`[UnionType]` analyzes the inner sealed record cases and generates type-safe Match/Switch methods.

## Union with State Transitions

The Shape and PaymentMethod so far are **pure data unions**. There is no concept of transitioning between cases, and computation is performed based on the current value only.

However, for cases like `OrderStatus` where state transitions like **Pending -> Confirmed -> Shipped** are needed, invalid transitions (e.g., Confirmed back to Confirmed) must be prevented. Functorium's `UnionValueObject<TSelf>` provides the `TransitionFrom` helper to resolve this problem.

### UnionValueObject&lt;TSelf&gt;

It uses CRTP (Curiously Recurring Template Pattern) to pass precise type information to `DomainError`:

```csharp
using Functorium.Domains.ValueObjects.Unions;
using LanguageExt;

public abstract record OrderStatus : UnionValueObject<OrderStatus>
{
    public sealed record Pending(string OrderId) : OrderStatus;
    public sealed record Confirmed(string OrderId, DateTime ConfirmedAt) : OrderStatus;
    private OrderStatus() { }

    public Fin<Confirmed> Confirm(DateTime confirmedAt) =>
        TransitionFrom<Pending, Confirmed>(
            p => new Confirmed(p.OrderId, confirmedAt));
}
```

### TransitionFrom Behavior

`TransitionFrom<TSource, TTarget>` works as follows:

1. If `this` is of type `TSource` -> applies the transition function and returns `TTarget` (success)
2. If `this` is not of type `TSource` -> returns `InvalidTransition` error (failure)

```csharp
// Success: Pending -> Confirmed
OrderStatus order = new OrderStatus.Pending("ORD-001");
var result = order.Confirm(DateTime.Now);
// result.IsSucc == true

// Failure: Confirmed -> Confirmed (invalid transition)
OrderStatus confirmed = new OrderStatus.Confirmed("ORD-001", DateTime.Now);
var fail = confirmed.Confirm(DateTime.Now);
// fail.IsFail == true, error message: "Invalid transition from Confirmed to Confirmed"
```

### UnionValueObject vs UnionValueObject&lt;TSelf&gt; Selection Criteria

| Criterion | `UnionValueObject` | `UnionValueObject<TSelf>` |
|------|:-------------------:|:-------------------------:|
| Pure data variants | Yes | Yes |
| State transitions | No | Yes (`TransitionFrom`) |
| DomainError type info | No | Yes (CRTP) |
| Use examples | Shape, PaymentMethod | OrderStatus, PaymentState |

## Summary at a Glance

| Component | Role |
|-----------|------|
| `UnionValueObject` | abstract record base class -- root of DU |
| `IUnionValueObject` | Marker interface -- extends IValueObject, for architecture test filtering |
| `Match<TResult>` | Handles all cases and returns a value |
| `Switch` | Handles all cases and executes side effects |
| `UnreachableCaseException` | Safety guard for default branch |
| `[UnionType]` | Source generator trigger (auto-generates Match/Switch) |
| `UnionValueObject<TSelf>` | Supports state transitions -- provides `TransitionFrom` helper |

### Position in Value Object Type Selection

| Condition | Selection |
|------|------|
| Single value, no comparison needed | `SimpleValueObject<T>` |
| Single value, comparison needed | `ComparableSimpleValueObject<T>` |
| Composite value | `ValueObject` / `ComparableValueObject` |
| Restricted enumeration + behavior | `SmartEnum + IValueObject` |
| **Exactly one of several variants** | **`UnionValueObject`** |
| **Variant requiring state transitions** | **`UnionValueObject<TSelf>`** |

## FAQ

### Q1: What is the difference between regular inheritance and UnionValueObject?
**A**: Regular inheritance is an "open hierarchy" where anyone can add new subtypes. UnionValueObject closes the cases with sealed records so that **all cases are known at compile time**, and Match/Switch enforces exhaustive handling.

### Q2: Can it be implemented with a class instead of a record?
**A**: It is possible, but using records automatically provides value-based equality, `ToString()`, and deconstruction. The core property of value objects -- "objects with the same value are equal" -- is provided for free by records.

### Q3: What is the difference between `[UnionType]` and manual Match?
**A**: The functionality is identical. `[UnionType]` automatically updates Match/Switch when cases are added or removed, preventing mistakes. In this tutorial, we implemented manually to understand the principles.

### Q4: When do you distinguish between `UnionValueObject` and `UnionValueObject<TSelf>`?
**A**: Use `UnionValueObject` for pure data unions without transitions between cases (Shape, PaymentMethod), and `UnionValueObject<TSelf>` for unions requiring state transitions (OrderStatus, PaymentState). `UnionValueObject<TSelf>` passes precise type information to `DomainError` via CRTP and allows only valid transitions through the `TransitionFrom` helper.

---

UnionValueObject expresses "exactly one of several variants" as a type.

-> See [Appendix B: Type Selection Guide](../../Appendix/B-type-selection-guide.md) for the complete type selection criteria.
