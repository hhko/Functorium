---
title: "Domain Code Design"
description: "C# implementation patterns and code snippets for the AI Model Governance domain"
---

The rules defined in natural language in the [Business Requirements](../00-business-requirements/) were classified as invariants and type strategies were derived in the [Type Design Decisions](../01-type-design-decisions/). This document maps those strategies to C# and Functorium DDD building blocks, examining the concrete code implementation of each pattern.

## Design Decision -> C# Implementation Mapping

| Design Decision | Functorium Type | Application Example | Guaranteed Effect |
|---|---|---|---|
| Single value validation + normalization | `SimpleValueObject<T>` + `Validate` chain | ModelName, ModelVersion, EndpointUrl | Validation at creation, Trim/SemVer normalization |
| Comparable value + range | `ComparableSimpleValueObject<T>` | DriftThreshold, AssessmentScore | Range validation, domain properties |
| Smart Enum + domain properties | `SimpleValueObject<string>` + `HashMap` | RiskTier, IncidentSeverity | Only valid values allowed, domain rules embedded |
| Smart Enum + state transition | `SimpleValueObject<string>` + transition map | DeploymentStatus, IncidentStatus | Only allowed transitions possible |
| Aggregate Root dual factory | `AggregateRoot<TId>` + `Create`/`CreateFromValidated` | AIModel, ModelDeployment | Separation of domain creation and ORM restoration |
| Child entity | `Entity<TId>` + `IReadOnlyList` | AssessmentCriterion | Collection cannot be directly modified externally |
| Cross-Aggregate verification | `IDomainService` | DeploymentEligibilityService | FinT LINQ cross-verification |
| Domain events/errors | Nested `sealed record` | RegisteredEvent, AlreadyDeleted | Cohesive within Aggregate |

## Code Snippets by Pattern

### 1. SimpleValueObject + Validate Chain

**ModelName** -- String length validation + Trim normalization:

```csharp
public sealed class ModelName : SimpleValueObject<string>
{
    public const int MaxLength = 100;

    private ModelName(string value) : base(value) { }

    public static Fin<ModelName> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new ModelName(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<ModelName>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenNormalize(v => v.Trim())
            .ThenMaxLength(MaxLength);

    public static ModelName CreateFromValidated(string value) => new(value);

    public static implicit operator string(ModelName modelName) => modelName.Value;
}
```

**ModelVersion** -- SemVer regex validation:

```csharp
public sealed partial class ModelVersion : SimpleValueObject<string>
{
    private ModelVersion(string value) : base(value) { }

    public static Fin<ModelVersion> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new ModelVersion(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<ModelVersion>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenNormalize(v => v.Trim())
            .ThenMatches(SemVerPattern(), "Invalid SemVer format");

    public static ModelVersion CreateFromValidated(string value) => new(value);

    public static implicit operator string(ModelVersion version) => version.Value;

    [GeneratedRegex(@"^\d+\.\d+\.\d+(-[\w.]+)?$")]
    private static partial Regex SemVerPattern();
}
```

**EndpointUrl** -- URI format custom validation:

```csharp
public sealed class EndpointUrl : SimpleValueObject<string>
{
    public sealed record InvalidUri : DomainErrorType.Custom;

    private EndpointUrl(string value) : base(value) { }

    public static Fin<EndpointUrl> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new EndpointUrl(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<EndpointUrl>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenNormalize(v => v.Trim())
            .ThenMust(
                v => Uri.TryCreate(v, UriKind.Absolute, out var uri)
                     && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
                new InvalidUri(),
                v => $"Invalid endpoint URL format: '{v}'");

    public static EndpointUrl CreateFromValidated(string value) => new(value);
}
```

`ThenMust` adds a custom validation condition to the chain. The `InvalidUri` error type is nested within the VO to clarify the error source.

### 2. ComparableSimpleValueObject + Range + Domain Properties

**DriftThreshold** -- Range 0.0~1.0:

```csharp
public sealed class DriftThreshold : ComparableSimpleValueObject<decimal>
{
    public const decimal MinValue = 0.0m;
    public const decimal MaxValue = 1.0m;

    private DriftThreshold(decimal value) : base(value) { }

    public static Fin<DriftThreshold> Create(decimal value) =>
        CreateFromValidation(Validate(value), v => new DriftThreshold(v));

    public static Validation<Error, decimal> Validate(decimal value) =>
        ValidationRules<DriftThreshold>
            .Between(value, MinValue, MaxValue);

    public static DriftThreshold CreateFromValidated(decimal value) => new(value);
}
```

