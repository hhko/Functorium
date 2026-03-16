---
title: "Adapter 연결 -- 단위 테스트"
---

이 문서는 Adapter의 단위 테스트 작성, End-to-End Walkthrough, 아키텍처 부록을 다루는 가이드입니다. Pipeline 생성과 DI 등록은 [14a-adapter-pipeline-di.md](./14a-adapter-pipeline-di), Port 정의는 [12-ports.md](./12-ports), Adapter 구현은 [13-adapters.md](./13-adapters)을 참조하세요.

## 들어가며

"Adapter 레이어의 단위 테스트를 어떻게 구성하고, Port 의존성을 어떻게 격리할 것인가?"
"Pipeline이 아닌 원본 Adapter를 직접 테스트해야 하는 이유는 무엇인가?"
"`FinT<IO, T>` 반환값을 테스트에서 어떻게 실행하고 검증하는가?"

Adapter 단위 테스트는 비즈니스 로직의 정확성을 검증하는 마지막 관문입니다. Pipeline(Observable)이 아닌 원본 Adapter 클래스를 직접 테스트하여, 관측성 래핑 없이 순수한 로직을 검증합니다. 이 문서는 유형별 테스트 전략과 End-to-End Walkthrough를 다룹니다.

### 이 문서에서 배우는 내용

이 문서를 통해 다음을 학습합니다:

1. **Adapter 단위 테스트 구조** — 원본 Adapter 직접 테스트, AAA 패턴, IO 실행 패턴
2. **Mock/Stub 전략** — Repository(직접 인스턴스), External API(MockHttpMessageHandler), Messaging(NSubstitute)
3. **E2E 워크스루** — Repository, External API, Messaging, Query Adapter의 전체 구현 과정 요약

### 사전 지식

이 문서를 이해하기 위해 다음 개념에 대한 기본적인 이해가 필요합니다:

- [Adapter 구현](./13-adapters) — Adapter 유형별 구현 패턴
- [Pipeline과 DI](./14a-adapter-pipeline-di) — Pipeline 생성과 DI 등록
- [단위 테스트 작성 가이드](../testing/15a-unit-testing) — 테스트 네이밍, AAA 패턴, Shouldly

> **테스트 대상은 Pipeline이 아닌 원본 Adapter입니다.** Pipeline은 관측성만 추가하므로, 비즈니스 로직 검증은 원본 클래스에서 수행합니다.

## 요약

### 주요 명령

```csharp
// 테스트에서 IO 실행
var result = await Task.Run(() => adapter.GetById(id).Run().RunAsync());
```

### 주요 절차

1. 원본 Adapter 클래스를 직접 인스턴스화하여 테스트 (Pipeline이 아님)
2. AAA (Arrange-Act-Assert) 패턴으로 테스트 작성
3. `.Run().RunAsync()` 또는 `Task.Run(() => ioResult.Run())`으로 IO 실행
4. 성공/실패 케이스 모두 테스트

### 주요 개념

| 개념 | 설명 |
|------|------|
| IO 실행 패턴 | 테스트에서 `adapter.Method().Run().RunAsync()`로 IO 실행 |
| 테스트 대상 | 원본 Adapter 클래스 (Pipeline 아님) |
| Mock 전략 | Repository: 직접 인스턴스, External API: MockHttpMessageHandler, Messaging: NSubstitute |

먼저 Adapter 단위 테스트의 원칙과 IO 실행 패턴을 살펴본 뒤, 유형별(Repository, External API, Messaging, Query) 테스트 예제를 확인합니다.

---

## Activity 5: 단위 테스트

Adapter의 단위 테스트는 **원본 클래스를 직접 테스트**합니다 (Pipeline이 아님).

### 테스트 원칙 / IO 실행 패턴

| 원칙 | 설명 |
|------|------|
| 테스트 대상 | 원본 Adapter 클래스 (Pipeline 아님) |
| 패턴 | AAA (Arrange-Act-Assert) |
| 네이밍 | `T1_T2_T3` (메서드명_시나리오_기대결과) |
| 실행 | `.Run().RunAsync()` 또는 `Task.Run(() => ioResult.Run())` |
| 단언 라이브러리 | Shouldly |
| Mock 라이브러리 | NSubstitute |

> **참고**: 테스트 규칙 상세는 [15a-unit-testing.md](../testing/15a-unit-testing)를 참조하세요.

**IO 실행 패턴** - `FinT<IO, T>` 반환값을 테스트에서 실행하는 패턴:

