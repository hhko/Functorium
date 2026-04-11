---
title: "Pagination and Sorting"
---
## Overview

What happens when you return 100,000 products at once? The client runs out of memory, the network becomes a bottleneck, and users have to scroll endlessly. Data must be sliced into appropriate sizes for delivery. This chapter covers the differences between Offset-based and Cursor (Keyset)-based pagination, and multi-field sorting using SortExpression.

---

## Learning Objectives

After completing this chapter, you will be able to:

1. Compose Offset-based pagination with PageRequest and PagedResult
2. Compose Keyset-based pagination with CursorPageRequest and CursorPagedResult
3. Express multi-field sorting with SortExpression's fluent API
4. Evaluate the trade-offs of Offset and Cursor pagination to choose the appropriate approach

---

## Core Concepts

### "Why Is This Needed?" -- Returning All Data at Once

Without pagination, returning all data is fine when there are 1,000 records but response time grows to tens of seconds at 100,000 records. There are two approaches: **Offset** navigates by page number, and **Cursor** navigates by "starting from after this item." Let's examine the pros and cons of each.

### Offset-Based Pagination

The Offset approach requests data as "from position N, get M items."

```
PageRequest(page: 2, pageSize: 10) -> OFFSET 10 LIMIT 10
```

| Type | Properties |
|------|-----------|
| `PageRequest` | Page, PageSize, Skip |
| `PagedResult<T>` | Items, TotalCount, TotalPages, HasPreviousPage, HasNextPage |

- Advantage: Can jump to a specific page directly, page numbers can be displayed in UI
- Disadvantage: Performance degrades on deep pages (slower as OFFSET grows)

### Cursor (Keyset)-Based Pagination

The Cursor approach requests data as "from after this cursor, get M items."

```
CursorPageRequest(after: "cursor-value", pageSize: 10) -> WHERE id > 'cursor-value' LIMIT 10
```

| Type | Properties |
|------|-----------|
| `CursorPageRequest` | After, Before, PageSize |
| `CursorPagedResult<T>` | Items, NextCursor, PrevCursor, HasMore |

- Advantage: O(1) performance even on deep pages, suitable for real-time data
- Disadvantage: Cannot jump to a specific page directly, only "next/previous" navigation

### SortExpression

Sorting must be controlled alongside pagination. SortExpression expresses multi-field sorting with a fluent API.

```csharp
// Single field sorting
SortExpression.By("Name")

// Multi-field sorting (fluent API)
SortExpression.By("Category").ThenBy("Price", SortDirection.Descending)

// Empty sort (uses default sorting)
SortExpression.Empty
```

### Offset vs Cursor Comparison

Which approach to choose depends on data characteristics and UI requirements.

| Criterion | Offset | Cursor |
|-----------|--------|--------|
| Deep Page Performance | O(N) | O(1) |
| Jump to Specific Page | Possible | Not possible |
| Real-time Data | May have duplicates/gaps | Stable |
| SQL | LIMIT/OFFSET | WHERE + LIMIT |
| UI | Page numbers | "Load more" button |

---

## Project Description

### PaginationDemo

Provides helper methods for creating PagedResult and CursorPagedResult, and sorting methods applying SortExpression. Simplifies the behavior of InMemoryQueryBase for demonstration.

---

## Summary at a Glance

| Item | Description |
|------|-------------|
| PageRequest | Offset-based pagination request (Page, PageSize) |
| PagedResult | Offset-based result (TotalCount, TotalPages, HasNext/Prev) |
| CursorPageRequest | Keyset-based pagination request (After, Before, PageSize) |
| CursorPagedResult | Keyset-based result (NextCursor, PrevCursor, HasMore) |
| SortExpression | Multi-field sort expression (By/ThenBy fluent API) |

---

## FAQ

### Q1: Should I use Offset or Cursor?
**A**: For most admin pages (boards, product lists), Offset is suitable. For infinite scroll, real-time feeds, and large datasets, Cursor is suitable. Functorium's IQueryPort supports both.

### Q2: Is there a maximum value for PageSize in PageRequest?
**A**: It's limited to MaxPageSize (10,000). Requesting a larger value is automatically adjusted to MaxPageSize.

### Q3: When is SortExpression.Empty used?
**A**: When the client doesn't specify sorting, passing Empty applies the Query Adapter's DefaultSortField.

---

We've defined pagination and sorting. Now we need to actually implement these interfaces. Can we test before integrating with Dapper? In the next chapter, we'll look at quickly validating with InMemory Query Adapter without a DB.

-> [Chapter 4: InMemory Query Adapter](../04-InMemory-Query-Adapter/)
