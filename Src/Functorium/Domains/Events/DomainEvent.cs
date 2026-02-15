namespace Functorium.Domains.Events;

/// <summary>
/// 도메인 이벤트의 기본 record.
/// 불변성과 값 기반 동등성을 제공합니다.
/// </summary>
/// <param name="OccurredAt">이벤트 발생 시각</param>
/// <param name="EventId">이벤트 고유 식별자</param>
/// <param name="CorrelationId">요청 추적 ID</param>
/// <param name="CausationId">원인 이벤트 ID</param>
public abstract record DomainEvent(
    DateTimeOffset OccurredAt,
    Ulid EventId,
    string? CorrelationId,
    string? CausationId) : IDomainEvent
{
    /// <summary>
    /// 현재 시각과 새 EventId로 이벤트를 생성합니다.
    /// </summary>
    protected DomainEvent() : this(DateTimeOffset.UtcNow, Ulid.NewUlid(), null, null)
    {
    }

    /// <summary>
    /// 현재 시각과 새 EventId, 지정된 CorrelationId로 이벤트를 생성합니다.
    /// </summary>
    /// <param name="correlationId">요청 추적 ID</param>
    protected DomainEvent(string? correlationId) : this(DateTimeOffset.UtcNow, Ulid.NewUlid(), correlationId, null)
    {
    }

    /// <summary>
    /// 현재 시각과 새 EventId, 지정된 CorrelationId와 CausationId로 이벤트를 생성합니다.
    /// </summary>
    /// <param name="correlationId">요청 추적 ID</param>
    /// <param name="causationId">원인 이벤트 ID</param>
    protected DomainEvent(string? correlationId, string? causationId)
        : this(DateTimeOffset.UtcNow, Ulid.NewUlid(), correlationId, causationId)
    {
    }
}
