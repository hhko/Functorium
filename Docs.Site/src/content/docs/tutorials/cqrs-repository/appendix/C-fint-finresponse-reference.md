---
title: "FinT/FinResponse Reference"
---
## Overview

This is a reference document for the functional types used in Functorium CQRS. It covers the Repository layer's `FinT<IO, T>`, the Usecase layer's `FinResponse<T>`, and the `ToFinResponse()` conversion that connects them.

---

## Type Hierarchy

Functorium's functional types are built on top of LanguageExt. Check each type's affiliation in the hierarchy below.

```
LanguageExt (Library)
├── Fin<T>              Represents success(T) or failure(Error)
├── FinT<M, T>          Monad transformer: wraps M<Fin<T>>
└── IO                  Pure functional IO effect

Functorium (Framework)
├── FinResponse<T>      Usecase return type: Fin<T> + IFinResponseFactory
└── ToFinResponse()     Fin<T> -> FinResponse<T> conversion extension method
```

---

## Fin\<T\>

LanguageExt library's Result type. Represents success or failure.

```csharp
// Creation
Fin<int> success = Fin.Succ(42);
Fin<int> failure = Fin.Fail<int>(Error.New("An error occurred"));

// Pattern matching
var result = fin.Match(
    Succ: value => $"Success: {value}",
    Fail: error => $"Failure: {error.Message}");

// State checking
if (fin.IsSucc) { /* success */ }
if (fin.IsFail) { /* failure */ }

// Value access (throws if not success)
var value = fin.ThrowIfFail();
```

---

## FinT\<IO, T\>

FinT is a **monad transformer** that wraps `IO<Fin<T>>`. It is the return type of Repository methods.

```csharp
// Repository methods return FinT<IO, T>
FinT<IO, Order> result = repository.GetById(orderId);

// Execution (runs the IO effect to obtain Fin<T>)
Fin<Order> fin = await result.RunAsync();
```

### LINQ Monadic Composition

FinT can be composed using LINQ's `from...select` syntax.

```csharp
// Sequentially compose multiple Repository operations
var pipeline =
    from order    in repository.GetById(orderId)
    from _        in guard(order.CanCancel(), Error.New("Cannot cancel"))
    from updated  in repository.Update(order.Cancel())
    select updated.Id;

// If any step fails, the entire pipeline fails
Fin<OrderId> fin = await pipeline.RunAsync();
```

### guard Function

Fails the pipeline if the condition is not met.

```csharp
// guard(condition, error on failure)
from _ in guard(order.CanCancel(), Error.New("Cannot cancel status"))
```

### map / bind

```csharp
// map: transform the success value
FinT<IO, OrderId> orderId = repository.Create(order).Map(o => o.Id);

// bind (SelectMany): chain to another FinT
FinT<IO, Order> result = repository.GetById(orderId)
    .Bind(order => repository.Update(order.Cancel()));
```

---

## FinResponse\<T\>

The return type of the Usecase layer. Compatible with the Mediator pipeline (validation, transaction, logging, etc.).

```csharp
// Creation
FinResponse<OrderId> success = FinResponse.Succ(orderId);
FinResponse<OrderId> failure = FinResponse.Fail<OrderId>(Error.New("Failed"));

// State checking
if (response.IsSucc) { /* success */ }
if (response.IsFail) { /* failure */ }
```

### FinResponse vs Fin Differences

The two types are used in different layers and for different purposes.

| Characteristic | Fin\<T\> | FinResponse\<T\> |
|---------------|---------|-----------------|
| **Layer** | Repository/Domain | Usecase/Application |
| **Purpose** | Functional composition (FinT) | Mediator response |
| **Factory** | Fin.Succ / Fin.Fail | FinResponse.Succ / FinResponse.Fail |
| **Pipeline** | LINQ from...select | Mediator Pipeline |

---

## ToFinResponse() Conversion

An extension method for converting from the Repository layer (Fin) to the Usecase layer (FinResponse). It provides four overloads for different use cases.

