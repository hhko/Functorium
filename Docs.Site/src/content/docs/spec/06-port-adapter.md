---
title: "Port and Adapter Specification"
---

This is the API specification for Port interfaces, Adapter implementation base classes, the Specification pattern, DI registration, and source generator attributes provided by the Functorium framework. For design principles and implementation guides, see [Port Architecture and Definitions](../guides/adapter/12-ports) and [Adapter Implementation](../guides/adapter/13-adapters).

## Summary

### Key Types

| Type | Namespace | Description |
|------|-------------|------|
| `IObservablePort` | `Functorium.Abstractions.Observabilities` | 모든 Port/Adapter의 기반 인터페이스 (Observability category 제공) |
| `IRepository<TAggregate, TId>` | `Functorium.Domains.Repositories` | Aggregate Root 단위 Repository CRUD 계약 |
| `IQueryPort` | `Functorium.Applications.Queries` | 비제네릭 QueryPort 마커 인터페이스 |
| `IQueryPort<TEntity, TDto>` | `Functorium.Applications.Queries` | Specification 기반 조회 + 페이지네이션 계약 |
| `PageRequest` | `Functorium.Applications.Queries` | Offset 기반 페이지네이션 요청 |
| `PagedResult<T>` | `Functorium.Applications.Queries` | Offset 기반 페이지네이션 결과 |
| `CursorPageRequest` | `Functorium.Applications.Queries` | Keyset(Cursor) 기반 페이지네이션 요청 |
| `CursorPagedResult<T>` | `Functorium.Applications.Queries` | Keyset(Cursor) 기반 페이지네이션 결과 |
| `SortExpression` | `Functorium.Applications.Queries` | 다중 필드 정렬 표현 |
| `SortField` | `Functorium.Applications.Queries` | 정렬 필드 + 방향 쌍 |
| `SortDirection` | `Functorium.Applications.Queries` | 정렬 방향 SmartEnum (`Ascending`, `Descending`) |
| `Specification<T>` | `Functorium.Domains.Specifications` | Specification 패턴 추상 기반 클래스 |
| `ExpressionSpecification<T>` | `Functorium.Domains.Specifications` | Expression Tree 기반 Specification 추상 클래스 |
| `IExpressionSpec<T>` | `Functorium.Domains.Specifications` | Expression Tree 제공 능력을 나타내는 인터페이스 |
| `PropertyMap<TEntity, TModel>` | `Functorium.Domains.Specifications.Expressions` | Entity-Model 간 Expression 자동 변환 프로퍼티 매핑 |
| `SpecificationExpressionResolver` | `Functorium.Domains.Specifications.Expressions` | Specification에서 Expression Tree를 추출/합성하는 유틸리티 |
| `EfCoreRepositoryBase<TAggregate, TId, TModel>` | `Functorium.Adapters.Repositories` | EF Core Repository 공통 베이스 클래스 |
| `InMemoryRepositoryBase<TAggregate, TId>` | `Functorium.Adapters.Repositories` | InMemory Repository 공통 베이스 클래스 |
| `DapperQueryBase<TEntity, TDto>` | `Functorium.Adapters.Repositories` | Dapper 기반 QueryAdapter 공통 베이스 클래스 |
| `InMemoryQueryBase<TEntity, TDto>` | `Functorium.Adapters.Repositories` | InMemory QueryAdapter 공통 베이스 클래스 |
| `DapperSpecTranslator<TEntity>` | `Functorium.Adapters.Repositories` | Specification → SQL WHERE 절 번역 레지스트리 |
| `IHasStringId` | `Functorium.Adapters.Repositories` | EF Core 모델의 string Id 공통 인터페이스 |
| `ObservablePortRegistration` | `Functorium.Abstractions.Registrations` | Observable Port DI 등록 확장 메서드 |
| `OptionsConfigurator` | `Functorium.Adapters.Abstractions.Options` | FluentValidation 기반 옵션 유효성 검사 등록 |
| `GenerateObservablePortAttribute` | `Functorium.Adapters.SourceGenerators` | Observable 래퍼 자동 생성 어트리뷰트 |
| `ObservablePortIgnoreAttribute` | `Functorium.Adapters.SourceGenerators` | Observable 래퍼 생성 제외 어트리뷰트 |

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
│   └── IQueryPort<TEntity, TDto>   — Specification 기반 조회 (Application Layer)
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
    FinT<IO, TAggregate> Create(TAggregate aggregate);
    FinT<IO, TAggregate> GetById(TId id);
    FinT<IO, TAggregate> Update(TAggregate aggregate);
    FinT<IO, int> Delete(TId id);

    FinT<IO, Seq<TAggregate>> CreateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, Seq<TAggregate>> GetByIds(IReadOnlyList<TId> ids);
    FinT<IO, Seq<TAggregate>> UpdateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, int> DeleteRange(IReadOnlyList<TId> ids);
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
| `GetById(id)` | `FinT<IO, TAggregate>` | Retrieves an Aggregate by ID (`NotFound` error if absent) |
| `Update(aggregate)` | `FinT<IO, TAggregate>` | Updates a single Aggregate |
| `Delete(id)` | `FinT<IO, int>` | Deletes an Aggregate by ID (returns deleted count) |
| `CreateRange(aggregates)` | `FinT<IO, Seq<TAggregate>>` | Batch creation |
| `GetByIds(ids)` | `FinT<IO, Seq<TAggregate>>` | Batch retrieval (`PartialNotFound` error if some are missing) |
| `UpdateRange(aggregates)` | `FinT<IO, Seq<TAggregate>>` | Batch update |
| `DeleteRange(ids)` | `FinT<IO, int>` | Batch deletion (returns deleted count) |

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
    public sealed override bool IsSatisfiedBy(T entity);  // Expression 컴파일 + 캐싱
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

