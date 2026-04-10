namespace EcommerceFiltering.Domain.ValueObjects;

public sealed record Quantity(int Value)
{
    public static implicit operator int(Quantity qty) => qty.Value;
}
