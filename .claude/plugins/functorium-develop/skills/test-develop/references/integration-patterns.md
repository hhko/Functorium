# 통합 테스트 패턴 레퍼런스

## HostTestFixture 사용법

### Fixture 클래스

```csharp
using Functorium.Testing.Arrangements.Hosting;

public class LayeredArchFixture : HostTestFixture<Program> { }
```

- `HostTestFixture<TProgram>`은 "Test" 환경을 기본으로 사용합니다.
- `appsettings.Test.json`이 자동 로드됩니다.

### IntegrationTestBase 패턴

```csharp
public abstract class IntegrationTestBase : IClassFixture<LayeredArchFixture>
{
    protected HttpClient Client { get; }

    protected IntegrationTestBase(LayeredArchFixture fixture) => Client = fixture.Client;
}
```

## HTTP 테스트 패턴

### POST (Create) - 201 Created

```csharp
public class CreateProductEndpointTests : IntegrationTestBase
{
    public CreateProductEndpointTests(LayeredArchFixture fixture) : base(fixture) { }

    [Fact]
    public async Task CreateProduct_ShouldReturn201Created_WhenRequestIsValid()
    {
        // Arrange
        var request = new
        {
            Name = $"Test Product {Guid.NewGuid()}",
            Description = "Test Description",
            Price = 100.00m,
            StockQuantity = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/products", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var result = await response.Content
            .ReadFromJsonAsync<CreateProductEndpoint.Response>(
                TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Name.ShouldBe(request.Name);
        result.Price.ShouldBe(request.Price);
    }

    [Fact]
    public async Task CreateProduct_ShouldReturn400BadRequest_WhenNameIsEmpty()
    {
        // Arrange
        var request = new
        {
            Name = "",
            Description = "Test Description",
            Price = 100.00m,
            StockQuantity = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync(
            "/api/products", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
```

### 중복 생성 시나리오

```csharp
[Fact]
public async Task CreateProduct_ShouldReturn400BadRequest_WhenDuplicateName()
{
    // Arrange
    var name = $"Duplicate Product {Guid.NewGuid()}";
    var request = new
    {
        Name = name,
        Description = "Test Description",
        Price = 100.00m,
        StockQuantity = 10
    };

    // Create first product
    var firstResponse = await Client.PostAsJsonAsync(
        "/api/products", request, TestContext.Current.CancellationToken);
    firstResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

    // Act - try to create duplicate
    var response = await Client.PostAsJsonAsync(
        "/api/products", request, TestContext.Current.CancellationToken);

    // Assert
    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
}
```

## appsettings.Test.json 설정

```json
{
  "OpenTelemetry": {
    "ServiceName": "LayeredArch.Test",
    "ServiceNamespace": "Functorium.Tests",
    "CollectorEndpoint": "http://localhost:4317"
  },
  "Persistence": {
    "Provider": "InMemory"
  }
}
```

### 필수 OpenTelemetry 설정

| 키 | 설명 |
|----|------|
| `ServiceName` | OTLP 서비스 이름 |
| `ServiceNamespace` | OTLP 네임스페이스 |
| `CollectorEndpoint` | OTLP Collector 주소 |

## 핵심 규칙

1. **고유한 테스트 데이터** - `Guid.NewGuid()`로 이름 충돌 방지
2. **CancellationToken** - `TestContext.Current.CancellationToken` 사용 (xUnit v3)
3. **Response 역직렬화** - `Seq<T>` 대신 `List<T>` 사용 (System.Text.Json 호환)
4. **StatusCode 먼저 검증** - 상태 코드 확인 후 body 검증
5. **ExcludeAssets=analyzers** - 호스트 프로젝트 참조 시 Mediator SourceGenerator 중복 방지

## HTTP 메서드별 패턴

| 메서드 | 사용 | 성공 코드 |
|--------|------|----------|
| `PostAsJsonAsync` | 생성 | 201 |
| `GetAsync` | 조회 | 200 |
| `PutAsJsonAsync` | 수정 | 200 |
| `DeleteAsync` | 삭제 | 200 |
| `GetAsync` + Query | 검색 | 200 |

---

## 스냅샷 테스트 폴더 구조

Verify.Xunit 스냅샷 테스트는 범주별 서브폴더로 구성합니다:

```
Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/
├── Pipelines/Snapshots/
│   ├── Logging/       ← UsecaseLoggingPipelineStructureTests
│   ├── Metrics/       ← UsecaseMetricsPipelineStructureTests
│   ├── Tracing/       ← UsecaseTracingPipelineStructureTests
│   └── CtxEnricher/   ← Enricher 통합 (Logging+Metrics+Tracing)
└── Events/Snapshots/
    ├── Logging/       ← DomainEventPublisherLoggingStructureTests
    ├── Metrics/       ← DomainEventPublisherMetricsStructureTests
    └── Tracing/       ← DomainEventPublisherTracingStructureTests
```

Verify 호출 시 `.UseDirectory("Snapshots/Logging")` 등으로 범주 지정:

```csharp
await Verify(tags).UseDirectory("Snapshots/CtxEnricher");
await Verify(logData).UseDirectory("Snapshots/Logging").ScrubMember("response.elapsed");
```

### 스냅샷 승인

```bash
# 새 스냅샷 또는 변경된 스냅샷 일괄 승인
pwsh -File Build-VerifyAccept.ps1
```
