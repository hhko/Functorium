using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;
using Functorium.Domains.Errors;

namespace ComparableValueObjectComposite.ValueObjects;

/// <summary>
/// 도시명을 나타내는 비교 가능한 값 객체
/// DomainError 라이브러리를 사용한 간결한 구현
/// </summary>
public sealed class City : ComparableSimpleValueObject<string>
{
    private City(string value) : base(value) { }

    public static Fin<City> Create(string value) =>
        CreateFromValidation(Validate(value), v => new City(v));

    public static City CreateFromValidated(string validatedValue) =>
        new(validatedValue);

    public static Validation<Error, string> Validate(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length >= 2 && value.Length <= 30
            ? value.Trim()
            : DomainError.For<City>(new DomainErrorKind.WrongLength(), value,
                $"City name length is invalid. Must be 2-30 characters. Current value: '{value}'");

    public override string ToString() => Value;
}
