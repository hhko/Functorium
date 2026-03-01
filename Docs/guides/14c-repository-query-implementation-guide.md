# Repository & Query Adapter 구현 가이드

## 1. 개요

이 문서는 새 Aggregate에 대한 **Repository** (Write Side)와 **Query Adapter** (Read Side)를 구현하는 절차를 설명합니다.

### CQRS 구조

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
│  │EfCoreRepo    │        │DapperQuery       │   │
│  │InMemoryRepo  │        │InMemoryQuery     │   │
│  └──────────────┘        └──────────────────┘   │
└─────────────────────────────────────────────────┘
```

- **Repository** — Aggregate Root 단위 CRUD. 도메인 객체를 통한 읽기/쓰기
- **Query Adapter** — DTO 직접 프로젝션. Aggregate 재구성 없이 DB → DTO

### 베이스 클래스 계층

| 역할 | 베이스 클래스 | 구현 대상 |
|------|-------------|----------|
| Write (EF Core) | `EfCoreRepositoryBase<TAgg, TId, TModel>` | `IRepository<TAgg, TId>` |
| Write (InMemory) | `InMemoryRepositoryBase<TAgg, TId>` | `IRepository<TAgg, TId>` |
| Read (Dapper) | `DapperQueryBase<TEntity, TDto>` | `IQueryPort<TEntity, TDto>` |
| Read (InMemory) | `InMemoryQueryBase<TEntity, TDto>` | `IQueryPort<TEntity, TDto>` |

---

## 2. Repository 구현 가이드 (Write Side)

### 2.1 구현 체크리스트

새 Aggregate `Xxx`를 추가할 때의 단계별 체크리스트:

| # | 계층 | 작업 | 파일 |
|---|------|------|------|
| 1 | Domain | `IXxxRepository` 인터페이스 정의 | `Domain/AggregateRoots/Xxxs/IXxxRepository.cs` |
| 2 | Adapter | `XxxModel` + `IHasStringId` 구현 | `Repositories/EfCore/Models/XxxModel.cs` |
| 3 | Adapter | `IEntityTypeConfiguration<XxxModel>` | `Repositories/EfCore/Configurations/XxxConfiguration.cs` |
| 4 | Adapter | `XxxMapper` (ToModel/ToDomain) | `Repositories/EfCore/Mappers/XxxMapper.cs` |
| 5 | Adapter | `EfCoreXxxRepository` 구현 | `Repositories/EfCore/EfCoreXxxRepository.cs` |
| 6 | Adapter | `InMemoryXxxRepository` 구현 | `Repositories/InMemory/Xxxs/InMemoryXxxRepository.cs` |
| 7 | Adapter | DI Registration 등록 | `Abstractions/Registrations/AdapterPersistenceRegistration.cs` |

### 2.2 Domain 인터페이스

기본 CRUD만 필요하면 `IRepository<TAgg, TId>`를 그대로 상속합니다:

```csharp
// 최소 구현 — 추가 메서드 없음
public interface ITagRepository : IRepository<Tag, TagId>;

// 추가 메서드가 필요한 경우
public interface IProductRepository : IRepository<Product, ProductId>
{
    FinT<IO, bool> Exists(Specification<Product> spec);
    FinT<IO, Product> GetByIdIncludingDeleted(ProductId id);
}
```

`IRepository<TAgg, TId>`가 제공하는 기본 8개 메서드:
- `Create`, `GetById`, `Update`, `Delete`
- `CreateRange`, `GetByIds`, `UpdateRange`, `DeleteRange`

### 2.3 EfCoreRepositoryBase 구현 패턴

#### 생성자 3인자 패턴

```csharp
protected EfCoreRepositoryBase(
    IDomainEventCollector eventCollector,                                 // 필수: 도메인 이벤트 수집
    Func<IQueryable<TModel>, IQueryable<TModel>>? applyIncludes = null,   // Navigation Property Include
    PropertyMap<TAggregate, TModel>? propertyMap = null)                  // Specification → SQL 변환
```

- **eventCollector** — 항상 필수
- **applyIncludes** — Navigation Property가 있으면 선언. `ReadQuery()`에 자동 적용되어 N+1 방지
- **propertyMap** — `Exists(Specification)` 또는 `BuildQuery` 사용 시 필수

#### 최소 구현 (TagRepository)

Navigation Property도 없고, Specification 검색도 없는 가장 단순한 형태:

```csharp
[GenerateObservablePort]
public class EfCoreTagRepository
    : EfCoreRepositoryBase<Tag, TagId, TagModel>, ITagRepository
{
    private readonly LayeredArchDbContext _dbContext;

    public EfCoreTagRepository(LayeredArchDbContext dbContext, IDomainEventCollector eventCollector)
        : base(eventCollector)                          // applyIncludes, propertyMap 모두 생략
        => _dbContext = dbContext;

    protected override DbContext DbContext => _dbContext;
    protected override DbSet<TagModel> DbSet => _dbContext.Tags;
    protected override Tag ToDomain(TagModel model) => model.ToDomain();
    protected override TagModel ToModel(Tag tag) => tag.ToModel();
}
```

서브클래스가 구현해야 하는 필수 멤버는 4개입니다:
- `DbContext` — EF Core DbContext (TrackedMerge Update에 사용)
- `DbSet` — EF Core DbSet
- `ToDomain()` — Model → Domain 매핑
- `ToModel()` — Domain → Model 매핑

#### Navigation Property가 있는 구현 (OrderRepository)

```csharp
[GenerateObservablePort]
public class EfCoreOrderRepository
    : EfCoreRepositoryBase<Order, OrderId, OrderModel>, IOrderRepository
{
    private readonly LayeredArchDbContext _dbContext;

    public EfCoreOrderRepository(LayeredArchDbContext dbContext, IDomainEventCollector eventCollector)
        : base(eventCollector, q => q.Include(o => o.OrderLines))   // Include 선언
        => _dbContext = dbContext;

    protected override DbContext DbContext => _dbContext;
    protected override DbSet<OrderModel> DbSet => _dbContext.Orders;
    protected override Order ToDomain(OrderModel model) => model.ToDomain();
    protected override OrderModel ToModel(Order order) => order.ToModel();
}
```

#### 전체 구현 (ProductRepository) — Include + PropertyMap + 커스텀 메서드

```csharp
[GenerateObservablePort]
public class EfCoreProductRepository
    : EfCoreRepositoryBase<Product, ProductId, ProductModel>, IProductRepository
{
    private readonly LayeredArchDbContext _dbContext;

    public EfCoreProductRepository(LayeredArchDbContext dbContext, IDomainEventCollector eventCollector)
        : base(eventCollector,
               q => q.Include(p => p.ProductTags),                // Navigation Include
               new PropertyMap<Product, ProductModel>()           // Specification 매핑
                   .Map(p => (decimal)p.Price, m => m.Price)
                   .Map(p => (string)p.Name, m => m.Name)
                   .Map(p => p.Id.ToString(), m => m.Id))
        => _dbContext = dbContext;

    protected override DbContext DbContext => _dbContext;
    protected override DbSet<ProductModel> DbSet => _dbContext.Products;
    protected override Product ToDomain(ProductModel model) => model.ToDomain();
    protected override ProductModel ToModel(Product p) => p.ToModel();

    // Specification 기반 존재 확인 — 베이스의 ExistsBySpec 활용
    public virtual FinT<IO, bool> Exists(Specification<Product> spec)
        => ExistsBySpec(spec);

    // Soft Delete 오버라이드 (섹션 5.1 참조)
    // ...
}
```

#### PropertyMap 선언 규칙

Domain의 Value Object를 Model의 primitive 타입에 매핑합니다:

```csharp
new PropertyMap<Customer, CustomerModel>()
    .Map(c => (string)c.Email, m => m.Email)                // Email(VO) → string
    .Map(c => (string)c.Name, m => m.Name)                  // CustomerName(VO) → string
    .Map(c => (decimal)c.CreditLimit, m => m.CreditLimit)   // Money(VO) → decimal
    .Map(c => c.Id.ToString(), m => m.Id)                   // CustomerId → string
