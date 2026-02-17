using System.Data;
using Dapper;
using Functorium.Adapters.SourceGenerators;
using Functorium.Adapters.Repositories;
using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Products.Dtos;
using LayeredArch.Application.Usecases.Products.Ports;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.AggregateRoots.Products.Specifications;

namespace LayeredArch.Adapters.Persistence.Repositories.Dapper;

/// <summary>
/// Product + Inventory JOIN 쿼리 어댑터.
/// 베이스 클래스의 JOIN 지원을 검증하는 예제입니다.
/// </summary>
[GeneratePipeline]
public class DapperProductWithStockQueryAdapter
    : DapperQueryAdapterBase<Product, ProductWithStockDto>, IProductWithStockQueryAdapter
{
    public string RequestCategory => "QueryAdapter";

    protected override string SelectSql =>
        "SELECT p.Id AS ProductId, p.Name, p.Price, i.StockQuantity " +
        "FROM Products p INNER JOIN Inventories i ON i.ProductId = p.Id";
    protected override string CountSql =>
        "SELECT COUNT(*) FROM Products p INNER JOIN Inventories i ON i.ProductId = p.Id";
    protected override string DefaultOrderBy => "p.Name ASC";
    protected override Dictionary<string, string> AllowedSortColumns { get; } =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Name"] = "p.Name",
            ["Price"] = "p.Price",
            ["StockQuantity"] = "i.StockQuantity"
        };

    public DapperProductWithStockQueryAdapter(IDbConnection connection) : base(connection) { }

    protected override (string, DynamicParameters) BuildWhereClause(Specification<Product>? spec)
        => spec switch
        {
            null => ("", new DynamicParameters()),
            ProductPriceRangeSpec s => (
                "WHERE p.Price >= @MinPrice AND p.Price <= @MaxPrice",
                Params(("MinPrice", (decimal)s.MinPrice), ("MaxPrice", (decimal)s.MaxPrice))),
            _ => throw new NotSupportedException(
                $"Specification '{spec.GetType().Name}'은 Dapper QueryAdapter에서 지원되지 않습니다.")
        };
}