**AssessmentScore** -- Range 0~100 + passing threshold domain property:

```csharp
public sealed class AssessmentScore : ComparableSimpleValueObject<int>
{
    public const int MinValue = 0;
    public const int MaxValue = 100;
    public const int PassingThreshold = 70;

    private AssessmentScore(int value) : base(value) { }

    public static Fin<AssessmentScore> Create(int value) =>
        CreateFromValidation(Validate(value), v => new AssessmentScore(v));

    public static Validation<Error, int> Validate(int value) =>
        ValidationRules<AssessmentScore>
            .Between(value, MinValue, MaxValue);

    public static AssessmentScore CreateFromValidated(int value) => new(value);

    public bool IsPassing => Value >= PassingThreshold;
}
```

The `IsPassing` property embeds the domain rule ("70 or above passes") in the value object. If this rule changes, only the `PassingThreshold` constant needs modification.

### 3. Smart Enum -- RiskTier + Domain Properties

`RiskTier` is a Smart Enum pattern with domain properties (`RequiresComplianceAssessment`, `IsProhibited`) embedded.

```csharp
public sealed class RiskTier : SimpleValueObject<string>
{
    public sealed record InvalidValue : DomainErrorType.Custom;

    public static readonly RiskTier Minimal = new("Minimal");
    public static readonly RiskTier Limited = new("Limited");
    public static readonly RiskTier High = new("High");
    public static readonly RiskTier Unacceptable = new("Unacceptable");

    private static readonly HashMap<string, RiskTier> All = HashMap(
        ("Minimal", Minimal), ("Limited", Limited),
        ("High", High), ("Unacceptable", Unacceptable));

    private RiskTier(string value) : base(value) { }

    public static Fin<RiskTier> Create(string value) =>
        Validate(value).ToFin();

    public static Validation<Error, RiskTier> Validate(string value) =>
        All.Find(value)
            .ToValidation(DomainError.For<RiskTier>(
                new InvalidValue(), currentValue: value,
                message: $"Invalid risk tier: '{value}'"));

    public bool RequiresComplianceAssessment => this == High || this == Unacceptable;
    public bool IsProhibited => this == Unacceptable;
}
```

`RequiresComplianceAssessment` and `IsProhibited` directly encode business rules in the type. `ComplianceAssessment.Create()` uses `riskTier.RequiresComplianceAssessment` to determine whether to create additional criteria, and `DeploymentEligibilityService` uses `riskTier.IsProhibited` to determine deployment prohibition.

### 4. Smart Enum -- DeploymentStatus + Transition Rules

`DeploymentStatus` declares 6-state transition rules using a `HashMap` transition map.

```csharp
public sealed class DeploymentStatus : SimpleValueObject<string>
{
    public static readonly DeploymentStatus Draft = new("Draft");
    public static readonly DeploymentStatus PendingReview = new("PendingReview");
    public static readonly DeploymentStatus Active = new("Active");
    public static readonly DeploymentStatus Quarantined = new("Quarantined");
    public static readonly DeploymentStatus Decommissioned = new("Decommissioned");
    public static readonly DeploymentStatus Rejected = new("Rejected");

    private static readonly HashMap<string, Seq<string>> AllowedTransitions = HashMap(
        ("Draft", Seq("PendingReview")),
        ("PendingReview", Seq("Active", "Rejected")),
        ("Active", Seq("Quarantined", "Decommissioned")),
        ("Quarantined", Seq("Active", "Decommissioned")));

    public bool CanTransitionTo(DeploymentStatus target) =>
        AllowedTransitions.Find(Value)
            .Map(allowed => allowed.Any(v => v == target.Value))
            .IfNone(false);
}
```

Transition rule summary:

| Current Status | Allowed Targets |
|---|---|
| Draft | PendingReview |
| PendingReview | Active, Rejected |
| Active | Quarantined, Decommissioned |
| Quarantined | Active, Decommissioned |
| Decommissioned | (terminal state) |
| Rejected | (terminal state) |

