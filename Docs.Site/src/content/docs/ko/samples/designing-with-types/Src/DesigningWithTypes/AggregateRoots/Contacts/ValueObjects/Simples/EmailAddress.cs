using System.Text.RegularExpressions;

namespace DesigningWithTypes.AggregateRoots.Contacts.ValueObjects;

/// <summary>
/// 이메일 주소 값 객체 (향상: string? 입력, NotNull, ThenNormalize 소문자)
/// </summary>
public sealed partial class EmailAddress : SimpleValueObject<string>
{
    public const int MaxLength = 320;

    private EmailAddress(string value) : base(value) { }

    public static Fin<EmailAddress> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new EmailAddress(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<EmailAddress>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenNormalize(v => v.Trim().ToLowerInvariant())
            .ThenMaxLength(MaxLength)
            .ThenMatches(EmailPattern(), "Invalid email format");

    public static EmailAddress CreateFromValidated(string value) => new(value);

    public static implicit operator string(EmailAddress vo) => vo.Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
