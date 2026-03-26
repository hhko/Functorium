using System.Collections.Concurrent;
using AiGovernance.Domain.AggregateRoots.Models;
using Functorium.Adapters.Errors;
using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using Functorium.Domains.Specifications;
using static Functorium.Adapters.Errors.AdapterErrorType;

namespace AiGovernance.Adapters.Persistence.Models.Repositories;

/// <summary>
/// 메모리 기반 AI 모델 리포지토리 구현
/// </summary>
[GenerateObservablePort]
public class AIModelRepositoryInMemory
    : InMemoryRepositoryBase<AIModel, AIModelId>, IAIModelRepository
{
    internal static readonly ConcurrentDictionary<AIModelId, AIModel> AIModels = new();
    protected override ConcurrentDictionary<AIModelId, AIModel> Store => AIModels;

    public AIModelRepositoryInMemory(IDomainEventCollector eventCollector)
        : base(eventCollector) { }

    // ─── Soft Delete 오버라이드 ──────────────────────

    public override FinT<IO, AIModel> GetById(AIModelId id)
    {
        return IO.lift(() =>
        {
            if (AIModels.TryGetValue(id, out AIModel? model) && model.DeletedAt.IsNone)
            {
                return Fin.Succ(model);
            }

            return NotFoundError(id);
        });
    }

    public override FinT<IO, int> Delete(AIModelId id)
    {
        return IO.lift(() =>
        {
            if (!AIModels.TryGetValue(id, out var model))
            {
                return Fin.Succ(0);
            }

            model.Archive("system");
            EventCollector.Track(model);
            return Fin.Succ(1);
        });
    }

    // ─── AIModel 고유 메서드 ─────────────────────────

    public virtual FinT<IO, bool> Exists(Specification<AIModel> spec)
    {
        return IO.lift(() =>
        {
            bool exists = AIModels.Values
                .Where(m => m.DeletedAt.IsNone)
                .Any(m => spec.IsSatisfiedBy(m));
            return Fin.Succ(exists);
        });
    }

    public virtual FinT<IO, AIModel> GetByIdIncludingDeleted(AIModelId id)
    {
        return IO.lift(() =>
        {
            if (AIModels.TryGetValue(id, out AIModel? model))
            {
                return Fin.Succ(model);
            }

            return NotFoundError(id);
        });
    }
}
