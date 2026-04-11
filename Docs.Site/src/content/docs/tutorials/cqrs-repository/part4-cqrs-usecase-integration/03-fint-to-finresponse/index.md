---
title: "From FinT to FinResponse"
---
## Overview

What if you need to chain multiple Repository calls sequentially with condition validation in between? A single `from...select` isn't enough. You need to weave multiple steps into a pipeline -- lookup, validate, modify, respond -- but repeating `RunAsync()` -> `IsSucc` check -> next call at each step causes boilerplate to explode. This chapter covers cleanly solving this problem with FinT's LINQ monadic composition.

---

## Learning Objectives

After completing this chapter, you will be able to:

1. Transform a single IO call with the **single operation** `from...select` pattern
2. Chain multiple IOs with the **sequential operation** `from...from...select` pattern
3. Halt the pipeline on business rule violations with **conditional interruption** via `guard()`
4. Bind pure computation results with **intermediate values** using `let`
5. Convert `Fin<T>` to `FinResponse<T>` with **ToFinResponse()**

---

## "Why Is This Needed?"

Without monadic composition, chaining multiple Repository calls sequentially looks like this.

```csharp
// Boilerplate: extracting and checking results at every step without monadic composition
var existingFin = await repository.GetById(productId).RunAsync();
if (existingFin.IsFail) return existingFin.ToFinResponse<Response>();

var existing = existingFin.ThrowIfFail();
if (!existing.IsActive)
    return FinResponse.Fail<Response>(Error.New("Product is not active"));

var updatedFin = await repository.Update(existing.UpdatePrice(newPrice)).RunAsync();
if (updatedFin.IsFail) return updatedFin.ToFinResponse<Response>();

var updated = updatedFin.ThrowIfFail();
return FinResponse.Succ(new Response(updated.Id.ToString(), updated.Price));
```

At every step: `RunAsync()` -> `IsFail` check -> value extraction repeats. As steps increase, nesting deepens, and core logic gets buried in error handling code. LINQ monadic composition eliminates this repetition.

---

## Core Concepts

### Pattern 1: Single Operation (from...select)

The simplest case. Execute one IO operation and transform the result.

```csharp
FinT<IO, Response> usecase =
    from created in repository.Create(product)
    select new Response(created.Id.ToString(), created.Name, created.Price);
```

If the Repository call succeeds, `select` transforms the result to Response; if it fails, Fail propagates without further operations.

### Pattern 2: Sequential Operation (from...from...select)

Used when multiple IO operations must be composed sequentially, like lookup then update. If an earlier step fails, subsequent steps are automatically skipped (Railway-oriented programming).

```csharp
FinT<IO, Response> usecase =
    from existing in repository.GetById(productId)
    let oldPrice = existing.Price
    from updated in repository.Update(existing.UpdatePrice(newPrice))
    select new Response(updated.Id.ToString(), oldPrice, updated.Price);
```

`let` binds a pure value without IO effects. Here it's used to remember the pre-change price.

### Pattern 3: Conditional Interruption with guard

Validates business rules and halts the pipeline on violation. `guard(condition, error)` generates `Fin.Fail` when the condition is false.

```csharp
FinT<IO, Response> usecase =
    from existing in repository.GetById(productId)
    from _ in guard(existing.IsActive, Error.New("Product is not active"))
    from updated in repository.Update(existing.UpdatePrice(newPrice))
    select new Response(...);
```

Failure is handled within monadic composition without throwing exceptions, keeping the pipeline flow consistent.

### Execution and Conversion

The pipeline composed with LINQ is still in a lazy, unexecuted state. Execute IO with `RunAsync()` and convert to a form deliverable to the API layer with `ToFinResponse()`.

```csharp
Fin<Response> result = await usecase.Run().RunAsync();  // Execute IO
return result.ToFinResponse();                           // Convert to FinResponse
```

---

## Project Description

You can run the three composition patterns directly in the files below.

| File | Description |
|------|-------------|
| `ProductId.cs` | Ulid-based Product identifier |
| `Product.cs` | AggregateRoot-based product (supports UpdatePrice, Deactivate) |
| `IProductRepository.cs` | Repository interface |
| `InMemoryProductRepository.cs` | InMemory implementation |
| `CompositionExamples.cs` | 3 LINQ composition pattern examples |
| `Program.cs` | Execution demo |

---

## Summary at a Glance

Compare each pattern's syntax and purpose at a glance.

| Pattern | Syntax | Purpose |
|---------|--------|---------|
| Single operation | `from x in op select ...` | Transform after one IO call |
| Sequential operation | `from x in op1 from y in op2 select ...` | Sequentially compose multiple IOs |
| Intermediate value | `let v = expr` | Bind pure computation result |
| Condition validation | `from _ in guard(cond, error)` | Halt with Fail if false |
| Execution | `.Run().RunAsync()` | Execute lazy IO to obtain Fin |
| Conversion | `.ToFinResponse()` | Fin -> FinResponse conversion |

---

## Structured Error Types

Using Functorium's structured error types instead of `Error.New("message")` allows clearly conveying error context.

```csharp
// ❌ String error -- caller cannot determine error type
from _ in guard(order.CanCancel(), Error.New("Cannot cancel"))

// ✅ Structured error -- determinable by type
from _ in guard(order.CanCancel(),
    DomainError.ForContext<Order>("Order status is not cancellable"))
```

Structured error types like `DomainError` and `ApplicationError` are used by the Pipeline layer to automatically map HTTP status codes based on error type.

---

## FAQ

### Q1: What's the difference between guard and if-throw?
**A**: `guard` generates Fail within monadic composition, handling failure without exceptions. if-throw raises an exception that must be caught in the Pipeline.

### Q2: What's the difference between let and from?
**A**: `from` binds `FinT<IO, T>` (operation with IO effects), `let` binds pure values (no IO effects).

### Q3: Where does execution halt on failure?
**A**: When the target of `from` returns `Fin.Fail`, all subsequent `from`, `let`, and `select` are not executed and Fail propagates as-is.

---

We've created clean pipelines with FinT composition. But where are domain events collected and published? In the next chapter, we'll examine the flow of events generated inside Aggregates and propagated externally.

-> [Chapter 4: Domain Event Flow](../04-Domain-Event-Flow/)
