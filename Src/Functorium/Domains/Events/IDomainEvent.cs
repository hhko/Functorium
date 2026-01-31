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

    /// <summary>
    /// 이벤트 고유 식별자.
    /// 이벤트 중복 처리 방지 및 추적에 사용됩니다.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// 요청 추적 ID.
    /// 동일한 요청에서 발생한 이벤트를 추적하는 데 사용됩니다.
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// 원인 이벤트 ID.
    /// 이 이벤트를 발생시킨 이전 이벤트의 ID입니다.
    /// </summary>
    string? CausationId { get; }
}
