---
title: "도메인 타입 설계 의사결정"
description: "AI 모델 거버넌스 도메인의 불변식 분류와 Functorium 패턴 매핑"
---

## 개요

[비즈니스 요구사항](../00-business-requirements/)에서 정의한 자연어 요구사항을 DDD 관점에서 분석합니다. 첫 번째 단계는 업무 영역에서 독립적인 일관성 경계(Aggregate)를 식별하고, 두 번째 단계는 각 경계 내의 규칙을 불변식으로 분류하는 것입니다.

## 업무 영역에서 Aggregate 식별

비즈니스 규칙을 타입으로 인코딩하기 전에, 먼저 업무 영역에서 독립적인 일관성 경계를 식별해야 합니다. 4개 업무 주제에서 4개 Aggregate가 도출됩니다.

### 업무 주제 -> Aggregate 매핑

| 업무 주제 | Aggregate | 도출 근거 |
|----------|-----------|----------|
| AI 모델 관리 | AIModel | 모델 고유의 생명주기, 위험 등급 독립 관리 |
| 배포 라이프사이클 | ModelDeployment | 6단계 상태 전이 규칙, 배포별 독립 관리 |
| 컴플라이언스 평가 | ComplianceAssessment | 평가 기준 소유, 점수 계산 일관성 경계 |
| 인시던트 관리 | ModelIncident | 인시던트 고유의 상태 전이, 독립 수명 |
| 교차 도메인 규칙 | -- | Aggregate 간 검증 -> Domain Service |

### 왜 배포를 모델에서 분리하는가?

모델 정보(이름, 목적, 버전) 변경과 배포 상태 전이는 빈도와 동시성 요구가 다릅니다. 하나의 모델에 여러 배포가 존재할 수 있고, 각 배포는 독립적인 상태 전이 규칙을 따릅니다. 모델 아카이브가 배포 트랜잭션을 차단해서는 안 됩니다.

### 왜 평가가 독립 Aggregate인가?

컴플라이언스 평가는 여러 평가 기준(AssessmentCriterion)을 소유하며, 모든 기준 평가 후 점수를 자동 계산합니다. 이 계산 로직은 배포와 독립적이어야 합니다. 배포 상태 변경이 진행 중인 평가에 영향을 주면 안 됩니다.

### Aggregate 분리 이유 종합

| Aggregate | 분리 이유 | 핵심 불변식 |
|-----------|----------|------------|
| **AIModel** | 모델 고유 생명주기, 아카이브/복원 | 아카이브 가드, 모델명 유효성 |
| **ModelDeployment** | 6단계 상태 전이, 배포별 독립 관리 | 상태 전이, 엔드포인트 URL 유효성 |
| **ComplianceAssessment** | 평가 기준 소유, 점수 계산 일관성 | 전체 기준 평가 완료, 점수 자동 계산 |
| **ModelIncident** | 인시던트 고유 상태 전이, 자동 격리 트리거 | 상태 전이, 심각도 기반 격리 |

## 도메인 용어 매핑

비즈니스 용어를 DDD 전술적 패턴으로 매핑합니다.

| 한글 | 영문 | DDD 패턴 | 역할 |
|------|------|---------|------|
| AI 모델 | AIModel | Aggregate Root | 거버넌스 대상, 위험 등급 소유 |
| 배포 | ModelDeployment | Aggregate Root | 배포 상태 관리, 모델 ID 참조 |
| 컴플라이언스 평가 | ComplianceAssessment | Aggregate Root | 평가 기준 소유, 점수 계산 |
| 평가 기준 | AssessmentCriterion | Entity (자식) | 평가 내 개별 기준, Assessment에 종속 |
| 인시던트 | ModelIncident | Aggregate Root | 인시던트 상태 관리 |
| 모델명 | ModelName | Value Object | 100자 이하 문자열 |
| 모델 버전 | ModelVersion | Value Object | SemVer 형식, 정규식 검증 |
| 모델 목적 | ModelPurpose | Value Object | 500자 이하 문자열 |
| 위험 등급 | RiskTier | Value Object (Smart Enum) | 4단계, 도메인 속성 내장 |
| 배포 상태 | DeploymentStatus | Value Object (Smart Enum) | 6단계, 전이 규칙 내장 |
| 배포 환경 | DeploymentEnvironment | Value Object (Smart Enum) | Staging, Production |
| 엔드포인트 URL | EndpointUrl | Value Object | URL 형식 검증 |
| 드리프트 임계값 | DriftThreshold | Value Object | 0.0~1.0 범위, 비교 가능 |
| 평가 점수 | AssessmentScore | Value Object | 0~100 범위, 통과 임계값 내장 |
| 평가 상태 | AssessmentStatus | Value Object (Smart Enum) | 5단계, 전이 규칙 내장 |
| 기준 결과 | CriterionResult | Value Object (Smart Enum) | Pass, Fail, NotApplicable |
| 인시던트 심각도 | IncidentSeverity | Value Object (Smart Enum) | 4단계, 격리 조건 내장 |
| 인시던트 상태 | IncidentStatus | Value Object (Smart Enum) | 4단계, 전이 규칙 내장 |
| 인시던트 설명 | IncidentDescription | Value Object | 2000자 이하 문자열 |
| 해결 노트 | ResolutionNote | Value Object | 2000자 이하 문자열 |
| 위험 분류 서비스 | RiskClassificationService | Domain Service | 키워드 기반 위험 등급 분류 |
| 배포 적격성 서비스 | DeploymentEligibilityService | Domain Service | 교차 Aggregate 적격성 검증 |

