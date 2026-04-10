using ECommerce.Application.Usecases.Customers.Ports;
using ECommerce.Domain.AggregateRoots.Customers;

namespace ECommerce.Application.Usecases.Customers.Queries;

/// <summary>
/// ID로 고객 조회 Query
/// </summary>
public sealed class GetCustomerByIdQuery
{
    /// <summary>
    /// Query Request
    /// </summary>
    public sealed record Request(string CustomerId) : IQueryRequest<Response>;

    /// <summary>
    /// Query Response
    /// </summary>
    public sealed record Response(
        string CustomerId,
        string Name,
        string Email,
        decimal CreditLimit,
        DateTime CreatedAt);

    /// <summary>
    /// Request Validator
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.CustomerId).MustBeEntityId<Request, CustomerId>();
        }
    }

    /// <summary>
    /// Query Handler
    /// </summary>
    public sealed class Usecase(ICustomerDetailQuery customerDetailQuery)
        : IQueryUsecase<Request, Response>
    {
        private readonly ICustomerDetailQuery _adapter = customerDetailQuery;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var customerId = CustomerId.Create(request.CustomerId);
            FinT<IO, Response> usecase =
                from dto in _adapter.GetById(customerId)
                select new Response(
                    dto.CustomerId,
                    dto.Name,
                    dto.Email,
                    dto.CreditLimit,
                    dto.CreatedAt);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
