---
title: "도메인 구현 결과"
description: "AI 모델 거버넌스 도메인 레이어 구현 결과 요약"
---

## 타입 수 현황

### Value Objects (16종)

| 분류 | 타입 | 기반 클래스 | 검증 규칙 |
|------|------|-----------|----------|
| 문자열 VO | ModelName | `SimpleValueObject<string>` | NotNull, NotEmpty, Trim, MaxLength(100) |
| 문자열 VO | ModelVersion | `SimpleValueObject<string>` | NotNull, NotEmpty, SemVer 정규식 |
| 문자열 VO | ModelPurpose | `SimpleValueObject<string>` | NotNull, NotEmpty, Trim, MaxLength(500) |
| 문자열 VO | EndpointUrl | `SimpleValueObject<string>` | NotNull, NotEmpty, URI 형식 |
| 문자열 VO | IncidentDescription | `SimpleValueObject<string>` | NotNull, NotEmpty, Trim, MaxLength(2000) |
| 문자열 VO | ResolutionNote | `SimpleValueObject<string>` | NotNull, NotEmpty, Trim, MaxLength(2000) |
| 비교 가능 VO | DriftThreshold | `ComparableSimpleValueObject<decimal>` | Between(0.0, 1.0) |
| 비교 가능 VO | AssessmentScore | `ComparableSimpleValueObject<int>` | Between(0, 100), IsPassing |
| Smart Enum | RiskTier | `SimpleValueObject<string>` | 4값, RequiresComplianceAssessment, IsProhibited |
| Smart Enum | DeploymentStatus | `SimpleValueObject<string>` | 6값, 전이 맵 |
| Smart Enum | DeploymentEnvironment | `SimpleValueObject<string>` | 2값 (Staging, Production) |
| Smart Enum | AssessmentStatus | `SimpleValueObject<string>` | 5값, 전이 맵 |
| Smart Enum | CriterionResult | `SimpleValueObject<string>` | 3값 (Pass, Fail, NotApplicable) |
| Smart Enum | IncidentSeverity | `SimpleValueObject<string>` | 4값, RequiresQuarantine |
| Smart Enum | IncidentStatus | `SimpleValueObject<string>` | 4값, 전이 맵 |

### Aggregate Roots (4종)

| Aggregate | 인터페이스 | 핵심 패턴 |
|-----------|-----------|----------|
| AIModel | IAuditable, ISoftDeletableWithUser | 이중 팩토리, Soft Delete 가드 |
| ModelDeployment | IAuditable | 이중 팩토리, 상태 전이 |
| ComplianceAssessment | IAuditable | 이중 팩토리, 자식 엔티티 관리 |
| ModelIncident | IAuditable | 이중 팩토리, 상태 전이 |

### Child Entity (1종)

| Entity | 부모 Aggregate | 역할 |
|--------|--------------|------|
| AssessmentCriterion | ComplianceAssessment | 평가 기준, 결과 기록 |

### Domain Services (2종)

| Service | 교차 대상 | 역할 |
|---------|----------|------|
| RiskClassificationService | AIModel | 목적 키워드 -> 위험 등급 분류 |
| DeploymentEligibilityService | AIModel, Assessment, Incident | 배포 적격성 3단계 검증 |

### Specifications (12종)

| Specification | 대상 Aggregate | 용도 |
|---------------|--------------|------|
| ModelNameSpec | AIModel | 모델명 검색 |
| ModelRiskTierSpec | AIModel | 위험 등급 필터 |
| DeploymentByModelSpec | ModelDeployment | 모델별 배포 조회 |
| DeploymentActiveSpec | ModelDeployment | 활성 배포 필터 |
| DeploymentQuarantinedSpec | ModelDeployment | 격리 배포 필터 |
| AssessmentByModelSpec | ComplianceAssessment | 모델별 평가 조회 |
| AssessmentByDeploymentSpec | ComplianceAssessment | 배포별 평가 조회 |
| AssessmentPendingSpec | ComplianceAssessment | 미완료 평가 필터 |
| IncidentByModelSpec | ModelIncident | 모델별 인시던트 조회 |
| IncidentByDeploymentSpec | ModelIncident | 배포별 인시던트 조회 |
| IncidentOpenSpec | ModelIncident | 미해결 인시던트 필터 |
| IncidentBySeveritySpec | ModelIncident | 심각도별 인시던트 필터 |

### Domain Events (18종)

