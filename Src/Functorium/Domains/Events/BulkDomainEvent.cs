namespace Functorium.Domains.Events;

/// <summary>
/// 동일 타입 도메인 이벤트의 벌크(Bulk) 래퍼.
/// PublishTrackedEvents에서 이벤트를 타입별로 그룹화하여 1회 발행합니다.
/// </summary>
/// <remarks>
/// 비제네릭 설계: Mediator.SourceGenerator는 오픈 제네릭 INotification 타입을 지원하지 않습니다.
/// 대신 비제네릭 래퍼로 발행하고, IBulkDomainEventHandler&lt;TEvent&gt;의 기본 인터페이스 메서드에서
/// 타입별 필터링 후 타입 안전한 GetEvents&lt;T&gt;()로 핸들러에 전달합니다.
/// </remarks>
public sealed record BulkDomainEvent : DomainEvent, IBulkEventInfo
{
    internal BulkDomainEvent(IReadOnlyList<IDomainEvent> events, Type innerEventType) : base()
    {
        Events = events;
        InnerEventType = innerEventType;
    }

    /// <summary>이벤트 목록 (동일 타입)</summary>
    public IReadOnlyList<IDomainEvent> Events { get; }

    /// <summary>이벤트 수</summary>
    public int Count => Events.Count;

    /// <summary>래핑된 이벤트의 실제 타입</summary>
    internal Type InnerEventType { get; }

    string IBulkEventInfo.InnerEventTypeName => InnerEventType.Name;

    /// <summary>
    /// 타입 안전한 이벤트 접근. Handler에서 사용합니다.
    /// </summary>
    public Seq<TEvent> GetEvents<TEvent>() where TEvent : IDomainEvent
        => toSeq(Events.Cast<TEvent>());
}
