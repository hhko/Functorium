using Functorium.Domains.Specifications;

namespace CatalogSearch;

/// <summary>
/// 가격 범위로 상품을 필터링하는 Specification.
/// </summary>
public sealed class PriceRangeSpec(decimal minPrice, decimal maxPrice) : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity) =>
        entity.Price >= minPrice && entity.Price <= maxPrice;
}
