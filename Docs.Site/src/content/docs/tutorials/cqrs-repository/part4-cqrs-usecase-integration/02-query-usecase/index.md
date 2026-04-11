---
title: "Query Usecase"
---
## Overview

List queries need IQueryPort instead of Repository -- how does the Usecase structure change? While Command Usecases change state through Aggregate Roots, Query Usecases return read-only DTOs. The data source, return type, and transaction handling are all different. This chapter designs the Query-specific path and directly verifies the structural differences from Command.

---

## Learning Objectives

After completing this chapter, you will be able to:

1. Define Query requests and handlers with **IQueryRequest / IQueryUsecase** interfaces
2. Query data through a read-only path via **Query Port** instead of Repository
3. Return read-optimized data instead of domain entities with **DTO-based responses**
4. Explain the **structural differences between Command and Query**

---

## Core Concepts

### Command vs Query Usecase

Command and Query differ in everything from purpose to data source. Compare the key differences between the two paths in the table below.

| Aspect | Command | Query |
|--------|---------|-------|
| Purpose | State change | Data query |
| Interface | `ICommandRequest<T>` | `IQueryRequest<T>` |
| Handler | `ICommandUsecase` | `IQueryUsecase` |
| Data source | Repository (Aggregate) | Query Port (DTO) |
| Transaction | SaveChanges auto-called | No transaction |

### Query Port Pattern

Query Port returns DTOs directly instead of domain entities. Inheriting `IQueryPort<TEntity, TDto>` automatically provides three query methods: Search/SearchByCursor/Stream based on Specification.

```csharp
// Query-only interface - inherits IQueryPort
public interface IProductQuery : IQueryPort<Product, ProductDto>
{
}
```

In the Usecase, combine Specification with PageRequest/SortExpression to perform dynamic search.

```csharp
public sealed record Request(string Keyword, PageRequest Page, SortExpression Sort)
    : IQueryRequest<Response>;

public sealed class Usecase(IProductQuery productQuery)
    : IQueryUsecase<Request, Response>
{
    public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken ct)
    {
        var spec = new ProductNameSpec(request.Keyword);

        FinT<IO, Response> usecase =
            from products in productQuery.Search(spec, request.Page, request.Sort)
            select new Response(products);

        Fin<Response> result = await usecase.Run().RunAsync();
        return result.ToFinResponse();
    }
}
```

### ICacheable

When `IQueryRequest` implements `ICacheable`, `UsecaseCachingPipeline` automatically caches responses.

```csharp
public sealed record Request(string Keyword, PageRequest Page, SortExpression Sort)
    : IQueryRequest<Response>, ICacheable
{
    public string CacheKey => $"products:search:{Keyword}:{Page.Page}:{Page.PageSize}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}
```

---

## Project Description

The files below constitute the complete structure of a Query Usecase.

| File | Description |
|------|-------------|
| `ProductId.cs` | Ulid-based Product identifier |
| `Product.cs` | AggregateRoot-based product entity |
| `ProductDto.cs` | Query-only DTO |
| `IProductQuery.cs` | Interface inheriting IQueryPort\<Product, ProductDto\> |
| `InMemoryProductQuery.cs` | InMemoryQueryBase-based Query adapter implementation |
| `ProductNameSpec.cs` | Specification\<Product\> -- name keyword search condition |
| `SearchProductsQuery.cs` | Query Usecase pattern (Request, Response, Usecase) |
| `Program.cs` | Execution demo |

---

## Summary at a Glance

A summary of the core concepts composing Query Usecase.

| Concept | Description |
|---------|-------------|
| `IQueryRequest<T>` | Query request marker (Mediator IQuery extension) |
| `IQueryUsecase<TQuery, T>` | Query handler (Mediator IQueryHandler extension) |
| Query Port | Read-only data access interface |
| DTO | Query-only data returned instead of domain entities |

---

## FAQ

### Q1: Why use a separate Query Port instead of Repository?
**A**: Repository focuses on Aggregate Root-level CRUD, but Query needs a separate read-optimized path for joining multiple tables or aggregating. The core of CQRS is this separation of read/write paths.

### Q2: Why use FinT in Query Usecase too?
**A**: Data queries can also fail (not found, DB connection error, etc.). Using FinT allows error handling with the same composition pattern as Command.

---

We've created the Query Usecase. But what if you need to chain multiple Repository calls sequentially with condition validation in between? In the next chapter, we'll explore various patterns of FinT monadic composition.

-> [Chapter 3: FinT -> FinResponse](../03-FinT-To-FinResponse/)
