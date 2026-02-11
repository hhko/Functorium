using Functorium.Applications.Events;
using LayeredArch.Domain.AggregateRoots.Orders;
using Microsoft.Extensions.Logging;

namespace LayeredArch.Application.Usecases.Orders;

/// <summary>
/// Order.CreatedEvent 핸들러 - 주문 생성 로깅
/// </summary>
public sealed class OnOrderCreated : IDomainEventHandler<Order.CreatedEvent>
{
    private readonly ILogger<OnOrderCreated> _logger;

    public OnOrderCreated(ILogger<OnOrderCreated> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(Order.CreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DomainEvent] Order created: {OrderId}, ProductId: {ProductId}, Quantity: {Quantity}, TotalAmount: {TotalAmount}",
            notification.OrderId,
            notification.ProductId,
            notification.Quantity,
            notification.TotalAmount);

        return ValueTask.CompletedTask;
    }
}
