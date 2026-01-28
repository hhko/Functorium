namespace Functorium.Domains.Events;

/// <summary>
/// 도메인 이벤트의 기본 record.
/// 불변성과 값 기반 동등성을 제공합니다.
/// </summary>
/// <param name="OccurredAt">이벤트 발생 시각</param>
public abstract record DomainEvent(DateTimeOffset OccurredAt) : IDomainEvent
{
    /// <summary>
    /// 현재 시각으로 이벤트를 생성합니다.
    /// </summary>
    protected DomainEvent() : this(DateTimeOffset.UtcNow)
    {
    }
}
