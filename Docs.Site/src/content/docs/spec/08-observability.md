---
title: "Observability Specification"
---

Defines the Functorium framework's Observability field/tag specification, Meter definition rules, and message template patterns. For Pipeline execution order, OpenTelemetryOptions settings, and custom extension points, see the [Pipeline Specification](../07-pipeline).

## Summary

### Key Concepts

| Concept | Description |
|------|------|
| Service Attributes | `service.namespace`, `service.name`, etc. for OpenTelemetry standard service identification |
| Error Classification | `expected` (business error), `exceptional` (system error), `aggregate` (composite error) |
| 3-Pillar Field/Tag | `request.*`, `response.*`, `error.*` fields used identically across Logging, Metrics, and Tracing |
| Meter Name | `{service.namespace}.{layer}[.{category}]` pattern |
| Instrument | `requests` (Counter), `responses` (Counter), `duration` (Histogram) |
| Message Template | Structured log message format per layer and Event ID scheme (Application 1001-1004, Adapter 2001-2004) |
| Span Name | `{layer} {category}[.{type}] {handler}.{method}` |
| ctx.* Context Fields | Source Generator automatically converts Request/Response/DomainEvent properties to `ctx.{snake_case}` fields. Pillar targeting via `[CtxTarget]` (default: Logging + Tracing, Metrics is opt-in) |

---

## Common Specifications

### Service Attributes

