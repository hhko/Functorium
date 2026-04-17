---
title: "Specification Pattern"
---

This document explains how to define and use the Specification pattern in the Functorium framework.

## Introduction

"Do we need to add a method to the Repository every time a new filter condition is needed?"
"Where do we encapsulate the business rule 'price is between 100-200 and stock is 5 or more'?"
"How do we reuse the same business conditions in InMemory tests and EF Core production environments?"

These problems manifest as business rules being scattered across Repository implementations, or interfaces becoming bloated as condition combinations grow. The Specification pattern encapsulates business rules as independent domain objects and composes complex conditions with `And`/`Or`/`Not` combinations.

### What You Will Learn

1. **Problems the Specification pattern solves** -- Preventing Repository method explosion and encapsulating business rules
2. **`ExpressionSpecification<T>` implementation pattern** -- `ToExpression()` definition and automatic SQL translation
3. **Composition and identity element** -- `&`/`|`/`!` operators and `Specification<T>.All`
4. **Repository/Usecase integration** -- Usage in InMemory, EF Core, Dapper environments

### Prerequisites

- [Entity/Aggregate Core Patterns](../06b-entity-aggregate-core) -- Basic structure of Entity and Aggregate
- [Adapter Implementation Guide](../adapter/13-adapters) -- Repository implementation patterns

> The core value of the Specification pattern is **encapsulating business rules as domain objects for reuse, and expressing complex conditions as compositions of simple conditions using combination operators.**

## Summary

### Key Commands

```csharp
// Specification definition
public sealed class ProductPriceRangeSpec : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression() { ... }
}

// Specification composition
var spec = priceRange & !lowStock;          // operator style
var spec = priceRange.And(lowStock.Not());  // method style

// Optional filter composition (All identity element)
var spec = Specification<Product>.All;
spec &= new ProductPriceRangeSpec(min, max);

// Used in Repository
_productRepository.Exists(new ProductNameUniqueSpec(productName));
_productRepository.FindAll(spec);
```

### Key Procedures

1. **Specification definition**: Inherit `ExpressionSpecification<T>`, implement `ToExpression()`
2. **Value Object conversion**: Convert Value Objects to primitives inside `ToExpression()` then capture in closure
3. **Add Repository Port**: Define `Exists(Specification<T>)`, `FindAll(Specification<T>)` methods
4. **Adapter implementation**: InMemory uses `IsSatisfiedBy()`, EfCore uses `PropertyMap` + `SpecificationExpressionResolver`
5. **Usecase integration**: Pass single Spec or combine with `&` / `|` / `!` to Repository

### Key Concepts

| Concept | Description |
|------|------|
| `ExpressionSpecification<T>` | Expression-based abstract class, supports automatic SQL translation |
| `IsSatisfiedBy()` | Auto-implemented from `ToExpression()` compilation (cached) |
| `And()` / `Or()` / `Not()` | Combination methods, `&` / `\|` / `!` operator overloads |
| `Specification<T>.All` | Identity element (Null Object), initial value for optional filter composition |
| `PropertyMap<TEntity, TModel>` | Entity Expression -> Model Expression automatic conversion |

First we understand the problems the Specification pattern solves, then proceed through definition and implementation to Repository and Usecase integration.

---

## Why the Specification Pattern

The Specification pattern is a building block in DDD that **encapsulates business rules and makes them composable.**

### Problems Specifications Solve

**Business Rule Encapsulation**:
When conditions like "price between 100 and 200" or "stock below 5" are scattered across Repository methods, reuse becomes difficult. Specifications encapsulate these conditions as independent domain objects.

**Preventing Repository Method Explosion**:
Adding a method to the Repository for each new filter condition bloats the interface. A single `Exists(spec)`/`FindAll(spec)` method that accepts Specifications handles all conditions.

**Composability**:
Simple Specifications can be composed into complex business rules using `And`, `Or`, `Not` combinations. Each Specification maintains single responsibility.

