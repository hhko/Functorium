# Usecase Observability Pipeline 리뷰

> **작성일**: 2026-01-04
> **최종 수정**: 2026-01-04
> **대상**: Usecase Pipeline (Metrics, Tracing, Logging)
> **참조**: OpenTelemetry Semantic Conventions
> **진척률**: 4/7 완료 (57%)

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

| Tag Key | requestCounter | responseCounter (성공) | responseCounter (실패) |
|---------|----------------|----------------------|----------------------|
| `request.layer` | `"application"` | `"application"` | `"application"` |
| `request.category` | `"usecase"` | `"usecase"` | `"usecase"` |
| `request.handler.cqrs` | `"command"` / `"query"` | `"command"` / `"query"` | `"command"` / `"query"` |
| `request.handler` | handler name | handler name | handler name |
| `request.handler.method` | `"Handle"` | `"Handle"` | `"Handle"` |
| `response.status` | - | `"success"` | `"failure"` |
| `error.type` | - | - | `"expected"` / `"exceptional"` / `"aggregate"` |
| `error.code` | - | - | 대표 에러 코드 |
| **Total Tags** | **5** | **6** | **8** |

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
| "비즈니스/시스템 에러?" | ✅ | Metrics: `error.type` 태그, Logging: Warning vs Error 레벨 |
| "에러율은?" | ✅ | Metrics: `failure / total` |
| "어떤 에러가 가장 많이?" | ✅ | Metrics: `error.code` 태그 |

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
| 에러 코드별 분포 | `sum by(error_code)` | ✅ |
| 에러 유형별 분포 | `sum by(error_type)` | ✅ |

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

### 5.6 SLI/SLO 관점 지원 현황

| 항목 | 상태 | 비고 |
|------|------|------|
| 에러 유형별 SLO | ✅ 지원 | `error.type` 태그로 비즈니스/시스템 에러 구분 |
| 에러 코드별 SLO | ✅ 지원 | `error.code` 태그로 특정 에러 추적 |
| 사용자별 SLO | ❌ 미지원 | `user.id` 태그 없음 |
| 테넌트별 SLO | ❌ 미지원 | `tenant.id` 태그 없음 |

---

## 6. 종합 개선 계획

### 6.1 진척률 요약

```
진행 상황: ███████████░░░░░░░░░ 57% (4/7 완료)

✅ 완료: 4개
⏳ 대기: 2개
➖ 취소: 1개 (방향 변경)
```

### 6.2 우선순위별 개선 항목

| 우선순위 | 항목 | 문제점 | 개선 방안 | 난이도 | 상태 |
|----------|------|--------|----------|--------|------|
| **1 (높음)** | 태그 구조 통일 | requests와 responses 태그 불일치 | 기본 태그 세트 통일 | 낮음 | ✅ 완료 |
| **2 (중)** | 응답 메트릭 통합 | success/failure 별도 Counter | 단일 Counter + status 태그 | 낮음 | ✅ 완료 |
| **3 (중)** | `error.type` 태그 | 에러 유형 구분 불가 | Expected/Exceptional 태그 추가 | 낮음 | ✅ 완료 |
| **4 (중)** | `error.code` 태그 | 에러 패턴 분석 불가 | 에러 코드 태그 추가 | 낮음 | ✅ 완료 |
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

#### 6.3.3 에러 태그 추가 (우선순위 3, 4) ✅ 완료

**변경 내용:**
- 커밋: (2026-01-04)
- 실패 응답에 `error.type`과 `error.code` 태그 추가
- ObservabilityNaming에 `ErrorTypes` 상수 클래스 추가
- ManyErrors 처리를 위한 대표 에러 선정 로직 구현

**태그 구조 변화:**

| 상태 | 태그 수 | 추가된 태그 |
|------|--------|------------|
| 성공 | 6개 | 없음 |
| 실패 | 8개 | `error.type`, `error.code` |

**에러 타입 매핑:**

| 에러 클래스 | `error.type` 값 | `error.code` 값 |
|------------|-----------------|-----------------|
| `ErrorCodeExpected` | `"expected"` | `ErrorCode` 속성 |
| `ErrorCodeExceptional` | `"exceptional"` | `ErrorCode` 속성 |
| `ManyErrors` | `"aggregate"` | 대표 에러 코드 (Exceptional 우선) |
| 기타 `Error` | `IsExceptional`에 따라 결정 | 타입 이름 |

**구현 코드:**
```csharp
// ObservabilityNaming.cs - 에러 타입 상수 추가
public static class ErrorTypes
{
    public const string Expected = "expected";
    public const string Exceptional = "exceptional";
    public const string Aggregate = "aggregate";
}

// UsecaseMetricsPipeline.cs - 응답 태그 생성
private static TagList CreateResponseTags(TagList requestTags, TResponse response)
{
    TagList tags = new();
    foreach (var tag in requestTags)
        tags.Add(tag);

    if (response.IsSucc)
    {
        tags.Add(ResponseStatus, Status.Success);
        return tags;
    }

    tags.Add(ResponseStatus, Status.Failure);

    if (response is IFinResponseWithError { Error: var error })
    {
        var (errorType, errorCode) = GetErrorInfo(error);
        tags.Add(OTelAttributes.ErrorType, errorType);
        tags.Add(CustomAttributes.ErrorCode, errorCode);
    }

    return tags;
}

// 대표 에러 선정 (Exceptional 우선)
private static string GetPrimaryErrorCode(ManyErrors many)
{
    foreach (var e in many.Errors)
    {
        if (e.IsExceptional)
            return GetErrorCode(e);
    }
    return many.Errors.Head.Match(
        Some: GetErrorCode,
        None: () => nameof(ManyErrors));
}
```