Functorium uses [OpenTelemetry Service Attributes](https://opentelemetry.io/docs/specs/semconv/registry/attributes/service/) for service identification.

| Attribute | Description | Example |
|-----------|------|------|
| `service.namespace` | Namespace of `service.name`. Helps distinguish service groups (e.g., by team or environment). | `mycompany.production` |
| `service.name` | Logical name of the service. Must be identical across all horizontally scaled instances. | `orderservice` |
| `service.version` | Version string of the service API or implementation. | `2.0.0` |
| `service.instance.id` | Unique ID of the service instance. Must be globally unique per `service.namespace,service.name` pair. Uses `HOSTNAME` environment variable when available, otherwise falls back to `Environment.MachineName`. | `my-pod-abc123` (Kubernetes), `DESKTOP-ABC123` (Windows) |
| `deployment.environment` | Attribute identifying the deployment environment. | `production`, `staging` |

> **Recommended**: Use lowercase values for `service.name` and `service.namespace` (e.g., `mycompany.production`, `orderservice`).
> This ensures consistency with OpenTelemetry conventions and prevents case-sensitivity issues in downstream systems (dashboards, queries, alerts).

### Error Classification

#### Error Type Tag Values

The following table summarizes how `error.type` and `error.code` tag values are determined based on the error cause.

| Error Case | error.type | error.code | Description |
|------------|------------|------------|------|
| `IHasErrorCode` + `IsExpected` | `"expected"` | Error code | Expected business logic error with error code |
| `IHasErrorCode` + `IsExceptional` | `"exceptional"` | Error code | Exceptional system error with error code |
| `ManyErrors` | `"aggregate"` | Primary error code | Multiple errors aggregated (Exceptional takes priority) |
| `Expected` (LanguageExt) | `"expected"` | Type name | LanguageExt default expected error without error code |
| `Exceptional` (LanguageExt) | `"exceptional"` | Type name | LanguageExt default exceptional error without error code |

#### Error Field Values (Logging Only)

> `error.type` and `@error.ErrorType` use different value formats for different purposes.

| Error Type | `error.type` (for filtering) | `@error.ErrorType` (for detail) |
|------------|------------------------|----------------------------|
| Expected Error | `"expected"` | `"ErrorCodeExpected"` |
| Exceptional Error | `"exceptional"` | `"ErrorCodeExceptional"` |
| Aggregate Error | `"aggregate"` | `"ManyErrors"` |
| LanguageExt Expected | `"expected"` | `"Expected"` |
| LanguageExt Exceptional | `"exceptional"` | `"Exceptional"` |

- **`error.type`**: Standardized value for log filtering/querying (consistent with Metrics/Tracing)
- **`@error.ErrorType`**: Actual class name for detailed error type identification

### Field/Tag Naming Rules

#### Notation: `snake_case + dot`

Uses `snake_case + dot` notation in compliance with OpenTelemetry semantic conventions.

```
# Correct examples
request.layer
request.category.type
request.handler.method
response.status
error.code

# Incorrect examples
requestLayer          # camelCase not allowed
request-layer         # kebab-case not allowed
REQUEST_LAYER         # UPPER_SNAKE_CASE not allowed
```

#### Hierarchy: `{namespace}.{property}`

Fields are organized in a hierarchical structure of namespace and property.

| Namespace | Description | Example |
|-------------|------|------|
| `request.*` | Request-related information | `request.layer`, `request.handler.name` |
| `response.*` | Response-related information | `response.status`, `response.elapsed` |
| `error.*` | Error-related information | `error.type`, `error.code` |

#### `count` Field Rules

| Category | Rule | Example |
|------|------|------|
| Static fields (event-related) | `.count` | `request.event.count` |
| Dynamic fields (parameter size) | `_count` | `request.params.orders_count` |
| Adjective/noun combination | `_count` | `response.event.success_count`, `response.event.failure_count` |

```
# Correct examples
request.event.count              # Static .count
response.event.success_count     # Adjective combination _count
request.params.orders_count      # Dynamic parameter _count

# Incorrect examples
response.event.success.count     # Do not use .count for combination count
request.params.orders.count      # Do not use .count for dynamic fields
```

---

## Field/Tag Consistency

### Usecase (Application/Adapter)

**Application Layer:** (Unit tests: [Logging](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseLoggingPipelineStructureTests.cs), [Metrics](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseMetricsPipelineStructureTests.cs), [Tracing](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseTracingPipelineStructureTests.cs))

| Field/Tag | Logging | Metrics | Tracing | Description |
|-----------|---------|---------|---------|------|
| `request.layer` | ✅ | ✅ | ✅ | Architecture layer (`"application"`) |
| `request.category.name` | ✅ | ✅ | ✅ | Request category (`"usecase"`) |
| `request.category.type` | ✅ | ✅ | ✅ | CQRS type (`"command"`, `"query"`) |
| `request.handler.name` | ✅ | ✅ | ✅ | Handler class name |
| `request.handler.method` | ✅ | ✅ | ✅ | Handler method name (`"Handle"`) |
| `response.status` | ✅ | ✅ | ✅ | Response status (`"success"`, `"failure"`) |
| `response.elapsed` | ✅ | -* | ✅ | Elapsed time (seconds) |
| `error.type` | ✅ | ✅ | ✅ | Error classification (`"expected"`, `"exceptional"`, `"aggregate"`) |
| `error.code` | ✅ | ✅ | ✅ | Domain-specific error code |
| `@error` | ✅ | - | - | Structured error object (detailed) |

**Adapter Layer:** (Unit tests: [Logging](../../Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/ObservableObservableSignalgingStructureTests.cs), [Metrics](../../Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/ObservablePortMetricsStructureTests.cs), [Tracing](../../Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/ObservablePortTracingStructureTests.cs))

| Field/Tag | Logging | Metrics | Tracing | Description |
|-----------|---------|---------|---------|------|
| `request.layer` | ✅ | ✅ | ✅ | Architecture layer (`"adapter"`) |
| `request.category.name` | ✅ | ✅ | ✅ | Category (e.g., `"repository"`) |
| `request.handler.name` | ✅ | ✅ | ✅ | Handler class name |
| `request.handler.method` | ✅ | ✅ | ✅ | Handler method name |
| `response.status` | ✅ | ✅ | ✅ | Response status (`"success"`, `"failure"`) |
| `response.elapsed` | ✅ | -* | ✅ | Elapsed time (seconds) |
| `error.type` | ✅ | ✅ | ✅ | Error classification (`"expected"`, `"exceptional"`, `"aggregate"`) |
| `error.code` | ✅ | ✅ | ✅ | Domain-specific error code |
| `@error` | ✅ | - | - | Structured error object (detailed) |

> **\* Why `response.elapsed` is not a Metrics tag:**
> - Metrics uses a dedicated `duration` **Histogram instrument** to capture processing time, which is the OpenTelemetry recommended approach for latency measurement.
> - Using elapsed time as a tag causes **high cardinality explosion** (each unique duration value creates a new time series, degrading metric storage and query performance).
> - Histogram provides **statistical aggregation** (percentiles, averages, counts) that is more useful for monitoring than individual elapsed values.

### DomainEvent Publisher

(Unit tests: [Logging](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventPublisherLoggingStructureTests.cs), [Metrics](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventPublisherMetricsStructureTests.cs), [Tracing](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventPublisherTracingStructureTests.cs))

> DomainEvent Publisher is classified as the Adapter layer, with `request.layer` as `"adapter"` and `request.category.name` as `"event"`.

| Field/Tag | Logging | Metrics | Tracing | Description |
|-----------|---------|---------|---------|------|
| `request.layer` | ✅ | ✅ | ✅ | Architecture layer (`"adapter"`) |
| `request.category.name` | ✅ | ✅ | ✅ | Request category (`"event"`) |
| `request.handler.name` | ✅ | ✅ | ✅ | Event type name or Aggregate type name |
| `request.handler.method` | ✅ | ✅ | ✅ | Method name (`"Publish"`, `"PublishTrackedEvents"`) |
| `request.aggregate.count` | - | - | ✅ | Number of Aggregate types (PublishTrackedEvents only) |
| `request.event.count` | ✅ | - | ✅ | Number of events in batch publishing (Aggregate only) |
| `response.status` | ✅ | ✅ | ✅ | Response status (`"success"`, `"failure"`) |
| `response.elapsed` | ✅ | -* | ✅ | Elapsed time (seconds) |
| `response.event.success_count` | ✅ | - | ✅ | Number of successful events on partial failure (Partial Failure only) |
| `response.event.failure_count` | ✅ | - | ✅ | Number of failed events on partial failure (Partial Failure only) |
| `error.type` | ✅ | ✅ | ✅ | Error classification (`"expected"`, `"exceptional"`) |
| `error.code` | ✅ | ✅ | ✅ | Domain-specific error code |
| `@error` | ✅ | - | - | Structured error object (detailed) |

### DomainEvent Handler

(Unit tests: [Logging](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerLoggingStructureTests.cs), [Metrics](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerMetricsStructureTests.cs), [Tracing](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerTracingStructureTests.cs))

> DomainEventHandler is classified as the Application layer, with `request.layer` as `"application"`, `request.category.name` as `"usecase"`, and `request.category.type` as `"event"`.

| Field/Tag | Logging | Metrics | Tracing | Description |
|-----------|---------|---------|---------|------|
| `request.layer` | ✅ | ✅ | ✅ | Architecture layer (`"application"`) |
| `request.category.name` | ✅ | ✅ | ✅ | Request category (`"usecase"`) |
| `request.category.type` | ✅ | ✅ | ✅ | CQRS type (`"event"`) |
| `request.handler.name` | ✅ | ✅ | ✅ | Handler class name |
| `request.handler.method` | ✅ | ✅ | ✅ | Method name (`"Handle"`) |
| `request.event.type` | ✅ | - | ✅ | Event type name |
| `request.event.id` | ✅ | - | ✅ | Event unique ID |
| `@request.message` | ✅ | - | - | Event object (on request) |
| `response.status` | ✅ | ✅ | ✅ | Response status (`"success"`, `"failure"`) |
| `response.elapsed` | ✅ | -* | - | Elapsed time (seconds) |
| `error.type` | ✅ | ✅ | ✅ | Error classification (`"expected"`, `"exceptional"`) |
| `error.code` | ✅ | ✅ | ✅ | Domain-specific error code |

> **Note:** DomainEventHandler's `response.elapsed` is not set on Tracing Span tags (Logging only). Since Spans inherently have their own start/end times (duration), a separate elapsed field would be redundant.
> DomainEventHandler's ErrorResponse logs the Exception object directly (instead of `@error`).
> DomainEventHandler returns `ValueTask`, so `@response.message` is not recorded.

---

## ctx.* User-Defined Context Fields (3-Pillar)

### Overview

`ctx.*` fields are user-defined context fields that simultaneously propagate business context to Logging, Tracing, and Metrics. Source Generator automatically detects public properties of Request/Response/DomainEvent and generates `IUsecaseCtxEnricher<TRequest, TResponse>` or `IDomainEventCtxEnricher<TEvent>` implementations. `CtxEnricherPipeline` runs as the first Pipeline, making ctx.* data accessible to subsequent Metrics/Tracing/Logging Pipelines.

| Item | Description |
|------|------|
| Target Pillar | **Logging + Tracing** (default). Metrics requires explicit opt-in via `[CtxTarget]` |
| Generator (Usecase) | `CtxEnricherGenerator` — detects records implementing `ICommandRequest<T>` / `IQueryRequest<T>` |
| Generator (DomainEvent) | `DomainEventCtxEnricherGenerator` — detects `T` from classes implementing `IDomainEventHandler<T>` |
| Runtime Mechanism | `CtxEnricherContext.Push(name, value, pillars)` — simultaneous propagation to Logging/Tracing/Metrics |
| Target Properties | Public scalar and collection properties (complex types are excluded) |
| Pipeline Order | `CtxEnricher → Metrics → Tracing → Logging → Validation → ... → Handler` |

### Per-Pillar Propagation Mechanism

| Pillar | Mechanism | Description |
|--------|----------|------|
| Logging | Serilog `LogContext.PushProperty` | Output as structured log field |
| Tracing | `Activity.Current?.SetTag` | Output as Span Attribute |
| MetricsTag | `MetricsTagContext` (AsyncLocal) → `TagList` merge | Added as dimension to existing Counter/Histogram |
| MetricsValue | Record value to separate Histogram instrument | Record numeric fields as targets for statistical aggregation |

### Pillar Targeting: `CtxPillar` Enum

```csharp
[Flags]
public enum CtxPillar
{
    Logging      = 1,   // Serilog LogContext
    Tracing      = 2,   // Activity.SetTag
    MetricsTag   = 4,   // TagList dimension (low cardinality only)
    MetricsValue = 8,   // Histogram value recording (numeric only)

    Default = Logging | Tracing,        // Default value
    All     = Logging | Tracing | MetricsTag,
}
```

### Field Naming Rules

| Scope | Pattern | Example (C# to ctx field) |
|------|------|----------------------|
| Usecase Request | `ctx.{containing_type}.request.{property}` | `PlaceOrderCommand.Request.CustomerId` → `ctx.place_order_command.request.customer_id` |
| Usecase Response | `ctx.{containing_type}.response.{property}` | `PlaceOrderCommand.Response.OrderId` → `ctx.place_order_command.response.order_id` |
| DomainEvent (top-level) | `ctx.{event}.{property}` | `OrderPlacedEvent.CustomerId` → `ctx.order_placed_event.customer_id` |
| DomainEvent (nested) | `ctx.{containing_type}.{event}.{property}` | `Order.CreatedEvent.OrderId` → `ctx.order.created_event.order_id` |
| Interface scope | `ctx.{interface (I removed)}.{property}` | `IRegional.RegionCode` → `ctx.regional.region_code` |
| `[CtxRoot]` promotion | `ctx.{property}` | `[CtxRoot] CustomerId` → `ctx.customer_id` |
| Collection | `above rules}_count` suffix | `List<OrderLine> Lines` → `ctx.place_order_command.request.lines_count` |

> All names are converted from PascalCase to `snake_case`. The `I` prefix is removed from interface names (`ICustomerRequest` → `customer_request`, `IX` → `x`).
> **Field names are identical across all Pillars** -- the same `ctx.*` names are used in Logging, Tracing, and Metrics.

### Code Example

```csharp
[CtxRoot]
[CtxTarget(CtxPillar.All)]                          // All properties -> 3-Pillar Tag
public interface IRegional
{
    string RegionCode { get; }                       // → ctx.region_code (Root + MetricsTag)
}

public sealed class PlaceOrderCommand
{
    public sealed record Request(
        string CustomerId,                           // Default (L+T). High cardinality
        [CtxTarget(CtxPillar.All)] bool IsExpress,   // 3-Pillar Tag. Boolean safe
        [CtxTarget(CtxPillar.Default | CtxPillar.MetricsValue)]
        int ItemCount,                               // L+T + Histogram recording
        List<OrderLine> Lines,                       // Default (L+T). Count propagation
        [CtxTarget(CtxPillar.Logging)] string InternalNote,  // Logging only
        [CtxIgnore] string DebugInfo                 // Fully excluded
    ) : ICommandRequest<Response>, IRegional;

    public sealed record Response(
        string OrderId,
        [CtxTarget(CtxPillar.Default | CtxPillar.MetricsValue)]
        decimal TotalAmount                          // L+T + Histogram recording
    );
}
```

### Field/Tag Consistency

✅ = Supported, - = Not supported/Not applicable

| ctx Field | Type | Logging | Tracing | Metrics Tag | Metrics Value |
|----------|------|---------|---------|-------------|---------------|
| `ctx.region_code` | keyword | ✅ | ✅ | **✅** | - |
| `ctx.place_order_command.request.customer_id` | keyword | ✅ | ✅ | - | - |
| `ctx.place_order_command.request.is_express` | boolean | ✅ | ✅ | **✅** | - |
| `ctx.place_order_command.request.item_count` | long | ✅ | ✅ | - | **✅** |
| `ctx.place_order_command.request.lines_count` | long | ✅ | ✅ | - | - |
| `ctx.place_order_command.request.internal_note` | keyword | ✅ | - | - | - |
| `ctx.place_order_command.response.order_id` | keyword | ✅ | ✅ | - | - |
| `ctx.place_order_command.response.total_amount` | double | ✅ | ✅ | - | **✅** |

### Type Mapping (C# to OpenSearch)

| C# Type | OpenSearch Type Group | Notes |
|---------|---------------------|------|
| `bool` | `boolean` | |
| `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong` | `long` | |
| `float`, `double`, `decimal` | `double` | |
| `string` | `keyword` | |
| `Guid`, `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `TimeOnly`, `Uri` | `keyword` | |
| `enum` | `keyword` | |
| `Option<T>` (LanguageExt) | `keyword` | |
| `IValueObject`, `IEntityId<T>` implementations | `keyword` | `.ToString()` call (DomainEvent only) |
| `Nullable<T>` | Delegates to inner `T` | |
| Collection (`List<T>`, `IReadOnlyList<T>`, etc.) | `long` | Element count value |
| Other complex types (class, record, struct) | — (excluded) | Only scalar/Collection types are targeted |

> If different type groups are assigned to the same ctx field name, OpenSearch dynamic mapping conflicts occur (compile-time diagnostic `FUNCTORIUM002`).

### Control Attributes

| Attribute | Target | Effect |
|-----------|----------|------|
| `[CtxRoot]` | `interface`, `property`, `parameter` | Promote field to `ctx.{field}` root level. Independent of Pillar targeting (affects naming only) |
| `[CtxIgnore]` | `class`, `property`, `parameter` | Excluded from all Pillars. Takes priority over `[CtxTarget]` |
| `[CtxTarget(CtxPillar)]` | `interface`, `property`, `parameter` | Specify Pillar target. Defaults to `Default` (Logging + Tracing) when unspecified |

#### Decision Flow

```
[CtxIgnore]? → YES → Excluded from all Pillars
    ↓ NO
[CtxTarget] specified? → YES → Specified Pillar
    ↓ NO
Default → CtxPillar.Default (Logging + Tracing)
```

Property/parameter level settings always take priority over interface level settings.

### Cardinality Classification Rules

| Cardinality Level | Applicable Types | MetricsTag | MetricsValue |
|---------------|----------|------------|--------------|
| `Fixed` | `bool` | Safe | Not allowed |
| `BoundedLow` | `enum` | Conditional | Not allowed |
| `Unbounded` | `string`, `Guid`, `DateTime`, `IValueObject`, `IEntityId<T>`, `Option<T>` | **Warning** (FUNCTORIUM005) | Not allowed |
| `Numeric` | `int`, `long`, `decimal`, `double` | **Warning** (FUNCTORIUM005) | Allowed |
| `NumericCount` | Collection count | **Warning** (FUNCTORIUM005) | Allowed |

### IDomainEvent Default Property Exclusion

DomainEvent Enricher automatically excludes the default properties of the `IDomainEvent` interface. These properties are already output as standard fields (such as `request.event.id`).

| Excluded Property | Reason |
|----------|------|
| `OccurredAt` | Timestamp is output separately as `@timestamp` |
| `EventId` | Output separately as `request.event.id` |
| `CorrelationId` | Managed separately as standard correlation ID field |
| `CausationId` | Managed separately as standard causation ID field |

### Safety Net

PascalCase properties pushed via `LogContext.PushProperty` without the `ctx.` prefix are automatically converted to `ctx.snake_case` by `OpenSearchJsonFormatter`.

```
CustomerId      → ctx.customer_id
OrderLineCount  → ctx.order_line_count
```

> Code generated by the Source Generator already includes the `ctx.` prefix and is therefore not subject to safety net conversion. This conversion only applies when `LogContext.PushProperty` is called manually.

### Extension Points (Partial Methods)

Generated Enricher classes are `partial class` and provide the following extension points.

#### Partial Methods

| Enricher Type | Partial Method | Call Timing |
|--------------|---------------|----------|
| Usecase | `OnEnrichRequest(request, disposables)` | At Request processing start |
| Usecase | `OnEnrichResponse(request, response, disposables)` | At Response processing completion |
| DomainEvent | `OnEnrich(domainEvent, disposables)` | Before Handler execution |

#### Helper Methods

| Enricher Type | Helper Method | Generated Field Pattern |
|--------------|---------------|------------------|
| Usecase | `PushRequestCtx(disposables, fieldName, value, pillars)` | `ctx.{type}.request.{fieldName}` |
| Usecase | `PushResponseCtx(disposables, fieldName, value, pillars)` | `ctx.{type}.response.{fieldName}` |
| DomainEvent | `PushEventCtx(disposables, fieldName, value, pillars)` | `ctx.{event}.{fieldName}` |
| Common | `PushRootCtx(disposables, fieldName, value, pillars)` | `ctx.{fieldName}` — generated only when `[CtxRoot]` attribute exists |

### Compile-Time Diagnostics

| Diagnostic Code | Severity | Condition | Description |
|----------|-------|------|------|
| `FUNCTORIUM002` | Warning | Different OpenSearch type groups assigned to same ctx field name | e.g., `ctx.customer_id` is `keyword` in enricher A, `long` in enricher B |
| `FUNCTORIUM003` | Warning | Request type has `private`/`protected` access restriction | Apply `[CtxIgnore]` to the Request record to suppress the warning |
| `FUNCTORIUM004` | Warning | Event type has `private`/`protected` access restriction | Apply `[CtxIgnore]` to the Event record to suppress the warning |
| `FUNCTORIUM005` | Warning | High cardinality type + `MetricsTag` | Cardinality explosion warning when specifying `string`/`Guid`/numeric as `MetricsTag` |
| `FUNCTORIUM006` | Error | Non-numeric type + `MetricsValue` | Error when specifying `boolean`/`keyword` as `MetricsValue` |
| `FUNCTORIUM007` | Warning | `MetricsTag` + `MetricsValue` specified simultaneously | Warning when both Tag and Value are specified on the same property |

### Integration

| Integration Point | Call Site | Description |
|----------|----------|------|
| `CtxEnricherPipeline` | Application Layer (first in pipeline) | `IUsecaseCtxEnricher<TRequest, TResponse>` DI injection. Simultaneous 3-Pillar propagation via `EnrichRequest`/`EnrichResponse` calls |
| `ObservableDomainEventNotificationPublisher` | DomainEvent Handler | Resolves `IDomainEventCtxEnricher<TEvent>` at runtime then calls `Enrich` |
| `UsecaseMetricsPipeline` | Application Layer | Reads ctx.* MetricsTag from `MetricsTagContext` and merges into existing TagList |
| `LogTestContext` | Testing | Captures and verifies ctx.* fields via `enrichFromLogContext: true` option |

---

## Logging

### Usecase Logging

#### Field Structure

| Field Name | Application Layer | Adapter Layer | Description |
|------------|-------------------|---------------|------|
| **Static Fields** | | | |
| `request.layer` | `"application"` | `"adapter"` | Request layer identifier |
| `request.category.name` | `"usecase"` | Category name | Request category identifier |
| `request.category.type` | `"command"` / `"query"` | - | CQRS type |
| `request.handler.name` | Handler name | Handler name | Handler class name |
| `request.handler.method` | `"Handle"` | Method name | Handler method name |
| `response.status` | `"success"` / `"failure"` | `"success"` / `"failure"` | Response status |
| `response.elapsed` | Elapsed time (seconds) | Elapsed time (seconds) | Elapsed time (seconds) |
| `error.type` | `"expected"` / `"exceptional"` / `"aggregate"` | `"expected"` / `"exceptional"` / `"aggregate"` | Error classification |
| `error.code` | Error code | Error code | Domain-specific error code |
| `@error` | Error object (structured) | Error object (structured) | Error data (detailed) |
| **Dynamic Fields** | | | |
| `@request.message` | Full Command/Query object | Full parameter object (Debug) | Request message |
| `@response.message` | Full response object | Method return value (Debug) | Response message |
| `@request.params` | - | Type-filtered parameter complex object (Info/Debug) | Request parameters |

#### Log Level per Event

| Event | Log Level | Application Layer | Adapter Layer | Description |
|-------|-----------|-------------------|---------------|------|
| Request | Information | 1001 `application.request` | 2001 `adapter.request` | Request received |
| Request (Debug) | Debug | - | 2001 `adapter.request` | Request with parameter values |
| Response Success | Information | 1002 `application.response.success` | 2002 `adapter.response.success` | Success response |
| Response Success (Debug) | Debug | - | 2002 `adapter.response.success` | Response with result values |
| Response Warning | Warning | 1003 `application.response.warning` | 2003 `adapter.response.warning` | Expected error (business logic) |
| Response Error | Error | 1004 `application.response.error` | 2004 `adapter.response.error` | Exceptional error (system failure) |

#### Message Templates (Application)

```
# Request
{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} requesting with {@request.message}

# Response - Success
{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {@response.message}

# Response - Warning/Error
{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}
```

#### Message Templates (Adapter)

```
# Request (Information) - 5 params
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} {@request.params} requesting

# Request (Debug) - 6 params
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} {@request.params} requesting with {@request.message}

# Response (Information) - 6 params
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s

# Response (Debug) - 7 params
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {@response.message}

# Response Warning/Error
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}
```

#### Implementation

| Layer | Approach | Testing | Notes |
|-------|------|--------|------|
| Application | Direct `ILogger.LogXxx()` calls | [UsecaseLoggingPipelineStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseLoggingPipelineStructureTests.cs) | 7+ parameters exceed `LoggerMessage.Define`'s 6-parameter limit |
| Adapter | `LoggerMessage.Define` delegates | [ObservableObservableSignalgingStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/ObservableObservableSignalgingStructureTests.cs) | Zero allocation, high performance |

### DomainEvent Logging

#### Field Comparison

**Application Usecase vs DomainEvent Publisher vs DomainEventHandler field comparison:**

✅ = Supported, ✅ (conditional) = Conditionally supported, - = Not supported/Not applicable

| Field | Application Usecase | DomainEvent Publisher | DomainEventHandler |
|-------|---------------------|----------------------|-------------------|
| `request.layer` | `"application"` | `"adapter"` | `"application"` |
| `request.category.name` | `"usecase"` | `"event"` | `"usecase"` |
| `request.category.type` | `"command"` / `"query"` | - | `"event"` |
| `request.handler.name` | Handler class name | Event/Aggregate type name | Handler class name |
| `request.handler.method` | `"Handle"` | `"Publish"` / `"PublishTrackedEvents"` | `"Handle"` |
| `@request.message` | Command/Query object | Event object | Event object |
| `@response.message` | Response object | - | - |
| `request.event.count` | - | ✅ (Aggregate only) | - |
| `response.event.success_count` | - | ✅ (Partial Failure only) | - |
| `response.event.failure_count` | - | ✅ (Partial Failure only) | - |
| `response.status` | `"success"` / `"failure"` | `"success"` / `"failure"` | `"success"` / `"failure"` |
| `response.elapsed` | Elapsed time (seconds) | Elapsed time (seconds) | Elapsed time (seconds) |
| `error.type` | `"expected"` / `"exceptional"` / `"aggregate"` | `"expected"` / `"exceptional"` | `"expected"` / `"exceptional"` |
| `error.code` | Error code | Error code | Error code |
| `@error` | Error object | Error object | Error object (Exception) |

> For error classification details, see the [Error Classification](#error-classification) section.

#### Message Templates (Publisher)

```
# Request - Single event
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} requesting with {@request.message}