### Without vs With Specifications

```csharp
// ❌ Without Specification: add Repository method per condition
public interface IProductRepository
{
    FinT<IO, bool> ExistsByName(ProductName name, ProductId? excludeId = null);
    FinT<IO, Seq<Product>> FindByPriceRange(Money min, Money max);
    FinT<IO, Seq<Product>> FindByLowStock(Quantity threshold);
    FinT<IO, Seq<Product>> FindByPriceRangeAndLowStock(Money min, Money max, Quantity threshold);
    // Methods grow explosively as combinations increase...
}

// ✅ With Specification: generic methods + composition
public interface IProductRepository
{
    FinT<IO, bool> Exists(Specification<Product> spec);
    FinT<IO, Seq<Product>> FindAll(Specification<Product> spec);
}
```

---

## What Are Specifications (WHAT)

### `Specification<T>` Abstract Class

Located in the `Functorium.Domains.Specifications` namespace.

```csharp
public abstract class Specification<T>
{
    // Check if entity satisfies the condition
    public abstract bool IsSatisfiedBy(T entity);

    // Method composition
    public Specification<T> And(Specification<T> other);
    public Specification<T> Or(Specification<T> other);
    public Specification<T> Not();

    // Operator overloads
    public static Specification<T> operator &(Specification<T> left, Specification<T> right);
    public static Specification<T> operator |(Specification<T> left, Specification<T> right);
    public static Specification<T> operator !(Specification<T> spec);
}
```

### Composition Methods

Two styles are supported: methods and operators:

```csharp
// Method style
var spec = priceRange.And(lowStock.Not());

// Operator style (same result)
var spec = priceRange & !lowStock;
```

### Internal Composition Classes

Composition classes are `internal sealed` and used only within the framework. The following table summarizes each composition method and behavior.

| Class | Creation Method | Behavior |
|--------|----------|------|
| `AndSpecification<T>` | `And()` / `&` | `true` when both sides are satisfied |
| `OrSpecification<T>` | `Or()` / `\|` | `true` when either side is satisfied |
| `NotSpecification<T>` | `Not()` / `!` | Inverted |

### `Specification<T>.All` (Identity Element)

`Specification<T>.All` is a Null Object Specification that satisfies all entities. It acts as the identity element for the `&` operation:

```csharp
// All & X = X, X & All = X (identity element)
Specification<Product>.All & priceRange  // → priceRange
priceRange & Specification<Product>.All  // → priceRange
```

**Primary use -- initial value for optional filter composition**:

When filter conditions are optional, using `All` as the initial value instead of `null` enables progressive composition with the `&` operator without null checks:

```csharp
private static Specification<Product> BuildSpecification(Request request)
{
    var spec = Specification<Product>.All;  // Start with All instead of null

    // Option<T>.Iter(): Add filter if Some, ignore if None
    request.Name.Iter(name =>
        spec &= new ProductNameSpec(ProductName.Create(name).ThrowIfFail()));

    // Bind().Map().Iter(): Add range filter only when both Options are Some
    request.MinPrice.Bind(min => request.MaxPrice.Map(max => (min, max)))
        .Iter(t => spec &= new ProductPriceRangeSpec(
            Money.Create(t.min).ThrowIfFail(),
            Money.Create(t.max).ThrowIfFail()));

    return spec;  // Return All as-is if no filters -> full query
}
```

The combination of `Option<T>` and `Iter()` is key. Instead of primitive type-based existence checks like `if (value.Length > 0)`, `Option<T>` **expresses the presence or absence of a value at the type level**. `Iter()` executes an action only when `Some`, making filter composition code declarative. When two filters must exist as a pair, the `Bind().Map().Iter()` chain pattern is used to **execute only when both Options are Some**.

`AllSpecification<T>` inherits from `ExpressionSpecification<T>`, so EfCore `PropertyMap` translation works correctly (`_ => true`).

