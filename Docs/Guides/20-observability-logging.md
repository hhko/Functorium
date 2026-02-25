# Functorium Logging 매뉴얼

Functorium 프레임워크에서 구조화된 로깅을 활용하여
애플리케이션의 동작을 추적하고 문제를 진단하는 방법을 알아봅니다.

## 목차

- [요약](#요약)
- [들어가며](#들어가며)
- [로깅의 기초](#로깅의-기초)
- [Functorium 로깅 아키텍처](#functorium-로깅-아키텍처)
- [로깅 필드 상세 가이드](#로깅-필드-상세-가이드)
- [Application Layer 로깅](#application-layer-로깅)
- [Adapter Layer 로깅](#adapter-layer-로깅)
- [DomainEvent 로깅](#domainevent-로깅)
- [에러 로깅 이해하기](#에러-로깅-이해하기)
- [로그 검색과 분석](#로그-검색과-분석)
- [실습: 로그 분석하기](#실습-로그-분석하기)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)

---

## 요약

### 주요 명령

```
# 특정 핸들러의 모든 로그 조회
request.handler = "CreateOrderCommandHandler"

# 시스템 에러만 조회
error.type = "exceptional"

# 느린 요청 식별
response.elapsed > 1.0
```

### 주요 절차

1. `ConfigurePipelines(p => p.UseAll())`로 Logging Pipeline 활성화
2. Application Layer는 `UsecaseLoggingPipeline`이 자동으로 로그 생성 (Event ID 1001-1004)
3. Adapter Layer는 Source Generator가 `LoggerMessage.Define` 기반 고성능 로그 코드 자동 생성 (Event ID 2001-2004)
4. 실패 시 `error.type`으로 Expected/Exceptional 자동 분류, 적절한 Log Level 자동 선택

### 주요 개념

| 개념 | 설명 |
|------|------|
| 구조화된 로깅 | 로그를 검색 가능한 필드(`request.*`, `response.*`, `error.*`)로 구성 |
| Event ID | Application(1001-1004), Adapter(2001-2004)로 로그 유형 분류 |
| `error.type` | `"expected"` (Warning), `"exceptional"` (Error), `"aggregate"` (복합) |
| `@error` | 구조화된 오류 상세 객체 (Serilog `@` 접두사 관례) |
| Information vs Debug | Adapter에서 Information은 기본 정보, Debug는 파라미터/결과값 포함 |

---

## 들어가며

소프트웨어가 운영 환경에서 실행될 때 "지금 무슨 일이 일어나고 있는가?"라는 질문에 답하는 것은 매우 중요합니다. 로깅은 이 질문에 답하는 가장 기본적인 방법입니다.

전통적인 로깅은 사람이 읽기 쉬운 문자열을 파일에 기록했습니다. 그러나 현대의 분산 시스템에서는 수천 개의 서비스가 초당 수만 건의 로그를 생성합니다. 이러한 환경에서 "특정 사용자의 주문 처리 로그만 찾아라"라는 요청에 문자열 검색으로 대응하기란 거의 불가능합니다.

Functorium은 OpenTelemetry 표준을 따르는 **구조화된 로깅(Structured Logging)**을 제공합니다. 구조화된 로깅이란 로그 메시지를 단순 텍스트가 아닌 **검색 가능한 필드**로 구성하는 것을 의미합니다.

### 이 문서에서 배우는 내용

이 문서를 통해 다음을 학습합니다:

1. **구조화된 로깅이 왜 중요한지** - 전통적 로깅의 한계와 구조화된 로깅의 장점
2. **Functorium이 어떻게 자동으로 로그를 생성하는지** - 아키텍처 레이어별 로깅 파이프라인
3. **각 로그 필드의 의미와 활용법** - request.*, response.*, error.* 필드 상세 설명
4. **로그를 검색하고 분석하는 방법** - Loki, Elasticsearch 쿼리 예시

### 사전 지식

이 문서를 이해하기 위해 다음 개념에 대한 기본적인 이해가 필요합니다:

- C#과 .NET 기본 문법
- 로깅의 기본 개념 (Log Level, Logger 등)
- JSON 형식에 대한 이해

### DomainEvent 로깅 요약

DomainEvent의 로깅은 Publisher(Adapter 레이어)와 Handler(Application 레이어)로 구분됩니다:

| 항목 | DomainEvent Publisher | DomainEvent Handler |
|------|----------------------|---------------------|
| `request.layer` | `"adapter"` | `"application"` |
| `request.category` | `"event"` | `"usecase"` |
| `request.category.type` | - | `"event"` |
| Event ID 범위 | 2001-2004 | 1001-1004 |

> 상세 필드 비교와 메시지 템플릿은 [DomainEvent 로깅](#domainevent-로깅) 섹션을 참조하세요.

---

## 로깅의 기초

### 전통적 로깅 vs 구조화된 로깅

**전통적 로깅**은 사람이 읽기 쉬운 문자열을 기록합니다:

```
2024-01-15 10:30:45 INFO CreateOrderCommandHandler started processing order for customer John
2024-01-15 10:30:46 INFO CreateOrderCommandHandler completed in 1.2s
2024-01-15 10:30:47 ERROR CreateOrderCommandHandler failed: Database connection timeout
```

이 방식은 직관적이고 읽기 쉽습니다. 그러나 몇 가지 심각한 문제점이 있습니다:

1. **검색의 어려움**: "CreateOrder"와 관련된 모든 로그를 찾으려면 문자열 검색에 의존해야 합니다. "CreateOrderCommandHandler", "Create Order", "create_order" 등 다양한 표현이 섞여 있으면 검색이 매우 어려워집니다.

2. **집계의 불가능**: "지난 1시간 동안 CreateOrderCommandHandler의 평균 처리 시간은?"이라는 질문에 답하려면 모든 로그를 파싱해야 합니다.

3. **상관관계 추적의 어려움**: 하나의 HTTP 요청이 여러 서비스를 거칠 때, 관련된 로그를 찾는 것이 매우 어렵습니다.

**구조화된 로깅**은 로그를 검색 가능한 필드로 저장합니다:

```json
{
  "timestamp": "2024-01-15T10:30:45Z",
  "level": "Information",
  "request.layer": "application",
  "request.category": "usecase",
  "request.category.type": "command",
  "request.handler": "CreateOrderCommandHandler",
  "request.handler.method": "Handle",
  "response.status": "success",
  "response.elapsed": 1.2
}
```

이제 다음과 같은 질문에 정확하게 답할 수 있습니다:

- `request.handler = "CreateOrderCommandHandler"`로 특정 핸들러의 모든 로그 조회
- `response.status = "failure"`로 모든 실패 로그 필터링
- `avg(response.elapsed) where request.handler = "CreateOrderCommandHandler"`로 평균 처리 시간 계산

### OpenTelemetry 로깅 표준

Functorium은 OpenTelemetry 시맨틱 컨벤션(Semantic Conventions)을 따릅니다. OpenTelemetry는 클라우드 네이티브 환경에서 관찰 가능성(Observability)을 구현하기 위한 업계 표준입니다.

이 표준을 따르면 다음과 같은 이점이 있습니다:

1. **도구 호환성**: Grafana Loki, Elasticsearch, Datadog 등 다양한 관찰 가능성 도구와 호환됩니다. 특정 벤더에 종속되지 않고 자유롭게 도구를 선택할 수 있습니다.

2. **팀 간 일관성**: 조직 내 모든 서비스가 동일한 필드 이름을 사용합니다. "핸들러 이름"이 어떤 서비스에서는 `handler_name`, 다른 서비스에서는 `handlerName`으로 기록되는 혼란을 방지합니다.

3. **학습 전이**: 한 번 배우면 다른 프로젝트에서도 활용할 수 있습니다. OpenTelemetry를 사용하는 모든 시스템에서 동일한 개념이 적용됩니다.

### 네이밍 규칙: snake_case + dot notation

Functorium의 모든 로깅 필드는 다음 규칙을 따릅니다:

- **snake_case**: 단어를 소문자로 작성하고 언더스코어가 아닌 점(dot)으로 연결합니다.
- **dot notation**: 계층 구조를 점으로 표현합니다.

**예시:**

| 잘못된 예 | 올바른 예 | 설명 |
|-----------|-----------|------|
| `ResponseStatus` | `response.status` | PascalCase 대신 소문자 사용 |
| `response_status` | `response.status` | 언더스코어 대신 점 사용 |
| `handlerMethod` | `request.handler.method` | 계층 구조를 점으로 표현 |

이 규칙을 따르는 이유:

1. **OpenTelemetry 시맨틱 컨벤션 준수**: 표준을 따름으로써 도구 호환성을 확보합니다.
2. **다운스트림 시스템과의 호환성**: 대시보드, 알림 시스템에서 필드를 일관되게 참조할 수 있습니다.
3. **대소문자 민감성 문제 방지**: 모든 필드가 소문자이므로 대소문자 차이로 인한 검색 실패를 방지합니다.

---

## Functorium 로깅 아키텍처

Functorium은 두 개의 아키텍처 레이어에서 자동으로 로그를 생성합니다. 개발자가 명시적으로 로그를 작성하지 않아도 프레임워크가 일관된 형식으로 로그를 기록합니다.

### 아키텍처 레이어 개요

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
|  |  - request.category: "usecase"                      |  |
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
|  |  - request.category: "repository", "gateway", etc.  |  |
|  |  - Auto-generated by Source Generator               |  |
|  +-----------------------------------------------------+  |
+-----------------------------------------------------------+
```

**Application Layer**는 비즈니스 로직을 담당합니다. CQRS(Command Query Responsibility Segregation) 패턴에 따라 Command(상태 변경)와 Query(데이터 조회)로 구분됩니다.

**Adapter Layer**는 외부 시스템과의 연동을 담당합니다. Repository(데이터베이스), Gateway(외부 API), Cache(캐시 시스템) 등이 포함됩니다.

### 로그 생성 시점

각 레이어에서 로그는 다음 네 가지 시점에 생성됩니다:

1. **요청 시작 (Request)**: 핸들러가 요청을 받았을 때 기록됩니다. 어떤 요청이 들어왔는지 추적하는 데 사용됩니다.

2. **성공 응답 (Success Response)**: 처리가 정상 완료되었을 때 기록됩니다. 처리 시간과 결과가 포함됩니다.

3. **경고 응답 (Warning Response)**: 예상된 비즈니스 오류가 발생했을 때 기록됩니다. 예를 들어, 유효성 검사 실패, 권한 없음, 리소스 없음 등이 해당됩니다. 이러한 오류는 시스템 문제가 아니라 정상적인 비즈니스 흐름의 일부입니다.

4. **에러 응답 (Error Response)**: 예외적 시스템 오류가 발생했을 때 기록됩니다. 데이터베이스 연결 실패, 네트워크 타임아웃, 예상치 못한 예외 등이 해당됩니다. 이러한 오류는 즉시 조사가 필요합니다.

### Event ID 체계

Functorium은 로그를 Event ID로 분류합니다. Event ID를 활용하면 특정 유형의 로그만 빠르게 필터링할 수 있습니다.

**Application Layer (1000번대):**

| Event ID | 이름 | Log Level | 설명 |
|----------|------|-----------|------|
| 1001 | `application.request` | Information | 요청 수신 |
| 1002 | `application.response.success` | Information | 성공 응답 |
| 1003 | `application.response.warning` | Warning | 예상된 오류 |
| 1004 | `application.response.error` | Error | 예외적 오류 |

**Adapter Layer (2000번대):**

| Event ID | 이름 | Log Level | 설명 |
|----------|------|-----------|------|
| 2001 | `adapter.request` | Information / Debug | 요청 수신 |
| 2002 | `adapter.response.success` | Information / Debug | 성공 응답 |
| 2003 | `adapter.response.warning` | Warning | 예상된 오류 |
| 2004 | `adapter.response.error` | Error | 예외적 오류 |

> **번호 갭 안내:** 1001-1004와 2001-2004 사이의 번호 갭(1005-1999, 2005-2999)은 향후 확장을 위해 의도적으로 예약된 범위입니다.

**활용 예시:**

- 모든 에러 로그 조회: `EventId IN (1004, 2004)`
- Application Layer 요청만 조회: `EventId = 1001`
- 경고 이상의 로그 조회: `EventId IN (1003, 1004, 2003, 2004)`

### Log Level과 에러 타입의 관계

Functorium은 에러 타입에 따라 자동으로 적절한 Log Level을 선택합니다:

| 에러 타입 | Log Level | 알림 필요 | 설명 |
|-----------|-----------|-----------|------|
| Expected (예상된 오류) | Warning | 선택적 | 비즈니스 규칙에 따른 정상적인 거부 |
| Exceptional (예외적 오류) | Error | 즉시 | 시스템 문제로 인한 처리 실패 |
| Aggregate (복합 오류) | 내부 타입에 따름 | 내부 타입에 따름 | 여러 오류가 결합된 경우 |

이 구분이 중요한 이유는 운영 모니터링에서 **진짜 문제**와 **정상적인 비즈니스 흐름**을 구분해야 하기 때문입니다. "사용자가 잘못된 이메일을 입력했다"는 경고지만, "데이터베이스가 응답하지 않는다"는 즉시 대응이 필요한 에러입니다.

---

## 로깅 필드 상세 가이드

이 섹션에서는 Functorium이 생성하는 각 로깅 필드의 의미와 활용법을 상세히 설명합니다.

### 요청 식별 필드

이 필드들은 "어떤 코드가 실행되고 있는가?"라는 질문에 답합니다.

#### request.layer

```
값: "application" 또는 "adapter"
```

현재 로그가 발생한 아키텍처 레이어를 나타냅니다.

- **"application"**: 비즈니스 로직 레이어 (Usecase/Command/Query)
- **"adapter"**: 외부 시스템 연동 레이어 (Repository, Gateway 등)

**활용 예시:**

```
# 비즈니스 로직 문제 조사
request.layer = "application"

# 데이터베이스 관련 문제 조사
request.layer = "adapter" AND request.category = "repository"
```

#### request.category

```
Application Layer: "usecase"
Adapter Layer: "repository", "gateway" 등 구체적인 카테고리명
```

요청의 카테고리를 나타냅니다. Application Layer에서는 항상 "usecase"이고, Adapter Layer에서는 구체적인 어댑터 종류를 나타냅니다.

**활용 예시:**

```
# 모든 Usecase 로그
request.category = "usecase"

# Repository 관련 로그만
request.category = "repository"

# Gateway 호출 로그만
request.category = "gateway"
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
- Command는 트랜잭션과 검증이 포함되어 처리 시간이 깁니다.
- Query는 캐싱이 가능하여 처리 시간이 짧습니다.

**활용 예시:**

```
# 모든 Command 처리 로그
request.category.type = "command"

# 느린 Query 찾기
request.category.type = "query" AND response.elapsed > 1.0
```

#### request.handler

```
값: 핸들러 클래스 이름
예: "CreateOrderCommandHandler", "OrderRepository"
```

요청을 처리하는 클래스의 이름입니다. 전체 네임스페이스가 아닌 클래스 이름만 포함됩니다.

**활용 예시:**

```
# 특정 핸들러의 모든 로그 조회
request.handler = "CreateOrderCommandHandler"

# 특정 Repository의 모든 호출
request.handler = "OrderRepository"
```

#### request.handler.method

```
Application Layer: 항상 "Handle"
Adapter Layer: 실제 메서드 이름 (예: "GetById", "SaveAsync")
```

호출된 메서드의 이름입니다. Application Layer에서는 Mediator 패턴에 따라 항상 "Handle" 메서드가 호출되므로 값이 고정됩니다. Adapter Layer에서는 실제 호출된 메서드 이름이 기록됩니다.

**활용 예시:**

```
# Repository의 GetById 호출만 조회
request.handler = "OrderRepository" AND request.handler.method = "GetById"
```

### 응답 상태 필드

이 필드들은 "처리가 어떻게 완료되었는가?"라는 질문에 답합니다.

#### response.status

```
값: "success" 또는 "failure"
```

요청 처리의 최종 결과입니다.

- **"success"**: 정상 처리 완료
- **"failure"**: 오류 발생 (예상된 오류 또는 예외 모두 포함)

**에러율 계산에 활용:**

```
에러율 = count(response.status = "failure") / count(*) × 100
```

**활용 예시:**

```
# 모든 실패 로그
response.status = "failure"

# 특정 핸들러의 성공률 계산
request.handler = "CreateOrderCommandHandler"
| stats count() by response.status
```

#### response.elapsed

```
값: 초 단위 처리 시간 (소수점 4자리)
예: 0.0234 (약 23.4ms)
```

요청 시작부터 응답까지 걸린 시간입니다. 이 필드는 성공/실패 응답 로그에만 포함되며, 요청 로그에는 포함되지 않습니다.

**성능 분석에 활용:**

```
# 느린 요청 식별 (1초 이상)
response.elapsed > 1.0

# 핸들러별 평균 처리 시간
| stats avg(response.elapsed) by request.handler

# P95 응답 시간 계산
| stats percentile(response.elapsed, 95) by request.handler
```

### 에러 정보 필드

이 필드들은 "무엇이 잘못되었는가?"라는 질문에 답합니다. `response.status = "failure"`인 경우에만 포함됩니다.

#### error.type

```
값: "expected", "exceptional", 또는 "aggregate"
```

에러의 분류입니다:

| 값 | 의미 | 예시 | Log Level |
|---|---|---|---|
| "expected" | 예상된 비즈니스 오류 | 유효성 검사 실패, 권한 없음, 리소스 없음 | Warning |
| "exceptional" | 예외적 시스템 오류 | DB 연결 실패, 타임아웃, 예상치 못한 예외 | Error |
| "aggregate" | 여러 오류가 결합됨 | 복합 유효성 검사 실패 | 내부 타입에 따름 |

**활용 예시:**

```
# 시스템 오류만 조회 (즉시 대응 필요)
error.type = "exceptional"

# 비즈니스 오류 패턴 분석
error.type = "expected" | stats count() by error.code
```

#### error.code

```
값: 도메인별 에러 코드
예: "Order.NotFound", "Validation.InvalidEmail", "Database.ConnectionFailed"
```

에러의 구체적인 코드입니다. 이 코드는 계층적 구조를 가지며, 점(.)으로 구분됩니다.

**코드 구조 예시:**

- `Order.NotFound` - 주문 도메인, 리소스 없음
- `Validation.InvalidEmail` - 유효성 검사, 잘못된 이메일
- `Database.ConnectionFailed` - 데이터베이스, 연결 실패

**활용 예시:**

```
# 특정 에러 코드 발생 횟수
error.code = "Order.NotFound" | count()

# 에러 코드별 발생 빈도
| stats count() by error.code | sort count desc

# 알림 설정: 특정 에러가 임계값 초과 시
error.code = "Database.ConnectionFailed" AND count() > 10
```

#### @error

```
값: 구조화된 에러 객체 (JSON)
```

에러의 전체 상세 정보를 담은 객체입니다. 로그 시스템에서 `@` 접두사는 객체 필드를 나타내는 Serilog 관례입니다.

**예시:**

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

| 필드 | 값 예시 | 용도 |
|------|---------|------|
| `error.type` | "expected" | 필터링/쿼리용 (일관된 값) |
| `@error.ErrorType` | "ErrorCodeExpected" | 상세 분석용 (실제 클래스명) |

`error.type`은 항상 세 가지 값 중 하나이므로 쿼리와 필터링에 적합합니다. `@error.ErrorType`은 실제 에러 클래스 이름을 포함하여 더 상세한 분석에 사용됩니다.

---

## Application Layer 로깅

Application Layer는 비즈니스 로직을 처리하는 핵심 레이어입니다. `UsecaseLoggingPipeline`이 자동으로 로그를 생성합니다.

### 메시지 템플릿

Application Layer의 로그 메시지는 다음 템플릿을 따릅니다:

**요청 로그:**
```
{request.layer} {request.category}.{request.category.type} {request.handler}.{request.handler.method} requesting with {@request.message}
```

**성공 응답 로그:**
```
{request.layer} {request.category}.{request.category.type} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {@response.message}
```

**실패 응답 로그:**
```
{request.layer} {request.category}.{request.category.type} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}
```

### 동적 필드

Application Layer에서는 요청과 응답 객체 전체가 로그에 포함됩니다:

| 필드 | 설명 | 포함 시점 |
|------|------|-----------|
| `@request.message` | 전체 Command/Query 객체 | 요청 로그 |
| `@response.message` | 전체 응답 객체 | 성공 응답 로그 |

**예시 - 요청 로그:**

```json
{
  "request.layer": "application",
  "request.category": "usecase",
  "request.category.type": "command",
  "request.handler": "CreateOrderCommandHandler",
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

**예시 - 성공 응답 로그:**

```json
{
  "request.layer": "application",
  "request.category": "usecase",
  "request.category.type": "command",
  "request.handler": "CreateOrderCommandHandler",
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

### 필드 구조 비교표

| 필드 | 요청 로그 | 성공 응답 | 실패 응답 |
|------|-----------|-----------|-----------|
| `request.layer` | "application" | "application" | "application" |
| `request.category` | "usecase" | "usecase" | "usecase" |
| `request.category.type` | "command"/"query" | "command"/"query" | "command"/"query" |
| `request.handler` | 핸들러명 | 핸들러명 | 핸들러명 |
| `request.handler.method` | "Handle" | "Handle" | "Handle" |
| `@request.message` | 요청 객체 | - | - |
| `response.status` | - | "success" | "failure" |
| `response.elapsed` | - | 처리 시간 | 처리 시간 |
| `@response.message` | - | 응답 객체 | - |
| `error.type` | - | - | 에러 타입 |
| `error.code` | - | - | 에러 코드 |
| `@error` | - | - | 에러 객체 |

---

## Adapter Layer 로깅

Adapter Layer는 외부 시스템(데이터베이스, API 등)과의 연동을 담당합니다. Source Generator가 자동으로 로깅 코드를 생성하며, `LoggerMessage.Define`을 사용하여 고성능 로깅을 구현합니다.

### 메시지 템플릿

Adapter Layer의 로그 메시지는 다음 템플릿을 따릅니다:

**요청 로그 (Information):**
```
{request.layer} {request.category} {request.handler}.{request.handler.method} requesting
```

**요청 로그 (Debug - 파라미터 포함):**
```
{request.layer} {request.category} {request.handler}.{request.handler.method} {request.params.items} {request.params.items.count} requesting
```

**성공 응답 로그 (Information):**
```
{request.layer} {request.category} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s
```

**성공 응답 로그 (Debug - 결과 포함):**
```
{request.layer} {request.category} {request.handler}.{request.handler.method} {response.result} responded {response.status} in {response.elapsed:0.0000} s
```

**실패 응답 로그:**
```
{request.layer} {request.category} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}
```

### Information vs Debug 레벨

Adapter Layer에서는 두 가지 레벨의 로그가 생성됩니다:

**Information 레벨:**
- 기본적인 요청/응답 정보만 포함
- 파라미터 값이나 결과 값은 포함하지 않음
- 운영 환경에서 항상 활성화

**Debug 레벨:**
- 파라미터 값과 결과 값 포함
- 민감한 데이터가 포함될 수 있으므로 개발 환경에서만 활성화 권장
- 문제 해결 시 상세 정보 확인에 유용

### 동적 필드

Adapter Layer에서는 메서드 파라미터와 반환값이 동적으로 기록됩니다:

| 필드 | 설명 | Log Level |
|------|------|-----------|
| `request.params.{name}` | 개별 메서드 파라미터 | Debug |
| `request.params.{name}.count` | 컬렉션 파라미터의 크기 | Debug |
| `response.result` | 메서드 반환값 | Debug |
| `response.result.count` | 컬렉션 반환값의 크기 | Debug |

**예시 - 요청 로그 (Debug):**

```json
{
  "request.layer": "adapter",
  "request.category": "repository",
  "request.handler": "OrderRepository",
  "request.handler.method": "GetByCustomerId",
  "request.params.customerId": "cust-123",
  "request.params.pageSize": 10
}
```

**예시 - 성공 응답 로그 (Debug):**

```json
{
  "request.layer": "adapter",
  "request.category": "repository",
  "request.handler": "OrderRepository",
  "request.handler.method": "GetByCustomerId",
  "response.status": "success",
  "response.elapsed": 0.0456,
  "response.result.count": 5
}
```

### 필드 구조 비교표

| 필드 | 요청 로그 | 성공 응답 | 실패 응답 |
|------|-----------|-----------|-----------|
| `request.layer` | "adapter" | "adapter" | "adapter" |
| `request.category` | 카테고리명 | 카테고리명 | 카테고리명 |
| `request.handler` | 핸들러명 | 핸들러명 | 핸들러명 |
| `request.handler.method` | 메서드명 | 메서드명 | 메서드명 |
| `request.params.{name}` | 파라미터값 (Debug) | - | - |
| `response.status` | - | "success" | "failure" |
| `response.elapsed` | - | 처리 시간 | 처리 시간 |
| `response.result` | - | 결과값 (Debug) | - |
| `error.type` | - | - | 에러 타입 |
| `error.code` | - | - | 에러 코드 |
| `@error` | - | - | 에러 객체 |

---

## DomainEvent 로깅

DomainEvent는 도메인 모델에서 발생한 이벤트를 다른 컴포넌트에 알리는 메커니즘입니다. Functorium에서 DomainEvent의 관측성은 두 가지 컴포넌트로 구성됩니다:

- **DomainEvent Publisher**: 이벤트를 발행하는 Adapter 레이어 컴포넌트 (`request.layer: "adapter"`, `request.category: "event"`)
- **DomainEvent Handler**: 이벤트를 처리하는 Application 레이어 컴포넌트 (`request.layer: "application"`, `request.category: "usecase"`, `request.category.type: "event"`)

### Event ID 체계

Publisher와 Handler는 각각 소속 레이어의 Event ID를 사용합니다:

| 컴포넌트 | 레이어 | Request | Success | Warning | Error |
|----------|--------|---------|---------|---------|-------|
| Publisher | Adapter (2000번대) | 2001 | 2002 | 2003 | 2004 |
| Handler | Application (1000번대) | 1001 | 1002 | 1003 | 1004 |

### Publisher 메시지 템플릿

Publisher는 Adapter 레이어 패턴을 따르며, 단일 이벤트(Publish)와 추적 이벤트(PublishTrackedEvents)를 구분합니다:

**단일 이벤트 요청 (Publish):**
```
{request.layer} {request.category} {request.handler}.{request.handler.method} requesting with {@request.message}
```

**추적 이벤트 요청 (PublishTrackedEvents):**
```
{request.layer} {request.category} {request.handler}.{request.handler.method} requesting with {request.event.count} events
```

**성공 응답:**
```
{request.layer} {request.category} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s
```

**성공 응답 (Aggregate):**
```
{request.layer} {request.category} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {request.event.count} events
```

**실패 응답:**
```
{request.layer} {request.category} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}
```

**실패 응답 (Aggregate):**
```
{request.layer} {request.category} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {request.event.count} events with {error.type}:{error.code} {@error}
```

**부분 실패 응답 (PublishTrackedEvents):**
```
{request.layer} {request.category} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {request.event.count} events partial failure: {response.event.success_count} succeeded, {response.event.failure_count} failed
```

### Handler 메시지 템플릿

Handler는 Application 레이어 Usecase 패턴을 따르되, `request.category.type`이 `"event"`입니다:

**요청:**
```
{request.layer} {request.category}.{request.category.type} {request.handler}.{request.handler.method} {request.event.type} {request.event.id} requesting with {@request.message}
```

**성공 응답:**
```
{request.layer} {request.category}.{request.category.type} {request.handler}.{request.handler.method} {request.event.type} {request.event.id} responded {response.status} in {response.elapsed:0.0000} s
```

**실패 응답:**
```
{request.layer} {request.category}.{request.category.type} {request.handler}.{request.handler.method} {request.event.type} {request.event.id} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}
```

### 필드 비교표

Application Usecase, DomainEvent Publisher, DomainEvent Handler의 필드 비교:

| Field | Application Usecase | DomainEvent Publisher | DomainEvent Handler |
|-------|---------------------|----------------------|---------------------|
| `request.layer` | `"application"` | `"adapter"` | `"application"` |
| `request.category` | `"usecase"` | `"event"` | `"usecase"` |
| `request.category.type` | `"command"` / `"query"` | - | `"event"` |
| `request.handler` | Handler 클래스명 | Event/Aggregate 타입명 | Handler 클래스명 |
| `request.handler.method` | `"Handle"` | `"Publish"` / `"PublishTrackedEvents"` | `"Handle"` |
| `request.event.type` | - | - | 이벤트 타입명 |
| `request.event.id` | - | - | 이벤트 고유 ID |
| `@request.message` | Command/Query 객체 | 이벤트 객체 | 이벤트 객체 |
| `@response.message` | 응답 객체 | - | - |
| `request.event.count` | - | O (PublishTrackedEvents만) | - |
| `response.event.success_count` | - | O (Partial Failure만) | - |
| `response.event.failure_count` | - | O (Partial Failure만) | - |
| `response.status` | `"success"` / `"failure"` | `"success"` / `"failure"` | `"success"` / `"failure"` |
| `response.elapsed` | 처리 시간(초) | 처리 시간(초) | 처리 시간(초) |
| `error.type` | `"expected"` / `"exceptional"` / `"aggregate"` | `"expected"` / `"exceptional"` | `"expected"` / `"exceptional"` |
| `error.code` | 오류 코드 | 오류 코드 | 오류 코드 |
| `@error` | 오류 객체 | 오류 객체 | 오류 객체 (Exception) |

### LayeredArch 시나리오 로그 예시

**상품 생성 성공 (`POST /api/products`):**

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

> **Note:** Handler에서 발생한 예외의 `error.code`는 예외 타입명(`InvalidOperationException`)이고, Publisher에서는 이를 래핑한 에러 코드(`ApplicationErrors.DomainEventPublisher.PublishFailed`)가 기록됩니다.

**어댑터 예외 (`POST /api/products` with `[adapter-error]`):**

어댑터 예외는 Repository에서 발생하므로 이벤트 발행까지 도달하지 않습니다:

```
fail: adapter repository InMemoryProductRepository.Create responded failure in 0.0005 s with exceptional:Exceptional {@error}
fail: application usecase.command CreateProductCommand.Handle responded failure in 0.0031 s with exceptional:AdapterErrors.UsecaseExceptionPipeline`2.PipelineException {@error}
```

---

## 에러 로깅 이해하기

Functorium은 에러를 세 가지 타입으로 분류합니다. 각 타입은 서로 다른 대응이 필요합니다.

### Expected Error (예상된 오류)

**정의:** 비즈니스 규칙에 따라 예상되는 오류입니다. 시스템이 정상적으로 동작하는 상황에서도 발생할 수 있습니다.

**예시:**
- 유효성 검사 실패 (잘못된 이메일 형식)
- 리소스 없음 (존재하지 않는 주문 ID)
- 권한 없음 (접근 권한이 없는 리소스)
- 비즈니스 규칙 위반 (재고 부족)

**로그 예시:**

```json
{
  "level": "Warning",
  "eventId": 1003,
  "request.layer": "application",
  "request.handler": "CreateOrderCommandHandler",
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
- 특정 에러 코드가 급증하면 비즈니스 관점에서 검토 필요
- 예: `Order.InsufficientStock`이 급증하면 재고 관리 확인

### Exceptional Error (예외적 오류)

**정의:** 시스템 문제로 인해 발생하는 예외적인 오류입니다. 즉시 조사와 대응이 필요합니다.

**예시:**
- 데이터베이스 연결 실패
- 외부 API 타임아웃
- 네트워크 오류
- 예상치 못한 예외 (NullReferenceException 등)

**로그 예시:**

```json
{
  "level": "Error",
  "eventId": 1004,
  "request.layer": "application",
  "request.handler": "CreateOrderCommandHandler",
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

### Aggregate Error (복합 오류)

**정의:** 여러 오류가 결합된 경우입니다. 일반적으로 여러 필드의 유효성 검사가 동시에 실패할 때 발생합니다.

**예시:**
- 여러 필드의 유효성 검사 동시 실패
- 복수 서비스 호출 중 일부 실패

**로그 예시:**

```json
{
  "level": "Warning",
  "eventId": 1003,
  "request.layer": "application",
  "request.handler": "CreateUserCommandHandler",
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

**참고:** `error.code`에는 첫 번째(Primary) 에러의 코드가 기록됩니다. 전체 에러 목록은 `@error.Errors`에서 확인할 수 있습니다.

### 에러 타입 결정 로직

Aggregate Error의 Log Level은 내부 에러 타입에 따라 결정됩니다:

1. 내부에 Exceptional 에러가 하나라도 있으면 → Error 레벨
2. 모든 내부 에러가 Expected이면 → Warning 레벨

이는 "가장 심각한 에러 기준"으로 Log Level을 결정하는 방식입니다.

---

## 로그 검색과 분석

### 기본 검색 패턴

**특정 핸들러의 모든 로그:**
```
request.handler = "CreateOrderCommandHandler"
```

**실패한 요청만 조회:**
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
sum by (request_handler) (
  count_over_time({response_status="failure"}[1h])
)
/
sum by (request_handler) (
  count_over_time({request_layer="application"}[1h])
)
* 100
```

**에러 코드별 발생 빈도:**
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
      "terms": { "field": "request.handler.keyword" },
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

## 실습: 로그 분석하기

### 시나리오 1: 성능 저하 조사

**상황:** 운영팀에서 "주문 생성이 느리다"는 보고를 받았습니다.

**단계 1: 문제 범위 파악**
```
request.handler = "CreateOrderCommandHandler"
AND response.elapsed > 1.0
| stats count(), avg(response.elapsed), p95(response.elapsed)
```

**단계 2: 시간대별 추이 확인**
```
request.handler = "CreateOrderCommandHandler"
| timechart avg(response.elapsed) by 1h
```

**단계 3: 하위 호출 분석**
```
request.layer = "adapter"
AND request.handler IN ("OrderRepository", "PaymentGateway")
| stats avg(response.elapsed) by request.handler, request.handler.method
```

**단계 4: 근본 원인 식별**

위 분석에서 `PaymentGateway.ProcessPayment`의 응답 시간이 급격히 증가했다면, 외부 결제 서비스의 지연이 근본 원인입니다.

### 시나리오 2: 에러 패턴 분석

**상황:** Warning 로그가 평소보다 3배 증가했습니다.

**단계 1: 에러 코드별 분포 확인**
```
error.type = "expected"
| stats count() by error.code
| sort count desc
```

**단계 2: 특정 에러 코드 상세 분석**
```
error.code = "Validation.InvalidEmail"
| stats count() by hour(@timestamp)
```

**단계 3: 관련 요청 샘플 확인**
```
error.code = "Validation.InvalidEmail"
| head 10
| fields @request.message
```

**결론:** 특정 시점 이후로 잘못된 이메일 형식이 증가했다면, 프론트엔드 유효성 검사가 제대로 동작하지 않는 것일 수 있습니다.

---

## 트러블슈팅

### 로그가 기록되지 않는 경우

**증상:** 특정 핸들러의 로그가 전혀 보이지 않습니다.

**확인 사항:**
1. Log Level 설정 확인 (`appsettings.json`에서 최소 Log Level이 Information 이상인지)
2. Pipeline 등록 확인 (DI 컨테이너에 `UsecaseLoggingPipeline`이 등록되어 있는지)
3. 필터 조건 확인 (검색 쿼리의 필터 조건이 너무 제한적이지 않은지)

### 필드 값이 비어 있는 경우

**증상:** `request.category.type` 값이 "unknown"으로 기록됩니다.

**원인:** Request 클래스가 `ICommandRequest<T>` 또는 `IQueryRequest<T>` 인터페이스를 구현하지 않았습니다.

**해결:** Request 클래스에 적절한 CQRS 인터페이스를 구현합니다.

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

1. **속성 레벨 제외:** `[JsonIgnore]` 속성을 사용하여 특정 필드를 직렬화에서 제외합니다.

```csharp
public record CreateUserCommand(
    string Email,
    [property: JsonIgnore] string Password  // 로그에 포함되지 않음
) : ICommandRequest<UserId>;
```

2. **Log Level 조정:** 운영 환경에서는 Debug 레벨 로그를 비활성화하여 파라미터 값이 기록되지 않도록 합니다.

### Q: 커스텀 필드를 추가하려면?

A: Functorium의 자동 로깅은 표준 필드를 생성합니다. 커스텀 필드가 필요한 경우 핸들러 내에서 직접 로그를 추가할 수 있습니다:

```csharp
public async Task<Fin<OrderId>> Handle(CreateOrderCommand command, CancellationToken ct)
{
    _logger.LogInformation(
        "Processing order for customer {CustomerId} with {ItemCount} items",
        command.CustomerId,
        command.Items.Count);

    // 비즈니스 로직...
}
```

### Q: Adapter Layer에서 Debug 로그는 언제 활성화해야 하나요?

A: Debug 로그는 다음 상황에서 활성화합니다:

- **개발 환경:** 항상 활성화하여 상세 정보 확인
- **스테이징 환경:** 통합 테스트 시 활성화
- **운영 환경:** 문제 해결이 필요할 때만 일시적으로 활성화

주의: Debug 로그에는 파라미터 값과 결과 값이 포함되므로 민감한 데이터가 노출될 수 있습니다.

### Q: 로그 저장 비용을 줄이려면?

A: 다음 전략을 고려하세요:

1. **샘플링:** 성공 로그는 10%만 샘플링, 실패 로그는 100% 유지
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

## 참고 문서

- [OpenTelemetry Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/)
- [Serilog Structured Logging](https://serilog.net/)
- [Grafana Loki LogQL](https://grafana.com/docs/loki/latest/logql/)

**내부 문서:**
- [18-observability-spec.md](./18-observability-spec.md) — Observability 사양 (Field/Tag, Meter, 메시지 템플릿)
- [19-observability-naming.md](./19-observability-naming.md) — Observability 네이밍 가이드
- [21-observability-metrics.md](./21-observability-metrics.md) — Observability 메트릭 상세
- [22-observability-tracing.md](./22-observability-tracing.md) — Observability 트레이싱 상세
