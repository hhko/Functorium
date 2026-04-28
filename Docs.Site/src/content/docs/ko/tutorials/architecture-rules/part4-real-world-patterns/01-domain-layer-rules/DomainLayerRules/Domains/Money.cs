using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

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

    public static Fin<Money> Create(decimal amount, string currency)
        => amount < 0
            ? Fin.Fail<Money>("Amount cannot be negative")
            : new Money(amount, currency);

    public static Validation<Error, Money> Validate(decimal amount, string currency)
        => amount < 0
            ? Fail<Error, Money>(Error.New("Amount cannot be negative"))
            : Success<Error, Money>(new Money(amount, currency));

    public override string ToString() => $"{Amount} {Currency}";
}
