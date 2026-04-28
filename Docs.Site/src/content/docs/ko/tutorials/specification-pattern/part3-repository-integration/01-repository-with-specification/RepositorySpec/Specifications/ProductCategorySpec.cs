using Functorium.Domains.Specifications;

namespace RepositorySpec.Specifications;

public sealed class ProductCategorySpec(string category) : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity) =>
        entity.Category.Equals(category, StringComparison.OrdinalIgnoreCase);
}
