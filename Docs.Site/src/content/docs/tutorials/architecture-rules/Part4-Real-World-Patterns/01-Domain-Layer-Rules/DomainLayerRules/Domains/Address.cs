using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace DomainLayerRules.Domains;

public sealed class Address : IValueObject
{
    public string City { get; }
    public string Street { get; }

    private Address(string city, string street)
    {
        City = city;
        Street = street;
    }

    public static Fin<Address> Create(string city, string street)
        => string.IsNullOrWhiteSpace(city)
            ? Fin<Address>.Fail("City cannot be empty")
            : new Address(city, street);

    public static Validation<Error, Address> Validate(string city, string street)
        => string.IsNullOrWhiteSpace(city)
            ? Fail<Error, Address>(Error.New("City cannot be empty"))
            : Success<Error, Address>(new Address(city, street));

    public override string ToString() => $"{City}, {Street}";
}
