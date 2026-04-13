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

**메서드별 Throughput:**

```promql
# Repository 메서드별 Requests per second
sum by (request_handler_method) (
  rate(adapter_repository_requests_total{
    request_handler_name="OrderRepository"
  }[5m])
)
```

**Identify slow queries:**

```promql
# P95 Response time이 1초를 초과하는 메서드
histogram_quantile(0.95,
  rate(adapter_repository_duration_bucket[5m])
) > 1
```

**Error rate이 높은 메서드:**

```promql
# Error rate이 5%를 초과하는 Repository 메서드
rate(adapter_repository_responses_total{response_status="failure"}[5m])
/
rate(adapter_repository_responses_total[5m])
> 0.05
```

---

## DomainEvent Metrics

DomainEvent의 메트릭은 Publisher와 Handler 각각에서 수집됩니다. 두 컴포넌트 모두 동일한 3종 Instrument(requests, responses, duration)를 사용합니다.

### Meter Name

| 컴포넌트 | Meter Name 패턴 | 예시 (`ServiceNamespace = "mycompany.production"`) |
|----------|-----------------|---------------------------------------------------|
| Publisher | `{service.namespace}.adapter.event` | `mycompany.production.adapter.event` |
| Handler | `{service.namespace}.application` | `mycompany.production.application` |

### Publisher Instrument 상세

**1. adapter.event.requests**

| attribute | Value |
|------|-----|
| Type | Counter |
| unit | {request} |
| Description | DomainEvent 발행 요청 수 |
| recorded at | Publisher 실행 시작 시 |

**2. adapter.event.responses**

| attribute | Value |
|------|-----|
| Type | Counter |
| unit | {response} |
| Description | DomainEvent 발행 응답 수 (success/failure 구분) |
| recorded at | Publisher 실행 완료 시 |

**3. adapter.event.duration**

| attribute | Value |
|------|-----|
| Type | Histogram |
| unit | s (seconds) |
| Description | DomainEvent 발행 processing time 분포 |
| recorded at | Publisher 실행 완료 시 |

### Publisher 태그 구조

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

> **DomainEvent Metrics에서 제외되는 태그:**
> `request.event.count`, `response.event.success_count`, `response.event.failure_count`는 Metrics 태그로 사용하지 않습니다.
> 이 값들은 각각 고유한 수치를 가지므로 태그로 사용하면 **높은 Cardinality 폭발**을 유발합니다.
> 이는 `response.elapsed`를 Metrics 태그로 사용하지 않는 것과 동일한 원칙입니다.

### Handler Instrument 상세

**1. application.usecase.event.requests**

| attribute | Value |
|------|-----|
| Type | Counter |
| unit | {request} |
| Description | DomainEvent Handler 요청 수 |
| recorded at | Handler 실행 시작 시 |

**2. application.usecase.event.responses**

| attribute | Value |
|------|-----|
| Type | Counter |
| unit | {response} |
| Description | DomainEvent Handler 응답 수 (success/failure 구분) |
| recorded at | Handler 실행 완료 시 |

**3. application.usecase.event.duration**

| attribute | Value |
|------|-----|
| Type | Histogram |
| unit | s (seconds) |
| Description | DomainEvent Handler processing time 분포 |
| recorded at | Handler 실행 완료 시 |

### Handler 태그 구조

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
# Publisher 초당 발행 수
rate(adapter_event_requests_total[5m])

# 특정 Aggregate의 발행량
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

**Handler별 processing time:**

```promql
# Handler P95 Response time
histogram_quantile(0.95,
  rate(application_usecase_event_duration_bucket[5m])
)

# 특정 Handler의 평균 Response time
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

failure한 응답의 메트릭에는 `error.type`과 `error.code` 태그가 추가됩니다.

| error.type | 의미 | 대응 |
|------------|------|------|
| "expected" | Expected business error | Pattern analysis, business improvement |
| "exceptional" | Exceptional system error | Immediate alert, Technical investigation |
| "aggregate" | Multiple errors combined | Classified by first error code |

### Analysis by Error Type

**System error monitoring (immediate action required):**

```promql
# Exceptional 에러 발생률
rate(application_usecase_command_responses_total{
  error_type="exceptional"
}[5m])
```

**Business error pattern analysis:**

```promql
# Expected error code별 발생 빈도
sum by (error_code) (
  increase(application_usecase_command_responses_total{
    error_type="expected"
  }[1h])
)
```

### Alert Rule Configuration

**1. System error alert:**

```yaml
# Prometheus AlertManager 규칙
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
    summary: "시스템 Error rate이 1%를 초과했습니다"
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
    summary: "데이터베이스 연결 에러가 급증했습니다"
