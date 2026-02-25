# DTO 전략 리뷰: Eric Evans DDD & Hexagonal Architecture 관점

`Docs/guides` 문서에 정의된 DTO 매핑 전략을 `Tests.Hosts/01-SingleHost`의 Src와 Tests에 실제 적용한 결과를 리뷰합니다. 리뷰 기준은 Eric Evans의 DDD 전술적 설계 원칙과 Alistair Cockburn의 Hexagonal Architecture(Ports and Adapters) 원칙입니다.

---

## 1. 전체 평가: 우수 (Excellent)

현재 구현은 DDD와 Hexagonal Architecture의 핵심 원칙을 충실히 따르고 있습니다. 특히 **도메인 순수성 보호**, **레이어 간 명확한 경계**, **Persistence Ignorance** 측면에서 교과서적인 구현입니다.

---

## 2. Eric Evans DDD 관점 리뷰

### 2.1 도메인 모델 순수성 ✅ 우수

**원칙**: 도메인 모델은 인프라스트럭처 관심사로부터 완전히 자유로워야 한다.

**현재 구현**:
- `Product`, `Customer`, `Order` 엔티티에 EF Core 어노테이션(`[Key]`, `[Required]` 등)이 전혀 없음
- 모든 속성이 Value Object 타입 (`ProductName`, `Money`, `Quantity` 등)
- 파라미터 없는 ORM용 생성자가 제거됨
- Domain Event가 Aggregate Root 내부에 중첩 record로 정의됨

```csharp
// Product.cs — 순수 도메인 모델의 모범 사례
public sealed class Product : AggregateRoot<ProductId>, IAuditable
{
    public ProductName Name { get; private set; }        // VO, not string
    public Money Price { get; private set; }             // VO, not decimal
    public Quantity StockQuantity { get; private set; }  // VO, not int
}
```

**Evans 원칙 충족**: "도메인 모델은 기술적 인프라 때문에 타협해서는 안 된다." — 완전 충족.

### 2.2 Aggregate Root 경계 ✅ 우수

**원칙**: Aggregate는 트랜잭션의 일관성 경계이며, 외부 Aggregate를 ID로만 참조한다.

**현재 구현**:
- `Order` → `ProductId` (ID 참조만 사용, Product 엔티티 직접 참조 없음)
- `Tag`는 Product Aggregate 내부의 Entity로 적절히 모델링
- `IProductCatalog` Port가 교차 Aggregate 검증을 깔끔하게 처리

```csharp
// Order.cs — 교차 Aggregate 참조는 ID만 사용
public static Order Create(ProductId productId, Quantity quantity, ...)
```

### 2.3 Factory 패턴 (Create / CreateFromValidated) ✅ 우수

**원칙**: 복잡한 객체 생성은 Factory가 담당하되, 도메인 불변식을 반드시 보장해야 한다.

**현재 구현**:

| 팩토리 메서드 | 용도 | 검증 | 이벤트 |
|-------------|------|------|--------|
| `Create()` | 비즈니스 로직에서 새 엔티티 생성 | VO가 사전 검증됨 | 발행 |
| `CreateFromValidated()` | ORM 복원 | 스킵 | 미발행 |

**핵심 포인트**: `CreateFromValidated()`는 "이미 한번 검증된 데이터의 재구성"이라는 DDD의 Repository 복원 패턴을 정확히 구현합니다. Evans가 말한 "Repository는 이미 존재하는 객체를 재구성하는 것이지, 새로 만드는 것이 아니다"에 부합합니다.

### 2.4 Repository 패턴 ✅ 우수

**원칙**: Repository는 Aggregate 단위로 동작하며, 도메인에게 "컬렉션 환상(illusion of collection)"을 제공한다.

**현재 구현**:
- `IProductRepository`는 `IRepository<Product, ProductId>`를 상속
- `Product` Aggregate Root 단위로 CRUD 제공
- 반환값이 도메인 엔티티 (`Product`, `Seq<Product>`)
- Port 인터페이스에 인프라 용어 없음 (SQL, DbContext 등)

```csharp
// Domain Layer — 인프라에 대한 어떤 가정도 없음
public interface IProductRepository : IRepository<Product, ProductId>
{
    FinT<IO, Seq<Product>> GetAll();
    FinT<IO, bool> Exists(Specification<Product> spec);
    FinT<IO, Seq<Product>> FindAll(Specification<Product> spec);
}
```

