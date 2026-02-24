using Functorium.Domains.Specifications;

namespace TestingStrategies.Specifications;

public sealed class ProductNameUniqueSpec : Specification<Product>
{
    public string Name { get; }

    public ProductNameUniqueSpec(string name) => Name = name;

    public override bool IsSatisfiedBy(Product entity)
        => entity.Name.Equals(Name, StringComparison.OrdinalIgnoreCase);
}
