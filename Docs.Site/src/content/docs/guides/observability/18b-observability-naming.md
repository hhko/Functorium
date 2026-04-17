---
title: "Observability Naming Guide"
---

This document defines the naming conventions to follow when writing Observability-related code in the Functorium project.

## Introduction

- "If observability code naming is inconsistent, searching and filtering become difficult -- how do we distinguish Signal from Component?"
- "What rules are needed to ensure the entire team writes Logger method names in the same pattern?"
- "Why do different class types such as Configurator, Pipeline, and Options have different naming patterns?"

By establishing naming conventions based on OpenTelemetry standard terminology while considering the .NET ecosystem and practicality, you can achieve both code readability and search efficiency.

### What You Will Learn

This document covers the following topics:

1. **Code naming conventions** - Distinguishing roles of Signal prefixes and Component suffixes
2. **Logger method naming** - The `Log{Context}{Phase}{Status}` pattern and usage examples
3. **Consistent instrumentation identifiers** - Naming patterns for each class type: Configurator, Pipeline, Options, Extensions, etc.

> **Core principle:** Signal names (`Logging`, `Tracing`, `Metrics`) are used as prefixes for configuration/activity targets, while Component types (`Logger`, `Span`, `Metric`) are used as suffixes for concrete objects. Internal consistency takes priority over external standard compliance.

## Summary

### Key Commands

```csharp
// Configurator: Signal prefix
LoggingConfigurator, TracingConfigurator, MetricsConfigurator

// Pipeline: Layer + Signal
UsecaseLoggingPipeline, UsecaseTracingPipeline, UsecaseMetricsPipeline

// Options: Signal + Property (gerund)
LoggingEndpoint, TracingEndpoint, MetricsEndpoint

// Logger Method: Log{Context}{Phase}{Status}
LogUsecaseRequest, LogUsecaseResponseSuccess, LogUsecaseResponseError
```

### Key Procedures

1. When writing new classes, distinguish between Signal names (`Logging`, `Tracing`, `Metrics`) and Component types (`Logger`, `Span`, `Metric`)
2. Use Signal names as prefixes (configuration/activity classes), Component types as suffixes (components)
3. Logger methods follow the `Log{Context}{Phase}{Status}` pattern

### Key Concepts

| Concept | Rule | Example |
|------|------|------|
| Signal prefix | Configuration/activity target | `LoggingConfigurator`, `TracingEndpoint` |
| Component suffix | Concrete object | `StartupLogger`, `ISpanFactory` |
| Logger method | `Log{Context}{Phase}{Status}` | `LogDomainEventHandlerResponseError` |
| Internal consistency first | Use `Tracing` instead of OpenTelemetry "Traces" | `TracingConfigurator` (not `TracesConfigurator`) |

The summary covered the core rules for Signal/Component distinction and Logger method patterns. Now let's examine each rule in detail at the code level.

---

## Code Naming Conventions

### Overview

This document defines the naming conventions to follow when writing Observability-related code in the Functorium project.
The conventions are based on OpenTelemetry standard terminology while considering the .NET ecosystem and practicality.

### Core Principles

#### OpenTelemetry Signals

OpenTelemetry defines three observability signals:

- **Logging**: Log signal system
- **Tracing**: Distributed tracing signal system
- **Metrics**: Metrics signal system

#### Terminology Role Distinction

**Signal names (Logging, Tracing, Metrics):**
- Represent the signal system/framework
- Also used as adjectives/gerunds representing activity/configuration
- Usage locations:
  - Prefix: Configuration/activity target → `LoggingConfigurator`, `TracingEndpoint`
  - Standalone: Activity/process → `UsecaseLoggingPipeline` (performs Logging activity)

**Component types (Logger, Span, Metric, etc.):**
- Concrete objects/components
- Primarily used only as suffixes
- Types:
  - `Logger`: Object that generates logs → `StartupLogger`
  - `Span`: Unit of tracing → `ISpan`, `OpenTelemetrySpan`
  - `Metric`: Measurement value → `IMetricRecorder`
  - `Tracer`: Factory that creates Spans (actually uses `SpanFactory`)
  - `Meter`: Object that records Metrics (actually uses `MetricRecorder`)

