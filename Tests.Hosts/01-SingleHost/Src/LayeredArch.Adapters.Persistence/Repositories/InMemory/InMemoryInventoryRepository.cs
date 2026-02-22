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
[GeneratePortObservable]
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

    public virtual FinT<IO, Unit> Delete(InventoryId id)
    {
        return IO.lift(() =>
        {
            if (!Inventories.TryRemove(id, out _))
            {
                return AdapterError.For<InMemoryInventoryRepository>(
                    new NotFound(),
                    id.ToString(),
                    $"재고 ID '{id}'을(를) 찾을 수 없습니다");
            }

            return Fin.Succ(unit);
        });
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
