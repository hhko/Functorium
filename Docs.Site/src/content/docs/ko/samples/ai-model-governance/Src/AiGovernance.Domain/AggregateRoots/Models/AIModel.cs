using Functorium.Domains.Errors;
using AiGovernance.Domain.AggregateRoots.Models.ValueObjects;
using static Functorium.Domains.Errors.DomainErrorType;

namespace AiGovernance.Domain.AggregateRoots.Models;

/// <summary>
/// AI 모델 도메인 모델 (Aggregate Root)
/// AIModelId는 [GenerateEntityId] 속성에 의해 소스 생성기로 자동 생성됩니다.
/// </summary>
[GenerateEntityId]
public sealed class AIModel : AggregateRoot<AIModelId>, IAuditable, ISoftDeletableWithUser
{
    #region Error Types

    public sealed record AlreadyDeleted : DomainErrorType.Custom;

    #endregion

    #region Domain Events

    /// <summary>
    /// AI 모델 등록 이벤트
    /// </summary>
    public sealed record RegisteredEvent(
        AIModelId ModelId,
        ModelName Name,
        ModelVersion Version,
        ModelPurpose Purpose,
        RiskTier RiskTier) : DomainEvent;

    /// <summary>
    /// 위험 등급 분류 이벤트
    /// </summary>
    public sealed record RiskClassifiedEvent(
        AIModelId ModelId,
        RiskTier OldRiskTier,
        RiskTier NewRiskTier) : DomainEvent;

    /// <summary>
    /// 모델 버전 업데이트 이벤트
    /// </summary>
    public sealed record VersionUpdatedEvent(
        AIModelId ModelId,
        ModelVersion OldVersion,
        ModelVersion NewVersion) : DomainEvent;

    /// <summary>
    /// 모델 정보 업데이트 이벤트
    /// </summary>
    public sealed record UpdatedEvent(
        AIModelId ModelId,
        ModelName Name,
        ModelPurpose Purpose) : DomainEvent;

    /// <summary>
    /// 모델 아카이브 이벤트
    /// </summary>
    public sealed record ArchivedEvent(AIModelId ModelId, string DeletedBy) : DomainEvent;

    /// <summary>
    /// 모델 복원 이벤트
    /// </summary>
    public sealed record RestoredEvent(AIModelId ModelId) : DomainEvent;

    #endregion

    // Value Object 속성
    public ModelName Name { get; private set; }
    public ModelVersion Version { get; private set; }
    public ModelPurpose Purpose { get; private set; }
    public RiskTier RiskTier { get; private set; }

    // Audit 속성
    public DateTime CreatedAt { get; private set; }
    public Option<DateTime> UpdatedAt { get; private set; }

    // SoftDelete 속성
    public Option<DateTime> DeletedAt { get; private set; }
    public Option<string> DeletedBy { get; private set; }

    // 내부 생성자: 이미 검증된 VO를 받음
    private AIModel(
        AIModelId id,
        ModelName name,
        ModelVersion version,
        ModelPurpose purpose,
        RiskTier riskTier)
        : base(id)
    {
        Name = name;
        Version = version;
        Purpose = purpose;
        RiskTier = riskTier;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Create: 이미 검증된 Value Object를 직접 받음
    /// Application Layer에서 VO 생성 후 호출
    /// </summary>
    public static AIModel Create(
        ModelName name,
        ModelVersion version,
        ModelPurpose purpose,
        RiskTier riskTier)
    {
        var model = new AIModel(AIModelId.New(), name, version, purpose, riskTier);
        model.AddDomainEvent(new RegisteredEvent(model.Id, name, version, purpose, riskTier));
        return model;
    }

    /// <summary>
    /// CreateFromValidated: ORM/Repository 복원용 (검증 없음)
    /// </summary>
    public static AIModel CreateFromValidated(
        AIModelId id,
        ModelName name,
        ModelVersion version,
        ModelPurpose purpose,
        RiskTier riskTier,
        DateTime createdAt,
        Option<DateTime> updatedAt,
        Option<DateTime> deletedAt,
        Option<string> deletedBy)
    {
        return new AIModel(id, name, version, purpose, riskTier)
        {
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            DeletedAt = deletedAt,
            DeletedBy = deletedBy
        };
    }

    /// <summary>
    /// 위험 등급을 재분류합니다.
    /// </summary>
    public Fin<AIModel> ClassifyRisk(RiskTier newRiskTier)
    {
        if (DeletedAt.IsSome)
            return DomainError.For<AIModel>(
                new AlreadyDeleted(),
                Id.ToString(),
                "Cannot classify risk for a deleted model");

        var oldRiskTier = RiskTier;
        RiskTier = newRiskTier;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new RiskClassifiedEvent(Id, oldRiskTier, newRiskTier));
        return this;
    }

    /// <summary>
    /// 모델 버전을 업데이트합니다.
    /// </summary>
    public Fin<AIModel> UpdateVersion(ModelVersion newVersion)
    {
        if (DeletedAt.IsSome)
            return DomainError.For<AIModel>(
                new AlreadyDeleted(),
                Id.ToString(),
                "Cannot update version of a deleted model");

        var oldVersion = Version;
        Version = newVersion;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new VersionUpdatedEvent(Id, oldVersion, newVersion));
        return this;
    }

    /// <summary>
    /// 모델 정보를 업데이트합니다.
    /// </summary>
    public Fin<AIModel> Update(ModelName name, ModelPurpose purpose)
    {
        if (DeletedAt.IsSome)
            return DomainError.For<AIModel>(
                new AlreadyDeleted(),
                Id.ToString(),
                "Cannot update a deleted model");

        Name = name;
        Purpose = purpose;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new UpdatedEvent(Id, name, purpose));
        return this;
    }

    /// <summary>
    /// 모델을 아카이브합니다. (멱등성 보장)
    /// </summary>
    public AIModel Archive(string deletedBy)
    {
        if (DeletedAt.IsSome)
            return this;

        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        AddDomainEvent(new ArchivedEvent(Id, deletedBy));
        return this;
    }

    /// <summary>
    /// 아카이브된 모델을 복원합니다. (멱등성 보장)
    /// </summary>
    public AIModel Restore()
    {
        if (DeletedAt.IsNone)
            return this;

        DeletedAt = Option<DateTime>.None;
        DeletedBy = Option<string>.None;
        AddDomainEvent(new RestoredEvent(Id));
        return this;
    }
}
