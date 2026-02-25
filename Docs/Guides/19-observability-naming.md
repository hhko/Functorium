# Observability 네이밍 가이드

## 목차

- [요약](#요약)
- [코드 네이밍 규칙](#코드-네이밍-규칙)
- [필드/태그 네이밍 규칙](#필드태그-네이밍-규칙)
- [Logger 메서드 네이밍 규칙](#logger-메서드-네이밍-규칙)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)
- [참고 문서](#참고-문서)

---

## 요약

### 주요 명령

```csharp
// Configurator: Signal 접두사
LoggingConfigurator, TracingConfigurator, MetricsConfigurator

// Pipeline: Layer + Signal
UsecaseLoggingPipeline, UsecaseTracingPipeline, UsecaseMetricsPipeline

// Options: Signal + Property (동명사)
LoggingEndpoint, TracingEndpoint, MetricsEndpoint

// Logger Method: Log{Context}{Phase}{Status}
LogUsecaseRequest, LogUsecaseResponseSuccess, LogUsecaseResponseError
```

### 주요 절차

1. 새 클래스 작성 시 Signal 이름(`Logging`, `Tracing`, `Metrics`)과 Component 유형(`Logger`, `Span`, `Metric`)을 구분
2. 접두사로 Signal 이름 사용 (설정/활동 클래스), 접미사로 Component 유형 사용 (구성 요소)
3. 필드/태그는 `snake_case + dot` 표기법 사용 (`request.layer`, `response.status`)
4. `count` 필드는 단독 시 `.count`, 형용사 조합 시 `_count` 사용

### 주요 개념

| 개념 | 규칙 | 예시 |
|------|------|------|
| Signal 접두사 | 설정/활동 대상 | `LoggingConfigurator`, `TracingEndpoint` |
| Component 접미사 | 구체적 객체 | `StartupLogger`, `ISpanFactory` |
| 필드 표기법 | `snake_case + dot` | `request.handler.method` |
| 계층 구조 | `{namespace}.{property}` | `request.*`, `response.*`, `error.*` |
| Logger 메서드 | `Log{Context}{Phase}{Status}` | `LogDomainEventHandlerResponseError` |
| 내부 일관성 우선 | OpenTelemetry "Traces" 대신 `Tracing` 사용 | `TracingConfigurator` (not `TracesConfigurator`) |

---

## 코드 네이밍 규칙

### 개요

이 문서는 Functorium 프로젝트의 Observability 관련 코드 작성 시 따라야 할 명명 규칙을 정의합니다.
OpenTelemetry 표준 용어를 기반으로 하되, .NET 생태계와 실용성을 고려하여 수립되었습니다.

### 핵심 원칙

#### OpenTelemetry Signals

OpenTelemetry는 세 가지 관찰 가능성 신호(Signals)를 정의합니다:

- **Logging**: 로그 신호 시스템
- **Tracing**: 분산 추적 신호 시스템
- **Metrics**: 메트릭 신호 시스템

#### 용어 역할 구분

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

#### 용어 일관성 원칙

**Endpoint와 Protocol 명명:**
- Endpoint: 동명사 형태 (`LoggingEndpoint`, `TracingEndpoint`, `MetricsEndpoint`)
- Protocol: 동명사 형태 (`LoggingProtocol`, `TracingProtocol`, `MetricsProtocol`)
- 이유: 설정/구성을 나타내므로 동명사가 자연스러움

**Configurator 명명:**
- Logging: `LoggingConfigurator` (동명사)
- Tracing: `TracingConfigurator` (동명사 — 근거: [Tracing 일관성 원칙](#tracing-일관성-원칙) 참조)
- Metrics: `MetricsConfigurator` (복수 명사)

### 명명 규칙

#### Configurator (설정 클래스)

**규칙**: `{Signal}Configurator`

Signal 전체 시스템을 설정하는 클래스이므로 Signal 이름을 접두사로 사용합니다.

```csharp
// ✅ Correct
public class LoggingConfigurator { }
public class TracingConfigurator { }
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

#### Logger (로거 구성 요소)

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

#### Pipeline (파이프라인)

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

#### Extensions (확장 메서드)

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

#### Options (설정 속성)

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

#### Builder Methods (빌더 메서드)

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

#### Interfaces (인터페이스)

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

#### Implementation Classes (구현 클래스)

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

### 특수 케이스

#### Span vs Tracer

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

#### Logging vs Logger

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

#### Tracing 일관성 원칙

- **Tracing**: 모든 컨텍스트에서 일관되게 사용 (동명사)

```csharp
// ✅ Correct - Configurator
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
- **Configurator**: `TracingConfigurator`
- **Options/Settings**: `TracingEndpoint`, `TracingProtocol` (설정 활동)
- **Pipeline**: `UsecaseTracingPipeline` (추적 활동)
- **Builder Method**: `ConfigureTracing()` (일관성 유지)

> **Design Decision — 내부 일관성 우선 원칙**
>
> OpenTelemetry 공식 용어는 "Traces"이지만, Functorium은 **내부 일관성**을 우선합니다.
> `LoggingConfigurator`, `TracingConfigurator`, `MetricsConfigurator`의 일관된 동명사 패턴이
> 외부 표준 준수보다 코드베이스의 가독성과 유지보수성에 더 기여한다고 판단했습니다.
> 동일한 원칙이 Endpoint, Protocol, Pipeline 등 모든 Signal 접두사 명명에 적용됩니다.

#### Configuration vs Configurator

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

#### 폴더 명명 규칙

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

> **참고**: 위 구조는 설계 의도를 나타내며, 현재 코드베이스의 실제 구조와 차이가 있습니다. 현재 `Applications/Observabilities/` 디렉토리는 존재하지 않고, `Adapters/Observabilities/`의 실제 하위 폴더 구조는 다음과 같습니다:
> ```
> Src/Functorium/Adapters/Observabilities/
> ├── Builders/Configurators/   (LoggingConfigurator, TracingConfigurator, MetricsConfigurator 등)
> ├── Events/                   (ObservableDomainEventPublisher 등)
> ├── Loggers/                  (UsecaseLoggerExtensions, StartupLogger 등)
> ├── Naming/                   (ObservabilityNaming 상수)
> └── Pipelines/                (UsecaseLoggingPipeline, UsecaseTracingPipeline, UsecaseMetricsPipeline 등)
> ```

### 네임스페이스 구조

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

### ObservabilityNaming 상수

`ObservabilityNaming` 클래스는 모든 관찰 가능성 관련 상수를 정의합니다.

```csharp
public static partial class ObservabilityNaming
{
    public static class Layers
    {
        public const string Application = "application";
        public const string Adapter = "adapter";
    }

    public static class Categories
    {
        public const string Usecase = "usecase";
        public const string Repository = "repository";
        public const string Event = "event";
        public const string Unknown = "unknown";
    }

    public static class CategoryTypes
    {
        public const string Command = "command";
        public const string Query = "query";
        public const string Event = "event";
        public const string Unknown = "unknown";
    }

    /// <summary>
    /// OpenTelemetry 표준 attributes
    /// https://opentelemetry.io/docs/specs/semconv/
    /// </summary>
    public static class OTelAttributes
    {
        public const string ErrorType = "error.type";
        public const string ServiceNamespace = "service.namespace";
        public const string ServiceName = "service.name";
        public const string ServiceVersion = "service.version";
        public const string ServiceInstanceId = "service.instance.id";
        // ...
    }

    /// <summary>
    /// 커스텀 attributes (request.*, response.*, error.*)
    /// </summary>
    public static class CustomAttributes
    {
        public const string RequestLayer = "request.layer";
        public const string RequestCategory = "request.category";
        public const string RequestCategoryType = "request.category.type";
        public const string RequestHandler = "request.handler";
        public const string RequestHandlerMethod = "request.handler.method";
        public const string ResponseStatus = "response.status";
        public const string ResponseElapsed = "response.elapsed";
        public const string ErrorCode = "error.code";
        // ...
    }
}
```

### 실전 예시

#### Configurator 구현

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

#### Pipeline 구현

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
        _logger.LogUsecaseRequest(...);

        // 다음 Pipeline 실행
        TResponse response = await next(request, cancellationToken);

        // 응답 로깅
        _logger.LogUsecaseResponseSuccess(...);

        return response;
    }
}
```

#### Builder 사용

```csharp
services
    .RegisterOpenTelemetry(configuration, AssemblyReference.Assembly)
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
    })
    .Build();
```

#### Options 설정 (appsettings.json)

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

### 체크리스트

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

### 용어 정리표

| 용도 | Logging | Tracing | Metrics | 비고 |
|------|---------|---------|---------|------|
| **Configurator** | `LoggingConfigurator` | `TracingConfigurator` | `MetricsConfigurator` | 내부 일관성 우선 |
| **Endpoint** | `LoggingEndpoint` | `TracingEndpoint` | `MetricsEndpoint` | 동명사 형태 |
| **Protocol** | `LoggingProtocol` | `TracingProtocol` | `MetricsProtocol` | 동명사 형태 |
| **Pipeline** | `UsecaseLoggingPipeline` | `UsecaseTracingPipeline` | `UsecaseMetricsPipeline` | 활동 강조 |
| **Builder Method** | `ConfigureLogging()` | `ConfigureTracing()` | `ConfigureMetrics()` | 일관성 유지 |
| **Getter Method** | `GetLoggingEndpoint()` | `GetTracingEndpoint()` | `GetMetricsEndpoint()` | 동명사 형태 |
| **Getter Method** | `GetLoggingProtocol()` | `GetTracingProtocol()` | `GetMetricsProtocol()` | 동명사 형태 |

---

> **Section 1과 Section 2의 관계:** Section 1은 C# 소스 코드에서 사용하는 식별자 명명 규칙을 다룹니다. 클래스명은 PascalCase(`TracingConfigurator`, `UsecaseLoggingPipeline`), 파라미터는 camelCase를 따릅니다. Section 2는 이러한 코드가 실행 시 방출하는 Observability 데이터의 식별자 명명 규칙을 다룹니다. 계측 데이터에서는 dot-separated lowercase(`request.layer`, `response.status`)와 snake_case(`success_count`)를 사용합니다. 즉, Section 1은 "코드를 어떻게 작성하는가"에 대한 규칙이고, Section 2는 "코드가 생성하는 텔레메트리 데이터를 어떻게 명명하는가"에 대한 규칙입니다.

## 필드/태그 네이밍 규칙

### 개요

이 섹션은 Functorium 프로젝트의 Logging, Tracing, Metrics에서 사용하는 필드(Field)와 태그(Tag) 이름 규칙을 정의합니다.

**목적:**
- 일관된 필드 이름으로 관측성 데이터 검색 및 분석 용이
- OpenTelemetry 시맨틱 규칙과의 호환성 유지
- 팀 내 명명 규칙 표준화

**범위:**
- Logging 구조화 필드
- Metrics 태그
- Tracing Span 속성

### 기본 명명 규칙

#### 표기법: `snake_case + dot`

OpenTelemetry 시맨틱 규칙을 준수하여 `snake_case + dot` 표기법을 사용합니다.

```
# 올바른 예시
request.layer
request.category.type
request.handler.method
response.status
error.code

# 잘못된 예시
requestLayer          # camelCase 사용 금지
request-layer         # kebab-case 사용 금지
REQUEST_LAYER         # UPPER_SNAKE_CASE 사용 금지
```

#### 계층 구조: `{namespace}.{property}`

필드는 네임스페이스와 속성의 계층 구조로 구성합니다.

| 네임스페이스 | 설명 | 예시 |
|-------------|------|------|
| `request.*` | 요청 관련 정보 | `request.layer`, `request.handler` |
| `response.*` | 응답 관련 정보 | `response.status`, `response.elapsed` |
| `error.*` | 오류 관련 정보 | `error.type`, `error.code` |

### 필드 카테고리별 규칙

#### Request 필드 (`request.*`)

| 필드 | 설명 | 예시 값 |
|------|------|--------|
| `request.layer` | 아키텍처 레이어 | `"application"`, `"adapter"` |
| `request.category` | 요청 카테고리 | `"usecase"`, `"repository"`, `"event"` |
| `request.category.type` | CQRS 타입 | `"command"`, `"query"`, `"event"` |
| `request.handler` | Handler 클래스 이름 | `"CreateOrderCommandHandler"` |
| `request.handler.method` | Handler 메서드 이름 | `"Handle"`, `"GetById"` |
| `request.params.{name}` | 동적 파라미터 값 | 파라미터 값 |
| `request.params.{name}.count` | 컬렉션 파라미터 크기 | 정수 값 |
| `request.event.count` | 이벤트 개수 | 정수 값 |

#### Response 필드 (`response.*`)

| 필드 | 설명 | 예시 값 |
|------|------|--------|
| `response.status` | 응답 상태 | `"success"`, `"failure"` |
| `response.elapsed` | 처리 시간(초) | `0.0123` |
| `response.result` | 응답 결과 값 | 반환 값 |
| `response.result.count` | 컬렉션 결과 크기 | 정수 값 |
| `response.event.success_count` | 성공한 이벤트 수 | 정수 값 |
| `response.event.failure_count` | 실패한 이벤트 수 | 정수 값 |

#### Error 필드 (`error.*`)

| 필드 | 설명 | 예시 값 |
|------|------|--------|
| `error.type` | 오류 분류 | `"expected"`, `"exceptional"`, `"aggregate"` |
| `error.code` | 도메인 특화 오류 코드 | `"ORDER_NOT_FOUND"` |
| `@error` | 구조화된 오류 객체 | 오류 상세 정보 |

### `count` 필드 규칙

`count` 필드는 컬렉션 크기를 나타내며, 다음 규칙을 따릅니다.

#### 규칙 1: `count`가 단독으로 사용될 때 → `.count`

`count`가 다른 형용사나 명사 없이 단독으로 사용될 때는 `.count` 형식을 사용합니다.

```
# 올바른 예시
request.event.count              # 이벤트 개수
request.params.orders.count      # 컬렉션 파라미터 크기
response.result.count            # 컬렉션 결과 크기

# 잘못된 예시
request.event_count              # 단독 count는 .count 사용
```

#### 규칙 2: `count`가 다른 형용사/명사와 조합될 때 → `{prefix}_count`

`count` 앞에 형용사나 명사가 붙을 때는 `_count` 형식을 사용합니다.

```
# 올바른 예시
response.event.success_count     # 성공한 이벤트 수
response.event.failure_count     # 실패한 이벤트 수

# 잘못된 예시
response.event.success.count     # 조합 count는 _count 사용
```

#### 현재 필드 적용 예시

| 필드명 | 규칙 | 설명 |
|--------|------|------|
| `request.event.count` | 단독 `.count` ✅ | DomainEvent 배치 발행 시 이벤트 개수 |
| `request.params.{name}.count` | 단독 `.count` ✅ | 컬렉션 파라미터 크기 |
| `response.result.count` | 단독 `.count` ✅ | 컬렉션 결과 크기 |
| `response.event.success_count` | 조합 `_count` ✅ | 부분 실패 시 성공한 이벤트 수 |
| `response.event.failure_count` | 조합 `_count` ✅ | 부분 실패 시 실패한 이벤트 수 |

### 레이어별 필드 목록

#### Application 레이어 필드

| Field/Tag | Logging | Metrics | Tracing | 설명 |
|-----------|---------|---------|---------|------|
| `request.layer` | ✅ | ✅ | ✅ | `"application"` |
| `request.category` | ✅ | ✅ | ✅ | `"usecase"` |
| `request.category.type` | ✅ | ✅ | ✅ | `"command"`, `"query"`, `"event"` |
| `request.handler` | ✅ | ✅ | ✅ | Handler 클래스 이름 |
| `request.handler.method` | ✅ | ✅ | ✅ | `"Handle"` |
| `@request.message` | ✅ | - | - | Command/Query 객체 |
| `response.status` | ✅ | ✅ | ✅ | `"success"`, `"failure"` |
| `response.elapsed` | ✅ | - | ✅ | 처리 시간(초) |
| `@response.message` | ✅ | - | - | 응답 객체 |
| `error.type` | ✅ | ✅ | ✅ | `"expected"`, `"exceptional"`, `"aggregate"` |
| `error.code` | ✅ | ✅ | ✅ | 도메인 특화 오류 코드 |
| `@error` | ✅ | - | - | 구조화된 오류 객체 |

#### Adapter 레이어 필드

| Field/Tag | Logging | Metrics | Tracing | 설명 |
|-----------|---------|---------|---------|------|
| `request.layer` | ✅ | ✅ | ✅ | `"adapter"` |
| `request.category` | ✅ | ✅ | ✅ | Adapter 카테고리 (예: `"repository"`) |
| `request.handler` | ✅ | ✅ | ✅ | Handler 클래스 이름 |
| `request.handler.method` | ✅ | ✅ | ✅ | 메서드 이름 |
| `request.params.{name}` | ✅ | - | - | 개별 메서드 파라미터 |
| `request.params.{name}.count` | ✅ | - | - | 컬렉션 파라미터 크기 |
| `response.status` | ✅ | ✅ | ✅ | `"success"`, `"failure"` |
| `response.elapsed` | ✅ | - | ✅ | 처리 시간(초) |
| `response.result` | ✅ | - | - | 메서드 반환 값 |
| `response.result.count` | ✅ | - | - | 컬렉션 결과 크기 |
| `error.type` | ✅ | ✅ | ✅ | `"expected"`, `"exceptional"`, `"aggregate"` |
| `error.code` | ✅ | ✅ | ✅ | 도메인 특화 오류 코드 |
| `@error` | ✅ | - | - | 구조화된 오류 객체 |

#### DomainEvent 필드

| Field/Tag | Logging | Metrics | Tracing | 설명 |
|-----------|---------|---------|---------|------|
| `request.layer` | ✅ | ✅ | ✅ | `"adapter"` (Publisher), `"application"` (Handler) |
| `request.category` | ✅ | ✅ | ✅ | `"event"` (Publisher), `"usecase"` (Handler) |
| `request.category.type` | ✅ | ✅ | ✅ | `"event"` (Handler만) |
| `request.handler` | ✅ | ✅ | ✅ | Event/Aggregate 타입명 또는 Handler 클래스명 |
| `request.handler.method` | ✅ | ✅ | ✅ | `"Publish"`, `"PublishTrackedEvents"`, `"Handle"` |
| `request.event.type` | ✅ | - | ✅ | 이벤트 타입명 (Handler만) |
| `request.event.id` | ✅ | - | ✅ | 이벤트 고유 ID (Handler만) |
| `request.event.count` | ✅ | - | ✅ | 배치 발행 시 이벤트 개수 |
| `@request.message` | ✅ | - | - | 이벤트 객체 |
| `response.status` | ✅ | ✅ | ✅ | `"success"`, `"failure"` |
| `response.elapsed` | ✅ | - | ✅ | 처리 시간(초) |
| `response.event.success_count` | ✅ | - | ✅ | 부분 실패 시 성공한 이벤트 수 |
| `response.event.failure_count` | ✅ | - | ✅ | 부분 실패 시 실패한 이벤트 수 |
| `error.type` | ✅ | ✅ | ✅ | `"expected"`, `"exceptional"` |
| `error.code` | ✅ | ✅ | ✅ | 도메인 특화 오류 코드 |
| `@error` | ✅ | - | - | 구조화된 오류 객체 |

### 예시 및 안티패턴

#### 올바른 예시

```
# 정적 필드
request.layer = "adapter"
request.category = "repository"
request.handler = "OrderRepository"
request.handler.method = "GetById"
response.status = "success"
response.elapsed = 0.0234

# 동적 필드 (Adapter 레이어)
request.params.id = "12345"
request.params.items = [...]
request.params.items.count = 5
response.result = {...}
response.result.count = 10

# DomainEvent 필드
request.event.count = 3
response.event.success_count = 2
response.event.failure_count = 1
```

#### 안티패턴

```
# 잘못된 표기법
requestLayer                     # camelCase 사용
request-layer                    # kebab-case 사용

# 잘못된 count 사용
response.event.success.count     # 조합 count에 .count 사용
response.event.failure.count     # 조합 count에 .count 사용

# 올바른 count 사용
response.event.success_count     # 조합 count는 _count
response.event.failure_count     # 조합 count는 _count
```

### 관련 코드 위치

| 구성 요소 | 파일 경로 |
|----------|----------|
| 필드 이름 생성 헬퍼 | `Src/Functorium.SourceGenerators/Generators/ObservablePortGenerator/CollectionTypeHelper.cs` |
| Application Logging | `Src/Functorium/Adapters/Observabilities/Pipelines/UsecaseLoggingPipeline.cs` |
| Adapter Logging | Source Generator 생성 코드 |
| Application Metrics | `Src/Functorium/Adapters/Observabilities/Pipelines/UsecaseMetricsPipeline.cs` |
| Application Tracing | `Src/Functorium/Adapters/Observabilities/Pipelines/UsecaseTracingPipeline.cs` |
| DomainEvent Publisher | `Src/Functorium/Adapters/Observabilities/Events/ObservableDomainEventPublisher.cs` |

### 관련 테스트

| 테스트 | 파일 경로 |
|--------|----------|
| Application Logging 구조 | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseLoggingPipelineStructureTests.cs` |
| Adapter Logging 구조 | `Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/ObservablePortLoggingStructureTests.cs` |
| Application Metrics 구조 | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseMetricsPipelineStructureTests.cs` |
| Adapter Metrics 구조 | `Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/ObservablePortMetricsStructureTests.cs` |
| Application Tracing 구조 | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseTracingPipelineStructureTests.cs` |
| Adapter Tracing 구조 | `Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/ObservablePortTracingStructureTests.cs` |
| DomainEvent Publisher Logging | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventPublisherLoggingStructureTests.cs` |
| DomainEvent Handler Logging | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerLoggingStructureTests.cs` |

## Logger 메서드 네이밍 규칙

LoggerExtensions 클래스에서 로그 메서드 이름을 작성할 때 따라야 하는 규칙입니다.

### 네이밍 패턴

```
Log{Context}{Phase}{Status}
```

### 구성 요소

| 요소 | 설명 | 값 |
|------|------|-----|
| `Context` | 로깅 대상 컨텍스트 | `Usecase`, `DomainEventHandler`, `DomainEventPublisher`, `DomainEventsPublisher` |
| `Phase` | 요청/응답 단계 | `Request`, `Response` |
| `Status` | 결과 상태 (Response에만 사용) | (없음), `Success`, `Warning`, `Error`, `PartialFailure` |

### 규칙

#### Phase 규칙
- `Request`: 작업 시작 시점 로그 (Status 없음)
- `Response`: 작업 완료 시점 로그 (Status 필수)

#### Status 규칙
| Status | 로그 레벨 | 용도 |
|--------|-----------|------|
| `Success` | Information | 정상 완료 |
| `Warning` | Warning | 예상된 에러 (Expected Error) |
| `Error` | Error | 예외적 에러 (Exceptional Error) |
| `PartialFailure` | Warning | 부분 실패 (일부만 성공) |

#### Context 규칙
- 단수/복수 구분: 단일 항목은 단수, 다중 항목은 복수 사용
  - `DomainEventPublisher`: 단일 이벤트 발행
  - `DomainEventsPublisher`: 다중 이벤트 발행 (Aggregate의 모든 이벤트)

### 예시

#### UsecaseLoggerExtensions
```csharp
// Request
LogUsecaseRequest<T>(...)

// Response
LogUsecaseResponseSuccess<T>(...)
LogUsecaseResponseWarning(...)
LogUsecaseResponseError(...)
```

#### DomainEventHandlerLoggerExtensions
```csharp
// Request
LogDomainEventHandlerRequest<TEvent>(...)

// Response
LogDomainEventHandlerResponseSuccess(...)
LogDomainEventHandlerResponseWarning(...)
LogDomainEventHandlerResponseError(...)   // Error 파라미터
LogDomainEventHandlerResponseError(...)   // Exception 파라미터 (오버로드)
```

#### DomainEventPublisherLoggerExtensions
```csharp
// 단일 이벤트
LogDomainEventPublisherRequest<TEvent>(...)
LogDomainEventPublisherResponseSuccess<TEvent>(...)
LogDomainEventPublisherResponseWarning<TEvent>(...)
LogDomainEventPublisherResponseError<TEvent>(...)

// 다중 이벤트 (Aggregate)
LogDomainEventsPublisherRequest(...)
LogDomainEventsPublisherResponseSuccess(...)
LogDomainEventsPublisherResponseWarning(...)
LogDomainEventsPublisherResponseError(...)
LogDomainEventsPublisherResponsePartialFailure(...)
```

### 안티패턴

| 잘못된 예 | 올바른 예 | 이유 |
|-----------|-----------|------|
| `LogRequestMessage` | `LogUsecaseRequest` | Context 누락 |
| `LogDomainEventHandlerSuccess` | `LogDomainEventHandlerResponseSuccess` | Phase 누락 |
| `LogDomainEventPublish` | `LogDomainEventPublisherRequest` | 동작 대신 Phase 사용 |
| `LogResponseMessageSuccess` | `LogUsecaseResponseSuccess` | "Message" 접미사 불필요 |

### 새 LoggerExtensions 클래스 추가 시

1. Context 이름 결정 (예: `Repository`, `ExternalApi`)
2. 필요한 Phase 결정 (`Request`, `Response` 또는 둘 다)
3. 필요한 Status 결정 (`Success`, `Warning`, `Error`)
4. 패턴에 따라 메서드 이름 작성

## 트러블슈팅

### Signal 이름과 Component 유형을 혼동하여 클래스 이름이 일관되지 않은 경우

**원인:** `Logging`(Signal/활동)과 `Logger`(Component/객체)의 역할 구분이 명확하지 않아 `LoggingPipeline`과 `LoggerPipeline`이 혼재됩니다.

**해결:** 설정/활동 클래스에는 Signal 이름을 접두사로(`LoggingConfigurator`, `UsecaseLoggingPipeline`), 구성 요소에는 Component 유형을 접미사로(`StartupLogger`, `ISpanFactory`) 사용합니다.

### 필드 이름에 camelCase나 kebab-case를 사용한 경우

**원인:** OpenTelemetry 시맨틱 규칙의 `snake_case + dot` 표기법을 따르지 않았습니다.

**해결:** 모든 필드/태그는 `request.handler.method` 형식의 소문자 dot notation을 사용합니다. `requestHandler`(camelCase)나 `request-handler`(kebab-case)는 사용하지 않습니다.

### count 필드에서 `.count`와 `_count`를 혼용한 경우

**원인:** 단독 count와 형용사 조합 count의 규칙을 구분하지 않았습니다.

**해결:** 단독 사용 시 `.count`(`request.event.count`, `response.result.count`), 형용사/명사 조합 시 `_count`(`response.event.success_count`, `response.event.failure_count`)를 사용합니다.

## FAQ

### Q1. OpenTelemetry에서는 "Traces"라고 하는데 왜 Functorium에서는 "Tracing"을 사용하나요?

Functorium은 **내부 일관성 우선 원칙**을 따릅니다. `LoggingConfigurator`, `TracingConfigurator`, `MetricsConfigurator`처럼 모든 Signal에 동명사 패턴을 일관되게 적용하는 것이 코드베이스의 가독성과 유지보수성에 더 기여한다고 판단했습니다.

### Q2. Logger 메서드에서 단수/복수 구분은 어떻게 하나요?

단일 항목 처리 시 단수, 다중 항목 처리 시 복수를 사용합니다. 예: `LogDomainEventPublisherRequest` (단일 이벤트 발행), `LogDomainEventsPublisherRequest` (Aggregate의 모든 이벤트 발행).

### Q3. Endpoint와 Protocol 속성명에 동명사를 사용하는 이유는?

설정/구성 활동을 나타내므로 동명사 형태가 자연스럽습니다. `LoggingEndpoint`는 "로깅을 위한 엔드포인트", `TracingProtocol`은 "추적을 위한 프로토콜"로 읽힙니다. 세 Signal 모두 동일한 패턴(`LoggingEndpoint`, `TracingEndpoint`, `MetricsEndpoint`)으로 일관성을 유지합니다.

### Q4. 폴더는 왜 복수형을 사용하나요?

폴더는 "무엇들을 담고 있는가"를 표현합니다. `Loggers/`는 Logger 관련 클래스들, `Spans/`는 Span 관련 클래스들을 담고 있습니다. 이는 .NET 프로젝트의 일반적인 관례와도 일치합니다.

### Q5. 새 LoggerExtensions 클래스를 추가할 때 어떤 순서로 작업하나요?

1) Context 이름 결정 (예: `Repository`, `ExternalApi`), 2) 필요한 Phase 결정 (`Request`, `Response`), 3) 필요한 Status 결정 (`Success`, `Warning`, `Error`), 4) `Log{Context}{Phase}{Status}` 패턴으로 메서드 이름 작성합니다.

## 참고 문서

- [18-observability-spec.md](./18-observability-spec.md) — Observability 사양 (Field/Tag, Meter, 메시지 템플릿)
- [20-observability-logging.md](./20-observability-logging.md) — Observability 로깅 상세
- [21-observability-metrics.md](./21-observability-metrics.md) — Observability 메트릭 상세
- [22-observability-tracing.md](./22-observability-tracing.md) — Observability 트레이싱 상세
- [OpenTelemetry Specification](https://opentelemetry.io/docs/specs/)
- [OpenTelemetry Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/)
- [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet)
