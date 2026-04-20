---
title: "Pipeline Specification"
---

Functorium's Usecase Pipeline system separates cross-cutting concerns into a Mediator `IPipelineBehavior<TRequest, TResponse>` chain. This document defines the 8 default Pipeline behaviors, custom extension points, the `PipelineConfigurator` API, and OpenTelemetry configuration types.

## Summary

### Key Types

| Type | Namespace | Description |
|------|-------------|------|
| `UsecasePipelineBase<TRequest>` | `Functorium.Adapters.Pipelines` | Common base class for all Pipelines |
| `UsecaseMetricsPipeline<TRequest, TResponse>` | Same | Automatic metrics collection Pipeline |
| `UsecaseTracingPipeline<TRequest, TResponse>` | Same | Distributed tracing Pipeline |
| `UsecaseLoggingPipeline<TRequest, TResponse>` | Same | Structured logging Pipeline |
| `UsecaseValidationPipeline<TRequest, TResponse>` | Same | FluentValidation validation Pipeline |
| `UsecaseCachingPipeline<TRequest, TResponse>` | Same | Query caching Pipeline |
| `UsecaseExceptionPipeline<TRequest, TResponse>` | Same | Exception to `FinResponse.Fail` conversion Pipeline |
| `UsecaseTransactionPipeline<TRequest, TResponse>` | Same | Transaction + UoW + domain event Pipeline |
| `ICustomUsecasePipeline` | Same | Custom Pipeline marker interface |
| `UsecaseMetricCustomPipelineBase<TRequest>` | Same | Custom metrics Pipeline base |
| `UsecaseTracingCustomPipelineBase<TRequest>` | Same | Custom tracing Pipeline base |
| `PipelineConfigurator` | `Functorium.Adapters.Observabilities.Builders.Configurators` | Pipeline enable/disable Fluent API |
| `OpenTelemetryBuilder` | `Functorium.Adapters.Observabilities.Builders` | OpenTelemetry configuration main Builder |
| `LoggingConfigurator` | Same (Configurators) | Serilog extension configuration |
| `MetricsConfigurator` | Same (Configurators) | Metrics extension configuration |
| `TracingConfigurator` | Same (Configurators) | Tracing extension configuration |
| `OpenTelemetryOptions` | `Functorium.Adapters.Observabilities` | OTLP endpoint/protocol configuration |
| `ObservabilityNaming` | `Functorium.Adapters.Observabilities.Naming` | Observability naming constants |

---

## Pipeline Execution Order

Pipelines execute from outside (Request side) to inside (Handler side) according to DI registration order. Different Pipelines apply to Commands and Queries.

**Command execution order:**

```
Request → Metrics → Tracing → Logging → Validation → Exception → Transaction → Custom → Handler
```

**Query execution order:**

```
Request → Metrics → Tracing → Logging → Validation → Caching → Exception → Custom → Handler
```

| Order | Pipeline | Command | Query | Description |
|------|----------|---------|-------|------|
| 1 | Metrics | O | O | Collects request/response counts and processing time |
| 2 | Tracing | O | O | Creates Activity Span and records tags |
| 3 | Logging | O | O | Request/response structured logging |
| 4 | Validation | O | O | FluentValidation validation |
| 5 | Caching | - | O | IMemoryCache caching when `ICacheable` is implemented |
| 6 | Exception | O | O | Exception to `FinResponse.Fail` conversion |
| 7 | Transaction | O | - | UoW.SaveChanges + domain event publishing |
| 8 | Custom | O | O | User-defined Pipeline |
| 9 | Handler | O | O | Actual Usecase Handler |

- **Transaction Pipeline** applies only to Commands via the `where TRequest : ICommand<TResponse>` constraint.
- **Caching Pipeline** applies only to Queries via the `where TRequest : IQuery<TResponse>` constraint.

---

## UsecasePipelineBase\<TRequest\>

An abstract base class inherited by all Pipelines. Provides common utilities such as request type analysis, handler name extraction, and error information extraction.

```csharp
public abstract partial class UsecasePipelineBase<TRequest>
```

### Static Methods