```

### 2.4 InMemoryRepositoryBase 구현 패턴

#### 기본 패턴 — `static ConcurrentDictionary` + `Store` 프로퍼티

```csharp
[GenerateObservablePort]
public class InMemoryTagRepository
    : InMemoryRepositoryBase<Tag, TagId>, ITagRepository
{
    internal static readonly ConcurrentDictionary<TagId, Tag> Tags = new();
    protected override ConcurrentDictionary<TagId, Tag> Store => Tags;

    public InMemoryTagRepository(IDomainEventCollector eventCollector)
        : base(eventCollector) { }
}
```

핵심 규칙:
- `ConcurrentDictionary`는 반드시 **`static`**으로 선언합니다 (DI Scope 간 데이터 공유)
- `internal static`으로 선언하여 같은 어셈블리의 Query Adapter에서 접근 가능하게 합니다
- 베이스 클래스가 8개 CRUD를 모두 구현하므로, 추가 메서드만 오버라이드합니다

#### 추가 메서드가 있는 구현 (InventoryRepository)

```csharp
[GenerateObservablePort]
public class InMemoryInventoryRepository
    : InMemoryRepositoryBase<Inventory, InventoryId>, IInventoryRepository
{
    internal static readonly ConcurrentDictionary<InventoryId, Inventory> Inventories = new();
    protected override ConcurrentDictionary<InventoryId, Inventory> Store => Inventories;

    public InMemoryInventoryRepository(IDomainEventCollector eventCollector)
        : base(eventCollector) { }

    public virtual FinT<IO, Inventory> GetByProductId(ProductId productId)
    {
        return IO.lift(() =>
        {
            var inventory = Inventories.Values.FirstOrDefault(i =>
                i.ProductId.Equals(productId));

            if (inventory is not null)
                return Fin.Succ(inventory);

            return AdapterError.For<InMemoryInventoryRepository>(
                new NotFound(), productId.ToString(),
                $"상품 ID '{productId}'에 대한 재고를 찾을 수 없습니다");
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

### 2.5 Mapper 구현 패턴

확장 메서드로 `ToModel()` / `ToDomain()`를 구현합니다.

#### 단순 매퍼 (TagMapper)

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
        Tag.CreateFromValidated(                        // 재검증 방지
            TagId.Create(model.Id),
            TagName.CreateFromValidated(model.Name),    // CreateFromValidated 사용
            model.CreatedAt,
            Optional(model.UpdatedAt));
}
```

핵심 규칙:
- `ToDomain`에서 **`CreateFromValidated()`** 를 사용합니다 — DB에서 읽은 데이터는 이미 검증됨
- `Option<DateTime>` → `DateTime?` 변환: `.ToNullable()`
- `DateTime?` → `Option<DateTime>` 변환: `Optional(model.UpdatedAt)`

#### Navigation Property 매핑 (ProductMapper)

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

#### 자식 엔티티를 포함하는 매핑 (OrderMapper)

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
            OrderLines = order.OrderLines.Select(l => l.ToModel(orderId)).ToList()  // 부모 Id 전달
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

### 2.6 EF Core Model & Configuration 패턴

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
    // Soft Delete 전용
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    // Navigation Property
    public List<ProductTagModel> ProductTags { get; set; } = [];
}
```

규칙:
- 반드시 `IHasStringId`를 구현합니다 — 베이스 클래스의 `ByIdPredicate`가 이 인터페이스에 의존
- `Id`는 `string` 타입, maxLength 26 (Ulid)

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

Configuration 규칙:
- Id: `HasMaxLength(26)` (Ulid 길이)
- decimal: `HasPrecision(18, 4)`
- Soft Delete: `HasQueryFilter(p => p.DeletedAt == null)`
- Navigation: Cascade Delete 설정

### 2.7 CRUD 대칭성 분석

`EfCoreRepositoryBase`의 단건/벌크 CRUD 연산 간 아키텍처 경로를 비교합니다.

#### 전체 비교표

| 연산 | 구분 | Change Tracker | 도메인 변환 | 이벤트 추적 | ReadQuery | 실행 방식 |
|------|------|:-:|:-:|:-:|:-:|------|
| **Create** | 단건 | O | O (ToModel) | O (Track) | - | `DbSet.Add` |
| **CreateRange** | 벌크 | O | O (ToModel) | O (TrackRange) | - | `DbSet.AddRange` |
| **GetById** | 단건 | X | O (ToDomain) | - | O | `AsNoTracking` → `FirstOrDefault` |
| **GetByIds** | 벌크 | X | O (ToDomain) | - | O | `AsNoTracking` → `Where` → `ToList` |
| **Update** | 단건 | O | O (ToModel) | O (Track) | - | `FindAsync + SetValues (TrackedMerge)` |
| **UpdateRange** | 벌크 | O | O (ToModel) | O (TrackRange) | - | `FindAsync + SetValues (TrackedMerge)` |
| **Delete** | 단건 | **X** | **X** | **X** | - | `Where(pred).ExecuteDeleteAsync` |
| **DeleteRange** | 벌크 | **X** | **X** | **X** | - | `Where(pred).ExecuteDeleteAsync` |

> **참고**: ReadQuery 열의 O는 `ReadQuery()` (AsNoTracking + Include 자동 적용)를 사용한다는 의미입니다.
>
> **참고**: GetByIds는 요청 ID 수와 결과 수가 다르면 `PartialNotFoundError`를 반환합니다.

#### CRUD 4쌍 대칭 요약

| 연산 | 단건 vs 벌크 | 이유 |
|------|:---:|------|
| Create | 대칭 | `DbSet.Add` vs `DbSet.AddRange` (API만 복수형) |
| Read | 대칭 | `FirstOrDefault` vs `Where().ToList()` (조건만 단수/복수) |
| Update | 대칭 | `FindAsync + SetValues` (TrackedMerge: 변경된 컬럼만 UPDATE) |
| Delete | 대칭 | `Where(pred).ExecuteDeleteAsync` (동일 경로, 조건만 단수/복수) |

**비대칭은 Soft Delete 오버라이드에서만 발생합니다.**
벌크 SQL 연산(`ExecuteUpdateAsync`)과 도메인 이벤트 추적은 구조적으로 양립 불가능합니다:

1. 도메인 이벤트는 도메인 객체의 상태 전이에서 발생
2. 벌크 SQL은 도메인 객체를 생성하지 않음
3. N건을 개별 로드하면 벌크 연산의 성능 이점이 소멸

이것은 의도된 성능 트레이드오프입니다. Soft Delete 코드는 섹션 5.1을 참조하세요.

#### 서브클래스 오버라이드 현황

| 리포지토리 | CRUD 오버라이드 | 고유 메서드 |
|-----------|:-:|------|
| `EfCoreProductRepository` | `Delete`, `DeleteRange` | `GetByIdIncludingDeleted`, `Exists` |
| `EfCoreOrderRepository` | 없음 | 없음 |
| `EfCoreCustomerRepository` | 없음 | `Exists` |
| `EfCoreInventoryRepository` | 없음 | `GetByProductId`, `Exists` |
| `EfCoreTagRepository` | 없음 | 없음 |

Product만 유일하게 CRUD를 오버라이드합니다. 이유는 Soft Delete라는 도메인 요구사항 때문입니다.

#### applyIncludes 선언 현황

| 리포지토리 | applyIncludes | Navigation Property |
|-----------|--------------|---------------------|
| `EfCoreProductRepository` | `q => q.Include(p => p.ProductTags)` | `ProductTags` |
| `EfCoreOrderRepository` | `q => q.Include(o => o.OrderLines)` | `OrderLines` |
| `EfCoreCustomerRepository` | `null` (기본값) | 없음 |
| `EfCoreInventoryRepository` | `null` (기본값) | 없음 |
| `EfCoreTagRepository` | `null` (기본값) | 없음 |

---

## 3. Query Adapter 구현 가이드 (Read Side)

### 3.1 Query 분류

| 유형 | 베이스 클래스 | 인터페이스 | 예시 |
|------|-------------|-----------|------|
| 검색 (페이징) | `DapperQueryBase` / `InMemoryQueryBase` | `IQueryPort<TEntity, TDto>` | `IProductQuery` |
| 단건 조회 | 직접 구현 | `IQueryPort` (비제네릭) | `IProductDetailQuery` |
| JOIN 검색 | `DapperQueryBase` / `InMemoryQueryBase` | `IQueryPort<TEntity, TDto>` | `IProductWithStockQuery` |
| LEFT JOIN 검색 | `DapperQueryBase` / `InMemoryQueryBase` | `IQueryPort<TEntity, TDto>` | `IProductWithOptionalStockQuery` |
| GROUP BY 집계 | `DapperQueryBase` / `InMemoryQueryBase` | `IQueryPort<TEntity, TDto>` | `ICustomerOrderSummaryQuery` |
| 복합 JOIN | 직접 구현 | `IQueryPort` (비제네릭) | `ICustomerOrdersQuery` |

**`IQueryPort<TEntity, TDto>`** — `Search` + `SearchByCursor` + `Stream` 제공
**`IQueryPort`** (비제네릭 마커) — 단건 조회 등 커스텀 시그니처용

### 3.2 DapperQueryBase 구현 패턴

#### 필수 추상 멤버

```csharp
protected abstract string SelectSql { get; }                              // SELECT 절
protected abstract string CountSql { get; }                               // COUNT 절
protected abstract string DefaultOrderBy { get; }                          // 기본 정렬 (예: "Name ASC")
protected abstract Dictionary<string, string> AllowedSortColumns { get; }  // 허용 정렬 컬럼
```

#### virtual 멤버

```csharp
protected virtual (string Where, DynamicParameters Params)
    BuildWhereClause(Specification<TEntity> spec);  // DapperSpecTranslator 제공 시 자동 위임
protected virtual string PaginationClause => "LIMIT @PageSize OFFSET @Skip";     // DB 방언별 오버라이드
protected virtual string CursorPaginationClause => "LIMIT @PageSize";            // Keyset 페이지네이션
```

`BuildWhereClause`는 `DapperSpecTranslator`를 생성자로 제공하면 자동 위임됩니다. Translator가 없으면 서브클래스에서 반드시 오버라이드해야 합니다.

#### 생성자

두 가지 오버로드를 제공합니다:

```csharp
// 1. 기본 — BuildWhereClause를 직접 오버라이드하는 경우
protected DapperQueryBase(IDbConnection connection)

// 2. DapperSpecTranslator 주입 — BuildWhereClause 자동 위임 (권장)
protected DapperQueryBase(IDbConnection connection, DapperSpecTranslator<TEntity> translator, string tableAlias = "")
```

#### SQL 조립

베이스 클래스가 `Search` 메서드에서 `QueryMultipleAsync`를 사용하여 단일 라운드트립으로 카운트와 데이터를 조회합니다:

```sql
-- QueryMultipleAsync 단일 라운드트립
{CountSql} {where};
{SelectSql} {where} {orderBy} {PaginationClause}
```

#### DapperSpecTranslator — Specification → SQL 변환 레지스트리

`DapperSpecTranslator<TEntity>`는 Specification 타입별 SQL WHERE 변환 핸들러를 등록하는 레지스트리 패턴입니다. 공유 Translator 인스턴스를 정의하면 여러 Query Adapter에서 재사용할 수 있습니다.

**Fluent API:**

| 메서드 | 설명 |
|--------|------|
| `WhenAll(handler)` | 모든 Specification에 적용되는 기본 핸들러 (예: Soft Delete 필터) |
| `When<TSpec>(handler)` | 특정 Specification 타입에 대한 핸들러 |
| `Translate(spec, tableAlias)` | 등록된 핸들러로 Specification을 SQL WHERE로 변환 |

**Static 헬퍼:**

| 메서드 | 설명 |
|--------|------|
| `Params(params (string, object)[])` | `DynamicParameters` 생성 헬퍼 |
| `Prefix(string tableAlias)` | 테이블 별칭 접두사 (`"p"` → `"p."`, `""` → `""`) |

**공유 Translator 예시 (ProductSpecTranslator):**

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

`WhenAll`은 `Specification.All`(IsAll == true)일 때 사용되고, `When<TSpec>`은 특정 Specification 타입에 매칭됩니다. 매칭되지 않는 타입은 `NotSupportedException`을 발생시킵니다.

#### 단일 테이블 예시 (DapperProductQuery)

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

    // DapperSpecTranslator를 주입하면 BuildWhereClause 오버라이드 불필요
    public DapperProductQuery(IDbConnection connection)
        : base(connection, ProductSpecTranslator.Instance) { }
}
```

#### JOIN 테이블 예시 (DapperProductWithStockQuery)

```csharp
[GenerateObservablePort]
public class DapperProductWithStockQuery
    : DapperQueryBase<Product, ProductWithStockDto>, IProductWithStockQuery
{
    public string RequestCategory => "QueryAdapter";

    // JOIN 시 테이블 별칭 사용
    protected override string SelectSql =>
        "SELECT p.Id AS ProductId, p.Name, p.Price, i.StockQuantity " +
        "FROM Products p INNER JOIN Inventories i ON i.ProductId = p.Id";
    protected override string CountSql =>
        "SELECT COUNT(*) FROM Products p INNER JOIN Inventories i ON i.ProductId = p.Id";
    protected override string DefaultOrderBy => "p.Name ASC";  // 별칭 포함

    // AllowedSortColumns에도 테이블 별칭 포함
    protected override Dictionary<string, string> AllowedSortColumns { get; } =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Name"] = "p.Name",
            ["Price"] = "p.Price",
            ["StockQuantity"] = "i.StockQuantity"
        };

    // DapperSpecTranslator + 테이블 별칭 "p"를 생성자에서 전달
    public DapperProductWithStockQuery(IDbConnection connection)
        : base(connection, ProductSpecTranslator.Instance, "p") { }
}
```

> **핵심:** JOIN 시 `DefaultOrderBy`, `AllowedSortColumns` 모두에서 테이블 별칭(`p.`, `i.`)을 사용해야 합니다. `DapperSpecTranslator`에 별칭을 전달하면 WHERE 절에도 자동으로 별칭이 적용됩니다.

#### LEFT JOIN 예시 (DapperProductWithOptionalStockQuery)

```csharp
protected override string SelectSql =>
    "SELECT p.Id AS ProductId, p.Name, p.Price, i.StockQuantity " +
    "FROM Products p LEFT JOIN Inventories i ON i.ProductId = p.Id";
```

LEFT JOIN의 결과에서 `i.StockQuantity`는 `null`이 될 수 있으므로 DTO에서 `int?`로 선언합니다:

```csharp
public sealed record ProductWithOptionalStockDto(
    string ProductId, string Name, decimal Price, int? StockQuantity);
```

#### GROUP BY 예시 (DapperCustomerOrderSummaryQuery)

```csharp
protected override string SelectSql =>
    "SELECT c.Id AS CustomerId, c.Name AS CustomerName, " +
    "COUNT(o.Id) AS OrderCount, " +
    "COALESCE(SUM(o.TotalAmount), 0) AS TotalSpent, " +
    "MAX(o.CreatedAt) AS LastOrderDate " +
    "FROM Customers c LEFT JOIN Orders o ON o.CustomerId = c.Id " +
    "GROUP BY c.Id, c.Name";
protected override string CountSql =>
    "SELECT COUNT(*) FROM Customers c";     // GROUP BY 전 원본 테이블 COUNT
```

GROUP BY의 `CountSql`은 GROUP BY 없이 원본 테이블 기준으로 작성합니다.

`AllowedSortColumns`에서 집계 컬럼은 별칭(AS 이름)을 직접 사용합니다:

```csharp
protected override Dictionary<string, string> AllowedSortColumns { get; } =
    new(StringComparer.OrdinalIgnoreCase)
    {
        ["CustomerName"] = "CustomerName",   // 집계 결과의 별칭
        ["OrderCount"] = "OrderCount",
        ["TotalSpent"] = "TotalSpent",
        ["LastOrderDate"] = "LastOrderDate"
    };
```

#### 복합 JOIN 예시 (DapperCustomerOrdersQuery) — QueryBase 미사용

4-table JOIN처럼 Row → DTO 그룹핑이 필요한 경우, `DapperQueryBase`를 사용하지 않고 직접 구현합니다:

```csharp
[GenerateObservablePort]
public class DapperCustomerOrdersQuery : ICustomerOrdersQuery  // IQueryPort 비제네릭 마커만
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

    public DapperCustomerOrdersQuery(IDbConnection connection) => _connection = connection;

    public virtual FinT<IO, CustomerOrdersDto> GetByCustomerId(CustomerId id)
    {
        return IO.liftAsync(async () =>
        {
            var customer = await _connection.QuerySingleOrDefaultAsync<CustomerRow>(
                CustomerSql, new { CustomerId = id.ToString() });

            if (customer is null)
                return AdapterError.For<DapperCustomerOrdersQuery>(
                    new NotFound(), id.ToString(),
                    $"고객 ID '{id}'을(를) 찾을 수 없습니다");

            var rows = (await _connection.QueryAsync<OrderLineRow>(
                OrderLinesSql, new { CustomerId = id.ToString() })).ToList();

            // Row → DTO 그룹핑
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

    // Dapper 매핑용 private record
    private sealed record CustomerRow(string CustomerId, string CustomerName);
    private sealed record OrderLineRow(
        string OrderId, decimal TotalAmount, string Status, DateTime CreatedAt,
        string ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);
}
```

### 3.3 InMemoryQueryBase 구현 패턴

#### 필수 추상 멤버

```csharp
protected abstract string DefaultSortField { get; }                          // 기본 정렬 필드명
protected abstract IEnumerable<TDto> GetProjectedItems(Specification<TEntity> spec);  // 필터 + 프로젝션
protected abstract Func<TDto, object> SortSelector(string fieldName);        // 정렬 키 셀렉터
```

#### 단일 테이블 예시 (InMemoryProductQuery)

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
            .Where(p => p.DeletedAt.IsNone && spec.IsSatisfiedBy(p))  // Soft Delete + Spec 필터
            .Select(p => new ProductSummaryDto(p.Id.ToString(), p.Name, p.Price));
    }

    protected override Func<ProductSummaryDto, object> SortSelector(string fieldName) => fieldName switch
    {
        "Name" => p => p.Name,
        "Price" => p => p.Price,
        _ => p => p.Name                                // 미지원 필드는 기본값으로 fallback
    };
}
```

#### JOIN 구현 — 다른 Repository의 static Store 접근

```csharp
[GenerateObservablePort]
public class InMemoryProductWithStockQuery
    : InMemoryQueryBase<Product, ProductWithStockDto>, IProductWithStockQuery
{
    public string RequestCategory => "QueryAdapter";

    protected override string DefaultSortField => "Name";

    protected override IEnumerable<ProductWithStockDto> GetProjectedItems(Specification<Product> spec)
    {
        return InMemoryProductRepository.Products.Values
            .Where(p => p.DeletedAt.IsNone && spec.IsSatisfiedBy(p))
            .Select(p =>
            {
                // 다른 Repository의 static Store에 직접 접근 (INNER JOIN)
                var inventory = InMemoryInventoryRepository.Inventories.Values
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

#### nullable 정렬 처리

LEFT JOIN 결과의 nullable 값을 정렬할 때 기본값을 제공합니다:

```csharp
protected override Func<ProductWithOptionalStockDto, object> SortSelector(string fieldName) => fieldName switch
{
    "StockQuantity" => p => p.StockQuantity ?? -1,           // int? → -1
    _ => p => p.Name
};

// DateTime? 예시
protected override Func<CustomerOrderSummaryDto, object> SortSelector(string fieldName) => fieldName switch
{
    "LastOrderDate" => c => c.LastOrderDate ?? DateTime.MinValue,  // DateTime? → MinValue
    _ => c => c.CustomerName
};
```

> `SortSelector`의 반환 타입이 `object`이므로, `null` 반환 시 NullReferenceException이 발생합니다. 항상 기본값을 제공하세요.

### 3.4 단건 조회 Query 패턴

`IQueryPort`(비제네릭 마커)만 상속하고, `GetById` 메서드를 직접 정의합니다:

```csharp
// 인터페이스
public interface IProductDetailQuery : IQueryPort
{
    FinT<IO, ProductDetailDto> GetById(ProductId id);
}

// Dapper 구현은 직접 SQL
// InMemory 구현은 static Store에서 TryGetValue
```

InMemory 단건 조회 예시:

```csharp
[GenerateObservablePort]
public class InMemoryProductDetailQuery : IProductDetailQuery
{
    public string RequestCategory => "QueryAdapter";

    public virtual FinT<IO, ProductDetailDto> GetById(ProductId id)
    {
        return IO.lift(() =>
        {
            if (InMemoryProductRepository.Products.TryGetValue(id, out var product)
                && product.DeletedAt.IsNone)
            {
                return Fin.Succ(new ProductDetailDto(
                    product.Id.ToString(), product.Name, product.Description,
                    product.Price, product.CreatedAt, product.UpdatedAt));
            }

            return AdapterError.For<InMemoryProductDetailQuery>(
                new NotFound(), id.ToString(),
                $"상품 ID '{id}'을(를) 찾을 수 없습니다");
        });
    }
}
```

### 3.5 Cursor 페이지네이션

Offset 페이지네이션(`Search`)의 대안으로, Keyset 기반 Cursor 페이지네이션(`SearchByCursor`)을 지원합니다.

#### API

```csharp
FinT<IO, CursorPagedResult<TDto>> SearchByCursor(
    Specification<TEntity> spec,
    CursorPageRequest cursor,
    SortExpression sort);
```

#### 요청/응답 타입

| 타입 | 속성 | 설명 |
|------|------|------|
| `CursorPageRequest` | `After` | 이 커서 이후의 데이터 조회 (forward) |
| | `Before` | 이 커서 이전의 데이터 조회 (backward) |
| | `PageSize` | 페이지 크기 (기본 20, 최대 10,000) |
| `CursorPagedResult<T>` | `Items` | 조회 결과 (`IReadOnlyList<T>`) |
| | `NextCursor` | 다음 페이지 커서 |
| | `PrevCursor` | 이전 페이지 커서 |
| | `HasMore` | 다음 데이터 존재 여부 |

#### Dapper 구현 원리

```sql
-- Cursor 페이지네이션 SQL
{SelectSql} {where} AND {sortColumn} > @CursorValue ORDER BY {sortColumn} {CursorPaginationClause}
```

`PageSize + 1`건을 fetch하여 `HasMore`를 판단합니다. 추가 COUNT 쿼리가 불필요하므로 대용량 데이터에서 Offset 방식보다 효율적입니다.

#### InMemory 구현 원리

`SortSelector`와 `FindLastIndex`/`FindIndex`를 사용하여 커서 위치를 찾고 슬라이싱합니다.

#### Offset vs Cursor 비교

| 항목 | Offset (`Search`) | Cursor (`SearchByCursor`) |
|------|-------------------|---------------------------|
| 총 건수 | O (COUNT 쿼리) | X (불필요) |
| 페이지 점프 | O (임의 페이지 이동) | X (순차 탐색만) |
| 대용량 성능 | 뒤 페이지일수록 느림 | 일정한 성능 |
| 실시간 데이터 | 삽입/삭제 시 중복/누락 | 커서 기반으로 안정적 |
| UI 적합성 | 페이지 번호 UI | 무한 스크롤, "더 보기" UI |

### 3.6 Compiled Query 패턴 (EF Core)

EF Core의 `EF.CompileAsyncQuery`를 사용하여 반복 호출 시 ~10-15% 성능 향상을 얻을 수 있습니다.

```csharp
// EfCoreProductRepository에서 opt-in 선언
private static readonly Func<LayeredArchDbContext, string, CancellationToken, Task<ProductModel?>>
    GetByIdIgnoringFiltersCompiled = EF.CompileAsyncQuery(
        (LayeredArchDbContext db, string id, CancellationToken _) =>
            db.Products.IgnoreQueryFilters().FirstOrDefault(p => p.Id == id));
```

**적용 원칙:**
- 베이스 클래스가 아닌 구체 서브클래스에서 opt-in으로 선언
- 동일 쿼리를 반복 호출하는 경우에만 적용 (예: `GetByIdIncludingDeleted`)
- Expression Tree 파싱 비용을 한 번만 지불하므로 반복 호출 시 유리

---

## 4. 잘못된 사례 (Anti-Patterns)

### 4.1 Repository Anti-Patterns

#### 1. ByIdPredicate 중복 구현

```csharp
// ❌ 잘못된 사례: 서브클래스마다 반복 구현
public class EfCoreProductRepository : EfCoreRepositoryBase<Product, ProductId, ProductModel>
{
    protected override Expression<Func<ProductModel, bool>> ByIdPredicate(ProductId id)
    {
        var s = id.ToString();
        return m => m.Id == s;  // 베이스에 이미 IHasStringId 기반 구현이 있음
    }
}
```

`EfCoreRepositoryBase`가 `IHasStringId` 기반 기본 구현을 제공합니다. 모든 Model이 `IHasStringId`를 구현하면 오버라이드가 불필요합니다.

#### 2. ReadQuery() 미사용

```csharp
// ❌ 잘못된 사례: DbSet에 직접 쿼리 → Include 누락 → N+1
var model = await DbSet.AsNoTracking()
    .FirstOrDefaultAsync(m => m.Id == id.ToString());

// ✅ 올바른 사례: ReadQuery() 사용 → Include 자동 적용
var model = await ReadQuery()
    .FirstOrDefaultAsync(ByIdPredicate(id));
```

#### 3. BuildQuery에서 예외 throw

```csharp
// ❌ 잘못된 사례: 예외 사용
protected Fin<IQueryable<TModel>> BuildQuery(Specification<TAggregate> spec)
{
    if (PropertyMap is null)
        throw new InvalidOperationException("PropertyMap is required");  // 예외!
}

// ✅ 올바른 사례: Fin<T>으로 에러 반환
return NotConfiguredError("PropertyMap is required for BuildQuery/ExistsBySpec.");
```

#### 4. 벌크 연산에서 도메인 이벤트 기대

`ExecuteDeleteAsync` / `ExecuteUpdateAsync`는 Change Tracker를 우회하므로 도메인 이벤트가 발행되지 않습니다.

```csharp
// ExecuteUpdateAsync — SQL 직접 실행, 도메인 이벤트 없음 (의도된 동작)
int affected = await DbSet.Where(ByIdsPredicate(ids))
    .ExecuteUpdateAsync(s => s
        .SetProperty(p => p.DeletedAt, DateTime.UtcNow)
        .SetProperty(p => p.DeletedBy, "system"));

// 이벤트가 필요하면 단건 Delete()를 사용
```

#### 5. PropertyMap 없이 ExistsBySpec 호출

```csharp
// ❌ PropertyMap을 생성자에 전달하지 않고 ExistsBySpec 호출 → 런타임 에러
public class EfCoreTagRepository : EfCoreRepositoryBase<Tag, TagId, TagModel>
{
    public EfCoreTagRepository(...) : base(eventCollector) { }  // propertyMap 없음

    public FinT<IO, bool> Exists(Specification<Tag> spec) => ExistsBySpec(spec);
    // → NotConfiguredError 반환
}
```

#### 6. IN 절 파라미터 제한 무시

베이스 클래스는 `IdBatchSize`(기본 500)로 자동 배치 처리합니다.
직접 `ByIdsPredicate`를 사용할 때는 이 제한을 직접 처리해야 합니다.

#### 7. Update에서 DbSet.Update 전체 Modified 설정

```csharp
// ❌ DbSet.Update — 모든 컬럼을 Modified로 설정하여 불필요한 UPDATE 발생
DbSet.Update(ToModel(aggregate));
EventCollector.Track(aggregate);

// ✅ TrackedMerge — FindAsync + SetValues로 변경된 컬럼만 UPDATE
var model = ToModel(aggregate);
var tracked = await DbSet.FindAsync(model.Id);
if (tracked is null) return NotFoundError(aggregate.Id);
DbContext.Entry(tracked).CurrentValues.SetValues(model);
EventCollector.Track(aggregate);
```

TrackedMerge는 `FindAsync`로 기존 엔티티를 추적 상태로 로드한 후 `SetValues`로 변경된 값만 덮어씁니다. EF Core Change Tracker가 자동으로 실제 변경된 컬럼만 UPDATE SQL에 포함하여 불필요한 DB I/O를 줄입니다.

### 4.2 Query Adapter Anti-Patterns

#### 1. AllowedSortColumns에 테이블 별칭 누락

```csharp
// ❌ JOIN 시 별칭 없이 컬럼명만 사용 → "ambiguous column name" 오류
protected override Dictionary<string, string> AllowedSortColumns { get; } =
    new() { ["Name"] = "Name" };  // Products.Name? Customers.Name?

// ✅ 테이블 별칭 포함
protected override Dictionary<string, string> AllowedSortColumns { get; } =
    new() { ["Name"] = "p.Name" };
```

#### 2. SortSelector에서 nullable 미처리

```csharp
// ❌ null 반환 → NullReferenceException (object boxing 시)
"StockQuantity" => p => p.StockQuantity  // int? → object 변환 시 null!

// ✅ 기본값 제공
"StockQuantity" => p => p.StockQuantity ?? -1
```

#### 3. BuildWhereClause에서 parameterized query 미사용

```csharp
// ❌ SQL Injection 위험
($"WHERE Name = '{spec.Name}'", new DynamicParameters())

// ✅ DapperSpecTranslator의 Params() 헬퍼 사용
var @params = DapperSpecTranslator<Product>.Params(("Name", (string)spec.Name));
return ("WHERE Name = @Name", @params);
```

> **권장**: `DapperSpecTranslator`를 사용하면 `Params()` 헬퍼로 안전한 파라미터 바인딩이 보장됩니다.

#### 4. InMemory GetProjectedItems에서 Specification 무시

```csharp
// ❌ Specification을 무시하고 항상 전체 데이터 반환
protected override IEnumerable<ProductSummaryDto> GetProjectedItems(Specification<Product> spec)
{
    return InMemoryProductRepository.Products.Values   // spec 미적용!
        .Select(p => new ProductSummaryDto(...));
}

// ✅ spec.IsSatisfiedBy 적용
    .Where(p => p.DeletedAt.IsNone && spec.IsSatisfiedBy(p))
```

#### 5. InMemory JOIN에서 O(N*M) 선형 탐색

```csharp
// ❌ 매 Product마다 Inventories 전체 순회 → O(N*M)
.Select(p =>
{
    var inventory = InMemoryInventoryRepository.Inventories.Values
        .FirstOrDefault(i => i.ProductId.Equals(p.Id));
    ...
})

// ✅ Dictionary 룩업으로 O(N) — 조회 전에 미리 빌드
var inventoryByProductId = InMemoryInventoryRepository.Inventories.Values
    .ToDictionary(i => i.ProductId);

.Select(p =>
{
    inventoryByProductId.TryGetValue(p.Id, out var inventory);
    ...
})
```

#### 6. Repository와 Query에서 Soft Delete 필터 불일치

Repository의 EF Core Global Query Filter와 Query Adapter의 WHERE 조건이 일치해야 합니다:
- **EfCore Repository** — `HasQueryFilter(p => p.DeletedAt == null)` (자동)
- **Dapper Query** — `WHERE p.DeletedAt IS NULL` (수동)
- **InMemory Query** — `.Where(p => p.DeletedAt.IsNone)` (수동)

---

## 5. 고급 패턴

### 5.1 Soft Delete 오버라이드

#### EfCore — ReadQueryIgnoringFilters + Attach + IsModified 패턴

```csharp
public override FinT<IO, int> Delete(ProductId id)
{
    return IO.liftAsync(async () =>
    {
        // 1. Global Filter 무시하여 이미 삭제된 것도 조회
        var model = await ReadQueryIgnoringFilters()
            .FirstOrDefaultAsync(ByIdPredicate(id));

        if (model is null) return NotFoundError(id);

        // 2. 도메인 상태 전이 (이벤트 발행)
        var product = ToDomain(model);
        product.Delete("system");

        // 3. Attach + IsModified로 변경된 컬럼만 UPDATE
        var updatedModel = ToModel(product);
        DbSet.Attach(updatedModel);
        _dbContext.Entry(updatedModel).Property(p => p.DeletedAt).IsModified = true;
        _dbContext.Entry(updatedModel).Property(p => p.DeletedBy).IsModified = true;

        EventCollector.Track(product);
        return Fin.Succ(1);
    });
}
```

벌크 Soft Delete는 성능을 위해 `ExecuteUpdateAsync` 사용 (이벤트 미발행):

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

#### InMemory — GetById/GetByIds에서 DeletedAt.IsNone 필터

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
        product.Delete("system");       // 도메인 상태 전이
        EventCollector.Track(product);  // 이벤트 수집
        return Fin.Succ(1);
    });
}
```

### 5.2 Specification → SQL 변환

#### PropertyMap 선언

```csharp
new PropertyMap<Product, ProductModel>()
    .Map(p => (decimal)p.Price, m => m.Price)
    .Map(p => (string)p.Name, m => m.Name)
    .Map(p => p.Id.ToString(), m => m.Id)
```

#### BuildQuery + ExistsBySpec

```csharp
// ExistsBySpec — 한 줄로 Specification 기반 존재 확인
public virtual FinT<IO, bool> Exists(Specification<Product> spec)
    => ExistsBySpec(spec);

// BuildQuery — Specification 기반 쿼리 빌드 (커스텀 사용)
var query = BuildQuery(spec);
// query는 Fin<IQueryable<TModel>>
```

#### Dapper의 DapperSpecTranslator 기반 변환

`DapperSpecTranslator`를 사용하면 Specification → SQL 변환을 공유 가능한 레지스트리로 관리합니다. 여러 Query Adapter가 동일한 Translator를 재사용할 수 있습니다.

```csharp
// 공유 Translator 정의 (한 번만 선언)
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

// Query Adapter에서 Translator 주입 — BuildWhereClause 오버라이드 불필요
public DapperProductQuery(IDbConnection connection)
    : base(connection, ProductSpecTranslator.Instance) { }

// JOIN Query에서 테이블 별칭과 함께 사용
public DapperProductWithStockQuery(IDbConnection connection)
    : base(connection, ProductSpecTranslator.Instance, "p") { }
```

> **기존 Pattern Matching 방식**도 `BuildWhereClause`를 직접 오버라이드하여 여전히 사용 가능합니다. Translator가 없는 생성자(`base(connection)`)를 사용하면 서브클래스에서 오버라이드가 필수입니다.

### 5.3 복합 JOIN Query (QueryBase 미사용)

#### Dapper — Row → DTO 그룹핑 패턴

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

#### InMemory — 다중 Repository static Store 접근

```csharp
public virtual FinT<IO, CustomerOrdersDto> GetByCustomerId(CustomerId id)
{
    return IO.lift(() =>
    {
        if (!InMemoryCustomerRepository.Customers.TryGetValue(id, out var customer))
            return /* NotFound error */;

        var orders = toSeq(InMemoryOrderRepository.Orders.Values
            .Where(o => o.CustomerId.Equals(id))
            .Select(o =>
            {
                var orderLines = toSeq(o.OrderLines.Select(l =>
                {
                    var product = InMemoryProductRepository.Products.Values
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

## 6. DI Registration 패턴

### Provider 기반 분기

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

### Repository 등록 — RegisterScopedObservablePort

`[GenerateObservablePort]` Source Generator가 `XxxObservable` 래퍼를 생성합니다.
등록 시 이 Observable 버전을 사용합니다:

```csharp
// InMemory
services.RegisterScopedObservablePort<IProductRepository, InMemoryProductRepositoryObservable>();
services.RegisterScopedObservablePort<IOrderRepository, InMemoryOrderRepositoryObservable>();
services.RegisterScopedObservablePort<ITagRepository, InMemoryTagRepositoryObservable>();

// EfCore (Sqlite)
services.RegisterScopedObservablePort<IProductRepository, EfCoreProductRepositoryObservable>();
services.RegisterScopedObservablePort<IOrderRepository, EfCoreOrderRepositoryObservable>();
```

### UnitOfWork 등록

```csharp
// InMemory
services.RegisterScopedObservablePort<IUnitOfWork, InMemoryUnitOfWorkObservable>();

// EfCore
services.RegisterScopedObservablePort<IUnitOfWork, EfCoreUnitOfWorkObservable>();
```

### Query Adapter 등록

```csharp
// InMemory — Query와 DetailQuery 모두 등록
services.RegisterScopedObservablePort<IProductQuery, InMemoryProductQueryObservable>();
services.RegisterScopedObservablePort<IProductDetailQuery, InMemoryProductDetailQueryObservable>();
services.RegisterScopedObservablePort<IProductWithStockQuery, InMemoryProductWithStockQueryObservable>();

// Dapper — IDbConnection도 등록 필요
services.AddScoped<IDbConnection>(_ =>
{
    var conn = new SqliteConnection(connectionString);
    conn.Open();
    return conn;
});
services.RegisterScopedObservablePort<IProductQuery, DapperProductQueryObservable>();
services.RegisterScopedObservablePort<IProductWithStockQuery, DapperProductWithStockQueryObservable>();
```

### InMemory Repository의 추가 등록

InMemory Query가 다른 Repository의 static Store에 접근할 때, 해당 Repository의 concrete 타입도 등록이 필요합니다:

```csharp
// InMemoryProductWithStockQuery가 InMemoryInventoryRepository.Inventories에 접근
services.AddScoped<InMemoryInventoryRepository>();

// InMemoryProductCatalog이 InMemoryProductRepository에 의존
services.AddScoped<InMemoryProductRepository>();
```

---

## 7. 트러블슈팅

### 벌크 DeleteRange 후 Change Tracker 상태가 불일치할 때

**원인:** `ExecuteDeleteAsync`/`ExecuteUpdateAsync`는 Change Tracker를 우회하므로, 이미 추적 중인 엔티티의 상태가 DB와 달라질 수 있습니다.

**해결:** `ReadQuery()`는 `AsNoTracking()`을 사용하므로 읽기 시에는 문제가 없습니다. 동일 트랜잭션 내에서 벌크 삭제 후 해당 엔티티를 Change Tracker로 조작해야 한다면, `DbContext.ChangeTracker.Clear()`를 호출하세요.

### Product 벌크 DeleteRange에서 도메인 이벤트가 발행되지 않을 때

**원인:** 의도된 동작입니다. `ExecuteUpdateAsync`는 도메인 객체를 생성하지 않으므로 이벤트가 발행되지 않습니다.

**해결:** 이벤트가 반드시 필요하다면 단건 `Delete()`를 개별 호출하세요. 성능이 중요하다면 벌크 `DeleteRange()`를 사용하고, 필요한 후처리를 별도로 수행하세요.

---

## 8. FAQ

### Repository

**Q: ByIdPredicate를 오버라이드해야 하나요?**
A: 아니요. 모든 Model이 `IHasStringId`를 구현하면 베이스의 기본 구현이 적용됩니다. 오버라이드가 필요한 경우는 복합 키나 Id 외 다른 컬럼으로 조회할 때입니다.

**Q: applyIncludes는 언제 설정하나요?**
A: Navigation Property가 있는 Aggregate만 설정합니다. `ReadQuery()`에 자동 적용되어 모든 읽기 메서드에서 N+1을 방지합니다. Navigation Property가 없으면 생략합니다.

**Q: PropertyMap은 언제 필요한가요?**
A: `Exists(Specification)` 또는 `BuildQuery`를 사용하는 Repository에서만 필요합니다. Tag처럼 단순 CRUD만 하는 Repository는 불필요합니다.

**Q: InMemoryRepository의 ConcurrentDictionary는 왜 static이어야 하나요?**
A: DI가 Scoped로 등록되어 요청마다 새 인스턴스가 생성됩니다. 데이터가 요청 간에 공유되려면 static이어야 합니다. 또한 InMemory Query Adapter가 static Store에 직접 접근합니다.

**Q: 왜 CRUD 8개 연산이 모두 대칭인가요?**
A: Create/Update는 호출자가 이미 도메인 객체를 가지고 있으므로, 단건이든 벌크든 `ToModel` → `DbSet.Add/Update`로 동일한 경로를 탑니다. Delete는 둘 다 ID를 받아 `ExecuteDeleteAsync`로 SQL DELETE를 직접 실행합니다. Read는 둘 다 `ReadQuery()`를 사용하고 조건만 단수/복수로 다릅니다. 자세한 비교표는 섹션 2.7을 참조하세요.

**Q: 벌크 DeleteRange에 단일 ID를 넘기면 어떻게 되나요?**
A: 정상 동작합니다. `DeleteRange(new[] { id })`는 단건 `Delete(id)`와 동일하게 1회 DB 왕복으로 삭제를 수행합니다. 둘 다 `ExecuteDeleteAsync`를 사용하므로 성능 차이가 없습니다.

**Q: Product 외에 Soft Delete가 필요한 엔티티가 추가되면 어떻게 하나요?**
A: `EfCoreProductRepository`의 패턴을 따르세요: (1) `Delete()`를 오버라이드하여 `ReadQueryIgnoringFilters` → `ToDomain` → 상태 전이 → `Attach + IsModified` 경로를 구현, (2) `DeleteRange()`를 오버라이드하여 `ExecuteUpdateAsync`로 `DeletedAt`/`DeletedBy`를 직접 갱신, (3) 글로벌 쿼리 필터를 `DbContext.OnModelCreating`에서 설정. 코드 예시는 섹션 5.1을 참조하세요.

### Query Adapter

**Q: 검색 Query와 단건 조회 Query의 차이는?**
A: 검색 Query는 `IQueryPort<TEntity, TDto>`를 구현하고 `DapperQueryBase`/`InMemoryQueryBase`를 상속합니다. 단건 조회는 `IQueryPort`(비제네릭)만 구현하고 `GetById` 메서드를 직접 정의합니다.

**Q: Dapper Query에서 새 Specification을 지원하려면?**
A: 공유 `DapperSpecTranslator`에 `When<TSpec>()` 핸들러를 추가합니다. `Params()` 헬퍼로 파라미터 바인딩을 생성하세요. Translator를 사용하지 않는 경우 `BuildWhereClause`를 직접 오버라이드하여 새 case를 추가합니다.

**Q: InMemory Query에서 JOIN은 어떻게 하나요?**
A: 다른 Repository의 `internal static` ConcurrentDictionary에 직접 접근합니다. LINQ의 `FirstOrDefault`, `Where` 등으로 JOIN을 모방합니다.

**Q: GROUP BY 집계를 InMemory로 구현하려면?**
A: `GetProjectedItems`에서 LINQ의 `GroupBy`, `Count()`, `Sum()`, `Max()` 등을 사용합니다. `InMemoryCustomerOrderSummaryQuery`를 참고하세요.

**Q: `[GenerateObservablePort]`는 무엇인가요?**
A: Source Generator가 관찰 가능한 래퍼 클래스(`XxxObservable`)를 자동 생성합니다. 이 래퍼는 메서드 호출을 로깅/트레이싱하는 파이프라인을 포함합니다. DI 등록 시 이 Observable 버전을 사용합니다.

---

## 9. 참고 문서

- [13-adapters.md](./13-adapters.md) — Adapter 구현 가이드
- [15a-unit-testing.md](./15a-unit-testing.md) — 단위 테스트 규칙
- [OPTIMIZATION-TECHNIQUES.md](../../Src.Benchmarks/BulkCrud.Benchmarks/OPTIMIZATION-TECHNIQUES.md) — 대량 CRUD 성능 최적화 기법
