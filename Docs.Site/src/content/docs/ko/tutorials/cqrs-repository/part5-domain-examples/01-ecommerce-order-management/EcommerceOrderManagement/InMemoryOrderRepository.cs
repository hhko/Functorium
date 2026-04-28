using System.Collections.Concurrent;
using Functorium.Adapters.Repositories;
using Functorium.Applications.Events;

namespace EcommerceOrderManagement;

/// <summary>
/// 주문 InMemory Repository 구현.
/// </summary>
public sealed class InMemoryOrderRepository : InMemoryRepositoryBase<Order, OrderId>, IOrderRepository
{
    private readonly ConcurrentDictionary<OrderId, Order> _store = new();
    protected override ConcurrentDictionary<OrderId, Order> Store => _store;

    public InMemoryOrderRepository(IDomainEventCollector eventCollector) : base(eventCollector) { }
}
