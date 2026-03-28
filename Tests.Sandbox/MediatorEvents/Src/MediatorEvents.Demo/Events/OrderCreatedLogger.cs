using Functorium.Applications.Events;
using MediatorEvents.Demo.Domain;

namespace MediatorEvents.Demo.Events;

/// <summary>
/// OrderCreatedEvent 핸들러 2: 감사 로그 기록
/// 동일한 이벤트에 대한 두 번째 핸들러로, 순차 실행을 확인하기 위해 사용
/// </summary>
public sealed class OrderCreatedLogger : IDomainEventHandler<OrderCreatedEvent>
{
    public async ValueTask Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"    [OrderCreatedLogger] 시작 - EventId: {notification.EventId}");
        Console.WriteLine($"    [OrderCreatedLogger] 감사 로그 기록 중... (1초 소요)");

        await Task.Delay(1000, cancellationToken);

        Console.WriteLine($"    [OrderCreatedLogger] 감사 로그 기록 완료 - OccurredAt: {notification.OccurredAt}");
    }
}
