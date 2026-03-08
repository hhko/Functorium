using System.Text.RegularExpressions;
using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations.Typed;

namespace WrappedPrimitives.ValueObjects;

/// <summary>
/// 이메일 주소 값 객체
/// string을 의미 있는 타입으로 래핑하여 타입 안전성 확보
/// </summary>
public sealed partial class EmailAddress : SimpleValueObject<string>
{
    private EmailAddress(string value) : base(value) { }

    public static Fin<EmailAddress> Create(string value) =>
        CreateFromValidation(Validate(value), v => new EmailAddress(v));

    public static Validation<Error, string> Validate(string value) =>
        ValidationRules<EmailAddress>.NotEmpty(value)
            .ThenMatches(EmailPattern());

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
