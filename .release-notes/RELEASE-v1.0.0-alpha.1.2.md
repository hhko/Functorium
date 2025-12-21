---
title: Functorium v1.0.0-alpha.1.2 새로운 기능
description: Functorium v1.0.0-alpha.1.2의 새로운 기능을 알아봅니다.
date: 2025-12-20
---

# Functorium Release v1.0.0-alpha.1.2

## 개요

Functorium v1.0.0-alpha.1.2는 .NET 생태계를 위한 함수형 프로그래밍 라이브러리의 첫 번째 알파 릴리스입니다. LanguageExt 5.0 기반의 함수형 추상화와 OpenTelemetry 통합 관측성, 그리고 테스트 헬퍼 라이브러리를 제공합니다.

**주요 기능**:

- **함수형 오류 처리**: LanguageExt Error 타입의 구조화된 생성 및 Serilog 직렬화 지원
- **OpenTelemetry 통합**: Traces, Metrics, Logs를 위한 통합 구성 빌더
- **테스트 헬퍼**: 아키텍처 검증, 호스트 통합 테스트, Quartz Job 테스트 지원

## Breaking Changes

이번 릴리스에는 Breaking Changes가 없습니다. (첫 배포)

## 새로운 기능

### Functorium 라이브러리

#### 1. 구조화된 오류 생성 (ErrorCodeFactory)

`ErrorCodeFactory`는 LanguageExt의 `Error` 타입을 일관된 형식으로 생성하는 팩토리 클래스입니다. 오류 코드, 현재 값, 메시지를 구조화하여 디버깅과 로깅을 용이하게 합니다.

```csharp
using Functorium.Abstractions.Errors;

// 단일 값 오류 생성
var error = ErrorCodeFactory.Create(
    errorCode: "USER_NOT_FOUND",
    errorCurrentValue: userId,
    errorMessage: "사용자를 찾을 수 없습니다.");

// 복수 값 오류 생성
var validationError = ErrorCodeFactory.Create(
    errorCode: "VALIDATION_FAILED",
    errorCurrentValue1: fieldName,
    errorCurrentValue2: fieldValue,
    errorMessage: "필드 검증에 실패했습니다.");

// 예외로부터 오류 생성
var exceptionalError = ErrorCodeFactory.CreateFromException(
    errorCode: "DATABASE_ERROR",
    exception: dbException);
```

**Why this matters (왜 중요한가):**
- 오류 생성 방식을 표준화하여 코드베이스 전체에서 일관된 오류 처리 가능
- 오류 코드 기반 분류로 모니터링 및 알림 시스템 구축 용이
- 현재 값 포함으로 디버깅 시 문제 원인 즉시 파악 가능

<!-- 관련 커밋: cda0a33 feat(functorium): 핵심 라이브러리 패키지 참조 및 소스 구조 추가 -->

---

#### 2. LanguageExt Error Serilog 직렬화 (ErrorsDestructuringPolicy)

`ErrorsDestructuringPolicy`는 LanguageExt의 다양한 Error 타입을 Serilog에서 구조화된 형태로 로깅할 수 있게 해주는 정책입니다.

```csharp
using Functorium.Abstractions.Errors.DestructuringPolicies;
using Serilog;

// Serilog 구성에 정책 추가
Log.Logger = new LoggerConfiguration()
    .Destructure.With<ErrorsDestructuringPolicy>()
    .WriteTo.Console()
    .CreateLogger();

// Error 타입이 자동으로 구조화됨
var error = ErrorCodeFactory.Create("CODE", "value", "message");
Log.Error("처리 실패: {@Error}", error);
// 출력: {"ErrorCode": "CODE", "CurrentValue": "value", "Message": "message"}
```

**Why this matters (왜 중요한가):**
- LanguageExt Error 타입이 JSON 형태로 깔끔하게 로깅됨
- 6가지 Error 타입 모두 지원 (Expected, Exceptional, ErrorCodeExpected, ErrorCodeExceptional, ErrorCodeExpected<T>, ManyErrors)
- 로그 분석 도구에서 오류 필터링 및 집계 가능

<!-- 관련 커밋: cda0a33, afd1a42 -->

---

#### 3. OpenTelemetry 통합 빌더 (OpenTelemetryBuilder)

`OpenTelemetryBuilder`는 OpenTelemetry의 Traces, Metrics, Logs를 단일 Fluent API로 구성할 수 있게 해주는 빌더입니다.

