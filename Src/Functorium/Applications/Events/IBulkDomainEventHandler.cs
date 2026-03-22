using Functorium.Domains.Events;
using Mediator;

namespace Functorium.Applications.Events;

/// <summary>
/// 벌크(Bulk) 도메인 이벤트를 처리하는 핸들러 인터페이스.
/// N개 동일 타입 이벤트를 한 번에 처리합니다.
/// </summary>
/// <remarks>
/// 기본 인터페이스 메서드가 이벤트 타입 필터링을 처리하므로,
/// 구현자는 HandleBulk만 구현하면 됩니다.
/// <code>
/// public class ProductBulkCreatedHandler : IBulkDomainEventHandler&lt;Product.CreatedEvent&gt;
/// {
///     public ValueTask HandleBulk(Seq&lt;Product.CreatedEvent&gt; events, CancellationToken ct)
///     {
///         // 벌크(Bulk) 처리 로직
///     }
/// }
/// </code>
/// </remarks>
public interface IBulkDomainEventHandler<TEvent>
    : IDomainEventHandler<BulkDomainEvent>
    where TEvent : IDomainEvent
{
    /// <summary>
    /// 벌크(Bulk) 이벤트를 타입 안전하게 처리합니다.
    /// </summary>
    ValueTask HandleBulk(Seq<TEvent> events, CancellationToken cancellationToken);

    /// <summary>
    /// 이벤트 타입 필터링 후 HandleBulk를 호출하는 기본 구현.
    /// </summary>
    ValueTask INotificationHandler<BulkDomainEvent>.Handle(
        BulkDomainEvent notification, CancellationToken cancellationToken)
    {
        if (notification.InnerEventType == typeof(TEvent))
            return HandleBulk(notification.GetEvents<TEvent>(), cancellationToken);
        return ValueTask.CompletedTask;
    }
}
