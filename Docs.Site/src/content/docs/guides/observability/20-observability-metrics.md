---
title: "Functorium Metrics Manual"
---

Learn how to collect and utilize metrics in the Functorium framework
to monitor application performance and health.

## Introduction

"How many requests were processed in the last hour?"
"What is the average response time?"
"Is the error rate increasing?"

These are questions frequently encountered when operating applications. Logs record individual events, but they are not well-suited for answering these kinds of **aggregate questions**. Metrics are designed to efficiently answer exactly these types of questions.

Functorium provides metrics collection capabilities that follow the OpenTelemetry Metrics standard. Since the framework automatically collects core metrics, developers can monitor request counts, response times, error rates, and more without writing any additional code.

### What You Will Learn

This document covers the following topics:

1. **How metrics differ from logging** - Comparison of purpose and characteristics
2. **Types of metrics Functorium automatically collects** - Usage of Counter and Histogram
3. **Design principles of the tag system** - Cardinality and performance considerations
4. **Methods for analyzing metrics with Prometheus and Grafana** - PromQL query examples

> **Note**: This document uses Prometheus as the primary example, but since Functorium is OpenTelemetry-based, metrics can be exported to various backends such as **Grafana Mimir**, **Datadog**, **New Relic**, **Azure Monitor**, etc. via OTLP Exporter. Prometheus is the most widely used option and is compatible with all observability platforms that support OTLP.

### Prerequisites

A basic understanding of the following concepts is needed to understand this document:

- Content from the Functorium logging manual (field naming, architecture layers)
- Basic statistical concepts (average, percentiles)
- Concept of time series data

> **Core principle:** Unlike logs, metrics store aggregated numbers to efficiently answer the question "How many/how fast?" Functorium automatically collects Counter (request/response counts) and Histogram (processing time distribution), and `response.elapsed` is recorded as a Histogram rather than a tag to prevent cardinality explosion.

## Summary

### Key Commands

```promql
# Requests per second
rate(application_usecase_command_requests_total[5m])

# Error rate (%)
rate(application_usecase_command_responses_total{response_status="failure"}[5m])
/ rate(application_usecase_command_responses_total[5m]) * 100

# P95 Response time
histogram_quantile(0.95, rate(application_usecase_command_duration_bucket[5m]))
```

### Key Procedures

1. Activate Metrics Pipeline with `ConfigurePipelines(p => p.UseObservability())` (`UseObservability()` activates CtxEnricher, Metrics, Tracing, and Logging all at once)
2. Application Layer automatically collects Counter/Histogram via `UsecaseMetricsPipeline`
3. Adapter Layer auto-generates metrics code via Source Generator
4. Configure RED (Rate, Errors, Duration) dashboards in Prometheus/Grafana

### Key Concepts

| Concept | Description |
|------|------|
| Counter | Cumulative value tracking (`requests`, `responses`) |
| Histogram | Value distribution tracking (`duration`) - enables percentile calculations |
| Meter Name | `{service.namespace}.{layer}[.{category}]` pattern |
| Instrument Name | `{layer}.{category}[.{cqrs}].{type}` (dot-separated, lowercase, plural) |
| Cardinality | Number of unique tag value combinations - `response.elapsed` recorded as Histogram to prevent explosion |
| TagList | High-performance struct with no heap allocation when 8 or fewer tags |

---

## Metrics Fundamentals

### Metrics vs Logging

Metrics and logging serve different purposes. Both are core elements of Observability, but they answer different types of questions.

| Characteristic | Logging | Metrics |
|------|------|--------|
| **Data type** | Individual events | Aggregated numbers |
| **Question type** | "What happened?" | "How many/how fast?" |
| **Storage cost** | High (stores all events) | Low (stores only aggregated values) |
| **Real-time nature** | Individual event tracking | Trend analysis |
| **Search method** | Field-based filtering | Mathematical aggregation operations |

**Understanding with real examples:**

Let us assume we are monitoring order processing in a web shopping mall.

**Logging** records individual events:
```
10:30:01 Order #1001 completed (0.5s)
10:30:02 Order #1002 failure (insufficient stock)
10:30:03 Order #1003 completed (0.3s)
...
```

**Metrics** store aggregated numbers:
```
10:30 - Orders processed: 150, avg time: 0.4s, error rate: 2%
10:31 - Orders processed: 160, avg time: 0.45s, error rate: 3%
10:32 - Orders processed: 140, avg time: 0.38s, error rate: 1%
```

To answer the question "Has order processing time increased in the last hour?" using logs, you would need to read and parse thousands of logs. Metrics store pre-aggregated data, so you can answer immediately with a simple query.

### Three Types of Metrics

OpenTelemetry defines three basic metric types. Each type is suited for different measurement purposes.

#### Counter

Counter tracks **cumulative values**. It always increases and never decreases. It resets to 0 only on restart.

**Suitable for:**
- Total request count
- Total error count
- Bytes processed
- Tasks completed

**In Functorium:**
- `requests` Counter: total request count
- `responses` Counter: total response count (distinguishes success/failure)

**Usage example:**