```csharp
using Functorium.Abstractions.Registrations;
using Functorium.Adapters.Observabilities.Builders;

// appsettings.json의 OpenTelemetry 섹션에서 옵션 로드
services.RegisterObservability(configuration)
    .ConfigureTraces(tracing => tracing
        .AddSource("MyApp.Service")
        .Configure(builder => builder.AddHttpClientInstrumentation()))
    .ConfigureMetrics(metrics => metrics
        .AddMeter("MyApp.Metrics")
        .AddInstrumentation(builder => builder.AddRuntimeInstrumentation()))
    .ConfigureSerilog(logging => logging
        .AddDestructuringPolicy<ErrorsDestructuringPolicy>()
        .AddEnricher<EnvironmentEnricher>())
    .ConfigureStartupLogger(logger =>
        logger.LogInformation("애플리케이션 시작"))
    .Build();
```

**Why this matters (왜 중요한가):**
- Traces, Metrics, Logs 구성을 한 곳에서 관리하여 설정 분산 방지
- OTLP Exporter 엔드포인트를 구성 파일에서 관리 (Grpc, HttpProtobuf 지원)
- StartupLogger로 애플리케이션 시작 시 구성 정보 로깅 가능

<!-- 관련 커밋: 1790c73, 7d9f182, 08a1af8, c9894fc, 8646736, 51533b1 -->

---

#### 4. OpenTelemetry 구성 옵션 (OpenTelemetryOptions)

`OpenTelemetryOptions`는 OpenTelemetry 관련 설정을 구조화하고 FluentValidation으로 검증합니다.

```csharp
// appsettings.json
{
  "OpenTelemetry": {
    "ServiceName": "MyService",
    "CollectorEndpoint": "http://localhost:4317",
    "CollectorProtocol": "Grpc",
    "SamplingRate": 1.0,
    "EnablePrometheusExporter": true,
    "TracingCollectorEndpoint": null,
    "MetricsCollectorEndpoint": null,
    "LoggingCollectorEndpoint": null
  }
}
```

**Why this matters (왜 중요한가):**
- 신호별(Traces, Metrics, Logs) 개별 엔드포인트 설정 지원
- FluentValidation 기반 구성 검증으로 잘못된 설정 조기 발견
- Prometheus Exporter 활성화 옵션으로 메트릭 수집 유연성 제공

<!-- 관련 커밋: 1790c73 -->

---

#### 5. FinT 모나드 확장 (FinTUtilites)

`FinTUtilites`는 LanguageExt의 `FinT` 모나드에 대한 확장 메서드를 제공합니다. 특히 `TraverseSerial`은 OpenTelemetry Activity 추적을 지원합니다.

```csharp
using Functorium.Applications.Linq;
using LanguageExt;

// Activity 추적이 포함된 순차 처리
var activitySource = new ActivitySource("MyApp");
var results = await items.ToSeq()
    .TraverseSerial(
        item => ProcessItemAsync(item),
        activitySource,
        operationName: "ProcessItems",
        getItemIdentifier: (item, index) => $"Item_{index}")
    .Run()
    .RunAsync();

// 기본 순차 처리
var simpleResults = await items.ToSeq()
    .TraverseSerial(item => ProcessItemAsync(item))
    .Run()
    .RunAsync();
```

**Why this matters (왜 중요한가):**
- 컬렉션의 각 항목 처리를 개별 Activity Span으로 추적 가능
- 분산 추적에서 각 항목의 처리 시간 및 오류 상태 확인 가능
- FinT 모나드의 오류 처리와 OpenTelemetry 추적을 자연스럽게 통합

<!-- 관련 커밋: 4683281 feat(linq): TraverseSerial 메서드 및 Activity Context 유틸리티 추가 -->

---

#### 6. FluentValidation 옵션 구성 (OptionsConfigurator)

`OptionsConfigurator`는 IOptions 패턴과 FluentValidation을 통합하여 구성 옵션을 검증합니다.

```csharp
using Functorium.Adapters.Options;

// 옵션 등록 및 검증 설정
services.RegisterConfigureOptions<MyOptions, MyOptionsValidator>("MySection");

// 서비스에서 옵션 사용
public class MyService
{
    public MyService(IOptions<MyOptions> options)
    {
        // 검증된 옵션 사용
        var config = options.Value;
    }
}

// 옵션 즉시 조회
var options = services.GetOptions<MyOptions>();
```

**Why this matters (왜 중요한가):**
- 애플리케이션 시작 시 구성 검증으로 런타임 오류 방지
- FluentValidation의 풍부한 검증 규칙 활용 가능
- IOptions/IOptionsSnapshot/IOptionsMonitor 패턴과 완벽 호환

<!-- 관련 커밋: 4edcf7f refactor(options): OptionsUtilities를 OptionsConfigurator로 교체 -->

---

#### 7. Adapter 패턴 관측성 인터페이스

외부 시스템과의 통신을 추상화하는 Adapter 패턴에 대한 관측성 인터페이스를 제공합니다.

