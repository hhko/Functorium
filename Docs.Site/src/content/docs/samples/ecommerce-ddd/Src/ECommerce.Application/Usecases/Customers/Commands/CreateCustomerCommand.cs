using ECommerce.Domain.AggregateRoots.Customers;
using ECommerce.Domain.AggregateRoots.Customers.Specifications;
using ECommerce.Domain.AggregateRoots.Customers.ValueObjects;
using Functorium.Applications.Errors;
using Functorium.Applications.Linq;
using static Functorium.Applications.Errors.ApplicationErrorType;

namespace ECommerce.Application.Usecases.Customers.Commands;

/// <summary>
/// 고객 생성 Command
/// Email 중복 검사 + 공유 VO(Money) 사용
/// </summary>
public sealed class CreateCustomerCommand
{
    /// <summary>
    /// Command Request
    /// </summary>
    public sealed record Request(
        string Name,
        string Email,
        decimal CreditLimit) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response
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
            RuleFor(x => x.Name).MustSatisfyValidation(CustomerName.Validate);
            RuleFor(x => x.Email).MustSatisfyValidation(Email.Validate);
            RuleFor(x => x.CreditLimit).MustSatisfyValidation(Money.Validate);
        }
    }

    /// <summary>
    /// Command Handler
    /// </summary>
    public sealed class Usecase(
        ICustomerRepository customerRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly ICustomerRepository _customerRepository = customerRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var name = CustomerName.Create(request.Name).Unwrap();
            var email = Email.Create(request.Email).Unwrap();
            var creditLimit = Money.Create(request.CreditLimit).Unwrap();

            FinT<IO, Response> usecase =
                from exists in _customerRepository.Exists(new CustomerEmailSpec(email))
                from _ in guard(!exists, ApplicationError.For<CreateCustomerCommand>(
                    new AlreadyExists(),
                    request.Email,
                    $"Email already exists: '{request.Email}'"))
                from customer in _customerRepository.Create(
                    Customer.Create(name, email, creditLimit))
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
