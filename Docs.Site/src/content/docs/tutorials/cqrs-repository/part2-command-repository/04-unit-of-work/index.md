---
title: "Unit of Work"
---
## Overview

What happens if the order is created but the inventory deduction fails?
Since Repositories handle CRUD for individual Aggregates, there's a risk that changes across multiple Repositories are only partially applied.
The Unit of Work pattern tracks all changes in a single business transaction and commits them at once to solve this problem.
This chapter practices the core concepts through the `IUnitOfWork` interface and its InMemory implementation.

---

## Learning Objectives

After completing this chapter, you will be able to:

1. Explain the roles of `IUnitOfWork`'s `SaveChanges()` and `BeginTransactionAsync()`.
2. Implement the mechanism for deferred execution of changes using the Pending Actions pattern.
3. Write explicit transaction scopes using `IUnitOfWorkTransaction`.

---

## Core Concepts

### IUnitOfWork Interface

`IUnitOfWork` provides two methods.

```csharp
public interface IUnitOfWork
{
    FinT<IO, Unit> SaveChanges(CancellationToken cancellationToken = default);
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
```

- **SaveChanges()** persists all tracked changes at once. Returns `FinT<IO, Unit>` for type-safe success/failure handling.
- **BeginTransactionAsync()** starts an explicit transaction. Used when immediate-execution SQL like `ExecuteDeleteAsync` and `SaveChanges` need to be in the same transaction.

### Deferred Execution Pattern

Even when Repository performs CRUD, it doesn't immediately reflect in the store. Instead, operations are registered with `AddPendingAction()` and executed in bulk when `SaveChanges()` is called.

```csharp
uow.AddPendingAction(() => store[product.Id] = product);
// Not yet reflected in store

await uow.SaveChanges().Run().RunAsync();
// Now reflected in store
```

Thanks to this pattern, changes from multiple Repositories are atomically applied with a single `SaveChanges()`. In actual EF Core, the Change Tracker takes on this role.

### IUnitOfWorkTransaction

For typical CRUD, `SaveChanges()` alone is sufficient, but when immediate-execution queries and Change Tracker-based changes need to be combined, explicit transactions are needed.

```csharp
await using var tx = await uow.BeginTransactionAsync();
// Perform multiple operations
await tx.CommitAsync();
// Uncommitted transactions are automatically rolled back on Dispose
```

If the block is exited without calling `CommitAsync()`, the transaction is automatically rolled back, preventing partial commits on failure.

### Multi-Aggregate Transactions

Let's return to the question from the overview. "What if the order is created but inventory deduction fails?" -- If each Repository saves individually, such inconsistency occurs.

```csharp
var productStore = new Dictionary<ProductId, Product>();
var orderStore = new Dictionary<OrderId, Order>();

var laptop = Product.Create("Laptop", 1_500_000m, stock: 10);
productStore[laptop.Id] = laptop;

var uow = new InMemoryUnitOfWork();

// Register changes from two Aggregates in a single UoW
var order = Order.Create(laptop.Id, quantity: 2, unitPrice: laptop.Price);
uow.AddPendingAction(() => orderStore[order.Id] = order);
uow.AddPendingAction(() => laptop.DeductStock(2));

// Before SaveChanges: 0 orders, stock 10
await uow.SaveChanges().Run().RunAsync();
// After SaveChanges: 1 order, stock 8
```

Changes from two Aggregates are atomically applied with a single `SaveChanges()`.
If individual Repositories called SaveChanges separately, the order might be created but inventory not deducted, causing inconsistency.

> **Note**: In the InMemory implementation, pending actions are executed sequentially, so if an exception occurs mid-way, side effects from already-executed actions remain. In actual EF Core, the Change Tracker guarantees all-or-nothing at the DB level.

---

## Project Description

### Project Structure

