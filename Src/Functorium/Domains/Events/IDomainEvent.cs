namespace Functorium.Domains.Events;

/// <summary>
/// 도메인 이벤트의 기본 인터페이스.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// 이벤트 발생 시각.
    /// </summary>
    DateTimeOffset OccurredAt { get; }
}
