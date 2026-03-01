using Functorium.Domains.Specifications;

namespace DynamicFilter.Specifications;

public sealed class ProductPriceRangeSpec(decimal minPrice, decimal maxPrice) : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity)
        => entity.Price >= minPrice && entity.Price <= maxPrice;
}
