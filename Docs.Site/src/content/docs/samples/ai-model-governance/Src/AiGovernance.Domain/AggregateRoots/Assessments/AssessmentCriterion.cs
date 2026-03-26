using AiGovernance.Domain.AggregateRoots.Assessments.ValueObjects;

namespace AiGovernance.Domain.AggregateRoots.Assessments;

/// <summary>
/// 평가 기준 엔티티 (ComplianceAssessment의 Child Entity)
/// AssessmentCriterionId는 [GenerateEntityId] 속성에 의해 소스 생성기로 자동 생성됩니다.
/// </summary>
[GenerateEntityId]
public sealed class AssessmentCriterion : Entity<AssessmentCriterionId>
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Option<CriterionResult> Result { get; private set; }
    public Option<string> Notes { get; private set; }
    public Option<DateTime> EvaluatedAt { get; private set; }

    private AssessmentCriterion(
        AssessmentCriterionId id,
        string name,
        string description)
        : base(id)
    {
        Name = name;
        Description = description;
    }

    /// <summary>
    /// Create: 평가 기준을 생성합니다.
    /// </summary>
    public static AssessmentCriterion Create(string name, string description) =>
        new(AssessmentCriterionId.New(), name, description);

    /// <summary>
    /// CreateFromValidated: ORM/Repository 복원용 (검증 없음)
    /// </summary>
    public static AssessmentCriterion CreateFromValidated(
        AssessmentCriterionId id,
        string name,
        string description,
        Option<CriterionResult> result,
        Option<string> notes,
        Option<DateTime> evaluatedAt)
    {
        return new AssessmentCriterion(id, name, description)
        {
            Result = result,
            Notes = notes,
            EvaluatedAt = evaluatedAt
        };
    }

    /// <summary>
    /// 평가 기준을 평가합니다.
    /// </summary>
    public AssessmentCriterion Evaluate(CriterionResult result, Option<string> notes)
    {
        Result = result;
        Notes = notes;
        EvaluatedAt = DateTime.UtcNow;
        return this;
    }
}
