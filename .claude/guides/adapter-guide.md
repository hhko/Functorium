# Adapter 구현 가이드

이 문서는 Functorium 프레임워크의 `Functorium.Applications.Observabilities` 및 `Functorium.Adapters.SourceGenerator` 네임스페이스를 사용하여 Clean Architecture의 Adapter를 구현하는 방법을 설명합니다.

## 목차

- [개요](#개요)
- [인터페이스 계층 구조](#인터페이스-계층-구조)
- [IAdapter 인터페이스](#iadapter-인터페이스)
- [Port 인터페이스 Request/Response 설계](#port-인터페이스-requestresponse-설계)
  - [Usecase와 Port의 Request/Response 차이점](#usecase와-port의-requestresponse-차이점)
  - [데이터 변환 흐름 아키텍처](#데이터-변환-흐름-아키텍처)
  - [Port 인터페이스 Request/Response 정의 원칙](#port-인터페이스-requestresponse-정의-원칙)
- [GeneratePipeline 소스 생성기](#generatepipeline-소스-생성기)
  - [생성되는 Pipeline 클래스](#생성되는-pipeline-클래스)
  - [자동 제공 기능](#자동-제공-기능)
- [구현 패턴](#구현-패턴)
  - [Repository 구현](#repository-구현)
  - [Messaging 구현](#messaging-구현)
  - [외부 API 서비스 구현](#외부-api-서비스-구현)
- [Adapter 구현에서의 데이터 변환](#adapter-구현에서의-데이터-변환)
  - [Infrastructure Adapter (HTTP API)](#infrastructure-adapter-http-api)
  - [Persistence Adapter (Repository)](#persistence-adapter-repository)
- [에러 처리 통합](#에러-처리-통합)
- [의존성 등록](#의존성-등록)
- [설계 체크리스트](#설계-체크리스트)
- [실전 예제](#실전-예제)
- [FAQ](#faq)
- [참고 문서](#참고-문서)

---

## 개요

Adapter는 Clean Architecture에서 **Application Layer와 외부 시스템 간의 경계**를 담당합니다. "데이터베이스 접근", "메시지 큐", "외부 API 호출" 같은 **인프라스트럭처 관심사**를 캡슐화합니다.

### 왜 Adapter 패턴을 사용하나요?

Adapter가 없으면 다음과 같은 문제가 발생합니다:

```csharp
// 문제점 1: Application Layer가 Infrastructure에 직접 의존
public class CreateOrderUsecase
{
    private readonly DbContext _dbContext;  // EF Core 직접 의존

    public async Task Handle(CreateOrderCommand command)
    {
        _dbContext.Orders.Add(order);  // Infrastructure 코드가 Application에 침투
        await _dbContext.SaveChangesAsync();
    }
}

// 문제점 2: 관찰성(Observability) 코드가 비즈니스 로직에 산재
public async Task<Product> GetProductAsync(Guid id)
{
    using var activity = ActivitySource.StartActivity("GetProduct");  // 트레이싱
    _logger.LogInformation("Getting product {Id}", id);  // 로깅
    var stopwatch = Stopwatch.StartNew();  // 메트릭

    var product = await _repository.FindAsync(id);  // 실제 로직

    stopwatch.Stop();
    _metrics.RecordDuration(stopwatch.Elapsed);  // 메트릭
    return product;
}

// 문제점 3: 테스트하기 어려움
// DbContext를 직접 사용하면 단위 테스트에서 Mock 불가
```

Adapter 패턴은 이 문제들을 해결합니다:

```csharp
// 해결책: Port 인터페이스로 추상화
public interface IProductRepository : IAdapter
{
    FinT<IO, Product> GetById(Guid id);
}

// Application Layer는 인터페이스에만 의존
public class GetProductUsecase(IProductRepository repository)
{
    public async ValueTask<FinResponse<Response>> Handle(Request request)
    {
        return await repository.GetById(request.Id)
            .Run().RunAsync()
            .ToFinResponse();
    }
}

// Infrastructure Layer에서 구현 + 자동 관찰성
[GeneratePipeline]  // 로깅, 트레이싱, 메트릭 자동 생성
public class InMemoryProductRepository : IProductRepository
{
    public string RequestCategory => "Repository";

    public virtual FinT<IO, Product> GetById(Guid id)
    {
        // 순수한 비즈니스 로직만 작성
        return IO.lift(() =>
            _products.TryGetValue(id, out var product)
                ? Fin.Succ(product)
                : Fin.Fail<Product>(Error.New($"Product not found: {id}")));
    }
}
```

### 핵심 특징

| 특성 | 설명 |
|------|------|
| **Port-Adapter 패턴** | Application Layer는 Port(인터페이스)만 알고, Infrastructure Layer가 Adapter를 구현 |
| **함수형 반환 타입** | `FinT<IO, T>`로 비동기 작업과 에러 처리를 함수형으로 합성 |
| **자동 관찰성** | `[GeneratePipeline]` 속성으로 로깅, 트레이싱, 메트릭 자동 생성 |
| **테스트 용이성** | 인터페이스 기반으로 Mock 객체 쉽게 생성 |

### Adapter 유형

| 유형 | 용도 | RequestCategory | 예시 |
|------|------|-----------------|------|
| **Repository** | 데이터 영속화 | `"Repository"` | `IProductRepository`, `IOrderRepository` |
| **Messaging** | 메시지 큐/이벤트 | `"Messaging"` | `IOrderMessaging`, `IInventoryMessaging` |
| **External API** | 외부 서비스 호출 | `"Http"` | `IPaymentApiService`, `IWeatherApiService` |

---

## 인터페이스 계층 구조

Functorium은 Adapter 구현을 위한 인터페이스 계층을 제공합니다.

```
IAdapter (인터페이스)
├── string RequestCategory - 관찰성 로그용 카테고리
│
├── IProductRepository : IAdapter
│   ├── FinT<IO, Product> GetById(Guid id)
│   ├── FinT<IO, Seq<Product>> GetAll()
│   └── FinT<IO, Product> Create(Product product)
│
├── IOrderMessaging : IAdapter
│   ├── FinT<IO, Unit> PublishOrderCreated(OrderCreatedEvent @event)
│   └── FinT<IO, CheckInventoryResponse> CheckInventory(CheckInventoryRequest request)
│
└── IExternalApiService : IAdapter
    └── FinT<IO, Response> CallApiAsync(Request request, CancellationToken ct)
```

**계층 이해하기:**

- **IAdapter**: 모든 Adapter가 구현하는 기반 인터페이스. `RequestCategory` 속성 제공
- **Domain Repository**: Entity 영속화를 담당. Domain Layer에 인터페이스 정의
- **Port Interface**: Application Layer에서 필요한 외부 서비스 인터페이스 정의

---

## IAdapter 인터페이스

모든 Adapter가 구현해야 하는 기반 인터페이스입니다.

**위치**: `Functorium.Applications.Observabilities.IAdapter`

```csharp
public interface IAdapter
{
    /// <summary>
    /// 관찰성 로그에서 사용할 요청 카테고리
    /// </summary>
    string RequestCategory { get; }
}
```

**RequestCategory 값 가이드:**

| 값 | 용도 | 예시 |
|---|------|------|
| `"Repository"` | 데이터베이스/영속화 | EF Core, Dapper, InMemory |
| `"Messaging"` | 메시지 큐 | RabbitMQ, Kafka, Azure Service Bus |
| `"Http"` | HTTP API 호출 | REST API, GraphQL |
| `"Cache"` | 캐시 서비스 | Redis, InMemory Cache |
| `"File"` | 파일 시스템 | 파일 읽기/쓰기 |

---

## Port 인터페이스 Request/Response 설계

Usecase의 Request/Response 패턴과 동일한 이름 패턴을 IAdapter Port 인터페이스에 적용하여 개념을 단순화하고, 레이어 간 데이터 변환 책임을 명확히 정의합니다.

### Usecase와 Port의 Request/Response 차이점

#### 핵심 차이점 비교

| 관점 | Usecase Request/Response | Port Request/Response |
|------|--------------------------|----------------------|
| **위치** | Command/Query 클래스 내부 | IAdapter 인터페이스 내부 |
| **목적** | 외부 API 경계 정의 | 내부 시스템 간 계약 정의 |
| **타입 선호도** | Primitive (string, Guid, decimal) | 도메인 값 객체 (ProductId, Money) |
| **검증 책임** | FluentValidation 입력 검증 | 값 객체 불변식으로 보장 |
| **직렬화** | JSON 직렬화 필요 (외부 노출) | 직렬화 불필요 (내부 사용) |

#### 패턴 비교

```csharp
// ═══════════════════════════════════════════════════════════════════════════
// Usecase 패턴 (외부 API 경계)
// ═══════════════════════════════════════════════════════════════════════════
public sealed class CreateProductCommand
{
    // Primitive 타입 사용 - JSON 직렬화 친화적
    public sealed record Request(
        string Name,           // string (not ProductName)
        string Description,
        decimal Price,         // decimal (not Money)
        int StockQuantity) : ICommandRequest<Response>;

    public sealed record Response(
        Guid ProductId,        // Guid (not ProductId)
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        DateTime CreatedAt);
}

// ═══════════════════════════════════════════════════════════════════════════
// Port 패턴 (내부 계약) - 동일한 구조, 다른 타입
// ═══════════════════════════════════════════════════════════════════════════
public interface IProductRepository : IAdapter
{
    // 도메인 값 객체 사용 - 기술 독립적
    sealed record GetByIdRequest(ProductId Id);
    sealed record GetByIdResponse(Product Product);

    sealed record CreateRequest(Product Product);
    sealed record CreateResponse(Product CreatedProduct);

    FinT<IO, GetByIdResponse> GetById(GetByIdRequest request);
    FinT<IO, CreateResponse> Create(CreateRequest request);
}
```

### 데이터 변환 흐름 아키텍처

#### 전체 흐름 다이어그램

```
┌────────────────────────────────────────────────────────────────────────────────┐
│                           PRESENTATION LAYER                                   │
│  ┌──────────────────────────────────────────────────────────────────────────┐ │
│  │ CreateProductEndpoint                                                     │ │
│  │   Request { Name: string, Price: decimal }  ← JSON 직렬화                │ │
│  │   Response { ProductId: Guid, ... }                                       │ │
│  └──────────────────────────────────────────────────────────────────────────┘ │
│                              │ [자동 바인딩]                                   │
└──────────────────────────────┼─────────────────────────────────────────────────┘
                               ▼
┌────────────────────────────────────────────────────────────────────────────────┐
│                           APPLICATION LAYER                                    │
│  ┌──────────────────────────────────────────────────────────────────────────┐ │
│  │ CreateProductCommand.Usecase                                              │ │
│  │   Request { Name: string, Price: decimal } : ICommandRequest<Response>   │ │
│  │                                                                           │ │
│  │   // [변환 책임 1] Usecase 내부에서 값 객체 생성                           │ │
│  │   var productId = ProductId.Create(Guid.NewGuid());                       │ │
│  │   var productName = ProductName.Create(request.Name);                     │ │
│  │   var money = Money.Create(request.Price, "KRW");                         │ │
│  └──────────────────────────────────────────────────────────────────────────┘ │
│                              │                                                 │
│  ┌──────────────────────────────────────────────────────────────────────────┐ │
│  │ IProductRepository : IAdapter  (Port Interface)                          │ │
│  │   CreateRequest { Product }       ← 도메인 값 객체/엔티티                 │ │
│  │   CreateResponse { Product }                                              │ │
│  └──────────────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────┼─────────────────────────────────────────────────┘
                               ▼
┌────────────────────────────────────────────────────────────────────────────────┐
│                      INFRASTRUCTURE / PERSISTENCE LAYER                        │
│  ┌──────────────────────────────────────────────────────────────────────────┐ │
│  │ [GeneratePipeline]                                                        │ │
│  │ ProductRepository : IProductRepository                                    │ │
│  │   RequestCategory => "Repository"                                         │ │
│  │                                                                           │ │
│  │   // [변환 책임 2] Adapter 내부에서 기술 DTO 변환                          │ │
│  │   ProductMapper.ToEntity(product)  : Product → ProductEntity              │ │
│  │   ProductMapper.ToDomain(entity)   : ProductEntity → Product              │ │
│  │                                                                           │ │
│  │   ┌────────────────────────────────────────────────────────────────────┐ │ │
│  │   │ 기술 관심사 DTO (internal)                                          │ │ │
│  │   │   ProductEntity { Id: Guid, Name: string }    ← EF Core Entity     │ │ │
│  │   │   ProductApiDto { id: string, name: string }  ← HTTP API DTO       │ │ │
│  │   └────────────────────────────────────────────────────────────────────┘ │ │
│  └──────────────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────┼─────────────────────────────────────────────────┘
                               ▼
┌────────────────────────────────────────────────────────────────────────────────┐
│                            EXTERNAL SYSTEMS                                    │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐                │
│  │ Database        │  │ Message Queue   │  │ External API    │                │
│  │ (Oracle, MSSQL) │  │ (RabbitMQ)      │  │ (HTTP/REST)     │                │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘                │
└────────────────────────────────────────────────────────────────────────────────┘
```

#### 각 경계에서의 변환 책임

| 경계 | 변환 주체 | 변환 내용 | 에러 처리 |
|------|----------|----------|----------|
| Presentation → Application | 프레임워크 | JSON → Usecase.Request | 400 Bad Request |
| Application 내부 | **Usecase 클래스** | Primitive → 값 객체 | FinResponse.Fail |
| Application → Infrastructure | **Adapter Mapper** | 값 객체 → 기술 DTO | FinT<IO, T> |
| Infrastructure → External | HttpClient / DbContext | DTO → 외부 프로토콜 | Exception → Fin.Fail |

### Port 인터페이스 Request/Response 정의 원칙

#### 원칙 1: 인터페이스 내부에 sealed record 정의

```csharp
// Application/Ports/IProductRepository.cs
public interface IProductRepository : IAdapter
{
    // ✅ 인터페이스 내부에 Request/Response 정의 (응집도 향상)
    sealed record GetByIdRequest(ProductId Id);
    sealed record GetByIdResponse(Product Product);

    sealed record CreateRequest(Product Product);
    sealed record CreateResponse(Product CreatedProduct);

    FinT<IO, GetByIdResponse> GetById(GetByIdRequest request);
    FinT<IO, CreateResponse> Create(CreateRequest request);
}
```

#### 원칙 2: 도메인 값 객체 직접 사용 (기술 독립성)

```csharp
// ✅ Good - 도메인 값 객체 사용
sealed record Request(ProductId Id, ProductName Name);

// ❌ Bad - Primitive 타입 직접 사용
sealed record Request(Guid Id, string Name);

// ❌ Bad - 기술 관심사 타입 사용
sealed record Request(ProductEntity Entity);  // EF Core 타입
```

#### 원칙 3: 메서드 수에 따른 네이밍 전략

**단일 메서드 인터페이스:**
```csharp
public interface IWeatherApiService : IAdapter
{
    // 접두사 없이 Request/Response
    sealed record Request(string City, DateTime Date);
    sealed record Response(decimal Temperature, string Condition);

    FinT<IO, Response> GetWeatherAsync(Request request, CancellationToken ct);
}
```

**다중 메서드 인터페이스:**
```csharp
public interface IProductRepository : IAdapter
{
    // {Action}Request / {Action}Response
    sealed record GetByIdRequest(ProductId Id);
    sealed record GetByIdResponse(Product Product);

    sealed record GetAllRequest(int? PageSize = null, int? PageNumber = null);
    sealed record GetAllResponse(Seq<Product> Products, int TotalCount);

    sealed record CreateRequest(Product Product);
    sealed record CreateResponse(Product CreatedProduct);

    FinT<IO, GetByIdResponse> GetById(GetByIdRequest request);
    FinT<IO, GetAllResponse> GetAll(GetAllRequest request);
    FinT<IO, CreateResponse> Create(CreateRequest request);
}
```

#### 원칙 4: 중첩 record 가이드라인

```csharp
public interface IEquipmentApiService : IAdapter
{
    // 복잡한 중첩 구조 예시
    sealed record GetHistoryRequest(
        EquipId EquipId,
        DateRange DateRange,        // 값 객체
        EquipmentFilter? Filter);   // 선택적 필터 (중첩)

    // 중첩 record도 인터페이스 내부에 정의
    sealed record EquipmentFilter(
        Seq<EquipTypeId> EquipTypes,
        bool IncludeInactive = false);

    sealed record GetHistoryResponse(Seq<EquipmentHistory> Histories);

    sealed record EquipmentHistory(
        EquipId EquipId,
        DateTime Timestamp,
        EquipmentStatus Status,
        Seq<HistoryDetail> Details);   // 최대 2-3 레벨

    sealed record HistoryDetail(
        string PropertyName,
        string OldValue,
        string NewValue);

    FinT<IO, GetHistoryResponse> GetHistoryAsync(GetHistoryRequest request, CancellationToken ct);
}
```

**중첩 record 규칙:**
- 2-3 레벨까지 허용 (과도한 중첩 지양)
- 도메인 의미가 있는 경우만 중첩
- 여러 메서드에서 재사용되면 별도 타입으로 분리
- 모든 중첩 record는 sealed

### Repository 인터페이스 설계 원칙

#### 파라미터 타입 원칙

| 원칙 | 설명 | 예시 |
|------|------|------|
| **VO 우선** | 원시 타입 대신 값 객체 사용 | `ExistsByName(ProductName name)` |
| **선택적 파라미터** | null 가능한 경우 명시 | `ProductId? excludeId = null` |
| **Entity ID 타입** | 강타입 ID 사용 | `GetById(ProductId id)` |

#### 메서드 시그니처 패턴

```csharp
public interface IProductRepository : IAdapter
{
    // 생성: Entity 반환
    FinT<IO, Product> Create(Product product);

    // 조회 (단일): 없으면 Error
    FinT<IO, Product> GetById(ProductId id);

    // 조회 (단일, Optional): 없으면 None
    FinT<IO, Option<Product>> GetByName(ProductName name);

    // 조회 (목록): 빈 Seq도 성공
    FinT<IO, Seq<Product>> GetAll();

    // 존재 확인: bool 반환
    FinT<IO, bool> ExistsByName(ProductName name, ProductId? excludeId = null);

    // 삭제: Unit 반환
    FinT<IO, Unit> Delete(ProductId id);

    // 업데이트: 변경된 Entity 반환
    FinT<IO, Product> Update(Product product);
}
```

#### 메서드 반환 타입 가이드

| 작업 | 반환 타입 | 설명 |
|------|----------|------|
| **Create** | `FinT<IO, Entity>` | 생성된 Entity 반환 |
| **GetById** | `FinT<IO, Entity>` | 없으면 Error (필수 조회) |
| **GetByX (Optional)** | `FinT<IO, Option<Entity>>` | 없으면 None (선택 조회) |
| **GetAll / GetMany** | `FinT<IO, Seq<Entity>>` | 빈 목록도 성공 |
| **ExistsBy** | `FinT<IO, bool>` | 존재 여부만 확인 |
| **Update** | `FinT<IO, Entity>` | 업데이트된 Entity 반환 |
| **Delete** | `FinT<IO, Unit>` | 성공/실패만 반환 |

#### ExistsByName with excludeId 패턴

업데이트 시 자기 자신을 제외하고 중복 검사가 필요한 경우:

```csharp
// 인터페이스
FinT<IO, bool> ExistsByName(ProductName name, ProductId? excludeId = null);

// 구현
public virtual FinT<IO, bool> ExistsByName(ProductName name, ProductId? excludeId = null)
{
    return IO.lift(() =>
    {
        bool exists = _products.Values.Any(p =>
            ((string)p.Name).Equals(name, StringComparison.OrdinalIgnoreCase) &&
            (excludeId is null || p.Id != excludeId.Value));
        return Fin.Succ(exists);
    });
}

// Usecase에서 사용 (UpdateProductCommand)
from exists in _productRepository.ExistsByName(name, productId)
from _ in guard(!exists, ApplicationErrors.ProductNameAlreadyExists(request.Name))
```

---

## GeneratePipeline 소스 생성기

`[GeneratePipeline]` 속성을 클래스에 적용하면 Source Generator가 **Pipeline 래퍼 클래스를 자동 생성**합니다.

**위치**: `Functorium.Adapters.SourceGenerator.GeneratePipelineAttribute`

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class GeneratePipelineAttribute : Attribute { }
```

### 생성되는 Pipeline 클래스

원본 클래스가 `InMemoryProductRepository`라면, `InMemoryProductRepositoryPipeline` 클래스가 자동 생성됩니다.

```
원본 클래스                         생성된 Pipeline 클래스
┌─────────────────────────────┐     ┌─────────────────────────────────────┐
│ [GeneratePipeline]          │     │ InMemoryProductRepositoryPipeline  │
│ InMemoryProductRepository   │ ──► │   : InMemoryProductRepository      │
│   : IProductRepository      │     │                                     │
├─────────────────────────────┤     ├─────────────────────────────────────┤
│ GetById(Guid id)            │     │ override GetById(Guid id)           │
│ GetAll()                    │     │   + Activity Span 생성              │
│ Create(Product)             │     │   + 로깅 (Debug/Info/Error)         │
└─────────────────────────────┘     │   + 메트릭 (Counter/Histogram)      │
                                    │   + 에러 분류                        │
                                    └─────────────────────────────────────┘
```

**생성된 Pipeline 클래스 구조:**

```csharp
// 자동 생성되는 코드 (개념적 표현)
public class InMemoryProductRepositoryPipeline : InMemoryProductRepository
{
    private readonly ActivitySource _activitySource;
    private readonly ILogger<InMemoryProductRepositoryPipeline> _logger;
    private readonly IMeterFactory _meterFactory;

    public InMemoryProductRepositoryPipeline(
        ActivitySource activitySource,
        ILogger<InMemoryProductRepositoryPipeline> logger,
        IMeterFactory meterFactory,
        IOptions<OpenTelemetryOptions> options,
        ILogger<InMemoryProductRepository> originalLogger)  // 원본 생성자 매개변수
        : base(originalLogger)
    {
        // 초기화
    }

    public override FinT<IO, Product> GetById(Guid id)
    {
        // 1. Activity Span 시작
        // 2. 요청 로깅
        // 3. 원본 메서드 호출: base.GetById(id)
        // 4. 응답/에러 로깅
        // 5. 메트릭 기록
        // 6. Activity Span 종료
    }
}
```

### 자동 제공 기능

Pipeline은 다음 관찰성 기능을 **자동으로** 제공합니다. 모든 필드는 OpenTelemetry 시맨틱 규칙과의 일관성을 위해 `snake_case + dot` 표기법을 사용합니다.

#### 1. 분산 트레이싱 (OpenTelemetry)

**Span 이름 패턴:**
```
{layer} {category} {handler}.{method}
```

**실제 출력 예시:**
```
Span Name: adapter repository InMemoryProductRepository.GetById
Kind: Internal
Status: Ok | Error

Tags (Success - 6개):
├── request.layer: "adapter"
├── request.category: "repository"
├── request.handler: "InMemoryProductRepository"
├── request.handler.method: "GetById"
├── response.status: "success"
└── response.elapsed: 0.0234

Tags (Failure - 8개):
├── request.layer: "adapter"
├── request.category: "repository"
├── request.handler: "InMemoryProductRepository"
├── request.handler.method: "GetById"
├── response.status: "failure"
├── response.elapsed: 0.0051
├── error.type: "expected"
└── error.code: "Product.NotFound"
```

**ActivityStatus 처리:**

| 결과 | Status | Description |
|------|--------|-------------|
| Success | `Ok` | - |
| Failure | `Error` | `{error.type}: {error.code}` |

#### 2. 구조화된 로깅

**Message Templates:**

```
# Request (Information) - EventId: 2001
{request.layer} {request.category} {request.handler}.{request.handler.method} requesting

# Request (Debug) - EventId: 2001 - 파라미터 포함
{request.layer} {request.category} {request.handler}.{request.handler.method} {request.params.id} requesting

# Response Success (Information) - EventId: 2002
{request.layer} {request.category} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s

# Response Success (Debug) - EventId: 2002 - 결과 포함
{request.layer} {request.category} {request.handler}.{request.handler.method} {response.result} responded {response.status} in {response.elapsed:0.0000} s

# Response Warning (Expected Error) - EventId: 2003
{request.layer} {request.category} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}

# Response Error (Exceptional Error) - EventId: 2004
{request.layer} {request.category} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}
```

**실제 출력 예시:**

```
# Information 레벨 - Request
[INF] adapter repository InMemoryProductRepository.GetById requesting

# Debug 레벨 - Request (파라미터 포함)
[DBG] adapter repository InMemoryProductRepository.GetById 550e8400-e29b-41d4-a716-446655440000 requesting

# Information 레벨 - Success Response
[INF] adapter repository InMemoryProductRepository.GetById responded success in 0.0234 s

# Debug 레벨 - Success Response (결과 포함)
[DBG] adapter repository InMemoryProductRepository.GetById Product { Id = ..., Name = "Sample" } responded success in 0.0234 s

# Warning 레벨 - Expected Error (비즈니스 오류)
[WRN] adapter repository InMemoryProductRepository.GetById responded failure in 0.0051 s with expected:Product.NotFound { ErrorCode = "Product.NotFound", Message = "Entity not found" }

# Error 레벨 - Exceptional Error (시스템 오류)
[ERR] adapter repository InMemoryProductRepository.GetById responded failure in 0.0012 s with exceptional:Exceptional { Message = "Database connection failed" }
```

**로그 레벨 규칙:**

| 이벤트 | EventId | 로그 레벨 | 조건 |
|--------|---------|----------|------|
| Request | 2001 | Information / Debug | Debug는 파라미터 값 포함 |
| Response Success | 2002 | Information / Debug | Debug는 반환값 포함 |
| Response Warning | 2003 | Warning | `error.IsExpected == true` |
| Response Error | 2004 | Error | `error.IsExceptional == true` |

#### 3. 메트릭 수집

**Meter Name 패턴:**
```
{service.namespace}.adapter.{category}
```

예시: `mycompany.production.adapter.repository`

**Instruments:**

| Instrument | 이름 패턴 | 타입 | Unit | 설명 |
|------------|----------|------|------|------|
| requests | `adapter.{category}.requests` | Counter | `{request}` | 총 요청 수 |
| responses | `adapter.{category}.responses` | Counter | `{response}` | 응답 수 |
| duration | `adapter.{category}.duration` | Histogram | `s` | 처리 시간(초) |

**Tag 구조:**

| Tag Key | Request Counter | Duration Histogram | Response (Success) | Response (Failure) |
|---------|-----------------|--------------------|--------------------|-------------------|
| `request.layer` | "adapter" | "adapter" | "adapter" | "adapter" |
| `request.category` | 카테고리명 | 카테고리명 | 카테고리명 | 카테고리명 |
| `request.handler` | 핸들러명 | 핸들러명 | 핸들러명 | 핸들러명 |
| `request.handler.method` | 메서드명 | 메서드명 | 메서드명 | 메서드명 |
| `response.status` | - | - | "success" | "failure" |
| `error.type` | - | - | - | 에러 타입 |
| `error.code` | - | - | - | 에러 코드 |
| **Total Tags** | **4** | **4** | **5** | **7** |

**실제 출력 예시:**

```
# Request Counter Tags
{
  request.layer: adapter,
  request.category: repository,
  request.handler: InMemoryProductRepository,
  request.handler.method: GetById
}

# Success Response Counter Tags
{
  request.layer: adapter,
  request.category: repository,
  request.handler: InMemoryProductRepository,
  request.handler.method: GetById,
  response.status: success
}

# Failure Response Counter Tags (Expected Error)
{
  request.layer: adapter,
  request.category: repository,
  request.handler: InMemoryProductRepository,
  request.handler.method: GetById,
  response.status: failure,
  error.type: expected,
  error.code: Product.NotFound
}
```

#### 4. 에러 분류

Pipeline은 에러를 다음과 같이 분류합니다:

| Error Case | `error.type` | `error.code` | 로그 레벨 | 설명 |
|------------|--------------|--------------|----------|------|
| `IHasErrorCode` + `IsExpected` | `"expected"` | 에러 코드 | Warning | 예상된 비즈니스 오류 |
| `IHasErrorCode` + `IsExceptional` | `"exceptional"` | 에러 코드 | Error | 예외적 시스템 오류 |
| `ManyErrors` | `"aggregate"` | 첫 번째 에러 코드 | Warning/Error | 복합 에러 |
| `Expected` (LanguageExt) | `"expected"` | 타입 이름 | Warning | 에러 코드 없는 기본 예상 오류 |
| `Exceptional` (LanguageExt) | `"exceptional"` | 타입 이름 | Error | 에러 코드 없는 기본 예외 오류 |

**에러 객체 (`@error`) 구조:**

```json
// Expected Error
{
  "ErrorCode": "Product.NotFound",
  "ErrorCurrentValue": "550e8400-e29b-41d4-a716-446655440000",
  "Message": "Entity not found",
  "Code": -1000
}

// Exceptional Error
{
  "Message": "Database connection failed",
  "Code": -2146233079
}

// Aggregate Error
{
  "Errors": [
    { "ErrorCode": "Validation.Required", "Message": "Name is required" },
    { "ErrorCode": "Validation.Range", "Message": "Price must be positive" }
  ]
}
```

---

## 구현 패턴

### Repository 구현

데이터 영속화를 담당하는 Repository 구현 패턴입니다.

```csharp
using System.Collections.Concurrent;
using Functorium.Adapters.SourceGenerator;
using Functorium.Applications.Observabilities;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace MyApp.Infrastructure.Repositories;

/// <summary>
/// 메모리 기반 상품 리포지토리 구현
/// </summary>
[GeneratePipeline]
public class InMemoryProductRepository : IProductRepository
{
    private readonly ILogger<InMemoryProductRepository> _logger;
    private readonly ConcurrentDictionary<Guid, Product> _products = new();

    /// <summary>
    /// 관찰성 로그용 카테고리
    /// </summary>
    public string RequestCategory => "Repository";

    public InMemoryProductRepository(ILogger<InMemoryProductRepository> logger)
    {
        _logger = logger;
    }

    public virtual FinT<IO, Product> GetById(Guid id)
    {
        return IO.lift(() =>
        {
            if (_products.TryGetValue(id, out Product? product))
            {
                return Fin.Succ(product);
            }
            return Fin.Fail<Product>(Error.New($"상품 ID '{id}'을(를) 찾을 수 없습니다"));
        });
    }

    public virtual FinT<IO, Seq<Product>> GetAll()
    {
        return IO.lift(() => Fin.Succ(toSeq(_products.Values)));
    }

    public virtual FinT<IO, Product> Create(Product product)
    {
        return IO.lift(() =>
        {
            _products[product.Id] = product;
            return Fin.Succ(product);
        });
    }

    public virtual FinT<IO, Product> Update(Product product)
    {
        return IO.lift(() =>
        {
            if (!_products.ContainsKey(product.Id))
            {
                return Fin.Fail<Product>(Error.New($"상품 ID '{product.Id}'을(를) 찾을 수 없습니다"));
            }
            _products[product.Id] = product;
            return Fin.Succ(product);
        });
    }

    public virtual FinT<IO, bool> ExistsByName(string name)
    {
        return IO.lift(() =>
        {
            bool exists = _products.Values.Any(p =>
                p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return Fin.Succ(exists);
        });
    }
}
```

**구현 필수 항목:**

| 항목 | 설명 |
|------|------|
| `[GeneratePipeline]` | 클래스에 속성 적용 |
| `: IAdapter` | IAdapter 상속 인터페이스 구현 |
| `RequestCategory` | 카테고리 문자열 반환 |
| `virtual` 메서드 | Pipeline이 override할 수 있도록 |
| `FinT<IO, T>` 반환 | 함수형 에러 처리 |

### Messaging 구현

메시지 큐/이벤트 버스를 사용하는 Messaging 구현 패턴입니다.

```csharp
using Functorium.Adapters.SourceGenerator;
using Functorium.Applications.Observabilities;
using LanguageExt;
using static LanguageExt.Prelude;

namespace MyApp.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ 기반 주문 메시징 구현
/// </summary>
[GeneratePipeline]
public class RabbitMqOrderMessaging : IOrderMessaging
{
    private readonly IMessageEndpoint _endpoint;

    public string RequestCategory => "Messaging";

    public RabbitMqOrderMessaging(IMessageEndpoint endpoint)
    {
        _endpoint = endpoint;
    }

    /// <summary>
    /// Request-Reply 패턴: 재고 확인
    /// </summary>
    public virtual FinT<IO, CheckInventoryResponse> CheckInventory(
        CheckInventoryRequest request)
    {
        return IO.liftAsync(async () =>
        {
            try
            {
                var response = await _endpoint.InvokeAsync<CheckInventoryResponse>(request);
                return Fin.Succ(response);
            }
            catch (Exception ex)
            {
                return Fin.Fail<CheckInventoryResponse>(Error.New(ex));
            }
        });
    }

    /// <summary>
    /// Fire-and-Forget 패턴: 재고 예약
    /// </summary>
    public virtual FinT<IO, Unit> ReserveInventory(ReserveInventoryCommand command)
    {
        return IO.liftAsync(async () =>
        {
            try
            {
                await _endpoint.SendAsync(command);
                return Fin.Succ(unit);
            }
            catch (Exception ex)
            {
                return Fin.Fail<Unit>(Error.New(ex));
            }
        });
    }
}
```

### 외부 API 서비스 구현

HTTP 클라이언트로 외부 API를 호출하는 패턴입니다.

```csharp
using System.Net.Http.Json;
using Functorium.Abstractions.Errors;
using Functorium.Adapters.SourceGenerator;
using Functorium.Applications.Observabilities;
using LanguageExt;

namespace MyApp.Infrastructure.ExternalApis;

/// <summary>
/// 외부 결제 API 서비스 구현
/// </summary>
using Functorium.Adapters.Errors;
using static Functorium.Adapters.Errors.AdapterErrorType;

[GeneratePipeline]
public class PaymentApiService : IPaymentApiService
{
    private readonly HttpClient _httpClient;

    public string RequestCategory => "Http";

    public PaymentApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public virtual FinT<IO, PaymentResponse> ProcessPaymentAsync(
        PaymentRequest request,
        CancellationToken cancellationToken)
    {
        return IO.liftAsync(async () =>
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    "/api/payments",
                    request,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    return Fin.Fail<PaymentResponse>(
                        AdapterError.For<PaymentApiService>(
                            new Custom("ApiCallFailed"),
                            $"Status: {response.StatusCode}",
                            errorContent));
                }

                var result = await response.Content
                    .ReadFromJsonAsync<PaymentResponse>(cancellationToken: cancellationToken);

                return result is not null
                    ? Fin.Succ(result)
                    : Fin.Fail<PaymentResponse>(
                        AdapterError.For<PaymentApiService>(
                            new Null(),
                            "/api/payments",
                            "Response data is null"));
            }
            catch (Exception ex)
            {
                return Fin.Fail<PaymentResponse>(
                    AdapterError.FromException<PaymentApiService>(
                        new Custom("HttpException"),
                        ex));
            }
        });
    }
}
```

---

## Adapter 구현에서의 데이터 변환

### Infrastructure Adapter (HTTP API)

```csharp
// Adapters.Infrastructure/Apis/CriteriaApi/CriteriaApiService.cs
[GeneratePipeline]
public class CriteriaApiService : ICriteriaApiService
{
    private readonly HttpClient _httpClient;

    public string RequestCategory => "Http";

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
                    AdapterError.For<CriteriaApiService>(new Custom("ResponseNull"), url, "Response data is null"));
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

### Persistence Adapter (Repository)

```csharp
// Adapters.Persistence/Repositories/ProductRepository.cs
[GeneratePipeline]
public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _dbContext;

    public string RequestCategory => "Repository";

    public virtual FinT<IO, IProductRepository.GetByIdResponse> GetById(
        IProductRepository.GetByIdRequest request)
    {
        return IO.liftAsync(async () =>
        {
            // 1. 값 객체 → Primitive 변환 (implicit conversion)
            var entity = await _dbContext.Products
                .FirstOrDefaultAsync(p => p.Id == (Guid)request.Id);

            if (entity is null)
            {
                return Fin.Fail<IProductRepository.GetByIdResponse>(
                    AdapterError.For<ProductRepository>(
                        new NotFound(),
                        request.Id.ToString(),
                        $"Product with ID '{request.Id}' not found"));
            }

            // 2. Entity → 도메인 모델 변환
            var product = ProductMapper.ToDomain(entity);
            return Fin.Succ(new IProductRepository.GetByIdResponse(product));
        });
    }

    public virtual FinT<IO, IProductRepository.CreateResponse> Create(
        IProductRepository.CreateRequest request)
    {
        return IO.liftAsync(async () =>
        {
            // 도메인 모델 → Entity 변환
            var entity = ProductMapper.ToEntity(request.Product);

            _dbContext.Products.Add(entity);
            await _dbContext.SaveChangesAsync();

            return Fin.Succ(new IProductRepository.CreateResponse(request.Product));
        });
    }
}

// Mapper 클래스 (Persistence 내부 - internal)
internal static class ProductMapper
{
    public static Product ToDomain(ProductEntity entity)
        => Product.Create(
            ProductId.Create(entity.Id).IfFail(e => throw new InvalidOperationException(e.Message)),
            ProductName.Create(entity.Name).IfFail(e => throw new InvalidOperationException(e.Message)),
            Money.Create(entity.Price, "KRW").IfFail(e => throw new InvalidOperationException(e.Message)));

    public static ProductEntity ToEntity(Product domain)
        => new()
        {
            Id = domain.Id,
            Name = domain.Name,
            Price = domain.Price
        };
}
```

---

## 에러 처리 통합

### Error 반환 단순화

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

### FinT<IO, T>와 AdapterError 연계

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
AdapterError.For<InventoryRepository>(
    new Custom("ReservationFailed"),
    orderId.ToString(),
    "Failed to reserve inventory");

// Exception 래핑
AdapterError.FromException<ProductRepository>(
    new PipelineException(),
    exception);
```

### Pipeline의 자동 에러 분류

```
에러 타입                              로그 레벨      메트릭 태그
────────────────────────────────────────────────────────────────
IHasErrorCode + IsExpected  ────────► Warning       error.type: "expected"
IHasErrorCode + IsExceptional ──────► Error         error.type: "exceptional"
ManyErrors ─────────────────────────► Warning/Error error.type: "aggregate"
```

### 값 객체 공유 전략

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
┌──────────────────┐  ┌──────────────┐  ┌──────────────────┐
│ Application      │  │ Infrastructure│  │ Persistence      │
│ (Usecase)        │  │ (API Adapter) │  │ (Repository)     │
│                  │  │               │  │                  │
│ ProductId 사용   │  │ ProductId →   │  │ ProductId →      │
│                  │  │ string (DTO)  │  │ Guid (Entity)    │
└──────────────────┘  └──────────────┘  └──────────────────┘
```

---

## 의존성 등록

Pipeline 클래스를 DI 컨테이너에 등록하는 방법입니다.

**위치**: `Functorium.Abstractions.Registrations.AdapterPipelineRegistration`

### 기본 등록

```csharp
using Functorium.Abstractions.Registrations;

public static class AdapterRegistration
{
    public static IServiceCollection RegisterAdapters(this IServiceCollection services)
    {
        // 단일 인터페이스 등록
        services.RegisterScopedAdapterPipeline<IProductRepository, InMemoryProductRepositoryPipeline>();

        // Messaging 등록
        services.RegisterScopedAdapterPipeline<IOrderMessaging, RabbitMqOrderMessagingPipeline>();

        return services;
    }
}
```

### HttpClient 연동 등록

외부 API 서비스의 경우 Named HttpClient와 함께 등록합니다.

```csharp
using Functorium.Abstractions.Registrations;

public static class ApiRegistration
{
    public static IServiceCollection RegisterApiServices(this IServiceCollection services)
    {
        // HttpClient + Pipeline 동시 등록
        services.RegisterScopedHttpClientPipeline<IPaymentApiService, PaymentApiServicePipeline>(
            clientName: "PaymentApi",
            configureClient: client =>
            {
                client.BaseAddress = new Uri("https://api.payment.com");
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });

        return services;
    }
}
```

### 다중 인터페이스 등록

하나의 구현 클래스가 여러 인터페이스를 구현하는 경우:

```csharp
// 2개 인터페이스
services.RegisterScopedAdapterPipelineFor<IReadRepository, IWriteRepository, ProductRepositoryPipeline>();

// 3개 인터페이스
services.RegisterScopedAdapterPipelineFor<IService1, IService2, IService3, MyServicePipeline>();

// 4개 이상 인터페이스
services.RegisterScopedAdapterPipelineFor<MyServicePipeline>(
    typeof(IService1), typeof(IService2), typeof(IService3), typeof(IService4));
```

### 생명주기 선택

```csharp
// Scoped (기본, 권장)
services.RegisterScopedAdapterPipeline<IProductRepository, ProductRepositoryPipeline>();

// Transient
services.RegisterTransientAdapterPipeline<IProductRepository, ProductRepositoryPipeline>();

// Singleton
services.RegisterSingletonAdapterPipeline<IProductRepository, ProductRepositoryPipeline>();
```

---

## 설계 체크리스트

### Port 인터페이스 설계 체크리스트

**기본 구조:**
- [ ] IAdapter 인터페이스를 상속하는가?
- [ ] 메서드 반환 타입이 FinT<IO, T>인가?
- [ ] Request/Response가 인터페이스 내부에 sealed record로 정의되어 있는가?

**네이밍:**
- [ ] 단일 메서드: Request/Response (접두사 없음)
- [ ] 다중 메서드: {Action}Request / {Action}Response
- [ ] 인터페이스 이름이 역할을 명확히 나타내는가?

**타입 사용:**
- [ ] Request에 도메인 값 객체를 사용하는가?
- [ ] Primitive 타입 직접 사용을 최소화했는가?
- [ ] 기술 관심사 타입(Entity, DTO)을 사용하지 않았는가?

**중첩 타입:**
- [ ] 중첩 레벨이 2-3을 초과하지 않는가?
- [ ] 모든 중첩 record가 sealed인가?

### Adapter 구현 체크리스트

**기본 구조:**
- [ ] [GeneratePipeline] 어트리뷰트가 적용되어 있는가?
- [ ] RequestCategory 프로퍼티가 올바른 값을 반환하는가?
- [ ] 모든 인터페이스 메서드가 virtual로 선언되어 있는가?

**FinT<IO, T> 사용:**
- [ ] 동기: IO.lift(() => Fin.Succ(result))
- [ ] 비동기: IO.liftAsync(async () => { ... })
- [ ] Exception → Fin.Fail 변환

**데이터 변환:**
- [ ] Mapper 클래스가 internal로 선언되어 있는가?
- [ ] Port Request/Response ↔ Infrastructure DTO 변환이 Mapper에서 처리되는가?

**의존성 등록:**
- [ ] services.RegisterScopedAdapterPipeline<IInterface, ImplementationPipeline>()

### 설계 원칙 요약

| 원칙 | 설명 |
|------|------|
| **일관된 네이밍** | Usecase와 동일하게 `Interface.Request`, `Interface.Response` |
| **기술 독립성** | Port의 Request/Response는 도메인 값 객체만 사용 |
| **변환 책임 분리** | Usecase: Primitive→VO, Adapter: VO→DTO |
| **변환 캡슐화** | Mapper 클래스로 DTO ↔ Response 변환, internal 접근 제한 |
| **에러 통합** | AdapterError + AdapterErrorType으로 일관된 에러 처리 |
| **값 객체 공유** | 도메인 값 객체는 모든 레이어에서 공유 |

---

## 실전 예제

### Clean Architecture 구조에서의 전체 흐름

```
+-------------------------------------------------------------------+
|                       Presentation Layer                          |
|  FastEndpoints / Controllers                                      |
+-------------------------------------------------------------------+
                              |
                              v
+-------------------------------------------------------------------+
|                       Application Layer                           |
|  ┌─────────────────────────────────────────────────────────────┐  |
|  │ CreateProductCommand.Usecase                                │  |
|  │   - IProductRepository (Port Interface 의존)                │  |
|  │   - 비즈니스 로직 구현                                       │  |
|  └─────────────────────────────────────────────────────────────┘  |
|  ┌─────────────────────────────────────────────────────────────┐  |
|  │ IProductRepository : IAdapter (Port Interface)              │  |
|  │   - FinT<IO, Product> GetById(Guid id)                      │  |
|  │   - FinT<IO, Product> Create(Product product)               │  |
|  └─────────────────────────────────────────────────────────────┘  |
+-------------------------------------------------------------------+
                              |
                              v
+-------------------------------------------------------------------+
|                      Infrastructure Layer                         |
|  ┌─────────────────────────────────────────────────────────────┐  |
|  │ [GeneratePipeline]                                          │  |
|  │ InMemoryProductRepository : IProductRepository              │  |
|  │   - RequestCategory => "Repository"                         │  |
|  │   - 실제 데이터 접근 구현                                    │  |
|  └─────────────────────────────────────────────────────────────┘  |
|                              |                                    |
|                              v (Source Generator)                 |
|  ┌─────────────────────────────────────────────────────────────┐  |
|  │ InMemoryProductRepositoryPipeline (자동 생성)               │  |
|  │   - 트레이싱, 로깅, 메트릭 자동 추가                         │  |
|  │   - DI에서 IProductRepository로 등록                        │  |
|  └─────────────────────────────────────────────────────────────┘  |
+-------------------------------------------------------------------+
```

### 1. Port 인터페이스 정의 (Application Layer)

```csharp
// Application/Ports/IProductRepository.cs
using Functorium.Applications.Observabilities;
using LanguageExt;

namespace MyApp.Application.Ports;

public interface IProductRepository : IAdapter
{
    FinT<IO, Product> GetById(ProductId id);
    FinT<IO, Seq<Product>> GetAll();
    FinT<IO, Product> Create(Product product);
    FinT<IO, Product> Update(Product product);
    FinT<IO, bool> ExistsByName(ProductName name);
}
```

### 2. Adapter 구현 (Infrastructure Layer)

```csharp
// Infrastructure/Repositories/InMemoryProductRepository.cs
using System.Collections.Concurrent;
using Functorium.Adapters.SourceGenerator;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace MyApp.Infrastructure.Repositories;

[GeneratePipeline]
public class InMemoryProductRepository : IProductRepository
{
    private readonly ILogger<InMemoryProductRepository> _logger;
    private readonly ConcurrentDictionary<ProductId, Product> _products = new();

    public string RequestCategory => "Repository";

    public InMemoryProductRepository(ILogger<InMemoryProductRepository> logger)
    {
        _logger = logger;
    }

    public virtual FinT<IO, Product> GetById(ProductId id) =>
        IO.lift(() => _products.TryGetValue(id, out var product)
            ? Fin.Succ(product)
            : Fin.Fail<Product>(Error.New($"Product not found: {id}")));

    public virtual FinT<IO, Seq<Product>> GetAll() =>
        IO.lift(() => Fin.Succ(toSeq(_products.Values)));

    public virtual FinT<IO, Product> Create(Product product) =>
        IO.lift(() =>
        {
            _products[product.Id] = product;
            return Fin.Succ(product);
        });

    public virtual FinT<IO, Product> Update(Product product) =>
        IO.lift(() =>
        {
            if (!_products.ContainsKey(product.Id))
                return Fin.Fail<Product>(Error.New($"Product not found: {product.Id}"));

            _products[product.Id] = product;
            return Fin.Succ(product);
        });

    public virtual FinT<IO, bool> ExistsByName(ProductName name) =>
        IO.lift(() => Fin.Succ(_products.Values.Any(p => p.Name == name)));
}
```

### 3. 의존성 등록

```csharp
// Infrastructure/Registrations/AdapterRegistration.cs
using Functorium.Abstractions.Registrations;

namespace MyApp.Infrastructure.Registrations;

public static class AdapterRegistration
{
    public static IServiceCollection RegisterInfrastructure(this IServiceCollection services)
    {
        // Repository Pipeline 등록
        services.RegisterScopedAdapterPipeline<IProductRepository, InMemoryProductRepositoryPipeline>();

        return services;
    }
}
```

### 4. Usecase에서 사용

```csharp
// Application/Usecases/GetProductByIdQuery.cs
using Functorium.Applications.Cqrs;
using Functorium.Applications.Linq;

namespace MyApp.Application.Usecases;

public sealed class GetProductByIdQuery
{
    public sealed record Request(string ProductId) : IQueryRequest<Response>;

    public sealed record Response(
        string ProductId,
        string Name,
        decimal Price);

    internal sealed class Usecase(IProductRepository repository)
        : IQueryUsecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(
            Request request,
            CancellationToken cancellationToken)
        {
            if (!ProductId.TryParse(request.ProductId, null, out var productId))
            {
                return FinResponse.Fail<Response>(Error.New("Invalid ProductId"));
            }

            // LINQ 쿼리 표현식으로 함수형 체이닝
            FinT<IO, Response> usecase =
                from product in repository.GetById(productId)
                select new Response(
                    product.Id.ToString(),
                    (string)product.Name,
                    (decimal)product.Price);

            Fin<Response> result = await usecase.Run().RunAsync();
            return result.ToFinResponse();
        }
    }
}
```

---

## FAQ

### Q1. 왜 `virtual` 메서드로 선언해야 하나요?

Pipeline 클래스가 원본 클래스를 상속받아 메서드를 `override`하기 때문입니다. `virtual`이 아니면 Pipeline이 메서드를 감쌀 수 없습니다.

```csharp
// Good
public virtual FinT<IO, Product> GetById(Guid id) { ... }

// Bad - Pipeline이 override 불가
public FinT<IO, Product> GetById(Guid id) { ... }
```

### Q2. `FinT<IO, T>` 대신 `Task<T>`를 사용할 수 없나요?

Pipeline은 `FinT<IO, T>` 반환 타입을 기대합니다. 함수형 에러 처리와 합성을 위해 이 타입을 사용합니다.

```csharp
// Good - 함수형 에러 처리
public virtual FinT<IO, Product> GetById(Guid id)
{
    return IO.lift(() =>
    {
        if (_products.TryGetValue(id, out var product))
            return Fin.Succ(product);
        return Fin.Fail<Product>(Error.New("Not found"));
    });
}

// 비동기 작업이 필요한 경우
public virtual FinT<IO, Product> GetByIdAsync(Guid id)
{
    return IO.liftAsync(async () =>
    {
        var product = await _dbContext.Products.FindAsync(id);
        return product is not null
            ? Fin.Succ(product)
            : Fin.Fail<Product>(Error.New("Not found"));
    });
}
```

### Q3. 테스트에서 Pipeline 없이 원본 클래스를 테스트할 수 있나요?

네, 원본 클래스를 직접 인스턴스화하여 테스트할 수 있습니다:

```csharp
[Fact]
public async Task GetById_ReturnsProduct_WhenExists()
{
    // Arrange - Pipeline 없이 원본 클래스 사용
    var logger = Substitute.For<ILogger<InMemoryProductRepository>>();
    var repository = new InMemoryProductRepository(logger);

    var product = CreateTestProduct();
    await repository.Create(product).Run().RunAsync();

    // Act
    var result = await repository.GetById(product.Id).Run().RunAsync();

    // Assert
    result.IsSucc.ShouldBeTrue();
}
```

### Q4. 생성자 매개변수가 너무 많으면 어떻게 되나요?

Pipeline은 원본 클래스의 생성자 매개변수를 자동으로 포함합니다. 동일한 타입의 매개변수가 여러 개 있으면 Source Generator가 에러를 발생시킵니다.

```csharp
// Bad - 동일 타입 매개변수 충돌
public InMemoryProductRepository(
    ILogger<InMemoryProductRepository> logger1,
    ILogger<InMemoryProductRepository> logger2)  // 타입 충돌!

// Good - 각 매개변수는 고유한 타입
public InMemoryProductRepository(
    ILogger<InMemoryProductRepository> logger,
    IOptions<RepositoryOptions> options)
```

### Q5. 특정 메서드만 Pipeline에서 제외할 수 있나요?

**아니요, 특정 메서드만 제외하는 기능은 지원되지 않습니다.**

`[GeneratePipeline]` 어트리뷰트는 **클래스 단위로만 적용**되며, `IAdapter` 인터페이스의 모든 메서드에 대해 Pipeline 래퍼가 생성됩니다.

#### 중요: virtual 키워드 필수

**`IAdapter` 인터페이스를 구현하는 모든 메서드는 반드시 `virtual`로 선언해야 합니다.** `virtual` 키워드가 없으면 빌드 에러가 발생합니다.

```csharp
[GeneratePipeline]
public class MyRepository : IMyRepository
{
    // ✅ 올바른 선언 - virtual 키워드 필수
    public virtual FinT<IO, Product> GetById(Guid id) { ... }
    public virtual FinT<IO, IReadOnlyList<Product>> GetAll() { ... }

    // ❌ 빌드 에러 - virtual 키워드 누락
    public FinT<IO, int> GetCount() { ... }  // CS0506 에러 발생
}
```

#### 빌드 에러 메시지

`virtual` 키워드 없이 빌드하면 다음과 같은 C# 컴파일러 에러가 발생합니다:

```
error CS0506: 'MyRepositoryPipeline.GetCount()': cannot override inherited member
'MyRepository.GetCount()' because it is not marked virtual, abstract, or override
```

#### 왜 이런 제약이 있나요?

소스 생성기가 생성하는 Pipeline 클래스는 원본 클래스를 상속받아 `override`로 메서드를 재정의합니다:

```csharp
// 소스 생성기가 자동 생성하는 코드
public class MyRepositoryPipeline : MyRepository
{
    // override를 사용하므로 원본 메서드가 virtual이어야 함
    public override FinT<IO, Product> GetById(Guid id) =>
        // Observability 파이프라인 래핑...
}
```

#### 대안

특정 메서드에 대해 Observability를 적용하고 싶지 않다면:
1. 해당 메서드를 별도의 클래스로 분리하세요
2. 분리된 클래스에는 `[GeneratePipeline]` 어트리뷰트를 적용하지 마세요

---

## 참고 문서

| 문서 | 설명 |
|------|------|
| [domain-modeling-overview.md](./domain-modeling-overview.md) | 도메인 모델링 전체 개요 |
| [valueobject-guide.md](./valueobject-guide.md) | Value Object 구현 가이드 |
| [entity-guide.md](./entity-guide.md) | Entity 구현 가이드 |
| [error-guide.md](./error-guide.md) | 에러 시스템 가이드 |
| [unit-testing-guide.md](./unit-testing-guide.md) | 단위 테스트 작성 가이드 |

**외부 참고:**

- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/) - 분산 트레이싱
- [LanguageExt](https://github.com/louthy/language-ext) - 함수형 프로그래밍 라이브러리
