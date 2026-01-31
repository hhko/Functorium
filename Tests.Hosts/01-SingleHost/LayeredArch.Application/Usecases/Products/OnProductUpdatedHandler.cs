using Functorium.Applications.Events;
using LayeredArch.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace LayeredArch.Application.Usecases.Products;

/// <summary>
/// Product.UpdatedEvent 핸들러 - 상품 업데이트 로깅.
/// </summary>
public sealed class OnProductUpdatedHandler : IDomainEventHandler<Product.UpdatedEvent>
{
    private readonly ILogger<OnProductUpdatedHandler> _logger;

    public OnProductUpdatedHandler(ILogger<OnProductUpdatedHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(Product.UpdatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DomainEvent] Product updated: {ProductId}, Name: {Name}, OldPrice: {OldPrice}, NewPrice: {NewPrice}",
            notification.ProductId,
            notification.Name,
            notification.OldPrice,
            notification.NewPrice);

        return ValueTask.CompletedTask;
    }
}
