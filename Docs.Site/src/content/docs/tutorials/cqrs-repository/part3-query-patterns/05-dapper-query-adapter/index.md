---
title: "Dapper Query Adapter"
---
## Overview

How do you directly control SQL in production? InMemoryQueryBase processes in-memory data with LINQ, but in actual services, SQL queries must be composed and executed directly against a database. DapperQueryBase<TEntity, TDto> is the common infrastructure for SQL-based Query Adapters. This chapter covers SQL generation patterns through SqlQueryBuilder without an actual DB.

---

## Learning Objectives

After completing this chapter, you will be able to:

1. Explain DapperQueryBase's Template Method pattern and the items subclasses must implement
2. Identify the SQL differences between Offset-based and Cursor-based pagination
3. Apply SQL Injection prevention using AllowedSortColumns
4. Understand the structural symmetry between InMemoryQueryBase and DapperQueryBase

---

## Core Concepts

### DapperQueryBase Subclass Implementation Items

If InMemoryQueryBase handled filtering/sorting/projection with C# code, DapperQueryBase performs the same roles with SQL. See what subclasses must implement.

| Abstract Member | Role | Example |
|----------------|------|---------|
| `SelectSql` | SELECT query body | `SELECT p.id, p.name FROM products p` |
| `CountSql` | COUNT query body | `SELECT COUNT(*) FROM products p` |
| `DefaultOrderBy` | Default ORDER BY clause | `p.name ASC` |
| `AllowedSortColumns` | Allowed sort column mapping | `{ "Name": "p.name" }` |
| `BuildWhereClause` | Specification -> SQL WHERE | `WHERE p.stock > 0` |

### Offset vs Cursor SQL Patterns

Compare how the two pagination approaches learned in the previous chapter are expressed in SQL.

```sql
-- Offset-based
SELECT * FROM products WHERE stock > 0 ORDER BY name ASC LIMIT 10 OFFSET 20

-- Cursor-based
SELECT * FROM products WHERE stock > 0 AND id > @CursorValue ORDER BY id LIMIT 10
```

Offset uses a LIMIT/OFFSET combination, while Cursor specifies the starting point with a WHERE condition.

### Role of AllowedSortColumns

Maps client sort field names (e.g., "Name") to actual SQL column names (e.g., "p.name"). Fields not in the mapping are rejected, preventing SQL Injection.

### InMemoryQueryBase vs DapperQueryBase

Both Query Adapters implement the same IQueryPort interface but differ in internal processing.

| Aspect | InMemoryQueryBase | DapperQueryBase |
|--------|-------------------|-----------------|
| Data source | ConcurrentDictionary (memory) | IDbConnection (SQL DB) |
| Filtering | Specification.IsSatisfiedBy (C#) | BuildWhereClause (SQL WHERE) |
| Sorting | SortSelector (C# function) | AllowedSortColumns (SQL ORDER BY) |
| Projection | LINQ Select (C#) | SelectSql (SQL SELECT) |
| Usage | Testing, prototyping | Production |

---

## Project Description

### SqlQueryBuilder

Simplifies the SQL composition patterns that DapperQueryBase performs internally. The actual DapperQueryBase uses Dapper's DynamicParameters and QueryMultipleAsync, but this chapter focuses on SQL string generation.

- `BuildSelectWithPagination`: Offset-based SELECT query (LIMIT/OFFSET)
- `BuildSelectWithCursor`: Cursor-based SELECT query (WHERE + LIMIT)
- `BuildCount`: COUNT query
- `BuildOrderBy`: ORDER BY clause with AllowedSortColumns mapping applied

---

## Summary at a Glance

| Item | Description |
|------|-------------|
| DapperQueryBase | Common base for SQL-based Query Adapters |
| SelectSql / CountSql | SQL body declared by subclasses |
| BuildWhereClause | Specification -> SQL WHERE conversion |
| AllowedSortColumns | Client field name -> SQL column name mapping (Injection prevention) |
| PaginationClause | LIMIT/OFFSET (supports DB dialects via override) |

---

## FAQ

### Q1: Why learn with SqlQueryBuilder instead of using DapperQueryBase directly?
**A**: DapperQueryBase requires an actual DB connection (IDbConnection). This chapter focuses on learning core SQL generation pattern concepts without a DB. Production implementations inherit from DapperQueryBase.

### Q2: How is Specification converted to SQL in BuildWhereClause?
**A**: Injecting DapperSpecTranslator handles the conversion automatically. Alternatively, subclasses can directly override BuildWhereClause to generate SQL by Specification type.

### Q3: Can it be used with SQL Server?
**A**: Override PaginationClause and CursorPaginationClause. The defaults are PostgreSQL/SQLite style (LIMIT/OFFSET), but they can be replaced with SQL Server's OFFSET FETCH or TOP.

---

The Query-side patterns are complete. Now it's time to integrate Command and Query into Usecases. How do you convert the FinT returned by Repository into an API response? In Part 4, we'll explore how to compose Command and Query through the Usecase layer.

-> [Chapter 1: Command Usecase](../../Part4-CQRS-Usecase-Integration/01-Command-Usecase/)