**Naming principle summary:**
```
Prefix:
  Logging-     : Logging configuration/activity (LoggingConfigurator, LoggingEndpoint)
  Tracing-     : Tracing configuration/activity (TracingConfigurator, TracingEndpoint, TracingProtocol)
  Metrics-     : Metrics configuration/activity (MetricsConfigurator, MetricsEndpoint)

Suffix (Component):
  -Logger      : Logger object (StartupLogger, IStartupOptionsLogger)
  -Span        : Tracing unit (ISpan, OpenTelemetrySpan)
  -Metric      : Metric (rarely used standalone)
  -SpanFactory : Span factory (ISpanFactory)
  -MetricRecorder : Metric recorder (IMetricRecorder)

Standalone (Activity):
  Logging      : Logging activity (UsecaseLoggingPipeline)
  Tracing      : Tracing activity (UsecaseTracingPipeline)
  Metrics      : Metrics activity (UsecaseMetricsPipeline)
```

#### Terminology Consistency Principle

**Endpoint and Protocol naming:**
- Endpoint: Gerund form (`LoggingEndpoint`, `TracingEndpoint`, `MetricsEndpoint`)
- Protocol: Gerund form (`LoggingProtocol`, `TracingProtocol`, `MetricsProtocol`)
- Reason: Gerunds are natural since they represent configuration/setup

