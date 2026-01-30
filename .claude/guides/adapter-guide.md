# Adapter 구현 가이드

이 문서는 Functorium 프레임워크의 `Functorium.Applications.Observabilities` 및 `Functorium.Adapters.SourceGenerator` 네임스페이스를 사용하여 Clean Architecture의 Adapter를 구현하는 방법을 설명합니다.

## 목차

- [개요](#개요)
- [인터페이스 계층 구조](#인터페이스-계층-구조)
- [IAdapter 인터페이스](#iadapter-인터페이스)
- [GeneratePipeline 소스 생성기](#generatepipeline-소스-생성기)
  - [생성되는 Pipeline 클래스](#생성되는-pipeline-클래스)
  - [자동 제공 기능](#자동-제공-기능)
- [구현 패턴](#구현-패턴)
  - [Repository 구현](#repository-구현)
  - [Messaging 구현](#messaging-구현)
  - [외부 API 서비스 구현](#외부-api-서비스-구현)
- [의존성 등록](#의존성-등록)
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

Pipeline은 다음 관찰성 기능을 **자동으로** 제공합니다:

#### 1. 분산 트레이싱 (OpenTelemetry)

```
Span 이름: adapter.repository.InMemoryProductRepository.GetById

Tags:
├── request.layer: "adapter"
├── request.category: "repository"
├── request.handler: "InMemoryProductRepository"
├── request.handler.method: "GetById"
├── response.status: "success" | "failure"
├── response.elapsed: "0.0234" (초)
├── error.type: "expected" | "exceptional" | "aggregate"
└── error.code: "DomainErrors.Product.NotFound"
```

#### 2. 구조화된 로깅

**Debug 레벨** - 상세 정보:
```
[DBG] Request  | adapter.repository.InMemoryProductRepository.GetById | id=550e8400-e29b-41d4-a716-446655440000
[DBG] Response | adapter.repository.InMemoryProductRepository.GetById | elapsed=23ms | product={Id=..., Name=...}
```

**Information 레벨** - 요약:
```
[INF] Response | adapter.repository.InMemoryProductRepository.GetById | elapsed=23ms | status=success
```

**Error 레벨** - 실패 정보:
```
[ERR] Response | adapter.repository.InMemoryProductRepository.GetById | elapsed=5ms | errorType=expected | errorCode=DomainErrors.Product.NotFound
```

#### 3. 메트릭 수집

```
Meter: {service.namespace}.adapter.repository

Counters:
├── adapter.repository.requests (요청 수)
└── adapter.repository.responses (응답 수, status 태그)

Histograms:
└── adapter.repository.duration (실행 시간, 초 단위)
```

#### 4. 에러 분류

Pipeline은 에러를 세 가지로 분류합니다:

| 에러 타입 | 설명 | 예시 |
|----------|------|------|
| `expected` | 예상된 비즈니스 에러 | "제품을 찾을 수 없음", "재고 부족" |
| `exceptional` | 예외적 시스템 에러 | `NullReferenceException`, 타임아웃 |
| `aggregate` | 복합 에러 (여러 개) | Validation 실패 시 여러 에러 |

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
                        AdapterErrors.ApiCallFailed(response.StatusCode, errorContent));
                }

                var result = await response.Content
                    .ReadFromJsonAsync<PaymentResponse>(cancellationToken: cancellationToken);

                return result is not null
                    ? Fin.Succ(result)
                    : Fin.Fail<PaymentResponse>(AdapterErrors.ResponseDataNull("/api/payments"));
            }
            catch (Exception ex)
            {
                return Fin.Fail<PaymentResponse>(Error.New(ex));
            }
        });
    }

    /// <summary>
    /// Adapter 계층 에러 정의
    /// </summary>
    private static class AdapterErrors
    {
        public static Error ApiCallFailed(HttpStatusCode statusCode, string content) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(AdapterErrors)}.{nameof(PaymentApiService)}.{nameof(ApiCallFailed)}",
                errorCurrentValue: $"Status: {statusCode}",
                errorMessage: content);

        public static Error ResponseDataNull(string requestUri) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(AdapterErrors)}.{nameof(PaymentApiService)}.{nameof(ResponseDataNull)}",
                errorCurrentValue: requestUri);
    }
}
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

현재는 클래스 단위로만 `[GeneratePipeline]`을 적용합니다. 특정 메서드를 제외하려면 해당 메서드를 `virtual`이 아닌 일반 메서드로 선언하세요.

```csharp
[GeneratePipeline]
public class MyRepository : IMyRepository
{
    // Pipeline에 포함됨
    public virtual FinT<IO, Product> GetById(Guid id) { ... }

    // Pipeline에서 제외됨 (virtual 아님)
    public FinT<IO, int> GetCount() { ... }
}
```

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
