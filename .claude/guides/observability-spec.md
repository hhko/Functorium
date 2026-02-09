# Observability Specification

> 이 문서는 Functorium 프레임워크의 관측성(Observability) 사양입니다.
> 각 Pillar별 상세 매뉴얼은 별도 문서를 참조하세요.

> 모든 관측성 필드는 OpenTelemetry 시맨틱 규칙과의 일관성을 위해 `snake_case + dot` 표기법을 사용합니다.

![](../../Functorium.Observability.png)

## 공통 사양

### Service Attributes

Functorium은 서비스 식별을 위해 [OpenTelemetry Service Attributes](https://opentelemetry.io/docs/specs/semconv/registry/attributes/service/)를 사용합니다.

| Attribute | 설명 | 예시 |
|-----------|------|------|
| `service.namespace` | `service.name`의 네임스페이스. 서비스 그룹을 구분하는 데 도움이 됩니다(예: 팀별 또는 환경별). | `mycompany.production` |
| `service.name` | 서비스의 논리적 이름. 수평 확장된 모든 인스턴스에서 동일해야 합니다. | `orderservice` |
| `service.version` | 서비스 API 또는 구현의 버전 문자열. | `2.0.0` |
| `service.instance.id` | 서비스 인스턴스의 고유 ID. `service.namespace,service.name` 쌍당 전역적으로 고유해야 합니다. 가능한 경우 `HOSTNAME` 환경 변수를 사용하고, 그렇지 않으면 `Environment.MachineName`으로 대체됩니다. | `my-pod-abc123` (Kubernetes), `DESKTOP-ABC123` (Windows) |

> **권장**: `service.name`과 `service.namespace`에는 소문자 값을 사용하세요(예: `mycompany.production`, `orderservice`).
> 이렇게 하면 OpenTelemetry 규칙과의 일관성을 보장하고 다운스트림 시스템(대시보드, 쿼리, 알림)에서 대소문자 구분 문제를 방지할 수 있습니다.

### Error 분류

#### Error Type Tag 값

| Error Case | error.type | error.code | 설명 |
|------------|------------|------------|------|
| `IHasErrorCode` + `IsExpected` | `"expected"` | 오류 코드 | 오류 코드가 있는 예상 비즈니스 로직 오류 |
| `IHasErrorCode` + `IsExceptional` | `"exceptional"` | 오류 코드 | 오류 코드가 있는 예외 시스템 오류 |
| `ManyErrors` | `"aggregate"` | Primary 오류 코드 | 여러 오류가 집계됨(Exceptional이 우선) |
| `Expected` (LanguageExt) | `"expected"` | 타입 이름 | 오류 코드가 없는 LanguageExt 기본 예상 오류 |
| `Exceptional` (LanguageExt) | `"exceptional"` | 타입 이름 | 오류 코드가 없는 LanguageExt 기본 예외 오류 |

#### Error Field 값 (Logging 전용)

> `error.type`과 `@error.ErrorType`은 서로 다른 목적을 위해 다른 값 형식을 사용합니다.

| Error Type | `error.type` (필터링용) | `@error.ErrorType` (상세용) |
|------------|------------------------|----------------------------|
| Expected Error | `"expected"` | `"ErrorCodeExpected"` |
| Exceptional Error | `"exceptional"` | `"ErrorCodeExceptional"` |
| Aggregate Error | `"aggregate"` | `"ManyErrors"` |
| LanguageExt Expected | `"expected"` | `"Expected"` |
| LanguageExt Exceptional | `"exceptional"` | `"Exceptional"` |

- **`error.type`**: 로그 필터링/쿼리를 위한 표준화된 값(Metrics/Tracing과 일관됨)
- **`@error.ErrorType`**: 상세한 오류 타입 식별을 위한 실제 클래스 이름

## Field/Tag 일관성

### Usecase (Application/Adapter)

**Application 레이어:** (단위 테스트: [Logging](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseLoggingPipelineStructureTests.cs), [Metrics](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseMetricsPipelineStructureTests.cs), [Tracing](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseTracingPipelineStructureTests.cs))

| Field/Tag | Logging | Metrics | Tracing | 설명 |
|-----------|---------|---------|---------|------|
| `request.layer` | ✅ | ✅ | ✅ | 아키텍처 레이어 (`"application"`) |
| `request.category` | ✅ | ✅ | ✅ | 요청 카테고리 (`"usecase"`) |
| `request.category.type` | ✅ | ✅ | ✅ | CQRS 타입 (`"command"`, `"query"`) |
| `request.handler` | ✅ | ✅ | ✅ | Handler 클래스 이름 |
| `request.handler.method` | ✅ | ✅ | ✅ | Handler 메서드 이름 (`"Handle"`) |
| `response.status` | ✅ | ✅ | ✅ | 응답 상태 (`"success"`, `"failure"`) |
| `response.elapsed` | ✅ | -* | ✅ | 처리 시간(초) |
| `error.type` | ✅ | ✅ | ✅ | 오류 분류 (`"expected"`, `"exceptional"`, `"aggregate"`) |
| `error.code` | ✅ | ✅ | ✅ | 도메인 특화 오류 코드 |
| `@error` | ✅ | - | - | 구조화된 오류 객체(상세) |

**Adapter 레이어:** (단위 테스트: [Logging](../../Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/AdapterLoggingPipelineStructureTests.cs), [Metrics](../../Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/AdapterMetricsPipelineStructureTests.cs), [Tracing](../../Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/AdapterTracingPipelineStructureTests.cs))

| Field/Tag | Logging | Metrics | Tracing | 설명 |
|-----------|---------|---------|---------|------|
| `request.layer` | ✅ | ✅ | ✅ | 아키텍처 레이어 (`"adapter"`) |
| `request.category` | ✅ | ✅ | ✅ | Adapter 카테고리 (예: `"repository"`) |
| `request.handler` | ✅ | ✅ | ✅ | Handler 클래스 이름 |
| `request.handler.method` | ✅ | ✅ | ✅ | Handler 메서드 이름 |
| `response.status` | ✅ | ✅ | ✅ | 응답 상태 (`"success"`, `"failure"`) |
| `response.elapsed` | ✅ | -* | ✅ | 처리 시간(초) |
| `error.type` | ✅ | ✅ | ✅ | 오류 분류 (`"expected"`, `"exceptional"`, `"aggregate"`) |
| `error.code` | ✅ | ✅ | ✅ | 도메인 특화 오류 코드 |
| `@error` | ✅ | - | - | 구조화된 오류 객체(상세) |

> **\* `response.elapsed`가 Metrics 태그가 아닌 이유:**
> - Metrics는 처리 시간을 캡처하기 위해 전용 `duration` **Histogram instrument**를 사용하며, 이는 지연 시간 측정에 대한 OpenTelemetry 권장 접근 방식입니다.
> - 경과 시간을 태그로 사용하면 **높은 카디널리티 폭발**을 유발합니다(각 고유한 duration 값이 새로운 시계열을 생성하여 메트릭 저장소 및 쿼리 성능이 저하됨).
> - Histogram은 개별 경과 값보다 모니터링에 더 유용한 **통계적 집계**(백분위수, 평균, 카운트)를 제공합니다.

### DomainEvent Publisher

(단위 테스트: [Logging](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventPublisherLoggingStructureTests.cs), [Metrics](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventPublisherMetricsStructureTests.cs), [Tracing](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventPublisherTracingStructureTests.cs))

> DomainEvent Publisher는 Adapter 레이어로 분류되며, `request.layer`는 `"adapter"`, `request.category`는 `"event"`입니다.

| Field/Tag | Logging | Metrics | Tracing | 설명 |
|-----------|---------|---------|---------|------|
| `request.layer` | ✅ | ✅ | ✅ | 아키텍처 레이어 (`"adapter"`) |
| `request.category` | ✅ | ✅ | ✅ | 요청 카테고리 (`"event"`) |
| `request.handler` | ✅ | ✅ | ✅ | Event 타입명 또는 Aggregate 타입명 |
| `request.handler.method` | ✅ | ✅ | ✅ | 메서드 이름 (`"Publish"`, `"PublishEvents"`, `"PublishEventsWithResult"`) |
| `request.event.count` | ✅ | - | ✅ | 배치 발행 시 이벤트 개수 (Aggregate 전용) |
| `response.status` | ✅ | ✅ | ✅ | 응답 상태 (`"success"`, `"failure"`) |
| `response.elapsed` | ✅ | -* | ✅ | 처리 시간(초) |
| `response.event.success_count` | ✅ | - | ✅ | 부분 실패 시 성공한 이벤트 수 (Partial Failure 전용) |
| `response.event.failure_count` | ✅ | - | ✅ | 부분 실패 시 실패한 이벤트 수 (Partial Failure 전용) |
| `error.type` | ✅ | ✅ | ✅ | 오류 분류 (`"expected"`, `"exceptional"`) |
| `error.code` | ✅ | ✅ | ✅ | 도메인 특화 오류 코드 |
| `@error` | ✅ | - | - | 구조화된 오류 객체(상세) |

### DomainEvent Handler

(단위 테스트: [Logging](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerLoggingStructureTests.cs), [Metrics](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerMetricsStructureTests.cs), [Tracing](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerTracingStructureTests.cs))

> DomainEventHandler는 Application 레이어로 분류되며, `request.layer`는 `"application"`, `request.category`는 `"usecase"`, `request.category.type`은 `"event"`입니다.

| Field/Tag | Logging | Metrics | Tracing | 설명 |
|-----------|---------|---------|---------|------|
| `request.layer` | ✅ | ✅ | ✅ | 아키텍처 레이어 (`"application"`) |
| `request.category` | ✅ | ✅ | ✅ | 요청 카테고리 (`"usecase"`) |
| `request.category.type` | ✅ | ✅ | ✅ | CQRS 타입 (`"event"`) |
| `request.handler` | ✅ | ✅ | ✅ | Handler 클래스 이름 |
| `request.handler.method` | ✅ | ✅ | ✅ | 메서드 이름 (`"Handle"`) |
| `event.type` | - | - | ✅ | 이벤트 타입명 |
| `event.id` | - | - | ✅ | 이벤트 고유 ID |
| `@request.message` | ✅ | - | - | 이벤트 객체 (요청 시) |
| `response.status` | ✅ | ✅ | ✅ | 응답 상태 (`"success"`, `"failure"`) |
| `response.elapsed` | ✅ | -* | ✅ | 처리 시간(초) |
| `error.type` | ✅ | ✅ | ✅ | 오류 분류 (`"expected"`, `"exceptional"`) |
| `error.code` | ✅ | ✅ | ✅ | 도메인 특화 오류 코드 |

> **Note:** DomainEventHandler의 ErrorResponse는 Exception 객체가 직접 로깅됩니다 (`@error` 대신).

## Logging

### Usecase Logging

#### Field 구조

| Field Name | Application 레이어 | Adapter 레이어 | 설명 |
|------------|-------------------|---------------|------|
| **Static Fields** | | | |
| `request.layer` | `"application"` | `"adapter"` | 요청 레이어 식별자 |
| `request.category` | `"usecase"` | Adapter 카테고리 이름 | 요청 카테고리 식별자 |
| `request.category.type` | `"command"` / `"query"` | - | CQRS 타입 |
| `request.handler` | Handler 이름 | Handler 이름 | Handler 클래스 이름 |
| `request.handler.method` | `"Handle"` | 메서드 이름 | Handler 메서드 이름 |
| `response.status` | `"success"` / `"failure"` | `"success"` / `"failure"` | 응답 상태 |
| `response.elapsed` | 처리 시간(초) | 처리 시간(초) | 경과 시간(초) |
| `error.type` | `"expected"` / `"exceptional"` / `"aggregate"` | `"expected"` / `"exceptional"` / `"aggregate"` | 오류 분류 |
| `error.code` | 오류 코드 | 오류 코드 | 도메인 특화 오류 코드 |
| `@error` | 오류 객체(구조화) | 오류 객체(구조화) | 오류 데이터(상세) |
| **Dynamic Fields** | | | |
| `@request.message` | 전체 Command/Query 객체 | - | 요청 메시지 |
| `@response.message` | 전체 응답 객체 | - | 응답 메시지 |
| `request.params.{name}` | - | 개별 메서드 파라미터 | 요청 파라미터 |
| `request.params.{name}.count` | - | 컬렉션 크기(파라미터가 컬렉션인 경우) | 요청 파라미터 카운트 |
| `response.result` | - | 메서드 반환 값 | 응답 결과 |
| `response.result.count` | - | 컬렉션 크기(반환이 컬렉션인 경우) | 응답 결과 카운트 |

#### 이벤트별 로그 레벨

| Event | Log 수준 | Application 레이어 | Adapter 레이어 | 설명 |
|-------|-----------|-------------------|---------------|------|
| Request | Information | 1001 `application.request` | 2001 `adapter.request` | 요청 수신 |
| Request (Debug) | Debug | - | 2001 `adapter.request` | 파라미터 값이 포함된 요청 |
| Response Success | Information | 1002 `application.response.success` | 2002 `adapter.response.success` | 성공 응답 |
| Response Success (Debug) | Debug | - | 2002 `adapter.response.success` | 결과 값이 포함된 응답 |
| Response Warning | Warning | 1003 `application.response.warning` | 2003 `adapter.response.warning` | 예상 오류(비즈니스 로직) |
| Response Error | Error | 1004 `application.response.error` | 2004 `adapter.response.error` | 예외 오류(시스템 장애) |

#### Message Templates (Application)

```
# Request
{request.layer} {request.category}.{request.category.type} {request.handler}.{request.handler.method} requesting with {@request.message}

# Response - Success
{request.layer} {request.category}.{request.category.type} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {@response.message}

# Response - Warning/Error
{request.layer} {request.category}.{request.category.type} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}
```

#### Message Templates (Adapter)

```
# Request (Information)
{request.layer} {request.category} {request.handler}.{request.handler.method} requesting

# Request (Debug) - 파라미터 포함
{request.layer} {request.category} {request.handler}.{request.handler.method} requesting with {request.params.items} {request.params.items.count}

# Response (Information)
{request.layer} {request.category} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s

# Response (Debug) - 결과 포함
{request.layer} {request.category} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {response.result}

# Response Warning/Error
{request.layer} {request.category} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}
```

#### 구현

| 레이어 | 방식 | 테스트 | 참고 |
|-------|------|--------|------|
| Application | 직접 `ILogger.LogXxx()` 호출 | [UsecaseLoggingPipelineStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseLoggingPipelineStructureTests.cs) | 7개 이상의 파라미터가 `LoggerMessage.Define`의 6개 제한을 초과 |
| Adapter | `LoggerMessage.Define` 델리게이트 | [AdapterLoggingPipelineStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/AdapterLoggingPipelineStructureTests.cs) | 제로 할당, 고성능 |

### DomainEvent Logging

#### Field 비교

**Application Usecase vs DomainEvent Publisher vs DomainEventHandler 필드 비교:**

| Field | Application Usecase | DomainEvent Publisher | DomainEventHandler |
|-------|---------------------|----------------------|-------------------|
| `request.layer` | `"application"` | `"adapter"` | `"application"` |
| `request.category` | `"usecase"` | `"event"` | `"usecase"` |
| `request.category.type` | `"command"` / `"query"` | - | `"event"` |
| `request.handler` | Handler 클래스명 | Event/Aggregate 타입명 | Handler 클래스명 |
| `request.handler.method` | `"Handle"` | `"Publish"` / `"PublishEvents"` | `"Handle"` |
| `@request.message` | Command/Query 객체 | 이벤트 객체 | 이벤트 객체 |
| `@response.message` | 응답 객체 | - | - |
| `request.event.count` | - | O (Aggregate만) | - |
| `response.event.success_count` | - | O (Partial Failure만) | - |
| `response.event.failure_count` | - | O (Partial Failure만) | - |
| `response.status` | `"success"` / `"failure"` | `"success"` / `"failure"` | `"success"` / `"failure"` |
| `response.elapsed` | 처리 시간(초) | 처리 시간(초) | 처리 시간(초) |
| `error.type` | `"expected"` / `"exceptional"` / `"aggregate"` | `"expected"` / `"exceptional"` | `"expected"` / `"exceptional"` |
| `error.code` | 오류 코드 | 오류 코드 | 오류 코드 |
| `@error` | 오류 객체 | 오류 객체 | 오류 객체 (Exception) |

> Error 분류 상세는 [Error 분류](#error-분류) 섹션 참조.

#### Message Templates (Publisher)

```
# Request - 단일 이벤트
{request.layer} {request.category} {request.handler}.{request.handler.method} requesting with {@request.message}

# Request - Aggregate 다중 이벤트
{request.layer} {request.category} {request.handler}.{request.handler.method} requesting with {request.event.count} events

# Response - Success
{request.layer} {request.category} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s

# Response - Success (Aggregate)
{request.layer} {request.category} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {request.event.count} events

# Response - Warning/Error
{request.layer} {request.category} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}

# Response - Warning/Error (Aggregate)
{request.layer} {request.category} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {request.event.count} events with {error.type}:{error.code} {@error}

# Response - Partial Failure (Aggregate)
{request.layer} {request.category} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {request.event.count} events partial failure: {response.event.success_count} succeeded, {response.event.failure_count} failed
```

#### Publisher Event IDs

> DomainEvent Publisher는 Adapter 레이어로 분류되므로, Adapter 레이어와 동일한 Event ID를 사용합니다.

| Event | ID | Name |
|-------|-----|------|
| Request | 2001 | `adapter.request` |
| Success | 2002 | `adapter.response.success` |
| Warning | 2003 | `adapter.response.warning` |
| Error | 2004 | `adapter.response.error` |

#### Message Templates (Handler)

> DomainEventHandler는 Publisher가 발행한 이벤트를 처리하는 Handler 관점의 로깅입니다. `request.layer`는 `"application"`, `request.category`는 `"usecase"`, `request.category.type`은 `"event"`입니다.

```
# Request
{request.layer} {request.category}.{request.category.type} {request.handler}.{request.handler.method} requesting with {@request.message}

# Response - Success
{request.layer} {request.category}.{request.category.type} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s

# Response - Warning/Error
{request.layer} {request.category}.{request.category.type} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}
```

#### Handler Event IDs

> DomainEventHandler는 Application 레이어의 usecase로 분류되므로, Application 레이어와 동일한 Event ID를 사용합니다.

| Event | ID | Name |
|-------|-----|------|
| Request | 1001 | `application.request` |
| Success | 1002 | `application.response.success` |
| Warning | 1003 | `application.response.warning` |
| Error | 1004 | `application.response.error` |

#### 구현

| 레이어 | 방식 | 테스트 | 참고 |
|-------|------|--------|------|
| DomainEvent Publisher | Decorator | [DomainEventPublisherLoggingStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventPublisherLoggingStructureTests.cs) | Adapter 레이어 패턴 |
| DomainEvent Handler | `INotificationPublisher` | [DomainEventHandlerLoggingStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerLoggingStructureTests.cs) | Application 레이어 패턴 |

## Metrics

### Meter Name

| 레이어 | Meter Name 패턴 | 예시 (`ServiceNamespace = "mycompany.production"`) |
|-------|-----------------|---------------------------------------------------|
| Application | `{service.namespace}.application` | `mycompany.production.application` |
| Adapter | `{service.namespace}.adapter.{category}` | `mycompany.production.adapter.repository` |
| DomainEvent Publisher | `{service.namespace}.adapter.event` | `mycompany.production.adapter.event` |
| DomainEvent Handler | `{service.namespace}.application` | `mycompany.production.application` |

### Instrument 구조

| Instrument | Application 레이어 | Adapter 레이어 | DomainEvent Publisher | DomainEvent Handler | Type | Unit |
|------------|-------------------|---------------|----------------------|--------------------|------|------|
| requests | `application.usecase.{type}.requests` | `adapter.{category}.requests` | `adapter.event.requests` | `application.usecase.event.requests` | Counter | `{request}` |
| responses | `application.usecase.{type}.responses` | `adapter.{category}.responses` | `adapter.event.responses` | `application.usecase.event.responses` | Counter | `{response}` |
| duration | `application.usecase.{type}.duration` | `adapter.{category}.duration` | `adapter.event.duration` | `application.usecase.event.duration` | Histogram | `s` |

### Usecase Metrics

#### Tag 구조 (Application)

| Tag Key | requestCounter | durationHistogram | responseCounter (success) | responseCounter (failure) |
|---------|----------------|-------------------|---------------------------|---------------------------|
| `request.layer` | `"application"` | `"application"` | `"application"` | `"application"` |
| `request.category` | `"usecase"` | `"usecase"` | `"usecase"` | `"usecase"` |
| `request.category.type` | `"command"` / `"query"` | `"command"` / `"query"` | `"command"` / `"query"` | `"command"` / `"query"` |
| `request.handler` | handler 이름 | handler 이름 | handler 이름 | handler 이름 |
| `request.handler.method` | `"Handle"` | `"Handle"` | `"Handle"` | `"Handle"` |
| `response.status` | - | - | `"success"` | `"failure"` |
| `error.type` | - | - | - | `"expected"` / `"exceptional"` / `"aggregate"` |
| `error.code` | - | - | - | Primary 오류 코드 |
| **Total Tags** | **5** | **5** | **6** | **8** |

#### Tag 구조 (Adapter)

| Tag Key | requestCounter | durationHistogram | responseCounter (success) | responseCounter (failure) |
|---------|----------------|-------------------|---------------------------|---------------------------|
| `request.layer` | `"adapter"` | `"adapter"` | `"adapter"` | `"adapter"` |
| `request.category` | 카테고리 이름 | 카테고리 이름 | 카테고리 이름 | 카테고리 이름 |
| `request.handler` | handler 이름 | handler 이름 | handler 이름 | handler 이름 |
| `request.handler.method` | 메서드 이름 | 메서드 이름 | 메서드 이름 | 메서드 이름 |
| `response.status` | - | - | `"success"` | `"failure"` |
| `error.type` | - | - | - | `"expected"` / `"exceptional"` / `"aggregate"` |
| `error.code` | - | - | - | 오류 코드 |
| **Total Tags** | **4** | **4** | **5** | **7** |

> Error 분류 상세는 [Error 분류](#error-분류) 섹션 참조.

### DomainEvent Metrics

#### Tag 구조 (Publisher)

| Tag Key | requestCounter | durationHistogram | responseCounter (success) | responseCounter (failure) |
|---------|----------------|-------------------|---------------------------|---------------------------|
| `request.layer` | `"adapter"` | `"adapter"` | `"adapter"` | `"adapter"` |
| `request.category` | `"event"` | `"event"` | `"event"` | `"event"` |
| `request.handler` | handler 이름 | handler 이름 | handler 이름 | handler 이름 |
| `request.handler.method` | 메서드 이름 | 메서드 이름 | 메서드 이름 | 메서드 이름 |
| `response.status` | - | `"success"` / `"failure"` | `"success"` | `"failure"` |
| `error.type` | - | - | - | `"expected"` / `"exceptional"` |
| `error.code` | - | - | - | 오류 코드 |
| **Total Tags** | **4** | **5** | **5** | **7** |

> **DomainEvent Metrics에서 제외되는 태그:**
> `request.event.count`, `response.event.success_count`, `response.event.failure_count`는 Metrics 태그로 사용하지 않습니다.
> 이 값들은 각각 고유한 수치를 가지므로 태그로 사용하면 **높은 카디널리티 폭발**을 유발합니다.
> 이는 `response.elapsed`를 Metrics 태그로 사용하지 않는 것과 동일한 원칙입니다.

#### Tag 구조 (Handler)

| Tag Key | requestCounter | durationHistogram | responseCounter (success) | responseCounter (failure) |
|---------|----------------|-------------------|---------------------------|---------------------------|
| `request.layer` | `"application"` | `"application"` | `"application"` | `"application"` |
| `request.category` | `"usecase"` | `"usecase"` | `"usecase"` | `"usecase"` |
| `request.category.type` | `"event"` | `"event"` | `"event"` | `"event"` |
| `request.handler` | handler 이름 | handler 이름 | handler 이름 | handler 이름 |
| `request.handler.method` | `"Handle"` | `"Handle"` | `"Handle"` | `"Handle"` |
| `response.status` | - | - | `"success"` | `"failure"` |
| `error.type` | - | - | - | `"expected"` / `"exceptional"` |
| `error.code` | - | - | - | 오류 코드 |
| **Total Tags** | **5** | **5** | **6** | **8** |

### 구현

| 레이어 | 방식 | 테스트 | 참고 |
|-------|------|--------|------|
| Application | `IPipelineBehavior` + `IMeterFactory` | [UsecaseMetricsPipelineStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseMetricsPipelineStructureTests.cs) | Mediator pipeline |
| Adapter | Source Generator | [AdapterMetricsPipelineStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/AdapterMetricsPipelineStructureTests.cs) | 자동 생성된 metrics instruments |
| DomainEvent Publisher | Decorator + `IMeterFactory` | [DomainEventPublisherMetricsStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventPublisherMetricsStructureTests.cs) | Adapter 레이어 패턴 |
| DomainEvent Handler | `INotificationPublisher` + `IMeterFactory` | [DomainEventHandlerMetricsStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerMetricsStructureTests.cs) | Application 레이어 패턴 |

## Tracing

### Usecase Tracing

#### Span 구조

| Property | Application 레이어 | Adapter 레이어 |
|----------|-------------------|---------------|
| Span Name | `{layer} {category}.{type} {handler}.{method}` | `{layer} {category} {handler}.{method}` |
| Example | `application usecase.command CreateOrderCommandHandler.Handle` | `adapter Repository OrderRepository.GetById` |
| Kind | `Internal` | `Internal` |

#### Tag 구조

| Tag Key | Application 레이어 | Adapter 레이어 | 설명 |
|---------|-------------------|---------------|------|
| **Request Tags** | | | |
| `request.layer` | `"application"` | `"adapter"` | 레이어 식별자 |
| `request.category` | `"usecase"` | 카테고리 이름 | 카테고리 식별자 |
| `request.category.type` | `"command"` / `"query"` | - | CQRS 타입 |
| `request.handler` | Handler 이름 | Handler 이름 | Handler 클래스 이름 |
| `request.handler.method` | `"Handle"` | 메서드 이름 | 메서드 이름 |
| **Response Tags** | | | |
| `response.status` | `"success"` / `"failure"` | `"success"` / `"failure"` | 응답 상태 |
| `response.elapsed` | 처리 시간(초) | 처리 시간(초) | 경과 시간(초) |
| **Error Tags** | | | |
| `error.type` | `"expected"` / `"exceptional"` / `"aggregate"` | `"expected"` / `"exceptional"` / `"aggregate"` | 오류 분류 |
| `error.code` | 오류 코드 | 오류 코드 | 오류 코드 |
| **ActivityStatus** | `Ok` / `Error` | `Ok` / `Error` | OpenTelemetry 상태 |

> Error 분류 상세는 [Error 분류](#error-분류) 섹션 참조.

### DomainEvent Tracing

#### Publisher Span 구조

| Property | Publish | PublishEvents / PublishEventsWithResult |
|----------|---------|---------------------------------------|
| Span Name | `adapter event {EventType}.Publish` | `adapter event {AggregateType}.PublishEvents` / `adapter event {AggregateType}.PublishEventsWithResult` |
| Kind | `Internal` | `Internal` |

#### Publisher Tag 구조 (Publish)

| Tag Key | Request | Success Response | Failure Response |
|---------|---------|-----------------|-----------------|
| `request.layer` | `"adapter"` | `"adapter"` | `"adapter"` |
| `request.category` | `"event"` | `"event"` | `"event"` |
| `request.handler` | event type name | event type name | event type name |
| `request.handler.method` | `"Publish"` | `"Publish"` | `"Publish"` |
| `response.elapsed` | - | 처리 시간(초) | 처리 시간(초) |
| `response.status` | - | `"success"` | `"failure"` |
| `error.type` | - | - | `"expected"` / `"exceptional"` |
| `error.code` | - | - | 오류 코드 |
| **Total Tags** | **4** | **6** | **8** |

#### Publisher Tag 구조 (PublishEvents)

*PublishEvents / PublishEventsWithResult 메서드:*

| Tag Key | Request | Success | Partial Failure | Total Failure |
|---------|---------|---------|-----------------|---------------|
| `request.layer` | `"adapter"` | `"adapter"` | `"adapter"` | `"adapter"` |
| `request.category` | `"event"` | `"event"` | `"event"` | `"event"` |
| `request.handler` | aggregate type | aggregate type | aggregate type | aggregate type |
| `request.handler.method` | method name | method name | method name | method name |
| `request.event.count` | event count | event count | event count | event count |
| `response.elapsed` | - | 처리 시간(초) | 처리 시간(초) | 처리 시간(초) |
| `response.status` | - | `"success"` | `"failure"` | `"failure"` |
| `response.event.success_count` | - | - | success count | - |
| `response.event.failure_count` | - | - | failure count | - |
| `error.type` | - | - | - | `"expected"` / `"exceptional"` |
| `error.code` | - | - | - | 오류 코드 |
| **Total Tags** | **5** | **7** | **9** | **9** |

#### Handler Span 구조

| Property | 설명 |
|----------|------|
| Span Name | `application usecase.event {HandlerName}.Handle` |
| Kind | `Internal` |

#### Handler Tag 구조

| Tag Key | Success | Failure |
|---------|---------|---------|
| `request.layer` | `"application"` | `"application"` |
| `request.category` | `"usecase"` | `"usecase"` |
| `request.category.type` | `"event"` | `"event"` |
| `request.handler` | handler name | handler name |
| `request.handler.method` | `"Handle"` | `"Handle"` |
| `event.type` | event type name | event type name |
| `event.id` | event id | event id |
| `response.status` | `"success"` | `"failure"` |
| `error.type` | - | `"expected"` / `"exceptional"` |
| `error.code` | - | 오류 코드 |
| **Total Tags** | **8** | **10** |

> **Note:** Handler의 `response.elapsed`는 Activity 태그에 설정되지 않습니다 (Logging 전용).

### 구현

| 레이어 | 방식 | 테스트 | 참고 |
|-------|------|--------|------|
| Application | `IPipelineBehavior` + `ActivitySource.StartActivity()` | [UsecaseTracingPipelineStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseTracingPipelineStructureTests.cs) | Mediator pipeline |
| Adapter | Source Generator | [AdapterTracingPipelineStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/AdapterTracingPipelineStructureTests.cs) | 자동 생성된 Activity spans |
| DomainEvent Publisher | Decorator + `ActivitySource.StartActivity()` | [DomainEventPublisherTracingStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventPublisherTracingStructureTests.cs) | Adapter 레이어 패턴 |
| DomainEvent Handler | `INotificationPublisher` + `ActivitySource.StartActivity()` | [DomainEventHandlerTracingStructureTests](../../Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerTracingStructureTests.cs) | Application 레이어 패턴 |

## 관련 문서

| 문서 | 설명 |
|------|------|
| [Logging 매뉴얼](./observability-logging.md) | 구조화된 로깅 상세 가이드 |
| [Metrics 매뉴얼](./observability-metrics.md) | 메트릭 수집 및 분석 가이드 |
| [Tracing 매뉴얼](./observability-tracing.md) | 분산 추적 상세 가이드 |
| [코드 명명 규칙](./observability-naming-guide.md) | Observability 코드 명명 규칙 |
| [필드 이름 규칙](./observability-field-naming-guide.md) | Field/Tag 이름 규칙 |
