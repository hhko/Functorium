using System.Linq.Expressions;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Inventories.Dtos;
using LayeredArch.Application.Usecases.Inventories.Ports;
using LayeredArch.Domain.AggregateRoots.Inventories;

namespace LayeredArch.Adapters.Persistence.Repositories.InMemory;

/// <summary>
/// InMemory 기반 Inventory 읽기 전용 어댑터.
/// InMemoryInventoryRepository를 위임하여 데이터를 가져온 후 정렬/페이지네이션/DTO 변환합니다.
/// </summary>
[GeneratePipeline]
public class InMemoryInventoryQueryAdapter : IInventoryQueryAdapter
{
    private readonly InMemoryInventoryRepository _repository;

    public string RequestCategory => "QueryAdapter";

    public InMemoryInventoryQueryAdapter(InMemoryInventoryRepository repository)
    {
        _repository = repository;
    }

    public virtual FinT<IO, PagedResult<InventorySummaryDto>> Search(
        Specification<Inventory>? spec, PageRequest page, SortExpression sort)
    {
        return IO.liftAsync(async () =>
        {
            // IInventoryRepository에 GetAll()이 없으므로 항상 참인 Spec으로 대체
            var effectiveSpec = spec ?? new AllMatchSpec();
            Fin<Seq<Inventory>> result = await _repository.FindAll(effectiveSpec).Run().RunAsync();

            return result.Match(
                Succ: inventories =>
                {
                    var sorted = ApplySort(inventories, sort);
                    var totalCount = sorted.Count;
                    var items = sorted
                        .Skip(page.Skip)
                        .Take(page.PageSize)
                        .Select(i => new InventorySummaryDto(
                            i.Id.ToString(), i.ProductId.ToString(), i.StockQuantity))
                        .ToSeq();

                    return Fin.Succ(new PagedResult<InventorySummaryDto>(
                        items, totalCount, page.Page, page.PageSize));
                },
                Fail: error => Fin.Fail<PagedResult<InventorySummaryDto>>(error));
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

            ordered = (ordered, field.Direction) switch
            {
                (null, SortDirection.Ascending) => inventories.OrderBy(selector),
                (null, SortDirection.Descending) => inventories.OrderByDescending(selector),
                (_, SortDirection.Ascending) => ordered!.ThenBy(selector),
                _ => ordered!.ThenByDescending(selector),
            };
        }

        return ordered is not null ? toSeq(ordered) : inventories;
    }

    /// <summary>
    /// 모든 항목을 매치하는 Specification (GetAll 대용).
    /// </summary>
    private sealed class AllMatchSpec : ExpressionSpecification<Inventory>
    {
        public override Expression<Func<Inventory, bool>> ToExpression()
            => _ => true;
    }
}
