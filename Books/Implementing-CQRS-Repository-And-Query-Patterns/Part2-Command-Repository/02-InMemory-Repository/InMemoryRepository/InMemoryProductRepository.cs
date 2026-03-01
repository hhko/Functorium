using System.Collections.Concurrent;
using Functorium.Adapters.Repositories;
using Functorium.Applications.Events;

namespace InMemoryRepository;

/// <summary>
/// Product의 InMemory Repository 구현체.
/// InMemoryRepositoryBase를 상속하여 ConcurrentDictionary 기반 CRUD를 제공합니다.
/// </summary>
public sealed class InMemoryProductRepository
    : InMemoryRepositoryBase<Product, ProductId>
{
    private static readonly ConcurrentDictionary<ProductId, Product> _store = new();

    public InMemoryProductRepository(IDomainEventCollector eventCollector)
        : base(eventCollector)
    {
    }

    protected override ConcurrentDictionary<ProductId, Product> Store => _store;

    /// <summary>
    /// 테스트용: 저장소를 초기화합니다.
    /// </summary>
    public void Clear() => _store.Clear();
}
