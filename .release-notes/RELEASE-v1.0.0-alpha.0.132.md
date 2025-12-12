---
title: Functorium v1.0.0-alpha.0 새로운 기능
description: Functorium v1.0.0-alpha.0의 새로운 기능을 알아봅니다.
date: 2025-12-12
---

# Functorium v1.0.0-alpha.0 새로운 기능

Functorium v1.0.0-alpha.0은 .NET 개발자를 위한 함수형 프로그래밍 기반 라이브러리의 첫 번째 릴리스입니다. LanguageExt와 통합된 구조화된 오류 처리, OpenTelemetry 기반 관찰성, 그리고 테스트 유틸리티를 제공합니다.

## 핵심 기능

### 구조화된 오류 처리 시스템

LanguageExt의 `Error` 타입을 확장하여 오류 코드 기반의 구조화된 오류 처리를 제공합니다.

```csharp
// 기본 오류 생성
var error = ErrorCodeFactory.Create("VALIDATION_001", invalidValue, "값이 유효하지 않습니다");

// 제네릭 오류 생성 (타입 안전)
var typedError = ErrorCodeFactory.Create<int>("USER_001", userId, "사용자를 찾을 수 없습니다");

// 다중 값 오류 생성
var multiError = ErrorCodeFactory.Create<string, int>(
    "ORDER_001",
    productCode,
    quantity,
    "재고가 부족합니다");

// 예외에서 구조화된 오류 생성
try
{
    await SomeOperationAsync();
}
catch (Exception ex)
{
    var error = ErrorCodeFactory.CreateFromException("SYSTEM_001", ex);
    return Fin<Result>.Fail(error);
}
```

**주요 API:**
- `ErrorCodeFactory.Create(string errorCode, string errorCurrentValue, string errorMessage)`
- `ErrorCodeFactory.Create<T>(string errorCode, T errorCurrentValue, string errorMessage)`
- `ErrorCodeFactory.Create<T1, T2>(...)` - 2개 값 지원
- `ErrorCodeFactory.Create<T1, T2, T3>(...)` - 3개 값 지원
- `ErrorCodeFactory.CreateFromException(string errorCode, Exception exception)`
- `ErrorCodeFactory.Format(params string[] parts)` - 오류 코드 포맷팅

### Serilog 오류 디스트럭처링

LanguageExt 오류 타입에 대한 구조화된 로깅을 자동으로 지원합니다.

```csharp
// ErrorsDestructuringPolicy를 통한 자동 디스트럭처링
Log.Error("작업 실패: {@Error}", error);
// 출력: ErrorCode=VALIDATION_001, Message="값이 유효하지 않습니다", ...
```

**지원되는 디스트럭처:**
- `ErrorCodeExceptionalDestructurer` - 예외 기반 오류 코드
- `ErrorCodeExpectedDestructurer` - 예상된 오류 코드
- `ErrorCodeExpectedTDestructurer` - 제네릭 타입 오류 코드
- `ExceptionalDestructurer` - 기본 예외 오류
- `ExpectedDestructurer` - 기본 예상 오류
- `ManyErrorsDestructurer` - 복합 오류 (여러 오류 결합)

**디스트럭처링 정책 인터페이스:**
```csharp
public interface IErrorDestructurer
{
    bool CanHandle(LanguageExt.Common.Error error);
    LogEventPropertyValue Destructure(LanguageExt.Common.Error error, ILogEventPropertyValueFactory factory);
}
```

### OpenTelemetry 통합 구성

OpenTelemetry와 Serilog를 통합한 관찰성 구성을 제공합니다.

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .RegisterObservability(builder.Configuration)
    .ConfigureSerilog(logging => logging
        .AddDestructuringPolicy<ErrorsDestructuringPolicy>()
        .AddEnricher<MyEnricher>())
    .ConfigureTraces(tracing => tracing
        .AddSource("MyApp.Tracing")
        .Configure(builder => builder.AddHttpClientInstrumentation()))
    .ConfigureMetrics(metrics => metrics
        .AddMeter("MyApp.Metrics")
        .AddInstrumentation(builder => builder.AddRuntimeInstrumentation()))
    .ConfigureStartupLogger(logger =>
        logger.LogInformation("애플리케이션 시작"))
    .Build();
