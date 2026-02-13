using Functorium.Applications.Events;
using LayeredArch.Domain.SharedKernel.Events;
using Microsoft.Extensions.Logging;

namespace LayeredArch.Application.Usecases.Tags;

/// <summary>
/// TagRemovedEvent 핸들러 - 태그 제거 로깅.
/// </summary>
public sealed class OnTagRemoved : IDomainEventHandler<TagRemovedEvent>
{
    private readonly ILogger<OnTagRemoved> _logger;

    public OnTagRemoved(ILogger<OnTagRemoved> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(TagRemovedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DomainEvent] Tag removed: {TagId}",
            notification.TagId);

        return ValueTask.CompletedTask;
    }
}
