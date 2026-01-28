using Functorium.Domains.Events;

namespace Functorium.Domains.Entities;

/// <summary>
/// Aggregate Root의 기본 추상 클래스.
/// 도메인 이벤트 관리 기능을 제공합니다.
/// </summary>
/// <typeparam name="TId">EntityId 구현 타입</typeparam>
[Serializable]
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : struct, IEntityId<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// 기본 생성자 (ORM/직렬화용).
    /// </summary>
    protected AggregateRoot()
    {
    }

    /// <summary>
    /// ID를 지정하여 Aggregate Root를 생성합니다.
    /// </summary>
    /// <param name="id">Entity 식별자</param>
    protected AggregateRoot(TId id) : base(id)
    {
    }

    /// <summary>
    /// 도메인 이벤트 목록 (읽기 전용).
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// 도메인 이벤트를 추가합니다.
    /// </summary>
    /// <param name="domainEvent">추가할 도메인 이벤트</param>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// 도메인 이벤트를 제거합니다.
    /// </summary>
    /// <param name="domainEvent">제거할 도메인 이벤트</param>
    protected void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    /// <summary>
    /// 모든 도메인 이벤트를 제거합니다.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
