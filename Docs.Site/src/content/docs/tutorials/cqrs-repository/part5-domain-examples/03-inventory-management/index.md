---
title: "Inventory Management"
---
## Overview

If you delete a product, do the related order histories disappear too? Physical deletion breaks referential integrity and makes past data unrecoverable. **Can we make it behave as deleted while preserving the data?**

This chapter implements the ISoftDeletable pattern and Cursor-based pagination through the Inventory domain. It demonstrates logical delete/restore mechanisms and filtering with ActiveProductSpec, and covers large-scale data processing patterns with Cursor pagination.

---

## Learning Objectives

After completing this chapter, you will be able to:

1. Implement logical deletion with the **ISoftDeletable** interface
2. Manage delete/restore states with **Delete() / Restore()** methods
3. Filter deleted items with **ActiveProductSpec**
4. Implement **Cursor-based pagination** (SearchByCursor)
5. Process large datasets with **Stream** async enumeration

---

## Core Concepts

### ISoftDeletable Pattern

Setting `DeletedAt` instead of physical DELETE preserves data while expressing deletion state. Restore is simple too.

```csharp
public sealed class Product : AggregateRoot<ProductId>, ISoftDeletable
{
    public Option<DateTime> DeletedAt { get; private set; }
    // ISoftDeletable.IsDeleted auto-computed from DeletedAt.IsSome

    public Fin<Unit> Delete()  { DeletedAt = DateTime.UtcNow; ... }
    public Fin<Unit> Restore() { DeletedAt = None; ... }
}
```

Querying only non-deleted products with `ActiveProductSpec` means application code doesn't need to be aware of deletion status.

### Cursor Pagination Flow

When querying large datasets page by page, the Cursor approach guarantees consistent performance regardless of page depth. The first page starts without a cursor, and subsequent pages pass `NextCursor` to fetch the next page.

```csharp
// Page 1: Start without cursor
var page1 = await query.SearchByCursor(spec,
    new CursorPageRequest(pageSize: 20),
    SortExpression.By("Name")).Run().RunAsync();

// Page 2: Pass NextCursor to after
if (page1.HasMore)
{
    var page2 = await query.SearchByCursor(spec,
        new CursorPageRequest(after: page1.NextCursor, pageSize: 20),
        SortExpression.By("Name")).Run().RunAsync();
}
```

---

## Project Description

### File Structure

Check each file's role in soft delete and pagination.

| File | Role |
|------|------|
| `ProductId.cs` | Ulid-based product identifier |
| `Product.cs` | Product Aggregate Root (ISoftDeletable) |
| `ProductDto.cs` | Query-side DTO |
| `ActiveProductSpec.cs` | Non-deleted product Specification |
| `IProductRepository.cs` | Repository interface |
| `InMemoryProductRepository.cs` | InMemory Repository implementation |
| `InMemoryProductQuery.cs` | InMemory Query Adapter (includes Cursor) |

---

## Summary at a Glance

A summary of the soft delete and pagination patterns used in this example.

| Concept | Implementation |
|---------|---------------|
| Soft Delete | `ISoftDeletable` -> `DeletedAt`, `IsDeleted` |
| Delete/Restore | `Delete()` / `Restore()` -> `Fin<Unit>` |
| Active filter | `ActiveProductSpec` (IsDeleted == false) |
| Cursor pagination | `SearchByCursor(spec, cursor, sort)` |
| Async stream | `Stream(spec, sort)` -> `IAsyncEnumerable<T>` |

---

## FAQ

### Q1: When should I choose Soft Delete vs Hard Delete?
**A**: It depends on business requirements. Use Soft Delete when audit trails are needed or accidental deletions must be recoverable. Use Hard Delete when complete deletion is required by privacy laws (GDPR, etc.).

### Q2: Why is Cursor pagination better than Offset?
**A**: Offset incurs O(N) cost for `SKIP N` on deep pages. Cursor directly specifies the starting point with a WHERE condition, providing O(1) performance regardless of page depth.

### Q3: Why use ActiveProductSpec in Query rather than Repository?
**A**: Repository focuses on Aggregate-level CRUD, while filtering/search is the Query side's responsibility. This is a core CQRS principle of separating Command and Query concerns.

---

Inventory management and soft delete are implemented. Finally, if the same data needs to be queried in three ways -- Offset, Cursor, and Stream -- which should you choose? The next chapter compares all three pagination approaches.

-> [Chapter 4: Catalog Search](../04-Catalog-Search/)
