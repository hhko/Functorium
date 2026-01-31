using Mediator;

namespace Functorium.Domains.Events;

/// <summary>
/// 도메인 이벤트의 기본 인터페이스.
/// Mediator.INotification을 확장하여 Pub/Sub 통합.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// 이벤트 발생 시각.
    /// </summary>
    DateTimeOffset OccurredAt { get; }
}