### 2.5 Specification 패턴 ✅ 우수 (인지된 trade-off 1개)

**원칙**: 비즈니스 규칙을 캡슐화한 Specification은 도메인에 속하며, 조합 가능해야 한다.

**현재 구현**:
- 도메인 순수성 유지: `bool IsSatisfiedBy(T)` (Expression 아닌 메서드 기반)
- Adapter에서 SQL 최적화: `switch` pattern matching으로 번역

**인지된 trade-off**: `EfCoreProductRepository.FindAll()`의 fallback 경로가 전체 테이블을 메모리로 로드합니다.

```csharp
// EfCoreProductRepository.cs — 알려지지 않은 Spec은 인메모리 폴백
_ => _dbContext.Products   // 전체 로드 후 인메모리 필터링
```

`ExistsBySpecInMemory()`도 마찬가지입니다. 새로운 Specification 추가 시 반드시 `switch`에 케이스를 추가해야 하는데, 이를 놓치면 성능 문제가 발생합니다. 현재는 의도적 설계 결정이고 가이드 문서에도 명시되어 있으므로 **인지하고 있으면 충분**합니다.

### 2.6 Domain Event와 Persistence 복원 ✅ 우수

**원칙**: Repository에서 엔티티를 복원할 때 도메인 이벤트가 발생하면 안 된다.

```csharp
// ProductMapper.cs — 복원 시 부산물 이벤트 제거
product.ClearDomainEvents();
```

`AddTag()` 호출이 `TagAssignedEvent`를 발행하지만, 이는 복원 과정의 부산물이므로 `ClearDomainEvents()`로 제거합니다. Evans의 "Repository 복원은 새 생성이 아니다"를 정확히 반영합니다.

---

## 3. Hexagonal Architecture 관점 리뷰

### 3.1 레이어별 DTO 소유권 ✅ 우수

**원칙**: 각 레이어(Port/Adapter)는 자신의 데이터 표현을 소유하며, 다른 레이어의 DTO에 의존하지 않는다.

**현재 데이터 흐름**:

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

각 경계에서 명시적 변환이 일어나고, 변환 책임이 명확합니다:

| 경계 | 변환 | 책임자 |
|------|------|--------|
| HTTP → Application | `Endpoint.Request` → `Usecase.Request` | Endpoint |
| Application → HTTP | `Usecase.Response` → `Endpoint.Response` | Endpoint (`result.Map()`) |
| Domain → DB | `Product` → `ProductModel` | Mapper (`ToModel()`) |
| DB → Domain | `ProductModel` → `Product` | Mapper (`ToDomain()`) |

### 3.2 Persistence Model 분리 (Anti-Corruption Layer) ✅ 우수

**원칙**: 외부 시스템(DB)의 표현이 도메인 모델에 영향을 주어서는 안 된다.

**이전**: Domain Entity에 직접 `HasConversion()` 적용 → 도메인이 ORM에 결합
**현재**: `ProductModel` POCO 도입 → 도메인은 EF Core의 존재를 모름

```csharp
// Models/ProductModel.cs — 순수 POCO, 도메인 의존성 없음
public class ProductModel
{
    public string Id { get; set; } = default!;      // Ulid → string
    public string Name { get; set; } = default!;    // ProductName → string
    public decimal Price { get; set; }               // Money → decimal
}

// Configurations/ProductConfiguration.cs — ORM 관심사는 여기서만
builder.Property(p => p.Id).HasMaxLength(26);
builder.Property(p => p.Price).HasPrecision(18, 4);
```

이는 Cockburn이 말한 "Driven Adapter는 Port를 구현하되, 외부 기술의 세부사항을 내부로 노출하지 않는다"를 정확히 따릅니다.

### 3.3 Mapper 설계 ✅ 우수

```csharp
// internal static class — 어댑터 내부 구현 세부사항
internal static class ProductMapper
{
    public static ProductModel ToModel(this Product product) => ...
    public static Product ToDomain(this ProductModel model) => ...
}
```

- `internal` 접근 제한자: Mapper는 Persistence Adapter의 구현 세부사항
- Extension method: 자연스러운 호출 문법 (`product.ToModel()`)
- `ToDomain()`에서 `CreateFromValidated()` 사용: 검증 스킵으로 성능 확보
- 양방향 변환이 하나의 파일에 응집

