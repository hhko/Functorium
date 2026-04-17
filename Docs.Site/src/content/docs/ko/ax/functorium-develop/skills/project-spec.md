---
title: "Project Spec"
description: "프로젝트 요구사항 명세(PRD) 작성"
---

> **project-spec** -> architecture-design -> domain-develop -> application-develop -> adapter-develop -> observability-develop -> test-develop

## 선행 조건

없음. 이 스킬은 워크플로의 첫 번째 단계입니다. 사용자의 비전과 비즈니스 문제에서 출발합니다.

## 배경

새 프로젝트를 시작할 때 가장 흔한 문제는 "바로 코드를 작성하기 시작하는 것"입니다. Aggregate 경계가 모호한 상태로 코드를 작성하면, 나중에 비싼 리팩터링이 필요해집니다. 유비쿼터스 언어를 정의하지 않으면, 팀원마다 같은 개념을 다른 이름으로 부르게 됩니다.

`project-spec` 스킬은 **PM 관점의 스펙 작성**(비전, 사용자 스토리, 우선순위, 수락 기준)과 **DDD 관점의 도메인 분석**(유비쿼터스 언어, Aggregate 경계, 비즈니스 규칙)을 통합합니다. 이 문서는 `domain-develop`, `application-develop`, `adapter-develop` 스킬의 입력이 되어, 설계 의도가 코드까지 끊기지 않고 전달됩니다.

## 스킬 개요

### 4 Phase 워크플로

| Phase | 활동 | 산출물 |
|-------|------|--------|
| 1. 비전 수집 | 프로젝트 기본 정보, 사용자, KPI, Non-Goals, 타임라인 | 프로젝트 개요 초안 |
| 2. 도메인 분석 + 사용자 스토리 | 유비쿼터스 언어, 비즈니스 규칙, 사용자 스토리(INVEST), 수락 기준(Given/When/Then) | 언어 테이블 + 규칙 카탈로그 + 스토리 |
| 3. 스코프 결정 + 우선순위 | Aggregate 경계 도출, P0/P1/P2 우선순위, 마일스톤 | Aggregate 후보 + 우선순위 테이블 |
| 4. 문서 생성 | 전체 내용을 구조화된 문서로 정리 | `00-project-spec.md` |

### 트리거 예시

```text
PRD 작성해줘
요구사항 정의해줘
프로젝트 기획해줘
스펙 작성해줘
비즈니스 요구사항 정리해줘
프로젝트 시작해줘
```

## Phase 1: 비전 수집

스킬은 대화를 통해 다음 정보를 수집합니다.

**프로젝트 기본 정보:**
- 프로젝트 이름은?
- 한 줄로 설명하면?
- 해결하려는 비즈니스 문제는?

**사용자 정보:**
- 대상 사용자(페르소나)는 누구인가?
- 각 페르소나의 핵심 목표는?

**비즈니스 정보:**
- 핵심 성공 지표(KPI)는?
  - 선행 지표 (예: 일일 활성 사용자, 기능 도입률)
  - 후행 지표 (예: 매출, 유지율)
- 기존 시스템과의 연동 제약은?
- 기술 제약 조건은? (.NET 10, 단일/마이크로서비스 등)

**스코프 경계:**
- **하지 않을 것(Non-Goals)은 무엇인가?** -- 이 프로젝트에서 명시적으로 제외하는 기능/범위
- 하드 데드라인이나 외부 의존성은?

사용자가 모든 정보를 한 번에 제공하지 않아도 됩니다. 스킬이 질문을 통해 점진적으로 수집합니다.

## Phase 2: 도메인 분석 + 사용자 스토리

수집한 비전에서 도메인 모델의 후보와 사용자 스토리를 추출합니다.

### 유비쿼터스 언어 추출

비즈니스 설명에서 핵심 요소를 식별합니다:
- **핵심 명사** -> Entity/VO 후보 (예: "모델", "배포", "평가")
- **핵심 동사** -> 유스케이스(Command/Query) 후보 (예: "등록", "승인", "격리")
- **상태 변화** -> 도메인 이벤트 후보 (예: "활성화됨", "격리됨")

결과는 다음과 같은 테이블로 정리됩니다:

| 한글 | 영문 | 정의 |
|------|------|------|
| AI 모델 | AIModel | 학습 완료된 AI/ML 모델. 생명주기와 리스크를 관리하는 핵심 엔티티 |
| 모델 배포 | ModelDeployment | 특정 AI 모델의 운영 환경 배포. 버전, 환경, 상태를 추적 |
| 리스크 등급 | RiskTier | EU AI Act 기반 모델 위험 수준 (Minimal, Limited, High, Unacceptable) |

