using System.Text.RegularExpressions;

namespace LayeredArch.Domain.AggregateRoots.Customers.ValueObjects;

/// <summary>
/// 이메일 값 객체
/// </summary>
public sealed partial class Email : SimpleValueObject<string>
{
    public const int MaxLength = 320;

    private Email(string value) : base(value) { }

    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new Email(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<Email>.NotEmpty(value ?? "")
            .ThenMaxLength(MaxLength)
            .ThenMatches(EmailRegex(), "올바른 이메일 형식이 아닙니다")
            .ThenNormalize(v => v.Trim().ToLowerInvariant());

    public static Email CreateFromValidated(string value) => new(value);

    public static implicit operator string(Email email) => email.Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();
}