**시뮬레이션 - 100건 요청 처리 결과:**

| 결과 유형 | 건수 | error.type | error.code 예시 |
|----------|------|------------|-----------------|
| 성공 | 85 | - | - |
| 비즈니스 에러 | 12 | `expected` | `DomainErrors.City.Empty` |
| 시스템 에러 | 3 | `exceptional` | `InfraErrors.Database.Timeout` |

**Prometheus 메트릭 출력 예시:**
```prometheus
# 성공 응답 (태그 6개)
responses_total{
  request_handler="CreateUserCommand",
  response_status="success"
} 85

# 비즈니스 에러 (태그 8개)
responses_total{
  request_handler="CreateUserCommand",
  response_status="failure",
  error_type="expected",
  error_code="DomainErrors.City.Empty"
} 7

# 시스템 에러 (태그 8개)
responses_total{
  request_handler="CreateUserCommand",
  response_status="failure",
  error_type="exceptional",
  error_code="InfraErrors.Database.Timeout"
} 3
```

**가치 분석:**

| 가치 | 현재 | 개선 후 |
|------|------|--------|
| **SLO 정밀화** | 모든 에러 동일 취급 | 비즈니스/시스템 에러 별도 목표 |
| **장애 분석** | Tracing/Logging 조회 필요 | Metrics에서 즉시 `error_code` 확인 |
| **트렌드 분석** | 불가능 | 특정 에러 코드 시계열 추이 |
| **알림 정확도** | 모든 에러 동일 알림 | 시스템 에러만 긴급 알림 |

**에러 유형별 SLO 쿼리:**
```promql
# 비즈니스 에러율 (허용 가능한 수준)
sum(rate(responses_total{error_type="expected"}[5m]))
  / sum(rate(requests_total[5m])) < 0.05  # 5% 이하

# 시스템 에러율 (심각한 문제)
sum(rate(responses_total{error_type="exceptional"}[5m]))
  / sum(rate(requests_total[5m])) < 0.001  # 0.1% 이하
```

**에러 코드 Top-N 분석:**
```promql
# 가장 많이 발생하는 에러 코드 Top 5
topk(5, sum by(error_code)(rate(responses_total{response_status="failure"}[1h])))
```

**Grafana 대시보드 개선:**
```
┌─────────────────────────────────────┐
│ 현재:                               │
│ Error Rate: 15%                     │
│ (모든 에러 합산, 세부 분류 불가)     │
├─────────────────────────────────────┤
│ 개선 후:                            │
│ Business Errors: 12% (정상 범위)    │
│ System Errors: 3%   (주의!)         │
│                                     │
│ Top Errors:                         │
│ 1. City.Empty         45%  ████▌    │
│ 2. Email.Invalid      30%  ███      │
│ 3. DB.Timeout         15%  █▌       │
└─────────────────────────────────────┘
```

**카디널리티 주의사항:**

| 태그 | 카디널리티 | 권장사항 |
|------|-----------|---------|
| `error.type` | **낮음** (2개) | `expected`, `exceptional` |
| `error.code` | **중간~높음** | 에러 코드 수에 비례, 상위 카테고리 권장 |

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

## 8. 에러 관찰 가능성 재설계 (Clean-Slate Design)

> **목적**: 하위 호환성을 무시하고 Metrics, Tracing, Logging의 에러 처리를 통합 설계
> **범위**: 에러 분류 체계, 태그 네이밍, ManyErrors 처리, 3-Pillar 일관성

### 8.1 현재 구조 분석

#### 8.1.1 에러 타입 계층

```
LanguageExt.Common.Error (추상)
├── ErrorCodeExpected          : 비즈니스 에러 (IsExpected = true)
│   ├── ErrorCode              : 에러 코드 (예: "DomainErrors.City.Empty")
│   ├── ErrorCurrentValue      : 현재 값
│   └── Message                : 메시지
│
├── ErrorCodeExceptional       : 시스템 에러 (IsExceptional = true)
│   ├── ErrorCode              : 에러 코드 (예: "InfraErrors.Database.Timeout")
│   └── Exception              : 원본 예외
│
└── ManyErrors                 : 복합 에러 (Validation 등)
    └── Errors                 : Seq<Error>
```

#### 8.1.2 현재 3-Pillar 에러 처리 비교

| 항목 | Metrics | Tracing | Logging |
|------|---------|---------|---------|
| **에러 타입 구분** | ❌ 없음 | ✅ `error.type` | ✅ Log Level |
| **에러 코드** | ❌ 없음 | ✅ `error.code` | ✅ `Error.Code` |
| **에러 메시지** | ❌ 없음 | ✅ `error.message` | ✅ `Error.Message` |
| **ManyErrors 처리** | ❌ 없음 | ⚠️ count만 | ✅ 전체 구조화 |
| **네이밍 일관성** | - | snake_case | PascalCase |

**현재 Tracing ManyErrors 처리의 문제:**
```csharp
// UsecaseTracingPipeline.cs:154-158
private static void SetManyErrorsTags(Activity activity, ManyErrors error)
{
    activity.SetTag("error.type", nameof(ManyErrors));   // "ManyErrors"
    activity.SetTag("error.count", error.Errors.Count);  // 개수만
    // 하위 에러 정보 누락!
}
```

