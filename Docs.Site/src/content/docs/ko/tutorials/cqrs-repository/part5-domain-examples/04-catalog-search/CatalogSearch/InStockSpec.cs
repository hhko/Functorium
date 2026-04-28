using Functorium.Domains.Specifications;

namespace CatalogSearch;

/// <summary>
/// 재고가 있는 상품을 필터링하는 Specification.
/// </summary>
public sealed class InStockSpec : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity) => entity.Stock > 0;
}
