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

### Field/Tag Consistency

**Application Layer:**

| Field/Tag | Logging | Metrics | Tracing | Note |
|-----------|---------|---------|---------|------|
| `request.layer` | ✅ | ✅ | ✅ | Consistent |
| `request.category` | ✅ | ✅ | ✅ | Consistent |
| `request.handler.cqrs` | ✅ | ✅ | ✅ | Consistent |
| `request.handler` | ✅ | ✅ | ✅ | Consistent |
| `request.handler.method` | ✅ `"Handle"` | ✅ `"Handle"` | ✅ `"Handle"` | Consistent |
| `response.status` | ✅ | ✅ | ✅ | Consistent |
| `response.elapsed` | ✅ | - | ✅ | Consistent |
| `error.type` | ❌ (`@error`) | ✅ | ✅ | Logging uses `@error` object |
| `error.code` | ❌ (`@error`) | ✅ | ✅ | Logging uses `@error` object |
| `slo.latency` | ❌ | ✅ | ❌ | Metrics only |

**Adapter Layer:**

| Field/Tag | Logging | Metrics | Tracing | Note |
|-----------|---------|---------|---------|------|
| `request.layer` | ✅ | ✅ | ✅ | Consistent |
| `request.category` | ✅ | ✅ | ✅ | Consistent |
| `request.handler` | ✅ | ✅ | ✅ | Consistent |
| `request.handler.method` | ✅ | ✅ | ✅ | Consistent |
| `response.status` | ✅ | ✅ | ✅ | Consistent |
| `response.elapsed` | ✅ | - | ✅ | Consistent |

### Logging

**Field Structure:**

| Field Name | Application Layer | Adapter Layer | Description |
|------------|-------------------|---------------|-------------|
| **Static Fields** | | | |
| `request.layer` | `"application"` | `"adapter"` | Request layer identifier |
| `request.category` | `"usecase"` | Adapter category name | Request category identifier |
| `request.handler.cqrs` | `"command"` / `"query"` | - | CQRS type |
| `request.handler` | Handler name | Handler name | Handler class name |
| `request.handler.method` | `"Handle"` | Method name | Handler method name |
| `response.status` | `"success"` / `"failure"` | `"success"` / `"failure"` | Response status |
| `response.elapsed` | Processing time (ms) | Processing time (ms) | Elapsed time |
| `@error` | Error object (structured) | Error object (structured) | Error data |
| **Dynamic Fields** | | | |
| `@request.message` | Full Command/Query object | - | Request message |
| `@response.message` | Full response object | - | Response message |
| `request.params.{name}` | - | Individual method parameter | Request params |
| `request.params.{name}.count` | - | Collection size (when parameter is collection) | Request params count |
| `response.result` | - | Method return value | Response result |
| `response.result.count` | - | Collection size (when return is collection) | Response result count |

**Log Level by Event:**

| Event | Log Level | Application Layer | Adapter Layer | Description |
|-------|-----------|-------------------|---------------|-------------|
| Request | Information | 1001 `application.request` | 2001 `adapter.request` | Request received |
| Request (Debug) | Debug | - | 2001 `adapter.request` | Request with parameter values |
| Response Success | Information | 1002 `application.response.success` | 2002 `adapter.response.success` | Successful response |
| Response Success (Debug) | Debug | - | 2002 `adapter.response.success` | Response with result value |
| Response Warning | Warning | 1003 `application.response.warning` | 2003 `adapter.response.warning` | Expected error (business logic) |
| Response Error | Error | 1004 `application.response.error` | 2004 `adapter.response.error` | Exceptional error (system failure) |

**Message Templates (Application Layer):**

```
# Request
{request.layer} {request.category}.{request.handler.cqrs} {request.handler}.{request.handler.method} {@request.message} requesting

# Response - Success
{request.layer} {request.category}.{request.handler.cqrs} {request.handler}.{request.handler.method} {@response.message} responded {response.status} in {response.elapsed:0.0000} ms

# Response - Warning/Error
{request.layer} {request.category}.{request.handler.cqrs} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} ms with {@error}
```

**Message Templates (Adapter Layer):**

```
# Request (Information)
{request.layer} {request.category} {request.handler}.{request.handler.method} requesting

# Request (Debug) - with parameters
{request.layer} {request.category} {request.handler}.{request.handler.method} {request.params.items} {request.params.items.count} requesting

# Response (Information)
{request.layer} {request.category} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} ms

# Response (Debug) - with result
{request.layer} {request.category} {request.handler}.{request.handler.method} {response.result} responded {response.status} in {response.elapsed:0.0000} ms

# Response Warning/Error
{request.layer} {request.category} {request.handler}.{request.handler.method} responded failure in {response.elapsed:0.0000} ms with {@error}
```

**Implementation:**

| Layer | Method | Note |
|-------|--------|------|
| Application | Direct `ILogger.LogXxx()` calls | 7+ parameters exceed `LoggerMessage.Define` limit of 6 |
| Adapter | `LoggerMessage.Define` delegates | Zero allocation, high performance |

### Metrics

**Instrument Structure:**