> **Prometheus Export Note**
> Functorium uses **dot separators** in Instrument names (e.g., `application.usecase.command.requests`).
> When exporting to Prometheus, the OpenTelemetry Exporter automatically converts dots to underscores and adds a `_total` suffix to Counters.
> Therefore, in PromQL you query with `application_usecase_command_requests_total`.
> Tag keys are also converted the same way (`response.status` -> `response_status`).

```
# Request count in the last 5 minutes
increase(application_usecase_command_requests_total[5m])

# Requests per second (Rate)
rate(application_usecase_command_requests_total[1m])
```

#### Histogram

Histogram tracks the **distribution of values**. It classifies and stores values in predefined buckets. It is used to calculate not only averages but also percentiles (P50, P95, P99).

**Suitable for:**
- Request processing time
- Response size
- Queue size

**In Functorium:**
- `duration` Histogram: processing time distribution

**Why percentiles matter more than averages:**

Averages alone cannot accurately capture the user experience. For example:

```
Response time of 10 requests:
90ms, 95ms, 100ms, 105ms, 110ms, 100ms, 95ms, 100ms, 105ms, 2000ms

Average: 290ms
P50 (median): 100ms
P99: 2000ms
```

The average is 290ms, but in reality most users (90%) receive responses under 110ms. Only one user waited 2 seconds. Looking only at the average makes it seem like all users are slow, but in reality only specific cases are slow.

**Why P99 matters:** 1 in 100 users experiences P99-level response time. If there are 1 million requests per day, 10,000 users experience P99-level slow responses.

**Usage example:**
```
# P95 Response time
histogram_quantile(0.95, rate(application_usecase_command_duration_bucket[5m]))

# Average Response time
rate(application_usecase_command_duration_sum[5m]) / rate(application_usecase_command_duration_count[5m])
```

#### Gauge

Gauge tracks the **current value**. It can increase or decrease and represents the state at a specific point in time.

**Suitable for:**
- Current active connections
- Memory usage
- Queue size
- Temperature

**Note:** Gauge is not included in Functorium's automatic metrics. Custom Gauges can be added based on business requirements.

### Why response.elapsed Is Not a Tag

In logging, `response.elapsed` is included as a field, but in metrics it is not included as a tag. There is an important reason for this design decision.

**Cardinality explosion problem:**

Tags are used to group metrics. The metrics system creates a separate **time series** for each unique tag combination.

If processing time (e.g., 0.0234 seconds) were used as a tag:
- 0.0234s -> time series 1
- 0.0235s -> time series 2
- 0.0236s -> time series 3
- ...increases infinitely

This is called **Cardinality Explosion**. Millions of time series are created, placing severe load on the metrics storage and drastically degrading query performance.

**Solution: Use Histogram**

Processing time is recorded as a `duration` Histogram. Histogram groups values into **buckets**:

```
Buckets: 0-50ms, 50-100ms, 100-250ms, 250-500ms, 500ms-1s, 1s-2.5s, 2.5s-5s, 5s+

Distribution of 100 requests:
- 0-50ms: 45
- 50-100ms: 30
- 100-250ms: 15
- 250-500ms: 7
- 500ms-1s: 2
- 1s+: 1
```

This approach limits the number of time series while maintaining distribution information (percentiles).

Now that we understand the differences between Counter, Histogram, and Gauge and the cardinality problem from the metrics fundamentals, let's look at how Functorium automates these concepts across architecture layers.

---

## Functorium Metrics Architecture

Functorium automatically collects metrics in both the Application Layer and Adapter Layer. The framework records core indicators even without developers explicitly writing metrics code.

### Architecture Overview

```
+---------------------------------------------------------------+
|                        HTTP Request                           |
+-------------------------------+-------------------------------+
                                |
                                v
+---------------------------------------------------------------+
|                     Application Layer                         |
|  +---------------------------------------------------------+  |
|  |              UsecaseMetricsPipeline                     |  |
|  |  Meter: {service.namespace}.application                 |  |
|  |  +---------------------------------------------------+  |  |
|  |  | Instruments:                                      |  |  |
|  |  | - application.usecase.{cqrs}.requests (Counter)   |  |  |
|  |  | - application.usecase.{cqrs}.responses (Counter)  |  |  |
|  |  | - application.usecase.{cqrs}.duration (Histogram) |  |  |
|  |  +---------------------------------------------------+  |  |
|  +---------------------------------------------------------+  |
+-------------------------------+-------------------------------+
                                |
                                v
+---------------------------------------------------------------+
|                       Adapter Layer                           |
|  +---------------------------------------------------------+  |
|  |          AdapterMetricsPipeline (Source Generated)      |  |
|  |  Meter: {service.namespace}.adapter.{category}          |  |
|  |  +---------------------------------------------------+  |  |
|  |  | Instruments:                                      |  |  |
|  |  | - adapter.{category}.requests (Counter)           |  |  |
|  |  | - adapter.{category}.responses (Counter)          |  |  |
|  |  | - adapter.{category}.duration (Histogram)         |  |  |
|  |  +---------------------------------------------------+  |  |
|  +---------------------------------------------------------+  |
+---------------------------------------------------------------+
```

### Meter Organization Structure

Functorium organizes Meters by service namespace and layer. This structure makes it easy to logically group and filter metrics.

**Meter name pattern:**

