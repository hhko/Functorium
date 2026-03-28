# 대시보드 패턴 레퍼런스

## L1 스코어카드: 전체 건강 상태

### 6개 건강 지표

| 지표 | PromQL | 의미 | 임계값 (Green/Yellow/Red) |
|------|--------|------|--------------------------|
| 요청 수 | `sum(rate(application_usecase_command_requests_total[5m]))` | 초당 요청 처리량 | 정상 범위 내 / ±50% 변동 / ±80% 변동 |
| 성공률 | `sum(rate(responses{response_status="success"}[5m])) / sum(rate(responses[5m])) * 100` | 전체 성공 비율 | > 99.9% / > 99% / < 99% |
| P95 지연 | `histogram_quantile(0.95, sum(rate(duration_bucket[5m])) by (le))` | 95번째 백분위 응답 시간 | < 200ms / < 500ms / > 500ms |
| 에러율 | `sum(rate(responses{response_status="failure"}[5m])) / sum(rate(responses[5m])) * 100` | 전체 에러 비율 | < 0.1% / < 1% / > 1% |
| 가용성 | `1 - (sum(rate(responses{error_type="exceptional"}[5m])) / sum(rate(responses[5m])))` | 시스템 오류 제외 가용성 | > 99.9% / > 99.5% / < 99.5% |
| 처리량 | `sum(rate(responses{response_status="success"}[5m]))` | 초당 성공 처리량 | 기준선 대비 안정 / -20% / -50% |

### Grafana 패널 구성

```
L1 스코어카드 대시보드
├── Row 1: Stat 패널 × 6 (건강 지표)
├── Row 2: 시계열 그래프 (요청 수 + 에러율 overlay)
├── Row 3: 시계열 그래프 (P95/P99 지연)
└── Row 4: 테이블 (최근 에러 top 10 by error.code)
```

## L2 드릴다운: 핸들러별 상세

### 차원 분석

L2 대시보드는 `request.layer` × `request.category.name` × `request.handler.name` 차원으로 드릴다운합니다.

#### Application Layer 드릴다운

```promql
# 핸들러별 초당 요청 수
sum(rate(application_usecase_command_requests_total[5m])) by (request_handler_name)

# 핸들러별 P95 지연
histogram_quantile(0.95,
  sum(rate(application_usecase_command_duration_bucket[5m])) by (le, request_handler_name)
)

# 핸들러별 에러율
sum(rate(application_usecase_command_responses_total{response_status="failure"}[5m])) by (request_handler_name)
/ sum(rate(application_usecase_command_responses_total[5m])) by (request_handler_name) * 100

# error.type별 분포
sum(rate(application_usecase_command_responses_total{response_status="failure"}[5m])) by (error_type)
```

#### Adapter Layer 드릴다운

```promql
# Repository별 P95 지연
histogram_quantile(0.95,
  sum(rate(adapter_repository_duration_bucket[5m])) by (le, request_handler_name)
)

# External API별 에러율
sum(rate(adapter_external_api_responses_total{response_status="failure"}[5m])) by (request_handler_name)
/ sum(rate(adapter_external_api_responses_total[5m])) by (request_handler_name) * 100
```

### DomainEvent 시각화

```promql
# 이벤트 타입별 발행 수
sum(rate(adapter_event_requests_total[5m])) by (request_handler_name)

# 이벤트 핸들러별 처리 시간
histogram_quantile(0.95,
  sum(rate(application_usecase_event_duration_bucket[5m])) by (le, request_handler_name)
)

# 이벤트 핸들러 에러율
sum(rate(application_usecase_event_responses_total{response_status="failure"}[5m])) by (request_handler_name)
/ sum(rate(application_usecase_event_responses_total[5m])) by (request_handler_name) * 100
```

### Grafana 패널 구성

```
L2 드릴다운 대시보드
├── Variable: $layer (application/adapter), $category, $handler
├── Row 1: 선택된 핸들러 요청 수 + 에러율
├── Row 2: P50/P95/P99 지연 시계열
├── Row 3: error.type 분포 (expected vs exceptional)
├── Row 4: error.code top 10 테이블
└── Row 5: DomainEvent 발행 → Handler 체인 (event type → handler latency)
```

## ctx.* 세그먼트 대시보드

MetricsTag로 지정된 ctx.* 필드를 차원으로 활용하여 세그먼트 분석합니다.

```promql
# 고객 등급별 P95 지연 (ctx.customer_tier가 MetricsTag인 경우)
histogram_quantile(0.95,
  sum(rate(application_usecase_command_duration_bucket[5m])) by (le, ctx_customer_tier)
)

# Express 주문 vs 일반 주문 에러율 (ctx.is_express가 MetricsTag인 경우)
sum(rate(application_usecase_command_responses_total{response_status="failure"}[5m])) by (ctx_place_order_command_request_is_express)
/ sum(rate(application_usecase_command_responses_total[5m])) by (ctx_place_order_command_request_is_express) * 100
```

## 안티패턴

### 허영 지표 (Vanity Metrics)

| 안티패턴 | 문제점 | 대안 |
|---------|--------|------|
| 총 요청 수 누적값 | 항상 증가하므로 의미 없음 | `rate()` 함수로 초당 요청 수 사용 |
| 평균 응답 시간만 모니터링 | 이상치를 숨김 | P95/P99 백분위 함께 모니터링 |
| 전체 성공률만 표시 | 특정 핸들러 장애를 숨김 | 핸들러별 분해 대시보드 |

### 과다 지표 (Metric Overload)

| 안티패턴 | 문제점 | 대안 |
|---------|--------|------|
| 모든 ctx.* 필드를 MetricsTag로 지정 | 카디널리티 폭발 → 저장소 비용 급증 | Bounded 값만 MetricsTag, 나머지는 Logging + Tracing |
| `customer_id`를 MetricsTag로 사용 | Unbounded 시계열 → 쿼리 성능 저하 | `customer_tier` 등 Bounded 세그먼트 사용 |
| 초 단위 스크래핑 간격 | 불필요한 데이터 과적 | 15~30초 스크래핑 간격 권장 |

### 방치 대시보드 (Dashboard Rot)

| 안티패턴 | 문제점 | 대안 |
|---------|--------|------|
| 생성 후 미확인 대시보드 | 장애 시 활용 불가 | 주간 대시보드 리뷰 루틴 |
| 소유자 없는 알림 규칙 | 알림 피로 → 무시 | 각 알림에 담당자 명시 |
| 기준선 없는 임계값 | 오탐/미탐 빈발 | 2주간 기준선 측정 후 임계값 설정 |
