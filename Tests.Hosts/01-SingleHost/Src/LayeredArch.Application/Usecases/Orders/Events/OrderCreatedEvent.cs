using Functorium.Applications.Events;
using LayeredArch.Domain.AggregateRoots.Orders;
using Microsoft.Extensions.Logging;

namespace LayeredArch.Application.Usecases.Orders.Events;

/// <summary>
/// Order.CreatedEvent 핸들러 - 주문 생성 로깅
/// </summary>
public sealed class OrderCreatedEvent : IDomainEventHandler<Order.CreatedEvent>
{
    private readonly ILogger<OrderCreatedEvent> _logger;

    public OrderCreatedEvent(ILogger<OrderCreatedEvent> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(Order.CreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DomainEvent] Order created: {OrderId}, OrderLines: {OrderLineCount}, TotalAmount: {TotalAmount}",
            notification.OrderId,
            notification.OrderLines.Count,
            notification.TotalAmount);

        return ValueTask.CompletedTask;
    }
}
