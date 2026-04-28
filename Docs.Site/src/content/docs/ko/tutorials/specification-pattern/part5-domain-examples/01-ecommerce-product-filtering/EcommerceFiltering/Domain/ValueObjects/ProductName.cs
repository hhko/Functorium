namespace EcommerceFiltering.Domain.ValueObjects;

public sealed record ProductName(string Value)
{
    public static implicit operator string(ProductName name) => name.Value;
    public override string ToString() => Value;
}
