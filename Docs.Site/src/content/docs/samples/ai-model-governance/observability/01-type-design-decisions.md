---
title: "Observability Type Design Decisions"
description: "KPI-metric mapping, key metrics per layer, SLO baselines, and ctx.* propagation strategy"
---

## KPI -> 기술 메트릭 매핑

[비즈니스 요구사항](./00-business-requirements/)에서 정의한 SLO와 KPI를 Functorium 메트릭으로 매핑합니다.

| 비즈니스 KPI | Functorium 메트릭 | 필드/태그 |
|-------------|-------------------|----------|
| 모델 등록 완료율 | `application.usecase.command.responses` | `request.handler.name=RegisterModelCommand`, `response.status` |
| 인시던트 자동 격리 응답 시간 | `application.usecase.event.duration` | `request.handler.name=QuarantineDeploymentOnCriticalIncidentHandler` |
| 컴플라이언스 평가 통과율 | `application.usecase.command.responses` | `request.handler.name=InitiateAssessmentCommand`, `response.status` |
| 외부 서비스 안정성 | `adapter.external_service.responses` | `response.status`, `error.type` |
| 전체 에러율 | `application.usecase.command.responses` | `response.status=failure` |

---

## 레이어별 핵심 지표

### Application Layer

| 지표 | Instrument | 타입 | 설명 |
|------|-----------|------|------|
| UseCase 요청 수 | `application.usecase.{cqrs}.requests` | Counter | Command/Query/Event 요청 수 |
| UseCase 처리 시간 | `application.usecase.{cqrs}.duration` | Histogram | Command/Query/Event 처리 시간(초) |
| UseCase 에러 수 | `application.usecase.{cqrs}.responses` | Counter | `response.status=failure` 필터 |

### Adapter Layer

| 지표 | Instrument | 타입 | 설명 |
|------|-----------|------|------|
| Repository 호출 수 | `adapter.repository.requests` | Counter | Repository 메서드 호출 수 |
| Repository 처리 시간 | `adapter.repository.duration` | Histogram | Repository 처리 시간(초) |
| External Service 호출 수 | `adapter.external_service.requests` | Counter | 외부 서비스 호출 수 |
| External Service 처리 시간 | `adapter.external_service.duration` | Histogram | 외부 서비스 처리 시간(초) |

### DomainEvent

| 지표 | Instrument | 타입 | 설명 |
|------|-----------|------|------|
| 이벤트 발행 수 | `adapter.event.requests` | Counter | DomainEvent 발행 수 |
| 이벤트 핸들러 처리 시간 | `application.usecase.event.duration` | Histogram | EventHandler 처리 시간(초) |

---

## SLO 기준선

| 카테고리 | 지표 | 목표 | PromQL |
|---------|------|------|--------|
| Command UseCase | P95 지연 | < 200ms | `histogram_quantile(0.95, sum(rate(application_usecase_command_duration_bucket[5m])) by (le))` |
| Query UseCase | P95 지연 | < 50ms | `histogram_quantile(0.95, sum(rate(application_usecase_query_duration_bucket[5m])) by (le))` |
| Repository | P95 지연 | < 100ms | `histogram_quantile(0.95, sum(rate(adapter_repository_duration_bucket[5m])) by (le))` |
| External Service | P95 지연 | < 500ms | `histogram_quantile(0.95, sum(rate(adapter_external_service_duration_bucket[5m])) by (le))` |
| 전체 에러율 | 에러 비율 | < 0.1% | `rate(responses{response_status="failure"}[5m]) / rate(responses[5m])` |

---

## ctx.* 전파 전략

### CtxPillar 선택 결정

이 프로젝트에서 사용하는 ctx.* 필드와 CtxPillar 전파 전략입니다.

#### RegisterModelCommand

| 프로퍼티 | CtxPillar | 근거 |
|---------|-----------|------|
| `Name` (string) | Default (L+T) | 모델명은 Unbounded 카디널리티 -> MetricsTag 금지 |
| `Version` (string) | Default (L+T) | SemVer 문자열 -> 트레이스 검색용 |
| `Purpose` (string) | Logging only | 긴 문자열(500자) -> 디버그/감사용 |

#### CreateDeploymentCommand

| 프로퍼티 | CtxPillar | 근거 |
|---------|-----------|------|
| `ModelId` (string) | Default (L+T) | Unbounded ID -> 트레이스 검색용 |
| `EndpointUrl` (string) | Default (L+T) | URL -> 트레이스 검색용 |
| `Environment` (string) | All (L+T+MetricsTag) | Bounded(2값: Staging/Production) -> 세그먼트 분석 안전 |
| `DriftThreshold` (decimal) | Default + MetricsValue | 수치 -> Histogram 분포 분석 |

#### ReportIncidentCommand

| 프로퍼티 | CtxPillar | 근거 |
|---------|-----------|------|
| `DeploymentId` (string) | Default (L+T) | Unbounded ID -> 트레이스 검색용 |
| `Severity` (string) | All (L+T+MetricsTag) | Bounded(4값: Critical/High/Medium/Low) -> 세그먼트 분석 안전 |
| `Description` (string) | Logging only | 긴 문자열(2000자) -> 디버그/감사용 |

### 카디널리티 관리 원칙

| 카디널리티 수준 | MetricsTag 허용 | 예시 (이 프로젝트) |
|---------------|----------------|-------------------|
| Fixed (`bool`) | 안전 | -- |
| BoundedLow (`enum`, < 20값) | 조건부 허용 | `Environment`(2값), `Severity`(4값), `RiskTier`(4값), `DeploymentStatus`(6값) |
| Unbounded (`string`, `Guid`) | **금지** | `ModelId`, `DeploymentId`, `EndpointUrl` |
| Numeric (`decimal`, `int`) | **경고** | `DriftThreshold` -> MetricsValue 사용 |

### 결정 흐름

```
프로퍼티가 디버그용인가? (Purpose, Description)
├── YES → Logging only: [CtxTarget(CtxPillar.Logging)]
└── NO → 트레이스에서 검색 필요?
    ├── NO → [CtxIgnore]
    └── YES → 메트릭 세그먼트로 사용?
        ├── NO → Default (L+T)
        └── YES → 카디널리티가 Bounded인가?
            ├── YES → [CtxTarget(CtxPillar.All)]
            └── NO → 수치인가?
                ├── YES → [CtxTarget(CtxPillar.Default | CtxPillar.MetricsValue)]
                └── NO → Default 유지 (MetricsTag 금지)
```

다음 단계에서는 이 메트릭 설계를 대시보드, 알림, 코드 패턴으로 구체화하여 [코드 설계](./02-code-design/)를 진행합니다.
