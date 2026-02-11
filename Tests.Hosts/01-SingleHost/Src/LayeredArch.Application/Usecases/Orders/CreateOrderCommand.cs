using LayeredArch.Domain.Ports;
using LayeredArch.Domain.AggregateRoots.Orders;
using LayeredArch.Domain.AggregateRoots.Orders.ValueObjects;
using LayeredArch.Domain.AggregateRoots.Products;
using Functorium.Applications.Errors;
using Functorium.Applications.Events;
using Functorium.Applications.Linq;
using static Functorium.Applications.Errors.ApplicationErrorType;

namespace LayeredArch.Application.Usecases.Orders;

/// <summary>
/// 주문 생성 Command - 공유 Port(IProductCatalog) 사용 데모
/// IProductCatalog로 상품 존재/가격 검증 후 주문 생성
/// </summary>
public sealed class CreateOrderCommand
{
    /// <summary>
    /// Command Request
    /// </summary>
    public sealed record Request(
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
    /// Command Handler - IProductCatalog 공유 Port를 사용하여 교차 Aggregate 검증
    /// </summary>
    public sealed class Usecase(
        IOrderRepository orderRepository,
        IProductCatalog productCatalog,
        IDomainEventPublisher eventPublisher)
        : ICommandUsecase<Request, Response>
    {
        private readonly IOrderRepository _orderRepository = orderRepository;
        private readonly IProductCatalog _productCatalog = productCatalog;
        private readonly IDomainEventPublisher _eventPublisher = eventPublisher;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // 1. Value Object 생성
            var shippingAddressResult = ShippingAddress.Create(request.ShippingAddress);
            var quantityResult = Quantity.Create(request.Quantity);

            if (shippingAddressResult.IsFail)
                return FinResponse.Fail<Response>(shippingAddressResult.Match(Succ: _ => throw new InvalidOperationException(), Fail: e => e));
            if (quantityResult.IsFail)
                return FinResponse.Fail<Response>(quantityResult.Match(Succ: _ => throw new InvalidOperationException(), Fail: e => e));

            var productId = ProductId.Create(request.ProductId);
            var shippingAddress = (ShippingAddress)shippingAddressResult;
            var quantity = (Quantity)quantityResult;

            // 2. IProductCatalog로 상품 검증 및 가격 조회 후 주문 생성
            FinT<IO, Response> usecase =
                from exists in _productCatalog.ExistsById(productId)
                from _ in guard(exists, ApplicationError.For<CreateOrderCommand>(
                    new NotFound(),
                    request.ProductId,
                    $"상품을 찾을 수 없습니다: '{request.ProductId}'"))
                from unitPrice in _productCatalog.GetPrice(productId)
                from order in _orderRepository.Create(
                    Order.Create(productId, quantity, unitPrice, shippingAddress))
                from __ in _eventPublisher.PublishEvents(order, cancellationToken)
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
