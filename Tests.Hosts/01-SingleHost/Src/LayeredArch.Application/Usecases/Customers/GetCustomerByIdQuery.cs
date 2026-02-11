using LayeredArch.Domain.AggregateRoots.Customers;

namespace LayeredArch.Application.Usecases.Customers;

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
    /// Query Handler
    /// </summary>
    public sealed class Usecase(ICustomerRepository customerRepository)
        : IQueryUsecase<Request, Response>
    {
        private readonly ICustomerRepository _customerRepository = customerRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var customerId = CustomerId.Create(request.CustomerId);
            FinT<IO, Response> usecase =
                from customer in _customerRepository.GetById(customerId)
                select new Response(
                    customer.Id.ToString(),
                    customer.Name,
                    customer.Email,
                    customer.CreditLimit,
                    customer.CreatedAt);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
