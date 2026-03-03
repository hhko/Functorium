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

    public static Address Create(string city, string street) => new(city, street);
    public override string ToString() => $"{City}, {Street}";
}
