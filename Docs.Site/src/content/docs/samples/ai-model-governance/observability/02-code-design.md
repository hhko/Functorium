---
title: "Observability Code Design"
description: "L1 scorecard, L2 drill-down, alert rules, and ctx.* propagation code patterns"
---

## L1 스코어카드: 전체 건강 상태

6개 건강 지표로 AI 모델 거버넌스 플랫폼의 전체 상태를 한눈에 파악합니다.

| 지표 | PromQL | 임계값 (Green / Yellow / Red) |
|------|--------|-------------------------------|
| 요청 수 | `sum(rate(application_usecase_command_requests_total[5m]))` | 정상 범위 / +-50% 변동 / +-80% 변동 |
| 성공률 | `sum(rate(responses{response_status="success"}[5m])) / sum(rate(responses[5m])) * 100` | > 99.9% / > 99% / < 99% |
| P95 지연 | `histogram_quantile(0.95, sum(rate(duration_bucket[5m])) by (le))` | < 200ms / < 500ms / > 500ms |
| 에러율 | `sum(rate(responses{response_status="failure"}[5m])) / sum(rate(responses[5m])) * 100` | < 0.1% / < 1% / > 1% |
| 가용성 | `1 - (sum(rate(responses{error_type="exceptional"}[5m])) / sum(rate(responses[5m])))` | > 99.9% / > 99.5% / < 99.5% |
| 처리량 | `sum(rate(responses{response_status="success"}[5m]))` | 기준선 대비 안정 / -20% / -50% |

### Grafana 패널 구성

```
L1 스코어카드 대시보드
├── Row 1: Stat 패널 x 6 (건강 지표)
├── Row 2: 시계열 그래프 (요청 수 + 에러율 overlay)
├── Row 3: 시계열 그래프 (P95/P99 지연)
└── Row 4: 테이블 (최근 에러 top 10 by error.code)
```

---

## L2 드릴다운: 핸들러별 상세

L2 대시보드는 `request.layer` x `request.category.name` x `request.handler.name` 차원으로 드릴다운합니다.

### Application Layer 드릴다운

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

### Adapter Layer 드릴다운

```promql
# Repository별 P95 지연
histogram_quantile(0.95,
  sum(rate(adapter_repository_duration_bucket[5m])) by (le, request_handler_name)
)

# External Service별 에러율
sum(rate(adapter_external_service_responses_total{response_status="failure"}[5m])) by (request_handler_name)
/ sum(rate(adapter_external_service_responses_total[5m])) by (request_handler_name) * 100
```

### DomainEvent 시각화

```promql
# 이벤트 타입별 발행 수
sum(rate(adapter_event_requests_total[5m])) by (request_handler_name)

# 이벤트 핸들러별 처리 시간
histogram_quantile(0.95,
  sum(rate(application_usecase_event_duration_bucket[5m])) by (le, request_handler_name)
)
```

### Grafana 패널 구성

```
L2 드릴다운 대시보드
├── Variable: $layer (application/adapter), $category, $handler
├── Row 1: 선택된 핸들러 요청 수 + 에러율
├── Row 2: P50/P95/P99 지연 시계열
├── Row 3: error.type 분포 (expected vs exceptional)
├── Row 4: error.code top 10 테이블
└── Row 5: DomainEvent 발행 → Handler 체인
```

---

## 알림 규칙

### P0 -- Critical (즉시 대응)

| 조건 | PromQL | 조치 |
|------|--------|------|
| `error.type=exceptional` 급증 | `rate(responses{error_type="exceptional"}[5m]) > 0.01` | 시스템 오류 -> 인프라 점검, 로그 확인 |
| 전체 에러율 > 10% | `rate(responses{response_status="failure"}[5m]) / rate(responses[5m]) > 0.1` | 즉시 인시던트 선언 |
| External Service 타임아웃 연쇄 | 여러 외부 서비스에서 동시 `error.type=exceptional` | 의존성 서비스 장애 확인 |

```yaml
groups:
  - name: ai-governance-p0
    rules:
      - alert: ExceptionalErrorRateHigh
        expr: |
          sum(rate(application_usecase_command_responses_total{error_type="exceptional"}[5m]))
          / sum(rate(application_usecase_command_responses_total[5m]))
          > 0.01
        for: 2m
        labels:
          severity: critical
          team: platform
        annotations:
          summary: "시스템 오류율이 1%를 초과했습니다"
          description: "error.type=exceptional 비율: {{ $value | humanizePercentage }}"
```

