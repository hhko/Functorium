---
title: "Port and Adapter Specification"
---

This is the API specification for Port interfaces, Adapter implementation base classes, the Specification pattern, DI registration, and source generator attributes provided by the Functorium framework. For design principles and implementation guides, see [Port Architecture and Definitions](../guides/adapter/12-ports) and [Adapter Implementation](../guides/adapter/13-adapters).

## Summary

### Key Types

| Type | Namespace | Description |
|------|-------------|------|
| `IObservablePort` | `Functorium.Abstractions.Observabilities` | Base interface for all Ports/Adapters (provides Observability category) |
| `IRepository<TAggregate, TId>` | `Functorium.Domains.Repositories` | Aggregate Root level Repository CRUD contract |
| `IQueryPort` | `Functorium.Applications.Queries` | Non-generic QueryPort marker interface |
| `IQueryPort<TEntity, TDto>` | `Functorium.Applications.Queries` | Specification-based query + pagination contract |
| `PageRequest` | `Functorium.Applications.Queries` | Offset-based pagination request |
| `PagedResult<T>` | `Functorium.Applications.Queries` | Offset-based pagination result |
| `CursorPageRequest` | `Functorium.Applications.Queries` | Keyset(Cursor)-based pagination request |
| `CursorPagedResult<T>` | `Functorium.Applications.Queries` | Keyset(Cursor)-based pagination result |
| `SortExpression` | `Functorium.Applications.Queries` | Multi-field sort expression |
| `SortField` | `Functorium.Applications.Queries` | Sort field + direction pair |
| `SortDirection` | `Functorium.Applications.Queries` | Sort direction SmartEnum (`Ascending`, `Descending`) |
| `Specification<T>` | `Functorium.Domains.Specifications` | Specification pattern abstract base class |
| `ExpressionSpecification<T>` | `Functorium.Domains.Specifications` | Expression Tree-based Specification abstract class |
| `IExpressionSpec<T>` | `Functorium.Domains.Specifications` | Interface indicating Expression Tree provision capability |
| `PropertyMap<TEntity, TModel>` | `Functorium.Domains.Specifications.Expressions` | Entity-Model Expression auto-conversion property mapping |
| `SpecificationExpressionResolver` | `Functorium.Domains.Specifications.Expressions` | Utility for extracting/composing Expression Trees from Specifications |
| `EfCoreRepositoryBase<TAggregate, TId, TModel>` | `Functorium.Adapters.Repositories` | EF Core Repository common base class |
| `InMemoryRepositoryBase<TAggregate, TId>` | `Functorium.Adapters.Repositories` | InMemory Repository common base class |
| `DapperQueryBase<TEntity, TDto>` | `Functorium.Adapters.Repositories` | Dapper-based QueryAdapter common base class |
| `InMemoryQueryBase<TEntity, TDto>` | `Functorium.Adapters.Repositories` | InMemory QueryAdapter common base class |
| `DapperSpecTranslator<TEntity>` | `Functorium.Adapters.Repositories` | Specification to SQL WHERE clause translation registry |
| `IHasStringId` | `Functorium.Adapters.Repositories` | Common string Id interface for EF Core models |
| `ObservablePortRegistration` | `Functorium.Abstractions.Registrations` | Observable Port DI registration extension methods |
| `OptionsConfigurator` | `Functorium.Adapters.Abstractions.Options` | FluentValidation-based options validation registration |
| `GenerateObservablePortAttribute` | `Functorium.Adapters.SourceGenerators` | Observable wrapper auto-generation attribute |
| `ObservablePortIgnoreAttribute` | `Functorium.Adapters.SourceGenerators` | Observable wrapper generation exclusion attribute |

---

## IObservablePort Interface

The base interface implemented by all Ports and Adapters. Identifies the request category in the Observability layer through the `RequestCategory` property.

```csharp
namespace Functorium.Abstractions.Observabilities;

public interface IObservablePort
{
    string RequestCategory { get; }
}
```

| Property | Type | Description |
|------|------|------|
| `RequestCategory` | `string` | Category used in observability logs/metrics (e.g., `"Repository"`, `"ExternalApi"`, `"Messaging"`) |

### Interface Hierarchy

```
IObservablePort
├── IRepository<TAggregate, TId>    — Aggregate Root CRUD (Domain Layer)
├── IQueryPort                      — Non-generic marker (Application Layer)
│   └── IQueryPort<TEntity, TDto>   — Specification-based query (Application Layer)
└── (User-defined Port)               — External API, Messaging, etc.
```

Inheriting `IObservablePort` enables the `[GenerateObservablePort]` source generator to auto-generate Tracing, Logging, and Metrics.

---

## Repository Contract (IRepository\<TAggregate, TId\>)

