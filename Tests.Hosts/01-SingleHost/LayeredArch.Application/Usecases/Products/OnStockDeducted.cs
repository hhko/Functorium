using Functorium.Applications.Events;
using LayeredArch.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace LayeredArch.Application.Usecases.Products;

/// <summary>
/// Product.StockDeductedEvent 핸들러 - 재고 차감 로깅.
/// </summary>
public sealed class OnStockDeducted : IDomainEventHandler<Product.StockDeductedEvent>
{
    private readonly ILogger<OnStockDeducted> _logger;

    public OnStockDeducted(ILogger<OnStockDeducted> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(Product.StockDeductedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DomainEvent] Stock deducted: ProductId={ProductId}, Quantity={Quantity}",
            notification.ProductId,
            notification.Quantity);

        return ValueTask.CompletedTask;
    }
}