| Instrument | Application Layer | Adapter Layer | Type | Unit | Description |
|------------|-------------------|---------------|------|------|-------------|
| requests | `application.usecase.{cqrs}.requests` | `adapter.{category}.requests` | Counter | `{request}` | Total request count |
| responses | `application.usecase.{cqrs}.responses` | `adapter.{category}.responses` | Counter | `{response}` | Response count |
| duration | `application.usecase.{cqrs}.duration` | `adapter.{category}.duration` | Histogram | `s` | Processing time (seconds) |

**Tag Structure (Application Layer):**

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

**Tag Structure (Adapter Layer):**

| Tag Key | requestCounter | durationHistogram | responseCounter (success) | responseCounter (failure) |
|---------|----------------|-------------------|---------------------------|---------------------------|
| `request.layer` | `"adapter"` | `"adapter"` | `"adapter"` | `"adapter"` |
| `request.category` | category name | category name | category name | category name |
| `request.handler` | handler name | handler name | handler name | handler name |
| `request.handler.method` | method name | method name | method name | method name |
| `response.status` | - | - | `"success"` | `"failure"` |
| `error.type` | - | - | - | `"expected"` / `"exceptional"` / `"aggregate"` |
| `error.code` | - | - | - | Error code |
| **Total Tags** | **4** | **4** | **5** | **7** |

**Error Type Tag Values:**

| Error Case | error.type | error.code |
|---------|----------------|----------------------|
| `ErrorCodeExpected` | `"expected"` | Error code (e.g., DomainErrors.City.Empty) |
| `ErrorCodeExceptional` | `"exceptional"` | Error code (e.g., InfraErrors.Database.Timeout) |
| `ManyErrors` | `"aggregate"` | Primary error code (Exceptional takes priority) |
| Other `Expected` | `"expected"` | Type name |
| Other `Exceptional` | `"exceptional"` | Type name |

- `IHasErrorCode`: ErrorCodeExpected, ErrorCodeExceptional

**SLO Latency Tag Values (Application Layer only, 3-tier severity):**

| Value | Condition | Description |
|-------|-----------|-------------|
| `"ok"` | `elapsed <= P95` | Normal (within target) |
| `"p95_exceeded"` | `P95 < elapsed <= P99` | Warning (slow) |
| `"p99_exceeded"` | `elapsed > P99` | Critical (SLO violation) |

**Implementation:**

| Layer | Method | Note |
|-------|--------|------|
| Application | `IPipelineBehavior` + `IMeterFactory` | MediatR pipeline |
| Adapter | Source Generator | Auto-generated metrics instruments |

### SLO Configuration (Application Layer)

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

### Tracing

**Span Structure:**

| Property | Application Layer | Adapter Layer |
|----------|-------------------|---------------|
| Span Name | `{layer} {category}.{cqrs} {handler}.{method}` | `{layer} {category} {handler}.{method}` |
| Example | `application usecase.command CreateOrderCommandHandler.Handle` | `adapter Repository OrderRepository.GetById` |
| Kind | `Internal` | `Internal` |

**Tag Structure:**

| Tag Key | Application Layer | Adapter Layer | Description |
|---------|-------------------|---------------|-------------|
| **Request Tags** | | | |
| `request.layer` | `"application"` | `"adapter"` | Layer identifier |
| `request.category` | `"usecase"` | Category name | Category identifier |
| `request.handler.cqrs` | `"command"` / `"query"` | - | CQRS type |
| `request.handler` | Handler name | Handler name | Handler class name |
| `request.handler.method` | `"Handle"` | Method name | Method name |
| **Response Tags** | | | |
| `response.status` | `"success"` / `"failure"` | `"success"` / `"failure"` | Response status |
| `response.elapsed` | Processing time (ms) | Processing time (ms) | Elapsed time |
| **Error Tags** | | | |
| `error.type` | `"expected"` / `"exceptional"` / `"aggregate"` | `"expected"` / `"exceptional"` / `"aggregate"` | Error classification |
| `error.code` | Error code | Error code | Error code |
| **ActivityStatus** | `Ok` / `Error` | `Ok` / `Error` | OpenTelemetry status |

**Error Type Tag Values:**

| Error Case | error.type | Additional Tags | Description |
|------------|------------|-----------------|-------------|
| `IHasErrorCode` + `IsExpected` | `"expected"` | `error.code` | Expected business logic error with error code |
| `IHasErrorCode` + `IsExceptional` | `"exceptional"` | `error.code` | Exceptional system error with error code |
| `ManyErrors` | `"aggregate"` | `error.code` | Multiple errors aggregated (primary error code) |
| Other | Type name | - | Unknown error type fallback |

**Implementation:**

| Layer | Method | Note |
|-------|--------|------|
| Application | `IPipelineBehavior` + `ActivitySource.StartActivity()` | Mediator pipeline |
| Adapter | Source Generator | Auto-generated Activity spans |

## Framework
### Abstractions
- [x] Structured Error
- [ ] Dependency Registration

### Adapter Layer
- [x] Configuration Option
- [x] Observability(Logging, Tracing, Metrics)
- [ ] Scheduling
- [ ] HTTP Method
- [ ] MQ
- [ ] ORM

### Application Layer
- [x] CQRS
- [x] Pipeline
- [x] IAdapter(Observability)
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
