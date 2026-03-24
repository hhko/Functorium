---
title: "파이프라인 사양"
---

Functorium의 Usecase Pipeline 시스템은 횡단 관심사(cross-cutting concerns)를 Mediator `IPipelineBehavior<TRequest, TResponse>` 체인으로 분리합니다. 이 문서는 8개 기본 Pipeline 동작, 커스텀 확장 포인트, `PipelineConfigurator` API, OpenTelemetry 설정 타입을 정의합니다.

## 요약

### 주요 타입

| 타입 | 네임스페이스 | 설명 |
|------|-------------|------|
| `UsecasePipelineBase<TRequest>` | `Functorium.Adapters.Observabilities.Pipelines` | 모든 Pipeline의 공통 베이스 클래스 |
| `UsecaseMetricsPipeline<TRequest, TResponse>` | 동일 | 메트릭 자동 수집 Pipeline |
| `UsecaseTracingPipeline<TRequest, TResponse>` | 동일 | 분산 추적 Pipeline |
| `UsecaseLoggingPipeline<TRequest, TResponse>` | 동일 | 구조화 로깅 Pipeline |
| `UsecaseValidationPipeline<TRequest, TResponse>` | 동일 | FluentValidation 검증 Pipeline |
| `UsecaseCachingPipeline<TRequest, TResponse>` | 동일 | Query 캐싱 Pipeline |
| `UsecaseExceptionPipeline<TRequest, TResponse>` | 동일 | 예외 → `FinResponse.Fail` 변환 Pipeline |
| `UsecaseTransactionPipeline<TRequest, TResponse>` | 동일 | 트랜잭션 + UoW + 도메인 이벤트 Pipeline |
| `ICustomUsecasePipeline` | 동일 | 커스텀 Pipeline 마커 인터페이스 |
| `UsecaseMetricCustomPipelineBase<TRequest>` | 동일 | 커스텀 메트릭 Pipeline 베이스 |
| `UsecaseTracingCustomPipelineBase<TRequest>` | 동일 | 커스텀 트레이싱 Pipeline 베이스 |
| `PipelineConfigurator` | `Functorium.Adapters.Observabilities.Builders.Configurators` | Pipeline 활성화/비활성화 Fluent API |
| `OpenTelemetryBuilder` | `Functorium.Adapters.Observabilities.Builders` | OpenTelemetry 설정 메인 Builder |
| `LoggingConfigurator` | 동일(Configurators) | Serilog 확장 설정 |
| `MetricsConfigurator` | 동일(Configurators) | Metrics 확장 설정 |
| `TracingConfigurator` | 동일(Configurators) | Tracing 확장 설정 |
| `OpenTelemetryOptions` | `Functorium.Adapters.Observabilities` | OTLP 엔드포인트/프로토콜 설정 |
| `ObservabilityNaming` | `Functorium.Adapters.Observabilities.Naming` | 관측 가능성 네이밍 상수 |

---

## Pipeline 실행 순서

Pipeline은 DI 등록 순서에 따라 바깥(Request 쪽)에서 안쪽(Handler 쪽)으로 실행됩니다. Command와 Query는 적용되는 Pipeline이 다릅니다.

**Command 실행 순서:**

```
Request → Metrics → Tracing → Logging → Validation → Exception → Transaction → Custom → Handler
```

**Query 실행 순서:**

```
Request → Metrics → Tracing → Logging → Validation → Caching → Exception → Custom → Handler
```

| 순서 | Pipeline | Command | Query | 설명 |
|------|----------|---------|-------|------|
| 1 | Metrics | O | O | 요청/응답 수, 처리 시간 수집 |
| 2 | Tracing | O | O | Activity Span 생성 및 태그 기록 |
| 3 | Logging | O | O | 요청/응답 구조화 로깅 |
| 4 | Validation | O | O | FluentValidation 검증 |
| 5 | Caching | - | O | `ICacheable` 구현 시 IMemoryCache 캐싱 |
| 6 | Exception | O | O | 예외 → `FinResponse.Fail` 변환 |
| 7 | Transaction | O | - | UoW.SaveChanges + 도메인 이벤트 발행 |
| 8 | Custom | O | O | 사용자 정의 Pipeline |
| 9 | Handler | O | O | 실제 Usecase Handler |

