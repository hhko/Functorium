using ECommerce.Domain.AggregateRoots.Customers;
using ECommerce.Domain.AggregateRoots.Orders;
using ECommerce.Domain.AggregateRoots.Orders.ValueObjects;
using ECommerce.Domain.AggregateRoots.Products;
using Functorium.Applications.Errors;
using Functorium.Applications.Linq;
using static Functorium.Applications.Errors.ApplicationErrorType;
using ECommerce.Application.Usecases.Orders.Ports;

namespace ECommerce.Application.Usecases.Orders.Commands;

/// <summary>
/// 주문 생성 Command - 공유 Port(IProductCatalog) 사용 데모
/// IProductCatalog로 상품 존재/가격 검증 후 주문 생성
/// </summary>
public sealed class CreateOrderCommand
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

            RuleFor(x => x.ShippingAddress).MustSatisfyValidation(ShippingAddress.Validate);
        }
    }

    /// <summary>
    /// Command Handler - IProductCatalog 공유 Port를 사용하여 교차 Aggregate 검증
    /// </summary>
    public sealed class Usecase(
        IOrderRepository orderRepository,
        IProductCatalog productCatalog)
        : ICommandUsecase<Request, Response>
    {
        private readonly IOrderRepository _orderRepository = orderRepository;
        private readonly IProductCatalog _productCatalog = productCatalog;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // 1. Value Object 생성
            var shippingAddressResult = ShippingAddress.Create(request.ShippingAddress);

            if (shippingAddressResult.IsFail)
                return FinResponse.Fail<Response>(shippingAddressResult.Match(Succ: _ => throw new InvalidOperationException(), Fail: e => e));

            var customerId = CustomerId.Create(request.CustomerId);
            var shippingAddress = (ShippingAddress)shippingAddressResult;

            // 2. 라인별 Quantity 검증 + ProductId 수집
            var lineRequests = new List<(ProductId ProductId, Quantity Quantity)>();
            foreach (var lineReq in request.OrderLines)
            {
                var productId = ProductId.Create(lineReq.ProductId);
                var quantityResult = Quantity.Create(lineReq.Quantity);
                if (quantityResult.IsFail)
                    return FinResponse.Fail<Response>(quantityResult.Match(Succ: _ => throw new InvalidOperationException(), Fail: e => e));

                lineRequests.Add((productId, (Quantity)quantityResult));
            }

            // 3. 배치 가격 조회 (단일 라운드트립)
            var productIds = lineRequests.Select(l => l.ProductId).Distinct().ToList();
            var pricesResult = await _productCatalog.GetPricesForProducts(productIds).Run().RunAsync();
            if (pricesResult.IsFail)
                return FinResponse.Fail<Response>(pricesResult.Match(Succ: _ => throw new InvalidOperationException(), Fail: e => e));

            var priceLookup = pricesResult.ThrowIfFail().ToDictionary(p => p.Id, p => p.Price);

            // 4. 존재 검증 + OrderLine 생성
            var orderLines = new List<OrderLine>();
            foreach (var (productId, quantity) in lineRequests)
            {
                if (!priceLookup.TryGetValue(productId, out var unitPrice))
                    return FinResponse.Fail<Response>(ApplicationError.For<CreateOrderCommand>(
                        new NotFound(),
                        productId.ToString(),
                        $"Product not found: '{productId}'"));

                var orderLineResult = OrderLine.Create(productId, quantity, unitPrice);
                if (orderLineResult.IsFail)
                    return FinResponse.Fail<Response>(orderLineResult.Match(Succ: _ => throw new InvalidOperationException(), Fail: e => e));

                orderLines.Add(orderLineResult.ThrowIfFail());
            }

            // 5. Order 생성 + 저장
            var orderResult = Order.Create(customerId, orderLines, shippingAddress);
            if (orderResult.IsFail)
                return FinResponse.Fail<Response>(orderResult.Match(Succ: _ => throw new InvalidOperationException(), Fail: e => e));

            FinT<IO, Response> usecase =
                from order in _orderRepository.Create(orderResult.ThrowIfFail())
                select new Response(
                    order.Id.ToString(),
                    Seq(order.OrderLines.Select(l => new OrderLineResponse(
                        l.ProductId.ToString(),
                        l.Quantity,
                        l.UnitPrice,
                        l.LineTotal))),
                    order.TotalAmount,
                    order.ShippingAddress,
                    order.CreatedAt);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
