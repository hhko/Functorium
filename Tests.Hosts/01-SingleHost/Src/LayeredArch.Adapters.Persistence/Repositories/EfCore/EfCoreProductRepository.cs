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

    protected override DbContext DbContext => _dbContext;
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
    /// Soft Delete 벌크 처리.
    /// 벌크 삭제 Use Case는 Domain Service(ProductBulkOperations)를 통해
    /// GetByIds → BulkDelete → UpdateRange 패턴을 사용합니다.
    /// 이 메서드는 IRepository 인터페이스 계약 유지용입니다.
    /// </summary>
    public override FinT<IO, int> DeleteRange(IReadOnlyList<ProductId> ids)
    {
        return IO.liftAsync(async () =>
        {
            if (ids.Count == 0)
                return Fin.Succ(0);

            var models = await ReadQueryIgnoringFilters()
                .Where(ByIdsPredicate(ids))
                .ToListAsync();

            if (models.Count == 0)
                return Fin.Succ(0);

            foreach (var model in models)
            {
                var product = ToDomain(model);
                product.Delete("system");

                var updatedModel = ToModel(product);
                DbSet.Attach(updatedModel);
                _dbContext.Entry(updatedModel).Property(p => p.DeletedAt).IsModified = true;
                _dbContext.Entry(updatedModel).Property(p => p.DeletedBy).IsModified = true;

                EventCollector.Track(product);
            }

            return Fin.Succ(models.Count);
        });
    }

    // ─── Compiled Query (opt-in 성능 최적화) ──────────
    // EF.CompileAsyncQuery를 사용하면 LINQ → SQL 컴파일을 1회만 수행합니다.
    // 반복 호출 시 ~10-15% 성능 향상을 기대할 수 있습니다.

    private static readonly Func<LayeredArchDbContext, string, Task<ProductModel?>> GetByIdIgnoringFiltersCompiled =
        EF.CompileAsyncQuery((LayeredArchDbContext ctx, string id) =>
            ctx.Products.IgnoreQueryFilters()
                .Include(p => p.ProductTags)
                .FirstOrDefault(m => m.Id == id));

    // ─── Product 고유 메서드 ─────────────────────────

    public virtual FinT<IO, Product> GetByIdIncludingDeleted(ProductId id)
    {
        return IO.liftAsync(async () =>
        {
            var model = await GetByIdIgnoringFiltersCompiled(_dbContext, id.ToString());

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