## 불변식 분류 체계

이 도메인에서는 6가지 불변식 유형을 식별했습니다.

| 유형 | 범위 | 핵심 질문 |
|------|------|----------|
| 단일 값 | 개별 필드 | 이 값이 항상 유효한가? |
| 구조 | 필드 조합 | 부모-자식 관계에서 파생 값이 일관적인가? |
| 상태 전이 | 시간에 따른 변화 | 허용된 상태 변화만 일어나는가? |
| 수명 | Aggregate 생명주기 | 삭제된 객체에 행위가 차단되는가? |
| 소유 | 자식 엔티티 경계 | 자식이 부모 경계를 벗어나지 않는가? |
| 교차 Aggregate | 여러 Aggregate 간 | 단일 Aggregate로 검증할 수 없는 규칙은 어디서 보장하는가? |

## 불변식별 설계 의사결정

### 1. 단일 값 불변식

개별 필드가 항상 유효한 값만 가져야 하는 제약입니다.

**비즈니스 규칙:**
- "모델명은 100자 이하, 비어있으면 안 된다"
- "모델 버전은 SemVer 형식이어야 한다"
- "모델 목적은 500자 이하, 비어있으면 안 된다"
- "엔드포인트 URL은 유효한 HTTP/HTTPS URL이어야 한다"
- "드리프트 임계값은 0.0~1.0 범위여야 한다"
- "평가 점수는 0~100 범위여야 한다"
- "인시던트 설명은 2000자 이하, 비어있으면 안 된다"

**설계 의사결정:** 생성 시 검증하고 이후 불변으로 보장합니다.

| Naive 필드 | Functorium 타입 | 검증 규칙 |
|-----------|----------------|----------|
| `string Name` | `SimpleValueObject<string>` | NotNull, NotEmpty, Trim, MaxLength(100) |
| `string Version` | `SimpleValueObject<string>` | NotNull, NotEmpty, SemVer 정규식 |
| `string Purpose` | `SimpleValueObject<string>` | NotNull, NotEmpty, Trim, MaxLength(500) |
| `string Url` | `SimpleValueObject<string>` | NotNull, NotEmpty, URI 형식 검증 |
| `decimal DriftThreshold` | `ComparableSimpleValueObject<decimal>` | Between(0.0, 1.0) |
| `int Score` | `ComparableSimpleValueObject<int>` | Between(0, 100) |
| `string Description` | `SimpleValueObject<string>` | NotNull, NotEmpty, Trim, MaxLength(2000) |

### 2. Smart Enum 불변식

열거형 값이 허용된 값만 가져야 하는 제약입니다.

**비즈니스 규칙:**
- "위험 등급은 Minimal, Limited, High, Unacceptable만 허용된다"
- "High 또는 Unacceptable 등급은 컴플라이언스 평가를 요구한다"
- "Unacceptable 등급은 배포가 금지된다"
- "Critical 또는 High 심각도는 배포 자동 격리를 요구한다"

**설계 의사결정:** Smart Enum 패턴으로 도메인 속성을 값 객체에 내장합니다.

| Smart Enum | 값 목록 | 내장 도메인 속성 |
|-----------|--------|-----------------|
| RiskTier | Minimal, Limited, High, Unacceptable | `RequiresComplianceAssessment`, `IsProhibited` |
| IncidentSeverity | Critical, High, Medium, Low | `RequiresQuarantine` |
| CriterionResult | Pass, Fail, NotApplicable | -- |
| DeploymentEnvironment | Staging, Production | -- |

### 3. 상태 전이 불변식

상태가 정해진 규칙에 따라서만 전이되는 제약입니다. Smart Enum + `HashMap` 전이 맵 패턴을 적용합니다.