**Configurator naming:**
- Logging: `LoggingConfigurator` (gerund)
- Tracing: `TracingConfigurator` (gerund -- see [Tracing Consistency Principle](#tracing-consistency-principle))
- Metrics: `MetricsConfigurator` (plural noun)

### Naming Rules

#### Configurator (Configuration Class)

**Rule**: `{Signal}Configurator`

Since these classes configure the entire Signal system, the Signal name is used as a prefix.

```csharp
// ✅ Correct
public class LoggingConfigurator { }
public class TracingConfigurator { }
public class MetricsConfigurator { }

// ❌ Incorrect
public class LogsConfigurator { }      // "Logs" confused with file directories
public class LoggerConfigurator { }    // Logger is a component, not a Signal
public class TraceConfigurator { }     // Singular form inappropriate
public class TracesConfigurator { }    // Official OpenTelemetry term, but internal consistency matters more
```

**Usage Example**:
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

#### Logger (Logger Component)

**Rule**: `{Purpose}Logger`

Components that generate logs use purpose or role as prefix and Logger as suffix.

```csharp
// ✅ Correct
public class StartupLogger : IHostedService { }
public class ConsoleLogger { }
public class FileLogger { }
public interface IStartupOptionsLogger { }

// ❌ Incorrect
public class StartupLogging { }        // Logging is activity/configuration, not an object
public class LoggerStartup { }         // Awkward word order
```

#### Pipeline

**Rule**: `{Layer}{Signal}Pipeline`

A Pipeline is a class that performs Signal activity in a specific layer.

```csharp
// ✅ Correct
public class UsecaseLoggingPipeline<TRequest, TResponse> { }
public class UsecaseTracingPipeline<TRequest, TResponse> { }
public class UsecaseMetricsPipeline<TRequest, TResponse> { }

public class AdapterLoggingPipeline { }
public class AdapterTracingPipeline { }

// ❌ Incorrect
public class UsecaseLoggerPipeline { }   // Logger is a component, not an activity
public class UsecaseTracePipeline { }    // Trace is singular
public class LoggingUsecasePipeline { }  // Awkward word order
```

**Reason**:
- Pipeline expresses "what it does" → emphasizes Signal activity
- All three Pipelines maintain the same pattern (consistency)
- Matches naming pattern with Configurator

#### Extensions (Extension Methods)

**Rule**: `{Target}Extensions`

The target Component is used as a prefix.

```csharp
// ✅ Correct
public static class LoggerExtensions { }
public static class UsecaseLoggerExtensions { }
public static class SpanExtensions { }
public static class MetricExtensions { }

// ❌ Incorrect
public static class LoggingExtensions { }  // Logging is configuration/activity
public static class ExtensionsLogger { }   // Awkward word order
```

#### Options (Configuration Properties)

**Rule**: `{Signal}{Property}`

Options property names use the Signal name (gerund form) as a prefix.

```csharp
// ✅ Correct
public class OpenTelemetryOptions
{
    // Endpoint in gerund form
    public string LoggingEndpoint { get; set; }
    public string TracingEndpoint { get; set; }
    public string MetricsEndpoint { get; set; }

    // Protocol also in gerund form
    public string LoggingProtocol { get; set; }
    public string TracingProtocol { get; set; }
    public string MetricsProtocol { get; set; }

    // Getter methods follow the same pattern
    public string GetLoggingEndpoint() { }
    public string GetTracingEndpoint() { }
    public string GetMetricsEndpoint() { }

    public OtlpCollectorProtocol GetLoggingProtocol() { }
    public OtlpCollectorProtocol GetTracingProtocol() { }
    public OtlpCollectorProtocol GetMetricsProtocol() { }
}

// ❌ Incorrect
public string LogsEndpoint { get; set; }        // "Logs" confused with file directories
public string TracesEndpoint { get; set; }      // Endpoint uses gerund form
public string LoggerEndpoint { get; set; }      // Logger is a component
public string LogEndpoint { get; set; }         // Singular form
```

**Consistency principle**:
- `LoggingEndpoint` + `LoggingProtocol` (unified with gerund form)
- `TracingEndpoint` + `TracingProtocol` (unified with gerund form)
- `MetricsEndpoint` + `MetricsProtocol` (unified with gerund form)

#### Builder Methods

**Rule**: `Configure{Signal}()`

Builder methods use the Signal name for Signal configuration.

```csharp
// ✅ Correct
public class OpenTelemetryBuilder
{
    public OpenTelemetryBuilder ConfigureLogging(Action<LoggingConfigurator> configure) { }
    public OpenTelemetryBuilder ConfigureTracing(Action<TracingConfigurator> configure) { }
    public OpenTelemetryBuilder ConfigureMetrics(Action<MetricsConfigurator> configure) { }
}

// ❌ Incorrect
public OpenTelemetryBuilder ConfigureSerilog(...) { }  // Technology-dependent
public OpenTelemetryBuilder ConfigureLogs(...) { }     // Logs confused with files
public OpenTelemetryBuilder ConfigureLogger(...) { }   // Logger is a component
public OpenTelemetryBuilder ConfigureTraces(...) { }   // Use Tracing for consistency
```

#### Interfaces

**Rule**: Use Component type as suffix

```csharp
// ✅ Correct - Component type is explicit
public interface IStartupOptionsLogger { }
public interface IMetricRecorder { }
public interface ISpanFactory { }
public interface ISpan { }

// ❌ Incorrect
public interface ILogging { }           // Too abstract
public interface ILog { }               // Confused with a single log entry
```

#### Implementation Classes

**Rule**: `{Technology}{Component}`

Implementations of specific technologies use the technology name as a prefix.

```csharp
// ✅ Correct
public class OpenTelemetrySpan : ISpan { }
public class OpenTelemetrySpanFactory : ISpanFactory { }
public class OpenTelemetryMetricRecorder : IMetricRecorder { }

// ❌ Incorrect
public class SpanOpenTelemetry : ISpan { }     // Awkward word order
public class OTelSpan : ISpan { }              // Avoid abbreviations
```

### Special Cases

#### Span vs Tracer

There are two concepts in the Tracing system:

- **Span**: A single unit of work in tracing (data object)
- **Tracer**: A factory that creates Spans (creation object)

```csharp
// ✅ Correct
public interface ISpan { }              // Single unit of work
public interface ISpanFactory { }       // Span creation factory (Tracer role)

// In OpenTelemetry, ActivitySource plays the Tracer role
public class OpenTelemetrySpanFactory : ISpanFactory
{
    private readonly ActivitySource _activitySource;  // Tracer
}
```

#### Logging vs Logger

- **Logging**: Signal name, configuration/activity (prefix)
- **Logger**: Component type (suffix)

```csharp
// ✅ Correct - Configuration class (Logging)
public class LoggingConfigurator { }

// ✅ Correct - Component (Logger)
public class StartupLogger { }
public interface IStartupOptionsLogger { }

// ✅ Correct - Pipeline (Logging activity)
public class UsecaseLoggingPipeline { }

// ✅ Correct - Extensions (Logger extension)
public static class UsecaseLoggerExtensions { }

// ✅ Correct - Options (Logging configuration)
public string LoggingEndpoint { get; set; }
public string LoggingProtocol { get; set; }
```

#### Tracing Consistency Principle

- **Tracing**: Used consistently across all contexts (gerund)

```csharp
// ✅ Correct - Configurator
public class TracingConfigurator { }

// ✅ Correct - Builder Method (Tracing)
public OpenTelemetryBuilder ConfigureTracing(Action<TracingConfigurator> configure) { }

// ✅ Correct - Pipeline (Tracing activity)
public class UsecaseTracingPipeline { }

// ✅ Correct - Options (Tracing configuration)
public string TracingEndpoint { get; set; }
public string TracingProtocol { get; set; }
public string GetTracingEndpoint() { }
public OtlpCollectorProtocol GetTracingProtocol() { }
```

**Naming principle summary:**
- **Configurator**: `TracingConfigurator`
- **Options/Settings**: `TracingEndpoint`, `TracingProtocol` (configuration activity)
- **Pipeline**: `UsecaseTracingPipeline` (tracing activity)
- **Builder Method**: `ConfigureTracing()` (maintain consistency)

> **Design Decision -- Internal Consistency First Principle**
>
> While the official OpenTelemetry term is "Traces", Functorium prioritizes **internal consistency**.
> The consistent gerund pattern of `LoggingConfigurator`, `TracingConfigurator`, `MetricsConfigurator`
> contributes more to codebase readability and maintainability than external standard compliance.
> The same principle applies to all Signal prefix naming including Endpoint, Protocol, Pipeline, etc.

#### Configuration vs Configurator

- **Configuration**: Configuration data/options
- **Configurator**: Builder class that performs configuration

```csharp
// ✅ Configuration (data)
public class OpenTelemetryOptions { }
public class LoggingConfiguration { }

// ✅ Configurator (builder)
public class LoggingConfigurator { }
public class TracingConfigurator { }
```

#### Folder Naming Convention

**Folders use plural forms** - "what do they contain"

```
Src/Functorium/Applications/Observabilities/
├── Loggers/          ✅ Contains Logger-related classes
├── Metrics/          ✅ Contains Metric-related classes
├── Spans/            ✅ Contains Span-related classes
└── Context/          ✅ Contains Context-related classes

Src/Functorium.Adapters/Observabilities/
├── Loggers/          ✅
├── Metrics/          ✅
├── Spans/            ✅
└── Context/          ✅
```

> **Note**: The above structure represents the design intent and may differ from the actual codebase structure. Currently the `Applications/Observabilities/` directory does not exist, and the actual subfolder structure of `Adapters/Observabilities/` is as follows:
> ```
> Src/Functorium.Adapters/Observabilities/
> ├── Builders/Configurators/   (LoggingConfigurator, TracingConfigurator, MetricsConfigurator, etc.)
> ├── Events/                   (ObservableDomainEventPublisher, etc.)
> ├── Loggers/                  (UsecaseLoggerExtensions, StartupLogger, etc.)
> ├── Naming/                   (ObservabilityNaming constants)
> └── Pipelines/                (UsecaseLoggingPipeline, UsecaseTracingPipeline, UsecaseMetricsPipeline, etc.)
> ```

### Namespace Structure

```
Functorium.Abstractions.Observabilities/
├── Context/
│   ├── IContextPropagator.cs
│   └── IObservabilityContext.cs
├── Loggers/
│   └── UsecaseLoggerExtensions.cs         // Logger extension
├── Metrics/
│   └── IMetricRecorder.cs                 // Metric recording
├── Spans/
│   ├── ISpan.cs                           // Span interface
│   └── ISpanFactory.cs                    // Span factory
└── ObservabilityNaming.cs

Functorium.Adapters.Observabilities/
├── Builders/
│   ├── Configurators/
│   │   ├── LoggingConfigurator.cs         // Logging configuration
│   │   ├── TracingConfigurator.cs         // Tracing configuration
│   │   └── MetricsConfigurator.cs         // Metrics configuration
│   ├── PipelineConfigurator.cs          // Pipeline selective registration (UseObservability, UseMetrics, etc.)
│   └── OpenTelemetryBuilder.cs
├── Loggers/
│   ├── IStartupOptionsLogger.cs           // Logger interface
│   └── StartupLogger.cs                   // Logger implementation
├── Metrics/
│   └── OpenTelemetryMetricRecorder.cs     // Metric implementation
└── Spans/
    ├── OpenTelemetrySpan.cs               // Span implementation
    └── OpenTelemetrySpanFactory.cs        // SpanFactory implementation

Functorium.Adapters.Pipelines/
├── UsecasePipelineBase.cs                 // Pipeline common base class
├── ICustomUsecasePipeline.cs              // Custom pipeline marker interface
├── CtxEnricherPipeline.cs                 // ctx.* 3-Pillar Enrichment Pipeline (runs first)
├── UsecaseLoggingPipeline.cs              // Logging Pipeline
├── UsecaseTracingPipeline.cs              // Tracing Pipeline
├── UsecaseTracingCustomPipelineBase.cs    // Custom Tracing Pipeline base
├── UsecaseMetricsPipeline.cs              // Metrics Pipeline
├── UsecaseMetricCustomPipelineBase.cs     // Custom Metrics Pipeline base
├── UsecaseValidationPipeline.cs           // FluentValidation Pipeline
├── UsecaseExceptionPipeline.cs            // Exception → Fin.Fail conversion Pipeline
├── UsecaseTransactionPipeline.cs          // Transaction + SaveChanges + event publishing Pipeline
└── UsecaseCachingPipeline.cs              // IMemoryCache-based caching Pipeline
```

### ObservabilityNaming Constants

The `ObservabilityNaming` class defines all observability-related constants.

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
    /// OpenTelemetry standard attributes
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
    /// Custom attributes (request.*, response.*, error.*)
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

### Practical Examples

#### Configurator Implementation

```csharp
/// <summary>
/// Configurator class for Tracing extension configuration
/// Provides project-specific Tracing extensions such as ActivitySource, Processor, etc.
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

#### Pipeline Implementation

```csharp
/// <summary>
/// Pipeline responsible for Usecase Logging
/// Safely logs requests/responses using the Result pattern.
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
        // Request logging
        _logger.LogUsecaseRequest(...);

        // Execute next Pipeline
        TResponse response = await next(request, cancellationToken);

        // Response logging
        _logger.LogUsecaseResponseSuccess(...);

        return response;
    }
}
```

#### Builder Usage

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

#### Options Configuration (appsettings.json)

```json
{
  "OpenTelemetry": {
    "ServiceName": "MyService",
    "CollectorEndpoint": "http://localhost:18889",

    // Individual endpoints (optional)
    "LoggingEndpoint": "http://localhost:21892",
    "TracingEndpoint": "http://localhost:21890",
    "MetricsEndpoint": "http://localhost:21891",

    // Individual protocols (optional)
    "LoggingProtocol": "HttpProtobuf",
    "TracingProtocol": "Grpc",
    "MetricsProtocol": "Grpc"
  }
}
```

### Checklist

When writing new Observability-related classes, verify the following:

- [ ] Are you using Signal names? → `Logging`, `Tracing`, `Metrics`
- [ ] Are you using Component types? → `Logger`, `Span`, `Metric`, `Tracer`, `Meter`
- [ ] Are Signal names used as prefixes? (configuration/activity)
- [ ] Are Component types used as suffixes? (components)
- [ ] Does it not conflict with `.gitignore`? (avoid `Logs`)
- [ ] Does it match OpenTelemetry standard terminology?
- [ ] Do all three Signals follow a consistent pattern?
- [ ] Are both Endpoint and Protocol in gerund form?
- [ ] Does Configurator use Tracing? (TracesConfigurator ❌)

### Terminology Reference Table

| Purpose | Logging | Tracing | Metrics | Note |
|------|---------|---------|---------|------|
| **Configurator** | `LoggingConfigurator` | `TracingConfigurator` | `MetricsConfigurator` | Internal consistency first |
| **Endpoint** | `LoggingEndpoint` | `TracingEndpoint` | `MetricsEndpoint` | Gerund form |
| **Protocol** | `LoggingProtocol` | `TracingProtocol` | `MetricsProtocol` | Gerund form |
| **Pipeline** | `UsecaseLoggingPipeline` | `UsecaseTracingPipeline` | `UsecaseMetricsPipeline` | Emphasizes activity |
| **Builder Method** | `ConfigureLogging()` | `ConfigureTracing()` | `ConfigureMetrics()` | Maintain consistency |
| **Getter Method** | `GetLoggingEndpoint()` | `GetTracingEndpoint()` | `GetMetricsEndpoint()` | Gerund form |
| **Getter Method** | `GetLoggingProtocol()` | `GetTracingProtocol()` | `GetMetricsProtocol()` | Gerund form |

---

> For field/tag naming conventions, see [08-observability.md](../../spec/08-observability).

The code naming conventions defined naming patterns for classes, interfaces, pipelines, etc. Next, let's look at the separate naming pattern applied to LoggerExtensions methods.

## Logger Method Naming Convention

These are the rules to follow when naming log methods in LoggerExtensions classes.

### Naming Pattern

```
Log{Context}{Phase}{Status}
```

### Components

| Element | Description | Value |
|------|------|-----|
| `Context` | Logging target context | `Usecase`, `DomainEventHandler`, `DomainEventPublisher`, `DomainEventsPublisher` |
| `Phase` | Request/response stage | `Request`, `Response` |
| `Status` | Result status (used only for Response) | (none), `Success`, `Warning`, `Error`, `PartialFailure` |

### Rules

#### Phase Rules
- `Request`: Log at the start of an operation (no Status)
- `Response`: Log at the completion of an operation (Status required)

#### Status Rules
| Status | Log Level | Purpose |
|--------|-----------|------|
| `Success` | Information | Normal completion |
| `Warning` | Warning | Expected Error |
| `Error` | Error | Exceptional Error |
| `PartialFailure` | Warning | Partial failure (only some succeeded) |

#### Context Rules
- Singular/plural distinction: Use singular for single items, plural for multiple items
  - `DomainEventPublisher`: Publishing a single event
  - `DomainEventsPublisher`: Publishing multiple events (all events from an Aggregate)

### Examples

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
LogDomainEventHandlerResponseError(...)   // Error parameter
LogDomainEventHandlerResponseError(...)   // Exception parameter (overload)
```

