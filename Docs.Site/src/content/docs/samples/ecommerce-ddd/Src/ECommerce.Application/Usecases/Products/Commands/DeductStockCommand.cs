using ECommerce.Domain.AggregateRoots.Inventories;
using ECommerce.Domain.AggregateRoots.Products;
using Functorium.Applications.Linq;

namespace ECommerce.Application.Usecases.Products.Commands;

/// <summary>
/// 재고 차감 Command - 트랜잭션 후 이벤트 발행 패턴 예제
/// Inventory Aggregate를 통해 재고를 차감합니다.
/// </summary>
public sealed class DeductStockCommand
{
    /// <summary>
    /// Command Request - 재고 차감에 필요한 데이터
    /// </summary>
    public sealed record Request(
        string ProductId,
        int Quantity) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response - 차감 후 재고 정보
    /// </summary>
    public sealed record Response(
        string ProductId,
        int RemainingStock);

    /// <summary>
    /// Request Validator - FluentValidation 검증 규칙
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.ProductId).MustBeEntityId<Request, ProductId>();

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Deduction quantity must be greater than 0");
        }
    }

    /// <summary>
    /// Command Handler - Inventory Aggregate를 통한 재고 차감
    /// </summary>
    public sealed class Usecase(
        IInventoryRepository inventoryRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly IInventoryRepository _inventoryRepository = inventoryRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var productId = ProductId.Create(request.ProductId);
            var quantity = Quantity.Create(request.Quantity).ThrowIfFail();

            FinT<IO, Response> usecase =
                from inventory in _inventoryRepository.GetByProductId(productId)
                from _1 in inventory.DeductStock(quantity)
                from updated in _inventoryRepository.Update(inventory)
                select new Response(
                    updated.ProductId.ToString(),
                    updated.StockQuantity);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