A persistence contract at the Aggregate Root level. Generic constraints enforce Aggregate Root-level persistence at compile time.

```csharp
namespace Functorium.Domains.Repositories;

public interface IRepository<TAggregate, TId> : IObservablePort
    where TAggregate : AggregateRoot<TId>
    where TId : struct, IEntityId<TId>
{
    // ── Write Single ──
    FinT<IO, TAggregate> Create(TAggregate aggregate);
    FinT<IO, TAggregate> Update(TAggregate aggregate);
    FinT<IO, int> Delete(TId id);

    // ── Write Batch ──
    FinT<IO, int> CreateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, int> UpdateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, int> DeleteRange(IReadOnlyList<TId> ids);

    // ── Read ──
    FinT<IO, TAggregate> GetById(TId id);
    FinT<IO, Seq<TAggregate>> GetByIds(IReadOnlyList<TId> ids);

    // ── Specification ──
    FinT<IO, bool> Exists(Specification<TAggregate> spec);
    FinT<IO, int> Count(Specification<TAggregate> spec);
    FinT<IO, int> DeleteBy(Specification<TAggregate> spec);
}
```

### Generic Constraints

| Type Parameter | Constraint | Description |
|---------------|------|------|
| `TAggregate` | `AggregateRoot<TId>` | Only Aggregate Roots can be Repository targets (prevents direct Entity persistence) |
| `TId` | `struct, IEntityId<TId>` | Ulid-based EntityId implementation type |

### Methods

| Method | Return Type | Description |
|--------|-----------|------|
| `Create(aggregate)` | `FinT<IO, TAggregate>` | Creates a single Aggregate |
| `Update(aggregate)` | `FinT<IO, TAggregate>` | Updates a single Aggregate |
| `Delete(id)` | `FinT<IO, int>` | Deletes an Aggregate by ID (returns deleted count) |
| `CreateRange(aggregates)` | `FinT<IO, int>` | Batch creation (returns affected count) |
| `UpdateRange(aggregates)` | `FinT<IO, int>` | Batch update (returns affected count) |
| `DeleteRange(ids)` | `FinT<IO, int>` | Batch deletion (returns deleted count) |
| `GetById(id)` | `FinT<IO, TAggregate>` | Retrieves an Aggregate by ID (`NotFound` error if absent) |
| `GetByIds(ids)` | `FinT<IO, Seq<TAggregate>>` | Batch retrieval (`PartialNotFound` error if some are missing) |
| `Exists(spec)` | `FinT<IO, bool>` | Specification-based existence check |
| `Count(spec)` | `FinT<IO, int>` | Specification-based count |
| `DeleteBy(spec)` | `FinT<IO, int>` | Specification-based deletion (returns deleted count) |

> All methods return `FinT<IO, T>`. Success is expressed as `Fin.Succ(value)`, and failure is expressed as domain/adapter errors.

---

## QueryPort Contract (IQueryPort\<TEntity, TDto\>)

A read-only port for Specification-based queries and direct DTO returns. Corresponds to the Read model in CQRS.

### IQueryPort (Non-generic Marker)

```csharp
namespace Functorium.Applications.Queries;

public interface IQueryPort : IObservablePort { }
```

A non-generic marker interface used for runtime type checking, DI scanning, and generic constraints.

### IQueryPort\<TEntity, TDto\>

```csharp
public interface IQueryPort<TEntity, TDto> : IQueryPort
{
    FinT<IO, PagedResult<TDto>> Search(
        Specification<TEntity> spec,
        PageRequest page,
        SortExpression sort);

    FinT<IO, CursorPagedResult<TDto>> SearchByCursor(
        Specification<TEntity> spec,
        CursorPageRequest cursor,
        SortExpression sort);

    IAsyncEnumerable<TDto> Stream(
        Specification<TEntity> spec,
        SortExpression sort,
        CancellationToken cancellationToken = default);
}
```

| Method | Return Type | Description |
|--------|-----------|------|
| `Search(spec, page, sort)` | `FinT<IO, PagedResult<TDto>>` | Offset-based pagination search |
| `SearchByCursor(spec, cursor, sort)` | `FinT<IO, CursorPagedResult<TDto>>` | Keyset(Cursor)-based pagination search. O(1) performance for deep pages |
| `Stream(spec, sort, ct)` | `IAsyncEnumerable<TDto>` | Streaming query for large data. Yields without loading everything into memory |

### Generic Parameters

| Type Parameter | Description |
|---------------|------|
| `TEntity` | Domain entity type (Specification target) |
| `TDto` | Return DTO type (directly returned to presentation layer) |

---

## Pagination Types

### PageRequest

An offset-based pagination request. This is an Application-level query concern, not a domain invariant.

