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

1. `ConfigurePipelines(p => p.UseObservability())`로 Logging Pipeline 활성화 (`UseObservability()`는 CtxEnricher, Metrics, Tracing, Logging을 일괄 활성화)
2. Application Layer는 `UsecaseLoggingPipeline`이 자동으로 로그 생성 (Event ID 1001-1004)
3. Adapter Layer는 Source Generator가 `LoggerMessage.Define` 기반 고성능 로그 코드 자동 생성 (Event ID 2001-2004)
4. failure 시 `error.type`으로 Expected/Exceptional 자동 분류, 적절한 Log Level 자동 Optional

### Key Concepts

| Concept | Description |
|------|------|
| Structured logging | Compose logs as searchable fields (`request.*`, `response.*`, `error.*`) |
| Event ID | Application(1001-1004), Adapter(2001-2004)로 로그 유형 분류 |
| `error.type` | `"expected"` (Warning), `"exceptional"` (Error), `"aggregate"` (composite) |
| `@error` | 구조화된 오류 상세 객체 (Serilog `@` 접두사 관례) |
| Information vs Debug | In Adapter, Information has basic info, Debug includes parameters/result values |

### DomainEvent Logging Summary

DomainEvent의 로깅은 Publisher(Adapter 레이어)와 Handler(Application 레이어)로 구분됩니다:

| Item | DomainEvent Publisher | DomainEvent Handler |
|------|----------------------|---------------------|
| `request.layer` | `"adapter"` | `"application"` |
| `request.category.name` | `"event"` | `"usecase"` |
| `request.category.type` | - | `"event"` |
| Event ID 범위 | 2001-2004 | 1001-1004 |