```

**OpenTelemetry 구성 옵션:**
```json
{
  "OpenTelemetry": {
    "ServiceName": "MyService",
    "CollectorEndpoint": "http://localhost:4317",
    "CollectorProtocol": "Grpc",
    "SamplingRate": 1.0,
    "EnablePrometheusExporter": true
  }
}
```

**주요 Configurator API:**
- `LoggingConfigurator.AddDestructuringPolicy<TPolicy>()`
- `LoggingConfigurator.AddEnricher<TEnricher>()`
- `LoggingConfigurator.Configure(Action<LoggerConfiguration>)`
- `TracingConfigurator.AddSource(string sourceName)`
- `TracingConfigurator.AddProcessor(BaseProcessor<Activity>)`
- `MetricsConfigurator.AddMeter(string meterName)`
- `MetricsConfigurator.AddInstrumentation(Action<MeterProviderBuilder>)`

### FluentValidation 기반 옵션 구성

FluentValidation을 활용한 강력한 옵션 검증을 제공합니다.

```csharp
// 옵션 등록 및 검증
builder.Services.RegisterConfigureOptions<OpenTelemetryOptions, OpenTelemetryOptions.Validator>(
    OpenTelemetryOptions.SectionName);

// 옵션 조회
var options = builder.Services.GetOptions<OpenTelemetryOptions>();
```

**OptionsConfigurator API:**
- `RegisterConfigureOptions<TOptions, TValidator>(string configurationSectionName)`
- `GetOptions<TOptions>()` - 서비스 컬렉션에서 옵션 조회

### LanguageExt Fin/FinT LINQ 확장

LanguageExt의 `Fin<T>` 및 `FinT<M, A>` 타입에 대한 LINQ 확장을 제공합니다.

```csharp
// Fin<A> 필터링
var result = fin.Filter(x => x > 0);

// IO와 FinT 조합
var combined =
    from a in ioOperation
    from b in finTOperation(a)
    select a + b;
```

**FinTUtilites API:**
- `Filter<A>(this Fin<A> fin, Func<A, bool> predicate)`
- `Filter<M, A>(this FinT<M, A> finT, Func<A, bool> predicate)`
- `SelectMany<A, B>(this IO<A> io, Func<A, B> selector)`
- `SelectMany<A, B, C>(...)` - IO와 FinT 조합

### 유틸리티 확장 메서드

자주 사용되는 유틸리티 확장 메서드를 제공합니다.

```csharp
// Dictionary 확장
dictionary.AddOrUpdate(key, value);

// IEnumerable 확장
items.Any();
items.IsEmpty();
items.Join(", ");
items.Join(',');

// String 확장
str.Empty();
str.NotEmpty();
str.NotContains("substring");
str.NotEquals("other");
str.ConvertToInt();
str.ConvertToDouble();
str.TryConvertToDouble();
str.Replace(new[] { "a", "b" }, "c");
```

## Functorium.Testing 테스트 유틸리티

### 아키텍처 검증 규칙

ArchUnitNET 기반의 아키텍처 검증 유틸리티를 제공합니다.

```csharp
// 클래스 아키텍처 규칙 검증
var summary = Types.InAssembly(assembly)
    .That().AreClasses()
    .ValidateAllClasses(architecture, validator =>
    {
        validator
            .RequireSealed()
            .RequirePublic()
            .RequireImplements(typeof(IMyInterface))
            .RequireAllMethods(method => method.RequireStatic());
    });

summary.ThrowIfAnyFailures("My Architecture Rules");
```

**ClassValidator API:**
- `RequireSealed()` / `RequirePublic()` / `RequireInternal()`
- `RequireImmutable()` - 불변 클래스 검증
- `RequireImplements(Type interfaceType)`
- `RequireImplementsGenericInterface(string genericInterfaceName)`
- `RequireInherits(Type baseType)`
- `RequireAllPrivateConstructors()` / `RequirePrivateAnyParameterlessConstructor()`
- `RequireMethod(string methodName, Action<MethodValidator>)`
- `RequireAllMethods(Action<MethodValidator>)`
- `RequireNestedClass(string nestedClassName, ...)`

**MethodValidator API:**
- `RequireStatic()`
- `RequireVisibility(Visibility visibility)`
- `RequireReturnType(Type returnType)`
- `RequireReturnTypeOfDeclaringClass()`

### 호스트 테스트 픽스처

ASP.NET Core 통합 테스트를 위한 테스트 픽스처를 제공합니다.

```csharp
public class MyIntegrationTests : IClassFixture<HostTestFixture<Program>>
{
    private readonly HostTestFixture<Program> _fixture;

