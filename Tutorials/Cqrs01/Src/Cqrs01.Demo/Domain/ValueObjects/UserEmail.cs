using System.Text.RegularExpressions;

using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations.Typed;

using LanguageExt;
using LanguageExt.Common;

namespace Cqrs01.Demo.Domain.ValueObjects;

/// <summary>
/// 사용자 이메일 Value Object
/// </summary>
public sealed partial class UserEmail : SimpleValueObject<string>
{
    private static readonly Regex EmailPattern = GetEmailRegex();

    private UserEmail(string value) : base(value) { }

    public static Fin<UserEmail> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new UserEmail(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<UserEmail>.NotEmpty(value ?? "")
            .ThenMatches(EmailPattern)
            .ThenNormalize(v => v.ToLowerInvariant());

    public static implicit operator string(UserEmail email) => email.ToString();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex GetEmailRegex();
}