### 8.2 재설계 원칙

#### 8.2.1 통합 에러 분류 체계

**에러 분류 기준:**

| 분류 | 코드 값 | 설명 | 예시 |
|------|--------|------|------|
| `expected` | IsExpected = true | 예상된 비즈니스 에러 | 유효성 검증 실패, 권한 부족 |
| `exceptional` | IsExceptional = true | 예외적 시스템 에러 | DB 타임아웃, 네트워크 오류 |
| `aggregate` | ManyErrors | 복합 에러 (하위 에러 포함) | Apply validation |

**에러 코드 네이밍 규칙:**
```
{Layer}Errors.{Domain}.{Specific}

예시:
- DomainErrors.City.Empty
- DomainErrors.Email.Invalid
- DomainErrors.DateRange.StartAfterEnd
- InfraErrors.Database.ConnectionTimeout
- InfraErrors.External.PaymentServiceUnavailable
```

#### 8.2.2 통합 태그 네이밍

**OTel 표준 + 커스텀 확장:**

| 태그 키 | 범위 | 설명 | 예시 값 |
|---------|------|------|---------|
| `error.type` | OTel 표준 | 에러 분류 | `expected`, `exceptional`, `aggregate` |
| `error.code` | 커스텀 | 에러 코드 | `DomainErrors.City.Empty` |
| `error.message` | 커스텀 | 에러 메시지 | `City cannot be empty` |
| `error.count` | 커스텀 | 하위 에러 개수 (aggregate용) | `3` |
| `error.codes` | 커스텀 | 하위 에러 코드 목록 (aggregate용) | `City.Empty,Email.Invalid` |

### 8.3 ManyErrors 처리 전략

#### 8.3.1 설계 옵션

| 옵션 | 설명 | 장점 | 단점 |
|------|------|------|------|
| **A. Primary Error** | 첫 번째 또는 가장 심각한 에러만 | 카디널리티 낮음 | 정보 손실 |
| **B. Aggregate + Codes** | aggregate 타입 + 코드 목록 | 완전한 정보 | 카디널리티 증가 |
| **C. Flatten** | 각 에러별 개별 기록 | 정확한 집계 | 중복 메트릭 |

**권장: 옵션 B (Aggregate + Codes)**

```
ManyErrors { City.Empty, Email.Invalid, Phone.TooShort }

→ error.type  = "aggregate"
→ error.count = 3
→ error.codes = "City.Empty,Email.Invalid,Phone.TooShort"
→ error.code  = "City.Empty"  (대표 에러)
```

#### 8.3.2 대표 에러 (Primary Error) 선정 규칙

ManyErrors에서 `error.code`에 사용할 대표 에러는 **심각도 우선** 원칙으로 선정합니다.

**선정 우선순위:**

| 우선순위 | 조건 | 이유 |
|---------|------|------|
| **1순위** | `IsExceptional = true`인 첫 번째 에러 | 시스템 에러가 비즈니스 에러보다 심각 |
| **2순위** | 첫 번째 에러 (`Errors.First()`) | Fallback |

**예시 1: 모두 Expected인 경우**
```
ManyErrors {
  [0] City.Empty (expected)       ← 대표 에러 (첫 번째)
  [1] Email.Invalid (expected)
  [2] Phone.TooShort (expected)
}

→ error.code = "City.Empty"
```

**예시 2: Exceptional이 섞인 경우**
```
ManyErrors {
  [0] City.Empty (expected)
  [1] Database.Timeout (exceptional)  ← 대표 에러 (exceptional 우선)
  [2] Email.Invalid (expected)
}

→ error.code = "Database.Timeout"
```

**설계 근거:**
- 시스템 에러(exceptional)는 즉각적인 대응이 필요한 장애 상황
- 비즈니스 에러(expected)에 묻혀 알림이 누락되면 안 됨
- Metrics에서 `error_type="exceptional"` 필터링으로 시스템 장애 빠르게 감지 가능

#### 8.3.3 Metrics에서 ManyErrors 처리

**옵션 B 구현:**
```csharp
private static (string ErrorType, string ErrorCode, string? ErrorCodes, int? ErrorCount)
    GetErrorInfo(Error error)
{
    return error switch
    {
        ManyErrors many => (
            ErrorType: "aggregate",
            ErrorCode: GetPrimaryErrorCode(many),           // 대표 에러
            ErrorCodes: string.Join(",", many.Errors.Map(GetErrorCode)),
            ErrorCount: many.Errors.Count
        ),
        ErrorCodeExpected expected => (
            ErrorType: "expected",
            ErrorCode: expected.ErrorCode,
            ErrorCodes: null,
            ErrorCount: null
        ),
        ErrorCodeExceptional exceptional => (
            ErrorType: "exceptional",
            ErrorCode: exceptional.ErrorCode,
            ErrorCodes: null,
            ErrorCount: null
        ),
        _ => (
            ErrorType: error.IsExceptional ? "exceptional" : "expected",
            ErrorCode: error.GetType().Name,
            ErrorCodes: null,
            ErrorCount: null
        )
    };
}

private static string GetPrimaryErrorCode(ManyErrors many)
{
    // 우선순위: Exceptional > Expected
    var exceptional = many.Errors.FirstOrDefault(e => e.IsExceptional);
    if (exceptional != null) return GetErrorCode(exceptional);

    return GetErrorCode(many.Errors.First());
}
```

