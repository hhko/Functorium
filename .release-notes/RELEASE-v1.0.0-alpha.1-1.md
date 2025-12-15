# Functorium Release v1.0.0-alpha.1

**릴리스 날짜:** 2025-12-16

## 개요

Functorium v1.0.0-alpha.1은 함수형 프로그래밍 패러다임을 .NET 애플리케이션에 적용하기 위한 첫 번째 프리릴리스입니다. LanguageExt 라이브러리를 기반으로 구조화된 오류 처리, OpenTelemetry 통합, 아키텍처 검증 도구를 제공합니다.

주요 기능:
- 함수형 오류 처리 (ErrorCodeFactory, Error Destructuring)
- OpenTelemetry 및 Serilog 통합 (분산 추적, 메트릭, 로깅)
- ArchUnitNET 기반 아키텍처 검증 도구
- ASP.NET Core 및 Quartz.NET 테스트 픽스처
- Serilog 로그 검증 유틸리티
- FinT/IO 모나드 LINQ 확장

## Breaking Changes

없음 (첫 릴리스)

## 새로운 기능

### 1. 함수형 오류 처리 (Error Handling)

LanguageExt의 `Error` 타입을 구조화된 방식으로 생성하고 Serilog에서 적절히 로깅할 수 있는 기능을 제공합니다.

#### ErrorCodeFactory

오류 코드, 현재 값, 메시지를 포함하는 구조화된 오류를 생성합니다.

```csharp
using Functorium.Abstractions.Errors;

// 기본 오류 생성
var error = ErrorCodeFactory.Create(
    errorCode: "USER_001",
    errorCurrentValue: "invalid@email",
    errorMessage: "이메일 형식이 올바르지 않습니다.");

// 제네릭 오류 생성
var error = ErrorCodeFactory.Create<int>(
    errorCode: "ORDER_002",
    errorCurrentValue: 0,
    errorMessage: "주문 수량은 0보다 커야 합니다.");

// 다중 값 오류 생성
var error = ErrorCodeFactory.Create<string, int>(
    errorCode: "PRICE_003",
    errorCurrentValue1: "USD",
    errorCurrentValue2: -100,
    errorMessage: "가격은 음수일 수 없습니다.");

// 예외로부터 오류 생성
var error = ErrorCodeFactory.CreateFromException(
    errorCode: "SYS_001",
    exception: ex);

// 오류 코드 포맷팅
var code = ErrorCodeFactory.Format("User", "Validation", "Email");
// 결과: "User.Validation.Email"
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
            where T1 : notnull
            where T2 : notnull;
        public static LanguageExt.Common.Error Create<T1, T2, T3>(string errorCode, T1 errorCurrentValue1, T2 errorCurrentValue2, T3 errorCurrentValue3, string errorMessage)
            where T1 : notnull
            where T2 : notnull
            where T3 : notnull;
        public static LanguageExt.Common.Error CreateFromException(string errorCode, System.Exception exception);
        public static string Format(params string[] parts);
    }
}
```

#### Error Destructuring Policy

Serilog에서 LanguageExt `Error` 타입을 구조화된 로그로 출력하기 위한 디스트럭처링 정책입니다.

```csharp
using Serilog;
using Functorium.Abstractions.Errors.DestructuringPolicies;

// Serilog 설정에 디스트럭처링 정책 추가
Log.Logger = new LoggerConfiguration()
    .Destructure.With<ErrorsDestructuringPolicy>()
    .WriteTo.Console()
    .CreateLogger();

// 오류 로깅 시 구조화된 형태로 출력
Log.Error("처리 실패: {@Error}", error);
```

**API:**
```csharp
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
- `ErrorCodeExceptionalDestructurer` - ErrorCodeExceptional 타입 처리
- `ErrorCodeExpectedDestructurer` - ErrorCodeExpected 타입 처리
- `ErrorCodeExpectedTDestructurer` - ErrorCodeExpected<T> 타입 처리
- `ExceptionalDestructurer` - Exceptional 타입 처리
- `ExpectedDestructurer` - Expected 타입 처리
- `ManyErrorsDestructurer` - ManyErrors 타입 처리

---

### 2. OpenTelemetry 통합 (Observability)

분산 추적(Tracing), 메트릭(Metrics), 로깅(Logging)을 OpenTelemetry 및 Serilog와 통합합니다.

#### 기본 사용법

```csharp
using Functorium.Abstractions.Registrations;
using Functorium.Adapters.Observabilities.Builders;

