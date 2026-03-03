namespace ValueObjectConversion;

public sealed record ProductName(string Value)
{
    public static implicit operator string(ProductName name) => name.Value;
    public override string ToString() => Value;
}

public sealed record Money(decimal Amount)
{
    public static implicit operator decimal(Money money) => money.Amount;
}

public sealed record Quantity(int Value)
{
    public static implicit operator int(Quantity qty) => qty.Value;
}

public record Product(ProductName Name, Money Price, Quantity Stock, string Category);