| Method | Return Type | Description |
|--------|----------|------|
| `GetRequestCategoryType<T>(T request)` | `string` | Identifies CQRS type from request instance (`command`, `query`, `unknown`) |
| `GetRequestCategoryType(Type requestType)` | `string` | Identifies CQRS type from request Type |
| `GetRequestHandlerPath()` | `string` | Returns the `FullName` of `TRequest` (full path including namespace) |
| `GetRequestHandler()` | `string` | Extracts handler class name from the `FullName` of `TRequest` |
| `GetRequestHandlerLower()` | `string` | Lowercase conversion of `GetRequestHandler()` (for metric naming) |
| `GetErrorInfo(Error error)` | `(string ErrorType, string ErrorCode)` | Extracts type/code information from error |

- `GetRequestCategoryType` determines the type by checking whether `ICommandRequest<>` / `IQueryRequest<>` interfaces are implemented.
- `GetRequestHandler()` parses nested types (`+`) and namespaces (`.`) from `typeof(TRequest).FullName` to extract only the class name.

---

## Individual Pipeline Behaviors

### UsecaseExceptionPipeline

Converts exceptions to `FinResponse.Fail` to prevent exceptions from propagating outside the Pipeline.

```csharp
internal sealed class UsecaseExceptionPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>, IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
    where TResponse : IFinResponseFactory<TResponse>
```

| Item | Description |
|------|------|
| Constraint | `TResponse : IFinResponseFactory<TResponse>` |
| Behavior | Catches exceptions via `try-catch` and returns `TResponse.CreateFail(AdapterError.FromException(...))` |
| Cancellation | `OperationCanceledException` (and its subtype `TaskCanceledException`) is **re-thrown**, not wrapped — so callers can distinguish cancellation from business failure |
| Error Type | `AdapterErrorType.PipelineException` |

### UsecaseValidationPipeline

Executes FluentValidation `IValidator<TRequest>` and returns `FinResponse.Fail` on validation failure.

```csharp
internal sealed class UsecaseValidationPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>, IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
    where TResponse : IFinResponseFactory<TResponse>
```

| Item | Description |
|------|------|
| DI Dependencies | `IEnumerable<IValidator<TRequest>>` |
| Behavior | Passes through `next()` if no Validators exist; runs all Validators otherwise |
| Error Type | `AdapterErrorType.PipelineValidation(PropertyName)` |
| Multiple errors | Returns `Error.Many(errors)` when there are 2 or more validation failures |

### UsecaseLoggingPipeline

Records request/response information via structured logging. Automatically pushes custom attributes if `IUsecaseCtxEnricher` is registered in DI.

```csharp
internal sealed class UsecaseLoggingPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>, IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
    where TResponse : IFinResponse, IFinResponseFactory<TResponse>
```

| Item | Description |
|------|------|
| DI Dependencies | `ILogger<UsecaseLoggingPipeline<TRequest, TResponse>>`, `IUsecaseCtxEnricher<TRequest, TResponse>?` (optional) |
| Request log | Information level, `{Layer} {Category} {CategoryType} {Handler} {Method} requesting` |
| Response log (success) | Information level, `responded success in {Elapsed:0.0000} ms` |
| Response log (Expected error) | Warning level, `responded failure ... with {@Error}` |
| Response log (Exceptional error) | Error level, `responded failure ... with {@Error}` |

### UsecaseTracingPipeline

Creates distributed tracing Spans using OpenTelemetry `ActivitySource`.

```csharp
internal sealed class UsecaseTracingPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>, IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
    where TResponse : IFinResponse, IFinResponseFactory<TResponse>
```

| Item | Description |
|------|------|
| DI Dependencies | `ActivitySource` |
| Span name | `{layer} {category}.{categoryType} {handler}.{method}` |
| ActivityKind | `Internal` |
| Request tags | `request.layer`, `request.category.name`, `request.category.type`, `request.handler.name`, `request.handler.method` |
| Response tags (success) | `response.status = success`, `ActivityStatusCode.Ok` |
| Response tags (failure) | `response.status = failure`, `error.type`, `error.code`, `ActivityStatusCode.Error` |
| Time tag | `response.elapsed` (in seconds) |

