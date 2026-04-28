using System.Collections.Concurrent;
using Functorium.Adapters.Repositories;
using Functorium.Applications.Events;

namespace FinTToFinResponse;

public sealed class InMemoryProductRepository(IDomainEventCollector eventCollector)
    : InMemoryRepositoryBase<Product, ProductId>(eventCollector), IProductRepository
{
    private static readonly ConcurrentDictionary<ProductId, Product> _store = new();

    protected override ConcurrentDictionary<ProductId, Product> Store => _store;

    public static void Clear() => _store.Clear();
}
