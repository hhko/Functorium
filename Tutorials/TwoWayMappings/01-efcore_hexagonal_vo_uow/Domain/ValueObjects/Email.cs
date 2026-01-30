namespace MyApp.Domain.ValueObjects;

public sealed class Email
{
    public string Value { get; }
    public string Normalized { get; }

    private Email(string value)
    {
        Value = value;
        Normalized = value.Trim().ToUpperInvariant();
    }

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email is required.", nameof(value));

        if (!value.Contains("@"))
            throw new ArgumentException("Invalid email format.", nameof(value));

        return new Email(value);
    }

    public override string ToString() => Value;
}