### 3.4 Driving Adapter (Presentation) ✅ 우수

**원칙**: Driving Adapter는 외부 요청을 Application Layer의 언어로 번역한다.

```csharp
// CreateProductEndpoint.cs — Driving Adapter의 모범 사례
public override async Task HandleAsync(Request req, CancellationToken ct)
{
    // 1. Endpoint DTO → Usecase DTO 변환
    var usecaseRequest = new CreateProductCommand.Request(
        req.Name, req.Description, req.Price, req.StockQuantity);

    // 2. Application Layer 호출
    var result = await _mediator.Send(usecaseRequest, ct);

    // 3. Usecase Response → Endpoint Response 변환
    var mapped = result.Map(r => new Response(
        r.ProductId, r.Name, r.Description, r.Price, r.StockQuantity, r.CreatedAt));

    // 4. HTTP 응답 전송
    await this.SendCreatedFinResponseAsync(mapped, ct);
}
```

3단계 변환 흐름이 명확하고, 각 단계의 역할이 분명합니다.

### 3.5 Port 인터페이스의 도메인 타입 사용 ✅ 우수

**원칙**: Port는 Application/Domain의 언어로 정의된다 (외부 기술 용어가 아님).

```csharp
// IProductRepository — ProductId, ProductName, Product 등 도메인 타입만 사용
FinT<IO, Product> GetById(ProductId id);           // ✅ not Guid/string
FinT<IO, Option<Product>> GetByName(ProductName name);  // ✅ not string
FinT<IO, bool> Exists(Specification<Product> spec);     // ✅ 도메인 Spec
```

---

## 4. 논의 사항 (Discussion Points)

### 4.1 ProductSummaryDto — Application DTO 재사용 (Pragmatic 접근)

**현상**: `ProductSummaryDto`는 Application 레이어(`Dtos/` 폴더)에만 존재하며, Presentation(`GetAllProductsEndpoint`)에서 직접 참조합니다. Presentation 전용 DTO는 삭제되었습니다.

```
Application/Usecases/Products/Dtos/ProductSummaryDto.cs   ← 유일한 정의
```

`GetAllProductsEndpoint`에서 Application DTO를 직접 사용하고, `Seq → List` 변환만 수행합니다:

```csharp
// GetAllProductsEndpoint.cs — Application DTO 재사용, Seq → List 변환만 수행
var mapped = result.Map(r => new Response(r.Products.ToList()));

public new sealed record Response(List<ProductSummaryDto> Products);
```

**DDD/Hexagonal 관점 평가**:
- **Pragmatic 판단**: 읽기 전용 Query 응답이고, 필드가 동일하여 identity mapping이 발생하는 상황. Presentation 전용 DTO를 유지하는 것은 boilerplate만 증가시킴.
- **의존성 방향 준수**: Presentation → Application 방향 참조이므로 아키텍처 규칙 위반 아님.
- **Seq → List 변환**: `Seq<T>`(Application) → `List<T>`(Presentation) 변환은 Response wrapper에서 처리.
- **해제 시점**: Presentation 고유 필드가 필요하거나 API 버저닝이 필요해지면 Endpoint 전용 DTO로 전환.

**결론**: 재사용 허용 조건(읽기 전용 Query, 동일 필드, Presentation 고유 필드 불필요, 컬렉션 변환만 필요)을 모두 충족. 상세 기준은 [17-dto-strategy.md](./guides/17-dto-strategy.md)의 "Application DTO 재사용 허용 조건" 참조.

### 4.2 Application 레이어의 Value Object 암시적 변환

`GetAllProductsQuery.Usecase`에서 도메인 엔티티 → Application DTO 변환 시 VO의 암시적 변환에 의존합니다:

```csharp
// GetAllProductsQuery.cs — p.Name은 ProductName → string 암시적 변환
new ProductSummaryDto(p.Id.ToString(), p.Name, p.Price, p.StockQuantity)
```

`ProductName` → `string`, `Money` → `decimal`, `Quantity` → `int`이 SimpleValueObject의 `implicit operator`를 통해 자동 변환됩니다.