#### DomainEventPublisherLoggerExtensions
```csharp
// Single event
LogDomainEventPublisherRequest<TEvent>(...)
LogDomainEventPublisherResponseSuccess<TEvent>(...)
LogDomainEventPublisherResponseWarning<TEvent>(...)
LogDomainEventPublisherResponseError<TEvent>(...)

// Multiple events (Aggregate)
LogDomainEventsPublisherRequest(...)
LogDomainEventsPublisherResponseSuccess(...)
LogDomainEventsPublisherResponseWarning(...)
LogDomainEventsPublisherResponseError(...)
LogDomainEventsPublisherResponsePartialFailure(...)
```

### Anti-Patterns

| Incorrect | Correct | Reason |
|-----------|-----------|------|
| `LogRequestMessage` | `LogUsecaseRequest` | Missing Context |
| `LogDomainEventHandlerSuccess` | `LogDomainEventHandlerResponseSuccess` | Missing Phase |
| `LogDomainEventPublish` | `LogDomainEventPublisherRequest` | Use Phase instead of action |
| `LogResponseMessageSuccess` | `LogUsecaseResponseSuccess` | "Message" suffix unnecessary |

### When Adding a New LoggerExtensions Class

1. Determine the Context name (e.g., `Repository`, `ExternalApi`)
2. Determine the required Phase (`Request`, `Response`, or both)
3. Determine the required Status (`Success`, `Warning`, `Error`)
4. Write method names according to the pattern

