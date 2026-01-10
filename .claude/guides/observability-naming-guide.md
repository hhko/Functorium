# Observability 명명 규칙 가이드

## 1. 개요

이 문서는 Functorium 프로젝트의 Observability 관련 코드 작성 시 따라야 할 명명 규칙을 정의합니다.
OpenTelemetry 표준 용어를 기반으로 하되, .NET 생태계와 실용성을 고려하여 수립되었습니다.

## 2. 핵심 원칙

### 2.1 OpenTelemetry Signals

OpenTelemetry는 세 가지 관찰 가능성 신호(Signals)를 정의합니다:

- **Logging**: 로그 신호 시스템
- **Tracing**: 분산 추적 신호 시스템
- **Metrics**: 메트릭 신호 시스템

### 2.2 용어 역할 구분

**Signal 이름 (Logging, Tracing, Metrics):**
- Signal 체계/시스템을 나타냄
- 활동/설정을 나타내는 형용사/동명사로도 사용
- 사용 위치:
  - 접두사: 설정/활동 대상 → `LoggingConfigurator`, `TracingEndpoint`
  - 단독: 활동/프로세스 → `UsecaseLoggingPipeline` (Logging 활동 수행)

**Component 유형 (Logger, Span, Metric 등):**
- 구체적인 객체/구성 요소
- 주로 접미사로만 사용
- 종류:
  - `Logger`: 로그를 생성하는 객체 → `StartupLogger`
  - `Span`: 추적의 단위 → `ISpan`, `OpenTelemetrySpan`
  - `Metric`: 측정값 → `IMetricRecorder`
  - `Tracer`: Span을 생성하는 팩토리 (실제로는 `SpanFactory` 사용)
  - `Meter`: Metric을 기록하는 객체 (실제로는 `MetricRecorder` 사용)

**명명 원칙 정리:**
```
접두사:
  Logging-     : 로깅 설정/활동 (LoggingConfigurator, LoggingEndpoint)
  Tracing-     : 추적 설정/활동 (TracingConfigurator, TracingEndpoint, TracingProtocol)
  Metrics-     : 메트릭 설정/활동 (MetricsConfigurator, MetricsEndpoint)

접미사 (Component):
  -Logger      : 로거 객체 (StartupLogger, IStartupOptionsLogger)
  -Span        : 추적 단위 (ISpan, OpenTelemetrySpan)
  -Metric      : 메트릭 (단독으로는 거의 사용 안 함)
  -SpanFactory : Span 팩토리 (ISpanFactory)
  -MetricRecorder : Metric 기록자 (IMetricRecorder)

단독 사용 (활동):
  Logging      : 로깅 활동 (UsecaseLoggingPipeline)
  Tracing      : 추적 활동 (UsecaseTracingPipeline)
  Metrics      : 메트릭 활동 (UsecaseMetricsPipeline)
```

### 2.3 용어 일관성 원칙

**Endpoint와 Protocol 명명:**
- Endpoint: 동명사 형태 (`LoggingEndpoint`, `TracingEndpoint`, `MetricsEndpoint`)
- Protocol: 동명사 형태 (`LoggingProtocol`, `TracingProtocol`, `MetricsProtocol`)
- 이유: 설정/구성을 나타내므로 동명사가 자연스러움

**Configurator 명명:**
- Logging: `LoggingConfigurator` (동명사)
- Tracing: `TracingConfigurator` (동명사 - 내부 일관성 우선)
- Metrics: `MetricsConfigurator` (복수 명사)

## 3. 명명 규칙

### 3.1 Configurator (설정 클래스)

**규칙**: `{Signal}Configurator`

Signal 전체 시스템을 설정하는 클래스이므로 Signal 이름을 접두사로 사용합니다.

```csharp
// ✅ Correct
public class LoggingConfigurator { }
public class TracingConfigurator { }   // 내부 일관성 우선 (Logging/Tracing 동명사 패턴 통일)
public class MetricsConfigurator { }

// ❌ Incorrect
public class LogsConfigurator { }      // "Logs"는 파일 디렉토리와 혼동
public class LoggerConfigurator { }    // Logger는 구성 요소, Signal 아님
public class TraceConfigurator { }     // 단수형 부적절
public class TracesConfigurator { }    // OpenTelemetry 공식 용어이지만 내부 일관성이 더 중요
```

