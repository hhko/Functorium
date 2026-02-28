using System.Collections.Concurrent;
using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using Functorium.Domains.Specifications;
using LayeredArch.Domain.AggregateRoots.Inventories;
using LayeredArch.Domain.AggregateRoots.Products;
using static Functorium.Adapters.Errors.AdapterErrorType;
using static LanguageExt.Prelude;

namespace LayeredArch.Adapters.Persistence.Repositories.InMemory;

/// <summary>
/// 메모리 기반 재고 리포지토리 구현
/// </summary>
[GenerateObservablePort]
public class InMemoryInventoryRepository : IInventoryRepository
{
    internal static readonly ConcurrentDictionary<InventoryId, Inventory> Inventories = new();
    private readonly IDomainEventCollector _eventCollector;

    public string RequestCategory => "Repository";

    public InMemoryInventoryRepository(IDomainEventCollector eventCollector)
    {
        _eventCollector = eventCollector;
    }

    public virtual FinT<IO, Inventory> Create(Inventory inventory)
    {
        return IO.lift(() =>
        {
            Inventories[inventory.Id] = inventory;
            _eventCollector.Track(inventory);
            return Fin.Succ(inventory);
        });
    }

    public virtual FinT<IO, Inventory> GetById(InventoryId id)
    {
        return IO.lift(() =>
        {
            if (Inventories.TryGetValue(id, out Inventory? inventory))
            {
                return Fin.Succ(inventory);
            }

            return AdapterError.For<InMemoryInventoryRepository>(
                new NotFound(),
                id.ToString(),
                $"재고 ID '{id}'을(를) 찾을 수 없습니다");
        });
    }

    public virtual FinT<IO, Inventory> GetByProductId(ProductId productId)
    {
        return IO.lift(() =>
        {
            var inventory = Inventories.Values.FirstOrDefault(i =>
                i.ProductId.Equals(productId));

            if (inventory is not null)
            {
                return Fin.Succ(inventory);
            }

            return AdapterError.For<InMemoryInventoryRepository>(
                new NotFound(),
                productId.ToString(),
                $"상품 ID '{productId}'에 대한 재고를 찾을 수 없습니다");
        });
    }

    public virtual FinT<IO, Inventory> Update(Inventory inventory)
    {
        return IO.lift(() =>
        {
            if (!Inventories.ContainsKey(inventory.Id))
            {
                return AdapterError.For<InMemoryInventoryRepository>(
                    new NotFound(),
                    inventory.Id.ToString(),
                    $"재고 ID '{inventory.Id}'을(를) 찾을 수 없습니다");
            }

            Inventories[inventory.Id] = inventory;
            _eventCollector.Track(inventory);
            return Fin.Succ(inventory);
        });
    }

    public virtual FinT<IO, int> Delete(InventoryId id)
    {
        return IO.lift(() =>
        {
            return Fin.Succ(Inventories.TryRemove(id, out _) ? 1 : 0);
        });
    }

    public virtual FinT<IO, Seq<Inventory>> CreateRange(IReadOnlyList<Inventory> inventories)
    {
        return IO.lift(() =>
        {
            foreach (var inventory in inventories)
                Inventories[inventory.Id] = inventory;
            _eventCollector.TrackRange(inventories);
            return Fin.Succ(toSeq(inventories));
        });
    }

    public virtual FinT<IO, Seq<Inventory>> GetByIds(IReadOnlyList<InventoryId> ids)
    {
        return IO.lift(() =>
        {
            var distinctIds = ids.Distinct().ToList();
            var result = distinctIds
                .Where(id => Inventories.ContainsKey(id))
                .Select(id => Inventories[id])
                .ToList();

            if (result.Count != distinctIds.Count)
            {
                var foundIds = result.Select(i => i.Id.ToString()).ToHashSet();
                var missingIds = distinctIds.Where(id => !foundIds.Contains(id.ToString())).ToList();
                var missingIdsStr = FormatIds(missingIds);
                return AdapterError.For<InMemoryInventoryRepository>(
                    new PartialNotFound(), missingIdsStr,
                    $"Requested {distinctIds.Count} but found {result.Count}. Missing IDs: {missingIdsStr}");
            }

            return Fin.Succ(toSeq(result));
        });
    }

    public virtual FinT<IO, Seq<Inventory>> UpdateRange(IReadOnlyList<Inventory> inventories)
    {
        return IO.lift(() =>
        {
            foreach (var inventory in inventories)
                Inventories[inventory.Id] = inventory;
            _eventCollector.TrackRange(inventories);
            return Fin.Succ(toSeq(inventories));
        });
    }

    public virtual FinT<IO, int> DeleteRange(IReadOnlyList<InventoryId> ids)
    {
        return IO.lift(() =>
        {
            int affected = 0;
            foreach (var id in ids)
            {
                if (Inventories.TryRemove(id, out _))
                    affected++;
            }
            return Fin.Succ(affected);
        });
    }

    private static string FormatIds<T>(IEnumerable<T> ids, int maxDisplay = 3)
    {
        var list = ids.Select(id => id!.ToString()!).ToList();
        if (list.Count <= maxDisplay)
            return string.Join(", ", list);

        return string.Join(", ", list.Take(maxDisplay)) + $" ... (+{list.Count - maxDisplay} more)";
    }

    public virtual FinT<IO, bool> Exists(Specification<Inventory> spec)
    {
        return IO.lift(() =>
        {
            bool exists = Inventories.Values.Any(i => spec.IsSatisfiedBy(i));
            return Fin.Succ(exists);
        });
    }
}
