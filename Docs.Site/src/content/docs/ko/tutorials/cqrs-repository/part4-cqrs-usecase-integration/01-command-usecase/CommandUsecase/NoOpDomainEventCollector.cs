using Functorium.Applications.Events;
using Functorium.Domains.Events;

namespace CommandUsecase;

internal sealed class NoOpDomainEventCollector : IDomainEventCollector
{
    public void Track(IHasDomainEvents aggregate) { }
    public void TrackRange(IEnumerable<IHasDomainEvents> aggregates) { }
    public IReadOnlyList<IHasDomainEvents> GetTrackedAggregates() => [];
    public void TrackEvent(IDomainEvent domainEvent) { }
    public IReadOnlyList<IDomainEvent> GetDirectlyTrackedEvents() => [];
}