```csharp
namespace Functorium.Applications.Queries;

public sealed record PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 10_000;

    public int Page { get; }
    public int PageSize { get; }
    public int Skip => (Page - 1) * PageSize;

    public PageRequest(int page = 1, int pageSize = DefaultPageSize);
}
```

| Property/Constant | Type | Description |
|-----------|------|------|
| `DefaultPageSize` | `int` | Default page size (`20`) |
| `MaxPageSize` | `int` | Maximum page size (`10,000`) |
| `Page` | `int` | Current page number (corrected to `1` if less than 1) |
| `PageSize` | `int` | Page size (corrected to `DefaultPageSize` if less than 1, capped at `MaxPageSize`) |
| `Skip` | `int` | Number of items to skip (computed property: `(Page - 1) * PageSize`) |

### PagedResult\<T\>

An offset-based pagination result container.

```csharp
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
```

| Property | Type | Description |
|------|------|------|
| `Items` | `IReadOnlyList<T>` | List of items for the current page |
| `TotalCount` | `int` | Total number of items |
| `Page` | `int` | Current page number |
| `PageSize` | `int` | Page size |
| `TotalPages` | `int` | Total number of pages (computed property) |
| `HasPreviousPage` | `bool` | Whether a previous page exists |
| `HasNextPage` | `bool` | Whether a next page exists |

### CursorPageRequest

A keyset(cursor)-based pagination request. Provides O(1) performance for deep pages compared to offset-based pagination.

```csharp
public sealed record CursorPageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 10_000;

    public string? After { get; }
    public string? Before { get; }
    public int PageSize { get; }

    public CursorPageRequest(
        string? after = null,
        string? before = null,
        int pageSize = DefaultPageSize);
}
```

| Property/Constant | Type | Description |
|-----------|------|------|
| `DefaultPageSize` | `int` | Default page size (`20`) |
| `MaxPageSize` | `int` | Maximum page size (`10,000`) |
| `After` | `string?` | Retrieve items after this cursor (forward pagination) |
| `Before` | `string?` | Retrieve items before this cursor (backward pagination) |
| `PageSize` | `int` | Page size (corrected to `DefaultPageSize` if less than 1, capped at `MaxPageSize`) |

### CursorPagedResult\<T\>

A keyset(cursor)-based pagination result container.

```csharp
public sealed record CursorPagedResult<T>(
    IReadOnlyList<T> Items,
    string? NextCursor,
    string? PrevCursor,
    bool HasMore);
```

| Property | Type | Description |
|------|------|------|
| `Items` | `IReadOnlyList<T>` | List of items for the current page |
| `NextCursor` | `string?` | Next page cursor (`null` if no more items) |
| `PrevCursor` | `string?` | Previous page cursor |
| `HasMore` | `bool` | Whether more pages exist |

### SortExpression

A multi-field sort expression. Combines sort conditions via Fluent API.

```csharp
public sealed class SortExpression
{
    public Seq<SortField> Fields { get; }
    public bool IsEmpty { get; }

    public static SortExpression Empty { get; }
    public static SortExpression By(string fieldName);
    public static SortExpression By(string fieldName, SortDirection direction);
    public SortExpression ThenBy(string fieldName);
    public SortExpression ThenBy(string fieldName, SortDirection direction);
}
```

| Member | Type | Description |
|------|------|------|
| `Fields` | `Seq<SortField>` | List of sort fields (applied in order) |
| `IsEmpty` | `bool` | Whether sort conditions are empty |
| `Empty` | `SortExpression` | No sorting (static property) |
| `By(fieldName)` | `SortExpression` | Creates single field ascending sort (static factory) |
| `By(fieldName, direction)` | `SortExpression` | Creates single field + direction sort (static factory) |
| `ThenBy(fieldName)` | `SortExpression` | Chains additional sort field (ascending) |
| `ThenBy(fieldName, direction)` | `SortExpression` | Chains additional sort field + direction |

### SortField

A pair of sort field and direction.

```csharp
public sealed record SortField(string FieldName, SortDirection Direction);
```

| Property | Type | Description |
|------|------|------|
| `FieldName` | `string` | Field name to sort by |
| `Direction` | `SortDirection` | Sort direction |

### SortDirection

A SmartEnum representing sort direction.

```csharp
public sealed class SortDirection : SmartEnum<SortDirection, string>
{
    public static readonly SortDirection Ascending;   // Value: "asc"
    public static readonly SortDirection Descending;  // Value: "desc"

    public static SortDirection Parse(string? value);
}
```

| Member | Value | Description |
|------|-------|------|
| `Ascending` | `"asc"` | Ascending order |
| `Descending` | `"desc"` | Descending order |
| `Parse(value)` | - | Parses `"asc"`/`"desc"` case-insensitively. Returns `Ascending` for `null`/empty string |

