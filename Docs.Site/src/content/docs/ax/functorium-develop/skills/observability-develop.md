---
title: "Observability Develop"
description: "Observability strategy design (KPI mapping, dashboard, alerts, ctx.* propagation)"
---

> project-spec -> architecture-design -> domain-develop -> application-develop -> adapter-develop -> **observability-develop** -> test-develop

## 선행 조건

`adapter-develop` 스킬에서 Observable Port와 CtxEnricher가 구현된 후 수행합니다.
Functorium의 3-Pillar(Logging/Metrics/Tracing) 파이프라인이 DI에 등록된 상태를 전제합니다.

## 배경

Functorium 프레임워크는 관측성 **수집**에 강합니다. `[GenerateObservablePort]`가 모든 어댑터에 Logging/Metrics/Tracing을 자동 부여하고, `CtxEnricher`가 비즈니스 컨텍스트를 3-Pillar에 동시 전파합니다.

그러나 **수집만으로는 부족합니다.** 수집된 데이터를 어떻게 분석하고, 어떤 지표가 건강한지 판단하며, 문제 발생 시 어떻게 행동할지 — 이 전략이 없으면 대시보드는 "보기만 하는 그래프"가 됩니다.

`observability-develop` 스킬은 이 간극을 메웁니다: **instrument → analyze → alert → act**.

## 스킬 개요

### 4 Phase 워크플로

| Phase | 활동 | 산출물 |
|-------|------|--------|
| 1. 관측성 전략 | KPI→메트릭 매핑, 기준선 설정, ctx.* 전파 전략 | 관측성 전략 문서 |
| 2. 대시보드 설계 | L1 스코어카드, L2 드릴다운, DomainEvent 추적 | 대시보드 레이아웃 |
| 3. 알림 설계 | P0/P1/P2 분류, 임계값, 알림 위생 | 알림 규칙 문서 |
| 4. 분석 + 조치 | 분산 추적 진단, 가설→실험, 리뷰 템플릿 | 분석 절차서 |

### 트리거 예시

```text
관측성 설계해줘
대시보드 설계해줘
메트릭 분석해줘
알림 설정해줘
성능 분석해줘
```

## Phase 1: 관측성 전략

### KPI → 기술 메트릭 매핑

비즈니스 성과 지표를 Functorium의 관측 필드에 매핑합니다:

| 비즈니스 KPI | 기술 메트릭 | Functorium 필드 |
|-------------|-----------|----------------|
| 사용자 응답 시간 | P95 지연 | `response.elapsed` (Histogram) |
| 서비스 가용성 | 에러율 | `response.status` + `error.type` |
| 기능별 사용량 | 요청 수 | `request.handler.name` (Counter) |
| 결제 성공률 | 성공/실패 비율 | `response.status` by `request.handler.name` |

### 기준선(SLO) 설정

| 지표 | Command 기준 | Query 기준 | External API 기준 |
|------|-------------|-----------|------------------|
| P95 지연 | < 200ms | < 50ms | < 1000ms |
| 에러율 | < 0.1% | < 0.1% | < 1% |
| 처리량 | > 100 RPS | > 500 RPS | - |

### ctx.* 전파 전략

| CtxPillar | 용도 | 예시 필드 | 카디널리티 |
|-----------|------|-----------|-----------|
| Logging only | 디버그/상세 데이터 | 요청 본문, 파라미터 상세 | 무제한 |
| Logging + Tracing (Default) | 식별자, 추적 컨텍스트 | customer_id, order_id | 높음 |
| All (+ MetricsTag) | 세그먼트 분석용 | customer_tier, region | **낮음 필수** |
| MetricsValue | 수치 기록 | order_total_amount | - |

**카디널리티 규칙:** MetricsTag에는 고유값이 제한된 필드만 사용 (customer_tier: 3~5종, customer_id: 수백만 → 금지).

## Phase 2: 대시보드 설계

### L1 스코어카드 (6개 건강 지표)