| Layer | Meter name pattern | Example |
|-------|-----------------|------|
| Application | `{service.namespace}.application` | `mycompany.production.application` |
| Adapter | `{service.namespace}.adapter.{category}` | `mycompany.production.adapter.repository` |

**Example configuration:**

```csharp
services.Configure<OpenTelemetryOptions>(options =>
{
    options.ServiceNamespace = "mycompany.production";
});
```

Meters generated from this configuration:
- `mycompany.production.application`
- `mycompany.production.adapter.repository`
- `mycompany.production.adapter.gateway`

---

## Understanding Meter and Instrument

### Meter

Meter is a logical group of related Instruments. In OpenTelemetry, Meters are typically created per library or component unit.

In Functorium, Meters are created from a combination of **service namespace + layer + category**. This structure provides the following benefits:

1. **Selective collection:** Collect only metrics from specific layers or categories
2. **Granular monitoring:** Apply different alert rules per layer
3. **Cost management:** Disable collection of unnecessary metrics

### Instrument Structure

Functorium generates three Instruments in each layer:

#### requests Counter

Incremented at request start. Tracks the total number of requests entering the system.

**Application Layer:**
```
Instrument: application.usecase.{cqrs}.requests
Unit: {request}
Example: application.usecase.command.requests
```

**Adapter Layer:**
```
Instrument: adapter.{category}.requests
Unit: {request}
Example: adapter.repository.requests
```

#### responses Counter

Incremented at response completion. Success/failure is distinguished by the `response.status` tag.

**Application Layer:**
```
Instrument: application.usecase.{cqrs}.responses
Unit: {response}
Example: application.usecase.command.responses
```

**Adapter Layer:**
```
Instrument: adapter.{category}.responses
Unit: {response}
Example: adapter.repository.responses
```

#### duration Histogram

Records the distribution of processing time. Measured in seconds.

**Application Layer:**
```
Instrument: application.usecase.{cqrs}.duration
Unit: s (seconds)
Example: application.usecase.command.duration
```

**Adapter Layer:**
```
Instrument: adapter.{category}.duration
Unit: s (seconds)
Example: adapter.repository.duration
```

### Instrument Naming Convention

All Instrument names in Functorium follow these rules:

1. **Lowercase**: `application.usecase.command.requests` (no PascalCase)
2. **Dot separator**: `application.usecase.command` (no underscores)
3. **Plural form**: `requests`, `responses` (no singular form)
4. **Meaningful hierarchy**: `{layer}.{category}.{cqrs}.{type}`

---

## Tag System Detailed Guide

Tags are metadata that enable analyzing metrics across various dimensions. Functorium maintains consistency by using the same tag keys as logging.

### Application Layer Tag Structure

In the Application Layer, different Instruments are used depending on CQRS type, so `request.category.type` is also included as a tag.

**Tag structure table:**

| tag key | requests | duration | responses (success) | responses (failure) |
|---------|----------|----------|------------------|------------------|
| `request.layer` | "application" | "application" | "application" | "application" |
| `request.category.name` | "usecase" | "usecase" | "usecase" | "usecase" |
| `request.category.type` | "command"/"query" | "command"/"query" | "command"/"query" | "command"/"query" |
| `request.handler.name` | handler name | handler name | handler name | handler name |
| `request.handler.method` | "Handle" | "Handle" | "Handle" | "Handle" |
| `response.status` | - | - | "success" | "failure" |
| `error.type` | - | - | - | "expected"/"exceptional"/"aggregate" |
| `error.code` | - | - | - | error code |
| **total tag count** | **5** | **5** | **6** | **8** |

**Example - Command success:**

```
# requests Counter
application_usecase_command_requests_total{
  request_layer="application",
  request_category_name="usecase",
  request_category_type="command",
  request_handler_name="CreateOrderCommandHandler",
  request_handler_method="Handle"
} 1

# responses Counter
application_usecase_command_responses_total{
  request_layer="application",
  request_category_name="usecase",
  request_category_type="command",
  request_handler_name="CreateOrderCommandHandler",
  request_handler_method="Handle",
  response_status="success"
} 1
```

**Example - Command failure:**

```
application_usecase_command_responses_total{
  request_layer="application",
  request_category_name="usecase",
  request_category_type="command",
  request_handler_name="CreateOrderCommandHandler",
  request_handler_method="Handle",
  response_status="failure",
  error_type="expected",
  error_code="Order.InsufficientStock"
} 1
```

### Adapter Layer Tag Structure

In the Adapter Layer, there is no CQRS distinction, so the tag count is less than the Application Layer.

**Tag structure table:**

| tag key | requests | duration | responses (success) | responses (failure) |
|---------|----------|----------|------------------|------------------|
| `request.layer` | "adapter" | "adapter" | "adapter" | "adapter" |
| `request.category.name` | category name | category name | category name | category name |
| `request.handler.name` | handler name | handler name | handler name | handler name |
| `request.handler.method` | method name | method name | method name | method name |
| `response.status` | - | - | "success" | "failure" |
| `error.type` | - | - | - | "expected"/"exceptional"/"aggregate" |
| `error.code` | - | - | - | error code |
| **total tag count** | **4** | **4** | **5** | **7** |

**Example - Repository success:**

