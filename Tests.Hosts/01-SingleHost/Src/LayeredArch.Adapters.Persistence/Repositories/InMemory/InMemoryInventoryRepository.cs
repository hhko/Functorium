using System.Collections.Concurrent;
using Functorium.Adapters.Errors;
using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using Functorium.Domains.Specifications;
using LayeredArch.Domain.AggregateRoots.Inventories;
using LayeredArch.Domain.AggregateRoots.Products;
using static Functorium.Adapters.Errors.AdapterErrorType;

namespace LayeredArch.Adapters.Persistence.Repositories.InMemory;

/// <summary>
/// 메모리 기반 재고 리포지토리 구현
/// </summary>
[GenerateObservablePort]
public class InMemoryInventoryRepository
    : InMemoryRepositoryBase<Inventory, InventoryId>, IInventoryRepository
{
    internal static readonly ConcurrentDictionary<InventoryId, Inventory> Inventories = new();
    protected override ConcurrentDictionary<InventoryId, Inventory> Store => Inventories;

    public InMemoryInventoryRepository(IDomainEventCollector eventCollector)
        : base(eventCollector) { }

    // ─── Inventory 고유 메서드 ───────────────────────

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

    public virtual FinT<IO, bool> Exists(Specification<Inventory> spec)
    {
        return IO.lift(() =>
        {
            bool exists = Inventories.Values.Any(i => spec.IsSatisfiedBy(i));
            return Fin.Succ(exists);
        });
    }
}