### UsecaseMetricsPipeline

Collects request counts, response counts, and processing time via OpenTelemetry Meter.

```csharp
internal sealed class UsecaseMetricsPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>, IPipelineBehavior<TRequest, TResponse>, IDisposable
    where TRequest : IMessage
    where TResponse : IFinResponse, IFinResponseFactory<TResponse>
```

| Item | Description |
|------|------|
| DI Dependencies | `IOptions<OpenTelemetryOptions>`, `IMeterFactory` |
| Meter name | `{ServiceNamespace}.application` |
| Counter (requests) | `application.usecase.{categoryType}.requests` (unit: `{request}`) |
| Counter (responses) | `application.usecase.{categoryType}.responses` (unit: `{response}`) |
| Histogram (duration) | `application.usecase.{categoryType}.duration` (unit: `s`) |
| Request tags | `request.layer`, `request.category.name`, `request.category.type`, `request.handler.name`, `request.handler.method` |
| Response tags (success) | Request tags + `response.status = success` |
| Response tags (failure) | Request tags + `response.status = failure` + `error.type` + `error.code` |

### UsecaseTransactionPipeline

Automatically handles explicit transactions, `UoW.SaveChanges`, and domain event publishing for Command Usecases.

```csharp
internal sealed class UsecaseTransactionPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>, IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
    where TResponse : IFinResponse, IFinResponseFactory<TResponse>
```

| Item | Description |
|------|------|
| Constraint | `TRequest : ICommand<TResponse>` (Command only) |
| DI Dependencies | `IUnitOfWork`, `IDomainEventPublisher`, `ILogger` |
| Execution order | 1) Begin transaction → 2) Execute Handler → 3) Rollback on failure → 4) `SaveChanges` → 5) Commit → 6) Publish domain events |
| Failure handling | Returns `TResponse.CreateFail(error)` on Handler failure or SaveChanges failure, with automatic transaction rollback |

### UsecaseCachingPipeline

Performs `IMemoryCache`-based caching for Query requests that implement `ICacheable`.

```csharp
internal sealed class UsecaseCachingPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>, IPipelineBehavior<TRequest, TResponse>
    where TRequest : IQuery<TResponse>
    where TResponse : IFinResponse, IFinResponseFactory<TResponse>
```

| Item | Description |
|------|------|
| Constraint | `TRequest : IQuery<TResponse>` (Query only) |
| DI Dependencies | `IMemoryCache` (`services.AddMemoryCache()` required) |
| Cache key | `ICacheable.CacheKey` |
| Cache duration | `ICacheable.Duration` (defaults to 5 minutes if null) |
| Behavior | Returns immediately on cache hit; on cache miss, executes Handler and caches only successful responses |

**ICacheable interface:**

```csharp
public interface ICacheable
{
    string CacheKey { get; }
    TimeSpan? Duration { get; }
}
```

### Custom Pipeline

Custom Pipeline defined by the user. Executes after the default Pipelines (Exception, Validation, etc.) and just before the Handler. Implementing the `ICustomUsecasePipeline` marker interface enables automatic assembly scanning registration.

---

## Custom Extensions

### ICustomUsecasePipeline

A marker interface for Scrutor auto-discovery registration.

```csharp
public interface ICustomUsecasePipeline { }
```

Using `AddCustomPipeline<T>()` explicitly registers types implementing this interface in DI.

### UsecaseMetricCustomPipelineBase\<TRequest\>

A base class for creating per-Usecase individual Metrics. Automatically identifies the CQRS type from the `TRequest` type.

```csharp
public abstract class UsecaseMetricCustomPipelineBase<TRequest>
    : UsecasePipelineBase<TRequest>, ICustomUsecasePipeline
```

| Member | Description |
|------|------|
| `protected readonly Meter _meter` | `{ServiceNamespace}.application` Meter instance |
| `protected const string DurationUnit` | `"s"` |
| `protected const string CountUnit` | `"requests"` |
| `GetMetricName(string metricName)` | Returns `application.usecase.{cqrs}.{handler}.{metricName}` format |
| `GetMetricNameWithoutHandler(string metricName)` | Returns `application.usecase.{cqrs}.{metricName}` format |

