namespace CustomerManagement.Domain.ValueObjects;

public sealed record CustomerName(string Value)
{
    public static implicit operator string(CustomerName name) => name.Value;
    public override string ToString() => Value;
}
