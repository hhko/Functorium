---
title: "Catalog Search"
---
## Overview

Admin lists need page numbers, mobile apps need infinite scroll, and batch jobs need to traverse all data. Among the three pagination approaches, **which should you choose?**

This chapter compares Offset, Cursor, and Stream pagination through the Catalog search domain. The same Specification performs all three query types, and you'll learn each approach's characteristics and suitable use scenarios.

---

## Learning Objectives

After completing this chapter, you will be able to:

1. Explain the differences among **Offset, Cursor, and Stream** pagination
2. Implement various filter conditions with **Specification composition**
3. Verify that the **same Specification** works across all Query methods
4. Select suitable scenarios based on each approach's **performance characteristics**

---

## Core Concepts

### Three Pagination Comparison Table

Refer to the table below when deciding which approach to choose.

| Characteristic | Search (Offset) | SearchByCursor (Keyset) | Stream |
|---------------|:---:|:---:|:---:|
| Total Count | O | X | X |
| Random Page Access | O | X | X |
| Deep Page Performance | O(N) | O(1) | N/A |
| Memory Usage | Per page | Per page | Per item |
| Suitable Scenario | UI lists | Infinite scroll | Batch processing |

### Specification Composition

All three query approaches use the same Specification. This is because filtering ("what to query") and pagination ("how to split results") are separated.

```csharp
// In stock AND price range 30,000~100,000
var spec = new InStockSpec() & new PriceRangeSpec(30_000m, 100_000m);

// Same Specification for all 3 query types
await query.Search(spec, page, sort);           // Offset
await query.SearchByCursor(spec, cursor, sort); // Cursor
query.Stream(spec, sort);                       // Stream
```

### Call Pattern for Each Approach

Each approach returns different result types and handles next-page processing differently. Choose the approach that fits your purpose.

```csharp
// 1. Offset: Provides TotalCount, access by page number
var paged = await query.Search(spec, new PageRequest(1, 20), sort);
// paged.TotalCount, paged.TotalPages, paged.HasNextPage

// 2. Cursor: Next page via HasMore + NextCursor
var cursor = await query.SearchByCursor(spec, new CursorPageRequest(pageSize: 20), sort);
// cursor.HasMore, cursor.NextCursor -> pass as after in next request

// 3. Stream: Consume one by one with await foreach
await foreach (var item in query.Stream(spec, sort, ct))
{
    Process(item); // Large-scale processing without memory burden
}
```

---

## Project Description

### File Structure

Check each file's role in the pagination comparison.

| File | Role |
|------|------|
| `ProductId.cs` | Ulid-based product identifier |
| `Product.cs` | Catalog product Aggregate |
| `ProductDto.cs` | Query-side DTO |
| `InStockSpec.cs` | Stock > 0 Specification |
| `PriceRangeSpec.cs` | Price range Specification |
| `InMemoryCatalogQuery.cs` | All 3 Query method implementations |

---

## Summary at a Glance

A summary of the pagination patterns used in this example.

| Concept | Implementation |
|---------|---------------|
| Offset pagination | `Search(spec, PageRequest, SortExpression)` -> `PagedResult<T>` |
| Cursor pagination | `SearchByCursor(spec, CursorPageRequest, SortExpression)` -> `CursorPagedResult<T>` |
| Async stream | `Stream(spec, SortExpression)` -> `IAsyncEnumerable<T>` |
| Specification composition | `new InStockSpec() & new PriceRangeSpec(min, max)` |
| Unified Query Adapter | `InMemoryCatalogQuery : InMemoryQueryBase<Product, ProductDto>` |

---

## FAQ

### Q1: Why provide both Offset and Cursor?
**A**: It depends on UI requirements. Offset suits admin lists (page numbers needed), Cursor suits mobile infinite scroll. `InMemoryQueryBase` implements both approaches, so the Usecase just chooses.

### Q2: When should Stream be used?
**A**: It's suitable for batch jobs that need to traverse all data, such as CSV export, data migration, and statistical aggregation. Since `IAsyncEnumerable<T>` yields one record at a time, the entire dataset isn't loaded into memory.

### Q3: Why does the same Specification work across all three approaches?
**A**: Specification addresses the concern of "what to filter," while pagination addresses the concern of "how to split results." Because these two concerns are separated, the same conditions can be freely combined with different approaches.

---

Through four domain examples, we've completed practical application of the CQRS pattern. The appendix provides additional reference materials including CQRS vs CRUD comparison, Repository vs Query adapter selection guide, and anti-patterns.

-> [Appendix A: CQRS vs Traditional CRUD](../../Appendix/A-cqrs-vs-crud/)
