using Functorium.Domains.Events;
using Mediator;

namespace Functorium.Applications.Events;

/// <summary>
/// 도메인 이벤트 핸들러 인터페이스.
/// Mediator.INotificationHandler를 확장하여 소스 생성기 호환.
/// </summary>
/// <typeparam name="TEvent">처리할 도메인 이벤트 타입</typeparam>
public interface IDomainEventHandler<in TEvent> : INotificationHandler<TEvent>
    where TEvent : IDomainEvent
{
    // ValueTask Handle(TEvent notification, CancellationToken cancellationToken) 상속
}
