---
title: "Functorium Logging Manual"
---

Using structured logging in the Functorium framework
to track application behavior and diagnose problems.

## Introduction

When software runs in production, answering the question "What is happening right now?" is critically important. Logging is the most fundamental way to answer this question.

Traditional logging recorded human-readable strings to files. However, in modern distributed systems, thousands of services generate tens of thousands of logs per second. In such an environment, it is nearly impossible to respond to a request like "find only the order processing logs for a specific user" using string searches.

Functorium provides **Structured Logging** that follows the OpenTelemetry standard. Structured logging means composing log messages as **searchable fields** rather than plain text.

### What You Will Learn

Through this document, you will learn:

1. **Why structured logging is important** - Limitations of traditional logging and advantages of structured logging
2. **How Functorium automatically generates logs** - Logging pipelines per architecture layer
3. **Meaning and usage of each log field** - Detailed explanation of request.*, response.*, error.* fields
4. **How to search and analyze logs** - Loki, Elasticsearch query examples

### Prerequisites

A basic understanding of the following concepts is required to understand this document:

- Basic C# and .NET syntax
- Basic logging concepts (Log Level, Logger, etc.)
- Understanding of JSON format

> **Core Principles:** Functorium structures logs into `request.*`, `response.*`, `error.*` fields, automatically classifying Expected/Exceptional via `error.type` and selecting the appropriate Log Level. Developers do not need to write separate logging code; the pipeline generates logs in a consistent format.

## Summary

### Key Commands

```
# Query all logs for a specific handler
request.handler.name = "CreateOrderCommandHandler"

# Query system errors only
error.type = "exceptional"

# Identify slow requests
response.elapsed > 1.0
```

### Key Procedures

1. Activate Logging Pipeline with `ConfigurePipelines(p => p.UseObservability())` (`UseObservability()` activates CtxEnricher, Metrics, Tracing, and Logging all at once)
2. Application Layer automatically generates logs via `UsecaseLoggingPipeline` (Event ID 1001-1004)
3. Adapter Layer uses Source Generator to auto-generate high-performance log code based on `LoggerMessage.Define` (Event ID 2001-2004)
4. On failure, `error.type` automatically classifies Expected/Exceptional and selects the appropriate Log Level

### Key Concepts

| Concept | Description |
|------|------|
| Structured logging | Compose logs as searchable fields (`request.*`, `response.*`, `error.*`) |
| Event ID | Classifies log types by Application(1001-1004) and Adapter(2001-2004) |
| `error.type` | `"expected"` (Warning), `"exceptional"` (Error), `"aggregate"` (composite) |
| `@error` | Structured error detail object (Serilog `@` prefix convention) |
| Information vs Debug | In Adapter, Information has basic info, Debug includes parameters/result values |

### DomainEvent Logging Summary

DomainEvent logging is divided into Publisher (Adapter layer) and Handler (Application layer):

| Item | DomainEvent Publisher | DomainEvent Handler |
|------|----------------------|---------------------|
| `request.layer` | `"adapter"` | `"application"` |
| `request.category.name` | `"event"` | `"usecase"` |
| `request.category.type` | - | `"event"` |
| Event ID range | 2001-2004 | 1001-1004 |

