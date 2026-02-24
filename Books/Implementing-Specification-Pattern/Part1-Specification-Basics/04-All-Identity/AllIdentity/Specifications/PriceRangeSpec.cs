using Functorium.Domains.Specifications;

namespace AllIdentity.Specifications;

public sealed class PriceRangeSpec(decimal min, decimal max) : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity) =>
        entity.Price >= min && entity.Price <= max;
}
