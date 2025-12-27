using Functorium.Domains.ValueObjects;
using Functorium.Abstractions.Errors;
using LanguageExt;
using LanguageExt.Common;

namespace BindSequentialValidation.ValueObjects;

// 1. public sealed 클래스 선언 - ValueObject 상속
public sealed class Address : ValueObject
{
    // 1.1 readonly 속성 선언 - 불변성 보장
    public string Street { get; }
    public string City { get; }
    public string PostalCode { get; }
    public string Country { get; }

    // 2. Private 생성자 - 단순 대입만 처리
    private Address(string street, string city, string postalCode, string country)
    {
        Street = street;
        City = city;
        PostalCode = postalCode;
        Country = country;
    }

    // 3. Public Create 메서드
    public static Fin<Address> Create(string street, string city, string postalCode, string country) =>
        CreateFromValidation(
            Validate(street, city, postalCode, country),
            validValues => new Address(
                validValues.Street,
                validValues.City,
                validValues.PostalCode,
                validValues.Country));

    // 4. Internal CreateFromValidated 메서드
    internal static Address CreateFromValidated((string Street, string City, string PostalCode, string Country) validatedValues) =>
        new Address(validatedValues.Street, validatedValues.City, validatedValues.PostalCode, validatedValues.Country);

    // 5. Public Validate 메서드 - 의존 검증 규칙들을 순차적으로 실행
    public static Validation<Error, (string Street, string City, string PostalCode, string Country)> Validate(
        string street, string city, string postalCode, string country) =>
        ValidateStreetFormat(street)
            .Bind(_ => ValidateCityFormat(city))
            .Bind(_ => ValidatePostalCodeFormat(postalCode))
            .Bind(_ => ValidateCountryAndPostalCodeMatch(country, postalCode))
            .Map(_ => (street, city, postalCode, country));

    // 5.1 도로명 형식 검증 - DomainErrors 단위로 Validate 접두사 사용
    private static Validation<Error, string> ValidateStreetFormat(string street) =>
        !string.IsNullOrWhiteSpace(street) && street.Length >= 5
            ? street                                                // 성공: 값 반환
            : DomainErrors.StreetTooShort(street);                  // 실패: 에러 반환

    // 5.2 도시 형식 검증 - 독립적 검증
    private static Validation<Error, string> ValidateCityFormat(string city) =>
        !string.IsNullOrWhiteSpace(city) && city.Length >= 2
            ? city                                                  // 성공: 값 반환
            : DomainErrors.CityTooShort(city);                      // 실패: 에러 반환

    // 5.3 우편번호 형식 검증 - 독립적 검증
    private static Validation<Error, string> ValidatePostalCodeFormat(string postalCode) =>
        !string.IsNullOrWhiteSpace(postalCode) && postalCode.Length >= 5
            ? postalCode                                            // 성공: 값 반환
            : DomainErrors.PostalCodeTooShort(postalCode);          // 실패: 에러 반환

    // 5.4 국가와 우편번호 일치 검증 - 국가와 우편번호 간의 의존성 검증
    private static Validation<Error, string> ValidateCountryAndPostalCodeMatch(string country, string postalCode) =>
        (country, postalCode) switch
        {
            ("KR", var code) when code.Length == 5 && code.All(char.IsDigit) => 
                country,
            ("US", var code) when code.Length == 5 && code.All(char.IsDigit) => 
                country,
            ("JP", var code) when code.Length == 7 && code.All(char.IsDigit) => 
                country,
            _ => DomainErrors.CountryPostalCodeMismatch(country, postalCode)
        };

    // 6. 동등성 컴포넌트 구현
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return PostalCode;
        yield return Country;
    }

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        // ValidateStreetFormat 메서드와 1:1 매핑되는 에러 - 도로명이 너무 짧음
        public static Error StreetTooShort(string street) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Address)}.{nameof(StreetTooShort)}",
                errorCurrentValue: street,
                errorMessage: "");

        // ValidateCityFormat 메서드와 1:1 매핑되는 에러 - 도시명이 너무 짧음
        public static Error CityTooShort(string city) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Address)}.{nameof(CityTooShort)}",
                errorCurrentValue: city,
                errorMessage: "");

        // ValidatePostalCodeFormat 메서드와 1:1 매핑되는 에러 - 우편번호가 너무 짧음
        public static Error PostalCodeTooShort(string postalCode) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Address)}.{nameof(PostalCodeTooShort)}",
                errorCurrentValue: postalCode,
                errorMessage: "");

        // ValidateCountryAndPostalCodeMatch 메서드와 1:1 매핑되는 에러 - 국가와 우편번호가 일치하지 않음
        public static Error CountryPostalCodeMismatch(string country, string postalCode) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Address)}.{nameof(CountryPostalCodeMismatch)}",
                errorCurrentValue: $"{country}:{postalCode}",
                errorMessage: "");
    }
}
