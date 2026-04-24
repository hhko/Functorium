---
title: "Adapter Implementation"
---

This document is a guide for implementing Adapters -- the implementations of Port interfaces -- by type. For Port definitions, see [12-ports.md](../12-ports); for Pipeline generation and DI registration, see [14a-adapter-pipeline-di.md](../14a-adapter-pipeline-di).

## Introduction

"When switching from an InMemory implementation to an EF Core implementation, do we need to modify Use Case code?"
"How do we convert exceptions from external HTTP API calls into `Fin<T>` errors?"
"When `[GenerateObservablePort]` is applied, logging and metrics are automatically generated -- what is the structure?"

An Adapter is the implementation of a Port interface, serving as a bridge between actual infrastructure technology and domain logic. This document covers implementation patterns by type -- Repository, External API, Messaging, Query Adapter -- and error handling strategies.

### What You Will Learn

This document covers the following topics:

1. **Common Adapter Patterns** — Choosing between `IO.lift`/`IO.liftAsync` and the Mapper pattern
2. **Repository Adapter** — Comparison of InMemory and EF Core implementations
3. **External API Adapter** — Error mapping by HTTP status code and exception handling
4. **Messaging Adapter** — Request/Reply and Fire-and-Forget patterns
5. **Query Adapter (CQRS Read)** — Direct DTO return with Dapper

### Prerequisites

A basic understanding of the following concepts is needed to understand this document:

- [Port Architecture and Definitions](../12-ports) — How to define Port interfaces
- [Error System: Basics and Naming](../domain/08a-error-system) — `Fin<T>`, `FinT<IO, T>` return patterns
- [Entity/Aggregate Core Patterns](../domain/06b-entity-aggregate-core) — `CreateFromValidated()` ORM restoration pattern

> **An Adapter is the boundary that separates "pure business logic" from "infrastructure technology details."** Wrap with `IO.lift` and apply `[GenerateObservablePort]`, and observability follows automatically.

## Summary

### Key Commands

```csharp
// Basic Adapter structure (inheriting base class)
[GenerateObservablePort]
public class InMemoryProductRepository
    : InMemoryRepositoryBase<Product, ProductId>, IProductRepository
{
    protected override ConcurrentDictionary<ProductId, Product> Store => Products;

    public override FinT<IO, Product> GetById(ProductId id)
    {
        return IO.lift(() => { /* business logic */ });
    }
}

// Synchronous operation: IO.lift
return IO.lift(() => Fin.Succ(value));

// Asynchronous operation: IO.liftAsync
return IO.liftAsync(async () => { var result = await ...; return Fin.Succ(result); });

// Error return
return Fin.Fail<T>(AdapterError.For<TAdapter>(errorType, context, message));
```

### Key Procedures

1. Apply `[GenerateObservablePort]` attribute to the class
2. Implement Port interface and define the `RequestCategory` property
3. Add `virtual` keyword to all interface methods
4. Wrap business logic with `IO.lift()` (sync) or `IO.liftAsync()` (async)
5. Use `Fin.Succ(value)` for success, `AdapterError.For<T>(...)` for failure
6. When needed, define Mapper classes as `internal` for domain/technical model conversion

### Key Concepts

| Concept | Description |
|------|------|
| `[GenerateObservablePort]` | Attribute that triggers Source Generator to auto-generate Observability Pipeline |
| `IO.lift` / `IO.liftAsync` | Methods that wrap sync/async operations into `FinT<IO, T>` |
| `virtual` keyword | Required for Pipeline to override methods |
| `RequestCategory` | Category used in Observability logs (`"Repository"`, `"ExternalApi"`, etc.) |
| Mapper pattern | `internal` class responsible for conversion between domain models and technical models (POCO, DTO) |
| `AdapterError` | Adapter layer-specific error type (`For<T>`, `FromException<T>`) |

---

## Why the Adapter Pattern

When an application is directly coupled to databases, external APIs, and messaging systems, two problems arise. First, changing infrastructure technology requires modifying business logic as well. Second, unit tests become slow and unreliable because they need actual databases or external services.

The Adapter pattern breaks this coupling. The Domain and Application layers know only the Port interface, and the Adapter provides the implementation of that interface. During testing, an InMemory implementation is substituted, while in production, EF Core or Dapper-based implementations are used.

Functorium adds Observability on top of this. When the `[GenerateObservablePort]` attribute is applied, the Source Generator creates an Observable wrapper that automatically adds Logging, Metrics, and Tracing to the Adapter. There is no need to write observability logic directly in Adapter code.

Now that we understand the necessity of the Adapter pattern, let's examine the actual implementation methods by type.

---

## Activity 2: Adapter Implementation

An Adapter is the **implementation** of a Port interface. The Observability Pipeline is automatically generated through the `[GenerateObservablePort]` attribute.

> **Source Generator Note**: `[GenerateObservablePort]` is implemented as a Roslyn Incremental Source Generator, enabling incremental generation at build time. For projects with many Adapters, verify that the Pipeline was correctly generated by checking the generated code in `obj/GeneratedFiles/`. Only methods wrapped with `IO.lift`/`IO.liftAsync` are Pipeline targets, and without the `virtual` keyword, the Pipeline cannot override the method.

### Common Implementation Checklist

Items required for all Adapter implementations.

- [ ] Has the `[GenerateObservablePort]` attribute been applied to the class?
- [ ] Does it implement a Port interface?
- [ ] Has the `RequestCategory` property been defined?
- [ ] Has the `virtual` keyword been added to all interface methods?
- [ ] Is the business logic wrapped with `IO.lift()` or `IO.liftAsync()`?
- [ ] Is the Mapper class declared as `internal`? (if applicable)

### Common Patterns

These are patterns that apply commonly to all Adapter types. Familiarize yourself with these before implementing type-specific Adapters.

#### IO.lift vs IO.liftAsync Decision

All Adapter methods return `FinT<IO, T>`, and the wrapping method is chosen based on the internal operation type.

| Criteria | `IO.lift(() => { ... })` | `IO.liftAsync(async () => { ... })` |
|------|--------------------------|--------------------------------------|
| Operation type | Synchronous (sync) | Asynchronous (async/await) |
| Typical cases | In-Memory store, cache lookup | HTTP calls, message sending, async DB queries |
| Return | `Fin<T>` | `Fin<T>` |
| Usage type | Repository (sync) | External API, Messaging |

**Decision Criteria**: Does the internal logic require `await`?
- **Yes** → `IO.liftAsync`
- **No** → `IO.lift`

> **Note**: For async DB access such as EF Core, `IO.liftAsync` is used in Repositories as well.

#### Data Conversion (Mapper Pattern)

Handles conversion between Port domain models and technology-concern DTOs inside the Adapter. Mapper classes must be declared as `internal`.

##### Infrastructure Adapter (HTTP API)

The key point to note in the following code is the ACL (Anti-Corruption Layer) structure that converts Port Request to Query Parameters and Infrastructure DTO to Port Response through the Mapper.

