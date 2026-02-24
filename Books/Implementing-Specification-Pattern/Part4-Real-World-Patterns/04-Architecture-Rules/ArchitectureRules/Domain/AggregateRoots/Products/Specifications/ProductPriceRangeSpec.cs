using Functorium.Domains.Specifications;

namespace ArchitectureRules.Domain.AggregateRoots.Products.Specifications;

public sealed class ProductPriceRangeSpec : Specification<Product>
{
    public decimal MinPrice { get; }
    public decimal MaxPrice { get; }

    public ProductPriceRangeSpec(decimal minPrice, decimal maxPrice)
    {
        MinPrice = minPrice;
        MaxPrice = maxPrice;
    }

    public override bool IsSatisfiedBy(Product entity)
        => entity.Price >= MinPrice && entity.Price <= MaxPrice;
}
