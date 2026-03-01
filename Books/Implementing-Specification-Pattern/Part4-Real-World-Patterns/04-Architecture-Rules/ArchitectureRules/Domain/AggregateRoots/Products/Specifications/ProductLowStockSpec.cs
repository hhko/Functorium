using Functorium.Domains.Specifications;

namespace ArchitectureRules.Domain.AggregateRoots.Products.Specifications;

public sealed class ProductLowStockSpec(int threshold) : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity) => entity.Stock < threshold;
}
