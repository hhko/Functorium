using System.Data;
using Functorium.Adapters.SourceGenerators;
using Functorium.Adapters.Repositories;
using LayeredArch.Application.Usecases.Inventories.Ports;
using LayeredArch.Domain.AggregateRoots.Inventories;
using LayeredArch.Domain.AggregateRoots.Inventories.Specifications;

namespace LayeredArch.Adapters.Persistence.Repositories.Dapper;

[GenerateObservablePort]
public class DapperInventoryQuery
    : DapperQueryBase<Inventory, InventorySummaryDto>, IInventoryQuery
{
    private static readonly DapperSpecTranslator<Inventory> Translator = new DapperSpecTranslator<Inventory>()
        .When<InventoryLowStockSpec>((spec, _) => (
            "WHERE StockQuantity < @Threshold",
            DapperSpecTranslator<Inventory>.Params(("Threshold", (int)spec.Threshold))));

    public string RequestCategory => "QueryAdapter";

    protected override string SelectSql => "SELECT Id AS InventoryId, ProductId, StockQuantity FROM Inventories";
    protected override string CountSql => "SELECT COUNT(*) FROM Inventories";
    protected override string DefaultOrderBy => "Id ASC";
    protected override Dictionary<string, string> AllowedSortColumns { get; } =
        new(StringComparer.OrdinalIgnoreCase) { ["StockQuantity"] = "StockQuantity", ["ProductId"] = "ProductId" };

    public DapperInventoryQuery(IDbConnection connection) : base(connection, Translator) { }
}
