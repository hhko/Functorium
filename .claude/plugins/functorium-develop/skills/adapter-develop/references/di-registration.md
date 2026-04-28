# DI 등록 패턴 레퍼런스

## RegisterScopedObservablePort 사용법

Source Generator가 생성한 Observable 래퍼를 DI에 등록합니다.
`[GenerateObservablePort]`가 `{ClassName}Observable` 클래스를 자동 생성합니다.

```csharp
// Repository 등록 (Observable Pipeline 래퍼 사용)
services.RegisterScopedObservablePort<IProductRepository, ProductRepositoryInMemoryObservable>();
services.RegisterScopedObservablePort<IProductRepository, ProductRepositoryEfCoreObservable>();

// Query Adapter 등록
services.RegisterScopedObservablePort<IProductQuery, ProductQueryDapperObservable>();
services.RegisterScopedObservablePort<IProductQuery, ProductQueryInMemoryObservable>();

// UnitOfWork 등록
services.RegisterScopedObservablePort<IUnitOfWork, UnitOfWorkInMemoryObservable>();

// 공유 Port 등록
services.RegisterScopedObservablePort<IProductCatalog, ProductCatalogEfCoreObservable>();
```

## RegisterConfigureOptions 패턴

Options + FluentValidation 자동 연결:

```csharp
// Options 등록 (IOptions<T> + 시작 시 유효성 검증)
services.RegisterConfigureOptions<PersistenceOptions, PersistenceOptions.Validator>(
    PersistenceOptions.SectionName);
```

### Options 클래스 예제

```csharp
public sealed class PersistenceOptions : IStartupOptionsLogger
{
    public const string SectionName = "Persistence";

    public string Provider { get; set; } = "InMemory";
    public string ConnectionString { get; set; } = "Data Source=layeredarch.db";

    public static readonly string[] SupportedProviders = ["InMemory", "Sqlite"];

    public void LogConfiguration(ILogger logger)
    {
        logger.LogInformation("Persistence Configuration");
        logger.LogInformation("  {Label}: {Value}", "Provider".PadRight(20), Provider);
    }

    public sealed class Validator : AbstractValidator<PersistenceOptions>
    {
        public Validator()
        {
            RuleFor(x => x.Provider)
                .NotEmpty().WithMessage($"{nameof(Provider)} is required.")
                .Must(p => SupportedProviders.Contains(p))
                .WithMessage($"{nameof(Provider)} must be one of: {string.Join(", ", SupportedProviders)}");

            RuleFor(x => x.ConnectionString)
                .NotEmpty()
                .When(x => x.Provider == "Sqlite")
                .WithMessage($"{nameof(ConnectionString)} is required when Provider is 'Sqlite'.");
        }
    }
}
```

## Mediator + DomainEventPublisher 등록

```csharp
// 1. Mediator 등록 (Handler 관점 관찰 가능성 활성화)
services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
    options.NotificationPublisherType = typeof(ObservableDomainEventNotificationPublisher);
});

// 2. 도메인 이벤트 발행자 등록 (Publisher 관점 관찰 가능성 활성화)
services.RegisterDomainEventPublisher();

// 3. 도메인 이벤트 핸들러 어셈블리 스캔 등록
services.RegisterDomainEventHandlersFromAssembly(LayeredArch.Application.AssemblyReference.Assembly);
```

## FluentValidation 어셈블리 스캔

```csharp
// 여러 어셈블리에서 AbstractValidator<T> 구현체 자동 등록
services.AddValidatorsFromAssembly(AssemblyReference.Assembly);
services.AddValidatorsFromAssembly(LayeredArch.Application.AssemblyReference.Assembly);
```

## OpenTelemetry 파이프라인 설정

```csharp
services
    .RegisterOpenTelemetry(configuration, AssemblyReference.Assembly)
    .ConfigurePipelines(pipelines => pipelines
        .UseAll()
        .AddCustomPipelinesFromAssembly(AssemblyReference.Assembly))
    .Build();
```

### ObservableSignal 팩토리 등록

`ObservableSignal.SetFactory()`는 `OpenTelemetryBuilder.Build()` 내에서 자동으로 호출됩니다.
별도의 수동 등록이 필요하지 않습니다.