---

## Specification Pattern

### Specification\<T\> (Abstract Base Class)

An abstract base class that encapsulates domain conditions and supports And/Or/Not composition.

```csharp
namespace Functorium.Domains.Specifications;

public abstract class Specification<T>
{
    public static Specification<T> All { get; }
    public virtual bool IsAll => false;

    public abstract bool IsSatisfiedBy(T entity);

    public Specification<T> And(Specification<T> other);
    public Specification<T> Or(Specification<T> other);
    public Specification<T> Not();

    public static Specification<T> operator &(Specification<T> left, Specification<T> right);
    public static Specification<T> operator |(Specification<T> left, Specification<T> right);
    public static Specification<T> operator !(Specification<T> spec);
}
```

| Member | Type | Description |
|------|------|------|
| `All` | `Specification<T>` | Specification satisfied by all entities (Null Object). `All & X = X` |
| `IsAll` | `bool` | Whether this Specification is the identity element (`All`) |
| `IsSatisfiedBy(entity)` | `bool` | Checks whether the entity satisfies the condition |
| `And(other)` | `Specification<T>` | AND composition |
| `Or(other)` | `Specification<T>` | OR composition |
| `Not()` | `Specification<T>` | NOT negation |

**Operator overloading:**

| Operator | Equivalent Method | Description |
|--------|-----------|------|
| `&` | `And()` | AND composition. Includes `All` identity optimization (`All & X = X`) |
| `\|` | `Or()` | OR composition |
| `!` | `Not()` | NOT negation |

### ExpressionSpecification\<T\>

An Expression Tree-based Specification abstract class. Implementing `ToExpression()` automatically provides `IsSatisfiedBy()`.

```csharp
public abstract class ExpressionSpecification<T> : Specification<T>, IExpressionSpec<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();
    public sealed override bool IsSatisfiedBy(T entity);  // Expression compile + caching
}
```

| Member | Description |
|------|------|
| `ToExpression()` | Returns the condition as `Expression<Func<T, bool>>` (must be implemented by subclass) |
| `IsSatisfiedBy(entity)` | Compiles and evaluates the Expression. Compiled delegate is cached (`sealed`) |

### IExpressionSpec\<T\>

An interface indicating that a Specification can provide an Expression Tree. Used by LINQ providers such as EF Core for automatic SQL translation.

```csharp
namespace Functorium.Domains.Specifications;

public interface IExpressionSpec<T>
{
    Expression<Func<T, bool>> ToExpression();
}
```

### PropertyMap\<TEntity, TModel\>

Property mapping for auto-converting Entity Expression to Model Expression. Used in `EfCoreRepositoryBase`'s `BuildQuery()` method for Specification to SQL conversion.

```csharp
namespace Functorium.Domains.Specifications.Expressions;

public sealed class PropertyMap<TEntity, TModel>
{
    public PropertyMap<TEntity, TModel> Map<TValue, TModelValue>(
        Expression<Func<TEntity, TValue>> entityProp,
        Expression<Func<TModel, TModelValue>> modelProp);

    public string? TranslateFieldName(string entityFieldName);
    public Expression<Func<TModel, bool>> Translate(Expression<Func<TEntity, bool>> expression);
}
```

| Method | Return Type | Description |
|--------|-----------|------|
| `Map(entityProp, modelProp)` | `PropertyMap<TEntity, TModel>` | Registers Entity-Model property mapping. Fluent API |
| `TranslateFieldName(name)` | `string?` | Translates Entity field name to Model field name (`null` if no mapping) |
| `Translate(expression)` | `Expression<Func<TModel, bool>>` | Converts Entity Expression to Model Expression |

**Supported Entity property expressions:**

| Form | Example |
|------|------|
| Direct member access | `p => p.Name` |
| Type conversion | `p => (decimal)p.Price` |
| `ToString()` call | `p => p.Id.ToString()` |

### SpecificationExpressionResolver

A utility that extracts Expression Trees from Specifications and recursively composes And/Or/Not combinations.

```csharp
namespace Functorium.Domains.Specifications.Expressions;

public static class SpecificationExpressionResolver
{
    public static Expression<Func<T, bool>>? TryResolve<T>(Specification<T> spec);
}
```

| Method | Return Type | Description |
|--------|-----------|------|
| `TryResolve(spec)` | `Expression<Func<T, bool>>?` | Extracts Expression from Specification. Direct extraction for `IExpressionSpec` implementations, recursive composition for And/Or/Not combinations. Returns `null` if unsupported |

---

## Implementation Base Classes

### EfCoreRepositoryBase\<TAggregate, TId, TModel\>

