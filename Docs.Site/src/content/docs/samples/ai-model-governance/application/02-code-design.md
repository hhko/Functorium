---
title: "Application Code Design"
description: "Core Command code and event handler patterns for the AI Model Governance Platform"
---

Implements the Use Cases, ports, and composition patterns identified in [Type Design Decisions](../01-type-design-decisions/) in C# code.

## Core Command Patterns

### 1. RegisterModelCommand -- VO Composition + Domain Service + Aggregate Creation

Model registration is the most representative Command pattern. VO composition (ApplyT), Domain Service call (risk classification), Aggregate creation, and saving are composed in a single FinT LINQ chain.

```csharp
public sealed class RegisterModelCommand
{
    public sealed record Request(
        string Name, string Version, string Purpose) : ICommandRequest<Response>;

    public sealed record Response(string ModelId);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name).MustSatisfyValidation(ModelName.Validate);
            RuleFor(x => x.Version).MustSatisfyValidation(ModelVersion.Validate);
            RuleFor(x => x.Purpose).MustSatisfyValidation(ModelPurpose.Validate);
        }
    }

    public sealed class Usecase(
        IAIModelRepository modelRepository,
        RiskClassificationService riskClassificationService)
        : ICommandUsecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(
            Request request, CancellationToken cancellationToken)
        {
            FinT<IO, Response> usecase =
                from vos in (
                    ModelName.Create(request.Name),
                    ModelVersion.Create(request.Version),
                    ModelPurpose.Create(request.Purpose)
                ).ApplyT((name, version, purpose) =>
                    (Name: name, Version: version, Purpose: purpose))
                from riskTier in _riskClassificationService
                    .ClassifyByPurpose(vos.Purpose)
                let model = AIModel.Create(
                    vos.Name, vos.Version, vos.Purpose, riskTier)
                from saved in _modelRepository.Create(model)
                select new Response(saved.Id.ToString());

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
```

Flow analysis:
1. `ApplyT`: Validates 3 VOs simultaneously, collects all errors
2. `from riskTier`: Domain Service classifies risk tier by purpose keywords
3. `let model`: Aggregate creation (always succeeds, `let` not `Fin`)
4. `from saved`: Save to Repository (IO effect)

### 2. SubmitDeploymentForReviewCommand -- Cross-Aggregate Validation

Deployment review submission performs cross-validation of 3 Aggregates through `DeploymentEligibilityService`.

```csharp
public sealed class Usecase(
    IDeploymentRepository deploymentRepository,
    IAIModelRepository modelRepository,
    IAssessmentRepository assessmentRepository,
    IIncidentRepository incidentRepository,
    DeploymentEligibilityService eligibilityService)
    : ICommandUsecase<Request, Response>
{
    public async ValueTask<FinResponse<Response>> Handle(
        Request request, CancellationToken cancellationToken)
    {
        var deploymentId = ModelDeploymentId.Create(request.DeploymentId);

        FinT<IO, Response> usecase =
            from deployment in _deploymentRepository.GetById(deploymentId)
            from model in _modelRepository.GetById(deployment.ModelId)
            from _1 in _eligibilityService.ValidateEligibility(
                model, _assessmentRepository, _incidentRepository)
            from _2 in deployment.SubmitForReview()
            from updated in _deploymentRepository.Update(deployment)
            select new Response();

        Fin<Response> response = await usecase.Run().RunAsync();
        return response.ToFinResponse();
    }
}
```

Flow analysis:
1. `from deployment`: Look up deployment
2. `from model`: Look up model referenced by deployment
3. `from _1`: Eligibility verification (prohibited tier, compliance, incidents) -- short-circuits on failure
4. `from _2`: Deployment state transition (Draft -> PendingReview) -- short-circuits on failure
5. `from updated`: Save

### 3. ActivateDeploymentCommand -- guard Pattern

Deployment activation uses `guard` to check assessment passage.

```csharp
FinT<IO, Response> usecase =
    from deployment in _deploymentRepository.GetById(deploymentId)
    from assessment in _assessmentRepository.GetById(assessmentId)
    from _ in guard(assessment.Status == AssessmentStatus.Passed,
        ApplicationError.For<ActivateDeploymentCommand>(
            new BusinessRuleViolated(),
            request.AssessmentId,
            $"Assessment '{request.AssessmentId}' has not passed"))
    from _2 in deployment.Activate()
    from updated in _deploymentRepository.Update(deployment)
    select new Response();
```

`guard` fails immediately with the specified error when the condition is `false`. This is a pattern for expressing cross-validation rules that do not belong to a domain Aggregate in the Application Layer.

