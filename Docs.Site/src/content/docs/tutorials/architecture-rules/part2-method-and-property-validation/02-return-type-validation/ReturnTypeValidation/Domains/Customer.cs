namespace ReturnTypeValidation.Domains;

public sealed class Customer
{
    public string Name { get; }
    public Email Email { get; }

    private Customer(string name, Email email)
    {
        Name = name;
        Email = email;
    }

    public static Customer CreateFromValidated(string name, Email email)
        => new(name, email);
}