```csharp
// Act
var ioFin = adapter.MethodUnderTest(args);   // FinT<IO, T> 반환
var ioResult = ioFin.Run();                  // IO<Fin<T>> 변환
var result = await Task.Run(() => ioResult.Run());  // Fin<T> 실행

// Assert
result.IsSucc.ShouldBeTrue();
```

### Repository 테스트

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
            ProductName.Create("테스트 상품").ThrowIfFail(),
            ProductDescription.Create("설명").ThrowIfFail(),
            Money.Create(10000m).ThrowIfFail());

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
        var nonExistentId = ProductId.New();

        // Act
        var ioFin = repository.GetById(nonExistentId);
        var ioResult = ioFin.Run();
        var result = await Task.Run(() => ioResult.Run());

        // Assert
        result.IsFail.ShouldBeTrue();
    }
}
```

### External API 테스트

External API Adapter는 `HttpClient`를 Mock하여 테스트합니다.

`MockHttpMessageHandler`로 HTTP 응답을 제어하고, 성공/실패 시나리오를 분리하여 테스트하는 패턴을 주목하세요.

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

### Messaging 테스트

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

### Query Adapter 테스트

Query Adapter는 InMemory 구현을 직접 인스턴스화하여 테스트합니다. Repository 테스트와 동일한 IO 실행 패턴을 사용합니다.

```csharp
// 파일: Tests/{Project}.Tests.Unit/Application/Products/SearchProductsQueryTests.cs

