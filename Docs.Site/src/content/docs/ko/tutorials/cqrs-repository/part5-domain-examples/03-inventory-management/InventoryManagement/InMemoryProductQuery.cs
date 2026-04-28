using System.Collections.Concurrent;
using Functorium.Adapters.Repositories;
using Functorium.Domains.Specifications;

namespace InventoryManagement;

/// <summary>
/// 상품 InMemory Query Adapter.
/// Cursor 기반 페이지네이션을 포함한 모든 쿼리 메서드를 지원합니다.
/// </summary>
public sealed class InMemoryProductQuery(
    ConcurrentDictionary<ProductId, Product> store) : InMemoryQueryBase<Product, ProductDto>
{
    protected override string DefaultSortField => "Name";

    protected override IEnumerable<ProductDto> GetProjectedItems(Specification<Product> spec) =>
        store.Values
            .Where(p => spec.IsSatisfiedBy(p))
            .Select(p => new ProductDto(p.Id.ToString(), p.Name, p.Price, p.Stock));

    protected override Func<ProductDto, object> SortSelector(string fieldName) =>
        fieldName.ToUpperInvariant() switch
        {
            "NAME" => dto => dto.Name,
            "PRICE" => dto => dto.Price,
            "STOCK" => dto => dto.Stock,
            _ => dto => dto.Name
        };
}
