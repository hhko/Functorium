# Functorium Release v1.0.0-alpha.1

**릴리스 날짜:** 2025-12-16

## 개요

Functorium v1.0.0-alpha.1은 .NET 10 기반의 함수형 프로그래밍 유틸리티 라이브러리의 첫 번째 알파 릴리스입니다. LanguageExt와의 깊은 통합을 통해 함수형 오류 처리, OpenTelemetry 기반 관측성(Observability), 그리고 테스트 지원 도구를 제공합니다.

주요 기능:
- LanguageExt Error 타입을 위한 구조화된 오류 생성 및 Serilog 로깅 통합
- OpenTelemetry 기반 분산 추적, 메트릭, 로깅 통합 구성
- FluentValidation을 활용한 타입-안전한 Options 패턴
- ArchUnitNET 기반 아키텍처 검증 테스트 지원
- ASP.NET Core 및 Quartz.NET 통합 테스트 픽스처

## Breaking Changes

없음 (첫 릴리스)

## 새로운 기능

### 1. 함수형 오류 처리 (Error Handling)

LanguageExt의 `Error` 타입을 구조화된 형태로 생성하고, Serilog 로깅 시 자동으로 분해(destructuring)하여 로그에 기록합니다.

**ErrorCodeFactory를 통한 오류 생성:**

```csharp
using Functorium.Abstractions.Errors;

// 기본 오류 생성
var error = ErrorCodeFactory.Create(
    errorCode: "USER_NOT_FOUND",
    errorCurrentValue: "user123",
    errorMessage: "사용자를 찾을 수 없습니다");

// 제네릭 타입 오류 생성
var typedError = ErrorCodeFactory.Create<int>(
    errorCode: "INVALID_AGE",
    errorCurrentValue: -5,
    errorMessage: "나이는 양수여야 합니다");

// 다중 값 오류 생성
var multiError = ErrorCodeFactory.Create<string, int>(
    errorCode: "VALIDATION_FAILED",
    errorCurrentValue1: "email",
    errorCurrentValue2: 100,
    errorMessage: "필드 길이 초과");

// 예외로부터 오류 생성
try { /* ... */ }
catch (Exception ex)
{
    var exError = ErrorCodeFactory.CreateFromException("UNEXPECTED_ERROR", ex);
}
```

**Serilog 로깅 통합:**

```csharp
using Functorium.Abstractions.Errors.DestructuringPolicies;
using Serilog;

// Serilog 구성 시 ErrorsDestructuringPolicy 추가
var logger = new LoggerConfiguration()
    .Destructure.With<ErrorsDestructuringPolicy>()
    .CreateLogger();

// Error 객체를 구조화된 형태로 로깅
logger.Error("처리 실패: {@Error}", error);
// 출력: 처리 실패: { ErrorCode: "USER_NOT_FOUND", CurrentValue: "user123", Message: "사용자를 찾을 수 없습니다" }
```

**API:**

```csharp
namespace Functorium.Abstractions.Errors
{
    public static class ErrorCodeFactory
    {
        public static LanguageExt.Common.Error Create(string errorCode, string errorCurrentValue, string errorMessage);
        public static LanguageExt.Common.Error Create<T>(string errorCode, T errorCurrentValue, string errorMessage)
            where T : notnull;
        public static LanguageExt.Common.Error Create<T1, T2>(string errorCode, T1 errorCurrentValue1, T2 errorCurrentValue2, string errorMessage)
            where T1 : notnull where T2 : notnull;
        public static LanguageExt.Common.Error Create<T1, T2, T3>(string errorCode, T1 errorCurrentValue1, T2 errorCurrentValue2, T3 errorCurrentValue3, string errorMessage)
            where T1 : notnull where T2 : notnull where T3 : notnull;
        public static LanguageExt.Common.Error CreateFromException(string errorCode, System.Exception exception);
        public static string Format(params string[] parts);
    }
}

namespace Functorium.Abstractions.Errors.DestructuringPolicies
{
    public class ErrorsDestructuringPolicy : Serilog.Core.IDestructuringPolicy
    {
        public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue? result);
        public static LogEventPropertyValue DestructureError(Error error, ILogEventPropertyValueFactory propertyValueFactory);
    }

    public interface IErrorDestructurer
    {
        bool CanHandle(LanguageExt.Common.Error error);
        LogEventPropertyValue Destructure(LanguageExt.Common.Error error, ILogEventPropertyValueFactory factory);
    }
}
```

