---
title: Functorium v1.0.0-alpha.1.4 새로운 기능
description: Functorium v1.0.0-alpha.1.4의 새로운 기능을 알아봅니다.
date: 2025-12-23
---

# Functorium Release v1.0.0-alpha.1.4

## 개요

Functorium v1.0.0-alpha.1.4는 함수형 프로그래밍과 도메인 주도 설계(DDD)를 결합한 .NET 라이브러리의 첫 번째 알파 릴리스입니다. 이 릴리스는 OpenTelemetry 통합, Adapter Pipeline DI 등록, LanguageExt 기반 LINQ 유틸리티, 그리고 포괄적인 테스트 인프라를 제공합니다.

**주요 기능**:

- **OpenTelemetry 통합**: Fluent API 빌더를 통한 추적, 메트릭, 로깅 통합 구성
- **AdapterPipeline DI 등록**: ActivityContext 전파가 포함된 Adapter 의존성 주입 유틸리티
- **FinT LINQ 유틸리티**: LanguageExt FinT 모나드를 위한 TraverseSerial 및 합성 메서드
- **테스트 인프라**: ASP.NET Core, Quartz, Source Generator, 아키텍처 검증 테스트 픽스처

## Breaking Changes

이번 릴리스에는 Breaking Changes가 없습니다. (첫 배포)

## 새로운 기능

### Functorium 라이브러리

#### 1. OpenTelemetry 통합

Serilog 로깅, OpenTelemetry 추적 및 메트릭을 하나의 Fluent API로 통합 구성할 수 있습니다. `appsettings.json` 구성을 읽어 OTLP Collector 엔드포인트, 프로토콜(gRPC/HTTP), 샘플링 레이트 등을 자동으로 설정합니다.

```csharp
// 서비스 등록
services
    .RegisterOpenTelemetry(configuration, typeof(Program).Assembly)
    .ConfigureSerilog(logging => logging
        .AddDestructuringPolicy<ErrorsDestructuringPolicy>()
        .AddEnricher<MyCustomEnricher>())
    .ConfigureTraces(tracing => tracing
        .AddSource("MyApp.Operations")
        .AddProcessor(new MyProcessor()))
    .ConfigureMetrics(metrics => metrics
        .AddMeter("MyApp.Metrics")
        .AddInstrumentation(builder => builder.AddHttpClientInstrumentation()))
    .ConfigureStartupLogger(logger => logger.LogInformation("App starting..."))
    .Build();
```

**Why this matters (왜 중요한가):**
- **통합 구성**: Serilog, OpenTelemetry Tracing, Metrics를 개별 구성하던 50줄 이상의 보일러플레이트를 5줄로 단축
- **일관된 리소스 속성**: 서비스명, 버전, 환경이 Logging/Tracing/Metrics에 자동 적용
- **프로토콜 유연성**: gRPC와 HTTP/protobuf를 신호별(Tracing/Metrics/Logging)로 개별 구성 가능
- **FluentValidation 통합**: 잘못된 옵션 값을 애플리케이션 시작 시 검증하여 런타임 오류 방지

<!-- 관련 커밋: 9c188e1 feat(opentelemetry): Assembly 매개변수로 네임스페이스 자동 등록 -->
<!-- 관련 커밋: 7d9f182 feat(observability): OpenTelemetry 의존성 등록 확장 메서드 추가 -->
<!-- 관련 커밋: 1790c73 feat(observability): OpenTelemetry 및 Serilog 통합 구성 추가 -->

---

#### 2. AdapterPipeline DI 등록

Hexagonal Architecture(포트-어댑터 패턴)에서 Adapter를 DI 컨테이너에 등록할 때 ActivityContext 전파를 자동으로 처리합니다. 분산 추적에서 Parent Activity를 Adapter 호출에 연결하는 보일러플레이트 코드를 제거합니다.

