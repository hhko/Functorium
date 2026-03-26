using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Products.Ports;
using LayeredArch.Domain.AggregateRoots.Products;

using LayeredArch.Adapters.Persistence.Repositories.Products.Repositories;

namespace LayeredArch.Adapters.Persistence.Repositories.Products.Queries;

/// <summary>
/// InMemory 기반 Product 읽기 전용 어댑터.
/// ProductRepositoryInMemory의 정적 저장소에서 데이터를 가져온 후 정렬/페이지네이션/DTO 변환합니다.
/// </summary>
[GenerateObservablePort]
public class ProductQueryInMemory
    : InMemoryQueryBase<Product, ProductSummaryDto>, IProductQuery
{
    public string RequestCategory => "QueryAdapter";

    protected override string DefaultSortField => "Name";

    protected override IEnumerable<ProductSummaryDto> GetProjectedItems(Specification<Product> spec)
    {
        return ProductRepositoryInMemory.Products.Values
            .Where(p => p.DeletedAt.IsNone && spec.IsSatisfiedBy(p))
            .Select(p => new ProductSummaryDto(p.Id.ToString(), p.Name, p.Price));
    }

    protected override Func<ProductSummaryDto, object> SortSelector(string fieldName) => fieldName switch
    {
        "Name" => p => p.Name,
        "Price" => p => p.Price,
        _ => p => p.Name
    };
}
