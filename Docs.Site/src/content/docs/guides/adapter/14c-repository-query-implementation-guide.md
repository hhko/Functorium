---
title: "Repository & Query Adapter Implementation Guide"
---

This document is a practical guide that walks through the step-by-step implementation procedures for Repository (Write Side) and Query Adapter (Read Side) for new Aggregates.

## Quick Navigation

| Task | Section |
|------|------|
| Repository implementation checklist | [§2. Repository Implementation Guide (Write Side)](#2-repository-implementation-guide-write-side) |
| EfCore Repository base class | [§2.3 EfCoreRepositoryBase Implementation Pattern](#23-efcorerepositorybase-implementation-pattern) |
| InMemory Repository implementation | [§2.4 InMemoryRepositoryBase Implementation Pattern](#24-inmemoryrepositorybase-implementation-pattern) |
| Query Adapter (Dapper) implementation | [§3. Query Adapter Implementation Guide (Read Side)](#3-query-adapter-implementation-guide-read-side) |
| Cursor pagination | [§3.5 Cursor Pagination](#35-cursor-pagination) |
| DI registration | [§6. DI Registration Pattern](#6-di-registration-pattern) |

## Introduction

Writing Repositories and Query Adapters from scratch every time a new Aggregate is added is repetitive and error-prone:

- How do you set up the EF Core Repository constructor 3-parameter pattern?
- How do you handle pagination and sorting in the Dapper Query Adapter?
- How do you branch DI registration between InMemory and EF Core implementations?

This document presents methods to reduce such repetition and errors through base class and checklist-based implementation patterns.

### What You Will Learn

1. Complete checklist and base class patterns for Repository (Write Side) implementation
2. Dapper/InMemory patterns for both sides of Query Adapter (Read Side) implementation
3. Integration structure of UnitOfWork and domain event publishing

### Prerequisites

- [Port Definition Guide](../12-ports) — Port interface design principles
- [Adapter Implementation Guide](../13-adapters) — Basic Adapter implementation patterns
- [Pipeline and DI](../14a-adapter-pipeline-di) — Pipeline generation and DI registration

> **Write by Aggregate unit, Read by DTO projection.** This CQRS separation principle drives all design decisions in Repository and Query Adapter implementation.

---

## 1. Overview

This document describes the step-by-step procedures for implementing **Repository** (Write Side) and **Query Adapter** (Read Side) for new Aggregates.

### CQRS Structure

```
┌─────────────────────────────────────────────────┐
│  Application Layer                              │
│                                                 │
│  Command (Write)              Query (Read)      │
│  ┌──────────────┐        ┌──────────────────┐   │
│  │ IRepository  │        │ IQueryPort       │   │
│  │ <TAgg, TId>  │        │ <TEntity, TDto>  │   │
│  └──────┬───────┘        └────────┬─────────┘   │
│         │                         │             │
├─────────┼─────────────────────────┼─────────────┤
│  Adapter Layer                    │             │
│         │                         │             │
│  ┌──────┴───────┐        ┌────────┴─────────┐   │
│  │RepoEfCore    │        │QueryDapper       │   │
│  │RepoInMemory  │        │QueryInMemory     │   │
│  └──────────────┘        └──────────────────┘   │
└─────────────────────────────────────────────────┘
```

- **Repository** — Aggregate Root-level CRUD. Read/write through domain objects
- **Query Adapter** — Direct DTO projection. DB to DTO without Aggregate reconstruction

### Base Class Hierarchy

The following table summarizes the base classes and target interfaces for the Write/Read sides.

| Role | Base Class | Implementation Target |
|------|-------------|----------|
| Write (EF Core) | `EfCoreRepositoryBase<TAgg, TId, TModel>` | `IRepository<TAgg, TId>` |
| Write (InMemory) | `InMemoryRepositoryBase<TAgg, TId>` | `IRepository<TAgg, TId>` |
| Read (Dapper) | `DapperQueryBase<TEntity, TDto>` | `IQueryPort<TEntity, TDto>` |
| Read (InMemory) | `InMemoryQueryBase<TEntity, TDto>` | `IQueryPort<TEntity, TDto>` |

---

## 2. Repository Implementation Guide (Write Side)

### 2.1 Implementation Checklist

This is a step-by-step checklist when adding a new Aggregate `Xxx`. Detailed implementation for each step is explained in the following sections.

| # | Layer | Task | File |
|---|------|------|------|
| 1 | Domain | Define `IXxxRepository` interface | `Domain/AggregateRoots/Xxxs/IXxxRepository.cs` |
| 2 | Adapter | Implement `XxxModel` + `IHasStringId` | `Repositories/Xxxs/Xxx.Model.cs` |
| 3 | Adapter | `IEntityTypeConfiguration<XxxModel>` | `Repositories/Xxxs/Xxx.Configuration.cs` |
| 4 | Adapter | `XxxMapper` (ToModel/ToDomain) | `Repositories/Xxxs/Xxx.Mapper.cs` |
| 5 | Adapter | Implement `XxxRepositoryEfCore` | `Repositories/Xxxs/Repositories/XxxRepositoryEfCore.cs` |
| 6 | Adapter | Implement `XxxRepositoryInMemory` | `Repositories/Xxxs/Repositories/XxxRepositoryInMemory.cs` |
| 7 | Adapter | DI Registration | `Abstractions/Registrations/AdapterPersistenceRegistration.cs` |

### 2.2 Domain Interface

If only basic CRUD is needed, simply inherit `IRepository<TAgg, TId>` as-is:

```csharp
// Minimal implementation — no additional methods
public interface ITagRepository : IRepository<Tag, TagId>;

// When additional methods are needed
public interface IProductRepository : IRepository<Product, ProductId>
{
    FinT<IO, Product> GetByIdIncludingDeleted(ProductId id);
}
```

The 13 default methods provided by `IRepository<TAgg, TId>`:
- **Write (Single)**: `Create`, `Update`, `Delete`
- **Write (Batch)**: `CreateRange`, `UpdateRange`, `DeleteRange`
- **Read**: `GetById`, `GetByIds`
- **Specification**: `Exists`, `Count`, `FindAllSatisfying`, `FindFirstSatisfying`, `DeleteBy`

### 2.3 EfCoreRepositoryBase Implementation Pattern

#### Constructor 3-Parameter Pattern

Note that `eventCollector` is required, while `applyIncludes` and `propertyMap` are only passed when needed.

```csharp
protected EfCoreRepositoryBase(
    IDomainEventCollector eventCollector,                                 // Required: domain event collection
    Func<IQueryable<TModel>, IQueryable<TModel>>? applyIncludes = null,   // Navigation Property Include
    PropertyMap<TAggregate, TModel>? propertyMap = null)                  // Specification → SQL translation
```

- **eventCollector** — Always required
- **applyIncludes** — Declare when Navigation Properties exist. Automatically applied to `ReadQuery()` to prevent N+1
- **propertyMap** — Required when using `Exists(Specification)` or `BuildQuery`

#### Minimal Implementation (TagRepository)

The simplest form with no Navigation Properties and no Specification search:

```csharp
[GenerateObservablePort]
public class TagRepositoryEfCore
    : EfCoreRepositoryBase<Tag, TagId, TagModel>, ITagRepository
{
    private readonly LayeredArchDbContext _dbContext;

    public TagRepositoryEfCore(LayeredArchDbContext dbContext, IDomainEventCollector eventCollector)
        : base(eventCollector)                          // both applyIncludes and propertyMap omitted
        => _dbContext = dbContext;

    protected override DbContext DbContext => _dbContext;
    protected override DbSet<TagModel> DbSet => _dbContext.Tags;
    protected override Tag ToDomain(TagModel model) => model.ToDomain();
    protected override TagModel ToModel(Tag tag) => tag.ToModel();
}
```

There are 4 required members that subclasses must implement:
- `DbContext` — EF Core DbContext (used for TrackedMerge Update)
- `DbSet` — EF Core DbSet
- `ToDomain()` — Model → Domain mapping
- `ToModel()` — Domain → Model mapping

#### Implementation with Navigation Property (OrderRepository)

```csharp
[GenerateObservablePort]
public class OrderRepositoryEfCore
    : EfCoreRepositoryBase<Order, OrderId, OrderModel>, IOrderRepository
{
    private readonly LayeredArchDbContext _dbContext;

    public OrderRepositoryEfCore(LayeredArchDbContext dbContext, IDomainEventCollector eventCollector)
        : base(eventCollector, q => q.Include(o => o.OrderLines))   // Include declaration
        => _dbContext = dbContext;

    protected override DbContext DbContext => _dbContext;
    protected override DbSet<OrderModel> DbSet => _dbContext.Orders;
    protected override Order ToDomain(OrderModel model) => model.ToDomain();
    protected override OrderModel ToModel(Order order) => order.ToModel();
}
```

#### Full Implementation (ProductRepository) -- Include + PropertyMap + Custom Methods

```csharp
[GenerateObservablePort]
public class ProductRepositoryEfCore
    : EfCoreRepositoryBase<Product, ProductId, ProductModel>, IProductRepository
{
    private readonly LayeredArchDbContext _dbContext;

    public ProductRepositoryEfCore(LayeredArchDbContext dbContext, IDomainEventCollector eventCollector)
        : base(eventCollector,
               q => q.Include(p => p.ProductTags),                // Navigation Include
               new PropertyMap<Product, ProductModel>()           // Specification mapping
                   .Map(p => (decimal)p.Price, m => m.Price)
                   .Map(p => (string)p.Name, m => m.Name)
                   .Map(p => p.Id.ToString(), m => m.Id))
        => _dbContext = dbContext;

    protected override DbContext DbContext => _dbContext;
    protected override DbSet<ProductModel> DbSet => _dbContext.Products;
    protected override Product ToDomain(ProductModel model) => model.ToDomain();
    protected override ProductModel ToModel(Product p) => p.ToModel();

    // Exists is inherited from base class — no subclass override needed

    // Soft Delete override (see section 5.1)
    // ...
}
```

#### PropertyMap Declaration Rules

Maps Domain Value Objects to Model primitive types:

```csharp
new PropertyMap<Customer, CustomerModel>()
    .Map(c => (string)c.Email, m => m.Email)                // Email(VO) → string
    .Map(c => (string)c.Name, m => m.Name)                  // CustomerName(VO) → string
    .Map(c => (decimal)c.CreditLimit, m => m.CreditLimit)   // Money(VO) → decimal
    .Map(c => c.Id.ToString(), m => m.Id)                   // CustomerId → string
```

### 2.4 InMemoryRepositoryBase Implementation Pattern

#### Basic Pattern -- `static ConcurrentDictionary` + `Store` Property

```csharp
[GenerateObservablePort]
public class TagRepositoryInMemory
    : InMemoryRepositoryBase<Tag, TagId>, ITagRepository
{
    internal static readonly ConcurrentDictionary<TagId, Tag> Tags = new();
    protected override ConcurrentDictionary<TagId, Tag> Store => Tags;

    public TagRepositoryInMemory(IDomainEventCollector eventCollector)
        : base(eventCollector) { }
}
```

Core rules:
- `ConcurrentDictionary` must be declared as **`static`** (data sharing across DI Scopes)
- Declared as `internal static` to allow access from Query Adapters in the same assembly
- Since the base class implements all 13 `IRepository` methods (8 CRUD + 5 Specification), only override additional methods

#### Implementation with Additional Methods (InventoryRepository)

```csharp
[GenerateObservablePort]
public class InventoryRepositoryInMemory
    : InMemoryRepositoryBase<Inventory, InventoryId>, IInventoryRepository
{
    internal static readonly ConcurrentDictionary<InventoryId, Inventory> Inventories = new();
    protected override ConcurrentDictionary<InventoryId, Inventory> Store => Inventories;

    public InventoryRepositoryInMemory(IDomainEventCollector eventCollector)
        : base(eventCollector) { }

    public virtual FinT<IO, Inventory> GetByProductId(ProductId productId)
    {
        return IO.lift(() =>
        {
            var inventory = Inventories.Values.FirstOrDefault(i =>
                i.ProductId.Equals(productId));

            if (inventory is not null)
                return Fin.Succ(inventory);

            return AdapterError.For<InventoryRepositoryInMemory>(
                new NotFound(), productId.ToString(),
                $"Inventory not found for product ID '{productId}'");
        });
    }

    public virtual FinT<IO, bool> Exists(Specification<Inventory> spec)
    {
        return IO.lift(() =>
        {
            bool exists = Inventories.Values.Any(i => spec.IsSatisfiedBy(i));
            return Fin.Succ(exists);
        });
    }
}
```

### 2.5 Mapper Implementation Pattern

Implements `ToModel()` / `ToDomain()` as extension methods.

#### Simple Mapper (TagMapper)

A pattern that uses `CreateFromValidated` in `ToDomain()` to prevent re-validation when restoring from DB.

```csharp
internal static class TagMapper
{
    public static TagModel ToModel(this Tag tag) => new()
    {
        Id = tag.Id.ToString(),
        Name = tag.Name,
        CreatedAt = tag.CreatedAt,
        UpdatedAt = tag.UpdatedAt.ToNullable()
    };

    public static Tag ToDomain(this TagModel model) =>
        Tag.CreateFromValidated(                        // Prevent re-validation
            TagId.Create(model.Id),
            TagName.CreateFromValidated(model.Name),    // Using CreateFromValidated
            model.CreatedAt,
            Optional(model.UpdatedAt));
}
```

Core rules:
- Use **`CreateFromValidated()`** in `ToDomain` -- data read from DB is already validated
- `Option<DateTime>` to `DateTime?` conversion: `.ToNullable()`
- `DateTime?` to `Option<DateTime>` conversion: `Optional(model.UpdatedAt)`

#### Navigation Property Mapping (ProductMapper)

```csharp
internal static class ProductMapper
{
    public static ProductModel ToModel(this Product product)
    {
        var productId = product.Id.ToString();
        return new()
        {
            Id = productId,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt.ToNullable(),
            DeletedAt = product.DeletedAt.ToNullable(),
            DeletedBy = product.DeletedBy.Match(Some: v => (string?)v, None: () => null),
            ProductTags = product.TagIds.Select(tagId => new ProductTagModel
            {
                ProductId = productId,
                TagId = tagId.ToString()
            }).ToList()
        };
    }

    public static Product ToDomain(this ProductModel model)
    {
        var tagIds = model.ProductTags.Select(pt => TagId.Create(pt.TagId));

        return Product.CreateFromValidated(
            ProductId.Create(model.Id),
            ProductName.CreateFromValidated(model.Name),
            ProductDescription.CreateFromValidated(model.Description),
            Money.CreateFromValidated(model.Price),
            tagIds,
            model.CreatedAt,
            Optional(model.UpdatedAt),
            Optional(model.DeletedAt),
            Optional(model.DeletedBy));
    }
}
```

#### Mapping with Child Entities (OrderMapper)

```csharp
internal static class OrderMapper
{
    public static OrderModel ToModel(this Order order)
    {
        var orderId = order.Id.ToString();
        return new()
        {
            Id = orderId,
            CustomerId = order.CustomerId.ToString(),
            TotalAmount = order.TotalAmount,
            ShippingAddress = order.ShippingAddress,
            Status = order.Status,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt.ToNullable(),
            OrderLines = order.OrderLines.Select(l => l.ToModel(orderId)).ToList()  // Pass parent Id
        };
    }

    public static Order ToDomain(this OrderModel model) =>
        Order.CreateFromValidated(
            OrderId.Create(model.Id),
            CustomerId.Create(model.CustomerId),
            model.OrderLines.Select(l => l.ToDomain()),
            Money.CreateFromValidated(model.TotalAmount),
            ShippingAddress.CreateFromValidated(model.ShippingAddress),
            OrderStatus.CreateFromValidated(model.Status),
            model.CreatedAt,
            Optional(model.UpdatedAt));
}
```

### 2.6 EF Core Model & Configuration Pattern

#### Model

```csharp
public class ProductModel : IHasStringId
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    // Soft Delete only
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    // Navigation Property
    public List<ProductTagModel> ProductTags { get; set; } = [];
}
```

Rules:
- Must implement `IHasStringId` -- the base class's `ByIdPredicate` depends on this interface
- `Id` is `string` type, maxLength 26 (Ulid)

#### Configuration

```csharp
public class ProductConfiguration : IEntityTypeConfiguration<ProductModel>
{
    public void Configure(EntityTypeBuilder<ProductModel> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasMaxLength(26);       // Ulid

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Price).HasPrecision(18, 4); // decimal precision

        // Soft Delete: Global Query Filter
        builder.HasQueryFilter(p => p.DeletedAt == null);

        // Navigation Property + Cascade Delete
        builder.HasMany(p => p.ProductTags)
            .WithOne()
            .HasForeignKey(pt => pt.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

Configuration rules:
- Id: `HasMaxLength(26)` (Ulid length)
- decimal: `HasPrecision(18, 4)`
- Soft Delete: `HasQueryFilter(p => p.DeletedAt == null)`
- Navigation: Cascade Delete configuration

### 2.7 CRUD Symmetry Analysis

Compares the architecture paths between single/bulk CRUD operations of `EfCoreRepositoryBase`.

#### Full Comparison Table

| Operation | Category | Change Tracker | Domain Conversion | Event Tracking | ReadQuery | Execution Method |
|------|------|:-:|:-:|:-:|:-:|------|
| **Create** | Single | O | O (ToModel) | O (Track) | - | `DbSet.Add` |
| **CreateRange** | Bulk | O | O (ToModel) | O (TrackRange) | - | `DbSet.AddRange` |
| **GetById** | Single | X | O (ToDomain) | - | O | `AsNoTracking` → `FirstOrDefault` |
| **GetByIds** | Bulk | X | O (ToDomain) | - | O | `AsNoTracking` → `Where` → `ToList` |
| **Update** | Single | **X** | O (ToModel) | **X** | - | `ExecuteUpdateAsync (Change Tracker bypass)` |
| **UpdateRange** | Bulk | **X** | O (ToModel) | **X** | - | `ExecuteUpdateAsync (Change Tracker bypass)` |
| **Delete** | Single | **X** | **X** | **X** | - | `Where(pred).ExecuteDeleteAsync` |
| **DeleteRange** | Bulk | **X** | **X** | **X** | - | `Where(pred).ExecuteDeleteAsync` |

> **Note**: O in the ReadQuery column means `ReadQuery()` (AsNoTracking + Include automatically applied) is used.
>
> **Note**: GetByIds returns `PartialNotFoundError` when the number of requested IDs differs from the number of results.

#### CRUD 4-Pair Symmetry Summary

| Operation | Single vs Bulk | Reason |
|------|:---:|------|
| Create | Symmetric | `DbSet.Add` vs `DbSet.AddRange` (only API is plural) |
| Read | Symmetric | `FirstOrDefault` vs `Where().ToList()` (only condition is singular/plural) |
| Update | Symmetric | `ExecuteUpdateAsync` (Change Tracker bypass: UPDATE only changed columns) |
| Delete | Symmetric | `Where(pred).ExecuteDeleteAsync` (same path, only condition is singular/plural) |

**Asymmetry occurs only in Soft Delete overrides.**
Bulk SQL operations (`ExecuteUpdateAsync`) and domain event tracking are structurally incompatible:

1. Domain events arise from state transitions of domain objects
2. Bulk SQL does not create domain objects
3. Loading N items individually eliminates the performance benefit of bulk operations

This is an intentional performance trade-off. See section 5.1 for Soft Delete code.

#### Subclass Override Status

| Repository | CRUD Override | Custom Methods |
|-----------|:-:|------|
| `ProductRepositoryEfCore` | `Delete`, `DeleteRange` | `GetByIdIncludingDeleted`, `Exists` |
| `OrderRepositoryEfCore` | None | None |
| `CustomerRepositoryEfCore` | None | `Exists` |
| `InventoryRepositoryEfCore` | None | `GetByProductId`, `Exists` |
| `TagRepositoryEfCore` | None | None |

Only Product overrides CRUD. The reason is the domain requirement of Soft Delete.

#### applyIncludes Declaration Status

| Repository | applyIncludes | Navigation Property |
|-----------|--------------|---------------------|
| `ProductRepositoryEfCore` | `q => q.Include(p => p.ProductTags)` | `ProductTags` |
| `OrderRepositoryEfCore` | `q => q.Include(o => o.OrderLines)` | `OrderLines` |
| `CustomerRepositoryEfCore` | `null` (default) | None |
| `InventoryRepositoryEfCore` | `null` (default) | None |
| `TagRepositoryEfCore` | `null` (default) | None |

If you have completed the Repository (Write Side) implementation, proceed to the Query Adapter (Read Side) implementation for read-only queries.

---

## 3. Query Adapter Implementation Guide (Read Side)

### 3.1 Query Classification

The following table summarizes the base classes and interfaces by Query type.

| Type | Base Class | Interface | Example |
|------|-------------|-----------|------|
| Search (paging) | `DapperQueryBase` / `InMemoryQueryBase` | `IQueryPort<TEntity, TDto>` | `IProductQuery` |
| Single-item query | Direct implementation | `IQueryPort` (non-generic) | `IProductDetailQuery` |
| JOIN search | `DapperQueryBase` / `InMemoryQueryBase` | `IQueryPort<TEntity, TDto>` | `IProductWithStockQuery` |
| LEFT JOIN search | `DapperQueryBase` / `InMemoryQueryBase` | `IQueryPort<TEntity, TDto>` | `IProductWithOptionalStockQuery` |
| GROUP BY aggregation | `DapperQueryBase` / `InMemoryQueryBase` | `IQueryPort<TEntity, TDto>` | `ICustomerOrderSummaryQuery` |
| Complex JOIN | Direct implementation | `IQueryPort` (non-generic) | `ICustomerOrdersQuery` |

**`IQueryPort<TEntity, TDto>`** -- Provides `Search` + `SearchByCursor` + `Stream`
**`IQueryPort`** (non-generic marker) -- For custom signatures such as single-item queries

### 3.2 DapperQueryBase Implementation Pattern

#### Required Abstract Members

There are 4 abstract members that subclasses must implement. They handle only SQL declaration, while execution is handled by the base class.

```csharp
protected abstract string SelectSql { get; }                              // SELECT clause
protected abstract string CountSql { get; }                               // COUNT clause
protected abstract string DefaultOrderBy { get; }                          // Default sort (e.g., "Name ASC")
protected abstract Dictionary<string, string> AllowedSortColumns { get; }  // Allowed sort columns
```

#### virtual Members

```csharp
protected virtual (string Where, DynamicParameters Params)
    BuildWhereClause(Specification<TEntity> spec);  // Auto-delegated when DapperSpecTranslator is provided
protected virtual string PaginationClause => "LIMIT @PageSize OFFSET @Skip";     // Override per DB dialect
protected virtual string CursorPaginationClause => "LIMIT @PageSize";            // Keyset pagination
```

`BuildWhereClause` is auto-delegated when `DapperSpecTranslator` is provided via the constructor. If no Translator is provided, subclasses must override it.

#### Constructor

Two overloads are provided:

```csharp
// 1. Basic -- When overriding BuildWhereClause directly
protected DapperQueryBase(IDbConnection connection)

// 2. DapperSpecTranslator injection -- Auto-delegates BuildWhereClause (recommended)
protected DapperQueryBase(IDbConnection connection, DapperSpecTranslator<TEntity> translator, string tableAlias = "")
```

#### SQL Assembly

The base class uses `QueryMultipleAsync` in the `Search` method for single round-trip count and data retrieval:

```sql
-- QueryMultipleAsync single round-trip
{CountSql} {where};
{SelectSql} {where} {orderBy} {PaginationClause}
```

#### DapperSpecTranslator -- Specification to SQL Translation Registry

`DapperSpecTranslator<TEntity>` is a registry pattern that registers SQL WHERE translation handlers per Specification type. Defining a shared Translator instance allows reuse across multiple Query Adapters.

**Fluent API:**

| Method | Description |
|--------|------|
| `WhenAll(handler)` | Default handler applied to all Specifications (e.g., Soft Delete filter) |
| `When<TSpec>(handler)` | Handler for a specific Specification type |
| `Translate(spec, tableAlias)` | Translates a Specification to SQL WHERE using registered handlers |

**Static Helpers:**

| Method | Description |
|--------|------|
| `Params(params (string, object)[])` | `DynamicParameters` creation helper |
| `Prefix(string tableAlias)` | Table alias prefix (`"p"` → `"p."`, `""` → `""`) |

**Shared Translator Example (ProductSpecTranslator):**

```csharp
public static class ProductSpecTranslator
{
    public static readonly DapperSpecTranslator<Product> Instance =
        new DapperSpecTranslator<Product>()
            .WhenAll(alias =>
            {
                var p = DapperSpecTranslator<Product>.Prefix(alias);
                return ($"WHERE {p}DeletedAt IS NULL", new DynamicParameters());
            })
            .When<ProductPriceRangeSpec>((spec, alias) =>
            {
                var p = DapperSpecTranslator<Product>.Prefix(alias);
                var @params = DapperSpecTranslator<Product>.Params(
                    ("MinPrice", (decimal)spec.MinPrice),
                    ("MaxPrice", (decimal)spec.MaxPrice));
                return ($"WHERE {p}DeletedAt IS NULL AND {p}Price >= @MinPrice AND {p}Price <= @MaxPrice",
                    @params);
            });
}
```

`WhenAll` is used when `Specification.All` (IsAll == true), and `When<TSpec>` matches specific Specification types. Unmatched types throw `NotSupportedException`.

#### Single Table Example (ProductQueryDapper)

```csharp
[GenerateObservablePort]
public class ProductQueryDapper
    : DapperQueryBase<Product, ProductSummaryDto>, IProductQuery
{
    public string RequestCategory => "QueryAdapter";

    protected override string SelectSql => "SELECT Id AS ProductId, Name, Price FROM Products";
    protected override string CountSql => "SELECT COUNT(*) FROM Products";
    protected override string DefaultOrderBy => "Name ASC";
    protected override Dictionary<string, string> AllowedSortColumns { get; } =
        new(StringComparer.OrdinalIgnoreCase) { ["Name"] = "Name", ["Price"] = "Price" };

    // No need to override BuildWhereClause when injecting DapperSpecTranslator
    public ProductQueryDapper(IDbConnection connection)
        : base(connection, ProductSpecTranslator.Instance) { }
}
```

#### JOIN Table Example (ProductWithStockQueryDapper)

```csharp
[GenerateObservablePort]
public class ProductWithStockQueryDapper
    : DapperQueryBase<Product, ProductWithStockDto>, IProductWithStockQuery
{
    public string RequestCategory => "QueryAdapter";

    // Use table aliases for JOINs
    protected override string SelectSql =>
        "SELECT p.Id AS ProductId, p.Name, p.Price, i.StockQuantity " +
        "FROM Products p INNER JOIN Inventories i ON i.ProductId = p.Id";
    protected override string CountSql =>
        "SELECT COUNT(*) FROM Products p INNER JOIN Inventories i ON i.ProductId = p.Id";
    protected override string DefaultOrderBy => "p.Name ASC";  // Includes alias

    // AllowedSortColumns also includes table aliases
    protected override Dictionary<string, string> AllowedSortColumns { get; } =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Name"] = "p.Name",
            ["Price"] = "p.Price",
            ["StockQuantity"] = "i.StockQuantity"
        };

    // Pass DapperSpecTranslator + table alias "p" in constructor
    public ProductWithStockQueryDapper(IDbConnection connection)
        : base(connection, ProductSpecTranslator.Instance, "p") { }
}
```

> **Key point:** When using JOINs, table aliases (`p.`, `i.`) must be used in both `DefaultOrderBy` and `AllowedSortColumns`. When passing an alias to `DapperSpecTranslator`, the alias is automatically applied to the WHERE clause as well.

#### LEFT JOIN Example (ProductWithOptionalStockQueryDapper)

```csharp
protected override string SelectSql =>
    "SELECT p.Id AS ProductId, p.Name, p.Price, i.StockQuantity " +
    "FROM Products p LEFT JOIN Inventories i ON i.ProductId = p.Id";
```

In LEFT JOIN results, `i.StockQuantity` can be `null`, so declare it as `int?` in the DTO:

```csharp
public sealed record ProductWithOptionalStockDto(
    string ProductId, string Name, decimal Price, int? StockQuantity);
```

#### GROUP BY Example (CustomerOrderSummaryQueryDapper)

```csharp
protected override string SelectSql =>
    "SELECT c.Id AS CustomerId, c.Name AS CustomerName, " +
    "COUNT(o.Id) AS OrderCount, " +
    "COALESCE(SUM(o.TotalAmount), 0) AS TotalSpent, " +
    "MAX(o.CreatedAt) AS LastOrderDate " +
    "FROM Customers c LEFT JOIN Orders o ON o.CustomerId = c.Id " +
    "GROUP BY c.Id, c.Name";
protected override string CountSql =>
    "SELECT COUNT(*) FROM Customers c";     // COUNT from original table before GROUP BY
```

The `CountSql` for GROUP BY is written based on the original table without GROUP BY.

In `AllowedSortColumns`, aggregate columns use aliases (AS names) directly:

```csharp
protected override Dictionary<string, string> AllowedSortColumns { get; } =
    new(StringComparer.OrdinalIgnoreCase)
    {
        ["CustomerName"] = "CustomerName",   // Alias from aggregate result
        ["OrderCount"] = "OrderCount",
        ["TotalSpent"] = "TotalSpent",
        ["LastOrderDate"] = "LastOrderDate"
    };
```

#### Complex JOIN Example (CustomerOrdersQueryDapper) -- Without QueryBase

For cases requiring Row to DTO grouping like 4-table JOINs, implement directly without using `DapperQueryBase`:

```csharp
[GenerateObservablePort]
public class CustomerOrdersQueryDapper : ICustomerOrdersQuery  // Only IQueryPort non-generic marker
{
    private const string CustomerSql =
        "SELECT Id AS CustomerId, Name AS CustomerName FROM Customers WHERE Id = @CustomerId";

    private const string OrderLinesSql =
        "SELECT o.Id AS OrderId, o.TotalAmount, o.Status, o.CreatedAt, " +
        "ol.ProductId, p.Name AS ProductName, ol.Quantity, ol.UnitPrice, ol.LineTotal " +
        "FROM Orders o " +
        "INNER JOIN OrderLines ol ON ol.OrderId = o.Id " +
        "INNER JOIN Products p ON p.Id = ol.ProductId " +
        "WHERE o.CustomerId = @CustomerId " +
        "ORDER BY o.CreatedAt DESC";

    private readonly IDbConnection _connection;
    public string RequestCategory => "QueryAdapter";

    public CustomerOrdersQueryDapper(IDbConnection connection) => _connection = connection;

    public virtual FinT<IO, CustomerOrdersDto> GetByCustomerId(CustomerId id)
    {
        return IO.liftAsync(async () =>
        {
            var customer = await _connection.QuerySingleOrDefaultAsync<CustomerRow>(
                CustomerSql, new { CustomerId = id.ToString() });

            if (customer is null)
                return AdapterError.For<CustomerOrdersQueryDapper>(
                    new NotFound(), id.ToString(),
                    $"Customer ID '{id}' not found");

            var rows = (await _connection.QueryAsync<OrderLineRow>(
                OrderLinesSql, new { CustomerId = id.ToString() })).ToList();

            // Row → DTO grouping
            var orders = toSeq(rows
                .GroupBy(r => r.OrderId)
                .Select(g =>
                {
                    var first = g.First();
                    var lines = toSeq(g.Select(r => new CustomerOrderLineDto(
                        r.ProductId, r.ProductName, r.Quantity, r.UnitPrice, r.LineTotal)));
                    return new CustomerOrderDto(
                        first.OrderId, lines, first.TotalAmount, first.Status, first.CreatedAt);
                }));

            return Fin.Succ(new CustomerOrdersDto(
                customer.CustomerId, customer.CustomerName, orders));
        });
    }

    // Private record for Dapper mapping
    private sealed record CustomerRow(string CustomerId, string CustomerName);
    private sealed record OrderLineRow(
        string OrderId, decimal TotalAmount, string Status, DateTime CreatedAt,
        string ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);
}
```

### 3.3 InMemoryQueryBase Implementation Pattern

#### Required Abstract Members

```csharp
protected abstract string DefaultSortField { get; }                          // Default sort field name
protected abstract IEnumerable<TDto> GetProjectedItems(Specification<TEntity> spec);  // Filter + projection
protected abstract Func<TDto, object> SortSelector(string fieldName);        // Sort key selector
```

#### Single Table Example (ProductQueryInMemory)

```csharp
[GenerateObservablePort]
public class ProductQueryInMemory
    : InMemoryQueryBase<Product, ProductSummaryDto>, IProductQuery
{
    public string RequestCategory => "QueryAdapter";

    protected override string DefaultSortField => "Name";

    protected override IEnumerable<ProductSummaryDto> GetProjectedItems(Specification<Product> spec)
    {
        return ProductRepositoryInMemory.Products.Values
            .Where(p => p.DeletedAt.IsNone && spec.IsSatisfiedBy(p))  // Soft Delete + Spec filter
            .Select(p => new ProductSummaryDto(p.Id.ToString(), p.Name, p.Price));
    }

    protected override Func<ProductSummaryDto, object> SortSelector(string fieldName) => fieldName switch
    {
        "Name" => p => p.Name,
        "Price" => p => p.Price,
        _ => p => p.Name                                // Unsupported fields fallback to default
    };
}
```

#### JOIN Implementation -- Accessing Other Repository's static Store

```csharp
[GenerateObservablePort]
public class ProductWithStockQueryInMemory
    : InMemoryQueryBase<Product, ProductWithStockDto>, IProductWithStockQuery
{
    public string RequestCategory => "QueryAdapter";

    protected override string DefaultSortField => "Name";

    protected override IEnumerable<ProductWithStockDto> GetProjectedItems(Specification<Product> spec)
    {
        return ProductRepositoryInMemory.Products.Values
            .Where(p => p.DeletedAt.IsNone && spec.IsSatisfiedBy(p))
            .Select(p =>
            {
                // Direct access to another Repository's static Store (INNER JOIN)
                var inventory = InventoryRepositoryInMemory.Inventories.Values
                    .FirstOrDefault(i => i.ProductId.Equals(p.Id));
                var stockQuantity = inventory is not null ? (int)inventory.StockQuantity : 0;
                return new ProductWithStockDto(p.Id.ToString(), p.Name, p.Price, stockQuantity);
            });
    }

    protected override Func<ProductWithStockDto, object> SortSelector(string fieldName) => fieldName switch
    {
        "Name" => p => p.Name,
        "Price" => p => p.Price,
        "StockQuantity" => p => p.StockQuantity,
        _ => p => p.Name
    };
}
```

#### nullable Sort Handling

Provide default values when sorting nullable values from LEFT JOIN results:

```csharp
protected override Func<ProductWithOptionalStockDto, object> SortSelector(string fieldName) => fieldName switch
{
    "StockQuantity" => p => p.StockQuantity ?? -1,           // int? → -1
    _ => p => p.Name
};

// DateTime? example
protected override Func<CustomerOrderSummaryDto, object> SortSelector(string fieldName) => fieldName switch
{
    "LastOrderDate" => c => c.LastOrderDate ?? DateTime.MinValue,  // DateTime? → MinValue
    _ => c => c.CustomerName
};
```

> Since the return type of `SortSelector` is `object`, returning `null` causes a NullReferenceException. Always provide a default value.

### 3.4 Single-Item Query Pattern

Only inherit `IQueryPort` (non-generic marker) and define the `GetById` method directly:

```csharp
// Interface
public interface IProductDetailQuery : IQueryPort
{
    FinT<IO, ProductDetailDto> GetById(ProductId id);
}

// Dapper implementation uses direct SQL
// InMemory implementation uses TryGetValue from static Store
```

InMemory single-item query example:

```csharp
[GenerateObservablePort]
public class ProductDetailQueryInMemory : IProductDetailQuery
{
    public string RequestCategory => "QueryAdapter";

    public virtual FinT<IO, ProductDetailDto> GetById(ProductId id)
    {
        return IO.lift(() =>
        {
            if (ProductRepositoryInMemory.Products.TryGetValue(id, out var product)
                && product.DeletedAt.IsNone)
            {
                return Fin.Succ(new ProductDetailDto(
                    product.Id.ToString(), product.Name, product.Description,
                    product.Price, product.CreatedAt, product.UpdatedAt));
            }

            return AdapterError.For<ProductDetailQueryInMemory>(
                new NotFound(), id.ToString(),
                $"Product ID '{id}' not found");
        });
    }
}
```

### 3.5 Cursor Pagination

As an alternative to Offset pagination (`Search`), Keyset-based Cursor pagination (`SearchByCursor`) is supported.

#### API

```csharp
FinT<IO, CursorPagedResult<TDto>> SearchByCursor(
    Specification<TEntity> spec,
    CursorPageRequest cursor,
    SortExpression sort);
```

#### Request/Response Types

| Type | Property | Description |
|------|------|------|
| `CursorPageRequest` | `After` | Retrieve data after this cursor (forward) |
| | `Before` | Retrieve data before this cursor (backward) |
| | `PageSize` | Page size (default 20, max 10,000) |
| `CursorPagedResult<T>` | `Items` | Query results (`IReadOnlyList<T>`) |
| | `NextCursor` | Next page cursor |
| | `PrevCursor` | Previous page cursor |
| | `HasMore` | Whether more data exists |

#### Dapper Implementation Principle

```sql
-- Cursor pagination SQL
{SelectSql} {where} AND {sortColumn} > @CursorValue ORDER BY {sortColumn} {CursorPaginationClause}
```

Fetches `PageSize + 1` items to determine `HasMore`. Since no additional COUNT query is needed, this is more efficient than the Offset approach for large datasets.

#### InMemory Implementation Principle

Uses `SortSelector` and `FindLastIndex`/`FindIndex` to locate the cursor position and slice.

#### Offset vs Cursor Comparison

| Item | Offset (`Search`) | Cursor (`SearchByCursor`) |
|------|-------------------|---------------------------|
| Total count | O (COUNT query) | X (not needed) |
| Page jump | O (arbitrary page navigation) | X (sequential traversal only) |
| Large dataset performance | Slower for later pages | Consistent performance |
| Real-time data | Duplicates/omissions on insert/delete | Stable with cursor-based approach |
| UI suitability | Page number UI | Infinite scroll, "Load more" UI |

### 3.6 Compiled Query Pattern (EF Core)

Using EF Core's `EF.CompileAsyncQuery`, you can achieve ~10-15% performance improvement for repeated calls.

```csharp
// Opt-in declaration in ProductRepositoryEfCore
private static readonly Func<LayeredArchDbContext, string, CancellationToken, Task<ProductModel?>>
    GetByIdIgnoringFiltersCompiled = EF.CompileAsyncQuery(
        (LayeredArchDbContext db, string id, CancellationToken _) =>
            db.Products.IgnoreQueryFilters().FirstOrDefault(p => p.Id == id));
```

**Application Principles:**
- Declare as opt-in in concrete subclasses, not base classes
- Apply only when the same query is called repeatedly (e.g., `GetByIdIncludingDeleted`)
- Expression Tree parsing cost is paid only once, making it advantageous for repeated calls

---

## 4. Anti-Patterns

### 4.1 Repository Anti-Patterns

#### 1. Duplicate ByIdPredicate Implementation

```csharp
// ❌ Bad: Repeated implementation in each subclass
public class ProductRepositoryEfCore : EfCoreRepositoryBase<Product, ProductId, ProductModel>
{
    protected override Expression<Func<ProductModel, bool>> ByIdPredicate(ProductId id)
    {
        var s = id.ToString();
        return m => m.Id == s;  // Base already has IHasStringId-based implementation
    }
}
```

`EfCoreRepositoryBase` provides a default implementation based on `IHasStringId`. If all Models implement `IHasStringId`, overriding is unnecessary.

#### 2. Not Using ReadQuery()

```csharp
// ❌ Bad: Querying DbSet directly → Missing Include → N+1
var model = await DbSet.AsNoTracking()
    .FirstOrDefaultAsync(m => m.Id == id.ToString());

// ✅ Good: Using ReadQuery() → Include automatically applied
var model = await ReadQuery()
    .FirstOrDefaultAsync(ByIdPredicate(id));
```

#### 3. Throwing Exceptions in BuildQuery

```csharp
// ❌ Bad: Using exceptions
protected Fin<IQueryable<TModel>> BuildQuery(Specification<TAggregate> spec)
{
    if (PropertyMap is null)
        throw new InvalidOperationException("PropertyMap is required");  // Exception!
}

// ✅ Good: Return error as Fin<T>
return NotConfiguredError("PropertyMap is required for BuildQuery/ExistsBySpec.");
```

#### 4. Expecting Domain Events from Bulk Operations

`ExecuteDeleteAsync` / `ExecuteUpdateAsync` bypass the Change Tracker, so domain events are not published.

```csharp
// ExecuteUpdateAsync -- Direct SQL execution, no domain events (intended behavior)
int affected = await DbSet.Where(ByIdsPredicate(ids))
    .ExecuteUpdateAsync(s => s
        .SetProperty(p => p.DeletedAt, DateTime.UtcNow)
        .SetProperty(p => p.DeletedBy, "system"));

// Use single-item Delete() if events are needed
```

#### 5. Calling ExistsBySpec Without PropertyMap

```csharp
// ❌ Calling ExistsBySpec without passing PropertyMap to constructor → runtime error
public class TagRepositoryEfCore : EfCoreRepositoryBase<Tag, TagId, TagModel>
{
    public TagRepositoryEfCore(...) : base(eventCollector) { }  // no propertyMap

    public FinT<IO, bool> Exists(Specification<Tag> spec) => ExistsBySpec(spec);
    // → Returns NotConfiguredError
}
```

#### 6. Ignoring IN Clause Parameter Limits

The base class automatically handles batch processing with `IdBatchSize` (default 500).
When using `ByIdsPredicate` directly, you must handle this limit yourself.

#### 7. Using DbSet.Update Setting All Columns as Modified

```csharp
// ❌ DbSet.Update -- Sets all columns as Modified, causing unnecessary UPDATEs
DbSet.Update(ToModel(aggregate));
EventCollector.Track(aggregate);

// ✅ TrackedMerge -- FindAsync + SetValues to UPDATE only changed columns
var model = ToModel(aggregate);
var tracked = await DbSet.FindAsync(model.Id);
if (tracked is null) return NotFoundError(aggregate.Id);
DbContext.Entry(tracked).CurrentValues.SetValues(model);
EventCollector.Track(aggregate);
```

TrackedMerge loads the existing entity in tracked state via `FindAsync`, then overwrites only changed values via `SetValues`. EF Core Change Tracker automatically includes only actually changed columns in the UPDATE SQL, reducing unnecessary DB I/O.

### 4.2 Query Adapter Anti-Patterns

#### 1. Missing Table Alias in AllowedSortColumns

```csharp
// ❌ Using column name without alias in JOINs → "ambiguous column name" error
protected override Dictionary<string, string> AllowedSortColumns { get; } =
    new() { ["Name"] = "Name" };  // Products.Name? Customers.Name?

// ✅ Include table alias
protected override Dictionary<string, string> AllowedSortColumns { get; } =
    new() { ["Name"] = "p.Name" };
```

#### 2. Not Handling nullable in SortSelector

```csharp
// ❌ null return → NullReferenceException (during object boxing)
"StockQuantity" => p => p.StockQuantity  // int? → null during object boxing!

// ✅ Provide default value
"StockQuantity" => p => p.StockQuantity ?? -1
```

#### 3. Not Using Parameterized Query in BuildWhereClause

```csharp
// ❌ SQL Injection risk
($"WHERE Name = '{spec.Name}'", new DynamicParameters())

// ✅ Use DapperSpecTranslator's Params() helper
var @params = DapperSpecTranslator<Product>.Params(("Name", (string)spec.Name));
return ("WHERE Name = @Name", @params);
```

> **Recommended**: Using `DapperSpecTranslator` ensures safe parameter binding via the `Params()` helper.

#### 4. Ignoring Specification in InMemory GetProjectedItems

```csharp
// ❌ Ignoring Specification and always returning all data
protected override IEnumerable<ProductSummaryDto> GetProjectedItems(Specification<Product> spec)
{
    return ProductRepositoryInMemory.Products.Values   // spec not applied!
        .Select(p => new ProductSummaryDto(...));
}

// ✅ Apply spec.IsSatisfiedBy
    .Where(p => p.DeletedAt.IsNone && spec.IsSatisfiedBy(p))
```

#### 5. O(N*M) Linear Scan in InMemory JOIN

```csharp
// ❌ Full Inventories scan for each Product → O(N*M)
.Select(p =>
{
    var inventory = InventoryRepositoryInMemory.Inventories.Values
        .FirstOrDefault(i => i.ProductId.Equals(p.Id));
    ...
})

// ✅ O(N) with Dictionary lookup -- Build before querying
var inventoryByProductId = InventoryRepositoryInMemory.Inventories.Values
    .ToDictionary(i => i.ProductId);

.Select(p =>
{
    inventoryByProductId.TryGetValue(p.Id, out var inventory);
    ...
})
```

#### 6. Soft Delete Filter Mismatch Between Repository and Query

The EF Core Global Query Filter in Repository and the WHERE condition in Query Adapter must match:
- **EfCore Repository** -- `HasQueryFilter(p => p.DeletedAt == null)` (automatic)
- **Dapper Query** -- `WHERE p.DeletedAt IS NULL` (manual)
- **InMemory Query** -- `.Where(p => p.DeletedAt.IsNone)` (manual)

---

## 5. Advanced Patterns

### 5.1 Soft Delete Override

#### EfCore -- ReadQueryIgnoringFilters + Attach + IsModified Pattern

```csharp
public override FinT<IO, int> Delete(ProductId id)
{
    return IO.liftAsync(async () =>
    {
        // 1. Ignore Global Filter to also retrieve already deleted items
        var model = await ReadQueryIgnoringFilters()
            .FirstOrDefaultAsync(ByIdPredicate(id));

        if (model is null) return NotFoundError(id);

        // 2. Domain state transition (event publishing)
        var product = ToDomain(model);
        product.Delete("system");

        // 3. Attach + IsModified to UPDATE only changed columns
        var updatedModel = ToModel(product);
        DbSet.Attach(updatedModel);
        _dbContext.Entry(updatedModel).Property(p => p.DeletedAt).IsModified = true;
        _dbContext.Entry(updatedModel).Property(p => p.DeletedBy).IsModified = true;

        EventCollector.Track(product);
        return Fin.Succ(1);
    });
}
```

Bulk Soft Delete uses `ExecuteUpdateAsync` for performance (events not published):

```csharp
public override FinT<IO, int> DeleteRange(IReadOnlyList<ProductId> ids)
{
    return IO.liftAsync(async () =>
    {
        if (ids.Count == 0) return Fin.Succ(0);

        int affected = await DbSet.Where(ByIdsPredicate(ids))
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.DeletedAt, DateTime.UtcNow)
                .SetProperty(p => p.DeletedBy, "system"));
        return Fin.Succ(affected);
    });
}
```

#### InMemory -- DeletedAt.IsNone Filter in GetById/GetByIds

```csharp
public override FinT<IO, Product> GetById(ProductId id)
{
    return IO.lift(() =>
    {
        if (Products.TryGetValue(id, out Product? product) && product.DeletedAt.IsNone)
            return Fin.Succ(product);
        return NotFoundError(id);
    });
}

public override FinT<IO, int> Delete(ProductId id)
{
    return IO.lift(() =>
    {
        if (!Products.TryGetValue(id, out var product)) return Fin.Succ(0);
        product.Delete("system");       // Domain state transition
        EventCollector.Track(product);  // Event collection
        return Fin.Succ(1);
    });
}
```

### 5.2 Specification to SQL Translation

#### PropertyMap Declaration

```csharp
new PropertyMap<Product, ProductModel>()
    .Map(p => (decimal)p.Price, m => m.Price)
    .Map(p => (string)p.Name, m => m.Name)
    .Map(p => p.Id.ToString(), m => m.Id)
```

#### BuildQuery + ExistsBySpec

```csharp
// ExistsBySpec -- Specification-based existence check in one line
public virtual FinT<IO, bool> Exists(Specification<Product> spec)
    => ExistsBySpec(spec);

// BuildQuery -- Specification-based query build (custom usage)
var query = BuildQuery(spec);
// query is Fin<IQueryable<TModel>>
```

#### Dapper's DapperSpecTranslator-Based Translation

Using `DapperSpecTranslator` manages Specification to SQL translation as a shareable registry. Multiple Query Adapters can reuse the same Translator.

```csharp
// Shared Translator definition (declared once)
public static class ProductSpecTranslator
{
    public static readonly DapperSpecTranslator<Product> Instance =
        new DapperSpecTranslator<Product>()
            .WhenAll(alias =>
            {
                var p = DapperSpecTranslator<Product>.Prefix(alias);
                return ($"WHERE {p}DeletedAt IS NULL", new DynamicParameters());
            })
            .When<ProductPriceRangeSpec>((spec, alias) =>
            {
                var p = DapperSpecTranslator<Product>.Prefix(alias);
                var @params = DapperSpecTranslator<Product>.Params(
                    ("MinPrice", (decimal)spec.MinPrice),
                    ("MaxPrice", (decimal)spec.MaxPrice));
                return ($"WHERE {p}DeletedAt IS NULL AND {p}Price >= @MinPrice AND {p}Price <= @MaxPrice",
                    @params);
            });
}

// Inject Translator in Query Adapter -- No need to override BuildWhereClause
public ProductQueryDapper(IDbConnection connection)
    : base(connection, ProductSpecTranslator.Instance) { }

// Use with table alias in JOIN Query
public ProductWithStockQueryDapper(IDbConnection connection)
    : base(connection, ProductSpecTranslator.Instance, "p") { }
```

> **The existing Pattern Matching approach** can still be used by directly overriding `BuildWhereClause`. When using the constructor without a Translator (`base(connection)`), overriding in the subclass is required.

### 5.3 Complex JOIN Query (Without QueryBase)

#### Dapper -- Row to DTO Grouping Pattern

```csharp
var rows = await _connection.QueryAsync<OrderLineRow>(sql, param);

var orders = rows.GroupBy(r => r.OrderId)
    .Select(g =>
    {
        var first = g.First();
        var lines = toSeq(g.Select(r => new CustomerOrderLineDto(...)));
        return new CustomerOrderDto(first.OrderId, lines, ...);
    });
```

#### InMemory -- Accessing Multiple Repository static Stores

```csharp
public virtual FinT<IO, CustomerOrdersDto> GetByCustomerId(CustomerId id)
{
    return IO.lift(() =>
    {
        if (!CustomerRepositoryInMemory.Customers.TryGetValue(id, out var customer))
            return /* NotFound error */;

        var orders = toSeq(OrderRepositoryInMemory.Orders.Values
            .Where(o => o.CustomerId.Equals(id))
            .Select(o =>
            {
                var orderLines = toSeq(o.OrderLines.Select(l =>
                {
                    var product = ProductRepositoryInMemory.Products.Values
                        .FirstOrDefault(p => p.Id.Equals(l.ProductId));
                    var productName = product is not null ? (string)product.Name : "Unknown";
                    return new CustomerOrderLineDto(...);
                }));
                return new CustomerOrderDto(...);
            }));

        return Fin.Succ(new CustomerOrdersDto(...));
    });
}
```

---

## 6. DI Registration Pattern

### Provider-Based Branching

```csharp
public static IServiceCollection RegisterAdapterPersistence(
    this IServiceCollection services, IConfiguration configuration)
{
    services.RegisterConfigureOptions<PersistenceOptions, PersistenceOptions.Validator>(
        PersistenceOptions.SectionName);

    var options = configuration.GetSection(PersistenceOptions.SectionName)
        .Get<PersistenceOptions>() ?? new PersistenceOptions();

    switch (options.Provider)
    {
        case "Sqlite":
            services.AddDbContext<LayeredArchDbContext>(opt =>
                opt.UseSqlite(options.ConnectionString));
            RegisterSqliteRepositories(services);
            RegisterDapperQueries(services, options.ConnectionString);
            break;

        case "InMemory":
        default:
            RegisterInMemoryRepositories(services);
            break;
    }

    return services;
}
```

### Repository Registration -- RegisterScopedObservablePort

The `[GenerateObservablePort]` Source Generator creates `XxxObservable` wrappers.
Use this Observable version when registering:

```csharp
// InMemory
services.RegisterScopedObservablePort<IProductRepository, ProductRepositoryInMemoryObservable>();
services.RegisterScopedObservablePort<IOrderRepository, OrderRepositoryInMemoryObservable>();
services.RegisterScopedObservablePort<ITagRepository, TagRepositoryInMemoryObservable>();

// EfCore (Sqlite)
services.RegisterScopedObservablePort<IProductRepository, ProductRepositoryEfCoreObservable>();
services.RegisterScopedObservablePort<IOrderRepository, OrderRepositoryEfCoreObservable>();
```

### UnitOfWork Registration

```csharp
// InMemory
services.RegisterScopedObservablePort<IUnitOfWork, UnitOfWorkInMemoryObservable>();

// EfCore
services.RegisterScopedObservablePort<IUnitOfWork, UnitOfWorkEfCoreObservable>();
```

### Query Adapter Registration

```csharp
// InMemory -- Register both Query and DetailQuery
services.RegisterScopedObservablePort<IProductQuery, ProductQueryInMemoryObservable>();
services.RegisterScopedObservablePort<IProductDetailQuery, ProductDetailQueryInMemoryObservable>();
services.RegisterScopedObservablePort<IProductWithStockQuery, ProductWithStockQueryInMemoryObservable>();

// Dapper -- IDbConnection registration also required
services.AddScoped<IDbConnection>(_ =>
{
    var conn = new SqliteConnection(connectionString);
    conn.Open();
    return conn;
});
services.RegisterScopedObservablePort<IProductQuery, ProductQueryDapperObservable>();
services.RegisterScopedObservablePort<IProductWithStockQuery, ProductWithStockQueryDapperObservable>();
```

### Additional Registration for InMemory Repository

When InMemory Query accesses another Repository's static Store, the concrete type of that Repository also needs to be registered:

```csharp
// ProductWithStockQueryInMemory accesses InventoryRepositoryInMemory.Inventories
services.AddScoped<InventoryRepositoryInMemory>();

// ProductCatalogInMemory depends on ProductRepositoryInMemory
services.AddScoped<ProductRepositoryInMemory>();
```

---

## 7. Troubleshooting

### Change Tracker State Mismatch After Bulk DeleteRange

**Cause:** `ExecuteDeleteAsync`/`ExecuteUpdateAsync` bypass the Change Tracker, so the state of already-tracked entities may differ from the DB.

**Resolution:** `ReadQuery()` uses `AsNoTracking()`, so there is no issue during reads. If you need to manipulate the entity via Change Tracker after a bulk delete within the same transaction, call `DbContext.ChangeTracker.Clear()`.

### Domain Events Not Published During Product Bulk DeleteRange

**Cause:** This is intended behavior. `ExecuteUpdateAsync` does not create domain objects, so events are not published.

**Resolution:** If events are absolutely required, call single-item `Delete()` individually. If performance is critical, use bulk `DeleteRange()` and perform necessary post-processing separately.

---

## 8. FAQ

### Repository

**Q: Do I need to override ByIdPredicate?**
A: No. If all Models implement `IHasStringId`, the base class's default implementation is applied. Override is only needed when querying by composite keys or columns other than Id.

**Q: When should applyIncludes be configured?**
A: Only configure it for Aggregates with Navigation Properties. It is automatically applied to `ReadQuery()` to prevent N+1 in all read methods. Omit it if there are no Navigation Properties.

**Q: When is PropertyMap needed?**
A: Only for Repositories that use `Exists(Specification)` or `BuildQuery`. Repositories that only do simple CRUD like Tag do not need it.

**Q: Why must InMemoryRepository's ConcurrentDictionary be static?**
A: DI registers as Scoped, so a new instance is created per request. The data must be static to be shared across requests. Additionally, InMemory Query Adapters directly access the static Store.

**Q: Why are CRUD operations symmetric (single ↔ batch) while Specification operations are not?**
A: For the 8 CRUD methods, single and batch share an identical execution path — Create/Update both route through `ToModel` -> `DbSet.Add/Update` (caller already holds domain objects), Delete uses `ExecuteDeleteAsync` whether one or many IDs are passed, and Read uses the same `ReadQuery()` differing only in singular/plural condition. See section 2.7 for the detailed comparison table.

The 5 Specification methods (`Exists`, `Count`, `FindAllSatisfying`, `FindFirstSatisfying`, `DeleteBy`) are intentionally **asymmetric** because they answer fundamentally different questions: existence/cardinality (`Exists`/`Count` return `bool`/`int`), retrieval shape (`FindAll` returns `Seq<T>`, `FindFirst` returns `Option<T>`), and bulk deletion (`DeleteBy` returns `int`). They do not pair off as single↔batch — each represents a distinct use case driven by Evans' `selectSatisfying` pattern.

**Q: What happens if a single ID is passed to bulk DeleteRange?**
A: It works normally. `DeleteRange(new[] { id })` performs the deletion with a single DB round-trip, identical to single-item `Delete(id)`. Both use `ExecuteDeleteAsync`, so there is no performance difference.

**Q: What if a new entity requiring Soft Delete is added besides Product?**
A: Follow the `ProductRepositoryEfCore` pattern: (1) Override `Delete()` to implement the `ReadQueryIgnoringFilters` -> `ToDomain` -> state transition -> `Attach + IsModified` path, (2) Override `DeleteRange()` to directly update `DeletedAt`/`DeletedBy` via `ExecuteUpdateAsync`, (3) Configure global query filter in `DbContext.OnModelCreating`. See section 5.1 for code examples.

### Query Adapter

**Q: What is the difference between search Query and single-item Query?**
A: Search Query implements `IQueryPort<TEntity, TDto>` and inherits from `DapperQueryBase`/`InMemoryQueryBase`. Single-item query only implements `IQueryPort` (non-generic) and defines the `GetById` method directly.

**Q: How do I support a new Specification in Dapper Query?**
A: Add a `When<TSpec>()` handler to the shared `DapperSpecTranslator`. Create parameter bindings with the `Params()` helper. If not using a Translator, directly override `BuildWhereClause` to add the new case.

**Q: How do you do JOINs in InMemory Query?**
A: Directly access other Repositories' `internal static` ConcurrentDictionary. Use LINQ's `FirstOrDefault`, `Where`, etc. to simulate JOINs.

**Q: How do you implement GROUP BY aggregation in InMemory?**
A: Use LINQ's `GroupBy`, `Count()`, `Sum()`, `Max()`, etc. in `GetProjectedItems`. Refer to `CustomerOrderSummaryQueryInMemory`.

**Q: What is `[GenerateObservablePort]`?**
A: The Source Generator automatically creates an observable wrapper class (`XxxObservable`). This wrapper includes a pipeline that logs/traces method calls. Use this Observable version when registering in DI.

---

## 9. References

- [13-adapters.md](../13-adapters) -- Adapter implementation guide
- [15a-unit-testing.md](../testing/15a-unit-testing) -- Unit testing rules
- [OPTIMIZATION-TECHNIQUES.md](../../Src.Benchmarks/BulkCrud.Benchmarks/OPTIMIZATION-TECHNIQUES.md) -- Bulk CRUD performance optimization techniques
