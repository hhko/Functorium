using LayeredArch.Domain.AggregateRoots.Products;
using Functorium.Adapters.SourceGenerators;
using static LanguageExt.Prelude;
using LayeredArch.Application.Usecases.Orders.Ports;

namespace LayeredArch.Adapters.Persistence.Repositories.Products.Repositories;

/// <summary>
/// 공유 Port IProductCatalog 구현.
/// ProductRepositoryInMemory의 정적 저장소에서 배치 조회합니다.
/// </summary>
[GenerateObservablePort]
public class ProductCatalogInMemory : IProductCatalog
{
    public string RequestCategory => "Repository";

    public virtual FinT<IO, Seq<(ProductId Id, Money Price)>> GetPricesForProducts(IReadOnlyList<ProductId> productIds)
    {
        return IO.lift(() =>
        {
            var requestedIds = new System.Collections.Generic.HashSet<ProductId>(productIds);
            var results = ProductRepositoryInMemory.Products.Values
                .Where(p => p.DeletedAt.IsNone && requestedIds.Contains(p.Id))
                .Select(p => (p.Id, p.Price));
            return Fin.Succ(toSeq(results));
        });
    }
}
