# Functorium Metrics 매뉴얼

Functorium 프레임워크에서 메트릭을 수집하고 활용하여
애플리케이션의 성능과 건강 상태를 모니터링하는 방법을 알아봅니다.

## 목차

- [들어가며](#들어가며)
- [메트릭의 기초](#메트릭의-기초)
- [Functorium 메트릭 아키텍처](#functorium-메트릭-아키텍처)
- [Meter와 Instrument 이해하기](#meter와-instrument-이해하기)
- [태그 시스템 상세 가이드](#태그-시스템-상세-가이드)
- [Application Layer 메트릭](#application-layer-메트릭)
- [Adapter Layer 메트릭](#adapter-layer-메트릭)
- [DomainEvent 메트릭](#domainevent-메트릭)
- [에러 메트릭 이해하기](#에러-메트릭-이해하기)
- [대시보드 구성하기](#대시보드-구성하기)
- [실습: 메트릭 분석하기](#실습-메트릭-분석하기)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)

---

## 들어가며

"지난 1시간 동안 얼마나 많은 요청이 처리되었는가?"
"평균 응답 시간은 얼마인가?"
"에러율이 증가하고 있는가?"

이러한 질문들은 애플리케이션을 운영할 때 자주 마주치게 됩니다. 로그는 개별 이벤트를 기록하지만, 이런 **집계 질문**에 답하기에는 적합하지 않습니다. 메트릭은 바로 이런 질문에 효율적으로 답하기 위해 설계되었습니다.

Functorium은 OpenTelemetry Metrics 표준을 따르는 메트릭 수집 기능을 제공합니다. 프레임워크가 자동으로 핵심 메트릭을 수집하므로, 개발자는 별도의 코드 없이도 요청 수, 응답 시간, 에러율 등을 모니터링할 수 있습니다.

### 이 문서에서 배우는 내용

이 문서를 통해 다음을 학습합니다:

1. **메트릭이 로깅과 어떻게 다른지** - 용도와 특성 비교
2. **Functorium이 자동으로 수집하는 메트릭의 종류** - Counter, Histogram의 활용
3. **태그 시스템의 설계 원리** - 카디널리티와 성능 고려사항
4. **Prometheus와 Grafana를 활용한 메트릭 분석 방법** - PromQL 쿼리 예시

### 사전 지식

이 문서를 이해하기 위해 다음 개념에 대한 기본적인 이해가 필요합니다:

- Functorium 로깅 매뉴얼의 내용 (필드 네이밍, 아키텍처 레이어)
- 기본적인 통계 개념 (평균, 백분위수)
- 시계열 데이터의 개념

---

## 메트릭의 기초

### 메트릭 vs 로깅

메트릭과 로깅은 서로 다른 목적을 가집니다. 두 가지 모두 관찰 가능성(Observability)의 핵심 요소이지만, 답하는 질문의 유형이 다릅니다.

| 특성 | 로깅 | 메트릭 |
|------|------|--------|
| **데이터 유형** | 개별 이벤트 | 집계된 숫자 |
| **질문 유형** | "무슨 일이 있었는가?" | "얼마나 많이/빠르게?" |
| **저장 비용** | 높음 (모든 이벤트 저장) | 낮음 (집계 값만 저장) |
| **실시간성** | 개별 이벤트 추적 | 트렌드 분석 |
| **검색 방식** | 필드 기반 필터링 | 수학적 집계 연산 |

**실제 예시로 이해하기:**

웹 쇼핑몰에서 주문 처리를 모니터링한다고 가정해봅시다.

**로깅**은 개별 이벤트를 기록합니다:
```
10:30:01 주문 #1001 처리 완료 (0.5초)
10:30:02 주문 #1002 처리 실패 (재고 부족)
10:30:03 주문 #1003 처리 완료 (0.3초)
...
```

**메트릭**은 집계된 숫자를 저장합니다:
```
10:30 - 주문 처리 수: 150건, 평균 시간: 0.4초, 에러율: 2%
10:31 - 주문 처리 수: 160건, 평균 시간: 0.45초, 에러율: 3%
10:32 - 주문 처리 수: 140건, 평균 시간: 0.38초, 에러율: 1%
```

로그로 "지난 1시간 동안 주문 처리 시간이 증가했는가?"라는 질문에 답하려면 수천 개의 로그를 읽고 파싱해야 합니다. 메트릭은 이미 집계된 데이터를 저장하므로 단순한 쿼리로 즉시 답할 수 있습니다.

### 메트릭의 세 가지 유형

OpenTelemetry는 세 가지 기본 메트릭 유형을 정의합니다. 각 유형은 서로 다른 측정 목적에 적합합니다.

#### 1. Counter (카운터)

Counter는 **누적되는 값**을 추적합니다. 항상 증가하며 절대 감소하지 않습니다. 재시작 시에만 0으로 리셋됩니다.

**적합한 용도:**
- 총 요청 수
- 총 에러 수
- 처리된 바이트 수
- 완료된 작업 수

**Functorium에서:**
- `requests` Counter: 총 요청 수
- `responses` Counter: 총 응답 수 (성공/실패 구분)

**사용 예시:**
```
# 지난 5분간 요청 수
increase(application_usecase_command_requests_total[5m])

# 초당 요청 수 (Rate)
rate(application_usecase_command_requests_total[1m])
```

#### 2. Histogram (히스토그램)

Histogram은 **값의 분포**를 추적합니다. 값을 미리 정의된 버킷(bucket)에 분류하여 저장합니다. 평균뿐 아니라 백분위수(P50, P95, P99) 계산에 사용됩니다.

**적합한 용도:**
- 요청 처리 시간
- 응답 크기
- 대기열 크기

**Functorium에서:**
- `duration` Histogram: 처리 시간 분포

**왜 평균보다 백분위수가 중요한가:**

평균만으로는 사용자 경험을 정확히 파악할 수 없습니다. 예를 들어:

```
10개 요청의 응답 시간:
90ms, 95ms, 100ms, 105ms, 110ms, 100ms, 95ms, 100ms, 105ms, 2000ms

평균: 290ms
P50 (중앙값): 100ms
P99: 2000ms
```

평균이 290ms이지만 실제로 대부분의 사용자(90%)는 110ms 이하의 응답을 받습니다. 한 명의 사용자만 2초를 기다렸습니다. 평균만 보면 모든 사용자가 느린 것처럼 보이지만, 실제로는 특정 케이스만 느린 것입니다.

**P99가 중요한 이유:** 100명 중 1명은 P99 수준의 응답 시간을 경험합니다. 하루에 100만 요청이 있다면, 1만 명의 사용자가 P99 수준의 느린 응답을 경험합니다.

**사용 예시:**
```
# P95 응답 시간
histogram_quantile(0.95, rate(application_usecase_command_duration_bucket[5m]))

# 평균 응답 시간
rate(application_usecase_command_duration_sum[5m]) / rate(application_usecase_command_duration_count[5m])
```

#### 3. Gauge (게이지)

Gauge는 **현재 값**을 추적합니다. 증가하거나 감소할 수 있으며, 특정 시점의 상태를 나타냅니다.

**적합한 용도:**
- 현재 활성 연결 수
- 메모리 사용량
- 큐 크기
- 온도

**참고:** Functorium의 자동 메트릭에는 Gauge가 포함되지 않습니다. 비즈니스 요구사항에 따라 사용자 정의 Gauge를 추가할 수 있습니다.

### response.elapsed가 태그가 아닌 이유

로깅에서는 `response.elapsed`가 필드로 포함되지만, 메트릭에서는 태그로 포함되지 않습니다. 이 설계 결정에는 중요한 이유가 있습니다.

**카디널리티 폭발 문제:**

태그는 메트릭을 그룹화하는 데 사용됩니다. 메트릭 시스템은 각 고유한 태그 조합마다 별도의 **시계열(time series)**을 생성합니다.

만약 처리 시간(예: 0.0234초)을 태그로 사용하면:
- 0.0234초 → 시계열 1
- 0.0235초 → 시계열 2
- 0.0236초 → 시계열 3
- ...무한히 증가

이를 **카디널리티 폭발(Cardinality Explosion)**이라고 합니다. 수백만 개의 시계열이 생성되어 메트릭 저장소에 심각한 부하를 주고, 쿼리 성능이 급격히 저하됩니다.

**해결책: Histogram 사용**

처리 시간은 `duration` Histogram으로 기록됩니다. Histogram은 값을 **버킷(bucket)**으로 그룹화합니다:

```
버킷: 0-50ms, 50-100ms, 100-250ms, 250-500ms, 500ms-1s, 1s-2.5s, 2.5s-5s, 5s+

요청 100개의 분포:
- 0-50ms: 45개
- 50-100ms: 30개
- 100-250ms: 15개
- 250-500ms: 7개
- 500ms-1s: 2개
- 1s+: 1개
```

이 방식으로 시계열 수를 제한하면서도 분포 정보(백분위수)를 유지할 수 있습니다.

---

## Functorium 메트릭 아키텍처

Functorium은 Application Layer와 Adapter Layer 각각에서 자동으로 메트릭을 수집합니다. 개발자가 명시적으로 메트릭 코드를 작성하지 않아도 프레임워크가 핵심 지표를 기록합니다.

### 아키텍처 개요

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

### Meter 조직 구조

Functorium은 Meter를 서비스 네임스페이스와 레이어별로 조직합니다. 이 구조는 메트릭을 논리적으로 그룹화하고 필터링하기 쉽게 합니다.

**Meter 이름 패턴:**

| Layer | Meter 이름 패턴 | 예시 |
|-------|-----------------|------|
| Application | `{service.namespace}.application` | `mycompany.production.application` |
| Adapter | `{service.namespace}.adapter.{category}` | `mycompany.production.adapter.repository` |

**예시 설정:**

```csharp
services.Configure<OpenTelemetryOptions>(options =>
{
    options.ServiceNamespace = "mycompany.production";
});
```

이 설정으로 생성되는 Meter:
- `mycompany.production.application`
- `mycompany.production.adapter.repository`
- `mycompany.production.adapter.gateway`

---

## Meter와 Instrument 이해하기

### Meter

Meter는 관련된 Instrument들의 논리적 그룹입니다. OpenTelemetry에서 Meter는 일반적으로 라이브러리나 컴포넌트 단위로 생성됩니다.

Functorium에서 Meter는 **서비스 네임스페이스 + 레이어 + 카테고리** 조합으로 생성됩니다. 이 구조는 다음과 같은 이점을 제공합니다:

1. **선택적 수집:** 특정 레이어나 카테고리의 메트릭만 수집 가능
2. **세분화된 모니터링:** 레이어별로 다른 알림 규칙 적용 가능
3. **비용 관리:** 필요 없는 메트릭 수집 비활성화 가능

### Instrument 구조

Functorium은 각 레이어에서 세 가지 Instrument를 생성합니다:

#### 1. requests Counter

요청 시작 시점에 증가합니다. 시스템에 들어온 총 요청 수를 추적합니다.

**Application Layer:**
```
Instrument: application.usecase.{cqrs}.requests
Unit: {request}
예: application.usecase.command.requests
```

**Adapter Layer:**
```
Instrument: adapter.{category}.requests
Unit: {request}
예: adapter.repository.requests
```

#### 2. responses Counter

응답 완료 시점에 증가합니다. 성공/실패가 `response.status` 태그로 구분됩니다.

**Application Layer:**
```
Instrument: application.usecase.{cqrs}.responses
Unit: {response}
예: application.usecase.command.responses
```

**Adapter Layer:**
```
Instrument: adapter.{category}.responses
Unit: {response}
예: adapter.repository.responses
```

#### 3. duration Histogram

처리 시간의 분포를 기록합니다. 초(second) 단위로 측정됩니다.

**Application Layer:**
```
Instrument: application.usecase.{cqrs}.duration
Unit: s (seconds)
예: application.usecase.command.duration
```

**Adapter Layer:**
```
Instrument: adapter.{category}.duration
Unit: s (seconds)
예: adapter.repository.duration
```

### Instrument 네이밍 규칙

Functorium의 모든 Instrument 이름은 다음 규칙을 따릅니다:

1. **소문자 사용**: `application.usecase.command.requests` (PascalCase 사용 안 함)
2. **점(dot) 구분자**: `application.usecase.command` (언더스코어 사용 안 함)
3. **복수형 사용**: `requests`, `responses` (단수형 사용 안 함)
4. **의미있는 계층 구조**: `{layer}.{category}.{cqrs}.{type}`

---

## 태그 시스템 상세 가이드

태그(Tag)는 메트릭을 다양한 차원으로 분석할 수 있게 해주는 메타데이터입니다. Functorium은 로깅과 동일한 태그 키를 사용하여 일관성을 유지합니다.

### Application Layer 태그 구조

Application Layer에서는 CQRS 타입에 따라 다른 Instrument를 사용하므로, `request.category.type`도 태그로 포함됩니다.

**태그 구조표:**

| 태그 키 | requests | duration | responses (성공) | responses (실패) |
|---------|----------|----------|------------------|------------------|
| `request.layer` | "application" | "application" | "application" | "application" |
| `request.category` | "usecase" | "usecase" | "usecase" | "usecase" |
| `request.category.type` | "command"/"query" | "command"/"query" | "command"/"query" | "command"/"query" |
| `request.handler` | 핸들러명 | 핸들러명 | 핸들러명 | 핸들러명 |
| `request.handler.method` | "Handle" | "Handle" | "Handle" | "Handle" |
| `response.status` | - | - | "success" | "failure" |
| `error.type` | - | - | - | "expected"/"exceptional"/"aggregate" |
| `error.code` | - | - | - | 에러 코드 |
| **총 태그 수** | **5** | **5** | **6** | **8** |

**예시 - Command 성공:**

```
# requests Counter
application_usecase_command_requests_total{
  request_layer="application",
  request_category="usecase",
  request_handler_cqrs="command",
  request_handler="CreateOrderCommandHandler",
  request_handler_method="Handle"
} 1

# responses Counter
application_usecase_command_responses_total{
  request_layer="application",
  request_category="usecase",
  request_handler_cqrs="command",
  request_handler="CreateOrderCommandHandler",
  request_handler_method="Handle",
  response_status="success"
} 1
```

**예시 - Command 실패:**

```
application_usecase_command_responses_total{
  request_layer="application",
  request_category="usecase",
  request_handler_cqrs="command",
  request_handler="CreateOrderCommandHandler",
  request_handler_method="Handle",
  response_status="failure",
  error_type="expected",
  error_code="Order.InsufficientStock"
} 1
```

### Adapter Layer 태그 구조

Adapter Layer에서는 CQRS 구분이 없으므로 태그 수가 Application Layer보다 적습니다.

**태그 구조표:**

| 태그 키 | requests | duration | responses (성공) | responses (실패) |
|---------|----------|----------|------------------|------------------|
| `request.layer` | "adapter" | "adapter" | "adapter" | "adapter" |
| `request.category` | 카테고리명 | 카테고리명 | 카테고리명 | 카테고리명 |
| `request.handler` | 핸들러명 | 핸들러명 | 핸들러명 | 핸들러명 |
| `request.handler.method` | 메서드명 | 메서드명 | 메서드명 | 메서드명 |
| `response.status` | - | - | "success" | "failure" |
| `error.type` | - | - | - | "expected"/"exceptional"/"aggregate" |
| `error.code` | - | - | - | 에러 코드 |
| **총 태그 수** | **4** | **4** | **5** | **7** |

**예시 - Repository 성공:**

```
adapter_repository_responses_total{
  request_layer="adapter",
  request_category="repository",
  request_handler="OrderRepository",
  request_handler_method="GetById",
  response_status="success"
} 1
```

### 카디널리티 고려사항

태그 값의 고유 조합 수를 **카디널리티(Cardinality)**라고 합니다. 높은 카디널리티는 메트릭 저장소의 성능을 저하시킵니다.

**Functorium의 카디널리티 관리:**

1. **고정된 태그 값 사용**: `request.layer`, `request.category`, `request.category.type`는 제한된 값만 가집니다.

2. **고유 식별자 제외**: 요청 ID, 사용자 ID, 주문 ID 등은 태그로 포함하지 않습니다. 이런 값을 포함하면 카디널리티가 폭발합니다.

3. **처리 시간 태그 제외**: `response.elapsed`는 연속적인 값이므로 태그가 아닌 Histogram으로 기록합니다.

**예상 카디널리티 계산:**

```
Application Layer:
- request.layer: 1 (application)
- request.category: 1 (usecase)
- request.category.type: 2 (command, query)
- request.handler: N (핸들러 수)
- request.handler.method: 1 (Handle)
- response.status: 2 (success, failure)
- error.type: 3 (expected, exceptional, aggregate)
- error.code: M (에러 코드 수)

최대 카디널리티 ≈ 1 × 1 × 2 × N × 1 × 2 × 3 × M = 12 × N × M

핸들러 100개, 에러 코드 50개 가정:
최대 카디널리티 ≈ 12 × 100 × 50 = 60,000 시계열
```

60,000 시계열은 대부분의 메트릭 시스템에서 충분히 처리 가능한 수준입니다.

---

## Application Layer 메트릭

Application Layer의 메트릭은 `UsecaseMetricsPipeline`에 의해 자동으로 수집됩니다.

### Instrument 상세

**1. application.usecase.{cqrs}.requests**

| 속성 | 값 |
|------|-----|
| 타입 | Counter |
| 단위 | {request} |
| 설명 | Usecase 요청 수 |
| 기록 시점 | 핸들러 실행 시작 시 |

**2. application.usecase.{cqrs}.responses**

| 속성 | 값 |
|------|-----|
| 타입 | Counter |
| 단위 | {response} |
| 설명 | Usecase 응답 수 (성공/실패 구분) |
| 기록 시점 | 핸들러 실행 완료 시 |

**3. application.usecase.{cqrs}.duration**

| 속성 | 값 |
|------|-----|
| 타입 | Histogram |
| 단위 | s (seconds) |
| 설명 | Usecase 처리 시간 분포 |
| 기록 시점 | 핸들러 실행 완료 시 |

### 핵심 지표 계산

**1. 처리량 (Throughput)**

초당 요청 수를 계산합니다:

```promql
# 전체 Command 처리량 (requests/s)
rate(application_usecase_command_requests_total[5m])

# 특정 핸들러의 처리량
rate(application_usecase_command_requests_total{
  request_handler="CreateOrderCommandHandler"
}[5m])
```

**2. 에러율 (Error Rate)**

전체 응답 중 실패 비율을 계산합니다:

```promql
# Command 에러율 (%)
rate(application_usecase_command_responses_total{response_status="failure"}[5m])
/
rate(application_usecase_command_responses_total[5m])
* 100

# 특정 핸들러의 에러율
rate(application_usecase_command_responses_total{
  request_handler="CreateOrderCommandHandler",
  response_status="failure"
}[5m])
/
rate(application_usecase_command_responses_total{
  request_handler="CreateOrderCommandHandler"
}[5m])
* 100
```

**3. 응답 시간 (Latency)**

```promql
# P50 (중앙값)
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

# 평균
rate(application_usecase_command_duration_sum[5m])
/
rate(application_usecase_command_duration_count[5m])
```

---

## Adapter Layer 메트릭

Adapter Layer의 메트릭은 Source Generator가 자동으로 생성한 코드에 의해 수집됩니다.

### Instrument 상세

**1. adapter.{category}.requests**

| 속성 | 값 |
|------|-----|
| 타입 | Counter |
| 단위 | {request} |
| 설명 | Adapter 요청 수 |
| 기록 시점 | 메서드 실행 시작 시 |

**2. adapter.{category}.responses**

| 속성 | 값 |
|------|-----|
| 타입 | Counter |
| 단위 | {response} |
| 설명 | Adapter 응답 수 (성공/실패 구분) |
| 기록 시점 | 메서드 실행 완료 시 |

**3. adapter.{category}.duration**

| 속성 | 값 |
|------|-----|
| 타입 | Histogram |
| 단위 | s (seconds) |
| 설명 | Adapter 처리 시간 분포 |
| 기록 시점 | 메서드 실행 완료 시 |

### Repository 메트릭 분석

데이터베이스 작업의 성능을 모니터링하는 예시입니다:

**메서드별 처리량:**

```promql
# Repository 메서드별 초당 요청 수
sum by (request_handler_method) (
  rate(adapter_repository_requests_total{
    request_handler="OrderRepository"
  }[5m])
)
```

**느린 쿼리 식별:**

```promql
# P95 응답 시간이 1초를 초과하는 메서드
histogram_quantile(0.95,
  rate(adapter_repository_duration_bucket[5m])
) > 1
```

**에러율이 높은 메서드:**

```promql
# 에러율이 5%를 초과하는 Repository 메서드
rate(adapter_repository_responses_total{response_status="failure"}[5m])
/
rate(adapter_repository_responses_total[5m])
> 0.05
```

---

## DomainEvent 메트릭

DomainEvent의 메트릭은 Publisher와 Handler 각각에서 수집됩니다. 두 컴포넌트 모두 동일한 3종 Instrument(requests, responses, duration)를 사용합니다.

### Meter Name

| 컴포넌트 | Meter Name 패턴 | 예시 (`ServiceNamespace = "mycompany.production"`) |
|----------|-----------------|---------------------------------------------------|
| Publisher | `{service.namespace}.adapter.event` | `mycompany.production.adapter.event` |
| Handler | `{service.namespace}.application` | `mycompany.production.application` |

### Publisher Instrument 상세

**1. adapter.event.requests**

| 속성 | 값 |
|------|-----|
| 타입 | Counter |
| 단위 | {request} |
| 설명 | DomainEvent 발행 요청 수 |
| 기록 시점 | Publisher 실행 시작 시 |

**2. adapter.event.responses**

| 속성 | 값 |
|------|-----|
| 타입 | Counter |
| 단위 | {response} |
| 설명 | DomainEvent 발행 응답 수 (성공/실패 구분) |
| 기록 시점 | Publisher 실행 완료 시 |

**3. adapter.event.duration**

| 속성 | 값 |
|------|-----|
| 타입 | Histogram |
| 단위 | s (seconds) |
| 설명 | DomainEvent 발행 처리 시간 분포 |
| 기록 시점 | Publisher 실행 완료 시 |

### Publisher 태그 구조

| 태그 키 | requests | duration | responses (성공) | responses (실패) |
|---------|----------|----------|------------------|------------------|
| `request.layer` | "adapter" | "adapter" | "adapter" | "adapter" |
| `request.category` | "event" | "event" | "event" | "event" |
| `request.handler` | 핸들러명 | 핸들러명 | 핸들러명 | 핸들러명 |
| `request.handler.method` | 메서드명 | 메서드명 | 메서드명 | 메서드명 |
| `response.status` | - | "success"/"failure" | "success" | "failure" |
| `error.type` | - | - | - | "expected"/"exceptional" |
| `error.code` | - | - | - | 에러 코드 |
| **총 태그 수** | **4** | **5** | **5** | **7** |

> **DomainEvent Metrics에서 제외되는 태그:**
> `request.event.count`, `response.event.success_count`, `response.event.failure_count`는 Metrics 태그로 사용하지 않습니다.
> 이 값들은 각각 고유한 수치를 가지므로 태그로 사용하면 **높은 카디널리티 폭발**을 유발합니다.
> 이는 `response.elapsed`를 Metrics 태그로 사용하지 않는 것과 동일한 원칙입니다.

### Handler Instrument 상세

**1. application.usecase.event.requests**

| 속성 | 값 |
|------|-----|
| 타입 | Counter |
| 단위 | {request} |
| 설명 | DomainEvent Handler 요청 수 |
| 기록 시점 | Handler 실행 시작 시 |

**2. application.usecase.event.responses**

| 속성 | 값 |
|------|-----|
| 타입 | Counter |
| 단위 | {response} |
| 설명 | DomainEvent Handler 응답 수 (성공/실패 구분) |
| 기록 시점 | Handler 실행 완료 시 |

**3. application.usecase.event.duration**

| 속성 | 값 |
|------|-----|
| 타입 | Histogram |
| 단위 | s (seconds) |
| 설명 | DomainEvent Handler 처리 시간 분포 |
| 기록 시점 | Handler 실행 완료 시 |

### Handler 태그 구조

| 태그 키 | requests | duration | responses (성공) | responses (실패) |
|---------|----------|----------|------------------|------------------|
| `request.layer` | "application" | "application" | "application" | "application" |
| `request.category` | "usecase" | "usecase" | "usecase" | "usecase" |
| `request.category.type` | "event" | "event" | "event" | "event" |
| `request.handler` | 핸들러명 | 핸들러명 | 핸들러명 | 핸들러명 |
| `request.handler.method` | "Handle" | "Handle" | "Handle" | "Handle" |
| `response.status` | - | - | "success" | "failure" |
| `error.type` | - | - | - | "expected"/"exceptional" |
| `error.code` | - | - | - | 에러 코드 |
| **총 태그 수** | **5** | **5** | **6** | **8** |

### PromQL 쿼리 예시

**DomainEvent Publisher 처리량:**

```promql
# Publisher 초당 발행 수
rate(adapter_event_requests_total[5m])

# 특정 Aggregate의 발행량
rate(adapter_event_requests_total{
  request_handler="Product"
}[5m])
```

**DomainEvent Handler 에러율:**

```promql
# Handler 에러율 (exceptional)
rate(application_usecase_event_responses_total{error_type="exceptional"}[5m])
/
rate(application_usecase_event_responses_total[5m])
* 100
```

**Handler별 처리 시간:**

```promql
# Handler P95 응답 시간
histogram_quantile(0.95,
  rate(application_usecase_event_duration_bucket[5m])
)

# 특정 Handler의 평균 응답 시간
rate(application_usecase_event_duration_sum{
  request_handler="OnProductCreated"
}[5m])
/
rate(application_usecase_event_duration_count{
  request_handler="OnProductCreated"
}[5m])
```

---

## 에러 메트릭 이해하기

### 에러 태그 구조

실패한 응답의 메트릭에는 `error.type`과 `error.code` 태그가 추가됩니다.

| error.type | 의미 | 대응 |
|------------|------|------|
| "expected" | 예상된 비즈니스 오류 | 패턴 분석, 비즈니스 개선 |
| "exceptional" | 예외적 시스템 오류 | 즉시 알림, 기술적 조사 |
| "aggregate" | 여러 오류 결합 | 첫 번째 에러 코드로 분류 |

### 에러 타입별 분석

**시스템 에러 모니터링 (즉시 대응 필요):**

```promql
# Exceptional 에러 발생률
rate(application_usecase_command_responses_total{
  error_type="exceptional"
}[5m])
```

**비즈니스 에러 패턴 분석:**

```promql
# Expected 에러 코드별 발생 빈도
sum by (error_code) (
  increase(application_usecase_command_responses_total{
    error_type="expected"
  }[1h])
)
```

### 알림 규칙 설정

**1. 시스템 에러 알림:**

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
    summary: "시스템 에러율이 1%를 초과했습니다"
```

**2. 특정 에러 코드 급증 알림:**

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

---

## 대시보드 구성하기

### RED 방법론

RED는 서비스 모니터링을 위한 핵심 지표입니다:

- **R**ate: 초당 요청 수
- **E**rrors: 에러율
- **D**uration: 응답 시간 분포

### 권장 대시보드 패널

**1. 전체 현황 패널**

```promql
# 총 처리량
sum(rate(application_usecase_command_requests_total[5m]))
+ sum(rate(application_usecase_query_requests_total[5m]))

# 전체 에러율
sum(rate(application_usecase_command_responses_total{response_status="failure"}[5m]))
+ sum(rate(application_usecase_query_responses_total{response_status="failure"}[5m]))
/
sum(rate(application_usecase_command_responses_total[5m]))
+ sum(rate(application_usecase_query_responses_total[5m]))

# P99 응답 시간
histogram_quantile(0.99,
  sum(rate(application_usecase_command_duration_bucket[5m])) by (le)
)
```

**2. 핸들러별 비교 패널**

```promql
# 핸들러별 처리량 (Top 10)
topk(10,
  sum by (request_handler) (
    rate(application_usecase_command_requests_total[5m])
  )
)

# 핸들러별 에러율
sum by (request_handler) (
  rate(application_usecase_command_responses_total{response_status="failure"}[5m])
)
/
sum by (request_handler) (
  rate(application_usecase_command_responses_total[5m])
)
```

**3. 에러 분석 패널**

```promql
# 에러 타입별 분포
sum by (error_type) (
  rate(application_usecase_command_responses_total{response_status="failure"}[5m])
)

# 에러 코드별 Top 10
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

## 실습: 메트릭 분석하기

### 시나리오 1: 성능 저하 조사

**상황:** "주문 생성이 느리다"는 보고가 들어왔습니다.

**단계 1: 현재 상태 확인**

```promql
# CreateOrderCommandHandler의 P95 응답 시간
histogram_quantile(0.95,
  rate(application_usecase_command_duration_bucket{
    request_handler="CreateOrderCommandHandler"
  }[5m])
)
```

**단계 2: 시간대별 추이 확인**

```promql
# 1시간 동안의 P95 추이
histogram_quantile(0.95,
  rate(application_usecase_command_duration_bucket{
    request_handler="CreateOrderCommandHandler"
  }[5m])
)
```

Grafana에서 1시간 범위로 그래프를 확인합니다.

**단계 3: 하위 호출 분석**

```promql
# 관련 Adapter의 응답 시간
histogram_quantile(0.95,
  rate(adapter_repository_duration_bucket{
    request_handler="OrderRepository"
  }[5m])
)

histogram_quantile(0.95,
  rate(adapter_gateway_duration_bucket{
    request_handler="PaymentGateway"
  }[5m])
)
```

**단계 4: 결론 도출**

PaymentGateway의 응답 시간이 급격히 증가했다면, 외부 결제 서비스의 지연이 근본 원인입니다.

### 시나리오 2: 에러 급증 조사

**상황:** 에러율이 평소 0.5%에서 5%로 급증했습니다.

**단계 1: 에러 타입 확인**

```promql
# 에러 타입별 발생률
sum by (error_type) (
  rate(application_usecase_command_responses_total{
    response_status="failure"
  }[5m])
)
```

**단계 2: 시스템 에러인 경우**

```promql
# Exceptional 에러 코드별 분포
sum by (error_code) (
  rate(application_usecase_command_responses_total{
    error_type="exceptional"
  }[5m])
)
```

`Database.ConnectionFailed`가 대부분이라면 데이터베이스 상태를 확인합니다.

**단계 3: 비즈니스 에러인 경우**

```promql
# Expected 에러 코드별 분포
sum by (error_code) (
  rate(application_usecase_command_responses_total{
    error_type="expected"
  }[5m])
)
```

`Order.InsufficientStock`이 급증했다면 재고 부족 상황을 확인합니다.

---

## 트러블슈팅

### 메트릭이 수집되지 않는 경우

**증상:** Prometheus에서 특정 메트릭이 보이지 않습니다.

**확인 사항:**

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

### 카디널리티가 너무 높은 경우

**증상:** 메트릭 저장소의 디스크 사용량이 급격히 증가합니다.

**원인:** 고유 값이 많은 태그가 포함되었습니다.

**확인 방법:**

```promql
# 카디널리티 확인
count(application_usecase_command_responses_total)
```

**해결 방법:**

1. 불필요한 태그 제거
2. 태그 값 정규화 (예: 에러 메시지 대신 에러 코드 사용)
3. 특정 조건에서만 메트릭 수집

### Histogram 버킷이 적절하지 않은 경우

**증상:** P95, P99 값이 부정확합니다.

**원인:** 기본 버킷 경계가 실제 분포와 맞지 않습니다.

**해결 방법:**

커스텀 버킷 경계를 설정합니다:

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

Functorium의 자동 메트릭은 요청/응답 수(Counter)와 처리 시간(Histogram)을 포함합니다.

### Q: rate()와 increase()의 차이는?

A:
- `rate()`: 초당 변화율 (requests/second)
- `increase()`: 지정 기간 동안의 총 증가량 (requests)

```promql
# 초당 요청 수
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

A: 동일한 태그 키를 사용하므로 상관관계를 쉽게 추적할 수 있습니다:

1. 메트릭에서 이상 감지: `error_code="Database.ConnectionFailed"` 급증
2. 로그에서 상세 조사: `error.code = "Database.ConnectionFailed"`로 필터링

Grafana의 Explore 기능을 사용하면 메트릭에서 관련 로그로 쉽게 이동할 수 있습니다.

---

## 참조

- [OpenTelemetry Metrics Specification](https://opentelemetry.io/docs/specs/otel/metrics/)
- [Prometheus Query Language (PromQL)](https://prometheus.io/docs/prometheus/latest/querying/basics/)
- [Grafana Dashboard Best Practices](https://grafana.com/docs/grafana/latest/best-practices/best-practices-for-creating-dashboards/)
- [RED Method](https://www.weave.works/blog/the-red-method-key-metrics-for-microservices-architecture/)
- [Observability Specification](./observability-spec.md)
