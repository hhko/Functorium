using System.Collections.Concurrent;
using AiGovernance.Domain.AggregateRoots.Incidents;
using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using Functorium.Domains.Specifications;

namespace AiGovernance.Adapters.Persistence.Incidents.Repositories;

/// <summary>
/// 메모리 기반 인시던트 리포지토리 구현
/// </summary>
[GenerateObservablePort]
public class IncidentRepositoryInMemory
    : InMemoryRepositoryBase<ModelIncident, ModelIncidentId>, IIncidentRepository
{
    internal static readonly ConcurrentDictionary<ModelIncidentId, ModelIncident> Incidents = new();
    protected override ConcurrentDictionary<ModelIncidentId, ModelIncident> Store => Incidents;

    public IncidentRepositoryInMemory(IDomainEventCollector eventCollector)
        : base(eventCollector) { }

    // ─── Incident 고유 메서드 ────────────────────────

    public virtual FinT<IO, bool> Exists(Specification<ModelIncident> spec)
    {
        return IO.lift(() =>
        {
            bool exists = Incidents.Values.Any(i => spec.IsSatisfiedBy(i));
            return Fin.Succ(exists);
        });
    }

    public virtual FinT<IO, Seq<ModelIncident>> Find(Specification<ModelIncident> spec)
    {
        return IO.lift(() =>
        {
            var result = Incidents.Values
                .Where(i => spec.IsSatisfiedBy(i))
                .ToList();
            return Fin.Succ(toSeq(result));
        });
    }
}
