---
title: "어댑터 기술 요구사항"
description: "AI 모델 거버넌스 플랫폼의 영속성, 외부 서비스, HTTP API, 관측성 기술 요구사항"
---

## 배경

[애플리케이션 비즈니스 요구사항](../application/00-business-requirements/)에서 정의한 포트(Port)를 실제 기술 구현으로 연결하는 Adapter 레이어의 기술 요구사항을 정의합니다. 이 레이어는 도메인/애플리케이션 레이어가 정의한 인터페이스를 구현하며, 외부 시스템과의 통합을 담당합니다.

이 예제의 핵심 차별점은 **LanguageExt IO 고급 기능(Timeout, Retry, Fork, Bracket)의** 실전 적용입니다. 외부 서비스와의 통합에서 발생하는 네트워크 지연, 간헐적 실패, 타임아웃, 리소스 관리 문제를 함수형 방식으로 해결합니다.

4가지 IO 고급 기능의 핵심 특성:

| IO 패턴 | 문제 상황 | 보장 | Functorium 통합 |
|---------|----------|------|----------------|
| **Timeout + Catch** | 응답 지연이 시스템 전체를 느리게 만듦 | 최대 대기 시간 제한 + 타임아웃을 폴백으로 변환 | `IO.Timeout()` -> `.Catch(TimedOut)` -> `.Catch(Exceptional)` |
| **Retry + Schedule** | 간헐적 503/네트워크 오류 | 지수 백오프 + 지터로 자동 복구 | `IO.Retry(exponential \| jitter \| recurs \| maxDelay)` |
| **Fork + awaitAll** | 독립적인 N개 작업의 순차 실행 병목 | 병렬 실행으로 최악 소요 시간 = max(개별 시간) | `forks.Map(io => io.Fork())` -> `awaitAll(forks)` |
| **Bracket** | 예외 시 리소스(세션, 연결) 누수 | Acquire-Use-Release 수명 보장 | `acquire.Bracket(Use: ..., Fin: ...)` |

## 기술 영역

### 1. 영속성 (Persistence)

데이터를 저장하고 조회하는 영속성 계층입니다.

- InMemory 구현을 기본으로 제공한다
- Sqlite(EfCore/Dapper) 구현으로 전환할 수 있도록 설계한다
- 설정 파일(`Persistence:Provider`)로 구현체를 선택한다
- Repository 인터페이스 4종을 구현한다: IAIModelRepository, IDeploymentRepository, IAssessmentRepository, IIncidentRepository
- Query 포트 5종을 구현한다: IAIModelQuery, IModelDetailQuery, IDeploymentQuery, IDeploymentDetailQuery, IIncidentQuery
- UnitOfWork 패턴을 지원한다
- Source Generator가 생성한 Observable 래퍼를 DI에 등록한다

### 2. 외부 서비스 (Infrastructure)

외부 AI 플랫폼과 통합하는 서비스 계층입니다. 각 서비스는 LanguageExt IO 고급 기능의 특정 패턴을 시연합니다.

#### 2-1. 모델 헬스 체크 (Timeout + Catch)

- 배포된 모델의 헬스 상태를 확인한다
- 10초 타임아웃을 적용한다
- 타임아웃 시 실패 대신 TimedOut 폴백 결과를 반환한다
- 예외 발생 시 AdapterError로 변환한다

#### 2-2. 모델 모니터링 (Retry + Schedule)

- 배포된 모델의 드리프트 보고서를 조회한다
- 간헐적 실패 시 지수 백오프(100ms base) + 지터(0.3) + 최대 3회 재시도한다
- 최대 지연 5초를 적용한다
- 최종 실패 시 AdapterError로 변환한다

#### 2-3. 병렬 컴플라이언스 체크 (Fork + awaitAll)

- 5개 컴플라이언스 기준을 병렬로 체크한다: DataGovernance, SecurityReview, BiasAssessment, TransparencyAudit, HumanOversight
- 각 기준 체크는 독립적인 IO 연산으로 Fork한다
- awaitAll로 모든 결과를 수집한다
- 모든 기준 통과 여부를 집계한다

#### 2-4. 모델 레지스트리 조회 (Bracket)

- 외부 레지스트리에서 모델 메타데이터를 조회한다
- 레지스트리 세션을 획득(Acquire) -> 사용(Use) -> 해제(Release)한다
- 세션 해제는 성공/실패 무관하게 보장한다
- 예외 발생 시 AdapterError로 변환한다

### 3. HTTP API (Presentation)

FastEndpoints 기반 REST API를 제공합니다.

- 모델 관리: 등록, 조회, 검색, 위험 등급 분류 (4개 엔드포인트)
- 배포 관리: 생성, 조회, 검색, 검토 제출, 활성화, 격리 (6개 엔드포인트)
- 평가 관리: 개시, 조회 (2개 엔드포인트)
- 인시던트 관리: 보고, 조회, 검색 (3개 엔드포인트)
- FinResponse -> HTTP 상태 코드 변환을 지원한다

### 4. 관측성 (Observability)

OpenTelemetry 3-Pillar 관측성을 제공합니다.

- `[GenerateObservablePort]` Source Generator로 로깅/메트릭/트레이싱을 자동 생성한다
- `ObservableDomainEventNotificationPublisher`로 이벤트 발행 관측성을 제공한다
- Pipeline 미들웨어(Validation, Logging)를 자동 등록한다

## IO 고급 기능 시나리오

### 정상 시나리오

1. **헬스 체크 성공** -- 10초 이내에 응답을 받아 Healthy/Degraded 결과를 반환한다.
2. **헬스 체크 타임아웃** -- 10초 초과 시 TimedOut 폴백 결과를 반환한다 (오류가 아님).
3. **모니터링 재시도 성공** -- 첫 시도 실패 후 재시도로 드리프트 보고서를 받는다.
4. **병렬 체크 완료** -- 5개 기준이 병렬로 실행되어 순차 대비 빠르게 완료한다.
5. **레지스트리 세션 정상 해제** -- 조회 성공 후 세션이 정상 해제된다.
6. **레지스트리 세션 오류 후 해제** -- 조회 실패해도 세션이 해제된다 (Bracket 보장).

### 거부 시나리오

7. **모니터링 최종 실패** -- 3회 재시도 후에도 실패하면 MonitoringFailed 오류를 반환한다.
8. **병렬 체크 부분 실패** -- 일부 기준 체크가 실패하면 ComplianceCheckFailed 오류를 반환한다.
9. **세션 획득 실패** -- 레지스트리 세션 획득에 실패하면 RegistryLookupFailed 오류를 반환한다.

다음 단계에서는 이 기술 요구사항을 분석하여 IO 패턴 선택 근거를 [타입 설계 의사결정](./01-type-design-decisions/)에서 도출합니다.