**지원되는 Destructurer:**
- `ErrorCodeExceptionalDestructurer` - 예외 기반 ErrorCode 오류
- `ErrorCodeExpectedDestructurer` - 예상된 ErrorCode 오류
- `ErrorCodeExpectedTDestructurer` - 제네릭 타입 ErrorCode 오류
- `ExceptionalDestructurer` - 일반 예외 오류
- `ExpectedDestructurer` - 일반 예상 오류
- `ManyErrorsDestructurer` - 다중 오류 집합

### 2. OpenTelemetry 통합 (Observability)

분산 추적(Tracing), 메트릭(Metrics), 로깅(Logging)을 OTLP 프로토콜로 통합 관리합니다.

**기본 구성:**

```csharp
using Functorium.Abstractions.Registrations;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .RegisterObservability(builder.Configuration)
    .ConfigureTraces(tracing =>
    {
        tracing.AddSource("MyApp.Service");
        tracing.Configure(builder => builder.AddHttpClientInstrumentation());
    })
    .ConfigureMetrics(metrics =>
    {
        metrics.AddMeter("MyApp.Metrics");
        metrics.AddInstrumentation(builder => builder.AddRuntimeInstrumentation());
    })
    .ConfigureSerilog(logging =>
    {
        logging.AddDestructuringPolicy<ErrorsDestructuringPolicy>();
        logging.AddEnricher(new MyCustomEnricher());
    })
    .ConfigureStartupLogger(logger =>
    {
        logger.LogInformation("애플리케이션 시작");
    })
    .Build();
```

**appsettings.json 구성:**

```json
{
  "OpenTelemetry": {
    "ServiceName": "MyService",
    "CollectorEndpoint": "http://localhost:4317",
    "CollectorProtocol": "Grpc",
    "SamplingRate": 1.0,
    "EnablePrometheusExporter": true,
    "LoggingCollectorEndpoint": "http://localhost:4318",
    "LoggingCollectorProtocol": "HttpProtobuf"
  }
}
```

**API:**

```csharp
namespace Functorium.Abstractions.Registrations
{
    public static class OpenTelemetryRegistration
    {
        public static OpenTelemetryBuilder RegisterObservability(
            this IServiceCollection services,
            IConfiguration configuration);
    }
}

namespace Functorium.Adapters.Observabilities.Builders
{
    public class OpenTelemetryBuilder
    {
        public OpenTelemetryOptions Options { get; }
        public IServiceCollection Build();
        public OpenTelemetryBuilder ConfigureMetrics(Action<MetricsConfigurator> configure);
        public OpenTelemetryBuilder ConfigureSerilog(Action<LoggingConfigurator> configure);
        public OpenTelemetryBuilder ConfigureStartupLogger(Action<ILogger> configure);
        public OpenTelemetryBuilder ConfigureTraces(Action<TracingConfigurator> configure);
    }
}

namespace Functorium.Adapters.Observabilities.Builders.Configurators
{
    public class TracingConfigurator
    {
        public TracingConfigurator AddSource(string sourceName);
        public TracingConfigurator AddProcessor(BaseProcessor<Activity> processor);
        public TracingConfigurator Configure(Action<TracerProviderBuilder> configure);
    }

    public class MetricsConfigurator
    {
        public MetricsConfigurator AddMeter(string meterName);
        public MetricsConfigurator AddInstrumentation(Action<MeterProviderBuilder> configure);
        public MetricsConfigurator Configure(Action<MeterProviderBuilder> configure);
    }

    public class LoggingConfigurator
    {
        public LoggingConfigurator AddDestructuringPolicy<TPolicy>()
            where TPolicy : IDestructuringPolicy, new();
        public LoggingConfigurator AddEnricher(ILogEventEnricher enricher);
        public LoggingConfigurator AddEnricher<TEnricher>()
            where TEnricher : ILogEventEnricher, new();
        public LoggingConfigurator Configure(Action<LoggerConfiguration> configure);
    }
}
```

**OpenTelemetryOptions:**

