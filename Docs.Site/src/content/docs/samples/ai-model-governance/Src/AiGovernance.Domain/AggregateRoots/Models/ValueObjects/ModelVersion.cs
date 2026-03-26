using System.Text.RegularExpressions;

namespace AiGovernance.Domain.AggregateRoots.Models.ValueObjects;

/// <summary>
/// AI 모델 버전 값 객체 (SemVer 형식)
/// </summary>
public sealed partial class ModelVersion : SimpleValueObject<string>
{
    private ModelVersion(string value) : base(value) { }

    public static Fin<ModelVersion> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new ModelVersion(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<ModelVersion>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenNormalize(v => v.Trim())
            .ThenMatches(SemVerPattern(), "Invalid SemVer format");

    public static ModelVersion CreateFromValidated(string value) => new(value);

    public static implicit operator string(ModelVersion version) => version.Value;

    [GeneratedRegex(@"^\d+\.\d+\.\d+(-[\w.]+)?$")]
    private static partial Regex SemVerPattern();
}
