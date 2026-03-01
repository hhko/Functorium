using System.Data;
using Dapper;
using Functorium.Adapters.SourceGenerators;
using Functorium.Adapters.Repositories;
using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Products.Ports;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.AggregateRoots.Products.Specifications;

namespace LayeredArch.Adapters.Persistence.Repositories.Dapper;

/// <summary>
/// Product + Optional Inventory LEFT JOIN 쿼리 어댑터.
/// 재고 없는 상품도 포함하는 LEFT JOIN 패턴입니다.
/// </summary>
[GenerateObservablePort]
public class DapperProductWithOptionalStockQuery
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

    public DapperProductWithOptionalStockQuery(IDbConnection connection) : base(connection) { }

    protected override (string, DynamicParameters) BuildWhereClause(Specification<Product> spec)
        => spec switch
        {
            { IsAll: true } => ("WHERE p.DeletedAt IS NULL", new DynamicParameters()),
            ProductPriceRangeSpec s => (
                "WHERE p.DeletedAt IS NULL AND p.Price >= @MinPrice AND p.Price <= @MaxPrice",
                Params(("MinPrice", (decimal)s.MinPrice), ("MaxPrice", (decimal)s.MaxPrice))),
            _ => throw new NotSupportedException(
                $"Specification '{spec.GetType().Name}'은 Dapper QueryAdapter에서 지원되지 않습니다.")
        };
}
