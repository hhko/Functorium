namespace EcommerceFiltering.Domain.ValueObjects;

public sealed record Category(string Value)
{
    public static implicit operator string(Category cat) => cat.Value;
}
