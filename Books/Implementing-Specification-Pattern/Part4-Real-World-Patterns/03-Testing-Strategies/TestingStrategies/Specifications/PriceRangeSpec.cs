using Functorium.Domains.Specifications;

namespace TestingStrategies.Specifications;

public sealed class PriceRangeSpec : Specification<Product>
{
    public decimal MinPrice { get; }
    public decimal MaxPrice { get; }

    public PriceRangeSpec(decimal minPrice, decimal maxPrice)
    {
        MinPrice = minPrice;
        MaxPrice = maxPrice;
    }

    public override bool IsSatisfiedBy(Product entity)
        => entity.Price >= MinPrice && entity.Price <= MaxPrice;
}