EF Core Repository의 공통 베이스 클래스입니다. 생성자에서 선언한 Include가 `ReadQuery()`를 통해 모든 읽기 쿼리에 자동 적용되어 N+1 문제를 구조적으로 방지합니다.

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
| `TAggregate` | `AggregateRoot<TId>` | Aggregate Root 타입 |
| `TId` | `struct, IEntityId<TId>` | Ulid 기반 EntityId 타입 |
| `TModel` | `class, IHasStringId` | EF Core 엔티티 모델 (string Id 필수) |

#### Constructors 파라미터

| Parameter | Type | Description |
|----------|------|------|
| `eventCollector` | `IDomainEventCollector` | 도메인 이벤트 수집기 |
| `applyIncludes` | `Func<IQueryable<TModel>, IQueryable<TModel>>?` | Navigation Property Include 선언 (N+1 방지). `null`이면 Include 없음 |
| `propertyMap` | `PropertyMap<TAggregate, TModel>?` | Specification → Model Expression 변환용 매핑. `BuildQuery`/`ExistsBySpec` 사용 시 필수 |

#### Required Subclass Implementation

| Member | Type | Description |
|------|------|------|
| `DbContext` | `DbContext` | EF Core DbContext (abstract property) |
| `DbSet` | `DbSet<TModel>` | 엔티티 모델의 DbSet (abstract property) |
| `ToDomain(model)` | `TAggregate` | Model → Domain 매핑 (abstract method) |
| `ToModel(aggregate)` | `TModel` | Domain → Model 매핑 (abstract method) |

#### Protected Infrastructure Members

| Member | Type | Description |
|------|------|------|
| `EventCollector` | `IDomainEventCollector` | 도메인 이벤트 수집기 |
| `PropertyMap` | `PropertyMap<TAggregate, TModel>?` | Specification → Model 프로퍼티 매핑 |
| `IdBatchSize` | `int` (virtual, 기본 `500`) | SQL IN 절 파라미터 한계 방지를 위한 배치 크기 |
| `ReadQuery()` | `IQueryable<TModel>` | Include가 자동 적용된 읽기 전용 쿼리 (`AsNoTracking`) |
| `ReadQueryIgnoringFilters()` | `IQueryable<TModel>` | Include + 글로벌 필터 무시 읽기 쿼리 (Soft Delete 조회용) |
| `BuildQuery(spec)` | `Fin<IQueryable<TModel>>` | Specification → Model Expression 쿼리 빌더 (PropertyMap 필수) |
| `ExistsBySpec(spec)` | `FinT<IO, bool>` | Specification 기반 존재 여부 확인 (PropertyMap 필수) |
| `ByIdPredicate(id)` | `Expression<Func<TModel, bool>>` | 단일 ID 매칭 Expression (virtual, `IHasStringId` 기반 기본 구현) |
| `ByIdsPredicate(ids)` | `Expression<Func<TModel, bool>>` | 복수 ID 매칭 Expression (virtual, `IHasStringId` 기반 기본 구현) |

#### Error Helpers

