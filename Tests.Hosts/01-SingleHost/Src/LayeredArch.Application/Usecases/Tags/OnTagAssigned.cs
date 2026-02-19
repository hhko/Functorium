using Functorium.Applications.Events;
using LayeredArch.Domain.SharedModels.Entities;
using Microsoft.Extensions.Logging;

namespace LayeredArch.Application.Usecases.Tags;

/// <summary>
/// Tag.AssignedEvent 핸들러 - 태그 할당 로깅.
/// </summary>
public sealed class OnTagAssigned : IDomainEventHandler<Tag.AssignedEvent>
{
    private readonly ILogger<OnTagAssigned> _logger;

    public OnTagAssigned(ILogger<OnTagAssigned> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(Tag.AssignedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DomainEvent] Tag assigned: {TagId}, Name: {TagName}",
            notification.TagId,
            notification.TagName);

        return ValueTask.CompletedTask;
    }
}