> 상세 필드 비교와 메시지 템플릿은 [DomainEvent 로깅](#domainevent-로깅) 섹션을 참조하세요.

---

## Logging Fundamentals

### Traditional Logging vs Structured Logging

**전통적 로깅**은 사람이 읽기 쉬운 문자열을 기록합니다:

```
2024-01-15 10:30:45 INFO CreateOrderCommandHandler started processing order for customer John
2024-01-15 10:30:46 INFO CreateOrderCommandHandler completed in 1.2s
2024-01-15 10:30:47 ERROR CreateOrderCommandHandler failed: Database connection timeout
```

이 방식은 직관적이고 읽기 쉽습니다. 그러나 몇 가지 심각한 문제점이 있습니다:

1. **검색의 어려움**: "CreateOrder"와 관련된 모든 로그를 찾으려면 문자열 검색에 의존해야 합니다. "CreateOrderCommandHandler", "Create Order", "create_order" 등 다양한 표현이 섞여 있으면 검색이 매우 어려워집니다.

2. **집계의 불가능**: "지난 1시간 동안 CreateOrderCommandHandler의 평균 processing time은?"이라는 질문에 답하려면 모든 로그를 파싱해야 합니다.

3. **상관관계 추적의 어려움**: 하나의 HTTP 요청이 여러 서비스를 거칠 때, 관련된 로그를 찾는 것이 매우 어렵습니다.

**구조화된 로깅**은 로그를 검색 가능한 필드로 저장합니다:

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

이제 다음과 같은 질문에 정확하게 답할 수 있습니다:

- `request.handler.name = "CreateOrderCommandHandler"`로 특정 핸들러의 모든 로그 조회
- `response.status = "failure"`로 모든 failure 로그 필터링
- `avg(response.elapsed) where request.handler.name = "CreateOrderCommandHandler"`로 평균 processing time 계산

### OpenTelemetry Logging Standard

Functorium은 OpenTelemetry 시맨틱 컨벤션(Semantic Conventions)을 따릅니다. OpenTelemetry는 클라우드 네이티브 환경에서 관찰 가능성(Observability)을 구현하기 위한 업계 표준입니다.

이 표준을 따르면 다음과 같은 이점이 있습니다:

1. **도구 호환성**: Grafana Loki, Elasticsearch, Datadog 등 다양한 관찰 가능성 도구와 호환됩니다. 특정 벤더에 종속되지 않고 자유롭게 도구를 Optional할 수 있습니다.

2. **팀 간 일관성**: 조직 내 모든 서비스가 동일한 필드 이름을 사용합니다. "핸들러 이름"이 어떤 서비스에서는 `handler_name`, 다른 서비스에서는 `handlerName`으로 기록되는 혼란을 방지합니다.

3. **학습 전이**: 한 번 배우면 다른 프로젝트에서도 활용할 수 있습니다. OpenTelemetry를 사용하는 모든 시스템에서 동일한 개념이 적용됩니다.

### Naming Convention: snake_case + dot notation

Functorium의 모든 로깅 필드는 다음 규칙을 따릅니다:

- **snake_case**: 단어를 소문자로 작성하고 언더스코어가 아닌 점(dot)으로 연결합니다.
- **dot notation**: 계층 구조를 점으로 표현합니다.

**Example:**

| 잘못된 예 | 올바른 예 | Description |
|-----------|-----------|------|
| `ResponseStatus` | `response.status` | PascalCase 대신 소문자 사용 |
| `response_status` | `response.status` | 언더스코어 대신 점 사용 |
| `handlerMethod` | `request.handler.method` | 계층 구조를 점으로 표현 |

이 규칙을 따르는 이유:

1. **OpenTelemetry 시맨틱 컨벤션 준수**: 표준을 따름으로써 도구 호환성을 확보합니다.
2. **다운스트림 시스템과의 호환성**: 대시보드, 알림 시스템에서 필드를 일관되게 참조할 수 있습니다.
3. **대소문자 민감성 문제 방지**: 모든 필드가 소문자이므로 대소문자 차이로 인한 검색 failure를 방지합니다.

로깅의 기초에서 구조화된 로깅의 필요성과 OpenTelemetry 표준을 이해했습니다. 이제 Functorium이 이 원칙을 아키텍처 레이어별로 어떻게 자동화하는지 살펴봅니다.

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

**Application Layer**는 비즈니스 로직을 담당합니다. CQRS(Command Query Responsibility Segregation) 패턴에 따라 Command(상태 변경)와 Query(데이터 조회)로 구분됩니다.

**Adapter Layer**는 외부 시스템과의 연동을 담당합니다. Repository(데이터베이스), Gateway(외부 API), Cache(캐시 시스템) 등이 포함됩니다.

### Log Generation Timing

In each layer, logs are generated at the following four points:

1. **요청 시작 (Request)**: 핸들러가 요청을 받았을 때 기록됩니다. 어떤 요청이 들어왔는지 추적하는 데 사용됩니다.

2. **success 응답 (Success Response)**: 처리가 정상 완료되었을 때 기록됩니다. processing time과 결과가 포함됩니다.

3. **경고 응답 (Warning Response)**: 예상된 비즈니스 오류가 발생했을 때 기록됩니다. 예를 들어, 유효성 검사 failure, 권한 없음, 리소스 없음 등이 해당됩니다. 이러한 오류는 시스템 문제가 아니라 정상적인 비즈니스 흐름의 일부입니다.

4. **에러 응답 (Error Response)**: 예외적 시스템 오류가 발생했을 때 기록됩니다. 데이터베이스 연결 failure, 네트워크 타임아웃, 예상치 못한 예외 등이 해당됩니다. 이러한 오류는 즉시 조사가 필요합니다.

### Event ID System

Functorium classifies logs by Event ID. Event ID를 활용하면 특정 유형의 로그만 빠르게 필터링할 수 있습니다.

**Application Layer (1000번대):**

| Event ID | Name | Log Level | Description |
|----------|------|-----------|------|
| 1001 | `application.request` | Information | Request received |
| 1002 | `application.response.success` | Information | Success response |
| 1003 | `application.response.warning` | Warning | Expected error |
| 1004 | `application.response.error` | Error | Exceptional error |

**Adapter Layer (2000번대):**

| Event ID | Name | Log Level | Description |
|----------|------|-----------|------|
| 2001 | `adapter.request` | Information / Debug | Request received |
| 2002 | `adapter.response.success` | Information / Debug | Success response |
| 2003 | `adapter.response.warning` | Warning | Expected error |
| 2004 | `adapter.response.error` | Error | Exceptional error |

> **번호 갭 안내:** 1001-1004와 2001-2004 사이의 번호 갭(1005-1999, 2005-2999)은 향후 확장을 위해 의도적으로 예약된 범위입니다.

**Usage Examples:**

- 모든 에러 로그 조회: `EventId IN (1004, 2004)`
- Application Layer 요청만 조회: `EventId = 1001`
- 경고 이상의 로그 조회: `EventId IN (1003, 1004, 2003, 2004)`

### Relationship Between Log Level and Error Type

Functorium automatically selects the appropriate Log Level based on the error type:

| Error Type | Log Level | Alert Required | Description |
|-----------|-----------|-----------|------|
| Expected (예상된 오류) | Warning | Optional적 | Normal rejection according to business rules |
| Exceptional (예외적 오류) | Error | Immediate | Processing failure due to system issues |
| Aggregate (복합 오류) | Depends on inner type | 내부 타입에 따름 | When multiple errors are combined |

이 구분이 중요한 이유는 운영 모니터링에서 **진짜 문제**와 **정상적인 비즈니스 흐름**을 구분해야 하기 때문입니다. "사용자가 잘못된 이메일을 입력했다"는 경고지만, "데이터베이스가 응답하지 않는다"는 즉시 대응이 필요한 에러입니다.

---

## Logging Field Detailed Guide

This section explains in detail the meaning and usage of each logging field generated by Functorium.

### Request Identification Fields

이 필드들은 "어떤 코드가 실행되고 있는가?"라는 질문에 답합니다.

#### request.layer

```
값: "application" 또는 "adapter"
```

현재 로그가 발생한 아키텍처 레이어를 나타냅니다.

- **"application"**: 비즈니스 로직 레이어 (Usecase/Command/Query)
- **"adapter"**: 외부 시스템 연동 레이어 (Repository, Gateway 등)

**Usage Examples:**

```
# 비즈니스 로직 문제 조사
request.layer = "application"

# 데이터베이스 관련 문제 조사
request.layer = "adapter" AND request.category.name = "repository"
```

#### request.category.name

```
Application Layer: "usecase"
Adapter Layer: "repository", "gateway" 등 구체적인 category name
```

요청의 카테고리를 나타냅니다. Application Layer에서는 항상 "usecase"이고, Adapter Layer에서는 구체적인 어댑터 종류를 나타냅니다.

**Usage Examples:**

```
# 모든 Usecase 로그
request.category.name = "usecase"

# Repository 관련 로그만
request.category.name = "repository"

# Gateway 호출 로그만
request.category.name = "gateway"
```

#### request.category.type

```
값: "command", "query", 또는 "unknown"
Application Layer에서만 사용
```

CQRS(Command Query Responsibility Segregation) 패턴에서 요청이 Command인지 Query인지를 나타냅니다.

- **"command"**: 상태를 변경하는 요청 (생성, 수정, 삭제)
- **"query"**: 데이터를 조회하는 요청 (읽기 전용)
- **"unknown"**: CQRS 인터페이스를 구현하지 않은 경우

이 구분은 성능 분석에 유용합니다. 일반적으로:
- Command는 트랜잭션과 검증이 포함되어 processing time이 깁니다.
- Query는 캐싱이 가능하여 processing time이 짧습니다.

**Usage Examples:**

```
# 모든 Command 처리 로그
request.category.type = "command"

# 느린 Query 찾기
request.category.type = "query" AND response.elapsed > 1.0
```

#### request.handler.name

```
값: 핸들러 클래스 이름
예: "CreateOrderCommandHandler", "OrderRepository"
```

요청을 처리하는 클래스의 이름입니다. 전체 네임스페이스가 아닌 클래스 이름만 포함됩니다.

**Usage Examples:**

```
# Query all logs for a specific handler
request.handler.name = "CreateOrderCommandHandler"

# 특정 Repository의 모든 호출
request.handler.name = "OrderRepository"
```

#### request.handler.method

```
Application Layer: 항상 "Handle"
Adapter Layer: 실제 메서드 이름 (예: "GetById", "SaveAsync")
```

호출된 메서드의 이름입니다. Application Layer에서는 Mediator 패턴에 따라 항상 "Handle" 메서드가 호출되므로 값이 고정됩니다. Adapter Layer에서는 실제 호출된 메서드 이름이 기록됩니다.

**Usage Examples:**

```
# Repository의 GetById 호출만 조회
request.handler.name = "OrderRepository" AND request.handler.method = "GetById"
```

### Response Status Fields

이 필드들은 "처리가 어떻게 완료되었는가?"라는 질문에 답합니다.

#### response.status

```
값: "success" 또는 "failure"
```

요청 처리의 최종 결과입니다.

- **"success"**: 정상 처리 완료
- **"failure"**: 오류 발생 (예상된 오류 또는 예외 모두 포함)

**For Error Rate Calculation:**

```
에러율 = count(response.status = "failure") / count(*) × 100
```

**Usage Examples:**

```
# 모든 failure 로그
response.status = "failure"

# 특정 핸들러의 success률 계산
request.handler.name = "CreateOrderCommandHandler"
| stats count() by response.status
```

#### response.elapsed

```
값: in seconds processing time (소수점 4자리)
예: 0.0234 (약 23.4ms)
```

요청 시작부터 응답까지 걸린 시간입니다. 이 필드는 success/failure 응답 로그에만 포함되며, 요청 로그에는 포함되지 않습니다.

**For Performance Analysis:**

```
# 느린 요청 식별 (1초 이상)
response.elapsed > 1.0

# 핸들러별 평균 processing time
| stats avg(response.elapsed) by request.handler.name

# P95 응답 시간 계산
| stats percentile(response.elapsed, 95) by request.handler.name
```

### Error Information Fields

이 필드들은 "무엇이 잘못되었는가?"라는 질문에 답합니다. `response.status = "failure"`인 경우에만 포함됩니다.

#### error.type

```
값: "expected", "exceptional", 또는 "aggregate"
```

에러의 분류입니다:

| Value | Meaning | Example | Log Level |
|---|---|---|---|
| "expected" | 예상된 비즈니스 오류 | 유효성 검사 failure, 권한 없음, 리소스 없음 | Warning |
| "exceptional" | 예외적 시스템 오류 | DB 연결 failure, 타임아웃, 예상치 못한 예외 | Error |
| "aggregate" | 여러 오류가 결합됨 | 복합 유효성 검사 failure | Depends on inner type |

**Usage Examples:**

```
# 시스템 오류만 조회 (즉시 대응 필요)
error.type = "exceptional"

# 비즈니스 오류 패턴 분석
error.type = "expected" | stats count() by error.code
```

#### error.code

```
값: 도메인별 error code
예: "Order.NotFound", "Validation.InvalidEmail", "Database.ConnectionFailed"
```

에러의 구체적인 코드입니다. 이 코드는 계층적 구조를 가지며, 점(.)으로 구분됩니다.

**코드 구조 예시:**

- `Order.NotFound` - 주문 도메인, 리소스 없음
- `Validation.InvalidEmail` - 유효성 검사, 잘못된 이메일
- `Database.ConnectionFailed` - 데이터베이스, 연결 failure

**Usage Examples:**

```
# 특정 error code 발생 횟수
error.code = "Order.NotFound" | count()

# error code별 발생 빈도
| stats count() by error.code | sort count desc

# 알림 설정: 특정 에러가 임계값 초과 시
error.code = "Database.ConnectionFailed" AND count() > 10
```

#### @error

```
값: 구조화된 에러 객체 (JSON)
```

에러의 전체 상세 정보를 담은 객체입니다. 로그 시스템에서 `@` 접두사는 객체 필드를 나타내는 Serilog 관례입니다.

**Example:**

```json
{
  "@error": {
    "ErrorType": "ErrorCodeExpected",
    "Code": "Order.NotFound",
    "Message": "주문을 찾을 수 없습니다.",
    "CurrentValue": "12345"
  }
}
```

Exceptional 에러의 경우 예외 정보가 포함됩니다:

```json
{
  "@error": {
    "ErrorType": "ErrorCodeExceptional",
    "Code": "Database.ConnectionFailed",
    "Exception": {
      "Type": "System.TimeoutException",
      "Message": "Connection timeout after 30 seconds",
      "StackTrace": "..."
    }
  }
}
```

**error.type vs @error.ErrorType:**

두 필드는 서로 다른 목적으로 사용됩니다:

| Field | 값 예시 | Purpose |
|------|---------|------|
| `error.type` | "expected" | 필터링/쿼리용 (일관된 값) |
| `@error.ErrorType` | "ErrorCodeExpected" | 상세 분석용 (실제 클래스명) |

`error.type`은 항상 세 가지 값 중 하나이므로 쿼리와 필터링에 적합합니다. `@error.ErrorType`은 실제 에러 클래스 이름을 포함하여 더 상세한 분석에 사용됩니다.

---

## Application Layer Logging

The Application Layer is the core layer that handles business logic. `UsecaseLoggingPipeline`이 자동으로 로그를 생성합니다.

### Message Template

Application Layer의 로그 메시지는 다음 템플릿을 따릅니다:

**요청 로그:**
```
{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} requesting with {@request.message}
```

**success 응답 로그:**
```
{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {@response.message}
```

**failure 응답 로그:**
```
{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}
```

### Dynamic Fields

Application Layer에서는 요청과 응답 객체 전체가 로그에 포함됩니다:

| Field | Description | Included at |
|------|------|-----------|
| `@request.message` | Full Command/Query object | Request log |
| `@response.message` | Full response object | Success response log |

**Example - 요청 로그:**

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

**Example - success 응답 로그:**

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

기본 `UsecaseLoggingPipeline`이 자동으로 생성하는 표준 필드 외에, 비즈니스 맥락에 맞는 커스텀 필드를 추가할 수 있습니다. `IUsecaseCtxEnricher<TRequest, TResponse>`를 구현하면 Request/Response 로그 출력 시 Serilog `LogContext`에 커스텀 attribute을 자동으로 Push합니다.

#### IUsecaseCtxEnricher\<TRequest, TResponse\> 인터페이스

```csharp
public interface IUsecaseCtxEnricher<in TRequest, in TResponse>
    where TResponse : IFinResponse
{
    IDisposable? EnrichRequest(TRequest request);
    IDisposable? EnrichResponse(TRequest request, TResponse response);
}
```

- `EnrichRequest`: Request 로그 출력 전에 호출됩니다. `CtxEnricherContext.Push`로 추가 attribute을 Push하고 `IDisposable`을 반환합니다.
- `EnrichResponse`: Response 로그 출력 전에 호출됩니다. Request와 Response 모두 파라미터로 전달되어 응답 기반 필드 추가가 가능합니다.
- 반환된 `IDisposable`은 로그 출력 후 자동으로 Dispose되어 스코프가 정리됩니다.

#### Source Generator 자동 생성 (CtxEnricherGenerator)

`ICommandRequest<T>` 또는 `IQueryRequest<T>`를 구현하는 Request record가 있으면, `CtxEnricherGenerator`가 `IUsecaseCtxEnricher<TRequest, TResponse>` 구현 코드를 **자동으로 생성**합니다. 개발자가 직접 Enricher를 작성할 필요가 없습니다.

**자동 생성 규칙:**

| Request/Response attribute 타입 | 생성되는 ctx 필드 | Example |
|---------------------------|------------------|------|
| 스칼라 (string, int, decimal 등) | `ctx.{usecase}.request.{field}` | `ctx.place_order_command.request.customer_id` |
| 컬렉션 (List, Seq 등) | `ctx.{usecase}.request.{field}_count` | `ctx.place_order_command.request.lines_count` |
| Response 스칼라 | `ctx.{usecase}.response.{field}` | `ctx.place_order_command.response.order_id` |
| Response 컬렉션 | `ctx.{usecase}.response.{field}_count` | `ctx.place_order_command.response.items_count` |

**생성 코드 예시 (PlaceOrderCommand):**

Source Generator가 `PlaceOrderCommand.Request`와 `Response`의 attribute을 분석하여 다음과 같은 Enricher를 자동 생성합니다:

```csharp
// 자동 생성된 코드 (PlaceOrderCommandRequestCtxEnricher.g.cs)
public partial class PlaceOrderCommandRequestCtxEnricher
    : IUsecaseCtxEnricher<PlaceOrderCommand.Request, FinResponse<PlaceOrderCommand.Response>>
{
    public IDisposable? EnrichRequest(PlaceOrderCommand.Request request)
    {
        var disposables = new List<IDisposable>(2);
        // [CtxRoot] 인터페이스의 attribute → Root Context
        disposables.Add(CtxEnricherContext.Push("ctx.customer_id", request.CustomerId));
        // 컬렉션 → _count 자동 변환
        disposables.Add(CtxEnricherContext.Push("ctx.place_order_command.request.lines_count", request.Lines?.Count ?? 0));
        OnEnrichRequest(request, disposables);  // partial 확장 포인트
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
        OnEnrichResponse(request, response, disposables);  // partial 확장 포인트
        return disposables.Count > 0 ? new GeneratedCompositeDisposable(disposables) : null;
    }

    // 확장 포인트: 커스텀 computed 필드 추가 가능
    partial void OnEnrichRequest(PlaceOrderCommand.Request request, List<IDisposable> disposables);
    partial void OnEnrichResponse(PlaceOrderCommand.Request request,
        FinResponse<PlaceOrderCommand.Response> response, List<IDisposable> disposables);

    // 헬퍼 메서드
    private static void PushRequestCtx(List<IDisposable> disposables, string fieldName, object? value)
        => disposables.Add(CtxEnricherContext.Push("ctx.place_order_command.request." + fieldName, value));
    private static void PushResponseCtx(List<IDisposable> disposables, string fieldName, object? value)
        => disposables.Add(CtxEnricherContext.Push("ctx.place_order_command.response." + fieldName, value));
    private static void PushRootCtx(List<IDisposable> disposables, string fieldName, object? value)
        => disposables.Add(CtxEnricherContext.Push("ctx." + fieldName, value));
}
```

#### partial 확장 포인트

Source Generator가 `partial void OnEnrichRequest()`와 `partial void OnEnrichResponse()`를 생성합니다. 자동 생성된 필드 외에 **computed 필드**(계산된 값)를 추가할 때 사용합니다:

```csharp
// PlaceOrderCommand.CtxEnricher.cs — 수동 partial 확장
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

#### `[CtxRoot]` 어트리뷰트 — Root Context 필드

**Location**: `Functorium.Abstractions.Observabilities.CtxRootAttribute`

`[CtxRoot]`를 인터페이스 또는 attribute에 적용하면, 해당 attribute이 Usecase prefix 없이 `ctx.{field}`로 승격됩니다.

```csharp
[CtxRoot]
public interface ICustomerRequest { string CustomerId { get; } }

public sealed record Request(string CustomerId, List<OrderLine> Lines)
    : ICommandRequest<Response>, ICustomerRequest;
// CustomerId → ctx.customer_id  (Root Level, usecase prefix 없음)
// Lines      → ctx.place_order_command.request.lines_count  (Usecase Level)
```

**Root Context의 가치:** OpenSearch에서 `ctx.customer_id: "CUST-001"` 하나로 해당 고객의 **모든 Usecase 활동**을 교차 검색할 수 있습니다. Usecase마다 `ctx.place_order_command.request.customer_id`, `ctx.get_order_summary_query.request.customer_id`를 각각 검색할 필요가 없습니다.

#### `[CtxIgnore]` 어트리뷰트 — 생성 제외

**Location**: `Functorium.Applications.Usecases.CtxIgnoreAttribute`

`[CtxIgnore]`를 클래스 또는 attribute에 적용하면 CtxEnricher 자동 생성에서 제외됩니다.

```csharp
// 클래스 레벨: 해당 Request의 Enricher 전체를 생성하지 않음
[CtxIgnore]
public sealed record Request(string Id) : IQueryRequest<Response>;

// attribute 레벨: 특정 attribute만 제외
public sealed record Request(
    string CustomerId,
    [property: CtxIgnore] string InternalToken  // Enricher에서 제외
) : ICommandRequest<Response>;
```

#### Registration Method

Ctx Enricher는 `ICustomUsecasePipeline`이 아니므로 별도로 DI에 등록합니다. `UseObservability()` 사용 시 CtxEnricher가 자동 활성화됩니다:

```csharp
// Source Generator가 생성한 Enricher 등록
services.AddScoped<
    IUsecaseCtxEnricher<PlaceOrderCommand.Request, FinResponse<PlaceOrderCommand.Response>>,
    PlaceOrderCommandRequestCtxEnricher>();
```

#### null-safe 동작

`CtxEnricherPipeline`이 최선두 Pipeline으로 `IUsecaseCtxEnricher<TRequest, TResponse>?`를 optional 의존성(`= null`)으로 주입받습니다. Enricher가 등록되지 않은 Usecase에서는 ctx.* 필드 없이 후속 Pipeline(Metrics, Tracing, Logging)이 실행됩니다. `UsecaseLoggingPipeline`은 Enricher를 직접 주입받지 않으며, `CtxEnricherPipeline`이 사전에 Push한 LogContext attribute을 통해 ctx.* 필드가 로그에 포함됩니다.

> **참조**: [커스텀 확장](../../spec/07-pipeline#커스텀-확장)

---

## Adapter Layer Logging

The Adapter Layer handles integration with external systems (databases, APIs, etc.). Source Generator가 자동으로 로깅 코드를 생성하며, `LoggerMessage.Define`을 사용하여 고성능 로깅을 구현합니다.

### Message Template

Adapter Layer의 로그 메시지는 다음 템플릿을 따릅니다:

**요청 로그 (Information — 5 params):**
```
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} {@request.params} requesting
```

**요청 로그 (Debug — 6 params, 파라미터 + 메시지 포함):**
```
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} {@request.params} requesting with {@request.message}
```

**success 응답 로그 (Information — 6 params):**
```
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s
```

**success 응답 로그 (Debug — 7 params, 결과 포함):**
```
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {@response.message}
```

**failure 응답 로그:**
```
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}
```

### Information vs Debug Levels

Adapter Layer에서는 두 가지 레벨의 로그가 생성됩니다:

**Information 레벨:**
- Basic request/response information와 `@request.params` (type-filtered 파라미터 복합 객체) 포함
- 결과 값은 포함하지 않음
- 운영 환경에서 항상 활성화

**Debug 레벨:**
- 파라미터 값과 결과 값 포함
- 민감한 데이터가 포함될 수 있으므로 개발 환경에서만 활성화 권장
- 문제 해결 시 상세 정보 확인에 유용

### Dynamic Fields

Adapter Layer에서는 메서드 파라미터와 반환값이 동적으로 기록됩니다:

| Field | Description | Log Level |
|------|------|-----------|
| `@request.params` | type-filtered 파라미터 복합 객체 | Information / Debug |
| `@request.message` | 전체 파라미터 객체 | Debug |
| `@response.message` | 메서드 반환값 | Debug |

**Example - 요청 로그 (Debug):**

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

**Example - success 응답 로그 (Debug):**

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

DomainEvent는 도메인 모델에서 발생한 이벤트를 다른 컴포넌트에 알리는 메커니즘입니다. Functorium에서 DomainEvent의 관측성은 두 가지 컴포넌트로 구성됩니다:

- **DomainEvent Publisher**: 이벤트를 발행하는 Adapter 레이어 컴포넌트 (`request.layer: "adapter"`, `request.category.name: "event"`)
- **DomainEvent Handler**: 이벤트를 처리하는 Application 레이어 컴포넌트 (`request.layer: "application"`, `request.category.name: "usecase"`, `request.category.type: "event"`)

### Event ID System

Publisher와 Handler는 각각 소속 레이어의 Event ID를 사용합니다:

| 컴포넌트 | Layer | Request | Success | Warning | Error |
|----------|--------|---------|---------|---------|-------|
| Publisher | Adapter (2000번대) | 2001 | 2002 | 2003 | 2004 |
| Handler | Application (1000번대) | 1001 | 1002 | 1003 | 1004 |

### Publisher Message Template

Publisher는 Adapter 레이어 패턴을 따르며, 단일 이벤트(Publish)와 추적 이벤트(PublishTrackedEvents)를 구분합니다:

**단일 이벤트 요청 (Publish):**
```
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} requesting with {@request.message}
```

**추적 이벤트 요청 (PublishTrackedEvents):**
```
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} requesting with {request.event.count} events
```

**success 응답:**
```
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s
```

**success 응답 (Aggregate):**
```
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {request.event.count} events
```

**failure 응답:**
```
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}
```

**failure 응답 (Aggregate):**
```
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {request.event.count} events with {error.type}:{error.code} {@error}
```

**부분 failure 응답 (PublishTrackedEvents):**
```
{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {request.event.count} events partial failure: {response.event.success_count} succeeded, {response.event.failure_count} failed
```

### Handler Message Template

Handler는 Application 레이어 Usecase 패턴을 따르되, `request.category.type`이 `"event"`입니다:

**요청:**
```
{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} {request.event.type} {request.event.id} requesting with {@request.message}
```

**success 응답:**
```
{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} {request.event.type} {request.event.id} responded {response.status} in {response.elapsed:0.0000} s
```

**failure 응답:**
```
{request.layer} {request.category.name}.{request.category.type} {request.handler.name}.{request.handler.method} {request.event.type} {request.event.id} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}
```

### Field Comparison Table

Application Usecase, DomainEvent Publisher, DomainEvent Handler의 필드 비교:

| Field | Application Usecase | DomainEvent Publisher | DomainEvent Handler |
|-------|---------------------|----------------------|---------------------|
| `request.layer` | `"application"` | `"adapter"` | `"application"` |
| `request.category.name` | `"usecase"` | `"event"` | `"usecase"` |
| `request.category.type` | `"command"` / `"query"` | - | `"event"` |
| `request.handler.name` | Handler 클래스명 | Event/Aggregate 타입명 | Handler 클래스명 |
| `request.handler.method` | `"Handle"` | `"Publish"` / `"PublishTrackedEvents"` | `"Handle"` |
| `request.event.type` | - | - | Event type name |
| `request.event.id` | - | - | Event unique ID |
| `@request.message` | Command/Query 객체 | Event object | 이벤트 객체 |
| `@response.message` | Response object | - | - |
| `request.event.count` | - | O (PublishTrackedEvents만) | - |
| `response.event.success_count` | - | O (Partial Failure만) | - |
| `response.event.failure_count` | - | O (Partial Failure만) | - |
| `response.status` | `"success"` / `"failure"` | `"success"` / `"failure"` | `"success"` / `"failure"` |
| `response.elapsed` | Processing time (seconds) | processing time(초) | Processing time (seconds) |
| `error.type` | `"expected"` / `"exceptional"` / `"aggregate"` | `"expected"` / `"exceptional"` | `"expected"` / `"exceptional"` |
| `error.code` | Error code | 오류 코드 | Error code |
| `@error` | Error object | 오류 객체 | 오류 객체 (Exception) |

### LayeredArch Scenario Log Examples

**상품 생성 success (`POST /api/products`):**

```
info: adapter event PublishTrackedEvents.PublishTrackedEvents requesting with 1 events
info: application usecase.event OnProductCreated.Handle ProductCreatedEvent 01J1234567890ABCDEFGHJKMNP requesting with {@request.message}
info: application usecase.event OnProductCreated.Handle ProductCreatedEvent 01J1234567890ABCDEFGHJKMNP responded success in 0.0001 s
info: adapter event PublishTrackedEvents.PublishTrackedEvents responded success in 0.0012 s with 1 events
```

**핸들러 예외 (`POST /api/products` with `[handler-error]`):**

```
info: adapter event PublishTrackedEvents.PublishTrackedEvents requesting with 1 events
info: application usecase.event OnProductCreated.Handle ProductCreatedEvent 01J1234567890ABCDEFGHJKMNP requesting with {@request.message}
fail: application usecase.event OnProductCreated.Handle ProductCreatedEvent 01J1234567890ABCDEFGHJKMNP responded failure in 0.0008 s with exceptional:InvalidOperationException
fail: adapter event PublishTrackedEvents.PublishTrackedEvents responded failure in 0.0309 s with 1 events with exceptional:ApplicationErrors.DomainEventPublisher.PublishFailed {@error}
```

> **Note:** Handler에서 발생한 예외의 `error.code`는 예외 타입명(`InvalidOperationException`)이고, Publisher에서는 이를 래핑한 error code(`ApplicationErrors.DomainEventPublisher.PublishFailed`)가 기록됩니다.

**어댑터 예외 (`POST /api/products` with `[adapter-error]`):**

어댑터 예외는 Repository에서 발생하므로 이벤트 발행까지 도달하지 않습니다:

```
fail: adapter repository InMemoryProductRepository.Create responded failure in 0.0005 s with exceptional:Exceptional {@error}
fail: application usecase.command CreateProductCommand.Handle responded failure in 0.0031 s with exceptional:AdapterErrors.UsecaseExceptionPipeline`2.PipelineException {@error}
```

