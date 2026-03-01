using Functorium.Adapters.SourceGenerators;
using Microsoft.EntityFrameworkCore;
using LayeredArch.Application.Usecases.Orders.Ports;
using static LanguageExt.Prelude;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore;

/// <summary>
/// EF Core 기반 공유 Port IProductCatalog 구현.
/// 단일 WHERE IN 쿼리로 배치 조회하여 N+1 라운드트립을 방지합니다.
/// </summary>
[GenerateObservablePort]
public class EfCoreProductCatalog : IProductCatalog
{
    private readonly LayeredArchDbContext _dbContext;

    public string RequestCategory => "Repository";

    public EfCoreProductCatalog(LayeredArchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public virtual FinT<IO, Seq<(ProductId Id, Money Price)>> GetPricesForProducts(IReadOnlyList<ProductId> productIds)
    {
        return IO.liftAsync(async () =>
        {
            var idStrings = productIds.Select(id => id.ToString()).ToList();
            var models = await _dbContext.Products.AsNoTracking()
                .Where(p => idStrings.Contains(p.Id))
                .Select(p => new { p.Id, p.Price })
                .ToListAsync();

            var results = models.Select(m =>
                (ProductId.Create(m.Id), Money.CreateFromValidated(m.Price)));
            return Fin.Succ(toSeq(results));
        });
    }
}