```csharp
// 단일 인터페이스 등록
services.RegisterScopedAdapterPipeline<IUserRepository, UserRepository>();

// Factory 패턴 - ActivityContext를 받아 인스턴스 생성
services.RegisterScopedAdapterPipeline<IUserRepository>(
    activityContext => new UserRepository(activityContext));

// 다중 인터페이스 등록 (하나의 구현이 여러 인터페이스를 구현)
services.RegisterScopedAdapterPipelineFor<IUserReader, IUserWriter, UserRepository>();

// Transient/Singleton 라이프타임 지원
services.RegisterTransientAdapterPipeline<ILogger, ConsoleLogger>();
services.RegisterSingletonAdapterPipeline<ICache, MemoryCache>();
```

**Why this matters (왜 중요한가):**
- **분산 추적 연속성**: HTTP 요청 → Use Case → Adapter 호출까지 Trace가 끊기지 않고 연결
- **보일러플레이트 제거**: ActivityContext 전파 코드를 수동 작성하던 것을 자동화
- **다중 인터페이스 지원**: 하나의 Adapter가 여러 인터페이스를 구현할 때 한 번의 등록으로 처리
- **라이프타임 유연성**: Scoped, Transient, Singleton을 Use Case에 맞게 선택

<!-- 관련 커밋: b036c38 feat(registration): AdapterPipeline DI 등록 유틸리티 추가 -->

---

#### 3. FinT LINQ 유틸리티

LanguageExt의 `FinT<M, A>` 모나드 변환자를 위한 LINQ 확장 메서드를 제공합니다. `TraverseSerial`은 컬렉션의 각 요소에 대해 순차적으로 `FinT` 연산을 수행하며, OpenTelemetry Activity와 통합됩니다.

```csharp
// TraverseSerial - 순차 실행 + Activity Span 자동 생성
var results = await userIds
    .ToSeq()
    .TraverseSerial(
        userId => GetUserAsync(userId),
        activitySource,
        operationName: "FetchUsers",
        getItemIdentifier: (id, index) => $"User-{id}")
    .Run();

// Filter - Fin<A>에 조건부 필터링
var validUser = GetUser().Filter(user => user.IsActive);

// SelectMany - IO와 FinT 합성
var result = from config in LoadConfig()
             from user in GetUser(config.UserId)
             select user.Name;
```

**Why this matters (왜 중요한가):**
- **배치 작업 추적**: 컬렉션 순회 시 각 항목마다 Child Span이 자동 생성되어 성능 병목 식별 용이
- **실패 격리**: 한 항목의 실패가 전체 배치를 중단하지 않고, 에러를 수집하여 반환
- **LINQ 구문 지원**: 기존 C# LINQ 구문(`from-in-select`)으로 모나드 합성 가능
- **Monad Transformer 패턴**: `IO`와 `Fin`을 결합한 `FinT<IO, A>`로 비동기 + 에러 처리 통합

<!-- 관련 커밋: 4683281 feat(linq): TraverseSerial 메서드 및 Activity Context 유틸리티 추가 -->

---

#### 4. 에러 처리 및 구조화 로깅

에러 코드 기반의 오류 생성과 Serilog 구조화 로깅을 위한 Destructuring 정책을 제공합니다. LanguageExt `Error` 타입을 Serilog 로그에 구조화된 형태로 기록합니다.

```csharp
// 에러 코드 기반 오류 생성
var error = ErrorCodeFactory.Create(
    errorCode: "USER_NOT_FOUND",
    errorCurrentValue: userId,
    errorMessage: "사용자를 찾을 수 없습니다");

// 복합 에러 코드 포맷팅
var code = ErrorCodeFactory.Format("PAYMENT", "VALIDATION", "AMOUNT_INVALID");
// 결과: "PAYMENT.VALIDATION.AMOUNT_INVALID"

// Exception을 Error로 변환
var error = ErrorCodeFactory.CreateFromException("DB_CONNECTION_ERROR", exception);

// Serilog에서 자동 구조화 로깅
Log.Error("Operation failed: {@Error}", error);
// 출력: { ErrorCode: "USER_NOT_FOUND", CurrentValue: "12345", Message: "..." }
```

