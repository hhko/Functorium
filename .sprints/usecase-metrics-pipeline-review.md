# Usecase Observability Pipeline 리뷰

> **작성일**: 2026-01-04
> **최종 수정**: 2026-01-04
> **대상**: Usecase Pipeline (Metrics, Tracing, Logging)
> **참조**: OpenTelemetry Semantic Conventions
> **진척률**: 2/7 완료 (29%)

---

## 목차

1. [개요](#1-개요)
2. [현재 데이터 구조](#2-현재-데이터-구조)
3. [OpenTelemetry 표준 준수 평가](#3-opentelemetry-표준-준수-평가)
4. [운영 시나리오 분석](#4-운영-시나리오-분석)
5. [SLI/SLO/SLA 지원 분석](#5-slislosla-지원-분석)
6. [종합 개선 계획](#6-종합-개선-계획)
7. [참고 자료](#7-참고-자료)

---

## 1. 개요

이 문서는 Functorium의 Usecase Pipeline이 생성하는 관찰 가능성 데이터를 리뷰한 결과입니다.

**리뷰 범위:**

| Pipeline | 파일 | 역할 |
|----------|------|------|
| **Metrics** | `UsecaseMetricsPipeline.cs` | 요청 수, 응답 수, 처리 시간 측정 |
| **Tracing** | `UsecaseTracingPipeline.cs` | 분산 추적 Span 생성 |
| **Logging** | `UsecaseLoggingPipeline.cs` | 구조화된 요청/응답 로깅 |

**리뷰 관점:**

- OpenTelemetry Semantic Conventions 준수 여부
- 운영 시나리오 지원 (장애/성능/용량 분석)
- SLI/SLO/SLA 관리 가능 여부
- 코드 품질 및 성능

---

## 2. 현재 데이터 구조

### 2.1 Metrics (UsecaseMetricsPipeline)

**메트릭 목록:**

| 메트릭 이름 | 타입 | 단위 | 설명 |
|------------|------|------|------|
| `application.usecase.{cqrs}.requests` | Counter | `{request}` | 총 요청 수 |
| `application.usecase.{cqrs}.responses` | Counter | `{response}` | 응답 수 (status 태그로 구분) |
| `application.usecase.{cqrs}.duration` | Histogram | `s` | 처리 시간 (초) |

**태그 구조 (통일됨 ✅):**

| Tag Key | requests / duration | responses |
|---------|---------------------|-----------|
| `request.layer` | `"application"` | `"application"` |
| `request.category` | `"usecase"` | `"usecase"` |
| `request.handler.cqrs` | `"command"` / `"query"` | `"command"` / `"query"` |
| `request.handler` | handler name | handler name |
| `request.handler.method` | `"Handle"` | `"Handle"` |
| `response.status` | (없음) | `"success"` / `"failure"` |
| **Total Tags** | **5** | **6** |

### 2.2 Tracing (UsecaseTracingPipeline)

**Span 태그:**

```
request.layer = "application"
request.category = "usecase"
request.handler = "CreateUserCommand"
request.handler.cqrs = "command"
code.function = "CreateUserCommand.Handle"
response.status = "success" | "failure"
response.elapsed = 42.5

# 실패 시 추가
error.type = "ErrorCodeExpected" | "ErrorCodeExceptional" | "ManyErrors"
error.code = "DomainErrors.City.Empty"
error.message = "City cannot be empty"
error.count = 3  # ManyErrors인 경우
```

### 2.3 Logging (UsecaseLoggingPipeline)

**로그 필드:**

| 필드 | 설명 | 예시 |
|------|------|------|
| `RequestLayer` | 레이어 | `"application"` |
| `RequestCategory` | 카테고리 | `"usecase"` |
| `RequestHandler` | 핸들러 이름 | `"CreateUserCommand"` |
| `RequestHandlerCqrs` | CQRS 타입 | `"command"` |
| `RequestHandlerMethod` | 메서드 | `"Handle"` |
| `Request` | 요청 데이터 | `{ Name: "John", ... }` |
| `Response` | 응답 데이터 | `{ UserId: "...", ... }` |
| `Status` | 상태 | `"success"` / `"failure"` |
| `Elapsed` | 처리 시간 (ms) | `42.5` |
| `Error` | 에러 정보 | `{ Code: "...", Message: "..." }` |

**로그 레벨:**

| 상황 | 레벨 | EventId |
|------|------|---------|
| 요청 | Information | 1001 (ApplicationRequest) |
| 성공 응답 | Information | 1002 (ApplicationResponseSuccess) |
| 비즈니스 에러 (Expected) | Warning | 1003 (ApplicationResponseWarning) |
| 시스템 에러 (Exceptional) | Error | 1004 (ApplicationResponseError) |

---

## 3. OpenTelemetry 표준 준수 평가

### 3.1 준수 항목 ✅

| 항목 | 평가 | 근거 |
|------|------|------|
| **단위 메타데이터** | ✅ 적합 | 단위를 이름에 포함하지 않고 메타데이터로 지정 |
| **Duration 단위** | ✅ 적합 | 초(s) 단위 사용 |
| **계층적 네이밍** | ✅ 적합 | `application.usecase.{cqrs}.*` 구조 |
| **커스텀 속성 네임스페이스** | ✅ 적합 | `request.*`, `response.*` 분리 |
| **에러 상태 처리** | ✅ 적합 | ActivityStatusCode.Error 설정 |

**준수하는 OTel 권장사항:**

- "Conventional metrics SHOULD NOT include the units in the metric name"
- "When instruments are measuring durations, seconds SHOULD be used"
- "Associated metrics SHOULD be nested together in a hierarchy"

### 3.2 개선 필요 항목 ⚠️

| 항목 | 문제점 | 영향 | 상태 |
|------|--------|------|------|
| ~~**응답 메트릭 중복**~~ | ~~success/failure가 별도 Counter + 태그에도 status 포함~~ | ~~카디널리티 증가, 쿼리 복잡~~ | ✅ 완료 |
| ~~**태그 구조 불일치**~~ | ~~requests와 responses의 태그 세트가 다름~~ | ~~JOIN 시 불일치~~ | ✅ 완료 |
| **request.handler.method** | 항상 "Handle" 값으로 의미 없음 | 불필요한 카디널리티 (단, 일관성 확보) | 유지 |
| **Meter 매 요청 생성** | 매 요청마다 Meter/Counter 생성 | 성능 오버헤드 | 대기 |

---

## 4. 운영 시나리오 분석

### 4.1 장애 분석 (Incident Analysis)

| 질문 | 지원 | 데이터 소스 |
|------|------|-------------|
| "에러가 발생하고 있나?" | ✅ | Metrics: `responses.failure` |
| "어떤 Usecase에서?" | ✅ | Metrics: `request.handler` 태그 |
| "에러 원인은?" | ✅ | Tracing: `error.code`, `error.message` |
| "요청 데이터는?" | ✅ | Logging: `Request` 필드 |
| "비즈니스/시스템 에러?" | ✅ | Logging: Warning vs Error 레벨 |
| "에러율은?" | ✅ | Metrics: `failure / total` |
| "어떤 에러가 가장 많이?" | ❌ | Metrics에 `error.code` 태그 없음 |

**활용 사례:**

```
시나리오: 새벽 3시에 에러 급증 알림 수신

1. "에러가 발생하고 있나?" → Metrics 확인
   ┌─────────────────────────────────────────────────────────────┐
   │ Grafana Alert: responses.failure rate > 10/min             │
   │ Query: sum(rate(responses_failure_total[5m])) > 0.16       │
   └─────────────────────────────────────────────────────────────┘

2. "어떤 Usecase에서?" → Metrics 태그로 필터링
   Query: sum by(request_handler)(rate(responses_failure_total[5m]))
   결과:
   ┌────────────────────────────┬──────────┐
   │ request_handler            │ rate     │
   ├────────────────────────────┼──────────┤
   │ CreateOrderCommand         │ 0.12     │ ← 원인 핸들러
   │ UpdateInventoryCommand     │ 0.02     │
   │ GetUserQuery               │ 0.01     │
   └────────────────────────────┴──────────┘

3. "에러 원인은?" → Tracing으로 상세 확인
   Jaeger에서 CreateOrderCommand Span 조회:
   ┌─────────────────────────────────────────────────────────────┐
   │ Span: Application usecase.command CreateOrderCommand.Handle│
   │ Status: Error                                               │
   │ Tags:                                                       │
   │   error.type = "ErrorCodeExceptional"                       │
   │   error.code = "InfraErrors.Database.ConnectionTimeout"     │
   │   error.message = "Connection to database timed out"        │
   └─────────────────────────────────────────────────────────────┘

4. "요청 데이터는?" → Logging으로 컨텍스트 확인
   Seq/Loki에서 로그 조회:
   ┌─────────────────────────────────────────────────────────────┐
   │ Level: Error                                                │
   │ EventId: 1004 (ApplicationResponseError)                    │
   │ RequestHandler: CreateOrderCommand                          │
   │ Request: { CustomerId: "C123", Items: [...] }               │
   │ Error: { Code: "InfraErrors.Database.ConnectionTimeout",    │
   │          Message: "Connection to database timed out" }      │
   │ Elapsed: 30045.23 ms                                        │
   └─────────────────────────────────────────────────────────────┘

5. 결론: DB 연결 타임아웃 → 인프라팀 에스컬레이션
```

### 4.2 성능 분석 (Performance Analysis)

| 질문 | 지원 | 데이터 소스 |
|------|------|-------------|
| "평균 응답 시간?" | ✅ | Metrics: `duration` Histogram |
| "느린 Usecase는?" | ✅ | Metrics: `request.handler` 태그 |
| "P95/P99 지연 시간?" | ✅ | Metrics: `histogram_quantile()` |
| "느린 요청 상세 흐름?" | ✅ | Tracing: Span timeline |
| "Command vs Query 성능?" | ✅ | Metrics: `request.handler.cqrs` 태그 |

**활용 사례:**

```
시나리오: 사용자가 "주문 화면이 느려요" 불만 접수

1. "평균 응답 시간?" → Metrics Histogram으로 확인
   Query: histogram_quantile(0.5, sum(rate(duration_bucket[5m])) by (le))
   결과: 125ms (중앙값)

2. "느린 Usecase는?" → 핸들러별 P95 비교
   Query: histogram_quantile(0.95,
            sum by(le, request_handler)(rate(duration_bucket[5m])))
   결과:
   ┌────────────────────────────┬──────────┐
   │ request_handler            │ P95 (ms) │
   ├────────────────────────────┼──────────┤
   │ GetOrderDetailsQuery       │ 2,450    │ ← 병목 발견
   │ GetProductListQuery        │ 320      │
   │ CreateOrderCommand         │ 180      │
   └────────────────────────────┴──────────┘

3. "P95/P99 지연 시간?" → 지연 분포 확인
   Query: histogram_quantile(0.99,
            sum(rate(duration_bucket{request_handler="GetOrderDetailsQuery"}[5m]))
            by (le))
   결과:
   ┌───────┬──────────┐
   │ 분위  │ 응답시간  │
   ├───────┼──────────┤
   │ P50   │ 450ms    │
   │ P95   │ 2,450ms  │
   │ P99   │ 5,200ms  │ ← 꼬리 지연 심각
   └───────┴──────────┘

4. "느린 요청 상세 흐름?" → Tracing으로 병목 구간 분석
   Jaeger에서 GetOrderDetailsQuery 느린 Span 조회:
   ┌─────────────────────────────────────────────────────────────┐
   │ GetOrderDetailsQuery.Handle                    [2,450ms]    │
   │ ├── OrderRepository.GetById                    [120ms]      │
   │ ├── PaymentService.GetPaymentHistory          [1,850ms] ←   │
   │ └── ShippingService.GetTrackingInfo           [480ms]       │
   └─────────────────────────────────────────────────────────────┘
   → PaymentService 외부 호출이 병목

5. "Command vs Query 성능?" → CQRS 타입별 비교
   Query: histogram_quantile(0.95,
            sum by(le, request_handler_cqrs)(rate(duration_bucket[5m])))
   결과:
   ┌──────────┬──────────┐
   │ CQRS     │ P95 (ms) │
   ├──────────┼──────────┤
   │ query    │ 890      │ ← Query가 더 느림 (외부 조회 많음)
   │ command  │ 180      │
   └──────────┴──────────┘

6. 결론: PaymentService 타임아웃 증가 또는 캐싱 검토
```

### 4.3 용량 분석 (Capacity Analysis)

| 질문 | 지원 | 데이터 소스 |
|------|------|-------------|
| "TPS는?" | ✅ | Metrics: `rate(requests_total)` |
| "피크 시간대?" | ✅ | Metrics: 시계열 데이터 |
| "가장 많이 호출되는 Usecase?" | ✅ | Metrics: `request.handler` 태그 |

**활용 사례:**

```
시나리오: 블랙프라이데이 대비 용량 계획 수립

1. "TPS는?" → 현재 처리량 확인
   Query: sum(rate(requests_total[5m]))
   결과:
   ┌────────────────────────────────────────┐
   │ 현재 TPS: 250 req/s                    │
   │ 피크 TPS (지난 30일): 420 req/s        │
   │ 예상 블프 TPS: 1,200 req/s (3배 예상)  │
   └────────────────────────────────────────┘

2. "피크 시간대?" → 시계열 패턴 분석
   Query: sum(rate(requests_total[5m])) over time range
   결과:
   ┌────────────────────────────────────────┐
   │ TPS                                    │
   │  500│      ╭──╮                        │
   │  400│     ╭╯  ╰─╮    ╭──╮             │
   │  300│  ╭──╯     ╰────╯  ╰──╮          │
   │  200│──╯                    ╰───      │
   │  100│                                  │
   │    0├──┬──┬──┬──┬──┬──┬──┬──┬──┬──┬   │
   │     00 04 08 12 16 20 00              │
   └────────────────────────────────────────┘
   → 피크: 11:00-13:00, 19:00-21:00

3. "가장 많이 호출되는 Usecase?" → 핸들러별 분포
   Query: topk(5, sum by(request_handler)(rate(requests_total[1h])))
   결과:
   ┌────────────────────────────┬──────────┬────────┐
   │ request_handler            │ req/s    │ 비율   │
   ├────────────────────────────┼──────────┼────────┤
   │ GetProductListQuery        │ 85       │ 34%    │
   │ GetProductDetailsQuery     │ 62       │ 25%    │
   │ AddToCartCommand           │ 45       │ 18%    │
   │ GetCartQuery               │ 35       │ 14%    │
   │ CreateOrderCommand         │ 23       │ 9%     │
   └────────────────────────────┴──────────┴────────┘

4. 용량 계획 도출:
   ┌─────────────────────────────────────────────────────────────┐
   │ 스케일링 우선순위:                                          │
   │ 1. GetProductListQuery - 캐시 레이어 강화 (34% 트래픽)      │
   │ 2. GetProductDetailsQuery - CDN 적용 검토 (25%)             │
   │ 3. AddToCartCommand - Redis 클러스터 확장 (18%)             │
   │                                                             │
   │ 예상 필요 리소스:                                           │
   │ - 현재 Pod: 4개 (250 TPS / 62.5 TPS per Pod)               │
   │ - 블프 필요: 20개 (1,200 TPS / 60 TPS per Pod + 여유분)    │
   └─────────────────────────────────────────────────────────────┘
```

### 4.4 Grafana 대시보드 구성

| 패널 | 쿼리 | 지원 |
|------|------|------|
| 총 요청 수 | `sum(rate(requests_total[5m]))` | ✅ |
| 에러율 | `failure / total` | ✅ |
| P95 응답 시간 | `histogram_quantile(0.95, duration_bucket)` | ✅ |
| 핸들러별 요청 | `sum by(request_handler)` | ✅ |
| 에러 코드별 분포 | `sum by(error_code)` | ❌ |

---

## 5. SLI/SLO/SLA 지원 분석

### 5.1 용어 정의

| 용어 | 정의 | 예시 |
|------|------|------|
| **SLI** | 서비스 품질 측정 지표 | 가용성, 응답 시간, 에러율 |
| **SLO** | SLI의 목표 값 | 가용성 99.9%, P95 < 200ms |
| **SLA** | 고객과의 계약 | SLO 미달 시 보상 조항 |

### 5.2 핵심 SLI 지원 현황

| SLI 유형 | 지원 | 계산 방법 |
|----------|------|----------|
| **Availability** | ✅ | `success / (success + failure)` |
| **Latency** | ✅ | `histogram_quantile(0.95, duration)` |
| **Throughput** | ✅ | `rate(requests_total)` |
| **Error Rate** | ✅ | `failure / total` |
| **Saturation** | ❌ | 시스템 메트릭 필요 |

### 5.3 SLO 대시보드 쿼리 예시

```promql
# 가용성 (30일)
sum(rate(responses_success_total[30d]))
/ (sum(rate(responses_success_total[30d])) + sum(rate(responses_failure_total[30d])))

# P95 응답 시간
histogram_quantile(0.95, sum(rate(duration_bucket[5m])) by (le))

# 에러율
sum(rate(responses_failure_total[5m])) / sum(rate(requests_total[5m]))
```

### 5.4 Error Budget

**개념:**

- SLO 99.9% → Error Budget 0.1%
- 30일 기준: 43.2분 다운타임 허용

**Alert 예시:**

```yaml
- alert: ErrorBudgetBurnRate
  expr: (sum(rate(responses_failure_total[1h])) / sum(rate(requests_total[1h]))) > 0.001 * 4
  for: 5m
  labels:
    severity: warning
```

### 5.5 권장 SLO 설정

| Usecase 유형 | 가용성 | P95 응답 시간 | 근거 |
|-------------|--------|--------------|------|
| **Command** | 99.9% | < 500ms | 데이터 변경 중요도 |
| **Query** | 99.5% | < 200ms | 조회는 재시도 가능 |

### 5.6 SLI/SLO 관점 미지원 항목

| 항목 | 문제점 | 영향 |
|------|--------|------|
| 에러 유형별 SLO | `error.type` 태그 없음 | 비즈니스/시스템 에러 구분 불가 |
| 에러 코드별 SLO | `error.code` 태그 없음 | 특정 에러 추적 불가 |
| 사용자별 SLO | `user.id` 태그 없음 | 고객별 SLO 불가 |
| 테넌트별 SLO | `tenant.id` 태그 없음 | 멀티테넌트 SLO 불가 |

---

## 6. 종합 개선 계획

### 6.1 진척률 요약

```
진행 상황: █████░░░░░░░░░░░░░░░ 29% (2/7 완료)

✅ 완료: 2개
⏳ 대기: 4개
➖ 취소: 1개 (방향 변경)
```

### 6.2 우선순위별 개선 항목

| 우선순위 | 항목 | 문제점 | 개선 방안 | 난이도 | 상태 |
|----------|------|--------|----------|--------|------|
| **1 (높음)** | 태그 구조 통일 | requests와 responses 태그 불일치 | 기본 태그 세트 통일 | 낮음 | ✅ 완료 |
| **2 (중)** | 응답 메트릭 통합 | success/failure 별도 Counter | 단일 Counter + status 태그 | 낮음 | ✅ 완료 |
| **3 (중)** | `error.type` 태그 | 에러 유형 구분 불가 | Expected/Exceptional 태그 추가 | 낮음 | ⏳ 대기 |
| **4 (중)** | `error.code` 태그 | 에러 패턴 분석 불가 | 에러 코드 태그 추가 | 낮음 | ⏳ 대기 |
| **5 (중)** | Meter 캐싱 | 매 요청 생성 오버헤드 | 인스턴스 재사용 | 중간 | ⏳ 대기 |
| ~~**6 (낮음)**~~ | ~~`request.handler.method` 제거~~ | ~~항상 "Handle"로 무의미~~ | ~~태그 제거~~ | ~~낮음~~ | ➖ 취소 |
| **7 (낮음)** | 사용자 컨텍스트 | 사용자별 추적 불가 | `user.id`, `tenant.id` 추가 | 중간 | ⏳ 대기 |

> **Note**: 우선순위 6번은 태그 구조 통일 방향으로 변경되어 취소됨. `request.handler.method`를 제거하는 대신, 모든 메트릭에 포함하여 일관성 확보.

### 6.3 상세 개선 계획

#### 6.3.1 태그 구조 통일 (우선순위 1) ✅ 완료

**변경 내용:**
- 커밋: `e355af2` (2026-01-04)
- 모든 메트릭에 `request.handler.method = "Handle"` 포함
- requests, duration: 5개 태그
- responses: 6개 태그 (5개 + `response.status`)

**이전 (불일치):**
```csharp
// requests, duration: 5개 태그 (request.handler.method 포함)
TagList tags = new TagList {
    { RequestLayer, "application" },
    { RequestCategory, "usecase" },
    { RequestHandlerCqrs, requestCqrs },
    { RequestHandler, requestHandler },
    { RequestHandlerMethod, "Handle" }
};

// responses: 5개 태그 (request.handler.method 없음!)
new KeyValuePair<string, object?>(RequestLayer, "application"),
new KeyValuePair<string, object?>(RequestCategory, "usecase"),
new KeyValuePair<string, object?>(RequestHandlerCqrs, requestCqrs),
new KeyValuePair<string, object?>(RequestHandler, requestHandler),
new KeyValuePair<string, object?>(ResponseStatus, "success/failure")
```

**현재 (통일됨):**
```csharp
// 요청 태그 (requests, duration 공통) - 5개
TagList requestTags = new TagList {
    { RequestLayer, "application" },
    { RequestCategory, "usecase" },
    { RequestHandlerCqrs, requestCqrs },
    { RequestHandler, requestHandler },
    { RequestHandlerMethod, "Handle" }
};

requestCounter.Add(1, requestTags);
durationHistogram.Record(elapsed, requestTags);

// 응답 태그 (requestTags + ResponseStatus) - 6개
TagList successTags = new TagList {
    { RequestLayer, "application" },
    { RequestCategory, "usecase" },
    { RequestHandlerCqrs, requestCqrs },
    { RequestHandler, requestHandler },
    { RequestHandlerMethod, "Handle" },
    { ResponseStatus, "success" }
};
responseSuccessCounter.Add(1, successTags);
```

**결과:**
```
┌──────────────────────────┬─────────────────────────┬─────────────────────────┐
│ Tag Key                  │ requestCounter          │ responseSuccessCounter  │
│                          │ durationHistogram       │ responseFailureCounter  │
├──────────────────────────┼─────────────────────────┼─────────────────────────┤
│ request.layer            │ "application"           │ "application"           │
│ request.category         │ "usecase"               │ "usecase"               │
│ request.handler.cqrs     │ "command"/"query"       │ "command"/"query"       │
│ request.handler          │ handler name            │ handler name            │
│ request.handler.method   │ "Handle"                │ "Handle"                │
│ response.status          │ (none)                  │ "success"/"failure"     │
├──────────────────────────┼─────────────────────────┼─────────────────────────┤
│ Total Tags               │ 5                       │ 6                       │
└──────────────────────────┴─────────────────────────┴─────────────────────────┘
```

#### 6.3.2 응답 메트릭 통합 (우선순위 2) ✅ 완료

**변경 내용:**
- 커밋: (2026-01-04)
- `responses.success`와 `responses.failure` 두 개의 Counter를 `responses` 단일 Counter로 통합
- `response.status` 태그로 성공/실패 구분

**이전 (분리됨):**
```csharp
// 메트릭 정의
Counter<long> responseSuccessCounter = meter.CreateCounter<long>(
    name: "application.usecase.{cqrs}.responses.success", ...);
Counter<long> responseFailureCounter = meter.CreateCounter<long>(
    name: "application.usecase.{cqrs}.responses.failure", ...);

// 기록
if (response.IsSucc)
    responseSuccessCounter.Add(1, successTags);
else
    responseFailureCounter.Add(1, failureTags);
```

**현재 (통합됨):**
```csharp
// 메트릭 정의 - 단일 Counter
Counter<long> responseCounter = meter.CreateCounter<long>(
    name: "application.usecase.{cqrs}.responses", ...);

// 기록 - status 태그로 구분
string responseStatus = response.IsSucc ? "success" : "failure";
TagList responseTags = new TagList {
    { RequestLayer, "application" },
    { RequestCategory, "usecase" },
    { RequestHandlerCqrs, requestCqrs },
    { RequestHandler, requestHandler },
    { RequestHandlerMethod, "Handle" },
    { ResponseStatus, responseStatus }
};
responseCounter.Add(1, responseTags);
```

**효과:**
- 메트릭 정의 수 감소 (2개 → 1개)
- Prometheus 쿼리 단순화:
  ```promql
  # 이전: 두 메트릭 합산 필요
  sum(rate(responses_success_total[5m])) + sum(rate(responses_failure_total[5m]))

  # 현재: 단일 메트릭
  sum(rate(responses_total[5m]))

  # 성공/실패 필터링
  sum(rate(responses_total{response_status="success"}[5m]))
  sum(rate(responses_total{response_status="failure"}[5m]))
  ```

#### 6.3.3 에러 태그 추가 (우선순위 3, 4) ⏳ 대기

**개선:**
```csharp
// 실패 응답 시 에러 태그 추가
if (response.IsFail && response is IFinResponseWithError errorResponse)
{
    Error error = errorResponse.Error;
    TagList responseTags = new TagList(baseTags) {
        { ResponseStatus, "failure" },
        { ErrorType, error.IsExceptional ? "exceptional" : "expected" },
        { ErrorCode, GetErrorCode(error) }
    };
    responseCounter.Add(1, responseTags);
}
```

**효과:**
```promql
# 에러 유형별 SLO 가능
sum(rate(responses_total{response_status="failure", error_type="expected"}[5m]))
  / sum(rate(requests_total[5m])) < 0.05  # 비즈니스 에러 5% 이하

sum(rate(responses_total{response_status="failure", error_type="exceptional"}[5m]))
  / sum(rate(requests_total[5m])) < 0.001  # 시스템 에러 0.1% 이하
```

#### 6.3.4 Meter 캐싱 (우선순위 5) ⏳ 대기

**현재:**
```csharp
public async ValueTask<TResponse> Handle(...)
{
    using Meter meter = _meterFactory.Create(_meterName);  // 매 요청 생성
    Counter<long> requestCounter = meter.CreateCounter<long>(...);
    // ...
}
```

**개선:**
```csharp
public sealed class UsecaseMetricsPipeline<TRequest, TResponse>
{
    private readonly Meter _meter;
    private readonly Counter<long> _requestCounter;
    private readonly Counter<long> _responseCounter;
    private readonly Histogram<double> _durationHistogram;

    public UsecaseMetricsPipeline(IMeterFactory meterFactory, ...)
    {
        _meter = meterFactory.Create(_meterName);
        _requestCounter = _meter.CreateCounter<long>(...);
        _responseCounter = _meter.CreateCounter<long>(...);
        _durationHistogram = _meter.CreateHistogram<double>(...);
    }

    public async ValueTask<TResponse> Handle(...)
    {
        _requestCounter.Add(1, tags);  // 캐시된 인스턴스 사용
        // ...
    }
}
```

---

## 7. 참고 자료

### OpenTelemetry

- [Semantic Conventions](https://opentelemetry.io/docs/concepts/semantic-conventions/)
- [Metrics Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/general/metrics/)
- [Naming Guidelines](https://opentelemetry.io/docs/specs/semconv/general/naming/)
- [How to Name Your Metrics (2025)](https://opentelemetry.io/blog/2025/how-to-name-your-metrics/)

### 프로젝트 내 문서

- `.claude/guides/observability-naming-guide.md` - 명명 규칙 가이드
- `TODO.md` - 개선 항목 추적

---

## 부록: 종합 평가 요약

| 영역 | 현재 상태 | 평가 |
|------|----------|------|
| **OTel 표준 준수** | ✅ 대부분 준수 | 단위, 네이밍, 계층 구조 양호 |
| **장애 분석** | ✅ 충분 | 에러 감지, 원인 분석 가능 |
| **성능 분석** | ✅ 충분 | Histogram으로 지연 분포 분석 |
| **용량 분석** | ✅ 충분 | TPS, 트래픽 분포 분석 |
| **SLI/SLO 기본** | ✅ 지원 | 가용성, 지연, 에러율 계산 가능 |
| **SLI/SLO 고급** | ⚠️ 부족 | 에러 유형별, 고객별 SLO 불가 |
| **코드 품질** | ⚠️ 개선 중 | ~~태그 불일치~~ ✅, Meter 재생성 ⏳ |

**종합:** 기본적인 운영은 가능하며, 태그 구조 통일 완료. 에러 유형 태그 및 Meter 캐싱 개선 권장

---

## 부록: 변경 이력

| 날짜 | 변경 내용 | 커밋 |
|------|----------|------|
| 2026-01-04 | 문서 초안 작성 | - |
| 2026-01-04 | 태그 구조 통일 완료 | `e355af2` |
| 2026-01-04 | 응답 메트릭 통합 완료 (6.3.2) | - |
