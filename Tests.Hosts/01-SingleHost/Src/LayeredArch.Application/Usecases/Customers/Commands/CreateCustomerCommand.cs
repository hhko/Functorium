using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Customers.Specifications;
using LayeredArch.Domain.AggregateRoots.Customers.ValueObjects;
using Functorium.Applications.Errors;
using Functorium.Applications.Linq;
using static Functorium.Applications.Errors.ApplicationErrorType;

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
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Customer name is required")
                .MaximumLength(CustomerName.MaxLength).WithMessage($"Customer name must not exceed {CustomerName.MaxLength} characters");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .MaximumLength(Email.MaxLength).WithMessage($"Email must not exceed {Email.MaxLength} characters");

            RuleFor(x => x.CreditLimit)
                .GreaterThan(0).WithMessage("Credit limit must be greater than 0");
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
            // 파이프라인 Validator가 검증 완료. Create()는 정규화 목적.
            var name = CustomerName.Create(request.Name).Unwrap();
            var email = Email.Create(request.Email).Unwrap();
            var creditLimit = Money.Create(request.CreditLimit).Unwrap();

            var customer = Customer.Create(name, email, creditLimit);

            FinT<IO, Response> usecase =
                from exists in _customerRepository.Exists(new CustomerEmailSpec(email))
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