- **Transaction Pipeline은** `where TRequest : ICommand<TResponse>` 제약으로 Command에만 적용됩니다.
- **Caching Pipeline은** `where TRequest : IQuery<TResponse>` 제약으로 Query에만 적용됩니다.

---

## UsecasePipelineBase\<TRequest\>

모든 Pipeline이 상속하는 추상 베이스 클래스입니다. 요청 타입 분석, 핸들러 이름 추출, 에러 정보 추출 등 공통 유틸리티를 제공합니다.

```csharp
public abstract partial class UsecasePipelineBase<TRequest>
```

### 정적 메서드

| 메서드 | 반환 타입 | 설명 |
|--------|----------|------|
| `GetRequestCategoryType<T>(T request)` | `string` | 요청 인스턴스에서 CQRS 타입 식별 (`command`, `query`, `unknown`) |
| `GetRequestCategoryType(Type requestType)` | `string` | 요청 Type에서 CQRS 타입 식별 |
| `GetRequestHandlerPath()` | `string` | `TRequest`의 `FullName` 반환 (네임스페이스 포함 전체 경로) |
| `GetRequestHandler()` | `string` | `TRequest`의 `FullName`에서 핸들러 클래스 이름 추출 |
| `GetRequestHandlerLower()` | `string` | `GetRequestHandler()`의 소문자 변환 (메트릭 네이밍용) |
| `GetErrorInfo(Error error)` | `(string ErrorType, string ErrorCode)` | 에러에서 타입/코드 정보 추출 |

- `GetRequestCategoryType`은 `ICommandRequest<>` / `IQueryRequest<>` 인터페이스 구현 여부로 판별합니다.
- `GetRequestHandler()`는 `typeof(TRequest).FullName`에서 중첩 타입(`+`)과 네임스페이스(`.`)를 파싱하여 클래스 이름만 추출합니다.

---

## 개별 Pipeline 동작

### UsecaseExceptionPipeline

예외를 `FinResponse.Fail`로 변환하여 예외가 Pipeline 바깥으로 전파되지 않도록 합니다.

```csharp
internal sealed class UsecaseExceptionPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>, IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
    where TResponse : IFinResponseFactory<TResponse>
```

| 항목 | 설명 |
|------|------|
| 제약 조건 | `TResponse : IFinResponseFactory<TResponse>` |
| 동작 | `try-catch`로 예외 포착 후 `TResponse.CreateFail(AdapterError.FromException(...))` 반환 |
| 에러 타입 | `AdapterErrorType.PipelineException` |

### UsecaseValidationPipeline

FluentValidation `IValidator<TRequest>`를 실행하여 검증 실패 시 `FinResponse.Fail`을 반환합니다.

```csharp
internal sealed class UsecaseValidationPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>, IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
    where TResponse : IFinResponseFactory<TResponse>
```

| 항목 | 설명 |
|------|------|
| DI 의존성 | `IEnumerable<IValidator<TRequest>>` |
| 동작 | Validator가 없으면 `next()` 통과, 있으면 모든 Validator 실행 |
| 에러 타입 | `AdapterErrorType.PipelineValidation(PropertyName)` |
| 다중 에러 | 검증 실패가 2개 이상이면 `Error.Many(errors)` 반환 |

### UsecaseLoggingPipeline

요청/응답 정보를 구조화 로깅으로 기록합니다. `IUsecaseCtxEnricher`가 DI에 등록되어 있으면 커스텀 속성을 자동 Push합니다.

```csharp
internal sealed class UsecaseLoggingPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>, IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
    where TResponse : IFinResponse, IFinResponseFactory<TResponse>
```

| 항목 | 설명 |
|------|------|
| DI 의존성 | `ILogger<UsecaseLoggingPipeline<TRequest, TResponse>>`, `IUsecaseCtxEnricher<TRequest, TResponse>?` (선택) |
| 요청 로그 | Information 레벨, `{Layer} {Category} {CategoryType} {Handler} {Method} requesting` |
| 응답 로그 (성공) | Information 레벨, `responded success in {Elapsed:0.0000} ms` |
| 응답 로그 (Expected 에러) | Warning 레벨, `responded failure ... with {@Error}` |
| 응답 로그 (Exceptional 에러) | Error 레벨, `responded failure ... with {@Error}` |

