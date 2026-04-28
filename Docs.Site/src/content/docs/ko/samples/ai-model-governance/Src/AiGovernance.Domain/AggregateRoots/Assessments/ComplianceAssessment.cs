using Functorium.Domains.Errors;
using AiGovernance.Domain.AggregateRoots.Assessments.ValueObjects;
using AiGovernance.Domain.AggregateRoots.Deployments;
using AiGovernance.Domain.AggregateRoots.Models;
using AiGovernance.Domain.AggregateRoots.Models.ValueObjects;
using static Functorium.Domains.Errors.DomainErrorKind;
using static LanguageExt.Prelude;

namespace AiGovernance.Domain.AggregateRoots.Assessments;

/// <summary>
/// 컴플라이언스 평가 도메인 모델 (Aggregate Root)
/// ComplianceAssessmentId는 [GenerateEntityId] 속성에 의해 소스 생성기로 자동 생성됩니다.
/// </summary>
[GenerateEntityId]
public sealed class ComplianceAssessment : AggregateRoot<ComplianceAssessmentId>, IAuditable
{
    #region Error Types

    public sealed record CriterionNotFound : DomainErrorKind.Custom;
    public sealed record InvalidStatusTransition : DomainErrorKind.Custom;
    public sealed record NotAllCriteriaEvaluated : DomainErrorKind.Custom;

    #endregion

    #region Domain Events

    /// <summary>
    /// 평가 생성 이벤트
    /// </summary>
    public sealed record CreatedEvent(
        ComplianceAssessmentId AssessmentId,
        AIModelId ModelId,
        ModelDeploymentId DeploymentId,
        int CriteriaCount) : DomainEvent;

    /// <summary>
    /// 기준 평가 이벤트
    /// </summary>
    public sealed record CriterionEvaluatedEvent(
        ComplianceAssessmentId AssessmentId,
        AssessmentCriterionId CriterionId,
        CriterionResult Result) : DomainEvent;

    /// <summary>
    /// 평가 완료 이벤트
    /// </summary>
    public sealed record CompletedEvent(
        ComplianceAssessmentId AssessmentId,
        AssessmentStatus Status,
        AssessmentScore OverallScore) : DomainEvent;

    #endregion

    // 교차 Aggregate 참조
    public AIModelId ModelId { get; private set; }
    public ModelDeploymentId DeploymentId { get; private set; }

    // Value Object 속성
    public Option<AssessmentScore> OverallScore { get; private set; }
    public AssessmentStatus Status { get; private set; }
    public DateTime AssessedAt { get; private set; }

    // 평가 기준 컬렉션
    private readonly List<AssessmentCriterion> _criteria = [];
    public IReadOnlyList<AssessmentCriterion> Criteria => _criteria.AsReadOnly();

    // Audit 속성
    public DateTime CreatedAt { get; private set; }
    public Option<DateTime> UpdatedAt { get; private set; }

    // 내부 생성자
    private ComplianceAssessment(
        ComplianceAssessmentId id,
        AIModelId modelId,
        ModelDeploymentId deploymentId)
        : base(id)
    {
        ModelId = modelId;
        DeploymentId = deploymentId;
        Status = AssessmentStatus.Initiated;
        AssessedAt = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Create: 위험 등급에 따라 평가 기준을 자동 생성합니다.
    /// </summary>
    public static ComplianceAssessment Create(
        AIModelId modelId,
        ModelDeploymentId deploymentId,
        RiskTier riskTier)
    {
        var assessment = new ComplianceAssessment(ComplianceAssessmentId.New(), modelId, deploymentId);
        var criteria = GenerateCriteria(riskTier);
        assessment._criteria.AddRange(criteria);
        assessment.AddDomainEvent(new CreatedEvent(assessment.Id, modelId, deploymentId, criteria.Count));
        return assessment;
    }

    /// <summary>
    /// CreateFromValidated: ORM/Repository 복원용 (검증 없음)
    /// </summary>
    public static ComplianceAssessment CreateFromValidated(
        ComplianceAssessmentId id,
        AIModelId modelId,
        ModelDeploymentId deploymentId,
        Option<AssessmentScore> overallScore,
        AssessmentStatus status,
        IEnumerable<AssessmentCriterion> criteria,
        DateTime assessedAt,
        DateTime createdAt,
        Option<DateTime> updatedAt)
    {
        var assessment = new ComplianceAssessment(id, modelId, deploymentId)
        {
            OverallScore = overallScore,
            Status = status,
            AssessedAt = assessedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
        assessment._criteria.AddRange(criteria);
        return assessment;
    }

    /// <summary>
    /// 개별 평가 기준을 평가합니다.
    /// </summary>
    public Fin<Unit> EvaluateCriterion(AssessmentCriterionId criterionId, CriterionResult result, Option<string> notes)
    {
        var criterion = _criteria.Find(c => c.Id == criterionId);
        if (criterion is null)
            return DomainError.For<ComplianceAssessment>(
                new CriterionNotFound(),
                criterionId.ToString(),
                $"Criterion not found: '{criterionId}'");

        if (Status == AssessmentStatus.Initiated)
        {
            Status = AssessmentStatus.InProgress;
        }

        criterion.Evaluate(result, notes);
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new CriterionEvaluatedEvent(Id, criterionId, result));
        return unit;
    }

    /// <summary>
    /// 평가를 완료합니다. 모든 기준이 평가되어야 합니다.
    /// </summary>
    public Fin<Unit> Complete()
    {
        var unevaluated = _criteria.Where(c => c.Result.IsNone).ToList();
        if (unevaluated.Count > 0)
            return DomainError.For<ComplianceAssessment, int>(
                new NotAllCriteriaEvaluated(),
                unevaluated.Count,
                $"{unevaluated.Count} criteria have not been evaluated");

        var applicableCriteria = _criteria
            .Where(c => c.Result.Map(r => r != CriterionResult.NotApplicable).IfNone(false))
            .ToList();

        var score = applicableCriteria.Count > 0
            ? (int)Math.Round(
                (double)applicableCriteria
                    .Count(c => c.Result.Map(r => r == CriterionResult.Pass).IfNone(false))
                / applicableCriteria.Count * 100)
            : 100;

        var assessmentScore = AssessmentScore.CreateFromValidated(score);
        OverallScore = assessmentScore;

        var newStatus = assessmentScore.IsPassing
            ? AssessmentStatus.Passed
            : score >= 40
                ? AssessmentStatus.RequiresRemediation
                : AssessmentStatus.Failed;

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new CompletedEvent(Id, newStatus, assessmentScore));
        return unit;
    }

    private static List<AssessmentCriterion> GenerateCriteria(RiskTier riskTier)
    {
        var criteria = new List<AssessmentCriterion>
        {
            AssessmentCriterion.Create("Data Governance", "Verify data quality and governance practices"),
            AssessmentCriterion.Create("Technical Documentation", "Review technical documentation completeness"),
            AssessmentCriterion.Create("Security Review", "Assess security measures and vulnerabilities")
        };

        if (riskTier.RequiresComplianceAssessment)
        {
            criteria.Add(AssessmentCriterion.Create("Human Oversight", "Verify human oversight mechanisms"));
            criteria.Add(AssessmentCriterion.Create("Bias Assessment", "Evaluate model fairness and bias"));
            criteria.Add(AssessmentCriterion.Create("Transparency", "Assess model interpretability and transparency"));
        }

        if (riskTier.IsProhibited)
        {
            criteria.Add(AssessmentCriterion.Create("Prohibition Review", "Confirm model does not fall under prohibited use"));
        }

        return criteria;
    }
}
