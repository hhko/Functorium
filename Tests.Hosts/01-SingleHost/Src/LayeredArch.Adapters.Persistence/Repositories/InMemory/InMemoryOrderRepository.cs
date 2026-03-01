using System.Collections.Concurrent;
using LayeredArch.Domain.AggregateRoots.Orders;
using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;

namespace LayeredArch.Adapters.Persistence.Repositories.InMemory;

/// <summary>
/// 메모리 기반 주문 리포지토리 구현
/// </summary>
[GenerateObservablePort]
public class InMemoryOrderRepository
    : InMemoryRepositoryBase<Order, OrderId>, IOrderRepository
{
    internal static readonly ConcurrentDictionary<OrderId, Order> Orders = new();
    protected override ConcurrentDictionary<OrderId, Order> Store => Orders;

    public InMemoryOrderRepository(IDomainEventCollector eventCollector)
        : base(eventCollector) { }
}
