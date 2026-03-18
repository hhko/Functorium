using ECommerce.Domain.AggregateRoots.Customers;
using ECommerce.Domain.AggregateRoots.Inventories;
using ECommerce.Domain.AggregateRoots.Orders;
using ECommerce.Domain.AggregateRoots.Orders.ValueObjects;
using ECommerce.Domain.AggregateRoots.Products;
using Functorium.Applications.Linq;
using Functorium.Applications.Errors;
using static Functorium.Applications.Errors.ApplicationErrorType;
using ECommerce.Domain.SharedModels.Services;

namespace ECommerce.Application.Usecases.Orders.Commands;

/// <summary>
/// 주문 접수 Command - 다중 Aggregate 쓰기(UoW) 패턴 데모
/// 신용 검증 + 주문 생성 + 재고 차감을 하나의 트랜잭션으로 처리합니다.
/// </summary>
public sealed class PlaceOrderCommand
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
    /// 재고 차감 결과 DTO
    /// </summary>
    public sealed record DeductedStockInfo(string ProductId, int RemainingStock);

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
        Seq<DeductedStockInfo> DeductedStocks,
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
    /// Command Handler - 신용 검증 + 주문 생성 + 재고 차감을 원자적으로 처리
    /// </summary>
    public sealed class Usecase(
        ICustomerRepository customerRepository,
        IOrderRepository orderRepository,
        IInventoryRepository inventoryRepository,
        IProductCatalog productCatalog)
        : ICommandUsecase<Request, Response>
    {
        private readonly ICustomerRepository _customerRepository = customerRepository;
        private readonly IOrderRepository _orderRepository = orderRepository;
        private readonly IInventoryRepository _inventoryRepository = inventoryRepository;
        private readonly IProductCatalog _productCatalog = productCatalog;
        private readonly OrderCreditCheckService _creditCheckService = new();

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // Phase 1: VO 파싱 → 배치 가격 조회 → OrderLine 생성 → Order 생성
            var customerId = CustomerId.Create(request.CustomerId);
            var shippingAddress = ShippingAddress.Create(request.ShippingAddress).ThrowIfFail();

            var lineRequests = request.OrderLines
                .Select(lineReq => (
                    ProductId: ProductId.Create(lineReq.ProductId),
                    Quantity: Quantity.Create(lineReq.Quantity).ThrowIfFail()))
                .ToList();

            var productIds = lineRequests.Select(l => l.ProductId).Distinct().ToList();
            var pricesResult = await _productCatalog.GetPricesForProducts(productIds).Run().RunAsync();
            if (pricesResult.IsFail)
                return FinResponse.Fail<Response>(pricesResult.Match(
                    Succ: _ => throw new InvalidOperationException(), Fail: e => e));

            var priceLookup = pricesResult.ThrowIfFail().ToDictionary(p => p.Id, p => p.Price);

            var orderLines = new List<OrderLine>();
            foreach (var (productId, quantity) in lineRequests)
            {
                if (!priceLookup.TryGetValue(productId, out var unitPrice))
                    return FinResponse.Fail<Response>(ApplicationError.For<PlaceOrderCommand>(
                        new NotFound(),
                        productId.ToString(),
                        $"Product not found: '{productId}'"));

                orderLines.Add(OrderLine.Create(productId, quantity, unitPrice).ThrowIfFail());
            }

            var order = Order.Create(customerId, orderLines, shippingAddress).ThrowIfFail();

            // Phase 2: 재고 검증 + 차감 (명령형)
            var deductedInventories = new List<Inventory>();
            foreach (var (productId, quantity) in lineRequests)
            {
                var inventoryResult = await _inventoryRepository.GetByProductId(productId).Run().RunAsync();
                if (inventoryResult.IsFail)
                    return FinResponse.Fail<Response>(inventoryResult.Match(
                        Succ: _ => throw new InvalidOperationException(), Fail: e => e));

                var inventory = inventoryResult.ThrowIfFail();
                var deductResult = inventory.DeductStock(quantity);
                if (deductResult.IsFail)
                    return FinResponse.Fail<Response>(deductResult.Match(
                        Succ: _ => throw new InvalidOperationException(), Fail: e => e));

                deductedInventories.Add(inventory);
            }

            // Phase 2 continued: 고객 신용 검증
            var customerResult = await _customerRepository.GetById(customerId).Run().RunAsync();
            if (customerResult.IsFail)
                return FinResponse.Fail<Response>(customerResult.Match(
                    Succ: _ => throw new InvalidOperationException(), Fail: e => e));

            var customer = customerResult.ThrowIfFail();
            var creditResult = _creditCheckService.ValidateCreditLimit(customer, order.TotalAmount);
            if (creditResult.IsFail)
                return FinResponse.Fail<Response>(creditResult.Match(
                    Succ: _ => throw new InvalidOperationException(), Fail: e => e));

            // Phase 3: FinT 체인 — 다중 Aggregate 쓰기 (Order + Inventory)
            FinT<IO, Response> usecase =
                _orderRepository.Create(order).Bind(saved =>
                _inventoryRepository.UpdateRange(deductedInventories).Map(updatedInventories =>
                    new Response(
                        saved.Id.ToString(),
                        Seq(saved.OrderLines.Select(l => new OrderLineResponse(
                            l.ProductId.ToString(),
                            l.Quantity,
                            l.UnitPrice,
                            l.LineTotal))),
                        saved.TotalAmount,
                        saved.ShippingAddress,
                        updatedInventories.Select(inv => new DeductedStockInfo(
                            inv.ProductId.ToString(),
                            inv.StockQuantity)),
                        saved.CreatedAt)));

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
