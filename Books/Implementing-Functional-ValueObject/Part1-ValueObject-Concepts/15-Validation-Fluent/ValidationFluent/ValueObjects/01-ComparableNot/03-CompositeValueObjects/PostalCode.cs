using System.Text.RegularExpressions;
using Framework.Layers.Domains;
using Framework.Layers.Domains.Validations;
using LanguageExt;
using LanguageExt.Common;

namespace ValidationFluent.ValueObjects.ComparableNot.CompositeValueObjects;

/// <summary>
/// 우편번호를 나타내는 값 객체
/// Validate&lt;T&gt; Fluent API를 사용한 간결한 검증
/// </summary>
public sealed partial class PostalCode : SimpleValueObject<string>
{
    private static readonly Regex DigitsPattern = DigitsRegex();

    private PostalCode(string value) : base(value) { }

    public static Fin<PostalCode> Create(string value) =>
        CreateFromValidation(Validate(value), validValue => new PostalCode(validValue));

    public static PostalCode CreateFromValidated(string validatedValue) =>
        new PostalCode(validatedValue);

    public static Validation<Error, string> Validate(string value) =>
        ValidationRules<PostalCode>.NotEmpty(value ?? "")
            .ThenExactLength(5)
            .ThenMatches(DigitsPattern, "Postal code must contain only digits");

    [GeneratedRegex(@"^\d+$", RegexOptions.Compiled)]
    private static partial Regex DigitsRegex();
}
