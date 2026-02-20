using Functorium.Applications.Events;
using LayeredArch.Domain.AggregateRoots.Products;
using Microsoft.Extensions.Logging;

namespace LayeredArch.Application.Usecases.Tags.Events;

/// <summary>
/// Product.TagUnassignedEvent 핸들러 - 태그 해제 로깅.
/// </summary>
public sealed class TagRemovedEvent : IDomainEventHandler<Product.TagUnassignedEvent>
{
    private readonly ILogger<TagRemovedEvent> _logger;

    public TagRemovedEvent(ILogger<TagRemovedEvent> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(Product.TagUnassignedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DomainEvent] Tag unassigned from product: {ProductId}, TagId: {TagId}",
            notification.ProductId,
            notification.TagId);

        return ValueTask.CompletedTask;
    }
}
