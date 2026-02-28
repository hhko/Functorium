using System.Collections.Concurrent;
using LayeredArch.Domain.AggregateRoots.Orders;
using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Adapters.Errors.AdapterErrorType;
using static LanguageExt.Prelude;

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

    public virtual FinT<IO, int> Delete(OrderId id)
    {
        return IO.lift(() =>
        {
            return Fin.Succ(Orders.TryRemove(id, out _) ? 1 : 0);
        });
    }

    public virtual FinT<IO, Seq<Order>> CreateRange(IReadOnlyList<Order> orders)
    {
        return IO.lift(() =>
        {
            foreach (var order in orders)
                Orders[order.Id] = order;
            _eventCollector.TrackRange(orders);
            return Fin.Succ(toSeq(orders));
        });
    }

    public virtual FinT<IO, Seq<Order>> GetByIds(IReadOnlyList<OrderId> ids)
    {
        return IO.lift(() =>
        {
            var distinctIds = ids.Distinct().ToList();
            var result = distinctIds
                .Where(id => Orders.ContainsKey(id))
                .Select(id => Orders[id])
                .ToList();

            if (result.Count != distinctIds.Count)
            {
                var foundIds = result.Select(o => o.Id.ToString()).ToHashSet();
                var missingIds = distinctIds.Where(id => !foundIds.Contains(id.ToString())).ToList();
                var missingIdsStr = FormatIds(missingIds);
                return AdapterError.For<InMemoryOrderRepository>(
                    new PartialNotFound(), missingIdsStr,
                    $"Requested {distinctIds.Count} but found {result.Count}. Missing IDs: {missingIdsStr}");
            }

            return Fin.Succ(toSeq(result));
        });
    }

    public virtual FinT<IO, Seq<Order>> UpdateRange(IReadOnlyList<Order> orders)
    {
        return IO.lift(() =>
        {
            foreach (var order in orders)
                Orders[order.Id] = order;
            _eventCollector.TrackRange(orders);
            return Fin.Succ(toSeq(orders));
        });
    }

    private static string FormatIds<T>(IEnumerable<T> ids, int maxDisplay = 3)
    {
        var list = ids.Select(id => id!.ToString()!).ToList();
        if (list.Count <= maxDisplay)
            return string.Join(", ", list);

        return string.Join(", ", list.Take(maxDisplay)) + $" ... (+{list.Count - maxDisplay} more)";
    }

    public virtual FinT<IO, int> DeleteRange(IReadOnlyList<OrderId> ids)
    {
        return IO.lift(() =>
        {
            int affected = 0;
            foreach (var id in ids)
            {
                if (Orders.TryRemove(id, out _))
                    affected++;
            }
            return Fin.Succ(affected);
        });
    }
}
