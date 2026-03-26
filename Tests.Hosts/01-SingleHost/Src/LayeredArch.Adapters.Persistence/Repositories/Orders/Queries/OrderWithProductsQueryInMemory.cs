using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using LayeredArch.Application.Usecases.Orders.Ports;
using LayeredArch.Domain.AggregateRoots.Orders;
using static Functorium.Adapters.Errors.AdapterErrorType;
using static LanguageExt.Prelude;

using LayeredArch.Adapters.Persistence.Repositories.Orders.Repositories;
using LayeredArch.Adapters.Persistence.Repositories.Products.Repositories;

namespace LayeredArch.Adapters.Persistence.Repositories.Orders.Queries;

/// <summary>
/// InMemory 기반 Order + OrderLine + Product 3-table JOIN 읽기 전용 어댑터.
/// 단건 주문 조회 시 주문 라인에 상품명을 포함합니다.
/// </summary>
[GenerateObservablePort]
public class OrderWithProductsQueryInMemory : IOrderWithProductsQuery
{
    public string RequestCategory => "QueryAdapter";

    public virtual FinT<IO, OrderWithProductsDto> GetById(OrderId id)
    {
        return IO.lift(() =>
        {
            if (!OrderRepositoryInMemory.Orders.TryGetValue(id, out var order))
            {
                return AdapterError.For<OrderWithProductsQueryInMemory>(
                    new NotFound(), id.ToString(),
                    $"주문 ID '{id}'을(를) 찾을 수 없습니다");
            }

            var productLookup = ProductRepositoryInMemory.Products;
            var orderLines = toSeq(order.OrderLines.Select(l =>
            {
                var productName = productLookup.TryGetValue(l.ProductId, out var product)
                    ? (string)product.Name : "Unknown";
                return new OrderLineWithProductDto(
                    l.ProductId.ToString(), productName,
                    l.Quantity, l.UnitPrice, l.LineTotal);
            }));

            return Fin.Succ(new OrderWithProductsDto(
                order.Id.ToString(),
                order.CustomerId.ToString(),
                orderLines,
                order.TotalAmount,
                order.Status,
                order.CreatedAt));
        });
    }
}
