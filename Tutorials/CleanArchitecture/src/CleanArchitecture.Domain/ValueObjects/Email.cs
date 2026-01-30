using System.Text.RegularExpressions;

using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations.Typed;

using LanguageExt;
using LanguageExt.Common;

namespace CleanArchitecture.Domain.ValueObjects;

public sealed partial class Email : SimpleValueObject<string>
{
    private static readonly Regex EmailPattern = GetEmailRegex();

    private Email(string value) : base(value) { }

    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new Email(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<Email>.NotEmpty(value ?? "")
            .ThenMatches(EmailPattern)
            .ThenNormalize(v => v.ToLowerInvariant());

    public static implicit operator string(Email email) => email.Value;

    [GeneratedRegex(@"^[^@]+@[^@]+\.[^@]+$")]
    private static partial Regex GetEmailRegex();
}
