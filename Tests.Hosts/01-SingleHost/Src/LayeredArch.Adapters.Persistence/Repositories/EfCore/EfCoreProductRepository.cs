using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using Functorium.Domains.Specifications;
using Functorium.Domains.Specifications.Expressions;
using LayeredArch.Adapters.Persistence.Repositories.EfCore.Mappers;
using LayeredArch.Adapters.Persistence.Repositories.EfCore.Models;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore;

/// <summary>
/// EF Core 기반 상품 리포지토리 구현
/// </summary>
[GenerateObservablePort]
public class EfCoreProductRepository
    : EfCoreRepositoryBase<Product, ProductId, ProductModel>, IProductRepository
{
    private readonly LayeredArchDbContext _dbContext;

    public EfCoreProductRepository(LayeredArchDbContext dbContext, IDomainEventCollector eventCollector)
        : base(eventCollector,
               q => q.Include(p => p.ProductTags),
               new PropertyMap<Product, ProductModel>()
                   .Map(p => (decimal)p.Price, m => m.Price)
                   .Map(p => (string)p.Name, m => m.Name)
                   .Map(p => p.Id.ToString(), m => m.Id))
        => _dbContext = dbContext;

    // ─── 필수 선언 ───────────────────────────────────

    protected override DbSet<ProductModel> DbSet => _dbContext.Products;

    protected override Product ToDomain(ProductModel model) => model.ToDomain();
    protected override ProductModel ToModel(Product p) => p.ToModel();

    // ─── Soft Delete 오버라이드 ──────────────────────

    public override FinT<IO, int> Delete(ProductId id)
    {
        return IO.liftAsync(async () =>
        {
            var model = await ReadQueryIgnoringFilters()
                .FirstOrDefaultAsync(ByIdPredicate(id));

            if (model is null)
            {
                return NotFoundError(id);
            }

            var product = ToDomain(model);
            product.Delete("system");

            var updatedModel = ToModel(product);
            DbSet.Attach(updatedModel);
            _dbContext.Entry(updatedModel).Property(p => p.DeletedAt).IsModified = true;
            _dbContext.Entry(updatedModel).Property(p => p.DeletedBy).IsModified = true;

            EventCollector.Track(product);
            return Fin.Succ(1);
        });
    }

    /// <summary>
    /// Soft Delete 벌크 처리. ExecuteUpdateAsync로 직접 SQL을 실행하여 성능을 최적화합니다.
    /// 도메인 객체를 생성하지 않으므로 도메인 이벤트가 발행되지 않습니다.
    /// 이벤트가 필요한 경우 단건 Delete()를 사용하세요.
    /// </summary>
    public override FinT<IO, int> DeleteRange(IReadOnlyList<ProductId> ids)
    {
        return IO.liftAsync(async () =>
        {
            if (ids.Count == 0)
                return Fin.Succ(0);

            int affected = await DbSet
                .Where(ByIdsPredicate(ids))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.DeletedAt, DateTime.UtcNow)
                    .SetProperty(p => p.DeletedBy, "system"));
            return Fin.Succ(affected);
        });
    }

    // ─── Product 고유 메서드 ─────────────────────────

    public virtual FinT<IO, Product> GetByIdIncludingDeleted(ProductId id)
    {
        return IO.liftAsync(async () =>
        {
            var model = await ReadQueryIgnoringFilters()
                .FirstOrDefaultAsync(ByIdPredicate(id));

            if (model is not null)
            {
                return Fin.Succ(ToDomain(model));
            }

            return NotFoundError(id);
        });
    }

    public virtual FinT<IO, bool> Exists(Specification<Product> spec)
        => ExistsBySpec(spec);
}