### UsecaseTracingPipeline

OpenTelemetry `ActivitySource`를 사용하여 분산 추적 Span을 생성합니다.

```csharp
internal sealed class UsecaseTracingPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>, IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
    where TResponse : IFinResponse, IFinResponseFactory<TResponse>
```

| 항목 | 설명 |
|------|------|
| DI 의존성 | `ActivitySource` |
| Span 이름 | `{layer} {category}.{categoryType} {handler}.{method}` |
| ActivityKind | `Internal` |
| 요청 태그 | `request.layer`, `request.category.name`, `request.category.type`, `request.handler.name`, `request.handler.method` |
| 응답 태그 (성공) | `response.status = success`, `ActivityStatusCode.Ok` |
| 응답 태그 (실패) | `response.status = failure`, `error.type`, `error.code`, `ActivityStatusCode.Error` |
| 시간 태그 | `response.elapsed` (초 단위) |

### UsecaseMetricsPipeline

요청 수, 응답 수, 처리 시간을 OpenTelemetry Meter로 수집합니다.

```csharp
internal sealed class UsecaseMetricsPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>, IPipelineBehavior<TRequest, TResponse>, IDisposable
    where TRequest : IMessage
    where TResponse : IFinResponse, IFinResponseFactory<TResponse>
```

| 항목 | 설명 |
|------|------|
| DI 의존성 | `IOptions<OpenTelemetryOptions>`, `IMeterFactory` |
| Meter 이름 | `{ServiceNamespace}.application` |
| Counter (요청) | `application.usecase.{categoryType}.requests` (단위: `{request}`) |
| Counter (응답) | `application.usecase.{categoryType}.responses` (단위: `{response}`) |
| Histogram (시간) | `application.usecase.{categoryType}.duration` (단위: `s`) |
| 요청 태그 | `request.layer`, `request.category.name`, `request.category.type`, `request.handler.name`, `request.handler.method` |
| 응답 태그 (성공) | 요청 태그 + `response.status = success` |
| 응답 태그 (실패) | 요청 태그 + `response.status = failure` + `error.type` + `error.code` |

### UsecaseTransactionPipeline

Command Usecase에 대해 명시적 트랜잭션, `UoW.SaveChanges`, 도메인 이벤트 발행을 자동 처리합니다.

```csharp
internal sealed class UsecaseTransactionPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>, IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
    where TResponse : IFinResponse, IFinResponseFactory<TResponse>
```

| 항목 | 설명 |
|------|------|
| 제약 조건 | `TRequest : ICommand<TResponse>` (Command 전용) |
| DI 의존성 | `IUnitOfWork`, `IDomainEventPublisher`, `ILogger` |
| 실행 순서 | 1) 트랜잭션 시작 → 2) Handler 실행 → 3) 실패 시 롤백 → 4) `SaveChanges` → 5) 커밋 → 6) 도메인 이벤트 발행 |
| 실패 처리 | Handler 실패 또는 SaveChanges 실패 시 `TResponse.CreateFail(error)` 반환, 트랜잭션 자동 롤백 |

### UsecaseCachingPipeline

`ICacheable`을 구현한 Query 요청에 대해 `IMemoryCache` 기반 캐싱을 수행합니다.

```csharp
internal sealed class UsecaseCachingPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>, IPipelineBehavior<TRequest, TResponse>
    where TRequest : IQuery<TResponse>
    where TResponse : IFinResponse, IFinResponseFactory<TResponse>
```

| 항목 | 설명 |
|------|------|
| 제약 조건 | `TRequest : IQuery<TResponse>` (Query 전용) |
| DI 의존성 | `IMemoryCache` (`services.AddMemoryCache()` 필요) |
| 캐시 키 | `ICacheable.CacheKey` |
| 캐시 기간 | `ICacheable.Duration` (null이면 기본 5분) |
| 동작 | 캐시 히트 시 즉시 반환, 미스 시 Handler 실행 후 성공 응답만 캐시 저장 |

**ICacheable 인터페이스:**

```csharp
public interface ICacheable
{
    string CacheKey { get; }
    TimeSpan? Duration { get; }
}
```

### Custom Pipeline

