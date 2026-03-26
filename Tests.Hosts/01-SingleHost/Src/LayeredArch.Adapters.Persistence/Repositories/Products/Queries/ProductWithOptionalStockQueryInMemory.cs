using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Products.Ports;
using LayeredArch.Domain.AggregateRoots.Products;

using LayeredArch.Adapters.Persistence.Repositories.Inventories.Repositories;
using LayeredArch.Adapters.Persistence.Repositories.Products.Repositories;

namespace LayeredArch.Adapters.Persistence.Repositories.Products.Queries;

/// <summary>
/// InMemory 기반 Product + Optional Inventory LEFT JOIN 읽기 전용 어댑터.
/// 재고 없는 상품도 StockQuantity = null로 포함합니다.
/// </summary>
[GenerateObservablePort]
public class ProductWithOptionalStockQueryInMemory
    : InMemoryQueryBase<Product, ProductWithOptionalStockDto>, IProductWithOptionalStockQuery
{
    public string RequestCategory => "QueryAdapter";

    protected override string DefaultSortField => "Name";

    protected override IEnumerable<ProductWithOptionalStockDto> GetProjectedItems(Specification<Product> spec)
    {
        var inventoryLookup = InventoryRepositoryInMemory.Inventories.Values
            .ToDictionary(i => i.ProductId, i => (int)i.StockQuantity);

        return ProductRepositoryInMemory.Products.Values
            .Where(p => p.DeletedAt.IsNone && spec.IsSatisfiedBy(p))
            .Select(p =>
            {
                int? stockQuantity = inventoryLookup.TryGetValue(p.Id, out var qty) ? qty : null;
                return new ProductWithOptionalStockDto(p.Id.ToString(), p.Name, p.Price, stockQuantity);
            });
    }

    protected override Func<ProductWithOptionalStockDto, object> SortSelector(string fieldName) => fieldName switch
    {
        "Name" => p => p.Name,
        "Price" => p => p.Price,
        "StockQuantity" => p => p.StockQuantity ?? -1,
        _ => p => p.Name
    };
}
