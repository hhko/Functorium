using Functorium.Domains.Errors;
using static Functorium.Domains.Errors.DomainErrorKind;
using static LanguageExt.Prelude;

namespace AiGovernance.Domain.AggregateRoots.Deployments.ValueObjects;

/// <summary>
/// 배포 상태 값 객체 (Smart Enum + 상태 전이 패턴)
/// Draft → PendingReview → Active → Decommissioned
/// Active → Quarantined → Active (Remediate)
/// PendingReview → Rejected
/// </summary>
public sealed class DeploymentStatus : SimpleValueObject<string>
{
    #region Error Types

    public sealed record InvalidValue : DomainErrorKind.Custom;

    #endregion

    public static readonly DeploymentStatus Draft = new("Draft");
    public static readonly DeploymentStatus PendingReview = new("PendingReview");
    public static readonly DeploymentStatus Active = new("Active");
    public static readonly DeploymentStatus Quarantined = new("Quarantined");
    public static readonly DeploymentStatus Decommissioned = new("Decommissioned");
    public static readonly DeploymentStatus Rejected = new("Rejected");

    private static readonly HashMap<string, DeploymentStatus> All = HashMap(
        ("Draft", Draft),
        ("PendingReview", PendingReview),
        ("Active", Active),
        ("Quarantined", Quarantined),
        ("Decommissioned", Decommissioned),
        ("Rejected", Rejected));

    private static readonly HashMap<string, Seq<string>> AllowedTransitions = HashMap(
        ("Draft", Seq("PendingReview")),
        ("PendingReview", Seq("Active", "Rejected")),
        ("Active", Seq("Quarantined", "Decommissioned")),
        ("Quarantined", Seq("Active", "Decommissioned")));

    private DeploymentStatus(string value) : base(value) { }

    public static Fin<DeploymentStatus> Create(string value) =>
        Validate(value).ToFin();

    public static Validation<Error, DeploymentStatus> Validate(string value) =>
        All.Find(value)
            .ToValidation(DomainError.For<DeploymentStatus>(
                new InvalidValue(),
                currentValue: value,
                message: $"Invalid deployment status: '{value}'"));

    public static DeploymentStatus CreateFromValidated(string value) =>
        All.Find(value)
            .IfNone(() => throw new InvalidOperationException(
                $"Invalid deployment status for CreateFromValidated: '{value}'"));

    public bool CanTransitionTo(DeploymentStatus target) =>
        AllowedTransitions.Find(Value)
            .Map(allowed => allowed.Any(v => v == target.Value))
            .IfNone(false);

    public static implicit operator string(DeploymentStatus status) => status.Value;
}
