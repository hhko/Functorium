using Functorium.Domains.Errors;
using static Functorium.Domains.Errors.DomainErrorKind;
using static LanguageExt.Prelude;

namespace AiGovernance.Domain.AggregateRoots.Assessments.ValueObjects;

/// <summary>
/// 컴플라이언스 평가 상태 값 객체 (Smart Enum + 상태 전이 패턴)
/// Initiated → InProgress → Passed/Failed/RequiresRemediation
/// </summary>
public sealed class AssessmentStatus : SimpleValueObject<string>
{
    #region Error Types

    public sealed record InvalidValue : DomainErrorKind.Custom;

    #endregion

    public static readonly AssessmentStatus Initiated = new("Initiated");
    public static readonly AssessmentStatus InProgress = new("InProgress");
    public static readonly AssessmentStatus Passed = new("Passed");
    public static readonly AssessmentStatus Failed = new("Failed");
    public static readonly AssessmentStatus RequiresRemediation = new("RequiresRemediation");

    private static readonly HashMap<string, AssessmentStatus> All = HashMap(
        ("Initiated", Initiated),
        ("InProgress", InProgress),
        ("Passed", Passed),
        ("Failed", Failed),
        ("RequiresRemediation", RequiresRemediation));

    private static readonly HashMap<string, Seq<string>> AllowedTransitions = HashMap(
        ("Initiated", Seq("InProgress")),
        ("InProgress", Seq("Passed", "Failed", "RequiresRemediation")));

    private AssessmentStatus(string value) : base(value) { }

    public static Fin<AssessmentStatus> Create(string value) =>
        Validate(value).ToFin();

    public static Validation<Error, AssessmentStatus> Validate(string value) =>
        All.Find(value)
            .ToValidation(DomainError.For<AssessmentStatus>(
                new InvalidValue(),
                currentValue: value,
                message: $"Invalid assessment status: '{value}'"));

    public static AssessmentStatus CreateFromValidated(string value) =>
        All.Find(value)
            .IfNone(() => throw new InvalidOperationException(
                $"Invalid assessment status for CreateFromValidated: '{value}'"));

    public bool CanTransitionTo(AssessmentStatus target) =>
        AllowedTransitions.Find(Value)
            .Map(allowed => allowed.Any(v => v == target.Value))
            .IfNone(false);

    public static implicit operator string(AssessmentStatus status) => status.Value;
}