> For detailed field comparisons and message templates, see the [DomainEvent Logging](#domainevent-logging) section.

---

## Logging Fundamentals

### Traditional Logging vs Structured Logging

**Traditional logging** records human-readable strings:

```
2024-01-15 10:30:45 INFO CreateOrderCommandHandler started processing order for customer John
2024-01-15 10:30:46 INFO CreateOrderCommandHandler completed in 1.2s
2024-01-15 10:30:47 ERROR CreateOrderCommandHandler failed: Database connection timeout
```

This approach is intuitive and easy to read. However, it has several serious problems:

1. **Difficulty in searching**: To find all logs related to "CreateOrder", you must rely on string searches. If various representations such as "CreateOrderCommandHandler", "Create Order", "create_order" are mixed, searching becomes very difficult.

2. **Impossibility of aggregation**: To answer the question "What is the average processing time of CreateOrderCommandHandler in the last hour?", all logs must be parsed.

3. **Difficulty in correlation tracking**: When a single HTTP request passes through multiple services, finding related logs is very difficult.

**Structured logging** stores logs as searchable fields:

```json
{
  "timestamp": "2024-01-15T10:30:45Z",
  "level": "Information",
  "request.layer": "application",
  "request.category.name": "usecase",
  "request.category.type": "command",
  "request.handler.name": "CreateOrderCommandHandler",
  "request.handler.method": "Handle",
  "response.status": "success",
  "response.elapsed": 1.2
}
```

Now the following questions can be answered precisely:

- Query all logs for a specific handler with `request.handler.name = "CreateOrderCommandHandler"`
- Filter all failure logs with `response.status = "failure"`
- Calculate average processing time with `avg(response.elapsed) where request.handler.name = "CreateOrderCommandHandler"`

### OpenTelemetry Logging Standard

Functorium follows OpenTelemetry Semantic Conventions. OpenTelemetry is an industry standard for implementing Observability in cloud-native environments.

Following this standard provides these benefits:

1. **Tool compatibility**: Compatible with various observability tools such as Grafana Loki, Elasticsearch, and Datadog. You can freely choose tools without vendor lock-in.

2. **Cross-team consistency**: All services within an organization use the same field names. This prevents confusion where "handler name" is recorded as `handler_name` in one service and `handlerName` in another.

3. **Learning transfer**: Once learned, it can be applied to other projects. The same concepts apply across all systems using OpenTelemetry.

### Naming Convention: snake_case + dot notation

All logging fields in Functorium follow these rules:

- **snake_case**: Words are written in lowercase and connected with dots instead of underscores.
- **dot notation**: Hierarchical structure is expressed with dots.

**Example:**

| Incorrect | Correct | Description |
|-----------|-----------|------|
| `ResponseStatus` | `response.status` | Use lowercase instead of PascalCase |
| `response_status` | `response.status` | Use dots instead of underscores |
| `handlerMethod` | `request.handler.method` | Express hierarchy with dots |

Reasons for following these rules:

1. **OpenTelemetry Semantic Convention compliance**: Following the standard ensures tool compatibility.
2. **Compatibility with downstream systems**: Fields can be consistently referenced in dashboards and alert systems.
3. **Case sensitivity issue prevention**: Since all fields are lowercase, search failures due to case differences are prevented.

Now that we understand the need for structured logging and the OpenTelemetry standard from the logging fundamentals, let's look at how Functorium automates these principles across architecture layers.

---

## Functorium Logging Architecture

Functorium automatically generates logs from two architecture layers. Even without developers explicitly writing logs, the framework records logs in a consistent format.

### Architecture Layer Overview

```
+-----------------------------------------------------------+
|                       HTTP Request                        |
+-----------------------------+-----------------------------+
                              |
                              v
+-----------------------------------------------------------+
|                Application Layer (Usecase)                |
|  +-----------------------------------------------------+  |
|  |           UsecaseLoggingPipeline                    |  |
|  |  - Event ID: 1001-1004                              |  |
|  |  - request.layer: "application"                     |  |
|  |  - request.category.name:"usecase"                      |  |
|  |  - request.category.type: "command" / "query"        |  |
|  +-----------------------------------------------------+  |
+-----------------------------+-----------------------------+
                              |
                              v
+-----------------------------------------------------------+
|              Adapter Layer (Repository, Gateway, etc.)    |
|  +-----------------------------------------------------+  |
|  |           AdapterLoggingPipeline                    |  |
|  |  - Event ID: 2001-2004                              |  |
|  |  - request.layer: "adapter"                         |  |
|  |  - request.category.name:"repository", "gateway", etc.  |  |
|  |  - Auto-generated by Source Generator               |  |
|  +-----------------------------------------------------+  |
+-----------------------------------------------------------+
```

**Application Layer** handles business logic. Following the CQRS (Command Query Responsibility Segregation) pattern, it is divided into Commands (state changes) and Queries (data retrieval).

**Adapter Layer** handles integration with external systems. This includes Repository (database), Gateway (external API), Cache (cache system), and more.

### Log Generation Timing

In each layer, logs are generated at the following four points:

1. **Request Start (Request)**: Recorded when the handler receives a request. Used to track what requests have come in.

2. **Success Response**: Recorded when processing completes normally. Includes processing time and result.

3. **Warning Response**: Recorded when an expected business error occurs. For example, validation failure, permission denied, resource not found, etc. These errors are part of normal business flow, not system problems.

4. **Error Response**: Recorded when an exceptional system error occurs. Includes database connection failure, network timeout, unexpected exceptions, etc. These errors require immediate investigation.

### Event ID System

Functorium classifies logs by Event ID. Using Event IDs enables quick filtering of specific log types.

**Application Layer (1000 range):**

| Event ID | Name | Log Level | Description |
|----------|------|-----------|------|
| 1001 | `application.request` | Information | Request received |
| 1002 | `application.response.success` | Information | Success response |
| 1003 | `application.response.warning` | Warning | Expected error |
| 1004 | `application.response.error` | Error | Exceptional error |

**Adapter Layer (2000 range):**

| Event ID | Name | Log Level | Description |
|----------|------|-----------|------|
| 2001 | `adapter.request` | Information / Debug | Request received |
| 2002 | `adapter.response.success` | Information / Debug | Success response |
| 2003 | `adapter.response.warning` | Warning | Expected error |
| 2004 | `adapter.response.error` | Error | Exceptional error |

> **Number gap note:** The number gaps between 1001-1004 and 2001-2004 (1005-1999, 2005-2999) are intentionally reserved ranges for future expansion.

**Usage Examples:**

- Query all error logs: `EventId IN (1004, 2004)`
- Query Application Layer requests only: `EventId = 1001`
- Query warning and above logs: `EventId IN (1003, 1004, 2003, 2004)`

### Relationship Between Log Level and Error Type

Functorium automatically selects the appropriate Log Level based on the error type:

| Error Type | Log Level | Alert Required | Description |
|-----------|-----------|-----------|------|
| Expected | Warning | Optional | Normal rejection according to business rules |
| Exceptional | Error | Immediate | Processing failure due to system issues |
| Aggregate | Depends on inner type | Depends on inner type | When multiple errors are combined |

This distinction is important because operational monitoring needs to differentiate between **real problems** and **normal business flows**. "A user entered an invalid email" is a warning, but "the database is not responding" is an error requiring immediate action.

---

## Logging Field Detailed Guide

This section explains in detail the meaning and usage of each logging field generated by Functorium.

### Request Identification Fields

These fields answer the question "What code is currently executing?"

#### request.layer

```
Value: "application" or "adapter"
```

Indicates the architecture layer where the current log originated.

- **"application"**: Business logic layer (Usecase/Command/Query)
- **"adapter"**: External system integration layer (Repository, Gateway, etc.)

**Usage Examples:**

```
# Investigate business logic issues
request.layer = "application"

# Investigate database-related issues
request.layer = "adapter" AND request.category.name = "repository"
```

#### request.category.name

```
Application Layer: "usecase"
Adapter Layer: specific category name such as "repository", "gateway", etc.
```

Indicates the category of the request. In the Application Layer, it is always "usecase"; in the Adapter Layer, it indicates the specific adapter type.

**Usage Examples:**

```
# All Usecase logs
request.category.name = "usecase"

# Repository-related logs only
request.category.name = "repository"

# Gateway call logs only
request.category.name = "gateway"
```

#### request.category.type

```
Value: "command", "query", or "unknown"
Used only in Application Layer
```

In the CQRS (Command Query Responsibility Segregation) pattern, indicates whether the request is a Command or Query.

- **"command"**: Requests that change state (create, update, delete)
- **"query"**: Requests that retrieve data (read-only)
- **"unknown"**: When CQRS interfaces are not implemented

This distinction is useful for performance analysis. Generally:
- Commands include transactions and validation, resulting in longer processing times.
- Queries can be cached, resulting in shorter processing times.

**Usage Examples:**

```
# All Command processing logs
request.category.type = "command"

# Find slow Queries
request.category.type = "query" AND response.elapsed > 1.0
```

#### request.handler.name

```
Value: handler class name
Example: "CreateOrderCommandHandler", "OrderRepository"
```

The name of the class that processes the request. Only the class name is included, not the full namespace.

**Usage Examples:**

```
# Query all logs for a specific handler
request.handler.name = "CreateOrderCommandHandler"

# All calls to a specific Repository
request.handler.name = "OrderRepository"
```

#### request.handler.method

```
Application Layer: always "Handle"
Adapter Layer: actual method name (e.g., "GetById", "SaveAsync")
```

The name of the invoked method. In the Application Layer, the value is fixed since the "Handle" method is always called following the Mediator pattern. In the Adapter Layer, the actual method name that was called is recorded.

**Usage Examples:**

```
# Query only GetById calls on Repository
request.handler.name = "OrderRepository" AND request.handler.method = "GetById"
```

### Response Status Fields

These fields answer the question "How did the processing complete?"

#### response.status

```
Value: "success" or "failure"
```

The final result of request processing.

- **"success"**: Completed normally
- **"failure"**: Error occurred (includes both expected errors and exceptions)

**For Error Rate Calculation:**

```
Error rate = count(response.status = "failure") / count(*) x 100
```

**Usage Examples:**

```
# All failure logs
response.status = "failure"

# Calculate success rate for a specific handler
request.handler.name = "CreateOrderCommandHandler"
| stats count() by response.status
```

#### response.elapsed

```
Value: processing time in seconds (4 decimal places)
Example: 0.0234 (approximately 23.4ms)
```

The time elapsed from request start to response. This field is included only in success/failure response logs, not in request logs.

**For Performance Analysis:**

```
# Identify slow requests (1 second or more)
response.elapsed > 1.0

# Average processing time per handler
| stats avg(response.elapsed) by request.handler.name

# Calculate P95 response time
| stats percentile(response.elapsed, 95) by request.handler.name
```

### Error Information Fields

These fields answer the question "What went wrong?" They are included only when `response.status = "failure"`.

#### error.type

```
Value: "expected", "exceptional", or "aggregate"
```

The classification of the error:

| Value | Meaning | Example | Log Level |
|---|---|---|---|
| "expected" | Expected business error | Validation failure, permission denied, resource not found | Warning |
| "exceptional" | Exceptional system error | DB connection failure, timeout, unexpected exception | Error |
| "aggregate" | Multiple errors combined | Composite validation failure | Depends on inner type |

**Usage Examples:**

```
# Query system errors only (immediate action required)
error.type = "exceptional"

# Analyze business error patterns
error.type = "expected" | stats count() by error.code
```

#### error.code

```
Value: domain-specific error code
Example: "Order.NotFound", "Validation.InvalidEmail", "Database.ConnectionFailed"
```

The specific code of the error. This code has a hierarchical structure and is separated by dots (.).

**Code structure examples:**

- `Order.NotFound` - Order domain, resource not found
- `Validation.InvalidEmail` - Validation, invalid email
- `Database.ConnectionFailed` - Database, connection failure

**Usage Examples:**

```
# Count occurrences of a specific error code
error.code = "Order.NotFound" | count()

# Frequency by error code
| stats count() by error.code | sort count desc

# Alert configuration: when a specific error exceeds threshold
error.code = "Database.ConnectionFailed" AND count() > 10
```

#### @error

```
Value: structured error object (JSON)
```

An object containing the full error detail information. In logging systems, the `@` prefix is a Serilog convention indicating object fields.

**Example:**

```json
{
  "@error": {
    "ErrorKind": "ExpectedError",
    "Code": "Order.NotFound",
    "Message": "Order not found.",
    "CurrentValue": "12345"
  }
}
```

For Exceptional errors, exception information is included:

```json
{
  "@error": {
    "ErrorKind": "ExceptionalError",
    "Code": "Database.ConnectionFailed",
    "Exception": {
      "Type": "System.TimeoutException",
      "Message": "Connection timeout after 30 seconds",
      "StackTrace": "..."
    }
  }
}
```

**error.type vs @error.ErrorKind:**

These two fields serve different purposes:

| Field | Example value | Purpose |
|------|---------|------|
| `error.type` | "expected" | For filtering/querying (consistent values) |
| `@error.ErrorKind` | "ExpectedError" | For detailed analysis (actual class name) |

`error.type` is always one of three values, making it suitable for queries and filtering. `@error.ErrorKind` contains the actual error class name and is used for more detailed analysis.

---

## Application Layer Logging

The Application Layer is the core layer that handles business logic. `UsecaseLoggingPipeline` automatically generates logs.

### Message Template

Application Layer log messages follow these templates:

**Request log:**
```
{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} requesting with {@request.message}
```

**Success response log:**
```
{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {@response.message}
```

**Failure response log:**
```
{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}
```

### Dynamic Fields

In the Application Layer, the entire request and response objects are included in logs:

| Field | Description | Included at |
|------|------|-----------|
| `@request.message` | Full Command/Query object | Request log |
| `@response.message` | Full response object | Success response log |

**Example - Request log:**

```json
{
  "request.layer": "application",
  "request.category.name": "usecase",
  "request.category.type": "command",
  "request.handler.name": "CreateOrderCommandHandler",
  "request.handler.method": "Handle",
  "@request.message": {
    "CustomerId": "cust-123",
    "Items": [
      { "ProductId": "prod-001", "Quantity": 2 },
      { "ProductId": "prod-002", "Quantity": 1 }
    ]
  }
}
```

**Example - Success response log:**

```json
{
  "request.layer": "application",
  "request.category.name": "usecase",
  "request.category.type": "command",
  "request.handler.name": "CreateOrderCommandHandler",
  "request.handler.method": "Handle",
  "response.status": "success",
  "response.elapsed": 0.1234,
  "@response.message": {
    "OrderId": "ord-456",
    "Status": "Created",
    "TotalAmount": 150000
  }
}
```

### Field Structure Comparison Table

| Field | Request log | Success response | Failure response |
|------|-----------|-----------|-----------|
| `request.layer` | "application" | "application" | "application" |
| `request.category.name` | "usecase" | "usecase" | "usecase" |
| `request.category.type` | "command"/"query" | "command"/"query" | "command"/"query" |
| `request.handler.name` | Handler name | handler name | Handler name |
| `request.handler.method` | "Handle" | "Handle" | "Handle" |
| `@request.message` | Request object | - | - |
| `response.status` | - | "success" | "failure" |
| `response.elapsed` | - | Processing time | processing time |
| `@response.message` | - | Response object | - |
| `error.type` | - | - | Error Type |
| `error.code` | - | - | Error code |
| `@error` | - | - | Error object |

### Custom Logging via Ctx Enricher

In addition to the standard fields automatically generated by `UsecaseLoggingPipeline`, you can add custom fields tailored to business context. By implementing `IUsecaseCtxEnricher<TRequest, TResponse>`, custom attributes are automatically pushed to the Serilog `LogContext` when Request/Response logs are output.

#### IUsecaseCtxEnricher\<TRequest, TResponse\> Interface

```csharp
public interface IUsecaseCtxEnricher<in TRequest, in TResponse>
    where TResponse : IFinResponse
{
    IDisposable? EnrichRequest(TRequest request);
    IDisposable? EnrichResponse(TRequest request, TResponse response);
}
```

- `EnrichRequest`: Called before Request log output. Pushes additional attributes via `CtxEnricherContext.Push` and returns an `IDisposable`.
- `EnrichResponse`: Called before Response log output. Both Request and Response are passed as parameters, enabling response-based field additions.
- The returned `IDisposable` is automatically disposed after log output, cleaning up the scope.

#### Source Generator Auto-Generation (CtxEnricherGenerator)

When there is a Request record implementing `ICommandRequest<T>` or `IQueryRequest<T>`, `CtxEnricherGenerator` **automatically generates** the `IUsecaseCtxEnricher<TRequest, TResponse>` implementation code. Developers do not need to write Enrichers manually.

**Auto-generation rules:**

| Request/Response attribute type | Generated ctx field | Example |
|---------------------------|------------------|------|
| Scalar (string, int, decimal, etc.) | `ctx.{usecase}.request.{field}` | `ctx.place_order_command.request.customer_id` |
| Collection (List, Seq, etc.) | `ctx.{usecase}.request.{field}_count` | `ctx.place_order_command.request.lines_count` |
| Response scalar | `ctx.{usecase}.response.{field}` | `ctx.place_order_command.response.order_id` |
| Response collection | `ctx.{usecase}.response.{field}_count` | `ctx.place_order_command.response.items_count` |

**Generated code example (PlaceOrderCommand):**

Source Generator analyzes the attributes of `PlaceOrderCommand.Request` and `Response` to auto-generate the following Enricher:

```csharp
// Auto-generated code (PlaceOrderCommandRequestCtxEnricher.g.cs)
public partial class PlaceOrderCommandRequestCtxEnricher
    : IUsecaseCtxEnricher<PlaceOrderCommand.Request, FinResponse<PlaceOrderCommand.Response>>
{
    public IDisposable? EnrichRequest(PlaceOrderCommand.Request request)
    {
        var disposables = new List<IDisposable>(2);
        // [CtxRoot] interface attribute → Root Context
        disposables.Add(CtxEnricherContext.Push("ctx.customer_id", request.CustomerId));
        // Collection → _count auto-conversion
        disposables.Add(CtxEnricherContext.Push("ctx.place_order_command.request.lines_count", request.Lines?.Count ?? 0));
        OnEnrichRequest(request, disposables);  // partial extension point
        return new GeneratedCompositeDisposable(disposables);
    }

    public IDisposable? EnrichResponse(
        PlaceOrderCommand.Request request,
        FinResponse<PlaceOrderCommand.Response> response)
    {
        var disposables = new List<IDisposable>(3);
        if (response is FinResponse<PlaceOrderCommand.Response>.Succ { Value: var r })
        {
            disposables.Add(CtxEnricherContext.Push("ctx.place_order_command.response.order_id", r.OrderId));
            disposables.Add(CtxEnricherContext.Push("ctx.place_order_command.response.line_count", r.LineCount));
            disposables.Add(CtxEnricherContext.Push("ctx.place_order_command.response.total_amount", r.TotalAmount));
        }
        OnEnrichResponse(request, response, disposables);  // partial extension point
        return disposables.Count > 0 ? new GeneratedCompositeDisposable(disposables) : null;
    }

    // Extension point: add custom computed fields
    partial void OnEnrichRequest(PlaceOrderCommand.Request request, List<IDisposable> disposables);
    partial void OnEnrichResponse(PlaceOrderCommand.Request request,
        FinResponse<PlaceOrderCommand.Response> response, List<IDisposable> disposables);

    // Helper methods
    private static void PushRequestCtx(List<IDisposable> disposables, string fieldName, object? value)
        => disposables.Add(CtxEnricherContext.Push("ctx.place_order_command.request." + fieldName, value));
    private static void PushResponseCtx(List<IDisposable> disposables, string fieldName, object? value)
        => disposables.Add(CtxEnricherContext.Push("ctx.place_order_command.response." + fieldName, value));
    private static void PushRootCtx(List<IDisposable> disposables, string fieldName, object? value)
        => disposables.Add(CtxEnricherContext.Push("ctx." + fieldName, value));
}
```

#### partial Extension Point

Source Generator generates `partial void OnEnrichRequest()` and `partial void OnEnrichResponse()`. Used to add **computed fields** (calculated values) beyond the auto-generated fields:

```csharp
// PlaceOrderCommand.CtxEnricher.cs — manual partial extension
public partial class PlaceOrderCommandRequestCtxEnricher
{
    partial void OnEnrichRequest(
        PlaceOrderCommand.Request request,
        List<IDisposable> disposables)
    {
        decimal total = request.Lines.Sum(l => l.Quantity * l.UnitPrice);
        // → ctx.place_order_command.request.order_total_amount
        PushRequestCtx(disposables, "order_total_amount", total);
    }

    partial void OnEnrichResponse(
        PlaceOrderCommand.Request request,
        FinResponse<PlaceOrderCommand.Response> response,
        List<IDisposable> disposables)
    {
        if (response is FinResponse<PlaceOrderCommand.Response>.Succ { Value: var r } && r.LineCount > 0)
        {
            // → ctx.place_order_command.response.average_line_amount
            PushResponseCtx(disposables, "average_line_amount", r.TotalAmount / r.LineCount);
        }
    }
}
```

#### `[CtxRoot]` Attribute -- Root Context Field

**Location**: `Functorium.Abstractions.Observabilities.CtxRootAttribute`

When `[CtxRoot]` is applied to an interface or attribute, that attribute is promoted to `ctx.{field}` without the Usecase prefix.

```csharp
[CtxRoot]
public interface ICustomerRequest { string CustomerId { get; } }

public sealed record Request(string CustomerId, List<OrderLine> Lines)
    : ICommandRequest<Response>, ICustomerRequest;
// CustomerId → ctx.customer_id  (Root Level, no usecase prefix)
// Lines      → ctx.place_order_command.request.lines_count  (Usecase Level)
```

**Value of Root Context:** In OpenSearch, a single `ctx.customer_id: "CUST-001"` enables cross-searching **all Usecase activities** for that customer. There is no need to search `ctx.place_order_command.request.customer_id`, `ctx.get_order_summary_query.request.customer_id`, etc. separately for each Usecase.

#### `[CtxIgnore]` Attribute -- Exclude from Generation

**Location**: `Functorium.Applications.Usecases.CtxIgnoreAttribute`

When `[CtxIgnore]` is applied to a class or attribute, it is excluded from CtxEnricher auto-generation.

```csharp
// Class level: do not generate the entire Enricher for this Request
[CtxIgnore]
public sealed record Request(string Id) : IQueryRequest<Response>;

// Attribute level: exclude only a specific attribute
public sealed record Request(
    string CustomerId,
    [property: CtxIgnore] string InternalToken  // Excluded from Enricher
) : ICommandRequest<Response>;
```

#### Registration Method

Ctx Enricher is not `ICustomUsecasePipeline`, so it must be registered separately in DI. When using `UseObservability()`, CtxEnricher is automatically activated:

```csharp
// Register Source Generator-generated Enricher
services.AddScoped<
    IUsecaseCtxEnricher<PlaceOrderCommand.Request, FinResponse<PlaceOrderCommand.Response>>,
    PlaceOrderCommandRequestCtxEnricher>();
```

#### null-safe Behavior

`CtxEnricherPipeline` runs as the foremost Pipeline and injects `IUsecaseCtxEnricher<TRequest, TResponse>?` as an optional dependency (`= null`). For Usecases without a registered Enricher, subsequent Pipelines (Metrics, Tracing, Logging) execute without ctx.* fields. `UsecaseLoggingPipeline` does not directly inject the Enricher; instead, ctx.* fields are included in logs through the LogContext attributes previously pushed by `CtxEnricherPipeline`.

> **Reference**: [Custom Extension](../../spec/07-pipeline#custom-extension)

---

## Adapter Layer Logging

The Adapter Layer handles integration with external systems (databases, APIs, etc.). Source Generator automatically generates logging code and implements high-performance logging using `LoggerMessage.Define`.

### Message Template

Adapter Layer log messages follow these templates:

**Request log (Information -- 5 params):**
```
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} {@request.params} requesting
```

**Request log (Debug -- 6 params, with parameters + message):**
```
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} {@request.params} requesting with {@request.message}
```

**Success response log (Information -- 6 params):**
```
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s
```

**Success response log (Debug -- 7 params, with result):**
```
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {@response.message}
```

**Failure response log:**
```
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}
```

### Information vs Debug Levels

Two levels of logs are generated in the Adapter Layer:

**Information level:**
- Includes basic request/response information and `@request.params` (type-filtered parameter composite object)
- Does not include result values
- Always enabled in production environments

**Debug level:**
- Includes parameter values and result values
- Recommended to enable only in development environments as it may contain sensitive data
- Useful for detailed information during troubleshooting

### Dynamic Fields

In the Adapter Layer, method parameters and return values are dynamically recorded:

| Field | Description | Log Level |
|------|------|-----------|
| `@request.params` | Type-filtered parameter composite object | Information / Debug |
| `@request.message` | Full parameter object | Debug |
| `@response.message` | Method return value | Debug |

**Example - Request log (Debug):**

```json
{
  "request.layer": "adapter",
  "request.category.name": "repository",
  "request.handler.name": "OrderRepository",
  "request.handler.method": "GetByCustomerId",
  "@request.params": { "customer_id": "cust-123", "page_size": 10 },
  "@request.message": { "customer_id": "cust-123", "page_size": 10 }
}
```

**Example - Success response log (Debug):**

```json
{
  "request.layer": "adapter",
  "request.category.name": "repository",
  "request.handler.name": "OrderRepository",
  "request.handler.method": "GetByCustomerId",
  "response.status": "success",
  "response.elapsed": 0.0456,
  "@response.message": [{ "order_id": "ord-001" }, { "order_id": "ord-002" }]
}
```

### Field Structure Comparison Table

| Field | Request log | Success response | Failure response |
|------|-----------|-----------|-----------|
| `request.layer` | "adapter" | "adapter" | "adapter" |
| `request.category.name` | Category name | category name | Category name |
| `request.handler.name` | Handler name | handler name | Handler name |
| `request.handler.method` | Method name | method name | Method name |
| `@request.params` | Parameter object (Info/Debug) | - | - |
| `@request.message` | Parameter object (Debug) | - | - |
| `response.status` | - | "success" | "failure" |
| `response.elapsed` | - | Processing time | processing time |
| `@response.message` | - | Result value (Debug) | - |
| `error.type` | - | - | Error Type |
| `error.code` | - | - | Error code |
| `@error` | - | - | Error object |

---

## DomainEvent Logging

DomainEvent is a mechanism for notifying other components about events that occur in the domain model. In Functorium, DomainEvent observability consists of two components:

- **DomainEvent Publisher**: An Adapter layer component that publishes events (`request.layer: "adapter"`, `request.category.name: "event"`)
- **DomainEvent Handler**: An Application layer component that processes events (`request.layer: "application"`, `request.category.name: "usecase"`, `request.category.type: "event"`)

### Event ID System

Publisher and Handler each use the Event IDs of their respective layers:

| Component | Layer | Request | Success | Warning | Error |
|----------|--------|---------|---------|---------|-------|
| Publisher | Adapter (2000 range) | 2001 | 2002 | 2003 | 2004 |
| Handler | Application (1000 range) | 1001 | 1002 | 1003 | 1004 |

### Publisher Message Template

Publisher follows the Adapter layer pattern and distinguishes between single events (Publish) and tracked events (PublishTrackedEvents):

**Single event request (Publish):**
```
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} requesting with {@request.message}
```

**Tracked event request (PublishTrackedEvents):**
```
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} requesting with {request.event.count} events
```

**Success response:**
```
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s
```

**Success response (Aggregate):**
```
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {request.event.count} events
```

**Failure response:**
```
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}
```

**Failure response (Aggregate):**
```
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {request.event.count} events with {error.type}:{error.code} {@error}
```

**Partial failure response (PublishTrackedEvents):**
```
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {request.event.count} events partial failure: {response.event.success_count} succeeded, {response.event.failure_count} failed
```

### Handler Message Template

Handler follows the Application layer Usecase pattern, but with `request.category.type` set to `"event"`:

**Request:**
```
{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} {request.event.type} {request.event.id} requesting with {@request.message}
```

**Success response:**
```
{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} {request.event.type} {request.event.id} responded {response.status} in {response.elapsed:0.0000} s
```

**Failure response:**
```
{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} {request.event.type} {request.event.id} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}
```

### Field Comparison Table

Field comparison of Application Usecase, DomainEvent Publisher, and DomainEvent Handler:

| Field | Application Usecase | DomainEvent Publisher | DomainEvent Handler |
|-------|---------------------|----------------------|---------------------|
| `request.layer` | `"application"` | `"adapter"` | `"application"` |
| `request.category.name` | `"usecase"` | `"event"` | `"usecase"` |
| `request.category.type` | `"command"` / `"query"` | - | `"event"` |
| `request.handler.name` | Handler class name | Event/Aggregate type name | Handler class name |
| `request.handler.method` | `"Handle"` | `"Publish"` / `"PublishTrackedEvents"` | `"Handle"` |
| `request.event.type` | - | - | Event type name |
| `request.event.id` | - | - | Event unique ID |
| `@request.message` | Command/Query object | Event object | Event object |
| `@response.message` | Response object | - | - |
| `request.event.count` | - | O (PublishTrackedEvents only) | - |
| `response.event.success_count` | - | O (Partial Failure only) | - |
| `response.event.failure_count` | - | O (Partial Failure only) | - |
| `response.status` | `"success"` / `"failure"` | `"success"` / `"failure"` | `"success"` / `"failure"` |
| `response.elapsed` | Processing time (seconds) | Processing time (seconds) | Processing time (seconds) |
| `error.type` | `"expected"` / `"exceptional"` / `"aggregate"` | `"expected"` / `"exceptional"` | `"expected"` / `"exceptional"` |
| `error.code` | Error code | Error code | Error code |
| `@error` | Error object | Error object | Error object (Exception) |

### LayeredArch Scenario Log Examples

**Product creation success (`POST /api/products`):**

```
info: adapter event PublishTrackedEvents.PublishTrackedEvents requesting with 1 events
info: application usecase.event OnProductCreated.Handle ProductCreatedEvent 01J1234567890ABCDEFGHJKMNP requesting with {@request.message}
info: application usecase.event OnProductCreated.Handle ProductCreatedEvent 01J1234567890ABCDEFGHJKMNP responded success in 0.0001 s
info: adapter event PublishTrackedEvents.PublishTrackedEvents responded success in 0.0012 s with 1 events
```

**Handler exception (`POST /api/products` with `[handler-error]`):**

```
info: adapter event PublishTrackedEvents.PublishTrackedEvents requesting with 1 events
info: application usecase.event OnProductCreated.Handle ProductCreatedEvent 01J1234567890ABCDEFGHJKMNP requesting with {@request.message}
fail: application usecase.event OnProductCreated.Handle ProductCreatedEvent 01J1234567890ABCDEFGHJKMNP responded failure in 0.0008 s with exceptional:InvalidOperationException
fail: adapter event PublishTrackedEvents.PublishTrackedEvents responded failure in 0.0309 s with 1 events with exceptional:Application.DomainEventPublisher.PublishFailed {@error}
```

> **Note:** The `error.code` for exceptions from the Handler is the exception type name (`InvalidOperationException`), while the Publisher records a wrapped error code (`Application.DomainEventPublisher.PublishFailed`).

**Adapter exception (`POST /api/products` with `[adapter-error]`):**

Adapter exceptions occur in the Repository, so they do not reach event publishing:

```
fail: adapter repository InMemoryProductRepository.Create responded failure in 0.0005 s with exceptional:Exceptional {@error}
fail: application usecase.command CreateProductCommand.Handle responded failure in 0.0031 s with exceptional:Adapter.UsecaseExceptionPipeline`2.PipelineException {@error}
```

### IDomainEventCtxEnricher\<TEvent\> -- Event Handler Log Enrichment

Just as Usecases have `IUsecaseCtxEnricher`, DomainEvent Handlers have `IDomainEventCtxEnricher<TEvent>`. It adds business context fields to the Handler's Request/internal logs/Response.

#### Interface Definition

**Location**: `Functorium.Adapters.Events`

```csharp
public interface IDomainEventCtxEnricher<in TEvent> : IDomainEventCtxEnricher
    where TEvent : IDomainEvent
{
    IDisposable? EnrichLog(TEvent domainEvent);
}

// Non-generic base (for runtime resolution)
public interface IDomainEventCtxEnricher
{
    IDisposable? EnrichLog(IDomainEvent domainEvent);
}
```

#### ObservableDomainEventNotificationPublisher Integration

`ObservableDomainEventNotificationPublisher` resolves the Enricher for the event from DI via `ResolveEnrichment()` before calling the Handler:

```csharp
// Inside ObservableDomainEventNotificationPublisher
private IDisposable? ResolveEnrichment(IDomainEvent domainEvent)
{
    var enricherServiceType = typeof(IDomainEventCtxEnricher<>).MakeGenericType(domainEvent.GetType());
    return (_serviceProvider.GetService(enricherServiceType) as IDomainEventCtxEnricher)?.EnrichLog(domainEvent);
}
```

The returned `IDisposable` is applied to the entire Handler execution via `using` scope. Therefore, the same `ctx.*` fields are included in **all of** the Handler's Request logs, internal logs, and Response logs.

#### Source Generator Auto-Generation

`DomainEventCtxEnricherGenerator` detects classes implementing `IDomainEventHandler<T>` and auto-generates an `IDomainEventCtxEnricher<T>` implementation for `T` (the event type). In Layered Architecture, it detects Handlers in the Application project and collects event type attributes from referenced assemblies via SemanticModel.

```csharp
// Event definition (Domain project)
public sealed record OrderPlacedEvent(
    [CtxRoot] string CustomerId,
    string OrderId,
    int LineCount,
    decimal TotalAmount) : DomainEvent;

// Handler definition (Application project) — detects this class to auto-generate Enricher
public sealed class OrderPlacedEventHandler : IDomainEventHandler<OrderPlacedEvent>
{
    public ValueTask Handle(OrderPlacedEvent notification, CancellationToken ct) { ... }
}

// ↓ Code auto-generated by DomainEventCtxEnricherGenerator
public partial class OrderPlacedEventCtxEnricher
    : IDomainEventCtxEnricher<OrderPlacedEvent>
{
    public IDisposable? EnrichLog(OrderPlacedEvent domainEvent)
    {
        var disposables = new List<IDisposable>(4);
        disposables.Add(CtxEnricherContext.Push("ctx.customer_id", domainEvent.CustomerId));
        disposables.Add(CtxEnricherContext.Push("ctx.order_placed_event.order_id", domainEvent.OrderId));
        disposables.Add(CtxEnricherContext.Push("ctx.order_placed_event.line_count", domainEvent.LineCount));
        disposables.Add(CtxEnricherContext.Push("ctx.order_placed_event.total_amount", domainEvent.TotalAmount));
        OnEnrichLog(domainEvent, disposables);
        return new GeneratedCompositeDisposable(disposables);
    }

    partial void OnEnrichLog(OrderPlacedEvent domainEvent, List<IDisposable> disposables);
    // ...
}
```

- `[CtxRoot]` attribute is promoted to `ctx.{field}` Root Level.
- Applying `[CtxIgnore]` to an attribute/class excludes it from generation.
- Implementing `partial void OnEnrichLog()` allows adding computed fields.
- Even if the same event has multiple Handlers, only one Enricher is generated.

#### DI Registration

```csharp
services.AddScoped<
    IDomainEventCtxEnricher<OrderPlacedEvent>,
    OrderPlacedEventCtxEnricher>();
```

### ctx.* Field 4-Level System

"How to track all activities of customer CUST-001 in OpenSearch?" -- To answer this question, Functorium organizes `ctx.*` fields into 4 levels:

| Priority | Level | Field pattern | Generation method | Purpose |
|---------|-------|----------|----------|------|
| 1 | **Root Context** | `ctx.{field}` | `[CtxRoot]` interface/attribute | Cross-Usecase search (e.g., `ctx.customer_id`) |
| 2 | **Interface Context** | `ctx.{interface}.{field}` | Attribute derived from non-root interface | Semantic grouping (e.g., `ctx.operator_context.operator_id`) |
| 3 | **Usecase Context** | `ctx.{usecase}.{request\|response}.{field}` | Direct attribute without interface | Usecase-specific detail fields |
| 4 | **Event Context** | `ctx.{event_name}.{field}` | Auto-generated by `DomainEventCtxEnricherGenerator` | Domain Event handler fields |

**Interface Context rules:**

- If Request/Response implements a non-root interface, attributes derived from that interface are output in `ctx.{interface}.{field}` format.
- Interface name conversion: remove `I` prefix → snake_case (`IOperatorContext` → `operator_context`, `IPartnerContext` → `partner_context`)
- In inheritance chains, it is determined based on the **declaring interface**. In `IPartnerContext : IRegional`, `RegionCode` is declared in `IRegional`, so it becomes `ctx.regional.region_code`.

```csharp
public interface IOperatorContext { string OperatorId { get; } }
public interface IRegional { string RegionCode { get; } }
public interface IPartnerContext : IRegional { string PartnerId { get; } }

[CtxRoot]
public interface ICustomerRequest { string CustomerId { get; } }

public sealed record Request(
    string CustomerId,      // → ctx.customer_id          (Root)
    List<OrderLine> Lines,  // → ctx.{usecase}.request.lines_count  (Usecase)
    string OperatorId,      // → ctx.operator_context.operator_id   (Interface)
    string RegionCode,      // → ctx.regional.region_code           (Interface)
    string PartnerId)       // → ctx.partner_context.partner_id     (Interface)
    : ICommandRequest<Response>, ICustomerRequest, IOperatorContext, IPartnerContext;
```

**OpenSearch query examples:**

```
# Track all activities per customer (Root Context)
ctx.customer_id: "CUST-001"

# All activities of a specific operator (Interface Context)
ctx.operator_context.operator_id: "admin@example.com"

# Request details for a specific Usecase (Usecase Context)
ctx.place_order_command.request.lines_count: [3 TO *]

# Details of a specific event (Event Context)
ctx.order_placed_event.total_amount: [100000 TO *]

# Root + Interface combination: specific customer's specific operator activity
ctx.customer_id: "CUST-001" AND ctx.operator_context.operator_id: "admin@example.com"
```

#### OpenSearchJsonFormatter Conversion Rules

`OpenSearchJsonFormatter` preserves `ctx.*` fields as **flat fields**. The `ctx.` prefixed attributes added via Serilog's `CtxEnricherContext.Push` become field names as-is in OpenSearch.

| Serilog LogContext attribute | OpenSearch field | Notes |
|------------------------|----------------|------|
| `ctx.customer_id` | `ctx.customer_id` | Root -- cross-search |
| `ctx.operator_context.operator_id` | `ctx.operator_context.operator_id` | Interface -- semantic grouping |
| `ctx.place_order_command.request.lines_count` | `ctx.place_order_command.request.lines_count` | Usecase -- detail |
| `ctx.order_placed_event.order_id` | `ctx.order_placed_event.order_id` | Event -- detail |
| PascalCase unrecognized attribute | `ctx.snake_case` | Safety net conversion |

---

## Understanding Error Logging

Functorium classifies errors into three types. Each type requires different responses.

### Expected Error

**Definition:** Errors expected according to business rules. They can occur even when the system is functioning normally.

**Example:**
- Validation failure (invalid email format)
- Resource not found (non-existent order ID)
- Permission denied (resource without access rights)
- Business rule violation (insufficient stock)

**Log example:**

```json
{
  "level": "Warning",
  "eventId": 1003,
  "request.layer": "application",
  "request.handler.name": "CreateOrderCommandHandler",
  "response.status": "failure",
  "error.type": "expected",
  "error.code": "Order.InsufficientStock",
  "@error": {
    "ErrorKind": "ExpectedError",
    "Code": "Order.InsufficientStock",
    "Message": "Insufficient stock.",
    "CurrentValue": "ProductId: prod-001, Requested: 10, Available: 3"
  }
}
```

**Response approach:**
- No separate response required by default
- If a specific error code surges, review from a business perspective is needed
- Example: if `Order.InsufficientStock` surges, check inventory management

### Exceptional Error

**Definition:** Exceptional errors caused by system issues. Requires immediate investigation and response.

**Example:**
- Database connection failure
- External API timeout
- Network errors
- Unexpected exceptions (NullReferenceException, etc.)

**Log example:**

```json
{
  "level": "Error",
  "eventId": 1004,
  "request.layer": "application",
  "request.handler.name": "CreateOrderCommandHandler",
  "response.status": "failure",
  "error.type": "exceptional",
  "error.code": "Database.ConnectionFailed",
  "@error": {
    "ErrorKind": "ExceptionalError",
    "Code": "Database.ConnectionFailed",
    "Exception": {
      "Type": "System.TimeoutException",
      "Message": "Connection timeout after 30 seconds",
      "StackTrace": "at Npgsql.NpgsqlConnection..."
    }
  }
}
```

**Response approach:**
- Send alerts immediately
- Check system status (DB, network, external services)
- Restart services or respond to incidents as needed

### Aggregate Error

**Definition:** When multiple errors are combined. Typically occurs when validation of multiple fields fails simultaneously.

**Example:**
- Simultaneous validation failure across multiple fields
- Partial failure among multiple service calls

**Log example:**

```json
{
  "level": "Warning",
  "eventId": 1003,
  "request.layer": "application",
  "request.handler.name": "CreateUserCommandHandler",
  "response.status": "failure",
  "error.type": "aggregate",
  "error.code": "Validation.NameRequired",
  "@error": {
    "ErrorKind": "ManyErrors",
    "Errors": [
      {
        "ErrorKind": "ExpectedError",
        "Code": "Validation.NameRequired",
        "Message": "Name is required."
      },
      {
        "ErrorKind": "ExpectedError",
        "Code": "Validation.EmailInvalid",
        "Message": "Invalid email."
      }
    ]
  }
}
```

**Note:** The first (Primary) error code is recorded in `error.code`. The full error list can be found in `@error.Errors`.

### Error Type Determination Logic

The Log Level for Aggregate Errors is determined by the inner error types:

1. If there is at least one Exceptional error inside → Error level
2. If all inner errors are Expected → Warning level

This approach determines the Log Level based on the "most severe error."

Now that we understand the structure and fields of logs automatically generated by Functorium, let's learn how to search and analyze these structured logs in production environments.

---

## Log Search and Analysis

### Basic Search Patterns

**All logs for a specific handler:**
```
request.handler.name = "CreateOrderCommandHandler"
```

**Query only failed requests:**
```
response.status = "failure"
```

**Slow requests in a specific time window:**
```
response.elapsed > 1.0 AND @timestamp > "2024-01-15T00:00:00Z"
```

**Query system errors only:**
```
error.type = "exceptional"
```

### Grafana Loki Query Examples

**Calculate error rate per handler:**
```logql
sum by (request_handler_name) (
  count_over_time({response_status="failure"}[1h])
)
/
sum by (request_handler_name) (
  count_over_time({request_layer="application"}[1h])
)
* 100
```

**Frequency by error code:**
```logql
sum by (error_code) (
  count_over_time({error_type="expected"}[1h])
)
```

**Slow request trends (P95):**
```logql
quantile_over_time(0.95,
  {request_layer="application"}
  | json
  | unwrap response_elapsed [1h]
)
```

### Elasticsearch Query Examples

**Average response time per handler:**
```json
{
  "aggs": {
    "handlers": {
      "terms": { "field": "request.handler.name.keyword" },
      "aggs": {
        "avg_elapsed": { "avg": { "field": "response.elapsed" } }
      }
    }
  }
}
```

**Error occurrence trend by time:**
```json
{
  "query": {
    "bool": {
      "filter": [
        { "term": { "response.status": "failure" } }
      ]
    }
  },
  "aggs": {
    "errors_over_time": {
      "date_histogram": {
        "field": "@timestamp",
        "fixed_interval": "5m"
      }
    }
  }
}
```

---

## Exercise: Analyzing Logs

### Scenario 1: Investigating Performance Degradation

**Situation:** The operations team reported that "order creation is slow."

**Step 1: Determine the scope of the problem**
```
request.handler.name = "CreateOrderCommandHandler"
AND response.elapsed > 1.0
| stats count(), avg(response.elapsed), p95(response.elapsed)
```

**Step 2: Check trends over time**
```
request.handler.name = "CreateOrderCommandHandler"
| timechart avg(response.elapsed) by 1h
```

**Step 3: Analyze sub-calls**
```
request.layer = "adapter"
AND request.handler.name IN ("OrderRepository", "PaymentGateway")
| stats avg(response.elapsed) by request.handler.name, request.handler.method
```

**Step 4: Identify root cause**

If the analysis above shows that `PaymentGateway.ProcessPayment` response time has increased dramatically, the root cause is latency from the external payment service.

### Scenario 2: Analyzing Error Patterns

**Situation:** Warning logs have increased 3x compared to normal.

**Step 1: Check distribution by error code**
```
error.type = "expected"
| stats count() by error.code
| sort count desc
```

**Step 2: Detailed analysis of a specific error code**
```
error.code = "Validation.InvalidEmail"
| stats count() by hour(@timestamp)
```

**Step 3: Check related request examples**
```
error.code = "Validation.InvalidEmail"
| head 10
| fields @request.message
```

**Conclusion:** If invalid email formats have increased since a specific point in time, the frontend validation may not be working properly.

---

## Troubleshooting

### Logs Are Not Being Recorded

**Symptom:** No logs are visible for a specific handler.

**Check the following:**
1. Verify Log Level settings (ensure minimum Log Level in `appsettings.json` is Information or above)
2. Verify Pipeline registration (ensure `UsecaseLoggingPipeline` is registered in the DI container)
3. Verify filter conditions (ensure search query filter conditions are not too restrictive)

### Field Values Are Empty

**Symptom:** `request.category.type` value is recorded as "unknown".

**Cause:** The Request class does not implement the `ICommandRequest<T>` or `IQueryRequest<T>` interface.

**Solution:** Implement the appropriate CQRS interface on the Request class.

### Response Time Is Abnormally Large

**Symptom:** `response.elapsed` value is much larger than expected.

**Check the following:**
1. Check sub-Adapter call times
2. Check synchronous/asynchronous call patterns
3. Check database query execution plans

---

## FAQ

### Q: How to prevent sensitive information from being included in logs?

A: There are two methods:

1. **Attribute-level exclusion:** Use `[JsonIgnore]` attribute to exclude specific fields from serialization.

```csharp
public record CreateUserCommand(
    string Email,
    [property: JsonIgnore] string Password  // Not included in logs
) : ICommandRequest<UserId>;
```

2. **Log Level adjustment:** Disable Debug level logs in production to prevent parameter values from being recorded.

### Q: How to add custom fields?

A: Functorium supports custom log fields through three methods:

1. **Source Generator auto-generation (recommended):** When implementing `ICommandRequest<T>` or `IQueryRequest<T>`, `CtxEnricherGenerator` automatically generates scalar attributes of Request/Response in `ctx.{usecase}.{request|response}.{field}` format. No separate code writing required.

2. **partial extension point:** Implement `OnEnrichRequest()` / `OnEnrichResponse()` as partial implementations of the auto-generated Enricher to add computed fields (calculated values).

3. **Domain Event Enricher:** When implementing `IDomainEventHandler<TEvent>`, `DomainEventCtxEnricherGenerator` automatically generates fields in `ctx.{event_name}.{field}` format for event handlers.

Using the `[CtxRoot]` attribute, you can generate Root Context fields (`ctx.{field}`) that enable cross-Usecase searching.

> **Details**: See the [Custom Logging via Ctx Enricher](#custom-logging-via-ctx-enricher) section.

### Q: When should Debug logs be enabled in the Adapter Layer?

A: Enable Debug logs in these situations:

- **Development environment:** Always enable for detailed information
- **Staging environment:** Enable during integration testing
- **Production environment:** Enable temporarily only when troubleshooting is needed

Note: Debug logs include parameter values and result values, so sensitive data may be exposed.

### Q: How to reduce log storage costs?

A: Consider the following strategies:

1. **Sampling:** Sample only 10% of success logs, keep 100% of failure logs
2. **TTL settings:** Auto-delete old logs (e.g., Information 7 days, Error 30 days)
3. **Log Level adjustment:** Disable Debug logs in production
4. **Field optimization:** Exclude unnecessary dynamic fields

### Q: What format should be used when searching by event.id?

A: It depends on the log system:

- **Serilog + Seq:** `EventId.Id = 1004`
- **Grafana Loki:** `{EventId="1004"}`
- **Elasticsearch:** `eventId.id: 1004`

Check the field mapping settings for each system.

---

## Reference Documents

- [OpenTelemetry Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/)
- [Serilog Structured Logging](https://serilog.net/)
- [Grafana Loki LogQL](https://grafana.com/docs/loki/latest/logql/)

**Internal documents:**
- [08-observability.md](../../spec/08-observability) -- Observability specification (Field/Tag, Meter, message templates)
- [18b-observability-naming.md](../18b-observability-naming) -- Observability naming guide
- [20-observability-metrics.md](../20-observability-metrics) -- Observability metrics details
- [21-observability-tracing.md](../21-observability-tracing) -- Observability tracing details
- [07-domain-events.md](../domain/07-domain-events) -- Domain events and handler Observability
