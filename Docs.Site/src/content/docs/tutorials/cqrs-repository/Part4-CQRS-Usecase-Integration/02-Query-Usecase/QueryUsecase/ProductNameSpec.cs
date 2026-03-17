using Functorium.Domains.Specifications;

namespace QueryUsecase;

public sealed class ProductNameSpec(string keyword) : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity)
        => entity.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase);
}