```

지금까지 에러 메트릭의 태그 구조와 알림 규칙을 살펴봤습니다. 이 메트릭을 시각적으로 모니터링하기 위해 Grafana 대시보드를 구성하는 방법을 알아봅니다.

---

## Configuring Dashboards

### RED Methodology

RED는 서비스 모니터링을 위한 핵심 지표입니다:

- **R**ate: Requests per second
- **E**rrors: Error rate
- **D**uration: Response time 분포

### Recommended Dashboard Panels

**1. 전체 현황 패널**

```promql
# 총 Throughput
sum(rate(application_usecase_command_requests_total[5m]))
+ sum(rate(application_usecase_query_requests_total[5m]))

# 전체 Error rate
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

**2. 핸들러별 비교 패널**

```promql
# 핸들러별 Throughput (Top 10)
topk(10,
  sum by (request_handler_name) (
    rate(application_usecase_command_requests_total[5m])
  )
)

# 핸들러별 Error rate
sum by (request_handler_name) (
  rate(application_usecase_command_responses_total{response_status="failure"}[5m])
)
/
sum by (request_handler_name) (
  rate(application_usecase_command_responses_total[5m])
)
```

**3. 에러 분석 패널**

```promql
# 에러 타입별 분포
sum by (error_type) (
  rate(application_usecase_command_responses_total{response_status="failure"}[5m])
)

# error code별 Top 10
topk(10,
  sum by (error_code) (
    rate(application_usecase_command_responses_total{response_status="failure"}[5m])
  )
)
```

### Grafana 대시보드 JSON 예시

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

**Situation:** "주문 생성이 느리다"는 보고가 들어왔습니다.

**Step 1: Check current status**

```promql
# CreateOrderCommandHandler의 P95 Response time
histogram_quantile(0.95,
  rate(application_usecase_command_duration_bucket{
    request_handler_name="CreateOrderCommandHandler"
  }[5m])
)
```

**Step 2: Check trends over time**

```promql
# 1시간 동안의 P95 추이
histogram_quantile(0.95,
  rate(application_usecase_command_duration_bucket{
    request_handler_name="CreateOrderCommandHandler"
  }[5m])
)
```

Grafana에서 1시간 범위로 그래프를 확인합니다.

**Step 3: Analyze sub-calls**

```promql
# 관련 Adapter의 Response time
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

PaymentGateway의 Response time이 급격히 증가했다면, 외부 결제 서비스의 지연이 근본 원인입니다.

### Scenario 2: Investigating Error Spike

**Situation:** Error rate이 평소 0.5%에서 5%로 급증했습니다.

**Step 1: Check error type**

```promql
# 에러 타입별 발생률
sum by (error_type) (
  rate(application_usecase_command_responses_total{
    response_status="failure"
  }[5m])
)
```

**Step 2: If system error**

```promql
# Exceptional error code별 분포
sum by (error_code) (
  rate(application_usecase_command_responses_total{
    error_type="exceptional"
  }[5m])
)
```

`Database.ConnectionFailed`가 대부분이라면 데이터베이스 상태를 확인합니다.

**Step 3: If business error**

```promql
# Expected error code별 분포
sum by (error_code) (
  rate(application_usecase_command_responses_total{
    error_type="expected"
  }[5m])
)
```

`Order.InsufficientStock`이 급증했다면 재고 부족 상황을 확인합니다.

---

## Troubleshooting

### When Metrics Are Not Being Collected

**Symptom:** Prometheus에서 특정 메트릭이 보이지 않습니다.

**Check the following:**

1. **Pipeline 등록 확인:**
   ```csharp
   services.AddMediator(options =>
   {
       options.AddOpenBehavior(typeof(UsecaseMetricsPipeline<,>));
   });
   ```

2. **OpenTelemetry 설정 확인:**
   ```csharp
   services.Configure<OpenTelemetryOptions>(options =>
   {
       options.ServiceNamespace = "mycompany.production";
   });
   ```

3. **Meter 필터 확인:**
   ```csharp
   builder.WithMetrics(metrics =>
   {
       metrics.AddMeter("mycompany.production.*");
   });
   ```

### Cardinality가 너무 높은 경우

**Symptom:** 메트릭 저장소의 디스크 사용량이 급격히 증가합니다.

**Cause:** 고유 값이 많은 태그가 포함되었습니다.

**확인 방법:**

```promql
# Cardinality 확인
count(application_usecase_command_responses_total)
```

**Solution:**

1. Remove unnecessary tags
2. Normalize tag values (e.g., use error codes instead of error messages)
3. Collect metrics only under specific conditions

### When Histogram Buckets Are Not Appropriate

**Symptom:** P95, P99 값이 부정확합니다.

**Cause:** 기본 버킷 경계가 실제 분포와 맞지 않습니다.

**Solution:**

Set custom bucket boundaries:

```csharp
// 예: 10ms, 25ms, 50ms, 100ms, 250ms, 500ms, 1s, 2.5s, 5s, 10s
var boundaries = new double[] { 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1.0, 2.5, 5.0, 10.0 };
```

---

## FAQ

### Q: Counter와 Gauge 중 무엇을 사용해야 하나요?

A: 값이 **누적**되는지 **현재 상태**인지에 따라 결정합니다:

- 요청 수, 에러 수, 처리된 바이트 → **Counter** (항상 증가)
- 활성 연결 수, 큐 크기, 메모리 사용량 → **Gauge** (증가/감소)

Functorium의 자동 메트릭은 요청/응답 수(Counter)와 processing time(Histogram)을 포함합니다.

### Q: rate()와 increase()의 차이는?

A:
- `rate()`: 초당 변화율 (requests/second)
- `increase()`: 지정 기간 동안의 총 증가량 (requests)

```promql
# Requests per second
rate(application_usecase_command_requests_total[5m])