```
04-Unit-Of-Work/
├── UnitOfWork/
│   ├── UnitOfWork.csproj
│   ├── Program.cs                 # SaveChanges, transaction, multi-Aggregate demo
│   ├── ProductId.cs               # Ulid-based identifier
│   ├── Product.cs                 # Demo Aggregate (with inventory management)
│   ├── OrderId.cs                 # Ulid-based order identifier
│   ├── Order.cs                   # Order Aggregate (for multi-Aggregate demo)
│   └── InMemoryUnitOfWork.cs      # IUnitOfWork InMemory implementation
├── UnitOfWork.Tests.Unit/
│   ├── UnitOfWork.Tests.Unit.csproj
│   ├── Using.cs
│   ├── xunit.runner.json
│   └── InMemoryUnitOfWorkTests.cs
└── README.md
```

### Core Code

Let's look at the `InMemoryUnitOfWork` implementation. Operations are accumulated in the `_pendingActions` list and executed all at once in `SaveChanges()`.

**InMemoryUnitOfWork** -- Register pending actions and execute in batch:

```csharp
public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    private readonly List<Action> _pendingActions = [];
    private bool _saved;
    public bool IsSaved => _saved;

    public void AddPendingAction(Action action) => _pendingActions.Add(action);

    public FinT<IO, Unit> SaveChanges(CancellationToken cancellationToken = default)
    {
        return IO.lift(() =>
        {
            foreach (var action in _pendingActions) action();
            _pendingActions.Clear();
            _saved = true;
            return Fin.Succ(unit);
        });
    }
}
```

Wrapped with `IO.lift()` to defer actual execution until `Run()` is called. The key is iterating `_pendingActions` to execute all operations and then clearing the list.

---

## Summary at a Glance

The following table summarizes the key components of Unit of Work.

| Item | Description |
|------|-------------|
| Interface | `IUnitOfWork` |
| Core methods | `SaveChanges()`, `BeginTransactionAsync()` |
| Return type | `FinT<IO, Unit>` (SaveChanges) |
| Transaction | `IUnitOfWorkTransaction` (CommitAsync + DisposeAsync) |
| InMemory implementation | `AddPendingAction()` -> `SaveChanges()` |
| Actual implementation | Wraps EF Core's `DbContext.SaveChangesAsync()` |

### Repository vs Unit of Work

How do Repository and Unit of Work differ? Compare the differences in concerns in the following table.

| Concern | Repository | Unit of Work |
|---------|------------|-------------|
| Scope | Single Aggregate | Entire transaction |
| Role | CRUD operations | Batch commit of changes |
| Dependency | Specific Aggregate type | Aggregate-agnostic |
| Call timing | Inside Usecase | On Usecase completion |

---

## FAQ

### Q1: Why not call SaveChanges directly in the Repository?
**A**: When a single Usecase uses multiple Repositories, if each Repository individually calls SaveChanges, partial commits can occur. Unit of Work commits all changes at once to guarantee atomicity.

### Q2: When should BeginTransactionAsync be used?
**A**: When immediate-execution queries like EF Core's `ExecuteDeleteAsync`/`ExecuteUpdateAsync` need to be combined with Change Tracker-based `SaveChanges` in the same transaction. For typical CRUD, `SaveChanges()` alone is sufficient.

### Q3: Is the IsSaved property used in production?
**A**: No. `IsSaved` is an InMemory implementation property added for testing convenience. In actual EF Core-based Unit of Work, success/failure is determined by the `Fin<Unit>` result from `SaveChanges()`.

---

The Command-side persistence is complete. Repositories save individual Aggregates, and Unit of Work guarantees transaction atomicity. But what are the limitations of querying order lists with Repository's GetById? It's inefficient because entire Aggregates must be loaded and then only needed fields extracted. In Part 3, we'll explore read-optimized Query patterns.

-> [Chapter 1: IQueryPort Interface](../../Part3-Query-Patterns/01-QueryPort-Interface/)
