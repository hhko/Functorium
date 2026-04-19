using ECommerce.Domain.AggregateRoots.Customers;
using ECommerce.Domain.AggregateRoots.Inventories;
using ECommerce.Domain.AggregateRoots.Orders;
using ECommerce.Domain.AggregateRoots.Orders.ValueObjects;
using ECommerce.Domain.AggregateRoots.Products;
using LanguageExt.Traits;
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
                    .MustSatisfyValidation(Quantity.Validate);
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

        private sealed record DeductionResult(Seq<Inventory> Inventories);

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var customerId = CustomerId.Create(request.CustomerId);
            var shippingAddress = ShippingAddress.Create(request.ShippingAddress).Unwrap();
            var lineRequests = request.OrderLines
                .Select(lineReq => (
                    ProductId: ProductId.Create(lineReq.ProductId),
                    Quantity: Quantity.Create(lineReq.Quantity).Unwrap()))
                .ToList();
            var productIds = lineRequests.Select(l => l.ProductId).Distinct().ToList();

            // 비즈니스 흐름: 가격 조회 → 주문 조립 → 재고 차감 → 신용 검증
            FinT<IO, (Order order, DeductionResult deducted)> validated =
                from prices   in _productCatalog.GetPricesForProducts(productIds)
                from order    in BuildOrder(customerId, lineRequests, prices, shippingAddress)
                from deducted in DeductInventories(lineRequests)
                from customer in _customerRepository.GetById(customerId)
                from _1       in _creditCheckService.ValidateCreditLimit(customer, order.TotalAmount)
                select (order, deducted);

            // 다중 Aggregate 저장 (Order + Inventory)
            FinT<IO, Response> usecase = validated.Bind(ctx =>
                _orderRepository.Create(ctx.order).Bind(saved =>
                _inventoryRepository.UpdateRange(ctx.deducted.Inventories.ToList()).Map(_ =>
                    new Response(
                        saved.Id.ToString(),
                        Seq(saved.OrderLines.Select(l => new OrderLineResponse(
                            l.ProductId.ToString(), l.Quantity, l.UnitPrice, l.LineTotal))),
                        saved.TotalAmount,
                        saved.ShippingAddress,
                        ctx.deducted.Inventories.Select(inv => new DeductedStockInfo(
                            inv.ProductId.ToString(), inv.StockQuantity)),
                        saved.CreatedAt))));

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }

        private static Fin<Order> BuildOrder(
            CustomerId customerId,
            List<(ProductId ProductId, Quantity Quantity)> lineRequests,
            Seq<(ProductId Id, Money Price)> prices,
            ShippingAddress shippingAddress)
        {
            var priceLookup = prices.ToDictionary(p => p.Id, p => p.Price);
            var orderLines = new List<OrderLine>();

            foreach (var (productId, quantity) in lineRequests)
            {
                if (!priceLookup.TryGetValue(productId, out var unitPrice))
                    return Fin.Fail<Order>(ApplicationError.For<PlaceOrderCommand>(
                        new NotFound(),
                        productId.ToString(),
                        $"Product not found: '{productId}'"));

                orderLines.Add(OrderLine.Create(productId, quantity, unitPrice).Unwrap());
            }

            return Order.Create(customerId, orderLines, shippingAddress);
        }

        private FinT<IO, DeductionResult> DeductInventories(
            List<(ProductId ProductId, Quantity Quantity)> lineRequests)
        {
            return toSeq(lineRequests)
                .Traverse(DeductSingleInventory)
                .As()
                .Map(k => new DeductionResult(k.As()));
        }

        private FinT<IO, Inventory> DeductSingleInventory(
            (ProductId ProductId, Quantity Quantity) req)
        {
            return
                from inventory in _inventoryRepository.GetByProductId(req.ProductId)
                from _1 in inventory.DeductStock(req.Quantity)
                select inventory;
        }
    }
}
