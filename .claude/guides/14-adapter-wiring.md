# Adapter 연결: Pipeline, DI, 테스트

이 문서는 Adapter의 Pipeline 생성, DI 등록, Options 패턴, 단위 테스트 작성을 다루는 가이드입니다. Port 정의는 [12-ports.md](./12-ports.md), Adapter 구현은 [13-adapters.md](./13-adapters.md)을 참조하세요.

## 목차

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
  - [4.6 Options 패턴 (OptionsConfigurator)](#46-options-패턴-optionsconfigurator)
- [Activity 5: 단위 테스트](#activity-5-단위-테스트)
  - [5.1 테스트 원칙 / IO 실행 패턴](#51-테스트-원칙--io-실행-패턴)
  - [5.2 Repository 테스트](#52-repository-테스트)
  - [5.3 External API 테스트](#53-external-api-테스트)
  - [5.4 Messaging 테스트](#54-messaging-테스트)
  - [5.5 Query Adapter 테스트](#55-query-adapter-테스트)
- [End-to-End Walkthroughs](#end-to-end-walkthroughs)
- [부록](#부록)
  - [A. Clean Architecture 전체 흐름](#a-clean-architecture-전체-흐름)
  - [B. FAQ](#b-faq)
  - [C. Troubleshooting](#c-troubleshooting)
  - [D. Quick Reference 체크리스트](#d-quick-reference-체크리스트)
  - [E. Observability 상세 사양 요약](#e-observability-상세-사양-요약)
- [참고 문서](#참고-문서)

---

## Activity 3: Pipeline 생성 확인

`[GeneratePipeline]` 어트리뷰트가 적용된 Adapter를 빌드하면, Source Generator가 자동으로 Pipeline 클래스를 생성합니다.

### 3.1 GeneratePipeline 소스 생성기

`[GeneratePipeline]` 속성을 클래스에 적용하면 Source Generator가 **Pipeline 래퍼 클래스를 자동 생성**합니다.

**위치**: `Functorium.Adapters.SourceGenerators.GeneratePipelineAttribute`

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
  └── Functorium.SourceGenerators/
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

> **상세 사양**: 트레이싱 Tag 구조, 로그 Message Template, 메트릭 Instrument 정의 등 상세 내용은 [18-observability-spec.md](./18-observability-spec.md)를 참조하세요.

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

> **참고**: Adapter에 Options 패턴이 필요한 경우, Registration 메서드에 `IConfiguration` 파라미터를 추가합니다. [4.6 Options 패턴](#46-options-패턴-optionsconfigurator) 참조.

### 4.2 유형별 등록 패턴

#### Repository 등록

```csharp
// 단일 인터페이스 등록
services.RegisterScopedAdapterPipeline<
    IProductRepository,                      // Port 인터페이스
    InMemoryProductRepositoryPipeline>();     // 생성된 Pipeline
```

#### UnitOfWork 등록

```csharp
// InMemory 환경
services.RegisterScopedAdapterPipeline<IUnitOfWork, InMemoryUnitOfWorkPipeline>();

// EF Core 환경
services.RegisterScopedAdapterPipeline<IUnitOfWork, EfCoreUnitOfWorkPipeline>();
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

#### Query Adapter 등록

```csharp
// InMemory Provider — Query Adapter Pipeline 등록
services.RegisterScopedAdapterPipeline<
    IProductQueryAdapter,
    InMemoryProductQueryAdapterPipeline>();

// Sqlite Provider — Dapper Query Adapter Pipeline 등록
services.RegisterScopedAdapterPipeline<
    IProductQueryAdapter,
    DapperProductQueryAdapterPipeline>();
```

> **참고**: Query Adapter는 Repository와 동일한 `RegisterScopedAdapterPipeline` API를 사용합니다. Provider 분기 패턴([4.6](#46-options-패턴-optionsconfigurator))에서 InMemory는 InMemory Query Adapter를, Sqlite는 Dapper Query Adapter를 등록합니다.

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
    .RegisterAdapterPersistence(builder.Configuration)       // Options 패턴 사용 시 IConfiguration 전달
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
- Options 패턴을 사용하는 Adapter는 `IConfiguration` 파라미터를 받음 ([4.6](#46-options-패턴-optionsconfigurator) 참조)

> **참고**: 등록 순서의 근거와 환경별 구성 분기는 [01-project-structure.md — Host 프로젝트](./01-project-structure.md#등록-순서-근거)를 참조하세요.

### 4.6 Options 패턴 (OptionsConfigurator)

Adapter에 구성 옵션이 필요한 경우 `OptionsConfigurator` 패턴을 사용합니다. `appsettings.json`에서 설정을 읽고, 시작 시 FluentValidation으로 검증하며, StartupLogger에 자동 출력합니다.

#### Options 클래스 구조

```csharp
// 파일: {Adapters.Persistence}/Abstractions/Options/PersistenceOptions.cs

using FluentValidation;
using Functorium.Adapters.Observabilities.Loggers;
using Microsoft.Extensions.Logging;

public sealed class PersistenceOptions : IStartupOptionsLogger
{
    public const string SectionName = "Persistence";   // appsettings.json 섹션 이름

    public string Provider { get; set; } = "InMemory";
    public string ConnectionString { get; set; } = "Data Source=layeredarch.db";

    public static readonly string[] SupportedProviders = ["InMemory", "Sqlite"];

    // IStartupOptionsLogger — 시작 시 자동 로깅
    public void LogConfiguration(ILogger logger)
    {
        const int labelWidth = 20;
        logger.LogInformation("Persistence Configuration");
        logger.LogInformation("  {Label}: {Value}", "Provider".PadRight(labelWidth), Provider);
        if (Provider == "Sqlite")
            logger.LogInformation("  {Label}: {Value}", "ConnectionString".PadRight(labelWidth), ConnectionString);
    }

    // FluentValidation — 시작 시 자동 검증
    public sealed class Validator : AbstractValidator<PersistenceOptions>
    {
        public Validator()
        {
            RuleFor(x => x.Provider)
                .NotEmpty()
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

> **참조**: `Tests.Hosts/01-SingleHost/LayeredArch.Adapters.Persistence/Abstractions/Options/PersistenceOptions.cs`

#### Options 클래스 체크리스트

- [ ] `sealed class`로 선언
- [ ] `SectionName` 상수 정의 (appsettings.json 섹션 이름)
- [ ] `IStartupOptionsLogger` 구현 (`LogConfiguration` 메서드)
- [ ] 중첩 `Validator` 클래스 (`AbstractValidator<TOptions>` 상속)
- [ ] 위치: `{Adapter}/Abstractions/Options/`

#### Registration에서 Options 등록

```csharp
// Options 등록 (1줄로 완료)
services.RegisterConfigureOptions<PersistenceOptions, PersistenceOptions.Validator>(
    PersistenceOptions.SectionName);
```

`RegisterConfigureOptions`가 자동으로 처리하는 항목:

| 항목 | 설명 |
|------|------|
| Options 바인딩 | `appsettings.json`의 `SectionName` → Options 프로퍼티 매핑 |
| FluentValidation 연결 | `IValidator<TOptions>` 등록 및 `IValidateOptions<TOptions>` 연결 |
| `ValidateOnStart()` | 프로그램 시작 시 검증 (실패 시 즉시 종료) |
| `IStartupOptionsLogger` 등록 | `IStartupOptionsLogger` 구현 시 StartupLogger에 자동 출력 |

> **참조**: `Src/Functorium/Adapters/Options/OptionsConfigurator.cs`

#### Provider 분기 등록 패턴

Options 값에 따라 다른 Adapter 구현체를 DI에 등록하는 패턴입니다.

```csharp
public static IServiceCollection RegisterAdapterPersistence(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // 1. Options 등록
    services.RegisterConfigureOptions<PersistenceOptions, PersistenceOptions.Validator>(
        PersistenceOptions.SectionName);

    // 2. 시작 시점에 Provider 읽기
    var options = configuration
        .GetSection(PersistenceOptions.SectionName)
        .Get<PersistenceOptions>() ?? new PersistenceOptions();

    // 3. Provider에 따라 분기 등록
    switch (options.Provider)
    {
        case "Sqlite":
            services.AddDbContext<LayeredArchDbContext>(opt =>
                opt.UseSqlite(options.ConnectionString));
            RegisterSqliteRepositories(services);       // Command 측: EF Core
            RegisterDapperQueryAdapters(services, options.ConnectionString);  // Query 측: Dapper
            break;

        case "InMemory":
        default:
            RegisterInMemoryRepositories(services);     // Command + Query 모두 InMemory
            break;
    }

    return services;
}
```

> **참조**: `Tests.Hosts/01-SingleHost/LayeredArch.Adapters.Persistence/Abstractions/Registrations/AdapterPersistenceRegistration.cs`

#### UseAdapter{Layer}에서 초기화

```csharp
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
```

#### appsettings.json 설정

**SectionName ↔ JSON 키 매핑:**

| Options 클래스 | SectionName | appsettings.json 키 |
|---|---|---|
| `PersistenceOptions` | `"Persistence"` | `"Persistence": { ... }` |
| `OpenTelemetryOptions` | `"OpenTelemetry"` | `"OpenTelemetry": { ... }` |

> 규칙: Options 클래스의 `SectionName` 상수값이 appsettings.json의 최상위 키와 정확히 일치해야 한다.

appsettings.json에서 Options 클래스의 `SectionName` 상수값과 일치하는 섹션을 정의합니다. 환경별 오버라이드는 [ASP.NET Core Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration) 문서를 참조하세요. 통합 테스트 appsettings 설정은 [16-testing-library.md](./16-testing-library.md)를 참조하세요.

**Provider 선택지:**

| Provider | Command 측 | Query 측 | 용도 |
|---|---|---|---|
| `"InMemory"` | ConcurrentDictionary | InMemory Query Adapter | 개발/테스트 (기본값) |
| `"Sqlite"` | EF Core (SQLite) | **Dapper** (SQLite) | 로컬 영속화 |

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

> **참고**: 테스트 규칙 상세는 [15-unit-testing.md](./15-unit-testing.md)를 참조하세요.

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

### 5.5 Query Adapter 테스트

Query Adapter는 InMemory 구현을 직접 인스턴스화하여 테스트합니다. Repository 테스트와 동일한 IO 실행 패턴을 사용합니다.

```csharp
// 파일: Tests/{Project}.Tests.Unit/Application/Products/SearchProductsQueryTests.cs

[Fact]
public async Task Search_ReturnsPagedResult_WhenProductsExist()
{
    // Arrange
    var queryAdapter = new InMemoryProductQueryAdapter(repository);

    // Act
    var ioFin = queryAdapter.Search(spec: null, new PageRequest(), SortExpression.Empty);
    var ioResult = ioFin.Run();
    var result = await Task.Run(() => ioResult.Run());

    // Assert
    result.IsSucc.ShouldBeTrue();
}
```

> **참조**: `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Application/Products/SearchProductsQueryTests.cs`

> **참고**: Dapper Query Adapter의 SQL 실행 테스트는 통합 테스트에서 수행합니다. 단위 테스트에서는 InMemory 구현을 사용하여 Query 로직을 검증합니다.

---

## End-to-End Walkthroughs

각 Adapter 유형의 전체 구현 과정을 요약합니다. 각 단계의 상세 코드는 해당 Activity 섹션을 참조하세요.

### Repository (01-SingleHost IProductRepository)

| Step | Activity | 파일 | 핵심 작업 |
|------|----------|------|----------|
| 1 | Port 정의 | `LayeredArch.Domain/Repositories/IProductRepository.cs` | `: IAdapter`, `FinT<IO, T>` 반환, 도메인 VO 매개변수 |
| 2 | Adapter 구현 | `LayeredArch.Adapters.Persistence/Repositories/InMemory/InMemoryProductRepository.cs` | `[GeneratePipeline]`, `virtual`, `IO.lift`, `AdapterError.For<T>` |
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

### Query Adapter (01-SingleHost IProductQueryAdapter)

| Step | Activity | 파일 | 핵심 작업 |
|------|----------|------|----------|
| 1 | Port 정의 | `LayeredArch.Application/Usecases/Products/Ports/IProductQueryAdapter.cs` | `: IQueryAdapter<Product, ProductSummaryDto>` |
| 2a | Dapper 구현 | `LayeredArch.Adapters.Persistence/Repositories/Dapper/DapperProductQueryAdapter.cs` | `DapperQueryAdapterBase` 상속, `[GeneratePipeline]`, SQL 선언만 담당 |
| 2b | InMemory 구현 | `LayeredArch.Adapters.Persistence/Repositories/InMemory/InMemoryProductQueryAdapter.cs` | `[GeneratePipeline]`, Repository 위임 |
| 3 | Pipeline 확인 | `obj/GeneratedFiles/.../Repositories.Dapper.DapperProductQueryAdapterPipeline.g.cs` | 빌드 후 자동 생성 |
| 4 | DI 등록 | `AdapterPersistenceRegistration.cs` → `Program.cs` | Sqlite: Dapper Pipeline, InMemory: InMemory Pipeline |
| 5 | 테스트 | `SearchProductsQueryTests.cs` | InMemory Query Adapter 직접 테스트, [5.5](#55-query-adapter-테스트) 참조 |

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

#### Q6. Repository와 Query Adapter를 언제 구분하나요?

**판단 기준**: 조회 결과로 Aggregate를 재구성할 필요가 있는가?

- **Aggregate 필요** (도메인 불변식 검증, Create/Update/Delete) → **Repository** (`IRepository<T, TId>`, Domain Layer, EF Core)
- **DTO 직접 반환** (읽기 전용, 페이지네이션/정렬) → **Query Adapter** (`IQueryAdapter<TEntity, TDto>`, Application Layer, Dapper)

> 상세 판단 기준은 [2.6 Query Adapter](#26-query-adapter-cqrs-read-측)의 비교 테이블을 참조하세요.

#### Q7. RequestCategory 값은 어떻게 정하나요?

`RequestCategory`는 Observability Pipeline의 메트릭/트레이싱에서 사용하는 분류 태그입니다. 프레임워크가 정한 예약어는 없으며, 팀 내 일관된 네이밍이 중요합니다.

| 권장 값 | 용도 |
|---------|------|
| `"Repository"` | Aggregate CRUD 영속화 |
| `"UnitOfWork"` | 트랜잭션 커밋 |
| `"QueryAdapter"` | 읽기 전용 조회 (DTO 직접 반환) |
| `"ExternalApi"` | 외부 HTTP API 호출 |
| `"Messaging"` | 메시지 큐 통신 |

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
- [ ] 위치: Repository → Domain, External API/Query Adapter → Application

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
- [ ] Query Adapter Pipeline 등록 (Dapper 또는 InMemory)
- [ ] `Program.cs`에서 Registration 호출

#### 단위 테스트

- [ ] 원본 Adapter 클래스 테스트 (Pipeline 아님)
- [ ] AAA 패턴
- [ ] `T1_T2_T3` 네이밍
- [ ] `.Run()` → `Task.Run(() => ioResult.Run())` 실행
- [ ] 성공/실패 케이스 모두 테스트

### E. Observability 상세 사양 요약

Pipeline이 자동 제공하는 Observability 기능의 요약입니다. 상세 사양은 [18-observability-spec.md](./18-observability-spec.md)를 참조하세요.

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
| [domain-modeling-overview.md](./04-ddd-tactical-overview.md) | 도메인 모델링 전체 개요 |
| [05-value-objects.md](./05-value-objects.md) | Value Object 구현 가이드 |
| [06-entities-and-aggregates.md](./06-entities-and-aggregates.md) | Entity 구현 가이드 |
| [11-usecases-and-cqrs.md](./11-usecases-and-cqrs.md) | 유스케이스 구현 (CQRS Command/Query) |
| [08-error-system.md](./08-error-system.md) | 에러 시스템 가이드 |
| [12-ports.md](./12-ports.md) | Port 정의 가이드 |
| [13-adapters.md](./13-adapters.md) | Adapter 구현 가이드 |
| [15-unit-testing.md](./15-unit-testing.md) | 단위 테스트 작성 가이드 |
| [18-observability-spec.md](./18-observability-spec.md) | Observability 사양 (트레이싱, 로깅, 메트릭 상세) |
| [01-project-structure.md](./01-project-structure.md) | 서비스 프로젝트 구조 가이드 |

**외부 참고:**

- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/) - 분산 트레이싱
- [LanguageExt](https://github.com/louthy/language-ext) - 함수형 프로그래밍 라이브러리
