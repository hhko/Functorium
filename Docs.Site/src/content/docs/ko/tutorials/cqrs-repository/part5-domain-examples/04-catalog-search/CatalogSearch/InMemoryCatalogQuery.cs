using System.Collections.Concurrent;
using Functorium.Adapters.Repositories;
using Functorium.Domains.Specifications;

namespace CatalogSearch;

/// <summary>
/// 카탈로그 InMemory Query Adapter.
/// Search (Offset), SearchByCursor (Keyset), Stream (비동기) 3가지 조회를 지원합니다.
/// </summary>
public sealed class InMemoryCatalogQuery(
    ConcurrentDictionary<ProductId, Product> store) : InMemoryQueryBase<Product, ProductDto>
{
    protected override string DefaultSortField => "Name";

    protected override IEnumerable<ProductDto> GetProjectedItems(Specification<Product> spec) =>
        store.Values
            .Where(p => spec.IsSatisfiedBy(p))
            .Select(p => new ProductDto(p.Id.ToString(), p.Name, p.Category, p.Price, p.Stock));

    protected override Func<ProductDto, object> SortSelector(string fieldName) =>
        fieldName.ToUpperInvariant() switch
        {
            "NAME" => dto => dto.Name,
            "CATEGORY" => dto => dto.Category,
            "PRICE" => dto => dto.Price,
            "STOCK" => dto => dto.Stock,
            _ => dto => dto.Name
        };
}