var builder = WebApplication.CreateBuilder(args);

// OpenTelemetry 등록
builder.Services
    .RegisterObservability(builder.Configuration)
    .ConfigureSerilog(logging =>
    {
        logging.AddDestructuringPolicy<ErrorsDestructuringPolicy>();
    })
    .ConfigureTraces(tracing =>
    {
        tracing.AddSource("MyApplication");
    })
    .ConfigureMetrics(metrics =>
    {
        metrics.AddMeter("MyApplication.Metrics");
    })
    .Build();
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
```

#### 설정 옵션

`appsettings.json`에서 OpenTelemetry 설정을 구성합니다.

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

**API:**
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
        public string? LoggingCollectorEndpoint { get; set; }
        public string? MetricsCollectorEndpoint { get; set; }
        public string? TracingCollectorEndpoint { get; set; }
    }
}
```

#### Configurator API

각 텔레메트리 유형별 세부 설정을 제공합니다.

```csharp
// Serilog 로깅 설정
.ConfigureSerilog(logging =>
{
    logging
        .AddDestructuringPolicy<ErrorsDestructuringPolicy>()
        .AddEnricher<MyCustomEnricher>()
        .Configure(config => config.MinimumLevel.Debug());
})

// 분산 추적 설정
.ConfigureTraces(tracing =>
{
    tracing
        .AddSource("MyApplication")
        .AddProcessor(new MyProcessor())
        .Configure(builder => builder.AddHttpClientInstrumentation());
})

// 메트릭 설정
.ConfigureMetrics(metrics =>
{
    metrics
        .AddMeter("MyApplication.Metrics")
        .AddInstrumentation(builder => builder.AddRuntimeInstrumentation());
})
```

**API:**
```csharp
namespace Functorium.Adapters.Observabilities.Builders.Configurators
{
    public class LoggingConfigurator
    {
        public LoggingConfigurator AddDestructuringPolicy<TPolicy>() where TPolicy : IDestructuringPolicy, new();
        public LoggingConfigurator AddEnricher(ILogEventEnricher enricher);
        public LoggingConfigurator AddEnricher<TEnricher>() where TEnricher : ILogEventEnricher, new();
        public LoggingConfigurator Configure(Action<LoggerConfiguration> configure);
    }

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
}
```

---

### 3. 아키텍처 검증 (Architecture Validation)

ArchUnitNET을 활용한 아키텍처 테스트를 위한 유창한 검증 API를 제공합니다.

```csharp
using Functorium.Testing.ArchitectureRules;
using ArchUnitNET.Loader;
using ArchUnitNET.Domain;

[Fact]
public void ValueObjects_Should_Be_Immutable_And_Sealed()
{
    var architecture = new ArchLoader()
        .LoadAssemblies(typeof(MyValueObject).Assembly)
        .Build();

    var valueObjects = Classes()
        .That()
        .HaveNameEndingWith("ValueObject")
        .As("Value Objects");

    var result = valueObjects.ValidateAllClasses(architecture, validator =>
    {
        validator
            .RequireSealed()
            .RequireImmutable()
            .RequireAllPrivateConstructors();
    });

    result.ThrowIfAnyFailures("Value Object Rules");
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
    }

    public sealed class ClassValidator
    {
        public ClassValidator RequireSealed();
        public ClassValidator RequirePublic();
        public ClassValidator RequireInternal();
        public ClassValidator RequireImmutable();
        public ClassValidator RequireAllPrivateConstructors();
        public ClassValidator RequirePrivateAnyParameterlessConstructor();
        public ClassValidator RequireImplements(Type interfaceType);
        public ClassValidator RequireImplementsGenericInterface(string genericInterfaceName);
        public ClassValidator RequireInherits(Type baseType);
        public ClassValidator RequireMethod(string methodName, Action<MethodValidator> methodValidation);
        public ClassValidator RequireAllMethods(Action<MethodValidator> methodValidation);
        public ClassValidator RequireNestedClass(string nestedClassName, Action<ClassValidator>? validation = null);
        public ClassValidator RequireNestedClassIfExists(string nestedClassName, Action<ClassValidator>? validation = null);
    }

    public sealed class MethodValidator
    {
        public MethodValidator RequireStatic();
        public MethodValidator RequireReturnType(Type returnType);
        public MethodValidator RequireReturnTypeOfDeclaringClass();
        public MethodValidator RequireVisibility(Visibility visibility);
    }

    public sealed class ValidationResultSummary
    {
        public void ThrowIfAnyFailures(string ruleName);
    }
}
```