**Constructor:**

```csharp
protected UsecaseMetricCustomPipelineBase(string serviceNamespace, IMeterFactory meterFactory)
```

**RequestDuration helper:**

```csharp
public class RequestDuration : IDisposable
{
    public RequestDuration(Histogram<double> histogram);
    public void Dispose(); // Automatically records elapsed time to histogram
}
```

Used with the `using` statement to automatically perform time measurement and Histogram recording.

### UsecaseTracingCustomPipelineBase\<TRequest\>

A base class for creating per-Usecase custom Tracing. Creates custom Activities (Spans) via `ActivitySource` and sets standard tags.

```csharp
public abstract class UsecaseTracingCustomPipelineBase<TRequest>
    : UsecasePipelineBase<TRequest>, ICustomUsecasePipeline
```

| Member | Description |
|------|------|
| `protected readonly ActivitySource _activitySource` | ActivitySource used for creating Activities |
| `StartCustomActivity(string operationName, ActivityKind kind)` | Creates custom Activity in `{prefix}.{operationName}` format |
| `GetActivityName(string operationName)` | Gets Activity name (`{prefix}.{operationName}`) |
| `SetStandardRequestTags(Activity activity, string method)` | Sets 5 standard Request tags |

**Constructor:**

```csharp
protected UsecaseTracingCustomPipelineBase(ActivitySource activitySource)
```

- Activity name prefix: `{layer} {category}.{categoryType} {handler}`
- If a parent `Activity.Current` exists, it is created as a child Span.

---

## PipelineConfigurator API

`PipelineConfigurator` enables/disables individual Pipelines via Fluent API and adds custom Pipelines.

```csharp
public class PipelineConfigurator
```

### Activation Methods

| Method | Return Type | Description |
|--------|----------|------|
| `UseObservability()` | `PipelineConfigurator` | Batch enable all 4 observability types (CtxEnricher, Metrics, Tracing, Logging) |
| `UseMetrics()` | `PipelineConfigurator` | Enable Metrics Pipeline (CtxEnricher auto-included) |
| `UseTracing()` | `PipelineConfigurator` | Enable Tracing Pipeline (CtxEnricher auto-included) |
| `UseLogging()` | `PipelineConfigurator` | Enable Logging Pipeline (CtxEnricher auto-included) |
| `UseValidation()` | `PipelineConfigurator` | Enable Validation Pipeline |
| `UseCaching()` | `PipelineConfigurator` | Enable Caching Pipeline (`IMemoryCache` DI registration required) |
| `UseException()` | `PipelineConfigurator` | Enable Exception Pipeline |
| `UseTransaction()` | `PipelineConfigurator` | Enable Transaction Pipeline (`IUnitOfWork`, `IDomainEventPublisher`, `IDomainEventCollector` DI registration required) |

### Configuration Methods

| Method | Return Type | Description |
|--------|----------|------|
| `WithLifetime(ServiceLifetime lifetime)` | `PipelineConfigurator` | Set Pipeline service Lifetime (default: `Scoped`) |
| `AddCustomPipeline<TPipeline>()` | `PipelineConfigurator` | Explicitly register custom Pipeline by type (deterministic guarantee of Pipeline execution order) |

### Usage Example

```csharp
// Enable observability + validation + exception handling
services
    .RegisterOpenTelemetry(configuration, Assembly.GetExecutingAssembly())
    .ConfigurePipelines(pipelines => pipelines
        .UseObservability()   // Batch enable CtxEnricher, Metrics, Tracing, Logging
        .UseValidation()
        .UseException())
    .Build();

// Selective enabling + custom Pipeline registration
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
        .AddCustomPipeline<PlaceOrderCommandMetricPipeline>()
        .WithLifetime(ServiceLifetime.Scoped))
    .Build();
```

### Registration Order

Inside `Apply()`, Pipelines are registered to `IPipelineBehavior<,>` in the following order.

1. Metrics
2. Tracing
3. Logging
4. Validation
5. Caching
6. Exception
7. Transaction
8. Custom (registered in the order `AddCustomPipeline<T>()` is called)
9. Handler