```csharp
// Adapters.Infrastructure/Apis/CriteriaApi/CriteriaApiService.cs
[GenerateObservablePort]
public class CriteriaApiService : ICriteriaApiService
{
    private readonly HttpClient _httpClient;

    public string RequestCategory => "ExternalApi";

    #region Error Types
    public sealed record ResponseNull : AdapterErrorKind.Custom;
    #endregion

    public virtual FinT<IO, ICriteriaApiService.Response> GetEquipHistoriesAsync(
        ICriteriaApiService.Request request,
        CancellationToken cancellationToken)
    {
        return IO.liftAsync(async () =>
        {
            // 1. Convert Port Request → Query Parameters
            var queryParams = CriteriaApiMapper.ToQueryParams(request);

            // 2. HTTP call
            var url = QueryHelpers.AddQueryString("/api/v2/criteria/equips/history", queryParams);
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return Fin.Fail<ICriteriaApiService.Response>(
                    AdapterError.For<CriteriaApiService>(
                        new ConnectionFailed("HTTP"),
                        url,
                        $"API call failed: {response.StatusCode} - {errorContent}"));
            }

            // 3. Convert Infrastructure DTO → Port Response
            var dto = await response.Content.ReadFromJsonAsync<GetEquipHistoryResponseDto>(cancellationToken);
            return dto?.Histories is not null
                ? Fin.Succ(CriteriaApiMapper.ToResponse(dto))
                : Fin.Fail<ICriteriaApiService.Response>(
                    AdapterError.For<CriteriaApiService>(new ResponseNull(), url, "Response data is null"));
        });
    }
}

// Mapper class (Infrastructure internal - internal)
internal static class CriteriaApiMapper
{
    public static Dictionary<string, string?> ToQueryParams(ICriteriaApiService.Request request)
        => new()
        {
            ["connType"] = request.ConnType,
            ["equipTypeId"] = request.EquipTypeId
        };

    public static ICriteriaApiService.Response ToResponse(GetEquipHistoryResponseDto dto)
        => new(Equipments: dto.Histories
            .Select(h => new ICriteriaApiService.Equipment(
                h.LineId, h.TypeId, h.ModelId, h.EquipId,
                h.Description, h.UpdateTime, h.ConnectionType,
                h.ConnIp, h.ConnPort, h.ConnId, h.ConnPw, h.ServiceName))
            .ToSeq());
}

// Infrastructure internal DTO (internal - not exposed externally)
internal record GetEquipHistoryResponseDto(List<EquipDto> Histories);
internal record EquipDto(string LineId, string TypeId, string ModelId, ...);
```

##### Persistence Adapter (Repository)

Persistence Adapter uses **Persistence Model (POCO)** and **Mapper (extension methods)** to separate domain entities from DB models. Instead of EF Core `HasConversion`, explicit conversion is done in the Mapper.

The key point to note in the following code is the bidirectional mapping where `ToModel()` converts domain to POCO, and `ToDomain()` restores POCO to domain through `CreateFromValidated()`.

```csharp
// Persistence Model — POCO (primitive types only, no domain dependencies)
// File: {Adapters.Persistence}/Repositories/EfCore/Models/ProductModel.cs
public class ProductModel
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public List<ProductTagModel> ProductTags { get; set; } = [];
}
```

```csharp
// Mapper — internal static class, extension methods
// File: {Adapters.Persistence}/Repositories/EfCore/Mappers/ProductMapper.cs
internal static class ProductMapper
{
    public static ProductModel ToModel(this Product product) => new()
    {
        Id = product.Id.ToString(),
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt.ToNullable(),
        DeletedAt = product.DeletedAt.ToNullable(),
        DeletedBy = product.DeletedBy.Match(Some: v => (string?)v, None: () => null),
        ProductTags = product.TagIds.Select(tagId => new ProductTagModel
        {
            ProductId = product.Id.ToString(),
            TagId = tagId.ToString()
        }).ToList()
    };

    public static Product ToDomain(this ProductModel model)
    {
        var tagIds = model.ProductTags.Select(pt => TagId.Create(pt.TagId));

        return Product.CreateFromValidated(   // Restore without validation
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

```csharp
// Repository — inherits EfCoreRepositoryBase + uses Mapper extension methods
// File: {Adapters.Persistence}/Repositories/EfCore/EfCoreProductRepository.cs
[GenerateObservablePort]
public class EfCoreProductRepository
    : EfCoreRepositoryBase<Product, ProductId, ProductModel>, IProductRepository
{
    private readonly LayeredArchDbContext _dbContext;

    public EfCoreProductRepository(
        LayeredArchDbContext dbContext,
        IDomainEventCollector eventCollector,
        Func<IQueryable<ProductModel>, IQueryable<ProductModel>>? applyIncludes = null,
        PropertyMap<Product, ProductModel>? propertyMap = null)
        : base(eventCollector, applyIncludes, propertyMap)
    {
        _dbContext = dbContext;
    }

    protected override DbContext DbContext => _dbContext;
    protected override DbSet<ProductModel> DbSet => _dbContext.Products;
    protected override Product ToDomain(ProductModel model) => model.ToDomain();
    protected override ProductModel ToModel(Product aggregate) => aggregate.ToModel();

    // CRUD (Create, GetById, Update, Delete, etc.) provided by EfCoreRepositoryBase default implementation
    // Only override or add domain-specific methods

    public virtual FinT<IO, bool> Exists(Specification<Product> spec)
    {
        return ExistsBySpec(spec);  // Leveraging base class BuildQuery
    }
}
```

> **Key point**: Since `EfCoreRepositoryBase` provides default implementations of 8 CRUD methods (`Create`, `GetById`, `Update`, `Delete`, `CreateRange`, `GetByIds`, `UpdateRange`, `DeleteRange`), subclasses only need to implement `ToDomain()`/`ToModel()` conversion and domain-specific methods. The `IHasStringId` interface ensures all Model `Id` properties are `string` type, and `ReadQuery()` automatically applies Include to structurally prevent N+1 problems.

#### Error Handling Integration

##### Simplified Error Return

LanguageExt provides implicit conversion from `Error → Fin<T>`.
Therefore, instead of `Fin.Fail<T>(error)`, you can return `error` directly:

```csharp
// Previous approach (verbose)
return Fin.Fail<Money>(AdapterError.For<MyAdapter>(
    new NotFound(), context, "Not found"));

// Recommended approach (implicit conversion)
return AdapterError.For<MyAdapter>(
    new NotFound(), context, "Not found");
```

The same applies to exception handling:

```csharp
catch (HttpRequestException ex)
{
    // Previous approach
    return Fin.Fail<Money>(AdapterError.FromException<MyAdapter>(
        new ConnectionFailed("ServiceName"), ex));

    // Recommended approach
    return AdapterError.FromException<MyAdapter>(
        new ConnectionFailed("ServiceName"), ex);
}
```

> **Note**: The method return type must be explicitly `Fin<T>` or `FinT<IO, T>`
> for the implicit conversion to work.

##### FinT<IO, T> and AdapterError Integration

```csharp
// AdapterErrorKind usage pattern
using static Functorium.Adapters.Errors.AdapterErrorKind;

// NotFound - Resource not found
AdapterError.For<ProductRepository>(
    new NotFound(),
    productId.ToString(),
    "Product not found");

// AlreadyExists - Resource already exists
AdapterError.For<ProductRepository>(
    new AlreadyExists(),
    productName,
    "Product already exists");

