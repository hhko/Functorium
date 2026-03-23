using Functorium.Applications.Events;
using LayeredArch.Domain.AggregateRoots.Products;
using Microsoft.Extensions.Logging;

namespace LayeredArch.Application.Usecases.Products.Events;

/// <summary>
/// ProductBulkOperations.BulkDeletedEvent 핸들러 - 벌크 삭제 시 1회 호출.
/// </summary>
public sealed class OnProductBulkDeleted : IDomainEventHandler<ProductBulkOperations.BulkDeletedEvent>
{
    private readonly ILogger<OnProductBulkDeleted> _logger;

    public OnProductBulkDeleted(ILogger<OnProductBulkDeleted> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(ProductBulkOperations.BulkDeletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DomainEvent] {Count} products deleted in bulk: [{ProductIds}]",
            notification.DeletedIds.Count,
            string.Join(", ", notification.DeletedIds.Select(id => id.ToString())));

        return ValueTask.CompletedTask;
    }
}
