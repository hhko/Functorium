using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using Microsoft.EntityFrameworkCore;
using static Functorium.Adapters.Errors.AdapterErrorType;
using LayeredArch.Application.Usecases.Orders;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore;

/// <summary>
/// EF Core 기반 공유 Port IProductCatalog 구현
/// DbContext를 직접 사용하여 교차 Aggregate 검증을 제공
/// </summary>
[GeneratePipeline]
public class EfCoreProductCatalog : IProductCatalog
{
    private readonly LayeredArchDbContext _dbContext;

    public string RequestCategory => "Repository";

    public EfCoreProductCatalog(LayeredArchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public virtual FinT<IO, bool> ExistsById(ProductId productId)
    {
        return IO.liftAsync(async () =>
        {
            var exists = await _dbContext.Products.AnyAsync(p => p.Id == productId.ToString());
            return Fin.Succ(exists);
        });
    }

    public virtual FinT<IO, Money> GetPrice(ProductId productId)
    {
        return IO.liftAsync(async () =>
        {
            var model = await _dbContext.Products.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId.ToString());
            if (model is not null)
            {
                return Fin.Succ(Money.CreateFromValidated(model.Price));
            }

            return AdapterError.For<EfCoreProductCatalog>(
                new NotFound(),
                productId.ToString(),
                $"상품 ID '{productId}'의 가격을 조회할 수 없습니다");
        });
    }
}
