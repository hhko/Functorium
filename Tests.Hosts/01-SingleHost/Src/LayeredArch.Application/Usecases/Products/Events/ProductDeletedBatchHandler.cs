using Functorium.Applications.Events;
using LayeredArch.Domain.AggregateRoots.Products;
using Microsoft.Extensions.Logging;

namespace LayeredArch.Application.Usecases.Products.Events;

/// <summary>
/// Product.DeletedEvent 배치 핸들러 - 벌크 삭제 시 1회 호출.
/// 개별 핸들러와 독립적으로 공존합니다.
/// </summary>
public sealed class ProductDeletedBatchHandler : IDomainEventBatchHandler<Product.DeletedEvent>
{
    private readonly ILogger<ProductDeletedBatchHandler> _logger;

    public ProductDeletedBatchHandler(ILogger<ProductDeletedBatchHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask HandleBatch(Seq<Product.DeletedEvent> events, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DomainEvent:Batch] {Count} products deleted in bulk: [{ProductIds}]",
            events.Count,
            string.Join(", ", events.Select(e => e.ProductId.ToString())));

        return ValueTask.CompletedTask;
    }
}
