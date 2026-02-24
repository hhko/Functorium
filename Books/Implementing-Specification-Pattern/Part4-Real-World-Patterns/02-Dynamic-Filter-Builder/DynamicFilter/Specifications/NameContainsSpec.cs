using Functorium.Domains.Specifications;

namespace DynamicFilter.Specifications;

public sealed class NameContainsSpec : Specification<Product>
{
    public string SearchTerm { get; }

    public NameContainsSpec(string searchTerm) => SearchTerm = searchTerm;

    public override bool IsSatisfiedBy(Product entity)
        => entity.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase);
}
