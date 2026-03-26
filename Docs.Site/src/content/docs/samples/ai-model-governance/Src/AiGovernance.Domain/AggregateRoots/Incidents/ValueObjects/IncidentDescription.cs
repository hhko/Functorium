namespace AiGovernance.Domain.AggregateRoots.Incidents.ValueObjects;

/// <summary>
/// 인시던트 설명 값 객체
/// </summary>
public sealed class IncidentDescription : SimpleValueObject<string>
{
    public const int MaxLength = 2000;

    private IncidentDescription(string value) : base(value) { }

    public static Fin<IncidentDescription> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new IncidentDescription(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<IncidentDescription>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenNormalize(v => v.Trim())
            .ThenMaxLength(MaxLength);

    public static IncidentDescription CreateFromValidated(string value) => new(value);

    public static implicit operator string(IncidentDescription description) => description.Value;
}