**Transaction Pipeline auto-detection:** Even if `UseTransaction()` is called, registration is skipped if `IUnitOfWork`, `IDomainEventPublisher`, and `IDomainEventCollector` are not all registered in DI.

---

## Configure OpenTelemetry

### OpenTelemetryBuilder

The main Builder class for OpenTelemetry configuration. Configures Serilog, Metrics, Tracing, and Pipeline settings via chaining.

```csharp
public partial class OpenTelemetryBuilder
```

#### Entry Point

```csharp
// IServiceCollection extension method
public static OpenTelemetryBuilder RegisterOpenTelemetry(
    this IServiceCollection services,
    IConfiguration configuration,
    Assembly projectAssembly)
```

#### Configure Methods

| Method | Return Type | Description |
|--------|----------|------|
| `ConfigureLogging(Action<LoggingConfigurator> configure)` | `OpenTelemetryBuilder` | Serilog extension configuration |
| `ConfigureMetrics(Action<MetricsConfigurator> configure)` | `OpenTelemetryBuilder` | OpenTelemetry Metrics extension configuration |
| `ConfigureTracing(Action<TracingConfigurator> configure)` | `OpenTelemetryBuilder` | OpenTelemetry Tracing extension configuration |
| `ConfigurePipelines(Action<PipelineConfigurator> configure)` | `OpenTelemetryBuilder` | Pipeline configuration (explicit opt-in required) |
| `ConfigureStartupLogger(Action<ILogger> configure)` | `OpenTelemetryBuilder` | Additional logging configuration at startup |
| `WithAdapterObservability(bool enable = true)` | `OpenTelemetryBuilder` | Enable/disable Adapter observability (default: `true`) |
| `Build()` | `IServiceCollection` | Returns IServiceCollection after applying all settings |

#### Build() Internal Processing Order

1. Read `OpenTelemetryOptions` (`IOptions<OpenTelemetryOptions>`)
2. Create Resource Attributes
3. Configure Serilog (ReadFrom.Configuration + WriteTo.OpenTelemetry + ErrorsDestructuringPolicy)
4. Configure CtxEnricherContext PushProperty factory
5. Configure OpenTelemetry (Metrics + Tracing + OTLP Exporter)
6. Register Adapter Observability (`ActivitySource`, `IMeterFactory`)
7. Register Usecase Pipeline
8. Register StartupLogger `IHostedService`

### LoggingConfigurator

A Builder class for Serilog extension configuration.

```csharp
public class LoggingConfigurator
```

| Member | Description |
|------|------|
| `Options` | `OpenTelemetryOptions` Access property |
| `AddDestructuringPolicy<TPolicy>()` | `IDestructuringPolicy` Register implementation type |
| `AddEnricher(ILogEventEnricher enricher)` | Register Enricher instance |
| `AddEnricher<TEnricher>()` | Register Enricher type |
| `Configure(Action<LoggerConfiguration> configure)` | `LoggerConfiguration` Direct access |

### MetricsConfigurator

A Builder class for OpenTelemetry Metrics extension configuration.

```csharp
public class MetricsConfigurator
```

| Member | Description |
|------|------|
| `Options` | `OpenTelemetryOptions` Access property |
| `AddMeter(string meterName)` | Register additional Meter (wildcard supported: `"MyApp.*"`) |
| `AddInstrumentation(Action<MeterProviderBuilder> configure)` | Register additional Instrumentation |
| `Configure(Action<MeterProviderBuilder> configure)` | `MeterProviderBuilder` Direct access |

### TracingConfigurator

A Builder class for OpenTelemetry Tracing extension configuration.

```csharp
public class TracingConfigurator
```

| Member | Description |
|------|------|
| `Options` | `OpenTelemetryOptions` Access property |
| `AddSource(string sourceName)` | Register additional ActivitySource (wildcard supported: `"MyApp.*"`) |
| `AddProcessor(BaseProcessor<Activity> processor)` | Register additional Processor |
| `Configure(Action<TracerProviderBuilder> configure)` | `TracerProviderBuilder` Direct access |

---