### P1 -- Warning (15분 내 대응)

| 조건 | PromQL | 조치 |
|------|--------|------|
| 핵심 Handler P95 > 1s | `histogram_quantile(0.95, duration_bucket{request_handler_name="..."}) > 1` | 느린 쿼리, 외부 API 지연 분석 |
| 에러율 > 5% | `rate(responses{response_status="failure"}[5m]) / rate(responses[5m]) > 0.05` | error.code별 분류 후 주요 원인 식별 |
| EventHandler 처리 지연 | `histogram_quantile(0.95, application_usecase_event_duration_bucket) > 5` | 이벤트 핸들러 병목 분석 |

```yaml
      - alert: HandlerLatencyHigh
        expr: |
          histogram_quantile(0.95,
            sum(rate(application_usecase_command_duration_bucket[5m])) by (le, request_handler_name)
          ) > 1.0
        for: 5m
        labels:
          severity: warning
          team: backend
        annotations:
          summary: "{{ $labels.request_handler_name }} P95 지연이 1초를 초과했습니다"
```

### P2 -- Info (업무 시간 내 확인)

| 조건 | PromQL | 조치 |
|------|--------|------|
| P95 > 500ms | `histogram_quantile(0.95, duration_bucket) > 0.5` | 성능 추이 확인, 백로그 등록 |
| 새로운 error.code 등장 | 기존에 없던 error.code 출현 | 새 에러 경로 분석 |
| 트래픽 패턴 변화 | 요청 수가 기준선 대비 +-50% | 용량 계획 검토 |

---

## ctx.* 전파 코드 패턴

### Command Request에서 CtxPillar 지정

```csharp
public sealed class CreateDeploymentCommand
{
    public sealed record Request(
        string ModelId,                                      // Default(L+T): Unbounded ID
        string EndpointUrl,                                  // Default(L+T): URL
        [CtxTarget(CtxPillar.All)] string Environment,       // All(L+T+MetricsTag): Bounded(2값)
        [CtxTarget(CtxPillar.Default | CtxPillar.MetricsValue)]
        decimal DriftThreshold                               // Default + MetricsValue: 수치
    ) : ICommandRequest<Response>;
}
```

### DomainEvent에서 CtxPillar 지정

```csharp
public sealed record ReportedEvent(
    ModelIncidentId IncidentId,                              // Default(L+T)
    [CtxTarget(CtxPillar.All)] IncidentSeverity Severity,    // All(L+T+MetricsTag): Bounded(4값)
    ModelDeploymentId DeploymentId                           // Default(L+T)
) : DomainEvent;
```

### 생성되는 ctx.* 필드 매핑 예시 (CreateDeploymentCommand)

| ctx 필드 | Logging | Tracing | MetricsTag | MetricsValue |
|----------|---------|---------|------------|--------------|
| `ctx.create_deployment_command.request.model_id` | O | O | - | - |
| `ctx.create_deployment_command.request.endpoint_url` | O | O | - | - |
| `ctx.create_deployment_command.request.environment` | O | O | **O** | - |
| `ctx.create_deployment_command.request.drift_threshold` | O | O | - | **O** |

### 세그먼트 분석 PromQL

```promql
# 배포 환경별(Staging vs Production) 에러율
sum(rate(application_usecase_command_responses_total{response_status="failure"}[5m]))
  by (ctx_create_deployment_command_request_environment)
/ sum(rate(application_usecase_command_responses_total[5m]))
  by (ctx_create_deployment_command_request_environment) * 100

# 인시던트 심각도별 자동 격리 지연
histogram_quantile(0.95,
  sum(rate(application_usecase_event_duration_bucket[5m]))
    by (le, ctx_reported_event_severity)
)
```

---

## 알림 -> 분석 연결

알림 발생 시 다음 순서로 원인을 분석합니다:

1. **L1 스코어카드 확인** -- 전체 건강 상태 파악
2. **L2 드릴다운** -- `request.handler.name` 기준으로 문제 핸들러 식별
3. **분산 추적** -- 해당 핸들러의 느린 Trace 검색, Span 체인 분석
4. **ctx.* 세그먼트** -- 특정 환경(Staging/Production), 심각도에 집중되는지 확인
5. **로그 상세** -- `error.code`, `@error` 필드로 근본 원인 파악

[구현 결과](./03-implementation-results/)에서 실제 Observable Port 현황과 파이프라인 구성을 확인합니다.