| Property/Method | Description |
|------------|------|
| `Specification<T>.All` | `AllSpecification<T>.Instance` (singleton) |
| `IsAll` | Returns `true`. Used for identity element optimization in `&` operator |
| `ToExpression()` | `_ => true` |

### Position in Functorium Type Hierarchy

```
Functorium.Domains.Specifications
├── Specification<T>              (abstract base class)
│   ├── IsSatisfiedBy()           (abstract method)
│   ├── And() / Or() / Not()     (combination methods)
│   └── & / | / !                (operator overloads)
├── ExpressionSpecification<T>    (abstract, Expression-based -- recommended)
│   ├── ToExpression()            (abstract method)
│   └── IsSatisfiedBy()           (auto-implemented, delegate caching)
├── IExpressionSpec<T>            (Expression provider interface)
├── AllSpecification<T>            (internal sealed, Null Object)
├── AndSpecification<T>           (internal sealed)
├── OrSpecification<T>            (internal sealed)
└── NotSpecification<T>           (internal sealed)

Functorium.Domains.Specifications.Expressions
├── SpecificationExpressionResolver  (And/Or/Not Expression composition)
└── PropertyMap<TEntity, TModel>     (Entity -> Model Expression conversion)
```

Now that we understand Specification concepts and composition, let us move on to implementation.

---

## Specification Implementation (HOW)

### Folder Structure

```
LayeredArch.Domain/
└── AggregateRoots/
    └── Products/
        ├── Product.cs
        ├── Ports/
        │   └── IProductRepository.cs
        └── Specifications/           <- Placed under Aggregate
            ├── ProductNameUniqueSpec.cs
            ├── ProductPriceRangeSpec.cs
            └── ProductLowStockSpec.cs
```

**Namespace**: `{Project}.Domain.AggregateRoots.{Aggregate}.Specifications`

### Basic Structure (template)

Pattern of converting Value Objects to primitives inside `ToExpression()` then capturing in closures.

```csharp
using System.Linq.Expressions;
using Functorium.Domains.Specifications;

namespace {Project}.Domain.AggregateRoots.{Aggregate}.Specifications;

public sealed class {Aggregate}{Condition}Spec : ExpressionSpecification<{Aggregate}>
{
    public {ValueObjectType} {PropertyName} { get; }

    public {Aggregate}{Condition}Spec({ValueObjectType} {paramName})
    {
        {PropertyName} = {paramName};
    }

    public override Expression<Func<{Aggregate}, bool>> ToExpression()
    {
        // Convert Value Object -> primitive then capture in closure
        var {paramPrimitive} = ({PrimitiveType}){PropertyName};
        return entity => ({PrimitiveType})entity.{EntityProperty} == {paramPrimitive};
    }
    // IsSatisfiedBy() is auto-implemented via ToExpression() compilation
}
```

**Key Rules:**
- Inherit `ExpressionSpecification<T>` (supports Expression-based automatic SQL translation)
- Convert Value Objects to primitives in `ToExpression()` to capture in closures
- Use `(primitiveType)entity.Property` cast when accessing Entity properties
- `IsSatisfiedBy()` is auto-implemented with internal caching of `ToExpression()` compilation result -- no separate implementation needed

### Practical Examples

#### Product Name Uniqueness Check (ProductNameUniqueSpec)

```csharp
public sealed class ProductNameUniqueSpec : ExpressionSpecification<Product>
{
    public ProductName Name { get; }
    public ProductId? ExcludeId { get; }

    public ProductNameUniqueSpec(ProductName name, ProductId? excludeId = null)
    {
        Name = name;
        ExcludeId = excludeId;
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        string nameStr = Name;
        string? excludeIdStr = ExcludeId?.ToString();
        return product => (string)product.Name == nameStr &&
                          (excludeIdStr == null || product.Id.ToString() != excludeIdStr);
    }
}
```

#### Price Range (ProductPriceRangeSpec)

