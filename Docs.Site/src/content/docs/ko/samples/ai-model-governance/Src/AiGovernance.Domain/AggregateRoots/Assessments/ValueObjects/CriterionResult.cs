using Functorium.Domains.Errors;
using static Functorium.Domains.Errors.DomainErrorType;
using static LanguageExt.Prelude;

namespace AiGovernance.Domain.AggregateRoots.Assessments.ValueObjects;

/// <summary>
/// 평가 기준 결과 값 객체 (Smart Enum 패턴)
/// </summary>
public sealed class CriterionResult : SimpleValueObject<string>
{
    #region Error Types

    public sealed record InvalidValue : DomainErrorType.Custom;

    #endregion

    public static readonly CriterionResult Pass = new("Pass");
    public static readonly CriterionResult Fail = new("Fail");
    public static readonly CriterionResult NotApplicable = new("NotApplicable");

    private static readonly HashMap<string, CriterionResult> All = HashMap(
        ("Pass", Pass),
        ("Fail", Fail),
        ("NotApplicable", NotApplicable));

    private CriterionResult(string value) : base(value) { }

    public static Fin<CriterionResult> Create(string value) =>
        Validate(value).ToFin();

    public static Validation<Error, CriterionResult> Validate(string value) =>
        All.Find(value)
            .ToValidation(DomainError.For<CriterionResult>(
                new InvalidValue(),
                currentValue: value,
                message: $"Invalid criterion result: '{value}'"));

    public static CriterionResult CreateFromValidated(string value) =>
        All.Find(value)
            .IfNone(() => throw new InvalidOperationException(
                $"Invalid criterion result for CreateFromValidated: '{value}'"));

    public static implicit operator string(CriterionResult result) => result.Value;
}
