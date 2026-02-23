using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Products.Ports;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Adapters.Persistence.Repositories.InMemory;

/// <summary>
/// InMemory 기반 Product + Inventory JOIN 읽기 전용 어댑터.
/// InMemoryProductRepository와 InMemoryInventoryRepository의 정적 저장소에서
/// 데이터를 결합하여 정렬/페이지네이션/DTO 변환합니다.
/// </summary>
[GenerateObservablePort]
public class InMemoryProductWithStockQueryAdapter : IProductWithStockQuery
{
    public string RequestCategory => "QueryAdapter";

    public virtual FinT<IO, PagedResult<ProductWithStockDto>> Search(
        Specification<Product> spec, PageRequest page, SortExpression sort)
    {
        return IO.lift(() =>
        {
            var allProducts = toSeq(InMemoryProductRepository.Products.Values)
                .Where(p => p.DeletedAt.IsNone).ToSeq();
            var filtered = allProducts.Where(p => spec.IsSatisfiedBy(p)).ToSeq();

            // Product + Inventory JOIN
            var joined = filtered
                .Select(p =>
                {
                    var inventory = InMemoryInventoryRepository.Inventories.Values
                        .FirstOrDefault(i => i.ProductId.Equals(p.Id));
                    var stockQuantity = inventory is not null ? (int)inventory.StockQuantity : 0;
                    return new ProductWithStockDto(p.Id.ToString(), p.Name, p.Price, stockQuantity);
                })
                .ToSeq();

            var sorted = ApplySort(joined, sort);
            var totalCount = sorted.Count;
            var items = sorted
                .Skip(page.Skip)
                .Take(page.PageSize)
                .ToSeq();

            return Fin.Succ(new PagedResult<ProductWithStockDto>(
                items, totalCount, page.Page, page.PageSize));
        });
    }

    private static Seq<ProductWithStockDto> ApplySort(Seq<ProductWithStockDto> items, SortExpression sort)
    {
        if (sort.IsEmpty)
            return toSeq(items.OrderBy(p => p.Name));

        IOrderedEnumerable<ProductWithStockDto>? ordered = null;

        foreach (var field in sort.Fields)
        {
            Func<ProductWithStockDto, object> selector = field.FieldName switch
            {
                "Name" => p => p.Name,
                "Price" => p => p.Price,
                "StockQuantity" => p => p.StockQuantity,
                _ => p => p.Name
            };

            var isDesc = field.Direction == SortDirection.Descending;
            ordered = (ordered, isDesc) switch
            {
                (null, false) => items.OrderBy(selector),
                (null, true) => items.OrderByDescending(selector),
                (_, false) => ordered!.ThenBy(selector),
                _ => ordered!.ThenByDescending(selector),
            };
        }

        return ordered is not null ? toSeq(ordered) : items;
    }
}
