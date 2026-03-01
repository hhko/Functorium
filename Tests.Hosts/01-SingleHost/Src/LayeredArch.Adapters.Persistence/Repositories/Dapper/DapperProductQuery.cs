using System.Data;
using Dapper;
using Functorium.Adapters.SourceGenerators;
using Functorium.Adapters.Repositories;
using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Products.Ports;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.AggregateRoots.Products.Specifications;

namespace LayeredArch.Adapters.Persistence.Repositories.Dapper;

[GenerateObservablePort]
public class DapperProductQuery
    : DapperQueryBase<Product, ProductSummaryDto>, IProductQuery
{
    public string RequestCategory => "QueryAdapter";

    protected override string SelectSql => "SELECT Id AS ProductId, Name, Price FROM Products";
    protected override string CountSql => "SELECT COUNT(*) FROM Products";
    protected override string DefaultOrderBy => "Name ASC";
    protected override Dictionary<string, string> AllowedSortColumns { get; } =
        new(StringComparer.OrdinalIgnoreCase) { ["Name"] = "Name", ["Price"] = "Price" };

    public DapperProductQuery(IDbConnection connection) : base(connection) { }

    protected override (string, DynamicParameters) BuildWhereClause(Specification<Product> spec)
        => spec switch
        {
            { IsAll: true } => ("WHERE DeletedAt IS NULL", new DynamicParameters()),
            ProductPriceRangeSpec s => (
                "WHERE DeletedAt IS NULL AND Price >= @MinPrice AND Price <= @MaxPrice",
                Params(("MinPrice", (decimal)s.MinPrice), ("MaxPrice", (decimal)s.MaxPrice))),
            _ => throw new NotSupportedException(
                $"Specification '{spec.GetType().Name}'은 Dapper QueryAdapter에서 지원되지 않습니다.")
        };
}