---

### 4. 테스트 픽스처 (Test Fixtures)

#### HostTestFixture

ASP.NET Core 호스트 통합 테스트를 위한 픽스처입니다.

```csharp
using Functorium.Testing.Arrangements.Hosting;

public class MyApiTests : IClassFixture<HostTestFixture<Program>>
{
    private readonly HostTestFixture<Program> _fixture;

    public MyApiTests(HostTestFixture<Program> fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetUsers_Should_Return_Ok()
    {
        var response = await _fixture.Client.GetAsync("/api/users");
        response.EnsureSuccessStatusCode();
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
```

#### QuartzTestFixture

Quartz.NET 스케줄러 통합 테스트를 위한 픽스처입니다.

```csharp
using Functorium.Testing.Arrangements.ScheduledJobs;

public class MyJobTests : IClassFixture<QuartzTestFixture<Program>>
{
    private readonly QuartzTestFixture<Program> _fixture;

    public MyJobTests(QuartzTestFixture<Program> fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MyJob_Should_Execute_Successfully()
    {
        var result = await _fixture.ExecuteJobOnceAsync<MyJob>(
            timeout: TimeSpan.FromSeconds(30));

        Assert.True(result.Success);
        Assert.Null(result.Exception);
    }
}
```

**API:**
```csharp
namespace Functorium.Testing.Arrangements.ScheduledJobs
{
    public class QuartzTestFixture<TProgram> : IAsyncDisposable, IAsyncLifetime
        where TProgram : class
    {
        public IScheduler Scheduler { get; }
        public IServiceProvider Services { get; }
        public JobCompletionListener JobListener { get; }
        public Task<JobExecutionResult> ExecuteJobOnceAsync<TJob>(TimeSpan timeout) where TJob : IJob;
        public Task<JobExecutionResult> ExecuteJobOnceAsync<TJob>(string jobName, string jobGroup, TimeSpan timeout) where TJob : IJob;
    }

    public sealed class JobExecutionResult
    {
        public string JobName { get; init; }
        public bool Success { get; init; }
        public object? Result { get; init; }
        public JobExecutionException? Exception { get; init; }
        public TimeSpan ExecutionTime { get; init; }
    }

    public sealed class JobCompletionListener : IJobListener
    {
        public Task<JobExecutionResult> WaitForJobCompletionAsync(string jobName, TimeSpan timeout, CancellationToken cancellationToken = default);
        public void Reset();
    }
}
```

---

### 5. Serilog 테스트 유틸리티 (Logging Testing)

테스트에서 Serilog 로그 이벤트를 검증하기 위한 유틸리티입니다.

```csharp
using Functorium.Testing.Arrangements.Logging;
using Functorium.Testing.Arrangements.Loggers;
using Functorium.Testing.Assertions.Logging;
using Serilog;

// 테스트용 로거 설정
var logEvents = new List<LogEvent>();
var logger = new LoggerConfiguration()
    .WriteTo.Sink(new TestSink(logEvents))
    .CreateLogger();

var testLogger = new StructuredTestLogger<MyService>(logger);

// 서비스 실행
var service = new MyService(testLogger);
await service.DoWorkAsync();

// 로그 검증
var logData = LogEventPropertyExtractor.ExtractLogData(logEvents);
Assert.Contains(logData, d => d.ToString().Contains("작업 완료"));
```

**API:**
```csharp
namespace Functorium.Testing.Arrangements.Logging
{
    public class StructuredTestLogger<T> : ILogger, ILogger<T>
    {
        public StructuredTestLogger(Serilog.ILogger serilogLogger);
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

---

### 6. FinT 유틸리티 (LINQ Extensions)

LanguageExt의 `FinT<M, A>` 및 `IO<A>` 모나드를 LINQ 쿼리 구문에서 사용할 수 있는 확장 메서드입니다.

```csharp
using Functorium.Applications.Linq;
using LanguageExt;
using static LanguageExt.Prelude;

