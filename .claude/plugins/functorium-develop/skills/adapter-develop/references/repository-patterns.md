# Repository 패턴 레퍼런스

## EfCoreRepositoryBase 상속 패턴

### 기본 구조

```csharp
using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using Functorium.Domains.Specifications;
using Functorium.Domains.Specifications.Expressions;

[GenerateObservablePort]
public class ProductRepositoryEfCore
    : EfCoreRepositoryBase<Product, ProductId, ProductModel>, IProductRepository
{
    private readonly LayeredArchDbContext _dbContext;

    public ProductRepositoryEfCore(LayeredArchDbContext dbContext, IDomainEventCollector eventCollector)
        : base(eventCollector,
               q => q.Include(p => p.ProductTags),        // applyIncludes: N+1 방지
               new PropertyMap<Product, ProductModel>()     // Specification 매핑
                   .Map(p => (decimal)p.Price, m => m.Price)
                   .Map(p => (string)p.Name, m => m.Name)
                   .Map(p => p.Id.ToString(), m => m.Id))
        => _dbContext = dbContext;

    // ─── 필수 선언 ───────────────────────────────────
    protected override DbContext DbContext => _dbContext;
    protected override DbSet<ProductModel> DbSet => _dbContext.Products;

    protected override Product ToDomain(ProductModel model) => model.ToDomain();
    protected override ProductModel ToModel(Product p) => p.ToModel();
}
```

### 생성자 파라미터

| 파라미터 | 필수 | 설명 |
|---------|------|------|
| `eventCollector` | O | `IDomainEventCollector` - 도메인 이벤트 수집기 |
| `applyIncludes` | X | `Func<IQueryable<TModel>, IQueryable<TModel>>` - Include 선언 |
| `propertyMap` | X | `PropertyMap<TAgg, TModel>` - Specification 매핑 (BuildQuery 사용 시 필수) |

### ToDomain / ToModel 매퍼

별도 static extension 클래스로 분리:

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

### Soft Delete Override

```csharp
public override FinT<IO, int> Delete(ProductId id)
{
    return IO.liftAsync(async () =>
    {
        var model = await ReadQueryIgnoringFilters()
            .FirstOrDefaultAsync(ByIdPredicate(id));

        if (model is null)
            return NotFoundError(id);

        var product = ToDomain(model);
        product.Delete("system");

        var updatedModel = ToModel(product);
        DbSet.Attach(updatedModel);
        _dbContext.Entry(updatedModel).Property(p => p.DeletedAt).IsModified = true;
        _dbContext.Entry(updatedModel).Property(p => p.DeletedBy).IsModified = true;

        EventCollector.Track(product);
        return Fin.Succ(1);
    });
}
```

### Compiled Query 최적화

```csharp
private static readonly Func<LayeredArchDbContext, string, Task<ProductModel?>> GetByIdIgnoringFiltersCompiled =
    EF.CompileAsyncQuery((LayeredArchDbContext ctx, string id) =>
        ctx.Products.IgnoreQueryFilters()
            .Include(p => p.ProductTags)
            .FirstOrDefault(m => m.Id == id));
```

### Specification 메서드

`Exists`, `Count`, `DeleteBy`는 `IRepository` 베이스에서 제공하므로 서브클래스 override 불필요.
`PropertyMap`이 생성자에 전달되어야 동작합니다.

## InMemoryRepositoryBase 상속 패턴

```csharp
[GenerateObservablePort]
public class ProductRepositoryInMemory
    : InMemoryRepositoryBase<Product, ProductId>, IProductRepository
{
    internal static readonly ConcurrentDictionary<ProductId, Product> Products = new();
    protected override ConcurrentDictionary<ProductId, Product> Store => Products;

    public ProductRepositoryInMemory(IDomainEventCollector eventCollector)
        : base(eventCollector) { }

    // Soft Delete: GetById에서 DeletedAt.IsNone 필터링
    public override FinT<IO, Product> GetById(ProductId id)
    {
        return IO.lift(() =>
        {
            if (Products.TryGetValue(id, out Product? product) && product.DeletedAt.IsNone)
                return Fin.Succ(product);
            return NotFoundError(id);
        });
    }
}
```

## EF Core Model

```csharp
using Functorium.Adapters.Repositories;

public class ProductModel : IHasStringId
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

## IHasStringId 인터페이스

모든 EF Core Model이 구현해야 하는 인터페이스:

```csharp
public interface IHasStringId
{
    string Id { get; set; }
}
```

## PropertyMap 설정

Domain 프로퍼티 ↔ Model 프로퍼티 매핑 (Specification → EF Core Expression 변환용):

```csharp
new PropertyMap<Product, ProductModel>()
    .Map(p => (decimal)p.Price, m => m.Price)    // Value Object → 원시 타입 캐스팅
    .Map(p => (string)p.Name, m => m.Name)
    .Map(p => p.Id.ToString(), m => m.Id)
```

## ByIdPredicate / ByIdsPredicate

`EfCoreRepositoryBase`가 `IHasStringId` 기반 기본 구현을 제공합니다.
커스텀이 필요한 경우 `protected virtual`로 오버라이드 가능합니다.

## Dapper Query Adapter 패턴

### DapperQueryBase 상속

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

    public ProductQueryDapper(IDbConnection connection)
        : base(connection, ProductSpecTranslator.Instance) { }
}
```

### DapperSpecTranslator 구현

