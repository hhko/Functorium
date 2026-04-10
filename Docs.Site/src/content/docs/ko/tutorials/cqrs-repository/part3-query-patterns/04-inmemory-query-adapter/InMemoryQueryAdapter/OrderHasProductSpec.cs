using Functorium.Domains.Specifications;

namespace InMemoryQueryAdapter;

public sealed class OrderHasProductSpec : Specification<Order>
{
    private readonly ProductId _productId;

    public OrderHasProductSpec(ProductId productId) => _productId = productId;

    public override bool IsSatisfiedBy(Order entity) => entity.ProductId == _productId;
}
