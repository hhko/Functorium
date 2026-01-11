# Functorium
[![Build](https://github.com/hhko/Functorium/actions/workflows/build.yml/badge.svg)](https://github.com/hhko/Functorium/actions/workflows/build.yml) [![Publish](https://github.com/hhko/Functorium/actions/workflows/publish.yml/badge.svg)](https://github.com/hhko/Functorium/actions/workflows/publish.yml)

> A functional domain = **functor** + domin**ium** with **fun**.

Functorium is a C# framework for implementing **Domain-Centric Functional Architecture**.

It enables expressing domain logic as pure functions and pushing side effects to architectural boundaries, allowing you to write **testable and predictable business logic**. The framework provides a functional type system based on LanguageExt 5.x and integrated observability through OpenTelemetry.

### Core Principles

| Principle | Description | Functorium Support |
|-----------|-------------|-------------------|
| **Domain First** | Domain model is the center of architecture | Value Object hierarchy, immutable domain types |
| **Pure Core** | Business logic expressed as pure functions | `Fin<T>` return type, exception-free error handling |
| **Impure Shell** | Side effects handled at boundary layers | Adapter Pipeline, ActivityContext propagation |
| **Explicit Effects** | All effects explicitly typed | `FinResponse<T>`, `FinT<IO, T>` monad |

## Book
- [Architecture](./Docs/ArchitectureIs/README.md)
- [Automating Release Notes with Claude Code and .NET 10](./Books/Automating-ReleaseNotes-with-ClaudeCode-and-.NET10/README.md)
- [Automating Observability Code with SourceGenerator](./Books/Automating-ObservabilityCode-with-SourceGenerator/README.md)
- [Implementing Functional ValueObject](./Books/Implementing-Functional-ValueObject/README.md)

## Observability

> All observability fields use `snake_case + dot` notation for consistency with OpenTelemetry semantic conventions.

### Logging (UsecaseLoggingPipeline) - Application Layer

**Field Structure:**

| Category | Field Name | Description |
|----------|------------|-------------|
| **Static Fields** | | |
| Request Layer | `request.layer` | `"application"` |
| Request Category | `request.category` | `"usecase"` |
| Request CQRS | `request.handler.cqrs` | `"command"` / `"query"` |
| Request Handler | `request.handler` | Handler name |
| Request Method | `request.handler.method` | `"Handle"` |
| Response Status | `status` | `"success"` / `"failure"` |
| Elapsed Time | `elapsed` | Processing time (ms) |
| Error Data | `@error` | Error object (structured) |
| **Dynamic Fields** | | |
| Request Message | `@request.message` | Full Command/Query object |
| Response Message | `@response.message` | Full response object |

**Log Level by Event:**

| Event | Log Level | EventId | Description |
|-------|-----------|---------|-------------|
| Request | Information | `ApplicationRequest` | Request received |
| Response Success | Information | `ApplicationResponseSuccess` | Successful response |
| Response Warning | Warning | `ApplicationResponseWarning` | Expected error (business logic) |
| Response Error | Error | `ApplicationResponseError` | Exceptional error (system failure) |

**Message Templates:**

```
# Request
{request.layer} {request.category}.{request.handler.cqrs} {request.handler}.{request.handler.method} {@request.message} requesting

# Response - Success
{request.layer} {request.category}.{request.handler.cqrs} {request.handler}.{request.handler.method} {@response.message} responded {status} in {elapsed:0.0000} ms

# Response - Warning/Error
{request.layer} {request.category}.{request.handler.cqrs} {request.handler}.{request.handler.method} responded {status} in {elapsed:0.0000} ms with {@error}
```

**Implementation:** Direct `ILogger.LogXxx()` calls (7+ parameters exceed `LoggerMessage.Define` limit of 6)

### Logging (AdapterPipelineGenerator) - Adapter Layer

**Field Structure:**

| Category | Field Name | Description |
|----------|------------|-------------|
| **Static Fields** | | |
| Request Layer | `request.layer` | `"adapter"` |
| Request Category | `request.category` | Adapter category name |
| Request Handler | `request.handler` | Handler name |
| Request Method | `request.handler.method` | Method name |
| Response Status | `status` | `"success"` / `"failure"` |
| Elapsed Time | `elapsed` | Processing time (ms) |
| Error Data | `@error` | Error object (structured) |
| **Dynamic Fields** | | |
| Request Params | `request.params.{name}` | Individual method parameter |
| Request Params Count | `request.params.{name}.count` | Collection size (when parameter is collection) |
| Response Result | `response.result` | Method return value |
| Response Result Count | `response.result.count` | Collection size (when return is collection) |

**Log Level by Event:**

| Event | Log Level | EventId | Description |
|-------|-----------|---------|-------------|
| Request | Information | `AdapterRequest` | Request received |
| Request (Debug) | Debug | `AdapterRequest` | Request with parameter values |
| Response Success | Information | `AdapterResponseSuccess` | Successful response |
| Response Success (Debug) | Debug | `AdapterResponseSuccess` | Response with result value |
| Response Warning | Warning | `AdapterResponseWarning` | Expected error |
| Response Error | Error | `AdapterResponseError` | Exceptional error |

**Message Templates:**

```
# Request (Information)
{request.layer} {request.category} {request.handler}.{request.handler.method} requesting

# Request (Debug) - with parameters
{request.layer} {request.category} {request.handler}.{request.handler.method} {request.params.items} {request.params.items.count} requesting

# Response (Information)
{request.layer} {request.category} {request.handler}.{request.handler.method} responded {status} in {elapsed:0.0000} ms

# Response (Debug) - with result
{request.layer} {request.category} {request.handler}.{request.handler.method} {response.result} responded {status} in {elapsed:0.0000} ms

# Response Warning/Error
{request.layer} {request.category} {request.handler}.{request.handler.method} responded failure in {elapsed:0.0000} ms with {@error}
```

**Implementation:** `LoggerMessage.Define` delegates (zero allocation, high performance)

### Metrics (UsecaseMetricsPipeline) - Application Layer

**Instrument Structure:**

| Instrument Name | Type | Unit | Description |
|------------|------|------|------|
| `application.usecase.{cqrs}.requests` | Counter | `{request}` | Total request count |
| `application.usecase.{cqrs}.responses` | Counter | `{response}` | Response count (distinguished by status tag) |
| `application.usecase.{cqrs}.duration` | Histogram | `s` | Processing time (seconds) |

**Tag Structure:**

| Tag Key | requestCounter | durationHistogram | responseCounter (success) | responseCounter (failure) |
|---------|----------------|-------------------|---------------------------|---------------------------|
| `request.layer` | `"application"` | `"application"` | `"application"` | `"application"` |
| `request.category` | `"usecase"` | `"usecase"` | `"usecase"` | `"usecase"` |
| `request.handler.cqrs` | `"command"` / `"query"` | `"command"` / `"query"` | `"command"` / `"query"` | `"command"` / `"query"` |
| `request.handler` | handler name | handler name | handler name | handler name |
| `request.handler.method` | `"Handle"` | `"Handle"` | `"Handle"` | `"Handle"` |
| `response.status` | - | - | `"success"` | `"failure"` |
| `error.type` | - | - | - | `"expected"` / `"exceptional"` / `"aggregate"` |
| `error.code` | - | - | - | Primary error code |
| `slo.latency` | - | `"ok"` / `"p95_exceeded"` / `"p99_exceeded"` | `"ok"` / `"p95_exceeded"` / `"p99_exceeded"` | `"ok"` / `"p95_exceeded"` / `"p99_exceeded"` |
| **Total Tags** | **5** | **6** | **7** | **9** |

**Error Type Tag Values:**

| Error Case | error.type | error.code |
|---------|----------------|----------------------|
| `ErrorCodeExpected` | "expected" | Error code (e.g., DomainErrors.City.Empty) |
| `ErrorCodeExceptional` | "exceptional" | Error code (e.g., InfraErrors.Database.Timeout) |
| `ManyErrors` | "aggregate" | Primary error code (Exceptional takes priority) |
| Other `Expected` | "expected" | Type name |
| Other `Exceptional` | "exceptional" | Type name |

- `IHasErrorCode`: ErrorCodeExpected, ErrorCodeExceptional

**SLO Latency Tag Values (3-tier severity):**

| Value | Condition | Description |
|-------|-----------|-------------|
| `"ok"` | `elapsed <= P95` | Normal (within target) |
| `"p95_exceeded"` | `P95 < elapsed <= P99` | Warning (slow) |
| `"p99_exceeded"` | `elapsed > P99` | Critical (SLO violation) |

### SLO Configuration

**Purpose:** Service Level Objective (SLO) configuration for monitoring and alerting.

**Configuration Structure:**

```csharp
public class SloConfiguration
{
    public SloTargets GlobalDefaults { get; set; }          // Global default SLO targets
    public CqrsSloDefaults CqrsDefaults { get; set; }       // CQRS type-specific defaults
    public Dictionary<string, SloTargets> HandlerOverrides; // Handler-specific overrides
    public double[] HistogramBuckets { get; set; }          // Custom histogram bucket boundaries
}

public class SloTargets
{
    public double? AvailabilityPercent { get; set; } = 99.9;      // Availability SLO (%)
    public double? LatencyP95Milliseconds { get; set; } = 500;    // P95 latency target (ms)
    public double? LatencyP99Milliseconds { get; set; } = 1000;   // P99 latency target (ms)
    public int? ErrorBudgetWindowDays { get; set; } = 30;         // Error budget window (days)
}
```

**Default Values:**

| CQRS Type | Availability | Latency P95 | Latency P99 | Error Budget Window |
|-----------|--------------|-------------|-------------|---------------------|
| **Command** | 99.9% | 500ms | 1000ms | 30 days |
| **Query** | 99.5% | 200ms | 500ms | 30 days |

**appsettings.json Configuration:**

```json
{
  "Observability": {
    "Slo": {
      "GlobalDefaults": {
        "AvailabilityPercent": 99.9,
        "LatencyP95Milliseconds": 500,
        "LatencyP99Milliseconds": 1000
      },
      "HandlerOverrides": {
        "CreateOrderCommand": {
          "LatencyP95Milliseconds": 600
        }
      },
      "HistogramBuckets": [0.001, 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10]
    }
  }
}
```

**Priority Order:**
1. HandlerOverrides (highest)
2. CqrsDefaults (Command/Query)
3. GlobalDefaults (fallback)

**Default Histogram Buckets (seconds):**
```
[0.001, 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10]
```
- Covers 1ms to 10s range
- Aligned with SLO thresholds (500ms, 1s)
- Higher density in critical range (1ms-1s)

**Histogram Bucket Configuration:**

The `HistogramBuckets` setting configures bucket boundaries for existing duration histograms via `OpenTelemetryBuilder.AddView()`:

| Target Instrument | Effect |
|-------------------|--------|
| `application.usecase.command.duration` | Custom bucket boundaries applied |
| `application.usecase.query.duration` | Custom bucket boundaries applied |

> **Note:** SLO configuration does not add new instruments or tags. It only configures histogram bucket boundaries for accurate P95/P99 percentile calculations. SLO target values (AvailabilityPercent, LatencyP95Milliseconds, etc.) are used as query thresholds in Prometheus/Grafana, not emitted as metrics.

### Metrics (AdapterPipelineGenerator) - Adapter Layer

**Instrument Structure:**

| Instrument Name | Type | Unit | Description |
|------------|------|------|------|
| `adapter.{category}.requests` | Counter | `{request}` | Total request count |
| `adapter.{category}.responses.success` | Counter | `{response}` | Successful response count |
| `adapter.{category}.responses.failure` | Counter | `{response}` | Failed response count |
| `adapter.{category}.duration` | Histogram | `s` | Processing time (seconds) |

**Tag Structure:**

| Tag Key | requestCounter | durationHistogram | successCounter | failureCounter |
|---------|----------------|-------------------|----------------|----------------|
| `request.layer` | `"adapter"` | `"adapter"` | `"adapter"` | `"adapter"` |
| `request.category` | category name | category name | category name | category name |
| `request.handler` | handler name | handler name | handler name | handler name |
| `request.handler.method` | method name | method name | method name | method name |
| **Total Tags** | **4** | **4** | **4** | **4** |

**Implementation:** Source Generator creates metrics instruments and records values automatically.

## Framework
### Abstractions
- [x] Structured Error
- [ ] Dependency Registration

### Adapter Layer
- [x] Configuration Option
- [ ] Observability(Logging, Tracing, Metrics)
- [ ] Scheduling
- [ ] HTTP Method
- [ ] MQ
- [ ] ORM

### Application Layer
- [x] CQRS
- [x] Pipeline
- [ ] IAdapter(Observability)
- [ ] Usecase(LINQ: FinT, Fin, IO, Guard, Validation)

### Domain Layer
- [x] Value Object
- [ ] Entity

## Infrastructure
### AI
- [x] Commit
- [x] Release Note
- [ ] Sprint Plan

### Testing
- [x] MTP(Microsoft Testing Platform)
- [x] Code Coverage
- [x] Architecture Testing
- [x] Snapshot Testing
- [ ] Container Testing
- [ ] Performance Testing

### CI/CD
- [ ] Versioning
- [x] Local Build
- [x] Remote Build
- [ ] Remote Deployment

### System
- [ ] Observability