```csharp
namespace Functorium.Adapters.Observabilities
{
    public sealed class OpenTelemetryOptions : IOpenTelemetryOptions, IStartupOptionsLogger
    {
        public const string SectionName = "OpenTelemetry";
        public string ServiceName { get; set; }
        public string ServiceVersion { get; }
        public string CollectorEndpoint { get; set; }
        public string CollectorProtocol { get; set; }
        public double SamplingRate { get; set; }
        public bool EnablePrometheusExporter { get; set; }
        public string? TracingCollectorEndpoint { get; set; }
        public string? TracingCollectorProtocol { get; set; }
        public string? MetricsCollectorEndpoint { get; set; }
        public string? MetricsCollectorProtocol { get; set; }
        public string? LoggingCollectorEndpoint { get; set; }
        public string? LoggingCollectorProtocol { get; set; }
    }
}
```

### 3. Options 패턴 (FluentValidation 통합)

FluentValidation을 활용하여 타입-안전한 Options 설정 및 검증을 지원합니다.

```csharp
using Functorium.Adapters.Options;

// Options 등록 및 검증
builder.Services.RegisterConfigureOptions<MyOptions, MyOptionsValidator>("MySection");

// 서비스 컬렉션에서 Options 직접 가져오기
var options = builder.Services.GetOptions<MyOptions>();
```

**API:**

```csharp
namespace Functorium.Adapters.Options
{
    public static class OptionsConfigurator
    {
        public static OptionsBuilder<TOptions> RegisterConfigureOptions<TOptions, TValidator>(
            this IServiceCollection services,
            string configurationSectionName)
            where TOptions : class
            where TValidator : class, IValidator<TOptions>;

        public static TOptions GetOptions<TOptions>(this IServiceCollection services)
            where TOptions : class, new();
    }
}
```

### 4. 아키텍처 검증 (ArchUnitNET)

ArchUnitNET을 활용하여 코드 아키텍처 규칙을 테스트로 검증합니다.

```csharp
using Functorium.Testing.ArchitectureRules;
using ArchUnitNET.Loader;

[Fact]
public void ValueObjects_Should_Be_Immutable_And_Sealed()
{
    var architecture = new ArchLoader()
        .LoadAssemblies(typeof(MyValueObject).Assembly)
        .Build();

    var valueObjects = Classes()
        .That().ResideInNamespace("MyApp.Domain.ValueObjects")
        .As("Value Objects");

    valueObjects.ValidateAllClasses(architecture, validator =>
    {
        validator
            .RequireSealed()
            .RequireImmutable()
            .RequireAllPrivateConstructors()
            .RequireMethod("Create", method =>
            {
                method.RequireStatic();
                method.RequireReturnTypeOfDeclaringClass();
            });
    }).ThrowIfAnyFailures("Value Object Rules");
}
```

**API:**

```csharp
namespace Functorium.Testing.ArchitectureRules
{
    public static class ArchitectureValidationEntryPoint
    {
        public static ValidationResultSummary ValidateAllClasses(
            this IObjectProvider<Class> classes,
            Architecture architecture,
            Action<ClassValidator> validationRule);
        public static ValidationResultSummary ValidateAllClasses(
            this IObjectProvider<Class> classes,
            Architecture architecture,
            Action<ClassValidator> validationRule,
            bool verbose);
    }

    public sealed class ClassValidator
    {
        public ClassValidator RequireSealed();
        public ClassValidator RequireImmutable();
        public ClassValidator RequirePublic();
        public ClassValidator RequireInternal();
        public ClassValidator RequireAllPrivateConstructors();
        public ClassValidator RequirePrivateAnyParameterlessConstructor();
        public ClassValidator RequireImplements(Type interfaceType);
        public ClassValidator RequireImplementsGenericInterface(string genericInterfaceName);
        public ClassValidator RequireInherits(Type baseType);
        public ClassValidator RequireMethod(string methodName, Action<MethodValidator> methodValidation);
        public ClassValidator RequireAllMethods(Action<MethodValidator> methodValidation);
        public ClassValidator RequireNestedClass(string nestedClassName, Action<ClassValidator>? nestedClassValidation = null);
        public ClassValidator RequireNestedClassIfExists(string nestedClassName, Action<ClassValidator>? nestedClassValidation = null);
    }

    public sealed class MethodValidator
    {
        public MethodValidator RequireStatic();
        public MethodValidator RequireVisibility(Visibility visibility);
        public MethodValidator RequireReturnType(Type returnType);
        public MethodValidator RequireReturnTypeOfDeclaringClass();
    }

    public sealed class ValidationResultSummary
    {
        public void ThrowIfAnyFailures(string ruleName);
    }
}
```

