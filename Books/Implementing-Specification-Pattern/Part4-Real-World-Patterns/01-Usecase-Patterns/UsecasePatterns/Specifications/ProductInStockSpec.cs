using Functorium.Domains.Specifications;

namespace UsecasePatterns.Specifications;

public sealed class ProductInStockSpec : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity) => entity.Stock > 0;
}
