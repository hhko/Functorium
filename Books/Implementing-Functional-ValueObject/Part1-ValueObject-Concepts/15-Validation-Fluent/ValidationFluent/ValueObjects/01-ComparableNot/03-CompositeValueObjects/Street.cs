using Framework.Layers.Domains;
using Framework.Layers.Domains.Validations;
using LanguageExt;
using LanguageExt.Common;

namespace ValidationFluent.ValueObjects.ComparableNot.CompositeValueObjects;

/// <summary>
/// 거리명을 나타내는 값 객체
/// Validate&lt;T&gt; Fluent API를 사용한 간결한 검증
/// </summary>
public sealed class Street : SimpleValueObject<string>
{
    private Street(string value) : base(value) { }

    public static Fin<Street> Create(string value) =>
        CreateFromValidation(Validate(value), validValue => new Street(validValue));

    public static Street CreateFromValidated(string validatedValue) =>
        new Street(validatedValue);

    public static Validation<Error, string> Validate(string value) =>
        ValidationRules<Street>.NotEmpty(value ?? "");
}
