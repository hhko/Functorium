using ECommerce.Domain.AggregateRoots.Customers;
using ECommerce.Domain.AggregateRoots.Orders;
using ECommerce.Domain.AggregateRoots.Orders.ValueObjects;
using ECommerce.Domain.AggregateRoots.Products;
using Functorium.Applications.Linq;
using Functorium.Applications.Errors;
using static Functorium.Applications.Errors.ApplicationErrorType;
using ECommerce.Domain.SharedModels.Services;
using ECommerce.Application.Usecases.Orders.Ports;

namespace ECommerce.Application.Usecases.Orders.Commands;

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
            RuleFor(x => x.CustomerId).MustBeEntityId<Request, CustomerId>();

            RuleFor(x => x.OrderLines)
                .Must(lines => !lines.IsEmpty).WithMessage("At least one order line is required");

            RuleForEach(x => x.OrderLines).ChildRules(line =>
            {
                line.RuleFor(l => l.ProductId).MustBeEntityId<OrderLineRequest, ProductId>();
                line.RuleFor(l => l.Quantity)
                    .GreaterThan(0).WithMessage("Order quantity must be greater than 0");
            });

            RuleFor(x => x.ShippingAddress).MustSatisfyValidation(ShippingAddress.Validate);
        }
    }

    /// <summary>
    /// Command Handler - Domain Service(OrderCreditCheckService)를 사용하여 신용 한도 검증
    /// </summary>
    public sealed class Usecase(
        ICustomerRepository customerRepository,
        IOrderRepository orderRepository,
        IProductCatalog productCatalog)
        : ICommandUsecase<Request, Response>
    {
        private readonly ICustomerRepository _customerRepository = customerRepository;
        private readonly IOrderRepository _orderRepository = orderRepository;
        private readonly IProductCatalog _productCatalog = productCatalog;
        private readonly OrderCreditCheckService _creditCheckService = new();

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var customerId = CustomerId.Create(request.CustomerId);
            var shippingAddress = ShippingAddress.Create(request.ShippingAddress).ThrowIfFail();

            var lineRequests = request.OrderLines
                .Select(lineReq => (
                    ProductId: ProductId.Create(lineReq.ProductId),
                    Quantity: Quantity.Create(lineReq.Quantity).ThrowIfFail()))
                .ToList();

            // 배치 가격 조회 (단일 라운드트립)
            var productIds = lineRequests.Select(l => l.ProductId).Distinct().ToList();
            var pricesResult = await _productCatalog.GetPricesForProducts(productIds).Run().RunAsync();
            if (pricesResult.IsFail)
                return FinResponse.Fail<Response>(pricesResult.Match(
                    Succ: _ => throw new InvalidOperationException(), Fail: e => e));

            var priceLookup = pricesResult.ThrowIfFail().ToDictionary(p => p.Id, p => p.Price);

            // 존재 검증 + OrderLine 생성
            var orderLines = new List<OrderLine>();
            foreach (var (productId, quantity) in lineRequests)
            {
                if (!priceLookup.TryGetValue(productId, out var unitPrice))
                    return FinResponse.Fail<Response>(ApplicationError.For<CreateOrderWithCreditCheckCommand>(
                        new NotFound(),
                        productId.ToString(),
                        $"Product not found: '{productId}'"));

                orderLines.Add(OrderLine.Create(productId, quantity, unitPrice).ThrowIfFail());
            }

            var order = Order.Create(customerId, orderLines, shippingAddress).ThrowIfFail();

            // 고객 조회 → 신용 검증 → 저장
            FinT<IO, Response> usecase =
                from customer in _customerRepository.GetById(customerId)
                from _ in _creditCheckService.ValidateCreditLimit(customer, order.TotalAmount)
                from saved in _orderRepository.Create(order)
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
