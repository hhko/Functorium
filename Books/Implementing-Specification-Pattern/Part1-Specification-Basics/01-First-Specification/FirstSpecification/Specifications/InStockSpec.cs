using Functorium.Domains.Specifications;

namespace FirstSpecification.Specifications;

public sealed class InStockSpec : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity) => entity.Stock > 0;
}
