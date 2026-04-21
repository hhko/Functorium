---
title: "IQueryPort Interface"
---
## Overview

What happens when you query order lists with Repository's `GetById`? You'd have to load Aggregates one by one and then filter in memory. When you try to solve **read-specialized requirements** like list queries, search, and pagination with Repository, inefficiency accumulates. IQueryPort<TEntity, TDto> is the core interface of the CQRS Query side that solves this problem.

---

## Learning Objectives

After completing this chapter, you will be able to:

1. Explain the roles of IQueryPort<TEntity, TDto>'s two type parameters
2. Choose the appropriate query method among Search, SearchByCursor, and Stream based on the use case
3. Understand the structure of PagedResult<T> and CursorPagedResult<T> return types
4. Explain why Query Port returns DTOs instead of domain entities

---

## Core Concepts

### "Why Is This Needed?" -- Limitations of Repository-Based Reads

Repository is optimized for single-item lookups. Imagine handling the request "show me 20 in-stock products sorted by price" with Repository.

```csharp
// Querying lists with Repository?
var allProducts = await repository.GetAll();      // Load all Aggregates into memory
var filtered = allProducts
    .Where(p => p.IsInStock)                      // Filter in memory
    .OrderBy(p => p.Price)                        // Sort in memory
    .Skip(20).Take(20);                           // Paginate in memory
```

All Aggregates are loaded, then filtered, sorted, and sliced in memory. Performance degrades sharply as data grows. This is why a **read-only interface** is needed.

### Type Parameters

IQueryPort separates the filtering target and return type with two type parameters.

| Parameter | Role | Example |
|-----------|------|---------|
| `TEntity` | Specification filtering target (domain entity) | `Product` |
| `TDto` | Read-only projection returned to clients | `ProductDto` |

### IQueryPort Method Catalog

IQueryPort provides three query methods (pagination variants + streaming) plus two read-side aggregate helpers (existence + count) — five in total.

| Method | Return Type | Purpose |
|--------|-------------|---------|
| `Search` | `FinT<IO, PagedResult<TDto>>` | Offset-based pagination |
| `SearchByCursor` | `FinT<IO, CursorPagedResult<TDto>>` | Keyset-based pagination |
| `Stream` | `IAsyncEnumerable<TDto>` | Large data streaming |
| `Exists` | `FinT<IO, bool>` | Read-side existence check (reporting · dashboard · early shortcut) |
| `Count` | `FinT<IO, int>` | Read-side count (reporting · pagination metadata) |

> **Exists / Count — why both on `IRepository` and `IQueryPort`?**
> - `IRepository.Exists` / `Count` → **write-side invariant checks** (e.g., duplicate-email check before `Create`)
> - `IQueryPort.Exists` / `Count` → **read-side reporting** (e.g., dashboard "any pending orders?")
>
> The same Specification can be reused on both sides; the distinction is the caller's intent and which side's performance characteristics matter.

### IObservablePort

IQueryPort, like IRepository, inherits from `IObservablePort`. Query-side implementations return `RequestCategory => "Query"`, enabling the Observability pipeline to collect Command and Query metrics separately.

### Command Side vs Query Side

In CQRS, writes and reads take different paths. Check how the two paths mirror each other in the table below.

| Aspect | Command Side | Query Side |
|--------|-------------|-----------|
| Port | IRepository<TEntity> | IQueryPort<TEntity, TDto> |
| Return | Entity (domain model) | DTO (read-only projection) |
| Purpose | State change (CUD) | Data query (R) |
| Filtering | ID-based | Specification-based |

---

## Project Description

### ProductId / Product

Domain entity with Ulid-based EntityId inheriting AggregateRoot. Used as TEntity for Specification<Product>.

### ProductDto

A read-only record containing only the fields clients need, not all fields of the domain entity.

### IProductQuery

```csharp
public interface IProductQuery : IQueryPort<Product, ProductDto> { }
```

A Product domain-specific Query Port. Inherits the three query methods by extending IQueryPort<Product, ProductDto>.

---

## Summary at a Glance

| Item | Description |
|------|-------------|
| IQueryPort | CQRS Query-side port interface |
| TEntity | Specification filtering target (domain entity) |
| TDto | Read-only projection for client return |
| Search | Offset-based pagination (PagedResult) |
| SearchByCursor | Keyset-based pagination (CursorPagedResult) |
| Stream | Large data streaming (IAsyncEnumerable) |

---

## FAQ

### Q1: Why does IQueryPort exist separately from IRepository?
**A**: Following CQRS principles, Command (write) and Query (read) are separated. Repository handles domain entity persistence, while QueryPort handles read-only projections. This separation allows each to be optimized independently.

### Q2: Why return DTOs?
**A**: Returning domain entities directly (1) exposes unnecessary domain logic to clients, (2) causes N+1 problems, and (3) makes read optimization difficult. With DTOs, you can query only needed fields and fetch all required data in a single query through JOINs.

### Q3: What is FinT<IO, T>?
**A**: LanguageExt's monad transformer that composes IO effects (side effects) with Fin<T> (success/failure results). It safely represents results of operations with side effects like database queries.

---

We've defined the read-only interface. But should the Order used in Commands and the OrderDto returned in Queries be the same class? In the next chapter, we'll examine the design criteria for separating Command DTOs and Query DTOs.

-> [Chapter 2: DTO Separation](../02-DTO-Separation/)