| Method | Description |
|--------|------|
| `NotFoundError(id)` | `AdapterErrorType.NotFound` 에러 생성. 실제 서브클래스 이름이 에러 코드에 포함 |
| `PartialNotFoundError(requestedIds, foundAggregates)` | `AdapterErrorType.PartialNotFound` 에러 생성. 누락 ID 목록 포함 |
| `NotConfiguredError(message)` | `AdapterErrorType.NotConfigured` 에러 생성 |
| `NotSupportedError(currentValue, message)` | `AdapterErrorType.NotSupported` 에러 생성 |

#### IHasStringId

EF Core 모델이 구현해야 하는 string Id 인터페이스입니다. `EfCoreRepositoryBase`의 `ByIdPredicate`/`ByIdsPredicate` 기본 구현을 제공합니다.

```csharp
namespace Functorium.Adapters.Repositories;

public interface IHasStringId
{
    string Id { get; set; }
}
```

### InMemoryRepositoryBase\<TAggregate, TId\>

InMemory Repository의 공통 베이스 클래스입니다. `ConcurrentDictionary` 기반으로 `IRepository` 전체 CRUD를 기본 구현합니다.

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
| `Store` | `ConcurrentDictionary<TId, TAggregate>` | 인메모리 저장소 (abstract property). 서브클래스에서 static 인스턴스 제공 |

#### Protected Members

| Member | Type | Description |
|------|------|------|
| `EventCollector` | `IDomainEventCollector` | 도메인 이벤트 수집기 |
| `RequestCategory` | `string` (virtual, 기본 `"Repository"`) | Observability category |

### DapperQueryBase\<TEntity, TDto\>

Dapper 기반 QueryAdapter의 공통 인프라입니다. 서브클래스는 SQL 선언과 WHERE 빌드만 담당합니다.

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
| `SelectSql` | `string` | SELECT 쿼리 (FROM + JOIN까지, WHERE 제외) |
| `CountSql` | `string` | COUNT 쿼리 (FROM + JOIN까지, WHERE 제외) |
| `DefaultOrderBy` | `string` | 기본 정렬 절 (예: `"p.created_at DESC"`) |
| `AllowedSortColumns` | `Dictionary<string, string>` | 허용된 정렬 컬럼 매핑 (DTO 필드명 → DB 컬럼명) |

#### Protected Methods

| Method | Description |
|--------|------|
| `BuildWhereClause(spec)` | Specification → SQL WHERE 절 변환. `DapperSpecTranslator` 주입 시 기본 구현 제공. 아니면 서브클래스에서 오버라이드 필수 |
| `PaginationClause` | DB 방언별 Offset 페이지네이션 절 (virtual, 기본 `"LIMIT @PageSize OFFSET @Skip"`) |
| `CursorPaginationClause` | DB 방언별 Keyset 페이지네이션 절 (virtual, 기본 `"LIMIT @PageSize"`) |
| `GetCursorValue(item, fieldName)` | DTO에서 커서 값 추출 (virtual, Reflection 기반 기본 구현 + 캐싱) |
| `Params(values)` | `DynamicParameters` 생성 헬퍼 (정적 메서드) |

#### Public Methods

| Method | Return Type | Description |
|--------|-----------|------|
| `Search(spec, page, sort)` | `FinT<IO, PagedResult<TDto>>` | Offset 기반 페이지네이션 검색 (COUNT + SELECT 멀티 쿼리) |
| `SearchByCursor(spec, cursor, sort)` | `FinT<IO, CursorPagedResult<TDto>>` | Keyset 기반 검색 (`PageSize + 1` 전략으로 HasMore 판단) |
| `Stream(spec, sort, ct)` | `IAsyncEnumerable<TDto>` | `QueryUnbufferedAsync`로 스트리밍 조회 (`DbConnection` 필수) |

### InMemoryQueryBase\<TEntity, TDto\>

InMemory 기반 QueryAdapter의 공통 인프라입니다. `DapperQueryBase`의 InMemory 대응 베이스 클래스입니다.

```csharp
namespace Functorium.Adapters.Repositories;

public abstract class InMemoryQueryBase<TEntity, TDto>
{
    // (생성자 파라미터 없음)
}
```

#### Required Subclass Implementation