```csharp
// OpenTelemetryBuilder 내부에서 자동 등록됨:
// ObservableSignal.SetFactory(new ObservableSignalFactory());
// CtxEnricherContext.SetPushFactory((name, value, pillars) => ...);
```

### Log Enricher 등록

```csharp
// Usecase Log Enricher (수동 등록 — ICustomUsecasePipeline이 아니므로 Scrutor 스캔 대상 아님)
services.AddScoped<
    IUsecaseCtxEnricher<CreateOrderCommand.Request, FinResponse<CreateOrderCommand.Response>>,
    CreateOrderCommandRequestCtxEnricher>();

// Domain Event Log Enricher
services.AddScoped<
    IDomainEventCtxEnricher<Order.CreatedEvent>,
    OrderCreatedEventCtxEnricher>();
```

## Provider 분기 패턴 (InMemory / Sqlite)

```csharp
public static class AdapterPersistenceRegistration
{
    public static IServiceCollection RegisterAdapterPersistence(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Options 등록
        services.RegisterConfigureOptions<PersistenceOptions, PersistenceOptions.Validator>(
            PersistenceOptions.SectionName);

        var options = configuration
            .GetSection(PersistenceOptions.SectionName)
            .Get<PersistenceOptions>() ?? new PersistenceOptions();

        switch (options.Provider)
        {
            case "Sqlite":
                services.AddDbContext<LayeredArchDbContext>(opt =>
                    opt.UseSqlite(options.ConnectionString));
                RegisterSqliteRepositories(services);
                RegisterDapperQueries(services, options.ConnectionString);
                break;

            case "InMemory":
            default:
                RegisterInMemoryRepositories(services);
                break;
        }

        return services;
    }

    public static IApplicationBuilder UseAdapterPersistence(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices
            .GetRequiredService<IOptions<PersistenceOptions>>().Value;

        if (options.Provider == "Sqlite")
        {
            using var scope = app.ApplicationServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LayeredArchDbContext>();
            dbContext.Database.EnsureCreated();
        }

        return app;
    }
}
```

### Dapper IDbConnection 등록

```csharp
private static void RegisterDapperQueries(IServiceCollection services, string connectionString)
{
    services.AddScoped<IDbConnection>(_ =>
    {
        var conn = new SqliteConnection(connectionString);
        conn.Open();
        return conn;
    });

    services.RegisterScopedObservablePort<IProductQuery, ProductQueryDapperObservable>();
    // ... 추가 Query Adapter 등록
}
```

## 등록 순서 (권장)

```
1. Options 등록       : RegisterConfigureOptions<T, TValidator>()
2. Mediator 등록      : AddMediator() + RegisterDomainEventPublisher()
3. FluentValidation   : AddValidatorsFromAssembly()
4. OpenTelemetry      : RegisterOpenTelemetry() → ConfigurePipelines() → Build()
5. Repository 등록    : RegisterScopedObservablePort<IRepo, RepoObservable>()
6. Query Adapter 등록 : RegisterScopedObservablePort<IQuery, QueryObservable>()
7. Log Enricher 등록  : AddScoped<IUsecaseCtxEnricher<...>, Enricher>()
```

## CtxEnricher 등록

```csharp
// CtxEnricher 파이프라인은 UseAll()에 포함됩니다.
services.RegisterOpenTelemetry(configuration, AssemblyReference.Assembly)
    .ConfigurePipelines(pipelines => pipelines.UseAll())
    .Build();
```

파이프라인 실행 순서:

| 순서 | Pipeline | 역할 |
|------|----------|------|
| 1 | `CtxEnricherPipeline` | ctx.* 3-Pillar 전파 |
| 2 | `UsecaseMetricsPipeline` | 메트릭 수집 (ctx MetricsTag 병합) |
| 3 | `UsecaseTracingPipeline` | 분산 추적 |
| 4 | `UsecaseLoggingPipeline` | 구조화된 로깅 |
| 5 | `UsecaseValidationPipeline` | FluentValidation |
| 6 | `UsecaseExceptionPipeline` | 예외 변환 |
| 7 | `UsecaseTransactionPipeline` | 트랜잭션 |
| 8 | Custom Pipelines | 사용자 정의 |