사용자가 정의하는 커스텀 Pipeline입니다. 기본 Pipeline(Exception, Validation 등) 이후, Handler 직전에 실행됩니다. `ICustomUsecasePipeline` 마커 인터페이스를 구현하면 어셈블리 자동 스캔으로 등록할 수 있습니다.

---

## 커스텀 확장

### ICustomUsecasePipeline

Scrutor 자동 검색 등록을 위한 마커 인터페이스입니다.

```csharp
public interface ICustomUsecasePipeline { }
```

`AddCustomPipelinesFromAssembly(assembly)`를 사용하면 이 인터페이스를 구현한 모든 타입이 자동으로 DI에 등록됩니다.

### UsecaseMetricCustomPipelineBase\<TRequest\>

Usecase별 개별 Metric을 생성하기 위한 베이스 클래스입니다. `TRequest` 타입으로부터 CQRS 타입을 자동 식별합니다.

```csharp
public abstract class UsecaseMetricCustomPipelineBase<TRequest>
    : UsecasePipelineBase<TRequest>, ICustomUsecasePipeline
```

| 멤버 | 설명 |
|------|------|
| `protected readonly Meter _meter` | `{ServiceNamespace}.application` Meter 인스턴스 |
| `protected const string DurationUnit` | `"s"` |
| `protected const string CountUnit` | `"requests"` |
| `GetMetricName(string metricName)` | `application.usecase.{cqrs}.{handler}.{metricName}` 형식 반환 |
| `GetMetricNameWithoutHandler(string metricName)` | `application.usecase.{cqrs}.{metricName}` 형식 반환 |

**생성자:**

```csharp
protected UsecaseMetricCustomPipelineBase(string serviceNamespace, IMeterFactory meterFactory)
```

**RequestDuration 헬퍼:**

```csharp
public class RequestDuration : IDisposable
{
    public RequestDuration(Histogram<double> histogram);
    public void Dispose(); // 경과 시간을 histogram에 자동 기록
}
```

`using` 구문과 함께 사용하여 자동으로 시간 측정 및 Histogram 기록을 수행합니다.

### UsecaseTracingCustomPipelineBase\<TRequest\>

Usecase별 커스텀 Tracing을 생성하기 위한 베이스 클래스입니다. `ActivitySource`를 통해 커스텀 Activity(Span)를 생성하고 표준 태그를 설정합니다.

```csharp
public abstract class UsecaseTracingCustomPipelineBase<TRequest>
    : UsecasePipelineBase<TRequest>, ICustomUsecasePipeline
```

| 멤버 | 설명 |
|------|------|
| `protected readonly ActivitySource _activitySource` | Activity 생성에 사용하는 ActivitySource |
| `StartCustomActivity(string operationName, ActivityKind kind)` | `{prefix}.{operationName}` 형식의 커스텀 Activity 생성 |
| `GetActivityName(string operationName)` | Activity 이름 조회 (`{prefix}.{operationName}`) |
| `SetStandardRequestTags(Activity activity, string method)` | 표준 요청 태그 5종 설정 |

**생성자:**

```csharp
protected UsecaseTracingCustomPipelineBase(ActivitySource activitySource)
```

- Activity 이름 접두사: `{layer} {category}.{categoryType} {handler}`
- 부모 `Activity.Current`가 존재하면 자식 Span으로 생성됩니다.

---

## PipelineConfigurator API

`PipelineConfigurator`는 Fluent API로 개별 Pipeline을 활성화/비활성화하고 커스텀 Pipeline을 추가합니다.

```csharp
public class PipelineConfigurator
```

### 활성화 메서드

| 메서드 | 반환 타입 | 설명 |
|--------|----------|------|
| `UseAll()` | `PipelineConfigurator` | 모든 기본 Pipeline 활성화 (Metrics, Tracing, Logging, Validation, Exception, Transaction) |
| `UseMetrics()` | `PipelineConfigurator` | Metrics Pipeline 활성화 |
| `UseTracing()` | `PipelineConfigurator` | Tracing Pipeline 활성화 |
| `UseLogging()` | `PipelineConfigurator` | Logging Pipeline 활성화 |
| `UseValidation()` | `PipelineConfigurator` | Validation Pipeline 활성화 |
| `UseCaching()` | `PipelineConfigurator` | Caching Pipeline 활성화 (`IMemoryCache` DI 등록 필요) |
| `UseException()` | `PipelineConfigurator` | Exception Pipeline 활성화 |
| `UseTransaction()` | `PipelineConfigurator` | Transaction Pipeline 활성화 (`IUnitOfWork`, `IDomainEventPublisher`, `IDomainEventCollector` DI 등록 필요) |

