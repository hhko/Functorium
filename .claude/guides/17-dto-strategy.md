# DTO 전략

레이어 간 데이터를 전송할 때 DTO가 무질서하게 증가하는 것은 흔한 문제입니다. 명확한 소유권과 재사용 규칙 없이 DTO를 만들면 레이어 간 결합이 강해지고, 하나의 변경이 여러 레이어에 연쇄적으로 영향을 미칩니다.
이 가이드는 레이어별 DTO 소유권을 정의하고, 재사용이 허용되는 조건을 명시하여 DTO 급증 문제를 방지합니다.

이 문서는 Functorium 프레임워크에서 레이어 간 데이터 전송 객체(DTO)의 설계 원칙, 소유권, 변환 규칙을 통합적으로 다룹니다.

## 목차

- [왜 레이어별 DTO가 필요한가 (WHY)](#왜-레이어별-dto가-필요한가-why)
- [레이어별 DTO 소유권 (WHAT)](#레이어별-dto-소유권-what)
- [레이어별 DTO 구현 (HOW)](#레이어별-dto-구현-how)
  - [Presentation Layer](#presentation-layer)
  - [Application Layer](#application-layer)
  - [Persistence Layer](#persistence-layer)
- [컬렉션 타입 변환](#컬렉션-타입-변환)
- [Application DTO 재사용 허용 조건](#application-dto-재사용-허용-조건)
- [FAQ](#faq)
- [참고 문서](#참고-문서)

---

## 왜 레이어별 DTO가 필요한가 (WHY)

Hexagonal Architecture에서 각 레이어(Port/Adapter)는 자신만의 데이터 표현을 소유합니다. 이는 레이어 간 독립적 진화를 보장합니다.

| 문제 상황 | 공유 DTO 사용 | 레이어별 DTO 사용 |
|----------|-------------|-----------------|
| API 필드 추가 | Application도 수정 | Presentation만 수정 |
| DB 컬럼 변경 | Domain에 영향 | Persistence Adapter만 수정 |
| 직렬화 포맷 변경 | 전 레이어 영향 | Adapter만 수정 |
| 타입 시스템 차이 | 타협 필요 (`Seq` vs `List`) | 각 레이어 최적 타입 사용 |

---

## 레이어별 DTO 소유권 (WHAT)

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

| 레이어 | DTO 형태 | 타입 특성 | 소유 위치 |
|--------|----------|----------|----------|
| Presentation | Endpoint nested record | primitive (JSON 직렬화) | Endpoint 클래스 내부 |
| Application | Usecase nested record | primitive (직렬화 가능) | Usecase 클래스 내부 |
| Application (공유) | 독립 record | primitive | `Usecases/{Aggregate}/Dtos/` |
| Persistence | Model (POCO) | primitive (DB 매핑) | `Repositories/EfCore/Models/` |

---

## 레이어별 DTO 구현 (HOW)

### Presentation Layer

**기본**: Endpoint nested record — 각 Endpoint가 자신의 Request/Response를 소유합니다.

```csharp
// CreateProductEndpoint.cs
public sealed class CreateProductEndpoint
    : Endpoint<CreateProductEndpoint.Request, CreateProductEndpoint.Response>
{
    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        // [변환 A] Endpoint Request → Usecase Request
        var usecaseRequest = new CreateProductCommand.Request(
            req.Name, req.Description, req.Price, req.StockQuantity);

        var result = await _mediator.Send(usecaseRequest, ct);

        // [변환 B] Usecase Response → Endpoint Response
        var mapped = result.Map(r => new Response(
            r.ProductId, r.Name, r.Description, r.Price, r.StockQuantity, r.CreatedAt));

        await this.SendCreatedFinResponseAsync(mapped, ct);
    }

    public sealed record Request(string Name, string Description, decimal Price, int StockQuantity);
    public new sealed record Response(string ProductId, string Name, string Description,
        decimal Price, int StockQuantity, DateTime CreatedAt);
}
```

**예외**: Application DTO 재사용 — [허용 조건](#application-dto-재사용-허용-조건)을 충족하면 Endpoint Response에서 Application DTO를 직접 사용할 수 있습니다.

```csharp
// GetAllProductsEndpoint.cs — Application DTO 재사용 예시
using LayeredArch.Application.Usecases.Products.Dtos;

public sealed class GetAllProductsEndpoint
    : EndpointWithoutRequest<GetAllProductsEndpoint.Response>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAllProductsQuery.Request(), ct);
        // Seq → List 변환만 수행, DTO 자체는 재사용
        var mapped = result.Map(r => new Response(r.Products.ToList()));
        await this.SendFinResponseAsync(mapped, ct);
    }

    // Response가 Application의 ProductSummaryDto를 직접 참조
    public new sealed record Response(List<ProductSummaryDto> Products);
}
```

### Application Layer

**기본**: Usecase nested record — 각 Command/Query가 자신의 Request/Response를 소유합니다.

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

**공유 DTO**: 여러 Usecase에서 동일한 DTO가 필요하면 `Dtos/` 폴더에 독립 record로 정의합니다.

```
Application/Usecases/Products/
├── GetAllProductsQuery.cs        ← Response에서 ProductSummaryDto 참조
├── SearchProductsQuery.cs        ← Response에서 ProductSummaryDto 참조
└── Dtos/
    └── ProductSummaryDto.cs      ← 교차 Usecase 공유 DTO
```

```csharp
// Dtos/ProductSummaryDto.cs
namespace LayeredArch.Application.Usecases.Products.Dtos;

public sealed record ProductSummaryDto(
    string ProductId,
    string Name,
    decimal Price,
    int StockQuantity);
```

**도메인 → Application DTO 변환**: Value Object의 `implicit operator`를 통해 자연스럽게 primitive로 변환됩니다.

```csharp
// Usecase 내부 — VO → primitive 암시적 변환
new ProductSummaryDto(p.Id.ToString(), p.Name, p.Price, p.StockQuantity)
//                     ↑ Ulid→string    ↑ ProductName→string  ↑ Money→decimal  ↑ Quantity→int
```

### Persistence Layer

**Model(POCO)**: primitive 타입만 사용하며, 도메인 의존성이 없습니다.

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

**Mapper**: `internal static class`, 확장 메서드로 양방향 변환을 제공합니다.

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
        var product = Product.CreateFromValidated(   // 검증 없이 복원
            ProductId.Create(model.Id),
            ProductName.CreateFromValidated(model.Name),
            // ...
        );
        product.ClearDomainEvents();  // 복원 부산물 이벤트 제거
        return product;
    }
}
```

| 설계 포인트 | 설명 |
|------------|------|
| `internal` 접근 제한 | Mapper는 Persistence Adapter의 구현 세부사항 |
| Extension method | 자연스러운 호출 문법 (`product.ToModel()`) |
| `CreateFromValidated` | DB에서 복원 시 검증 스킵으로 성능 확보 |
| `ClearDomainEvents()` | 복원 과정의 부산물 이벤트 제거 (DDD 원칙) |

---

## 컬렉션 타입 변환

Application Layer는 `Seq<T>` (LanguageExt FP 타입), Presentation/Persistence는 `List<T>` (JSON 직렬화/EF Core 호환)를 사용합니다.

```
Application (Seq<T>) ──.ToList()──→ Presentation (List<T>)
Application (Seq<T>) ──.ToList()──→ Persistence  (List<T>)
Persistence (List<T>) ──toSeq()───→ Application  (Seq<T>)
```

```csharp
// Presentation: Seq → List (Endpoint에서 변환)
var mapped = result.Map(r => new Response(r.Products.ToList()));

// Persistence: List → Seq (Repository에서 변환)
return Fin.Succ(toSeq(models.Select(m => m.ToDomain())));
```

> **참고**: `Seq<T>`는 System.Text.Json으로 직렬화할 수 없으므로, Presentation 경계에서 반드시 `List<T>`로 변환해야 합니다.

---

## Application DTO 재사용 허용 조건

기본 원칙은 각 레이어가 자신의 DTO를 소유하는 것입니다. 그러나 다음 **4가지 조건을 모두** 충족하면 Presentation에서 Application DTO를 직접 재사용할 수 있습니다:

| # | 조건 | 근거 |
|---|------|------|
| 1 | **읽기 전용 Query** 응답이다 | Command 결과는 레이어별 진화 가능성이 높음 |
| 2 | **필드가 동일**하여 identity mapping이 발생한다 | 필드 추가/제거 예정이면 분리 유지 |
| 3 | Presentation 고유 필드(HATEOAS link 등)가 **불필요**하다 | 고유 필드가 필요하면 Endpoint DTO 필요 |
| 4 | **`Seq<T>` → `List<T>` 변환만** 필요하다 | 컬렉션 타입 변환은 Response wrapper에서 처리 |

**적용 예시**: `GetAllProductsEndpoint`는 `ProductSummaryDto`를 직접 참조하되, Response wrapper에서 `Seq → List` 변환만 수행합니다.

**해제 시점**: 4가지 조건 중 하나라도 깨지면 Endpoint 전용 DTO로 전환합니다.

---

## FAQ

### Q: Usecase Request/Response는 왜 primitive 타입을 사용하나요?

Usecase의 Request/Response는 **외부 API 경계**(Presentation에서 호출)에 위치합니다. JSON 직렬화 호환성과 외부 계약 안정성을 위해 primitive 타입(`string`, `decimal`, `int`)을 사용합니다. 반면, Port 인터페이스는 **내부 계약**(Application ↔ Adapter)이므로 도메인 Value Object를 사용합니다.

### Q: Application DTO 재사용은 Hexagonal Architecture 위반 아닌가요?

원칙적으로는 각 레이어가 독립적 DTO를 소유해야 합니다. 그러나 identity mapping(동일 필드를 1:1 복사)이 발생하는 읽기 전용 시나리오에서는 실용적 판단으로 재사용을 허용합니다. 이는 의존성 방향(Presentation → Application)과 일치하므로 아키텍처 규칙을 위반하지 않습니다.

### Q: Persistence Model에서 `HasConversion` 대신 Mapper를 쓰는 이유는?

EF Core `HasConversion`은 도메인 Entity에 직접 적용되어, 도메인이 ORM에 결합됩니다. Mapper 패턴은 도메인 Entity와 Persistence Model(POCO)을 완전히 분리하여 **Persistence Ignorance**를 보장합니다.

### Q: 공유 DTO(`Dtos/` 폴더)는 언제 만드나요?

2개 이상의 Usecase가 동일한 DTO를 사용할 때입니다. 단일 Usecase에서만 사용되면 Usecase nested record로 유지합니다.

---

## 참고 문서

- [11-usecases-and-cqrs.md](./11-usecases-and-cqrs.md) — Usecase Request/Response 패턴
- [12-ports.md §1.4](./12-ports.md) — Port Request/Response 설계
- [13-adapters.md §2.6](./13-adapters.md) — 데이터 변환 (Mapper 패턴)
- [01-project-structure.md](./01-project-structure.md) — Dtos/ 폴더 위치 규칙
- [dto-strategy-review.md](../dto-strategy-review.md) — DTO 매핑 전략 리뷰 (DDD & Hexagonal 관점)
