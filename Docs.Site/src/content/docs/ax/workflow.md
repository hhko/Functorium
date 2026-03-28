---
title: "워크플로"
description: "PRD에서 테스트까지 7단계 개발 워크플로"
---

AX는 PRD 작성부터 테스트까지 7단계로 이어지는 개발 워크플로를 제공합니다. 각 단계는 이전 단계의 산출물을 입력으로 받아, 설계 의도가 코드까지 일관되게 전달됩니다.

## 전체 흐름

```
 1. project-spec          비전 -> 요구사항 명세
        |
        v
 2. architecture-design   구조 -> 폴더/네이밍/인프라 설계
        |
        v
 3. domain-develop        모델 -> VO, Aggregate, Event 구현
        |
        v
 4. application-develop   흐름 -> Command/Query/EventHandler 구현
        |
        v
 5. adapter-develop       연결 -> Repository, Endpoint, DI 구현
        |
        v
 6. observability-develop 관측 -> KPI 매핑, 대시보드, 알림, ctx.* 전파
        |
        v
 7. test-develop          검증 -> 단위/통합/아키텍처/관측성 테스트
```

별도로 `domain-review` 스킬은 어느 시점에서든 기존 코드를 DDD 관점에서 리뷰합니다.

## 각 단계 요약

### 1단계: Project Spec -- 비전에서 스펙으로

| 항목 | 내용 |
|------|------|
| 스킬 | [project-spec](./skills/project-spec/) |
| 입력 | 사용자의 비전, 비즈니스 문제, 대상 사용자 |
| 출력 | `{context}/00-project-spec.md` |
| 핵심 활동 | 유비쿼터스 언어 추출, Aggregate 후보 식별, 비즈니스 규칙 분류, MVP 범위 결정 |

비즈니스 언어로 시작하여 도메인 모델의 첫 윤곽을 그립니다. "해결하려는 문제가 무엇인가?"에서 "어떤 Aggregate가 필요하고, 어떤 규칙이 있는가?"까지 대화를 통해 도달합니다.

### 2단계: Architecture Design -- 구조 결정

| 항목 | 내용 |
|------|------|
| 스킬 | [architecture-design](./skills/architecture-design/) |
| 입력 | `00-project-spec.md` (있으면 자동 참조) |
| 출력 | `{context}/01-architecture-design.md` |
| 핵심 활동 | 프로젝트 구조, 레이어 구성, 네이밍 규칙, 영속성/관측성/API 인프라 결정 |

코드를 작성하기 전에 솔루션의 뼈대를 결정합니다. 3차원 폴더 구조(Aggregate/CQRS Role/Technology), 프로젝트 참조 방향, DI 등록 전략, Provider 전환 구성을 문서화합니다.

### 3단계: Domain Develop -- 모델 구현

| 항목 | 내용 |
|------|------|
| 스킬 | [domain-develop](./skills/domain-develop/) |
| 입력 | `00-project-spec.md`, `01-architecture-design.md` (있으면 자동 참조) |
| 출력 | `domain/00~03` 문서 4종 + VO, Aggregate, Event, Spec, Service 소스 코드 |
| 핵심 활동 | 불변식 분류, Functorium 타입 매핑, 코드 생성, 단위 테스트 |

불변식을 타입으로 인코딩하는 단계입니다. 비즈니스 규칙을 분류하고(단일값/비교/열거형/상태전이/배타적 상태), 각 규칙에 맞는 Functorium 타입(`SimpleValueObject`, `ComparableSimpleValueObject`, `UnionValueObject`, `AggregateRoot` 등)으로 매핑한 뒤, 실제 코드와 테스트를 생성합니다.

### 4단계: Application Develop -- 유스케이스 구현

| 항목 | 내용 |
|------|------|
| 스킬 | [application-develop](./skills/application-develop/) |
| 입력 | `domain/03-implementation-results.md` (도메인 모델 현황) |
| 출력 | `application/00~03` 문서 4종 + Command, Query, EventHandler, Validator 소스 코드 |
| 핵심 활동 | CQRS 분류, 포트 식별, FinT LINQ 합성, FluentValidation 통합 |

도메인 모델 위에 비즈니스 흐름을 구축합니다. 워크플로를 Command/Query/EventHandler로 분해하고, 각 유스케이스가 의존하는 포트(IRepository, IQueryPort, External Service)를 식별한 뒤, FinT 모나드 합성으로 핸들러를 구현합니다.

### 5단계: Adapter Develop -- 인프라 연결

