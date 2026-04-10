using Functorium.Domains.Errors;
using AiGovernance.Domain.AggregateRoots.Deployments.ValueObjects;
using AiGovernance.Domain.AggregateRoots.Models;
using static Functorium.Domains.Errors.DomainErrorType;
using static LanguageExt.Prelude;

namespace AiGovernance.Domain.AggregateRoots.Deployments;

/// <summary>
/// AI 모델 배포 도메인 모델 (Aggregate Root)
/// ModelDeploymentId는 [GenerateEntityId] 속성에 의해 소스 생성기로 자동 생성됩니다.
/// </summary>
[GenerateEntityId]
public sealed class ModelDeployment : AggregateRoot<ModelDeploymentId>, IAuditable
{
    #region Error Types

    public sealed record InvalidStatusTransition : DomainErrorType.Custom;

    #endregion

    #region Domain Events

    /// <summary>
    /// 배포 생성 이벤트
    /// </summary>
    public sealed record CreatedEvent(
        ModelDeploymentId DeploymentId,
        AIModelId ModelId,
        EndpointUrl EndpointUrl,
        DeploymentEnvironment Environment) : DomainEvent;

    /// <summary>
    /// 검토 제출 이벤트
    /// </summary>
    public sealed record SubmittedForReviewEvent(ModelDeploymentId DeploymentId) : DomainEvent;

    /// <summary>
    /// 배포 활성화 이벤트
    /// </summary>
    public sealed record ActivatedEvent(ModelDeploymentId DeploymentId) : DomainEvent;

    /// <summary>
    /// 배포 격리 이벤트
    /// </summary>
    public sealed record QuarantinedEvent(ModelDeploymentId DeploymentId, string Reason) : DomainEvent;

    /// <summary>
    /// 격리 해제(복구) 이벤트
    /// </summary>
    public sealed record RemediatedEvent(ModelDeploymentId DeploymentId) : DomainEvent;

    /// <summary>
    /// 배포 해제 이벤트
    /// </summary>
    public sealed record DecommissionedEvent(ModelDeploymentId DeploymentId) : DomainEvent;

    #endregion

    // 교차 Aggregate 참조
    public AIModelId ModelId { get; private set; }

    // Value Object 속성
    public EndpointUrl EndpointUrl { get; private set; }
    public DeploymentEnvironment Environment { get; private set; }
    public DeploymentStatus Status { get; private set; }
    public DriftThreshold DriftThreshold { get; private set; }
    public Option<DateTime> LastHealthCheckAt { get; private set; }
    public DateTime DeployedAt { get; private set; }

    // Audit 속성
    public DateTime CreatedAt { get; private set; }
    public Option<DateTime> UpdatedAt { get; private set; }

    // 내부 생성자: 이미 검증된 VO를 받음
    private ModelDeployment(
        ModelDeploymentId id,
        AIModelId modelId,
        EndpointUrl endpointUrl,
        DeploymentEnvironment environment,
        DriftThreshold driftThreshold)
        : base(id)
    {
        ModelId = modelId;
        EndpointUrl = endpointUrl;
        Environment = environment;
        DriftThreshold = driftThreshold;
        Status = DeploymentStatus.Draft;
        DeployedAt = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Create: 이미 검증된 Value Object를 직접 받음
    /// </summary>
    public static ModelDeployment Create(
        AIModelId modelId,
        EndpointUrl endpointUrl,
        DeploymentEnvironment environment,
        DriftThreshold driftThreshold)
    {
        var deployment = new ModelDeployment(ModelDeploymentId.New(), modelId, endpointUrl, environment, driftThreshold);
        deployment.AddDomainEvent(new CreatedEvent(deployment.Id, modelId, endpointUrl, environment));
        return deployment;
    }

    /// <summary>
    /// CreateFromValidated: ORM/Repository 복원용 (검증 없음)
    /// </summary>
    public static ModelDeployment CreateFromValidated(
        ModelDeploymentId id,
        AIModelId modelId,
        EndpointUrl endpointUrl,
        DeploymentEnvironment environment,
        DeploymentStatus status,
        DriftThreshold driftThreshold,
        Option<DateTime> lastHealthCheckAt,
        DateTime deployedAt,
        DateTime createdAt,
        Option<DateTime> updatedAt)
    {
        return new ModelDeployment(id, modelId, endpointUrl, environment, driftThreshold)
        {
            Status = status,
            LastHealthCheckAt = lastHealthCheckAt,
            DeployedAt = deployedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    /// <summary>
    /// 검토를 위해 제출합니다. (Draft → PendingReview)
    /// </summary>
    public Fin<Unit> SubmitForReview() =>
        TransitionTo(DeploymentStatus.PendingReview, new SubmittedForReviewEvent(Id));

    /// <summary>
    /// 배포를 활성화합니다. (PendingReview → Active)
    /// </summary>
    public Fin<Unit> Activate() =>
        TransitionTo(DeploymentStatus.Active, new ActivatedEvent(Id));

    /// <summary>
    /// 배포를 격리합니다. (Active → Quarantined)
    /// </summary>
    public Fin<Unit> Quarantine(string reason) =>
        TransitionTo(DeploymentStatus.Quarantined, new QuarantinedEvent(Id, reason));

    /// <summary>
    /// 격리를 해제하고 복구합니다. (Quarantined → Active)
    /// </summary>
    public Fin<Unit> Remediate() =>
        TransitionTo(DeploymentStatus.Active, new RemediatedEvent(Id));

    /// <summary>
    /// 배포를 해제합니다. (Active/Quarantined → Decommissioned)
    /// </summary>
    public Fin<Unit> Decommission() =>
        TransitionTo(DeploymentStatus.Decommissioned, new DecommissionedEvent(Id));

    /// <summary>
    /// 헬스 체크를 기록합니다.
    /// </summary>
    public ModelDeployment RecordHealthCheck()
    {
        LastHealthCheckAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        return this;
    }

    private Fin<Unit> TransitionTo(DeploymentStatus target, DomainEvent domainEvent)
    {
        if (!Status.CanTransitionTo(target))
            return DomainError.For<ModelDeployment, string, string>(
                new InvalidStatusTransition(),
                value1: Status,
                value2: target,
                message: $"Cannot transition from '{Status}' to '{target}'");

        Status = target;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(domainEvent);
        return unit;
    }
}