Common base class for EF Core Repositories. Includes declared in the constructor are automatically applied to all read queries through `ReadQuery()`, structurally preventing N+1 problems.

```csharp
namespace Functorium.Adapters.Repositories;

public abstract class EfCoreRepositoryBase<TAggregate, TId, TModel>
    : IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : struct, IEntityId<TId>
    where TModel : class, IHasStringId
{
    protected EfCoreRepositoryBase(
        IDomainEventCollector eventCollector,
        Func<IQueryable<TModel>, IQueryable<TModel>>? applyIncludes = null,
        PropertyMap<TAggregate, TModel>? propertyMap = null);
}
```

#### Generic Constraints

| Type Parameter | Constraint | Description |
|---------------|------|------|
| `TAggregate` | `AggregateRoot<TId>` | Aggregate Root type |
| `TId` | `struct, IEntityId<TId>` | Ulid-based EntityId type |
| `TModel` | `class, IHasStringId` | EF Core entity model (string Id required) |

#### Constructor Parameters

| Parameter | Type | Description |
|----------|------|------|
| `eventCollector` | `IDomainEventCollector` | Domain event collector |
| `applyIncludes` | `Func<IQueryable<TModel>, IQueryable<TModel>>?` | Navigation Property Include declaration (N+1 prevention). No Includes if `null` |
| `propertyMap` | `PropertyMap<TAggregate, TModel>?` | Mapping for Specification to Model Expression conversion. Required when using `BuildQuery`/`Exists` |

#### Required Subclass Implementation

| Member | Type | Description |
|------|------|------|
| `DbContext` | `DbContext` | EF Core DbContext (abstract property) |
| `DbSet` | `DbSet<TModel>` | Entity model's DbSet (abstract property) |
| `ToDomain(model)` | `TAggregate` | Model to Domain mapping (abstract method) |
| `ToModel(aggregate)` | `TModel` | Domain to Model mapping (abstract method) |
| `BuildSetters(builder, model)` | `void` | Registers which columns to update via `UpdateSettersBuilder<TModel>` (abstract method). Used by `Update` and `UpdateRange` for `ExecuteUpdateAsync` |

#### Protected Infrastructure Members

| Member | Type | Description |
|------|------|------|
| `EventCollector` | `IDomainEventCollector` | Domain event collector |
| `PropertyMap` | `PropertyMap<TAggregate, TModel>?` | Specification to Model property mapping |
| `IdBatchSize` | `int` (virtual, default `500`) | Batch size for preventing SQL IN clause parameter limit |
| `ReadQuery()` | `IQueryable<TModel>` | Read-only query with Includes auto-applied (`AsNoTracking`) |
| `ReadQueryIgnoringFilters()` | `IQueryable<TModel>` | Read query with Includes + global filter bypass (for Soft Delete queries) |
| `BuildQuery(spec)` | `Fin<IQueryable<TModel>>` | Specification to Model Expression query builder (PropertyMap required) |
| `Exists(spec)` | `FinT<IO, bool>` | Specification-based existence check (PropertyMap required, public) |
| `UpdateBy(spec, setters)` | `FinT<IO, int>` | Specification-based bulk update via `ExecuteUpdateAsync` (protected) |
| `ByIdPredicate(id)` | `Expression<Func<TModel, bool>>` | Single ID matching Expression (virtual, default `IHasStringId`-based implementation) |
| `ByIdsPredicate(ids)` | `Expression<Func<TModel, bool>>` | Multiple ID matching Expression (virtual, default `IHasStringId`-based implementation) |

#### Error Helpers

| Method | Description |
|--------|------|
| `NotFoundError(id)` | Creates `AdapterErrorType.NotFound` error. Actual subclass name is included in the error code |
| `PartialNotFoundError(requestedIds, foundAggregates)` | Creates `AdapterErrorType.PartialNotFound` error. Includes list of missing IDs |
| `NotConfiguredError(message)` | Creates `AdapterErrorType.NotConfigured` error |
| `NotSupportedError(currentValue, message)` | Creates `AdapterErrorType.NotSupported` error |

#### Update Strategy

`Update` and `UpdateRange` use EF Core's `ExecuteUpdateAsync` for server-side updates instead of `FindAsync` + `SetValues`. The subclass-implemented `BuildSetters` method declares which columns to set via `UpdateSettersBuilder<TModel>`, and the base class translates this into a single `ExecuteUpdateAsync` call. This avoids loading entities into memory for updates.

`UpdateBy(spec, setters)` is a protected method that combines a Specification-based WHERE clause with an `Action<UpdateSettersBuilder<TModel>>` setter callback for bulk conditional updates.

#### \[GenerateSetters\] Source Generator