```csharp
using Functorium.Applications.Observabilities;

public class UserRepositoryAdapter : IAdapter
{
    private readonly IAdapterMetric _metric;
    private readonly IAdapterTrace _trace;

    public string RequestCategory => "Database";

    public async Task<Fin<User>> GetUserAsync(int userId)
    {
        var startTime = DateTimeOffset.UtcNow;
        var parentContext = TraceParentContextHolder.GetCurrent();
        var activity = _trace.Request(parentContext.Value, RequestCategory,
            nameof(UserRepositoryAdapter), nameof(GetUserAsync), startTime);

        try
        {
            var result = await _repository.FindAsync(userId);
            _trace.ResponseSuccess(activity, elapsedMs);
            _metric.ResponseSuccess(activity, RequestCategory, elapsedMs);
            return result;
        }
        catch (Exception ex)
        {
            var error = ErrorCodeFactory.CreateFromException("DB_ERROR", ex);
            _trace.ResponseFailure(activity, elapsedMs, error);
            _metric.ResponseFailure(activity, RequestCategory, elapsedMs, error);
            return error;
        }
    }
}
```

**Why this matters (왜 중요한가):**
- Adapter 계층의 요청/응답을 일관되게 추적 및 측정
- 성공/실패 메트릭 자동 기록으로 SLI/SLO 모니터링 가능
- TraceParentContextHolder로 분산 추적 컨텍스트 전파

<!-- 관련 커밋: 51533b1 refactor(observability): Observability 추상화 및 구조 개선 -->

---

### Functorium.Testing 라이브러리

#### 1. 아키텍처 검증 (ClassValidator, MethodValidator)

ArchUnitNET 기반의 Fluent API로 아키텍처 규칙을 검증합니다.

```csharp
using Functorium.Testing.ArchitectureRules;
using ArchUnitNET.Loader;

// 아키텍처 로드
var architecture = new ArchLoader()
    .LoadAssemblies(typeof(MyClass).Assembly)
    .Build();

// 클래스 검증 규칙 정의
var result = Classes()
    .That().ResideInNamespace("MyApp.Domain")
    .ValidateAllClasses(architecture, validator =>
    {
        validator
            .RequireSealed()
            .RequireImmutable()
            .RequireAllPrivateConstructors()
            .RequireImplementsGenericInterface("IEntity")
            .RequireAllMethods(method => method
                .RequireStatic()
                .RequireReturnTypeOfDeclaringClass());
    });

result.ThrowIfAnyFailures("Domain Entity Rules");
```

**Why this matters (왜 중요한가):**
- 아키텍처 규칙을 코드로 정의하여 CI/CD 파이프라인에서 자동 검증
- Fluent API로 복잡한 검증 규칙도 가독성 있게 표현
- 중첩 클래스, 제네릭 인터페이스 등 고급 검증 지원

<!-- 관련 커밋: 0282d23 feat(testing): 테스트 헬퍼 라이브러리 소스 구조 추가 -->

---

#### 2. 호스트 통합 테스트 (HostTestFixture)

`HostTestFixture<TProgram>`은 WebApplicationFactory 기반의 통합 테스트 픽스처입니다.

```csharp
using Functorium.Testing.Arrangements.Hosting;

public class ApiTests : IClassFixture<HostTestFixture<Program>>
{
    private readonly HostTestFixture<Program> _fixture;

    public ApiTests(HostTestFixture<Program> fixture) => _fixture = fixture;

    [Fact]
    public async Task GetUsers_ReturnsOk()
    {
        // Arrange & Act
        var response = await _fixture.Client.GetAsync("/api/users");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public void CanResolveService()
    {
        // Services 속성으로 DI 컨테이너 접근
        var service = _fixture.Services.GetRequiredService<IUserService>();
        Assert.NotNull(service);
    }
}
```

**Why this matters (왜 중요한가):**
- xUnit IAsyncLifetime 구현으로 비동기 초기화/정리 지원
- Client 속성으로 HTTP 요청 즉시 전송 가능
- Services 속성으로 DI 컨테이너 직접 접근하여 서비스 검증 가능

<!-- 관련 커밋: 0282d23, 9094097 refactor(testing): ControllerTestFixture를 HostTestFixture로 이름 변경 -->

---

#### 3. Quartz Job 테스트 (QuartzTestFixture)

`QuartzTestFixture<TProgram>`은 Quartz.NET 스케줄러 Job을 테스트하기 위한 픽스처입니다.

