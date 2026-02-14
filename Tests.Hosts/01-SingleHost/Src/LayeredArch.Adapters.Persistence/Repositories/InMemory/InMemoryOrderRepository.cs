using System.Collections.Concurrent;
using LayeredArch.Domain.AggregateRoots.Orders;
using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Adapters.Errors.AdapterErrorType;

namespace LayeredArch.Adapters.Persistence.Repositories.InMemory;

/// <summary>
/// 메모리 기반 주문 리포지토리 구현
/// </summary>
[GeneratePipeline]
public class InMemoryOrderRepository : IOrderRepository
{
    private static readonly ConcurrentDictionary<OrderId, Order> _orders = new();

    public string RequestCategory => "Repository";

    public InMemoryOrderRepository()
    {
    }

    public virtual FinT<IO, Order> Create(Order order)
    {
        return IO.lift(() =>
        {
            _orders[order.Id] = order;
            return Fin.Succ(order);
        });
    }

    public virtual FinT<IO, Order> GetById(OrderId id)
    {
        return IO.lift(() =>
        {
            if (_orders.TryGetValue(id, out Order? order))
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
            if (!_orders.ContainsKey(order.Id))
            {
                return AdapterError.For<InMemoryOrderRepository>(
                    new NotFound(),
                    order.Id.ToString(),
                    $"주문 ID '{order.Id}'을(를) 찾을 수 없습니다");
            }

            _orders[order.Id] = order;
            return Fin.Succ(order);
        });
    }

    public virtual FinT<IO, Unit> Delete(OrderId id)
    {
        return IO.lift(() =>
        {
            if (!_orders.TryRemove(id, out _))
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