### IDomainEventCtxEnricher\<TEvent\> — 이벤트 핸들러 로그 Enrichment

Usecase에 `IUsecaseCtxEnricher`가 있듯이, DomainEvent Handler에는 `IDomainEventCtxEnricher<TEvent>`가 있습니다. Handler의 Request/내부 로그/Response 모두에 비즈니스 컨텍스트 필드를 추가합니다.

#### 인터페이스 정의

**Location**: `Functorium.Adapters.Events`

```csharp
public interface IDomainEventCtxEnricher<in TEvent> : IDomainEventCtxEnricher
    where TEvent : IDomainEvent
{
    IDisposable? EnrichLog(TEvent domainEvent);
}

// 비제네릭 베이스 (런타임 해석용)
public interface IDomainEventCtxEnricher
{
    IDisposable? EnrichLog(IDomainEvent domainEvent);
}
```

#### ObservableDomainEventNotificationPublisher 통합

`ObservableDomainEventNotificationPublisher`는 Handler를 호출하기 전에 `ResolveEnrichment()`로 DI에서 해당 이벤트의 Enricher를 해석합니다:

```csharp
// ObservableDomainEventNotificationPublisher 내부
private IDisposable? ResolveEnrichment(IDomainEvent domainEvent)
{
    var enricherServiceType = typeof(IDomainEventCtxEnricher<>).MakeGenericType(domainEvent.GetType());
    return (_serviceProvider.GetService(enricherServiceType) as IDomainEventCtxEnricher)?.EnrichLog(domainEvent);
}
```