```csharp
public sealed class ProductPriceRangeSpec : ExpressionSpecification<Product>
{
    public Money MinPrice { get; }
    public Money MaxPrice { get; }

    public ProductPriceRangeSpec(Money minPrice, Money maxPrice)
    {
        MinPrice = minPrice;
        MaxPrice = maxPrice;
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        decimal min = MinPrice;
        decimal max = MaxPrice;
        return product => (decimal)product.Price >= min && (decimal)product.Price <= max;
    }
}
```

#### Low Stock (ProductLowStockSpec)

```csharp
public sealed class ProductLowStockSpec : ExpressionSpecification<Product>
{
    public Quantity Threshold { get; }

    public ProductLowStockSpec(Quantity threshold)
    {
        Threshold = threshold;
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        int threshold = Threshold;
        return product => (int)product.StockQuantity < threshold;
    }
}
```

### Value Object Conversion Pattern in Expressions

When converting Value Objects to primitives in `ToExpression()`:

```csharp
// ✅ Convert to primitive before closure capture
decimal min = MinPrice;  // Value Object → primitive (implicit operator)
return product => (decimal)product.Price >= min;

// ✅ EntityId converted with ToString()
string? excludeIdStr = ExcludeId?.ToString();
return product => product.Id.ToString() != excludeIdStr;

// ❌ Direct Value Object comparison inside Expression (PropertyMap cannot convert)
return product => product.Price >= MinPrice;
```

Now that Specification implementation is complete, let us see how to integrate with the Repository for actual data queries.

---

## Usage in Repository (HOW)

### Port Definition (Domain Layer)

Add methods that accept Specifications to the Repository interface:

```csharp
public interface IProductRepository : IRepository<Product, ProductId>
{
    // Specification-based methods
    FinT<IO, bool> Exists(Specification<Product> spec);
    FinT<IO, Seq<Product>> FindAll(Specification<Product> spec);
}
```

### InMemory Implementation Pattern

Use `IsSatisfiedBy()` directly:

```csharp
public virtual FinT<IO, bool> Exists(Specification<Product> spec)
{
    return IO.lift(() =>
    {
        bool exists = _products.Values.Any(p => spec.IsSatisfiedBy(p));
        return Fin.Succ(exists);
    });
}

public virtual FinT<IO, Seq<Product>> FindAll(Specification<Product> spec)
{
    return IO.lift(() =>
    {
        var products = _products.Values.Where(p => spec.IsSatisfiedBy(p));
        return Fin.Succ(toSeq(products));
    });
}
```

### EfCore Implementation Pattern (Expression-Based Automatic SQL Translation)

Auto-convert Entity Expression -> Model Expression with `PropertyMap` then apply to EF Core LINQ. **No switch cases needed**:

Once Entity-Model property mapping is configured in `PropertyMap`, there is no need to modify Adapter code when adding new Specifications.

```csharp
// PropertyMap configuration (static readonly, once only)
private static readonly PropertyMap<Product, ProductModel> _propertyMap =
    new PropertyMap<Product, ProductModel>()
        .Map(p => (decimal)p.Price, m => m.Price)
        .Map(p => (string)p.Name, m => m.Name)
        .Map(p => (int)p.StockQuantity, m => m.StockQuantity)
        .Map(p => p.Id.ToString(), m => m.Id);

// BuildQuery -- switch removed, automatic conversion
private IQueryable<ProductModel> BuildQuery(Specification<Product> spec)
{
    var expression = SpecificationExpressionResolver.TryResolve(spec);
    if (expression is not null)
    {
        var modelExpression = _propertyMap.Translate(expression);
        return _dbContext.Products.Where(modelExpression);
    }

    throw new NotSupportedException(
        $"No Expression defined for Specification '{spec.GetType().Name}'. " +
        $"Inherit ExpressionSpecification<T> and implement ToExpression().");
}
```

**Changes when adding new Specifications:**
- Domain: Only inherit `ExpressionSpecification<T>` and implement `ToExpression()`
- Adapter: **No changes needed** (if only using properties already mapped in PropertyMap)
- PropertyMap: Add mapping if the Spec uses new Entity properties