### 5. AggregateRoot Dual Factory + Guard

Functorium Aggregate Roots have two factory methods:

- **`Create()`** -- Receives already-validated Value Objects from the Application Layer, creates a new Aggregate, and publishes DomainEvents
- **`CreateFromValidated()`** -- ORM/Repository restoration only. Passes through already-validated/normalized data directly without executing validation logic or publishing DomainEvents. Should only be called from the persistence layer

This contract clearly separates creation and restoration paths, preventing "the cost of redundantly validating already-valid data" and "the side effect of re-publishing events during restoration."

**AIModel.Create()** -- Receives VOs for creation + event publishing:

```csharp
public static AIModel Create(
    ModelName name, ModelVersion version,
    ModelPurpose purpose, RiskTier riskTier)
{
    var model = new AIModel(AIModelId.New(), name, version, purpose, riskTier);
    model.AddDomainEvent(new RegisteredEvent(model.Id, name, version, purpose, riskTier));
    return model;
}
```

**AIModel.ClassifyRisk()** -- Soft Delete guard + event:

```csharp
public Fin<AIModel> ClassifyRisk(RiskTier newRiskTier)
{
    if (DeletedAt.IsSome)
        return DomainError.For<AIModel>(
            new AlreadyDeleted(), Id.ToString(),
            "Cannot classify risk for a deleted model");

    var oldRiskTier = RiskTier;
    RiskTier = newRiskTier;
    UpdatedAt = DateTime.UtcNow;
    AddDomainEvent(new RiskClassifiedEvent(Id, oldRiskTier, newRiskTier));
    return this;
}
```

**ModelDeployment.TransitionTo()** -- State transition + unified transition method:

```csharp
private Fin<Unit> TransitionTo(DeploymentStatus target, DomainEvent domainEvent)
{
    if (!Status.CanTransitionTo(target))
        return DomainError.For<ModelDeployment, string, string>(
            new InvalidStatusTransition(),
            value1: Status, value2: target,
            message: $"Cannot transition from '{Status}' to '{target}'");

    Status = target;
    UpdatedAt = DateTime.UtcNow;
    AddDomainEvent(domainEvent);
    return unit;
}

public Fin<Unit> SubmitForReview() =>
    TransitionTo(DeploymentStatus.PendingReview, new SubmittedForReviewEvent(Id));

public Fin<Unit> Activate() =>
    TransitionTo(DeploymentStatus.Active, new ActivatedEvent(Id));

public Fin<Unit> Quarantine(string reason) =>
    TransitionTo(DeploymentStatus.Quarantined, new QuarantinedEvent(Id, reason));
```

All state transition methods delegate to the private `TransitionTo` method. Transition validation logic is concentrated in one place, so adding a new transition only requires adding an entry to the `AllowedTransitions` map and a public method.

### 6. Entity Child Entity (AssessmentCriterion)

`AssessmentCriterion` inherits from `Entity<AssessmentCriterionId>` as a child entity that exists only within the ComplianceAssessment Aggregate boundary.

```csharp
public sealed class AssessmentCriterion : Entity<AssessmentCriterionId>
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Option<CriterionResult> Result { get; private set; }
    public Option<string> Notes { get; private set; }
    public Option<DateTime> EvaluatedAt { get; private set; }

    public static AssessmentCriterion Create(string name, string description) =>
        new(AssessmentCriterionId.New(), name, description);

    public AssessmentCriterion Evaluate(CriterionResult result, Option<string> notes)
    {
        Result = result;
        Notes = notes;
        EvaluatedAt = DateTime.UtcNow;
        return this;
    }
}
```

**ComplianceAssessment.Create()** -- Automatic assessment criteria generation based on risk tier:

