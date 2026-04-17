---
title: "DTO Strategy"
---

When transferring data between layers, the uncontrolled proliferation of DTOs is a common problem. This guide defines DTO ownership per layer and specifies the conditions under which reuse is permitted, preventing DTO explosion.

## Introduction

- Which layer owns a DTO, and what rules should be followed when passing DTOs between layers?
- How do we prevent increased coupling between layers when DTOs are shared indiscriminately?
- What criteria determine whether it is acceptable to reuse Application DTOs in read-only scenarios?

To address these problems, we establish per-layer DTO ownership principles and transformation patterns.

### What You Will Learn

This document covers the following topics:

1. **Per-layer DTO ownership rules** - DTO forms and ownership locations for Presentation, Application, and Persistence layers
2. **DTO reuse vs separation criteria** - 4 conditions under which Application DTO reuse is permitted
3. **Transformation patterns and mapping strategies** - Mapper pattern, collection type conversion, VO implicit conversion

> **The core of the DTO strategy is** that each layer owns its own DTOs and explicitly transforms them when crossing boundaries.

## Summary

### Key Commands

```csharp
// Endpoint nested record (Presentation Layer)
public sealed record Request(string Name, decimal Price);
public new sealed record Response(string ProductId, string Name);

// Usecase nested record (Application Layer)
public sealed record Request(string Name, decimal Price) : ICommandRequest<Response>;
public sealed record Response(string ProductId, string Name);

// Persistence Mapper (internal static)
internal static class ProductMapper
{
    public static ProductModel ToModel(this Product product) => new() { ... };
    public static Product ToDomain(this ProductModel model) => Product.CreateFromValidated(...);
}

// Collection conversion
result.Map(r => new Response(r.Products));  // PagedResult.Items is already IReadOnlyList<T>
return Fin.Succ(toSeq(models.Select(m => m.ToDomain())));  // List → Seq
```

### Key Procedures

**1. DTO Design:**
1. Verify DTO ownership per layer (Presentation, Application, Persistence)
2. Define Request/Response as Usecase nested records
3. Implement cross-layer transformation with Mappers

**2. Application DTO Reuse Decision:**
1. Verify it is a read-only Query response
2. Verify fields are identical, resulting in identity mapping
3. Verify Presentation-specific fields are unnecessary
4. Verify collection type conversion is minimal (`PagedResult.Items` is already `IReadOnlyList<T>`)
5. Allow reuse when all 4 conditions are met

### Key Concepts

| Concept | Description |
|------|------|
| Per-layer DTO ownership | Each layer owns its own data representation, ensuring independent evolution |
| Usecase nested record | Request/Response defined as nested types inside Command/Query classes |
| Persistence Model | POCO using only primitive types, no domain dependencies |
| Mapper pattern | Bidirectional Domain <-> Model conversion via `internal static` extension methods |
| `Seq<T>` vs `List<T>` | Application domain collections use `Seq<T>`, Presentation/Persistence use `List<T>`. `PagedResult.Items` is `IReadOnlyList<T>` |

---

## Why Per-Layer DTOs Are Needed

In Hexagonal Architecture, each layer (Port/Adapter) owns its own data representation. This ensures independent evolution between layers.

The following table compares the impact scope of each change scenario when using shared DTOs versus per-layer DTOs.

| Problem Scenario | Using Shared DTOs | Using Per-Layer DTOs |
|----------|-------------|-----------------|
| API field addition | Application also needs modification | Only Presentation is modified |
| DB column change | Affects Domain | Only Persistence Adapter is modified |
| Serialization format change | Affects all layers | Only Adapter is modified |
| Type system differences | Compromise needed (`Seq` vs `List`) | Each layer uses optimal types |

Now that we understand the need for per-layer DTOs, let us examine what form of DTO each layer owns and how data is transformed as it passes through layers.

---

## Per-Layer DTO Ownership (WHAT)

```
HTTP Request
  → Endpoint.Request (Presentation, primitive)
    → Usecase.Request (Application, primitive)
      → Domain Entity (Domain, Value Objects)
        → ProductModel (Persistence, POCO)
          → Database

Database
  → ProductModel (Persistence, POCO)
    → Domain Entity (via CreateFromValidated + Mapper)
      → Usecase.Response (Application, primitive)
        → Endpoint.Response (Presentation, primitive)
          → HTTP Response
```

The following table summarizes the DTO form, type characteristics, and ownership location per layer.

| Layer | DTO Form | Type Characteristics | Ownership Location |
|--------|----------|----------|----------|
| Presentation | Endpoint nested record | primitive (JSON serialization) | Inside Endpoint class |
| Application | Usecase nested record | primitive (serializable) | Inside Usecase class |
| Application (shared) | Independent record | primitive | Query Port file or `Usecases/{Aggregate}/Dtos/` |
| Persistence | Model (POCO) | primitive (DB mapping) | `Repositories/EfCore/Models/` |

Now that we understand the ownership structure, let us examine how DTOs are actually implemented in each layer with code.

---

## Per-Layer DTO Implementation (HOW)

### Presentation Layer

**Default**: Endpoint nested record -- Each Endpoint owns its own Request/Response.