#### 8.3.3 Tracing에서 ManyErrors 처리 (재설계)

**현재:**
```csharp
private static void SetManyErrorsTags(Activity activity, ManyErrors error)
{
    activity.SetTag("error.type", nameof(ManyErrors));
    activity.SetTag("error.count", error.Errors.Count);
}
```

**재설계:**
```csharp
private static void SetManyErrorsTags(Activity activity, ManyErrors error)
{
    // 분류: aggregate
    activity.SetTag(ObservabilityNaming.OTelAttributes.ErrorType, "aggregate");

    // 개수
    activity.SetTag(ObservabilityNaming.CustomAttributes.ErrorCount, error.Errors.Count);

    // 대표 에러 코드
    string primaryCode = GetPrimaryErrorCode(error);
    activity.SetTag(ObservabilityNaming.CustomAttributes.ErrorCode, primaryCode);

    // 모든 에러 코드 (최대 10개)
    string errorCodes = string.Join(",", error.Errors
        .Take(10)
        .Map(GetErrorCode));
    activity.SetTag(ObservabilityNaming.CustomAttributes.ErrorCodes, errorCodes);

    // 하위 에러 상세 (Span Events로 기록)
    foreach (var (childError, index) in error.Errors.Take(10).Select((e, i) => (e, i)))
    {
        var tags = new ActivityTagsCollection
        {
            { "error.index", index },
            { "error.type", childError.IsExceptional ? "exceptional" : "expected" },
            { "error.code", GetErrorCode(childError) },
            { "error.message", childError.Message }
        };
        activity.AddEvent(new ActivityEvent("error.child", tags: tags));
    }
}
```

### 8.4 통합 태그 구조

#### 8.4.1 성공 응답 (Metrics & Tracing)

```yaml
# 공통 태그 (6개)
request.layer: "application"
request.category: "usecase"
request.handler.cqrs: "command"
request.handler: "CreateUserCommand"
request.handler.method: "Handle"
response.status: "success"
```

#### 8.4.2 단일 에러 응답

**Expected Error:**
```yaml
# Metrics (8개 태그)
request.layer: "application"
request.category: "usecase"
request.handler.cqrs: "command"
request.handler: "CreateUserCommand"
request.handler.method: "Handle"
response.status: "failure"
error.type: "expected"
error.code: "DomainErrors.City.Empty"

# Tracing (동일 + 추가 정보)
error.message: "City cannot be empty"
response.elapsed: 12.5
```

**Exceptional Error:**
```yaml
# Metrics (8개 태그)
request.layer: "application"
request.category: "usecase"
request.handler.cqrs: "command"
request.handler: "CreateUserCommand"
request.handler.method: "Handle"
response.status: "failure"
error.type: "exceptional"
error.code: "InfraErrors.Database.Timeout"

# Tracing (동일 + 추가 정보)
error.message: "Connection timed out after 30000ms"
exception.type: "System.TimeoutException"
exception.stacktrace: "..."
```

#### 8.4.3 복합 에러 응답 (ManyErrors)

```yaml
# Metrics (10개 태그)
request.layer: "application"
request.category: "usecase"
request.handler.cqrs: "command"
request.handler: "CreateUserCommand"
request.handler.method: "Handle"
response.status: "failure"
error.type: "aggregate"
error.code: "DomainErrors.City.Empty"  # 대표 에러
error.codes: "City.Empty,Email.Invalid,Phone.TooShort"
error.count: 3

# Tracing (동일 + Span Events)
Events:
  - error.child { index: 0, type: expected, code: City.Empty, message: "..." }
  - error.child { index: 1, type: expected, code: Email.Invalid, message: "..." }
  - error.child { index: 2, type: expected, code: Phone.TooShort, message: "..." }
```

### 8.5 PromQL 쿼리 시나리오

#### 8.5.1 에러 유형별 분석

```promql
# 전체 에러율
sum(rate(responses_total{response_status="failure"}[5m]))
  / sum(rate(requests_total[5m]))

# 비즈니스 에러율 (expected)
sum(rate(responses_total{error_type="expected"}[5m]))
  / sum(rate(requests_total[5m]))

# 시스템 에러율 (exceptional)
sum(rate(responses_total{error_type="exceptional"}[5m]))
  / sum(rate(requests_total[5m]))

# 복합 에러율 (aggregate/validation)
sum(rate(responses_total{error_type="aggregate"}[5m]))
  / sum(rate(requests_total[5m]))
```

#### 8.5.2 에러 코드 Top-N

```promql
# 가장 빈번한 에러 코드 Top 10
topk(10, sum by(error_code)(rate(responses_total{response_status="failure"}[1h])))

# 핸들러별 가장 빈번한 에러
topk(5, sum by(request_handler, error_code)(
  rate(responses_total{response_status="failure"}[1h])
))
```

#### 8.5.3 SLO 쿼리

```promql
# SLO: 시스템 에러 0.1% 이하
(1 - sum(rate(responses_total{error_type="exceptional"}[30d]))
     / sum(rate(requests_total[30d]))) * 100
# 결과: 99.9% (SLO 충족)

# SLO: 비즈니스 에러 5% 이하
(1 - sum(rate(responses_total{error_type="expected"}[30d]))
     / sum(rate(requests_total[30d]))) * 100
```

