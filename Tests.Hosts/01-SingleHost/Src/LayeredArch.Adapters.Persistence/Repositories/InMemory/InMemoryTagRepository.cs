using System.Collections.Concurrent;
using LayeredArch.Domain.AggregateRoots.Tags;
using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Adapters.Errors.AdapterErrorType;

namespace LayeredArch.Adapters.Persistence.Repositories.InMemory;

/// <summary>
/// 메모리 기반 태그 리포지토리 구현
/// </summary>
[GeneratePortObservable]
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

    public virtual FinT<IO, Unit> Delete(TagId id)
    {
        return IO.lift(() =>
        {
            if (!Tags.TryRemove(id, out _))
            {
                return AdapterError.For<InMemoryTagRepository>(
                    new NotFound(),
                    id.ToString(),
                    $"태그 ID '{id}'을(를) 찾을 수 없습니다");
            }

            return Fin.Succ(unit);
        });
    }
}