| Member | Type | Description |
|------|------|------|
| `DefaultSortField` | `string` | 기본 정렬 필드명 |
| `GetProjectedItems(spec)` | `IEnumerable<TDto>` | 필터링 + DTO 프로젝션 (JOIN 로직 포함) |
| `SortSelector(fieldName)` | `Func<TDto, object>` | 정렬 키 셀렉터 (필드명 → 셀렉터 함수) |

#### Public Methods

| Method | Return Type | Description |
|--------|-----------|------|
| `Search(spec, page, sort)` | `FinT<IO, PagedResult<TDto>>` | Offset-based pagination search |
| `SearchByCursor(spec, cursor, sort)` | `FinT<IO, CursorPagedResult<TDto>>` | Keyset 기반 검색 |
| `Stream(spec, sort, ct)` | `IAsyncEnumerable<TDto>` | 메모리 내 스트리밍 조회 |

### DapperSpecTranslator\<TEntity\>

Specification을 SQL WHERE 절로 번역하는 레지스트리입니다. 엔티티 타입별로 한 번 구성하면 여러 Dapper 어댑터가 테이블 별칭만 달리하여 공유할 수 있습니다.

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
| `WhenAll(handler)` | `DapperSpecTranslator<TEntity>` | `IsAll` (항등원) Specification 핸들러 등록 (Fluent API) |
| `When<TSpec>(handler)` | `DapperSpecTranslator<TEntity>` | 특정 Specification 타입의 SQL 번역 핸들러 등록 (Fluent API) |
| `Translate(spec, alias)` | `(string Where, DynamicParameters Params)` | Specification을 SQL WHERE 절로 번역 |
| `Params(values)` | `DynamicParameters` | `DynamicParameters` 생성 헬퍼 (정적) |
| `Prefix(tableAlias)` | `string` | 테이블 별칭 접두사 반환 (예: `"p"` → `"p."`, `""` → `""`) |

---

## DI Registration (ObservablePortRegistration)

`IObservablePort` 구현체를 DI 컨테이너에 등록하는 `IServiceCollection` 확장 메서드 모음입니다. `ActivatorUtilities.CreateInstance`를 사용하여 구현 타입의 생성자에 `ActivitySource`, `ILogger`, `IMeterFactory`를 자동 주입합니다.

