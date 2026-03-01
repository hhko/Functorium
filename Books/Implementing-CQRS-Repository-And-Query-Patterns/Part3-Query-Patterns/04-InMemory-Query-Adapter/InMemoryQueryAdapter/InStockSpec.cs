using Functorium.Domains.Specifications;

namespace InMemoryQueryAdapter;

/// <summary>
/// 재고가 있는 상품만 필터링하는 Specification.
/// </summary>
public sealed class InStockSpec : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity) => entity.IsInStock;
}
