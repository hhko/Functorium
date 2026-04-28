namespace DesigningWithTypes.SharedModels.ValueObjects;

/// <summary>
/// 최대 50자 문자열 값 객체 (향상: string? 입력, NotNull, ThenNormalize)
/// </summary>
public sealed class String50 : SimpleValueObject<string>
{
    public const int MaxLength = 50;

    private String50(string value) : base(value) { }

    public static Fin<String50> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new String50(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<String50>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenNormalize(v => v.Trim())
            .ThenMaxLength(MaxLength);

    public static String50 CreateFromValidated(string value) => new(value);

    public static implicit operator string(String50 vo) => vo.Value;
}