**Why this matters (왜 중요한가):**
- **에러 추적성**: 에러 코드로 로그 검색 및 알림 규칙 설정 가능
- **현재 값 포함**: 실패 시점의 입력 값이 로그에 포함되어 디버깅 시간 단축
- **Serilog 통합**: `Error` 타입이 자동으로 구조화된 JSON으로 기록
- **타입별 Destructurer**: `Exceptional`, `Expected`, `ManyErrors` 등 에러 타입별 최적화된 로깅

<!-- 관련 커밋: cda0a33 feat(functorium): 핵심 라이브러리 패키지 참조 및 소스 구조 추가 -->

---

#### 5. 옵션 구성

FluentValidation과 통합된 옵션 등록 유틸리티입니다. `appsettings.json` 섹션을 읽어 옵션 객체에 바인딩하고, 유효성 검사를 자동으로 수행합니다.

```csharp
// FluentValidation 연동 옵션 등록
services.RegisterConfigureOptions<OpenTelemetryOptions, OpenTelemetryOptions.Validator>(
    OpenTelemetryOptions.SectionName);

// 등록된 옵션 즉시 조회 (서비스 등록 시점에 필요한 경우)
var options = services.GetOptions<OpenTelemetryOptions>();
```

**Why this matters (왜 중요한가):**
- **시작 시 검증**: 잘못된 구성을 애플리케이션 시작 시 감지하여 런타임 오류 방지
- **FluentValidation 재사용**: 기존 Validator를 그대로 사용하여 일관된 검증 로직
- **즉시 조회**: DI 컨테이너 빌드 전에 옵션 값이 필요한 경우 `GetOptions` 사용

<!-- 관련 커밋: 4edcf7f refactor(options): OptionsUtilities를 OptionsConfigurator로 교체 -->

---

#### 6. Activity Context 관리

분산 추적에서 현재 Activity와 ActivityContext를 관리하는 유틸리티입니다. AsyncLocal을 사용하여 비동기 호출 체인에서 컨텍스트가 유지됩니다.

```csharp
// ActivityContext를 현재 스레드에 설정 (파이프라인 시작점)
using (TraceParentContextHolder.Enter(incomingContext))
{
    // 이 스코프 내에서 GetCurrent()로 컨텍스트 조회 가능
    var currentContext = TraceParentContextHolder.GetCurrent();
    await ProcessAsync();
}

// Activity를 현재 스레드에 설정
using (TraceParentActivityHolder.Enter(parentActivity))
{
    var current = TraceParentActivityHolder.GetCurrent();
    // Child Activity 생성 시 parent로 사용
}

// LanguageExt Unit 반환 버전 (파이프라인에서 사용)
var _ = TraceParentContextHolder.SetCurrentUnit(context);
```

**Why this matters (왜 중요한가):**
- **암묵적 전파**: 메서드 파라미터로 ActivityContext를 전달하지 않아도 됨
- **AsyncLocal 기반**: 비동기 호출 체인에서 컨텍스트가 자동 유지
- **RAII 패턴**: `using` 블록으로 스코프 종료 시 자동 정리

<!-- 관련 커밋: 4683281 feat(linq): TraverseSerial 메서드 및 Activity Context 유틸리티 추가 -->

---

### Functorium.Testing 라이브러리

#### 1. ASP.NET Core 통합 테스트 픽스처

`WebApplicationFactory`를 래핑한 테스트 픽스처로, xUnit `IAsyncLifetime`과 통합됩니다. 환경 구성, 서비스 오버라이드, HTTP 클라이언트 생성을 간소화합니다.

```csharp
public class MyApiTests : IClassFixture<HostTestFixture<Program>>
{
    private readonly HostTestFixture<Program> _fixture;

    public MyApiTests(HostTestFixture<Program> fixture) => _fixture = fixture;

    [Fact]
    public async Task GetUser_ReturnsOk()
    {
        // Arrange
        var client = _fixture.Client;

        // Act
        var response = await client.GetAsync("/api/users/1");

        // Assert
        response.EnsureSuccessStatusCode();
    }
}

// 커스텀 픽스처 (서비스 오버라이드)
public class CustomFixture : HostTestFixture<Program>
{
    protected override string EnvironmentName => "Testing";

    protected override void ConfigureHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IUserRepository, MockUserRepository>();
        });
    }
}
```

