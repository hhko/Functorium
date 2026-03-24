---
title: "Observability 네이밍 가이드"
---

이 문서는 Functorium 프로젝트의 Observability 관련 코드 작성 시 따라야 할 명명 규칙을 정의합니다.

## 들어가며

- "관측 가능성 코드의 네이밍이 일관되지 않으면 검색과 필터링이 어려워지는데, Signal과 Component를 어떻게 구분하는가?"
- "Logger 메서드 이름을 팀 전체가 동일한 패턴으로 작성하려면 어떤 규칙이 필요한가?"
- "Configurator, Pipeline, Options 등 클래스 유형마다 네이밍 패턴이 다른 이유는 무엇인가?"

OpenTelemetry 표준 용어를 기반으로 하되 .NET 생태계와 실용성을 고려한 네이밍 규칙을 수립하면, 코드베이스의 가독성과 검색 효율성을 동시에 확보할 수 있습니다.

### 이 문서에서 배우는 내용

이 문서를 통해 다음을 학습합니다:

1. **코드 네이밍 규칙** - Signal 접두사와 Component 접미사의 역할 구분
2. **Logger 메서드 네이밍** - `Log{Context}{Phase}{Status}` 패턴과 적용 예시
3. **일관된 계측 식별자** - Configurator, Pipeline, Options, Extensions 등 클래스 유형별 명명 패턴

> **핵심 원칙:** Signal 이름(`Logging`, `Tracing`, `Metrics`)은 설정/활동 대상에 접두사로, Component 유형(`Logger`, `Span`, `Metric`)은 구체적 객체에 접미사로 사용합니다. 내부 일관성이 외부 표준 준수보다 우선합니다.

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
3. Logger 메서드는 `Log{Context}{Phase}{Status}` 패턴 사용

### 주요 개념

| 개념 | 규칙 | 예시 |
|------|------|------|
| Signal 접두사 | 설정/활동 대상 | `LoggingConfigurator`, `TracingEndpoint` |
| Component 접미사 | 구체적 객체 | `StartupLogger`, `ISpanFactory` |
| Logger 메서드 | `Log{Context}{Phase}{Status}` | `LogDomainEventHandlerResponseError` |
| 내부 일관성 우선 | OpenTelemetry "Traces" 대신 `Tracing` 사용 | `TracingConfigurator` (not `TracesConfigurator`) |

요약에서 Signal/Component 구분과 Logger 메서드 패턴의 핵심 규칙을 확인했습니다. 이제 각 규칙을 코드 수준에서 상세히 살펴봅니다.

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
- `LoggingEndpoint` + `LoggingProtocol` (동명사로 통일)
- `TracingEndpoint` + `TracingProtocol` (동명사로 통일)
- `MetricsEndpoint` + `MetricsProtocol` (동명사로 통일)

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

Src/Functorium.Adapters/Observabilities/
├── Loggers/          ✅
├── Metrics/          ✅
├── Spans/            ✅
└── Context/          ✅
```

> **참고**: 위 구조는 설계 의도를 나타내며, 현재 코드베이스의 실제 구조와 차이가 있습니다. 현재 `Applications/Observabilities/` 디렉토리는 존재하지 않고, `Adapters/Observabilities/`의 실제 하위 폴더 구조는 다음과 같습니다:
> ```
> Src/Functorium.Adapters/Observabilities/
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
│   ├── PipelineConfigurator.cs          // Pipeline 선택적 등록 (UseAll, UseMetrics 등)
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
├── UsecasePipelineBase.cs                 // Pipeline 공통 베이스 클래스
├── ICustomUsecasePipeline.cs              // 커스텀 파이프라인 마커 인터페이스
├── CtxEnricherPipeline.cs                 // ctx.* 3-Pillar Enrichment Pipeline (최선두)
├── UsecaseLoggingPipeline.cs              // Logging Pipeline
├── UsecaseTracingPipeline.cs              // Tracing Pipeline
├── UsecaseTracingCustomPipelineBase.cs    // 커스텀 Tracing Pipeline 베이스
├── UsecaseMetricsPipeline.cs              // Metrics Pipeline
├── UsecaseMetricCustomPipelineBase.cs     // 커스텀 Metrics Pipeline 베이스
├── UsecaseValidationPipeline.cs           // FluentValidation Pipeline
├── UsecaseExceptionPipeline.cs            // Exception → Fin.Fail 변환 Pipeline
├── UsecaseTransactionPipeline.cs          // Transaction + SaveChanges + 이벤트 발행 Pipeline
└── UsecaseCachingPipeline.cs              // IMemoryCache 기반 캐싱 Pipeline
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
        public const string RequestCategoryName = "request.category.name";
        public const string RequestCategoryType = "request.category.type";
        public const string RequestHandlerName = "request.handler.name";
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

> 필드/태그 네이밍 규칙은 [08-observability.md](../../spec/08-observability)를 참조하세요.

코드 네이밍 규칙에서 클래스, 인터페이스, 파이프라인 등의 명명 패턴을 정의했습니다. 이어서, LoggerExtensions 메서드에 적용되는 별도의 네이밍 패턴을 살펴봅니다.

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

네이밍 규칙을 적용하면서 자주 발생하는 문제와 해결 방법을 정리합니다.

## 트러블슈팅

### Signal 이름과 Component 유형을 혼동하여 클래스 이름이 일관되지 않은 경우

**원인:** `Logging`(Signal/활동)과 `Logger`(Component/객체)의 역할 구분이 명확하지 않아 `LoggingPipeline`과 `LoggerPipeline`이 혼재됩니다.

**해결:** 설정/활동 클래스에는 Signal 이름을 접두사로(`LoggingConfigurator`, `UsecaseLoggingPipeline`), 구성 요소에는 Component 유형을 접미사로(`StartupLogger`, `ISpanFactory`) 사용합니다.

### count 필드에서 `.count`와 `_count`를 혼용한 경우

**원인:** 단독 count와 형용사 조합 count의 규칙을 구분하지 않았습니다.

**해결:** 엔티티 수준 count는 `.count`(`request.event.count`, `request.aggregate.count`), 동적 필드 count는 `_count` 접미사(`request.params.{name}_count`, `response.event.success_count`, `response.event.failure_count`)를 사용합니다.

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

- [08-observability.md](../../spec/08-observability) — Observability 사양 (Field/Tag, Meter, 메시지 템플릿)
- [19-observability-logging.md](./19-observability-logging) — Observability 로깅 상세
- [20-observability-metrics.md](./20-observability-metrics) — Observability 메트릭 상세
- [21-observability-tracing.md](./21-observability-tracing) — Observability 트레이싱 상세
- [OpenTelemetry Specification](https://opentelemetry.io/docs/specs/)
- [OpenTelemetry Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/)
- [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet)
