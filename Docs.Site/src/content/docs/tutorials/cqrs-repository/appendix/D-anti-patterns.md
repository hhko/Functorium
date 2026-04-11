---
title: "CQRS Anti-Patterns"
---
## Overview

This appendix summarizes common anti-patterns and their correct alternatives when applying the CQRS pattern. Recognizing these patterns helps prevent design mistakes proactively.

---

## 1. Querying Lists with Repository

### Anti-Pattern

```csharp
// Attempting list queries with IRepository's GetByIds
public async ValueTask<FinResponse<List<OrderDto>>> Handle(
    ListOrdersQuery request, CancellationToken ct)
{
    var ids = await GetAllOrderIds();                    // Query all IDs
    var fin = await repository.GetByIds(ids).RunAsync(); // Load entire Aggregates
    var dtos = fin.Map(orders => orders.Map(o => new OrderDto(...))); // Manual conversion
    return dtos.ToFinResponse();
}
```

**Problems:**
- Loads entire Aggregate Roots into memory (including unnecessary domain logic)
- Loads all data without pagination
- Handles DTO conversion manually
- Potential N+1 query issue

### Correct Approach

```csharp
// Query lists with IQueryPort
public async ValueTask<FinResponse<PagedResult<OrderDto>>> Handle(
    ListOrdersQuery request, CancellationToken ct)
{
    var spec = Specification<Order>.All;
    var fin = await query.Search(spec, request.Page, request.Sort).RunAsync();
    return fin.ToFinResponse();
}
```

---

## 2. Modifying Data in Query Usecases

### Anti-Pattern

```csharp
// Modifying data in a Query Usecase
public class GetOrderUsecase
    : IQueryUsecase<GetOrderQuery, OrderDto>
{
    public async ValueTask<FinResponse<OrderDto>> Handle(
        GetOrderQuery request, CancellationToken ct)
    {
        // Incrementing view count in a Query
        await repository.Update(order.IncrementViewCount()).RunAsync();
        return fin.ToFinResponse();
    }
}
```

**Problems:**
- Queries should be read-only (CQS principle violation)
- Transaction pipeline may not be applied to Queries
- Writing is impossible when using read replicas

### Correct Approach

If data modification is needed, issue a separate Command.

---

## 3. Sharing Command DTOs and Query DTOs

### Anti-Pattern

```csharp
// Using the same DTO for Command and Query
public record OrderDto(
    string Id,
    string CustomerId,
    string CustomerName,       // Only needed for Query
    List<OrderItemDto> Items,  // Only needed for Command
    decimal TotalAmount,
    string StatusText,         // Only needed for Query
    DateTime CreatedAt,
    DateTime? UpdatedAt);
```

**Problems:**
- Command includes unnecessary read-only fields
- Query includes unnecessary write-only fields
- Changes on one side affect the other

### Correct Approach

```csharp
// Command DTO: only fields needed for writing
public record CreateOrderCommand(
    CustomerId CustomerId,
    List<CreateOrderItemDto> Items)
    : ICommandRequest<OrderId>;

// Query DTO: fields optimized for reading
public record OrderDto(
    string Id,
    string CustomerName,
    decimal TotalAmount,
    string StatusText,
    int ItemCount,
    DateTime CreatedAt);
```

---

## 4. Querying All Data Without Pagination

### Anti-Pattern

```csharp
// Querying all data at once
var allOrders = await query.Search(
    Specification<Order>.All,
    new PageRequest(1, int.MaxValue),  // Query all
    SortExpression.Empty).RunAsync();
```

**Problems:**
- Risk of out-of-memory (large datasets)
- Response time spikes
- Database load

### Correct Approach

```csharp
// Apply appropriate pagination
var pagedOrders = await query.Search(
    spec,
    new PageRequest(page: 1, size: 20),
    SortExpression.By("CreatedAt", SortDirection.Descending)).RunAsync();

// Use Stream for large datasets
await foreach (var dto in query.Stream(spec, sort, ct))
{
    // Process per record
}
```

---

## 5. Ignoring Domain Events

### Anti-Pattern

```csharp
// Not publishing events after Aggregate state changes
public class CancelOrderUsecase(IRepository<Order, OrderId> repository)
    : ICommandUsecase<CancelOrderCommand, OrderId>
{
    public async ValueTask<FinResponse<OrderId>> Handle(
        CancelOrderCommand command, CancellationToken ct)
    {
        var fin = await repository.GetById(command.OrderId).RunAsync();
        var order = fin.ThrowIfFail();
        order.Cancel();  // Without events, other bounded contexts cannot be notified
        await repository.Update(order).RunAsync();
        // How about inventory restoration, payment cancellation, etc.?
    }
}
```

**Problems:**
- State changes are not propagated to related systems
- Consistency between bounded contexts breaks down

### Correct Approach

```csharp
// Publish domain events inside the Aggregate
public class Order : AggregateRoot<OrderId>
{
    public void Cancel()
    {
        Status = OrderStatus.Cancelled;
        AddDomainEvent(new OrderCancelledEvent(Id));  // Add event
    }
}
// Transaction pipeline automatically publishes domain events after SaveChanges
```

---

## 6. Using DbContext Directly in Usecases

### Anti-Pattern

```csharp
// Direct infrastructure layer dependency in Usecase
public class CreateOrderUsecase(AppDbContext dbContext)
    : ICommandUsecase<CreateOrderCommand, OrderId>
{
    public async ValueTask<FinResponse<OrderId>> Handle(
        CreateOrderCommand command, CancellationToken ct)
    {
        var entity = new OrderEntity { ... };
        dbContext.Orders.Add(entity);
        await dbContext.SaveChangesAsync(ct);
        return FinResponse.Succ(new OrderId(entity.Id));
    }
}
```

**Problems:**
- Application layer directly depends on Infrastructure layer
- Testing requires real DB or complex mocking
- Domain logic and persistence logic are mixed

### Correct Approach

```csharp
// Persist through IRepository abstraction
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

---

## 7. Applying CQRS Everywhere

### Anti-Pattern

Applying CQRS even to simple settings management, code table management, and other features where CRUD is sufficient.

**Problems:**
- Unnecessary complexity increase
- Slower development speed
- Increased maintenance costs

### Correct Approach

Apply CQRS when there is complex domain logic or when read/write requirements differ. Simple CRUD can remain as-is. (See Appendix A)

---

## Anti-Pattern Checklist

Use this checklist during code reviews to quickly check for anti-patterns.

| Anti-Pattern | Symptom | Solution |
|-------------|---------|----------|
| List queries with Repository | Slow lists, increased memory usage | Use IQueryPort |
| Data modification in Query | CQS violation, transaction issues | Issue separate Command |
| Shared DTOs | Unnecessary fields, mutual impact | Separate Command/Query DTOs |
| No pagination applied | OOM, slow responses | Use PageRequest/Stream |
| Ignoring domain events | Cross-system inconsistency | Use AddDomainEvent |
| Direct DbContext usage | Layer violation, hard to test | Use IRepository |
| Excessive CQRS application | Unnecessary complexity | Identify cases where simple CRUD suffices |

---

Let's review the definitions and code examples of CQRS-related terminology used in this tutorial.

-> [Appendix E: Glossary](../E-glossary/)
