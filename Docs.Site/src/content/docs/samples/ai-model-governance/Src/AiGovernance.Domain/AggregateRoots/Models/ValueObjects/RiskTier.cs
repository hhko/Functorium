using Functorium.Domains.Errors;
using static Functorium.Domains.Errors.DomainErrorType;
using static LanguageExt.Prelude;

namespace AiGovernance.Domain.AggregateRoots.Models.ValueObjects;

/// <summary>
/// AI 모델 위험 등급 값 객체 (Smart Enum 패턴)
/// EU AI Act 기반 4단계 분류: Minimal, Limited, High, Unacceptable
/// </summary>
public sealed class RiskTier : SimpleValueObject<string>
{
    #region Error Types

    public sealed record InvalidValue : DomainErrorType.Custom;

    #endregion

    public static readonly RiskTier Minimal = new("Minimal");
    public static readonly RiskTier Limited = new("Limited");
    public static readonly RiskTier High = new("High");
    public static readonly RiskTier Unacceptable = new("Unacceptable");

    private static readonly HashMap<string, RiskTier> All = HashMap(
        ("Minimal", Minimal),
        ("Limited", Limited),
        ("High", High),
        ("Unacceptable", Unacceptable));

    private RiskTier(string value) : base(value) { }

    public static Fin<RiskTier> Create(string value) =>
        Validate(value).ToFin();

    public static Validation<Error, RiskTier> Validate(string value) =>
        All.Find(value)
            .ToValidation(DomainError.For<RiskTier>(
                new InvalidValue(),
                currentValue: value,
                message: $"Invalid risk tier: '{value}'"));

    public static RiskTier CreateFromValidated(string value) =>
        All.Find(value)
            .IfNone(() => throw new InvalidOperationException(
                $"Invalid risk tier for CreateFromValidated: '{value}'"));

    public bool RequiresComplianceAssessment => this == High || this == Unacceptable;

    public bool IsProhibited => this == Unacceptable;

    public static implicit operator string(RiskTier tier) => tier.Value;
}
