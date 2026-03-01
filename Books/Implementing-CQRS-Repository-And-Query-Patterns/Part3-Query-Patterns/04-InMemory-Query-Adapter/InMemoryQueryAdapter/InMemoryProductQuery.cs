using System.Collections.Concurrent;
using Functorium.Adapters.Repositories;
using Functorium.Domains.Specifications;

namespace InMemoryQueryAdapter;

/// <summary>
/// InMemoryQueryBase를 상속한 Product 전용 Query Adapter.
/// ConcurrentDictionary를 내부 저장소로 사용합니다.
/// </summary>
public sealed class InMemoryProductQuery : InMemoryQueryBase<Product, ProductDto>
{
    private readonly ConcurrentDictionary<ProductId, Product> _store = new();

    protected override string DefaultSortField => "Name";

    /// <summary>
    /// 테스트를 위한 상품 추가 메서드.
    /// </summary>
    public void Add(Product product) => _store[product.Id] = product;

    protected override IEnumerable<ProductDto> GetProjectedItems(Specification<Product> spec) =>
        _store.Values
            .Where(p => spec.IsSatisfiedBy(p))
            .Select(p => new ProductDto(
                p.Id.ToString(),
                p.Name,
                p.Price,
                p.Stock,
                p.Category));

    protected override Func<ProductDto, object> SortSelector(string fieldName) =>
        fieldName switch
        {
            "Name" => dto => dto.Name,
            "Price" => dto => dto.Price,
            "Stock" => dto => dto.Stock,
            "Category" => dto => dto.Category,
            _ => dto => dto.Name
        };
}
