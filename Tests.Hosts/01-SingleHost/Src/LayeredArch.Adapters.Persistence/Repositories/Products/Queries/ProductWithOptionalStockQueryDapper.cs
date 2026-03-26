using System.Data;
using Functorium.Adapters.SourceGenerators;
using Functorium.Adapters.Repositories;
using LayeredArch.Application.Usecases.Products.Ports;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Adapters.Persistence.Repositories.Products.Queries;

/// <summary>
/// Product + Optional Inventory LEFT JOIN 쿼리 어댑터.
/// 재고 없는 상품도 포함하는 LEFT JOIN 패턴입니다.
/// </summary>
[GenerateObservablePort]
public class ProductWithOptionalStockQueryDapper
    : DapperQueryBase<Product, ProductWithOptionalStockDto>, IProductWithOptionalStockQuery
{
    public string RequestCategory => "QueryAdapter";

    protected override string SelectSql =>
        "SELECT p.Id AS ProductId, p.Name, p.Price, i.StockQuantity " +
        "FROM Products p LEFT JOIN Inventories i ON i.ProductId = p.Id";
    protected override string CountSql =>
        "SELECT COUNT(*) FROM Products p LEFT JOIN Inventories i ON i.ProductId = p.Id";
    protected override string DefaultOrderBy => "p.Name ASC";
    protected override Dictionary<string, string> AllowedSortColumns { get; } =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Name"] = "p.Name",
            ["Price"] = "p.Price",
            ["StockQuantity"] = "i.StockQuantity"
        };

    public ProductWithOptionalStockQueryDapper(IDbConnection connection)
        : base(connection, ProductSpecTranslator.Instance, "p") { }
}