### 8.6 카디널리티 관리

#### 8.6.1 태그별 카디널리티 분석

| 태그 | 카디널리티 | 권장 제한 | 비고 |
|------|-----------|----------|------|
| `error.type` | **3** | - | `expected`, `exceptional`, `aggregate` |
| `error.code` | **중간** | ≤50 | 도메인별 에러 코드 수 제한 |
| `error.codes` | **높음** | ≤100 조합 | aggregate에서만 사용 |
| `error.count` | **낮음** | ≤10 | 최대 에러 개수 제한 |

#### 8.6.2 카디널리티 폭발 방지

```csharp
// 에러 코드 정규화 (상세 → 상위 카테고리)
private static string NormalizeErrorCode(string errorCode)
{
    // "DomainErrors.City.Empty" → "DomainErrors.City"
    // "DomainErrors.City.TooLong" → "DomainErrors.City"
    var parts = errorCode.Split('.');
    return parts.Length >= 3
        ? string.Join(".", parts.Take(parts.Length - 1))
        : errorCode;
}

// ManyErrors의 error.codes는 최대 5개로 제한
private static string GetErrorCodes(ManyErrors many, int maxCount = 5)
{
    return string.Join(",", many.Errors
        .Take(maxCount)
        .Map(e => NormalizeErrorCode(GetErrorCode(e)))
        .Distinct());
}
```

### 8.7 3-Pillar 일관성 상세 설계

#### 8.7.1 현재 3-Pillar 구조 비교

**현재 Metrics (UsecaseMetricsPipeline.cs):**
```csharp
// 에러 정보 없음 - response.status만 있음
string responseStatus = response.IsSucc ? "success" : "failure";
TagList responseTags = new TagList {
    { "response.status", responseStatus }
    // error.type, error.code 없음!
};
```

**현재 Tracing (UsecaseTracingPipeline.cs:118-164):**
```csharp
// 에러 타입별 분기 처리
switch (error) {
    case ErrorCodeExpected:
        activity.SetTag("error.type", "ErrorCodeExpected");  // 클래스명
        activity.SetTag("error.code", error.ErrorCode);
        activity.SetTag("error.message", error.Message);
        break;
    case ManyErrors:
        activity.SetTag("error.type", "ManyErrors");         // 클래스명
        activity.SetTag("error.count", error.Count);
        // error.code 없음!
        break;
}
```

**현재 Logging (UsecaseLoggingPipeline.cs:70-105):**
```csharp
// Log Level로 에러 분류
if (error.IsExceptional) {
    logger.LogError(..., error);      // Error 레벨
} else {
    logger.LogWarning(..., error);    // Warning 레벨
}
// error.type 명시적 필드 없음 (Log Level로 대체)
```

#### 8.7.2 문제점 분석

| 문제 | Metrics | Tracing | Logging |
|------|---------|---------|---------|
| **에러 분류 값** | ❌ 없음 | `ErrorCodeExpected` (클래스명) | Log Level |
| **값 일관성** | - | PascalCase | N/A |
| **ManyErrors 코드** | ❌ 없음 | ❌ 없음 | ✅ 구조화 객체 |
| **aggregate 분류** | ❌ 없음 | ❌ 없음 (ManyErrors) | ❌ 없음 |
| **네이밍 컨벤션** | snake_case | snake_case | PascalCase |

**핵심 문제:**
1. **에러 분류 값 불일치**: Tracing은 클래스명(`ErrorCodeExpected`), Logging은 Log Level
2. **Metrics 에러 정보 부재**: 에러 분류/코드 태그 없음
3. **ManyErrors 처리 불완전**: Tracing에서 하위 에러 코드 누락
4. **네이밍 컨벤션 혼재**: Logging만 PascalCase

#### 8.7.3 통합 재설계 원칙

**원칙 1: 통합 에러 분류 값**
```
expected     ← IsExpected = true (비즈니스 에러)
exceptional  ← IsExceptional = true (시스템 에러)
aggregate    ← ManyErrors (복합 에러)
```

**원칙 2: 동일한 필드 구조**
```
모든 Pillar에서 동일한 필드명/태그키 사용:
- error.type (또는 ErrorType)
- error.code (또는 ErrorCode)
- error.count (또는 ErrorCount)
- error.codes (또는 ErrorCodes)
```

**원칙 3: 정보 밀도 계층화**
```
Metrics  : 집계용 (type, code만 - 카디널리티 제어)
Tracing  : 추적용 (type, code, codes, count + Span Events)
Logging  : 디버깅용 (전체 Error 객체 구조화)
```

#### 8.7.4 재설계된 3-Pillar 구조

##### A. Metrics (집계 최적화)

