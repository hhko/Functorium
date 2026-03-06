using Functorium.Domains.Specifications;

namespace DynamicFilter.Specifications;

public sealed class ProductInStockSpec : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity) => entity.Stock > 0;
}