반환된 `IDisposable`은 `using` 스코프로 Handler 전체 실행에 적용됩니다. 따라서 Handler의 Request 로그, Handler 내부 로그, Response 로그 **모두에** 동일한 `ctx.*` 필드가 포함됩니다.

#### Source Generator 자동 생성

`DomainEventCtxEnricherGenerator`가 `IDomainEventHandler<T>` 구현 클래스를 감지하여 `T`(이벤트 타입)에 대한 `IDomainEventCtxEnricher<T>` 구현체를 자동 생성합니다. Layered Architecture에서는 Application 프로젝트의 Handler를 감지하고, 참조 어셈블리의 이벤트 타입 attribute을 SemanticModel로 수집합니다.

```csharp
// 이벤트 정의 (Domain 프로젝트)
public sealed record OrderPlacedEvent(
    [CtxRoot] string CustomerId,
    string OrderId,
    int LineCount,
    decimal TotalAmount) : DomainEvent;

// Handler 정의 (Application 프로젝트) — 이 클래스를 감지하여 Enricher 자동 생성
public sealed class OrderPlacedEventHandler : IDomainEventHandler<OrderPlacedEvent>
{
    public ValueTask Handle(OrderPlacedEvent notification, CancellationToken ct) { ... }
}

// ↓ DomainEventCtxEnricherGenerator가 자동 생성하는 코드
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

- `[CtxRoot]` attribute은 `ctx.{field}` Root Level로 승격됩니다.
- `[CtxIgnore]` attribute/클래스에 적용하면 생성에서 제외됩니다.
- `partial void OnEnrichLog()`를 구현하면 computed 필드를 추가할 수 있습니다.
- 같은 이벤트에 여러 Handler가 있어도 Enricher는 1개만 생성됩니다.

#### DI 등록

```csharp
services.AddScoped<
    IDomainEventCtxEnricher<OrderPlacedEvent>,
    OrderPlacedEventCtxEnricher>();