// Fin<A>에 Filter 적용
Fin<int> value = FinSucc(42);
var filtered = value.Filter(x => x > 0);

// IO<A>와 FinT<IO, B> 결합
var result =
    from a in liftIO(() => GetValueAsync())
    from b in ProcessAsync(a)
    select (a, b);
```

**API:**
```csharp
namespace Functorium.Applications.Linq
{
    public static class FinTUtilites
    {
        public static Fin<A> Filter<A>(this Fin<A> fin, Func<A, bool> predicate);
        public static FinT<M, A> Filter<M, A>(this FinT<M, A> finT, Func<A, bool> predicate) where M : Monad<M>;
        public static FinT<IO, B> SelectMany<A, B>(this IO<A> io, Func<A, B> selector);
        public static FinT<IO, C> SelectMany<A, B, C>(this IO<A> io, Func<A, FinT<IO, B>> finTSelector, Func<A, B, C> projector);
        public static FinT<M, C> SelectMany<M, A, B, C>(this Fin<A> fin, Func<A, FinT<M, B>> finTSelector, Func<A, B, C> projector) where M : Monad<M>;
    }
}
```

---

### 7. Options 패턴 (Configuration)

FluentValidation과 통합된 Options 패턴 등록을 간소화합니다.

```csharp
using Functorium.Adapters.Options;
using FluentValidation;

// Options 클래스 정의
public class MyOptions
{
    public const string SectionName = "MySection";
    public string ConnectionString { get; set; } = string.Empty;
    public int Timeout { get; set; } = 30;
}

// Validator 정의
public class MyOptionsValidator : AbstractValidator<MyOptions>
{
    public MyOptionsValidator()
    {
        RuleFor(x => x.ConnectionString).NotEmpty();
        RuleFor(x => x.Timeout).GreaterThan(0);
    }
}

// 등록
builder.Services.RegisterConfigureOptions<MyOptions, MyOptionsValidator>(MyOptions.SectionName);

// 즉시 Options 가져오기
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

---

### 8. 유틸리티 확장 메서드

일반적인 컬렉션 및 문자열 작업을 위한 확장 메서드입니다.

```csharp
using Functorium.Abstractions.Utilities;

// Dictionary 확장
var dict = new Dictionary<string, int>();
dict.AddOrUpdate("key", 1);
dict.AddOrUpdate("key", 2); // 기존 값 업데이트

// IEnumerable 확장
var list = new List<int> { 1, 2, 3 };
bool hasItems = list.Any();
bool isEmpty = list.IsEmpty();
string joined = list.Join(", "); // "1, 2, 3"

// String 확장
string str = "123";
int value = str.ConvertToInt();
double dValue = str.ConvertToDouble();
bool empty = str.Empty();
bool notEmpty = str.NotEmpty();
bool notEquals = str.NotEquals("456");
bool notContains = str.NotContains("abc");
string replaced = str.Replace(new[] { "1", "3" }, "X"); // "X2X"
```

**API:**
```csharp
namespace Functorium.Abstractions.Utilities
{
    public static class DictionaryUtilities
    {
        public static void AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value) where TKey : notnull;
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
        public static bool NotEquals(this string str, string otherStr, StringComparison stringComparison = OrdinalIgnoreCase);
        public static bool NotContains(this string str, string subStr, StringComparison stringComparison = OrdinalIgnoreCase);
        public static string Replace(this string str, string[] oldStrList, string newStr);
    }
}
```

## 버그 수정

- NuGet 패키지 아이콘 경로 수정 ([a8ec763])

## API 변경사항

### Functorium 어셈블리