# Request - Aggregate multiple events
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} requesting with {request.event.count} events

# Response - Success
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s

# Response - Success (Aggregate)
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {request.event.count} events

# Response - Warning/Error
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}

# Response - Warning/Error (Aggregate)
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {request.event.count} events with {error.type}:{error.code} {@error}

# Response - Partial Failure (Aggregate)
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {request.event.count} events partial failure: {response.event.success_count} succeeded, {response.event.failure_count} failed
```

#### Publisher Event IDs

> DomainEvent Publisher is classified as the Adapter layer, so it uses the same Event IDs as the Adapter layer.

| Event | ID | Name |
|-------|-----|------|
| Request | 2001 | `adapter.request` |
| Success | 2002 | `adapter.response.success` |
| Warning | 2003 | `adapter.response.warning` |
| Error | 2004 | `adapter.response.error` |

#### Message Templates (Handler)

> DomainEventHandler logging is from the Handler perspective, processing events published by the Publisher. `request.layer` is `"application"`, `request.category.name` is `"usecase"`, and `request.category.type` is `"event"`.

```
# Request
{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} {request.event.type} {request.event.id} requesting with {@request.message}

# Response - Success
{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} {request.event.type} {request.event.id} responded {response.status} in {response.elapsed:0.0000} s

# Response - Warning/Error
{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} {request.event.type} {request.event.id} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}
```

#### Handler Event IDs

> DomainEventHandler is classified as a usecase in the Application Layer, so it uses the same Event IDs as the Application Layer.

| Event | ID | Name |
|-------|-----|------|
| Request | 1001 | `application.request` |
| Success | 1002 | `application.response.success` |
| Warning | 1003 | `application.response.warning` |
| Error | 1004 | `application.response.error` |

#### Implementation

| Layer | Approach | Testing | Notes |
|-------|------|--------|------|
| DomainEvent Publisher | Decorator | [DomainEventPublisherLoggingStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventPublisherLoggingStructureTests.cs) | Adapter layer pattern |
| DomainEvent Handler | `INotificationPublisher` | [DomainEventHandlerLoggingStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerLoggingStructureTests.cs) | Application Layer pattern |
| DomainEvent Handler Enricher | `IDomainEventCtxEnricher<TEvent>` | [DomainEventHandlerEnricherLoggingStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerEnricherLoggingStructureTests.cs) | CtxEnricherContext-based 3-Pillar Enrichment |

---

## Metrics

### Meter Name

| Layer | Meter Name Pattern | Example (`ServiceNamespace = "mycompany.production"`) |
|-------|-----------------|---------------------------------------------------|
| Application | `{service.namespace}.application` | `mycompany.production.application` |
| Adapter | `{service.namespace}.adapter.{category}` | `mycompany.production.adapter.repository` |
| DomainEvent Publisher | `{service.namespace}.adapter.event` | `mycompany.production.adapter.event` |
| DomainEvent Handler | `{service.namespace}.application` | `mycompany.production.application` |

### Instrument Structure

| Instrument | Application Layer | Adapter Layer | DomainEvent Publisher | DomainEvent Handler | Type | Unit |
|------------|-------------------|---------------|----------------------|--------------------|------|------|
| requests | `application.usecase.{type}.requests` | `adapter.{category}.requests` | `adapter.event.requests` | `application.usecase.event.requests` | Counter | `{request}` |
| responses | `application.usecase.{type}.responses` | `adapter.{category}.responses` | `adapter.event.responses` | `application.usecase.event.responses` | Counter | `{response}` |
| duration | `application.usecase.{type}.duration` | `adapter.{category}.duration` | `adapter.event.duration` | `application.usecase.event.duration` | Histogram | `s` |

### Usecase Metrics

#### Tag Structure (Application)

| Tag Key | requestCounter | durationHistogram | responseCounter (success) | responseCounter (failure) |
|---------|----------------|-------------------|---------------------------|---------------------------|
| `request.layer` | `"application"` | `"application"` | `"application"` | `"application"` |
| `request.category.name` | `"usecase"` | `"usecase"` | `"usecase"` | `"usecase"` |
| `request.category.type` | `"command"` / `"query"` | `"command"` / `"query"` | `"command"` / `"query"` | `"command"` / `"query"` |
| `request.handler.name` | Handler name | Handler name | Handler name | Handler name |
| `request.handler.method` | `"Handle"` | `"Handle"` | `"Handle"` | `"Handle"` |
| `response.status` | - | - | `"success"` | `"failure"` |
| `error.type` | - | - | - | `"expected"` / `"exceptional"` / `"aggregate"` |
| `error.code` | - | - | - | Primary error code |
| **Total Tags** | **5** | **5** | **6** | **8** |

#### Tag Structure (Adapter)

| Tag Key | requestCounter | durationHistogram | responseCounter (success) | responseCounter (failure) |
|---------|----------------|-------------------|---------------------------|---------------------------|
| `request.layer` | `"adapter"` | `"adapter"` | `"adapter"` | `"adapter"` |
| `request.category.name` | Category name | Category name | Category name | Category name |
| `request.handler.name` | Handler name | Handler name | Handler name | Handler name |
| `request.handler.method` | Method name | Method name | Method name | Method name |
| `response.status` | - | - | `"success"` | `"failure"` |
| `error.type` | - | - | - | `"expected"` / `"exceptional"` / `"aggregate"` |
| `error.code` | - | - | - | Error code |
| **Total Tags** | **4** | **4** | **5** | **7** |

> For error classification details, see the [Error Classification](#error-classification) section.

### DomainEvent Metrics

#### Tag Structure (Publisher)

| Tag Key | requestCounter | durationHistogram | responseCounter (success) | responseCounter (failure) |
|---------|----------------|-------------------|---------------------------|---------------------------|
| `request.layer` | `"adapter"` | `"adapter"` | `"adapter"` | `"adapter"` |
| `request.category.name` | `"event"` | `"event"` | `"event"` | `"event"` |
| `request.handler.name` | Handler name | Handler name | Handler name | Handler name |
| `request.handler.method` | Method name | Method name | Method name | Method name |
| `response.status` | - | - | `"success"` | `"failure"` |
| `error.type` | - | - | - | `"expected"` / `"exceptional"` |
| `error.code` | - | - | - | Error code |
| **Total Tags** | **4** | **4** | **5** | **7** |

> **Tags excluded from DomainEvent Metrics:**
> `request.event.count`, `response.event.success_count`, `response.event.failure_count` are not used as Metrics tags.
> These values each have unique numeric values, so using them as tags would cause **high cardinality explosion**.
> This follows the same principle as not using `response.elapsed` as a Metrics tag.

#### Tag Structure (Handler)

| Tag Key | requestCounter | durationHistogram | responseCounter (success) | responseCounter (failure) |
|---------|----------------|-------------------|---------------------------|---------------------------|
| `request.layer` | `"application"` | `"application"` | `"application"` | `"application"` |
| `request.category.name` | `"usecase"` | `"usecase"` | `"usecase"` | `"usecase"` |
| `request.category.type` | `"event"` | `"event"` | `"event"` | `"event"` |
| `request.handler.name` | Handler name | Handler name | Handler name | Handler name |
| `request.handler.method` | `"Handle"` | `"Handle"` | `"Handle"` | `"Handle"` |
| `response.status` | - | - | `"success"` | `"failure"` |
| `error.type` | - | - | - | `"expected"` / `"exceptional"` |
| `error.code` | - | - | - | Error code |
| **Total Tags** | **5** | **5** | **6** | **8** |

### Implementation

| Layer | Approach | Testing | Notes |
|-------|------|--------|------|
| Application | `IPipelineBehavior` + `IMeterFactory` | [UsecaseMetricsPipelineStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseMetricsPipelineStructureTests.cs) | Mediator pipeline |
| Adapter | Source Generator | [ObservablePortMetricsStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/ObservablePortMetricsStructureTests.cs) | Auto-generated metrics instruments |
| DomainEvent Publisher | Decorator + `IMeterFactory` | [DomainEventPublisherMetricsStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventPublisherMetricsStructureTests.cs) | Adapter layer pattern |
| DomainEvent Handler | `INotificationPublisher` + `IMeterFactory` | [DomainEventHandlerMetricsStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerMetricsStructureTests.cs) | Application Layer pattern |

---

## Tracing

### Usecase Tracing

#### Span Structure

| Property | Application Layer | Adapter Layer |
|----------|-------------------|---------------|
| Span Name | `{layer} {category}.{type} {handler}.{method}` | `{layer} {category} {handler}.{method}` |
| Example | `application usecase.command CreateOrderCommandHandler.Handle` | `adapter repository OrderRepository.GetById` |
| Kind | `Internal` | `Internal` |

> **Span Name format difference:** Application Layer includes the `.{type}` segment (command/query/event), but the Adapter layer omits the `.{type}` segment since there is no CQRS type distinction.

#### Tag Structure

| Tag Key | Application Layer | Adapter Layer | Description |
|---------|-------------------|---------------|------|
| **Request Tags** | | | |
| `request.layer` | `"application"` | `"adapter"` | Layer identifier |
| `request.category.name` | `"usecase"` | Category name | Category identifier |
| `request.category.type` | `"command"` / `"query"` | - | CQRS type |
| `request.handler.name` | Handler name | Handler name | Handler class name |
| `request.handler.method` | `"Handle"` | Method name | Method name |
| **Response Tags** | | | |
| `response.status` | `"success"` / `"failure"` | `"success"` / `"failure"` | Response status |
| `response.elapsed` | Elapsed time (seconds) | Elapsed time (seconds) | Elapsed time (seconds) |
| **Error Tags** | | | |
| `error.type` | `"expected"` / `"exceptional"` / `"aggregate"` | `"expected"` / `"exceptional"` / `"aggregate"` | Error classification |
| `error.code` | Error code | Error code | Error code |
| **ActivityStatus** | `Ok` / `Error` | `Ok` / `Error` | OpenTelemetry status |

> For error classification details, see the [Error Classification](#error-classification) section.

### DomainEvent Tracing

#### Publisher Span Structure

| Property | Publish | PublishTrackedEvents |
|----------|---------|---------------------|
| Span Name | `adapter event {EventType}.Publish` | `adapter event PublishTrackedEvents.PublishTrackedEvents` |
| Kind | `Internal` | `Internal` |

#### Publisher Tag Structure (Publish)

| Tag Key | Request | Success Response | Failure Response |
|---------|---------|-----------------|-----------------|
| `request.layer` | `"adapter"` | `"adapter"` | `"adapter"` |
| `request.category.name` | `"event"` | `"event"` | `"event"` |
| `request.handler.name` | event type name | event type name | event type name |
| `request.handler.method` | `"Publish"` | `"Publish"` | `"Publish"` |
| `response.elapsed` | - | Elapsed time (seconds) | Elapsed time (seconds) |
| `response.status` | - | `"success"` | `"failure"` |
| `error.type` | - | - | `"expected"` / `"exceptional"` |
| `error.code` | - | - | Error code |
| **Total Tags** | **4** | **6** | **8** |

#### Publisher Tag Structure (PublishTrackedEvents)

| Tag Key | Request | Success | Partial Failure | Total Failure |
|---------|---------|---------|-----------------|---------------|
| `request.layer` | `"adapter"` | `"adapter"` | `"adapter"` | `"adapter"` |
| `request.category.name` | `"event"` | `"event"` | `"event"` | `"event"` |
| `request.handler.name` | `"PublishTrackedEvents"` | `"PublishTrackedEvents"` | `"PublishTrackedEvents"` | `"PublishTrackedEvents"` |
| `request.handler.method` | `"PublishTrackedEvents"` | `"PublishTrackedEvents"` | `"PublishTrackedEvents"` | `"PublishTrackedEvents"` |
| `request.aggregate.count` | aggregate count | aggregate count | aggregate count | aggregate count |
| `request.event.count` | event count | event count | event count | event count |
| `response.elapsed` | - | Elapsed time (seconds) | Elapsed time (seconds) | Elapsed time (seconds) |
| `response.status` | - | `"success"` | `"failure"` | `"failure"` |
| `response.event.success_count` | - | - | success count | - |
| `response.event.failure_count` | - | - | failure count | - |
| `error.type` | - | - | - | `"expected"` / `"exceptional"` |
| `error.code` | - | - | - | Error code |
| **Total Tags** | **6** | **8** | **10** | **10** |

#### Handler Span Structure

| Property | Description |
|----------|------|
| Span Name | `application usecase.event {HandlerName}.Handle` |
| Kind | `Internal` |

#### Handler Tag Structure

| Tag Key | Success | Failure |
|---------|---------|---------|
| `request.layer` | `"application"` | `"application"` |
| `request.category.name` | `"usecase"` | `"usecase"` |
| `request.category.type` | `"event"` | `"event"` |
| `request.handler.name` | handler name | handler name |
| `request.handler.method` | `"Handle"` | `"Handle"` |
| `request.event.type` | event type name | event type name |
| `request.event.id` | event id | event id |
| `response.status` | `"success"` | `"failure"` |
| `error.type` | - | `"expected"` / `"exceptional"` |
| `error.code` | - | Error code |
| **Total Tags** | **8** | **10** |

> **Note:** Handler's `response.elapsed` is not set on Activity tags (Logging only).

### Implementation

| Layer | Approach | Testing | Notes |
|-------|------|--------|------|
| Application | `IPipelineBehavior` + `ActivitySource.StartActivity()` | [UsecaseTracingPipelineStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseTracingPipelineStructureTests.cs) | Mediator pipeline |
| Adapter | Source Generator | [ObservablePortTracingStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/ObservablePortTracingStructureTests.cs) | Auto-generated Activity spans |
| DomainEvent Publisher | Decorator + `ActivitySource.StartActivity()` | [DomainEventPublisherTracingStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventPublisherTracingStructureTests.cs) | Adapter layer pattern |
| DomainEvent Handler | `INotificationPublisher` + `ActivitySource.StartActivity()` | [DomainEventHandlerTracingStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerTracingStructureTests.cs) | Application Layer pattern |

---

## Related Code

### Code Locations

| Component | File Path |
|----------|----------|
| Field Name Generation Helper | `Src/Functorium.SourceGenerators/Generators/ObservablePortGenerator/CollectionTypeHelper.cs` |
| Application Logging | `Src/Functorium.Adapters/Observabilities/Pipelines/UsecaseLoggingPipeline.cs` |
| Adapter Logging | Source Generator generated code |
| Application Metrics | `Src/Functorium.Adapters/Observabilities/Pipelines/UsecaseMetricsPipeline.cs` |
| Application Tracing | `Src/Functorium.Adapters/Observabilities/Pipelines/UsecaseTracingPipeline.cs` |
| DomainEvent Publisher | `Src/Functorium.Adapters/Observabilities/Events/ObservableDomainEventPublisher.cs` |
| Custom Pipeline Marker | `Src/Functorium.Adapters/Observabilities/Pipelines/ICustomUsecasePipeline.cs` |
| Ctx Enricher Pipeline | `Src/Functorium.Adapters/Observabilities/Pipelines/CtxEnricherPipeline.cs` |
| Ctx Enricher Interface | `Src/Functorium/Applications/Observabilities/IUsecaseCtxEnricher.cs` |
| DomainEvent Ctx Enricher Interface | `Src/Functorium/Applications/Observabilities/IDomainEventCtxEnricher.cs` |
| CtxEnricher Source Generator | `Src/Functorium.SourceGenerators/Generators/CtxEnricherGenerator/CtxEnricherGenerator.cs` |
| DomainEvent CtxEnricher Source Generator | `Src/Functorium.SourceGenerators/Generators/DomainEventCtxEnricherGenerator/DomainEventCtxEnricherGenerator.cs` |
| CtxEnricherContext | `Src/Functorium/Applications/Observabilities/CtxEnricherContext.cs` |
| MetricsTagContext | `Src/Functorium.Adapters/Observabilities/Contexts/MetricsTagContext.cs` |
| CtxPillar enum | `Src/Functorium/Applications/Observabilities/CtxPillar.cs` |
| CtxRoot Attribute | `Src/Functorium/Applications/Observabilities/CtxRootAttribute.cs` |
| CtxIgnore Attribute | `Src/Functorium/Applications/Observabilities/CtxIgnoreAttribute.cs` |
| CtxTarget Attribute | `Src/Functorium/Applications/Observabilities/CtxTargetAttribute.cs` |
| Tracing Custom Base | `Src/Functorium.Adapters/Observabilities/Pipelines/UsecaseTracingCustomPipelineBase.cs` |
| Metric Custom Base | `Src/Functorium.Adapters/Observabilities/Pipelines/UsecaseMetricCustomPipelineBase.cs` |
| Pipeline Configuration | `Src/Functorium.Adapters/Observabilities/Builders/Configurators/PipelineConfigurator.cs` |

### Tests

| Test | File Path |
|--------|----------|
| Application Logging Structure | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseLoggingPipelineStructureTests.cs` |
| Adapter Logging Structure | `Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/ObservableObservableSignalgingStructureTests.cs` |
| Application Metrics Structure | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseMetricsPipelineStructureTests.cs` |
| Adapter Metrics Structure | `Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/ObservablePortMetricsStructureTests.cs` |
| Application Tracing Structure | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseTracingPipelineStructureTests.cs` |
| Adapter Tracing Structure | `Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/ObservablePortTracingStructureTests.cs` |
| DomainEvent Publisher Logging | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventPublisherLoggingStructureTests.cs` |
| DomainEvent Handler Logging | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerLoggingStructureTests.cs` |
| CtxEnricher Source Generator | `Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/CtxEnricherGeneratorTests.cs` |
| DomainEvent CtxEnricher Source Generator | `Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/DomainEventCtxEnricherGeneratorTests.cs` |
| Ctx Enricher Integration | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseLoggingPipelineEnricherTests.cs` |
| DomainEvent Handler Enricher Logging | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerEnricherLoggingStructureTests.cs` |
| DomainEvent Handler Enricher Metrics | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerMetricsStructureTests.cs` |
| DomainEvent Handler Enricher Tracing | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerTracingStructureTests.cs` |
| Tracing Custom Base | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseTracingCustomPipelineBaseTests.cs` |
| Pipeline Configuration | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Configurators/PipelineConfiguratorTests.cs` |

