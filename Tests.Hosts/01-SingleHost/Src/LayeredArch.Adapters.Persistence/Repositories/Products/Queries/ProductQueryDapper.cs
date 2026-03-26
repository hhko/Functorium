using System.Data;
using Functorium.Adapters.SourceGenerators;
using Functorium.Adapters.Repositories;
using LayeredArch.Application.Usecases.Products.Ports;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Adapters.Persistence.Repositories.Products.Queries;

[GenerateObservablePort]
public class ProductQueryDapper
    : DapperQueryBase<Product, ProductSummaryDto>, IProductQuery
{
    public string RequestCategory => "QueryAdapter";

    protected override string SelectSql => "SELECT Id AS ProductId, Name, Price FROM Products";
    protected override string CountSql => "SELECT COUNT(*) FROM Products";
    protected override string DefaultOrderBy => "Name ASC";
    protected override Dictionary<string, string> AllowedSortColumns { get; } =
        new(StringComparer.OrdinalIgnoreCase) { ["Name"] = "Name", ["Price"] = "Price" };

    public ProductQueryDapper(IDbConnection connection)
        : base(connection, ProductSpecTranslator.Instance) { }
}
