using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Orders;
using LayeredArch.Domain.AggregateRoots.Orders.ValueObjects;
using LayeredArch.Domain.AggregateRoots.Products;
using Functorium.Applications.Linq;
using Functorium.Applications.Errors;
using static Functorium.Applications.Errors.ApplicationErrorType;
using LayeredArch.Domain.SharedModels.Services;
namespace LayeredArch.Application.Usecases.Orders;

/// <summary>
/// 신용 한도 검증 후 주문 생성 Command - Domain Service 사용 데모
/// OrderCreditCheckService로 고객 신용 한도를 검증한 후 주문을 생성합니다.
/// </summary>
public sealed class CreateOrderWithCreditCheckCommand
{
    /// <summary>
    /// Command Request
    /// </summary>
    public sealed record Request(
        string CustomerId,
        string ProductId,
        int Quantity,
        string ShippingAddress) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response
    /// </summary>
    public sealed record Response(
        string OrderId,
        string ProductId,
        int Quantity,
        decimal UnitPrice,
        decimal TotalAmount,
        string ShippingAddress,
        DateTime CreatedAt);

    /// <summary>
    /// Request Validator
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.CustomerId)
                .NotEmpty().WithMessage("고객 ID는 필수입니다");

            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("상품 ID는 필수입니다");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("주문 수량은 0보다 커야 합니다");

            RuleFor(x => x.ShippingAddress)
                .NotEmpty().WithMessage("배송지 주소는 필수입니다")
                .MaximumLength(ShippingAddress.MaxLength).WithMessage($"배송지 주소는 {ShippingAddress.MaxLength}자를 초과할 수 없습니다");
        }
    }

    /// <summary>
    /// Command Handler - Domain Service(OrderCreditCheckService)를 사용하여 신용 한도 검증
    /// </summary>
    /// <remarks>
    /// 실행 흐름:
    /// 1. 고객 조회 (ICustomerRepository)
    /// 2. 상품 존재 및 가격 검증 (IProductCatalog)
    /// 3. 신용 한도 검증 (OrderCreditCheckService - 순수 도메인 로직)
    /// 4. 주문 생성 및 영속화 (IOrderRepository)
    /// 5. 도메인 이벤트 발행 (IDomainEventPublisher)
    /// </remarks>
    public sealed class Usecase(
        ICustomerRepository customerRepository,
        IOrderRepository orderRepository,
        IProductCatalog productCatalog,
        OrderCreditCheckService creditCheckService)
        : ICommandUsecase<Request, Response>
    {
        private readonly ICustomerRepository _customerRepository = customerRepository;
        private readonly IOrderRepository _orderRepository = orderRepository;
        private readonly IProductCatalog _productCatalog = productCatalog;
        private readonly OrderCreditCheckService _creditCheckService = creditCheckService;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // 1. Value Object 생성
            var shippingAddressResult = ShippingAddress.Create(request.ShippingAddress);
            var quantityResult = Quantity.Create(request.Quantity);

            if (shippingAddressResult.IsFail)
                return FinResponse.Fail<Response>(shippingAddressResult.Match(Succ: _ => throw new InvalidOperationException(), Fail: e => e));
            if (quantityResult.IsFail)
                return FinResponse.Fail<Response>(quantityResult.Match(Succ: _ => throw new InvalidOperationException(), Fail: e => e));

            var customerId = CustomerId.Create(request.CustomerId);
            var productId = ProductId.Create(request.ProductId);
            var shippingAddress = (ShippingAddress)shippingAddressResult;
            var quantity = (Quantity)quantityResult;

            // 2. 조회 → 신용 검증(Domain Service) → 주문 생성 → 이벤트 발행
            FinT<IO, Response> usecase =
                from customer in _customerRepository.GetById(customerId)
                from exists in _productCatalog.ExistsById(productId)
                from _1 in guard(exists, ApplicationError.For<CreateOrderWithCreditCheckCommand>(
                    new NotFound(),
                    request.ProductId,
                    $"상품을 찾을 수 없습니다: '{request.ProductId}'"))
                from unitPrice in _productCatalog.GetPrice(productId)
                from _2 in _creditCheckService.ValidateCreditLimit(customer, unitPrice.Multiply(quantity))   // Domain Service 호출
                from order in _orderRepository.Create(
                    Order.Create(productId, quantity, unitPrice, shippingAddress))
                select new Response(
                    order.Id.ToString(),
                    order.ProductId.ToString(),
                    order.Quantity,
                    order.UnitPrice,
                    order.TotalAmount,
                    order.ShippingAddress,
                    order.CreatedAt);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