```csharp
namespace Functorium.Abstractions.Registrations;

public static class ObservablePortRegistration
{
    // 단일 인터페이스 등록
    public static IServiceCollection RegisterScopedObservablePort<TService, TImpl>(...);
    public static IServiceCollection RegisterTransientObservablePort<TService, TImpl>(...);
    public static IServiceCollection RegisterSingletonObservablePort<TService, TImpl>(...);

    // 복수 인터페이스 → 단일 구현체 등록 (For 접미사)
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
| `Register{Lifetime}ObservablePort<TService, TImpl>` | 단일 인터페이스를 하나의 구현체로 등록 |
| `Register{Lifetime}ObservablePortFor<T1, T2, TImpl>` | 2개 인터페이스를 하나의 구현체로 등록 |
| `Register{Lifetime}ObservablePortFor<T1, T2, T3, TImpl>` | 3개 인터페이스를 하나의 구현체로 등록 |
| `Register{Lifetime}ObservablePortFor<TImpl>(params Type[])` | N개 인터페이스를 하나의 구현체로 등록 (4개 이상) |

### Supported Lifetimes

| Lifetime | 인스턴스 공유 범위 |
|----------|-------------------|
| `Scoped` | HTTP 요청당 1개 인스턴스 |
| `Transient` | 요청될 때마다 새 인스턴스 |
| `Singleton` | 애플리케이션 전체에서 1개 인스턴스 |

### Generic Constraints

모든 서비스 인터페이스 타입 파라미터에는 `class, IObservablePort` 제약이 적용됩니다. `params Type[]` 오버로드는 런타임에 `IObservablePort` 구현 여부와 구현 클래스의 인터페이스 구현 여부를 검증합니다.

### For Suffix Behavior

`For` 접미사가 붙은 메서드는 구현체를 먼저 등록한 뒤, 각 서비스 인터페이스가 `GetRequiredService<TImplementation>()`으로 동일한 인스턴스를 참조하도록 등록합니다. 이를 통해 하나의 구현체를 여러 인터페이스로 resolve할 수 있습니다.

```csharp
// 사용 예시: IProductRepository와 IProductQuery를 동일한 Observable 구현체로 등록
services.RegisterScopedObservablePortFor<IProductRepository, IProductQuery, ProductObservable>();
```

---

## Options Configuration (OptionsConfigurator)

FluentValidation 기반 옵션 유효성 검사를 DI에 등록하는 유틸리티입니다.

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
| 1 | `IValidator<TOptions>` 등록 | `TValidator`를 Scoped로 DI 등록 |
| 2 | `BindConfiguration` | `appsettings.json`의 `configurationSectionName` 섹션을 `TOptions`에 바인딩 |
| 3 | FluentValidation 연결 | `IValidateOptions<TOptions>` 구현체를 통해 FluentValidation 검증 연결 |
| 4 | `ValidateOnStart` | 프로그램 시작 시 옵션 유효성 검사 실행 |
| 5 | `IStartupOptionsLogger` 자동 등록 | `TOptions`가 `IStartupOptionsLogger`를 구현하면 시작 시 옵션 값 로깅에 자동 등록 |

```csharp
// 사용 예시
services.RegisterConfigureOptions<DatabaseOptions, DatabaseOptionsValidator>("Database");
```

---

## Source Generator Attributes

### \[GenerateObservablePort\]

Adapter 클래스에 이 어트리뷰트를 적용하면 Observable 래퍼 클래스가 소스 생성기에 의해 자동으로 생성됩니다. 생성되는 Observable은 OpenTelemetry 기반의 Tracing, Logging, Metrics를 제공합니다.

```csharp
namespace Functorium.Adapters.SourceGenerators;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GenerateObservablePortAttribute : Attribute;
```

| 속성 | 값 | 설명 |
|------|----|------|
| `AttributeTargets` | `Class` | 클래스에만 적용 가능 |
| `AllowMultiple` | `false` | 한 클래스에 한 번만 적용 |
| `Inherited` | `false` | 파생 클래스에 상속되지 않음 |

**사전 조건:**
- 프로젝트에서 `Functorium.SourceGenerators` 패키지를 참조해야 합니다
- Adapter 클래스의 인터페이스 메서드에 `virtual` 키워드가 필요합니다 (Pipeline이 override)

```csharp
// 사용 예시
[GenerateObservablePort]
public class ProductRepositoryInMemory
    : InMemoryRepositoryBase<Product, ProductId>, IProductRepository
{
    // virtual 메서드들...
}
// → ProductRepositoryInMemoryObservable 클래스가 자동 생성됨
```

### \[ObservablePortIgnore\]

특정 메서드를 Observable 래퍼 생성에서 제외하는 어트리뷰트입니다. 관측성이 불필요한 헬퍼 메서드나 내부 메서드에 사용합니다.

```csharp
namespace Functorium.Adapters.SourceGenerators;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ObservablePortIgnoreAttribute : Attribute;
```

| 속성 | 값 | 설명 |
|------|----|------|
| `AttributeTargets` | `Method` | 메서드에만 적용 가능 |
| `AllowMultiple` | `false` | 한 메서드에 한 번만 적용 |
| `Inherited` | `false` | 파생 클래스에 상속되지 않음 |

```csharp
// 사용 예시
[GenerateObservablePort]
public class ProductRepository : InMemoryRepositoryBase<Product, ProductId>, IProductRepository
{
    [ObservablePortIgnore]
    public virtual FinT<IO, int> GetCount() => ...;  // Observable 래퍼에서 제외
}
```

---

## Related Documents

| Document | Description |
|------|------|
| [Port 아키텍처와 정의](../guides/adapter/12-ports) | Port 설계 원칙, 유형별 인터페이스 정의 패턴 가이드 |
| [Adapter 구현](../guides/adapter/13-adapters) | Repository, External API, Query Adapter 유형별 구현 가이드 |
| [Adapter Pipeline과 DI 등록](../guides/adapter/14a-adapter-pipeline-di) | Observable Pipeline 생성과 DI 등록 가이드 |
| [Adapter 테스트](../guides/adapter/14b-adapter-testing) | Adapter 단위/통합 테스트 가이드 |
| [엔티티와 애그리거트 사양](./01-entity-aggregate) | `AggregateRoot<TId>`, `IEntityId<TId>` API 사양 |
| [에러 시스템 사양](./04-error-system) | `AdapterErrorType` (NotFound, PartialNotFound 등) API 사양 |
| [관측 가능성 사양](./08-observability) | 3-Pillar 필드/태그 사양, Meter 정의 규칙 |
| [소스 생성기 사양](./10-source-generators) | `ObservablePortGenerator` 소스 생성기 상세 사양 |