## OpenTelemetryOptions

A configuration class bound to the `"OpenTelemetry"` section in `appsettings.json`.

```csharp
public sealed class OpenTelemetryOptions : IStartupOptionsLogger, IOpenTelemetryOptions
```

### Properties

| Property | Type | Default | Description |
|---------|------|--------|------|
| `ServiceNamespace` | `string` | `""` | Service namespace (group) |
| `ServiceName` | `string` | `""` | Service name |
| `ServiceVersion` | `string` | (assembly version) | Service version (auto-configured) |
| `ServiceInstanceId` | `string` | (hostname) | Service instance ID (auto-configured) |
| `CollectorEndpoint` | `string` | `""` | Unified OTLP Collector endpoint |
| `TracingEndpoint` | `string?` | `null` | Tracing-specific endpoint (uses `CollectorEndpoint` if null) |
| `MetricsEndpoint` | `string?` | `null` | Metrics-specific endpoint (uses `CollectorEndpoint` if null) |
| `LoggingEndpoint` | `string?` | `null` | Logging-specific endpoint (uses `CollectorEndpoint` if null) |
| `CollectorProtocol` | `string` | `"Grpc"` | Unified OTLP Protocol |
| `TracingProtocol` | `string?` | `null` | Tracing-specific Protocol |
| `MetricsProtocol` | `string?` | `null` | Metrics-specific Protocol |
| `LoggingProtocol` | `string?` | `null` | Logging-specific Protocol |
| `SamplingRate` | `double` | `1.0` | Tracing sampling rate (0.0 ~ 1.0) |
| `EnablePrometheusExporter` | `bool` | `false` | Enable Prometheus Exporter |

### Endpoint Resolution Rules

The resolution rules for individual endpoints (`TracingEndpoint`, `MetricsEndpoint`, `LoggingEndpoint`) are as follows.

| Value | Behavior |
|----|------|
| `null` | Uses `CollectorEndpoint` (default behavior) |
| `""` (empty string) | Disables that signal |
| `"http://..."` | Uses that endpoint |

### Protocol Methods

| Method | Return Type | Description |
|--------|----------|------|
| `GetTracingProtocol()` | `OtlpCollectorProtocol` | Tracing Protocol (individual setting takes priority) |
| `GetMetricsProtocol()` | `OtlpCollectorProtocol` | Metrics Protocol (individual setting takes priority) |
| `GetLoggingProtocol()` | `OtlpCollectorProtocol` | Logging Protocol (individual setting takes priority) |
| `GetTracingEndpoint()` | `string` | Tracing endpoint (resolution rules applied) |
| `GetMetricsEndpoint()` | `string` | Metrics endpoint (resolution rules applied) |
| `GetLoggingEndpoint()` | `string` | Logging endpoint (resolution rules applied) |

### OtlpCollectorProtocol (SmartEnum)

```csharp
public sealed class OtlpCollectorProtocol : SmartEnum<OtlpCollectorProtocol>
```

| Constant | Value | Description |
|------|----|------|
| `Grpc` | 1 | gRPC protocol (default) |
| `HttpProtobuf` | 2 | HTTP/Protobuf protocol |

### Validator

A `FluentValidation`-based options validator.

| Rule | Description |
|------|------|
| `ServiceNamespace` | Required (NotEmpty) |
| `ServiceName` | Required (NotEmpty) |
| Endpoints | `CollectorEndpoint` or at least one individual endpoint required |
| `SamplingRate` | 0.0 ~ 1.0 Range |
| Protocol | SmartEnum valid value validation |

### appsettings.json Example

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

## ObservabilityNaming Constants

Defines unified naming conventions for observability. The single source of truth for metric names, tag keys, Span names, etc.

```csharp
public static partial class ObservabilityNaming
```

### Layers

| Constant | Value | Description |
|------|----|------|
| `Application` | `"application"` | Application Layer |
| `Adapter` | `"adapter"` | Adapter Layer |

### Categories

| Constant | Value | Description |
|------|----|------|
| `Usecase` | `"usecase"` | Usecase category |
| `Repository` | `"repository"` | Repository category |
| `Event` | `"event"` | Event category |
| `Unknown` | `"unknown"` | Unknown category |