The `[GenerateSetters]` attribute can be applied to EF Core model classes to auto-generate the `UpdateSettersBuilder<TModel>` fluent API. The source generator creates a `Set{PropertyName}(value)` method for each settable property on the model, eliminating boilerplate in `BuildSetters` implementations.

```csharp
// Usage example
[GenerateSetters]
public class ProductModel : IHasStringId
{
    public string Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}
// -> UpdateSettersBuilder<ProductModel> with SetName(value), SetPrice(value), etc. is auto-generated
```

#### IHasStringId

String Id interface that EF Core models must implement. Provides default implementation for `EfCoreRepositoryBase`'s `ByIdPredicate`/`ByIdsPredicate`.

```csharp
namespace Functorium.Adapters.Repositories;

public interface IHasStringId
{
    string Id { get; set; }
}
```

### InMemoryRepositoryBase\<TAggregate, TId\>

Common base class for InMemory Repositories. Provides default implementation of full `IRepository` CRUD based on `ConcurrentDictionary`.

```csharp
namespace Functorium.Adapters.Repositories;

public abstract class InMemoryRepositoryBase<TAggregate, TId>
    : IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : struct, IEntityId<TId>
{
    protected InMemoryRepositoryBase(IDomainEventCollector eventCollector);
}
```

#### Required Subclass Implementation

| Member | Type | Description |
|------|------|------|
| `Store` | `ConcurrentDictionary<TId, TAggregate>` | In-memory store (abstract property). Subclass provides a static instance |

#### Protected Members

| Member | Type | Description |
|------|------|------|
| `EventCollector` | `IDomainEventCollector` | Domain event collector |
| `RequestCategory` | `string` (virtual, default `"Repository"`) | Observability category |

### DapperQueryBase\<TEntity, TDto\>

Common infrastructure for Dapper-based QueryAdapters. Subclasses are responsible only for SQL declaration and WHERE building.

```csharp
namespace Functorium.Adapters.Repositories;

public abstract class DapperQueryBase<TEntity, TDto>
{
    protected DapperQueryBase(IDbConnection connection);
    protected DapperQueryBase(
        IDbConnection connection,
        DapperSpecTranslator<TEntity> translator,
        string tableAlias = "");
}
```

#### Required Subclass Implementation

| Member | Type | Description |
|------|------|------|
| `SelectSql` | `string` | SELECT query (up to FROM + JOIN, excluding WHERE) |
| `CountSql` | `string` | COUNT query (up to FROM + JOIN, excluding WHERE) |
| `DefaultOrderBy` | `string` | Default ORDER BY clause (e.g., `"p.created_at DESC"`) |
| `AllowedSortColumns` | `Dictionary<string, string>` | Allowed sort column mapping (DTO field name to DB column name) |

#### Protected Methods

| Method | Description |
|--------|------|
| `BuildWhereClause(spec)` | Specification to SQL WHERE clause conversion. Default implementation provided when `DapperSpecTranslator` is injected. Otherwise, subclass override is required |
| `PaginationClause` | DB dialect-specific Offset pagination clause (virtual, default `"LIMIT @PageSize OFFSET @Skip"`) |
| `CursorPaginationClause` | DB dialect-specific Keyset pagination clause (virtual, default `"LIMIT @PageSize"`) |
| `GetCursorValue(item, fieldName)` | Extract cursor value from DTO (virtual, Reflection-based default implementation + caching) |
| `Params(values)` | `DynamicParameters` creation helper (static method) |

#### Public Methods

| Method | Return Type | Description |
|--------|-----------|------|
| `Search(spec, page, sort)` | `FinT<IO, PagedResult<TDto>>` | Offset-based pagination search (COUNT + SELECT multi-query) |
| `SearchByCursor(spec, cursor, sort)` | `FinT<IO, CursorPagedResult<TDto>>` | Keyset-based search (`PageSize + 1` strategy for HasMore determination) |
| `Stream(spec, sort, ct)` | `IAsyncEnumerable<TDto>` | Streaming query via `QueryUnbufferedAsync` (`DbConnection` required) |

### InMemoryQueryBase\<TEntity, TDto\>

Common infrastructure for InMemory-based QueryAdapters. InMemory counterpart base class for `DapperQueryBase`.

```csharp
namespace Functorium.Adapters.Repositories;

public abstract class InMemoryQueryBase<TEntity, TDto>
{
    // (No constructor parameters)
}
```

#### Required Subclass Implementation

| Member | Type | Description |
|------|------|------|
| `DefaultSortField` | `string` | Default sort field name |
| `GetProjectedItems(spec)` | `IEnumerable<TDto>` | Filtering + DTO projection (including JOIN logic) |
| `SortSelector(fieldName)` | `Func<TDto, object>` | Sort key selector (field name to selector function) |

