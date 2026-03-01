using LayeredArch.Domain.AggregateRoots.Products;
using Functorium.Adapters.SourceGenerators;
using static LanguageExt.Prelude;
using LayeredArch.Application.Usecases.Orders.Ports;

namespace LayeredArch.Adapters.Persistence.Repositories.InMemory.Products;

/// <summary>
/// 공유 Port IProductCatalog 구현.
/// InMemoryProductRepository의 정적 저장소에서 배치 조회합니다.
/// </summary>
[GenerateObservablePort]
public class InMemoryProductCatalog : IProductCatalog
{
    public string RequestCategory => "Repository";

    public virtual FinT<IO, Seq<(ProductId Id, Money Price)>> GetPricesForProducts(IReadOnlyList<ProductId> productIds)
    {
        return IO.lift(() =>
        {
            var requestedIds = new System.Collections.Generic.HashSet<ProductId>(productIds);
            var results = InMemoryProductRepository.Products.Values
                .Where(p => p.DeletedAt.IsNone && requestedIds.Contains(p.Id))
                .Select(p => (p.Id, p.Price));
            return Fin.Succ(toSeq(results));
        });
    }
}
