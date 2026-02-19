using Functorium.Applications.Events;
using LayeredArch.Domain.AggregateRoots.Inventories;
using Microsoft.Extensions.Logging;

namespace LayeredArch.Application.Usecases.Products.Events;

/// <summary>
/// Inventory.StockDeductedEvent 핸들러 - 재고 차감 로깅.
/// </summary>
public sealed class OnStockDeducted : IDomainEventHandler<Inventory.StockDeductedEvent>
{
    private readonly ILogger<OnStockDeducted> _logger;

    public OnStockDeducted(ILogger<OnStockDeducted> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(Inventory.StockDeductedEvent notification, CancellationToken cancellationToken)
    {
        int quantity = notification.Quantity;
        if (quantity == 999)
        {
            throw new InvalidOperationException(
                $"[{nameof(OnStockDeducted)}] 시뮬레이션된 핸들러 예외: 이벤트 핸들러 예외 처리 데모");
        }

        _logger.LogInformation(
            "[DomainEvent] Stock deducted: ProductId={ProductId}, Quantity={Quantity}",
            notification.ProductId,
            notification.Quantity);

        return ValueTask.CompletedTask;
    }
}
