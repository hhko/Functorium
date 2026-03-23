using Functorium.Domains.Events;

namespace Functorium.Applications.Events;

/// <summary>
/// Scoped 범위에서 Aggregate를 추적하여 도메인 이벤트를 수집하는 인터페이스.
/// Repository의 Create/Update에서 Track()을 호출하고,
/// UsecaseTransactionPipeline에서 GetTrackedAggregates()로 이벤트를 수집합니다.
/// </summary>
public interface IDomainEventCollector
{
    /// <summary>
    /// Aggregate를 추적 대상으로 등록합니다.
    /// 동일한 Aggregate가 이미 등록되어 있으면 무시합니다.
    /// </summary>
    void Track(IHasDomainEvents aggregate);

    /// <summary>
    /// 여러 Aggregate를 추적 대상으로 일괄 등록합니다.
    /// </summary>
    void TrackRange(IEnumerable<IHasDomainEvents> aggregates);

    /// <summary>
    /// 추적 중인 Aggregate 중 도메인 이벤트가 있는 것들을 반환합니다.
    /// </summary>
    IReadOnlyList<IHasDomainEvents> GetTrackedAggregates();
}
