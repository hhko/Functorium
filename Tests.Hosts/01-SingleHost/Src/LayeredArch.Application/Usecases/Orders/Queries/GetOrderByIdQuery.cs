using LayeredArch.Application.Usecases.Orders.Ports;
using LayeredArch.Domain.AggregateRoots.Orders;

namespace LayeredArch.Application.Usecases.Orders.Queries;

/// <summary>
/// ID로 주문 조회 Query
/// </summary>
public sealed class GetOrderByIdQuery
{
    /// <summary>
    /// 주문 라인 응답 DTO
    /// </summary>
    public sealed record OrderLineResponse(
        string ProductId,
        int Quantity,
        decimal UnitPrice,
        decimal LineTotal);

    /// <summary>
    /// Query Request
    /// </summary>
    public sealed record Request(string OrderId) : IQueryRequest<Response>;

    /// <summary>
    /// Request Validator
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.OrderId).MustBeEntityId<Request, OrderId>();
        }
    }

    /// <summary>
    /// Query Response
    /// </summary>
    public sealed record Response(
        string OrderId,
        Seq<OrderLineResponse> OrderLines,
        decimal TotalAmount,
        string ShippingAddress,
        DateTime CreatedAt);

    /// <summary>
    /// Query Handler
    /// </summary>
    public sealed class Usecase(IOrderDetailQuery orderDetailQuery)
        : IQueryUsecase<Request, Response>
    {
        private readonly IOrderDetailQuery _adapter = orderDetailQuery;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var orderId = OrderId.Create(request.OrderId);
            FinT<IO, Response> usecase =
                from dto in _adapter.GetById(orderId)
                select new Response(
                    dto.OrderId,
                    dto.OrderLines.Select(l => new OrderLineResponse(
                        l.ProductId, l.Quantity, l.UnitPrice, l.LineTotal)).ToSeq(),
                    dto.TotalAmount,
                    dto.ShippingAddress,
                    dto.CreatedAt);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