### 5. 테스트 픽스처 (Host, Quartz)

ASP.NET Core 및 Quartz.NET 기반 통합 테스트를 지원합니다.

**HostTestFixture 사용:**

```csharp
using Functorium.Testing.Arrangements.Hosting;

public class ApiTests : IClassFixture<HostTestFixture<Program>>
{
    private readonly HostTestFixture<Program> _fixture;

    public ApiTests(HostTestFixture<Program> fixture) => _fixture = fixture;

    [Fact]
    public async Task GetUsers_ReturnsOk()
    {
        var response = await _fixture.Client.GetAsync("/api/users");
        response.EnsureSuccessStatusCode();
    }
}
```

**QuartzTestFixture 사용:**

```csharp
using Functorium.Testing.Arrangements.ScheduledJobs;

public class JobTests : IAsyncLifetime
{
    private readonly QuartzTestFixture<Program> _fixture = new();

    public async Task InitializeAsync() => await _fixture.InitializeAsync();
    public async Task DisposeAsync() => await _fixture.DisposeAsync();

    [Fact]
    public async Task MyJob_ExecutesSuccessfully()
    {
        var result = await _fixture.ExecuteJobOnceAsync<MyJob>(TimeSpan.FromSeconds(30));

        Assert.True(result.Success);
        Assert.Null(result.Exception);
    }
}
```

**API:**

```csharp
namespace Functorium.Testing.Arrangements.Hosting
{
    public class HostTestFixture<TProgram> : IAsyncDisposable, IAsyncLifetime
        where TProgram : class
    {
        public HttpClient Client { get; }
        public IServiceProvider Services { get; }
        protected virtual string EnvironmentName { get; }
        protected virtual void ConfigureHost(IWebHostBuilder builder);
        protected virtual string GetTestProjectPath();
    }
}

namespace Functorium.Testing.Arrangements.ScheduledJobs
{
    public class QuartzTestFixture<TProgram> : IAsyncDisposable, IAsyncLifetime
        where TProgram : class
    {
        public IScheduler Scheduler { get; }
        public IServiceProvider Services { get; }
        public JobCompletionListener JobListener { get; }

        public Task<JobExecutionResult> ExecuteJobOnceAsync<TJob>(TimeSpan timeout)
            where TJob : IJob;
        public Task<JobExecutionResult> ExecuteJobOnceAsync<TJob>(string jobName, string jobGroup, TimeSpan timeout)
            where TJob : IJob;
    }

    public sealed class JobExecutionResult
    {
        public string JobName { get; init; }
        public bool Success { get; init; }
        public object? Result { get; init; }
        public JobExecutionException? Exception { get; init; }
        public TimeSpan ExecutionTime { get; init; }
    }
}
```

### 6. Serilog 테스트 유틸리티

구조화된 로깅 출력을 테스트에서 검증할 수 있습니다.

```csharp
using Functorium.Testing.Arrangements.Logging;
using Functorium.Testing.Assertions.Logging;

[Fact]
public void Logger_Should_Log_StructuredData()
{
    var logEvents = new List<LogEvent>();
    var serilogLogger = new LoggerConfiguration()
        .WriteTo.Sink(new TestSink(logEvents))
        .CreateLogger();

    var logger = new StructuredTestLogger<MyService>(serilogLogger);
    logger.LogInformation("User {UserId} logged in", 123);

    var logData = LogEventPropertyExtractor.ExtractLogData(logEvents.First());
    Assert.Equal(123, ((dynamic)logData).UserId);
}
```

**API:**