```csharp
using Functorium.Testing.Arrangements.ScheduledJobs;

public class JobTests : IClassFixture<QuartzTestFixture<Program>>
{
    private readonly QuartzTestFixture<Program> _fixture;

    public JobTests(QuartzTestFixture<Program> fixture) => _fixture = fixture;

    [Fact]
    public async Task DataSyncJob_ExecutesSuccessfully()
    {
        // Arrange & Act
        var result = await _fixture.ExecuteJobOnceAsync<DataSyncJob>(
            timeout: TimeSpan.FromSeconds(30));

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.Exception);
        Assert.True(result.ExecutionTime < TimeSpan.FromSeconds(10));
    }
}
```

**Why this matters (왜 중요한가):**
- Job을 즉시 실행하고 완료를 대기하는 간편한 API
- JobExecutionResult로 성공/실패, 실행 시간, 예외 정보 확인
- JobCompletionListener로 여러 Job 실행 결과 수집 가능

<!-- 관련 커밋: 0282d23 feat(testing): 테스트 헬퍼 라이브러리 소스 구조 추가 -->

---

#### 4. 로깅 테스트 유틸리티

Serilog 로그를 캡처하고 검증하기 위한 유틸리티를 제공합니다.

```csharp
using Functorium.Testing.Arrangements.Loggers;
using Functorium.Testing.Arrangements.Logging;
using Functorium.Testing.Assertions.Logging;

// 로그 캡처 설정
var logEvents = new List<LogEvent>();
var logger = new LoggerConfiguration()
    .WriteTo.Sink(new TestSink(logEvents))
    .CreateLogger();

// 테스트 실행
var testLogger = new StructuredTestLogger<MyService>(logger);
var service = new MyService(testLogger);
service.DoWork();

// 로그 검증
var logData = LogEventPropertyExtractor.ExtractLogData(logEvents);
Assert.Contains(logData, d => d.ToString().Contains("작업 완료"));
```

**Why this matters (왜 중요한가):**
- 로그 출력을 메모리에 캡처하여 테스트에서 검증 가능
- 구조화된 로그 속성을 객체로 변환하여 상세 검증 가능
- Microsoft.Extensions.Logging.ILogger<T> 인터페이스 호환

<!-- 관련 커밋: 922c7b3 refactor(testing): 로깅 테스트 유틸리티 재구성 -->

---

## 버그 수정

- NuGet 패키지 아이콘 경로가 잘못 설정된 문제 수정 (a8ec763)

## API 변경사항

### Functorium 네임스페이스 구조

```
Functorium.Abstractions
├── Errors/
│   ├── ErrorCodeFactory
│   └── DestructuringPolicies/
│       ├── ErrorsDestructuringPolicy
│       ├── IErrorDestructurer
│       └── ErrorTypes/ (6개 Destructurer)
├── Registrations/
│   └── OpenTelemetryRegistration
└── Utilities/
    ├── DictionaryUtilities
    ├── IEnumerableUtilities
    └── StringUtilities

Functorium.Adapters
├── Observabilities/
│   ├── OpenTelemetryOptions
│   ├── IOpenTelemetryOptions
│   ├── Builders/
│   │   ├── OpenTelemetryBuilder
│   │   └── Configurators/
│   │       ├── LoggingConfigurator
│   │       ├── MetricsConfigurator
│   │       └── TracingConfigurator
│   └── Logging/
│       ├── IStartupOptionsLogger
│       └── StartupLogger
└── Options/
    └── OptionsConfigurator

Functorium.Applications
├── Linq/
│   └── FinTUtilites
├── Observabilities/
│   ├── IAdapter
│   ├── IAdapterMetric
│   ├── IAdapterTrace
│   └── TraceParentActivityHolder
└── TraceParentContextHolder
```

### Functorium.Testing 네임스페이스 구조

```
Functorium.Testing
├── ArchitectureRules/
│   ├── ArchitectureValidationEntryPoint
│   ├── ClassValidator
│   ├── MethodValidator
│   └── ValidationResultSummary
├── Arrangements/
│   ├── Hosting/
│   │   └── HostTestFixture<TProgram>
│   ├── Loggers/
│   │   └── TestSink
│   ├── Logging/
│   │   └── StructuredTestLogger<T>
│   └── ScheduledJobs/
│       ├── JobCompletionListener
│       ├── JobExecutionResult
│       └── QuartzTestFixture<TProgram>
└── Assertions/
    └── Logging/
        ├── LogEventPropertyExtractor
        ├── LogEventPropertyValueConverter
        └── SerilogTestPropertyValueFactory
```

## 설치

### NuGet 패키지 설치

```bash
# Functorium 핵심 라이브러리
dotnet add package Functorium --version 1.0.0-alpha.1.2

# Functorium 테스트 라이브러리 (선택적)
dotnet add package Functorium.Testing --version 1.0.0-alpha.1.2
```

### 필수 의존성

- .NET 10 이상
- LanguageExt.Core 5.0.0-beta-58
- OpenTelemetry 1.x
- Serilog 4.x
- FluentValidation 11.x
