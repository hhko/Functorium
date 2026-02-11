using LayeredArch.Domain.AggregateRoots.Orders;

namespace LayeredArch.Application.Usecases.Orders;

/// <summary>
/// ID로 주문 조회 Query
/// </summary>
public sealed class GetOrderByIdQuery
{
    /// <summary>
    /// Query Request
    /// </summary>
    public sealed record Request(string OrderId) : IQueryRequest<Response>;

    /// <summary>
    /// Query Response
    /// </summary>
    public sealed record Response(
        string OrderId,
        string ProductId,
        int Quantity,
        decimal UnitPrice,
        decimal TotalAmount,
        string ShippingAddress,
        DateTime CreatedAt);

    /// <summary>
    /// Query Handler
    /// </summary>
    public sealed class Usecase(IOrderRepository orderRepository)
        : IQueryUsecase<Request, Response>
    {
        private readonly IOrderRepository _orderRepository = orderRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var orderId = OrderId.Create(request.OrderId);
            FinT<IO, Response> usecase =
                from order in _orderRepository.GetById(orderId)
                select new Response(
                    order.Id.ToString(),
                    order.ProductId.ToString(),
                    order.Quantity,
                    order.UnitPrice,
                    order.TotalAmount,
                    order.ShippingAddress,
                    order.CreatedAt);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
