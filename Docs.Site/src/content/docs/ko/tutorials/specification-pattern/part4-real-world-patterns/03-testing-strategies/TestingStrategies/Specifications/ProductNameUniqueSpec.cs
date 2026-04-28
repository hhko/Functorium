using Functorium.Domains.Specifications;

namespace TestingStrategies.Specifications;

public sealed class ProductNameUniqueSpec(string name) : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity)
        => entity.Name.Equals(name, StringComparison.OrdinalIgnoreCase);
}
