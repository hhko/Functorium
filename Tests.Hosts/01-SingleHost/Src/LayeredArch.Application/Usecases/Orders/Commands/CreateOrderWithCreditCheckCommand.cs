using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Orders;
using LayeredArch.Domain.AggregateRoots.Orders.ValueObjects;
using LayeredArch.Domain.AggregateRoots.Products;
using Functorium.Applications.Linq;
using Functorium.Applications.Errors;
using static Functorium.Applications.Errors.ApplicationErrorType;
using LayeredArch.Domain.SharedModels.Services;
using LayeredArch.Application.Usecases.Orders.Ports;

namespace LayeredArch.Application.Usecases.Orders.Commands;

/// <summary>
/// 신용 한도 검증 후 주문 생성 Command - Domain Service 사용 데모
/// OrderCreditCheckService로 고객 신용 한도를 검증한 후 주문을 생성합니다.
/// </summary>
public sealed class CreateOrderWithCreditCheckCommand
{
    /// <summary>
    /// 주문 라인 요청 DTO
    /// </summary>
    public sealed record OrderLineRequest(string ProductId, int Quantity);

    /// <summary>
    /// 주문 라인 응답 DTO
    /// </summary>
    public sealed record OrderLineResponse(
        string ProductId,
        int Quantity,
        decimal UnitPrice,
        decimal LineTotal);

    /// <summary>
    /// Command Request
    /// </summary>
    public sealed record Request(
        string CustomerId,
        Seq<OrderLineRequest> OrderLines,
        string ShippingAddress) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response
    /// </summary>
    public sealed record Response(
        string OrderId,
        Seq<OrderLineResponse> OrderLines,
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
                .NotEmpty().WithMessage("Customer ID is required");

            RuleFor(x => x.OrderLines)
                .Must(lines => !lines.IsEmpty).WithMessage("At least one order line is required");

            RuleForEach(x => x.OrderLines).ChildRules(line =>
            {
                line.RuleFor(l => l.ProductId)
                    .NotEmpty().WithMessage("Product ID is required");
                line.RuleFor(l => l.Quantity)
                    .GreaterThan(0).WithMessage("Order quantity must be greater than 0");
            });

            RuleFor(x => x.ShippingAddress)
                .NotEmpty().WithMessage("Shipping address is required")
                .MaximumLength(ShippingAddress.MaxLength).WithMessage($"Shipping address must not exceed {ShippingAddress.MaxLength} characters");
        }
    }

    /// <summary>
    /// Command Handler - Domain Service(OrderCreditCheckService)를 사용하여 신용 한도 검증
    /// </summary>
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

            if (shippingAddressResult.IsFail)
                return FinResponse.Fail<Response>(shippingAddressResult.Match(Succ: _ => throw new InvalidOperationException(), Fail: e => e));

            var customerId = CustomerId.Create(request.CustomerId);
            var shippingAddress = (ShippingAddress)shippingAddressResult;

            // 2. 각 라인별 상품 검증 및 가격 조회 → OrderLine 생성
            var orderLines = new List<OrderLine>();
            foreach (var lineReq in request.OrderLines)
            {
                var productId = ProductId.Create(lineReq.ProductId);
                var quantityResult = Quantity.Create(lineReq.Quantity);
                if (quantityResult.IsFail)
                    return FinResponse.Fail<Response>(quantityResult.Match(Succ: _ => throw new InvalidOperationException(), Fail: e => e));

                var quantity = (Quantity)quantityResult;

                var existsResult = await _productCatalog.ExistsById(productId).Run().RunAsync();
                if (existsResult.IsFail)
                    return FinResponse.Fail<Response>(existsResult.Match(Succ: _ => throw new InvalidOperationException(), Fail: e => e));
                if (!existsResult.ThrowIfFail())
                    return FinResponse.Fail<Response>(ApplicationError.For<CreateOrderWithCreditCheckCommand>(
                        new NotFound(),
                        lineReq.ProductId,
                        $"Product not found: '{lineReq.ProductId}'"));

                var priceResult = await _productCatalog.GetPrice(productId).Run().RunAsync();
                if (priceResult.IsFail)
                    return FinResponse.Fail<Response>(priceResult.Match(Succ: _ => throw new InvalidOperationException(), Fail: e => e));

                var unitPrice = priceResult.ThrowIfFail();
                var orderLineResult = OrderLine.Create(productId, quantity, unitPrice);
                if (orderLineResult.IsFail)
                    return FinResponse.Fail<Response>(orderLineResult.Match(Succ: _ => throw new InvalidOperationException(), Fail: e => e));

                orderLines.Add(orderLineResult.ThrowIfFail());
            }

            // 3. Order 생성
            var orderResult = Order.Create(customerId, orderLines, shippingAddress);
            if (orderResult.IsFail)
                return FinResponse.Fail<Response>(orderResult.Match(Succ: _ => throw new InvalidOperationException(), Fail: e => e));

            var newOrder = orderResult.ThrowIfFail();

            // 4. 고객 조회 → 신용 검증 → 저장
            FinT<IO, Response> usecase =
                from customer in _customerRepository.GetById(customerId)
                from _ in _creditCheckService.ValidateCreditLimit(customer, newOrder.TotalAmount)
                from saved in _orderRepository.Create(newOrder)
                select new Response(
                    saved.Id.ToString(),
                    Seq(saved.OrderLines.Select(l => new OrderLineResponse(
                        l.ProductId.ToString(),
                        l.Quantity,
                        l.UnitPrice,
                        l.LineTotal))),
                    saved.TotalAmount,
                    saved.ShippingAddress,
                    saved.CreatedAt);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
