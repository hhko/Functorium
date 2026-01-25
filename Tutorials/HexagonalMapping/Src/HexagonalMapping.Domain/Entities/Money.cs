namespace HexagonalMapping.Domain.Entities;

/// <summary>
/// 값 객체: 통화 금액을 나타냅니다.
/// </summary>
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        return new Money(amount, currency.ToUpperInvariant());
    }

    public static Money Zero(string currency = "USD") => new(0, currency.ToUpperInvariant());

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add {Currency} and {other.Currency}");

        return new Money(Amount + other.Amount, Currency);
    }

    public override string ToString() => $"{Amount:N2} {Currency}";
}
