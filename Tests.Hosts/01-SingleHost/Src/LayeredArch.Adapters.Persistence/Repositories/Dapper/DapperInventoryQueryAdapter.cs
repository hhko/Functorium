using System.Data;
using Dapper;
using Functorium.Adapters.SourceGenerators;
using Functorium.Adapters.Repositories;
using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Inventories.Ports;
using LayeredArch.Domain.AggregateRoots.Inventories;
using LayeredArch.Domain.AggregateRoots.Inventories.Specifications;

namespace LayeredArch.Adapters.Persistence.Repositories.Dapper;

[GeneratePortObservable]
public class DapperInventoryQueryAdapter
    : DapperQueryAdapterBase<Inventory, InventorySummaryDto>, IInventoryQuery
{
    public string RequestCategory => "QueryAdapter";

    protected override string SelectSql => "SELECT Id AS InventoryId, ProductId, StockQuantity FROM Inventories";
    protected override string CountSql => "SELECT COUNT(*) FROM Inventories";
    protected override string DefaultOrderBy => "Id ASC";
    protected override Dictionary<string, string> AllowedSortColumns { get; } =
        new(StringComparer.OrdinalIgnoreCase) { ["StockQuantity"] = "StockQuantity", ["ProductId"] = "ProductId" };

    public DapperInventoryQueryAdapter(IDbConnection connection) : base(connection) { }

    protected override (string, DynamicParameters) BuildWhereClause(Specification<Inventory> spec)
        => spec switch
        {
            { IsAll: true } => ("", new DynamicParameters()),
            InventoryLowStockSpec s => (
                "WHERE StockQuantity < @Threshold",
                Params(("Threshold", (int)s.Threshold))),
            _ => throw new NotSupportedException(
                $"Specification '{spec.GetType().Name}'은 Dapper QueryAdapter에서 지원되지 않습니다.")
        };
}
