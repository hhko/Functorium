using Functorium.Applications.Events;
using Functorium.Domains.Events;

namespace Functorium.Adapters.Events;

/// <summary>
/// Scoped 범위에서 Aggregate를 추적하는 IDomainEventCollector 구현체.
/// </summary>
/// <remarks>
/// 동일 Aggregate 인스턴스의 중복 추적을 방지하기 위해 참조 동등성(ReferenceEquals)을 사용합니다.
/// Value Object와 달리 Aggregate는 동일 참조(same instance)만 동일한 추적 대상으로 간주합니다.
/// </remarks>
internal sealed class DomainEventCollector : IDomainEventCollector
{
    private readonly System.Collections.Generic.HashSet<IHasDomainEvents> _tracked = new(ReferenceEqualityComparer.Instance);

    public void Track(IHasDomainEvents aggregate) => _tracked.Add(aggregate);

    public void TrackRange(IEnumerable<IHasDomainEvents> aggregates)
    {
        foreach (var aggregate in aggregates)
            _tracked.Add(aggregate);
    }

    public IReadOnlyList<IHasDomainEvents> GetTrackedAggregates()
        => _tracked.Where(a => a.DomainEvents.Count > 0).ToList();
}