The following summarizes common issues and solutions encountered when applying naming conventions.

## Troubleshooting

### Class names are inconsistent due to confusion between Signal names and Component types

**Cause:** The role distinction between `Logging` (Signal/activity) and `Logger` (Component/object) is not clear, resulting in a mix of `LoggingPipeline` and `LoggerPipeline`.

**Resolution:** Use Signal names as prefixes for configuration/activity classes (`LoggingConfigurator`, `UsecaseLoggingPipeline`), and Component types as suffixes for components (`StartupLogger`, `ISpanFactory`).

### Mixed usage of `.count` and `_count` in count fields

**Cause:** The rules for standalone count and adjective-combined count were not distinguished.

**Resolution:** Use `.count` for entity-level counts (`request.event.count`, `request.aggregate.count`), and `_count` suffix for dynamic field counts (`request.params.{name}_count`, `response.event.success_count`, `response.event.failure_count`).

## FAQ

### Q1. OpenTelemetry uses "Traces", so why does Functorium use "Tracing"?

Functorium follows the **internal consistency first principle**. Consistently applying the gerund pattern across all Signals like `LoggingConfigurator`, `TracingConfigurator`, `MetricsConfigurator` contributes more to codebase readability and maintainability.

### Q2. How do you distinguish singular/plural in Logger methods?

