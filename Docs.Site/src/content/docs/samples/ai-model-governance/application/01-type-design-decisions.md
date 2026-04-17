---
title: "Application Type Design Decisions"
description: "Port design, ApplyT pattern, and FinT<IO> composition for the AI Model Governance Platform"
---

## Overview

This document analyzes the workflow rules defined in [application business requirements](../00-business-requirements/) to identify Use Cases (Command/Query) and ports. It determines which ports each Use Case uses and in what order it orchestrates them.

## Command Identification

| Command | Input | Core Flow | Result |
|---------|------|----------|------|
| RegisterModelCommand | Name, Version, Purpose | VO composition -> risk classification -> model creation -> save | ModelId |
| ClassifyModelRiskCommand | ModelId, RiskTier | Model lookup -> reclassification -> update | -- |
| CreateDeploymentCommand | ModelId, Url, Env, Drift | VO composition -> model confirmation -> deployment creation -> save | DeploymentId |
| SubmitDeploymentForReviewCommand | DeploymentId | Deployment lookup -> model lookup -> eligibility verification -> submit -> save | -- |
| ActivateDeploymentCommand | DeploymentId, AssessmentId | Deployment lookup -> assessment lookup -> passage confirmation -> activate -> save | -- |
| QuarantineDeploymentCommand | DeploymentId, Reason | Deployment lookup -> quarantine -> save | -- |
| InitiateAssessmentCommand | ModelId, DeploymentId | Model lookup -> deployment lookup -> assessment creation -> save | AssessmentId |
| ReportIncidentCommand | DeploymentId, Severity, Desc | VO composition -> deployment lookup -> incident creation -> save | IncidentId |

## Query Identification

| Query | Input | Result | Port |
|-------|------|------|------|
| GetModelByIdQuery | ModelId | Model detail (including deployments/assessments/incidents) | IModelDetailQuery |
| SearchModelsQuery | RiskTier?, Page, Size | Model list | IAIModelQuery |
| GetDeploymentByIdQuery | DeploymentId | Deployment detail | IDeploymentDetailQuery |
| SearchDeploymentsQuery | Status?, Env?, Page, Size | Deployment list | IDeploymentQuery |
| GetAssessmentByIdQuery | AssessmentId | Assessment detail (including criteria) | IAssessmentRepository |
| GetIncidentByIdQuery | IncidentId | Incident detail | IIncidentRepository |
| SearchIncidentsQuery | Severity?, Status?, Page, Size | Incident list | IIncidentQuery |

## Event Handler Identification

| Event Handler | Trigger Event | Action |
|--------------|-------------|------|
| QuarantineDeploymentOnCriticalIncidentHandler | ModelIncident.ReportedEvent | Auto-quarantine deployment on Critical/High severity |
| InitiateAssessmentOnRiskUpgradeHandler | AIModel.RiskClassifiedEvent | Create assessments for active deployments on High/Unacceptable upgrade |

## Port Design

Ports are interfaces through which the Application Layer communicates with the outside world. Command ports (Repository) are write-only interfaces for state changes and lookups, and Query ports (Read Adapter) are read-only interfaces. This separation follows CQRS principles, allowing independent optimization paths for writes and reads.

External service ports express LanguageExt IO advanced features (Timeout, Retry, Fork, Bracket) through the return type `FinT<IO, T>`, enabling natural composition into the Application Layer's FinT LINQ chain.

### Command Ports (Repository)

| Port | Base CRUD | Custom Methods |
|------|-----------|-------------|
| IAIModelRepository | IRepository base | Exists(spec), GetByIdIncludingDeleted(id) |
| IDeploymentRepository | IRepository base | Exists(spec), Find(spec) |
| IAssessmentRepository | IRepository base | Exists(spec), Find(spec) |
| IIncidentRepository | IRepository base | Exists(spec), Find(spec) |

### Query Ports (Read Adapter)