### 4. CreateDeploymentCommand -- Model Existence Confirmation + VO Composition

```csharp
FinT<IO, Response> usecase =
    from vos in (
        EndpointUrl.Create(request.EndpointUrl),
        DeploymentEnvironment.Create(request.Environment),
        DriftThreshold.Create(request.DriftThreshold)
    ).ApplyT((url, env, drift) => (Url: url, Env: env, Drift: drift))
    from model in _modelRepository.GetById(modelId)
    let deployment = ModelDeployment.Create(
        model.Id, vos.Url, vos.Env, vos.Drift)
    from saved in _deploymentRepository.Create(deployment)
    select new Response(saved.Id.ToString());
```

### 5. ReportIncidentCommand -- VO Composition + Deployment Reference

```csharp
FinT<IO, Response> usecase =
    from vos in (
        IncidentSeverity.Create(request.Severity),
        IncidentDescription.Create(request.Description)
    ).ApplyT((severity, description) =>
        (Severity: severity, Description: description))
    from deployment in _deploymentRepository.GetById(deploymentId)
    let incident = ModelIncident.Create(
        deployment.Id, deployment.ModelId,
        vos.Severity, vos.Description)
    from saved in _incidentRepository.Create(incident)
    select new Response(saved.Id.ToString());
```

## Event Handler Patterns

### QuarantineDeploymentOnCriticalIncidentHandler

Automatically quarantines deployments when Critical/High severity incidents occur. Utilizes the `IncidentSeverity.RequiresQuarantine` domain property.

```csharp
public sealed class QuarantineDeploymentOnCriticalIncidentHandler(
    IDeploymentRepository deploymentRepository)
    : IDomainEventHandler<ModelIncident.ReportedEvent>
{
    public async ValueTask Handle(
        ModelIncident.ReportedEvent notification,
        CancellationToken cancellationToken)
    {
        if (!notification.Severity.RequiresQuarantine)
            return;

        var result = await _deploymentRepository
            .GetById(notification.DeploymentId)
            .Run().RunAsync();

        if (result.IsFail) return;

        var deployment = result.Unwrap();
        var quarantineResult = deployment.Quarantine(
            $"Auto-quarantined due to {notification.Severity} incident");

        if (quarantineResult.IsSucc)
            await _deploymentRepository.Update(deployment).Run().RunAsync();
    }
}
```

Event handler characteristics:
- Does not roll back the entire transaction on failure (eventual consistency)
- Determines conditions via `RequiresQuarantine` domain property
- Ignores quarantine failure (e.g., already Decommissioned)

### InitiateAssessmentOnRiskUpgradeHandler

Automatically creates compliance assessments for active deployments on risk tier upgrade. Utilizes Specification composition (`&` operator).

```csharp
public sealed class InitiateAssessmentOnRiskUpgradeHandler(
    IDeploymentRepository deploymentRepository,
    IAssessmentRepository assessmentRepository)
    : IDomainEventHandler<AIModel.RiskClassifiedEvent>
{
    public async ValueTask Handle(
        AIModel.RiskClassifiedEvent notification,
        CancellationToken cancellationToken)
    {
        if (!notification.NewRiskTier.RequiresComplianceAssessment)
            return;

        var spec = new DeploymentByModelSpec(notification.ModelId)
                 & new DeploymentActiveSpec();
        var deploymentsResult = await _deploymentRepository
            .Find(spec).Run().RunAsync();

        if (deploymentsResult.IsFail) return;

        foreach (var deployment in deploymentsResult.Unwrap())
        {
            var assessment = ComplianceAssessment.Create(
                notification.ModelId, deployment.Id,
                notification.NewRiskTier);
            await _assessmentRepository.Create(assessment).Run().RunAsync();
        }
    }
}
```

Key points:
- Determines conditions via `RequiresComplianceAssessment` domain property
- `DeploymentByModelSpec & DeploymentActiveSpec`: Specification composition queries "active deployments for the given model"
- Creates assessments independently for each active deployment

## Command vs Event Handler Execution Model Comparison

| Aspect | Command Handler | Event Handler |
|------|----------------|---------------|
| Trigger | External request | Domain event |
| Return type | `FinResponse<Response>` | `void` (ValueTask) |
| Failure handling | Returns error to caller | Logs and ignores (eventual consistency) |
| Transaction | Entire chain as one transaction | Independent transaction |
| Validation | FluentValidation + FinT | Conditional branching via domain properties |

See the complete Use Case list and port mappings in [Implementation Results](./03-implementation-results/).
