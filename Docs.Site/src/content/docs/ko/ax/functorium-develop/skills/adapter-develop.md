---
title: "Adapter Develop"
description: "Repository, Query Adapter, Endpoint, DI 등록 구현"
---

> project-spec -> architecture-design -> domain-develop -> application-develop -> **adapter-develop** -> observability-develop -> test-develop

## 선행 조건

- `application-develop` 스킬에서 생성한 `application/03-implementation-results.md`를 읽어 Port 목록(IRepository, IQueryPort, External Service)을 확인합니다.
- `architecture-design` 스킬에서 생성한 `01-architecture-design.md`가 있으면 읽어 폴더 구조와 영속성 전략을 확인합니다.
- 선행 문서가 없으면 사용자에게 직접 질문합니다.

## 배경

Application 레이어의 Port 인터페이스가 정의되면, 이를 구현하는 Adapter를 작성해야 합니다. InMemory Repository, EF Core Repository, Dapper Query Adapter, FastEndpoints 엔드포인트, DI 등록까지 — 각 Adapter 유형마다 반복되는 구조(베이스 클래스 상속, `[GenerateObservablePort]`, `IO.lift`/`IO.liftAsync` 래핑, Mapper, PropertyMap)가 있습니다.

`/adapter-develop` 스킬은 이 반복을 자동화합니다. Port 인터페이스와 요구사항을 전달하면, Functorium 프레임워크 패턴에 맞는 Adapter 구현체, Mapper, DI 등록 코드를 4단계로 생성합니다.

## 스킬 개요

### 4단계 프로세스

| 단계 | 작업 | 산출물 |
|------|------|--------|
| 1 | 포트 → 어댑터 매핑 | Port 인터페이스별 Adapter 유형 결정 |
| 2 | 구현 생성 | Adapter 클래스, Model, Mapper, Configuration |
| 3 | DI 등록 | `RegisterScopedObservablePort` 등록 코드 |
| 4 | EF Core 설정 | DbContext, Migration, Query Filter (해당 시) |

### 지원하는 어댑터

| 어댑터 유형 | 기반 클래스 | IO 래핑 | 설명 |
|------------|-----------|---------|------|
| InMemory Repository | `InMemoryRepositoryBase<T, TId>` | `IO.lift` | 테스트용 메모리 저장소 |
| EF Core Repository | `EfCoreRepositoryBase<T, TId, TModel>` | `IO.liftAsync` | 영속화 저장소 |
| Dapper Query | `DapperQueryBase<T, TDto>` | — | CQRS Read Side |
| FastEndpoints | `Endpoint<TReq, TRes>` | — | HTTP 엔드포인트 |
| External API | 직접 구현 | `IO.liftAsync` | 외부 HTTP API 연동 |

### 핵심 API 패턴

| 패턴 | 사용법 |
|------|--------|
| Observable Port | `[GenerateObservablePort]` 어트리뷰트 적용 |
| 동기 래핑 | `IO.lift(() => Fin.Succ(value))` |
| 비동기 래핑 | `IO.liftAsync(async () => { ... return Fin.Succ(result); })` |
| Adapter 에러 | `AdapterError.For<TAdapter>(new NotFound(), id, message)` |
| DI 등록 | `services.RegisterScopedObservablePort<IPort, AdapterObservable>()` |
| Mapper | `internal static class` 확장 메서드, `ToModel()`/`ToDomain()` |

### 네이밍 규칙 + 폴더 구조

Adapter 프로젝트는 3차원 폴더 구조를 따릅니다.

```
{Name}.Adapters.Persistence/
└── {Aggregate}/                     # 1차: Aggregate (무엇)
    ├── {Aggregate}.Model.cs         # DB POCO
    ├── {Aggregate}.Configuration.cs # EF Core Fluent API
    ├── {Aggregate}.Mapper.cs        # Domain <-> Model 변환
    ├── Repositories/                # 2차: CQRS Role (쓰기)
    │   ├── {Aggregate}RepositoryEfCore.cs    # 3차: Technology (어떻게)
    │   └── {Aggregate}RepositoryInMemory.cs
    └── Queries/                     # 2차: CQRS Role (읽기)
        ├── {Aggregate}QueryDapper.cs
        └── {Aggregate}QueryInMemory.cs
```

**네이밍 패턴:** `{Subject}{Role}{Variant}` (예: `ProductRepositoryEfCore`, `ProductQueryDapper`)

### ObservableSignal -- Adapter 내부 운영 로깅

Observable Port의 자동 관측성과 별도로, Adapter 내부에서 개발자가 직접 운영 로그를 남길 때 `ObservableSignal`을 사용합니다.

```csharp
public override FinT<IO, Product> GetById(ProductId id)
{
    return IO.liftAsync(async () =>
    {
        var model = await ReadQuery().FirstOrDefaultAsync(ByIdPredicate(id));
        if (model is null)
        {
            ObservableSignal.Info("cache_miss", new { ProductId = id.ToString() });
            return NotFoundError(id);
        }
        ObservableSignal.Info("cache_hit", new { ProductId = id.ToString() });
        return Fin.Succ(ToDomain(model));
    });
}
```

