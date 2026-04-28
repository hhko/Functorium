using Functorium.Domains.Errors;
using static Functorium.Domains.Errors.DomainErrorKind;
using static LanguageExt.Prelude;

namespace AiGovernance.Domain.AggregateRoots.Deployments.ValueObjects;

/// <summary>
/// 배포 환경 값 객체 (Smart Enum 패턴)
/// </summary>
public sealed class DeploymentEnvironment : SimpleValueObject<string>
{
    #region Error Types

    public sealed record InvalidValue : DomainErrorKind.Custom;

    #endregion

    public static readonly DeploymentEnvironment Staging = new("Staging");
    public static readonly DeploymentEnvironment Production = new("Production");

    private static readonly HashMap<string, DeploymentEnvironment> All = HashMap(
        ("Staging", Staging),
        ("Production", Production));

    private DeploymentEnvironment(string value) : base(value) { }

    public static Fin<DeploymentEnvironment> Create(string value) =>
        Validate(value).ToFin();

    public static Validation<Error, DeploymentEnvironment> Validate(string value) =>
        All.Find(value)
            .ToValidation(DomainError.For<DeploymentEnvironment>(
                new InvalidValue(),
                currentValue: value,
                message: $"Invalid deployment environment: '{value}'"));

    public static DeploymentEnvironment CreateFromValidated(string value) =>
        All.Find(value)
            .IfNone(() => throw new InvalidOperationException(
                $"Invalid deployment environment for CreateFromValidated: '{value}'"));

    public static implicit operator string(DeploymentEnvironment env) => env.Value;
}
