# Observability 필드/태그 이름 규칙 가이드

## 개요

이 문서는 Functorium 프로젝트의 Logging, Tracing, Metrics에서 사용하는 필드(Field)와 태그(Tag) 이름 규칙을 정의합니다.

**목적:**
- 일관된 필드 이름으로 관측성 데이터 검색 및 분석 용이
- OpenTelemetry 시맨틱 규칙과의 호환성 유지
- 팀 내 명명 규칙 표준화

**범위:**
- Logging 구조화 필드
- Metrics 태그
- Tracing Span 속성

## 기본 명명 규칙

### 표기법: `snake_case + dot`

OpenTelemetry 시맨틱 규칙을 준수하여 `snake_case + dot` 표기법을 사용합니다.

```
# 올바른 예시
request.layer
request.category.type
request.handler.method
response.status
error.code

# 잘못된 예시
requestLayer          # camelCase 사용 금지
request-layer         # kebab-case 사용 금지
REQUEST_LAYER         # UPPER_SNAKE_CASE 사용 금지
```

### 계층 구조: `{namespace}.{property}`

필드는 네임스페이스와 속성의 계층 구조로 구성합니다.

| 네임스페이스 | 설명 | 예시 |
|-------------|------|------|
| `request.*` | 요청 관련 정보 | `request.layer`, `request.handler` |
| `response.*` | 응답 관련 정보 | `response.status`, `response.elapsed` |
| `error.*` | 오류 관련 정보 | `error.type`, `error.code` |

## 필드 카테고리별 규칙

### Request 필드 (`request.*`)

| 필드 | 설명 | 예시 값 |
|------|------|--------|
| `request.layer` | 아키텍처 레이어 | `"application"`, `"adapter"` |
| `request.category` | 요청 카테고리 | `"usecase"`, `"repository"`, `"event"` |
| `request.category.type` | CQRS 타입 | `"command"`, `"query"`, `"event"` |
| `request.handler` | Handler 클래스 이름 | `"CreateOrderCommandHandler"` |
| `request.handler.method` | Handler 메서드 이름 | `"Handle"`, `"GetById"` |
| `request.params.{name}` | 동적 파라미터 값 | 파라미터 값 |
| `request.params.{name}.count` | 컬렉션 파라미터 크기 | 정수 값 |
| `request.event.count` | 이벤트 개수 | 정수 값 |

### Response 필드 (`response.*`)

| 필드 | 설명 | 예시 값 |
|------|------|--------|
| `response.status` | 응답 상태 | `"success"`, `"failure"` |
| `response.elapsed` | 처리 시간(초) | `0.0123` |
| `response.result` | 응답 결과 값 | 반환 값 |
| `response.result.count` | 컬렉션 결과 크기 | 정수 값 |
| `response.event.success_count` | 성공한 이벤트 수 | 정수 값 |
| `response.event.failure_count` | 실패한 이벤트 수 | 정수 값 |

### Error 필드 (`error.*`)

| 필드 | 설명 | 예시 값 |
|------|------|--------|
| `error.type` | 오류 분류 | `"expected"`, `"exceptional"`, `"aggregate"` |
| `error.code` | 도메인 특화 오류 코드 | `"ORDER_NOT_FOUND"` |
| `@error` | 구조화된 오류 객체 | 오류 상세 정보 |

## `count` 필드 규칙

`count` 필드는 컬렉션 크기를 나타내며, 다음 규칙을 따릅니다.

### 규칙 1: `count`가 단독으로 사용될 때 → `.count`

`count`가 다른 형용사나 명사 없이 단독으로 사용될 때는 `.count` 형식을 사용합니다.

```
# 올바른 예시
request.event.count              # 이벤트 개수
request.params.orders.count      # 컬렉션 파라미터 크기
response.result.count            # 컬렉션 결과 크기

# 잘못된 예시
request.event_count              # 단독 count는 .count 사용
```

### 규칙 2: `count`가 다른 형용사/명사와 조합될 때 → `{prefix}_count`