[Fact]
public async Task Search_ReturnsPagedResult_WhenProductsExist()
{
    // Arrange
    var queryAdapter = new InMemoryProductQuery(repository);

    // Act
    var ioFin = queryAdapter.Search(Specification<Product>.All, new PageRequest(), SortExpression.Empty);
    var ioResult = ioFin.Run();
    var result = await Task.Run(() => ioResult.Run());

    // Assert
    result.IsSucc.ShouldBeTrue();
}
```

> **참조**: `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Application/Products/SearchProductsQueryTests.cs`

> **참고**: Dapper Query Adapter의 SQL 실행 테스트는 통합 테스트에서 수행합니다. 단위 테스트에서는 InMemory 구현을 사용하여 Query 로직을 검증합니다.

유형별 테스트 패턴을 익혔다면, 이제 각 Adapter의 전체 구현 과정을 End-to-End로 확인합니다.

---

## End-to-End Walkthroughs

각 Adapter 유형의 전체 구현 과정을 요약합니다. 각 단계의 상세 코드는 해당 Activity 섹션을 참조하세요.

### Repository (01-SingleHost IProductRepository)

| Step | Activity | 파일 | 핵심 작업 |
|------|----------|------|----------|
| 1 | Port 정의 | `LayeredArch.Domain/Repositories/IProductRepository.cs` | `: IObservablePort`, `FinT<IO, T>` 반환, 도메인 VO 매개변수 |
| 2 | Adapter 구현 | `LayeredArch.Adapters.Persistence/Repositories/InMemory/InMemoryProductRepository.cs` | `[GenerateObservablePort]`, `virtual`, `IO.lift`, `AdapterError.For<T>` |
| 3 | Pipeline 확인 | `obj/GeneratedFiles/.../Repositories.InMemoryProductRepositoryObservable.g.cs` | 빌드 후 자동 생성 |
| 4 | DI 등록 | `AdapterPersistenceRegistration.cs` -> `Program.cs` | `RegisterScopedObservablePort<IProductRepository, ...Observable>()` |
| 5 | 테스트 | `InMemoryProductRepositoryTests.cs` | 원본 클래스 직접 테스트, [Repository 테스트](#repository-테스트) 참조 |

### External API (01-SingleHost IExternalPricingService)

| Step | Activity | 파일 | 핵심 작업 |
|------|----------|------|----------|
| 1 | Port 정의 | `LayeredArch.Application/Ports/IExternalPricingService.cs` | `CancellationToken` 포함, `Async` 접미사 |
| 2 | Adapter 구현 | `LayeredArch.Adapters.Infrastructure/ExternalApis/ExternalPricingApiService.cs` | `IO.liftAsync`, `HandleHttpError<T>`, try/catch 패턴 |
| 3 | Pipeline 확인 | `obj/GeneratedFiles/.../ExternalApis.ExternalPricingApiServiceObservable.g.cs` | 빌드 후 자동 생성 |
| 4 | DI 등록 | `AdapterInfrastructureRegistration.cs` -> `Program.cs` | `AddHttpClient<...Observable>()` + `RegisterScopedObservablePort` |
| 5 | 테스트 | `ExternalPricingApiServiceTests.cs` | `MockHttpMessageHandler` 사용, [External API 테스트](#external-api-테스트) 참조 |

### Messaging (Cqrs06Services IInventoryMessaging)

| Step | Activity | 파일 | 핵심 작업 |
|------|----------|------|----------|
| 1 | Port 정의 | `OrderService/Adapters/Messaging/IInventoryMessaging.cs` | Request/Reply + Fire-and-Forget |
| 2 | Adapter 구현 | `OrderService/Adapters/Messaging/RabbitMqInventoryMessaging.cs` | `IMessageBus` 주입, `InvokeAsync` / `SendAsync` |
| 3 | Pipeline 확인 | `obj/GeneratedFiles/.../Messaging.RabbitMqInventoryMessagingObservable.g.cs` | 빌드 후 자동 생성 |
| 4 | DI 등록 | `OrderService/Program.cs` (57행) | `RegisterScopedObservablePort`, MessageBus는 Wolverine 별도 등록 |
| 5 | 테스트 | `RabbitMqInventoryMessagingTests.cs` | NSubstitute로 `IMessageBus` Mock, [Messaging 테스트](#messaging-테스트) 참조 |

### Query Adapter (01-SingleHost IProductQuery)

| Step | Activity | 파일 | 핵심 작업 |
|------|----------|------|----------|
| 1 | Port 정의 | `LayeredArch.Application/Usecases/Products/Ports/IProductQuery.cs` | `: IQueryPort<Product, ProductSummaryDto>` |
| 2a | Dapper 구현 | `LayeredArch.Adapters.Persistence/Repositories/Dapper/DapperProductQuery.cs` | `DapperQueryBase` 상속, `[GenerateObservablePort]`, SQL 선언만 담당 |
| 2b | InMemory 구현 | `LayeredArch.Adapters.Persistence/Repositories/InMemory/InMemoryProductQuery.cs` | `[GenerateObservablePort]`, Repository 위임 |
| 3 | Pipeline 확인 | `obj/GeneratedFiles/.../Repositories.Dapper.DapperProductQueryObservable.g.cs` | 빌드 후 자동 생성 |
| 4 | DI 등록 | `AdapterPersistenceRegistration.cs` -> `Program.cs` | Sqlite: Dapper Observable, InMemory: InMemory Observable |
| 5 | 테스트 | `SearchProductsQueryTests.cs` | InMemory Query Adapter 직접 테스트, [Query Adapter 테스트](#query-adapter-테스트) 참조 |

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
|  │ IProductRepository : IRepository<Product, ProductId> (Port Interface) │  |
|  │   - FinT<IO, Product> GetById(ProductId id)                  │  |
|  │   - FinT<IO, Product> Create(Product product)               │  |
|  └─────────────────────────────────────────────────────────────┘  |
+-------------------------------------------------------------------+
                              |
                              v
+-------------------------------------------------------------------+
|                      Infrastructure Layer                         |
|  ┌─────────────────────────────────────────────────────────────┐  |
|  │ [GenerateObservablePort]                                          │  |
|  │ InMemoryProductRepository : IProductRepository              │  |
|  │   - RequestCategory => "Repository"                         │  |
|  │   - 실제 데이터 접근 구현                                    │  |
|  └─────────────────────────────────────────────────────────────┘  |
|                              |                                    |
|                              v (Source Generator)                 |
|  ┌─────────────────────────────────────────────────────────────┐  |
|  │ InMemoryProductRepositoryObservable (자동 생성)               │  |
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

    internal sealed class Usecase(IProductDetailQuery productDetailQuery)
        : IQueryUsecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(
            Request request, CancellationToken cancellationToken)
        {
            var productId = ProductId.Create(request.ProductId);

            FinT<IO, Response> usecase =
                from dto in productDetailQuery.GetById(productId)
                select new Response(
                    dto.ProductId,
                    dto.Name,
                    dto.Price);

            Fin<Response> result = await usecase.Run().RunAsync();
            return result.ToFinResponse();
        }
    }
}
```

### D. Quick Reference 체크리스트

#### Port 인터페이스

- [ ] `IObservablePort` 상속
- [ ] 반환 타입: `FinT<IO, T>`
- [ ] 도메인 VO 사용 (Repository)
- [ ] `CancellationToken` (External API)
- [ ] 위치: Repository -> Domain, External API/Query Adapter -> Application

#### Adapter 구현

