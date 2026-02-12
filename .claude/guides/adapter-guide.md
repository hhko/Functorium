# Adapter 구현 가이드

이 문서는 Functorium 프레임워크에서 Clean Architecture의 Adapter를 설계하고 구현하는 **통합 가이드**입니다.
설계 원칙과 단계별 구현 활동(Activity)을 하나의 문서에서 다룹니다.

## 목차

- [개요](#개요)
  - [왜 Adapter 패턴을 사용하나요?](#왜-adapter-패턴을-사용하나요)
  - [핵심 특징](#핵심-특징)
  - [Adapter 유형](#adapter-유형)
  - [구현 라이프사이클 개요](#구현-라이프사이클-개요)
  - [단계별 소속 레이어/프로젝트](#단계별-소속-레이어프로젝트)
- [IAdapter 인터페이스](#iadapter-인터페이스)
  - [인터페이스 계층 구조](#인터페이스-계층-구조)
  - [RequestCategory 값 가이드](#requestcategory-값-가이드)
- [Activity 1: Port 인터페이스 정의](#activity-1-port-인터페이스-정의)
  - [1.1 위치 규칙](#11-위치-규칙)
  - [1.2 Port 정의 체크리스트](#12-port-정의-체크리스트)
  - [1.3 유형별 Port 정의 패턴](#13-유형별-port-정의-패턴)
  - [1.4 Port Request/Response 설계](#14-port-requestresponse-설계)
  - [1.5 Repository 인터페이스 설계 원칙](#15-repository-인터페이스-설계-원칙)
- [Activity 2: Adapter 구현](#activity-2-adapter-구현)
  - [2.1 공통 구현 체크리스트](#21-공통-구현-체크리스트)
  - [2.2 Repository Adapter](#22-repository-adapter)
  - [2.3 External API Adapter](#23-external-api-adapter)
  - [2.4 Messaging Adapter](#24-messaging-adapter)
  - [2.5 IO.lift vs IO.liftAsync 판단](#25-iolift-vs-ioliftasync-판단)
  - [2.6 데이터 변환 (Mapper 패턴)](#26-데이터-변환-mapper-패턴)
  - [2.7 에러 처리 통합](#27-에러-처리-통합)
- [Activity 3: Pipeline 생성 확인](#activity-3-pipeline-생성-확인)
  - [3.1 GeneratePipeline 소스 생성기](#31-generatepipeline-소스-생성기)
  - [3.2 생성 파일 확인](#32-생성-파일-확인)
  - [3.3 생성 코드 구조](#33-생성-코드-구조)
  - [3.4 자동 제공 기능 (요약)](#34-자동-제공-기능-요약)
  - [3.5 빌드 에러 대응](#35-빌드-에러-대응)
- [Activity 4: DI 등록](#activity-4-di-등록)
  - [4.1 Registration 클래스 생성](#41-registration-클래스-생성)
  - [4.2 유형별 등록 패턴](#42-유형별-등록-패턴)
  - [4.3 다중 인터페이스 등록](#43-다중-인터페이스-등록)
  - [4.4 DI Lifetime 선택 가이드](#44-di-lifetime-선택-가이드)
  - [4.5 Host Bootstrap 통합](#45-host-bootstrap-통합)
- [Activity 5: 단위 테스트](#activity-5-단위-테스트)
  - [5.1 테스트 원칙 / IO 실행 패턴](#51-테스트-원칙--io-실행-패턴)
  - [5.2 Repository 테스트](#52-repository-테스트)
  - [5.3 External API 테스트](#53-external-api-테스트)
  - [5.4 Messaging 테스트](#54-messaging-테스트)
- [End-to-End Walkthroughs](#end-to-end-walkthroughs)
- [부록](#부록)
  - [A. Clean Architecture 전체 흐름](#a-clean-architecture-전체-흐름)
  - [B. FAQ](#b-faq)
  - [C. Troubleshooting](#c-troubleshooting)
  - [D. Quick Reference 체크리스트](#d-quick-reference-체크리스트)
  - [E. Observability 상세 사양 요약](#e-observability-상세-사양-요약)
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

### 구현 라이프사이클 개요

Adapter 구현은 5단계 활동으로 구성됩니다.

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Adapter 구현 라이프사이클                         │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Activity 1          Activity 2          Activity 3                 │
│  ┌──────────┐        ┌──────────┐        ┌──────────┐              │
│  │ Port     │───────▶│ Adapter  │───────▶│ Pipeline │              │
│  │ 인터페이스│        │ 구현     │        │ 생성 확인│              │
│  │ 정의     │        │          │        │ (자동)   │              │
│  └──────────┘        └──────────┘        └──────────┘              │
│       │                   │                   │                     │
│       ▼                   ▼                   ▼                     │
│  Domain Layer /      Adapter Layer       obj/GeneratedFiles/       │
│  Application Layer                                                  │
│                                                                     │
│  Activity 4          Activity 5                                     │
│  ┌──────────┐        ┌──────────┐                                  │
│  │ DI 등록  │───────▶│ 단위     │                                  │
│  │          │        │ 테스트   │                                  │
│  └──────────┘        └──────────┘                                  │
│       │                   │                                         │
│       ▼                   ▼                                         │
│  Registration /      Tests.Unit/                                    │
│  Program.cs                                                         │
└─────────────────────────────────────────────────────────────────────┘
```

### 단계별 소속 레이어/프로젝트

| Activity | 작업 | 소속 레이어 | 프로젝트 예시 |
|----------|------|-------------|---------------|
| 1 | Port 인터페이스 정의 | Domain / Application | `LayeredArch.Domain`, `LayeredArch.Application` |
| 2 | Adapter 구현 | Adapter | `LayeredArch.Adapters.Persistence`, `LayeredArch.Adapters.Infrastructure` |
| 3 | Pipeline 생성 확인 | (자동 생성) | `obj/GeneratedFiles/` |
| 4 | DI 등록 | Adapter / Host | `{Project}.Adapters.{Layer}`, `LayeredArch` |
| 5 | 단위 테스트 | Test | `{Project}.Tests.Unit` |

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

### 인터페이스 계층 구조

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

### RequestCategory 값 가이드

| 값 | 용도 | 예시 |
|---|------|------|
| `"Repository"` | 데이터베이스/영속화 | EF Core, Dapper, InMemory |
| `"Messaging"` | 메시지 큐 | RabbitMQ, Kafka, Azure Service Bus |
| `"Http"` | HTTP API 호출 | REST API, GraphQL |
| `"Cache"` | 캐시 서비스 | Redis, InMemory Cache |
| `"File"` | 파일 시스템 | 파일 읽기/쓰기 |

---

## Activity 1: Port 인터페이스 정의

Port 인터페이스는 Application Layer가 외부 시스템과 통신하기 위한 **계약(Contract)**입니다.

### 1.1 위치 규칙

| 유형 | 위치 | 이유 |
|------|------|------|
| Repository | **Domain Layer** (`Domain/Repositories/`) | 도메인 모델(Entity, VO)에 직접 의존 |
| External API | **Application Layer** (`Application/Ports/`) | 외부 시스템 통신은 Application 관심사 |
| Messaging | **Application Layer** (`Application/Ports/`) 또는 Adapter 내부 | 메시징은 인프라 관심사, 프로젝트 구조에 따라 결정 |

> **참고**: Cqrs06Services 튜토리얼에서는 Messaging Port를 `Adapters/Messaging/` 내부에 배치합니다.
> 이는 Port와 Adapter가 동일 프로젝트에 있는 간소화된 구조입니다.

### 1.2 Port 정의 체크리스트

- [ ] `IAdapter` 인터페이스를 상속하는가?
- [ ] 모든 메서드의 반환 타입이 `FinT<IO, T>`인가?
- [ ] 매개변수와 반환 타입에 도메인 값 객체(VO)를 사용하는가?
- [ ] 비동기 작업이 필요한 메서드에 `CancellationToken` 매개변수가 있는가?
- [ ] 인터페이스 이름이 `I` 접두사 규칙을 따르는가?
- [ ] Request/Response가 인터페이스 내부에 sealed record로 정의되어 있는가? (해당 시)
- [ ] 기술 관심사 타입(Entity, DTO)을 사용하지 않았는가?

### 1.3 유형별 Port 정의 패턴

#### Repository Port

도메인 Entity의 영속성을 담당합니다. **Domain Layer**에 위치합니다.

```csharp
// 파일: {Domain}/Repositories/IProductRepository.cs

using Functorium.Applications.Observabilities;  // IAdapter

public interface IProductRepository : IAdapter
{
    FinT<IO, Product> Create(Product product);
    FinT<IO, Product> GetById(ProductId id);
    FinT<IO, Option<Product>> GetByName(ProductName name);
    FinT<IO, Seq<Product>> GetAll();
    FinT<IO, Product> Update(Product product);
    FinT<IO, Unit> Delete(ProductId id);
    FinT<IO, bool> ExistsByName(ProductName name, ProductId? excludeId = null);
}
```

> **참조**: `Tests.Hosts/01-SingleHost/LayeredArch.Domain/Repositories/IProductRepository.cs`

**핵심 포인트**:
- 매개변수는 도메인 값 객체 (`ProductId`, `ProductName`) 사용
- 조회 실패 가능성이 있으면 `Option<T>` 래핑
- 컬렉션 반환은 `Seq<T>` 사용
- 반환 값이 없으면 `Unit` 사용

#### External API Port

외부 시스템 API 호출을 추상화합니다. **Application Layer**에 위치합니다.

```csharp
// 파일: {Application}/Ports/IExternalPricingService.cs

using Functorium.Applications.Observabilities;  // IAdapter

public interface IExternalPricingService : IAdapter
{
    FinT<IO, Money> GetPriceAsync(string productCode, CancellationToken cancellationToken);
    FinT<IO, Map<string, Money>> GetPricesAsync(Seq<string> productCodes, CancellationToken cancellationToken);
}
```

> **참조**: `Tests.Hosts/01-SingleHost/LayeredArch.Application/Ports/IExternalPricingService.cs`

**핵심 포인트**:
- 비동기 작업이므로 `CancellationToken` 매개변수 포함
- 메서드 이름에 `Async` 접미사 사용 (내부적으로 `IO.liftAsync` 사용 예정)
- 응답 DTO는 같은 파일 또는 별도 파일에 정의 가능

#### Messaging Port

메시지 브로커(RabbitMQ 등)를 통한 서비스 간 통신을 추상화합니다.

```csharp
// 파일: {Application}/Ports/IInventoryMessaging.cs
// 또는: {Adapters}/Messaging/IInventoryMessaging.cs

using Functorium.Applications.Observabilities;  // IAdapter

public interface IInventoryMessaging : IAdapter
{
    /// Request/Reply 패턴
    FinT<IO, CheckInventoryResponse> CheckInventory(CheckInventoryRequest request);

    /// Fire-and-Forget 패턴
    FinT<IO, Unit> ReserveInventory(ReserveInventoryCommand command);
}
```

> **참조**: `Tutorials/Cqrs06Services/Src/OrderService/Adapters/Messaging/IInventoryMessaging.cs`

**핵심 포인트**:
- Request/Reply: 응답 타입을 반환 (`FinT<IO, TResponse>`)
- Fire-and-Forget: `FinT<IO, Unit>` 반환
- 메시지 타입(`CheckInventoryRequest` 등)은 공유 프로젝트에 정의

#### 유형별 비교 테이블

| 항목 | Repository | External API | Messaging |
|------|-----------|-------------|-----------|
| 위치 | Domain Layer | Application Layer | Application 또는 Adapter |
| `IAdapter` 상속 | 필수 | 필수 | 필수 |
| 반환 타입 | `FinT<IO, T>` | `FinT<IO, T>` | `FinT<IO, T>` |
| `CancellationToken` | 선택 | 권장 | 선택 |
| 값 객체 사용 | 필수 | 권장 | 메시지 DTO 사용 |
| 컬렉션 타입 | `Seq<T>` | `Seq<T>`, `Map<K,V>` | 단일 메시지 |

### 1.4 Port Request/Response 설계

Usecase의 Request/Response 패턴과 동일한 이름 패턴을 IAdapter Port 인터페이스에 적용하여 개념을 단순화하고, 레이어 간 데이터 변환 책임을 명확히 정의합니다.

#### Usecase와 Port의 Request/Response 차이점

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

#### 데이터 변환 흐름 아키텍처

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

#### Port Request/Response 정의 원칙

**원칙 1: 인터페이스 내부에 sealed record 정의**

```csharp
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

**원칙 2: 도메인 값 객체 직접 사용 (기술 독립성)**

```csharp
// ✅ Good - 도메인 값 객체 사용
sealed record Request(ProductId Id, ProductName Name);

// ❌ Bad - Primitive 타입 직접 사용
sealed record Request(Guid Id, string Name);

// ❌ Bad - 기술 관심사 타입 사용
sealed record Request(ProductEntity Entity);  // EF Core 타입
```

**원칙 3: 메서드 수에 따른 네이밍 전략**

단일 메서드 인터페이스:
```csharp
public interface IWeatherApiService : IAdapter
{
    // 접두사 없이 Request/Response
    sealed record Request(string City, DateTime Date);
    sealed record Response(decimal Temperature, string Condition);

    FinT<IO, Response> GetWeatherAsync(Request request, CancellationToken ct);
}
```

다중 메서드 인터페이스:
```csharp
public interface IProductRepository : IAdapter
{
    // {Action}Request / {Action}Response
    sealed record GetByIdRequest(ProductId Id);
    sealed record GetByIdResponse(Product Product);

    sealed record GetAllRequest(int? PageSize = null, int? PageNumber = null);
    sealed record GetAllResponse(Seq<Product> Products, int TotalCount);

    FinT<IO, GetByIdResponse> GetById(GetByIdRequest request);
    FinT<IO, GetAllResponse> GetAll(GetAllRequest request);
}
```

**원칙 4: 중첩 record 가이드라인**

```csharp
public interface IEquipmentApiService : IAdapter
{
    sealed record GetHistoryRequest(
        EquipId EquipId,
        DateRange DateRange,
        EquipmentFilter? Filter);

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

### 1.5 Repository 인터페이스 설계 원칙

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

## Activity 2: Adapter 구현

Adapter는 Port 인터페이스의 **구현체**입니다. `[GeneratePipeline]` 어트리뷰트를 통해 Observability Pipeline이 자동 생성됩니다.

### 2.1 공통 구현 체크리스트

모든 Adapter 구현에 필수인 항목입니다.

- [ ] `[GeneratePipeline]` 어트리뷰트를 클래스에 적용했는가?
- [ ] Port 인터페이스를 구현하는가?
- [ ] `RequestCategory` 프로퍼티를 정의했는가?
- [ ] 모든 인터페이스 메서드에 `virtual` 키워드를 추가했는가?
- [ ] `IO.lift()` 또는 `IO.liftAsync()` 로 비즈니스 로직을 래핑했는가?
- [ ] Mapper 클래스가 `internal`로 선언되어 있는가? (해당 시)

### 2.2 Repository Adapter

Repository Adapter는 데이터 저장소에 대한 CRUD 작업을 구현합니다.

```csharp
// 파일: {Adapters.Persistence}/Repositories/InMemoryProductRepository.cs

using Functorium.Adapters.Errors;
using Functorium.SourceGenerators;
using static Functorium.Adapters.Errors.AdapterErrorType;
using static LanguageExt.Prelude;

[GeneratePipeline]                                    // 1. Pipeline 자동 생성
public class InMemoryProductRepository : IProductRepository  // 2. Port 인터페이스 구현
{
    private static readonly ConcurrentDictionary<ProductId, Product> _products = new();

    public string RequestCategory => "Repository";     // 3. 요청 카테고리

    public InMemoryProductRepository()                 // 4. 생성자
    {
    }

    public virtual FinT<IO, Product> Create(Product product)  // 5. virtual 필수
    {
        return IO.lift(() =>                           // 6. IO.lift (동기)
        {
            _products[product.Id] = product;
            return Fin.Succ(product);                  // 7. 성공 반환
        });
    }

    public virtual FinT<IO, Product> GetById(ProductId id)
    {
        return IO.lift(() =>
        {
            if (_products.TryGetValue(id, out Product? product))
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
            if (!_products.TryRemove(id, out _))
            {
                return AdapterError.For<InMemoryProductRepository>(
                    new NotFound(),
                    id.ToString(),
                    $"상품 ID '{id}'을(를) 찾을 수 없습니다");
            }

            return Fin.Succ(unit);                     // 9. Unit 반환
        });
    }

    // ... 나머지 메서드도 동일 패턴
}
```

> **참조**: `Tests.Hosts/01-SingleHost/LayeredArch.Adapters.Persistence/Repositories/InMemoryProductRepository.cs`

**Repository Adapter 핵심 패턴**:

| 패턴 | 코드 | 설명 |
|------|------|------|
| IO 래핑 | `IO.lift(() => { ... })` | 동기 작업은 `IO.lift` 사용 |
| 성공 | `Fin.Succ(value)` | 성공 값 래핑 |
| 도메인 실패 | `AdapterError.For<T>(errorType, context, message)` | 비즈니스 실패 (not found 등) |
| Unit 반환 | `Fin.Succ(unit)` | 반환 값 없는 작업 (`using static LanguageExt.Prelude`) |
| Optional | `Fin.Succ(Optional(value))` | `Option<T>` 래핑 |
| 컬렉션 | `Fin.Succ(toSeq(values))` | `Seq<T>` 래핑 |

### 2.3 External API Adapter

External API Adapter는 HTTP 클라이언트를 통한 외부 시스템 호출을 구현합니다.

```csharp
// 파일: {Adapters.Infrastructure}/ExternalApis/ExternalPricingApiService.cs

using Functorium.Adapters.Errors;
using Functorium.SourceGenerators;
using static Functorium.Adapters.Errors.AdapterErrorType;

[GeneratePipeline]
public class ExternalPricingApiService : IExternalPricingService
{
    private readonly HttpClient _httpClient;              // 1. HttpClient 주입

    public string RequestCategory => "ExternalApi";       // 2. 요청 카테고리

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
                    new Custom("OperationCancelled"),
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
                    new Custom("UnexpectedException"),
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
                new Custom("RateLimited"), context, "요청 제한에 도달했습니다"),

            HttpStatusCode.ServiceUnavailable => AdapterError.For<ExternalPricingApiService>(
                new ExternalServiceUnavailable("ExternalPricingApi"),
                context, "서비스를 사용할 수 없습니다"),

            _ => AdapterError.For<ExternalPricingApiService, HttpStatusCode>(
                new Custom("HttpError"), response.StatusCode,
                $"API 호출 실패. Status: {response.StatusCode}")
        };
}
```

> **참조**: `Tests.Hosts/01-SingleHost/LayeredArch.Adapters.Infrastructure/ExternalApis/ExternalPricingApiService.cs`

**HTTP 상태 코드 → AdapterErrorType 매핑 참조**:

| HTTP 상태 코드 | AdapterErrorType | 설명 |
|---------------|------------------|------|
| 404 | `new NotFound()` | 리소스 없음 |
| 401 | `new Unauthorized()` | 인증 실패 |
| 403 | `new Forbidden()` | 접근 거부 |
| 429 | `new Custom("RateLimited")` | 요청 제한 초과 |
| 503 | `new ExternalServiceUnavailable(name)` | 서비스 불가 |
| 기타 | `new Custom("HttpError")` | 일반 HTTP 에러 |

**예외 → AdapterErrorType 매핑 참조**:

| 예외 타입 | AdapterErrorType | 설명 |
|----------|------------------|------|
| `HttpRequestException` | `new ConnectionFailed(name)` | 연결 실패 |
| `TaskCanceledException` (사용자) | `new Custom("OperationCancelled")` | 요청 취소 |
| `TaskCanceledException` (타임아웃) | `new Timeout(timespan)` | 응답 시간 초과 |
| `Exception` | `new Custom("UnexpectedException")` | 예상 외 예외 |

### 2.4 Messaging Adapter

Messaging Adapter는 메시지 브로커를 통한 서비스 간 통신을 구현합니다.

```csharp
// 파일: {Adapters}/Messaging/RabbitMqInventoryMessaging.cs

using Functorium.SourceGenerators;
using static LanguageExt.Prelude;
using Wolverine;

[GeneratePipeline]
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

### 2.5 IO.lift vs IO.liftAsync 판단

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

### 2.6 데이터 변환 (Mapper 패턴)

Adapter 내부에서 Port의 도메인 모델과 기술 관심사 DTO 간의 변환을 처리합니다. Mapper 클래스는 반드시 `internal`로 선언합니다.

#### Infrastructure Adapter (HTTP API)

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

#### Persistence Adapter (Repository)

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

### 2.7 에러 처리 통합

#### Error 반환 단순화

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

#### FinT<IO, T>와 AdapterError 연계

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

#### Pipeline의 자동 에러 분류

```
에러 타입                              로그 레벨      메트릭 태그
────────────────────────────────────────────────────────────────
IHasErrorCode + IsExpected  ────────► Warning       error.type: "expected"
IHasErrorCode + IsExceptional ──────► Error         error.type: "exceptional"
ManyErrors ─────────────────────────► Warning/Error error.type: "aggregate"
```

#### 값 객체 공유 전략

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

## Activity 3: Pipeline 생성 확인

`[GeneratePipeline]` 어트리뷰트가 적용된 Adapter를 빌드하면, Source Generator가 자동으로 Pipeline 클래스를 생성합니다.

### 3.1 GeneratePipeline 소스 생성기

`[GeneratePipeline]` 속성을 클래스에 적용하면 Source Generator가 **Pipeline 래퍼 클래스를 자동 생성**합니다.

**위치**: `Functorium.SourceGenerator.GeneratePipelineAttribute`

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class GeneratePipelineAttribute : Attribute { }
```

**변환 다이어그램:**

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

### 3.2 생성 파일 확인

빌드 후 다음 경로에서 생성된 파일을 확인합니다.

```
{Project}/obj/GeneratedFiles/
  └── Functorium.SourceGenerator/
      └── Functorium.SourceGenerators.Generators.AdapterPipelineGenerator.AdapterPipelineGenerator/
          └── {Namespace}.{ClassName}Pipeline.g.cs
```

**예시**:
```
LayeredArch.Adapters.Persistence/obj/GeneratedFiles/.../
  └── Repositories.InMemoryProductRepositoryPipeline.g.cs

LayeredArch.Adapters.Infrastructure/obj/GeneratedFiles/.../
  └── ExternalApis.ExternalPricingApiServicePipeline.g.cs

OrderService/obj/GeneratedFiles/.../
  └── Messaging.RabbitMqInventoryMessagingPipeline.g.cs
```

### 3.3 생성 코드 구조

생성된 Pipeline 클래스는 다음과 같은 구조를 가집니다.

```csharp
// 자동 생성 코드 (예시 구조)
public class InMemoryProductRepositoryPipeline : InMemoryProductRepository
{
    private readonly ActivitySource _activitySource;
    private readonly ILogger<InMemoryProductRepositoryPipeline> _logger;
    private readonly Histogram<double> _durationHistogram;
    // ... 기타 Observability 필드

    public InMemoryProductRepositoryPipeline(
        ActivitySource activitySource,
        ILogger<InMemoryProductRepositoryPipeline> logger,
        IMeterFactory meterFactory,
        IOptions<OpenTelemetryOptions> openTelemetryOptions
        /* + 원본 생성자의 매개변수들 */)
        : base(/* 원본 생성자 매개변수 */)
    {
        // Observability 초기화
    }

    public override FinT<IO, Product> Create(Product product)
    {
        // Activity 시작 → 원본 메서드 호출 → 로깅/메트릭 기록
        return /* 래핑된 호출 */;
    }
}
```

**핵심 구조**:
- 원본 Adapter 클래스를 **상속** (`InMemoryProductRepositoryPipeline : InMemoryProductRepository`)
- `virtual` 메서드를 **override**하여 Observability 로직 추가
- 생성자에 `ActivitySource`, `ILogger`, `IMeterFactory` 등 Observability 의존성 주입
- 원본 생성자 매개변수도 함께 전달

### 3.4 자동 제공 기능 (요약)

Pipeline은 다음 관찰성 기능을 **자동으로** 제공합니다. 모든 필드는 OpenTelemetry 시맨틱 규칙과의 일관성을 위해 `snake_case + dot` 표기법을 사용합니다.

| 기능 | 설명 | 주요 Tag/Field |
|------|------|----------------|
| **분산 트레이싱** | Span 자동 생성 (`{layer} {category} {handler}.{method}`) | `request.layer`, `request.category`, `request.handler`, `request.handler.method`, `response.status`, `response.elapsed` |
| **구조화된 로깅** | 요청/응답/에러 자동 로깅 (EventId 2001-2004) | Request(Info/Debug), Success(Info/Debug), Warning(Expected), Error(Exceptional) |
| **메트릭 수집** | Counter + Histogram 자동 기록 | `adapter.{category}.requests`, `adapter.{category}.responses`, `adapter.{category}.duration` |
| **에러 분류** | Expected/Exceptional/Aggregate 자동 분류 | `error.type`, `error.code` |

**로그 레벨 규칙:**

| 이벤트 | EventId | 로그 레벨 | 조건 |
|--------|---------|----------|------|
| Request | 2001 | Information / Debug | Debug는 파라미터 값 포함 |
| Response Success | 2002 | Information / Debug | Debug는 반환값 포함 |
| Response Warning | 2003 | Warning | `error.IsExpected == true` |
| Response Error | 2004 | Error | `error.IsExceptional == true` |

**에러 분류 규칙:**

| Error Case | `error.type` | `error.code` | 로그 레벨 |
|------------|--------------|--------------|----------|
| `IHasErrorCode` + `IsExpected` | `"expected"` | 에러 코드 | Warning |
| `IHasErrorCode` + `IsExceptional` | `"exceptional"` | 에러 코드 | Error |
| `ManyErrors` | `"aggregate"` | 첫 번째 에러 코드 | Warning/Error |
| `Expected` (LanguageExt) | `"expected"` | 타입 이름 | Warning |
| `Exceptional` (LanguageExt) | `"exceptional"` | 타입 이름 | Error |

> **상세 사양**: 트레이싱 Tag 구조, 로그 Message Template, 메트릭 Instrument 정의 등 상세 내용은 [observability-spec.md](./observability-spec.md)를 참조하세요.

### 3.5 빌드 에러 대응

| 에러 | 증상 | 원인 | 해결 |
|------|------|------|------|
| CS0506 | `cannot override because it is not virtual` | 메서드에 `virtual` 키워드 누락 | 모든 인터페이스 메서드에 `virtual` 추가 |
| Pipeline 클래스 미생성 | `obj/GeneratedFiles/`에 파일 없음 | `[GeneratePipeline]` 어트리뷰트 누락 | 클래스에 어트리뷰트 추가 |
| 생성자 매개변수 충돌 | Source Generator 에러 | 생성자 매개변수 타입이 Observability 타입과 충돌 | 생성자 매개변수에 고유 타입 사용 |
| 네임스페이스 누락 | `using` 에러 | Functorium 패키지 참조 누락 | `Functorium.SourceGenerators` NuGet 패키지 추가 |

---

## Activity 4: DI 등록

생성된 Pipeline 클래스를 DI 컨테이너에 등록합니다.

### 4.1 Registration 클래스 생성

**위치 규칙**: `{Project}.Adapters.{Layer}/Abstractions/Registrations/`

**네이밍 규칙**: `Adapter{Layer}Registration`

```csharp
// 파일: {Adapters.Persistence}/Abstractions/Registrations/AdapterPersistenceRegistration.cs

using Functorium.Abstractions.Registrations;

public static class AdapterPersistenceRegistration
{
    public static IServiceCollection RegisterAdapterPersistence(
        this IServiceCollection services)
    {
        // Pipeline 등록
        services.RegisterScopedAdapterPipeline<
            IProductRepository,
            InMemoryProductRepositoryPipeline>();

        return services;
    }

    public static IApplicationBuilder UseAdapterPersistence(
        this IApplicationBuilder app)
    {
        return app;
    }
}
```

> **참조**: `Tests.Hosts/01-SingleHost/LayeredArch.Adapters.Persistence/Abstractions/Registrations/AdapterPersistenceRegistration.cs`

### 4.2 유형별 등록 패턴

#### Repository 등록

```csharp
// 단일 인터페이스 등록
services.RegisterScopedAdapterPipeline<
    IProductRepository,                      // Port 인터페이스
    InMemoryProductRepositoryPipeline>();     // 생성된 Pipeline
```

#### External API 등록

External API Adapter는 HttpClient와 Pipeline 두 가지를 등록해야 합니다.

```csharp
// 1단계: HttpClient 등록
services.AddHttpClient<ExternalPricingApiServicePipeline>(client =>
{
    client.BaseAddress = new Uri(configuration["ExternalApi:BaseUrl"]
        ?? "https://api.example.com");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// 2단계: Pipeline 등록
services.RegisterScopedAdapterPipeline<
    IExternalPricingService,
    ExternalPricingApiServicePipeline>();
```

> **참고**: `HttpClient`는 Pipeline 클래스 타입으로 등록합니다. Pipeline이 원본 Adapter를 상속하므로 생성자의 `HttpClient` 매개변수를 그대로 받습니다.

#### Messaging 등록

```csharp
// Pipeline 등록 (MessageBus는 별도 등록 필요)
services.RegisterScopedAdapterPipeline<
    IInventoryMessaging,
    RabbitMqInventoryMessagingPipeline>();
```

> **참조**: `Tutorials/Cqrs06Services/Src/OrderService/Program.cs` (57행)

### 4.3 다중 인터페이스 등록

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

### 4.4 DI Lifetime 선택 가이드

| Lifetime | 사용 시점 | 주의사항 |
|----------|----------|---------|
| **Scoped** (기본) | Repository, External API, Messaging | HTTP 요청 내 동일 인스턴스 공유 |
| **Transient** | 상태 없는 가벼운 Adapter | 매번 새 인스턴스 생성 (메모리 주의) |
| **Singleton** | 스레드 안전한 읽기 전용 Adapter | 상태 변경 불가, 스레드 안전성 보장 필요 |

> **권장**: 특별한 이유가 없으면 **Scoped**를 사용하세요.

**등록 API 요약:**

| 등록 API | Lifetime | 용도 |
|----------|----------|------|
| `RegisterScopedAdapterPipeline<TService, TImpl>()` | Scoped | HTTP 요청당 1개 (기본 권장) |
| `RegisterTransientAdapterPipeline<TService, TImpl>()` | Transient | 매 요청마다 새 인스턴스 |
| `RegisterSingletonAdapterPipeline<TService, TImpl>()` | Singleton | 애플리케이션 전체 1개 |
| `RegisterScopedAdapterPipelineFor<T1, T2, TImpl>()` | Scoped | 2개 인터페이스 → 1개 구현체 |
| `RegisterScopedAdapterPipelineFor<T1, T2, T3, TImpl>()` | Scoped | 3개 인터페이스 → 1개 구현체 |

> **참조**: `Src/Functorium/Abstractions/Registrations/AdapterPipelineRegistration.cs`

### 4.5 Host Bootstrap 통합

`Program.cs`에서 레이어별 Registration을 호출합니다.

```csharp
// 파일: {Host}/Program.cs

var builder = WebApplication.CreateBuilder(args);

// 레이어별 서비스 등록
builder.Services
    .RegisterAdapterPresentation()
    .RegisterAdapterPersistence()
    .RegisterAdapterInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseAdapterInfrastructure()
   .UseAdapterPersistence()
   .UseAdapterPresentation();

app.Run();
```

> **참조**: `Tests.Hosts/01-SingleHost/LayeredArch/Program.cs`

**핵심 포인트**:
- `RegisterAdapter{Layer}()`: `IServiceCollection` 확장 메서드로 서비스 등록
- `UseAdapter{Layer}()`: `IApplicationBuilder` 확장 메서드로 미들웨어 설정
- 등록 순서는 의존성 방향에 따라 결정 (Presentation → Persistence → Infrastructure)

---

## Activity 5: 단위 테스트

Adapter의 단위 테스트는 **원본 클래스를 직접 테스트**합니다 (Pipeline이 아님).

### 5.1 테스트 원칙 / IO 실행 패턴

| 원칙 | 설명 |
|------|------|
| 테스트 대상 | 원본 Adapter 클래스 (Pipeline 아님) |
| 패턴 | AAA (Arrange-Act-Assert) |
| 네이밍 | `T1_T2_T3` (메서드명_시나리오_기대결과) |
| 실행 | `.Run().RunAsync()` 또는 `Task.Run(() => ioResult.Run())` |
| 단언 라이브러리 | Shouldly |
| Mock 라이브러리 | NSubstitute |

> **참고**: 테스트 규칙 상세는 [unit-testing-guide.md](./unit-testing-guide.md)를 참조하세요.

**IO 실행 패턴** - `FinT<IO, T>` 반환값을 테스트에서 실행하는 패턴:

```csharp
// Act
var ioFin = adapter.MethodUnderTest(args);   // FinT<IO, T> 반환
var ioResult = ioFin.Run();                  // IO<Fin<T>> 변환
var result = await Task.Run(() => ioResult.Run());  // Fin<T> 실행

// Assert
result.IsSucc.ShouldBeTrue();
```

### 5.2 Repository 테스트

Repository Adapter는 외부 의존성이 없으므로 (In-Memory 구현의 경우) 직접 인스턴스를 생성하여 테스트합니다.

```csharp
// 파일: Tests/{Project}.Tests.Unit/LayerTests/Adapters/InMemoryProductRepositoryTests.cs

public sealed class InMemoryProductRepositoryTests
{
    [Fact]
    public async Task Create_ReturnsProduct_WhenProductIsValid()
    {
        // Arrange
        var repository = new InMemoryProductRepository();
        var product = Product.Create(
            ProductId.Create(Guid.NewGuid()),
            ProductName.Create("테스트 상품"),
            Money.Create(10000m));

        // Act
        var ioFin = repository.Create(product);
        var ioResult = ioFin.Run();
        var result = await Task.Run(() => ioResult.Run());

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Match(
            Succ: created =>
            {
                created.Id.ShouldBe(product.Id);
                created.Name.ShouldBe(product.Name);
            },
            Fail: _ => throw new Exception("Should be success"));
    }

    [Fact]
    public async Task GetById_ReturnsFail_WhenProductNotFound()
    {
        // Arrange
        var repository = new InMemoryProductRepository();
        var nonExistentId = ProductId.Create(Guid.NewGuid());

        // Act
        var ioFin = repository.GetById(nonExistentId);
        var ioResult = ioFin.Run();
        var result = await Task.Run(() => ioResult.Run());

        // Assert
        result.IsFail.ShouldBeTrue();
    }
}
```

### 5.3 External API 테스트

External API Adapter는 `HttpClient`를 Mock하여 테스트합니다.

```csharp
// 파일: Tests/{Project}.Tests.Unit/LayerTests/Adapters/ExternalPricingApiServiceTests.cs

public sealed class ExternalPricingApiServiceTests
{
    [Fact]
    public async Task GetPriceAsync_ReturnsMoney_WhenApiReturnsSuccess()
    {
        // Arrange
        var priceResponse = new ExternalPriceResponse(
            "PROD-001", 29900m, "KRW", DateTime.UtcNow.AddHours(1));

        var handler = new MockHttpMessageHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(priceResponse));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.com")
        };

        var service = new ExternalPricingApiService(httpClient);

        // Act
        var ioFin = service.GetPriceAsync("PROD-001", CancellationToken.None);
        var ioResult = ioFin.Run();
        var result = await Task.Run(() => ioResult.Run());

        // Assert
        result.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public async Task GetPriceAsync_ReturnsFail_WhenApiReturns404()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(HttpStatusCode.NotFound);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.com")
        };

        var service = new ExternalPricingApiService(httpClient);

        // Act
        var ioFin = service.GetPriceAsync("INVALID", CancellationToken.None);
        var ioResult = ioFin.Run();
        var result = await Task.Run(() => ioResult.Run());

        // Assert
        result.IsFail.ShouldBeTrue();
    }

    // HttpClient Mock을 위한 도우미 클래스
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string? _content;

        public MockHttpMessageHandler(
            HttpStatusCode statusCode, string? content = null)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode);
            if (_content is not null)
            {
                response.Content = new StringContent(
                    _content, System.Text.Encoding.UTF8, "application/json");
            }
            return Task.FromResult(response);
        }
    }
}
```

### 5.4 Messaging 테스트

Messaging Adapter는 `IMessageBus`를 NSubstitute로 Mock하여 테스트합니다.

```csharp
// 파일: Tests/{Project}.Tests.Unit/LayerTests/Adapters/RabbitMqInventoryMessagingTests.cs

public sealed class RabbitMqInventoryMessagingTests
{
    [Fact]
    public async Task CheckInventory_SendsRequest_WhenRequestIsValid()
    {
        // Arrange
        var request = new CheckInventoryRequest(Guid.NewGuid(), Quantity: 5);
        var expectedResponse = new CheckInventoryResponse(
            ProductId: request.ProductId,
            IsAvailable: true,
            AvailableQuantity: 10);

        var messageBus = Substitute.For<IMessageBus>();
        messageBus.InvokeAsync<CheckInventoryResponse>(
                request, Arg.Any<CancellationToken>(), Arg.Any<TimeSpan?>())
            .Returns(expectedResponse);

        var messaging = new RabbitMqInventoryMessaging(messageBus);

        // Act
        var ioFin = messaging.CheckInventory(request);
        var ioResult = ioFin.Run();
        var result = await Task.Run(() => ioResult.Run());

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Match(
            Succ: response =>
            {
                response.ProductId.ShouldBe(request.ProductId);
                response.IsAvailable.ShouldBeTrue();
                response.AvailableQuantity.ShouldBe(10);
            },
            Fail: _ => throw new Exception("Should be success"));
    }

    [Fact]
    public async Task ReserveInventory_SendsCommand_WhenCommandIsValid()
    {
        // Arrange
        var command = new ReserveInventoryCommand(
            OrderId: Guid.NewGuid(),
            ProductId: Guid.NewGuid(),
            Quantity: 5);

        var messageBus = Substitute.For<IMessageBus>();
        messageBus.SendAsync(command)
            .Returns(ValueTask.CompletedTask);

        var messaging = new RabbitMqInventoryMessaging(messageBus);

        // Act
        var ioFin = messaging.ReserveInventory(command);
        var ioResult = ioFin.Run();
        var result = await Task.Run(() => ioResult.Run());

        // Assert
        result.IsSucc.ShouldBeTrue();
        await messageBus.Received(1).SendAsync(command);
    }
}
```

> **참조**: `Tutorials/Cqrs06Services/Tests/OrderService.Tests.Unit/LayerTests/Adapters/RabbitMqInventoryMessagingTests.cs`

---

## End-to-End Walkthroughs

각 Adapter 유형의 전체 구현 과정을 요약합니다. 각 단계의 상세 코드는 해당 Activity 섹션을 참조하세요.

### Repository (01-SingleHost IProductRepository)

| Step | Activity | 파일 | 핵심 작업 |
|------|----------|------|----------|
| 1 | Port 정의 | `LayeredArch.Domain/Repositories/IProductRepository.cs` | `: IAdapter`, `FinT<IO, T>` 반환, 도메인 VO 매개변수 |
| 2 | Adapter 구현 | `LayeredArch.Adapters.Persistence/Repositories/InMemoryProductRepository.cs` | `[GeneratePipeline]`, `virtual`, `IO.lift`, `AdapterError.For<T>` |
| 3 | Pipeline 확인 | `obj/GeneratedFiles/.../Repositories.InMemoryProductRepositoryPipeline.g.cs` | 빌드 후 자동 생성 |
| 4 | DI 등록 | `AdapterPersistenceRegistration.cs` → `Program.cs` | `RegisterScopedAdapterPipeline<IProductRepository, ...Pipeline>()` |
| 5 | 테스트 | `InMemoryProductRepositoryTests.cs` | 원본 클래스 직접 테스트, [5.2](#52-repository-테스트) 참조 |

### External API (01-SingleHost IExternalPricingService)

| Step | Activity | 파일 | 핵심 작업 |
|------|----------|------|----------|
| 1 | Port 정의 | `LayeredArch.Application/Ports/IExternalPricingService.cs` | `CancellationToken` 포함, `Async` 접미사 |
| 2 | Adapter 구현 | `LayeredArch.Adapters.Infrastructure/ExternalApis/ExternalPricingApiService.cs` | `IO.liftAsync`, `HandleHttpError<T>`, try/catch 패턴 |
| 3 | Pipeline 확인 | `obj/GeneratedFiles/.../ExternalApis.ExternalPricingApiServicePipeline.g.cs` | 빌드 후 자동 생성 |
| 4 | DI 등록 | `AdapterInfrastructureRegistration.cs` → `Program.cs` | `AddHttpClient<...Pipeline>()` + `RegisterScopedAdapterPipeline` |
| 5 | 테스트 | `ExternalPricingApiServiceTests.cs` | `MockHttpMessageHandler` 사용, [5.3](#53-external-api-테스트) 참조 |

### Messaging (Cqrs06Services IInventoryMessaging)

| Step | Activity | 파일 | 핵심 작업 |
|------|----------|------|----------|
| 1 | Port 정의 | `OrderService/Adapters/Messaging/IInventoryMessaging.cs` | Request/Reply + Fire-and-Forget |
| 2 | Adapter 구현 | `OrderService/Adapters/Messaging/RabbitMqInventoryMessaging.cs` | `IMessageBus` 주입, `InvokeAsync` / `SendAsync` |
| 3 | Pipeline 확인 | `obj/GeneratedFiles/.../Messaging.RabbitMqInventoryMessagingPipeline.g.cs` | 빌드 후 자동 생성 |
| 4 | DI 등록 | `OrderService/Program.cs` (57행) | `RegisterScopedAdapterPipeline`, MessageBus는 Wolverine 별도 등록 |
| 5 | 테스트 | `RabbitMqInventoryMessagingTests.cs` | NSubstitute로 `IMessageBus` Mock, [5.4](#54-messaging-테스트) 참조 |

---

## 부록

### A. Clean Architecture 전체 흐름

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

**Usecase에서 Adapter 사용 예시:**

```csharp
// Application/Usecases/GetProductByIdQuery.cs
public sealed class GetProductByIdQuery
{
    public sealed record Request(string ProductId) : IQueryRequest<Response>;
    public sealed record Response(string ProductId, string Name, decimal Price);

    internal sealed class Usecase(IProductRepository repository)
        : IQueryUsecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(
            Request request, CancellationToken cancellationToken)
        {
            if (!ProductId.TryParse(request.ProductId, null, out var productId))
                return FinResponse.Fail<Response>(Error.New("Invalid ProductId"));

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

### B. FAQ

#### Q1. 왜 `virtual` 메서드로 선언해야 하나요?

Pipeline 클래스가 원본 클래스를 상속받아 메서드를 `override`하기 때문입니다. `virtual`이 아니면 Pipeline이 메서드를 감쌀 수 없습니다.

```csharp
// Good
public virtual FinT<IO, Product> GetById(Guid id) { ... }

// Bad - Pipeline이 override 불가
public FinT<IO, Product> GetById(Guid id) { ... }
```

#### Q2. `FinT<IO, T>` 대신 `Task<T>`를 사용할 수 없나요?

Pipeline은 `FinT<IO, T>` 반환 타입을 기대합니다. 함수형 에러 처리와 합성을 위해 이 타입을 사용합니다.

```csharp
// 동기 작업
public virtual FinT<IO, Product> GetById(Guid id)
{
    return IO.lift(() =>
    {
        if (_products.TryGetValue(id, out var product))
            return Fin.Succ(product);
        return Fin.Fail<Product>(Error.New("Not found"));
    });
}

// 비동기 작업
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

#### Q3. 테스트에서 Pipeline 없이 원본 클래스를 테스트할 수 있나요?

네, 원본 클래스를 직접 인스턴스화하여 테스트할 수 있습니다. [Activity 5](#activity-5-단위-테스트) 섹션의 테스트 예제를 참조하세요.

#### Q4. 생성자 매개변수가 너무 많으면 어떻게 되나요?

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

#### Q5. 특정 메서드만 Pipeline에서 제외할 수 있나요?

**아니요, 특정 메서드만 제외하는 기능은 지원되지 않습니다.**

`[GeneratePipeline]` 어트리뷰트는 **클래스 단위로만 적용**되며, `IAdapter` 인터페이스의 모든 메서드에 대해 Pipeline 래퍼가 생성됩니다. `virtual` 키워드가 없으면 빌드 에러(`CS0506`)가 발생합니다.

**대안**: 특정 메서드에 대해 Observability를 적용하고 싶지 않다면, 해당 메서드를 별도의 클래스로 분리하고 `[GeneratePipeline]` 어트리뷰트를 적용하지 마세요.

### C. Troubleshooting

| 문제 | 증상 | 해결 |
|------|------|------|
| `virtual` 누락 | `CS0506: cannot override because it is not virtual` | 모든 인터페이스 메서드에 `virtual` 키워드 추가 |
| `[GeneratePipeline]` 누락 | Pipeline 클래스가 `obj/GeneratedFiles/`에 생성되지 않음 | 클래스에 `[GeneratePipeline]` 어트리뷰트 추가 |
| `IAdapter` 미상속 | `RegisterScopedAdapterPipeline` 컴파일 에러 | Port 인터페이스에 `: IAdapter` 상속 추가 |
| 생성자 타입 충돌 | Source Generator 에러 또는 잘못된 Pipeline 생성 | 생성자 매개변수에 고유 타입 사용 (동일 타입 중복 금지) |
| Pipeline DI 미등록 | `InvalidOperationException: No service for type 'IXxx'` | `RegisterScopedAdapterPipeline<IXxx, XxxPipeline>()` 호출 확인 |
| Pipeline 타입 미발견 | `The type or namespace name 'XxxPipeline' could not be found` | `dotnet build` 실행 후 재시도 (Source Generator 트리거) |
| `RequestCategory` 누락 | 컴파일 에러 | `public string RequestCategory => "카테고리명";` 추가 |
| `IO.lift` 내 `await` 사용 | 컴파일 에러 (async 불가) | `IO.liftAsync(async () => ...)` 로 변경 |
| HttpClient 미등록 | `No service for type 'HttpClient'` | `services.AddHttpClient<XxxPipeline>(...)` 등록 |

### D. Quick Reference 체크리스트

#### Port 인터페이스

- [ ] `IAdapter` 상속
- [ ] 반환 타입: `FinT<IO, T>`
- [ ] 도메인 VO 사용 (Repository)
- [ ] `CancellationToken` (External API)
- [ ] 위치: Repository → Domain, External API → Application

#### Adapter 구현

- [ ] `[GeneratePipeline]` 어트리뷰트
- [ ] Port 인터페이스 구현
- [ ] `RequestCategory` 프로퍼티
- [ ] 모든 메서드에 `virtual`
- [ ] `IO.lift` (동기) 또는 `IO.liftAsync` (비동기)
- [ ] 성공: `Fin.Succ(value)`
- [ ] 실패: `AdapterError.For<T>(errorType, context, message)`
- [ ] 예외: `AdapterError.FromException<T>(errorType, ex)`

#### DI 등록

- [ ] Registration 클래스 생성 (`Adapter{Layer}Registration`)
- [ ] `RegisterScopedAdapterPipeline<IPort, AdapterPipeline>()`
- [ ] HttpClient 등록 (External API)
- [ ] `Program.cs`에서 Registration 호출

#### 단위 테스트

- [ ] 원본 Adapter 클래스 테스트 (Pipeline 아님)
- [ ] AAA 패턴
- [ ] `T1_T2_T3` 네이밍
- [ ] `.Run()` → `Task.Run(() => ioResult.Run())` 실행
- [ ] 성공/실패 케이스 모두 테스트

### E. Observability 상세 사양 요약

Pipeline이 자동 제공하는 Observability 기능의 요약입니다. 상세 사양은 [observability-spec.md](./observability-spec.md)를 참조하세요.

**Span 이름 패턴**: `{layer} {category} {handler}.{method}`

**Tracing Tag 구조:**

| Tag Key | Success | Failure |
|---------|---------|---------|
| `request.layer` | "adapter" | "adapter" |
| `request.category` | 카테고리명 | 카테고리명 |
| `request.handler` | 핸들러명 | 핸들러명 |
| `request.handler.method` | 메서드명 | 메서드명 |
| `response.status` | "success" | "failure" |
| `response.elapsed` | 초(s) | 초(s) |
| `error.type` | - | "expected" / "exceptional" / "aggregate" |
| `error.code` | - | 에러 코드 |

**메트릭 Instruments:**

| Instrument | 이름 패턴 | 타입 | Unit |
|------------|----------|------|------|
| requests | `adapter.{category}.requests` | Counter | `{request}` |
| responses | `adapter.{category}.responses` | Counter | `{response}` |
| duration | `adapter.{category}.duration` | Histogram | `s` |

**에러 객체 (`@error`) 구조:**

```json
// Expected Error
{ "ErrorCode": "Product.NotFound", "Message": "Entity not found" }

// Exceptional Error
{ "Message": "Database connection failed" }

// Aggregate Error
{ "Errors": [{ "ErrorCode": "Validation.Required", "Message": "Name is required" }] }
```

---

## 참고 문서

| 문서 | 설명 |
|------|------|
| [domain-modeling-overview.md](./domain-modeling-overview.md) | 도메인 모델링 전체 개요 |
| [valueobject-guide.md](./valueobject-guide.md) | Value Object 구현 가이드 |
| [entity-guide.md](./entity-guide.md) | Entity 구현 가이드 |
| [usecase-implementation-guide.md](./usecase-implementation-guide.md) | 유스케이스 구현 (CQRS Command/Query) |
| [error-guide.md](./error-guide.md) | 에러 시스템 가이드 |
| [error-testing-guide.md](./error-testing-guide.md) | 에러 테스트 패턴 |
| [unit-testing-guide.md](./unit-testing-guide.md) | 단위 테스트 작성 가이드 |
| [observability-spec.md](./observability-spec.md) | Observability 사양 (트레이싱, 로깅, 메트릭 상세) |

**외부 참고:**

- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/) - 분산 트레이싱
- [LanguageExt](https://github.com/louthy/language-ext) - 함수형 프로그래밍 라이브러리