---

## ObservableSignal -- Internal Developer Logging API for Adapter Implementations

### Overview

`ObservableSignal` is a static API for developers to directly emit operational logs within Adapter implementation code. The common context set by the Observable wrapper (`request.layer`, `request.category.name`, `request.handler.name`, `request.handler.method`) is automatically included.

### Pillar Scope

✅ = Supported, X = Not supported

| Level | Logging | Tracing (Activity Event) | Metrics |
|-------|---------|--------------------------|---------|
| Debug | ✅ | X (high frequency -> noise) | X |
| Warning | ✅ | ✅ (track degradation cause within span) | X |
| Error | ✅ | ✅ (track failure cause within span) | X |

- **Metrics excluded**: The Observable wrapper already auto-generates request/response/duration metrics. Use `IMeterFactory` directly for custom metrics.
- **Tracing excluded at Debug level**: Adding high-frequency events like cache misses to spans creates trace noise.

### EventId

| EventId | Name | Level | Description |
|---------|------|-------|------|
| 2021 | `adapter.signal.debug` | Debug | Normal flow details (cache miss, query details) |
| 2022 | `adapter.signal.warning` | Warning | Auto-recoverable degradation (retry, fallback, rate limit) |
| 2023 | `adapter.signal.error` | Error | Unrecoverable failure (retries exhausted, circuit open) |

