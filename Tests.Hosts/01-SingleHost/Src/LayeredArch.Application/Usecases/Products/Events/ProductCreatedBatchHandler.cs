using Functorium.Applications.Events;
using LayeredArch.Domain.AggregateRoots.Products;
using Microsoft.Extensions.Logging;

namespace LayeredArch.Application.Usecases.Products.Events;

/// <summary>
/// Product.CreatedEvent 배치 핸들러 - 벌크 생성 시 1회 호출.
/// 개별 핸들러(ProductCreatedEvent)와 독립적으로 공존합니다.
/// </summary>
public sealed class ProductCreatedBatchHandler : IDomainEventBatchHandler<Product.CreatedEvent>
{
    private readonly ILogger<ProductCreatedBatchHandler> _logger;

    public ProductCreatedBatchHandler(ILogger<ProductCreatedBatchHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask HandleBatch(Seq<Product.CreatedEvent> events, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DomainEvent:Batch] {Count} products created in bulk: [{ProductIds}]",
            events.Count,
            string.Join(", ", events.Select(e => e.ProductId.ToString())));

        return ValueTask.CompletedTask;
    }
}