```

### ctx.* Field 4-Level System

OpenSearch에서 "고객 CUST-001의 모든 활동을 추적하려면?" — 이 질문에 답하기 위해 Functorium은 `ctx.*` 필드를 4개 레벨로 체계화합니다:

| 우선순위 | Level | 필드 패턴 | 생성 방식 | Purpose |
|---------|-------|----------|----------|------|
| 1 | **Root Context** | `ctx.{field}` | `[CtxRoot]` 인터페이스/attribute | 교차 Usecase 검색 (예: `ctx.customer_id`) |
| 2 | **Interface Context** | `ctx.{interface}.{field}` | 비-root 인터페이스에서 유래한 attribute | 의미적 그룹핑 (예: `ctx.operator_context.operator_id`) |
| 3 | **Usecase Context** | `ctx.{usecase}.{request\|response}.{field}` | 인터페이스 없는 직접 attribute | Usecase별 상세 필드 |
| 4 | **Event Context** | `ctx.{event_name}.{field}` | `DomainEventCtxEnricherGenerator` 자동 생성 | Domain Event 핸들러 필드 |

**Interface Context 규칙:**

- Request/Response가 비-root 인터페이스를 구현하면, 해당 인터페이스에서 유래한 attribute은 `ctx.{interface}.{field}` 형식으로 출력됩니다.
- 인터페이스 이름 변환: `I` prefix 제거 → snake_case (`IOperatorContext` → `operator_context`, `IPartnerContext` → `partner_context`)
- 상속 체인에서는 **선언 인터페이스** 기준으로 결정됩니다. `IPartnerContext : IRegional`에서 `RegionCode`는 `IRegional`에 선언되었으므로 `ctx.regional.region_code`가 됩니다.

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

**OpenSearch 활용 쿼리 예시:**

```
# 고객별 모든 활동 추적 (Root Context)
ctx.customer_id: "CUST-001"

