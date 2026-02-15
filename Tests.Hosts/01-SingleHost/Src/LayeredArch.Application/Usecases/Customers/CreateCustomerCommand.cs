using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Customers.Specifications;
using LayeredArch.Domain.AggregateRoots.Customers.ValueObjects;
using Functorium.Applications.Errors;
using Functorium.Applications.Linq;
using static Functorium.Applications.Errors.ApplicationErrorType;

namespace LayeredArch.Application.Usecases.Customers;

/// <summary>
/// 고객 생성 Command - Apply 패턴 데모
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
                .NotEmpty().WithMessage("고객명은 필수입니다")
                .MaximumLength(CustomerName.MaxLength).WithMessage($"고객명은 {CustomerName.MaxLength}자를 초과할 수 없습니다");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("이메일은 필수입니다")
                .MaximumLength(Email.MaxLength).WithMessage($"이메일은 {Email.MaxLength}자를 초과할 수 없습니다");

            RuleFor(x => x.CreditLimit)
                .GreaterThan(0).WithMessage("신용 한도는 0보다 커야 합니다");
        }
    }

    /// <summary>
    /// Command Handler - Apply 패턴 적용
    /// </summary>
    public sealed class Usecase(
        ICustomerRepository customerRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly ICustomerRepository _customerRepository = customerRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // 1. Value Object 생성 (Apply 패턴)
            var customerResult = CreateCustomer(request);

            // 2. 검증 실패 시 조기 반환
            if (customerResult.IsFail)
            {
                return customerResult.Match(
                    Succ: _ => throw new InvalidOperationException(),
                    Fail: error => FinResponse.Fail<Response>(error));
            }

            // 3. Email 생성 (중복 검사용)
            var email = Email.Create(request.Email).ThrowIfFail();

            // 4. 중복 검사 및 저장
            FinT<IO, Response> usecase =
                from exists in _customerRepository.Exists(new CustomerEmailSpec(email))
                from _ in guard(!exists, ApplicationError.For<CreateCustomerCommand>(
                    new AlreadyExists(),
                    request.Email,
                    $"이미 등록된 이메일입니다: '{request.Email}'"))
                from customer in _customerRepository.Create((Customer)customerResult)
                select new Response(
                    customer.Id.ToString(),
                    customer.Name,
                    customer.Email,
                    customer.CreditLimit,
                    customer.CreatedAt);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }

        /// <summary>
        /// Apply 패턴: VO Validate() + Apply 병합
        /// </summary>
        private static Fin<Customer> CreateCustomer(Request request)
        {
            var name = CustomerName.Validate(request.Name);
            var email = Email.Validate(request.Email);
            var creditLimit = Money.Validate(request.CreditLimit);

            return (name, email, creditLimit)
                .Apply((n, e, c) =>
                    Customer.Create(
                        CustomerName.Create(n).ThrowIfFail(),
                        Email.Create(e).ThrowIfFail(),
                        Money.Create(c).ThrowIfFail()))
                .As()
                .ToFin();
        }
    }
}