### 설정 메서드

| 메서드 | 반환 타입 | 설명 |
|--------|----------|------|
| `WithLifetime(ServiceLifetime lifetime)` | `PipelineConfigurator` | Pipeline 서비스 Lifetime 설정 (기본값: `Scoped`) |
| `AddCustomPipeline<TPipeline>()` | `PipelineConfigurator` | 커스텀 Pipeline을 타입으로 개별 등록 |
| `AddCustomPipelinesFromAssembly(Assembly assembly)` | `PipelineConfigurator` | 어셈블리에서 `ICustomUsecasePipeline` 구현체 자동 검색 등록 |

### 사용 예시

```csharp
// 기본값: 모든 Pipeline 활성화
services
    .RegisterOpenTelemetry(configuration, Assembly.GetExecutingAssembly())
    .ConfigurePipelines()  // UseAll() 자동 호출
    .Build();

// 선택적 활성화
services
    .RegisterOpenTelemetry(configuration, Assembly.GetExecutingAssembly())
    .ConfigurePipelines(pipelines => pipelines
        .UseMetrics()
        .UseTracing()
        .UseLogging()
        .UseValidation()
        .UseException()
        .UseTransaction()
        .UseCaching()
        .AddCustomPipelinesFromAssembly(Assembly.GetExecutingAssembly())
        .WithLifetime(ServiceLifetime.Scoped))
    .Build();
```

### 등록 순서

`Apply()` 내부에서 Pipeline은 다음 순서로 `IPipelineBehavior<,>`에 등록됩니다.

1. Metrics
2. Tracing
3. Logging
4. Validation
5. Caching
6. Exception
7. Transaction
8. Custom (개별 등록 → 어셈블리 자동 검색 순)
9. Handler

**Transaction Pipeline 자동 감지:** `UseTransaction()`을 호출해도 `IUnitOfWork`, `IDomainEventPublisher`, `IDomainEventCollector`가 DI에 모두 등록되어 있지 않으면 등록을 건너뜁니다.

---

## OpenTelemetry 설정

### OpenTelemetryBuilder

OpenTelemetry 설정을 위한 메인 Builder 클래스입니다. Serilog, Metrics, Tracing, Pipeline 설정을 체이닝으로 구성합니다.

```csharp
public partial class OpenTelemetryBuilder
```

#### 진입점

```csharp
// IServiceCollection 확장 메서드
public static OpenTelemetryBuilder RegisterOpenTelemetry(
    this IServiceCollection services,
    IConfiguration configuration,
    Assembly projectAssembly)
```

#### Configure 메서드

| 메서드 | 반환 타입 | 설명 |
|--------|----------|------|
| `ConfigureLogging(Action<LoggingConfigurator> configure)` | `OpenTelemetryBuilder` | Serilog 확장 설정 |
| `ConfigureMetrics(Action<MetricsConfigurator> configure)` | `OpenTelemetryBuilder` | OpenTelemetry Metrics 확장 설정 |
| `ConfigureTracing(Action<TracingConfigurator> configure)` | `OpenTelemetryBuilder` | OpenTelemetry Tracing 확장 설정 |
| `ConfigurePipelines()` | `OpenTelemetryBuilder` | 모든 기본 Pipeline 활성화 |
| `ConfigurePipelines(Action<PipelineConfigurator> configure)` | `OpenTelemetryBuilder` | 커스텀 Pipeline 설정 |
| `ConfigureStartupLogger(Action<ILogger> configure)` | `OpenTelemetryBuilder` | 시작 시 추가 로깅 설정 |
| `WithAdapterObservability(bool enable = true)` | `OpenTelemetryBuilder` | Adapter 관측 가능성 활성화/비활성화 (기본값: `true`) |
| `Build()` | `IServiceCollection` | 모든 설정 적용 후 IServiceCollection 반환 |

#### Build() 내부 처리 순서