> **Design decision**: `ExpressionSpecification<T>`'s `ToExpression()` defines Expressions based on domain entities while casting Value Objects to primitives. `PropertyMap`'s `ExpressionVisitor` recognizes this cast pattern and automatically converts to Model properties. And/Or/Not combinations are also automatically composed by `SpecificationExpressionResolver`.

> **Note**: For the complete Repository implementation procedure, see the [Repository & Query Implementation Guide](../adapter/14c-repository-query-implementation-guide).

Now that Repository integration is complete, let us examine patterns for using Specifications individually or in combination within Usecases.

---

## Usage in Usecase (HOW)

### Single Spec Usage -- Duplicate Check (CreateProductCommand)

```csharp
public sealed class Usecase(IProductRepository productRepository)
    : ICommandUsecase<Request, Response>
{
    public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
    {
        var productName = ProductName.Create(request.Name).ThrowIfFail();

        FinT<IO, Response> usecase =
            from exists in _productRepository.Exists(new ProductNameUniqueSpec(productName))
            from _ in guard(!exists, ApplicationError.For<CreateProductCommand>(
                new AlreadyExists(),
                request.Name,
                $"Product name already exists: '{request.Name}'"))
            from product in _productRepository.Create(...)
            select new Response(...);

        Fin<Response> response = await usecase.Run().RunAsync();
        return response.ToFinResponse();
    }
}
```

### Composite Spec Combination -- Search Filter (SearchProductsQuery)

```csharp
// Reference: samples/ecommerce-ddd/.../SearchProductsQuery.cs
public sealed class Usecase(IProductQuery productQuery)
    : IQueryUsecase<Request, Response>
{
    private readonly IProductQuery _productQuery = productQuery;

    public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
    {
        var spec = BuildSpecification(request);
        var pageRequest = new PageRequest(request.Page, request.PageSize);
        var sortExpression = SortExpression.By(request.SortBy, SortDirection.Parse(request.SortDirection));

        FinT<IO, Response> usecase =
            from result in _productQuery.Search(spec, pageRequest, sortExpression)
            select new Response(
                result.Items,
                result.TotalCount,
                result.Page,
                result.PageSize,
                result.TotalPages,
                result.HasNextPage,
                result.HasPreviousPage);

        Fin<Response> response = await usecase.Run().RunAsync();
        return response.ToFinResponse();
    }

    private static Specification<Product> BuildSpecification(Request request)
    {
        var spec = Specification<Product>.All;

        request.Name.Iter(name =>
            spec &= new ProductNameSpec(
                ProductName.Create(name).ThrowIfFail()));

        request.MinPrice.Bind(min => request.MaxPrice.Map(max => (min, max)))
            .Iter(t => spec &= new ProductPriceRangeSpec(
                Money.Create(t.min).ThrowIfFail(),
                Money.Create(t.max).ThrowIfFail()));

        return spec;
    }
}
```

**Key Points:**
- Use `Specification<T>.All` as the initial value for progressive composition with the `&` operator without null checks
- `Option<T>.Iter()`: Add filter only when Some, ignore when None -- no primitive type-based `if` checks needed
- `Bind().Map().Iter()`: Add range filter only when both Options are Some
- If no filters, return `All` as-is -> full query. `All` inherits from `ExpressionSpecification<T>`, so it works correctly in EfCore

---

## Test Patterns

### Specification Self-Testing (Boundary Values)