    public MyIntegrationTests(HostTestFixture<Program> fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Should_Return_Success()
    {
        var response = await _fixture.Client.GetAsync("/api/health");
        response.EnsureSuccessStatusCode();
    }
}
```

**HostTestFixture API:**
- `Client` - 테스트용 HttpClient
- `Services` - 서비스 프로바이더
- `ConfigureHost(IWebHostBuilder builder)` - 호스트 구성 커스터마이징

### Quartz 스케줄러 테스트 픽스처

Quartz.NET 작업 테스트를 위한 픽스처를 제공합니다.

```csharp
public class MyJobTests : IClassFixture<QuartzTestFixture<Program>>
{
    private readonly QuartzTestFixture<Program> _fixture;

    [Fact]
    public async Task Job_Should_Execute_Successfully()
    {
        var result = await _fixture.ExecuteJobOnceAsync<MyJob>(TimeSpan.FromSeconds(30));

        Assert.True(result.Success);
        Assert.Null(result.Exception);
    }
}
```

**QuartzTestFixture API:**
- `Scheduler` - Quartz 스케줄러
- `JobListener` - 작업 완료 리스너
- `ExecuteJobOnceAsync<TJob>(TimeSpan timeout)` - 단일 작업 실행

**JobExecutionResult:**
- `JobName`, `Success`, `Result`, `Exception`, `ExecutionTime`

### Serilog 테스트 유틸리티

Serilog 로그 이벤트 검증을 위한 유틸리티를 제공합니다.

```csharp
// 로그 이벤트에서 데이터 추출
var logData = LogEventPropertyExtractor.ExtractLogData(logEvent);
var allLogData = LogEventPropertyExtractor.ExtractLogData(logEvents);

// PropertyValue를 익명 객체로 변환
var obj = LogEventPropertyValueConverter.ToAnonymousObject(propertyValue);

// 테스트용 PropertyValueFactory
var factory = new SerilogTestPropertyValueFactory();
var value = factory.CreatePropertyValue(myObject, destructureObjects: true);
```

**LogEventPropertyExtractor API:**
- `ExtractLogData(LogEvent logEvent)`
- `ExtractLogData(IEnumerable<LogEvent> logEvents)`
- `ExtractValue(LogEventPropertyValue propertyValue)`

**테스트 싱크:**
```csharp
var logEvents = new List<LogEvent>();
var logger = new LoggerConfiguration()
    .WriteTo.Sink(new TestSink(logEvents))
    .CreateLogger();
```

## 문서 및 가이드

이번 릴리스에는 다음 문서가 포함되어 있습니다:

- **아키텍처 가이드** - Clean Architecture 기반 설계 원칙
- **단위 테스트 가이드** - xUnit 기반 테스트 작성법
- **통합 테스트 가이드** - ASP.NET Core 통합 테스트
- **오류 처리 가이드** - ErrorCodeFactory 활용법
- **Options 구성 가이드** - FluentValidation 기반 옵션 검증
- **OpenTelemetry 가이드** - 관찰성 구성
- **CI/CD 가이드** - GitHub Actions, MinVer, NuGet 배포

## 기술 스택

- **.NET 10.0** 지원
- **LanguageExt 5.0.0-beta-58** - 함수형 프로그래밍 라이브러리
- **Serilog** - 구조화된 로깅
- **OpenTelemetry** - 분산 추적 및 메트릭
- **FluentValidation** - 옵션 검증
- **Ardalis.SmartEnum** - 스마트 열거형
- **ArchUnitNET** - 아키텍처 검증 (Testing)
- **Quartz.NET** - 스케줄러 테스트 (Testing)

## 설치

```bash
# 핵심 라이브러리
dotnet add package Functorium

# 테스트 유틸리티
dotnet add package Functorium.Testing
```

## 피드백

버그 리포트, 기능 요청 또는 피드백은 [GitHub Issues](https://github.com/hhko/Functorium/issues)에서 받고 있습니다.