1. `OpenTelemetryOptions` 읽기 (`IOptions<OpenTelemetryOptions>`)
2. Resource Attributes 생성
3. Serilog 설정 (ReadFrom.Configuration + WriteTo.OpenTelemetry + ErrorsDestructuringPolicy)
4. CtxEnricherContext PushProperty 팩토리 설정
5. OpenTelemetry 설정 (Metrics + Tracing + OTLP Exporter)
6. Adapter Observability 등록 (`ActivitySource`, `IMeterFactory`)
7. Usecase Pipeline 등록
8. StartupLogger `IHostedService` 등록

### LoggingConfigurator

Serilog 확장 설정을 위한 Builder 클래스입니다.

```csharp
public class LoggingConfigurator
```

| 멤버 | 설명 |
|------|------|
| `Options` | `OpenTelemetryOptions` 접근 프로퍼티 |
| `AddDestructuringPolicy<TPolicy>()` | `IDestructuringPolicy` 구현 타입 등록 |
| `AddEnricher(ILogEventEnricher enricher)` | Enricher 인스턴스 등록 |
| `AddEnricher<TEnricher>()` | Enricher 타입 등록 |
| `Configure(Action<LoggerConfiguration> configure)` | `LoggerConfiguration` 직접 접근 |

### MetricsConfigurator

OpenTelemetry Metrics 확장 설정을 위한 Builder 클래스입니다.

```csharp
public class MetricsConfigurator
```

| 멤버 | 설명 |
|------|------|
| `Options` | `OpenTelemetryOptions` 접근 프로퍼티 |
| `AddMeter(string meterName)` | 추가 Meter 등록 (와일드카드 지원: `"MyApp.*"`) |
| `AddInstrumentation(Action<MeterProviderBuilder> configure)` | 추가 Instrumentation 등록 |
| `Configure(Action<MeterProviderBuilder> configure)` | `MeterProviderBuilder` 직접 접근 |

### TracingConfigurator

OpenTelemetry Tracing 확장 설정을 위한 Builder 클래스입니다.

```csharp
public class TracingConfigurator
```

| 멤버 | 설명 |
|------|------|
| `Options` | `OpenTelemetryOptions` 접근 프로퍼티 |
| `AddSource(string sourceName)` | 추가 ActivitySource 등록 (와일드카드 지원: `"MyApp.*"`) |
| `AddProcessor(BaseProcessor<Activity> processor)` | 추가 Processor 등록 |
| `Configure(Action<TracerProviderBuilder> configure)` | `TracerProviderBuilder` 직접 접근 |

---

## OpenTelemetryOptions

`appsettings.json`의 `"OpenTelemetry"` 섹션에 바인딩되는 설정 클래스입니다.

```csharp
public sealed class OpenTelemetryOptions : IStartupOptionsLogger, IOpenTelemetryOptions
```

### 프로퍼티

| 프로퍼티 | 타입 | 기본값 | 설명 |
|---------|------|--------|------|
| `ServiceNamespace` | `string` | `""` | 서비스 네임스페이스(그룹) |
| `ServiceName` | `string` | `""` | 서비스 이름 |
| `ServiceVersion` | `string` | (어셈블리 버전) | 서비스 버전 (자동 설정) |
| `ServiceInstanceId` | `string` | (호스트네임) | 서비스 인스턴스 ID (자동 설정) |
| `CollectorEndpoint` | `string` | `""` | 통합 OTLP Collector 엔드포인트 |
| `TracingEndpoint` | `string?` | `null` | Tracing 전용 엔드포인트 (null이면 `CollectorEndpoint` 사용) |
| `MetricsEndpoint` | `string?` | `null` | Metrics 전용 엔드포인트 (null이면 `CollectorEndpoint` 사용) |
| `LoggingEndpoint` | `string?` | `null` | Logging 전용 엔드포인트 (null이면 `CollectorEndpoint` 사용) |
| `CollectorProtocol` | `string` | `"Grpc"` | 통합 OTLP Protocol |
| `TracingProtocol` | `string?` | `null` | Tracing 전용 Protocol |
| `MetricsProtocol` | `string?` | `null` | Metrics 전용 Protocol |
| `LoggingProtocol` | `string?` | `null` | Logging 전용 Protocol |
| `SamplingRate` | `double` | `1.0` | Tracing 샘플링 비율 (0.0 ~ 1.0) |
| `EnablePrometheusExporter` | `bool` | `false` | Prometheus Exporter 활성화 |

