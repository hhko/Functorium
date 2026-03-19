using ECommerce.Domain.AggregateRoots.Orders;
using Functorium.Applications.Linq;

namespace ECommerce.Application.Usecases.Orders.Commands;

/// <summary>
/// 주문 취소 Command - Pending/Confirmed 상태의 주문을 취소
/// 취소 시 CancelledEvent가 발생하여 도메인 이벤트 핸들러가 재고 복원 등을 처리
/// </summary>
public sealed class CancelOrderCommand
{
    /// <summary>
    /// Command Request
    /// </summary>
    public sealed record Request(
        string OrderId) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response
    /// </summary>
    public sealed record Response(
        string OrderId,
        DateTime CancelledAt);

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
    /// Command Handler - Order.Cancel() 도메인 로직 위임
    /// </summary>
    public sealed class Usecase(
        IOrderRepository orderRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly IOrderRepository _orderRepository = orderRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var orderId = OrderId.Create(request.OrderId);

            FinT<IO, Response> usecase =
                from order in _orderRepository.GetById(orderId)
                from _1 in order.Cancel()
                from updated in _orderRepository.Update(order)
                select new Response(
                    updated.Id.ToString(),
                    updated.UpdatedAt.IfNone(DateTime.UtcNow));

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