| Smart Enum | 전이 규칙 | 터미널 상태 |
|-----------|----------|------------|
| DeploymentStatus | Draft->PendingReview, PendingReview->Active/Rejected, Active->Quarantined/Decommissioned, Quarantined->Active/Decommissioned | Decommissioned, Rejected |
| IncidentStatus | Reported->Investigating/Escalated, Investigating->Resolved/Escalated | Resolved, Escalated |
| AssessmentStatus | Initiated->InProgress, InProgress->Passed/Failed/RequiresRemediation | Passed, Failed, RequiresRemediation |

### 4. 수명 불변식

**비즈니스 규칙:** "아카이브된 모델은 수정할 수 없다"

**설계 의사결정:** `ISoftDeletableWithUser` + `DeletedAt.IsSome` 가드 패턴. `ClassifyRisk`, `UpdateVersion`, `Update` 메서드에서 `DeletedAt.IsSome`이면 `AlreadyDeleted` 오류를 반환합니다.

### 5. 소유 불변식

**비즈니스 규칙:** "평가 기준은 컴플라이언스 평가에 종속된다"

**설계 의사결정:** `ComplianceAssessment`가 private `List<AssessmentCriterion>`으로 자식 엔티티를 관리하고, 외부에는 `IReadOnlyList`만 노출합니다. 평가 기준의 생성, 평가, 완료는 모두 부모 Aggregate를 통해서만 가능합니다.

### 6. 교차 Aggregate 불변식

**비즈니스 규칙:**
- "배포 적격성: 금지 등급 확인, 컴플라이언스 평가 확인, 미해결 인시던트 확인"
- "위험 등급 분류: 목적 키워드 기반 자동 분류"

**설계 의사결정:** Domain Service로 구현합니다.

| Domain Service | 교차 대상 | 검증 내용 |
|---------------|----------|----------|
| RiskClassificationService | AIModel (목적) | 키워드 -> 위험 등급 매핑 |
| DeploymentEligibilityService | AIModel, ComplianceAssessment, ModelIncident | 금지 등급, 평가 통과, 인시던트 부재 |

### UnionValueObject 미사용 결정

이 도메인의 Smart Enum은 모두 단일 차원(하나의 문자열 값)입니다. `RiskTier`, `DeploymentStatus` 등은 값마다 다른 필드 구조를 가지지 않으므로 UnionValueObject(discriminated union) 대신 `SimpleValueObject<string>` + `HashMap` 패턴을 선택했습니다. 도메인 속성(`RequiresComplianceAssessment`, `IsProhibited` 등)은 Smart Enum의 인스턴스 메서드/프로퍼티로 충분히 표현됩니다.

## 설계 의사결정 요약표

| 설계 의사결정 | Functorium 타입 | 적용 예 | 보장 효과 |
|---|---|---|---|
| 단일 값 검증 + 불변 + 정규화 | `SimpleValueObject<T>` + `Validate` 체인 | ModelName, ModelVersion, EndpointUrl | 생성 시 검증, Trim 정규화, 빈 문자열 차단 |
| 비교 가능한 단일 값 + 범위 | `ComparableSimpleValueObject<T>` | DriftThreshold, AssessmentScore | 범위 검증, 크기 비교, 도메인 속성(IsPassing) |
| Smart Enum + 도메인 속성 | `SimpleValueObject<string>` + `HashMap` | RiskTier, IncidentSeverity | 유효 값만 허용, 도메인 속성 내장 |
| Smart Enum + 상태 전이 규칙 | `SimpleValueObject<string>` + 전이 맵 | DeploymentStatus, IncidentStatus, AssessmentStatus | 허용된 전이만 가능, 터미널 상태 보장 |
| Aggregate Root 이중 팩토리 | `AggregateRoot<TId>` + `Create`/`CreateFromValidated` | AIModel, ModelDeployment, ComplianceAssessment, ModelIncident | 도메인 생성과 ORM 복원 분리 |
| 자식 엔티티 + 컬렉션 관리 | `Entity<TId>` + private `List` + `IReadOnlyList` | AssessmentCriterion | 외부에서 컬렉션 직접 수정 불가 |
| 교차 Aggregate 비즈니스 규칙 | `IDomainService` | DeploymentEligibilityService, RiskClassificationService | 교차 Aggregate 검증 |
| 쿼리 가능한 도메인 사양 | `ExpressionSpecification<T>` | ModelNameSpec, DeploymentActiveSpec 등 12종 | Expression Tree 기반 조회 |
| 도메인 이벤트 + 도메인 오류 | 중첩 `sealed record` | AIModel.RegisteredEvent, ModelDeployment.InvalidStatusTransition | Aggregate 내부에 이벤트/오류 응집 |
| Soft Delete + 가드 | `ISoftDeletableWithUser` + `DeletedAt.IsSome` | AIModel.ClassifyRisk(), Update() | 아카이브된 Aggregate 변경 차단 |

다음 단계에서는 이 설계 의사결정을 C# 코드로 매핑하여 [코드 설계](../02-code-design/)를 진행합니다.
