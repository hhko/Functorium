---
title: "Why CQRS"
---
## Overview

It's Friday afternoon, and a product manager sends a Slack message: "Please add a filter for order history by customer."

You open `OrderRepository`. It already has `GetByCustomer`, `GetRecent`, `GetSummaries`, and `SearchByKeyword`. Just add one more, right? But looking back, you did the same thing three months ago, and you "just added one" back then too. Now the Repository has 15 methods. Next quarter, it will be 25.

The moment the question **"Is this really a Repository?"** comes to mind, this tutorial begins.

This tutorial solves that problem with **Command Query Responsibility Segregation (CQRS)**. Starting from domain entity foundations and progressing through Repository patterns, Query adapters, and Usecase integration, you will learn every aspect of the CQRS pattern step by step through **22 hands-on projects**.

---

## The Problem with Handling Everything with a Single Model

### The Traditional CRUD Approach

Most applications handle both reads and writes with a single model. Take a look at the following code.

```csharp
// ❌ A single Repository bearing all responsibilities
public interface IOrderRepository
{
    // Write (Command)
    Task<Order> CreateAsync(Order order);
    Task UpdateAsync(Order order);
    Task DeleteAsync(Guid id);

    // Read (Query)
    Task<Order> GetByIdAsync(Guid id);
    Task<List<Order>> GetByCustomerAsync(Guid customerId);
    Task<List<Order>> GetRecentOrdersAsync(int count);
    Task<List<OrderSummary>> GetOrderSummariesAsync(int page, int size);
    Task<List<Order>> SearchAsync(string keyword, DateTime? from, DateTime? to);
    // ... methods keep growing with each new query condition
}
```

A single interface has 4 write methods and 5 read methods mixed together, and a new method is added every time a new query condition arises. The following table summarizes the specific problems this approach creates.

| Problem | Description |
|---------|-------------|
| **Read/Write requirement conflicts** | Writes need domain invariant validation; reads need fast projections |
| **Model bloat** | Read-only fields and write-only logic coexist in a single class |
| **Difficult performance optimization** | Reads and writes have different performance characteristics but share the same path |
| **Method explosion** | Every combination of query conditions requires a new method |
| **Test complexity** | Tests for a single Repository become bloated |

---

## What CQRS Solves

CQRS (Command Query Responsibility Segregation) **separates write and read models** to optimize each for its own requirements. When writes are handled at the Aggregate Root level and reads use Specification-based dynamic search, all the above problems are resolved.

```csharp
// ✅ Command side: persistence at the Aggregate Root level
public interface IRepository<TAggregate, TId>
{
    FinT<IO, TAggregate> Create(TAggregate aggregate);
    FinT<IO, TAggregate> GetById(TId id);
    FinT<IO, TAggregate> Update(TAggregate aggregate);
    FinT<IO, int> Delete(TId id);
}

// ✅ Query side: DTO projection + pagination
public interface IQueryPort<TEntity, TDto>
{
    FinT<IO, PagedResult<TDto>> Search(
        Specification<TEntity> spec,
        PageRequest page,
        SortExpression sort);
}
```

The following table maps each problem listed above to how it is resolved in CQRS.

| Problem | CQRS Solution |
|---------|---------------|
| Read/Write conflicts | Separate Command (IRepository) and Query (IQueryPort) |
| Model bloat | Command uses domain models, Query uses DTOs |
| Performance optimization | Reads and writes can be optimized independently |
| Method explosion | Resolved with Specification-based dynamic search |
| Test complexity | Command and Query can be tested independently |

---

## Target Audience

You can choose your learning scope based on your experience level.

| Level | Audience | Recommended Scope |
|-------|----------|-------------------|
| **Beginner** | Developers who know basic C# syntax and want to get started with CQRS | Part 1 |
| **Intermediate** | Developers who understand patterns and want practical application | Part 1~3 |
| **Advanced** | Developers interested in architecture design and domain modeling | Part 4~5 + Appendix |

---

## Prerequisites

To effectively learn from this tutorial, you should understand basic C# syntax (classes, interfaces, generics) and fundamental object-oriented programming concepts, and have experience running .NET projects.

Familiarity with basic LINQ syntax, unit testing experience, Domain-Driven Design (DDD) basics (Entity, Aggregate Root), and basic Entity Framework Core usage will make learning smoother. However, these are not required and can be learned as you progress through the tutorial.

---

## Expected Outcomes

After completing this tutorial, you will be able to:

### 1. Implement Write Operations with Aggregate Root-Level Repositories

You can persist domain models while guaranteeing their invariants through IRepository.

```csharp
// Aggregate-level persistence with IRepository
public class CreateOrderUsecase(IRepository<Order, OrderId> repository)
    : ICommandUsecase<CreateOrderCommand, OrderId>
{
    public async ValueTask<FinResponse<OrderId>> Handle(
        CreateOrderCommand command, CancellationToken ct)
    {
        var order = Order.Create(OrderId.New(), command.CustomerId);
        var fin = await repository.Create(order).RunAsync();
        return fin.ToFinResponse(o => o.Id);
    }
}
```

### 2. Implement Read-Optimized Queries with Query Adapters

