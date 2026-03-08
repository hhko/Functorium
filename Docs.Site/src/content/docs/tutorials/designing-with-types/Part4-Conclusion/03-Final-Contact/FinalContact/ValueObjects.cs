using System.Text.RegularExpressions;
using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations.Typed;

namespace FinalContact;

/// <summary>
/// 최대 50자 문자열 값 객체
/// </summary>
public sealed class String50 : SimpleValueObject<string>
{
    private String50(string value) : base(value) { }

    public static Fin<String50> Create(string value) =>
        CreateFromValidation(Validate(value), v => new String50(v));

    public static Validation<Error, string> Validate(string value) =>
        ValidationRules<String50>.NotEmpty(value)
            .ThenMaxLength(50);
}

/// <summary>
/// 이메일 주소 값 객체
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

/// <summary>
/// 미국 주 코드 값 객체 (2자리 대문자)
/// </summary>
public sealed partial class StateCode : SimpleValueObject<string>
{
    private StateCode(string value) : base(value) { }

    public static Fin<StateCode> Create(string value) =>
        CreateFromValidation(Validate(value), v => new StateCode(v));

    public static Validation<Error, string> Validate(string value) =>
        ValidationRules<StateCode>.NotEmpty(value)
            .ThenMatches(StatePattern());

    [GeneratedRegex(@"^[A-Z]{2}$")]
    private static partial Regex StatePattern();
}

/// <summary>
/// 우편번호 값 객체 (5자리 숫자)
/// </summary>
public sealed partial class ZipCode : SimpleValueObject<string>
{
    private ZipCode(string value) : base(value) { }

    public static Fin<ZipCode> Create(string value) =>
        CreateFromValidation(Validate(value), v => new ZipCode(v));

    public static Validation<Error, string> Validate(string value) =>
        ValidationRules<ZipCode>.NotEmpty(value)
            .ThenMatches(ZipPattern());

    [GeneratedRegex(@"^\d{5}$")]
    private static partial Regex ZipPattern();
}
