using System.Collections.Concurrent;
using Functorium.Adapters.Repositories;
using Functorium.Applications.Events;

namespace InventoryManagement;

/// <summary>
/// 상품 InMemory Repository 구현.
/// </summary>
public sealed class InMemoryProductRepository : InMemoryRepositoryBase<Product, ProductId>, IProductRepository
{
    private readonly ConcurrentDictionary<ProductId, Product> _store = new();
    protected override ConcurrentDictionary<ProductId, Product> Store => _store;

    public InMemoryProductRepository(IDomainEventCollector eventCollector) : base(eventCollector) { }

    /// <summary>
    /// 내부 Store에 대한 읽기 전용 접근.
    /// InMemoryProductQuery에 주입하기 위한 메서드입니다.
    /// </summary>
    public ConcurrentDictionary<ProductId, Product> GetStore() => _store;
}
