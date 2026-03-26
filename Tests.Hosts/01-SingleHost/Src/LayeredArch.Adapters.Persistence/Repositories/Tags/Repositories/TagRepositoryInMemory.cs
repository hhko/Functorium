using System.Collections.Concurrent;
using LayeredArch.Domain.AggregateRoots.Tags;
using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;

namespace LayeredArch.Adapters.Persistence.Repositories.Tags.Repositories;

/// <summary>
/// 메모리 기반 태그 리포지토리 구현
/// </summary>
[GenerateObservablePort]
public class TagRepositoryInMemory
    : InMemoryRepositoryBase<Tag, TagId>, ITagRepository
{
    internal static readonly ConcurrentDictionary<TagId, Tag> Tags = new();
    protected override ConcurrentDictionary<TagId, Tag> Store => Tags;

    public TagRepositoryInMemory(IDomainEventCollector eventCollector)
        : base(eventCollector) { }
}
