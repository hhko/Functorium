# 알림 패턴 레퍼런스

## 우선순위 분류

### P0 — Critical (즉시 대응)

| 조건 | PromQL / 진단 | 조치 |
|------|---------------|------|
| `error.type = "exceptional"` 급증 | `rate(responses{error_type="exceptional"}[5m]) > 0.01` | 시스템 오류 → 인프라 점검, 로그 확인 |
| DB 연결 실패 | `adapter.repository.responses{response_status="failure", error_type="exceptional"}` 급증 | DB 연결 풀, 네트워크, 인스턴스 상태 확인 |
| Adapter 타임아웃 연쇄 | 여러 Adapter에서 동시 `error.type = "exceptional"` | 의존성 서비스 장애 확인, 서킷 브레이커 상태 점검 |
| 전체 에러율 > 10% | `rate(responses{response_status="failure"}[5m]) / rate(responses[5m]) > 0.1` | 전체 시스템 영향 → 즉시 인시던트 선언 |

### P1 — Warning (15분 내 대응)

| 조건 | PromQL / 진단 | 조치 |
|------|---------------|------|
| 핵심 Handler P95 > 1s | `histogram_quantile(0.95, duration_bucket{request_handler_name="..."}) > 1` | 느린 쿼리, N+1, 외부 API 지연 분석 |
| 에러율 > 5% | `rate(responses{response_status="failure"}[5m]) / rate(responses[5m]) > 0.05` | error.code별 분류 후 주요 원인 식별 |
| Validation 에러 급증 | `rate(responses{error_type="expected"}[5m])` 급격한 증가 | 클라이언트 변경, 데이터 품질 문제 확인 |
| DomainEvent 처리 지연 | `histogram_quantile(0.95, application_usecase_event_duration_bucket) > 5` | 이벤트 핸들러 병목 분석 |

### P2 — Info (업무 시간 내 확인)

| 조건 | PromQL / 진단 | 조치 |
|------|---------------|------|
| P95 > 500ms | `histogram_quantile(0.95, duration_bucket) > 0.5` | 성능 추이 확인, 최적화 백로그 등록 |
| 새로운 `error.code` 등장 | 기존에 없던 error.code 출현 | 새 에러 경로 분석, 문서 업데이트 |
| DomainEvent Handler 지연 급증 | 이벤트 핸들러 P95가 기준선 대비 200% 이상 | 핸들러 코드 리뷰, 의존 Adapter 점검 |
| 트래픽 패턴 변화 | 요청 수가 기준선 대비 ±50% | 용량 계획 검토, 스케일링 정책 확인 |

## Prometheus AlertManager 규칙 예시

### P0: Exceptional 에러 급증

```yaml
groups:
  - name: functorium-p0
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
          runbook: "https://wiki/runbooks/exceptional-error-high"
```

### P1: 핸들러 지연 경고

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
          description: "현재 P95: {{ $value | humanizeDuration }}"
```

### P1: 에러율 경고

```yaml
      - alert: ErrorRateWarning
        expr: |
          sum(rate(application_usecase_command_responses_total{response_status="failure"}[5m])) by (request_handler_name)
          / sum(rate(application_usecase_command_responses_total[5m])) by (request_handler_name)
          > 0.05
        for: 5m
        labels:
          severity: warning
          team: backend
        annotations:
          summary: "{{ $labels.request_handler_name }} 에러율이 5%를 초과했습니다"
          description: "현재 에러율: {{ $value | humanizePercentage }}"
```

### P2: 성능 추이 알림

```yaml
      - alert: LatencyTrendWarning
        expr: |
          histogram_quantile(0.95,
            sum(rate(application_usecase_command_duration_bucket[5m])) by (le)
          ) > 0.5
        for: 15m
        labels:
          severity: info
          team: backend
        annotations:
          summary: "전체 Command P95 지연이 500ms를 초과하고 있습니다"
          description: "15분간 지속. 현재 P95: {{ $value | humanizeDuration }}"
```

## 알림 위생 (Alert Hygiene)

### 실행 가능한 알림만 유지

| 원칙 | 설명 | 예시 |
|------|------|------|
| 조치 매핑 필수 | 알림마다 구체적 조치 문서(Runbook) 연결 | `annotations.runbook` 필드 필수 |
| 수신자 명확 | 담당 팀/개인 지정 | `labels.team` 필드 필수 |
| 중복 제거 | 동일 원인의 다중 알림 방지 | `group_by: [request_handler_name]` |

### 오탐(False Positive) 관리

| 전략 | 설명 |
|------|------|
| `for` 지속 조건 | 최소 2~5분 지속 시에만 알림 발생 |
| 기준선 기반 임계값 | 2주간 데이터 수집 후 P95 기반 임계값 설정 |
| 정기 리뷰 | 월 1회 오탐률 검토, 임계값 조정 |

### 에스컬레이션 경로

```
P0 (Critical)
├── 즉시: 담당 팀 호출 (PagerDuty/Opsgenie)
├── 5분: 팀 리드 알림
├── 15분: 엔지니어링 매니저 알림
└── 30분: 인시던트 커맨더 소집

P1 (Warning)
├── 즉시: Slack 채널 알림
├── 15분: 담당 엔지니어 확인
└── 1시간: 팀 리드 에스컬레이션

P2 (Info)
├── 업무 시간: 대시보드 확인
└── 주간 리뷰: 추이 분석 후 백로그 등록
```

## 알림 → 분석 연결

알림 발생 시 다음 순서로 원인을 분석합니다:

1. **L1 스코어카드 확인** → 전체 건강 상태 파악
2. **L2 드릴다운** → `request.handler.name` 기준으로 문제 핸들러 식별
3. **분산 추적** → 해당 핸들러의 느린 Trace 검색, Span 체인 분석
4. **ctx.* 세그먼트** → 특정 고객 등급/지역에 집중되는지 확인
5. **로그 상세** → `error.code`, `@error` 필드로 근본 원인 파악
