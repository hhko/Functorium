using Functorium.Domains.Specifications;

namespace InMemoryImpl.Specifications;

public sealed class ProductPriceRangeSpec(decimal min, decimal max) : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity) =>
        entity.Price >= min && entity.Price <= max;
}