```csharp
// UsecaseMetricsPipeline.cs - 재설계
private static TagList CreateResponseTags(
    TagList baseTags,
    TResponse response)
{
    TagList tags = new(baseTags);

    if (response.IsSucc)
    {
        tags.Add("response.status", "success");
        return tags;
    }

    tags.Add("response.status", "failure");

    if (response is IFinResponseWithError { Error: var error })
    {
        var errorInfo = GetErrorInfo(error);
        tags.Add("error.type", errorInfo.Type);
        tags.Add("error.code", errorInfo.Code);

        // aggregate인 경우만 추가 태그
        if (errorInfo.Type == "aggregate")
        {
            tags.Add("error.count", errorInfo.Count);
            // error.codes는 카디널리티 문제로 Metrics에서 제외
        }
    }

    return tags;
}

private static (string Type, string Code, int? Count) GetErrorInfo(Error error)
{
    return error switch
    {
        ManyErrors many => (
            Type: "aggregate",
            Code: GetPrimaryErrorCode(many),
            Count: many.Errors.Count
        ),
        ErrorCodeExpected expected => (
            Type: "expected",
            Code: expected.ErrorCode,
            Count: null
        ),
        ErrorCodeExceptional exceptional => (
            Type: "exceptional",
            Code: exceptional.ErrorCode,
            Count: null
        ),
        _ => (
            Type: error.IsExceptional ? "exceptional" : "expected",
            Code: error.GetType().Name,
            Count: null
        )
    };
}
```

**Prometheus 출력:**
```prometheus
# 성공 (6개 태그)
responses_total{response_status="success"} 85

# 단일 에러 (8개 태그)
responses_total{response_status="failure",error_type="expected",error_code="City.Empty"} 10

# 복합 에러 (9개 태그)
responses_total{response_status="failure",error_type="aggregate",error_code="City.Empty",error_count="3"} 5
```

##### B. Tracing (상세 추적)

```csharp
// UsecaseTracingPipeline.cs - 재설계
private static void SetErrorTags(Activity activity, Error error)
{
    switch (error)
    {
        case ManyErrors many:
            SetAggregateErrorTags(activity, many);
            break;
        case ErrorCodeExpected expected:
            SetExpectedErrorTags(activity, expected);
            break;
        case ErrorCodeExceptional exceptional:
            SetExceptionalErrorTags(activity, exceptional);
            break;
        default:
            SetUnknownErrorTags(activity, error);
            break;
    }
}

private static void SetExpectedErrorTags(Activity activity, ErrorCodeExpected error)
{
    activity.SetTag("error.type", "expected");           // 통합 값
    activity.SetTag("error.code", error.ErrorCode);
    activity.SetTag("error.message", error.Message);
}

private static void SetExceptionalErrorTags(Activity activity, ErrorCodeExceptional error)
{
    activity.SetTag("error.type", "exceptional");        // 통합 값
    activity.SetTag("error.code", error.ErrorCode);
    activity.SetTag("error.message", error.Message);

    // 예외 상세 (OTel 표준)
    if (error.ToException() is { } ex)
    {
        activity.SetTag("exception.type", ex.GetType().FullName);
        activity.SetTag("exception.message", ex.Message);
        activity.SetTag("exception.stacktrace", ex.StackTrace);
    }
}

private static void SetAggregateErrorTags(Activity activity, ManyErrors error)
{
    activity.SetTag("error.type", "aggregate");          // 통합 값
    activity.SetTag("error.count", error.Errors.Count);

    // 대표 에러 코드
    string primaryCode = GetPrimaryErrorCode(error);
    activity.SetTag("error.code", primaryCode);

    // 모든 에러 코드 (최대 10개)
    string errorCodes = string.Join(",", error.Errors
        .Take(10)
        .Map(GetErrorCode));
    activity.SetTag("error.codes", errorCodes);

    // 하위 에러 상세를 Span Events로 기록
    foreach (var (child, idx) in error.Errors.Take(10).Select((e, i) => (e, i)))
    {
        activity.AddEvent(new ActivityEvent("error.child", tags: new ActivityTagsCollection
        {
            { "error.index", idx },
            { "error.type", child.IsExceptional ? "exceptional" : "expected" },
            { "error.code", GetErrorCode(child) },
            { "error.message", child.Message }
        }));
    }
}
```

**Jaeger UI 출력:**
```yaml
Span: application usecase.command CreateUserCommand.Handle
Status: Error
Tags:
  response.status: "failure"
  error.type: "aggregate"
  error.code: "City.Empty"
  error.codes: "City.Empty,Email.Invalid,Phone.TooShort"
  error.count: 3
Events:
  - name: "error.child"
    tags: { index: 0, type: "expected", code: "City.Empty", message: "..." }
  - name: "error.child"
    tags: { index: 1, type: "expected", code: "Email.Invalid", message: "..." }
  - name: "error.child"
    tags: { index: 2, type: "expected", code: "Phone.TooShort", message: "..." }
```

##### C. Logging (디버깅 상세)

