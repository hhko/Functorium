namespace CustomerManagement.Domain.ValueObjects;

public sealed record Email(string Value)
{
    public static implicit operator string(Email email) => email.Value;
    public override string ToString() => Value;
}
