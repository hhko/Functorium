using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Inventories.Ports;
using LayeredArch.Domain.AggregateRoots.Inventories;

namespace LayeredArch.Adapters.Persistence.Repositories.InMemory;

/// <summary>
/// InMemory 기반 Inventory 읽기 전용 어댑터.
/// InMemoryInventoryRepository의 정적 저장소에서 데이터를 가져온 후 정렬/페이지네이션/DTO 변환합니다.
/// </summary>
[GenerateObservablePort]
public class InMemoryInventoryQueryAdapter : IInventoryQuery
{
    public string RequestCategory => "QueryAdapter";

    public virtual FinT<IO, PagedResult<InventorySummaryDto>> Search(
        Specification<Inventory> spec, PageRequest page, SortExpression sort)
    {
        return IO.lift(() =>
        {
            var allInventories = toSeq(InMemoryInventoryRepository.Inventories.Values);
            var filtered = allInventories.Where(i => spec.IsSatisfiedBy(i)).ToSeq();

            var sorted = ApplySort(filtered, sort);
            var totalCount = sorted.Count;
            var items = sorted
                .Skip(page.Skip)
                .Take(page.PageSize)
                .Select(i => new InventorySummaryDto(
                    i.Id.ToString(), i.ProductId.ToString(), i.StockQuantity))
                .ToSeq();

            return Fin.Succ(new PagedResult<InventorySummaryDto>(
                items, totalCount, page.Page, page.PageSize));
        });
    }

    private static Seq<Inventory> ApplySort(Seq<Inventory> inventories, SortExpression sort)
    {
        if (sort.IsEmpty)
            return toSeq(inventories.OrderBy(i => i.Id.ToString()));

        IOrderedEnumerable<Inventory>? ordered = null;

        foreach (var field in sort.Fields)
        {
            Func<Inventory, object> selector = field.FieldName switch
            {
                "StockQuantity" => i => (int)i.StockQuantity,
                "ProductId" => i => i.ProductId.ToString(),
                _ => i => i.Id.ToString()
            };

            var isDesc = field.Direction == SortDirection.Descending;
            ordered = (ordered, isDesc) switch
            {
                (null, false) => inventories.OrderBy(selector),
                (null, true) => inventories.OrderByDescending(selector),
                (_, false) => ordered!.ThenBy(selector),
                _ => ordered!.ThenByDescending(selector),
            };
        }

        return ordered is not null ? toSeq(ordered) : inventories;
    }
}
