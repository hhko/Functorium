using Functorium.Domains.ValueObjects;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;

namespace BindSequentialValidation.ValueObjects;

/// <summary>
/// Address 값 객체 - 순차 검증(Bind) 패턴 예제
/// DomainError 라이브러리를 사용한 간결한 구현
/// </summary>
public sealed class Address : ValueObject
{
    public sealed record CountryPostalCodeMismatch : DomainErrorType.Custom;
    public string Street { get; }
    public string City { get; }
    public string PostalCode { get; }
    public string Country { get; }

    private Address(string street, string city, string postalCode, string country)
    {
        Street = street;
        City = city;
        PostalCode = postalCode;
        Country = country;
    }

    public static Fin<Address> Create(string street, string city, string postalCode, string country) =>
        CreateFromValidation(
            Validate(street, city, postalCode, country),
            v => new Address(v.Street, v.City, v.PostalCode, v.Country));

    public static Address CreateFromValidated((string Street, string City, string PostalCode, string Country) v) =>
        new(v.Street, v.City, v.PostalCode, v.Country);

    // 순차 검증 - Bind 패턴 (의존 검증 규칙들을 순차적으로 실행)
    public static Validation<Error, (string Street, string City, string PostalCode, string Country)> Validate(
        string street, string city, string postalCode, string country) =>
        ValidateStreetFormat(street)
            .Bind(_ => ValidateCityFormat(city))
            .Bind(_ => ValidatePostalCodeFormat(postalCode))
            .Bind(_ => ValidateCountryAndPostalCodeMatch(country, postalCode))
            .Map(_ => (street, city, postalCode, country));

    private static Validation<Error, string> ValidateStreetFormat(string street) =>
        !string.IsNullOrWhiteSpace(street) && street.Length >= 5
            ? street
            : DomainError.For<Address>(new DomainErrorType.TooShort(), street,
                $"Street is too short. Minimum length is 5 characters. Current value: '{street}'");

    private static Validation<Error, string> ValidateCityFormat(string city) =>
        !string.IsNullOrWhiteSpace(city) && city.Length >= 2
            ? city
            : DomainError.For<Address>(new DomainErrorType.TooShort(), city,
                $"City is too short. Minimum length is 2 characters. Current value: '{city}'");

    private static Validation<Error, string> ValidatePostalCodeFormat(string postalCode) =>
        !string.IsNullOrWhiteSpace(postalCode) && postalCode.Length >= 5
            ? postalCode
            : DomainError.For<Address>(new DomainErrorType.TooShort(), postalCode,
                $"Postal code is too short. Minimum length is 5 characters. Current value: '{postalCode}'");

    private static Validation<Error, string> ValidateCountryAndPostalCodeMatch(string country, string postalCode) =>
        (country, postalCode) switch
        {
            ("KR", var code) when code.Length == 5 && code.All(char.IsDigit) => country,
            ("US", var code) when code.Length == 5 && code.All(char.IsDigit) => country,
            ("JP", var code) when code.Length == 7 && code.All(char.IsDigit) => country,
            _ => DomainError.For<Address>(new CountryPostalCodeMismatch(), $"{country}:{postalCode}",
                $"Country and postal code format do not match. Current value: '{country}:{postalCode}'")
        };

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return PostalCode;
        yield return Country;
    }
}
