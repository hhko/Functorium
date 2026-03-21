using Functorium.Applications.Events;
using Microsoft.Extensions.Logging;

namespace ObservabilityHost.DomainEvents;

public sealed class OrderPlacedEventHandler : IDomainEventHandler<OrderPlacedEvent>
{
    private readonly ILogger<OrderPlacedEventHandler> _logger;

    public OrderPlacedEventHandler(ILogger<OrderPlacedEventHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(OrderPlacedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DomainEvent] Order placed: {OrderId}, Customer: {CustomerId}, Lines: {LineCount}, Total: {TotalAmount}",
            notification.OrderId,
            notification.CustomerId,
            notification.LineCount,
            notification.TotalAmount);

        return ValueTask.CompletedTask;
    }
}