**DDD 관점**: 허용 가능. Evans는 VO에서 primitive 추출을 "의도적인 경계 횡단"으로 봅니다. 암시적 변환이 이를 매끄럽게 해주지만, **변환이 발생한다는 사실 자체는 코드에서 읽을 수 있어야** 합니다. 현재 구현은 record 생성자의 타입 시그니처(`string`, `decimal`, `int`)가 이를 명확히 보여주므로 적절합니다.

### 4.3 EfCoreProductCatalog — 교차 Aggregate 쿼리 포트

```csharp
// EfCoreProductCatalog.cs — IProductCatalog Port 구현
public virtual FinT<IO, Money> GetPrice(ProductId productId)
```

`IProductCatalog`은 Order Aggregate가 Product의 가격을 조회하기 위한 **교차 Aggregate 읽기 전용 Port**입니다. 이 Port가 `ProductModel`을 직접 쿼리하여 `Money` VO를 반환하는 것은:

- **DDD 관점 ✅**: Aggregate 경계를 존중하면서 필요한 데이터만 제공 (Full Product를 반환하지 않음)
- **Hexagonal 관점 ✅**: Port 인터페이스가 도메인 타입(`Money`)으로 정의, Adapter가 DB 쿼리 세부사항을 캡슐화

---

## 5. 테스트 전략 리뷰

### 5.1 Mapper 라운드트립 테스트 ✅ 우수

```
Tests.Unit/Persistence/Mappers/
├── ProductMapperTests.cs   — 필드 보존, Tag 보존, 이벤트 제거 검증
├── CustomerMapperTests.cs  — 필드 보존 검증
├── OrderMapperTests.cs     — 필드 보존 검증
└── TagMapperTests.cs       — 필드 + FK 관계 검증
```

`Domain → Model → Domain` 라운드트립 테스트는 Anti-Corruption Layer의 정확성을 보장합니다. 특히 `ProductMapperTests`에서 "라운드트립 후 도메인 이벤트가 비어있어야 한다"를 검증하는 것은 DDD 관점에서 중요한 테스트입니다.

### 5.2 통합 테스트 — Endpoint Response DTO 활용 ✅ 우수

통합 테스트가 Endpoint의 Response DTO 타입으로 역직렬화하여 검증합니다:

```csharp
// CreateProductEndpointTests.cs
response.ProductId.ShouldNotBeNullOrEmpty();
response.Name.ShouldBe("Test Product");
```

이는 "외부에서 본 시스템의 동작"을 검증하는 것으로, Hexagonal Architecture의 테스트 전략과 일치합니다.

---

## 6. 종합 요약

| 평가 항목 | DDD 관점 | Hexagonal 관점 | 등급 |
|----------|---------|---------------|------|
| 도메인 모델 순수성 | ORM 어노테이션 없음, VO 기반 | 인프라 의존성 제로 | ✅ 우수 |
| Persistence Model 분리 | CreateFromValidated 복원 패턴 | Anti-Corruption Layer | ✅ 우수 |
| 레이어별 DTO 소유권 | 각 레이어 독립 진화 가능 | Port/Adapter 경계 준수 | ✅ 우수 |
| Mapper 설계 | 양방향 변환, 이벤트 제거 | internal 접근, 어댑터 세부사항 | ✅ 우수 |
| Repository 패턴 | Aggregate Root 단위, 도메인 타입 | Port가 도메인 언어로 정의 | ✅ 우수 |
| Factory 패턴 | Create/CreateFromValidated 이원화 | 복원 vs 생성 구분 | ✅ 우수 |
| Specification 패턴 | 도메인 순수, 조합 가능 | SQL 최적화는 Adapter 책임 | ✅ 우수 |
| 가이드 문서 일관성 | 실제 구현과 문서가 일치 | 아키텍처 결정이 문서화됨 | ✅ 우수 |
| 테스트 전략 | 라운드트립 + 이벤트 제거 검증 | 외부 관점 통합 테스트 | ✅ 우수 |

**총평**: 가이드 문서에 정의된 DTO 전략이 실제 코드에 충실하게 반영되었으며, Eric Evans DDD의 전술적 설계 원칙과 Hexagonal Architecture의 Port-Adapter 분리 원칙을 모두 잘 따르고 있습니다. `ProductSummaryDto` 양층 중복은 의도적 설계 결정으로 원칙에 부합하며, Specification 인메모리 폴백은 인지된 trade-off입니다.
