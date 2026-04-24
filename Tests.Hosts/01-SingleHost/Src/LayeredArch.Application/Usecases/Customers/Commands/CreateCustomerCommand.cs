using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Customers.Specifications;
using LayeredArch.Domain.AggregateRoots.Customers.ValueObjects;
using Functorium.Applications.Errors;
using Functorium.Applications.Linq;
using static Functorium.Applications.Errors.ApplicationErrorKind;

namespace LayeredArch.Application.Usecases.Customers.Commands;

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
            // ApplyT: VO 합성 + 에러 수집 → FinT<IO, R> LINQ from 첫 구문
            FinT<IO, Response> usecase =
                from vos in (
                    CustomerName.Create(request.Name),
                    Email.Create(request.Email),
                    Money.Create(request.CreditLimit)
                ).ApplyT((name, email, creditLimit) => (Name: name, Email: email, CreditLimit: creditLimit))
                let customer = Customer.Create(vos.Name, vos.Email, vos.CreditLimit)
                from exists in _customerRepository.Exists(new CustomerEmailSpec(vos.Email))
                from _ in guard(!exists, ApplicationError.For<CreateCustomerCommand>(
                    new AlreadyExists(),
                    request.Email,
                    $"Email already exists: '{request.Email}'"))
                from created in _customerRepository.Create(customer)
                select new Response(
                    created.Id.ToString(),
                    created.Name,
                    created.Email,
                    created.CreditLimit,
                    created.CreatedAt);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
