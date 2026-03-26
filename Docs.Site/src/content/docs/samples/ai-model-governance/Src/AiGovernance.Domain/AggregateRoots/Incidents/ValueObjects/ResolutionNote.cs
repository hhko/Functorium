namespace AiGovernance.Domain.AggregateRoots.Incidents.ValueObjects;

/// <summary>
/// 인시던트 해결 노트 값 객체
/// </summary>
public sealed class ResolutionNote : SimpleValueObject<string>
{
    public const int MaxLength = 2000;

    private ResolutionNote(string value) : base(value) { }

    public static Fin<ResolutionNote> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new ResolutionNote(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<ResolutionNote>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenNormalize(v => v.Trim())
            .ThenMaxLength(MaxLength);

    public static ResolutionNote CreateFromValidated(string value) => new(value);

    public static implicit operator string(ResolutionNote note) => note.Value;
}
