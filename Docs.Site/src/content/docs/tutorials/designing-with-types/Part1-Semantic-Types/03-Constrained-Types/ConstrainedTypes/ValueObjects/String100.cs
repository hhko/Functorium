using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations.Typed;

namespace ConstrainedTypes.ValueObjects;

/// <summary>
/// 최대 100자 문자열 값 객체
/// </summary>
public sealed class String100 : SimpleValueObject<string>
{
    private String100(string value) : base(value) { }

    public static Fin<String100> Create(string value) =>
        CreateFromValidation(Validate(value), v => new String100(v));

    public static Validation<Error, string> Validate(string value) =>
        ValidationRules<String100>.NotEmpty(value)
            .ThenMaxLength(100);
}
