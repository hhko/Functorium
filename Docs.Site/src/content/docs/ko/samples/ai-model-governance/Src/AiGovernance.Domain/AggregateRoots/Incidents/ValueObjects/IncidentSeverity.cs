using Functorium.Domains.Errors;
using static Functorium.Domains.Errors.DomainErrorKind;
using static LanguageExt.Prelude;

namespace AiGovernance.Domain.AggregateRoots.Incidents.ValueObjects;

/// <summary>
/// 인시던트 심각도 값 객체 (Smart Enum 패턴)
/// </summary>
public sealed class IncidentSeverity : SimpleValueObject<string>
{
    #region Error Types

    public sealed record InvalidValue : DomainErrorKind.Custom;

    #endregion

    public static readonly IncidentSeverity Critical = new("Critical");
    public static readonly IncidentSeverity High = new("High");
    public static readonly IncidentSeverity Medium = new("Medium");
    public static readonly IncidentSeverity Low = new("Low");

    private static readonly HashMap<string, IncidentSeverity> All = HashMap(
        ("Critical", Critical),
        ("High", High),
        ("Medium", Medium),
        ("Low", Low));

    private IncidentSeverity(string value) : base(value) { }

    public static Fin<IncidentSeverity> Create(string value) =>
        Validate(value).ToFin();

    public static Validation<Error, IncidentSeverity> Validate(string value) =>
        All.Find(value)
            .ToValidation(DomainError.For<IncidentSeverity>(
                new InvalidValue(),
                currentValue: value,
                message: $"Invalid incident severity: '{value}'"));

    public static IncidentSeverity CreateFromValidated(string value) =>
        All.Find(value)
            .IfNone(() => throw new InvalidOperationException(
                $"Invalid incident severity for CreateFromValidated: '{value}'"));

    public bool RequiresQuarantine => this == Critical || this == High;

    public static implicit operator string(IncidentSeverity severity) => severity.Value;
}
