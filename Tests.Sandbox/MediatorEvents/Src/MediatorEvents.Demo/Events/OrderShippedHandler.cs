using Functorium.Applications.Events;
using MediatorEvents.Demo.Domain;

namespace MediatorEvents.Demo.Events;

/// <summary>
/// OrderShippedEvent 핸들러: 배송 알림 전송
/// </summary>
public sealed class OrderShippedHandler : IDomainEventHandler<OrderShippedEvent>
{
    public async ValueTask Handle(OrderShippedEvent notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"    [OrderShippedHandler] 시작 - OrderId: {notification.OrderId}");
        Console.WriteLine($"    [OrderShippedHandler] 배송 알림 전송 중... (1.5초 소요)");

        await Task.Delay(1500, cancellationToken);

        Console.WriteLine($"    [OrderShippedHandler] 배송 알림 전송 완료 - ShippedAt: {notification.ShippedAt}");
    }
}
