using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Products.Dtos;
using LayeredArch.Application.Usecases.Products.Ports;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Adapters.Persistence.Repositories.InMemory;

/// <summary>
/// InMemory 기반 Product 읽기 전용 어댑터.
/// InMemoryProductRepository를 위임하여 데이터를 가져온 후 정렬/페이지네이션/DTO 변환합니다.
/// </summary>
[GeneratePipeline]
public class InMemoryProductQueryAdapter : IProductQueryAdapter
{
    private readonly InMemoryProductRepository _repository;

    public string RequestCategory => "QueryAdapter";

    public InMemoryProductQueryAdapter(InMemoryProductRepository repository)
    {
        _repository = repository;
    }

    public virtual FinT<IO, PagedResult<ProductSummaryDto>> Search(
        Specification<Product>? spec, PageRequest page, SortExpression sort)
    {
        return IO.liftAsync(async () =>
        {
            Fin<Seq<Product>> result = spec is not null
                ? await _repository.FindAll(spec).Run().RunAsync()
                : await _repository.GetAll().Run().RunAsync();

            return result.Match(
                Succ: products =>
                {
                    var sorted = ApplySort(products, sort);
                    var totalCount = sorted.Count;
                    var items = sorted
                        .Skip(page.Skip)
                        .Take(page.PageSize)
                        .Select(p => new ProductSummaryDto(p.Id.ToString(), p.Name, p.Price))
                        .ToSeq();

                    return Fin.Succ(new PagedResult<ProductSummaryDto>(
                        items, totalCount, page.Page, page.PageSize));
                },
                Fail: error => Fin.Fail<PagedResult<ProductSummaryDto>>(error));
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

            ordered = (ordered, field.Direction) switch
            {
                (null, SortDirection.Ascending) => products.OrderBy(selector),
                (null, SortDirection.Descending) => products.OrderByDescending(selector),
                (_, SortDirection.Ascending) => ordered!.ThenBy(selector),
                _ => ordered!.ThenByDescending(selector),
            };
        }

        return ordered is not null ? toSeq(ordered) : products;
    }
}
