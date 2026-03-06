namespace DomainLayerRules.Domains;

public sealed class Money : IValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency)
        => new(amount, currency);

    public override string ToString() => $"{Amount} {Currency}";
}