```
adapter_repository_responses_total{
  request_layer="adapter",
  request_category_name="repository",
  request_handler_name="OrderRepository",
  request_handler_method="GetById",
  response_status="success"
} 1
```

### Cardinality Considerations

The number of unique tag value combinations is called **Cardinality**. High cardinality degrades the performance of metrics storage.

**Functorium's cardinality management:**

1. **Fixed tag values**: `request.layer`, `request.category.name`, `request.category.type` have only limited values.

2. **Exclude unique identifiers**: Request IDs, user IDs, order IDs, etc. are not included as tags. Including such values causes cardinality explosion.

3. **Exclude processing time tags**: `response.elapsed` is a continuous value, so it is recorded as a Histogram rather than a tag.

**Expected cardinality calculation:**

```
Application Layer:
- request.layer: 1 (application)
- request.category.name: 1 (usecase)
- request.category.type: 2 (command, query)
- request.handler.name: N (number of handlers)
- request.handler.method: 1 (Handle)
- response.status: 2 (success, failure)
- error.type: 3 (expected, exceptional, aggregate)
- error.code: M (number of error codes)

Max Cardinality ≈ 1 x 1 x 2 x N x 1 x 2 x 3 x M = 12 x N x M

Assuming 100 handlers and 50 error codes:
Max Cardinality ≈ 12 x 100 x 50 = 60,000 time series
```

60,000 time series is a level that most metrics systems can handle sufficiently.

### ctx.* MetricsTag -- User-Defined Metric Dimensions

`CtxEnricherPipeline` runs first and stores low-cardinality ctx.* fields designated with `[CtxTarget(CtxPillar.MetricsTag)]` in `MetricsTagContext`. Then `UsecaseMetricsPipeline` generates standard tags and merges ctx.* tags from `MetricsTagContext` into the `TagList`.

```
Pipeline execution order:
CtxEnricher → Metrics → Tracing → Logging → ... → Handler

CtxEnricherPipeline:
  ctx.is_express = true  -> Stored in MetricsTagContext (CtxPillar.MetricsTag)
  ctx.customer_id = "C1" -> Not stored in MetricsTagContext (CtxPillar.Default -> Logging + Tracing only)

UsecaseMetricsPipeline:
  TagList = { request.layer, request.handler.name, ..., ctx.is_express }
                                                        ↑ Merged from MetricsTagContext
```

**Usage example:**

```csharp
public sealed record Request(
    string CustomerId,                                    // Default (Logging + Tracing)
    [CtxTarget(CtxPillar.All)] bool IsExpress,            // Includes MetricsTag -> Counter dimension
    [CtxTarget(CtxPillar.Default | CtxPillar.MetricsValue)]
    decimal TotalAmount                                   // MetricsValue -> Recorded in separate Histogram
) : ICommandRequest<Response>;
```

> **Cardinality caution**: The number of unique values for MetricsTag-designated fields multiplies with existing tag dimensions. Only designate `boolean` (2 values), `enum` (limited values), or limited strings as MetricsTag. Specifying high-cardinality types triggers compile-time warning `FUNCTORIUM005`.

---

## Application Layer Metrics

Application Layer metrics are automatically collected by `UsecaseMetricsPipeline`.

### Instrument Details

**1. application.usecase.{cqrs}.requests**

| attribute | Value |
|------|-----|
| Type | Counter |
| unit | {request} |
| Description | Usecase request count |
| recorded at | At handler execution start |

**2. application.usecase.{cqrs}.responses**

| attribute | Value |
|------|-----|
| Type | Counter |
| unit | {response} |
| Description | Usecase response count (distinguishes success/failure) |
| recorded at | At handler execution completion |

**3. application.usecase.{cqrs}.duration**

| attribute | Value |
|------|-----|
| Type | Histogram |
| unit | s (seconds) |
| Description | Usecase processing time distribution |
| recorded at | At handler execution completion |

### Key Metric Calculations

**1. Throughput (Throughput)**

Calculate requests per second:

```promql
# Total Command Throughput (requests/s)
rate(application_usecase_command_requests_total[5m])

# Throughput for a specific handler
rate(application_usecase_command_requests_total{
  request_handler_name="CreateOrderCommandHandler"
}[5m])
```

**2. Error rate (Error Rate)**

Calculate the failure ratio among total responses:

```promql
# Command Error rate (%)
rate(application_usecase_command_responses_total{response_status="failure"}[5m])
/
rate(application_usecase_command_responses_total[5m])
* 100

# Error rate for a specific handler
rate(application_usecase_command_responses_total{
  request_handler_name="CreateOrderCommandHandler",
  response_status="failure"
}[5m])
/
rate(application_usecase_command_responses_total{
  request_handler_name="CreateOrderCommandHandler"
}[5m])
* 100
```

**3. Response time (Latency)**

```promql
# P50 (median)
histogram_quantile(0.50,
  rate(application_usecase_command_duration_bucket[5m])
)

# P95
histogram_quantile(0.95,
  rate(application_usecase_command_duration_bucket[5m])
)

# P99
histogram_quantile(0.99,
  rate(application_usecase_command_duration_bucket[5m])
)

# Average
rate(application_usecase_command_duration_sum[5m])
/
rate(application_usecase_command_duration_count[5m])
```

### Custom Metric Extension (UsecaseMetricCustomPipelineBase)