ObservableSignal 부가 필드는 `adapter.*` 프리픽스를 사용합니다. 이 필드는 Logging Pillar에만 전파됩니다.

## 사용 방법

### 기본 호출

```text
/adapter-develop 상품 InMemory Repository를 만들어줘.
```

### 대화형 모드

인자 없이 `/adapter-develop`만 호출하면, 스킬이 대화형으로 요구사항을 수집합니다.

### 실행 흐름

1. **어댑터 분석** — Port별 Adapter 유형과 구현 계획을 표로 보여줍니다
2. **사용자 확인** — 분석 결과를 확인한 후 코드 생성으로 진행합니다
3. **코드 생성** — Adapter, Model, Mapper, DI 등록 코드를 생성합니다
4. **빌드 검증** — `dotnet build`를 실행하여 통과를 확인합니다

## 예제 1: 초급 — InMemory Repository

가장 기본적인 Adapter 패턴입니다. `InMemoryRepositoryBase` 상속, `[GenerateObservablePort]` 적용, `IO.lift` 래핑으로 테스트용 메모리 저장소를 구현합니다.

### 프롬프트

```text
/adapter-develop 상품 InMemory Repository를 만들어줘.
```

### 기대 결과

| 산출물 | 타입 | 설명 |
|--------|------|------|
| Repository | `InMemoryProductRepository` | `InMemoryRepositoryBase<Product, ProductId>` 상속 |
| DI 등록 | `RegisterScopedObservablePort` | Observable 버전으로 등록 |

### 핵심 스니펫

**InMemory Repository** — `InMemoryRepositoryBase` 상속, `[GenerateObservablePort]` 적용:

```csharp
[GenerateObservablePort]
public class InMemoryProductRepository
    : InMemoryRepositoryBase<Product, ProductId>, IProductRepository
{
    internal static readonly ConcurrentDictionary<ProductId, Product> Products = new();
    protected override ConcurrentDictionary<ProductId, Product> Store => Products;

    public InMemoryProductRepository(IDomainEventCollector eventCollector)
        : base(eventCollector) { }

    public virtual FinT<IO, bool> Exists(Specification<Product> spec)
    {
        return IO.lift(() =>
        {
            bool exists = Products.Values.Any(p => spec.IsSatisfiedBy(p));
            return Fin.Succ(exists);
        });
    }
}
```

**DI 등록** — Source Generator가 생성한 Observable 버전 사용:

```csharp
services.RegisterScopedObservablePort<IProductRepository, InMemoryProductRepositoryObservable>();
```

## 예제 2: 중급 — EF Core Repository + Configuration + Mapper

예제 1에 영속화 계층을 추가합니다. EF Core Repository의 3인자 생성자 패턴(EventCollector, ApplyIncludes, PropertyMap), Persistence Model(POCO), Mapper 확장 메서드, EF Core Configuration을 보여줍니다.

### 프롬프트

```text
/adapter-develop 상품 EF Core Repository를 구현해줘. Soft Delete 포함, SQLite 사용.
```

### 기대 결과

| 산출물 | 타입 | 설명 |
|--------|------|------|
| Repository | `EfCoreProductRepository` | `EfCoreRepositoryBase` 상속, Soft Delete 오버라이드 |
| Model | `ProductModel` | POCO, primitive 타입만 |
| Configuration | `ProductConfiguration` | EF Core Fluent API, Query Filter |
| Mapper | `ProductMapper` | `internal static class`, `ToModel()`/`ToDomain()` |
| DI 등록 | `RegisterScopedObservablePort` | Observable 버전으로 등록 |

### 핵심 스니펫

**EF Core Repository** — 3인자 생성자, PropertyMap, Soft Delete 오버라이드:

```csharp
[GenerateObservablePort]
public class EfCoreProductRepository
    : EfCoreRepositoryBase<Product, ProductId, ProductModel>, IProductRepository
{
    private readonly LayeredArchDbContext _dbContext;

    public EfCoreProductRepository(LayeredArchDbContext dbContext, IDomainEventCollector eventCollector)
        : base(eventCollector,
               q => q.Include(p => p.ProductTags),
               new PropertyMap<Product, ProductModel>()
                   .Map(p => (decimal)p.Price, m => m.Price)
                   .Map(p => (string)p.Name, m => m.Name)
                   .Map(p => p.Id.ToString(), m => m.Id))
        => _dbContext = dbContext;

    protected override DbContext DbContext => _dbContext;
    protected override DbSet<ProductModel> DbSet => _dbContext.Products;
    protected override Product ToDomain(ProductModel model) => model.ToDomain();
    protected override ProductModel ToModel(Product p) => p.ToModel();

    // Soft Delete 오버라이드
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
}
```

**Mapper** — `internal static class`, `CreateFromValidated`로 검증 없이 도메인 복원:

```csharp
internal static class ProductMapper
{
    public static ProductModel ToModel(this Product product) => new()
    {
        Id = product.Id.ToString(),
        Name = product.Name,
        Price = product.Price,
        CreatedAt = product.CreatedAt,
        DeletedAt = product.DeletedAt.ToNullable(),
        DeletedBy = product.DeletedBy.Match(Some: v => (string?)v, None: () => null),
    };

    public static Product ToDomain(this ProductModel model) =>
        Product.CreateFromValidated(
            ProductId.Create(model.Id),
            ProductName.CreateFromValidated(model.Name),
            Money.CreateFromValidated(model.Price),
            model.CreatedAt,
            Optional(model.DeletedAt),
            Optional(model.DeletedBy));
}
```

## 예제 3: 고급 — Dapper Query + Specification + Endpoint

예제 2에 CQRS Read Side를 추가합니다. Dapper 기반 Query Adapter, Specification을 SQL WHERE 절로 변환하는 `DapperSpecTranslator`, FastEndpoints 엔드포인트, DI 등록을 보여줍니다.

### 프롬프트

```text
/adapter-develop 상품 검색 API를 구현해줘. Dapper Query + Specification 기반 검색 + 페이지네이션 + FastEndpoints.
```

### 기대 결과

| 산출물 | 타입 | 설명 |
|--------|------|------|
| Query Adapter | `DapperProductQuery` | `DapperQueryBase<Product, ProductSummaryDto>` 상속 |
| Spec Translator | `ProductSpecTranslator` | Specification → SQL WHERE 절 변환 |
| Endpoint | `SearchProductsEndpoint` | FastEndpoints, 페이지네이션/정렬 지원 |
| DI 등록 | `RegisterScopedObservablePort` | Query Adapter + IDbConnection 등록 |

### 핵심 스니펫

**Dapper Query Adapter** — `DapperQueryBase` 상속, SQL 직접 작성:

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

**DapperSpecTranslator** — Specification을 SQL WHERE 절로 변환:

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

**FastEndpoints Endpoint** — `SendFinResponseAsync`로 HTTP 응답 변환:

```csharp
public sealed class SearchProductsEndpoint
    : Endpoint<SearchProductsEndpoint.Request, SearchProductsEndpoint.Response>
{
    private readonly IMediator _mediator;

    public SearchProductsEndpoint(IMediator mediator) => _mediator = mediator;

    public override void Configure()
    {
        Get("api/products/search");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new SearchProductsQuery.Request(
            req.Name ?? "", req.MinPrice ?? 0, req.MaxPrice ?? 0,
            req.Page ?? 1, req.PageSize ?? PageRequest.DefaultPageSize,
            req.SortBy ?? "", req.SortDirection ?? "");

        var result = await _mediator.Send(usecaseRequest, ct);
        var mapped = result.Map(r => new Response(r.Products.ToList(), r.TotalCount, r.Page, r.PageSize));
        await this.SendFinResponseAsync(mapped, ct);
    }

    public sealed record Request(
        [property: QueryParam] string? Name = null,
        [property: QueryParam] decimal? MinPrice = null,
        [property: QueryParam] decimal? MaxPrice = null,
        [property: QueryParam] int? Page = null,
        [property: QueryParam] int? PageSize = null,
        [property: QueryParam] string? SortBy = null,
        [property: QueryParam] string? SortDirection = null);

    public new sealed record Response(
        List<ProductSummaryDto> Products, int TotalCount, int Page, int PageSize);
}
```

**DI 등록** — Provider별 분기, Observable 버전 사용:

```csharp
// InMemory
services.RegisterScopedObservablePort<IProductRepository, InMemoryProductRepositoryObservable>();
services.RegisterScopedObservablePort<IProductQuery, InMemoryProductQueryObservable>();

// SQLite (EF Core + Dapper)
services.RegisterScopedObservablePort<IProductRepository, EfCoreProductRepositoryObservable>();
services.AddScoped<IDbConnection>(_ =>
{
    var conn = new SqliteConnection(connectionString);
    conn.Open();
    return conn;
});
services.RegisterScopedObservablePort<IProductQuery, DapperProductQueryObservable>();
```

## 참고 자료

### 워크플로

- [워크플로](../workflow/) -- 7단계 전체 흐름
- [Application Develop 스킬](../application-develop/) -- 이전 단계: 유스케이스 구현
- [Test Develop 스킬](../test-develop/) -- 다음 단계: 테스트 작성

### 프레임워크 가이드

- [Port 정의](../guides/adapter/12-ports/)
- [Adapter 구현](../guides/adapter/13-adapters/)
- [Pipeline과 DI](../guides/adapter/14a-adapter-pipeline-di/)
- [Adapter 테스트](../guides/adapter/14b-adapter-testing/)
- [Repository & Query Adapter 구현 가이드](../guides/adapter/14c-repository-query-implementation-guide/)
- [에러 시스템: Adapter & 테스트](../guides/domain/08c-error-system-adapter-testing/)

### 관련 스킬

- [도메인 개발 스킬](../domain-develop/) -- Aggregate, Value Object, Event 등 도메인 빌딩블록 생성
- [Application 레이어 개발 스킬](../application-develop/) -- Command/Query/EventHandler 유스케이스 생성
- [테스트 개발 스킬](../test-develop/) -- 단위/통합/아키텍처 테스트 생성