| 항목 | 내용 |
|------|------|
| 스킬 | [adapter-develop](./skills/adapter-develop/) |
| 입력 | `application/03-implementation-results.md` (포트 목록), `01-architecture-design.md` (인프라 전략) |
| 출력 | `adapter/00~03` 문서 4종 + Repository, Query Adapter, Endpoint, DI 등록 소스 코드 |
| 핵심 활동 | InMemory/EfCore Repository, Dapper Query, FastEndpoints, Observable Port, DI 등록 |

포트 인터페이스에 구체적인 인프라 기술을 연결합니다. `[GenerateObservablePort]`로 관측성을 자동 부여하고, Mapper로 도메인-영속성 경계를 분리하며, Provider 전환으로 InMemory/Sqlite를 전환할 수 있게 구성합니다.

### 6단계: Observability Develop -- 관측성 전략

| 항목 | 내용 |
|------|------|
| 스킬 | [observability-develop](./skills/observability-develop/) |
| 입력 | 구현된 어댑터 코드 (Observable Port, CtxEnricher) |
| 출력 | 관측성 전략 문서 (KPI 매핑, 대시보드 레이아웃, 알림 규칙) |
| 핵심 활동 | KPI→메트릭 매핑, 기준선 설정, 대시보드 설계, 알림 패턴, ctx.* 전파 전략 |

수집된 관측 데이터를 어떻게 분석하고 행동할지 설계합니다. 비즈니스 KPI를 기술 메트릭에 매핑하고, L1/L2 대시보드를 설계하며, P0/P1/P2 알림을 분류하고, 장애 시 분산 추적 분석 절차를 정의합니다.

### 7단계: Test Develop -- 품질 검증

| 항목 | 내용 |
|------|------|
| 스킬 | [test-develop](./skills/test-develop/) |
| 입력 | 구현된 소스 코드, `03-implementation-results.md` 문서들, 관측성 전략 문서 |
| 출력 | 단위 테스트, 통합 테스트, 아키텍처 규칙 테스트, 관측성 검증 테스트 코드 |
| 핵심 활동 | VO/Aggregate/Usecase 단위 테스트, HostTestFixture 통합 테스트, ArchUnitNET 규칙, ctx 3-Pillar 스냅샷 테스트 |

구현이 설계 의도를 충족하는지 검증합니다. Value Object의 Create 성공/실패, Aggregate의 상태 변경과 이벤트 발행, Usecase의 성공/실패 시나리오, HTTP 엔드포인트의 상태 코드, 레이어 의존성 방향, ctx.* 3-Pillar 전파 정합성을 테스트합니다.

## 단계 간 연결

각 단계의 출력 문서는 다음 단계의 입력이 됩니다. 이 연결이 설계 의도의 일관성을 보장합니다.

```
00-project-spec.md
  |-- 유비쿼터스 언어 테이블 --> domain-develop Phase 1이 참조
  |-- Aggregate 후보 목록   --> architecture-design이 폴더 구조에 반영
  |-- 비즈니스 규칙         --> domain-develop Phase 2가 타입 매핑에 활용
  |-- 유스케이스 개요       --> application-develop Phase 1이 Command/Query 분류에 활용

01-architecture-design.md
  |-- 폴더 구조             --> domain/application/adapter 코드 생성 위치 결정
  |-- 네이밍 규칙           --> 모든 코드 생성에 적용
  |-- 영속성 전략           --> adapter-develop의 Provider 선택에 활용

domain/03-implementation-results.md
  |-- Aggregate 목록        --> application-develop의 Repository Port 식별
  |-- VO 목록               --> application-develop의 Validator 작성
  |-- Domain Event 목록     --> application-develop의 EventHandler 식별

application/03-implementation-results.md
  |-- Port 인터페이스       --> adapter-develop의 구현 대상
  |-- Request/Response DTO  --> adapter-develop의 Endpoint 작성
```

## 유연한 진입점

6단계를 반드시 순서대로 진행할 필요는 없습니다. 각 스킬은 독립적으로 동작하며, 선행 문서가 없으면 사용자에게 직접 질문합니다.