### 사용자 스토리 추출 (INVEST 기준)

각 페르소나별로 핵심 스토리를 작성합니다:

```text
[페르소나]로서, [행동]하고 싶다, [가치]를 얻기 위해.
```

**INVEST 기준 검증:**
- **I**ndependent: 다른 스토리와 독립적
- **N**egotiable: 구현 방법은 협상 가능
- **V**aluable: 사용자에게 가치 제공
- **E**stimable: 크기 추정 가능
- **S**mall: 한 스프린트 이내 완료
- **T**estable: 수락 기준으로 검증 가능

### 유스케이스별 수락 기준

각 유스케이스(Command/Query)에 대해 Given/When/Then 형식의 수락 기준을 작성합니다:

```text
Given: [사전 조건]
When:  [사용자 행동]
Then:  [기대 결과]
```

정상 시나리오와 거부 시나리오 모두 작성합니다.

### 비즈니스 규칙 분류

식별된 규칙을 유형별로 분류합니다:

| 규칙 유형 | 설명 | 예시 |
|-----------|------|------|
| 불변식 | 항상 참이어야 하는 조건 | "모델 이름은 비어있을 수 없다" |
| 상태 전이 | 허용된 전이만 가능 | "Draft -> PendingReview만 가능" |
| 교차 규칙 | 여러 Aggregate 참조 | "Critical 인시던트 시 Active 배포 자동 격리" |
| 금지 상태 | 구조적으로 불가능해야 하는 상태 | "Unacceptable 리스크 모델의 배포" |

## Phase 3: 스코프 결정 + 우선순위

### Aggregate 경계 도출

Evans의 기준에 따라 Aggregate 후보를 식별합니다.

**경계 결정 기준:**
1. **트랜잭션 일관성** -- 같은 트랜잭션에서 변경되는 데이터는 같은 Aggregate
2. **불변식 범위** -- 불변식이 보장해야 하는 데이터 범위
3. **독립적 생명주기** -- 다른 Aggregate 없이 독립적으로 생성/삭제 가능
4. **Aggregate 간 참조** -- ID로만 참조 (직접 객체 참조 금지)

**Aggregate 간 조율:**
- **동기 조율** -> Domain Service (같은 트랜잭션 내)
- **비동기 조율** -> Domain Event + Event Handler (최종 일관성)

### P0/P1/P2 우선순위 분류

모든 유스케이스와 사용자 스토리를 우선순위별로 분류합니다:

| 우선순위 | 기준 | MoSCoW 매핑 |
|---------|------|-------------|
| **P0** | 없으면 출시 불가 | Must Have |
| **P1** | 없으면 경쟁력 약화 | Should Have |
| **P2** | 있으면 차별화 | Could Have |

### 스코프 크리프 방지 체크리스트

기능 추가 요청 시 다음 5가지를 확인합니다:
1. 이 기능이 핵심 문제를 직접 해결하는가?
2. P0 없이 이 기능만으로 가치가 있는가?
3. 출시 후로 미뤄도 사용자가 수용하는가?
4. 구현 비용 대비 가치가 충분한가?
5. Non-Goals에 해당하지 않는가?

### 타임라인 + 마일스톤

- 하드 데드라인 식별
- 외부 의존성 (다른 팀, 서드파티 API, 인프라)
- 마일스톤별 범위 (Phase 1: P0, Phase 2: P0+P1, ...)

## Phase 4: 문서 생성

수집한 모든 정보를 `{context}/00-project-spec.md`로 구조화합니다.

### 출력 문서 구조

```markdown
# {프로젝트 이름} -- 프로젝트 요구사항 명세

## 1. 프로젝트 개요
### 배경 / 목표 / 대상 사용자 / 성공 지표(선행+후행) / 기술 제약

## 2. Non-Goals (하지 않을 것)

## 3. 유비쿼터스 언어
| 한글 | 영문 | 정의 |

## 4. 사용자 스토리 (INVEST)
### 페르소나별 스토리 + 우선순위

## 5. Aggregate 후보
| Aggregate | 핵심 책임 | 상태 전이 | 주요 이벤트 |

## 6. 비즈니스 규칙
### Aggregate별 규칙 + 교차 규칙

## 7. 유스케이스 + 수락 기준
### Commands / Queries / Event Handlers + Given/When/Then

## 8. 금지 상태
| 금지 상태 | 방지 전략 | Functorium 패턴 |

## 9. 우선순위 요약 (P0/P1/P2)

## 10. 타임라인 / 마일스톤

## 11. Open Questions (engineering/product/design/legal)

## 12. 다음 단계
```

### 다음 단계 안내

문서 생성 후 스킬은 다음 단계를 안내합니다:

