using System.Collections.Concurrent;
using LayeredArch.Domain.AggregateRoots.Tags;
using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Adapters.Errors.AdapterErrorType;
using static LanguageExt.Prelude;

namespace LayeredArch.Adapters.Persistence.Repositories.InMemory;

/// <summary>
/// 메모리 기반 태그 리포지토리 구현
/// </summary>
[GenerateObservablePort]
public class InMemoryTagRepository : ITagRepository
{
    internal static readonly ConcurrentDictionary<TagId, Tag> Tags = new();
    private readonly IDomainEventCollector _eventCollector;

    public string RequestCategory => "Repository";

    public InMemoryTagRepository(IDomainEventCollector eventCollector)
    {
        _eventCollector = eventCollector;
    }

    public virtual FinT<IO, Tag> Create(Tag tag)
    {
        return IO.lift(() =>
        {
            Tags[tag.Id] = tag;
            _eventCollector.Track(tag);
            return Fin.Succ(tag);
        });
    }

    public virtual FinT<IO, Tag> GetById(TagId id)
    {
        return IO.lift(() =>
        {
            if (Tags.TryGetValue(id, out Tag? tag))
            {
                return Fin.Succ(tag);
            }

            return AdapterError.For<InMemoryTagRepository>(
                new NotFound(),
                id.ToString(),
                $"태그 ID '{id}'을(를) 찾을 수 없습니다");
        });
    }

    public virtual FinT<IO, Tag> Update(Tag tag)
    {
        return IO.lift(() =>
        {
            if (!Tags.ContainsKey(tag.Id))
            {
                return AdapterError.For<InMemoryTagRepository>(
                    new NotFound(),
                    tag.Id.ToString(),
                    $"태그 ID '{tag.Id}'을(를) 찾을 수 없습니다");
            }

            Tags[tag.Id] = tag;
            _eventCollector.Track(tag);
            return Fin.Succ(tag);
        });
    }

    public virtual FinT<IO, int> Delete(TagId id)
    {
        return IO.lift(() =>
        {
            return Fin.Succ(Tags.TryRemove(id, out _) ? 1 : 0);
        });
    }

    public virtual FinT<IO, Seq<Tag>> CreateRange(IReadOnlyList<Tag> tags)
    {
        return IO.lift(() =>
        {
            foreach (var tag in tags)
                Tags[tag.Id] = tag;
            _eventCollector.TrackRange(tags);
            return Fin.Succ(toSeq(tags));
        });
    }

    public virtual FinT<IO, Seq<Tag>> GetByIds(IReadOnlyList<TagId> ids)
    {
        return IO.lift(() =>
        {
            var distinctIds = ids.Distinct().ToList();
            var result = distinctIds
                .Where(id => Tags.ContainsKey(id))
                .Select(id => Tags[id])
                .ToList();

            if (result.Count != distinctIds.Count)
            {
                var foundIds = result.Select(t => t.Id.ToString()).ToHashSet();
                var missingIds = distinctIds.Where(id => !foundIds.Contains(id.ToString())).ToList();
                var missingIdsStr = FormatIds(missingIds);
                return AdapterError.For<InMemoryTagRepository>(
                    new PartialNotFound(), missingIdsStr,
                    $"Requested {distinctIds.Count} but found {result.Count}. Missing IDs: {missingIdsStr}");
            }

            return Fin.Succ(toSeq(result));
        });
    }

    public virtual FinT<IO, Seq<Tag>> UpdateRange(IReadOnlyList<Tag> tags)
    {
        return IO.lift(() =>
        {
            foreach (var tag in tags)
                Tags[tag.Id] = tag;
            _eventCollector.TrackRange(tags);
            return Fin.Succ(toSeq(tags));
        });
    }

    private static string FormatIds<T>(IEnumerable<T> ids, int maxDisplay = 3)
    {
        var list = ids.Select(id => id!.ToString()!).ToList();
        if (list.Count <= maxDisplay)
            return string.Join(", ", list);

        return string.Join(", ", list.Take(maxDisplay)) + $" ... (+{list.Count - maxDisplay} more)";
    }

    public virtual FinT<IO, int> DeleteRange(IReadOnlyList<TagId> ids)
    {
        return IO.lift(() =>
        {
            int affected = 0;
            foreach (var id in ids)
            {
                if (Tags.TryRemove(id, out _))
                    affected++;
            }
            return Fin.Succ(affected);
        });
    }
}
