# 인프라 결정 가이드

## 영속성 전략

### Provider 전환 패턴

`appsettings.json`의 `Persistence:Provider`로 InMemory/Sqlite 전환:

```json
{
  "Persistence": {
    "Provider": "InMemory",
    "ConnectionString": "Data Source=app.db"
  }
}
```

### DI 등록 (AdapterPersistenceRegistration)

```csharp
public static IServiceCollection RegisterAdapterPersistence(
    this IServiceCollection services, IConfiguration config)
{
    var provider = config["Persistence:Provider"];
    return provider switch
    {
        "Sqlite" => services.RegisterSqliteRepositories(config),
        _ => services.RegisterInMemoryRepositories()
    };
}
```

### Repository 등록 패턴

```csharp
// Observable Port 래퍼 자동 생성 + DI 등록
services.RegisterScopedObservablePort<IProductRepository, ProductRepositoryInMemoryObservable>();
services.RegisterScopedObservablePort<IProductQuery, ProductQueryInMemoryObservable>();
```

## 관측성 구성

### OpenTelemetry 파이프라인 순서

```csharp
services.RegisterOpenTelemetry(config, assembly)
    .ConfigurePipelines(p => p
        .UseMetrics()        // 1. 메트릭 수집
        .UseTracing()        // 2. 분산 추적
        .UseCtxEnricher()    // 3. 비즈니스 컨텍스트 전파
        .UseLogging()        // 4. 구조화 로깅
        .UseException())     // 5. 예외 변환
    .Build();
```

### appsettings.json

```json
{
  "OpenTelemetry": {
    "ServiceName": "{ServiceName}",
    "ServiceNamespace": "{ServiceNamespace}",
    "CollectorEndpoint": "http://localhost:18889",
    "CollectorProtocol": "Grpc",
    "SamplingRate": 1.0,
    "EnablePrometheusExporter": false
  }
}
```

## HTTP API 구성

### FastEndpoints 등록

```csharp
// AdapterPresentationRegistration
public static IServiceCollection RegisterAdapterPresentation(this IServiceCollection services)
    => services.AddFastEndpoints();

public static WebApplication UseAdapterPresentation(this WebApplication app)
{
    app.UseFastEndpoints(c => c.Serializer.Options.PropertyNamingPolicy = null);
    return app;
}
```

## Host Program.cs 패턴

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .RegisterAdapterPresentation()
    .RegisterAdapterPersistence(builder.Configuration)
    .RegisterAdapterInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseAdapterPresentation();
app.Run();

public partial class Program { }  // Integration Test 지원
```

## IO 고급 기능 (External Service)

| 패턴 | 용도 | 예시 |
|------|------|------|
| `IO.Timeout` + `Catch` | 타임아웃 + 조건부 복구 | 헬스체크 API |
| `IO.Retry` + `Schedule` | 지수 백오프 재시도 | 외부 모니터링 API |
| `IO.Fork` + `Await` | 병렬 실행 | 병렬 컴플라이언스 체크 |
| `IO.Bracket` | 리소스 획득-사용-해제 | 레지스트리 세션 관리 |
