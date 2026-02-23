using System.Collections.Concurrent;
using LayeredArch.Domain.AggregateRoots.Orders;
using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Adapters.Errors.AdapterErrorType;

namespace LayeredArch.Adapters.Persistence.Repositories.InMemory;

/// <summary>
/// 메모리 기반 주문 리포지토리 구현
/// </summary>
[GenerateObservablePort]
public class InMemoryOrderRepository : IOrderRepository
{
    internal static readonly ConcurrentDictionary<OrderId, Order> Orders = new();
    private readonly IDomainEventCollector _eventCollector;

    public string RequestCategory => "Repository";

    public InMemoryOrderRepository(IDomainEventCollector eventCollector)
    {
        _eventCollector = eventCollector;
    }

    public virtual FinT<IO, Order> Create(Order order)
    {
        return IO.lift(() =>
        {
            Orders[order.Id] = order;
            _eventCollector.Track(order);
            return Fin.Succ(order);
        });
    }

    public virtual FinT<IO, Order> GetById(OrderId id)
    {
        return IO.lift(() =>
        {
            if (Orders.TryGetValue(id, out Order? order))
            {
                return Fin.Succ(order);
            }

            return AdapterError.For<InMemoryOrderRepository>(
                new NotFound(),
                id.ToString(),
                $"주문 ID '{id}'을(를) 찾을 수 없습니다");
        });
    }

    public virtual FinT<IO, Order> Update(Order order)
    {
        return IO.lift(() =>
        {
            if (!Orders.ContainsKey(order.Id))
            {
                return AdapterError.For<InMemoryOrderRepository>(
                    new NotFound(),
                    order.Id.ToString(),
                    $"주문 ID '{order.Id}'을(를) 찾을 수 없습니다");
            }

            Orders[order.Id] = order;
            _eventCollector.Track(order);
            return Fin.Succ(order);
        });
    }

    public virtual FinT<IO, Unit> Delete(OrderId id)
    {
        return IO.lift(() =>
        {
            if (!Orders.TryRemove(id, out _))
            {
                return AdapterError.For<InMemoryOrderRepository>(
                    new NotFound(),
                    id.ToString(),
                    $"주문 ID '{id}'을(를) 찾을 수 없습니다");
            }

            return Fin.Succ(unit);
        });
    }
}
