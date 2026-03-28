---
title: "어댑터 구현 결과"
description: "AI 모델 거버넌스 플랫폼의 프로젝트 구조, 엔드포인트 목록, 테스트 현황"
---

## 프로젝트 구조

### Host (AiGovernance)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .RegisterAdapterPresentation()
    .RegisterAdapterPersistence(builder.Configuration)
    .RegisterAdapterInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseAdapterPresentation();
app.Run();
```

3개 Adapter 등록 메서드가 Builder 체인으로 구성됩니다. 각 Adapter는 독립적으로 등록되어 교체 가능합니다.

### 프로젝트 의존성

```
AiGovernance (Host)
├── AiGovernance.Adapters.Presentation  → Application
├── AiGovernance.Adapters.Persistence   → Application, Domain
└── AiGovernance.Adapters.Infrastructure → Application, Domain

AiGovernance.Application → Domain
AiGovernance.Domain → Functorium (Framework)
```

## Adapter 프로젝트별 현황

### AiGovernance.Adapters.Presentation

FastEndpoints 기반 HTTP API를 제공합니다.

| 영역 | 엔드포인트 | HTTP 메서드 | 경로 패턴 |
|------|----------|-----------|----------|
| 모델 | RegisterModelEndpoint | POST | /api/models |
| 모델 | GetModelByIdEndpoint | GET | /api/models/{id} |
| 모델 | SearchModelsEndpoint | GET | /api/models |
| 모델 | ClassifyModelRiskEndpoint | PUT | /api/models/{id}/risk |
| 배포 | CreateDeploymentEndpoint | POST | /api/deployments |
| 배포 | GetDeploymentByIdEndpoint | GET | /api/deployments/{id} |
| 배포 | SearchDeploymentsEndpoint | GET | /api/deployments |
| 배포 | SubmitForReviewEndpoint | POST | /api/deployments/{id}/submit |
| 배포 | ActivateDeploymentEndpoint | POST | /api/deployments/{id}/activate |
| 배포 | QuarantineDeploymentEndpoint | POST | /api/deployments/{id}/quarantine |
| 평가 | InitiateAssessmentEndpoint | POST | /api/assessments |
| 평가 | GetAssessmentByIdEndpoint | GET | /api/assessments/{id} |
| 인시던트 | ReportIncidentEndpoint | POST | /api/incidents |
| 인시던트 | GetIncidentByIdEndpoint | GET | /api/incidents/{id} |
| 인시던트 | SearchIncidentsEndpoint | GET | /api/incidents |

**합계: 15개 엔드포인트** (모델 4, 배포 6, 평가 2, 인시던트 3)

### AiGovernance.Adapters.Persistence

InMemory 구현으로 Repository와 Query를 제공합니다.

| 구현체 | 포트 | Observable 래퍼 |
|--------|------|----------------|
| AIModelRepositoryInMemory | IAIModelRepository | AIModelRepositoryInMemoryObservable |
| DeploymentRepositoryInMemory | IDeploymentRepository | DeploymentRepositoryInMemoryObservable |
| AssessmentRepositoryInMemory | IAssessmentRepository | AssessmentRepositoryInMemoryObservable |
| IncidentRepositoryInMemory | IIncidentRepository | IncidentRepositoryInMemoryObservable |
| UnitOfWorkInMemory | IUnitOfWork | UnitOfWorkInMemoryObservable |
| AIModelQueryInMemory | IAIModelQuery | AIModelQueryInMemoryObservable |
| ModelDetailQueryInMemory | IModelDetailQuery | ModelDetailQueryInMemoryObservable |
| DeploymentQueryInMemory | IDeploymentQuery | DeploymentQueryInMemoryObservable |
| DeploymentDetailQueryInMemory | IDeploymentDetailQuery | DeploymentDetailQueryInMemoryObservable |
| IncidentQueryInMemory | IIncidentQuery | IncidentQueryInMemoryObservable |

**합계: 10개 구현체** (Repository 4, UnitOfWork 1, Query 5)

설정에 따라 Sqlite(EfCore) 구현으로 전환 가능하도록 `PersistenceOptions.Provider`로 분기합니다.

### EfCore 구현 현황

| 구현체 | 포트 | Observable 래퍼 |
|--------|------|----------------|
| AIModelRepositoryEfCore | IAIModelRepository | AIModelRepositoryEfCoreObservable |
| DeploymentRepositoryEfCore | IDeploymentRepository | DeploymentRepositoryEfCoreObservable |
| AssessmentRepositoryEfCore | IAssessmentRepository | AssessmentRepositoryEfCoreObservable |
| IncidentRepositoryEfCore | IIncidentRepository | IncidentRepositoryEfCoreObservable |
| UnitOfWorkEfCore | IUnitOfWork | UnitOfWorkEfCoreObservable |

**EfCore 합계: 5개 구현체** (Repository 4, UnitOfWork 1)

EfCore 구현에서 Query는 InMemory 쿼리를 재사용합니다 (향후 Dapper 쿼리로 교체 가능).

### AiGovernance.Adapters.Infrastructure

외부 서비스(IO 고급 기능)와 파이프라인을 제공합니다.

| 구현체 | 포트 | IO 패턴 |
|--------|------|---------|
| ModelHealthCheckService | IModelHealthCheckService | Timeout(10s) + Catch |
| ModelMonitoringService | IModelMonitoringService | Retry(exponential, 3회) + Catch |
| ParallelComplianceCheckService | IParallelComplianceCheckService | Fork + awaitAll |
| ModelRegistryService | IModelRegistryService | Bracket(Acquire/Use/Release) |

**합계: 4개 외부 서비스**

등록 항목:
- Mediator + `ObservableDomainEventNotificationPublisher`
- FluentValidation (2개 어셈블리)
- OpenTelemetry 3-Pillar
- Pipeline (UseAll + Custom)
- Domain Services (RiskClassificationService, DeploymentEligibilityService)

## 테스트 현황

### 단위 테스트 (AiGovernance.Tests.Unit)

| 범주 | 테스트 파일 수 | 테스트 대상 |
|------|-------------|-----------|
| Value Objects | 15 | 16종 VO의 생성, 검증, Smart Enum 전이 |
| Aggregates | 4 | AIModel, ModelDeployment, ComplianceAssessment, ModelIncident |
| Domain Services | 1 | RiskClassificationService |
| Architecture | 3 | 도메인/애플리케이션 아키텍처 규칙, 레이어 의존성 |
| **합계** | **23** | |

### 통합 테스트 (AiGovernance.Tests.Integration)

| 범주 | 테스트 파일 수 | 테스트 대상 |
|------|-------------|-----------|
| Models | 3 | Register, GetById, Search 엔드포인트 |
| Deployments | 2 | Create, Workflow(전체 라이프사이클) 엔드포인트 |
| Assessments | 1 | Initiate 엔드포인트 |
| Incidents | 1 | Report 엔드포인트 |
| **합계** | **7** | |

**전체 테스트 파일: 30개** (전체 솔루션 기준 **268개 테스트가** 2개 어셈블리에서 실행됩니다)

## 전체 프로젝트 구조

```
samples/ai-model-governance/
├── ai-model-governance.slnx
├── Directory.Build.props
├── Directory.Build.targets
├── domain/                                    # 도메인 레이어 문서 (4개)
├── application/                               # 애플리케이션 레이어 문서 (4개)
├── adapter/                                   # 어댑터 레이어 문서 (4개)
├── Src/
│   ├── AiGovernance.Domain/                   # 도메인 레이어
│   │   ├── SharedModels/Services/             # Domain Services (2종)
│   │   └── AggregateRoots/                    # Aggregates (4종)
│   │       ├── Models/                        # AIModel + VOs(4) + Specs(2)
│   │       ├── Deployments/                   # ModelDeployment + VOs(4) + Specs(3)
│   │       ├── Assessments/                   # ComplianceAssessment + Child Entity + VOs(3) + Specs(3)
│   │       └── Incidents/                     # ModelIncident + VOs(4) + Specs(4)
│   ├── AiGovernance.Application/              # 애플리케이션 레이어
│   │   └── Usecases/                          # Commands(8) + Queries(7) + EventHandlers(2) + Ports(9)
│   ├── AiGovernance.Adapters.Persistence/     # 영속성 어댑터
│   │   ├── Repositories/                      # Aggregate별 Repository/Query 구현 (10종)
│   │   └── Registrations/
│   ├── AiGovernance.Adapters.Infrastructure/  # 인프라 어댑터
│   │   ├── ExternalServices/                  # IO 고급 기능 (4종)
│   │   └── Registrations/
│   ├── AiGovernance.Adapters.Presentation/    # 프레젠테이션 어댑터
│   │   ├── Endpoints/                         # FastEndpoints (15종)
│   │   └── Registrations/
│   └── AiGovernance/                          # Host
│       └── Program.cs
└── Tests/
    ├── AiGovernance.Tests.Unit/               # 단위 테스트 (23개 파일)
    └── AiGovernance.Tests.Integration/        # 통합 테스트 (7개 파일)
```

## 수치 요약

| 항목 | 수량 |
|------|------|
| Aggregate Root | 4 |
| Child Entity | 1 |
| Value Object | 16 (문자열 6, 비교 가능 2, Smart Enum 8) |
| Domain Service | 2 |
| Specification | 12 |
| Domain Event | 18 |
| Command | 8 |
| Query | 7 |
| Event Handler | 2 |
| Repository (Port) | 4 |
| Query Port | 5 |
| External Service Port | 4 |
| HTTP Endpoint | 15 |
| InMemory 구현체 | 10 |
| IO 고급 패턴 | 4 (Timeout, Retry, Fork, Bracket) |
| Observable Port | 19 (InMemory 5, EfCore 5, Query 5, ExternalService 4) |
| 단위 테스트 파일 | 23 |
| 통합 테스트 파일 | 7 |
| **총 테스트 파일** | **30** |
| **총 테스트 수** | **268** |
