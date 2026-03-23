using Functorium.Applications.Events;
using LayeredArch.Domain.AggregateRoots.Products;
using Microsoft.Extensions.Logging;

namespace LayeredArch.Application.Usecases.Products.Events;

/// <summary>
/// ProductBulkOperations.BulkCreatedEvent 핸들러 - 벌크 생성 시 1회 호출.
/// </summary>
public sealed class OnProductBulkCreated : IDomainEventHandler<ProductBulkOperations.BulkCreatedEvent>
{
    private readonly ILogger<OnProductBulkCreated> _logger;

    public OnProductBulkCreated(ILogger<OnProductBulkCreated> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(ProductBulkOperations.BulkCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DomainEvent] {Count} products created in bulk: [{ProductIds}]",
            notification.CreatedIds.Count,
            string.Join(", ", notification.CreatedIds.Select(id => id.ToString())));

        return ValueTask.CompletedTask;
    }
}
