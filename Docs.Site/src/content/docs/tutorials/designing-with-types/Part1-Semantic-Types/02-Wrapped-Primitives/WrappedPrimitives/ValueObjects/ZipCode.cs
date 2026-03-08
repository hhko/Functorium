using System.Text.RegularExpressions;
using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations.Typed;

namespace WrappedPrimitives.ValueObjects;

/// <summary>
/// 우편번호 값 객체
/// 5자리 숫자 문자열
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