`count` 앞에 형용사나 명사가 붙을 때는 `_count` 형식을 사용합니다.

```
# 올바른 예시
response.event.success_count     # 성공한 이벤트 수
response.event.failure_count     # 실패한 이벤트 수

# 잘못된 예시
response.event.success.count     # 조합 count는 _count 사용
```

### 현재 필드 적용 예시

| 필드명 | 규칙 | 설명 |
|--------|------|------|
| `request.event.count` | 단독 `.count` ✅ | DomainEvent 배치 발행 시 이벤트 개수 |
| `request.params.{name}.count` | 단독 `.count` ✅ | 컬렉션 파라미터 크기 |
| `response.result.count` | 단독 `.count` ✅ | 컬렉션 결과 크기 |
| `response.event.success_count` | 조합 `_count` ✅ | 부분 실패 시 성공한 이벤트 수 |
| `response.event.failure_count` | 조합 `_count` ✅ | 부분 실패 시 실패한 이벤트 수 |

## 레이어별 필드 목록

### Application 레이어 필드

| Field/Tag | Logging | Metrics | Tracing | 설명 |
|-----------|---------|---------|---------|------|
| `request.layer` | ✅ | ✅ | ✅ | `"application"` |
| `request.category` | ✅ | ✅ | ✅ | `"usecase"` |
| `request.category.type` | ✅ | ✅ | ✅ | `"command"`, `"query"`, `"event"` |
| `request.handler` | ✅ | ✅ | ✅ | Handler 클래스 이름 |
| `request.handler.method` | ✅ | ✅ | ✅ | `"Handle"` |
| `@request.message` | ✅ | - | - | Command/Query 객체 |
| `response.status` | ✅ | ✅ | ✅ | `"success"`, `"failure"` |
| `response.elapsed` | ✅ | - | ✅ | 처리 시간(초) |
| `@response.message` | ✅ | - | - | 응답 객체 |
| `error.type` | ✅ | ✅ | ✅ | `"expected"`, `"exceptional"`, `"aggregate"` |
| `error.code` | ✅ | ✅ | ✅ | 도메인 특화 오류 코드 |
| `@error` | ✅ | - | - | 구조화된 오류 객체 |

### Adapter 레이어 필드

| Field/Tag | Logging | Metrics | Tracing | 설명 |
|-----------|---------|---------|---------|------|
| `request.layer` | ✅ | ✅ | ✅ | `"adapter"` |
| `request.category` | ✅ | ✅ | ✅ | Adapter 카테고리 (예: `"repository"`) |
| `request.handler` | ✅ | ✅ | ✅ | Handler 클래스 이름 |
| `request.handler.method` | ✅ | ✅ | ✅ | 메서드 이름 |
| `request.params.{name}` | ✅ | - | - | 개별 메서드 파라미터 |
| `request.params.{name}.count` | ✅ | - | - | 컬렉션 파라미터 크기 |
| `response.status` | ✅ | ✅ | ✅ | `"success"`, `"failure"` |
| `response.elapsed` | ✅ | - | ✅ | 처리 시간(초) |
| `response.result` | ✅ | - | - | 메서드 반환 값 |
| `response.result.count` | ✅ | - | - | 컬렉션 결과 크기 |
| `error.type` | ✅ | ✅ | ✅ | `"expected"`, `"exceptional"`, `"aggregate"` |
| `error.code` | ✅ | ✅ | ✅ | 도메인 특화 오류 코드 |
| `@error` | ✅ | - | - | 구조화된 오류 객체 |

### DomainEvent 필드

