using Functorium.Applications.Events;
using LayeredArch.Domain.AggregateRoots.Products;
using Microsoft.Extensions.Logging;

namespace LayeredArch.Application.Usecases.Tags.Events;

/// <summary>
/// Product.TagAssignedEvent 핸들러 - 태그 할당 로깅.
/// </summary>
public sealed class TagAssignedEvent : IDomainEventHandler<Product.TagAssignedEvent>
{
    private readonly ILogger<TagAssignedEvent> _logger;

    public TagAssignedEvent(ILogger<TagAssignedEvent> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(Product.TagAssignedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DomainEvent] Tag assigned to product: {ProductId}, TagId: {TagId}",
            notification.ProductId,
            notification.TagId);

        return ValueTask.CompletedTask;
    }
}
