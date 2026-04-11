---
title: "Transaction Pipeline"
---
## Overview

What happens if every Command Usecase has to repeat SaveChanges and event publishing? Writing the same transaction management code in every Usecase buries business logic in infrastructure boilerplate. This chapter automates transaction start, SaveChanges, commit, and event publishing with a Mediator Pipeline, so Usecases can focus solely on business logic.

---

## Learning Objectives

After completing this chapter, you will be able to:

1. Explain the **Command Pipeline execution flow** (Request -> Handler -> SaveChanges -> Commit -> Event Publishing)
2. Implement automatic transaction management with **IUnitOfWork and IUnitOfWorkTransaction**
3. Explain the principle of **automatic rollback via uncommitted Dispose** on Handler/SaveChanges failure
4. Explain **why events are published after transaction commit**

---

## "Why Is This Needed?"

Without a Pipeline, managing transactions manually in every Usecase looks like this.

```csharp
// Boilerplate repeated in every Usecase without Pipeline
public async ValueTask<FinResponse<Response>> Handle(
    CreateProductCommand.Request request, CancellationToken ct)
{
    await using var transaction = await unitOfWork.BeginTransactionAsync(ct);
    try
    {
        // --- Business logic ---
        var product = Product.Create(request.Name, request.Price);
        var fin = await repository.Create(product).RunAsync();
        if (fin.IsFail) return fin.ToFinResponse<Response>();
        // --- Actual logic ends here ---

        await unitOfWork.SaveChanges(ct).RunAsync();
        await transaction.CommitAsync(ct);
        await eventPublisher.PublishTrackedEvents(ct);

        return fin.ToFinResponse(p => new Response(p.Id.ToString()));
    }
    catch
    {
        // Uncommitted transaction auto-rolls back on Dispose
        throw;
    }
}
```

Business logic is 3 lines, but transaction management code occupies the rest. As Usecases grow to 10, 20, this boilerplate is all duplicated. The Transaction Pipeline handles this cross-cutting concern in one place.

---

## Core Concepts

### Command Pipeline Execution Order

The Pipeline executes in the order below. The Handler (business logic) only runs in step 2; everything else is handled automatically by the Pipeline.

```
1. BeginTransactionAsync()       <- Start transaction
2. Handler execution (next)      <- Business logic
3. On failure -> return          <- Uncommitted -> auto-rollback on Dispose
4. UoW.SaveChanges()            <- Save to DB
5. transaction.CommitAsync()    <- Commit transaction
6. PublishTrackedEvents()       <- Publish domain events
7. return response              <- Return success response
```

If the Handler returns failure at step 3, everything from SaveChanges onward is skipped and returned immediately. Since the transaction wasn't committed, it auto-rolls back on Dispose.

### IUnitOfWork

Abstracts SaveChanges and transaction initiation.

```csharp
public interface IUnitOfWork
{
    FinT<IO, Unit> SaveChanges(CancellationToken ct = default);
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default);
}
```

- `SaveChanges`: Wraps EF Core's `SaveChangesAsync()` in FinT
- `BeginTransactionAsync`: Starts an explicit transaction scope

### IUnitOfWorkTransaction

Implements `IAsyncDisposable`, so use with `await using`. Uncommitted transactions auto-rollback on Dispose.

```csharp
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);
}
```

### Query Bypass

The Pipeline only processes Commands. When a Query request arrives, only the Handler executes and SaveChanges/transaction are skipped.

---

## Project Description

You can verify Pipeline behavior directly in the files below.

| File | Description |
|------|-------------|
| `ProductId.cs` | Ulid-based Product identifier |
| `Product.cs` | AggregateRoot + product that generates events |
| `InMemoryUnitOfWork.cs` | IUnitOfWork / IUnitOfWorkTransaction InMemory implementation |
| `SimpleDomainEventCollector.cs` | IDomainEventCollector implementation |
| `TransactionDemo.cs` | Command Pipeline flow simulation |
| `Program.cs` | Success/failure scenario demo |

---

## Summary at a Glance

A summary of Transaction Pipeline's core components.

| Concept | Description |
|---------|-------------|
| `IUnitOfWork` | Abstracts SaveChanges + transaction start |
| `IUnitOfWorkTransaction` | Explicit transaction scope (IAsyncDisposable) |
| `UsecaseTransactionPipeline` | Automatic transaction management as Mediator Pipeline |
| Rollback | Uncommitted transactions auto-rollback on Dispose |
| Event publishing | Events published only after transaction commit |

---

## FAQ

### Q1: Why is event publishing after transaction commit?
**A**: If events are published before commit, event handlers may query data that hasn't been committed yet. Also, if the commit fails, already-published events cannot be cancelled.

### Q2: Should the Usecase call SaveChanges directly?
**A**: No. The Pipeline calls it automatically. The Usecase only needs to call Repository's Create/Update.

### Q3: When is an explicit transaction needed?
**A**: Since the Pipeline manages transactions automatically, there's typically no need to handle transactions directly in the Usecase. The Pipeline's `BeginTransactionAsync` also includes immediate-execution SQL like ExecuteDeleteAsync in the same transaction.

---

All layers of the CQRS architecture are now complete. Let's see how these patterns integrate in real domains. In Part 5, we'll apply the complete CQRS flow to four domain examples: orders, customers, inventory, and catalog.

-> [Chapter 1: Order Management](../../Part5-Domain-Examples/01-Ecommerce-Order-Management/)