```csharp
public class ProductPriceRangeSpecTests
{
    private static Product CreateSampleProduct(decimal price = 100m)
    {
        return Product.Create(
            ProductName.Create("Test Product").ThrowIfFail(),
            ProductDescription.Create("Test Description").ThrowIfFail(),
            Money.Create(price).ThrowIfFail(),
            Quantity.Create(10).ThrowIfFail());
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsTrue_WhenPriceWithinRange()
    {
        // Arrange
        var product = CreateSampleProduct(price: 150m);
        var sut = new ProductPriceRangeSpec(
            Money.Create(100m).ThrowIfFail(),
            Money.Create(200m).ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsTrue_WhenPriceEqualsMinPrice()
    {
        // Arrange
        var product = CreateSampleProduct(price: 100m);
        var sut = new ProductPriceRangeSpec(
            Money.Create(100m).ThrowIfFail(),
            Money.Create(200m).ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }
}
```

### Specification Composition Tests

```csharp
// Method style composition
var sut = new IsPositiveSpec().And(new IsEvenSpec());
sut.IsSatisfiedBy(2).ShouldBe(true);   // positive and even
sut.IsSatisfiedBy(3).ShouldBe(false);  // positive but odd

// Operator style composition
var sut = new IsPositiveSpec() & !new IsEvenSpec();
sut.IsSatisfiedBy(3).ShouldBe(true);   // positive and not even
```

### Usecase Tests (NSubstitute Mock)

Mock with `Arg.Any<Specification<T>>()` without needing to verify Specification types:

```csharp
public class SearchProductsQueryTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly SearchProductsQuery.Usecase _sut;

    public SearchProductsQueryTests()
    {
        _sut = new SearchProductsQuery.Usecase(_productRepository);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenPriceRangeProvided()
    {
        // Arrange
        var matchingProducts = Seq(Product.Create(...));
        var request = new SearchProductsQuery.Request(100m, 200m, null);

        _productRepository.FindAll(Arg.Any<Specification<Product>>())
            .Returns(FinTFactory.Succ(matchingProducts));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }
}
```

---

## Checklist

### When Implementing Specifications

- [ ] Inherits `ExpressionSpecification<T>` (`Functorium.Domains.Specifications`)
- [ ] Declared as `sealed class`
- [ ] `ToExpression()` implemented -- uses Value Object -> primitive casts
- [ ] Placed in `{Aggregate}/Specifications/` folder
- [ ] Naming: `{Aggregate}{Condition}Spec`

### When Integrating with Repository

- [ ] Added `Exists(Specification<T>)` / `FindAll(Specification<T>)` to Port
- [ ] InMemory implementation: Use `IsSatisfiedBy()` directly (auto-implemented)
- [ ] EfCore implementation: Configure `PropertyMap` + use `SpecificationExpressionResolver.TryResolve()`
- [ ] Add `PropertyMap.Map()` when using new Entity properties

### When Testing

- [ ] Specification self-tests: satisfied/unsatisfied boundary values
- [ ] Composition tests: `And`, `Or`, `Not` (method + operator)
- [ ] Usecase tests: `Arg.Any<Specification<T>>()` Mock

---

## Troubleshooting

### NotSupportedException occurs in EfCore

**Cause:** The Specification inherits from the base `Specification<T>` instead of `ExpressionSpecification<T>`, causing `SpecificationExpressionResolver.TryResolve()` to return `null`.

**Resolution:** You must inherit `ExpressionSpecification<T>` and implement `ToExpression()`. `Specification<T>.All` also inherits from `ExpressionSpecification<T>`, so it works correctly in EfCore.

### Using properties not mapped in PropertyMap

**Cause:** The Entity property used in `ToExpression()` is not registered in `PropertyMap`.

**Resolution:** Add new property mapping to `PropertyMap`:
```csharp
private static readonly PropertyMap<Product, ProductModel> _propertyMap =
    new PropertyMap<Product, ProductModel>()
        .Map(p => (decimal)p.Price, m => m.Price)
        .Map(p => (string)p.NewProperty, m => m.NewProperty);  // Added
```

### Translation fails when directly comparing Value Objects in ToExpression()

**Cause:** Direct Value Object comparison inside Expressions cannot be converted by `PropertyMap`.