> 프로젝트 스펙이 완성되었습니다.
>
> **다음 단계:**
> 1. `architecture-design` 스킬로 프로젝트 구조와 인프라를 설계하세요
> 2. `domain-develop` 스킬로 각 Aggregate를 상세 설계하고 구현하세요
> 3. `application-develop` 스킬로 유스케이스를 구현하세요

## 예시: AI 모델 거버넌스 PRD

실제 프로젝트의 PRD 핵심 요약입니다.

### Non-Goals

- 모델 학습 파이프라인 관리 -- 별도 MLOps 플랫폼 영역
- A/B 테스트 플랫폼 -- Phase 2 이후 검토
- 실시간 모델 성능 대시보드 -- 외부 모니터링 도구 연동으로 대체

### Aggregate 4개

| Aggregate | 핵심 책임 | 상태 전이 | 주요 이벤트 |
|-----------|----------|-----------|------------|
| AIModel | 모델 생명주기 관리 | - | RegisteredEvent, RiskClassifiedEvent |
| ModelDeployment | 배포 환경 관리 | Draft -> PendingReview -> Active -> Quarantined -> Decommissioned | ActivatedEvent, QuarantinedEvent |
| ComplianceAssessment | 규제 준수 평가 | Initiated -> InProgress -> Passed/Failed | PassedEvent, FailedEvent |
| ModelIncident | 모델 장애/이슈 추적 | Reported -> Investigating -> Resolved/Escalated | ReportedEvent, ResolvedEvent |

### 사용자 스토리 예시

| ID | 스토리 | 우선순위 |
|----|--------|---------|
| US-001 | AI 거버넌스 관리자로서, 새 AI 모델을 등록하고 싶다, 리스크를 체계적으로 관리하기 위해. | P0 |
| US-002 | AI 거버넌스 관리자로서, 모델을 배포 환경에 등록하고 싶다, 운영 상태를 추적하기 위해. | P0 |
| US-003 | 컴플라이언스 담당자로서, 배포 전 컴플라이언스 평가를 수행하고 싶다, EU AI Act를 준수하기 위해. | P0 |

### 수락 기준 예시

**모델 등록 (RegisterModel):**

정상 시나리오:
```text
Given: 유효한 모델 이름, 버전(SemVer), 용도가 준비됨
When:  관리자가 모델을 등록
Then:  모델이 Minimal 리스크 등급으로 생성되고 RegisteredEvent가 발행됨
```

거부 시나리오:
```text
Given: 모델 이름이 비어있음
When:  관리자가 모델을 등록 시도
Then:  "ModelName is required" 검증 오류가 반환됨
```

### 우선순위

| 우선순위 | 유스케이스 |
|---------|-----------|
| **P0** | RegisterModel, CreateDeployment, ReportIncident, InitiateAssessment |
| **P1** | SubmitForReview, ActivateDeployment, QuarantineDeployment |
| **P2** | 드리프트 감지 자동화, 병렬 컴플라이언스 체크 |

### Open Questions

| ID | 질문 | 카테고리 | 차단 여부 |
|----|------|---------|----------|
| Q-001 | 외부 ML 모니터링 API 스펙 확정 시점은? | engineering | 비차단 |
| Q-002 | EU AI Act 시행 일정에 따른 컴플라이언스 기준 변경 가능성은? | legal | 차단 |

## 핵심 원칙

- **비즈니스 언어로 시작, 기술 언어로 끝내지 않음** -- 유비쿼터스 언어가 코드 네이밍의 원천
- **Non-Goals를 명시하여 스코프 크리프 방지** -- "하지 않을 것"을 합의하는 것이 "할 것"만큼 중요
- **사용자 스토리는 INVEST 기준으로 검증** -- 테스트 불가능한 스토리는 수정
- **수락 기준은 정상 + 거부 시나리오 모두 포함** -- 거부 시나리오가 도메인 규칙의 원천
- **P0부터 시작** -- P0 없으면 출시 불가, P1/P2는 후순위
- **Aggregate 경계는 트랜잭션 일관성 기준** -- 데이터 모델이 아닌 비즈니스 규칙이 경계를 결정
- **금지 상태는 타입 시스템으로 구조적 제거** -- 런타임 검증보다 컴파일 타임 보장 우선
- **Open Questions는 카테고리별 태깅으로 추적** -- 차단/비차단 구분으로 진행 가능 여부 판단

## 참고 자료

- [워크플로](../workflow/) -- 7단계 전체 흐름
- [Architecture Design 스킬](../architecture-design/) -- 다음 단계: 프로젝트 구조 설계
- [Domain Develop 스킬](../domain-develop/) -- Aggregate 상세 설계와 구현