In addition to `requests`, `responses`, and `duration` automatically collected by `UsecaseMetricsPipeline`, you can add per-Usecase business metrics. Inherit from `UsecaseMetricCustomPipelineBase<TRequest>` to create custom Instruments.

#### Base Class API

```csharp
public abstract class UsecaseMetricCustomPipelineBase<TRequest>
    : UsecasePipelineBase<TRequest>, ICustomUsecasePipeline
{
    protected readonly Meter _meter;
    protected string GetMetricName(string metricName);
    protected string GetMetricNameWithoutHandler(string metricName);
}
```

- `GetMetricName(metricName)`: Generates a Metric name including the Handler. Format: `application.usecase.{cqrs}.{handler}.{metricName}`
- `GetMetricNameWithoutHandler(metricName)`: Generates a CQRS-level Metric name excluding the Handler. Format: `application.usecase.{cqrs}.{metricName}`
- `RequestDuration`: Helper class for measuring request processing time. Used with `using` statement for automatic recording to Histogram.

#### Implementation Example (PlaceOrderCommand.MetricsPipeline)

```csharp
public sealed class PlaceOrderMetricsPipeline
    : UsecaseMetricCustomPipelineBase<PlaceOrderCommand.Request>
    , IPipelineBehavior<PlaceOrderCommand.Request, FinResponse<PlaceOrderCommand.Response>>
{
    private readonly Histogram<int> _orderLineCount;
    private readonly Histogram<double> _orderTotalAmount;

    public PlaceOrderMetricsPipeline(
        IOptions<OpenTelemetryOptions> options, IMeterFactory meterFactory)
        : base(options.Value.ServiceNamespace, meterFactory)
    {
        _orderLineCount = _meter.CreateHistogram<int>(
            name: GetMetricName("order_line_count"),
            unit: "{lines}",
            description: "Number of order lines per PlaceOrder");

        _orderTotalAmount = _meter.CreateHistogram<double>(
            name: GetMetricName("order_total_amount"),
            unit: "{currency}",
            description: "Total amount per PlaceOrder");
    }

    public async ValueTask<FinResponse<PlaceOrderCommand.Response>> Handle(
        PlaceOrderCommand.Request request,
        MessageHandlerDelegate<PlaceOrderCommand.Request, FinResponse<PlaceOrderCommand.Response>> next,
        CancellationToken ct)
    {
        _orderLineCount.Record(request.Lines.Count);
        _orderTotalAmount.Record((double)request.Lines.Sum(l => l.Quantity * l.UnitPrice));
        return await next(request, ct);
    }
}
```

#### Registration Method

`UsecaseMetricCustomPipelineBase<TRequest>` implements `ICustomUsecasePipeline`, so it is explicitly registered with `AddCustomPipeline<T>()`. Individual registration is used instead of assembly scanning to guarantee deterministic pipeline execution order:

```csharp
.ConfigurePipelines(p => p
    .UseObservability()
    .AddCustomPipeline<PlaceOrderCommandMetricPipeline>())
```

