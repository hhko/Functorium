using Functorium.Applications.Events;
using MediatorEvents.Demo.Domain;

namespace MediatorEvents.Demo.Events;

/// <summary>
/// OrderCreatedEvent 핸들러 1: 재고 확인 시뮬레이션
/// </summary>
public sealed class OrderCreatedHandler : IDomainEventHandler<OrderCreatedEvent>
{
    public async ValueTask Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"    [OrderCreatedHandler] 시작 - OrderId: {notification.OrderId}");
        Console.WriteLine($"    [OrderCreatedHandler] 재고 확인 중... (2초 소요)");

        await Task.Delay(2000, cancellationToken);

        Console.WriteLine($"    [OrderCreatedHandler] 재고 확인 완료 - 고객: {notification.CustomerName}, 금액: {notification.TotalAmount:C}");
    }
}
