using Functorium.Applications.Events;
using LayeredArch.Domain.AggregateRoots.Products;
using Microsoft.Extensions.Logging;

namespace LayeredArch.Application.Usecases.Products.Events;

/// <summary>
/// Product.UpdatedEvent 핸들러 - 상품 업데이트 로깅.
/// </summary>
public sealed class ProductUpdatedEvent : IDomainEventHandler<Product.UpdatedEvent>
{
    private readonly ILogger<ProductUpdatedEvent> _logger;

    public ProductUpdatedEvent(ILogger<ProductUpdatedEvent> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(Product.UpdatedEvent notification, CancellationToken cancellationToken)
    {
        string name = notification.Name;
        if (name.Contains("[handler-error]", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"[{nameof(ProductUpdatedEvent)}] 시뮬레이션된 핸들러 예외: 이벤트 핸들러 예외 처리 데모");
        }

        _logger.LogInformation(
            "[DomainEvent] Product updated: {ProductId}, Name: {Name}, OldPrice: {OldPrice}, NewPrice: {NewPrice}",
            notification.ProductId,
            notification.Name,
            notification.OldPrice,
            notification.NewPrice);

        return ValueTask.CompletedTask;
    }
}