**사용 예시**:
```csharp
builder.ConfigureLogging(logging =>
{
    logging.AddEnricher<MyEnricher>();
});

builder.ConfigureTracing(tracing =>
{
    tracing.AddSource("MySource");
});
```

### 3.2 Logger (로거 구성 요소)

**규칙**: `{Purpose}Logger`

로그를 생성하는 구성 요소는 용도나 역할을 접두사로, Logger를 접미사로 사용합니다.

```csharp
// ✅ Correct
public class StartupLogger : IHostedService { }
public class ConsoleLogger { }
public class FileLogger { }
public interface IStartupOptionsLogger { }

// ❌ Incorrect
public class StartupLogging { }        // Logging은 활동/설정, 객체 아님
public class LoggerStartup { }         // 어색한 어순
```

### 3.3 Pipeline (파이프라인)

**규칙**: `{Layer}{Signal}Pipeline`

Pipeline은 특정 계층에서 Signal 활동을 수행하는 클래스입니다.

```csharp
// ✅ Correct
public class UsecaseLoggingPipeline<TRequest, TResponse> { }
public class UsecaseTracingPipeline<TRequest, TResponse> { }
public class UsecaseMetricsPipeline<TRequest, TResponse> { }

public class AdapterLoggingPipeline { }
public class AdapterTracingPipeline { }

// ❌ Incorrect
public class UsecaseLoggerPipeline { }   // Logger는 구성 요소, 활동 아님
public class UsecaseTracePipeline { }    // Trace는 단수형
public class LoggingUsecasePipeline { }  // 어색한 어순
```

**이유**:
- Pipeline은 "무엇을 하는가"를 표현 → Signal 활동 강조
- 세 가지 Pipeline 모두 동일한 패턴 유지 (일관성)
- Configurator와 명명 패턴 일치

### 3.4 Extensions (확장 메서드)

**규칙**: `{Target}Extensions`

확장 대상 Component를 접두사로 사용합니다.

```csharp
// ✅ Correct
public static class LoggerExtensions { }
public static class UsecaseLoggerExtensions { }
public static class SpanExtensions { }
public static class MetricExtensions { }

// ❌ Incorrect
public static class LoggingExtensions { }  // Logging은 설정/활동
public static class ExtensionsLogger { }   // 어색한 어순
```

### 3.5 Options (설정 속성)

**규칙**: `{Signal}{Property}`

Options 속성명은 Signal 이름(동명사 형태)을 접두사로 사용합니다.

```csharp
// ✅ Correct
public class OpenTelemetryOptions
{
    // Endpoint는 동명사 형태
    public string LoggingEndpoint { get; set; }
    public string TracingEndpoint { get; set; }
    public string MetricsEndpoint { get; set; }

    // Protocol도 동명사 형태
    public string LoggingProtocol { get; set; }
    public string TracingProtocol { get; set; }
    public string MetricsProtocol { get; set; }

    // Getter 메서드도 동일
    public string GetLoggingEndpoint() { }
    public string GetTracingEndpoint() { }
    public string GetMetricsEndpoint() { }

    public OtlpCollectorProtocol GetLoggingProtocol() { }
    public OtlpCollectorProtocol GetTracingProtocol() { }
    public OtlpCollectorProtocol GetMetricsProtocol() { }
}

// ❌ Incorrect
public string LogsEndpoint { get; set; }        // "Logs"는 파일 디렉토리와 혼동
public string TracesEndpoint { get; set; }      // Endpoint는 동명사 사용
public string LoggerEndpoint { get; set; }      // Logger는 구성 요소
public string LogEndpoint { get; set; }         // 단수형
```

**일관성 원칙**:
- `LoggingEndpoint` + `LoggingProtocol` (✅ 동명사로 통일)
- `TracingEndpoint` + `TracingProtocol` (✅ 동명사로 통일)
- `MetricsEndpoint` + `MetricsProtocol` (✅ 동명사로 통일)

### 3.6 Builder Methods (빌더 메서드)

**규칙**: `Configure{Signal}()`

빌더 메서드는 Signal 설정을 위해 Signal 이름을 사용합니다.

