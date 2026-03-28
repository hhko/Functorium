namespace ParameterValidation.Domains;

public sealed class Address
{
    public string City { get; }
    public string Street { get; }
    public string ZipCode { get; }

    private Address(string city, string street, string zipCode)
    {
        City = city;
        Street = street;
        ZipCode = zipCode;
    }

    public static Address Create(string city, string street, string zipCode)
        => new(city, street, zipCode);
}