```csharp
// UsecaseLoggingPipeline.cs - 재설계
private void LogResponse(TResponse response, ...)
{
    if (response.IsSucc)
    {
        LogSuccess(...);
        return;
    }

    if (response is not IFinResponseWithError { Error: var error })
        return;

    // 에러 분류에 따른 로깅
    var errorInfo = ClassifyError(error);

    using var scope = logger.BeginScope(new Dictionary<string, object?>
    {
        ["ErrorType"] = errorInfo.Type,        // "expected", "exceptional", "aggregate"
        ["ErrorCode"] = errorInfo.Code,
        ["ErrorCount"] = errorInfo.Count,
        ["ErrorCodes"] = errorInfo.Codes,
        ["Error"] = error                      // 전체 구조화 객체
    });

    // Log Level 결정
    LogLevel level = DetermineLogLevel(error);
    EventId eventId = DetermineEventId(level);

    logger.Log(
        level,
        eventId,
        "{Layer} {Category}.{Cqrs} {Handler}.{Method} responded {Status} " +
        "in {Elapsed:0.0000} ms with {ErrorType} error: {ErrorCode}",
        layer, category, cqrs, handler, method, "failure", elapsed,
        errorInfo.Type, errorInfo.Code);
}

private static LogLevel DetermineLogLevel(Error error)
{
    return error switch
    {
        ManyErrors many => many.Errors.Any(e => e.IsExceptional)
            ? LogLevel.Error    // 하위에 Exceptional 있으면 Error
            : LogLevel.Warning, // 모두 Expected면 Warning
        _ when error.IsExceptional => LogLevel.Error,
        _ => LogLevel.Warning
    };
}

private static ErrorInfo ClassifyError(Error error)
{
    return error switch
    {
        ManyErrors many => new ErrorInfo(
            Type: "aggregate",
            Code: GetPrimaryErrorCode(many),
            Count: many.Errors.Count,
            Codes: string.Join(",", many.Errors.Take(10).Map(GetErrorCode))
        ),
        ErrorCodeExpected expected => new ErrorInfo(
            Type: "expected",
            Code: expected.ErrorCode,
            Count: null,
            Codes: null
        ),
        ErrorCodeExceptional exceptional => new ErrorInfo(
            Type: "exceptional",
            Code: exceptional.ErrorCode,
            Count: null,
            Codes: null
        ),
        _ => new ErrorInfo(
            Type: error.IsExceptional ? "exceptional" : "expected",
            Code: error.GetType().Name,
            Count: null,
            Codes: null
        )
    };
}

private record ErrorInfo(string Type, string Code, int? Count, string? Codes);
```

**Seq/Loki 출력:**
```json
{
  "Timestamp": "2026-01-04T10:30:00Z",
  "Level": "Warning",
  "MessageTemplate": "{Layer} {Category}.{Cqrs} {Handler}.{Method} responded {Status} in {Elapsed:0.0000} ms with {ErrorType} error: {ErrorCode}",
  "Properties": {
    "Layer": "application",
    "Category": "usecase",
    "Cqrs": "command",
    "Handler": "CreateUserCommand",
    "Method": "Handle",
    "Status": "failure",
    "Elapsed": 12.5,
    "ErrorType": "aggregate",
    "ErrorCode": "City.Empty",
    "ErrorCount": 3,
    "ErrorCodes": "City.Empty,Email.Invalid,Phone.TooShort",
    "Error": {
      "Errors": [
        { "Code": "City.Empty", "Message": "City cannot be empty" },
        { "Code": "Email.Invalid", "Message": "Invalid email format" },
        { "Code": "Phone.TooShort", "Message": "Phone number too short" }
      ]
    }
  },
  "EventId": { "Id": 1003, "Name": "ApplicationResponseWarning" }
}
```

#### 8.7.5 통합 일관성 매트릭스 (재설계 후)

| 항목 | Metrics | Tracing | Logging | 비고 |
|------|---------|---------|---------|------|
| **에러 분류 필드** | `error.type` | `error.type` | `ErrorType` | snake vs Pascal |
| **값 (expected)** | `"expected"` | `"expected"` | `"expected"` | ✅ 일치 |
| **값 (exceptional)** | `"exceptional"` | `"exceptional"` | `"exceptional"` | ✅ 일치 |
| **값 (aggregate)** | `"aggregate"` | `"aggregate"` | `"aggregate"` | ✅ 일치 |
| **에러 코드 필드** | `error.code` | `error.code` | `ErrorCode` | snake vs Pascal |
| **에러 코드 값** | 대표 코드 | 대표 코드 | 대표 코드 | ✅ 일치 |
| **복합 에러 개수** | `error.count` | `error.count` | `ErrorCount` | snake vs Pascal |
| **복합 에러 코드** | ❌ (카디널리티) | `error.codes` | `ErrorCodes` | Metrics 제외 |
| **복합 에러 상세** | ❌ | Span Events | `Error` 객체 | Pillar별 최적화 |
| **에러 메시지** | ❌ | `error.message` | `Error.Message` | Metrics 제외 |
| **Log Level** | N/A | N/A | Warning/Error | Logging 전용 |

#### 8.7.6 Cross-Pillar 상관관계 쿼리

**시나리오: 특정 에러 코드에 대한 3-Pillar 분석**

```
1. Metrics → 얼마나 자주 발생하는가?
2. Tracing → 어떤 요청 흐름에서 발생하는가?
3. Logging → 정확히 무슨 데이터로 발생했는가?
```

**Grafana Explore 쿼리:**

```promql
# 1. Metrics: City.Empty 에러 발생률 (5분간)
sum(rate(responses_total{error_code="City.Empty"}[5m]))
```

```logql
# 2. Logging: City.Empty 에러 상세 로그
{app="functorium"} | json | ErrorCode="City.Empty"
```

```tempo
# 3. Tracing: City.Empty 에러 Span
{ error.code = "City.Empty" }
```

**상관관계 결과:**
```
┌─────────────────────────────────────────────────────────────────────┐
│ Cross-Pillar Analysis: error.code = "City.Empty"                    │
├─────────────────────────────────────────────────────────────────────┤
│ Metrics (Grafana)                                                   │
│   Rate: 2.5/min (↑ 150% from yesterday)                            │
│   Handler: CreateUserCommand (95%), UpdateUserCommand (5%)          │
├─────────────────────────────────────────────────────────────────────┤
│ Tracing (Tempo)                                                     │
│   TraceID: abc123...                                                │
│   Span: application usecase.command CreateUserCommand.Handle        │
│   Duration: 12ms                                                    │
│   Tags: error.type=expected, error.code=City.Empty                  │
├─────────────────────────────────────────────────────────────────────┤
│ Logging (Loki)                                                      │
│   Request: { Name: "John", City: "", Email: "john@example.com" }    │
│   Error: { Code: "City.Empty", Message: "City cannot be empty",     │
│            CurrentValue: "" }                                       │
└─────────────────────────────────────────────────────────────────────┘
```

