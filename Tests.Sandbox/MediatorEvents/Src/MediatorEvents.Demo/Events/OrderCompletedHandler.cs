using Functorium.Applications.Events;
using MediatorEvents.Demo.Domain;

namespace MediatorEvents.Demo.Events;

/// <summary>
/// OrderCompletedEvent 핸들러: 주문 완료 처리
/// </summary>
public sealed class OrderCompletedHandler : IDomainEventHandler<OrderCompletedEvent>
{
    public async ValueTask Handle(OrderCompletedEvent notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"    [OrderCompletedHandler] 시작 - OrderId: {notification.OrderId}");
        Console.WriteLine($"    [OrderCompletedHandler] 주문 완료 처리 중... (1초 소요)");

        await Task.Delay(1000, cancellationToken);

        Console.WriteLine($"    [OrderCompletedHandler] 주문 완료 처리 완료 - OrderId: {notification.OrderId}");
    }
}
