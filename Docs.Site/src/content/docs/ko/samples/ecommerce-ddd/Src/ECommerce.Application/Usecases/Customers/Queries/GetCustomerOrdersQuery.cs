using Functorium.Applications.Queries;
using ECommerce.Application.Usecases.Customers.Ports;
using ECommerce.Domain.AggregateRoots.Customers;

namespace ECommerce.Application.Usecases.Customers.Queries;

/// <summary>
/// 고객 주문 상세 조회 Query - 4-table JOIN 패턴 데모
/// Customer → Order → OrderLine → Product 4개 테이블을 결합하여
/// 특정 고객의 모든 주문과 각 주문의 상품명까지 포함합니다.
/// </summary>
public sealed class GetCustomerOrdersQuery
{
    public sealed record Request(string CustomerId) : IQueryRequest<Response>;

    public sealed record Response(CustomerOrdersDto CustomerOrders);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.CustomerId).MustBeEntityId<Request, CustomerId>();
        }
    }

    public sealed class Usecase(ICustomerOrdersQuery readAdapter)
        : IQueryUsecase<Request, Response>
    {
        private readonly ICustomerOrdersQuery _readAdapter = readAdapter;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var customerId = CustomerId.Create(request.CustomerId);

            FinT<IO, Response> usecase =
                from customerOrders in _readAdapter.GetByCustomerId(customerId)
                select new Response(customerOrders);

            Fin<Response> response = await usecase.Run().RunAsync();

            return response.ToFinResponse();
        }
    }
}