#### 8.7.7 네이밍 컨벤션 통일 옵션

**옵션 A: 모두 snake_case (OTel 표준 준수)**

| Pillar | 태그/필드 키 | 예시 |
|--------|-------------|------|
| Metrics | `error.type` | `error.type="expected"` |
| Tracing | `error.type` | `error.type="expected"` |
| Logging | `error.type` | `"error.type": "expected"` |

**옵션 B: Pillar별 컨벤션 유지 (현재 방식)**

| Pillar | 태그/필드 키 | 예시 |
|--------|-------------|------|
| Metrics | `error.type` | snake_case (OTel 표준) |
| Tracing | `error.type` | snake_case (OTel 표준) |
| Logging | `ErrorType` | PascalCase (.NET 컨벤션) |

**권장: 옵션 B**
- Metrics/Tracing: OTel 표준 snake_case
- Logging: .NET 구조화 로깅 PascalCase
- **값은 모두 동일하게 유지** (`expected`, `exceptional`, `aggregate`)

#### 8.7.8 ObservabilityNaming 확장

```csharp
public static class ObservabilityNaming
{
    /// <summary>
    /// 에러 분류 값 (3-Pillar 공통)
    /// </summary>
    public static class ErrorTypes
    {
        public const string Expected = "expected";
        public const string Exceptional = "exceptional";
        public const string Aggregate = "aggregate";
    }

    public static class CustomAttributes
    {
        // 기존 태그들...

        // 에러 태그 (Metrics, Tracing 공통)
        public const string ErrorType = "error.type";
        public const string ErrorCode = "error.code";
        public const string ErrorMessage = "error.message";
        public const string ErrorCount = "error.count";
        public const string ErrorCodes = "error.codes";
    }

    public static class LogKeys
    {
        // 기존 키들...

        // 에러 키 (Logging 전용)
        public const string ErrorType = "ErrorType";
        public const string ErrorCode = "ErrorCode";
        public const string ErrorCount = "ErrorCount";
        public const string ErrorCodes = "ErrorCodes";
    }
}
```

### 8.8 구현 우선순위

| 순서 | 항목 | 영향 범위 | 복잡도 |
|------|------|----------|--------|
| **1** | ObservabilityNaming에 새 상수 추가 | 낮음 | 낮음 |
| **2** | UsecaseMetricsPipeline 에러 태그 추가 | 중간 | 낮음 |
| **3** | UsecaseTracingPipeline ManyErrors 재설계 | 중간 | 중간 |
| **4** | 테스트 코드 업데이트 | 중간 | 중간 |
| **5** | 문서 업데이트 | 낮음 | 낮음 |

### 8.9 마이그레이션 고려사항

> 이 섹션은 하위 호환성 무시 전제이지만, 참고용으로 기록

**Breaking Changes:**
1. `error.type` 값 변경: `ErrorCodeExpected` → `expected`
2. `error.type` 값 변경: `ErrorCodeExceptional` → `exceptional`
3. `error.type` 값 변경: `ManyErrors` → `aggregate`
4. 새 태그 추가: `error.codes` (ManyErrors용)

**Grafana 대시보드 쿼리 변경:**
```promql
# 이전 (Tracing 기반)
count(span{error_type="ErrorCodeExpected"})

# 이후 (Metrics 기반)
sum(rate(responses_total{error_type="expected"}[5m]))
```

---

## 부록: 종합 평가 요약

| 영역 | 현재 상태 | 평가 |
|------|----------|------|
| **OTel 표준 준수** | ✅ 대부분 준수 | 단위, 네이밍, 계층 구조 양호 |
| **장애 분석** | ✅ 충분 | 에러 감지, 원인 분석 가능 |
| **성능 분석** | ✅ 충분 | Histogram으로 지연 분포 분석 |
| **용량 분석** | ✅ 충분 | TPS, 트래픽 분포 분석 |
| **SLI/SLO 기본** | ✅ 지원 | 가용성, 지연, 에러율 계산 가능 |
| **SLI/SLO 고급** | ⚠️ 일부 지원 | ~~에러 유형별~~ ✅, 고객별 SLO 불가 |
| **코드 품질** | ⚠️ 개선 중 | ~~태그 불일치~~ ✅, ~~에러 태그~~ ✅, Meter 재생성 ⏳ |

**종합:** 기본적인 운영은 가능하며, 태그 구조 통일 및 에러 태그 추가 완료. Meter 캐싱 개선 권장

---

## 부록: 변경 이력

| 날짜 | 변경 내용 | 커밋 |
|------|----------|------|
| 2026-01-04 | 문서 초안 작성 | - |
| 2026-01-04 | 태그 구조 통일 완료 | `e355af2` |
| 2026-01-04 | 응답 메트릭 통합 완료 (6.3.2) | - |
| 2026-01-04 | 에러 태그 추가 완료 (6.3.3) - `error.type`, `error.code` | - |
