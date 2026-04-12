---
title: "Adapter Implementation Results"
description: "Project structure, endpoint list, and test status for the AI Model Governance Platform"
---

## Project Structure

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

Three Adapter registration methods are composed via Builder chaining. Each Adapter is registered independently and is replaceable.

### Project Dependencies

```
AiGovernance (Host)
├── AiGovernance.Adapters.Presentation  → Application
├── AiGovernance.Adapters.Persistence   → Application, Domain
└── AiGovernance.Adapters.Infrastructure → Application, Domain

AiGovernance.Application → Domain
AiGovernance.Domain → Functorium (Framework)
```

## Adapter Project Status

### AiGovernance.Adapters.Presentation

Provides HTTP API based on FastEndpoints.

| Area | Endpoint | HTTP Method | Route Pattern |
|------|----------|-----------|----------|
| Model | RegisterModelEndpoint | POST | /api/models |
| Model | GetModelByIdEndpoint | GET | /api/models/{id} |
| Model | SearchModelsEndpoint | GET | /api/models |
| Model | ClassifyModelRiskEndpoint | PUT | /api/models/{id}/risk |
| Deployment | CreateDeploymentEndpoint | POST | /api/deployments |
| Deployment | GetDeploymentByIdEndpoint | GET | /api/deployments/{id} |
| Deployment | SearchDeploymentsEndpoint | GET | /api/deployments |
| Deployment | SubmitForReviewEndpoint | POST | /api/deployments/{id}/submit |
| Deployment | ActivateDeploymentEndpoint | POST | /api/deployments/{id}/activate |
| Deployment | QuarantineDeploymentEndpoint | POST | /api/deployments/{id}/quarantine |
| Assessment | InitiateAssessmentEndpoint | POST | /api/assessments |
| Assessment | GetAssessmentByIdEndpoint | GET | /api/assessments/{id} |
| Incident | ReportIncidentEndpoint | POST | /api/incidents |
| Incident | GetIncidentByIdEndpoint | GET | /api/incidents/{id} |
| Incident | SearchIncidentsEndpoint | GET | /api/incidents |

**Total: 15 endpoints** (Model 4, Deployment 6, Assessment 2, Incident 3)

### AiGovernance.Adapters.Persistence

Provides Repository and Query implementations via InMemory.

| Implementation | Port | Observable Wrapper |
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

**Total: 10 implementations** (Repository 4, UnitOfWork 1, Query 5)

Supports switching to Sqlite (EF Core) implementation via `PersistenceOptions.Provider` branching.

### EF Core Implementation Status

| Implementation | Port | Observable Wrapper |
|--------|------|----------------|
| AIModelRepositoryEfCore | IAIModelRepository | AIModelRepositoryEfCoreObservable |
| DeploymentRepositoryEfCore | IDeploymentRepository | DeploymentRepositoryEfCoreObservable |
| AssessmentRepositoryEfCore | IAssessmentRepository | AssessmentRepositoryEfCoreObservable |
| IncidentRepositoryEfCore | IIncidentRepository | IncidentRepositoryEfCoreObservable |
| UnitOfWorkEfCore | IUnitOfWork | UnitOfWorkEfCoreObservable |

**EF Core total: 5 implementations** (Repository 4, UnitOfWork 1)

In the EF Core implementation, Queries reuse InMemory queries (replaceable with Dapper queries in the future).

### AiGovernance.Adapters.Infrastructure

Provides external services (IO advanced features) and pipelines.

| Implementation | Port | IO Pattern |
|--------|------|---------|
| ModelHealthCheckService | IModelHealthCheckService | Timeout(10s) + Catch |
| ModelMonitoringService | IModelMonitoringService | Retry(exponential, 3 times) + Catch |
| ParallelComplianceCheckService | IParallelComplianceCheckService | Fork + awaitAll |
| ModelRegistryService | IModelRegistryService | Bracket(Acquire/Use/Release) |

**Total: 4 external services**

Registration items:
- Mediator + `ObservableDomainEventNotificationPublisher`
- FluentValidation (2 assemblies)
- OpenTelemetry 3-Pillar
- Pipeline (UseObservability + UseValidation + UseException + Custom)
- Domain Services (RiskClassificationService, DeploymentEligibilityService)

## Test Status

### Unit tests (AiGovernance.Tests.Unit)

| Category | Test File Count | Test Target |
|------|-------------|-----------|
| Value Objects | 15 | Creation, validation, Smart Enum transitions for 16 VOs |
| Aggregates | 4 | AIModel, ModelDeployment, ComplianceAssessment, ModelIncident |
| Domain Services | 1 | RiskClassificationService |
| Architecture | 3 | Domain/application architecture rules, layer dependencies |
| **Total** | **23** | |

### Integration tests (AiGovernance.Tests.Integration)

| Category | Test File Count | Test Target |
|------|-------------|-----------|
| Models | 3 | Register, GetById, Search endpoints |
| Deployments | 2 | Create, Workflow (full lifecycle) endpoints |
| Assessments | 1 | Initiate endpoint |
| Incidents | 1 | Report endpoint |
| **Total** | **7** | |

**Total test files: 30** (**268 tests** across 2 assemblies for the entire solution)

## Complete Project Structure

```
samples/ai-model-governance/
├── ai-model-governance.slnx
├── Directory.Build.props
├── Directory.Build.targets
├── domain/                                    # Domain layer docs (4)
├── application/                               # Application layer docs (4)
├── adapter/                                   # Adapter layer docs (4)
├── Src/
│   ├── AiGovernance.Domain/                   # Domain layer
│   │   ├── SharedModels/Services/             # Domain Services (2 types)
│   │   └── AggregateRoots/                    # Aggregates (4 types)
│   │       ├── Models/                        # AIModel + VOs(4) + Specs(2)
│   │       ├── Deployments/                   # ModelDeployment + VOs(4) + Specs(3)
│   │       ├── Assessments/                   # ComplianceAssessment + Child Entity + VOs(3) + Specs(3)
│   │       └── Incidents/                     # ModelIncident + VOs(4) + Specs(4)
│   ├── AiGovernance.Application/              # Application layer
│   │   └── Usecases/                          # Commands(8) + Queries(7) + EventHandlers(2) + Ports(9)
│   ├── AiGovernance.Adapters.Persistence/     # Persistence adapter
│   │   ├── Repositories/                      # Per-Aggregate Repository/Query implementations (10 types)
│   │   └── Registrations/
│   ├── AiGovernance.Adapters.Infrastructure/  # Infrastructure adapter
│   │   ├── ExternalServices/                  # IO advanced features (4 types)
│   │   └── Registrations/
│   ├── AiGovernance.Adapters.Presentation/    # Presentation adapter
│   │   ├── Endpoints/                         # FastEndpoints (15 types)
│   │   └── Registrations/
│   └── AiGovernance/                          # Host
│       └── Program.cs
└── Tests/
    ├── AiGovernance.Tests.Unit/               # Unit tests (23 files)
    └── AiGovernance.Tests.Integration/        # Integration tests (7 files)
```

## Numerical Summary

| Item | Count |
|------|------|
| Aggregate Root | 4 |
| Child Entity | 1 |
| Value Object | 16 (String 6, Comparable 2, Smart Enum 8) |
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
| InMemory Implementation | 10 |
| Advanced IO Pattern | 4 (Timeout, Retry, Fork, Bracket) |
| Observable Port | 19 (InMemory 5, EfCore 5, Query 5, ExternalService 4) |
| Unit test files | 23 |
| Integration test files | 7 |
| **Total test files** | **30** |
| **Total tests** | **268** |
