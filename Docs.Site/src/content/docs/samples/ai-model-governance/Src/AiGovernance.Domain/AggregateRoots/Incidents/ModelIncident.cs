using Functorium.Domains.Errors;
using AiGovernance.Domain.AggregateRoots.Deployments;
using AiGovernance.Domain.AggregateRoots.Incidents.ValueObjects;
using AiGovernance.Domain.AggregateRoots.Models;
using static Functorium.Domains.Errors.DomainErrorType;
using static LanguageExt.Prelude;

namespace AiGovernance.Domain.AggregateRoots.Incidents;

/// <summary>
/// AI 모델 인시던트 도메인 모델 (Aggregate Root)
/// ModelIncidentId는 [GenerateEntityId] 속성에 의해 소스 생성기로 자동 생성됩니다.
/// </summary>
[GenerateEntityId]
public sealed class ModelIncident : AggregateRoot<ModelIncidentId>, IAuditable
{
    #region Error Types

    public sealed record InvalidStatusTransition : DomainErrorType.Custom;

    #endregion

    #region Domain Events

    /// <summary>
    /// 인시던트 보고 이벤트
    /// </summary>
    public sealed record ReportedEvent(
        ModelIncidentId IncidentId,
        IncidentSeverity Severity,
        ModelDeploymentId DeploymentId) : DomainEvent;

    /// <summary>
    /// 인시던트 조사 시작 이벤트
    /// </summary>
    public sealed record InvestigatingEvent(ModelIncidentId IncidentId) : DomainEvent;

    /// <summary>
    /// 인시던트 해결 이벤트
    /// </summary>
    public sealed record ResolvedEvent(ModelIncidentId IncidentId, ResolutionNote ResolutionNote) : DomainEvent;

    /// <summary>
    /// 인시던트 에스컬레이션 이벤트
    /// </summary>
    public sealed record EscalatedEvent(ModelIncidentId IncidentId) : DomainEvent;

    #endregion

    // 교차 Aggregate 참조
    public ModelDeploymentId DeploymentId { get; private set; }
    public AIModelId ModelId { get; private set; }

    // Value Object 속성
    public IncidentSeverity Severity { get; private set; }
    public IncidentStatus Status { get; private set; }
    public IncidentDescription Description { get; private set; }
    public Option<ResolutionNote> ResolutionNote { get; private set; }
    public DateTime ReportedAt { get; private set; }
    public Option<DateTime> ResolvedAt { get; private set; }

    // Audit 속성
    public DateTime CreatedAt { get; private set; }
    public Option<DateTime> UpdatedAt { get; private set; }

    // 내부 생성자: 이미 검증된 VO를 받음
    private ModelIncident(
        ModelIncidentId id,
        ModelDeploymentId deploymentId,
        AIModelId modelId,
        IncidentSeverity severity,
        IncidentDescription description)
        : base(id)
    {
        DeploymentId = deploymentId;
        ModelId = modelId;
        Severity = severity;
        Description = description;
        Status = IncidentStatus.Reported;
        ReportedAt = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Create: 이미 검증된 Value Object를 직접 받음
    /// </summary>
    public static ModelIncident Create(
        ModelDeploymentId deploymentId,
        AIModelId modelId,
        IncidentSeverity severity,
        IncidentDescription description)
    {
        var incident = new ModelIncident(ModelIncidentId.New(), deploymentId, modelId, severity, description);
        incident.AddDomainEvent(new ReportedEvent(incident.Id, severity, deploymentId));
        return incident;
    }

    /// <summary>
    /// CreateFromValidated: ORM/Repository 복원용 (검증 없음)
    /// </summary>
    public static ModelIncident CreateFromValidated(
        ModelIncidentId id,
        ModelDeploymentId deploymentId,
        AIModelId modelId,
        IncidentSeverity severity,
        IncidentStatus status,
        IncidentDescription description,
        Option<ResolutionNote> resolutionNote,
        DateTime reportedAt,
        Option<DateTime> resolvedAt,
        DateTime createdAt,
        Option<DateTime> updatedAt)
    {
        return new ModelIncident(id, deploymentId, modelId, severity, description)
        {
            Status = status,
            ResolutionNote = resolutionNote,
            ReportedAt = reportedAt,
            ResolvedAt = resolvedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    /// <summary>
    /// 인시던트 조사를 시작합니다. (Reported → Investigating)
    /// </summary>
    public Fin<Unit> Investigate() =>
        TransitionTo(IncidentStatus.Investigating, new InvestigatingEvent(Id));

    /// <summary>
    /// 인시던트를 해결합니다. (Investigating → Resolved)
    /// </summary>
    public Fin<Unit> Resolve(ResolutionNote resolutionNote)
    {
        if (!Status.CanTransitionTo(IncidentStatus.Resolved))
            return DomainError.For<ModelIncident, string, string>(
                new InvalidStatusTransition(),
                value1: Status,
                value2: IncidentStatus.Resolved,
                message: $"Cannot transition from '{Status}' to 'Resolved'");

        Status = IncidentStatus.Resolved;
        ResolutionNote = resolutionNote;
        ResolvedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new ResolvedEvent(Id, resolutionNote));
        return unit;
    }

    /// <summary>
    /// 인시던트를 에스컬레이션합니다. (Reported/Investigating → Escalated)
    /// </summary>
    public Fin<Unit> Escalate() =>
        TransitionTo(IncidentStatus.Escalated, new EscalatedEvent(Id));

    private Fin<Unit> TransitionTo(IncidentStatus target, DomainEvent domainEvent)
    {
        if (!Status.CanTransitionTo(target))
            return DomainError.For<ModelIncident, string, string>(
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
