---
title: "Repository Interface"
---
## Overview

Should every Repository repeatedly define `Create`, `GetById`, `Update`, `Delete`?
Copy-pasting the same CRUD methods for every domain causes code duplication to grow exponentially.
`IRepository<TAggregate, TId>` is the common interface that solves this problem.
Through generic constraints, it enforces at compile time that only Aggregate Roots can be Repository targets,
and all methods return `FinT<IO, T>` to handle side effects and error handling in a composable form.

---

## Learning Objectives

After completing this chapter, you will be able to:

1. Explain the 8 CRUD methods of the `IRepository<TAggregate, TId>` interface.
2. Explain why the `FinT<IO, T>` return type is more advantageous for composition than `Task<T>`.
3. Explain how generic constraints (`AggregateRoot<TId>`, `IEntityId<TId>`) prevent incorrect usage.
4. Define domain-specific Repository interfaces yourself.

---

## Core Concepts

### Why Is This Needed?

Imagine defining separate Repository interfaces for Product, Order, and Customer domains.

```csharp
// Product Repository
FinT<IO, Product> Create(Product product);
FinT<IO, Product> GetById(ProductId id);
FinT<IO, Product> Update(Product product);
FinT<IO, int>     Delete(ProductId id);

// Order Repository - repeating the same pattern
FinT<IO, Order> Create(Order order);
FinT<IO, Order> GetById(OrderId id);
FinT<IO, Order> Update(Order order);
FinT<IO, int>   Delete(OrderId id);

// Customer Repository - repeating again...
```

The same signatures are copied every time an Aggregate is added. If the return type of a single method changes, all interfaces must be modified.
`IRepository<TAggregate, TId>` extracts this common pattern as generics, defining it in one place for all domains to reuse.

### 8 CRUD Methods

The following table shows the complete list of methods provided by `IRepository`. Single and batch versions are symmetrical, allowing you to handle both individual Aggregates and lists with the same pattern.

| Category | Method | Return Type |
|----------|--------|-------------|
| Single Create | `Create(TAggregate)` | `FinT<IO, TAggregate>` |
| Single Read | `GetById(TId)` | `FinT<IO, TAggregate>` |
| Single Update | `Update(TAggregate)` | `FinT<IO, TAggregate>` |
| Single Delete | `Delete(TId)` | `FinT<IO, int>` |
| Batch Create | `CreateRange(IReadOnlyList<TAggregate>)` | `FinT<IO, Seq<TAggregate>>` |
| Batch Read | `GetByIds(IReadOnlyList<TId>)` | `FinT<IO, Seq<TAggregate>>` |
| Batch Update | `UpdateRange(IReadOnlyList<TAggregate>)` | `FinT<IO, Seq<TAggregate>>` |
| Batch Delete | `DeleteRange(IReadOnlyList<TId>)` | `FinT<IO, int>` |

### FinT\<IO, T\> Return Type

Why return `FinT<IO, T>` instead of `Task<T>`? Look at the following structure.

```
FinT<IO, T> = IO<Fin<T>>
            = IO<Succ(T) | Fail(Error)>
```

- **Fin\<T\>** is a Result type that represents success (`Succ`) or failure (`Fail`). It handles failures as values without throwing exceptions.
- **IO** is a monad that tracks side effects. It makes side effects like DB access explicit in the type system.
- **FinT** is the composition of two monads (Monad Transformer). Multiple Repository calls can be chained with the `|` operator.

### Generic Constraints

The following constraints block incorrect Repository usage at compile time.

```csharp
public interface IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>  // Only Aggregate Roots allowed
    where TId : struct, IEntityId<TId>     // Only value-type IDs allowed
```

- `AggregateRoot<TId>` constraint: Attempting to directly persist an Entity or Value Object causes a compile error.
- `IEntityId<TId>` constraint: Enforces Ulid-based identifiers to unify the ID generation strategy.

### IObservablePort

IRepository inherits from `IObservablePort`. `IObservablePort` has a single `RequestCategory` property, used by the Observability pipeline to distinguish Command/Query for metric and log collection. Repository implementations return `RequestCategory => "Command"`.

---

## Project Description

### Project Structure

```
01-Repository-Interface/
├── RepositoryInterface/
│   ├── RepositoryInterface.csproj
│   ├── Program.cs              # Console demo
│   ├── ProductId.cs            # Ulid-based identifier
│   ├── Product.cs              # Aggregate Root
│   └── IProductRepository.cs   # Domain-specific Repository interface
├── RepositoryInterface.Tests.Unit/
│   ├── RepositoryInterface.Tests.Unit.csproj
│   ├── Using.cs
│   ├── xunit.runner.json
│   └── ProductTests.cs
└── README.md
```

### Core Code

Now that we've defined the common interface, how do we add domain-specific methods? Just inherit from `IRepository`.

**IProductRepository** -- A domain-specific interface extending IRepository:

```csharp
public interface IProductRepository : IRepository<Product, ProductId>
{
    FinT<IO, bool> Exists(Specification<Product> spec);
}
```

It inherits all 8 CRUD methods from `IRepository` while adding an `Exists` method specific to the Product domain. Even when new domains are added, there's no need to redefine CRUD signatures.

---

## Summary at a Glance

The following table summarizes the key items covered in this chapter.

| Item | Description |
|------|-------------|
| Interface | `IRepository<TAggregate, TId>` |
| CRUD methods | 4 single + 4 batch = 8 |
| Return type | `FinT<IO, T>` (composable monad) |
| Constraints | Aggregate Root + Ulid-based ID |
| Extension method | Inherit as domain-specific interface |

---

## FAQ

### Q1: Why does the Repository only handle Aggregate Roots?
**A**: In DDD, the Aggregate Root is the consistency boundary. Internal Entities must only be accessed through the Aggregate Root, so the Repository also operates at the Aggregate Root level.

### Q2: What advantage does FinT\<IO, T\> have over Task\<T\>?
**A**: `Task<T>` throws exceptions, while `FinT<IO, T>` represents failures as values. This allows error handling to be composed, avoiding exception-based control flow.

### Q3: Why use IReadOnlyList as the parameter type?
**A**: `IReadOnlyList<T>` provides index access and Count while disallowing modifications, making it a safe and flexible collection interface. It can accept various types such as `List<T>`, arrays, and `Seq<T>`.

---

We've defined the common Repository interface. But how do you test this interface without a DB? In the next chapter, we'll implement a ConcurrentDictionary-based InMemory Repository to verify behavior quickly without an actual DB connection.

-> [Chapter 2: InMemory Repository](../02-InMemory-Repository/)
