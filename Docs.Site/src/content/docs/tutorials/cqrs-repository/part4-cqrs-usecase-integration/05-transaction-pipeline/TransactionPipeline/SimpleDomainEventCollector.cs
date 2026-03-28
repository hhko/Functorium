using Functorium.Applications.Events;
using Functorium.Domains.Events;

namespace TransactionPipeline;

public sealed class SimpleDomainEventCollector : IDomainEventCollector
{
    private readonly HashSet<IHasDomainEvents> _tracked = new(ReferenceEqualityComparer.Instance);

    public void Track(IHasDomainEvents aggregate) => _tracked.Add(aggregate);

    public void TrackRange(IEnumerable<IHasDomainEvents> aggregates)
    {
        foreach (var aggregate in aggregates)
            _tracked.Add(aggregate);
    }

    public IReadOnlyList<IHasDomainEvents> GetTrackedAggregates()
        => _tracked.Where(a => a.DomainEvents.Count > 0).ToList();

    private readonly List<IDomainEvent> _directEvents = [];
    public void TrackEvent(IDomainEvent domainEvent) => _directEvents.Add(domainEvent);
    public IReadOnlyList<IDomainEvent> GetDirectlyTrackedEvents() => _directEvents;
}
