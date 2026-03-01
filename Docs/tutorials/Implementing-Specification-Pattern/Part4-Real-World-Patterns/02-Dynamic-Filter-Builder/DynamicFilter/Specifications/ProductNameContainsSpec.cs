using Functorium.Domains.Specifications;

namespace DynamicFilter.Specifications;

public sealed class ProductNameContainsSpec(string searchTerm) : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity)
        => entity.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
}