| 네임스페이스 | 타입 |
|-------------|------|
| Functorium.Abstractions.Errors | ErrorCodeFactory |
| Functorium.Abstractions.Errors.DestructuringPolicies | ErrorsDestructuringPolicy, IErrorDestructurer |
| Functorium.Abstractions.Errors.DestructuringPolicies.ErrorTypes | ErrorCodeExceptionalDestructurer, ErrorCodeExpectedDestructurer, ErrorCodeExpectedTDestructurer, ExceptionalDestructurer, ExpectedDestructurer, ManyErrorsDestructurer |
| Functorium.Abstractions.Registrations | OpenTelemetryRegistration |
| Functorium.Abstractions.Utilities | DictionaryUtilities, IEnumerableUtilities, StringUtilities |
| Functorium.Adapters.Observabilities.Builders | OpenTelemetryBuilder |
| Functorium.Adapters.Observabilities.Builders.Configurators | LoggingConfigurator, MetricsConfigurator, TracingConfigurator |
| Functorium.Adapters.Observabilities | OpenTelemetryOptions, IOpenTelemetryOptions |
| Functorium.Adapters.Observabilities.Logging | StartupLogger, IStartupOptionsLogger |
| Functorium.Adapters.Options | OptionsConfigurator |
| Functorium.Applications.Linq | FinTUtilites |

### Functorium.Testing 어셈블리

| 네임스페이스 | 타입 |
|-------------|------|
| Functorium.Testing.ArchitectureRules | ArchitectureValidationEntryPoint, ClassValidator, MethodValidator, ValidationResultSummary |
| Functorium.Testing.Arrangements.Hosting | HostTestFixture |
| Functorium.Testing.Arrangements.ScheduledJobs | QuartzTestFixture, JobCompletionListener, JobExecutionResult |
| Functorium.Testing.Arrangements.Logging | StructuredTestLogger |
| Functorium.Testing.Arrangements.Loggers | TestSink |
| Functorium.Testing.Assertions.Logging | LogEventPropertyExtractor, LogEventPropertyValueConverter, SerilogTestPropertyValueFactory |
| Functorium.Testing | AssemblyReference |

## 문서화

이번 릴리스에는 38개의 문서가 포함되어 있습니다:

**아키텍처 문서:**
- ArchitectureIs/README.md - 아키텍처 개요

**Functorium 가이드:**
- Guide-01-Unit-Testing.md - 단위 테스트 가이드
- Guide-02-Integration-Testing.md - 통합 테스트 가이드
- Infra-01-VSCode.md - VSCode 설정 가이드
- Infra-02-VSCode-Scripts.md - VSCode 스크립트 가이드
- Infra-03-Git-Hooks.md - Git Hooks 가이드
- LogEventPropertyExtractor.md - 로그 속성 추출 가이드
- Src-01-Error.md - 오류 처리 가이드
- Src-02-Options.md - Options 패턴 가이드
- Src-03-OpenTelemetry.md - OpenTelemetry 가이드

**일반 가이드:**
- CI-GitHub-Actions.md - GitHub Actions 가이드
- CI-MinVer.md - MinVer 버전 관리 가이드
- CI-NuGet-Package.md - NuGet 패키지 가이드
- Code-Quality.md - 코드 품질 가이드
- Git.md - Git 명령어 가이드
- SdkVersion.md - SDK 버전 가이드
- xUnit.md - xUnit 테스트 가이드

## 알려진 제한사항

- **LanguageExt 버전**: LanguageExt 5.0.0-beta-58을 사용합니다. 베타 버전이므로 향후 API 변경이 있을 수 있습니다.
- **프리릴리스**: 이 버전은 알파 릴리스이며, API가 안정화되지 않았습니다.

## 감사의 말

이 프로젝트는 다음 오픈소스 라이브러리를 사용합니다:

- [LanguageExt](https://github.com/louthy/language-ext) - 함수형 프로그래밍 라이브러리
- [OpenTelemetry](https://opentelemetry.io/) - 분산 추적 및 메트릭
- [Serilog](https://serilog.net/) - 구조화된 로깅
- [FluentValidation](https://fluentvalidation.net/) - 유효성 검사
- [ArchUnitNET](https://github.com/TNG/ArchUnitNET) - 아키텍처 테스트
- [Quartz.NET](https://www.quartz-scheduler.net/) - 작업 스케줄러
- [xUnit](https://xunit.net/) - 테스트 프레임워크
- [Ardalis.SmartEnum](https://github.com/ardalis/SmartEnum) - 스마트 열거형

## 설치

```bash
dotnet add package Functorium --version 1.0.0-alpha.1
dotnet add package Functorium.Testing --version 1.0.0-alpha.1
```

## 다음 릴리스 예정

- API 안정화
- 추가 Error 타입 지원
- 더 많은 테스트 유틸리티
- 성능 최적화