| Port | Role |
|------|------|
| IAIModelQuery | Model list search (filter, pagination) |
| IModelDetailQuery | Model detail lookup (deployment/assessment/incident aggregation) |
| IDeploymentQuery | Deployment list search |
| IDeploymentDetailQuery | Deployment detail lookup |
| IIncidentQuery | Incident list search |

### External Service Ports

| Port | Role | IO Pattern |
|------|------|---------|
| IModelHealthCheckService | Model health check | Timeout + Catch |
| IModelMonitoringService | Model drift monitoring | Retry + Schedule |
| IParallelComplianceCheckService | Parallel compliance check | Fork + awaitAll |
| IModelRegistryService | External registry lookup | Bracket |

## VO Composition Pattern: ApplyT

Validates multiple Value Objects simultaneously and **collects all errors at once.** Does not stop at the first error; runs all validations.

```csharp
// RegisterModelCommand.Usecase.Handle()
from vos in (
    ModelName.Create(request.Name),
    ModelVersion.Create(request.Version),
    ModelPurpose.Create(request.Purpose)
).ApplyT((name, version, purpose) => (Name: name, Version: version, Purpose: purpose))
```

`ApplyT` composes each `Fin<T>`'s validation result applicatively. If 2 out of 3 fail, both errors are collected.

## FinT<IO, T> LINQ Composition

The core pattern of Command Handlers is `FinT<IO, T>` LINQ composition. It expresses IO effects (database lookups, saves) and failure possibilities (domain errors) in a single chain.

```csharp
// SubmitDeploymentForReviewCommand.Usecase.Handle()
FinT<IO, Response> usecase =
    from deployment in _deploymentRepository.GetById(deploymentId)
    from model in _modelRepository.GetById(deployment.ModelId)
    from _1 in _eligibilityService.ValidateEligibility(
        model, _assessmentRepository, _incidentRepository)
    from _2 in deployment.SubmitForReview()
    from updated in _deploymentRepository.Update(deployment)
    select new Response();
```

Each `from` clause executes only when the previous step succeeds. If `ValidateEligibility` returns a `ProhibitedModel` error, `SubmitForReview` is not executed.

## Nested Class Encapsulation

All Use Cases encapsulate Request, Response, Validator, and Usecase within a single outer class using the Nested Class pattern.

```csharp
public sealed class RegisterModelCommand
{
    public sealed record Request(...) : ICommandRequest<Response>;
    public sealed record Response(string ModelId);

    public sealed class Validator : AbstractValidator<Request> { ... }

    public sealed class Usecase(...) : ICommandUsecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(...) { ... }
    }
}
```

Advantages of this pattern:
- All components of a Use Case are cohesive in one file
- Request/Response types are clear at the Use Case namespace level
- Validator is located in the same scope as Request, making rule tracing easy

## FluentValidation + MustSatisfyValidation

Validators directly reuse domain VO `Validate` methods.

```csharp
public sealed class Validator : AbstractValidator<Request>
{
    public Validator()
    {
        RuleFor(x => x.Name).MustSatisfyValidation(ModelName.Validate);
        RuleFor(x => x.Version).MustSatisfyValidation(ModelVersion.Validate);
        RuleFor(x => x.Purpose).MustSatisfyValidation(ModelPurpose.Validate);
    }
}
```

`MustSatisfyValidation` converts a VO's `Validate` method into a FluentValidation rule. Domain validation rules are defined in one place and reused in the Application Layer.

Smart Enum type validation uses `MustSatisfyValidationOf`:

```csharp
RuleFor(x => x.RiskTier)
    .MustSatisfyValidationOf<Request, string, RiskTier>(RiskTier.Validate);
```

Entity ID validation uses `MustBeEntityId`:

```csharp
RuleFor(x => x.ModelId).MustBeEntityId<Request, AIModelId>();
```

In the next step, we map this type design to C# code in [Code Design](../02-code-design/).
