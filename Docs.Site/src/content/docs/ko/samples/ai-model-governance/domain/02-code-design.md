---
title: "도메인 코드 설계"
description: "AI 모델 거버넌스 도메인의 C# 구현 패턴과 코드 스니펫"
---

[비즈니스 요구사항](../00-business-requirements/)에서 자연어로 정의한 규칙을, [타입 설계 의사결정](../01-type-design-decisions/)에서 불변식으로 분류하고 타입 전략을 도출했습니다. 이 문서에서는 그 전략을 C#과 Functorium DDD 빌딩 블록으로 매핑하고, 각 패턴의 구체적인 코드 구현을 살펴봅니다.

## 설계 의사결정 -> C# 구현 매핑

| 설계 의사결정 | Functorium 타입 | 적용 예 | 보장 효과 |
|---|---|---|---|
| 단일 값 검증 + 정규화 | `SimpleValueObject<T>` + `Validate` 체인 | ModelName, ModelVersion, EndpointUrl | 생성 시 검증, Trim/SemVer 정규화 |
| 비교 가능한 값 + 범위 | `ComparableSimpleValueObject<T>` | DriftThreshold, AssessmentScore | 범위 검증, 도메인 속성 |
| Smart Enum + 도메인 속성 | `SimpleValueObject<string>` + `HashMap` | RiskTier, IncidentSeverity | 유효 값만 허용, 도메인 규칙 내장 |
| Smart Enum + 상태 전이 | `SimpleValueObject<string>` + 전이 맵 | DeploymentStatus, IncidentStatus | 허용된 전이만 가능 |
| Aggregate Root 이중 팩토리 | `AggregateRoot<TId>` + `Create`/`CreateFromValidated` | AIModel, ModelDeployment | 도메인 생성과 ORM 복원 분리 |
| 자식 엔티티 | `Entity<TId>` + `IReadOnlyList` | AssessmentCriterion | 외부에서 컬렉션 직접 수정 불가 |
| 교차 Aggregate 검증 | `IDomainService` | DeploymentEligibilityService | FinT LINQ 교차 검증 |
| 도메인 이벤트/오류 | 중첩 `sealed record` | RegisteredEvent, AlreadyDeleted | Aggregate 내부 응집 |

## 패턴별 코드 스니펫

### 1. SimpleValueObject + Validate 체인

**ModelName** -- 문자열 길이 검증 + Trim 정규화:

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

**ModelVersion** -- SemVer 정규식 검증:

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

**EndpointUrl** -- URI 형식 커스텀 검증:

```csharp
public sealed class EndpointUrl : SimpleValueObject<string>
{
    public sealed record InvalidUri : DomainErrorKind.Custom;

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

`ThenMust`는 커스텀 검증 조건을 체이닝에 추가합니다. `InvalidUri` 오류 타입을 VO 내부에 중첩 정의하여 오류 출처를 명확히 합니다.

### 2. ComparableSimpleValueObject + 범위 + 도메인 속성

**DriftThreshold** -- 0.0~1.0 범위:

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

**AssessmentScore** -- 0~100 범위 + 통과 임계값 도메인 속성:

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

`IsPassing` 속성은 도메인 규칙("70점 이상이면 통과")을 값 객체에 내장합니다. 이 규칙이 변경되면 `PassingThreshold` 상수 하나만 수정하면 됩니다.

### 3. Smart Enum -- RiskTier + 도메인 속성

`RiskTier`는 Smart Enum 패턴에 도메인 속성(`RequiresComplianceAssessment`, `IsProhibited`)을 내장합니다.

```csharp
public sealed class RiskTier : SimpleValueObject<string>
{
    public sealed record InvalidValue : DomainErrorKind.Custom;

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

`RequiresComplianceAssessment`와 `IsProhibited`는 비즈니스 규칙을 타입에 직접 인코딩합니다. `ComplianceAssessment.Create()`에서 `riskTier.RequiresComplianceAssessment`로 추가 기준 생성 여부를 결정하고, `DeploymentEligibilityService`에서 `riskTier.IsProhibited`로 배포 금지를 판별합니다.

### 4. Smart Enum -- DeploymentStatus + 전이 규칙

`DeploymentStatus`는 6단계 상태 전이 규칙을 `HashMap` 전이 맵으로 선언합니다.

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

전이 규칙 요약:

| 현재 상태 | 허용 전이 대상 |
|---|---|
| Draft | PendingReview |
| PendingReview | Active, Rejected |
| Active | Quarantined, Decommissioned |
| Quarantined | Active, Decommissioned |
| Decommissioned | (터미널 상태) |
| Rejected | (터미널 상태) |

### 5. AggregateRoot 이중 팩토리 + 가드

Functorium Aggregate Root는 두 개의 팩토리 메서드를 가집니다:

- **`Create()`** -- Application Layer에서 이미 검증된 Value Object를 받아 새 Aggregate를 생성하고 DomainEvent를 발행합니다
- **`CreateFromValidated()`** -- ORM/Repository 복원 전용입니다. 이미 검증/정규화된 데이터를 직접 pass-through하며, 검증 로직을 실행하지 않고 DomainEvent도 발행하지 않습니다. 영속성 레이어에서만 호출해야 합니다

이 계약은 생성 경로와 복원 경로를 명확히 분리하여, "이미 유효한 데이터를 중복 검증하는 비용"과 "복원 시 이벤트가 재발행되는 부작용"을 방지합니다.

**AIModel.Create()** -- VO를 받아 생성 + 이벤트 발행:

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

**AIModel.ClassifyRisk()** -- Soft Delete 가드 + 이벤트:

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

**ModelDeployment.TransitionTo()** -- 상태 전이 + 통합 전이 메서드:

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

모든 상태 전이 메서드가 `TransitionTo` private 메서드로 위임합니다. 전이 검증 로직이 한 곳에 집중되어, 새 전이를 추가할 때 `AllowedTransitions` 맵과 public 메서드만 추가하면 됩니다.

### 6. Entity 자식 엔티티 (AssessmentCriterion)

`AssessmentCriterion`은 `Entity<AssessmentCriterionId>`를 상속하는 자식 엔티티로, ComplianceAssessment Aggregate 경계 내에서만 존재합니다.

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

**ComplianceAssessment.Create()** -- 위험 등급에 따른 평가 기준 자동 생성:

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

`RiskTier`의 `RequiresComplianceAssessment`와 `IsProhibited` 도메인 속성이 평가 기준 수를 결정합니다: Minimal/Limited는 3개, High는 6개, Unacceptable은 7개. 이 규칙이 Smart Enum에 내장되어 있으므로 `if` 분기가 도메인 언어와 일치합니다.

### 7. IDomainService -- DeploymentEligibilityService

`DeploymentEligibilityService`는 배포 적격성을 교차 Aggregate로 검증합니다. `FinT<IO, Unit>` LINQ 합성으로 3가지 검증을 순차적으로 체이닝합니다.

```csharp
public sealed class DeploymentEligibilityService : IDomainService
{
    public sealed record ProhibitedModel : DomainErrorKind.Custom;
    public sealed record ComplianceAssessmentRequired : DomainErrorKind.Custom;
    public sealed record OpenIncidentsExist : DomainErrorKind.Custom;

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

세 검증은 LINQ `from...in`으로 순차 합성됩니다. 첫 번째 검증이 실패하면 나머지는 실행되지 않습니다(short-circuit). `FinT<IO, Unit>`는 IO 효과와 실패 가능성을 타입으로 표현합니다.

## Failable vs Idempotent 반환 타입

| 메서드 | 반환 타입 | 분류 | 이유 |
|---|---|---|---|
| `AIModel.Create()` | `AIModel` | 항상 성공 | 이미 검증된 VO만 수신 |
| `AIModel.ClassifyRisk()` | `Fin<AIModel>` | 실패 가능 | 아카이브된 모델 변경 시 AlreadyDeleted |
| `AIModel.Archive()` | `AIModel` | 멱등 | 이미 아카이브된 상태에서 재호출 허용 |
| `AIModel.Restore()` | `AIModel` | 멱등 | 이미 복원된 상태에서 재호출 허용 |
| `ModelDeployment.Create()` | `ModelDeployment` | 항상 성공 | 이미 검증된 VO만 수신 |
| `ModelDeployment.SubmitForReview()` | `Fin<Unit>` | 실패 가능 | 잘못된 상태 전이 |
| `ModelDeployment.Activate()` | `Fin<Unit>` | 실패 가능 | 잘못된 상태 전이 |
| `ModelDeployment.Quarantine()` | `Fin<Unit>` | 실패 가능 | 잘못된 상태 전이 |
| `ModelDeployment.RecordHealthCheck()` | `ModelDeployment` | 항상 성공 | 헬스 체크 기록은 항상 유효 |
| `ComplianceAssessment.Create()` | `ComplianceAssessment` | 항상 성공 | 이미 검증된 VO만 수신 |
| `ComplianceAssessment.EvaluateCriterion()` | `Fin<Unit>` | 실패 가능 | 기준을 찾을 수 없을 때 |
| `ComplianceAssessment.Complete()` | `Fin<Unit>` | 실패 가능 | 미평가 기준 존재 시 |
| `ModelIncident.Create()` | `ModelIncident` | 항상 성공 | 이미 검증된 VO만 수신 |
| `ModelIncident.Investigate()` | `Fin<Unit>` | 실패 가능 | 잘못된 상태 전이 |
| `ModelIncident.Resolve()` | `Fin<Unit>` | 실패 가능 | 잘못된 상태 전이 |
| `ModelIncident.Escalate()` | `Fin<Unit>` | 실패 가능 | 잘못된 상태 전이 |

[구현 결과](../03-implementation-results/)에서 이 타입 구조가 비즈니스 시나리오를 어떻게 보장하는지 확인합니다.
