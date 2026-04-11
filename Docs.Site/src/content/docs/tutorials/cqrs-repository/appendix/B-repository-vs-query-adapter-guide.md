---
title: "Repository vs Query Selection"
---
## Overview

In Functorium CQRS, data access is divided into two paths: **IRepository** (Command side) and **IQueryPort** (Query side). This guide helps you choose the right path depending on the situation.

---

## IRepository vs IQueryPort Comparison

Check how the design purposes and usage patterns of the two interfaces differ.

| Characteristic | IRepository | IQueryPort |
|---------------|-------------|------------|
| **Purpose** | Aggregate Root-level persistence | Read-only queries |
| **Target** | AggregateRoot\<TId\> | DTO projection |
| **Return type** | FinT\<IO, TAggregate\> | FinT\<IO, PagedResult\<TDto\>\> |
| **Methods** | Create, GetById, Update, Delete | Search, SearchByCursor, Stream |
| **Transaction** | Used with IUnitOfWork | Not needed |
| **Pagination** | None (ID-based lookup) | Offset, Cursor, Stream |
| **Specification** | Not used | Used as search criteria |
| **Implementations** | InMemoryRepositoryBase, EfCoreRepositoryBase | InMemoryQueryBase, DapperQueryBase |

### Relationship with Specification

`Specification<T>` is the core search parameter for IQueryPort. The Search, SearchByCursor, and Stream methods all accept `Specification<TEntity>` as their first parameter to perform dynamic filtering. The And, Or, and Not combinations of Specification are used to compose complex search conditions on the Query side.

For detailed learning on the Specification pattern, see [Implementing Domain Rules with the Specification Pattern](../../specification-pattern/).

---

## Selection Criteria

### When to Use IRepository

Use it when you need to modify data or execute domain logic.

| Situation | Reason |
|-----------|--------|
| Data create/update/delete | Repository is Command-only |
| Lookup by ID then modify | GetById -> domain logic -> Update |
| Domain invariant validation needed | Execute Aggregate Root business rules |
| Transaction-required operations | Used with IUnitOfWork |
| Domain event publishing | Collect domain events from AggregateRoot |

```csharp
// IRepository usage example: Cancel order (Command)
public class CancelOrderUsecase(
    IRepository<Order, OrderId> repository)
    : ICommandUsecase<CancelOrderCommand, OrderId>
{
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
}
```

### When to Use IQueryPort

Use it for read-only queries, especially when lists/search/aggregation are needed.

| Situation | Reason |
|-----------|--------|
| List queries | Pagination + sorting support |
| Search features | Specification-based dynamic filtering |
| DTO projection | Return only the needed fields |
| Queries requiring joins | Data from multiple tables into a single DTO |
| Large data streaming | Memory-efficient queries with Stream method |
| Read performance optimization | Direct SQL control with Dapper etc. |

```csharp
// IQueryPort usage example: Search order list (Query)
public class SearchOrdersUsecase(
    IQueryPort<Order, OrderDto> query)
    : IQueryUsecase<SearchOrdersQuery, PagedResult<OrderDto>>
{
    public async ValueTask<FinResponse<PagedResult<OrderDto>>> Handle(
        SearchOrdersQuery request, CancellationToken ct)
    {
        var spec = BuildSpec(request);
        var fin = await query.Search(spec, request.Page, request.Sort).RunAsync();
        return fin.ToFinResponse();
    }

    private static Specification<Order> BuildSpec(SearchOrdersQuery request)
    {
        var spec = Specification<Order>.All;
        if (request.CustomerId is not null)
            spec &= new OrderByCustomerSpec(request.CustomerId.Value);
        if (request.Status is not null)
            spec &= new OrderByStatusSpec(request.Status.Value);
        return spec;
    }
}
```

---

## Decision Tree

```
Are you modifying data? (Create/Update/Delete)
├── Yes -> IRepository
│         └── Manage transactions with IUnitOfWork
└── No (read-only)
    ├── Lookup by ID then execute business logic?
    │   ├── Yes -> IRepository.GetById
    │   └── No -> Continue
    ├── List query + pagination?
    │   └── Yes -> IQueryPort.Search / SearchByCursor
    ├── Large data streaming?
    │   └── Yes -> IQueryPort.Stream
    └── Simple DTO projection?
        └── Yes -> IQueryPort.Search
```

---

## Common Scenario Guide

A summary of which path to choose for frequently encountered real-world scenarios.

| Scenario | Choice | Reason |
|----------|--------|--------|
| Create order | IRepository.Create | Aggregate creation + invariant validation |
| Change order status | IRepository.GetById + Update | Domain logic execution needed |
| Order list query | IQueryPort.Search | Pagination + DTO projection |
| Order detail view (display) | IQueryPort.Search | Joined DTO needed |
| Order detail view (for editing) | IRepository.GetById | Domain model needed |
| Order search | IQueryPort.Search | Specification-based dynamic filter |
| Dashboard aggregation | IQueryPort.Search | Read-only DTO |
| Data export | IQueryPort.Stream | Large data streaming |

---

## Anti-Patterns

### Querying Lists with IRepository

```csharp
// Anti-pattern: Querying lists with Repository's GetByIds
var ids = await GetAllOrderIds(); // Get entire ID list first
var orders = await repository.GetByIds(ids).RunAsync(); // Load entire Aggregates
var dtos = orders.Map(o => o.ToDto()); // Manual conversion

// Correct approach: Use IQueryPort
var result = await query.Search(spec, page, sort).RunAsync();
```

### Writing with IQueryPort

The Query side is read-only. Data changes must go through IRepository.

### Mixing IRepository and IQueryPort in the Same Usecase

```csharp
// Anti-pattern: Using IQueryPort in a Command Usecase
public class CreateOrderUsecase(
    IRepository<Order, OrderId> repository,
    IQueryPort<Order, OrderDto> query)  // Mixing Query into Command
{ ... }

// Correct approach: Commands use only IRepository, Queries use only IQueryPort
```

---

Let's review the complete API of the FinT and FinResponse functional types used in the Repository and Usecase layers.

-> [Appendix C: FinT / FinResponse Type Reference](../C-fint-finresponse-reference/)
