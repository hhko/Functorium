using Functorium.Domains.Errors;
using static Functorium.Domains.Errors.DomainErrorType;
using static LanguageExt.Prelude;

namespace AiGovernance.Domain.AggregateRoots.Incidents.ValueObjects;

/// <summary>
/// 인시던트 상태 값 객체 (Smart Enum + 상태 전이 패턴)
/// Reported → Investigating → Resolved
/// Reported/Investigating → Escalated
/// </summary>
public sealed class IncidentStatus : SimpleValueObject<string>
{
    #region Error Types

    public sealed record InvalidValue : DomainErrorType.Custom;

    #endregion

    public static readonly IncidentStatus Reported = new("Reported");
    public static readonly IncidentStatus Investigating = new("Investigating");
    public static readonly IncidentStatus Resolved = new("Resolved");
    public static readonly IncidentStatus Escalated = new("Escalated");

    private static readonly HashMap<string, IncidentStatus> All = HashMap(
        ("Reported", Reported),
        ("Investigating", Investigating),
        ("Resolved", Resolved),
        ("Escalated", Escalated));

    private static readonly HashMap<string, Seq<string>> AllowedTransitions = HashMap(
        ("Reported", Seq("Investigating", "Escalated")),
        ("Investigating", Seq("Resolved", "Escalated")));

    private IncidentStatus(string value) : base(value) { }

    public static Fin<IncidentStatus> Create(string value) =>
        Validate(value).ToFin();

    public static Validation<Error, IncidentStatus> Validate(string value) =>
        All.Find(value)
            .ToValidation(DomainError.For<IncidentStatus>(
                new InvalidValue(),
                currentValue: value,
                message: $"Invalid incident status: '{value}'"));

    public static IncidentStatus CreateFromValidated(string value) =>
        All.Find(value)
            .IfNone(() => throw new InvalidOperationException(
                $"Invalid incident status for CreateFromValidated: '{value}'"));

    public bool CanTransitionTo(IncidentStatus target) =>
        AllowedTransitions.Find(Value)
            .Map(allowed => allowed.Any(v => v == target.Value))
            .IfNone(false);

    public static implicit operator string(IncidentStatus status) => status.Value;
}
