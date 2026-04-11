---
title: "CQRS vs Traditional CRUD"
---
## Overview

Does our project really need CQRS? CQRS is a powerful pattern, but it's not suitable for every project. Applying CQRS to simple data input/output only adds unnecessary complexity. Conversely, if you push through with CRUD when there's complex domain logic and diverse read requirements, the Repository becomes bloated. This appendix helps you make the right choice by comparing the pros and cons of both approaches.

---

## Overall Comparison

A side-by-side comparison of the key characteristics of both approaches.

| Characteristic | Traditional CRUD | CQRS |
|---------------|-----------------|------|
| **Model** | Single model (shared read/write) | Dual model (Command/Query separation) |
| **Repository** | Single Repository | IRepository (write) + IQueryPort (read) |
| **DTO** | Single DTO or direct Entity exposure | Command DTO + Query DTO separation |
| **Complexity** | Low | Medium~High |
| **Scalability** | Limited | Independent read/write scaling |
| **Performance optimization** | Applied uniformly | Individual read/write optimization |
| **Learning curve** | Low | Medium |

---

## Detailed Comparison

### 1. Single Model vs Dual Model

#### Traditional CRUD: Single Model

```csharp
// A single Entity handles all responsibilities
public class Order
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public List<OrderItem> Items { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }

    // Join results for reads also in the same class
    public string CustomerName { get; set; }
    public string StatusDisplayText { get; set; }
}
```

#### CQRS: Dual Model

```csharp
// Command side: Domain model (includes business logic)
public class Order : AggregateRoot<OrderId>
{
    public CustomerId CustomerId { get; private set; }
    private readonly List<OrderItem> _items = [];
    public OrderStatus Status { get; private set; }

    public void AddItem(Product product, int qty) { /* invariant validation */ }
    public void Cancel() { /* domain rules */ }
}

// Query side: DTO (optimized for display)
public record OrderDto(
    string Id,
    string CustomerName,
    string StatusText,
    decimal TotalAmount,
    int ItemCount,
    DateTime CreatedAt);
```

---

### 2. Repository Design

#### Traditional CRUD

```csharp
public interface IOrderRepository
{
    Task<Order> CreateAsync(Order order);
    Task<Order> GetByIdAsync(Guid id);
    Task UpdateAsync(Order order);
    Task DeleteAsync(Guid id);

    // Read methods keep growing
    Task<List<Order>> GetByCustomerAsync(Guid customerId);
    Task<List<Order>> GetRecentAsync(int count);
    Task<PagedList<Order>> SearchAsync(OrderFilter filter, int page, int size);
    Task<List<OrderSummary>> GetSummariesAsync();
    // ...
}
```

#### CQRS

```csharp
// Command: clean 8 CRUD methods
public interface IRepository<TAggregate, TId>
{
    FinT<IO, TAggregate> Create(TAggregate aggregate);
    FinT<IO, TAggregate> GetById(TId id);
    FinT<IO, TAggregate> Update(TAggregate aggregate);
    FinT<IO, int> Delete(TId id);
    // + 4 Range methods
}

// Query: Specification-based dynamic search
public interface IQueryPort<TEntity, TDto>
{
    FinT<IO, PagedResult<TDto>> Search(
        Specification<TEntity> spec,
        PageRequest page,
        SortExpression sort);
}
```

---

### 3. Scalability

#### Traditional CRUD

```
Client -> Service -> Repository -> Single DB
                                     |
                    Reads/writes go through the same DB
                    Read traffic affects write performance
```

#### CQRS

```
Client -> Mediator
             |
    Command Path          Query Path
         |                    |
    IRepository          IQueryPort
         |                    |
    Write DB              Read DB (or same DB)
                              |
                    Read replicas can be added independently
```

---

### 4. Complexity Tradeoffs

#### When Traditional CRUD Is Appropriate

In the following situations, the benefits of CQRS don't outweigh its initial costs.

| Situation | Reason |
|-----------|--------|
| Simple data input/output | Little to no domain logic |
| Similar read/write ratio | Little benefit from separation |
| Small team/project | Benefits don't outweigh CQRS initial costs |
| Admin CRUD screens | Read optimization unnecessary |
| Prototype | Fast development is the priority |

#### When CQRS Is Appropriate

In the following situations, read/write separation provides tangible benefits.

| Situation | Reason |
|-----------|--------|
| Reads far exceed writes | Significant benefit from read optimization |
| Complex domain logic | Can focus on Command model |
| Diverse read requirements | Query models can be optimized per use case |
| High performance requirements | Independent read/write scaling |
| Event sourcing applied | Natural combination with CQRS |

---

## Selection Guide

```
Is the domain logic complex?
├── No -> Are read requirements diverse?
│         ├── No -> Traditional CRUD
│         └── Yes -> CQRS (separate Query side only)
└── Yes -> Are read/write performance requirements different?
           ├── No -> CQRS (same DB)
           └── Yes -> CQRS (consider DB separation)
```

---

## Gradual Adoption

CQRS is not all-or-nothing. It can be introduced gradually:

### Stage 1: DTO Separation

Separate read DTOs while keeping the existing Repository.

### Stage 2: Add Query-Only Path

Introduce IQueryPort to handle complex read requirements.

### Stage 3: Command/Query Usecase Separation

Fully separate Command and Query with the Mediator pattern.

### Stage 4: Infrastructure Separation (Optional)

Add read DB replicas or cache layers as needed.

---

Let's look at the specific decision criteria for choosing between IRepository and IQueryPort depending on the situation.

-> [Appendix B: Repository vs Query Adapter Selection Guide](../B-repository-vs-query-adapter-guide/)