// ConnectionFailed - External system connection failure
AdapterError.For<CriteriaApiService>(
    new ConnectionFailed("HTTP"),
    url,
    "API connection failed");

// Custom - User-defined error type
// Error type definition: public sealed record ReservationFailed : AdapterErrorKind.Custom;
AdapterError.For<InventoryRepository>(
    new ReservationFailed(),
    orderId.ToString(),
    "Failed to reserve inventory");

// Exception wrapping
AdapterError.FromException<ProductRepository>(
    new PipelineException(),
    exception);
```

##### Pipeline's Automatic Error Classification

```
Error Type                             Log Level      Metric Tag
────────────────────────────────────────────────────────────────
IHasErrorCode + IsExpected  ────────► Warning       error.type: "expected"
IHasErrorCode + IsExceptional ──────► Error         error.type: "exceptional"
ManyErrors ─────────────────────────► Warning/Error error.type: "aggregate"
```

##### Value Object Sharing Strategy

```
┌──────────────────────────────────────────────────────────────┐
│                      Domain Layer                            │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Value Objects (shared across all layers)                │  │
│  │   - ProductId, ProductName, Money, Quantity            │  │
│  │   - EquipId, EquipTypeId, RecipeHostId                │  │
│  │   - EquipmentConnectionInfo                            │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┼───────────────┐
              ▼               ▼               ▼
┌──────────────────┐  ┌──────────────┐  ┌───────────────────────┐
│ Application      │  │ Infrastructure│  │ Persistence           │
│ (Usecase)        │  │ (API Adapter) │  │ (Repository)          │
│                  │  │               │  │                       │
│ Uses ProductId   │  │ ProductId →   │  │ ProductModel (POCO)   │
│                  │  │ string (DTO)  │  │ ProductId → string    │
└──────────────────┘  └──────────────┘  └───────────────────────┘
```

#### ACL Checklist by External System Type

##### Common ACL Principles

- Ports use only domain types (VO, Entity, Domain Event)
- Define technology-specific models/DTOs inside the Adapter (`internal` visibility)
- Define Mappers inside the Adapter (`internal static class`, extension methods)
- Never expose external types to the Application/Domain layer

##### Mapping Table by System Type

The types and Mapper patterns used for ACL implementation vary depending on the external system type.

| External System Type | Adapter Project | Internal Conversion Type | Mapper Pattern | Existing Example |
|---|---|---|---|---|
| Database (RDBMS) | Persistence | `internal class XxxModel` (POCO) | `internal static class XxxMapper` (extension methods) | `ProductModel` + `ProductMapper` (§2.2) |
| External HTTP API | Infrastructure | `internal record XxxDto` | `internal static class XxxApiMapper` | `CriteriaApiMapper` (§2.2) |
| Message Broker | Infrastructure | `internal record XxxMessage` | `internal static class XxxMessageMapper` | Apply when applicable (see §2.5) |
| File System | Infrastructure | `internal record/class XxxFileModel` | `internal static class XxxFileMapper` | — (same pattern) |
| Cache | Infrastructure | `internal record XxxCacheEntry` | `internal static class XxxCacheMapper` | — (same pattern) |
| External Auth | Infrastructure | `internal record XxxAuthResponse` | `internal static class XxxAuthMapper` | — (same pattern) |

##### ACL Application Criteria

```
New external system integration
├─ External schema can change independently? → ACL required (internal DTO + Mapper)
└─ Jointly managed via shared contract? → ACL optional (Pass-through allowed)
```

- **ACL required examples**: Legacy DB, external team's API, third-party message schema
- **Pass-through allowed examples**: Shared message contracts within the same team (current Messaging Adapter pattern)

### Repository Adapter

Repository Adapter implements CRUD operations against data stores.

#### InMemory Repository

`InMemoryRepositoryBase<TAggregate, TId>` is a framework base class that provides default implementations of all 8 `IRepository` CRUD operations based on `ConcurrentDictionary`.
Subclasses only need to provide the `Store` property and override only those methods that require special logic such as Soft Delete.

The key point to note in the following code is the structure where the base class provides default CRUD implementations, and only `GetById` and `Delete` that require Soft Delete are overridden.

```csharp
// File: {Adapters.Persistence}/Repositories/InMemory/InMemoryProductRepository.cs

using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;

[GenerateObservablePort]                                                    // 1. Auto-generate Pipeline
public class InMemoryProductRepository
    : InMemoryRepositoryBase<Product, ProductId>, IProductRepository         // 2. Base class + Port implementation
{
    internal static readonly ConcurrentDictionary<ProductId, Product> Products = new();
    protected override ConcurrentDictionary<ProductId, Product> Store => Products;  // 3. Provide store

    public InMemoryProductRepository(IDomainEventCollector eventCollector)
        : base(eventCollector) { }                                          // 4. Call base constructor

    // ─── Soft Delete Override ──────────────────────
    // Inherit default Create/GetById/Update/Delete/CreateRange/GetByIds/UpdateRange/DeleteRange
    // from base, and only override methods that require Soft Delete.

    public override FinT<IO, Product> GetById(ProductId id)
    {
        return IO.lift(() =>
        {
            if (Products.TryGetValue(id, out Product? product) && product.DeletedAt.IsNone)
            {
                return Fin.Succ(product);
            }

            return NotFoundError(id);                                       // 5. Base error helper
        });
    }

    public override FinT<IO, int> Delete(ProductId id)                      // 6. Return type: int (affected rows)
    {
        return IO.lift(() =>
        {
            if (!Products.TryGetValue(id, out var product))
            {
                return Fin.Succ(0);
            }

            product.Delete("system");
            EventCollector.Track(product);
            return Fin.Succ(1);
        });
    }

    // ... Product-specific methods (Exists, GetByIdIncludingDeleted, etc.)
}
```

> **Reference**: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/InMemory/Products/InMemoryProductRepository.cs`

**InMemoryRepositoryBase Features:**

| Member | Type | Description |
|------|------|------|
| `Store` | `abstract ConcurrentDictionary<TId, T>` | In-memory store provided by subclass |
| `Create` / `CreateRange` | `virtual` | Save + event tracking |
| `GetById` / `GetByIds` | `virtual` | Lookup + Not Found error |
| `Update` / `UpdateRange` | `virtual` | Update + event tracking |
| `Delete` / `DeleteRange` | `virtual` | Delete (returns: `int` — affected rows) |
| `NotFoundError()` | `protected` | Error helper |
| `EventCollector` | `protected` | Domain event collector |

**Repository Adapter Core Patterns**:

| Pattern | Code | Description |
|------|------|------|
| IO wrapping | `IO.lift(() => { ... })` | Use `IO.lift` for synchronous operations |
| Success | `Fin.Succ(value)` | Wrap success value |
| Domain failure | `AdapterError.For<T>(errorType, context, message)` | Business failure (not found, etc.) |
| Delete return | `Fin.Succ(1)` / `Fin.Succ(0)` | Affected rows (`int`) |
| Optional | `Fin.Succ(Optional(value))` | `Option<T>` wrapping |
| Collection | `Fin.Succ(toSeq(values))` | `Seq<T>` wrapping |

#### EF Core Repository

