namespace AiGovernance.Domain.AggregateRoots.Assessments.ValueObjects;

/// <summary>
/// 컴플라이언스 평가 점수 값 객체 (0~100 범위)
/// </summary>
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

    public static implicit operator int(AssessmentScore score) => score.Value;
}
