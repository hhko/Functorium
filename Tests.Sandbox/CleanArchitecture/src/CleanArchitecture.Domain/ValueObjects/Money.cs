using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations;
using Functorium.Domains.ValueObjects.Validations.Typed;

using LanguageExt;
using LanguageExt.Common;

namespace CleanArchitecture.Domain.ValueObjects;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public static Fin<Money> Create(decimal amount, string currency) =>
        CreateFromValidation(Validate(amount, currency), v => new Money(v.Amount, v.Currency));

    public static Validation<Error, (decimal Amount, string Currency)> Validate(decimal amount, string currency) =>
        (ValidateAmount(amount), ValidateCurrency(currency))
            .Apply((a, c) => (Amount: a, Currency: c));

    private static Validation<Error, decimal> ValidateAmount(decimal amount) =>
        ValidationRules<Money>.NonNegative(amount);

    private static Validation<Error, string> ValidateCurrency(string currency) =>
        ValidationRules<Money>.NotEmpty(currency)
            .ThenNormalize(v => v.ToUpperInvariant())
            .ThenExactLength(3);

    public override string ToString() => $"{Amount:N2} {Currency}";
}