#### Public Methods

| Method | Return Type | Description |
|--------|-----------|------|
| `Search(spec, page, sort)` | `FinT<IO, PagedResult<TDto>>` | Offset-based pagination search |
| `SearchByCursor(spec, cursor, sort)` | `FinT<IO, CursorPagedResult<TDto>>` | Keyset-based search |
| `Stream(spec, sort, ct)` | `IAsyncEnumerable<TDto>` | In-memory streaming query |

### DapperSpecTranslator\<TEntity\>

A registry that translates Specifications to SQL WHERE clauses. Once configured per entity type, multiple Dapper adapters can share it with different table aliases.

```csharp
namespace Functorium.Adapters.Repositories;

public sealed class DapperSpecTranslator<TEntity>
{
    public DapperSpecTranslator<TEntity> WhenAll(
        Func<string, (string Where, DynamicParameters Params)> handler);

    public DapperSpecTranslator<TEntity> When<TSpec>(
        Func<TSpec, string, (string Where, DynamicParameters Params)> handler)
        where TSpec : Specification<TEntity>;

    public (string Where, DynamicParameters Params) Translate(
        Specification<TEntity> spec, string tableAlias = "");

    public static DynamicParameters Params(params (string Name, object Value)[] values);
    public static string Prefix(string tableAlias);
}
```

| Method | Return Type | Description |
|--------|-----------|------|
| `WhenAll(handler)` | `DapperSpecTranslator<TEntity>` | Register `IsAll` (identity) Specification handler (Fluent API) |
| `When<TSpec>(handler)` | `DapperSpecTranslator<TEntity>` | Register SQL translation handler for a specific Specification type (Fluent API) |
| `Translate(spec, alias)` | `(string Where, DynamicParameters Params)` | Translates Specification to SQL WHERE clause |
| `Params(values)` | `DynamicParameters` | `DynamicParameters` creation helper (static) |
| `Prefix(tableAlias)` | `string` | Returns table alias prefix (e.g., `"p"` to `"p."`, `""` to `""`) |

---

## DI Registration (ObservablePortRegistration)

A collection of `IServiceCollection` extension methods for registering `IObservablePort` implementations in the DI container. Uses `ActivatorUtilities.CreateInstance` to auto-inject `ActivitySource`, `ILogger`, and `IMeterFactory` into implementation type constructors.

```csharp
namespace Functorium.Abstractions.Registrations;

public static class ObservablePortRegistration
{
    // Single interface registration
    public static IServiceCollection RegisterScopedObservablePort<TService, TImpl>(...);
    public static IServiceCollection RegisterTransientObservablePort<TService, TImpl>(...);
    public static IServiceCollection RegisterSingletonObservablePort<TService, TImpl>(...);

    // Multiple interfaces to single implementation registration (For suffix)
    public static IServiceCollection RegisterScopedObservablePortFor<T1, T2, TImpl>(...);
    public static IServiceCollection RegisterScopedObservablePortFor<T1, T2, T3, TImpl>(...);
    public static IServiceCollection RegisterScopedObservablePortFor<TImpl>(
        ..., params Type[] serviceTypes);

    public static IServiceCollection RegisterTransientObservablePortFor<T1, T2, TImpl>(...);
    public static IServiceCollection RegisterTransientObservablePortFor<T1, T2, T3, TImpl>(...);
    public static IServiceCollection RegisterTransientObservablePortFor<TImpl>(
        ..., params Type[] serviceTypes);

    public static IServiceCollection RegisterSingletonObservablePortFor<T1, T2, TImpl>(...);
    public static IServiceCollection RegisterSingletonObservablePortFor<T1, T2, T3, TImpl>(...);
    public static IServiceCollection RegisterSingletonObservablePortFor<TImpl>(
        ..., params Type[] serviceTypes);
}
```

### Naming Conventions

| Pattern | Description |
|------|------|
| `Register{Lifetime}ObservablePort<TService, TImpl>` | Register a single interface with one implementation |
| `Register{Lifetime}ObservablePortFor<T1, T2, TImpl>` | Register 2 interfaces with one implementation |
| `Register{Lifetime}ObservablePortFor<T1, T2, T3, TImpl>` | Register 3 interfaces with one implementation |
| `Register{Lifetime}ObservablePortFor<TImpl>(params Type[])` | Register N interfaces with one implementation (4 or more) |

### Supported Lifetimes

| Lifetime | Instance sharing scope |
|----------|-------------------|
| `Scoped` | One instance per HTTP request |
| `Transient` | New instance per request |
| `Singleton` | One instance for the entire application |

### Generic Constraints