```csharp
// ✅ Correct
public class OpenTelemetryBuilder
{
    public OpenTelemetryBuilder ConfigureLogging(Action<LoggingConfigurator> configure) { }
    public OpenTelemetryBuilder ConfigureTracing(Action<TracingConfigurator> configure) { }
    public OpenTelemetryBuilder ConfigureMetrics(Action<MetricsConfigurator> configure) { }
}

// ❌ Incorrect
public OpenTelemetryBuilder ConfigureSerilog(...) { }  // 기술 종속적
public OpenTelemetryBuilder ConfigureLogs(...) { }     // Logs는 파일과 혼동
public OpenTelemetryBuilder ConfigureLogger(...) { }   // Logger는 구성 요소
public OpenTelemetryBuilder ConfigureTraces(...) { }   // Tracing으로 일관성 유지
```

### 3.7 Interfaces (인터페이스)

**규칙**: Component 유형을 접미사로 사용

```csharp
// ✅ Correct - Component 유형이 명확
public interface IStartupOptionsLogger { }
public interface IMetricRecorder { }
public interface ISpanFactory { }
public interface ISpan { }

// ❌ Incorrect
public interface ILogging { }           // 너무 추상적
public interface ILog { }               // 단일 로그 엔트리와 혼동
```

### 3.8 Implementation Classes (구현 클래스)

**규칙**: `{Technology}{Component}`

특정 기술의 구현체는 기술명을 접두사로 사용합니다.

```csharp
// ✅ Correct
public class OpenTelemetrySpan : ISpan { }
public class OpenTelemetrySpanFactory : ISpanFactory { }
public class OpenTelemetryMetricRecorder : IMetricRecorder { }

// ❌ Incorrect
public class SpanOpenTelemetry : ISpan { }     // 어색한 어순
public class OTelSpan : ISpan { }              // 약어 사용 지양
```

## 4. 특수 케이스

### 4.1 Span vs Tracer

Tracing 시스템에서는 두 가지 개념이 있습니다:

- **Span**: 추적의 단일 작업 단위 (데이터 객체)
- **Tracer**: Span을 생성하는 팩토리 (생성 객체)

```csharp
// ✅ Correct
public interface ISpan { }              // 단일 작업 단위
public interface ISpanFactory { }       // Span 생성 팩토리 (Tracer 역할)

// OpenTelemetry에서는 ActivitySource가 Tracer 역할
public class OpenTelemetrySpanFactory : ISpanFactory
{
    private readonly ActivitySource _activitySource;  // Tracer
}
```

### 4.2 Logging vs Logger

- **Logging**: Signal 이름, 설정/활동 (접두사)
- **Logger**: Component 유형 (접미사)

```csharp
// ✅ Correct - 설정 클래스 (Logging)
public class LoggingConfigurator { }

// ✅ Correct - 구성 요소 (Logger)
public class StartupLogger { }
public interface IStartupOptionsLogger { }

// ✅ Correct - Pipeline (Logging 활동)
public class UsecaseLoggingPipeline { }

// ✅ Correct - Extensions (Logger 확장)
public static class UsecaseLoggerExtensions { }

// ✅ Correct - Options (Logging 설정)
public string LoggingEndpoint { get; set; }
public string LoggingProtocol { get; set; }
```

### 4.3 Tracing 일관성 원칙

- **Tracing**: 모든 컨텍스트에서 일관되게 사용 (동명사)

```csharp
// ✅ Correct - Configurator (Tracing - 내부 일관성 우선)
public class TracingConfigurator { }

// ✅ Correct - Builder Method (Tracing)
public OpenTelemetryBuilder ConfigureTracing(Action<TracingConfigurator> configure) { }

// ✅ Correct - Pipeline (Tracing 활동)
public class UsecaseTracingPipeline { }

// ✅ Correct - Options (Tracing 설정)
public string TracingEndpoint { get; set; }
public string TracingProtocol { get; set; }
public string GetTracingEndpoint() { }
public OtlpCollectorProtocol GetTracingProtocol() { }
```

**명명 원칙 정리:**
- **Configurator**: `TracingConfigurator` (내부 일관성: Logging/Tracing 패턴 통일)
- **Options/Settings**: `TracingEndpoint`, `TracingProtocol` (설정 활동)
- **Pipeline**: `UsecaseTracingPipeline` (추적 활동)
- **Builder Method**: `ConfigureTracing()` (일관성 유지)

**설계 철학:**
OpenTelemetry 공식 용어는 "Traces"이지만, 우리는 내부 일관성을 더 중요하게 생각합니다.
`LoggingConfigurator`, `TracingConfigurator`, `MetricsConfigurator`의 일관된 패턴이
외부 표준보다 코드베이스의 가독성과 유지보수성에 더 기여한다고 판단했습니다.