```csharp
public static ComplianceAssessment Create(
    AIModelId modelId, ModelDeploymentId deploymentId, RiskTier riskTier)
{
    var assessment = new ComplianceAssessment(
        ComplianceAssessmentId.New(), modelId, deploymentId);
    var criteria = GenerateCriteria(riskTier);
    assessment._criteria.AddRange(criteria);
    assessment.AddDomainEvent(new CreatedEvent(
        assessment.Id, modelId, deploymentId, criteria.Count));
    return assessment;
}

private static List<AssessmentCriterion> GenerateCriteria(RiskTier riskTier)
{
    var criteria = new List<AssessmentCriterion>
    {
        AssessmentCriterion.Create("Data Governance", "Verify data quality..."),
        AssessmentCriterion.Create("Technical Documentation", "Review completeness..."),
        AssessmentCriterion.Create("Security Review", "Assess security...")
    };

    if (riskTier.RequiresComplianceAssessment)
    {
        criteria.Add(AssessmentCriterion.Create("Human Oversight", "..."));
        criteria.Add(AssessmentCriterion.Create("Bias Assessment", "..."));
        criteria.Add(AssessmentCriterion.Create("Transparency", "..."));
    }

    if (riskTier.IsProhibited)
        criteria.Add(AssessmentCriterion.Create("Prohibition Review", "..."));

    return criteria;
}
```

The `RequiresComplianceAssessment` and `IsProhibited` domain properties of `RiskTier` determine the number of assessment criteria: Minimal/Limited gets 3, High gets 6, Unacceptable gets 7. Since these rules are embedded in the Smart Enum, the `if` branches align with the domain language.

### 7. IDomainService -- DeploymentEligibilityService

`DeploymentEligibilityService` verifies deployment eligibility across Aggregates. It chains 3 verifications sequentially using `FinT<IO, Unit>` LINQ composition.

```csharp
public sealed class DeploymentEligibilityService : IDomainService
{
    public sealed record ProhibitedModel : DomainErrorType.Custom;
    public sealed record ComplianceAssessmentRequired : DomainErrorType.Custom;
    public sealed record OpenIncidentsExist : DomainErrorType.Custom;

    public FinT<IO, Unit> ValidateEligibility(
        AIModel model,
        IAssessmentRepository assessmentRepository,
        IIncidentRepository incidentRepository)
    {
        return
            from _1 in CheckNotProhibited(model)
            from _2 in CheckComplianceAssessment(model, assessmentRepository)
            from _3 in CheckNoOpenIncidents(model, incidentRepository)
            select unit;
    }
}
```

The three verifications are composed sequentially using LINQ `from...in`. If the first verification fails, the rest are not executed (short-circuit). `FinT<IO, Unit>` expresses IO effects and failure possibility through types.

## Failable vs Idempotent Return Types

| Method | Return Type | Classification | Reason |
|---|---|---|---|
| `AIModel.Create()` | `AIModel` | Always succeeds | Only receives already-validated VOs |
| `AIModel.ClassifyRisk()` | `Fin<AIModel>` | Failable | AlreadyDeleted when modifying archived model |
| `AIModel.Archive()` | `AIModel` | Idempotent | Re-invocation allowed in already-archived state |
| `AIModel.Restore()` | `AIModel` | Idempotent | Re-invocation allowed in already-restored state |
| `ModelDeployment.Create()` | `ModelDeployment` | Always succeeds | Only receives already-validated VOs |
| `ModelDeployment.SubmitForReview()` | `Fin<Unit>` | Failable | Invalid state transition |
| `ModelDeployment.Activate()` | `Fin<Unit>` | Failable | Invalid state transition |
| `ModelDeployment.Quarantine()` | `Fin<Unit>` | Failable | Invalid state transition |
| `ModelDeployment.RecordHealthCheck()` | `ModelDeployment` | Always succeeds | Health check recording is always valid |
| `ComplianceAssessment.Create()` | `ComplianceAssessment` | Always succeeds | Only receives already-validated VOs |
| `ComplianceAssessment.EvaluateCriterion()` | `Fin<Unit>` | Failable | When criterion cannot be found |
| `ComplianceAssessment.Complete()` | `Fin<Unit>` | Failable | When unevaluated criteria exist |
| `ModelIncident.Create()` | `ModelIncident` | Always succeeds | Only receives already-validated VOs |
| `ModelIncident.Investigate()` | `Fin<Unit>` | Failable | Invalid state transition |
| `ModelIncident.Resolve()` | `Fin<Unit>` | Failable | Invalid state transition |
| `ModelIncident.Escalate()` | `Fin<Unit>` | Failable | Invalid state transition |

See [Implementation Results](./03-implementation-results/) to confirm how this type structure guarantees business scenarios.