All service interface type parameters have the `class, IObservablePort` constraint. The `params Type[]` overload validates `IObservablePort` implementation and interface implementation of the implementation class at runtime.

### For Suffix Behavior

Methods with the `For` suffix first register the implementation, then register each service interface to reference the same instance via `GetRequiredService<TImplementation>()`. This enables resolving a single implementation through multiple interfaces.

```csharp
// Usage example: Register IProductRepository and IProductQuery with the same Observable implementation
services.RegisterScopedObservablePortFor<IProductRepository, IProductQuery, ProductObservable>();
```

---

## Options Configuration (OptionsConfigurator)

A utility for registering FluentValidation-based options validation in DI.

```csharp
namespace Functorium.Adapters.Abstractions.Options;

public static class OptionsConfigurator
{
    public static OptionsBuilder<TOptions> RegisterConfigureOptions<TOptions, TValidator>(
        this IServiceCollection services,
        string configurationSectionName)
        where TOptions : class
        where TValidator : class, IValidator<TOptions>;
}
```

### RegisterConfigureOptions Behavior

| Order | Behavior | Description |
|------|------|------|
| 1 | `IValidator<TOptions>` registration | Registers `TValidator` as Scoped in DI |
| 2 | `BindConfiguration` | Binds the `configurationSectionName` section of `appsettings.json` to `TOptions` |
| 3 | FluentValidation connection | Connects FluentValidation validation through `IValidateOptions<TOptions>` implementation |
| 4 | `ValidateOnStart` | Run options validation at program startup |
| 5 | `IStartupOptionsLogger` auto-registration | If `TOptions` implements `IStartupOptionsLogger`, auto-registers for options value logging at startup |

```csharp
// Usage example
services.RegisterConfigureOptions<DatabaseOptions, DatabaseOptionsValidator>("Database");
```

---

## Source Generator Attributes

### \[GenerateObservablePort\]

Applying this attribute to an Adapter class causes the source generator to automatically generate an Observable wrapper class. The generated Observable provides OpenTelemetry-based Tracing, Logging, and Metrics.

```csharp
namespace Functorium.Adapters.SourceGenerators;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GenerateObservablePortAttribute : Attribute;
```

| Property | Value | Description |
|------|----|------|
| `AttributeTargets` | `Class` | Can only be applied to classes |
| `AllowMultiple` | `false` | Applied only once per class |
| `Inherited` | `false` | Not inherited by derived classes |

**Prerequisites:**
- The project must reference the `Functorium.SourceGenerators` package
- Interface methods in the Adapter class require the `virtual` keyword (Pipeline overrides)

```csharp
// Usage example
[GenerateObservablePort]
public class ProductRepositoryInMemory
    : InMemoryRepositoryBase<Product, ProductId>, IProductRepository
{
    // virtual methods...
}
// -> ProductRepositoryInMemoryObservable class is auto-generated
```

### \[ObservablePortIgnore\]

An attribute that excludes a specific method from Observable wrapper generation. Used for helper methods or internal methods where observability is unnecessary.

```csharp
namespace Functorium.Adapters.SourceGenerators;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ObservablePortIgnoreAttribute : Attribute;
```

| Property | Value | Description |
|------|----|------|
| `AttributeTargets` | `Method` | Can only be applied to methods |
| `AllowMultiple` | `false` | Applied only once per method |
| `Inherited` | `false` | Not inherited by derived classes |

```csharp
// Usage example
[GenerateObservablePort]
public class ProductRepository : InMemoryRepositoryBase<Product, ProductId>, IProductRepository
{
    [ObservablePortIgnore]
    public virtual FinT<IO, int> GetCount() => ...;  // Excluded from Observable wrapper
}
```

---

## Related Documents

| Document | Description |
|------|------|
| [Port Architecture and Definitions](../guides/adapter/12-ports) | Port design principles, interface definition pattern guide by type |
| [Adapter Implementation](../guides/adapter/13-adapters) | Repository, External API, Query Adapter implementation guide by type |
| [Adapter Pipeline and DI Registration](../guides/adapter/14a-adapter-pipeline-di) | Observable Pipeline creation and DI registration guide |
| [Adapter Testing](../guides/adapter/14b-adapter-testing) | Adapter unit/integration testing guide |
| [Entity and Aggregate Specification](../01-entity-aggregate) | `AggregateRoot<TId>`, `IEntityId<TId>` API specification |
| [Error System Specification](../04-error-system) | `AdapterErrorType` (NotFound, PartialNotFound, etc.) API specification |
| [Observability Specification](../08-observability) | 3-Pillar field/tag specification, Meter definition rules |
| [Source Generator Specification](../10-source-generators) | `ObservablePortGenerator` source generator detailed specification |
