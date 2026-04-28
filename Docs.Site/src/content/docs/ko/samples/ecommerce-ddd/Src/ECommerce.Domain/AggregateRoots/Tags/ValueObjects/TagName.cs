namespace ECommerce.Domain.AggregateRoots.Tags;

/// <summary>
/// 태그명 값 객체
/// </summary>
public sealed class TagName : SimpleValueObject<string>
{
    public const int MaxLength = 50;

    private TagName(string value) : base(value) { }

    public static Fin<TagName> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new TagName(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<TagName>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenNormalize(v => v.Trim())
            .ThenMaxLength(MaxLength);

    public static TagName CreateFromValidated(string value) => new(value);

    public static implicit operator string(TagName tagName) => tagName.Value;
}
