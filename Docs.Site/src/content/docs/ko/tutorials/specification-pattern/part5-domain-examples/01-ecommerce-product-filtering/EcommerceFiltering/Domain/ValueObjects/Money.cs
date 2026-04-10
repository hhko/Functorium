namespace EcommerceFiltering.Domain.ValueObjects;

public sealed record Money(decimal Amount)
{
    public static implicit operator decimal(Money money) => money.Amount;
    public override string ToString() => $"{Amount:N0}원";
}
