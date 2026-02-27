using System.Linq.Expressions;
using Functorium.Adapters.Errors;
using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using Functorium.Domains.Specifications;
using Functorium.Domains.Specifications.Expressions;
using LayeredArch.Adapters.Persistence.Repositories.EfCore.Mappers;
using LayeredArch.Adapters.Persistence.Repositories.EfCore.Models;
using static Functorium.Adapters.Errors.AdapterErrorType;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore;

/// <summary>
/// EF Core 기반 상품 리포지토리 구현
/// </summary>
[GenerateObservablePort]
public class EfCoreProductRepository
    : EfCoreRepositoryBase<Product, ProductId, ProductModel>, IProductRepository
{
    private static readonly PropertyMap<Product, ProductModel> _propertyMap =
        new PropertyMap<Product, ProductModel>()
            .Map(p => (decimal)p.Price, m => m.Price)
            .Map(p => (string)p.Name, m => m.Name)
            .Map(p => p.Id.ToString(), m => m.Id);

    private readonly LayeredArchDbContext _dbContext;

    public override string RequestCategory => "Repository";

    public EfCoreProductRepository(LayeredArchDbContext dbContext, IDomainEventCollector eventCollector)
        : base(eventCollector)
        => _dbContext = dbContext;

    // ─── 필수 선언 ───────────────────────────────────

    protected override DbSet<ProductModel> DbSet => _dbContext.Products;

    protected override IQueryable<ProductModel> ApplyIncludes(IQueryable<ProductModel> query)
        => query.Include(p => p.ProductTags);

    protected override Product ToDomain(ProductModel model) => model.ToDomain();
    protected override ProductModel ToModel(Product p) => p.ToModel();

    protected override Expression<Func<ProductModel, bool>> ByIdPredicate(ProductId id)
    {
        var s = id.ToString();
        return m => m.Id == s;
    }

    protected override Expression<Func<ProductModel, bool>> ByIdsPredicate(
        IReadOnlyList<ProductId> ids)
    {
        var ss = ids.Select(id => id.ToString()).ToList();
        return m => ss.Contains(m.Id);
    }

    // ─── Soft Delete 오버라이드 ──────────────────────

    public override FinT<IO, Unit> Delete(ProductId id)
    {
        return IO.liftAsync(async () =>
        {
            var model = await ApplyIncludes(DbSet.IgnoreQueryFilters())
                .FirstOrDefaultAsync(ByIdPredicate(id));

            if (model is null)
            {
                return NotFoundError(id);
            }

            var product = ToDomain(model);
            product.Delete("system");
            DbSet.Update(ToModel(product));
            EventCollector.Track(product);
            return Fin.Succ(unit);
        });
    }

    public override FinT<IO, Unit> DeleteRange(IReadOnlyList<ProductId> ids)
    {
        return IO.liftAsync(async () =>
        {
            await DbSet
                .Where(ByIdsPredicate(ids))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.DeletedAt, DateTime.UtcNow)
                    .SetProperty(p => p.DeletedBy, "system"));
            return Fin.Succ(unit);
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
    {
        return IO.liftAsync(async () =>
        {
            bool exists = await BuildQuery(spec).AnyAsync();
            return Fin.Succ(exists);
        });
    }

    private IQueryable<ProductModel> BuildQuery(Specification<Product> spec)
    {
        var expression = SpecificationExpressionResolver.TryResolve(spec);
        if (expression is not null)
        {
            var modelExpression = _propertyMap.Translate(expression);
            return DbSet.Where(modelExpression);
        }

        throw new NotSupportedException(
            $"Specification '{spec.GetType().Name}'에 대한 Expression이 정의되지 않았습니다. " +
            $"ExpressionSpecification<T>을 상속하고 ToExpression()을 구현하세요.");
    }
}