| Field/Tag | Logging | Metrics | Tracing | 설명 |
|-----------|---------|---------|---------|------|
| `request.layer` | ✅ | ✅ | ✅ | `"adapter"` (Publisher), `"application"` (Handler) |
| `request.category` | ✅ | ✅ | ✅ | `"event"` (Publisher), `"usecase"` (Handler) |
| `request.category.type` | ✅ | ✅ | ✅ | `"event"` (Handler만) |
| `request.handler` | ✅ | ✅ | ✅ | Event/Aggregate 타입명 또는 Handler 클래스명 |
| `request.handler.method` | ✅ | ✅ | ✅ | `"Publish"`, `"PublishEvents"`, `"Handle"` |
| `request.event.type` | ✅ | - | ✅ | 이벤트 타입명 (Handler만) |
| `request.event.id` | ✅ | - | ✅ | 이벤트 고유 ID (Handler만) |
| `request.event.count` | ✅ | - | ✅ | 배치 발행 시 이벤트 개수 |
| `@request.message` | ✅ | - | - | 이벤트 객체 |
| `response.status` | ✅ | ✅ | ✅ | `"success"`, `"failure"` |
| `response.elapsed` | ✅ | - | ✅ | 처리 시간(초) |
| `response.event.success_count` | ✅ | - | ✅ | 부분 실패 시 성공한 이벤트 수 |
| `response.event.failure_count` | ✅ | - | ✅ | 부분 실패 시 실패한 이벤트 수 |
| `error.type` | ✅ | ✅ | ✅ | `"expected"`, `"exceptional"` |
| `error.code` | ✅ | ✅ | ✅ | 도메인 특화 오류 코드 |
| `@error` | ✅ | - | - | 구조화된 오류 객체 |

## 예시 및 안티패턴

### 올바른 예시

```
# 정적 필드
request.layer = "adapter"
request.category = "repository"
request.handler = "OrderRepository"
request.handler.method = "GetById"
response.status = "success"
response.elapsed = 0.0234

# 동적 필드 (Adapter 레이어)
request.params.id = "12345"
request.params.items = [...]
request.params.items.count = 5
response.result = {...}
response.result.count = 10

# DomainEvent 필드
request.event.count = 3
response.event.success_count = 2
response.event.failure_count = 1
```

### 안티패턴

```
# 잘못된 표기법
requestLayer                     # camelCase 사용
request-layer                    # kebab-case 사용

# 잘못된 count 사용
response.event.success.count     # 조합 count에 .count 사용
response.event.failure.count     # 조합 count에 .count 사용

# 올바른 count 사용
response.event.success_count     # 조합 count는 _count
response.event.failure_count     # 조합 count는 _count
```

## 관련 코드 위치

| 구성 요소 | 파일 경로 |
|----------|----------|
| 필드 이름 생성 헬퍼 | `Src/Functorium.SourceGenerators/Generators/AdapterPipelineGenerator/CollectionTypeHelper.cs` |
| Application Logging | `Src/Functorium/Adapters/Observabilities/Pipelines/UsecaseLoggingPipeline.cs` |
| Adapter Logging | Source Generator 생성 코드 |
| Application Metrics | `Src/Functorium/Adapters/Observabilities/Pipelines/UsecaseMetricsPipeline.cs` |
| Application Tracing | `Src/Functorium/Adapters/Observabilities/Pipelines/UsecaseTracingPipeline.cs` |
| DomainEvent Publisher | `Src/Functorium/Adapters/Observabilities/Events/ObservableDomainEventPublisher.cs` |

## 관련 테스트

| 테스트 | 파일 경로 |
|--------|----------|
| Application Logging 구조 | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseLoggingPipelineStructureTests.cs` |
| Adapter Logging 구조 | `Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/AdapterLoggingPipelineStructureTests.cs` |
| Application Metrics 구조 | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseMetricsPipelineStructureTests.cs` |
| Adapter Metrics 구조 | `Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/AdapterMetricsPipelineStructureTests.cs` |
| Application Tracing 구조 | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Pipelines/UsecaseTracingPipelineStructureTests.cs` |
| Adapter Tracing 구조 | `Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/AdapterTracingPipelineStructureTests.cs` |
| DomainEvent Publisher Logging | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventPublisherLoggingStructureTests.cs` |
| DomainEvent Handler Logging | `Tests/Functorium.Tests.Unit/AdaptersTests/Observabilities/Events/DomainEventHandlerLoggingStructureTests.cs` |