| 지표 | PromQL 예시 | 상태 |
|------|-----------|------|
| 요청 수 | `rate(usecase_request_total[5m])` | 처리량 추세 |
| 성공률 | `1 - (error_total / request_total)` | 99.9% 이상 |
| P95 지연 | `histogram_quantile(0.95, duration_bucket)` | < 200ms |
| 에러율 | `rate(error_total[5m]) / rate(request_total[5m])` | < 0.1% |
| Exceptional 에러 | `rate(error_total{error_type="exceptional"}[5m])` | 0에 수렴 |
| DomainEvent 처리량 | `rate(event_publish_total[5m])` | 추세 확인 |

### L2 드릴다운

`request.layer` × `request.category.name` × `request.handler.name` 3차원으로 분해하여 병목을 식별합니다.

## Phase 3: 알림 설계

### P0/P1/P2 분류

| 우선순위 | 조건 | 예시 | 대응 |
|---------|------|------|------|
| **P0** (즉시) | `error.type = "exceptional"` 급증 | DB 연결 실패, 외부 API 타임아웃 | 온콜 페이지 |
| **P1** (1시간) | P95 > 1s 또는 에러율 > 5% | 특정 핸들러 성능 저하 | Slack 알림 |
| **P2** (일간) | P95 > 500ms 또는 새 에러 코드 | 점진적 성능 저하 | 대시보드 확인 |

## Phase 4: 분석 + 조치

문제 신호 감지 시 분산 추적으로 원인을 진단합니다:

1. **신호 감지** — 대시보드/알림에서 이상 식별
2. **추적 쿼리** — `request.handler.name = "X"` AND `duration > threshold` 검색
3. **스팬 분석** — 하위 스팬 중 어디서 시간을 소비하는지 확인
4. **가설 수립** — DB N+1? 캐시 미스? 외부 API 지연?
5. **실험** — 개선 적용 후 기준선 대비 비교

## 관측성 필드 체계

Functorium Source Generator가 자동 수집하는 필드입니다.

### 요청/응답 필드

| 필드 | 설명 | 예시 |
|------|------|------|
| `request.layer` | 아키텍처 레이어 | `"application"`, `"adapter"` |
| `request.category.name` | 요청 카테고리 | `"usecase"`, `"repository"`, `"event"` |
| `request.category.type` | CQRS 타입 | `"command"`, `"query"`, `"event"` |
| `request.handler.name` | Handler 클래스 이름 | `"CreateProductCommand"` |
| `request.handler.method` | Handler 메서드 이름 | `"Handle"`, `"GetById"` |
| `response.status` | 응답 상태 | `"success"`, `"failure"` |
| `response.elapsed` | 처리 시간(초) | Histogram instrument로 기록 |

### 에러 분류 체계

| `error.type` | 분류 | 설명 | 알림 대응 |
|-------------|------|------|----------|
| `expected` | 비즈니스 오류 | 도메인 규칙 위반, 검증 실패 | 모니터링만 (정상 흐름) |
| `exceptional` | 시스템 오류 | DB 연결 실패, 외부 API 타임아웃 | P0/P1 알림 (즉시 대응) |
| `aggregate` | 복합 오류 | 여러 검증 실패 누적 | 모니터링 (Apply 패턴 결과) |

`error.code`는 도메인 특화 오류 코드입니다. 예: `"ProductName.Required"`, `"Order.InvalidTransition"`.

### Meter/Instrument 네이밍

| 구성 요소 | 패턴 | 예시 |
|-----------|------|------|
| Meter Name | `{service.namespace}.{layer}[.{category}]` | `AiGovernance.application.usecase` |
| Instrument Name | `{layer}.{category}[.{cqrs}].{type}` | `application.usecase.command.duration` |

점 구분, 소문자, 복수형을 사용합니다.

## 핵심 원칙

- **수집은 시작일 뿐** — instrument → analyze → alert → act 전체 사이클 설계
- **비즈니스 KPI에서 출발** — 기술 메트릭만 보지 말고 비즈니스 영향으로 번역
- **카디널리티 관리** — MetricsTag에 고카디널리티 필드 금지 (unbounded series 방지)
- **알림은 실행 가능해야** — "이 알림을 받으면 무엇을 해야 하는가?"에 답할 수 없으면 제거

## 참고 자료

- [워크플로](../workflow/) -- 7단계 전체 흐름
- [Adapter Develop 스킬](./adapter-develop/) -- 선행 단계: Observable Port 구현
- [Test Develop 스킬](./test-develop/) -- 후속 단계: 관측성 검증 테스트