| Aggregate | 이벤트 | 트리거 |
|-----------|--------|-------|
| AIModel | RegisteredEvent | 모델 등록 |
| AIModel | RiskClassifiedEvent | 위험 등급 재분류 |
| AIModel | VersionUpdatedEvent | 버전 업데이트 |
| AIModel | UpdatedEvent | 정보 업데이트 |
| AIModel | ArchivedEvent | 모델 아카이브 |
| AIModel | RestoredEvent | 모델 복원 |
| ModelDeployment | CreatedEvent | 배포 생성 |
| ModelDeployment | SubmittedForReviewEvent | 검토 제출 |
| ModelDeployment | ActivatedEvent | 배포 활성화 |
| ModelDeployment | QuarantinedEvent | 배포 격리 |
| ModelDeployment | RemediatedEvent | 격리 해제 |
| ModelDeployment | DecommissionedEvent | 배포 해제 |
| ComplianceAssessment | CreatedEvent | 평가 생성 |
| ComplianceAssessment | CriterionEvaluatedEvent | 기준 평가 |
| ComplianceAssessment | CompletedEvent | 평가 완료 |
| ModelIncident | ReportedEvent | 인시던트 보고 |
| ModelIncident | InvestigatingEvent | 조사 시작 |
| ModelIncident | ResolvedEvent | 인시던트 해결 |

### Repository Interfaces (4종)

| Repository | 추가 메서드 |
|-----------|-----------|
| IAIModelRepository | `Exists(spec)`, `GetByIdIncludingDeleted(id)` |
| IDeploymentRepository | `Exists(spec)`, `Find(spec)` |
| IAssessmentRepository | `Exists(spec)`, `Find(spec)` |
| IIncidentRepository | `Exists(spec)`, `Find(spec)` |

## 도메인 레이어 구조

```
AiGovernance.Domain/
├── SharedModels/
│   └── Services/
│       ├── RiskClassificationService.cs
│       └── DeploymentEligibilityService.cs
└── AggregateRoots/
    ├── Models/
    │   ├── AIModel.cs
    │   ├── IAIModelRepository.cs
    │   ├── ValueObjects/ (ModelName, ModelVersion, ModelPurpose, RiskTier)
    │   └── Specifications/ (ModelNameSpec, ModelRiskTierSpec)
    ├── Deployments/
    │   ├── ModelDeployment.cs
    │   ├── IDeploymentRepository.cs
    │   ├── ValueObjects/ (DeploymentStatus, DeploymentEnvironment, EndpointUrl, DriftThreshold)
    │   └── Specifications/ (DeploymentByModelSpec, DeploymentActiveSpec, DeploymentQuarantinedSpec)
    ├── Assessments/
    │   ├── ComplianceAssessment.cs
    │   ├── AssessmentCriterion.cs
    │   ├── IAssessmentRepository.cs
    │   ├── ValueObjects/ (AssessmentScore, AssessmentStatus, CriterionResult)
    │   └── Specifications/ (AssessmentByModelSpec, AssessmentByDeploymentSpec, AssessmentPendingSpec)
    └── Incidents/
        ├── ModelIncident.cs
        ├── IIncidentRepository.cs
        ├── ValueObjects/ (IncidentSeverity, IncidentStatus, IncidentDescription, ResolutionNote)
        └── Specifications/ (IncidentByModelSpec, IncidentByDeploymentSpec, IncidentOpenSpec, IncidentBySeveritySpec)
```

## 테스트 현황

단위 테스트는 Value Object, Aggregate, Domain Service, Architecture 네 범주로 구성됩니다. 전체 솔루션 기준 **268개 테스트가** 2개 어셈블리에서 실행됩니다.

| 범주 | 테스트 파일 수 | 테스트 대상 |
|------|-------------|-----------|
| Value Objects | 15 | 16종 VO의 생성, 검증, Smart Enum 전이 규칙 |
| Aggregates | 4 | 4종 Aggregate의 Create, 상태 전이, 가드, CreateFromValidated |
| Domain Services | 1 | RiskClassificationService 키워드 분류 |
| Architecture | 3 | 도메인/애플리케이션 아키텍처 규칙, 레이어 의존성 |
| **합계** | **23** | |

> 268개 테스트에는 단위 테스트(AiGovernance.Tests.Unit)와 통합 테스트(AiGovernance.Tests.Integration) 모두 포함됩니다.

다음 단계에서는 [애플리케이션 비즈니스 요구사항](../application/00-business-requirements/)에서 이 도메인 규칙을 유스케이스로 조율하는 방법을 정의합니다.
