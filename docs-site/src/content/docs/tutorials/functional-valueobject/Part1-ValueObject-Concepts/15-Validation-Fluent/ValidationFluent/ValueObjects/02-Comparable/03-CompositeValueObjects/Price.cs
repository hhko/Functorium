using Framework.Layers.Domains;
using Framework.Layers.Domains.Validations;
using LanguageExt;
using LanguageExt.Common;

namespace ValidationFluent.ValueObjects.Comparable.CompositeValueObjects;

/// <summary>
/// 가격을 나타내는 복합 값 객체
/// Validate&lt;T&gt; Fluent API를 사용한 간결한 검증
/// </summary>
public sealed class Price : ComparableValueObject
{
    public MoneyAmount Amount { get; }
    public Currency Currency { get; }

    private Price(MoneyAmount amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Fin<Price> Create(decimal amount, string currency) =>
        CreateFromValidation(
            Validate(amount, currency),
            validValues => new Price(validValues.Amount, validValues.Currency));

    public static Price CreateFromValidated((MoneyAmount Amount, Currency Currency) validatedValues) =>
        new Price(validatedValues.Amount, validatedValues.Currency);

    public static Validation<Error, (MoneyAmount Amount, Currency Currency)> Validate(decimal amount, string currency) =>
        from validAmount in MoneyAmount.Validate(amount)
        from validCurrency in Currency.Validate(currency)
        select (Amount: MoneyAmount.CreateFromValidated(validAmount),
                Currency: Currency.CreateFromValidated(validCurrency));

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return Currency.Value;
        yield return (decimal)Amount;
    }

    public bool CanCompareWith(Price other) =>
        Currency.Equals(other.Currency);

    public override string ToString() =>
        $"{Currency} {Amount}";
}
