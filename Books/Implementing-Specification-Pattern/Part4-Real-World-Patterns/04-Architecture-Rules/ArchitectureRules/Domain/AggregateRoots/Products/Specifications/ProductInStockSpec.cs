using Functorium.Domains.Specifications;

namespace ArchitectureRules.Domain.AggregateRoots.Products.Specifications;

public sealed class ProductInStockSpec : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity) => entity.Stock > 0;
}
