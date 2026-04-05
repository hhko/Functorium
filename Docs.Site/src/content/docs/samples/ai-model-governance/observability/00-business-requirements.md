---
title: "관측성 비즈니스 요구사항"
description: "AI 모델 거버넌스 플랫폼의 관측성이 필요한 이유와 3-Pillar 요구사항"
---

## 왜 관측성이 필요한가

AI 모델 거버넌스 플랫폼은 EU AI Act 규정 준수를 자동화합니다. 모델 등록, 배포 상태 전이, 컴플라이언스 평가, 인시던트 자동 격리 등 핵심 워크플로우가 실패하면 규정 위반으로 이어질 수 있습니다. 관측성은 다음 질문에 답합니다:

1. **시스템이 정상적으로 동작하는가?** -- 요청 처리량, 에러율, 지연 시간으로 전체 건강 상태를 파악한다
2. **어디서 문제가 발생하는가?** -- 핸들러별, 레이어별 드릴다운으로 병목을 식별한다
3. **비즈니스 KPI가 달성되고 있는가?** -- 인시던트 자동 격리 응답 시간, 컴플라이언스 평가 통과율 등을 추적한다

---

## 3-Pillar 요구사항

### Metrics (메트릭)

정량적 시계열 데이터로 시스템 건강 상태를 측정합니다.

- 모든 Command/Query UseCase의 요청 수, 응답 수, 처리 시간을 측정한다
- 모든 Repository/Query Adapter의 호출 수, 응답 수, 처리 시간을 측정한다
- 외부 서비스(ExternalService)의 호출 수, 응답 수, 처리 시간을 측정한다
- DomainEvent 발행 수와 EventHandler 처리 시간을 측정한다
- error.type별(expected/exceptional) 에러 분류를 제공한다
- Bounded 카디널리티 ctx.* 필드를 MetricsTag로 전파하여 세그먼트 분석을 지원한다

### Tracing (분산 추적)

요청의 전체 경로를 추적하여 병목과 의존성을 시각화합니다.

- UseCase 진입부터 Repository 저장까지 전체 스팬 체인을 기록한다
- 외부 서비스 호출(헬스 체크, 모니터링, 컴플라이언스 체크, 레지스트리)의 스팬을 기록한다
- DomainEvent 발행과 EventHandler 처리의 스팬을 기록한다
- ctx.* 식별자(ModelId, DeploymentId)를 스팬 태그로 전파하여 검색 가능하게 한다

### Logging (구조화 로깅)

사람이 읽을 수 있는 상세 컨텍스트를 기록합니다.

- UseCase 진입/종료, Adapter 호출/응답을 구조화 로그로 기록한다
- 오류 발생 시 error.type, error.code, @error 필드를 포함한다
- 요청/응답의 ctx.* 필드를 Serilog LogContext에 전파하여 상관 분석을 지원한다
- 디버그용 상세 데이터는 Logging 전용 pillar로 전파한다

---

## SLO Targets

| 카테고리 | 지표 | 목표 | 측정 방법 |
|---------|------|------|----------|
| Command UseCase | P95 지연 | < 200ms | `histogram_quantile(0.95, application_usecase_command_duration)` |
| Query UseCase | P95 지연 | < 50ms | `histogram_quantile(0.95, application_usecase_query_duration)` |
| Repository Adapter | P95 지연 | < 100ms | `histogram_quantile(0.95, adapter_repository_duration)` |
| External API | P95 지연 | < 500ms | `histogram_quantile(0.95, adapter_external_service_duration)` |
| 전체 에러율 | 에러 비율 | < 0.1% | `rate(responses{response_status=failure}) / rate(responses)` |
| 가용성 | 성공 비율 | > 99.9% | `1 - (failure_rate)` |

---

## 관측성 구현 전략

이 프로젝트는 Functorium의 Source Generator 기반 관측성을 사용합니다:

1. **`[GenerateObservablePort]`** -- Repository, Query, External Service 클래스에 적용하여 Observable 래퍼를 자동 생성한다
2. **`ObservableDomainEventNotificationPublisher`** -- DomainEvent 발행/처리의 관측성을 자동으로 제공한다
3. **Pipeline 미들웨어** -- UseObservability()로 CtxEnricher, Metrics, Tracing, Logging을 일괄 활성화하고, UseValidation(), UseException() 등 명시적 opt-in으로 나머지를 등록한다
4. **`RegisterScopedObservablePort`** -- DI에서 Observable 래퍼를 인터페이스에 등록하여 투명한 관측성을 제공한다

다음 단계에서는 비즈니스 KPI를 기술 메트릭으로 매핑하고, ctx.* 전파 전략을 [타입 설계 의사결정](./01-type-design-decisions/)에서 결정합니다.
