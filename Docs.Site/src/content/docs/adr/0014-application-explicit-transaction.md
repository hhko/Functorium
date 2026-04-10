---
title: "ADR-0014: Application - Explicit Transaction Support"
status: "accepted"
date: 2026-03-22
---

## Context and Problem

Functorium's pipeline manages automatic transactions at the use case level. For most use cases that modify a single Aggregate, this approach works well. However, when creating an order requires wrapping `Order` Aggregate persistence and `Inventory` Aggregate stock deduction in a single atomic transaction, the auto-transaction scope is fixed to a single use case and cannot encompass both Aggregate modifications. Consequently, the order may be created but the stock deduction fails in a separate transaction and rolls back, causing data inconsistency.

Conversely, when read-only queries and write operations coexist within a single use case, the read segment is included in the transaction, causing unnecessary locking. Explicit control to expand or contract transaction scope is needed.

## Considered Options

1. IUnitOfWork.BeginTransactionAsync() + IUnitOfWorkTransaction
2. Always auto-transaction
3. Always explicit transaction
4. Saga pattern

## Decision

**Chosen option: "IUnitOfWork.BeginTransactionAsync() + IUnitOfWorkTransaction"**, to maintain the convenience of existing auto-transactions while extending the ability to explicitly specify transaction scope only for multi-Aggregate scenarios.

`BeginTransactionAsync()` is added to `IUnitOfWork`, returning an `IUnitOfWorkTransaction`. Developers directly determine `CommitAsync()` / `RollbackAsync()` timing through this transaction object. The key design principle is **conflict prevention with auto-transactions**. When the pipeline detects an already-active explicit transaction, it skips auto-transaction creation, allowing both mechanisms to coexist without nesting.

### Consequences

- <span class="adr-good">Good</span>, because single-Aggregate use cases (the vast majority) continue working with existing auto-transactions without code changes, requiring zero migration cost.
- <span class="adr-good">Good</span>, because multi-Aggregate changes like `Order` + `Inventory` can be wrapped in a single atomic transaction, preventing data inconsistency.
- <span class="adr-good">Good</span>, because the pipeline's active transaction detection logic structurally prevents auto/explicit transaction nesting.
- <span class="adr-bad">Bad</span>, because in use cases using explicit transactions, risks of forgetting `CommitAsync()` calls or missing `RollbackAsync()` in exception paths are shifted to the developer.

### Confirmation

- Verify through integration tests that auto-transactions and explicit transactions do not nest.
- Verify that rollback is performed correctly when exceptions occur within explicit transactions.

## Pros and Cons of the Options

### IUnitOfWork.BeginTransactionAsync() + IUnitOfWorkTransaction

- <span class="adr-good">Good</span>, because multi-Aggregate support can be added without modifying any existing auto-transaction code for single-Aggregate use cases.
- <span class="adr-good">Good</span>, because `BeginTransactionAsync()` is called only in the minority of use cases needing explicit transactions, confining boilerplate to that scope.
- <span class="adr-good">Good</span>, because `IUnitOfWorkTransaction` implements `IAsyncDisposable`, ensuring automatic rollback on commit omission via `await using` blocks.
- <span class="adr-bad">Bad</span>, because detection logic for "is there a currently active transaction?" must be additionally implemented inside the pipeline.

### Always Auto-Transaction

- <span class="adr-good">Good</span>, because developers do not need to be aware of transaction boundaries, minimizing cognitive load.
- <span class="adr-bad">Bad</span>, because transaction scope is fixed to the use case unit, so when `Inventory` deduction fails after `Order` persistence, the order cannot be rolled back.
- <span class="adr-bad">Bad</span>, because read-only segments are included in the transaction, causing unnecessary DB locking and performance degradation.

### Always Explicit Transaction

- <span class="adr-good">Good</span>, because transaction start, commit, and rollback points are explicitly stated in code for every use case, making scope transparent.
- <span class="adr-bad">Bad</span>, because simple use cases modifying only a single Aggregate (the vast majority) still require `BeginTransaction`/`Commit`/`Rollback` boilerplate.
- <span class="adr-bad">Bad</span>, because all developers must manually manage transactions, increasing the probability of commit omissions and unhandled exception paths across all use cases.

### Saga Pattern

- <span class="adr-good">Good</span>, because distributed transactions spanning different databases or microservices can be managed.
- <span class="adr-bad">Bad</span>, because applying Saga to a problem solvable by wrapping multiple Aggregates in a single DB transaction adds unnecessary infrastructure complexity -- compensating transactions, state machines, and message brokers.
- <span class="adr-bad">Bad</span>, because compensating logic for each step (e.g., inventory restoration, payment cancellation) must be separately designed and tested, costing several times more than single DB transactions.

## Related Information

- Related commits: `5a802766`, `71272343`
- Related guide: `Docs.Site/src/content/docs/guides/application/`