# 특정 운영자의 모든 활동 (Interface Context)
ctx.operator_context.operator_id: "admin@example.com"

# 특정 Usecase의 요청 상세 (Usecase Context)
ctx.place_order_command.request.lines_count: [3 TO *]

# 특정 이벤트의 상세 (Event Context)
ctx.order_placed_event.total_amount: [100000 TO *]

# Root + Interface 조합: 특정 고객의 특정 운영자 활동
ctx.customer_id: "CUST-001" AND ctx.operator_context.operator_id: "admin@example.com"
```

#### OpenSearchJsonFormatter 변환 규칙

`OpenSearchJsonFormatter`는 `ctx.*` 필드를 **플랫 필드**로 보존합니다. Serilog의 `CtxEnricherContext.Push`로 추가된 `ctx.` 접두사 attribute은 OpenSearch에서 그대로 필드명이 됩니다.

| Serilog LogContext attribute | OpenSearch 필드 | Notes |
|------------------------|----------------|------|
| `ctx.customer_id` | `ctx.customer_id` | Root — 교차 검색 |
| `ctx.operator_context.operator_id` | `ctx.operator_context.operator_id` | Interface — 의미적 그룹핑 |
| `ctx.place_order_command.request.lines_count` | `ctx.place_order_command.request.lines_count` | Usecase — 상세 |
| `ctx.order_placed_event.order_id` | `ctx.order_placed_event.order_id` | Event — 상세 |
| PascalCase 미인식 attribute | `ctx.snake_case` | Safety net 변환 |

---

## Understanding Error Logging

Functorium classifies errors into three types. 각 타입은 서로 다른 대응이 필요합니다.

### Expected Error

**정의:** 비즈니스 규칙에 따라 예상되는 오류입니다. 시스템이 정상적으로 동작하는 상황에서도 발생할 수 있습니다.

**Example:**
- 유효성 검사 failure (잘못된 이메일 형식)
- 리소스 없음 (존재하지 않는 주문 ID)
- 권한 없음 (접근 권한이 없는 리소스)
- 비즈니스 규칙 위반 (재고 부족)

**로그 예시:**

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
    "ErrorType": "ErrorCodeExpected",
    "Code": "Order.InsufficientStock",
    "Message": "재고가 부족합니다.",
    "CurrentValue": "ProductId: prod-001, Requested: 10, Available: 3"
  }
}
```