| 상황 | 시작 스킬 | 이유 |
|------|----------|------|
| 새 프로젝트 시작 | project-spec | 비전부터 체계적으로 정의 |
| 요구사항은 이미 정리됨 | architecture-design 또는 domain-develop | PRD를 건너뛰고 구조/모델부터 |
| 기존 도메인 모델에 유스케이스 추가 | application-develop | 새 Command/Query만 추가 |
| 기존 코드의 어댑터만 교체 | adapter-develop | InMemory에서 EF Core로 전환 |
| 테스트 누락 보완 | test-develop | 기존 코드에 테스트 추가 |
| 기존 코드 품질 점검 | domain-review | DDD 관점 리뷰로 개선점 파악 |

## 실제 프로젝트 예시

AI 모델 거버넌스 플랫폼을 6단계로 개발하는 과정입니다.

### 1단계: Project Spec

```text
PRD 작성해줘. AI 모델 거버넌스 플랫폼을 만들고 싶어.
```

산출물:
- 유비쿼터스 언어: AIModel, ModelDeployment, ComplianceAssessment, ModelIncident
- Aggregate 4개 식별
- 상태 전이: Draft -> PendingReview -> Active -> Quarantined -> Decommissioned
- 교차 규칙: Critical 인시던트 -> Active 배포 자동 격리
- 금지 상태: Unacceptable 리스크 등급 모델의 배포

### 2단계: Architecture Design

```text
프로젝트 구조를 설계해줘.
```

산출물:
- `AiGovernance.Domain`, `AiGovernance.Application`, `AiGovernance.Adapters.*` 프로젝트 구조
- Aggregate별 폴더: `AIModels/`, `ModelDeployments/`, `ComplianceAssessments/`, `ModelIncidents/`
- 영속성: InMemory (개발) + SQLite (운영)
- 관측성: OpenTelemetry 3-Pillar

### 3단계: Domain Develop

```text
AIModel Aggregate를 설계하고 구현해줘.
```

산출물:
- Value Object: ModelName, ModelVersion, RiskLevel(SmartEnum), ModelStatus(UnionValueObject)
- AggregateRoot: AIModel (Create, SubmitForReview, Activate, Quarantine, Decommission)
- Domain Event: CreatedEvent, ActivatedEvent, QuarantinedEvent
- Specification: AIModelByStatusSpec, AIModelByRiskLevelSpec
- 단위 테스트 40개 이상

### 4단계: Application Develop

```text
모델 등록 Command Usecase를 만들어줘.
```

산출물:
- Command: RegisterAIModelCommand, SubmitForReviewCommand
- Query: GetAIModelByIdQuery, SearchAIModelsQuery
- EventHandler: OnModelActivated (배포 트리거)
- Validator: FluentValidation + MustSatisfyValidation

### 5단계: Adapter Develop

```text
AIModel Repository를 EF Core로 구현해줘.
```

산출물:
- InMemory + EfCore Repository
- Model, Configuration, Mapper
- FastEndpoints 엔드포인트
- DI 등록 (Provider 전환)

### 6단계: Test Develop

```text
AIModel 도메인 단위 테스트를 작성해줘.
```

산출물:
- VO 단위 테스트: Create 성공/실패, 정규화
- Aggregate 단위 테스트: 상태 전이 성공/실패, 이벤트 발행
- Usecase 단위 테스트: Mock 기반 성공/실패
- 통합 테스트: HTTP 엔드포인트 201/400/404
- 아키텍처 규칙: 레이어 의존성, sealed class

## 레이어별 4단계 문서

domain-develop, application-develop, adapter-develop 스킬은 각각 동일한 4단계 문서를 생성합니다.

| 문서 | 내용 | 목적 |
|------|------|------|
| `00-business-requirements.md` | 요구사항, 유비쿼터스 언어, 비즈니스 규칙 | 무엇을 만드는가 |
| `01-type-design-decisions.md` | 불변식 분류, 타입/포트/어댑터 매핑 전략 | 어떤 타입을 사용하는가 |
| `02-code-design.md` | 전략에서 C#/Functorium 패턴으로 변환 | 어떻게 구현하는가 |
| `03-implementation-results.md` | 구현 코드 + 테스트 검증 결과 | 실제로 무엇이 만들어졌는가 |

이 4단계 문서 체계는 설계 결정의 추적성(Traceability)을 제공합니다. "이 타입을 왜 선택했는가?"라는 질문에 01번 문서가 답하고, "이 패턴을 왜 사용했는가?"라는 질문에 02번 문서가 답합니다.

## 다음 단계

- [Project Spec 스킬](./skills/project-spec/) -- 첫 번째 단계부터 시작
- [전문 에이전트](./agents/) -- 설계 결정에 전문가 활용
- [설치 가이드](./installation/) -- 플러그인 설치와 설정