Use singular for single item processing and plural for multiple item processing. Example: `LogDomainEventPublisherRequest` (publishing a single event), `LogDomainEventsPublisherRequest` (publishing all events from an Aggregate).

### Q3. Why use gerunds in Endpoint and Protocol property names?

Gerund forms are natural since they represent configuration/setup activities. `LoggingEndpoint` reads as "endpoint for logging", `TracingProtocol` reads as "protocol for tracing". All three Signals maintain consistency with the same pattern (`LoggingEndpoint`, `TracingEndpoint`, `MetricsEndpoint`).

### Q4. Why do folders use plural forms?

Folders express "what they contain". `Loggers/` contains Logger-related classes, `Spans/` contains Span-related classes. This is consistent with common conventions in .NET projects.

### Q5. What order should you follow when adding a new LoggerExtensions class?

1) Determine the Context name (e.g., `Repository`, `ExternalApi`), 2) Determine the required Phase (`Request`, `Response`), 3) Determine the required Status (`Success`, `Warning`, `Error`), 4) Write method names using the `Log{Context}{Phase}{Status}` pattern.

## References

- [08-observability.md](../../spec/08-observability) — Observability specification (Field/Tag, Meter, message templates)
- [19-observability-logging.md](../19-observability-logging) — Observability logging details
- [20-observability-metrics.md](../20-observability-metrics) — Observability metrics details
- [21-observability-tracing.md](../21-observability-tracing) — Observability tracing details
- [OpenTelemetry Specification](https://opentelemetry.io/docs/specs/)
- [OpenTelemetry Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/)
- [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet)
