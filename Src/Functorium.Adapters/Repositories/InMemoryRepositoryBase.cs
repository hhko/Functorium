using System.Collections.Concurrent;
using Functorium.Adapters.Errors;
using Functorium.Applications.Events;
using Functorium.Domains.Entities;
using Functorium.Domains.Events;
using Functorium.Domains.Observabilities;
using Functorium.Domains.Repositories;
using static Functorium.Adapters.Errors.AdapterErrorType;
using static LanguageExt.Prelude;

namespace Functorium.Adapters.Repositories;

/// <summary>
/// InMemory Repository의 공통 베이스 클래스.
/// ConcurrentDictionary 기반으로 IRepository 전체 CRUD를 기본 구현합니다.
/// </summary>
/// <typeparam name="TAggregate">Aggregate Root 타입</typeparam>
/// <typeparam name="TId">EntityId 구현 타입</typeparam>
public abstract class InMemoryRepositoryBase<TAggregate, TId>
    : IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : struct, IEntityId<TId>
{
    protected InMemoryRepositoryBase(IDomainEventCollector eventCollector)
    {
        EventCollector = eventCollector;
    }

    protected IDomainEventCollector EventCollector { get; }

    public virtual string RequestCategory => "Repository";

    /// <summary>인메모리 저장소. 서브클래스에서 static ConcurrentDictionary를 제공합니다.</summary>
    protected abstract ConcurrentDictionary<TId, TAggregate> Store { get; }

    // ─── IRepository 구현 ─────────────────────────────

    public virtual FinT<IO, TAggregate> Create(TAggregate aggregate)
    {
        return IO.lift(() =>
        {
            Store[aggregate.Id] = aggregate;
            EventCollector.Track(aggregate);
            return Fin.Succ(aggregate);
        });
    }

    public virtual FinT<IO, TAggregate> GetById(TId id)
    {
        return IO.lift(() =>
        {
            if (Store.TryGetValue(id, out TAggregate? aggregate))
            {
                return Fin.Succ(aggregate);
            }

            return NotFoundError(id);
        });
    }

    public virtual FinT<IO, TAggregate> Update(TAggregate aggregate)
    {
        return IO.lift(() =>
        {
            if (!Store.ContainsKey(aggregate.Id))
            {
                return NotFoundError(aggregate.Id);
            }

            Store[aggregate.Id] = aggregate;
            EventCollector.Track(aggregate);
            return Fin.Succ(aggregate);
        });
    }

    public virtual FinT<IO, int> Delete(TId id)
    {
        return IO.lift(() =>
        {
            return Fin.Succ(Store.TryRemove(id, out _) ? 1 : 0);
        });
    }

    public virtual FinT<IO, Seq<TAggregate>> CreateRange(IReadOnlyList<TAggregate> aggregates)
    {
        return IO.lift(() =>
        {
            if (aggregates.Count == 0)
                return Fin.Succ(LanguageExt.Seq<TAggregate>.Empty);

            foreach (var aggregate in aggregates)
                Store[aggregate.Id] = aggregate;
            EventCollector.TrackRange(aggregates);
            return Fin.Succ(toSeq(aggregates));
        });
    }

    public virtual FinT<IO, Seq<TAggregate>> GetByIds(IReadOnlyList<TId> ids)
    {
        return IO.lift(() =>
        {
            if (ids.Count == 0)
                return Fin.Succ(LanguageExt.Seq<TAggregate>.Empty);

            var distinctIds = ids.Distinct().ToList();
            var result = distinctIds
                .Where(id => Store.ContainsKey(id))
                .Select(id => Store[id])
                .ToList();

            if (result.Count != distinctIds.Count)
            {
                return PartialNotFoundError(distinctIds, result);
            }

            return Fin.Succ(toSeq(result));
        });
    }

    public virtual FinT<IO, Seq<TAggregate>> UpdateRange(IReadOnlyList<TAggregate> aggregates)
    {
        return IO.lift(() =>
        {
            if (aggregates.Count == 0)
                return Fin.Succ(LanguageExt.Seq<TAggregate>.Empty);

            foreach (var aggregate in aggregates)
                Store[aggregate.Id] = aggregate;
            EventCollector.TrackRange(aggregates);
            return Fin.Succ(toSeq(aggregates));
        });
    }

    public virtual FinT<IO, int> DeleteRange(IReadOnlyList<TId> ids)
    {
        return IO.lift(() =>
        {
            if (ids.Count == 0)
                return Fin.Succ(0);

            int affected = 0;
            foreach (var id in ids)
            {
                if (Store.TryRemove(id, out _))
                    affected++;
            }

            if (affected > 0)
            {
                EventCollector.TrackEvent(
                    BulkDeletedEvent.From(ids, affected));
            }

            return Fin.Succ(affected);
        });
    }

    // ─── 에러 헬퍼 ───────────────────────────────────

    protected Error NotFoundError(TId id) =>
        AdapterError.For(GetType(),
            new NotFound(), id.ToString()!,
            $"NotFound: No record found for ID '{id}'");

    protected Error PartialNotFoundError(IReadOnlyList<TId> requestedIds, List<TAggregate> foundAggregates)
    {
        var foundIds = foundAggregates.Select(a => a.Id.ToString()).ToHashSet();
        var missingIds = requestedIds.Where(id => !foundIds.Contains(id.ToString()!)).ToList();
        var missingIdsStr = FormatIds(missingIds.Select(id => id.ToString()!));

        return AdapterError.For(GetType(),
            new PartialNotFound(), missingIdsStr,
            $"Requested {requestedIds.Count} but found {foundAggregates.Count}. Missing IDs: {missingIdsStr}");
    }

    private static string FormatIds(IEnumerable<string> ids, int maxDisplay = 3)
    {
        var list = ids.ToList();
        if (list.Count <= maxDisplay)
            return string.Join(", ", list);

        return string.Join(", ", list.Take(maxDisplay)) + $" ... (+{list.Count - maxDisplay} more)";
    }
}