This is a Repository Adapter pattern that uses EF Core instead of InMemory (ConcurrentDictionary). It implements the same Port interface but uses `IO.liftAsync` to wrap EF Core's asynchronous API.

##### DbContext Definition

DbContext uses **Persistence Model (POCO)** as the DbSet type. It directly references Models, not domain entities.

```csharp
// File: {Adapters.Persistence}/Repositories/EfCore/{ServiceName}DbContext.cs

public class LayeredArchDbContext : DbContext
{
    public DbSet<ProductModel> Products => Set<ProductModel>();
    public DbSet<InventoryModel> Inventories => Set<InventoryModel>();
    public DbSet<OrderModel> Orders => Set<OrderModel>();
    public DbSet<OrderLineModel> OrderLines => Set<OrderLineModel>();
    public DbSet<CustomerModel> Customers => Set<CustomerModel>();
    public DbSet<TagModel> Tags => Set<TagModel>();
    public DbSet<ProductTagModel> ProductTags => Set<ProductTagModel>();

    public LayeredArchDbContext(DbContextOptions<LayeredArchDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LayeredArchDbContext).Assembly);
    }
}
```

> **Reference**: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/EfCore/LayeredArchDbContext.cs`

**Key Points:**
- DbSet types are **Persistence Models** (`ProductModel`, `OrderModel`, ...) — not domain entities (`Product`, `Order`, ...)
- `ApplyConfigurationsFromAssembly` automatically discovers `IEntityTypeConfiguration<T>` implementations in the same assembly
- DbSet properties are defined using the `=> Set<T>()` expression

##### Entity Configuration — Direct Persistence Model Mapping

Since Persistence Models use only primitive types, EF Core `HasConversion` is unnecessary. Configuration implements `IEntityTypeConfiguration<XxxModel>`.

| Model Property Type | EF Core Setting | Note |
|---|---|---|
| `string` (EntityId) | `HasMaxLength(26)` | Ulid string (26 chars) |
| `string` (name, etc.) | `HasMaxLength(N).IsRequired()` | — |
| `decimal` (amount) | `HasPrecision(18, 4)` | — |
| `int` (quantity) | — | Default mapping |
| `DateTime?` (deletion time) | — | Soft Delete support |
| `string?` (deleted by) | `HasMaxLength(320)` | — |
| `List<ProductTagModel>` (collection) | `HasMany().WithOne().HasForeignKey().OnDelete(Cascade)` | — |

**Entity Configuration Example:**

```csharp
// File: {Adapters.Persistence}/Repositories/EfCore/Configurations/ProductConfiguration.cs

