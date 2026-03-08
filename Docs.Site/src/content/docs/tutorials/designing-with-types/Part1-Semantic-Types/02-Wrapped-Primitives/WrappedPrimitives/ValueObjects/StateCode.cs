using System.Text.RegularExpressions;
using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations.Typed;

namespace WrappedPrimitives.ValueObjects;

/// <summary>
/// 미국 주 코드 값 객체
/// 2자리 대문자 영문
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
