using Functorium.Applications.Events;
using ECommerce.Domain.AggregateRoots.Inventories;

namespace ECommerce.Application.Usecases.Inventories.EventHandlers;

/// <summary>
/// 재고 차감 시 저재고 감지 핸들러.
/// Inventory.CheckLowStock()에 위임하여 임계값 비교 후 LowStockDetectedEvent 발생.
/// 추후 외부 알림(이메일, Slack) 연동의 확장 포인트 역할.
/// </summary>
public sealed class DetectLowStockOnStockDeductedHandler(
    IInventoryRepository inventoryRepository)
    : IDomainEventHandler<Inventory.StockDeductedEvent>
{
    private static readonly Quantity DefaultThreshold = Quantity.Create(10).ThrowIfFail();
    private readonly IInventoryRepository _inventoryRepository = inventoryRepository;

    public async ValueTask Handle(Inventory.StockDeductedEvent notification, CancellationToken cancellationToken)
    {
        var result = await _inventoryRepository.GetById(notification.InventoryId).Run().RunAsync();
        result.IfSucc(inventory => inventory.CheckLowStock(DefaultThreshold));
    }
}
