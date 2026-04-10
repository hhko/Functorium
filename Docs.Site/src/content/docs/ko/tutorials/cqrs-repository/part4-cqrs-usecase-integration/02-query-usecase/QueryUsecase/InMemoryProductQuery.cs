using System.Collections.Concurrent;
using Functorium.Adapters.Repositories;
using Functorium.Domains.Specifications;

namespace QueryUsecase;

/// <summary>
/// InMemoryQueryBase + IProductQuery 동시 구현.
/// InMemoryQueryBase는 IQueryPort를 자체 구현하지 않으므로 IProductQuery도 명시적으로 구현합니다.
/// </summary>
public sealed class InMemoryProductQuery : InMemoryQueryBase<Product, ProductDto>, IProductQuery
{
    private readonly ConcurrentDictionary<ProductId, Product> _store = new();

    public string RequestCategory => "Query";

    protected override string DefaultSortField => "Name";

    public void Add(Product product) => _store[product.Id] = product;

    protected override IEnumerable<ProductDto> GetProjectedItems(Specification<Product> spec) =>
        _store.Values
            .Where(p => spec.IsSatisfiedBy(p))
            .Select(p => new ProductDto(
                p.Id.ToString(),
                p.Name,
                p.Price,
                p.IsActive));

    protected override Func<ProductDto, object> SortSelector(string fieldName) =>
        fieldName switch
        {
            "Name" => dto => dto.Name,
            "Price" => dto => dto.Price,
            _ => dto => dto.Name
        };
}