```csharp
namespace Functorium.Testing.Arrangements.Logging
{
    public class StructuredTestLogger<T> : ILogger, ILogger<T>
    {
        public StructuredTestLogger(Serilog.ILogger serilogLogger);
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull;
        public bool IsEnabled(LogLevel logLevel);
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter);
    }
}

namespace Functorium.Testing.Arrangements.Loggers
{
    public class TestSink : ILogEventSink
    {
        public TestSink(List<LogEvent> logEvents);
        public void Emit(LogEvent logEvent);
    }
}

namespace Functorium.Testing.Assertions.Logging
{
    public static class LogEventPropertyExtractor
    {
        public static object ExtractLogData(LogEvent logEvent);
        public static IEnumerable<object> ExtractLogData(IEnumerable<LogEvent> logEvents);
        public static object ExtractValue(LogEventPropertyValue propertyValue);
    }

    public static class LogEventPropertyValueConverter
    {
        public static object ToAnonymousObject(LogEventPropertyValue value);
    }

    public sealed class SerilogTestPropertyValueFactory : ILogEventPropertyValueFactory
    {
        public LogEventPropertyValue CreatePropertyValue(object? value, bool destructureObjects = false);
    }
}
```

### 7. FinT 유틸리티 (LINQ 확장)

LanguageExt의 `Fin`/`FinT` 모나드에 대한 LINQ 쿼리 구문을 지원합니다.

```csharp
using Functorium.Applications.Linq;
using LanguageExt;

// Fin 필터링
Fin<int> fin = Fin<int>.Succ(42);
var filtered = fin.Filter(x => x > 0);

// FinT와 IO 조합
var result =
    from a in IO.lift(() => GetValueFromDb())
    from b in ValidateAndProcess(a)
    select b;
```

**API:**

```csharp
namespace Functorium.Applications.Linq
{
    public static class FinTUtilites
    {
        public static Fin<A> Filter<A>(this Fin<A> fin, Func<A, bool> predicate);
        public static FinT<M, A> Filter<M, A>(this FinT<M, A> finT, Func<A, bool> predicate)
            where M : Monad<M>;
        public static FinT<IO, B> SelectMany<A, B>(this IO<A> io, Func<A, B> selector);
        public static FinT<IO, C> SelectMany<A, B, C>(this IO<A> io, Func<A, FinT<IO, B>> finTSelector, Func<A, B, C> projector);
        public static FinT<M, C> SelectMany<M, A, B, C>(this Fin<A> fin, Func<A, FinT<M, B>> finTSelector, Func<A, B, C> projector)
            where M : Monad<M>;
    }
}
```

### 8. 유틸리티 확장 메서드

자주 사용되는 컬렉션 및 문자열 연산을 간결하게 표현합니다.

```csharp
using Functorium.Abstractions.Utilities;

// Dictionary 확장
var dict = new Dictionary<string, int>();
dict.AddOrUpdate("key", 1);  // 없으면 추가, 있으면 업데이트

// IEnumerable 확장
var list = new List<int> { 1, 2, 3 };
bool hasItems = list.Any();         // true
bool isEmpty = list.IsEmpty();      // false
string joined = list.Join(", ");    // "1, 2, 3"

// String 확장
string str = "123";
int num = str.ConvertToInt();       // 123
double dbl = str.ConvertToDouble(); // 123.0
bool empty = "".Empty();            // true
bool notEmpty = "hello".NotEmpty(); // true
bool notContains = "hello".NotContains("xyz");  // true
```

**API:**

```csharp
namespace Functorium.Abstractions.Utilities
{
    public static class DictionaryUtilities
    {
        public static void AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
            where TKey : notnull;
    }

    public static class IEnumerableUtilities
    {
        public static bool Any(this IEnumerable source);
        public static bool IsEmpty(this IEnumerable source);
        public static string Join<TValue>(this IEnumerable<TValue> items, char separator);
        public static string Join<TValue>(this IEnumerable<TValue> items, string separator);
    }

    public static class StringUtilities
    {
        public static int ConvertToInt(this string str);
        public static double ConvertToDouble(this string str);
        public static bool TryConvertToDouble(this string str);
        public static bool Empty(this string? str);
        public static bool NotEmpty(this string? str);
        public static bool NotContains(this string str, string subStr, StringComparison stringComparison = OrdinalIgnoreCase);
        public static bool NotEquals(this string str, string otherStr, StringComparison stringComparison = OrdinalIgnoreCase);
        public static string Replace(this string str, string[] oldStrList, string newStr);
    }
}
```

## 버그 수정

### NuGet 패키지 아이콘 경로 수정
- NuGet 패키지 배포 시 아이콘 파일 경로가 올바르게 참조되도록 수정

## API 변경사항

