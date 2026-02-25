# Adapter 구현

이 문서는 Port 인터페이스의 구현체인 Adapter를 유형별로 구현하는 가이드입니다. Port 정의는 [12-ports.md](./12-ports.md), Pipeline 생성과 DI 등록은 [14-adapter-wiring.md](./14-adapter-wiring.md)을 참조하세요.

## 목차

- [요약](#요약)
- [Activity 2: Adapter 구현](#activity-2-adapter-구현)
- [공통 구현 체크리스트](#공통-구현-체크리스트)
- [공통 패턴](#공통-패턴)
  - [외부 시스템 유형별 ACL 체크리스트](#외부-시스템-유형별-acl-체크리스트)
- [Repository Adapter](#repository-adapter)
- [External API Adapter](#external-api-adapter)
- [Messaging Adapter](#messaging-adapter)
- [Query Adapter (CQRS Read 측)](#query-adapter-cqrs-read-측)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)
- [참고 문서](#참고-문서)

---

## 요약

### 주요 명령

```csharp
// Adapter 기본 구조
[GenerateObservablePort]
public class InMemoryProductRepository : IProductRepository
{
    public string RequestCategory => "Repository";

    public virtual FinT<IO, Product> GetById(ProductId id)
    {
        return IO.lift(() => { /* 비즈니스 로직 */ });
    }
}

// 동기 작업: IO.lift
return IO.lift(() => Fin.Succ(value));

// 비동기 작업: IO.liftAsync
return IO.liftAsync(async () => { var result = await ...; return Fin.Succ(result); });

// 에러 반환
return Fin.Fail<T>(AdapterError.For<TAdapter>(errorType, context, message));
```

### 주요 절차

1. `[GenerateObservablePort]` 어트리뷰트를 클래스에 적용
2. Port 인터페이스 구현 및 `RequestCategory` 프로퍼티 정의
3. 모든 인터페이스 메서드에 `virtual` 키워드 추가
4. `IO.lift()` (동기) 또는 `IO.liftAsync()` (비동기)로 비즈니스 로직 래핑
5. 성공은 `Fin.Succ(value)`, 실패는 `AdapterError.For<T>(...)` 사용
6. 필요 시 Mapper 클래스를 `internal`로 정의하여 도메인/기술 모델 변환

### 주요 개념

| 개념 | 설명 |
|------|------|
| `[GenerateObservablePort]` | Source Generator가 Observability Pipeline을 자동 생성하는 어트리뷰트 |
| `IO.lift` / `IO.liftAsync` | 동기/비동기 작업을 `FinT<IO, T>`로 래핑하는 메서드 |
| `virtual` 키워드 | Pipeline이 메서드를 override하기 위해 필수 |
| `RequestCategory` | Observability 로그에서 사용할 카테고리 (`"Repository"`, `"ExternalApi"` 등) |
| Mapper 패턴 | 도메인 모델과 기술 모델(POCO, DTO) 간 변환을 담당하는 `internal` 클래스 |
| `AdapterError` | Adapter 레이어 전용 에러 타입 (`For<T>`, `FromException<T>`) |

---

## Activity 2: Adapter 구현

Adapter는 Port 인터페이스의 **구현체**입니다. `[GenerateObservablePort]` 어트리뷰트를 통해 Observability Pipeline이 자동 생성됩니다.

> **Source Generator 참고**: `[GenerateObservablePort]`는 Roslyn Incremental Source Generator로 구현되어 있어 빌드 시 증분 생성됩니다. Adapter 수가 많은 프로젝트에서는 `obj/GeneratedFiles/`에 생성된 코드를 확인하여 Pipeline이 올바르게 생성되었는지 검증하세요. `IO.lift`/`IO.liftAsync`로 래핑된 메서드만 Pipeline 대상이 되며, `virtual` 키워드가 없으면 Pipeline이 메서드를 오버라이드할 수 없습니다.

### 공통 구현 체크리스트

모든 Adapter 구현에 필수인 항목입니다.

- [ ] `[GenerateObservablePort]` 어트리뷰트를 클래스에 적용했는가?
- [ ] Port 인터페이스를 구현하는가?
- [ ] `RequestCategory` 프로퍼티를 정의했는가?
- [ ] 모든 인터페이스 메서드에 `virtual` 키워드를 추가했는가?
- [ ] `IO.lift()` 또는 `IO.liftAsync()` 로 비즈니스 로직을 래핑했는가?
- [ ] Mapper 클래스가 `internal`로 선언되어 있는가? (해당 시)

### 공통 패턴

모든 Adapter 유형에 공통으로 적용되는 패턴입니다. 유형별 Adapter 구현 전에 먼저 숙지하세요.

#### IO.lift vs IO.liftAsync 판단

| 기준 | `IO.lift(() => { ... })` | `IO.liftAsync(async () => { ... })` |
|------|--------------------------|--------------------------------------|
| 작업 유형 | 동기 (sync) | 비동기 (async/await) |
| 대표 사례 | In-Memory 저장소, 캐시 조회 | HTTP 호출, 메시지 전송, DB 비동기 쿼리 |
| 반환 | `Fin<T>` | `Fin<T>` |
| 사용 유형 | Repository (동기) | External API, Messaging |

**판단 기준**: 내부에서 `await`를 사용해야 하는가?
- **예** → `IO.liftAsync`
- **아니오** → `IO.lift`

> **참고**: EF Core 등 비동기 DB 접근 시에는 Repository에서도 `IO.liftAsync`를 사용합니다.

#### 데이터 변환 (Mapper 패턴)

Adapter 내부에서 Port의 도메인 모델과 기술 관심사 DTO 간의 변환을 처리합니다. Mapper 클래스는 반드시 `internal`로 선언합니다.

##### Infrastructure Adapter (HTTP API)

```csharp
// Adapters.Infrastructure/Apis/CriteriaApi/CriteriaApiService.cs
[GenerateObservablePort]
public class CriteriaApiService : ICriteriaApiService
{
    private readonly HttpClient _httpClient;

    public string RequestCategory => "ExternalApi";

    #region Error Types
    public sealed record ResponseNull : AdapterErrorType.Custom;
    #endregion

    public virtual FinT<IO, ICriteriaApiService.Response> GetEquipHistoriesAsync(
        ICriteriaApiService.Request request,
        CancellationToken cancellationToken)
    {
        return IO.liftAsync(async () =>
        {
            // 1. Port Request → Query Parameters 변환
            var queryParams = CriteriaApiMapper.ToQueryParams(request);

            // 2. HTTP 호출
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

            // 3. Infrastructure DTO → Port Response 변환
            var dto = await response.Content.ReadFromJsonAsync<GetEquipHistoryResponseDto>(cancellationToken);
            return dto?.Histories is not null
                ? Fin.Succ(CriteriaApiMapper.ToResponse(dto))
                : Fin.Fail<ICriteriaApiService.Response>(
                    AdapterError.For<CriteriaApiService>(new ResponseNull(), url, "Response data is null"));
        });
    }
}

// Mapper 클래스 (Infrastructure 내부 - internal)
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

// Infrastructure 내부 DTO (internal - 외부 노출 안 함)
internal record GetEquipHistoryResponseDto(List<EquipDto> Histories);
internal record EquipDto(string LineId, string TypeId, string ModelId, ...);
```

##### Persistence Adapter (Repository)

Persistence Adapter는 **Persistence Model(POCO)** 과 **Mapper(확장 메서드)** 를 사용하여 도메인 엔티티와 DB 모델을 분리합니다. EF Core `HasConversion` 대신 Mapper에서 명시적으로 변환합니다.

```csharp
// Persistence Model — POCO (primitive 타입만, 도메인 의존성 없음)
// 파일: {Adapters.Persistence}/Repositories/EfCore/Models/ProductModel.cs
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
// Mapper — internal static class, 확장 메서드
// 파일: {Adapters.Persistence}/Repositories/EfCore/Mappers/ProductMapper.cs
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

        return Product.CreateFromValidated(   // 검증 없이 복원
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
// Repository — Mapper 확장 메서드 사용
// 파일: {Adapters.Persistence}/Repositories/EfCore/EfCoreProductRepository.cs
[GenerateObservablePort]
public class EfCoreProductRepository : IProductRepository
{
    private readonly LayeredArchDbContext _dbContext;
    private readonly IDomainEventCollector _eventCollector;

    public string RequestCategory => "Repository";

    public virtual FinT<IO, Product> GetById(ProductId id)
    {
        return IO.liftAsync(async () =>
        {
            var model = await _dbContext.Products
                .AsNoTracking()
                .Include(p => p.ProductTags)
                .FirstOrDefaultAsync(p => p.Id == id.ToString());

            if (model is not null)
            {
                return Fin.Succ(model.ToDomain());   // 확장 메서드로 도메인 복원
            }

            return AdapterError.For<EfCoreProductRepository>(
                new NotFound(),
                id.ToString(),
                $"상품 ID '{id}'을(를) 찾을 수 없습니다");
        });
    }

    public virtual FinT<IO, Product> Create(Product product)
    {
        return IO.liftAsync(async () =>
        {
            _dbContext.Products.Add(product.ToModel());  // 확장 메서드로 Model 변환
            _eventCollector.Track(product);
            return Fin.Succ(product);
        });
    }
}
```

#### 에러 처리 통합

##### Error 반환 단순화

LanguageExt는 `Error → Fin<T>` 암시적 변환을 제공합니다.
따라서 `Fin.Fail<T>(error)` 대신 `error`를 직접 반환할 수 있습니다:

```csharp
// 기존 방식 (verbose)
return Fin.Fail<Money>(AdapterError.For<MyAdapter>(
    new NotFound(), context, "Not found"));

// 권장 방식 (implicit conversion)
return AdapterError.For<MyAdapter>(
    new NotFound(), context, "Not found");
```

예외 처리에서도 동일하게 적용됩니다:

```csharp
catch (HttpRequestException ex)
{
    // 기존 방식
    return Fin.Fail<Money>(AdapterError.FromException<MyAdapter>(
        new ConnectionFailed("ServiceName"), ex));

    // 권장 방식
    return AdapterError.FromException<MyAdapter>(
        new ConnectionFailed("ServiceName"), ex);
}
```

> **참고**: 메서드 반환 타입이 `Fin<T>` 또는 `FinT<IO, T>`로 명시되어 있어야
> 암시적 변환이 작동합니다.

##### FinT<IO, T>와 AdapterError 연계

```csharp
// AdapterErrorType 사용 패턴
using static Functorium.Adapters.Errors.AdapterErrorType;

// NotFound - 리소스를 찾을 수 없음
AdapterError.For<ProductRepository>(
    new NotFound(),
    productId.ToString(),
    "Product not found");

// AlreadyExists - 리소스가 이미 존재함
AdapterError.For<ProductRepository>(
    new AlreadyExists(),
    productName,
    "Product already exists");

// ConnectionFailed - 외부 시스템 연결 실패
AdapterError.For<CriteriaApiService>(
    new ConnectionFailed("HTTP"),
    url,
    "API connection failed");

// Custom - 사용자 정의 에러 타입
// Error type definition: public sealed record ReservationFailed : AdapterErrorType.Custom;
AdapterError.For<InventoryRepository>(
    new ReservationFailed(),
    orderId.ToString(),
    "Failed to reserve inventory");

// Exception 래핑
AdapterError.FromException<ProductRepository>(
    new PipelineException(),
    exception);
```

##### Pipeline의 자동 에러 분류

```
에러 타입                              로그 레벨      메트릭 태그
────────────────────────────────────────────────────────────────
IHasErrorCode + IsExpected  ────────► Warning       error.type: "expected"
IHasErrorCode + IsExceptional ──────► Error         error.type: "exceptional"
ManyErrors ─────────────────────────► Warning/Error error.type: "aggregate"
```

##### 값 객체 공유 전략

```
┌──────────────────────────────────────────────────────────────┐
│                      Domain Layer                            │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Value Objects (모든 레이어에서 공유)                    │  │
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
│ ProductId 사용   │  │ ProductId →   │  │ ProductModel (POCO)   │
│                  │  │ string (DTO)  │  │ ProductId → string    │
└──────────────────┘  └──────────────┘  └───────────────────────┘
```

#### 외부 시스템 유형별 ACL 체크리스트

##### ACL 공통 원칙

- Port는 도메인 타입(VO, Entity, Domain Event)만 사용한다
- Adapter 내부에 기술 특화 모델/DTO를 정의한다 (`internal` 가시성)
- Adapter 내부에 Mapper를 정의한다 (`internal static class`, 확장 메서드)
- 외부 타입은 Application/Domain 레이어로 절대 노출하지 않는다

##### 시스템 유형별 매핑 표

| 외부 시스템 유형 | Adapter 프로젝트 | 내부 변환 타입 | Mapper 패턴 | 기존 예시 |
|---|---|---|---|---|
| Database (RDBMS) | Persistence | `internal class XxxModel` (POCO) | `internal static class XxxMapper` (확장 메서드) | `ProductModel` + `ProductMapper` (§2.2) |
| External HTTP API | Infrastructure | `internal record XxxDto` | `internal static class XxxApiMapper` | `CriteriaApiMapper` (§2.2) |
| Message Broker | Infrastructure | `internal record XxxMessage` | `internal static class XxxMessageMapper` | 해당 시 적용 (§2.5 참조) |
| File System | Infrastructure | `internal record/class XxxFileModel` | `internal static class XxxFileMapper` | — (패턴 동일) |
| Cache | Infrastructure | `internal record XxxCacheEntry` | `internal static class XxxCacheMapper` | — (패턴 동일) |
| 외부 인증/인가 | Infrastructure | `internal record XxxAuthResponse` | `internal static class XxxAuthMapper` | — (패턴 동일) |

##### ACL 적용 판단 기준

```
새 외부 시스템 연동
├─ 외부 스키마가 독립적으로 변경 가능? → ACL 필수 (internal DTO + Mapper)
└─ 공유 계약(shared contract)으로 공동 관리? → ACL 선택적 (Pass-through 허용)
```

- **ACL 필수 예**: 레거시 DB, 외부 팀의 API, 서드파티 메시지 스키마
- **Pass-through 허용 예**: 같은 팀의 공유 메시지 계약 (현재 Messaging Adapter 패턴)

### Repository Adapter

Repository Adapter는 데이터 저장소에 대한 CRUD 작업을 구현합니다.

#### InMemory Repository

```csharp
// 파일: {Adapters.Persistence}/Repositories/InMemory/InMemoryProductRepository.cs

using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using static Functorium.Adapters.Errors.AdapterErrorType;
using static LanguageExt.Prelude;

[GenerateObservablePort]                                    // 1. Pipeline 자동 생성
public class InMemoryProductRepository : IProductRepository  // 2. Port 인터페이스 구현
{
    internal static readonly ConcurrentDictionary<ProductId, Product> Products = new();
    private readonly IDomainEventCollector _eventCollector;

    public string RequestCategory => "Repository";     // 3. 요청 카테고리

    public InMemoryProductRepository(IDomainEventCollector eventCollector)  // 4. 생성자
    {
        _eventCollector = eventCollector;
    }

    public virtual FinT<IO, Product> Create(Product product)  // 5. virtual 필수
    {
        return IO.lift(() =>                           // 6. IO.lift (동기)
        {
            Products[product.Id] = product;
            _eventCollector.Track(product);
            return Fin.Succ(product);                  // 7. 성공 반환
        });
    }

    public virtual FinT<IO, Product> GetById(ProductId id)
    {
        return IO.lift(() =>
        {
            if (Products.TryGetValue(id, out Product? product) && product.DeletedAt.IsNone)
            {
                return Fin.Succ(product);
            }

            return AdapterError.For<InMemoryProductRepository>(  // 8. 실패 반환
                new NotFound(),
                id.ToString(),
                $"상품 ID '{id}'을(를) 찾을 수 없습니다");
        });
    }

    public virtual FinT<IO, Unit> Delete(ProductId id)
    {
        return IO.lift(() =>
        {
            if (!Products.TryGetValue(id, out var product))
            {
                return AdapterError.For<InMemoryProductRepository>(
                    new NotFound(),
                    id.ToString(),
                    $"상품 ID '{id}'을(를) 찾을 수 없습니다");
            }

            product.Delete("system");
            _eventCollector.Track(product);
            return Fin.Succ(unit);                     // 9. Unit 반환
        });
    }

    // ... 나머지 메서드도 동일 패턴
}
```

> **참조**: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/InMemory/InMemoryProductRepository.cs`

**Repository Adapter 핵심 패턴**:

| 패턴 | 코드 | 설명 |
|------|------|------|
| IO 래핑 | `IO.lift(() => { ... })` | 동기 작업은 `IO.lift` 사용 |
| 성공 | `Fin.Succ(value)` | 성공 값 래핑 |
| 도메인 실패 | `AdapterError.For<T>(errorType, context, message)` | 비즈니스 실패 (not found 등) |
| Unit 반환 | `Fin.Succ(unit)` | 반환 값 없는 작업 (`using static LanguageExt.Prelude`) |
| Optional | `Fin.Succ(Optional(value))` | `Option<T>` 래핑 |
| 컬렉션 | `Fin.Succ(toSeq(values))` | `Seq<T>` 래핑 |

#### EF Core Repository

InMemory(ConcurrentDictionary) 대신 EF Core를 사용하는 Repository Adapter 패턴입니다. 동일한 Port 인터페이스를 구현하되, `IO.liftAsync`를 사용하여 EF Core의 비동기 API를 래핑합니다.

##### DbContext 정의

DbContext는 **Persistence Model(POCO)** 을 DbSet 타입으로 사용합니다. 도메인 엔티티가 아닌 Model을 직접 참조합니다.

```csharp
// 파일: {Adapters.Persistence}/Repositories/EfCore/{ServiceName}DbContext.cs

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

> **참조**: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/EfCore/LayeredArchDbContext.cs`

**핵심 포인트:**
- DbSet 타입은 **Persistence Model** (`ProductModel`, `OrderModel`, ...) — 도메인 엔티티(`Product`, `Order`, ...)가 아님
- `ApplyConfigurationsFromAssembly`로 동일 어셈블리의 `IEntityTypeConfiguration<T>` 구현체를 자동 검색
- DbSet 프로퍼티는 `=> Set<T>()` 표현식으로 정의

##### Entity Configuration — Persistence Model 직접 매핑

Persistence Model은 primitive 타입만 사용하므로, EF Core `HasConversion`이 불필요합니다. Configuration은 `IEntityTypeConfiguration<XxxModel>`을 구현합니다.

| Model 프로퍼티 타입 | EF Core 설정 | 비고 |
|---|---|---|
| `string` (EntityId) | `HasMaxLength(26)` | Ulid 문자열 (26자) |
| `string` (이름 등) | `HasMaxLength(N).IsRequired()` | — |
| `decimal` (금액) | `HasPrecision(18, 4)` | — |
| `int` (수량) | — | 기본 매핑 |
| `DateTime?` (삭제일시) | — | Soft Delete 지원 |
| `string?` (삭제자) | `HasMaxLength(320)` | — |
| `List<ProductTagModel>` (컬렉션) | `HasMany().WithOne().HasForeignKey().OnDelete(Cascade)` | — |

**Entity Configuration 예시:**

```csharp
// 파일: {Adapters.Persistence}/Repositories/EfCore/Configurations/ProductConfiguration.cs

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

        // Global Query Filter: 삭제된 상품 자동 제외
        builder.HasQueryFilter(p => p.DeletedAt == null);

        builder.HasMany(p => p.ProductTags)
            .WithOne()
            .HasForeignKey(pt => pt.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

> **참조**: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/EfCore/Configurations/ProductConfiguration.cs`

**이전 패턴과의 차이:** 도메인 엔티티를 직접 매핑하던 이전 방식에서는 Value Object마다 `HasConversion` + `IdConverter`/`IdComparer`가 필요했습니다. Persistence Model(POCO)을 사용하면 primitive 타입이므로 변환이 불필요합니다.

##### EF Core Repository 구현

기존 InMemory Repository와 동일한 Port를 구현하되, `IO.liftAsync`로 EF Core 비동기 API를 래핑합니다. DbContext는 **Persistence Model** 을 다루므로, Mapper 확장 메서드(`ToModel()` / `ToDomain()`)로 도메인 엔티티와 변환합니다.

```csharp
// 파일: {Adapters.Persistence}/Repositories/EfCore/EfCoreProductRepository.cs

using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using LayeredArch.Adapters.Persistence.Repositories.EfCore.Mappers;
using LayeredArch.Adapters.Persistence.Repositories.EfCore.Models;
using static Functorium.Adapters.Errors.AdapterErrorType;

[GenerateObservablePort]
public class EfCoreProductRepository : IProductRepository
{
    private readonly LayeredArchDbContext _dbContext;
    private readonly IDomainEventCollector _eventCollector;

    public string RequestCategory => "Repository";

    public EfCoreProductRepository(LayeredArchDbContext dbContext, IDomainEventCollector eventCollector)
    {
        _dbContext = dbContext;
        _eventCollector = eventCollector;
    }

    public virtual FinT<IO, Product> Create(Product product)
    {
        return IO.liftAsync(async () =>
        {
            _dbContext.Products.Add(product.ToModel());  // 도메인 → Model
            _eventCollector.Track(product);
            return Fin.Succ(product);
        });
    }

    public virtual FinT<IO, Product> GetById(ProductId id)
    {
        return IO.liftAsync(async () =>
        {
            var model = await _dbContext.Products
                .AsNoTracking()
                .Include(p => p.ProductTags)
                .FirstOrDefaultAsync(p => p.Id == id.ToString());  // string 비교

            if (model is not null)
            {
                return Fin.Succ(model.ToDomain());  // Model → 도메인
            }

            return AdapterError.For<EfCoreProductRepository>(
                new NotFound(),
                id.ToString(),
                $"상품 ID '{id}'을(를) 찾을 수 없습니다");
        });
    }

    public virtual FinT<IO, Unit> Delete(ProductId id)
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
                    $"상품 ID '{id}'을(를) 찾을 수 없습니다");
            }

            var product = model.ToDomain();
            product.Delete("system");
            _dbContext.Products.Update(product.ToModel());
            _eventCollector.Track(product);
            return Fin.Succ(unit);
        });
    }

    // ... 나머지 메서드도 동일 패턴
}
```

> **참조**: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/EfCore/EfCoreProductRepository.cs`

**InMemory vs EF Core Repository 비교:**

| 항목 | InMemory | EF Core |
|---|---|---|
| IO 래핑 | `IO.lift(() => { ... })` | `IO.liftAsync(async () => { ... })` |
| 저장소 | `ConcurrentDictionary<TId, T>` | `DbContext.Set<TModel>()` |
| 저장/조회 변환 | 불필요 (도메인 객체 직접 저장) | `product.ToModel()` / `model.ToDomain()` |
| 조회 | `Products.TryGetValue(id, ...)` | `_dbContext.Products.FirstOrDefaultAsync(...)` |
| Navigation 로딩 | 불필요 (메모리 내 참조) | `.Include(p => p.ProductTags)` |
| 삭제 방식 | Soft Delete (`product.Delete(...)`) | Soft Delete (`product.Delete(...)` + `Update`) |
| 트랜잭션 관리 | No-op (`InMemoryUnitOfWork`) | `DbContext.SaveChangesAsync()` (`EfCoreUnitOfWork`) |
| 에러 패턴 | `AdapterError.For<T>(...)` | `AdapterError.For<T>(...)` (동일) |
| Pipeline 생성 | `[GenerateObservablePort]` | `[GenerateObservablePort]` (동일) |
| DI 등록 | `RegisterScopedObservablePort<>` | `RegisterScopedObservablePort<>` (동일) |

#### Unit of Work

Unit of Work(UoW)는 Usecase에서 트랜잭션을 커밋하는 Port입니다. Repository는 엔티티 변경만 추적하고, 실제 커밋은 UoW가 담당합니다.

##### IUnitOfWork 인터페이스

**위치**: `Functorium.Applications.Persistence`

```csharp
public interface IUnitOfWork : IObservablePort
{
    FinT<IO, Unit> SaveChanges(CancellationToken cancellationToken = default);
}
```

##### EfCoreUnitOfWork 구현

`DbContext.SaveChangesAsync()`를 호출하여 변경사항을 커밋합니다. `DbUpdateException` 계열의 예외를 `AdapterError`로 변환합니다.

```csharp
// 파일: {Adapters.Persistence}/Repositories/EfCore/EfCoreUnitOfWork.cs

[GenerateObservablePort]
public class EfCoreUnitOfWork : IUnitOfWork
{
    private readonly LayeredArchDbContext _dbContext;

    public string RequestCategory => "UnitOfWork";

    #region Error Types
    public sealed record ConcurrencyConflict : AdapterErrorType.Custom;
    public sealed record DatabaseUpdateFailed : AdapterErrorType.Custom;
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

> **참조**: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/EfCore/EfCoreUnitOfWork.cs`

##### InMemoryUnitOfWork 구현

`ConcurrentDictionary` 기반 InMemory 저장소는 즉시 반영되므로 SaveChanges가 no-op입니다.

```csharp
// 파일: {Adapters.Persistence}/Repositories/InMemory/InMemoryUnitOfWork.cs

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

> **참조**: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/InMemory/InMemoryUnitOfWork.cs`

##### IDomainEventCollector — Repository와 Publisher의 브릿지

`IDomainEventCollector`는 Repository에서 추적된 Aggregate를 `DomainEventPublisher`에 전달하는 브릿지 역할을 합니다.

**위치**: `Functorium.Applications.Events`

```csharp
public interface IDomainEventCollector
{
    void Track(IHasDomainEvents aggregate);
    IReadOnlyList<IHasDomainEvents> GetTrackedAggregates();
}
```

**Repository에서의 사용**: Repository의 `Create()`, `Update()` 메서드에서 `_eventCollector.Track(aggregate)`를 호출하여 Aggregate를 추적 대상으로 등록해야 합니다:

```csharp
public FinT<IO, Product> Create(Product product)
{
    _eventCollector.Track(product);  // 필수: 도메인 이벤트 수집 대상 등록
    // ... 저장 로직 ...
}
```

**등록**: `RegisterDomainEventPublisher()` 호출 시 `IDomainEventCollector`가 Scoped 서비스로 자동 등록됩니다:

```csharp
services.RegisterDomainEventPublisher();  // IDomainEventPublisher + IDomainEventCollector 등록
```

##### Repository에서 SaveChanges를 호출하지 않는 이유

Repository의 `Create()`, `Update()`, `Delete()` 메서드는 EF Core 변경 추적(Change Tracking)에 엔티티를 등록만 합니다. 실제 `SaveChangesAsync()` 호출은 `UsecaseTransactionPipeline`이 Handler 실행 후 자동으로 수행합니다.

이 분리를 통해:
- 여러 Repository 변경을 하나의 트랜잭션으로 묶을 수 있음 (파이프라인 보장)
- 이벤트 발행을 트랜잭션 성공 후로 보장할 수 있음 (파이프라인 보장)
- Repository가 순수한 데이터 접근 계층으로 유지됨
- Repository는 `IDomainEventCollector.Track(aggregate)`를 호출하여 도메인 이벤트 수집 대상을 등록

> **참조**: 파이프라인 패턴은 [11-usecases-and-cqrs.md §트랜잭션과 이벤트 발행](./11-usecases-and-cqrs.md#트랜잭션과-이벤트-발행-usecasetransactionpipeline)을 참조하세요.

### External API Adapter

External API Adapter는 HTTP 클라이언트를 통한 외부 시스템 호출을 구현합니다.

```csharp
// 파일: {Adapters.Infrastructure}/ExternalApis/ExternalPricingApiService.cs

using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using static Functorium.Adapters.Errors.AdapterErrorType;

[GenerateObservablePort]
public class ExternalPricingApiService : IExternalPricingService
{
    private readonly HttpClient _httpClient;              // 1. HttpClient 주입

    public string RequestCategory => "ExternalApi";       // 2. 요청 카테고리

    #region Error Types
    public sealed record OperationCancelled : AdapterErrorType.Custom;
    public sealed record UnexpectedException : AdapterErrorType.Custom;
    public sealed record RateLimited : AdapterErrorType.Custom;
    public sealed record HttpError : AdapterErrorType.Custom;
    #endregion

    public ExternalPricingApiService(HttpClient httpClient)  // 3. 생성자 주입
    {
        _httpClient = httpClient;
    }

    public virtual FinT<IO, Money> GetPriceAsync(
        string productCode, CancellationToken cancellationToken)
    {
        return IO.liftAsync(async () =>                   // 4. IO.liftAsync (비동기)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"/api/pricing/{productCode}",
                    cancellationToken);

                // 5. HTTP 오류 처리
                if (!response.IsSuccessStatusCode)
                {
                    return HandleHttpError<Money>(response, productCode);
                }

                // 6. 응답 역직렬화
                var priceResponse = await response.Content
                    .ReadFromJsonAsync<ExternalPriceResponse>(
                        cancellationToken: cancellationToken);

                // 7. null 응답 처리
                if (priceResponse is null)
                {
                    return AdapterError.For<ExternalPricingApiService>(
                        new Null(),
                        productCode,
                        $"외부 API 응답이 null입니다. ProductCode: {productCode}");
                }

                return Money.Create(priceResponse.Price);
            }
            catch (HttpRequestException ex)               // 8. 연결 실패
            {
                return AdapterError.FromException<ExternalPricingApiService>(
                    new ConnectionFailed("ExternalPricingApi"),
                    ex);
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                return AdapterError.For<ExternalPricingApiService>(  // 9. 사용자 취소
                    new OperationCancelled(),
                    productCode,
                    "요청이 취소되었습니다");
            }
            catch (TaskCanceledException ex)              // 10. 타임아웃
            {
                return AdapterError.FromException<ExternalPricingApiService>(
                    new AdapterErrorType.Timeout(TimeSpan.FromSeconds(30)),
                    ex);
            }
            catch (Exception ex)                          // 11. 기타 예외
            {
                return AdapterError.FromException<ExternalPricingApiService>(
                    new UnexpectedException(),
                    ex);
            }
        });
    }

    // HTTP 상태 코드별 에러 매핑
    private static Fin<T> HandleHttpError<T>(
        HttpResponseMessage response, string context) =>
        response.StatusCode switch
        {
            HttpStatusCode.NotFound => AdapterError.For<ExternalPricingApiService>(
                new NotFound(), context, "리소스를 찾을 수 없습니다"),

            HttpStatusCode.Unauthorized => AdapterError.For<ExternalPricingApiService>(
                new Unauthorized(), context, "인증에 실패했습니다"),

            HttpStatusCode.Forbidden => AdapterError.For<ExternalPricingApiService>(
                new Forbidden(), context, "접근이 금지되었습니다"),

            HttpStatusCode.TooManyRequests => AdapterError.For<ExternalPricingApiService>(
                new RateLimited(), context, "요청 제한에 도달했습니다"),

            HttpStatusCode.ServiceUnavailable => AdapterError.For<ExternalPricingApiService>(
                new ExternalServiceUnavailable("ExternalPricingApi"),
                context, "서비스를 사용할 수 없습니다"),

            _ => AdapterError.For<ExternalPricingApiService, HttpStatusCode>(
                new HttpError(), response.StatusCode,
                $"API 호출 실패. Status: {response.StatusCode}")
        };
}
```

> **참조**: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Infrastructure/ExternalApis/ExternalPricingApiService.cs`

**HTTP 상태 코드 → AdapterErrorType 매핑 참조**:

| HTTP 상태 코드 | AdapterErrorType | 설명 |
|---------------|------------------|------|
| 404 | `new NotFound()` | 리소스 없음 |
| 401 | `new Unauthorized()` | 인증 실패 |
| 403 | `new Forbidden()` | 접근 거부 |
| 429 | `new RateLimited()` | 요청 제한 초과 |
| 503 | `new ExternalServiceUnavailable(name)` | 서비스 불가 |
| 기타 | `new HttpError()` | 일반 HTTP 에러 |

**예외 → AdapterErrorType 매핑 참조**:

| 예외 타입 | AdapterErrorType | 설명 |
|----------|------------------|------|
| `HttpRequestException` | `new ConnectionFailed(name)` | 연결 실패 |
| `TaskCanceledException` (사용자) | `new OperationCancelled()` | 요청 취소 |
| `TaskCanceledException` (타임아웃) | `new Timeout(timespan)` | 응답 시간 초과 |
| `Exception` | `new UnexpectedException()` | 예상 외 예외 |

### Messaging Adapter

Messaging Adapter는 메시지 브로커를 통한 서비스 간 통신을 구현합니다.

```csharp
// 파일: {Adapters}/Messaging/RabbitMqInventoryMessaging.cs

using Functorium.Adapters.SourceGenerators;
using static LanguageExt.Prelude;
using Wolverine;

[GenerateObservablePort]
public class RabbitMqInventoryMessaging : IInventoryMessaging
{
    private readonly IMessageBus _messageBus;              // 1. MessageBus 주입

    public string RequestCategory => "Messaging";          // 2. 요청 카테고리

    public RabbitMqInventoryMessaging(IMessageBus messageBus)  // 3. 생성자 주입
    {
        _messageBus = messageBus;
    }

    // Request/Reply 패턴
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
                    Error.New(ex.Message));                 // 6. 에러 래핑
            }
        });
    }

    // Fire-and-Forget 패턴
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

> **참조**: `Tutorials/Cqrs06Services/Src/OrderService/Adapters/Messaging/RabbitMqInventoryMessaging.cs`

**Messaging Adapter 핵심 패턴**:

| 패턴 | API | 설명 |
|------|-----|------|
| Request/Reply | `_messageBus.InvokeAsync<TResponse>(request)` | 응답을 기다리는 동기적 메시징 |
| Fire-and-Forget | `_messageBus.SendAsync(command)` | 응답 없이 메시지 전송 |
| 에러 래핑 | `Fin.Fail<T>(Error.New(ex.Message))` | 메시징 예외를 `Fin.Fail`로 변환 |

##### Messaging ACL: 메시지 스키마 변환이 필요한 경우

현재 예시는 공유 DTO를 직접 전달하며, 공동 설계된 계약일 때 유효합니다.
외부/레거시 메시지 스키마와 통합 시에는 ACL을 적용합니다:

```
수신: Broker Message → internal XxxMessage → Mapper → Domain Type (Port)
발신: Domain Type (Port) → Mapper → internal XxxMessage → Broker Message
```

- 동일 패턴: `internal record` + `internal static class XxxMessageMapper`
- 판단 기준은 [외부 시스템 유형별 ACL 체크리스트](#외부-시스템-유형별-acl-체크리스트) 참조

### Query Adapter (CQRS Read 측)

Query Adapter는 CQRS의 Read 측을 담당하는 Adapter입니다. Aggregate 재구성 없이 DTO를 직접 반환하며, 페이지네이션/정렬을 DB 레벨에서 처리합니다.

#### CQRS 관점의 기술 선택

| 관점 | Command 측 (Repository) | Query 측 (Query Adapter) |
|------|------------------------|------------------------|
| **ORM** | EF Core | **Dapper + 명시적 SQL** |
| **이유** | 변경 추적, UnitOfWork, 마이그레이션 | 성능 극대화, SQL 튜닝 용이성 |
| **Aggregate 재구성** | O — 도메인 불변식 검증 필요 | X — DTO 직접 반환 |
| **데이터 변경** | O — Create/Update/Delete | X — 읽기 전용 |
| **페이지네이션/정렬** | X — 전체 조회 후 가공 | O — DB 레벨 처리 |
| **인터페이스 위치** | Domain 레이어 | Application 레이어 |

**판단 기준**: 조회 결과로 **Aggregate를 재구성할 필요가 있는가?**
- 있다 → Repository (Command 측, EF Core)
- 없다 (DTO 직접 반환) → Query Adapter (Query 측, Dapper)

#### 왜 Query 측에 Dapper인가?

CQRS 원칙에 따라 Command/Query의 기술 스택을 독립적으로 최적화합니다:

- **성능**: Dapper는 EF Core 대비 오버헤드가 적음 (변경 추적, 프록시 생성 없음)
- **SQL 튜닝**: 명시적 SQL로 쿼리 플랜 최적화 가능 (JOIN, INDEX HINT 등)
- **유지보수**: 쿼리별 SQL이 명확하여 성능 병목 추적이 용이
- **기술 독립**: Command 측 ORM 변경이 Query 측에 영향 없음

#### 페이지네이션/정렬 프레임워크 타입

`Functorium.Applications.Queries` 네임스페이스에 위치한 Application 레벨 쿼리 관심사 타입입니다.

#### PageRequest — Offset 기반 페이지네이션

```csharp
var page = new PageRequest(page: 2, pageSize: 10);
// page.Skip == 10, page.Page == 2, page.PageSize == 10
// 기본값: page=1, pageSize=20, 최대: 100
```

- `Page < 1` → 1로 클램핑
- `PageSize < 1` → DefaultPageSize(20)로 클램핑
- `PageSize > MaxPageSize(100)` → MaxPageSize로 클램핑

#### PagedResult — 페이지네이션 결과

```csharp
var result = new PagedResult<ProductSummaryDto>(items, totalCount: 50, page: 2, pageSize: 10);
// result.TotalPages == 5, result.HasPreviousPage == true, result.HasNextPage == true
```

#### SortExpression — 다중 필드 정렬

```csharp
// 단일 필드
var sort = SortExpression.By("Name");

// 다중 필드
var sort = SortExpression.By("Price", SortDirection.Descending).ThenBy("Name");

// 정렬 없음
var sort = SortExpression.Empty;
```

---

#### DapperQueryAdapterBase — 프레임워크 베이스 클래스

`Functorium.Adapters.Repositories` 네임스페이스에 위치한 프레임워크 제공 베이스 클래스입니다.
서브클래스는 **SQL 선언과 WHERE 빌드만** 담당하고, 인프라(Search 실행, ORDER BY, 페이지네이션, 파라미터 헬퍼)는 베이스가 처리합니다.

```
베이스 클래스 (인프라)                    서브클래스 (SQL 선언)
┌────────────────────────────────┐      ┌──────────────────────────────────┐
│ DapperQueryAdapterBase<T,TDto> │      │ DapperProductQueryAdapter        │
│                                │      │   : DapperQueryAdapterBase<...>  │
│ • Search() — 실행 엔진         │ ◄─── │   , IProductQuery                │
│ • BuildOrderByClause()        │      │                                  │
│ • Params() 헬퍼               │      │ • SelectSql, CountSql            │
│ • IDbConnection 보유           │      │ • DefaultOrderBy                 │
└────────────────────────────────┘      │ • AllowedSortColumns             │
                                        │ • BuildWhereClause()             │
                                        └──────────────────────────────────┘
```

**서브클래스가 선언하는 것 (abstract):**

| 멤버 | 역할 | 예시 |
|------|------|------|
| `SelectSql` | 전체 SELECT문 (WHERE/ORDER BY 제외) | `"SELECT Id AS ProductId, Name, Price FROM Products"` |
| `CountSql` | 전체 COUNT문 (WHERE 제외) | `"SELECT COUNT(*) FROM Products"` |
| `DefaultOrderBy` | 정렬 미지정 시 기본값 | `"Name ASC"` |
| `AllowedSortColumns` | 허용 정렬 필드 Allowlist | `{ ["Name"] = "Name", ["Price"] = "Price" }` |
| `BuildWhereClause()` | Spec → SQL WHERE + Parameters | `ProductPriceRangeSpec → "WHERE Price >= @Min ..."` |

> **참조**: `Src/Functorium/Adapters/Repositories/DapperQueryAdapterBase.cs`

#### Dapper Query Adapter 구현 — 단일 테이블

핵심: SQL 선언부만 작성하면 Search/ORDER BY/페이지네이션은 베이스가 처리합니다.

```csharp
[GenerateObservablePort]
public class DapperProductQueryAdapter
    : DapperQueryAdapterBase<Product, ProductSummaryDto>, IProductQuery
{
    public string RequestCategory => "QueryAdapter";

    protected override string SelectSql => "SELECT Id AS ProductId, Name, Price FROM Products";
    protected override string CountSql => "SELECT COUNT(*) FROM Products";
    protected override string DefaultOrderBy => "Name ASC";
    protected override Dictionary<string, string> AllowedSortColumns { get; } =
        new(StringComparer.OrdinalIgnoreCase) { ["Name"] = "Name", ["Price"] = "Price" };

    public DapperProductQueryAdapter(IDbConnection connection) : base(connection) { }

    protected override (string, DynamicParameters) BuildWhereClause(Specification<Product> spec)
        => spec switch
        {
            { IsAll: true } => ("WHERE DeletedAt IS NULL", new DynamicParameters()),
            ProductPriceRangeSpec s => (
                "WHERE DeletedAt IS NULL AND Price >= @MinPrice AND Price <= @MaxPrice",
                Params(("MinPrice", (decimal)s.MinPrice), ("MaxPrice", (decimal)s.MaxPrice))),
            _ => throw new NotSupportedException(
                $"Specification '{spec.GetType().Name}'은 Dapper QueryAdapter에서 지원되지 않습니다.")
        };
}
```

> **참조**: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/Dapper/DapperProductQueryAdapter.cs`

#### Dapper Query Adapter 구현 — JOIN

`SelectSql`/`CountSql`을 통째로 선언하므로 JOIN, GROUP BY 등 복잡한 쿼리도 자유롭게 작성할 수 있습니다.

```csharp
[GenerateObservablePort]
public class DapperProductWithStockQueryAdapter
    : DapperQueryAdapterBase<Product, ProductWithStockDto>, IProductWithStockQuery
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

    public DapperProductWithStockQueryAdapter(IDbConnection connection) : base(connection) { }

    protected override (string, DynamicParameters) BuildWhereClause(Specification<Product> spec)
        => spec switch
        {
            { IsAll: true } => ("WHERE p.DeletedAt IS NULL", new DynamicParameters()),
            ProductPriceRangeSpec s => (
                "WHERE p.DeletedAt IS NULL AND p.Price >= @MinPrice AND p.Price <= @MaxPrice",
                Params(("MinPrice", (decimal)s.MinPrice), ("MaxPrice", (decimal)s.MaxPrice))),
            _ => throw new NotSupportedException(...)
        };
}
```

> **참조**: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/Dapper/DapperProductWithStockQueryAdapter.cs`

#### Specification → SQL WHERE 변환

Dapper Query Adapter는 Specification을 패턴 매칭으로 SQL WHERE 절로 변환합니다. 모든 값은 `@Parameter`로 바인딩합니다.

```csharp
protected override (string, DynamicParameters) BuildWhereClause(Specification<Product> spec)
    => spec switch
    {
        { IsAll: true } => ("WHERE DeletedAt IS NULL", new DynamicParameters()),
        ProductPriceRangeSpec s => (
            "WHERE DeletedAt IS NULL AND Price >= @MinPrice AND Price <= @MaxPrice",
            Params(("MinPrice", (decimal)s.MinPrice), ("MaxPrice", (decimal)s.MaxPrice))),
        _ => throw new NotSupportedException(...)
    };
```

#### Dapper SQL 작성 체크리스트

- [ ] 모든 WHERE 조건의 값은 `@Parameter`로 바인딩했는가? (문자열 결합 금지)
- [ ] `SelectSql`/`CountSql`에 `WHERE`/`ORDER BY`를 포함하지 않았는가? (베이스 클래스가 처리)
- [ ] 컬럼 alias는 DTO 프로퍼티명과 일치하는가? (예: `Id AS ProductId`)
- [ ] JOIN 시 테이블 alias를 사용했는가? (예: `p.Name`, `i.StockQuantity`)
- [ ] `AllowedSortColumns`에 정렬 가능한 필드를 모두 등록했는가?
- [ ] `DefaultOrderBy`에 유효한 기본 정렬을 지정했는가?
- [ ] 미지원 Specification에 대해 `NotSupportedException`을 던지는가?

#### SQL 인젝션 방지 (3중 보호)

| 계층 | 보호 방식 | 위치 |
|------|----------|------|
| Application Validator | `AllowedSortFields` 검증 | FluentValidation (Request 검증) |
| Adapter Allowlist | `AllowedSortColumns` Dictionary lookup → 미등록 필드는 기본 정렬로 폴백 | Query Adapter |
| Dapper Parameters | 모든 값은 `@Parameter`로 바인딩, 문자열 결합 없음 | SQL 실행 |

#### InMemory Query Adapter 구현

InMemory 구현은 기존 Repository를 위임하여 데이터를 가져온 후 인메모리 정렬/페이지네이션합니다.

```csharp
[GenerateObservablePort]
public class InMemoryProductQueryAdapter : IProductQuery
{
    public string RequestCategory => "QueryAdapter";

    public virtual FinT<IO, PagedResult<ProductSummaryDto>> Search(
        Specification<Product> spec, PageRequest page, SortExpression sort)
    {
        return IO.lift(() =>
        {
            // InMemoryProductRepository.Products 정적 필드에서 데이터 조회
            // → 인메모리 정렬/페이지네이션/DTO 변환
        });
    }
}
```

- InMemory는 테스트용이므로 Aggregate 재구성 비용이 무시 가능
- `InMemoryProductRepository.Products` 정적 필드를 직접 참조하여 데이터 조회

---

## 트러블슈팅

### virtual 키워드 누락으로 CS0506 빌드 에러 발생

**원인:** Pipeline 클래스가 원본 Adapter 클래스를 상속받아 메서드를 `override`합니다. `virtual`이 없으면 override가 불가능하여 `CS0506: cannot override because it is not virtual` 에러가 발생합니다.

**해결:**
```csharp
// Before - 빌드 에러
public FinT<IO, Product> GetById(ProductId id) { ... }

// After - Pipeline override 가능
public virtual FinT<IO, Product> GetById(ProductId id) { ... }
```

### IO.lift 내부에서 await 사용 시 컴파일 에러

**원인:** `IO.lift`는 동기 람다만 허용합니다. 내부에서 `await`를 사용하려면 `IO.liftAsync`를 사용해야 합니다.

**해결:**
```csharp
// Before - 컴파일 에러
return IO.lift(() => { var result = await _httpClient.GetAsync(url); ... });

// After - 비동기 작업은 IO.liftAsync 사용
return IO.liftAsync(async () => { var result = await _httpClient.GetAsync(url); ... });
```

### Mapper 클래스가 public으로 노출되어 도메인 경계가 깨진다

**원인:** Adapter 내부의 Mapper 클래스가 `public`으로 선언되면 외부 프로젝트에서 기술 관심사 변환 로직에 접근할 수 있어 레이어 경계가 무너집니다.

**해결:**
```csharp
// Before - 외부 노출
public static class ProductMapper { ... }

// After - Adapter 프로젝트 내부로 제한
internal static class ProductMapper { ... }
```

---

## FAQ

### Q1. IO.lift와 IO.liftAsync 중 어떤 것을 사용해야 하나요?

내부에서 `await`를 사용해야 하면 `IO.liftAsync`, 그렇지 않으면 `IO.lift`를 사용합니다. In-Memory 저장소나 캐시 조회는 `IO.lift`, HTTP 호출이나 DB 비동기 쿼리는 `IO.liftAsync`를 사용합니다. EF Core 등 비동기 DB 접근 시에는 Repository에서도 `IO.liftAsync`를 사용합니다.

### Q2. Adapter에서 에러를 반환할 때 Exception을 throw하면 안 되나요?

Exception을 throw하면 Pipeline의 에러 처리 흐름을 우회하게 됩니다. 대신 `AdapterError.For<T>(errorType, context, message)`로 `Fin.Fail`을 반환하여 함수형 에러 처리 체인을 유지합니다. 외부 라이브러리에서 발생하는 Exception은 `AdapterError.FromException<T>(errorType, ex)`로 변환합니다.

### Q3. Persistence Model(POCO)과 도메인 Entity를 왜 분리하나요?

도메인 Entity는 비즈니스 불변식을 보호하는 반면, Persistence Model은 DB 스키마에 맞는 단순 POCO입니다. 분리하면 DB 스키마 변경이 도메인 모델에 영향을 주지 않고, 도메인 모델의 진화가 DB 마이그레이션과 독립적으로 이루어집니다.

### Q4. Query Adapter에서 Aggregate를 재구성하지 않는 이유는?

Query Adapter는 CQRS의 Read 측을 담당하며, 읽기 전용 조회에서는 도메인 불변식 검증이 불필요합니다. Aggregate 재구성 비용을 피하고 DTO를 직접 반환하여 조회 성능을 최적화합니다. Dapper 등으로 직접 SQL 쿼리를 실행할 수 있습니다.

### Q5. [GenerateObservablePort] 어트리뷰트를 적용하지 않으면 어떻게 되나요?

Pipeline 클래스가 생성되지 않으므로 로깅, 트레이싱, 메트릭이 자동 적용되지 않습니다. Adapter가 직접 Port 인터페이스로 DI 등록되며, Observability 코드를 수동으로 작성해야 합니다.

---

## 참고 문서

| 문서 | 설명 |
|------|------|
| [12-ports.md](./12-ports.md) | Port 아키텍처, IObservablePort 계층, Port 정의 규칙 |
| [14-adapter-wiring.md](./14-adapter-wiring.md) | Pipeline 생성, DI 등록, Options 패턴, 테스트 |
| [15a-unit-testing.md](./15a-unit-testing.md) | 단위 테스트 작성 가이드 |
| [08a-error-system.md](./08a-error-system.md) | 에러 시스템: 기초와 네이밍 |
| [08b-error-system-layers.md](./08b-error-system-layers.md) | 에러 시스템: 레이어별 구현과 테스트 |
