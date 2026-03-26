using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Products.Ports;
using LayeredArch.Domain.AggregateRoots.Products;

using LayeredArch.Adapters.Persistence.Repositories.Inventories.Repositories;
using LayeredArch.Adapters.Persistence.Repositories.Products.Repositories;

namespace LayeredArch.Adapters.Persistence.Repositories.Products.Queries;

/// <summary>
/// InMemory 기반 Product + Inventory JOIN 읽기 전용 어댑터.
/// ProductRepositoryInMemory와 InventoryRepositoryInMemory의 정적 저장소에서
/// 데이터를 결합하여 정렬/페이지네이션/DTO 변환합니다.
/// </summary>
[GenerateObservablePort]
public class ProductWithStockQueryInMemory
    : InMemoryQueryBase<Product, ProductWithStockDto>, IProductWithStockQuery
{
    public string RequestCategory => "QueryAdapter";

    protected override string DefaultSortField => "Name";

    protected override IEnumerable<ProductWithStockDto> GetProjectedItems(Specification<Product> spec)
    {
        var inventoryLookup = InventoryRepositoryInMemory.Inventories.Values
            .ToDictionary(i => i.ProductId, i => (int)i.StockQuantity);

        return ProductRepositoryInMemory.Products.Values
            .Where(p => p.DeletedAt.IsNone && spec.IsSatisfiedBy(p))
            .Select(p =>
            {
                var stockQuantity = inventoryLookup.GetValueOrDefault(p.Id, 0);
                return new ProductWithStockDto(p.Id.ToString(), p.Name, p.Price, stockQuantity);
            });
    }

    protected override Func<ProductWithStockDto, object> SortSelector(string fieldName) => fieldName switch
    {
        "Name" => p => p.Name,
        "Price" => p => p.Price,
        "StockQuantity" => p => p.StockQuantity,
        _ => p => p.Name
    };
}
