using Functorium.Applications.Queries;
using LayeredArch.Application.Usecases.Orders.Ports;
using LayeredArch.Domain.AggregateRoots.Orders;

namespace LayeredArch.Application.Usecases.Orders.Queries;

/// <summary>
/// 주문+상품명 단건 조회 Query - 3-table JOIN 패턴 데모
/// Order → OrderLine → Product 3개 테이블을 결합하여 상품명 포함 주문 상세를 조회합니다.
/// </summary>
public sealed class GetOrderWithProductsQuery
{
    public sealed record Request(string OrderId) : IQueryRequest<Response>;

    public sealed record Response(OrderWithProductsDto Order);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.OrderId)
                .NotEmpty()
                .WithMessage("OrderId is required");
        }
    }

    public sealed class Usecase(IOrderWithProductsQuery readAdapter)
        : IQueryUsecase<Request, Response>
    {
        private readonly IOrderWithProductsQuery _readAdapter = readAdapter;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var orderId = OrderId.Create(request.OrderId);

            FinT<IO, Response> usecase =
                from order in _readAdapter.GetById(orderId)
                select new Response(order);

            Fin<Response> response = await usecase.Run().RunAsync();

            return response.ToFinResponse();
        }
    }
}
