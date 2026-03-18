using System.Collections.Concurrent;
using Functorium.Adapters.Repositories;
using Functorium.Domains.Specifications;

namespace InMemoryQueryAdapter;

public sealed class InMemoryOrderSummaryQuery : InMemoryQueryBase<Order, OrderSummaryDto>
{
    private readonly ConcurrentDictionary<OrderId, Order> _orderStore = new();
    private readonly ConcurrentDictionary<ProductId, Product> _productStore;

    protected override string DefaultSortField => "ProductName";

    public InMemoryOrderSummaryQuery(ConcurrentDictionary<ProductId, Product> productStore)
    {
        _productStore = productStore;
    }

    public void AddOrder(Order order) => _orderStore[order.Id] = order;

    protected override IEnumerable<OrderSummaryDto> GetProjectedItems(Specification<Order> spec) =>
        from order in _orderStore.Values
        where spec.IsSatisfiedBy(order)
        join product in _productStore.Values
            on order.ProductId equals product.Id
        select new OrderSummaryDto(
            order.Id.ToString(),
            product.Name,
            product.Category,
            order.Quantity,
            product.Price,
            order.TotalAmount);

    protected override Func<OrderSummaryDto, object> SortSelector(string fieldName) =>
        fieldName switch
        {
            "ProductName" => dto => dto.ProductName,
            "Quantity" => dto => dto.Quantity,
            "TotalAmount" => dto => dto.TotalAmount,
            "Category" => dto => dto.Category,
            _ => dto => dto.ProductName
        };
}