- [ ] `[GenerateObservablePort]` 어트리뷰트
- [ ] Port 인터페이스 구현
- [ ] `RequestCategory` 프로퍼티
- [ ] 모든 메서드에 `virtual`
- [ ] `IO.lift` (동기) 또는 `IO.liftAsync` (비동기)
- [ ] 성공: `Fin.Succ(value)`
- [ ] 실패: `AdapterError.For<T>(errorType, context, message)`
- [ ] 예외: `AdapterError.FromException<T>(errorType, ex)`

#### DI 등록

- [ ] Registration 클래스 생성 (`Adapter{Layer}Registration`)
- [ ] `RegisterScopedObservablePort<IObservablePort, ObservablePort>()`
- [ ] HttpClient 등록 (External API)
- [ ] Query Adapter Observable 등록 (Dapper 또는 InMemory)
- [ ] `Program.cs`에서 Registration 호출

#### 단위 테스트

- [ ] 원본 Adapter 클래스 테스트 (Pipeline 아님)
- [ ] AAA 패턴
- [ ] `T1_T2_T3` 네이밍
- [ ] `.Run()` -> `Task.Run(() => ioResult.Run())` 실행
- [ ] 성공/실패 케이스 모두 테스트

### E. Observability 상세 사양 요약

Pipeline이 자동 제공하는 Observability 기능의 요약입니다. 상세 사양은 [18a-observability-spec.md](../observability/18a-observability-spec)를 참조하세요.

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

## 트러블슈팅

### 테스트에서 `FinT<IO, T>` 실행 방법을 모르겠음

**원인:** `FinT<IO, T>` 반환값은 IO 모나드를 감싸고 있어 직접 실행이 필요합니다.

**해결:** `.Run().RunAsync()` 패턴을 사용합니다.

```csharp
var ioFin = adapter.MethodUnderTest(args);   // FinT<IO, T> 반환
var ioResult = ioFin.Run();                  // IO<Fin<T>> 변환
var result = await Task.Run(() => ioResult.Run());  // Fin<T> 실행
```

---

## FAQ

### Q3. 테스트에서 Pipeline 없이 원본 클래스를 테스트할 수 있나요?

네, 원본 클래스를 직접 인스턴스화하여 테스트할 수 있습니다. [Activity 5](#activity-5-단위-테스트) 섹션의 테스트 예제를 참조하세요.

### Q6. Repository와 Query Adapter를 언제 구분하나요?

**판단 기준**: 조회 결과로 Aggregate를 재구성할 필요가 있는가?

- **Aggregate 필요** (도메인 불변식 검증, Create/Update/Delete) -> **Repository** (`IRepository<T, TId>`, Domain Layer, EF Core)
- **DTO 직접 반환** (읽기 전용, 페이지네이션/정렬) -> **Query Adapter** (`IQueryPort<TEntity, TDto>`, Application Layer, Dapper)

> 상세 판단 기준은 [Query Adapter](./13-adapters#query-adapter-cqrs-read-측)의 비교 테이블을 참조하세요.

---

## 참고 문서

| 문서 | 설명 |
|------|------|
| [04-ddd-tactical-overview.md](../domain/04-ddd-tactical-overview) | 도메인 모델링 전체 개요 |
| [11-usecases-and-cqrs.md](../application/11-usecases-and-cqrs) | 유스케이스 구현 (CQRS Command/Query) |
| [08a-error-system.md](../domain/08a-error-system) | 에러 시스템: 기초와 네이밍 |
| [08b-error-system-domain-app.md](../domain/08b-error-system-domain-app) | 에러 시스템: Domain/Application 에러 |
| [08c-error-system-adapter-testing.md](../domain/08c-error-system-adapter-testing) | 에러 시스템: Adapter 에러와 테스트 |
| [12-ports.md](./12-ports) | Port 정의 가이드 |
| [13-adapters.md](./13-adapters) | Adapter 구현 가이드 |
| [14a-adapter-pipeline-di.md](./14a-adapter-pipeline-di) | Pipeline 생성, DI 등록, Options 패턴 |
| [15a-unit-testing.md](../testing/15a-unit-testing) | 단위 테스트 작성 가이드 |
| [18a-observability-spec.md](../observability/18a-observability-spec) | Observability 사양 (트레이싱, 로깅, 메트릭 상세) |
| [01-project-structure.md](../architecture/01-project-structure) | 서비스 프로젝트 구조 가이드 |

**외부 참고:**

- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/) - 분산 트레이싱
- [LanguageExt](https://github.com/louthy/language-ext) - 함수형 프로그래밍 라이브러리
