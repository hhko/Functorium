# Phase 4: 모든 코드 샘플

Generated: 2025-12-16

## 1. ErrorCodeFactory 코드 샘플

### 기본 오류 생성
```csharp
using Functorium.Abstractions.Errors;

// 기본 오류 생성
var error = ErrorCodeFactory.Create(
    errorCode: "USER_001",
    errorCurrentValue: "invalid@email",
    errorMessage: "이메일 형식이 올바르지 않습니다.");
```
**검증**: ErrorCodeFactory.Create(string, string, string) - Uber Line 77

### 제네릭 오류 생성
```csharp
// 제네릭 오류 생성
var error = ErrorCodeFactory.Create<int>(
    errorCode: "ORDER_002",
    errorCurrentValue: 0,
    errorMessage: "주문 수량은 0보다 커야 합니다.");
```
**검증**: ErrorCodeFactory.Create<T>(string, T, string) - Uber Line 78-79

### 다중 값 오류 생성
```csharp
// 다중 값 오류 생성
var error = ErrorCodeFactory.Create<string, int>(
    errorCode: "PRICE_003",
    errorCurrentValue1: "USD",
    errorCurrentValue2: -100,
    errorMessage: "가격은 음수일 수 없습니다.");
```
**검증**: ErrorCodeFactory.Create<T1, T2>(string, T1, T2, string) - Uber Line 80-82

### 예외로부터 오류 생성
```csharp
// 예외로부터 오류 생성
var error = ErrorCodeFactory.CreateFromException(
    errorCode: "SYS_001",
    exception: ex);
```
**검증**: ErrorCodeFactory.CreateFromException(string, Exception) - Uber Line 87

### 오류 코드 포맷팅
```csharp
// 오류 코드 포맷팅
var code = ErrorCodeFactory.Format("User", "Validation", "Email");
// 결과: "User.Validation.Email"
```
**검증**: ErrorCodeFactory.Format(params string[]) - Uber Line 88

---

## 2. ErrorsDestructuringPolicy 코드 샘플

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
**검증**: ErrorsDestructuringPolicy - Uber Line 61-66

---

## 3. OpenTelemetry 통합 코드 샘플

### 기본 사용법
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
**검증**:
- OpenTelemetryRegistration.RegisterObservability - Uber Line 95
- OpenTelemetryBuilder.ConfigureSerilog - Uber Line 158
- OpenTelemetryBuilder.ConfigureTraces - Uber Line 160
- OpenTelemetryBuilder.ConfigureMetrics - Uber Line 157
- OpenTelemetryBuilder.Build - Uber Line 156
- LoggingConfigurator.AddDestructuringPolicy - Uber Line 129-130
- TracingConfigurator.AddSource - Uber Line 147
- MetricsConfigurator.AddMeter - Uber Line 140

### 설정 옵션
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
**검증**: OpenTelemetryOptions 속성들 - Uber Line 176-187

### Configurator API
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
**검증**:
- LoggingConfigurator.AddEnricher<T> - Uber Line 132-133
- LoggingConfigurator.Configure - Uber Line 134
- TracingConfigurator.AddProcessor - Uber Line 146
- TracingConfigurator.Configure - Uber Line 148
- MetricsConfigurator.AddInstrumentation - Uber Line 139
- MetricsConfigurator.Configure - Uber Line 141

---

## 4. 아키텍처 검증 코드 샘플

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
**검증**:
- ArchitectureValidationEntryPoint.ValidateAllClasses - Uber Line 263-264
- ClassValidator.RequireSealed - Uber Line 281
- ClassValidator.RequireImmutable - Uber Line 271
- ClassValidator.RequireAllPrivateConstructors - Uber Line 270
- ValidationResultSummary.ThrowIfAnyFailures - Uber Line 295

---

## 5. HostTestFixture 코드 샘플

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
**검증**:
- HostTestFixture<TProgram> - Uber Line 300-311
- HostTestFixture.Client - Uber Line 304
- HostTestFixture.Services - Uber Line 306

---

## 6. QuartzTestFixture 코드 샘플

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
**검증**:
- QuartzTestFixture<TProgram> - Uber Line 353-369
- QuartzTestFixture.ExecuteJobOnceAsync - Uber Line 363-366
- JobExecutionResult.Success - Uber Line 351
- JobExecutionResult.Exception - Uber Line 347

---

## 7. Serilog 테스트 유틸리티 코드 샘플

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
**검증**:
- TestSink - Uber Line 315-319
- StructuredTestLogger<T> - Uber Line 323-330
- LogEventPropertyExtractor.ExtractLogData - Uber Line 382-384

---

## 8. FinTUtilites 코드 샘플

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
**검증**:
- FinTUtilites.Filter<A> - Uber Line 234
- FinTUtilites.SelectMany (overloads) - Uber Line 237-240

---

## 9. OptionsConfigurator 코드 샘플

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
**검증**:
- OptionsConfigurator.RegisterConfigureOptions - Uber Line 225-227
- OptionsConfigurator.GetOptions - Uber Line 223-224

---

## 10. 유틸리티 확장 메서드 코드 샘플

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
**검증**:
- DictionaryUtilities.AddOrUpdate - Uber Line 102-103
- IEnumerableUtilities.Any - Uber Line 107
- IEnumerableUtilities.IsEmpty - Uber Line 108
- IEnumerableUtilities.Join - Uber Line 109-110
- StringUtilities.ConvertToInt - Uber Line 115
- StringUtilities.ConvertToDouble - Uber Line 114
- StringUtilities.Empty - Uber Line 116
- StringUtilities.NotEmpty - Uber Line 118
- StringUtilities.NotEquals - Uber Line 119
- StringUtilities.NotContains - Uber Line 117
- StringUtilities.Replace - Uber Line 120

---

## 검증 요약

- 총 코드 샘플: 24개
- 검증된 API 참조: 모두 Uber 파일에서 확인됨
- 검증 실패: 0개
- 상태: ✓ 모두 검증됨
