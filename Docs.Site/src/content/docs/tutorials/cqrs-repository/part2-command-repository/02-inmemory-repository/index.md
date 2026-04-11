---
title: "InMemory Repository"
---
## Overview

Can you test a Repository without a DB?
Spinning up a real DB for every unit test makes tests slow and prone to CI failures due to environment dependencies.
`InMemoryRepositoryBase<TAggregate, TId>` is a `ConcurrentDictionary`-based `IRepository` implementation where
subclasses only need to provide a single `Store` property for all 8 CRUD methods to work automatically.

---

## Learning Objectives

After completing this chapter, you will be able to:

1. Explain the structure of `InMemoryRepositoryBase` and the subclass implementation pattern.
2. Explain how CRUD operations work on a `ConcurrentDictionary`-based store.
3. Explain the domain event collection mechanism through `IDomainEventCollector`.
4. Write test code that executes and verifies `FinT<IO, T>` results.

---

## Core Concepts

### InMemoryRepositoryBase Structure

Let's first look at the overall structure of `InMemoryRepositoryBase`. The only thing the subclass needs to provide is the `Store` property.

```
InMemoryRepositoryBase<TAggregate, TId>
├── abstract Store (ConcurrentDictionary)  <- Provided by subclass
├── IDomainEventCollector                  <- Injected via constructor
└── 8 CRUD methods                         <- Automatically implemented
```

### Subclass Implementation Pattern

How do you implement a subclass? Just override the `Store` property and CRUD is complete.

```csharp
public sealed class InMemoryProductRepository
    : InMemoryRepositoryBase<Product, ProductId>
{
    private static readonly ConcurrentDictionary<ProductId, Product> _store = new();

    public InMemoryProductRepository(IDomainEventCollector eventCollector)
        : base(eventCollector) { }

    protected override ConcurrentDictionary<ProductId, Product> Store => _store;
}
```

Note that `_store` is declared as `static`. Even when registered as Scoped in the DI container, the store is shared for the process lifetime.

### FinT\<IO, T\> Execution Pattern

Repository methods return `FinT<IO, T>`, but this alone doesn't execute anything. You need to go through 3 steps to get results.

```csharp
// 1. Call Repository method -> returns FinT<IO, T> (not yet executed)
FinT<IO, Product> operation = repository.Create(product);

// 2. Run() -> execute IO, RunAsync() -> convert to Task
Fin<Product> result = await operation.Run().RunAsync();

// 3. Check result
if (result.IsSucc)
    Console.WriteLine(result.ThrowIfFail().Name);
```

`Run()` executes the IO monad, and `RunAsync()` converts it to an async Task. Both steps are needed for the actual store operation to be performed.

### IDomainEventCollector

The Repository's `Create`/`Update` methods call `IDomainEventCollector.Track()` to
collect the Aggregate's domain events. For testing, `NoOpDomainEventCollector` is used since event collection is unnecessary.

---

## Project Description

### Project Structure

```
02-InMemory-Repository/
├── InMemoryRepository/
│   ├── InMemoryRepository.csproj
│   ├── Program.cs                      # CRUD demo
│   ├── ProductId.cs                    # Ulid-based identifier
│   ├── Product.cs                      # Aggregate + domain events
│   └── InMemoryProductRepository.cs    # InMemoryRepositoryBase implementation
├── InMemoryRepository.Tests.Unit/
│   ├── InMemoryRepository.Tests.Unit.csproj
│   ├── Using.cs
│   ├── xunit.runner.json
│   └── InMemoryProductRepositoryTests.cs
└── README.md
```

### Core Code

**InMemoryProductRepository** -- Just provide Store and CRUD is complete:

```csharp
public sealed class InMemoryProductRepository
    : InMemoryRepositoryBase<Product, ProductId>
{
    private static readonly ConcurrentDictionary<ProductId, Product> _store = new();

    public InMemoryProductRepository(IDomainEventCollector eventCollector)
        : base(eventCollector) { }

    protected override ConcurrentDictionary<ProductId, Product> Store => _store;
}
```

In tests, execute the FinT and verify results immediately.

**Executing FinT in tests**:

```csharp
var result = await repository.Create(product).Run().RunAsync();
result.IsSucc.ShouldBeTrue();
```

---

## Summary at a Glance

The following table summarizes the key components of InMemory Repository.

| Item | Description |
|------|-------------|
| Base class | `InMemoryRepositoryBase<TAggregate, TId>` |
| Store | `ConcurrentDictionary<TId, TAggregate>` |
| Required implementation | 1 `Store` property |
| Event collection | `IDomainEventCollector.Track()` |
| Execution pattern | `.Run().RunAsync()` |
| Usage | Testing, prototyping |

---

## FAQ

### Q1: Why use a static ConcurrentDictionary?
**A**: InMemory Repository needs to maintain data for the process lifetime, so it's declared as `static`. Even when registered as Scoped in the DI container, the store is shared.

### Q2: What happens if Create is called with an already existing ID?
**A**: The default implementation of `InMemoryRepositoryBase` overwrites with `Store[id] = aggregate`. If duplicate checking is needed, you can override the `Create` method.

### Q3: Is NoOpDomainEventCollector used in production?
**A**: No. In production, a `DomainEventCollector` implementation is injected via DI. `NoOpDomainEventCollector` is purely for testing purposes.

---

The InMemory implementation now allows quick Repository testing without a DB. But in production, EF Core must be used. What problems arise if you map domain models directly to DbSet? In the next chapter, we'll implement an EF Core Repository that separates Domain Model and Persistence Model.

-> [Chapter 3: EF Core Repository](../03-EfCore-Repository/)