> **Reference**: [Custom Extension](../../spec/07-pipeline#custom-extension)

---

## Adapter Layer Metrics

Adapter Layer metrics are collected by code automatically generated by Source Generator.

### Instrument Details

**1. adapter.{category}.requests**

| attribute | Value |
|------|-----|
| Type | Counter |
| unit | {request} |
| Description | Adapter request count |
| recorded at | At method execution start |

**2. adapter.{category}.responses**

| attribute | Value |
|------|-----|
| Type | Counter |
| unit | {response} |
| Description | Adapter response count (distinguishes success/failure) |
| recorded at | At method execution completion |

**3. adapter.{category}.duration**

| attribute | Value |
|------|-----|
| Type | Histogram |
| unit | s (seconds) |
| Description | Adapter processing time distribution |
| recorded at | At method execution completion |

### Repository Metric Analysis

An example of monitoring database operation performance:

**Throughput by Method:**

```promql
# Requests per second by Repository method
sum by (request_handler_method) (
  rate(adapter_repository_requests_total{
    request_handler_name="OrderRepository"
  }[5m])
)
```

**Identify slow queries:**

```promql
# Methods where P95 response time exceeds 1 second
histogram_quantile(0.95,
  rate(adapter_repository_duration_bucket[5m])
) > 1
```

**Methods with High Error Rate:**

```promql
# Repository methods where error rate exceeds 5%
rate(adapter_repository_responses_total{response_status="failure"}[5m])
/
rate(adapter_repository_responses_total[5m])
> 0.05
```

---

## DomainEvent Metrics

DomainEvent metrics are collected from both the Publisher and Handler. Both components use the same three Instruments (requests, responses, duration).

### Meter Name

| Component | Meter Name Pattern | Example (`ServiceNamespace = "mycompany.production"`) |
|----------|-----------------|---------------------------------------------------|
| Publisher | `{service.namespace}.adapter.event` | `mycompany.production.adapter.event` |
| Handler | `{service.namespace}.application` | `mycompany.production.application` |

### Publisher Instrument Details

**1. adapter.event.requests**

| attribute | Value |
|------|-----|
| Type | Counter |
| unit | {request} |
| Description | DomainEvent publish request count |
| recorded at | At Publisher execution start |

**2. adapter.event.responses**

| attribute | Value |
|------|-----|
| Type | Counter |
| unit | {response} |
| Description | DomainEvent publish response count (distinguishes success/failure) |
| recorded at | At Publisher execution completion |

**3. adapter.event.duration**

| attribute | Value |
|------|-----|
| Type | Histogram |
| unit | s (seconds) |
| Description | DomainEvent publish processing time distribution |
| recorded at | At Publisher execution completion |

### Publisher Tag Structure

| tag key | requests | duration | responses (success) | responses (failure) |
|---------|----------|----------|------------------|------------------|
| `request.layer` | "adapter" | "adapter" | "adapter" | "adapter" |
| `request.category.name` | "event" | "event" | "event" | "event" |
| `request.handler.name` | handler name | handler name | handler name | handler name |
| `request.handler.method` | method name | method name | method name | method name |
| `response.status` | - | "success"/"failure" | "success" | "failure" |
| `error.type` | - | - | - | "expected"/"exceptional" |
| `error.code` | - | - | - | error code |
| **total tag count** | **4** | **5** | **5** | **7** |

> **Tags excluded from DomainEvent Metrics:**
> `request.event.count`, `response.event.success_count`, `response.event.failure_count` are not used as Metrics tags.
> Since each of these values has unique numeric values, using them as tags would cause **high Cardinality explosion**.
> This follows the same principle as not using `response.elapsed` as a Metrics tag.

### Handler Instrument Details

**1. application.usecase.event.requests**

| attribute | Value |
|------|-----|
| Type | Counter |
| unit | {request} |
| Description | DomainEvent Handler request count |
| recorded at | At Handler execution start |

**2. application.usecase.event.responses**

| attribute | Value |
|------|-----|
| Type | Counter |
| unit | {response} |
| Description | DomainEvent Handler response count (distinguishes success/failure) |
| recorded at | At Handler execution completion |

**3. application.usecase.event.duration**

| attribute | Value |
|------|-----|
| Type | Histogram |
| unit | s (seconds) |
| Description | DomainEvent Handler processing time distribution |
| recorded at | At Handler execution completion |

### Handler Tag Structure

| tag key | requests | duration | responses (success) | responses (failure) |
|---------|----------|----------|------------------|------------------|
| `request.layer` | "application" | "application" | "application" | "application" |
| `request.category.name` | "usecase" | "usecase" | "usecase" | "usecase" |
| `request.category.type` | "event" | "event" | "event" | "event" |
| `request.handler.name` | handler name | handler name | handler name | handler name |
| `request.handler.method` | "Handle" | "Handle" | "Handle" | "Handle" |
| `response.status` | - | - | "success" | "failure" |
| `error.type` | - | - | - | "expected"/"exceptional" |
| `error.code` | - | - | - | error code |
| **total tag count** | **5** | **5** | **6** | **8** |

### PromQL query examples

**DomainEvent Publisher Throughput:**

```promql
# Publisher publishes per second
rate(adapter_event_requests_total[5m])

# Publish volume for a specific Aggregate
rate(adapter_event_requests_total{
  request_handler_name="Product"
}[5m])
```

**DomainEvent Handler Error rate:**

```promql
# Handler Error rate (exceptional)
rate(application_usecase_event_responses_total{error_type="exceptional"}[5m])
/
rate(application_usecase_event_responses_total[5m])
* 100
```

**Processing time by Handler:**

```promql
# Handler P95 Response time
histogram_quantile(0.95,
  rate(application_usecase_event_duration_bucket[5m])
)

# Average response time for a specific Handler
rate(application_usecase_event_duration_sum{
  request_handler_name="OnProductCreated"
}[5m])
/
rate(application_usecase_event_duration_count{
  request_handler_name="OnProductCreated"
}[5m])
```

---

## Understanding Error Metrics

### Error Tag Structure

Failed response metrics include additional `error.type` and `error.code` tags.

| error.type | Meaning | Response |
|------------|------|------|
| "expected" | Expected business error | Pattern analysis, business improvement |
| "exceptional" | Exceptional system error | Immediate alert, Technical investigation |
| "aggregate" | Multiple errors combined | Classified by first error code |

### Analysis by Error Type

**System error monitoring (immediate action required):**

```promql
# Exceptional error occurrence rate
rate(application_usecase_command_responses_total{
  error_type="exceptional"
}[5m])
```

**Business error pattern analysis:**

```promql
# Occurrence frequency by Expected error code
sum by (error_code) (
  increase(application_usecase_command_responses_total{
    error_type="expected"
  }[1h])
)
```

### Alert Rule Configuration

**1. System error alert:**

```yaml
# Prometheus AlertManager rule
- alert: HighExceptionalErrorRate
  expr: |
    rate(application_usecase_command_responses_total{error_type="exceptional"}[5m])
    /
    rate(application_usecase_command_responses_total[5m])
    > 0.01
  for: 5m
  labels:
    severity: critical
  annotations:
    summary: "System error rate has exceeded 1%"
```

**2. Specific error code surge alert:**

```yaml
- alert: DatabaseConnectionErrors
  expr: |
    increase(adapter_repository_responses_total{
      error_code="Database.ConnectionFailed"
    }[5m]) > 10
  for: 1m
  labels:
    severity: critical
  annotations:
    summary: "Database connection errors are surging"
```

We have now covered the error metrics tag structure and alert rules. Let us look at how to configure Grafana dashboards for visually monitoring these metrics.

---

## Configuring Dashboards

### RED Methodology

RED is a set of core indicators for service monitoring:

- **R**ate: Requests per second
- **E**rrors: Error rate
- **D**uration: Response time distribution

### Recommended Dashboard Panels

**1. Overall Status Panel**

```promql
# Total Throughput
sum(rate(application_usecase_command_requests_total[5m]))
+ sum(rate(application_usecase_query_requests_total[5m]))

# Overall Error rate
sum(rate(application_usecase_command_responses_total{response_status="failure"}[5m]))
+ sum(rate(application_usecase_query_responses_total{response_status="failure"}[5m]))
/
sum(rate(application_usecase_command_responses_total[5m]))
+ sum(rate(application_usecase_query_responses_total[5m]))

# P99 Response time
histogram_quantile(0.99,
  sum(rate(application_usecase_command_duration_bucket[5m])) by (le)
)
```

**2. Handler Comparison Panel**

```promql
# Throughput by Handler (Top 10)
topk(10,
  sum by (request_handler_name) (
    rate(application_usecase_command_requests_total[5m])
  )
)

# Error rate by Handler
sum by (request_handler_name) (
  rate(application_usecase_command_responses_total{response_status="failure"}[5m])
)
/
sum by (request_handler_name) (
  rate(application_usecase_command_responses_total[5m])
)
```

**3. Error Analysis Panel**

```promql
# Distribution by error type
sum by (error_type) (
  rate(application_usecase_command_responses_total{response_status="failure"}[5m])
)

# Top 10 by error code
topk(10,
  sum by (error_code) (
    rate(application_usecase_command_responses_total{response_status="failure"}[5m])
  )
)
```

### Grafana Dashboard JSON Example

```json
{
  "panels": [
    {
      "title": "Request Rate",
      "type": "stat",
      "targets": [{
        "expr": "sum(rate(application_usecase_command_requests_total[5m]))",
        "legendFormat": "requests/s"
      }]
    },
    {
      "title": "Error Rate",
      "type": "gauge",
      "targets": [{
        "expr": "sum(rate(application_usecase_command_responses_total{response_status=\"failure\"}[5m])) / sum(rate(application_usecase_command_responses_total[5m])) * 100",
        "legendFormat": "error %"
      }],
      "fieldConfig": {
        "defaults": {
          "thresholds": {
            "steps": [
              { "value": 0, "color": "green" },
              { "value": 1, "color": "yellow" },
              { "value": 5, "color": "red" }
            ]
          }
        }
      }
    }
  ]
}
```

---

## Exercise: Analyzing Metrics

### Scenario 1: Investigating Performance Degradation

**Situation:** A report has come in saying "order creation is slow."

**Step 1: Check current status**

```promql
# P95 Response time for CreateOrderCommandHandler
histogram_quantile(0.95,
  rate(application_usecase_command_duration_bucket{
    request_handler_name="CreateOrderCommandHandler"
  }[5m])
)
```

**Step 2: Check trends over time**

```promql
# P95 trend over 1 hour
histogram_quantile(0.95,
  rate(application_usecase_command_duration_bucket{
    request_handler_name="CreateOrderCommandHandler"
  }[5m])
)
```

Check the graph in Grafana with a 1-hour range.

**Step 3: Analyze sub-calls**

```promql
# Related Adapter Response time
histogram_quantile(0.95,
  rate(adapter_repository_duration_bucket{
    request_handler_name="OrderRepository"
  }[5m])
)

histogram_quantile(0.95,
  rate(adapter_gateway_duration_bucket{
    request_handler_name="PaymentGateway"
  }[5m])
)
```

**Step 4: Draw conclusion**

If PaymentGateway's response time has sharply increased, the root cause is latency in the external payment service.

### Scenario 2: Investigating Error Spike

**Situation:** Error rate has surged from the usual 0.5% to 5%.

**Step 1: Check error type**

```promql
# Occurrence rate by error type
sum by (error_type) (
  rate(application_usecase_command_responses_total{
    response_status="failure"
  }[5m])
)
```

**Step 2: If system error**

```promql
# Distribution by Exceptional error code
sum by (error_code) (
  rate(application_usecase_command_responses_total{
    error_type="exceptional"
  }[5m])
)
```

If `Database.ConnectionFailed` accounts for most errors, check the database status.

**Step 3: If business error**

```promql
# Distribution by Expected error code
sum by (error_code) (
  rate(application_usecase_command_responses_total{
    error_type="expected"
  }[5m])
)
```

If `Order.InsufficientStock` has surged, check for an out-of-stock situation.

---

## Troubleshooting

### When Metrics Are Not Being Collected

**Symptom:** Specific metrics are not visible in Prometheus.

**Check the following:**

1. **Verify Pipeline registration:**
   ```csharp
   services.AddMediator(options =>
   {
       options.AddOpenBehavior(typeof(UsecaseMetricsPipeline<,>));
   });
   ```

2. **Verify OpenTelemetry configuration:**
   ```csharp
   services.Configure<OpenTelemetryOptions>(options =>
   {
       options.ServiceNamespace = "mycompany.production";
   });
   ```

3. **Verify Meter filter:**
   ```csharp
   builder.WithMetrics(metrics =>
   {
       metrics.AddMeter("mycompany.production.*");
   });
   ```

### When Cardinality Is Too High

**Symptom:** Disk usage of the metrics storage is increasing rapidly.

**Cause:** Tags with many unique values are included.

**How to check:**

```promql
# Check Cardinality
count(application_usecase_command_responses_total)
```

**Solution:**

1. Remove unnecessary tags
2. Normalize tag values (e.g., use error codes instead of error messages)
3. Collect metrics only under specific conditions

### When Histogram Buckets Are Not Appropriate

**Symptom:** P95, P99 values are inaccurate.

**Cause:** Default bucket boundaries do not match the actual distribution.

**Solution:**

Set custom bucket boundaries:

```csharp
// e.g., 10ms, 25ms, 50ms, 100ms, 250ms, 500ms, 1s, 2.5s, 5s, 10s
var boundaries = new double[] { 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1.0, 2.5, 5.0, 10.0 };
```

---

## FAQ

### Q: Should I use Counter or Gauge?

A: It depends on whether the value is **cumulative** or represents a **current state**:

- Request count, error count, bytes processed → **Counter** (always increases)
- Active connection count, queue size, memory usage → **Gauge** (increases/decreases)

Functorium's automatic metrics include request/response counts (Counter) and processing time (Histogram).

### Q: What is the difference between rate() and increase()?

A:
- `rate()`: Rate of change per second (requests/second)
- `increase()`: Total increase over a specified period (requests)

```promql
# Requests per second
rate(application_usecase_command_requests_total[5m])

# Total requests over 5 minutes
increase(application_usecase_command_requests_total[5m])
```

### Q: How do I set the metrics retention period?

A: Use the `--storage.tsdb.retention.time` option in the Prometheus configuration:

```yaml
# 15-day retention
prometheus --storage.tsdb.retention.time=15d
```

For long-term retention, use long-term storage solutions such as Thanos or Cortex.

### Q: How do I collect metrics for specific handlers only?

A: You can filter using OpenTelemetry Views:

```csharp
meterProvider.AddView(
    instrumentName: "application.usecase.command.requests",
    new MetricStreamConfiguration
    {
        TagKeys = new[] { "request_handler" }
    }
);
```

### Q: How do I correlate metrics with logs?

A: Since the same tag keys are used, correlations are easy to track:

1. Detect anomaly in metrics: `error_code="Database.ConnectionFailed"` surge
2. Investigate details in logs: Filter by `error.code = "Database.ConnectionFailed"`

Using Grafana's Explore feature, you can easily navigate from metrics to related logs.

---

## References

- [OpenTelemetry Metrics Specification](https://opentelemetry.io/docs/specs/otel/metrics/)
- [Prometheus Query Language (PromQL)](https://prometheus.io/docs/prometheus/latest/querying/basics/)
- [Grafana Dashboard Best Practices](https://grafana.com/docs/grafana/latest/best-practices/best-practices-for-creating-dashboards/)
- [RED Method](https://www.weave.works/blog/the-red-method-key-metrics-for-microservices-architecture/)

**Internal documents:**
- [08-observability.md](../../spec/08-observability) — Observability specification (Field/Tag, Meter, message templates)
- [18b-observability-naming.md](./18b-observability-naming) — Observability naming guide
- [19-observability-logging.md](./19-observability-logging) — Observability logging details
- [21-observability-tracing.md](./21-observability-tracing) — Observability tracing details

---

## TagList Performance Optimization

When passing metric tags in `System.Diagnostics.Metrics`, using the `TagList` struct prevents heap allocations and minimizes GC overhead.

### Problem: Array-Based Tag Passing

```csharp
// Array object is allocated on the heap every time
KeyValuePair<string, object?>[] tags =
[
    new("layer", "adapter"),
    new("category", "repository"),
    new("handler", "UserRepository"),
    new("method", "GetById")
];

counter.Add(1, tags);
```

### Solution: TagList Struct

```csharp
// TagList is a struct allocated on the stack
TagList tags = new()
{
    { "layer", "adapter" },
    { "category", "repository" },
    { "handler", "UserRepository" },
    { "method", "GetById" }
};

counter.Add(1, tags);
```

### TagList Internal Structure

`TagList` stores up to 8 tags in inline fields (no heap allocation). Internal array allocation occurs only when there are 9 or more tags.

| Approach | Allocation for 4 tags | GC Impact |
|------|-------------------|---------|
| `KeyValuePair[]` | 96 bytes/call | Gen0 increase |
| `TagList` | 0 bytes/call | None |

### Supported APIs

| Method | TagList Support |
|--------|-------------|
| `Counter<T>.Add(T, TagList)` | O |
| `Histogram<T>.Record(T, TagList)` | O |
| `UpDownCounter<T>.Add(T, TagList)` | O |
| `Measurement<T>(T, TagList)` | O |

### Cautions

- Keep the tag count to **8 or fewer** (exceeding this triggers internal array allocation)
- Especially important for high-frequency metrics
- OpenTelemetry also recommends minimizing the number of tags
