using Functorium.Domains.Specifications;

namespace Composition.Specifications;

public sealed class CategorySpec(string category) : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity) =>
        entity.Category.Equals(category, StringComparison.OrdinalIgnoreCase);
}