**Why this matters (왜 중요한가):**
- **보일러플레이트 감소**: `WebApplicationFactory` 설정 코드를 상속으로 재사용
- **xUnit 통합**: `IAsyncLifetime`으로 비동기 설정/정리 자동화
- **환경 격리**: 테스트별 독립된 서비스 인스턴스

<!-- 관련 커밋: 0282d23 feat(testing): 테스트 헬퍼 라이브러리 소스 구조 추가 -->

---

#### 2. Quartz 스케줄러 테스트 픽스처

Quartz.NET 잡을 테스트하기 위한 픽스처입니다. 잡 실행을 트리거하고 완료를 대기하며, 실행 결과와 예외를 캡처합니다.

```csharp
public class MyJobTests : IClassFixture<QuartzTestFixture<Program>>
{
    private readonly QuartzTestFixture<Program> _fixture;

    [Fact]
    public async Task ProcessDataJob_Succeeds()
    {
        // Act - 잡 한 번 실행하고 완료 대기
        var result = await _fixture.ExecuteJobOnceAsync<ProcessDataJob>(
            timeout: TimeSpan.FromSeconds(30));

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.Exception);
        Assert.True(result.ExecutionTime < TimeSpan.FromSeconds(5));
    }
}
```

**Why this matters (왜 중요한가):**
- **비동기 잡 테스트**: 잡 실행 완료를 안전하게 대기하는 메커니즘 제공
- **실행 결과 캡처**: 성공/실패, 예외, 실행 시간을 `JobExecutionResult`로 제공
- **타임아웃 제어**: 잡이 응답하지 않을 때 테스트가 무한 대기하지 않음

<!-- 관련 커밋: 0282d23 feat(testing): 테스트 헬퍼 라이브러리 소스 구조 추가 -->

---

#### 3. Source Generator 테스트 러너

Roslyn Source Generator를 단위 테스트하기 위한 유틸리티입니다. 소스 코드를 입력받아 생성된 코드를 문자열로 반환합니다.

```csharp
[Fact]
public void Generator_CreatesAdapter()
{
    // Arrange
    var sourceCode = """
        [GenerateAdapter]
        public interface IUserRepository
        {
            User GetUser(int id);
        }
        """;

    // Act
    var generated = new AdapterGenerator().Generate(sourceCode);

    // Assert
    Assert.Contains("public class UserRepositoryAdapter", generated);
}
```

**Why this matters (왜 중요한가):**
- **빠른 피드백**: Generator 로직을 컴파일 없이 문자열 비교로 검증
- **간단한 API**: 복잡한 Roslyn 설정 없이 `Generate()` 한 줄로 테스트
- **Incremental Generator 지원**: `IIncrementalGenerator` 인터페이스 지원

<!-- 관련 커밋: 1fb6971 refactor(source-generator): 코드 구조 개선 및 테스트 인프라 추가 -->

---

#### 4. 아키텍처 검증

ArchUnitNET을 래핑한 Fluent API로 아키텍처 규칙을 검증합니다. 클래스와 메서드의 가시성, 불변성, 인터페이스 구현 등을 검사합니다.

```csharp
[Fact]
public void ValueObjects_ShouldBeImmutable()
{
    // Arrange
    var classes = Types.InAssembly(DomainAssembly)
        .That().ResideInNamespace("Domain.ValueObjects");

    // Act & Assert
    classes.ValidateAllClasses(architecture, validator =>
    {
        validator
            .RequireSealed()
            .RequireImmutable()
            .RequireAllPrivateConstructors();
    }).ThrowIfAnyFailures("ValueObject Immutability");
}

[Fact]
public void Factories_ShouldHaveCreateMethod()
{
    var classes = Types.InNamespace("Domain.Factories");

    classes.ValidateAllClasses(architecture, validator =>
    {
        validator
            .RequireMethod("Create", method => method
                .RequireStatic()
                .RequireReturnTypeOfDeclaringClass());
    }).ThrowIfAnyFailures("Factory Pattern");
}
```

