---
title: "Application Implementation Results"
description: "Summary of application layer implementation results for the AI Model Governance Platform"
---

## Use Case 현황

### Commands (8종)

| Command | 입력 VO 합성 | 핵심 흐름 | 결과 |
|---------|-----------|----------|------|
| RegisterModelCommand | ApplyT(Name, Version, Purpose) | VO 합성 -> 위험 분류 -> 모델 생성 | ModelId |
| ClassifyModelRiskCommand | RiskTier.Create | 모델 조회 -> 재분류 -> 업데이트 | -- |
| CreateDeploymentCommand | ApplyT(Url, Env, Drift) | VO 합성 -> 모델 확인 -> 배포 생성 | DeploymentId |
| SubmitDeploymentForReviewCommand | -- | 배포/모델 조회 -> 적격성 검증 -> 제출 | -- |
| ActivateDeploymentCommand | -- | 배포/평가 조회 -> guard 통과 -> 활성화 | -- |
| QuarantineDeploymentCommand | -- | 배포 조회 -> 격리 | -- |
| InitiateAssessmentCommand | -- | 모델/배포 조회 -> 평가 생성 | AssessmentId |
| ReportIncidentCommand | ApplyT(Severity, Description) | VO 합성 -> 배포 조회 -> 인시던트 생성 | IncidentId |

### Queries (7종)

| Query | 포트 | 필터/옵션 |
|-------|------|----------|
| GetModelByIdQuery | IModelDetailQuery | 배포/평가/인시던트 집계 포함 |
| SearchModelsQuery | IAIModelQuery | 위험 등급 필터, 페이지네이션 |
| GetDeploymentByIdQuery | IDeploymentDetailQuery | -- |
| SearchDeploymentsQuery | IDeploymentQuery | 상태/환경 필터, 페이지네이션 |
| GetAssessmentByIdQuery | IAssessmentRepository | 평가 기준 포함 |
| GetIncidentByIdQuery | IIncidentRepository | -- |
| SearchIncidentsQuery | IIncidentQuery | 심각도/상태 필터, 페이지네이션 |

### Event Handlers (2종)

| Event Handler | 트리거 이벤트 | 조건 | 동작 |
|--------------|-------------|------|------|
| QuarantineDeploymentOnCriticalIncidentHandler | ModelIncident.ReportedEvent | Severity.RequiresQuarantine | 배포 자동 격리 |
| InitiateAssessmentOnRiskUpgradeHandler | AIModel.RiskClassifiedEvent | NewRiskTier.RequiresComplianceAssessment | 활성 배포에 평가 생성 |

## 포트 현황

### Command 포트 (Repository 4종)

| 포트 | 기본 CRUD | 커스텀 메서드 |
|------|-----------|-------------|
| IAIModelRepository | GetById, Create, Update, Delete | Exists(spec), GetByIdIncludingDeleted(id) |
| IDeploymentRepository | GetById, Create, Update, Delete | Exists(spec), Find(spec) |
| IAssessmentRepository | GetById, Create, Update, Delete | Exists(spec), Find(spec) |
| IIncidentRepository | GetById, Create, Update, Delete | Exists(spec), Find(spec) |

### Query 포트 (Read Adapter 5종)

| 포트 | 역할 |
|------|------|
| IAIModelQuery | 모델 목록 검색 |
| IModelDetailQuery | 모델 상세 (집계 데이터 포함) |
| IDeploymentQuery | 배포 목록 검색 |
| IDeploymentDetailQuery | 배포 상세 |
| IIncidentQuery | 인시던트 목록 검색 |

### 외부 서비스 포트 (4종)

| 포트 | 반환 타입 | IO 패턴 |
|------|----------|---------|
| IModelHealthCheckService | `FinT<IO, HealthCheckResult>` | Timeout + Catch |
| IModelMonitoringService | `FinT<IO, DriftReport>` | Retry + Schedule |
| IParallelComplianceCheckService | `FinT<IO, ComplianceCheckReport>` | Fork + awaitAll |
| IModelRegistryService | `FinT<IO, ModelRegistryEntry>` | Bracket |

## 애플리케이션 레이어 구조

```
AiGovernance.Application/
├── Using.cs
├── AssemblyReference.cs
└── Usecases/
    ├── Models/
    │   ├── IAIModelQuery.cs
    │   ├── IModelDetailQuery.cs
    │   ├── Commands/
    │   │   ├── RegisterModelCommand.cs
    │   │   └── ClassifyModelRiskCommand.cs
    │   └── Queries/
    │       ├── GetModelByIdQuery.cs
    │       └── SearchModelsQuery.cs
    ├── Deployments/
    │   ├── IDeploymentQuery.cs
    │   ├── IDeploymentDetailQuery.cs
    │   ├── IModelHealthCheckService.cs
    │   ├── IModelMonitoringService.cs
    │   ├── Commands/
    │   │   ├── CreateDeploymentCommand.cs
    │   │   ├── SubmitDeploymentForReviewCommand.cs
    │   │   ├── ActivateDeploymentCommand.cs
    │   │   └── QuarantineDeploymentCommand.cs
    │   └── Queries/
    │       ├── GetDeploymentByIdQuery.cs
    │       └── SearchDeploymentsQuery.cs
    ├── Assessments/
    │   ├── Commands/
    │   │   └── InitiateAssessmentCommand.cs
    │   ├── Queries/
    │   │   └── GetAssessmentByIdQuery.cs
    │   └── EventHandlers/
    │       └── InitiateAssessmentOnRiskUpgradeHandler.cs
    └── Incidents/
        ├── IIncidentQuery.cs
        ├── Commands/
        │   └── ReportIncidentCommand.cs
        ├── Queries/
        │   ├── GetIncidentByIdQuery.cs
        │   └── SearchIncidentsQuery.cs
        └── EventHandlers/
            └── QuarantineDeploymentOnCriticalIncidentHandler.cs
```

## 적용 패턴 요약

| 패턴 | 적용 수 | 예시 |
|------|--------|------|
| ApplyT (VO 병렬 합성) | 4 | RegisterModel, CreateDeployment, ReportIncident |
| FinT LINQ (from...in) | 8 | 모든 Command Handler |
| guard (조건부 실패) | 1 | ActivateDeploymentCommand |
| MustSatisfyValidation | 8+ | 모든 Validator |
| IDomainEventHandler | 2 | QuarantineDeployment, InitiateAssessment |
| Nested Class | 15 | 모든 Command/Query |

전체 솔루션 기준 **268개 테스트가** 2개 어셈블리에서 실행됩니다. Application 레이어 코드는 단위 테스트(Architecture 규칙)와 통합 테스트(Endpoint E2E)에서 함께 검증됩니다.

다음 단계에서는 [어댑터 기술 요구사항](../adapter/00-business-requirements/)에서 이 포트를 구현하는 Adapter 레이어를 정의합니다.