### Basic Conversion

Passes the success value as-is.

```csharp
// Fin<A> -> FinResponse<A>
Fin<Order> fin = await repository.Create(order).RunAsync();
FinResponse<Order> response = fin.ToFinResponse();
```

### Mapping Conversion

Transforms the success value to a different type.

```csharp
// Fin<A> -> FinResponse<B> (success value transformation)
Fin<Order> fin = await repository.Create(order).RunAsync();
FinResponse<OrderId> response = fin.ToFinResponse(order => order.Id);
```

### Factory Conversion

Ignores the success value and creates a new instance.

```csharp
// Fin<A> -> FinResponse<B> (ignore success value, create new instance)
Fin<int> fin = await repository.Delete(orderId).RunAsync();
FinResponse<DeleteResult> response = fin.ToFinResponse(() => new DeleteResult(orderId));
```

### Custom Conversion

Handles both success and failure with custom logic.

```csharp
// Fin<A> -> FinResponse<B> (custom handling for both success/failure)
Fin<Order> fin = await repository.GetById(orderId).RunAsync();
FinResponse<OrderDto> response = fin.ToFinResponse(
    onSucc: order => FinResponse.Succ(order.ToDto()),
    onFail: error => FinResponse.Fail<OrderDto>(error));
```

---

## Common Usage Patterns

### Pattern 1: Simple Conversion

```csharp
public async ValueTask<FinResponse<OrderId>> Handle(
    CreateOrderCommand command, CancellationToken ct)
{
    var order = Order.Create(OrderId.New(), command.CustomerId);
    var fin = await repository.Create(order).RunAsync();
    return fin.ToFinResponse(o => o.Id);
}
```

### Pattern 2: Monadic Composition Then Conversion

```csharp
public async ValueTask<FinResponse<OrderId>> Handle(
    CancelOrderCommand command, CancellationToken ct)
{
    var pipeline =
        from order in repository.GetById(command.OrderId)
        from _     in guard(order.CanCancel(), Error.New("Cannot cancel"))
        from __    in repository.Update(order.Cancel())
        select order.Id;

    var fin = await pipeline.RunAsync();
    return fin.ToFinResponse();
}
```

### Pattern 3: Query Adapter Usage

```csharp
public async ValueTask<FinResponse<PagedResult<OrderDto>>> Handle(
    SearchOrdersQuery request, CancellationToken ct)
{
    var fin = await query.Search(spec, request.Page, request.Sort).RunAsync();
    return fin.ToFinResponse();
}
```

---

## Error Handling

### Error Creation

```csharp
// Simple error
Error.New("Order not found")

// With code
Error.New(404, "Order not found")

// Exception wrapping
Error.New(exception)
```

### Error Propagation in Pipelines

In a FinT pipeline, if any step fails, subsequent steps are skipped and the error propagates.

```csharp
var pipeline =
    from order in repository.GetById(orderId)     // If fails, skips steps below
    from _     in guard(order.CanCancel(), ...)    // If fails, skips steps below
    from __    in repository.Update(order.Cancel())
    select order.Id;
```

---

## Structured Error Types

Functorium provides structured error types beyond `Error.New("message")`. The Pipeline layer automatically maps HTTP status codes based on error type.

| Error Type | Purpose | HTTP Mapping |
|-----------|---------|-------------|
| `DomainError` | Domain rule violation | 422 Unprocessable Entity |
| `ApplicationError` | Application-level error | 400 Bad Request |
| `NotFoundError` | Resource not found | 404 Not Found |

```csharp
// DomainError creation example
DomainError.ForContext<Order>("Order status is not cancellable")

// Combined with Guard
from _ in guard(order.CanCancel(),
    DomainError.ForContext<Order>("Cannot cancel in current status"))
```

---

Let's review common design mistakes and their correct alternatives when applying CQRS.

-> [Appendix D: CQRS Anti-Patterns](../D-anti-patterns/)