### Message Template

```
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} — {adapter.log.message} {@adapter.log.context}
```

### Additional Field Naming: `adapter.*`

| Prefix | Purpose | Example |
|----------|------|------|
| `adapter.retry.*` | Retry-related | `adapter.retry.attempt`, `adapter.retry.delay_ms` |
| `adapter.http.*` | HTTP-related | `adapter.http.status_code`, `adapter.http.retry_after_seconds` |
| `adapter.message.*` | Message broker-related | `adapter.message.id`, `adapter.message.queue` |
| `adapter.db.*` | Database-related | `adapter.db.elapsed_ms`, `adapter.db.operation` |
| `adapter.cache.*` | Cache-related | `adapter.cache.key`, `adapter.cache.provider` |

### Usage Example

```csharp
// Warning on Polly retry
ObservableSignal.Warning("Retry attempt {Attempt}/{MaxRetry} after {Delay}s delay",
    ("adapter.retry.attempt", attempt),
    ("adapter.retry.delay_ms", delay.TotalMilliseconds));

// Debug on cache miss (high frequency)
ObservableSignal.Debug("Cache miss", ("adapter.cache.key", cacheKey));

// Error when retries exhausted
ObservableSignal.Error(ex, "Database operation failed after exhausting retries",
    ("adapter.db.retry.attempt", maxRetries));
```

### How It Works

1. `[GenerateObservablePort]` Source Generator calls `ObservableSignalScope.Begin()` within `ExecuteWithSpan`
2. `ObservableSignalScope` sets current context (logger, layer, category, handler, method) via `AsyncLocal`
3. When `ObservableSignal.Debug/Warning/Error` is called in Adapter code, common fields are obtained from `ObservableSignalScope.Current`
4. `ObservableSignalFactory` outputs via ILogger + Activity Event

### Test Reference

| Test | File |
|--------|------|
| ObservableSignal API + Scope | `Tests/Functorium.Tests.Unit/DomainsTests/Observabilities/ObservableSignalTests.cs` |

---

## Related Documents

- [Logging Guide](../guides/observability/19-observability-logging) — Structured logging detailed guide
- [Metrics Guide](../guides/observability/20-observability-metrics) — Metrics collection and analysis guide
- [Tracing Guide](../guides/observability/21-observability-tracing) — Distributed tracing detailed guide
- [Pipeline Specification](../07-pipeline) — Pipeline execution order, OpenTelemetryOptions, custom extension points
- [Code Naming Conventions](../guides/observability/18b-observability-naming) — Observability code naming conventions