**Resolution:** Convert Value Objects to primitives outside `ToExpression()` then capture in closures:
```csharp
// Correct pattern
decimal min = MinPrice;  // Convert to primitive
return product => (decimal)product.Price >= min;

// Incorrect pattern
return product => product.Price >= MinPrice;  // Direct Value Object comparison
```

---

## FAQ

### Q1. What are the criteria for choosing between Specification and Entity methods?

Specifications are for **encapsulating and composing query conditions.** Entity methods are used for state change logic. The criterion is "Does this condition need to be reused in Repository queries?"

### Q2. When should Specification<T>.All be used?

Used as an initial value instead of `null` when progressively composing optional filters. `All` returns `_ => true`, acting as the identity element for the `&` operation, and works correctly in EfCore.

### Q3. What is the implementation difference between InMemory Repository and EfCore Repository?

InMemory uses `IsSatisfiedBy()` directly to filter in memory. EfCore extracts the Expression with `SpecificationExpressionResolver.TryResolve()`, converts it to a Model Expression with `PropertyMap.Translate()`, and applies it to LINQ Where.

### Q4. Do I need to modify Adapter code when adding a new Specification?

If the Specification only uses properties already mapped in `PropertyMap`, no Adapter code modification is needed. `PropertyMap.Map()` addition is only needed when using new Entity properties.

### Q5. What is the difference between method style (.And(), .Or()) and operator style (&, |, !)?

The results are identical. Operator style is more concise, but method style may be more readable. Choose a consistent style within the project.

---

## References

- [04-ddd-tactical-overview.md](../04-ddd-tactical-overview) -- DDD tactical design overview
- [09-domain-services.md](../09-domain-services) -- Domain Services
- [12-ports.md](../adapter/12-ports) -- Port architecture
- [15a-unit-testing.md](../testing/15a-unit-testing) -- Unit test rules

### Practical Examples Files

| Category | File |
|------|------|
| **Framework** | `Src/Functorium/Domains/Specifications/Specification.cs` |
| | `Src/Functorium/Domains/Specifications/AndSpecification.cs` |
| | `Src/Functorium/Domains/Specifications/OrSpecification.cs` |
| | `Src/Functorium/Domains/Specifications/NotSpecification.cs` |
| **Domain Spec** | `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/AggregateRoots/Products/Specifications/ProductNameUniqueSpec.cs` |
| | `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/AggregateRoots/Products/Specifications/ProductPriceRangeSpec.cs` |
| | `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/AggregateRoots/Products/Specifications/ProductLowStockSpec.cs` |
| | `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/AggregateRoots/Customers/Specifications/CustomerEmailSpec.cs` |
| **Repository Port** | `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/AggregateRoots/Products/Ports/IProductRepository.cs` |
| | `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/AggregateRoots/Customers/Ports/ICustomerRepository.cs` |
| **Repository Implementation** | `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/InMemory/InMemoryProductRepository.cs` |
| | `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/EfCore/EfCoreProductRepository.cs` |
| **Usecase** | `Tests.Hosts/01-SingleHost/Src/LayeredArch.Application/Usecases/Products/CreateProductCommand.cs` |
| | `Tests.Hosts/01-SingleHost/Src/LayeredArch.Application/Usecases/Products/SearchProductsQuery.cs` |
| **Framework Tests** | `Tests/Functorium.Tests.Unit/DomainsTests/Specifications/SpecificationTests.cs` |
| | `Tests/Functorium.Tests.Unit/DomainsTests/Specifications/SpecificationOperatorTests.cs` |
| **Domain Spec Tests** | `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Domain/Products/ProductPriceRangeSpecTests.cs` |
| | `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Domain/Products/ProductLowStockSpecTests.cs` |
| | `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Domain/Products/ProductNameUniqueSpecTests.cs` |
| | `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Domain/Products/ProductSpecificationCompositionTests.cs` |
| | `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Domain/Customers/CustomerEmailSpecTests.cs` |
| **Usecase Tests** | `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Application/Products/SearchProductsQueryTests.cs` |