**대응 방법:**
- 기본적으로 별도 대응 불필요
- 특정 error code가 급증하면 비즈니스 관점에서 검토 필요
- 예: `Order.InsufficientStock`이 급증하면 재고 관리 확인

### Exceptional Error

**정의:** 시스템 문제로 인해 발생하는 예외적인 오류입니다. 즉시 조사와 대응이 필요합니다.

**Example:**
- 데이터베이스 연결 failure
- 외부 API 타임아웃
- 네트워크 오류
- 예상치 못한 예외 (NullReferenceException 등)

**로그 예시:**

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
    "ErrorType": "ErrorCodeExceptional",
    "Code": "Database.ConnectionFailed",
    "Exception": {
      "Type": "System.TimeoutException",
      "Message": "Connection timeout after 30 seconds",
      "StackTrace": "at Npgsql.NpgsqlConnection..."
    }
  }
}
```

**대응 방법:**
- 즉시 알림 발송
- 시스템 상태 확인 (DB, 네트워크, 외부 서비스)
- 필요시 서비스 재시작 또는 장애 대응

### Aggregate Error

**정의:** When multiple errors are combined입니다. 일반적으로 여러 필드의 유효성 검사가 동시에 failure할 때 발생합니다.

**Example:**
- 여러 필드의 유효성 검사 동시 failure
- 복수 서비스 호출 중 일부 failure

**로그 예시:**

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
    "ErrorType": "ManyErrors",
    "Errors": [
      {
        "ErrorType": "ErrorCodeExpected",
        "Code": "Validation.NameRequired",
        "Message": "이름은 필수입니다."
      },
      {
        "ErrorType": "ErrorCodeExpected",
        "Code": "Validation.EmailInvalid",
        "Message": "유효하지 않은 이메일입니다."
      }
    ]
  }
}
```

**Note:** `error.code`에는 첫 번째(Primary) 에러의 코드가 기록됩니다. 전체 에러 목록은 `@error.Errors`에서 확인할 수 있습니다.

### Error Type Determination Logic

Aggregate Error의 Log Level은 내부 에러 타입에 따라 결정됩니다:

1. 내부에 Exceptional 에러가 하나라도 있으면 → Error 레벨
2. 모든 내부 에러가 Expected이면 → Warning 레벨

이는 "가장 심각한 에러 기준"으로 Log Level을 결정하는 방식입니다.

지금까지 Functorium이 자동으로 생성하는 로그의 구조와 필드를 이해했습니다. 이제 이 구조화된 로그를 실제 운영 환경에서 검색하고 분석하는 방법을 알아봅니다.

---

## Log Search and Analysis

### Basic Search Patterns

**특정 핸들러의 모든 로그:**
```
request.handler.name = "CreateOrderCommandHandler"
```

**failure한 요청만 조회:**
```
response.status = "failure"
```

**특정 시간대의 느린 요청:**
```
response.elapsed > 1.0 AND @timestamp > "2024-01-15T00:00:00Z"
```

**시스템 에러만 조회:**
```
error.type = "exceptional"
```

### Grafana Loki 쿼리 예시

**핸들러별 에러율 계산:**
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

**error code별 발생 빈도:**
```logql
sum by (error_code) (
  count_over_time({error_type="expected"}[1h])
)
```

**느린 요청 추이 (P95):**
```logql
quantile_over_time(0.95,
  {request_layer="application"}
  | json
  | unwrap response_elapsed [1h]
)
```

### Elasticsearch 쿼리 예시

**핸들러별 평균 응답 시간:**
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

**시간대별 에러 발생 추이:**
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

**상황:** 운영팀에서 "주문 생성이 느리다"는 보고를 받았습니다.

**단계 1: 문제 범위 파악**
```
request.handler.name = "CreateOrderCommandHandler"
AND response.elapsed > 1.0
| stats count(), avg(response.elapsed), p95(response.elapsed)
```

