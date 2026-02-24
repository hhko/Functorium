using Functorium.Domains.Specifications;

namespace ArchitectureRules.Domain.AggregateRoots.Products.Specifications;

public sealed class ProductLowStockSpec : Specification<Product>
{
    public int Threshold { get; }

    public ProductLowStockSpec(int threshold) => Threshold = threshold;

    public override bool IsSatisfiedBy(Product entity) => entity.Stock < Threshold;
}
