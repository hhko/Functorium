using Functorium.Domains.Specifications;

namespace InMemoryImpl.Specifications;

public sealed class ProductInStockSpec : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity) => entity.Stock > 0;
}