### Functorium 어셈블리
- **Functorium.Abstractions.Errors**: ErrorCodeFactory, ErrorsDestructuringPolicy, IErrorDestructurer 및 6개 Destructurer 추가
- **Functorium.Abstractions.Registrations**: OpenTelemetryRegistration 추가
- **Functorium.Abstractions.Utilities**: DictionaryUtilities, IEnumerableUtilities, StringUtilities 추가
- **Functorium.Adapters.Observabilities**: OpenTelemetryBuilder, OpenTelemetryOptions, Configurators 추가
- **Functorium.Adapters.Options**: OptionsConfigurator 추가
- **Functorium.Applications.Linq**: FinTUtilites 추가

### Functorium.Testing 어셈블리
- **Functorium.Testing.ArchitectureRules**: ArchitectureValidationEntryPoint, ClassValidator, MethodValidator, ValidationResultSummary 추가
- **Functorium.Testing.Arrangements.Hosting**: HostTestFixture 추가
- **Functorium.Testing.Arrangements.ScheduledJobs**: QuartzTestFixture, JobCompletionListener, JobExecutionResult 추가
- **Functorium.Testing.Arrangements.Logging**: StructuredTestLogger, TestSink 추가
- **Functorium.Testing.Assertions.Logging**: LogEventPropertyExtractor, LogEventPropertyValueConverter, SerilogTestPropertyValueFactory 추가

## 문서화

다음 가이드 문서가 추가되었습니다:

- **아키텍처 가이드**: ArchitectureIs 문서 및 다이어그램
- **Functorium 가이드**:
  - Src-01-Error.md - 오류 처리 가이드
  - Src-02-Options.md - Options 패턴 가이드
  - Src-03-OpenTelemetry.md - OpenTelemetry 가이드
  - Guide-01-Unit-Testing.md - 단위 테스트 가이드
  - Guide-02-Integration-Testing.md - 통합 테스트 가이드
  - LogEventPropertyExtractor.md - 로그 검증 가이드
- **인프라 가이드**:
  - Infra-01-VSCode.md - VSCode 설정 가이드
  - Infra-02-VSCode-Scripts.md - VSCode 스크립트 가이드
  - Infra-03-Git-Hooks.md - Git Hooks 가이드
- **CI/CD 가이드**:
  - CI-GitHub-Actions.md - GitHub Actions 가이드
  - CI-MinVer.md - MinVer 버전 관리 가이드
  - CI-NuGet-Package.md - NuGet 패키지 생성 가이드
  - Code-Quality.md - 코드 품질 가이드
- **기타 가이드**:
  - Git.md - Git 명령어 가이드
  - xUnit.md - xUnit 테스트 가이드
  - Writing-Guide.md - 문서 작성 가이드
  - Writing-ps1.md - PowerShell 스크립트 가이드

## 알려진 제한사항

- **Alpha 릴리스**: 이 릴리스는 알파 버전으로, API가 변경될 수 있습니다
- **.NET 10 필수**: .NET 10.0 이상에서만 지원됩니다
- **LanguageExt 베타 의존성**: LanguageExt 5.0.0-beta-58에 의존합니다

## 감사의 말

이 프로젝트는 다음 오픈소스 라이브러리를 사용합니다:

- [LanguageExt](https://github.com/louthy/language-ext) - 함수형 프로그래밍 라이브러리
- [Serilog](https://serilog.net/) - 구조화된 로깅
- [OpenTelemetry](https://opentelemetry.io/) - 관측성 프레임워크
- [FluentValidation](https://fluentvalidation.net/) - 유효성 검사 라이브러리
- [ArchUnitNET](https://github.com/TNG/ArchUnitNET) - 아키텍처 테스트 라이브러리
- [Quartz.NET](https://www.quartz-scheduler.net/) - 작업 스케줄링 라이브러리
- [Ardalis.SmartEnum](https://github.com/ardalis/SmartEnum) - 스마트 열거형 라이브러리

## 설치

```bash
dotnet add package Functorium --version 1.0.0-alpha.1
dotnet add package Functorium.Testing --version 1.0.0-alpha.1
```

또는 `.csproj` 파일에 직접 추가:

```xml
<PackageReference Include="Functorium" Version="1.0.0-alpha.1" />
<PackageReference Include="Functorium.Testing" Version="1.0.0-alpha.1" />
```
