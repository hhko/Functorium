using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Inventories.Ports;
using LayeredArch.Domain.AggregateRoots.Inventories;

namespace LayeredArch.Adapters.Persistence.Repositories.InMemory.Inventories;

/// <summary>
/// InMemory 기반 Inventory 읽기 전용 어댑터.
/// InMemoryInventoryRepository의 정적 저장소에서 데이터를 가져온 후 정렬/페이지네이션/DTO 변환합니다.
/// </summary>
[GenerateObservablePort]
public class InMemoryInventoryQuery
    : InMemoryQueryBase<Inventory, InventorySummaryDto>, IInventoryQuery
{
    public string RequestCategory => "QueryAdapter";

    protected override string DefaultSortField => "Id";

    protected override IEnumerable<InventorySummaryDto> GetProjectedItems(Specification<Inventory> spec)
    {
        return InMemoryInventoryRepository.Inventories.Values
            .Where(i => spec.IsSatisfiedBy(i))
            .Select(i => new InventorySummaryDto(
                i.Id.ToString(), i.ProductId.ToString(), i.StockQuantity));
    }

    protected override Func<InventorySummaryDto, object> SortSelector(string fieldName) => fieldName switch
    {
        "StockQuantity" => i => i.StockQuantity,
        "ProductId" => i => i.ProductId,
        _ => i => i.InventoryId
    };
}
