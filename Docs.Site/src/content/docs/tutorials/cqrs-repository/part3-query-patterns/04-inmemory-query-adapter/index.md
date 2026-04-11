---
title: "InMemory Query Adapter"
---
## Overview

How do you verify a Query Adapter before integrating with Dapper? You want to confirm that Specification filtering, pagination, and sorting work correctly without spinning up a database. InMemoryQueryBase<TEntity, TDto> is the common infrastructure for Query Adapters on in-memory data sources, where subclasses only need to implement three things for Search, SearchByCursor, and Stream to work automatically.

---

## Learning Objectives

After completing this chapter, you will be able to:

1. Explain the Template Method pattern structure of InMemoryQueryBase
2. Filter using Specification.IsSatisfiedBy in GetProjectedItems
3. Apply field-to-sort-key mapping with SortSelector
4. Implement a test-purpose InMemory Query Adapter
5. Inject multiple stores and query across multiple Aggregates with LINQ Join

---

## Core Concepts

### Template Method Pattern

InMemoryQueryBase defines the algorithm for Search/SearchByCursor/Stream and delegates three things to subclasses. Subclasses only need to decide "what to filter, how to project, and what key to sort by."

| Abstract Member | Role |
|----------------|------|
| `DefaultSortField` | Default sort field when sorting is unspecified |
| `GetProjectedItems(spec)` | Specification filtering + DTO projection |
| `SortSelector(fieldName)` | Field name -> sort key selector function |

### Specification-Based Filtering

See the process of filtering with Specification and projecting to DTO in GetProjectedItems.

```csharp
protected override IEnumerable<ProductDto> GetProjectedItems(Specification<Product> spec) =>
    _store.Values
        .Where(p => spec.IsSatisfiedBy(p))  // Filter with Specification
        .Select(p => new ProductDto(...));   // Project to DTO
```

Since Specification encapsulates the filter conditions, the Query Adapter only needs to call `IsSatisfiedBy` without knowing the filter logic.

### InStockSpec Example

```csharp
public sealed class InStockSpec : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity) => entity.IsInStock;
}
```

Specification.All (identity element) satisfies all entities and is used for querying all data.

---

## Project Description

### InMemoryProductQuery

A concrete Query Adapter inheriting InMemoryQueryBase<Product, ProductDto>. Uses ConcurrentDictionary as internal storage and has an Add method for inserting test data.

### InStockSpec

A simple Specification that checks `Product.IsInStock`. Can be combined with `Specification<Product>.All`.

### Multi-Aggregate Query (LINQ Join)

In FAQ Q2, we answered "inject multiple stores and process with LINQ Join." Let's look at the actual implementation.

A Query Adapter that combines Order and Product information to return `OrderSummaryDto`.

```csharp
public sealed class InMemoryOrderSummaryQuery : InMemoryQueryBase<Order, OrderSummaryDto>
{
    private readonly ConcurrentDictionary<OrderId, Order> _orderStore = new();
    private readonly ConcurrentDictionary<ProductId, Product> _productStore;

    public InMemoryOrderSummaryQuery(ConcurrentDictionary<ProductId, Product> productStore)
    {
        _productStore = productStore;
    }

    protected override IEnumerable<OrderSummaryDto> GetProjectedItems(Specification<Order> spec) =>
        from order in _orderStore.Values
        where spec.IsSatisfiedBy(order)
        join product in _productStore.Values
            on order.ProductId equals product.Id
        select new OrderSummaryDto(
            order.Id.ToString(),
            product.Name,
            product.Category,
            order.Quantity,
            product.Price,
            order.TotalAmount);
}
```

The key is joining two stores with LINQ `join` in `GetProjectedItems`. After filtering Orders with `Specification<Order>`, join with Product to project both sides' fields into a single DTO.

This pattern naturally maps to SQL JOIN in Dapper:

```sql
SELECT o.Id, p.Name, p.Category, o.Quantity, p.Price, o.TotalAmount
FROM Orders o
JOIN Products p ON o.ProductId = p.Id
WHERE ...
```

---

## Summary at a Glance

| Item | Description |
|------|-------------|
| InMemoryQueryBase | Common base for InMemory Query Adapters |
| GetProjectedItems | Filtering + DTO projection (subclass implementation) |
| SortSelector | Field name -> sort key selector (subclass implementation) |
| DefaultSortField | Default sort field name (subclass implementation) |
| IsSatisfiedBy | Specification's core method -- entity evaluation |

---

## FAQ

### Q1: Is InMemoryQueryBase used in production?
**A**: Primarily used for testing and prototyping. In production, DapperQueryBase or EF Core-based implementations are used. Thanks to the IQueryPort interface, adapters can be easily swapped.

### Q2: What if JOIN is needed in GetProjectedItems?
**A**: Inject multiple stores via constructor and process with LINQ `join`. The `InMemoryOrderSummaryQuery` in the "Multi-Aggregate Query" section above is an example. After filtering Orders with `Specification<Order>` and joining with Product, InMemoryQueryBase's Search/SearchByCursor/Stream all work automatically.

### Q3: Why use ConcurrentDictionary?
**A**: For safety in multi-threaded environments. While a regular Dictionary suffices for unit tests, ConcurrentDictionary is safer for integration tests or parallel tests.

---

The InMemory implementation now enables fast testing without a DB. What if you want to directly control SQL in production? In the next chapter, we'll look at implementing SQL-based queries through the Dapper Query Adapter.

-> [Chapter 5: Dapper Query Adapter](../05-Dapper-Query-Adapter/)