# 5분 동안의 총 요청 수
increase(application_usecase_command_requests_total[5m])
```

### Q: 메트릭 보존 기간은 어떻게 설정하나요?

A: Prometheus 설정에서 `--storage.tsdb.retention.time` 옵션을 사용합니다:

```yaml
# 15일 보존
prometheus --storage.tsdb.retention.time=15d
```

장기 보존이 필요한 경우 Thanos나 Cortex 같은 장기 저장소를 사용합니다.

### Q: 특정 핸들러의 메트릭만 수집하려면?

A: OpenTelemetry의 View를 사용하여 필터링할 수 있습니다:

```csharp
meterProvider.AddView(
    instrumentName: "application.usecase.command.requests",
    new MetricStreamConfiguration
    {
        TagKeys = new[] { "request_handler" }
    }
);
```

### Q: 메트릭과 로그를 어떻게 연결하나요?

A: 동일한 tag key를 사용하므로 상관관계를 쉽게 추적할 수 있습니다:

1. 메트릭에서 이상 감지: `error_code="Database.ConnectionFailed"` 급증
2. 로그에서 상세 조사: `error.code = "Database.ConnectionFailed"`로 필터링

Grafana의 Explore 기능을 사용하면 메트릭에서 관련 로그로 쉽게 이동할 수 있습니다.

---

## References

- [OpenTelemetry Metrics Specification](https://opentelemetry.io/docs/specs/otel/metrics/)
- [Prometheus Query Language (PromQL)](https://prometheus.io/docs/prometheus/latest/querying/basics/)
- [Grafana Dashboard Best Practices](https://grafana.com/docs/grafana/latest/best-practices/best-practices-for-creating-dashboards/)
- [RED Method](https://www.weave.works/blog/the-red-method-key-metrics-for-microservices-architecture/)

**내부 문서:**
- [08-observability.md](../../spec/08-observability) — Observability 사양 (Field/Tag, Meter, 메시지 템플릿)
- [18b-observability-naming.md](./18b-observability-naming) — Observability 네이밍 가이드
- [19-observability-logging.md](./19-observability-logging) — Observability 로깅 상세
- [21-observability-tracing.md](./21-observability-tracing) — Observability 트레이싱 상세

---

## TagList Performance Optimization

`System.Diagnostics.Metrics`에서 메트릭 태그를 전달할 때 `TagList` 구조체를 사용하면 힙 할당을 방지하고 GC 부담을 최소화합니다.

### Problem: Array-Based Tag Passing

```csharp
// 매번 배열 객체가 힙에 할당됨
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
// TagList는 구조체로 스택에 할당됨
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

`TagList`는 8개까지 인라인 필드로 저장합니다 (힙 할당 없음). 9개 이상일 때만 내부 배열을 할당합니다.

| 방식 | 태그 4개 기준 할당 | GC 영향 |
|------|-------------------|---------|
| `KeyValuePair[]` | 96 bytes/호출 | Gen0 증가 |
| `TagList` | 0 bytes/호출 | 없음 |

### Supported APIs

| 메서드 | TagList 지원 |
|--------|-------------|
| `Counter<T>.Add(T, TagList)` | O |
| `Histogram<T>.Record(T, TagList)` | O |
| `UpDownCounter<T>.Add(T, TagList)` | O |
| `Measurement<T>(T, TagList)` | O |

### 주의 사항

- 태그 수를 **8개 이하**로 유지 (초과 시 내부 배열 할당 발생)
- 고빈도 메트릭에서 특히 중요
- OpenTelemetry 권장 사항도 태그 수 최소화
