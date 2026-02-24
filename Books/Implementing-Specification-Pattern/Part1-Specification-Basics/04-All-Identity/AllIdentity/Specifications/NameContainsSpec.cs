using Functorium.Domains.Specifications;

namespace AllIdentity.Specifications;

public sealed class NameContainsSpec(string keyword) : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity) =>
        entity.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase);
}
