using System.Collections.Concurrent;
using AiGovernance.Domain.AggregateRoots.Deployments;
using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using Functorium.Domains.Specifications;

namespace AiGovernance.Adapters.Persistence.Deployments.Repositories;

/// <summary>
/// 메모리 기반 배포 리포지토리 구현
/// </summary>
[GenerateObservablePort]
public class DeploymentRepositoryInMemory
    : InMemoryRepositoryBase<ModelDeployment, ModelDeploymentId>, IDeploymentRepository
{
    internal static readonly ConcurrentDictionary<ModelDeploymentId, ModelDeployment> Deployments = new();
    protected override ConcurrentDictionary<ModelDeploymentId, ModelDeployment> Store => Deployments;

    public DeploymentRepositoryInMemory(IDomainEventCollector eventCollector)
        : base(eventCollector) { }

    // ─── Deployment 고유 메서드 ──────────────────────

    public virtual FinT<IO, bool> Exists(Specification<ModelDeployment> spec)
    {
        return IO.lift(() =>
        {
            bool exists = Deployments.Values.Any(d => spec.IsSatisfiedBy(d));
            return Fin.Succ(exists);
        });
    }

    public virtual FinT<IO, Seq<ModelDeployment>> Find(Specification<ModelDeployment> spec)
    {
        return IO.lift(() =>
        {
            var result = Deployments.Values
                .Where(d => spec.IsSatisfiedBy(d))
                .ToList();
            return Fin.Succ(toSeq(result));
        });
    }
}
