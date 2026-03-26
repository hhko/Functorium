namespace AiGovernance.Domain.AggregateRoots.Models.ValueObjects;

/// <summary>
/// AI 모델 목적 값 객체
/// </summary>
public sealed class ModelPurpose : SimpleValueObject<string>
{
    public const int MaxLength = 500;

    private ModelPurpose(string value) : base(value) { }

    public static Fin<ModelPurpose> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new ModelPurpose(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<ModelPurpose>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenNormalize(v => v.Trim())
            .ThenMaxLength(MaxLength);

    public static ModelPurpose CreateFromValidated(string value) => new(value);

    public static implicit operator string(ModelPurpose purpose) => purpose.Value;
}