```csharp
// CreateProductEndpoint.cs
public sealed class CreateProductEndpoint
    : Endpoint<CreateProductEndpoint.Request, CreateProductEndpoint.Response>
{
    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        // [Transform A] Endpoint Request → Usecase Request
        var usecaseRequest = new CreateProductCommand.Request(
            req.Name, req.Description, req.Price, req.StockQuantity);

        var result = await _mediator.Send(usecaseRequest, ct);

        // [Transform B] Usecase Response → Endpoint Response
        var mapped = result.Map(r => new Response(
            r.ProductId, r.Name, r.Description, r.Price, r.StockQuantity, r.CreatedAt));

        await this.SendCreatedFinResponseAsync(mapped, ct);
    }

    public sealed record Request(string Name, string Description, decimal Price, int StockQuantity);
    public new sealed record Response(string ProductId, string Name, string Description,
        decimal Price, int StockQuantity, DateTime CreatedAt);
}
```

**Exception**: Application DTO reuse -- If the [permitted conditions](#application-dto-reuse-permitted-conditions) are met, Application DTOs can be used directly in Endpoint Response.

```csharp
// GetAllProductsEndpoint.cs — Application DTO reuse example
using LayeredArch.Application.Usecases.Products.Ports;

public sealed class GetAllProductsEndpoint
    : EndpointWithoutRequest<GetAllProductsEndpoint.Response>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAllProductsQuery.Request(), ct);
        // Only Seq → List conversion is performed, the DTO itself is reused
        var mapped = result.Map(r => new Response(r.Products.ToList()));
        await this.SendFinResponseAsync(mapped, ct);
    }

    // Response directly references Application's ProductSummaryDto
    public new sealed record Response(List<ProductSummaryDto> Products);
}
```

### Application Layer

**Default**: Usecase nested record -- Each Command/Query owns its own Request/Response.

```csharp
// CreateProductCommand.cs
public sealed class CreateProductCommand
{
    public sealed record Request(string Name, string Description,
        decimal Price, int StockQuantity) : ICommandRequest<Response>;

    public sealed record Response(string ProductId, string Name, string Description,
        decimal Price, int StockQuantity, DateTime CreatedAt);
}
```

**Shared DTOs**: When multiple Usecases need the same DTO, define it alongside the Query Port interface file or separate it as an independent record in the `Dtos/` folder.

```
Application/Usecases/Products/
├── IProductQuery.cs              ← Query Port + ProductSummaryDto definition
├── GetAllProductsQuery.cs        ← Response references ProductSummaryDto
└── SearchProductsQuery.cs        ← Response references ProductSummaryDto
```

```csharp
// IProductQuery.cs — Query Port and shared DTO defined together
namespace LayeredArch.Application.Usecases.Products.Ports;

public interface IProductQuery : IQueryPort<Product, ProductSummaryDto> { }

public sealed record ProductSummaryDto(
    string ProductId,
    string Name,
    decimal Price);
```

**Domain to Application DTO conversion**: Value Object `implicit operator` provides natural conversion to primitives.

```csharp
// Inside Usecase — VO → primitive implicit conversion
new ProductSummaryDto(p.Id.ToString(), p.Name, p.Price)
//                     ↑ Ulid→string    ↑ ProductName→string  ↑ Money→decimal
```

### Persistence Layer

**Model (POCO)**: Uses only primitive types with no domain dependencies.

```csharp
// Models/ProductModel.cs
public class ProductModel
{
    public string Id { get; set; } = default!;       // Ulid → string
    public string Name { get; set; } = default!;     // ProductName → string
    public decimal Price { get; set; }                // Money → decimal
    public int StockQuantity { get; set; }            // Quantity → int
    // ...
}
```

**Mapper**: Provides bidirectional conversion via `internal static class` extension methods.

```csharp
// Mappers/ProductMapper.cs
internal static class ProductMapper
{
    public static ProductModel ToModel(this Product product) => new()
    {
        Id = product.Id.ToString(),
        Name = product.Name,         // implicit: ProductName → string
        Price = product.Price,       // implicit: Money → decimal
        // ...
    };

    public static Product ToDomain(this ProductModel model)
    {
        var product = Product.CreateFromValidated(   // Restore without validation
            ProductId.Create(model.Id),
            ProductName.CreateFromValidated(model.Name),
            // ...
        );
        product.ClearDomainEvents();  // Remove side-effect events from restoration
        return product;
    }
}
```

| Design Point | Description |
|------------|------|
| `internal` access restriction | Mapper is an implementation detail of the Persistence Adapter |
| Extension method | Natural call syntax (`product.ToModel()`) |
| `CreateFromValidated` | Skips validation during DB restoration for performance |
| `ClearDomainEvents()` | Removes side-effect events from the restoration process (DDD principle) |

Now that we have reviewed the per-layer implementation patterns, let us address the collection type conversion issues that frequently arise during cross-layer data transfer.

---

## Collection Type Conversion

Application Layer domain collections use `Seq<T>` (LanguageExt FP type), while Presentation/Persistence use `List<T>` (JSON serialization/EF Core compatible). However, `PagedResult<T>.Items` is `IReadOnlyList<T>`, so it can be used in Presentation without additional conversion.

```
Application (Seq<T>) ──.ToList()──→ Presentation (List<T>)
Application (Seq<T>) ──.ToList()──→ Persistence  (List<T>)
Persistence (List<T>) ──toSeq()───→ Application  (Seq<T>)
```

```csharp
// Presentation: PagedResult.Items is IReadOnlyList<T> — no conversion needed
var mapped = result.Map(r => new Response(r.Products));

// Domain Seq<T> collection: Seq → List (converted in Endpoint)
var mapped = result.Map(r => new Response(r.Items.ToList()));  // Seq<T> → List<T>

// Persistence: List → Seq (converted in Repository)
return Fin.Succ(toSeq(models.Select(m => m.ToDomain())));
```

> **Note**: `Seq<T>` cannot be serialized with System.Text.Json, so it must be converted to `List<T>` at the Presentation boundary.

---

## Application DTO Reuse Permitted Conditions

The default principle is that each layer owns its own DTOs. The following 4 conditions define practical exceptions to this principle. When **all 4 conditions** are met, Application DTOs can be directly reused in Presentation:

| # | Condition | Rationale |
|---|------|------|
| 1 | It is a **read-only Query** response | Command results have a high potential for per-layer evolution |
| 2 | **Fields are identical**, resulting in identity mapping | If field additions/removals are planned, maintain separation |
| 3 | Presentation-specific fields (HATEOAS links, etc.) are **not needed** | If specific fields are needed, Endpoint DTO is required |
| 4 | **Collection type conversion is minimal** | `PagedResult.Items` is `IReadOnlyList<T>`, so no conversion needed. Only domain `Seq<T>` requires `List<T>` conversion |

**Application example**: `GetAllProductsEndpoint` directly references `ProductSummaryDto`, performing only `Seq → List` conversion in the Response wrapper.

**Disengagement point**: When any of the 4 conditions breaks, switch to a dedicated Endpoint DTO.

---

## Troubleshooting

### `Seq<T>` JSON Serialization Failure

**Cause:** `Seq<T>` is a LanguageExt FP type that cannot be serialized with `System.Text.Json`. Directly returning a Response containing `Seq<T>` in the Presentation Layer causes a serialization error.

**Resolution:** Always convert to `List<T>` at the Presentation boundary.

```csharp
// Seq → List conversion in Endpoint
var mapped = result.Map(r => new Response(r.Products.ToList()));
```

### Duplicate Domain Events in Persistence Mapper

**Cause:** When restoring an Entity from the DB using the `Create()` factory, a creation event is published. Since we are restoring existing data, the event should not be published.

**Resolution:** Use `CreateFromValidated()` in the Mapper's `ToDomain()`, and call `ClearDomainEvents()` after restoration to remove side-effect events.

```csharp
public static Product ToDomain(this ProductModel model)
{
    var product = Product.CreateFromValidated(...);
    product.ClearDomainEvents();
    return product;
}
```

### Endpoint and Usecase Response Fields Require Constant Synchronization

**Cause:** Presentation and Application DTOs are defined separately with identical fields, resulting in identity mapping where changing one side requires modifying the other.

**Resolution:** If all Application DTO reuse permitted conditions are met (read-only Query, identical fields, no Presentation-specific fields needed, only collection conversion required), directly reuse the Application DTO. When any condition breaks, switch to a dedicated Endpoint DTO.

---

## FAQ

### Q: Why do Usecase Request/Response use primitive types?

Usecase Request/Response are located at the **external API boundary** (called from Presentation). Primitive types (`string`, `decimal`, `int`) are used for JSON serialization compatibility and external contract stability. In contrast, Port interfaces are **internal contracts** (Application <-> Adapter), so domain Value Objects are used.

### Q: Is Application DTO reuse a violation of Hexagonal Architecture?

In principle, each layer should own independent DTOs. However, in read-only scenarios where identity mapping (1:1 copy of identical fields) occurs, reuse is permitted as a pragmatic decision. Since this aligns with the dependency direction (Presentation -> Application), it does not violate architecture rules.

### Q: Why use Mapper instead of `HasConversion` in Persistence Models?

EF Core `HasConversion` is applied directly to domain Entities, coupling the domain to the ORM. The Mapper pattern completely separates domain Entities from Persistence Models (POCOs), ensuring **Persistence Ignorance.**

### Q: When should shared DTOs (`Dtos/` folder) be created?

When 2 or more Usecases need the same DTO. If only a single Usecase uses it, keep it as a Usecase nested record.

---

## References

- [11-usecases-and-cqrs.md](../11-usecases-and-cqrs) -- Usecase Request/Response pattern
- [12-ports.md §1.4](../adapter/12-ports) -- Port Request/Response design
- [13-adapters.md §2.6](../adapter/13-adapters) -- Data transformation (Mapper pattern)
- [01-project-structure.md](../architecture/01-project-structure) -- Dtos/ folder location rules
- [dto-strategy-review.md](../../.claude/dto-strategy-review.md) -- DTO mapping strategy review (DDD & Hexagonal perspective)
