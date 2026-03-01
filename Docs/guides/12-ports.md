# Port 아키텍처와 정의

이 문서는 Functorium 프레임워크에서 Port 아키텍처의 설계 원칙과 Port 인터페이스 정의 방법을 다루는 가이드입니다.
Adapter 구현, Pipeline 생성, DI 등록, 테스트는 별도 문서를 참고하십시오.

## 목차

- [요약](#요약)
- [왜 Port-Adapter 아키텍처인가](#왜-port-adapter-아키텍처인가)
  - [DDD에서 Anti-Corruption Layer의 역할](#ddd에서-anti-corruption-layer의-역할)
  - [도메인 순수성 보호: 외부 의존성 격리](#도메인-순수성-보호-외부-의존성-격리)
  - [Hexagonal Architecture와의 관계](#hexagonal-architecture와의-관계)
- [개요](#개요)
  - [왜 Adapter 패턴을 사용하나요?](#왜-adapter-패턴을-사용하나요)
  - [핵심 특징](#핵심-특징)
  - [Adapter 유형](#adapter-유형)
  - [구현 라이프사이클 개요](#구현-라이프사이클-개요)
  - [단계별 소속 레이어/프로젝트](#단계별-소속-레이어프로젝트)
- [IObservablePort 인터페이스](#iobservableport-인터페이스)
  - [인터페이스 계층 구조](#인터페이스-계층-구조)
  - [RequestCategory 값 가이드](#requestcategory-값-가이드)
- [Activity 1: Port 인터페이스 정의](#activity-1-port-인터페이스-정의)
  - [위치 규칙](#위치-규칙)
  - [Port 정의 체크리스트](#port-정의-체크리스트)
  - [유형별 Port 정의 패턴](#유형별-port-정의-패턴)
  - [Port Request/Response 설계](#port-requestresponse-설계)
  - [Repository 인터페이스 설계 원칙](#repository-인터페이스-설계-원칙)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)
- [참고 문서](#참고-문서)

---

## 요약

### 주요 명령

```csharp
// Port 인터페이스 정의 (IObservablePort 상속)
public interface IProductRepository : IRepository<Product, ProductId>
{
    FinT<IO, bool> Exists(Specification<Product> spec);
}

// External API Port 정의
public interface IExternalPricingService : IObservablePort
{
    FinT<IO, Money> GetPriceAsync(string productCode, CancellationToken ct);
}

// Query Adapter Port 정의
public interface IProductQuery : IQueryPort<Product, ProductSummaryDto> { }
```

### 주요 절차

1. Port 유형 결정 (Repository / External API / Messaging / Query Adapter)
2. 위치 결정 (Repository → Domain Layer, 나머지 → Application Layer)
3. `IObservablePort` 또는 파생 인터페이스 상속
4. 반환 타입을 `FinT<IO, T>`로 정의
5. 매개변수에 도메인 값 객체(VO) 사용
6. Request/Response가 필요하면 인터페이스 내부에 `sealed record`로 정의

### 주요 개념

| 개념 | 설명 |
|------|------|
| Port | Application Layer가 외부 시스템과 통신하기 위한 계약(인터페이스) |
| Adapter | Port 인터페이스의 구현체 |
| `IObservablePort` | 모든 Adapter가 구현하는 기반 인터페이스 (`RequestCategory` 속성 제공) |
| `IRepository<T, TId>` | Aggregate Root 단위 Repository 공통 인터페이스 (CRUD 기본 제공) |
| `IQueryPort<TEntity, TDto>` | 읽기 전용 조회용 제네릭 인터페이스 (DTO 직접 반환) |
| `FinT<IO, T>` | 함수형 반환 타입 (비동기 작업 + 에러 처리 합성) |
| Driving Adapter | 외부에서 애플리케이션을 호출 (Presentation, Mediator가 Port 역할) |
| Driven Adapter | 애플리케이션이 외부를 호출 (Persistence, Infrastructure) |

---

## 왜 Port-Adapter 아키텍처인가

### DDD에서 Anti-Corruption Layer의 역할

도메인 모델은 외부 시스템의 기술적 세부사항으로부터 보호되어야 합니다. Port-Adapter 아키텍처(Hexagonal Architecture)는 도메인과 외부 세계 사이에 명확한 경계를 제공합니다.

### 도메인 순수성 보호: 외부 의존성 격리

| 문제 | Port-Adapter 없이 | Port-Adapter 사용 |
|------|------------------|------------------|
| 의존성 방향 | Application → Infrastructure | Application → Port(인터페이스) ← Adapter |
| 테스트 | DB/API 연결 필요 | Mock으로 대체 가능 |
| 기술 교체 | 전체 수정 필요 | Adapter만 교체 |
| 관찰성 | 수동 로깅/트레이싱 | Pipeline 자동 생성 |

### Hexagonal Architecture와의 관계

Functorium의 Adapter 시스템은 Hexagonal Architecture의 Port와 Adapter 개념을 구현합니다:
- **Port** = 도메인/애플리케이션이 필요로 하는 인터페이스 (`IProductRepository`, `IExternalPricingService`)
- **Adapter** = Port의 구현체 (`InMemoryProductRepository`, `ExternalPricingApiService`)
- **Pipeline** = 소스 생성기가 자동 생성하는 관찰성 래퍼

#### Driving vs Driven Adapter 구분

헥사고날 아키텍처에서 Adapter는 호출 방향에 따라 두 종류로 나뉩니다.

| 구분 | Driving (Primary) | Driven (Secondary) |
|------|-------------------|---------------------|
| **역할** | 외부에서 애플리케이션을 호출 | 애플리케이션이 외부를 호출 |
| **방향** | Outside → Inside | Inside → Outside |
| **Port 위치** | 없음 (Mediator가 대신) | Domain 또는 Application Layer |
| **Functorium 매핑** | `Adapters.Presentation` | `Adapters.Persistence`, `Adapters.Infrastructure` |

#### Presentation Adapter에 Port가 없는 이유

Driving Adapter인 Presentation은 별도의 Port 인터페이스 없이 Mediator를 직접 호출합니다. 이 설계 결정의 근거:

1. **Mediator가 Port 역할을 대신함** — `IMediator.Send()`가 Presentation과 Application 사이의 계약으로 작동
2. **Command/Query가 이미 계약** — Request/Response 타입 자체가 명시적 인터페이스 역할 수행
3. **불필요한 간접 계층 제거** — Driving Adapter에 Port를 도입하면 Mediator와 중복되는 추상화
4. **Driven Adapter와의 비대칭은 의도적** — Driven Adapter는 구현체 교체가 빈번하므로 Port가 필수, Driving Adapter는 교체 시나리오가 희소

```csharp
// Driving Adapter: Port 없이 Mediator 직접 호출
public class CreateProductEndpoint : EndpointBase
{
    public override void Configure(RouteGroupBuilder group) =>
        group.MapPost("/products", HandleAsync);

    private Task<IResult> HandleAsync(
        IMediator mediator,               // ← Mediator가 Port 역할
        CreateProductRequest request) =>
        mediator.Send(new CreateProduct.Command(request.Name, request.Price))
                .ToApiResultAsync();
}

// Driven Adapter: Port(인터페이스) 구현
public interface IProductRepository : IObservablePort  // ← Port
{
    FinT<IO, Product> FindById(ProductId id);
}
```

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
public interface IProductRepository : IObservablePort
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
[GenerateObservablePort]  // 로깅, 트레이싱, 메트릭 자동 생성
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
| **자동 관찰성** | `[GenerateObservablePort]` 속성으로 로깅, 트레이싱, 메트릭 자동 생성 |
| **테스트 용이성** | 인터페이스 기반으로 Mock 객체 쉽게 생성 |

### Adapter 유형

| 유형 | 용도 | RequestCategory | 헥사고날 역할 | 예시 |
|------|------|-----------------|---------------|------|
| **Repository** | 데이터 영속화 | `"Repository"` | Driven | `IProductRepository`, `IOrderRepository` |
| **Messaging** | 메시지 큐/이벤트 | `"Messaging"` | Driven | `IOrderMessaging`, `IInventoryMessaging` |
| **External API** | 외부 서비스 호출 | `"ExternalApi"` | Driven | `IPaymentApiService`, `IWeatherApiService` |
| **Query Adapter** | 읽기 전용 조회 (DTO 직접 반환) | `"QueryAdapter"` | Driven | `IProductQuery`, `IInventoryQuery`, `IProductWithStockQuery` (JOIN) |

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

## IObservablePort 인터페이스

모든 Adapter가 구현해야 하는 기반 인터페이스입니다.

**위치**: `Functorium.Domains.Observabilities.IObservablePort`

```csharp
public interface IObservablePort
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
IObservablePort (인터페이스)
├── string RequestCategory - 관찰성 로그용 카테고리
│
├── IRepository<TAggregate, TId> : IObservablePort   ← Aggregate Root 단위 Repository
│   ├── FinT<IO, TAggregate> Create(TAggregate aggregate)
│   ├── FinT<IO, TAggregate> GetById(TId id)
│   ├── FinT<IO, TAggregate> Update(TAggregate aggregate)
│   ├── FinT<IO, int> Delete(TId id)
│   ├── FinT<IO, Seq<TAggregate>> CreateRange(IReadOnlyList<TAggregate> aggregates)
│   ├── FinT<IO, Seq<TAggregate>> GetByIds(IReadOnlyList<TId> ids)
│   ├── FinT<IO, Seq<TAggregate>> UpdateRange(IReadOnlyList<TAggregate> aggregates)
│   └── FinT<IO, int> DeleteRange(IReadOnlyList<TId> ids)
│   │
│   ├── IProductRepository : IRepository<Product, ProductId>
│   │   ├── FinT<IO, bool> Exists(Specification<Product> spec)  ← 도메인 전용
│   │   └── FinT<IO, Product> GetByIdIncludingDeleted(ProductId id)
│   │
│   └── IOrderRepository : IRepository<Order, OrderId>
│       └── (CRUD는 IRepository에서 상속)
│
├── IUnitOfWork : IObservablePort   ← Application Layer의 트랜잭션 커밋 Port
│   └── FinT<IO, Unit> SaveChanges(CancellationToken)
│
├── IOrderMessaging : IObservablePort
│   ├── FinT<IO, Unit> PublishOrderCreated(OrderCreatedEvent @event)
│   └── FinT<IO, CheckInventoryResponse> CheckInventory(CheckInventoryRequest request)
│
├── IExternalApiService : IObservablePort
│   └── FinT<IO, Response> CallApiAsync(Request request, CancellationToken ct)
│
├── IQueryPort : IObservablePort   ← 비제네릭 마커 (런타임 타입 체크, DI 스캐닝용)
│
└── IQueryPort<TEntity, TDto> : IQueryPort   ← 읽기 전용 조회 (DTO 직접 반환)
    └── FinT<IO, PagedResult<TDto>> Search(Specification<TEntity>, PageRequest, SortExpression)
    │
    ├── IProductQuery : IQueryPort<Product, ProductSummaryDto>
    ├── IProductWithStockQuery : IQueryPort<Product, ProductWithStockDto>  ← JOIN 예제
    └── IInventoryQuery : IQueryPort<Inventory, InventorySummaryDto>
```

**계층 이해하기:**

- **IObservablePort**: 모든 Adapter가 구현하는 기반 인터페이스. `RequestCategory` 속성 제공. 위치: `Functorium.Domains.Observabilities`
- **IRepository\<TAggregate, TId\>**: Aggregate Root 단위 Repository의 공통 인터페이스. `AggregateRoot<TId>` 제약으로 컴파일 타임에 Aggregate 단위 영속화를 강제. 위치: `Functorium.Domains.Repositories`
- **IUnitOfWork**: Application Layer의 트랜잭션 커밋 Port. 위치: `Functorium.Applications.Persistence`
- **IQueryPort** (비제네릭): 런타임 타입 체크, DI 스캐닝용 마커 인터페이스. **IQueryPort\<TEntity, TDto\>**: 제네릭 쿼리 어댑터. 위치: `Functorium.Applications.Queries`
- **Domain Repository**: `IRepository`를 상속하여 도메인 전용 메서드만 추가 선언. Domain Layer에 인터페이스 정의
- **Port Interface**: Application Layer에서 필요한 외부 서비스 인터페이스 정의

### RequestCategory 값 가이드

| 값 | 용도 | 예시 |
|---|------|------|
| `"Repository"` | 데이터베이스/영속화 | EF Core, Dapper, InMemory |
| `"UnitOfWork"` | 트랜잭션 커밋 | EfCoreUnitOfWork, InMemoryUnitOfWork |
| `"Messaging"` | 메시지 큐 | RabbitMQ, Kafka, Azure Service Bus |
| `"ExternalApi"` | HTTP API 호출 | REST API, GraphQL |
| `"QueryAdapter"` | 읽기 전용 조회 | Dapper Query Adapter, InMemory Query Adapter |
| `"Cache"` | 캐시 서비스 | Redis, InMemory Cache |
| `"File"` | 파일 시스템 | 파일 읽기/쓰기 |

---

## Activity 1: Port 인터페이스 정의

Port 인터페이스는 Application Layer가 외부 시스템과 통신하기 위한 **계약(Contract)**입니다.

### 위치 규칙

| 유형 | 위치 | 이유 |
|------|------|------|
| Repository | **Domain Layer** (`Domain/Repositories/`) | 도메인 모델(Entity, VO)에 직접 의존 |
| External API | **Application Layer** (`Application/Ports/`) | 외부 시스템 통신은 Application 관심사 |
| Messaging | **Application Layer** (`Application/Ports/`) 또는 Adapter 내부 | 메시징은 인프라 관심사, 프로젝트 구조에 따라 결정 |
| Query Adapter | **Application Layer** (`Application/Usecases/{Feature}/Ports/`) | 읽기 전용 조회는 Application 관심사 |

> **참고**: Cqrs06Services 튜토리얼에서는 Messaging Port를 `Adapters/Messaging/` 내부에 배치합니다.
> 이는 Port와 Adapter가 동일 프로젝트에 있는 간소화된 구조입니다.

### Port 정의 체크리스트

- [ ] `IObservablePort` 인터페이스를 상속하는가? (Repository인 경우 `IRepository<TAggregate, TId>` 상속)
- [ ] 모든 메서드의 반환 타입이 `FinT<IO, T>`인가?
- [ ] 매개변수와 반환 타입에 도메인 값 객체(VO)를 사용하는가?
- [ ] 비동기 작업이 필요한 메서드에 `CancellationToken` 매개변수가 있는가?
- [ ] 인터페이스 이름이 `I` 접두사 규칙을 따르는가?
- [ ] Request/Response가 인터페이스 내부에 sealed record로 정의되어 있는가? (해당 시)
- [ ] 기술 관심사 타입(Entity, DTO)을 사용하지 않았는가?

> **왜 sealed record인가?** Port의 Request/Response 타입은 `sealed record`로 정의합니다. `sealed`는 상속을 금지하여 계약의 명확성을 보장하고, `record`는 값 기반 동등성과 불변성을 제공하여 Port 경계에서 안전한 데이터 전달을 보장합니다.

### 유형별 Port 정의 패턴

#### Repository Port

도메인 Aggregate Root의 영속성을 담당합니다. **Domain Layer**에 위치합니다.
`IRepository<TAggregate, TId>`를 상속하여 CRUD는 기본 제공받고, 도메인 전용 메서드만 추가합니다.

```csharp
// 파일: {Domain}/AggregateRoots/Products/IProductRepository.cs

using Functorium.Domains.Repositories;  // IRepository

// CRUD (Create, GetById, Update, Delete)는 IRepository에서 상속
// 도메인 전용 메서드만 선언
public interface IProductRepository : IRepository<Product, ProductId>
{
    FinT<IO, bool> Exists(Specification<Product> spec);
    FinT<IO, Product> GetByIdIncludingDeleted(ProductId id);
}
```

> **참조**: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/AggregateRoots/Products/IProductRepository.cs`

**핵심 포인트**:
- 매개변수는 도메인 값 객체 (`ProductId`) 또는 Specification 패턴 사용
- 조회 실패 가능성이 있으면 `Option<T>` 래핑
- 컬렉션 반환은 `Seq<T>` 사용
- 반환 값이 없으면 `Unit` 사용

#### External API Port

외부 시스템 API 호출을 추상화합니다. **Application Layer**에 위치합니다.

```csharp
// 파일: {Application}/Ports/IExternalPricingService.cs

using Functorium.Domains.Observabilities;  // IObservablePort

public interface IExternalPricingService : IObservablePort
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

using Functorium.Domains.Observabilities;  // IObservablePort

public interface IInventoryMessaging : IObservablePort
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

#### Query Adapter Port

읽기 전용 조회를 위한 Adapter로, Aggregate 재구성 없이 DTO를 직접 반환합니다. **Application Layer**에 위치합니다.

프레임워크가 제공하는 `IQueryPort<TEntity, TDto>` 제네릭 인터페이스를 상속하여 정의합니다.

```csharp
// 프레임워크 인터페이스 (Functorium.Applications.Queries)
// 비제네릭 마커 — 런타임 타입 체크, DI 스캐닝, 제네릭 제약에 활용
public interface IQueryPort : IObservablePort { }

// 제네릭 쿼리 어댑터 — Specification 기반 검색, PagedResult 반환
public interface IQueryPort<TEntity, TDto> : IQueryPort
{
    FinT<IO, PagedResult<TDto>> Search(
        Specification<TEntity> spec,   // 전체 조회 시 Specification<TEntity>.All 사용
        PageRequest page,
        SortExpression sort);
}
```

**Search 파라미터 설명:**

| 파라미터 | 타입 | 설명 |
|----------|------|------|
| `spec` | `Specification<TEntity>` | 도메인 Specification 패턴으로 필터 조건 표현. 전체 조회 시 `Specification<TEntity>.All` 사용. 상세는 [10-specifications.md](./10-specifications.md) 참조 |
| `page` | `PageRequest` | Offset 기반 페이지네이션 (`Page`, `PageSize`). 기본값 page=1, pageSize=20, 최대 100 |
| `sort` | `SortExpression` | 다중 필드 정렬 표현. `SortExpression.Empty`이면 Adapter의 `DefaultOrderBy` 사용 |

```csharp
// 단일 테이블 — 파일: {Application}/Usecases/Products/IProductQuery.cs
public interface IProductQuery : IQueryPort<Product, ProductSummaryDto> { }

// JOIN (Product + Inventory) — 파일: {Application}/Usecases/Products/IProductWithStockQuery.cs
public interface IProductWithStockQuery : IQueryPort<Product, ProductWithStockDto> { }
```

> **참조**: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Application/Usecases/Products/IProductQuery.cs`

**핵심 포인트**:
- `IQueryPort<TEntity, TDto>`를 상속 — Search 시그니처가 자동 제공됨
- 반환 타입은 `PagedResult<TDto>` — Aggregate가 아닌 DTO 직접 반환
- `Specification<T>`, `PageRequest`, `SortExpression`으로 조회 조건 표현
- Port 인터페이스는 Usecase 근처(`Application/Usecases/{Feature}/Ports/`)에 위치
- JOIN 쿼리도 동일한 Port 패턴 — `TEntity`는 필터 대상 엔티티, `TDto`는 JOIN 결과 DTO

#### 유형별 비교 테이블

| 항목 | Repository | External API | Messaging | Query Adapter |
|------|-----------|-------------|-----------|-------------|
| 위치 | Domain Layer | Application Layer | Application 또는 Adapter | Application Layer |
| `IObservablePort` 상속 | `IRepository<T, TId>` | `IObservablePort` | `IObservablePort` | `IQueryPort<TEntity, TDto>` |
| 반환 타입 | `FinT<IO, T>` | `FinT<IO, T>` | `FinT<IO, T>` | `FinT<IO, PagedResult<TDto>>` |
| `CancellationToken` | 선택 | 권장 | 선택 | 선택 |
| 값 객체 사용 | 필수 | 권장 | 메시지 DTO 사용 | DTO 직접 반환 |
| 컬렉션 타입 | `Seq<T>` | `Seq<T>`, `Map<K,V>` | 단일 메시지 | `PagedResult<T>` |

### Port Request/Response 설계

Usecase의 Request/Response 패턴과 동일한 이름 패턴을 IObservablePort Port 인터페이스에 적용하여 개념을 단순화하고, 레이어 간 데이터 변환 책임을 명확히 정의합니다.

#### Usecase와 Port의 Request/Response 차이점

| 관점 | Usecase Request/Response | Port Request/Response |
|------|--------------------------|----------------------|
| **위치** | Command/Query 클래스 내부 | IObservablePort 인터페이스 내부 |
| **목적** | 외부 API 경계 정의 | 내부 시스템 간 계약 정의 |
| **타입 선호도** | Primitive (string, Guid, decimal) | 도메인 값 객체 (ProductId, Money) |
| **검증 책임** | FluentValidation 입력 검증 | 값 객체 불변식으로 보장 |
| **직렬화** | JSON 직렬화 필요 (외부 노출) | 직렬화 불필요 (내부 사용) |

> **Usecase Request/Response에서 기본 타입을 사용하는 이유**: Port는 Application-Adapter 경계의 DTO 역할이며, Value Object는 Domain 내부 개념입니다. Usecase Request/Response에 기본 타입(string, int, decimal 등)을 사용하여 Adapter(Presentation)가 Domain 타입에 의존하지 않도록 합니다. Primitive → Value Object 변환은 Usecase 내부에서 수행합니다.

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
public interface IProductRepository : IObservablePort
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
│  │   Request { Name, Description, Price, StockQuantity }  ← JSON 직렬화    │ │
│  │   Response { ProductId, Name, Description, ... }  ← Endpoint 자체 DTO   │ │
│  │                                                                           │ │
│  │   // [변환 책임 A] Endpoint Request → Usecase Request (수동 매핑)         │ │
│  │   var usecaseRequest = new CreateProductCommand.Request(req.Name, ...);  │ │
│  │   // [변환 책임 B] Usecase Response → Endpoint Response                   │ │
│  │   result.Map(r => new Response(r.ProductId, r.Name, ...));               │ │
│  └──────────────────────────────────────────────────────────────────────────┘ │
│                              │                                                 │
└──────────────────────────────┼─────────────────────────────────────────────────┘
                               ▼
┌────────────────────────────────────────────────────────────────────────────────┐
│                           APPLICATION LAYER                                    │
│  ┌──────────────────────────────────────────────────────────────────────────┐ │
│  │ CreateProductCommand.Usecase                                              │ │
│  │   Request { Name: string, Price: decimal } : ICommandRequest<Response>   │ │
│  │                                                                           │ │
│  │   // [변환 책임 1] Usecase 내부에서 값 객체 생성                           │ │
│  │   var productId = ProductId.New();                                        │ │
│  │   var productName = ProductName.Create(request.Name);                     │ │
│  │   var money = Money.Create(request.Price, "KRW");                         │ │
│  └──────────────────────────────────────────────────────────────────────────┘ │
│                              │                                                 │
│  ┌──────────────────────────────────────────────────────────────────────────┐ │
│  │ IProductRepository : IObservablePort  (Port Interface)                             │ │
│  │   Create(Product) → FinT<IO, Product>   ← 도메인 엔티티                  │ │
│  │   GetById(ProductId) → FinT<IO, Product>                                 │ │
│  └──────────────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────┼─────────────────────────────────────────────────┘
                               ▼
┌────────────────────────────────────────────────────────────────────────────────┐
│                      INFRASTRUCTURE / PERSISTENCE LAYER                        │
│  ┌──────────────────────────────────────────────────────────────────────────┐ │
│  │ [GenerateObservablePort]                                                        │ │
│  │ EfCoreProductRepository : IProductRepository                              │ │
│  │   RequestCategory => "Repository"                                         │ │
│  │                                                                           │ │
│  │   // [변환 책임 2] Adapter 내부에서 Persistence Model 변환                │ │
│  │   product.ToModel()   : Product → ProductModel (POCO)                    │ │
│  │   model.ToDomain()    : ProductModel → Product                            │ │
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
| Presentation → Application | **Endpoint** | Endpoint.Request → Usecase.Request (수동 매핑) | 400 Bad Request |
| Application → Presentation | **Endpoint** | `FinResponse<A>.Map<B>()` (Usecase.Response → Endpoint.Response) | — |
| Application 내부 | **Usecase 클래스** | Primitive → 값 객체 | FinResponse.Fail |
| Application → Persistence | **Adapter Mapper** | 도메인 엔티티 → Persistence Model (POCO) | FinT<IO, T> |
| Persistence → Application | **Adapter Mapper** | Persistence Model → 도메인 엔티티 (`CreateFromValidated`) | FinT<IO, T> |
| Infrastructure → External | HttpClient / DbContext | DTO → 외부 프로토콜 | Exception → Fin.Fail |
| Application → Messaging | **Adapter Mapper** | Domain Type → Broker Message (해당 시) | FinT<IO, T> |
| Messaging → Application | **Adapter Mapper** | Broker Message → Domain Type (해당 시) | FinT<IO, T> |

#### Port Request/Response 정의 원칙

**원칙 1: 인터페이스 내부에 sealed record 정의**

```csharp
public interface IProductRepository : IObservablePort
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
sealed record Request(ProductModel Model);  // Persistence 타입
```

**원칙 3: 메서드 수에 따른 네이밍 전략**

단일 메서드 인터페이스:
```csharp
public interface IWeatherApiService : IObservablePort
{
    // 접두사 없이 Request/Response
    sealed record Request(string City, DateTime Date);
    sealed record Response(decimal Temperature, string Condition);

    FinT<IO, Response> GetWeatherAsync(Request request, CancellationToken ct);
}
```

다중 메서드 인터페이스:
```csharp
public interface IProductRepository : IObservablePort
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
public interface IEquipmentApiService : IObservablePort
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

### Repository 인터페이스 설계 원칙

#### 기본 CRUD — `IRepository<TAggregate, TId>`

모든 Repository는 `IRepository<TAggregate, TId>`를 상속합니다. CRUD 메서드는 기본 인터페이스에서 제공되므로 파생 인터페이스에서 재선언하지 않습니다.

```csharp
// Functorium.Domains.Repositories
public interface IRepository<TAggregate, TId> : IObservablePort
    where TAggregate : AggregateRoot<TId>
    where TId : struct, IEntityId<TId>
{
    FinT<IO, TAggregate> Create(TAggregate aggregate);
    FinT<IO, TAggregate> GetById(TId id);
    FinT<IO, TAggregate> Update(TAggregate aggregate);
    FinT<IO, int> Delete(TId id);

    FinT<IO, Seq<TAggregate>> CreateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, Seq<TAggregate>> GetByIds(IReadOnlyList<TId> ids);
    FinT<IO, Seq<TAggregate>> UpdateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, int> DeleteRange(IReadOnlyList<TId> ids);
}
```

#### 파라미터 타입 원칙

| 원칙 | 설명 | 예시 |
|------|------|------|
| **VO 우선** | 원시 타입 대신 값 객체 사용 | `ExistsByName(ProductName name)` |
| **선택적 파라미터** | null 가능한 경우 명시 | `ProductId? excludeId = null` |
| **Entity ID 타입** | 강타입 ID 사용 | `GetById(ProductId id)` |

#### 도메인 전용 메서드 시그니처 패턴

파생 인터페이스에는 `IRepository`에 없는 도메인 전용 메서드만 추가합니다.

```csharp
public interface IProductRepository : IRepository<Product, ProductId>
{
    // 조회 (단일, Optional): 없으면 None
    FinT<IO, Option<Product>> GetByName(ProductName name);

    // 조회 (목록): 빈 Seq도 성공
    FinT<IO, Seq<Product>> GetAll();

    // 존재 확인: bool 반환
    FinT<IO, bool> ExistsByName(ProductName name, ProductId? excludeId = null);
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
| **Delete** | `FinT<IO, int>` | 삭제된 건수 반환 |
| **CreateRange** | `FinT<IO, Seq<Entity>>` | 일괄 생성된 Entity 목록 반환 |
| **GetByIds** | `FinT<IO, Seq<Entity>>` | 일괄 조회된 Entity 목록 반환 |
| **UpdateRange** | `FinT<IO, Seq<Entity>>` | 일괄 업데이트된 Entity 목록 반환 |
| **DeleteRange** | `FinT<IO, int>` | 삭제된 건수 반환 |

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

## 트러블슈팅

### IObservablePort를 상속하지 않아 DI 등록이 실패한다

**원인:** Port 인터페이스가 `IObservablePort` 또는 그 파생 인터페이스(`IRepository<T, TId>`, `IQueryPort<TEntity, TDto>`)를 상속하지 않으면 `RegisterScopedObservablePort` 호출 시 컴파일 에러가 발생합니다.

**해결:**
```csharp
// Before - IObservablePort 미상속
public interface IProductRepository { ... }

// After - IRepository<T, TId>는 IObservablePort를 이미 상속
public interface IProductRepository : IRepository<Product, ProductId> { ... }
```

### Port 메서드 반환 타입이 FinT<IO, T>가 아니어서 Pipeline이 작동하지 않는다

**원인:** Source Generator가 `FinT<IO, T>` 반환 타입만 Pipeline 대상으로 인식합니다. `Task<T>`나 `ValueTask<T>` 등 다른 반환 타입을 사용하면 Pipeline이 생성되지 않습니다.

**해결:**
```csharp
// Before - Pipeline 미생성
Task<Product> GetById(ProductId id);

// After - Pipeline 정상 생성
FinT<IO, Product> GetById(ProductId id);
```

### Repository Port에서 Primitive 타입을 사용하여 도메인 순수성이 깨진다

**원인:** Port 인터페이스의 매개변수에 `Guid`, `string` 등 Primitive 타입을 직접 사용하면 Adapter가 도메인 값 객체를 모르게 되어 도메인 경계가 무너집니다.

**해결:**
```csharp
// Before - Primitive 타입 사용
FinT<IO, Product> GetById(Guid id);

// After - 도메인 값 객체 사용
FinT<IO, Product> GetById(ProductId id);
```

---

## FAQ

### Q1. Repository Port는 왜 Domain Layer에 위치하나요?

Repository는 Aggregate Root의 영속성을 담당하며, 도메인 모델(Entity, Value Object)에 직접 의존합니다. Domain Layer에 인터페이스를 두어야 Application Layer가 도메인 모델을 통해 Repository와 상호작용할 수 있고, 의존성 방향이 도메인을 향하게 됩니다.

### Q2. Driving Adapter(Presentation)에는 왜 Port가 없나요?

Mediator가 Port 역할을 대신합니다. `IMediator.Send()`가 Presentation과 Application 사이의 계약으로 작동하고, Command/Query Request/Response 타입 자체가 명시적 인터페이스 역할을 수행합니다. Driving Adapter에 별도 Port를 도입하면 Mediator와 중복되는 추상화가 됩니다.

### Q3. Port Request/Response를 인터페이스 내부에 sealed record로 정의하는 이유는?

`sealed`는 상속을 금지하여 계약의 명확성을 보장하고, `record`는 값 기반 동등성과 불변성을 제공합니다. 인터페이스 내부에 정의하면 Port와 Request/Response의 응집도가 높아져 관련 타입을 한 곳에서 관리할 수 있습니다.

### Q4. IQueryPort와 IRepository의 차이는 무엇인가요?

`IRepository<T, TId>`는 Aggregate Root 단위 CRUD를 담당하며 도메인 엔티티를 반환합니다. `IQueryPort<TEntity, TDto>`는 읽기 전용 조회를 담당하며 Aggregate 재구성 없이 DTO를 직접 반환합니다. Aggregate 필요 여부가 핵심 판단 기준입니다.

### Q5. External API Port에서 CancellationToken은 필수인가요?

필수는 아니지만 권장됩니다. External API 호출은 네트워크 지연이 있으므로 `CancellationToken`으로 요청 취소를 지원하는 것이 좋습니다. Repository Port에서는 선택 사항입니다.

---

## 참고 문서

| 문서 | 설명 |
|------|------|
| [13-adapters.md](./13-adapters.md) | Adapter 구현 가이드 (Repository, External API, Messaging, Query) |
| [14a-adapter-pipeline-di.md](./14a-adapter-pipeline-di.md) | Pipeline 생성, DI 등록 |
| [14b-adapter-testing.md](./14b-adapter-testing.md) | Adapter 단위 테스트 |
| [04-ddd-tactical-overview.md](./04-ddd-tactical-overview.md) | 도메인 모델링 전체 개요 |
| [11-usecases-and-cqrs.md](./11-usecases-and-cqrs.md) | 유스케이스 구현 (CQRS Command/Query) |
