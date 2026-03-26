namespace AiGovernance.Domain.AggregateRoots.Models.ValueObjects;

/// <summary>
/// AI 모델명 값 객체
/// </summary>
public sealed class ModelName : SimpleValueObject<string>
{
    public const int MaxLength = 100;

    private ModelName(string value) : base(value) { }

    public static Fin<ModelName> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new ModelName(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<ModelName>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenNormalize(v => v.Trim())
            .ThenMaxLength(MaxLength);

    public static ModelName CreateFromValidated(string value) => new(value);

    public static implicit operator string(ModelName modelName) => modelName.Value;
}
