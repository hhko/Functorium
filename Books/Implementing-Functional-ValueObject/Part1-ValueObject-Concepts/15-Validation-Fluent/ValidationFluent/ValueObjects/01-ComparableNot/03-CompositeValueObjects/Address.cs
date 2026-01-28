using Functorium.Domains.ValueObjects;
using LanguageExt;
using LanguageExt.Common;

namespace ValidationFluent.ValueObjects.ComparableNot.CompositeValueObjects;

/// <summary>
/// 주소를 나타내는 복합 값 객체 (3개 Validation 조합 예제)
/// Validate&lt;T&gt; Fluent API를 사용한 간결한 검증
/// </summary>
public sealed class Address : ValueObject
{
    public Street Street { get; }
    public City City { get; }
    public PostalCode PostalCode { get; }

    private Address(Street street, City city, PostalCode postalCode)
    {
        Street = street;
        City = city;
        PostalCode = postalCode;
    }

    public static Fin<Address> Create(string streetValue, string cityValue, string postalCodeValue) =>
        CreateFromValidation(
            Validate(streetValue, cityValue, postalCodeValue),
            validValues => new Address(
                validValues.Street,
                validValues.City,
                validValues.PostalCode));

    public static Address CreateFromValidated(Street street, City city, PostalCode postalCode) =>
        new Address(street, city, postalCode);

    public static Validation<Error, (Street Street, City City, PostalCode PostalCode)> Validate(
            string street,
            string city,
            string postalCode) =>
        from validStreet in Street.Validate(street)
        from validCity in City.Validate(city)
        from validPostalCode in PostalCode.Validate(postalCode)
        select
        (
            Street: Street.CreateFromValidated(validStreet),
            City: City.CreateFromValidated(validCity),
            PostalCode: PostalCode.CreateFromValidated(validPostalCode)
        );

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return PostalCode;
    }

    public override string ToString() =>
        $"{Street}, {City} {PostalCode}";
}