public class ProductConfiguration : IEntityTypeConfiguration<ProductModel>
{
    public void Configure(EntityTypeBuilder<ProductModel> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasMaxLength(26);

        builder.Property(p => p.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(p => p.Price)
            .HasPrecision(18, 4);

        builder.Property(p => p.CreatedAt);
        builder.Property(p => p.UpdatedAt);

        builder.Property(p => p.DeletedAt);
        builder.Property(p => p.DeletedBy).HasMaxLength(320);

        // Global Query Filter: automatically exclude deleted products
        builder.HasQueryFilter(p => p.DeletedAt == null);

        builder.HasMany(p => p.ProductTags)
            .WithOne()
            .HasForeignKey(pt => pt.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

> **Reference**: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/EfCore/Configurations/ProductConfiguration.cs`

**Difference from previous pattern:** In the previous approach where domain entities were mapped directly, each Value Object required `HasConversion` + `IdConverter`/`IdComparer`. Using Persistence Models (POCO) with primitive types eliminates the need for conversion.

##### EF Core Repository Implementation

Inherits from `EfCoreRepositoryBase<TAggregate, TId, TModel>` to get default CRUD implementations, and only adds domain-specific methods. Since DbContext works with **Persistence Models**, Mapper extension methods (`ToModel()` / `ToDomain()`) are used for conversion with domain entities.

**`EfCoreRepositoryBase` Key Features:**

| Feature | Description |
|------|------|
| `ReadQuery()` | Read-only query with Include automatically applied. Structurally prevents N+1 problems |
| `BuildQuery(spec)` | Specification → Model Expression query builder. `PropertyMap` required |
| `ExistsBySpec(spec)` | Specification-based existence check. `PropertyMap` required |
| `PropertyMap` | Aggregate → Model property mapping. Used for Specification SQL translation |
| `IdBatchSize` | Batch size to prevent SQL IN clause parameter limits (default: 500) |
| `IHasStringId` | Interface that all Models must implement. Guarantees `string Id` property |

```csharp
// File: {Adapters.Persistence}/Repositories/EfCore/EfCoreProductRepository.cs

using Functorium.Adapters.Errors;
using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using LayeredArch.Adapters.Persistence.Repositories.EfCore.Mappers;
using LayeredArch.Adapters.Persistence.Repositories.EfCore.Models;
using static Functorium.Adapters.Errors.AdapterErrorKind;

[GenerateObservablePort]
public class EfCoreProductRepository
    : EfCoreRepositoryBase<Product, ProductId, ProductModel>, IProductRepository
{
    private readonly LayeredArchDbContext _dbContext;

    public EfCoreProductRepository(
        LayeredArchDbContext dbContext,
        IDomainEventCollector eventCollector)
        : base(
            eventCollector,
            applyIncludes: q => q.Include(p => p.ProductTags),  // Prevent N+1
            propertyMap: ProductPropertyMap.Instance)             // Specification SQL translation
    {
        _dbContext = dbContext;
    }

    // --- Required abstract member implementations ---
    protected override DbContext DbContext => _dbContext;
    protected override DbSet<ProductModel> DbSet => _dbContext.Products;
    protected override Product ToDomain(ProductModel model) => model.ToDomain();
    protected override ProductModel ToModel(Product aggregate) => aggregate.ToModel();

    // 8 CRUD methods are provided by EfCoreRepositoryBase default implementation

    // --- Domain-specific methods ---
    public virtual FinT<IO, bool> Exists(Specification<Product> spec)
    {
        return ExistsBySpec(spec);
    }

    public virtual FinT<IO, int> Delete(ProductId id)
    {
        return IO.liftAsync(async () =>
        {
            var model = await _dbContext.Products
                .IgnoreQueryFilters()
                .Include(p => p.ProductTags)
                .FirstOrDefaultAsync(p => p.Id == id.ToString());

            if (model is null)
            {
                return AdapterError.For<EfCoreProductRepository>(
                    new NotFound(),
                    id.ToString(),
                    $"Product ID '{id}' not found");
            }

            var product = model.ToDomain();
            product.Delete("system");
            _dbContext.Products.Update(product.ToModel());
            _eventCollector.Track(product);
            return Fin.Succ(1);
        });
    }

    // ... remaining methods follow the same pattern
}
```

> **Reference**: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/EfCore/EfCoreProductRepository.cs`

**InMemory vs EF Core Repository Comparison:**

Here is a comparison of the differences when implementing the same Port but with different storage technologies.

| Item | InMemory | EF Core |
|---|---|---|
| IO wrapping | `IO.lift(() => { ... })` | `IO.liftAsync(async () => { ... })` |
| Store | `ConcurrentDictionary<TId, T>` | `DbContext.Set<TModel>()` |
| Save/Load conversion | Not needed (domain objects stored directly) | `product.ToModel()` / `model.ToDomain()` |
| Lookup | `Products.TryGetValue(id, ...)` | `_dbContext.Products.FirstOrDefaultAsync(...)` |
| Navigation loading | Not needed (in-memory references) | `.Include(p => p.ProductTags)` |
| Deletion method | Soft Delete (`product.Delete(...)`) | Soft Delete (`product.Delete(...)` + `Update`) |
| Transaction management | No-op (`InMemoryUnitOfWork`) | `DbContext.SaveChangesAsync()` (`EfCoreUnitOfWork`) |
| Error pattern | `AdapterError.For<T>(...)` | `AdapterError.For<T>(...)` (same) |
| Pipeline generation | `[GenerateObservablePort]` | `[GenerateObservablePort]` (same) |
| DI registration | `RegisterScopedObservablePort<>` | `RegisterScopedObservablePort<>` (same) |

#### Unit of Work

Unit of Work (UoW) is a Port for committing transactions in Use Cases. Repositories only track entity changes, and the actual commit is handled by UoW.

##### IUnitOfWork Interface

**Location**: `Functorium.Applications.Persistence`

```csharp
public interface IUnitOfWork : IObservablePort
{
    FinT<IO, Unit> SaveChanges(CancellationToken cancellationToken = default);
}
```

##### EfCoreUnitOfWork Implementation

Commits changes by calling `DbContext.SaveChangesAsync()`. Converts `DbUpdateException`-family exceptions to `AdapterError`.

```csharp
// File: {Adapters.Persistence}/Repositories/EfCore/EfCoreUnitOfWork.cs

[GenerateObservablePort]
public class EfCoreUnitOfWork : IUnitOfWork
{
    private readonly LayeredArchDbContext _dbContext;

    public string RequestCategory => "UnitOfWork";

    #region Error Types
    public sealed record ConcurrencyConflict : AdapterErrorKind.Custom;
    public sealed record DatabaseUpdateFailed : AdapterErrorKind.Custom;
    #endregion

    public EfCoreUnitOfWork(LayeredArchDbContext dbContext) => _dbContext = dbContext;

    public virtual FinT<IO, Unit> SaveChanges(CancellationToken cancellationToken = default)
    {
        return IO.liftAsync(async () =>
        {
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                return Fin.Succ(unit);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return AdapterError.FromException<EfCoreUnitOfWork>(
                    new ConcurrencyConflict(), ex);
            }
            catch (DbUpdateException ex)
            {
                return AdapterError.FromException<EfCoreUnitOfWork>(
                    new DatabaseUpdateFailed(), ex);
            }
        });
    }
}
```

> **Reference**: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/EfCore/EfCoreUnitOfWork.cs`

##### InMemoryUnitOfWork Implementation

Since `ConcurrentDictionary`-based InMemory storage reflects changes immediately, SaveChanges is a no-op.

```csharp
// File: {Adapters.Persistence}/Repositories/InMemory/InMemoryUnitOfWork.cs

[GenerateObservablePort]
public class InMemoryUnitOfWork : IUnitOfWork
{
    public string RequestCategory => "UnitOfWork";

    public virtual FinT<IO, Unit> SaveChanges(CancellationToken cancellationToken = default)
    {
        return IO.lift(() => Fin.Succ(unit));
    }
}
```

> **Reference**: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/InMemory/InMemoryUnitOfWork.cs`

##### IDomainEventCollector — Bridge Between Repository and Publisher

`IDomainEventCollector` serves as a bridge that passes tracked Aggregates from Repositories to `DomainEventPublisher`.

**Location**: `Functorium.Applications.Events`

```csharp
public interface IDomainEventCollector
{
    void Track(IHasDomainEvents aggregate);
    IReadOnlyList<IHasDomainEvents> GetTrackedAggregates();
}
```

**Usage in Repository**: In the Repository's `Create()` and `Update()` methods, you must call `_eventCollector.Track(aggregate)` to register the Aggregate as a tracking target:

```csharp
public FinT<IO, Product> Create(Product product)
{
    _eventCollector.Track(product);  // Required: register as domain event collection target
    // ... save logic ...
}
```

**Registration**: When `RegisterDomainEventPublisher()` is called, `IDomainEventCollector` is automatically registered as a Scoped service:

```csharp
services.RegisterDomainEventPublisher();  // Registers IDomainEventPublisher + IDomainEventCollector
```

##### Why SaveChanges Is Not Called in Repositories

The Repository's `Create()`, `Update()`, and `Delete()` methods only register entities in EF Core's Change Tracking. The actual `SaveChangesAsync()` call is automatically performed by `UsecaseTransactionPipeline` after Handler execution.

This separation enables:
- Multiple Repository changes can be grouped into a single transaction (guaranteed by pipeline)
- Event publishing can be guaranteed to occur after transaction success (guaranteed by pipeline)
- Repositories remain as pure data access layers
- Repositories call `IDomainEventCollector.Track(aggregate)` to register domain event collection targets

> **Reference**: For the pipeline pattern, see [11-usecases-and-cqrs.md -- Transaction and Event Publishing](../application/11-usecases-and-cqrs#transactions-and-event-publishing-usecasetransactionpipeline).

While Repository Adapters handle data persistence, External API Adapters handle HTTP communication with external systems.

### External API Adapter

External API Adapter implements external system calls through HTTP clients.

The key point to note in the following code is the error mapping by HTTP status code (`HandleHttpError`) and the `AdapterError` conversion structure by exception type.

```csharp
// File: {Adapters.Infrastructure}/ExternalApis/ExternalPricingApiService.cs

using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using static Functorium.Adapters.Errors.AdapterErrorKind;

[GenerateObservablePort]
public class ExternalPricingApiService : IExternalPricingService
{
    private readonly HttpClient _httpClient;              // 1. HttpClient injection

    public string RequestCategory => "ExternalApi";       // 2. Request category

    #region Error Types
    public sealed record OperationCancelled : AdapterErrorKind.Custom;
    public sealed record UnexpectedException : AdapterErrorKind.Custom;
    public sealed record RateLimited : AdapterErrorKind.Custom;
    public sealed record HttpError : AdapterErrorKind.Custom;
    #endregion

    public ExternalPricingApiService(HttpClient httpClient)  // 3. Constructor injection
    {
        _httpClient = httpClient;
    }

    public virtual FinT<IO, Money> GetPriceAsync(
        string productCode, CancellationToken cancellationToken)
    {
        return IO.liftAsync(async () =>                   // 4. IO.liftAsync (async)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"/api/pricing/{productCode}",
                    cancellationToken);

                // 5. HTTP error handling
                if (!response.IsSuccessStatusCode)
                {
                    return HandleHttpError<Money>(response, productCode);
                }

                // 6. Response deserialization
                var priceResponse = await response.Content
                    .ReadFromJsonAsync<ExternalPriceResponse>(
                        cancellationToken: cancellationToken);

                // 7. null response handling
                if (priceResponse is null)
                {
                    return AdapterError.For<ExternalPricingApiService>(
                        new Null(),
                        productCode,
                        $"External API response is null. ProductCode: {productCode}");
                }

                return Money.Create(priceResponse.Price);
            }
            catch (HttpRequestException ex)               // 8. Connection failure
            {
                return AdapterError.FromException<ExternalPricingApiService>(
                    new ConnectionFailed("ExternalPricingApi"),
                    ex);
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                return AdapterError.For<ExternalPricingApiService>(  // 9. User cancellation
                    new OperationCancelled(),
                    productCode,
                    "Request was cancelled");
            }
            catch (TaskCanceledException ex)              // 10. Timeout
            {
                return AdapterError.FromException<ExternalPricingApiService>(
                    new AdapterErrorKind.Timeout(TimeSpan.FromSeconds(30)),
                    ex);
            }
            catch (Exception ex)                          // 11. Other exceptions
            {
                return AdapterError.FromException<ExternalPricingApiService>(
                    new UnexpectedException(),
                    ex);
            }
        });
    }

    // Error mapping by HTTP status code
    private static Fin<T> HandleHttpError<T>(
        HttpResponseMessage response, string context) =>
        response.StatusCode switch
        {
            HttpStatusCode.NotFound => AdapterError.For<ExternalPricingApiService>(
                new NotFound(), context, "Resource not found"),

            HttpStatusCode.Unauthorized => AdapterError.For<ExternalPricingApiService>(
                new Unauthorized(), context, "Authentication failed"),

            HttpStatusCode.Forbidden => AdapterError.For<ExternalPricingApiService>(
                new Forbidden(), context, "Access forbidden"),

            HttpStatusCode.TooManyRequests => AdapterError.For<ExternalPricingApiService>(
                new RateLimited(), context, "Rate limit reached"),

            HttpStatusCode.ServiceUnavailable => AdapterError.For<ExternalPricingApiService>(
                new ExternalServiceUnavailable("ExternalPricingApi"),
                context, "Service unavailable"),

            _ => AdapterError.For<ExternalPricingApiService, HttpStatusCode>(
                new HttpError(), response.StatusCode,
                $"API call failed. Status: {response.StatusCode}")
        };
}
```

> **Reference**: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Infrastructure/ExternalApis/ExternalPricingApiService.cs`

**HTTP Status Code → AdapterErrorKind Mapping Reference**:

| HTTP Status Code | AdapterErrorKind | Description |
|---------------|------------------|------|
| 404 | `new NotFound()` | Resource not found |
| 401 | `new Unauthorized()` | Authentication failure |
| 403 | `new Forbidden()` | Access denied |
| 429 | `new RateLimited()` | Rate limit exceeded |
| 503 | `new ExternalServiceUnavailable(name)` | Service unavailable |
| Other | `new HttpError()` | General HTTP error |

**Exception → AdapterErrorKind Mapping Reference**:

| Exception Type | AdapterErrorKind | Description |
|----------|------------------|------|
| `HttpRequestException` | `new ConnectionFailed(name)` | Connection failure |
| `TaskCanceledException` (user) | `new OperationCancelled()` | Request cancelled |
| `TaskCanceledException` (timeout) | `new Timeout(timespan)` | Response timeout |
| `Exception` | `new UnexpectedException()` | Unexpected exception |

While Repository and External API handle synchronous request/response, Messaging Adapter handles asynchronous message-based inter-service communication.

### Messaging Adapter

Messaging Adapter implements inter-service communication through message brokers.

```csharp
// File: {Adapters}/Messaging/RabbitMqInventoryMessaging.cs

using Functorium.Adapters.SourceGenerators;
using static LanguageExt.Prelude;
using Wolverine;

[GenerateObservablePort]
public class RabbitMqInventoryMessaging : IInventoryMessaging
{
    private readonly IMessageBus _messageBus;              // 1. MessageBus injection

    public string RequestCategory => "Messaging";          // 2. Request category

    public RabbitMqInventoryMessaging(IMessageBus messageBus)  // 3. Constructor injection
    {
        _messageBus = messageBus;
    }

    // Request/Reply pattern
    public virtual FinT<IO, CheckInventoryResponse> CheckInventory(
        CheckInventoryRequest request)
    {
        return IO.liftAsync(async () =>                    // 4. IO.liftAsync
        {
            try
            {
                var response = await _messageBus
                    .InvokeAsync<CheckInventoryResponse>(request);  // 5. InvokeAsync
                return Fin.Succ(response);
            }
            catch (Exception ex)
            {
                return Fin.Fail<CheckInventoryResponse>(
                    Error.New(ex.Message));                 // 6. Error wrapping
            }
        });
    }

    // Fire-and-Forget pattern
    public virtual FinT<IO, Unit> ReserveInventory(
        ReserveInventoryCommand command)
    {
        return IO.liftAsync(async () =>
        {
            try
            {
                await _messageBus.SendAsync(command);      // 7. SendAsync
                return Fin.Succ(unit);
            }
            catch (Exception ex)
            {
                return Fin.Fail<Unit>(Error.New(ex.Message));
            }
        });
    }
}
```

> **Reference**: `Tutorials/Cqrs06Services/Src/OrderService/Adapters/Messaging/RabbitMqInventoryMessaging.cs`

**Messaging Adapter Core Patterns**:

| Pattern | API | Description |
|------|-----|------|
| Request/Reply | `_messageBus.InvokeAsync<TResponse>(request)` | Synchronous messaging that waits for a response |
| Fire-and-Forget | `_messageBus.SendAsync(command)` | Send message without waiting for response |
| Error wrapping | `Fin.Fail<T>(Error.New(ex.Message))` | Convert messaging exceptions to `Fin.Fail` |

##### Messaging ACL: When Message Schema Conversion Is Needed

The current example passes shared DTOs directly, which is valid for jointly designed contracts.
When integrating with external/legacy message schemas, apply ACL:

```
Receive: Broker Message → internal XxxMessage → Mapper → Domain Type (Port)
Send:    Domain Type (Port) → Mapper → internal XxxMessage → Broker Message
```

- Same pattern: `internal record` + `internal static class XxxMessageMapper`
- See [ACL Checklist by External System Type](#acl-checklist-by-external-system-type) for decision criteria

All Adapters covered so far belong to the Command side (write). Finally, let's look at the Query Adapter that handles the Read side of CQRS.

### Query Adapter (CQRS Read Side)

Query Adapter is an Adapter that handles the Read side of CQRS. It returns DTOs directly without Aggregate reconstruction and handles pagination/sorting at the DB level.

#### Technology Choice from CQRS Perspective

Here is a comparison of how technology choices differ between the Command side and the Query side.

| Aspect | Command Side (Repository) | Query Side (Query Adapter) |
|------|------------------------|------------------------|
| **ORM** | EF Core | **Dapper + explicit SQL** |
| **Reason** | Change tracking, UnitOfWork, migrations | Maximize performance, ease of SQL tuning |
| **Aggregate Reconstruction** | Yes — domain invariant validation needed | No — direct DTO return |
| **Data Modification** | Yes — Create/Update/Delete | No — read-only |
| **Pagination/Sorting** | No — post-processing after full retrieval | Yes — DB-level processing |
| **Interface Location** | Domain layer | Application layer |

**Decision Criteria**: Does the query result **require Aggregate reconstruction?**
- Yes → Repository (Command side, EF Core)
- No (direct DTO return) → Query Adapter (Query side, Dapper)

#### Why Dapper for the Query Side?

Following CQRS principles, the technology stacks for Command/Query are optimized independently:

- **Performance**: Dapper has less overhead compared to EF Core (no change tracking, no proxy generation)
- **SQL Tuning**: Query plan optimization possible with explicit SQL (JOIN, INDEX HINT, etc.)
- **Maintainability**: Clear per-query SQL makes performance bottleneck tracking easy
- **Technology Independence**: ORM changes on the Command side don't affect the Query side

#### Pagination/Sorting Framework Types

These are Application-level query concern types located in the `Functorium.Applications.Queries` namespace.

#### PageRequest — Offset-based Pagination

```csharp
var page = new PageRequest(page: 2, pageSize: 10);
// page.Skip == 10, page.Page == 2, page.PageSize == 10
// defaults: page=1, pageSize=20, max: 100
```

- `Page < 1` → Clamped to 1
- `PageSize < 1` → Clamped to DefaultPageSize(20)
- `PageSize > MaxPageSize(100)` → Clamped to MaxPageSize

#### PagedResult — Pagination Result

```csharp
var result = new PagedResult<ProductSummaryDto>(items, totalCount: 50, page: 2, pageSize: 10);
// result.TotalPages == 5, result.HasPreviousPage == true, result.HasNextPage == true
```

#### SortExpression — Multi-field Sorting

```csharp
// Single field
var sort = SortExpression.By("Name");

// Multiple fields
var sort = SortExpression.By("Price", SortDirection.Descending).ThenBy("Name");

// No sorting
var sort = SortExpression.Empty;
```

---

#### DapperQueryBase — Framework Base Class

A framework-provided base class located in the `Functorium.Adapters.Repositories` namespace.
Subclasses are only responsible for **SQL declarations and WHERE building**, while infrastructure (Search execution, ORDER BY, pagination, parameter helpers) is handled by the base.

```
Base Class (Infrastructure)              Subclass (SQL Declaration)
┌────────────────────────────────┐      ┌──────────────────────────────────┐
│ DapperQueryBase<T,TDto>        │      │ DapperProductQuery               │
│                                │      │   : DapperQueryBase<...>         │
│ • Search() — execution engine  │ ◄─── │   , IProductQuery                │
│ • SearchByCursor() — cursor    │      │                                  │
│ • Stream() — streaming         │      │ • SelectSql, CountSql            │
│ • BuildOrderByClause()        │      │ • DefaultOrderBy                 │
│ • Params() helper             │      │ • AllowedSortColumns             │
│ • IDbConnection holder         │      │ • BuildWhereClause() (optional)  │
└────────────────────────────────┘      └──────────────────────────────────┘
```

**Constructor Overloads:**

| Constructor | Description |
|--------|------|
| `base(connection)` | Default constructor. Must directly override `BuildWhereClause()` |
| `base(connection, translator, tableAlias)` | `DapperSpecTranslator`-based. Delegates WHERE translation to translator |

**What Subclasses Declare (abstract):**

| Member | Role | Example |
|------|------|------|
| `SelectSql` | Full SELECT statement (excluding WHERE/ORDER BY) | `"SELECT Id AS ProductId, Name, Price FROM Products"` |
| `CountSql` | Full COUNT statement (excluding WHERE) | `"SELECT COUNT(*) FROM Products"` |
| `DefaultOrderBy` | Default when sort is not specified | `"Name ASC"` |
| `AllowedSortColumns` | Allowlist of permitted sort fields | `{ ["Name"] = "Name", ["Price"] = "Price" }` |
| `BuildWhereClause()` | Spec → SQL WHERE + Parameters (virtual — no override needed when using translator) | `ProductPriceRangeSpec → "WHERE Price >= @Min ..."` |

> **Reference**: `Src/Functorium.Adapters/Repositories/DapperQueryBase.cs`

#### Dapper Query Implementation — Single Table

Key point: Just write the SQL declarations, and the base handles Search/ORDER BY/pagination.

```csharp
[GenerateObservablePort]
public class DapperProductQuery
    : DapperQueryBase<Product, ProductSummaryDto>, IProductQuery
{
    public string RequestCategory => "QueryAdapter";

    protected override string SelectSql => "SELECT Id AS ProductId, Name, Price FROM Products";
    protected override string CountSql => "SELECT COUNT(*) FROM Products";
    protected override string DefaultOrderBy => "Name ASC";
    protected override Dictionary<string, string> AllowedSortColumns { get; } =
        new(StringComparer.OrdinalIgnoreCase) { ["Name"] = "Name", ["Price"] = "Price" };

    public DapperProductQuery(IDbConnection connection)
        : base(connection, ProductSpecTranslator.Instance) { }
}
```

> **Reference**: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/Dapper/DapperProductQuery.cs`

#### Dapper Query Implementation — JOIN

Since `SelectSql`/`CountSql` are declared as whole statements, complex queries with JOIN, GROUP BY, etc. can be written freely.

```csharp
[GenerateObservablePort]
public class DapperProductWithStockQuery
    : DapperQueryBase<Product, ProductWithStockDto>, IProductWithStockQuery
{
    public string RequestCategory => "QueryAdapter";

    protected override string SelectSql =>
        "SELECT p.Id AS ProductId, p.Name, p.Price, i.StockQuantity " +
        "FROM Products p INNER JOIN Inventories i ON i.ProductId = p.Id";
    protected override string CountSql =>
        "SELECT COUNT(*) FROM Products p INNER JOIN Inventories i ON i.ProductId = p.Id";
    protected override string DefaultOrderBy => "p.Name ASC";
    protected override Dictionary<string, string> AllowedSortColumns { get; } =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Name"] = "p.Name",
            ["Price"] = "p.Price",
            ["StockQuantity"] = "i.StockQuantity"
        };

    public DapperProductWithStockQuery(IDbConnection connection)
        : base(connection, ProductSpecTranslator.Instance, "p") { }
}
```

> **Reference**: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/Dapper/DapperProductWithStockQuery.cs`

#### Specification → SQL WHERE Translation (DapperSpecTranslator)

`DapperSpecTranslator<T>` is a Fluent API-based translator that converts Specifications to SQL WHERE clauses. Instead of directly overriding `BuildWhereClause()` in Query subclasses, pass the translator to the constructor and the base class delegates automatically.

```csharp
public static class ProductSpecTranslator
{
    public static readonly DapperSpecTranslator<Product> Instance = new DapperSpecTranslator<Product>()
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
            return ($"WHERE {p}DeletedAt IS NULL AND {p}Price >= @MinPrice AND {p}Price <= @MaxPrice", @params);
        });
}
```

**DapperSpecTranslator Fluent API:**

| Method | Description |
|--------|------|
| `.WhenAll(alias => ...)` | Handles `Specification.All` (default condition) |
| `.When<TSpec>((spec, alias) => ...)` | Handles specific Specification types |
| `Translate(spec, tableAlias)` | Returns `(string Where, DynamicParameters Params)` |

**Static Helpers:**

| Helper | Description | Example |
|------|------|------|
| `Prefix(alias)` | Returns `"p."` if table alias exists, `""` otherwise | `Prefix("p")` → `"p."` |
| `Params(...)` | Creates `DynamicParameters` | `Params(("MinPrice", 100m))` |

#### Dapper SQL Writing Checklist

- [ ] Are all WHERE condition values bound with `@Parameter`? (no string concatenation)
- [ ] Does `SelectSql`/`CountSql` not include `WHERE`/`ORDER BY`? (handled by base class)
- [ ] Do column aliases match DTO property names? (e.g., `Id AS ProductId`)
- [ ] Are table aliases used in JOINs? (e.g., `p.Name`, `i.StockQuantity`)
- [ ] Are all sortable fields registered in `AllowedSortColumns`?
- [ ] Is a valid default sort specified in `DefaultOrderBy`?
- [ ] Is `NotSupportedException` thrown for unsupported Specifications?

#### SQL Injection Prevention (Triple Protection)

| Layer | Protection Method | Location |
|------|----------|------|
| Application Validator | `AllowedSortFields` validation | FluentValidation (Request validation) |
| Adapter Allowlist | `AllowedSortColumns` Dictionary lookup → unregistered fields fall back to default sort | Query Adapter |
| Dapper Parameters | All values bound with `@Parameter`, no string concatenation | SQL execution |

#### InMemory Query Implementation

`InMemoryQueryBase<TEntity, TDto>` is the InMemory counterpart base class to `DapperQueryBase`.
Subclasses are only responsible for **data source access (`GetProjectedItems`) and sort key (`SortSelector`)**, while Search/Stream/pagination are handled by the base.

```csharp
[GenerateObservablePort]
public class InMemoryProductQuery
    : InMemoryQueryBase<Product, ProductSummaryDto>, IProductQuery
{
    public string RequestCategory => "QueryAdapter";

    protected override string DefaultSortField => "Name";

    protected override IEnumerable<ProductSummaryDto> GetProjectedItems(Specification<Product> spec)
    {
        return InMemoryProductRepository.Products.Values
            .Where(p => p.DeletedAt.IsNone && spec.IsSatisfiedBy(p))
            .Select(p => new ProductSummaryDto(p.Id.ToString(), p.Name, p.Price));
    }

    protected override Func<ProductSummaryDto, object> SortSelector(string fieldName) => fieldName switch
    {
        "Name" => p => p.Name,
        "Price" => p => p.Price,
        _ => p => p.Name
    };
}
```

> **Reference**: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/InMemory/Products/InMemoryProductQuery.cs`

**InMemoryQueryBase Features:**

| Member | Type | Description |
|------|------|------|
| `DefaultSortField` | `abstract string` | Default field name when sort is not specified |
| `GetProjectedItems()` | `abstract` | Filtering + DTO projection (including JOIN logic) |
| `SortSelector()` | `abstract` | Field name → sort key selector function |
| `Search()` | `virtual` | Offset-based pagination search (provided by base) |
| `SearchByCursor()` | `virtual` | Cursor-based pagination search (provided by base) |
| `Stream()` | `virtual` | `IAsyncEnumerable<TDto>` streaming (provided by base) |

- InMemory is for testing, so Aggregate reconstruction cost is negligible
- Queries data by directly referencing the `InMemoryProductRepository.Products` static field

---

## Troubleshooting

### CS0506 Build Error Due to Missing virtual Keyword

**Cause:** Pipeline classes inherit from the original Adapter class and `override` methods. Without `virtual`, overriding is not possible and a `CS0506: cannot override because it is not virtual` error occurs.

**Resolution:**
```csharp
// Before - build error
public FinT<IO, Product> GetById(ProductId id) { ... }

// After - Pipeline override possible
public virtual FinT<IO, Product> GetById(ProductId id) { ... }
```

### Compile Error When Using await Inside IO.lift

**Cause:** `IO.lift` only accepts synchronous lambdas. To use `await` inside, you must use `IO.liftAsync`.

**Resolution:**
```csharp
// Before - compile error
return IO.lift(() => { var result = await _httpClient.GetAsync(url); ... });

// After - use IO.liftAsync for async operations
return IO.liftAsync(async () => { var result = await _httpClient.GetAsync(url); ... });
```

### Mapper Class Exposed as public Breaks Domain Boundary

**Cause:** If a Mapper class inside the Adapter is declared as `public`, external projects can access the technical concern conversion logic, breaking layer boundaries.

**Resolution:**
```csharp
// Before - externally exposed
public static class ProductMapper { ... }

// After - restricted to Adapter project internal
internal static class ProductMapper { ... }
```

---

## FAQ

### Q1. Which should I use, IO.lift or IO.liftAsync?

Use `IO.liftAsync` if you need to use `await` internally, otherwise use `IO.lift`. Use `IO.lift` for In-Memory stores or cache lookups, and `IO.liftAsync` for HTTP calls or async DB queries. For async DB access such as EF Core, `IO.liftAsync` is also used in Repositories.

### Q2. Can't I throw Exceptions when returning errors from an Adapter?

Throwing exceptions bypasses the Pipeline's error handling flow. Instead, return `Fin.Fail` with `AdapterError.For<T>(errorType, context, message)` to maintain the functional error handling chain. Exceptions from external libraries are converted with `AdapterError.FromException<T>(errorType, ex)`.

### Q3. Why separate Persistence Model (POCO) from domain Entity?

Domain Entities protect business invariants, while Persistence Models are simple POCOs matching the DB schema. Separation means DB schema changes don't affect the domain model, and domain model evolution occurs independently of DB migrations.

### Q4. Why doesn't the Query Adapter reconstruct Aggregates?

The Query Adapter handles the Read side of CQRS, and domain invariant validation is unnecessary for read-only queries. It avoids Aggregate reconstruction costs and optimizes query performance by returning DTOs directly. Direct SQL queries can be executed with Dapper, etc.

### Q5. What happens if the [GenerateObservablePort] attribute is not applied?

Since no Pipeline class is generated, logging, tracing, and metrics are not automatically applied. The Adapter is directly registered in DI as the Port interface, and Observability code must be written manually.

---

## References

| Document | Description |
|------|------|
| [12-ports.md](../12-ports) | Port architecture, IObservablePort hierarchy, Port definition rules |
| [14a-adapter-pipeline-di.md](../14a-adapter-pipeline-di) | Pipeline generation, DI registration, Options pattern |
| [14b-adapter-testing.md](../14b-adapter-testing) | Adapter unit testing, E2E Walkthrough |
| [15a-unit-testing.md](../testing/15a-unit-testing) | Unit testing guide |
| [08a-error-system.md](../domain/08a-error-system) | Error system: basics and naming |
| [08b-error-system-domain-app.md](../domain/08b-error-system-domain-app) | Error system: Domain/Application errors |
| [08c-error-system-adapter-testing.md](../domain/08c-error-system-adapter-testing) | Error system: Adapter errors and testing |

---

## Related Documents

- Port interface definition rules: [Port Definition](../12-ports)
- Pipeline generation and DI registration: [Adapter Integration -- Pipeline and DI](../14a-adapter-pipeline-di)
- Adapter unit testing: [Adapter Testing](../14b-adapter-testing)