```csharp
internal static class ProductSpecTranslator
{
    internal static readonly DapperSpecTranslator<Product> Instance = new DapperSpecTranslator<Product>()
        .WhenAll(alias =>
        {
            var p = DapperSpecTranslator<Product>.Prefix(alias);
            return ($"WHERE {p}DeletedAt IS NULL", new DynamicParameters());
        })
        .When<ProductPriceRangeSpec>((spec, alias) =>
        {
            var p = DapperSpecTranslator<Product>.Prefix(alias);
            return ($"WHERE {p}DeletedAt IS NULL AND {p}Price >= @MinPrice AND {p}Price <= @MaxPrice",
                DapperSpecTranslator<Product>.Params(
                    ("MinPrice", (decimal)spec.MinPrice),
                    ("MaxPrice", (decimal)spec.MaxPrice)));
        });
}
```

### InMemoryQueryBase 상속

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
            .Where(p => p.DeletedAt.IsNone && spec.IsSatisfiedBy(p))
            .Select(p => new ProductSummaryDto(p.Id.ToString(), p.Name, p.Price));
    }

    protected override Func<ProductSummaryDto, object> SortSelector(string fieldName)
        => fieldName.ToLowerInvariant() switch
        {
            "price" => dto => dto.Price,
            _ => dto => dto.Name
        };
}
```

## External API Adapter 패턴

```csharp
[GenerateObservablePort]
public class ExternalPricingApiService : IExternalPricingService
{
    // 커스텀 에러 타입 정의
    public sealed record OperationCancelled : AdapterErrorKind.Custom;
    public sealed record UnexpectedException : AdapterErrorKind.Custom;

    private readonly HttpClient _httpClient;

    public string RequestCategory => "ExternalApi";

    public virtual FinT<IO, Money> GetPriceAsync(string productCode, CancellationToken ct)
    {
        return IO.liftAsync(async () =>
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/pricing/{productCode}", ct);

                if (!response.IsSuccessStatusCode)
                    return HandleHttpError<Money>(response, productCode);

                var data = await response.Content.ReadFromJsonAsync<PriceResponse>(cancellationToken: ct);
                if (data is null)
                    return AdapterError.For<ExternalPricingApiService>(
                        new Null(), productCode, $"응답이 null입니다. ProductCode: {productCode}");

                return Money.Create(data.Price);
            }
            catch (HttpRequestException ex)
            {
                return AdapterError.FromException<ExternalPricingApiService>(
                    new ConnectionFailed("ExternalPricingApi"), ex);
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == ct)
            {
                return AdapterError.For<ExternalPricingApiService>(
                    new OperationCancelled(), productCode, "요청이 취소되었습니다");
            }
            catch (Exception ex)
            {
                return AdapterError.FromException<ExternalPricingApiService>(
                    new UnexpectedException(), ex);
            }
        });
    }
}
```

## 핵심 규칙

1. **모든 public 메서드는 `virtual`** - Source Generator가 Observable Pipeline을 생성하기 위해 필수
2. **`[GenerateObservablePort]`** - 관찰 가능성 래퍼 자동 생성
3. **`RequestCategory` 프로퍼티** - 관찰 가능성 로그 카테고리 (`"Repository"`, `"QueryAdapter"`, `"ExternalApi"`)
4. **에러 처리** - 예외 대신 `Fin.Fail` 반환 (`AdapterError.For`, `AdapterError.FromException`)
5. **EntityId는 Ulid 기반** - `string Id`로 저장, `TId.Create(string)`으로 복원
6. **`ObservableSignal`** - Adapter 구현 내부에서 운영 목적 로그 출력 (`ObservableSignal.Debug/Warning/Error`). 공통 컨텍스트 자동 포함, 부가 필드는 `adapter.*` 프리픽스 사용

---

## IRepository 벌크 메서드

IRepository는 Write Single / Write Batch / Read / Specification 4그룹으로 구성됩니다:

```csharp
public interface IRepository<TAggregate, TId> : IObservablePort
    where TAggregate : AggregateRoot<TId>
    where TId : struct, IEntityId<TId>
{
    // ── Write: Single ──
    FinT<IO, TAggregate> Create(TAggregate aggregate);
    FinT<IO, TAggregate> Update(TAggregate aggregate);
    FinT<IO, int> Delete(TId id);

    // ── Write: Batch ──
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

> **성능 개선**: `Update`/`UpdateRange`는 `ExecuteUpdateAsync`를 사용하여 Change Tracker를 우회합니다 (SELECT 없이 직접 UPDATE). `CreateRange`는 대량 데이터 시 청크 단위로 `ChangeTracker.Clear()`를 수행합니다.

### 벌크 연산과 Domain Service

벌크 삭제/생성에서 도메인 이벤트가 필요한 경우, **Domain Service 패턴**을 사용합니다:

```csharp
// Use Case에서:
from products in productRepository.GetByIds(ids)
let bulkResult = ProductBulkOperations.BulkDelete(products.ToList(), "system")
let _ = eventCollector.TrackEvent(bulkResult.Event)  // 벌크 이벤트 직접 추적
from affectedCount in productRepository.UpdateRange(bulkResult.Deleted.ToList())
select new Response(affectedCount)
```

### EfCoreRepositoryBase 구현 전략

- `Update`: `ExecuteUpdateAsync` + `BuildSetters` (Change Tracker 우회, 1 RT)
- `UpdateRange`: 건별 `ExecuteUpdateAsync` (SELECT 제거)
- `CreateRange`: `DbSet.AddRange()` (대량 시 청크 + `ChangeTracker.Clear()`)
- `Exists/Count/DeleteBy`: `BuildQuery(spec)` → 단일 SQL
- `GetByIds`: `WHERE ... IN` (IdBatchSize=500 단위 분할)
- `UpdateRange`: `DbSet.UpdateRange()` + `EventCollector.TrackRange()`
- `DeleteRange`: `ExecuteDeleteAsync()` (hard delete, 이벤트 없음)

벌크 Soft Delete는 서브클래스에서 override하여 Domain Service 패턴으로 처리합니다.
