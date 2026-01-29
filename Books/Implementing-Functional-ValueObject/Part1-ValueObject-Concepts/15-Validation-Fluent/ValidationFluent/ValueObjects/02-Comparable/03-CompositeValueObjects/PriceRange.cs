using Framework.Layers.Domains;
using Framework.Layers.Domains.Validations;
using LanguageExt;
using LanguageExt.Common;

namespace ValidationFluent.ValueObjects.Comparable.CompositeValueObjects;

/// <summary>
/// 가격 범위를 나타내는 값 객체
/// Validate&lt;T&gt; Fluent API를 사용한 간결한 검증
/// </summary>
public sealed class PriceRange : ComparableValueObject
{
    public Price MinPrice { get; }
    public Price MaxPrice { get; }

    private PriceRange(Price minPrice, Price maxPrice)
    {
        MinPrice = minPrice;
        MaxPrice = maxPrice;
    }

    public static Fin<PriceRange> Create(decimal minPriceValue, decimal maxPriceValue, string currencyCode) =>
        CreateFromValidation(
            Validate(minPriceValue, maxPriceValue, currencyCode),
            validValues => new PriceRange(validValues.MinPrice, validValues.MaxPrice));

    public static PriceRange CreateFromValidated(Price minPrice, Price maxPrice) =>
        new PriceRange(minPrice, maxPrice);

    public static Validation<Error, (Price MinPrice, Price MaxPrice)> Validate(
        decimal minPriceValue,
        decimal maxPriceValue,
        string currencyCode) =>
        from validMinPriceTuple in Price.Validate(minPriceValue, currencyCode)
        from validMaxPriceTuple in Price.Validate(maxPriceValue, currencyCode)
        from validPriceRange in ValidatePriceRange(
            Price.CreateFromValidated(validMinPriceTuple),
            Price.CreateFromValidated(validMaxPriceTuple))
        select validPriceRange;

    private static Validation<Error, (Price MinPrice, Price MaxPrice)> ValidatePriceRange(Price minPrice, Price maxPrice) =>
        ValidationRules<PriceRange>.ValidRange((decimal)minPrice.Amount, (decimal)maxPrice.Amount)
            .ToValidation()
            .Map(_ => (MinPrice: minPrice, MaxPrice: maxPrice));

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return MinPrice.Currency.Value;
        yield return (decimal)MinPrice.Amount;
        yield return (decimal)MaxPrice.Amount;
    }

    public override string ToString() =>
        $"{MinPrice} ~ {MaxPrice}";
}