### CategoryTypes

| Constant | Value | Description |
|------|----|------|
| `Command` | `"command"` | Command type |
| `Query` | `"query"` | Query type |
| `Event` | `"event"` | Event type |
| `Unknown` | `"unknown"` | Unknown type |

### Status

| Constant | Value | Description |
|------|----|------|
| `Success` | `"success"` | Success |
| `Failure` | `"failure"` | Failure |

### ErrorTypes

| Constant | Value | Description |
|------|----|------|
| `Expected` | `"expected"` | Expected business error (`IsExpected = true`) |
| `Exceptional` | `"exceptional"` | Exceptional system error (`IsExceptional = true`) |
| `Aggregate` | `"aggregate"` | Aggregate error (`ManyErrors`) |

### Methods

| Constant | Value | Description |
|------|----|------|
| `Handle` | `"Handle"` | Usecase Handler method |
| `Publish` | `"Publish"` | Event publishing method |
| `PublishTrackedEvents` | `"PublishTrackedEvents"` | Tracked event publishing method |

### OTelAttributes (OpenTelemetry Standard)

| Constant | Value |
|------|----|
| `ErrorType` | `"error.type"` |
| `ServiceNamespace` | `"service.namespace"` |
| `ServiceName` | `"service.name"` |
| `ServiceVersion` | `"service.version"` |
| `ServiceInstanceId` | `"service.instance.id"` |
| `DeploymentEnvironment` | `"deployment.environment"` |

### CustomAttributes (3-Pillar Common)

| Constant | Value | Purpose |
|------|----|------|
| `RequestMessage` | `"request.message"` | Request message |
| `RequestParams` | `"request.params"` | Request parameters |
| `RequestLayer` | `"request.layer"` | Request layer |
| `RequestCategoryName` | `"request.category.name"` | Request category name |
| `RequestCategoryType` | `"request.category.type"` | Request category type |
| `RequestHandlerName` | `"request.handler.name"` | Request handler name |
| `RequestHandlerMethod` | `"request.handler.method"` | Request handler method |
| `ResponseMessage` | `"response.message"` | Response message |
| `ResponseStatus` | `"response.status"` | Response status |
| `ResponseElapsed` | `"response.elapsed"` | Response elapsed time |
| `ErrorCode` | `"error.code"` | Error code |

### Metrics Name Generation

| Method | Example |
|--------|------|
| `Metrics.UsecaseRequest("command")` | `"application.usecase.command.requests"` |
| `Metrics.UsecaseResponse("query")` | `"application.usecase.query.responses"` |
| `Metrics.UsecaseDuration("command")` | `"application.usecase.command.duration"` |
| `Metrics.AdapterRequest("repository")` | `"adapter.repository.requests"` |
| `Metrics.AdapterResponse("repository")` | `"adapter.repository.responses"` |
| `Metrics.AdapterDuration("repository")` | `"adapter.repository.duration"` |

### Spans Name Generation

| Method | Example |
|--------|------|
| `Spans.OperationName("adapter", "repository", "OrderRepository", "GetById")` | `"adapter repository OrderRepository.GetById"` |

### EventIds

| Scope | ID | Name |
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

## Related Documents

- **Guide:** [Adapter Connection -- Pipeline and DI](../guides/adapter/14a-adapter-pipeline-di) -- Pipeline DI registration practice
- **Guide:** [Adapter Testing](../guides/adapter/14b-adapter-testing) -- Pipeline unit testing
- **Guide:** [Observability Logging](../guides/observability/19-observability-logging) -- Logging Pipeline details
- **Guide:** [Observability Metrics](../guides/observability/20-observability-metrics) -- Metrics Pipeline details
- **Guide:** [Observability Tracing](../guides/observability/21-observability-tracing) -- Tracing Pipeline details
- **Spec:** [Observability Specification](../08-observability) -- Field/Tag specification, Meter definition, message templates
- **Spec:** [Usecase CQRS](../05-usecase-cqrs) -- `FinResponse<T>`, `ICommandRequest`, `IQueryRequest`