Combining IQueryPort with Specification means you never need to add methods when new query conditions arise.

```csharp
// DTO projection + pagination with IQueryPort
public class SearchOrdersUsecase(IQueryPort<Order, OrderDto> query)
    : IQueryUsecase<SearchOrdersQuery, PagedResult<OrderDto>>
{
    public async ValueTask<FinResponse<PagedResult<OrderDto>>> Handle(
        SearchOrdersQuery request, CancellationToken ct)
    {
        var spec = new OrderByCustomerSpec(request.CustomerId);
        var fin = await query.Search(spec, request.Page, request.Sort).RunAsync();
        return fin.ToFinResponse();
    }
}
```

### 3. Compose Functional Pipelines with FinT Monads

Chaining multiple Repository calls with `from...select` syntax automatically propagates error handling.

```csharp
// Monadic composition with from...select syntax
var pipeline =
    from order in repository.GetById(orderId)
    from _     in guard(order.CanCancel(), Error.New("Cannot cancel"))
    from __    in repository.Update(order.Cancel())
    select order.Id;

var fin = await pipeline.RunAsync();
return fin.ToFinResponse();
```

### 4. Ensure Consistency with Transaction Pipelines

Command Usecases automatically pass through the transaction pipeline, so you never need to call SaveChanges or publish domain events manually.

```csharp
// Commands automatically pass through the transaction pipeline
// SaveChanges + domain event publishing are handled automatically
ICommandRequest<TSuccess> -> UsecaseTransactionPipeline -> ICommandUsecase
```

---

## Tutorial Structure

```
Part 0: Introduction
├── CQRS pattern concepts and motivation
├── Environment setup
└── CQRS architecture overview

Part 1: Domain Entity Foundations
├── Entity<TId> and IEntityId
├── AggregateRoot<TId>
├── Domain events
└── Entity interfaces (IAuditable, ISoftDeletable)

Part 2: Command Side -- Repository Pattern
├── IRepository<TAggregate, TId> interface
├── InMemory Repository implementation
├── EF Core Repository implementation
└── Unit of Work pattern

Part 3: Query Side -- Read-Only Patterns
├── IQueryPort<TEntity, TDto> interface
├── Command DTO vs Query DTO separation
├── Pagination and sorting
├── InMemory Query adapter
└── Dapper Query adapter

Part 4: CQRS Usecase Integration
├── Command/Query Usecase
├── FinT -> FinResponse conversion
├── Domain event flow
└── Transaction pipeline

Part 5: Domain-Specific Practical Examples
├── Order management
├── Customer management
├── Inventory management
└── Catalog search
```

---

## Differences from Quickstart Tutorials

The following table compares the quick hands-on approach of quickstart tutorials with this tutorial's approach.

| Aspect | Quickstart Tutorial | This Tutorial |
|--------|---------------------|---------------|
| **Purpose** | Quick hands-on and result verification | Concept understanding and design principle learning |
| **Depth** | Core usage focus | Deep dive into internals and principles |
| **Scope** | Basic CQRS usage | Repository, Query adapter, transactions, events |
| **Audience** | Developers who want to apply immediately | Developers who want deep understanding of patterns |

---

## Learning Path

```
Beginner (Part 1)
├── Entity and Identity implementation
├── Aggregate Root and domain invariants
├── Domain events
└── Entity interfaces

Intermediate (Part 2~3)
├── Repository interface and implementation
├── Unit of Work pattern
├── Query adapter and DTO separation
└── Pagination and sorting

Advanced (Part 4~5 + Appendix)
├── Command/Query Usecase integration
├── FinT monadic composition
├── Transaction pipeline
└── Domain-specific practical examples
```

---

## FAQ

### Q1: Should CQRS be applied to every project?
**A**: No. CQRS provides value when read and write requirements differ significantly. For simple CRUD-centric applications, it may only increase complexity. We recommend gradually introducing it starting with domains that have diverse query conditions and need performance optimization.

### Q2: What advantage does `FinT<IO, T>` have over `Task<T>`?
**A**: `Task<T>` expresses failure by throwing exceptions, while `FinT<IO, T>` represents success and failure as values. This enables composing multiple Repository calls using `from...select` LINQ syntax and implementing Railway-oriented programming without exception-based control flow.

### Q3: Doesn't separating `IRepository` and `IQueryPort` mean more code?
**A**: Initially, interfaces and DTOs do increase, but the "method explosion" problem of adding methods to Repository for every new query condition disappears. `IQueryPort` supports Specification-based dynamic search, so adding new filter conditions requires no interface changes.

### Q4: What is the recommended order for learning this tutorial?
**A**: Beginners should proceed sequentially from Part 1 (Domain Entity Foundations). If you already know CQRS concepts, you can start from Part 2 (Repository Pattern) or Part 4 (Usecase Integration). Each Part can be built/tested independently.

---

Now that you've seen why CQRS is needed, it's time to prepare your development environment. The next chapter guides you through the entire environment setup process, from installing the .NET SDK to configuring VS Code and cloning the tutorial project.

-> [Chapter 0.2: Environment Setup](02-prerequisites-and-setup.md)
