using Functorium.Domains.Specifications;

namespace TestingStrategies.Specifications;

public sealed class CategorySpec : Specification<Product>
{
    public string Category { get; }

    public CategorySpec(string category) => Category = category;

    public override bool IsSatisfiedBy(Product entity)
        => entity.Category.Equals(Category, StringComparison.OrdinalIgnoreCase);
}