### 엔드포인트 해석 규칙

개별 엔드포인트(`TracingEndpoint`, `MetricsEndpoint`, `LoggingEndpoint`)의 해석 규칙은 다음과 같습니다.

| 값 | 동작 |
|----|------|
| `null` | `CollectorEndpoint` 사용 (기본 동작) |
| `""` (빈 문자열) | 해당 신호 비활성화 |
| `"http://..."` | 해당 엔드포인트 사용 |

### Protocol 메서드

| 메서드 | 반환 타입 | 설명 |
|--------|----------|------|
| `GetTracingProtocol()` | `OtlpCollectorProtocol` | Tracing Protocol (개별 설정 우선) |
| `GetMetricsProtocol()` | `OtlpCollectorProtocol` | Metrics Protocol (개별 설정 우선) |
| `GetLoggingProtocol()` | `OtlpCollectorProtocol` | Logging Protocol (개별 설정 우선) |
| `GetTracingEndpoint()` | `string` | Tracing 엔드포인트 (해석 규칙 적용) |
| `GetMetricsEndpoint()` | `string` | Metrics 엔드포인트 (해석 규칙 적용) |
| `GetLoggingEndpoint()` | `string` | Logging 엔드포인트 (해석 규칙 적용) |

### OtlpCollectorProtocol (SmartEnum)

```csharp
public sealed class OtlpCollectorProtocol : SmartEnum<OtlpCollectorProtocol>
```

| 상수 | 값 | 설명 |
|------|----|------|
| `Grpc` | 1 | gRPC 프로토콜 (기본값) |
| `HttpProtobuf` | 2 | HTTP/Protobuf 프로토콜 |

### Validator

`FluentValidation` 기반 옵션 검증기입니다.

| 규칙 | 설명 |
|------|------|
| `ServiceNamespace` | 필수 (NotEmpty) |
| `ServiceName` | 필수 (NotEmpty) |
| 엔드포인트 | `CollectorEndpoint` 또는 개별 엔드포인트 중 하나 이상 필수 |
| `SamplingRate` | 0.0 ~ 1.0 범위 |
| Protocol | SmartEnum 유효값 검증 |

### appsettings.json 예시

```json
{
  "OpenTelemetry": {
    "ServiceNamespace": "mycompany.production",
    "ServiceName": "orderservice",
    "CollectorEndpoint": "http://localhost:4317",
    "CollectorProtocol": "Grpc",
    "SamplingRate": 1.0,
    "EnablePrometheusExporter": false
  }
}
```

---

## ObservabilityNaming 상수

관측 가능성 관련 통합 네이밍 규칙을 정의합니다. 메트릭 이름, 태그 키, Span 이름 등의 단일 진실 공급원(Single Source of Truth)입니다.

```csharp
public static partial class ObservabilityNaming
```

### Layers

| 상수 | 값 | 설명 |
|------|----|------|
| `Application` | `"application"` | Application Layer |
| `Adapter` | `"adapter"` | Adapter Layer |

### Categories

| 상수 | 값 | 설명 |
|------|----|------|
| `Usecase` | `"usecase"` | Usecase 카테고리 |
| `Repository` | `"repository"` | Repository 카테고리 |
| `Event` | `"event"` | Event 카테고리 |
| `Unknown` | `"unknown"` | 미식별 카테고리 |

### CategoryTypes

| 상수 | 값 | 설명 |
|------|----|------|
| `Command` | `"command"` | Command 타입 |
| `Query` | `"query"` | Query 타입 |
| `Event` | `"event"` | Event 타입 |
| `Unknown` | `"unknown"` | 미식별 타입 |

### Status

| 상수 | 값 | 설명 |
|------|----|------|
| `Success` | `"success"` | 성공 |
| `Failure` | `"failure"` | 실패 |

### ErrorTypes

| 상수 | 값 | 설명 |
|------|----|------|
| `Expected` | `"expected"` | 예상된 비즈니스 에러 (`IsExpected = true`) |
| `Exceptional` | `"exceptional"` | 예외적 시스템 에러 (`IsExceptional = true`) |
| `Aggregate` | `"aggregate"` | 복합 에러 (`ManyErrors`) |