### 4.4 Configuration vs Configurator

- **Configuration**: 설정 데이터/옵션
- **Configurator**: 설정을 수행하는 빌더 클래스

```csharp
// ✅ Configuration (데이터)
public class OpenTelemetryOptions { }
public class LoggingConfiguration { }

// ✅ Configurator (빌더)
public class LoggingConfigurator { }
public class TracingConfigurator { }
```

### 4.5 폴더 명명 규칙

**폴더는 복수형을 사용합니다** - "무엇들을 담고 있는가"

```
Src/Functorium/Applications/Observabilities/
├── Loggers/          ✅ Logger 관련 클래스들을 담고 있음
├── Metrics/          ✅ Metric 관련 클래스들을 담고 있음
├── Spans/            ✅ Span 관련 클래스들을 담고 있음
└── Context/          ✅ Context 관련 클래스들을 담고 있음

Src/Functorium/Adapters/Observabilities/
├── Loggers/          ✅
├── Metrics/          ✅
├── Spans/            ✅
└── Context/          ✅
```

## 5. 네임스페이스 구조

```
Functorium.Applications.Observabilities/
├── Context/
│   ├── IContextPropagator.cs
│   └── IObservabilityContext.cs
├── Loggers/
│   └── UsecaseLoggerExtensions.cs         // Logger 확장
├── Metrics/
│   └── IMetricRecorder.cs                 // Metric 기록
├── Spans/
│   ├── ISpan.cs                           // Span 인터페이스
│   └── ISpanFactory.cs                    // Span 팩토리
└── ObservabilityNaming.cs

Functorium.Adapters.Observabilities/
├── Builders/
│   ├── Configurators/
│   │   ├── LoggingConfigurator.cs         // Logging 설정
│   │   ├── TracingConfigurator.cs         // Tracing 설정
│   │   └── MetricsConfigurator.cs         // Metrics 설정
│   └── OpenTelemetryBuilder.cs
├── Loggers/
│   ├── IStartupOptionsLogger.cs           // Logger 인터페이스
│   └── StartupLogger.cs                   // Logger 구현
├── Metrics/
│   └── OpenTelemetryMetricRecorder.cs     // Metric 구현
└── Spans/
    ├── OpenTelemetrySpan.cs               // Span 구현
    └── OpenTelemetrySpanFactory.cs        // SpanFactory 구현

Functorium.Adapters.Observabilities.Pipelines/
├── UsecaseLoggingPipeline.cs              // Logging Pipeline
├── UsecaseTracingPipeline.cs              // Tracing Pipeline
└── UsecaseMetricsPipeline.cs              // Metrics Pipeline
```

## 6. ObservabilityNaming 상수

`ObservabilityNaming` 클래스는 모든 관찰 가능성 관련 상수를 정의합니다.

```csharp
public static class ObservabilityNaming
{
    /// <summary>
    /// OpenTelemetry Signals
    /// - Logging: 로그 신호 시스템
    /// - Tracing: 분산 추적 신호 시스템
    /// - Metrics: 메트릭 신호 시스템
    /// </summary>
    public static class Signals
    {
        public const string Logging = "logging";
        public const string Tracing = "tracing";
        public const string Metrics = "metrics";
    }

    /// <summary>
    /// OpenTelemetry 표준 attributes
    /// https://opentelemetry.io/docs/specs/semconv/
    /// </summary>
    public static class OTelAttributes
    {
        public const string CodeFunction = "code.function";
        public const string ErrorType = "error.type";
        // ...
    }

    /// <summary>
    /// 커스텀 attributes (request.*, response.*, error.*)
    /// </summary>
    public static class CustomAttributes
    {
        public const string RequestLayer = "request.layer";
        public const string ResponseStatus = "response.status";
        // ...
    }
}
```

## 7. 실전 예시

### 7.1 Configurator 구현

```csharp
/// <summary>
/// Tracing 확장 설정을 위한 Configurator 클래스
/// ActivitySource, Processor 등 프로젝트별 Tracing 확장 제공
/// </summary>
public class TracingConfigurator
{
    private readonly List<string> _sourceNames = new();
    private readonly OpenTelemetryOptions _options;

    public TracingConfigurator AddSource(string sourceName)
    {
        _sourceNames.Add(sourceName);
        return this;
    }
}
```

### 7.2 Pipeline 구현

