namespace CustomerManagement.Domain.ValueObjects;

public sealed record CustomerId(Guid Value)
{
    public static CustomerId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
