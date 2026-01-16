# Functorium
[![Build](https://github.com/hhko/Functorium/actions/workflows/build.yml/badge.svg)](https://github.com/hhko/Functorium/actions/workflows/build.yml) [![Publish](https://github.com/hhko/Functorium/actions/workflows/publish.yml/badge.svg)](https://github.com/hhko/Functorium/actions/workflows/publish.yml)

> A functional domain is functor + dominium, seasoned with fun, designed to bridge **the age of deterministic rules** and **the age of probabilistic intuition**.

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

| Field/Tag | Logging | Metrics | Tracing | Description |
|-----------|---------|---------|---------|-------------|
| `request.layer` | ✅ | ✅ | ✅ | Architecture layer (`"application"`) |
| `request.category` | ✅ | ✅ | ✅ | Request category (`"usecase"`) |
| `request.handler.cqrs` | ✅ | ✅ | ✅ | CQRS type (`"command"`, `"query"`) |
| `request.handler` | ✅ | ✅ | ✅ | Handler class name |
| `request.handler.method` | ✅ | ✅ | ✅ | Handler method name (`"Handle"`) |
| `response.status` | ✅ | ✅ | ✅ | Response status (`"success"`, `"failure"`) |
| `response.elapsed` | ✅ | - | ✅ | Processing time in seconds |
| `error.type` | ✅ | ✅ | ✅ | Error classification (`"expected"`, `"exceptional"`, `"aggregate"`) |
| `error.code` | ✅ | ✅ | ✅ | Domain-specific error code |
| `@error` | ✅ | - | - | Structured error object (detailed) |

**Adapter Layer:**

| Field/Tag | Logging | Metrics | Tracing | Description |
|-----------|---------|---------|---------|-------------|
| `request.layer` | ✅ | ✅ | ✅ | Architecture layer (`"adapter"`) |
| `request.category` | ✅ | ✅ | ✅ | Adapter category (e.g., `"repository"`) |
| `request.handler` | ✅ | ✅ | ✅ | Handler class name |
| `request.handler.method` | ✅ | ✅ | ✅ | Handler method name |
| `response.status` | ✅ | ✅ | ✅ | Response status (`"success"`, `"failure"`) |
| `response.elapsed` | ✅ | - | ✅ | Processing time in seconds |
| `error.type` | ✅ | ✅ | ✅ | Error classification (`"expected"`, `"exceptional"`, `"aggregate"`) |
| `error.code` | ✅ | ✅ | ✅ | Domain-specific error code |
| `@error` | ✅ | - | - | Structured error object (detailed) |

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
| `response.elapsed` | Processing time (s) | Processing time (s) | Elapsed time in seconds |
| `error.type` | `"expected"` / `"exceptional"` / `"aggregate"` | `"expected"` / `"exceptional"` / `"aggregate"` | Error classification |
| `error.code` | Error code | Error code | Domain-specific error code |
| `@error` | Error object (structured) | Error object (structured) | Error data (detailed) |
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
{request.layer} {request.category}.{request.handler.cqrs} {request.handler}.{request.handler.method} {@response.message} responded {response.status} in {response.elapsed:0.0000} s

# Response - Warning/Error
{request.layer} {request.category}.{request.handler.cqrs} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}
```

**Message Templates (Adapter Layer):**

```
# Request (Information)
{request.layer} {request.category} {request.handler}.{request.handler.method} requesting

# Request (Debug) - with parameters
{request.layer} {request.category} {request.handler}.{request.handler.method} {request.params.items} {request.params.items.count} requesting

# Response (Information)
{request.layer} {request.category} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s

# Response (Debug) - with result
{request.layer} {request.category} {request.handler}.{request.handler.method} {response.result} responded {response.status} in {response.elapsed:0.0000} s

# Response Warning/Error
{request.layer} {request.category} {request.handler}.{request.handler.method} responded failure in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}
```

**Error Field Values (`error.type` vs `@error.ErrorType`):**

> `error.type` and `@error.ErrorType` use different value formats for different purposes.

| Error Type | `error.type` (Filtering) | `@error.ErrorType` (Detail) |
|------------|--------------------------|----------------------------|
| Expected Error | `"expected"` | `"ErrorCodeExpected"` |
| Exceptional Error | `"exceptional"` | `"ErrorCodeExceptional"` |
| Aggregate Error | `"aggregate"` | `"ManyErrors"` |
| LanguageExt Expected | `"expected"` | `"Expected"` |
| LanguageExt Exceptional | `"exceptional"` | `"Exceptional"` |

- **`error.type`**: Standardized values for log filtering/querying (consistent with Metrics/Tracing)
- **`@error.ErrorType`**: Actual class name for detailed error type identification

**Implementation:**

| Layer | Method | Note |
|-------|--------|------|
| Application | Direct `ILogger.LogXxx()` calls | 7+ parameters exceed `LoggerMessage.Define` limit of 6 |
| Adapter | `LoggerMessage.Define` delegates | Zero allocation, high performance |

### Metrics

**Meter Name:**

| Layer | Meter Name Pattern | Example (`ServiceNamespace = "mycompany.production"`) |
|-------|-------------------|-------------------------------------------------------|
| Application | `{service.namespace}.application` | `mycompany.production.application` |
| Adapter | `{service.namespace}.adapter.{category}` | `mycompany.production.adapter.repository` |

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
| **Total Tags** | **5** | **5** | **6** | **8** |

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

| Error Case | error.type | error.code | Description |
|------------|------------|------------|-------------|
| `IHasErrorCode` + `IsExpected` | `"expected"` | Error code | Expected business logic error with error code |
| `IHasErrorCode` + `IsExceptional` | `"exceptional"` | Error code | Exceptional system error with error code |
| `ManyErrors` | `"aggregate"` | Primary error code | Multiple errors aggregated (Exceptional takes priority) |
| `Expected` (LanguageExt) | `"expected"` | Type name | LanguageExt base expected error without error code |
| `Exceptional` (LanguageExt) | `"exceptional"` | Type name | LanguageExt base exceptional error without error code |

**Implementation:**

| Layer | Method | Note |
|-------|--------|------|
| Application | `IPipelineBehavior` + `IMeterFactory` | Mediator pipeline |
| Adapter | Source Generator | Auto-generated metrics instruments |

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
| `response.elapsed` | Processing time (s) | Processing time (s) | Elapsed time in seconds |
| **Error Tags** | | | |
| `error.type` | `"expected"` / `"exceptional"` / `"aggregate"` | `"expected"` / `"exceptional"` / `"aggregate"` | Error classification |
| `error.code` | Error code | Error code | Error code |
| **ActivityStatus** | `Ok` / `Error` | `Ok` / `Error` | OpenTelemetry status |

**Error Type Tag Values:**

| Error Case | error.type | error.code | Description |
|------------|------------|------------|-------------|
| `IHasErrorCode` + `IsExpected` | `"expected"` | Error code | Expected business logic error with error code |
| `IHasErrorCode` + `IsExceptional` | `"exceptional"` | Error code | Exceptional system error with error code |
| `ManyErrors` | `"aggregate"` | Primary error code | Multiple errors aggregated (Exceptional takes priority) |
| `Expected` (LanguageExt) | `"expected"` | Type name | LanguageExt base expected error without error code |
| `Exceptional` (LanguageExt) | `"exceptional"` | Type name | LanguageExt base exceptional error without error code |

**Implementation:**

| Layer | Method | Note |
|-------|--------|------|
| Application | `IPipelineBehavior` + `ActivitySource.StartActivity()` | Mediator pipeline |
| Adapter | Source Generator | Auto-generated Activity spans |