```csharp
/// <summary>
/// Usecase의 Logging을 담당하는 Pipeline
/// Result 패턴을 사용하여 요청/응답을 안전하게 로깅합니다.
/// </summary>
public sealed class UsecaseLoggingPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>
    , IPipelineBehavior<TRequest, TResponse>
{
    private readonly ILogger<UsecaseLoggingPipeline<TRequest, TResponse>> _logger;

    public async ValueTask<TResponse> Handle(
        TRequest request,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        // 요청 로깅
        _logger.LogRequestMessage(...);

        // 다음 Pipeline 실행
        TResponse response = await next(request, cancellationToken);

        // 응답 로깅
        _logger.LogResponseMessage(...);

        return response;
    }
}
```

### 7.3 Builder 사용

```csharp
services.AddOpenTelemetry(options, builder =>
{
    builder
        .ConfigureLogging(logging =>
        {
            logging.AddEnricher<EnvironmentEnricher>();
            logging.AddDestructuringPolicy<ValueObjectPolicy>();
        })
        .ConfigureTracing(tracing =>
        {
            tracing.AddSource("MyApplication");
            tracing.AddProcessor(new CustomProcessor());
        })
        .ConfigureMetrics(metrics =>
        {
            metrics.AddMeter("MyApplication");
        });
});
```

### 7.4 Options 설정 (appsettings.json)

```json
{
  "OpenTelemetry": {
    "ServiceName": "MyService",
    "CollectorEndpoint": "http://localhost:18889",

    // 개별 엔드포인트 (선택적)
    "LoggingEndpoint": "http://localhost:21892",
    "TracingEndpoint": "http://localhost:21890",
    "MetricsEndpoint": "http://localhost:21891",

    // 개별 프로토콜 (선택적)
    "LoggingProtocol": "HttpProtobuf",
    "TracingProtocol": "Grpc",
    "MetricsProtocol": "Grpc"
  }
}
```

## 8. 체크리스트

새로운 Observability 관련 클래스를 작성할 때 다음을 확인하세요:

- [ ] Signal 이름을 사용하는가? → `Logging`, `Tracing`, `Metrics`
- [ ] Component 유형을 사용하는가? → `Logger`, `Span`, `Metric`, `Tracer`, `Meter`
- [ ] 접두사로 Signal 이름을 사용하는가? (설정/활동)
- [ ] 접미사로 Component 유형을 사용하는가? (구성 요소)
- [ ] `.gitignore`와 충돌하지 않는가? (`Logs` 지양)
- [ ] OpenTelemetry 표준 용어와 일치하는가?
- [ ] 세 가지 Signal이 일관된 패턴을 따르는가?
- [ ] Endpoint와 Protocol이 모두 동명사 형태인가?
- [ ] Configurator는 Tracing을 사용하는가? (TracesConfigurator ❌)

## 9. 용어 정리표

| 용도 | Logging | Tracing | Metrics | 비고 |
|------|---------|---------|---------|------|
| **Configurator** | `LoggingConfigurator` | `TracingConfigurator` | `MetricsConfigurator` | 내부 일관성 우선 |
| **Endpoint** | `LoggingEndpoint` | `TracingEndpoint` | `MetricsEndpoint` | 동명사 형태 |
| **Protocol** | `LoggingProtocol` | `TracingProtocol` | `MetricsProtocol` | 동명사 형태 |
| **Pipeline** | `UsecaseLoggingPipeline` | `UsecaseTracingPipeline` | `UsecaseMetricsPipeline` | 활동 강조 |
| **Builder Method** | `ConfigureLogging()` | `ConfigureTracing()` | `ConfigureMetrics()` | 일관성 유지 |
| **Getter Method** | `GetLoggingEndpoint()` | `GetTracingEndpoint()` | `GetMetricsEndpoint()` | 동명사 형태 |
| **Getter Method** | `GetLoggingProtocol()` | `GetTracingProtocol()` | `GetMetricsProtocol()` | 동명사 형태 |

## 10. 참고 자료

- [OpenTelemetry Specification](https://opentelemetry.io/docs/specs/)
- [OpenTelemetry Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/)
- [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet)

## 11. 변경 이력

| 날짜 | 변경 사항 | 작성자 |
|------|----------|--------|
| 2025-12-31 | 최초 작성 - Logging/Tracing/Metrics 용어 통일 규칙 수립 | - |
| 2025-12-31 | 최종 확정 - TracingConfigurator (내부 일관성 우선), TracingEndpoint/Protocol 규칙 명확화 | - |
