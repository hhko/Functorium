using Functorium.Applications.Events;
using ECommerce.Domain.AggregateRoots.Inventories;
using ECommerce.Domain.AggregateRoots.Orders;

namespace ECommerce.Application.Usecases.Orders.EventHandlers;

/// <summary>
/// 주문 취소 시 재고 자동 복원 핸들러.
/// 개별 주문 라인 단위로 독립 처리 — 하나의 재고 복원 실패가 나머지를 차단하지 않는다.
/// Order는 Inventory를 모른다 — 이벤트가 두 Aggregate를 느슨하게 연결한다.
/// </summary>
public sealed class RestoreInventoryOnOrderCancelledHandler(
    IInventoryRepository inventoryRepository)
    : IDomainEventHandler<Order.CancelledEvent>
{
    private readonly IInventoryRepository _inventoryRepository = inventoryRepository;

    public async ValueTask Handle(Order.CancelledEvent notification, CancellationToken cancellationToken)
    {
        foreach (var line in notification.OrderLines)
        {
            var result = await _inventoryRepository.GetByProductId(line.ProductId)
                .Map(inventory => inventory.AddStock(line.Quantity))
                .Bind(inventory => _inventoryRepository.Update(inventory))
                .Run().RunAsync();
            // 실패 시 관측성 레이어(ObservableDomainEventNotificationPublisher)가 로깅
        }
    }
}