### Methods

| 상수 | 값 | 설명 |
|------|----|------|
| `Handle` | `"Handle"` | Usecase Handler 메서드 |
| `Publish` | `"Publish"` | 이벤트 발행 메서드 |
| `PublishTrackedEvents` | `"PublishTrackedEvents"` | 추적 이벤트 발행 메서드 |

### OTelAttributes (OpenTelemetry 표준)

| 상수 | 값 |
|------|----|
| `ErrorType` | `"error.type"` |
| `ServiceNamespace` | `"service.namespace"` |
| `ServiceName` | `"service.name"` |
| `ServiceVersion` | `"service.version"` |
| `ServiceInstanceId` | `"service.instance.id"` |
| `DeploymentEnvironment` | `"deployment.environment"` |

### CustomAttributes (3-Pillar 공통)

| 상수 | 값 | 용도 |
|------|----|------|
| `RequestMessage` | `"request.message"` | 요청 메시지 |
| `RequestParams` | `"request.params"` | 요청 파라미터 |
| `RequestLayer` | `"request.layer"` | 요청 레이어 |
| `RequestCategoryName` | `"request.category.name"` | 요청 카테고리 이름 |
| `RequestCategoryType` | `"request.category.type"` | 요청 카테고리 타입 |
| `RequestHandlerName` | `"request.handler.name"` | 요청 핸들러 이름 |
| `RequestHandlerMethod` | `"request.handler.method"` | 요청 핸들러 메서드 |
| `ResponseMessage` | `"response.message"` | 응답 메시지 |
| `ResponseStatus` | `"response.status"` | 응답 상태 |
| `ResponseElapsed` | `"response.elapsed"` | 응답 경과 시간 |
| `ErrorCode` | `"error.code"` | 에러 코드 |

### Metrics 이름 생성

| 메서드 | 예시 |
|--------|------|
| `Metrics.UsecaseRequest("command")` | `"application.usecase.command.requests"` |
| `Metrics.UsecaseResponse("query")` | `"application.usecase.query.responses"` |
| `Metrics.UsecaseDuration("command")` | `"application.usecase.command.duration"` |
| `Metrics.AdapterRequest("repository")` | `"adapter.repository.requests"` |
| `Metrics.AdapterResponse("repository")` | `"adapter.repository.responses"` |
| `Metrics.AdapterDuration("repository")` | `"adapter.repository.duration"` |

### Spans 이름 생성

| 메서드 | 예시 |
|--------|------|
| `Spans.OperationName("adapter", "repository", "OrderRepository", "GetById")` | `"adapter repository OrderRepository.GetById"` |

### EventIds

| 범위 | ID | 이름 |
|------|----|------|
| Application Request | 1001 | `application.request` |
| Application Response (Success) | 1002 | `application.response.success` |
| Application Response (Warning) | 1003 | `application.response.warning` |
| Application Response (Error) | 1004 | `application.response.error` |
| Adapter Request | 2001 | `adapter.request` |
| Adapter Response (Success) | 2002 | `adapter.response.success` |
| Adapter Response (Warning) | 2003 | `adapter.response.warning` |
| Adapter Response (Error) | 2004 | `adapter.response.error` |

---

## 관련 문서

- **가이드:** [Adapter 연결 -- Pipeline과 DI](../guides/adapter/14a-adapter-pipeline-di) -- Pipeline DI 등록 실습
- **가이드:** [Adapter 테스트](../guides/adapter/14b-adapter-testing) -- Pipeline 단위 테스트
- **가이드:** [Observability 로깅](../guides/observability/19-observability-logging) -- 로깅 Pipeline 상세
- **가이드:** [Observability 메트릭](../guides/observability/20-observability-metrics) -- 메트릭 Pipeline 상세
- **가이드:** [Observability 트레이싱](../guides/observability/21-observability-tracing) -- 트레이싱 Pipeline 상세
- **사양:** [관측 가능성 사양](./08-observability) -- Field/Tag 사양, Meter 정의, 메시지 템플릿
- **사양:** [유스케이스 CQRS](./05-usecase-cqrs) -- `FinResponse<T>`, `ICommandRequest`, `IQueryRequest`