**Why this matters (왜 중요한가):**
- **Fluent API**: ArchUnitNET의 복잡한 규칙 정의를 직관적인 체이닝으로 단순화
- **커스텀 규칙**: `ClassValidator`와 `MethodValidator`로 프로젝트별 규칙 정의
- **실패 요약**: 여러 클래스의 위반 사항을 한 번에 리포트

<!-- 관련 커밋: 0282d23 feat(testing): 테스트 헬퍼 라이브러리 소스 구조 추가 -->

---

#### 5. Serilog 테스트 유틸리티

테스트에서 로그 이벤트를 캡처하고 검증하기 위한 유틸리티입니다. 구조화된 로그 데이터를 익명 객체로 추출합니다.

```csharp
// 로그 이벤트 캡처
var logEvents = new List<LogEvent>();
var logger = new LoggerConfiguration()
    .WriteTo.Sink(new TestSink(logEvents))
    .CreateLogger();

// 테스트 실행 후 로그 검증
var extractedData = LogEventPropertyExtractor.ExtractLogData(logEvents);
Assert.Contains(extractedData, log =>
    log.Message.Contains("User created"));
```

**Why this matters (왜 중요한가):**
- **로그 캡처**: 테스트 중 발생한 로그를 메모리에 수집
- **구조화 데이터 추출**: `LogEventPropertyValue`를 일반 객체로 변환하여 쉽게 Assert
- **통합 테스트 검증**: 비즈니스 로직이 올바른 로그를 남기는지 확인

<!-- 관련 커밋: 0282d23 feat(testing): 테스트 헬퍼 라이브러리 소스 구조 추가 -->

## 버그 수정

- NuGet 패키지 아이콘 경로 수정 (a8ec763)

## API 변경사항

### Functorium 네임스페이스 구조

```
Functorium.Abstractions/
├── ElapsedTimeCalculator
├── Errors/
│   ├── ErrorCodeFactory
│   └── DestructuringPolicies/
│       ├── ErrorsDestructuringPolicy
│       └── IErrorDestructurer
├── Registrations/
│   ├── AdapterPipelineRegistration
│   └── OpenTelemetryRegistration
└── Utilities/
    ├── DictionaryUtilities
    ├── IEnumerableUtilities
    └── StringUtilities

Functorium.Adapters.Observabilities/
├── Builders/
│   ├── OpenTelemetryBuilder
│   └── Configurators/
│       ├── LoggingConfigurator
│       ├── MetricsConfigurator
│       └── TracingConfigurator
├── Logging/
│   ├── IStartupOptionsLogger
│   └── StartupLogger
├── IOpenTelemetryOptions
├── ObservabilityFields
└── OpenTelemetryOptions

Functorium.Adapters.Options/
└── OptionsConfigurator

Functorium.Applications/
├── TraceParentContextHolder
├── Linq/
│   └── FinTUtilites
└── Observabilities/
    ├── IAdapter
    ├── IAdapterMetric
    ├── IAdapterTrace
    └── TraceParentActivityHolder
```

### Functorium.Testing 네임스페이스 구조

```
Functorium.Testing/
├── AssemblyReference
├── Actions.SourceGenerators/
│   └── SourceGeneratorTestRunner
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
    ├── ArchitectureRules/
    │   ├── ArchitectureValidationEntryPoint
    │   ├── ClassValidator
    │   ├── MethodValidator
    │   └── ValidationResultSummary
    └── Logging/
        ├── LogEventPropertyExtractor
        ├── LogEventPropertyValueConverter
        └── SerilogTestPropertyValueFactory
```

## 설치

### NuGet 패키지 설치

```bash
# Functorium 핵심 라이브러리
dotnet add package Functorium --version 1.0.0-alpha.1.4

# Functorium 테스트 라이브러리 (선택적)
dotnet add package Functorium.Testing --version 1.0.0-alpha.1.4
```

### 필수 의존성

- .NET 10.0 이상
- LanguageExt.Core 5.0.0-beta-58
- OpenTelemetry.Extensions.Hosting 1.x
- Serilog.AspNetCore 9.x
