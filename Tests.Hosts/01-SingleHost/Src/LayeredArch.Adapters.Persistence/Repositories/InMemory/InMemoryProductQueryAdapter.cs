using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Products.Ports;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Adapters.Persistence.Repositories.InMemory;

/// <summary>
/// InMemory 기반 Product 읽기 전용 어댑터.
/// InMemoryProductRepository의 정적 저장소에서 데이터를 가져온 후 정렬/페이지네이션/DTO 변환합니다.
/// </summary>
[GenerateObservablePort]
public class InMemoryProductQueryAdapter : IProductQuery
{
    public string RequestCategory => "QueryAdapter";

    public virtual FinT<IO, PagedResult<ProductSummaryDto>> Search(
        Specification<Product> spec, PageRequest page, SortExpression sort)
    {
        return IO.lift(() =>
        {
            var allProducts = toSeq(InMemoryProductRepository.Products.Values)
                .Where(p => p.DeletedAt.IsNone).ToSeq();
            var filtered = allProducts.Where(p => spec.IsSatisfiedBy(p)).ToSeq();

            var sorted = ApplySort(filtered, sort);
            var totalCount = sorted.Count;
            var items = sorted
                .Skip(page.Skip)
                .Take(page.PageSize)
                .Select(p => new ProductSummaryDto(p.Id.ToString(), p.Name, p.Price))
                .ToSeq();

            return Fin.Succ(new PagedResult<ProductSummaryDto>(
                items, totalCount, page.Page, page.PageSize));
        });
    }

    private static Seq<Product> ApplySort(Seq<Product> products, SortExpression sort)
    {
        if (sort.IsEmpty)
            return toSeq(products.OrderBy(p => (string)p.Name));

        IOrderedEnumerable<Product>? ordered = null;

        foreach (var field in sort.Fields)
        {
            Func<Product, object> selector = field.FieldName switch
            {
                "Name" => p => (string)p.Name,
                "Price" => p => (decimal)p.Price,
                _ => p => (string)p.Name
            };

            var isDesc = field.Direction == SortDirection.Descending;
            ordered = (ordered, isDesc) switch
            {
                (null, false) => products.OrderBy(selector),
                (null, true) => products.OrderByDescending(selector),
                (_, false) => ordered!.ThenBy(selector),
                _ => ordered!.ThenByDescending(selector),
            };
        }

        return ordered is not null ? toSeq(ordered) : products;
    }
}
