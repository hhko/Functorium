using Functorium.Domains.ValueObjects;
using LanguageExt;
using LanguageExt.Common;

namespace ErrorCodeFluent.ValueObjects.ComparableNot.CompositeValueObjects;

/// <summary>
/// 도시명을 나타내는 값 객체
/// DomainError 헬퍼를 사용한 간결한 에러 처리
/// </summary>
public sealed class City : SimpleValueObject<string>
{
    private City(string value) : base(value) { }

    public static Fin<City> Create(string value) =>
        CreateFromValidation(Validate(value), validValue => new City(validValue));

    internal static City CreateFromValidated(string validatedValue) =>
        new City(validatedValue);

    public static Validation<Error, string> Validate(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? DomainError.For<City>(new DomainErrorType.Empty(), value ?? "",
                $"City name cannot be empty. Current value: '{value}'")
            : value;
}
