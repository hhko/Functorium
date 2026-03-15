namespace DesigningWithTypes.AggregateRoots.Contacts.ValueObjects;

/// <summary>
/// 메모 내용 값 객체
/// </summary>
public sealed class NoteContent : SimpleValueObject<string>
{
    public const int MaxLength = 500;

    private NoteContent(string value) : base(value) { }

    public static Fin<NoteContent> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new NoteContent(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<NoteContent>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenMaxLength(MaxLength)
            .ThenNormalize(v => v.Trim());

    public static NoteContent CreateFromValidated(string value) => new(value);

    public static implicit operator string(NoteContent vo) => vo.Value;
}
