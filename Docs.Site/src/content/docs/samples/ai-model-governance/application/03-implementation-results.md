---
title: "Application Implementation Results"
description: "Summary of application layer implementation results for the AI Model Governance Platform"
---

## Use Case Status

### Commands (8 types)

| Command | Input VO Composition | Core Flow | Result |
|---------|-----------|----------|------|
| RegisterModelCommand | ApplyT(Name, Version, Purpose) | VO composition -> risk classification -> model creation | ModelId |
| ClassifyModelRiskCommand | RiskTier.Create | Model lookup -> reclassification -> update | -- |
| CreateDeploymentCommand | ApplyT(Url, Env, Drift) | VO composition -> model confirmation -> deployment creation | DeploymentId |
| SubmitDeploymentForReviewCommand | -- | Deployment/model lookup -> eligibility verification -> submit | -- |
| ActivateDeploymentCommand | -- | Deployment/assessment lookup -> guard passage -> activate | -- |
| QuarantineDeploymentCommand | -- | Deployment lookup -> quarantine | -- |
| InitiateAssessmentCommand | -- | Model/deployment lookup -> assessment creation | AssessmentId |
| ReportIncidentCommand | ApplyT(Severity, Description) | VO composition -> deployment lookup -> incident creation | IncidentId |

### Queries (7 types)

| Query | Port | Filter/Options |
|-------|------|----------|
| GetModelByIdQuery | IModelDetailQuery | Includes deployment/assessment/incident aggregation |
| SearchModelsQuery | IAIModelQuery | Risk tier filter, pagination |
| GetDeploymentByIdQuery | IDeploymentDetailQuery | -- |
| SearchDeploymentsQuery | IDeploymentQuery | Status/environment filter, pagination |
| GetAssessmentByIdQuery | IAssessmentRepository | Includes assessment criteria |
| GetIncidentByIdQuery | IIncidentRepository | -- |
| SearchIncidentsQuery | IIncidentQuery | Severity/status filter, pagination |

### Event Handlers (2 types)

| Event Handler | Trigger Event | Condition | Action |
|--------------|-------------|------|------|
| QuarantineDeploymentOnCriticalIncidentHandler | ModelIncident.ReportedEvent | Severity.RequiresQuarantine | Auto-quarantine deployment |
| InitiateAssessmentOnRiskUpgradeHandler | AIModel.RiskClassifiedEvent | NewRiskTier.RequiresComplianceAssessment | Create assessments for active deployments |

## Port Status

### Command Ports (4 Repository types)

| Port | Base CRUD | Custom Methods |
|------|-----------|-------------|
| IAIModelRepository | GetById, Create, Update, Delete | Exists(spec), GetByIdIncludingDeleted(id) |
| IDeploymentRepository | GetById, Create, Update, Delete | Exists(spec), Find(spec) |
| IAssessmentRepository | GetById, Create, Update, Delete | Exists(spec), Find(spec) |
| IIncidentRepository | GetById, Create, Update, Delete | Exists(spec), Find(spec) |

### Query Ports (5 Read Adapter types)

| Port | Role |
|------|------|
| IAIModelQuery | Model list search |
| IModelDetailQuery | Model detail (including aggregated data) |
| IDeploymentQuery | Deployment list search |
| IDeploymentDetailQuery | Deployment detail |
| IIncidentQuery | Incident list search |

### External Service Ports (4 types)

| Port | Return Type | IO Pattern |
|------|----------|---------|
| IModelHealthCheckService | `FinT<IO, HealthCheckResult>` | Timeout + Catch |
| IModelMonitoringService | `FinT<IO, DriftReport>` | Retry + Schedule |
| IParallelComplianceCheckService | `FinT<IO, ComplianceCheckReport>` | Fork + awaitAll |
| IModelRegistryService | `FinT<IO, ModelRegistryEntry>` | Bracket |

## Application layer structure

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

## Applied Patterns Summary

| Pattern | Application Count | Example |
|------|--------|------|
| ApplyT (VO parallel composition) | 4 | RegisterModel, CreateDeployment, ReportIncident |
| FinT LINQ (from...in) | 8 | All Command Handlers |
| guard (conditional failure) | 1 | ActivateDeploymentCommand |
| MustSatisfyValidation | 8+ | All Validators |
| IDomainEventHandler | 2 | QuarantineDeployment, InitiateAssessment |
| Nested Class | 15 | All Commands/Queries |

**268 tests** run across 2 assemblies for the entire solution. Application layer code is validated together in unit tests (Architecture rules) and integration tests (Endpoint E2E).

In the next step, we define the Adapter layer implementing these ports in [Adapter Technical Requirements](../adapter/00-business-requirements/).