**단계 2: 시간대별 추이 확인**
```
request.handler.name = "CreateOrderCommandHandler"
| timechart avg(response.elapsed) by 1h
```

**단계 3: 하위 호출 분석**
```
request.layer = "adapter"
AND request.handler.name IN ("OrderRepository", "PaymentGateway")
| stats avg(response.elapsed) by request.handler.name, request.handler.method
```

**단계 4: 근본 원인 식별**

위 분석에서 `PaymentGateway.ProcessPayment`의 응답 시간이 급격히 증가했다면, 외부 결제 서비스의 지연이 근본 원인입니다.

### Scenario 2: Analyzing Error Patterns

**상황:** Warning 로그가 평소보다 3배 증가했습니다.

**단계 1: error code별 분포 확인**
```
error.type = "expected"
| stats count() by error.code
| sort count desc
```

**단계 2: 특정 error code 상세 분석**
```
error.code = "Validation.InvalidEmail"
| stats count() by hour(@timestamp)
```

**단계 3: 관련 요청 예제 확인**
```
error.code = "Validation.InvalidEmail"
| head 10
| fields @request.message
```

**결론:** 특정 시점 이후로 잘못된 이메일 형식이 증가했다면, 프론트엔드 유효성 검사가 제대로 동작하지 않는 것일 수 있습니다.

---

## Troubleshooting

### 로그가 기록되지 않는 경우

**증상:** 특정 핸들러의 로그가 전혀 보이지 않습니다.

**확인 사항:**
1. Log Level 설정 확인 (`appsettings.json`에서 최소 Log Level이 Information 이상인지)
2. Pipeline 등록 확인 (DI 컨테이너에 `UsecaseLoggingPipeline`이 등록되어 있는지)
3. 필터 조건 확인 (검색 쿼리의 필터 조건이 너무 제한적이지 않은지)

### 필드 값이 비어 있는 경우

**증상:** `request.category.type` 값이 "unknown"으로 기록됩니다.

**Cause:** Request 클래스가 `ICommandRequest<T>` 또는 `IQueryRequest<T>` 인터페이스를 구현하지 않았습니다.

**Solution:** Request 클래스에 적절한 CQRS 인터페이스를 구현합니다.

### 응답 시간이 비정상적으로 큰 경우

**증상:** `response.elapsed` 값이 예상보다 훨씬 큽니다.

**확인 사항:**
1. 하위 Adapter 호출 시간 확인
2. 동기/비동기 호출 패턴 확인
3. 데이터베이스 쿼리 실행 계획 확인

---

## FAQ

### Q: 민감한 정보가 로그에 포함되지 않도록 하려면?

A: 두 가지 방법이 있습니다:

1. **attribute 레벨 제외:** `[JsonIgnore]` attribute을 사용하여 특정 필드를 직렬화에서 제외합니다.

```csharp
public record CreateUserCommand(
    string Email,
    [property: JsonIgnore] string Password  // 로그에 포함되지 않음
) : ICommandRequest<UserId>;
```

2. **Log Level 조정:** 운영 환경에서는 Debug 레벨 로그를 비활성화하여 파라미터 값이 기록되지 않도록 합니다.

### Q: 커스텀 필드를 추가하려면?

A: Functorium은 세 가지 방법으로 커스텀 로그 필드를 지원합니다:

1. **Source Generator 자동 생성 (권장):** `ICommandRequest<T>` 또는 `IQueryRequest<T>`를 구현하면 `CtxEnricherGenerator`가 Request/Response의 스칼라 attribute을 `ctx.{usecase}.{request|response}.{field}` 형태로 자동 생성합니다. 별도 코드 작성 불필요.

2. **partial 확장 포인트:** 자동 생성된 Enricher의 `OnEnrichRequest()` / `OnEnrichResponse()`를 partial 구현하여 computed 필드(계산된 값)를 추가합니다.

3. **Domain Event Enricher:** `IDomainEventHandler<TEvent>`를 구현하면 `DomainEventCtxEnricherGenerator`가 이벤트 핸들러에 `ctx.{event_name}.{field}` 형태의 필드를 자동 생성합니다.

`[CtxRoot]` 어트리뷰트를 사용하면 교차 Usecase 검색이 가능한 Root Context 필드(`ctx.{field}`)를 생성할 수 있습니다.

> **상세**: [Ctx Enricher를 통한 커스텀 로깅](#log-enricher를-통한-커스텀-로깅) 섹션 참조.

### Q: Adapter Layer에서 Debug 로그는 언제 활성화해야 하나요?

A: Debug 로그는 다음 상황에서 활성화합니다:

- **개발 환경:** 항상 활성화하여 상세 정보 확인
- **스테이징 환경:** 통합 테스트 시 활성화
- **운영 환경:** 문제 해결이 필요할 때만 일시적으로 활성화

주의: Debug 로그에는 파라미터 값과 결과 값이 포함되므로 민감한 데이터가 노출될 수 있습니다.

### Q: 로그 저장 비용을 줄이려면?

A: 다음 전략을 고려하세요:

1. **샘플링:** success 로그는 10%만 샘플링, failure 로그는 100% 유지
2. **TTL 설정:** 오래된 로그 자동 삭제 (예: Information 7일, Error 30일)
3. **Log Level 조정:** 운영 환경에서 Debug 로그 비활성화
4. **필드 최적화:** 불필요한 동적 필드 제외

### Q: event.id로 검색할 때 어떤 형식을 사용해야 하나요?

A: 로그 시스템에 따라 다릅니다:

- **Serilog + Seq:** `EventId.Id = 1004`
- **Grafana Loki:** `{EventId="1004"}`
- **Elasticsearch:** `eventId.id: 1004`

각 시스템의 필드 매핑 설정을 확인하세요.

---

## Reference Documents

- [OpenTelemetry Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/)
- [Serilog Structured Logging](https://serilog.net/)
- [Grafana Loki LogQL](https://grafana.com/docs/loki/latest/logql/)

**내부 문서:**
- [08-observability.md](../../spec/08-observability) — Observability 사양 (Field/Tag, Meter, 메시지 템플릿)
- [18b-observability-naming.md](./18b-observability-naming) — Observability 네이밍 가이드
- [20-observability-metrics.md](./20-observability-metrics) — Observability 메트릭 상세
- [21-observability-tracing.md](./21-observability-tracing) — Observability 트레이싱 상세
- [07-domain-events.md](../domain/07-domain-events) — 도메인 이벤트와 핸들러 Observability
