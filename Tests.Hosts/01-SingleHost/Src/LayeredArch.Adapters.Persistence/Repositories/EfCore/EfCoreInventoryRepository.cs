using System.Linq.Expressions;
using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using Functorium.Domains.Specifications;
using Functorium.Domains.Specifications.Expressions;
using LayeredArch.Adapters.Persistence.Repositories.EfCore.Mappers;
using LayeredArch.Adapters.Persistence.Repositories.EfCore.Models;
using LayeredArch.Domain.AggregateRoots.Inventories;
using static Functorium.Adapters.Errors.AdapterErrorType;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore;

/// <summary>
/// EF Core 기반 재고 리포지토리 구현
/// </summary>
[GenerateObservablePort]
public class EfCoreInventoryRepository
    : EfCoreRepositoryBase<Inventory, InventoryId, InventoryModel>, IInventoryRepository
{
    private static readonly PropertyMap<Inventory, InventoryModel> _propertyMap =
        new PropertyMap<Inventory, InventoryModel>()
            .Map(i => (int)i.StockQuantity, m => m.StockQuantity)
            .Map(i => i.ProductId.ToString(), m => m.ProductId)
            .Map(i => i.Id.ToString(), m => m.Id);

    private readonly LayeredArchDbContext _dbContext;

    public EfCoreInventoryRepository(LayeredArchDbContext dbContext, IDomainEventCollector eventCollector)
        : base(eventCollector)
        => _dbContext = dbContext;

    // ─── 필수 선언 ───────────────────────────────────

    protected override DbSet<InventoryModel> DbSet => _dbContext.Inventories;

    protected override Inventory ToDomain(InventoryModel model) => model.ToDomain();
    protected override InventoryModel ToModel(Inventory inventory) => inventory.ToModel();

    protected override Expression<Func<InventoryModel, bool>> ByIdPredicate(InventoryId id)
    {
        var s = id.ToString();
        return m => m.Id == s;
    }

    protected override Expression<Func<InventoryModel, bool>> ByIdsPredicate(
        IReadOnlyList<InventoryId> ids)
    {
        var ss = ids.Select(id => id.ToString()).ToList();
        return m => ss.Contains(m.Id);
    }

    // ─── Inventory 고유 메서드 ───────────────────────

    public virtual FinT<IO, Inventory> GetByProductId(ProductId productId)
    {
        return IO.liftAsync(async () =>
        {
            var s = productId.ToString();
            var model = await ReadQuery()
                .FirstOrDefaultAsync(i => i.ProductId == s);

            if (model is not null)
            {
                return Fin.Succ(ToDomain(model));
            }

            return Functorium.Adapters.Errors.AdapterError.For(GetType(),
                new NotFound(),
                productId.ToString(),
                $"상품 ID '{productId}'에 대한 재고를 찾을 수 없습니다");
        });
    }

    public virtual FinT<IO, bool> Exists(Specification<Inventory> spec)
    {
        return IO.liftAsync(async () =>
        {
            bool exists = await BuildQuery(spec).AnyAsync();
            return Fin.Succ(exists);
        });
    }

    private IQueryable<InventoryModel> BuildQuery(Specification<Inventory> spec)
    {
        var expression = SpecificationExpressionResolver.TryResolve(spec);
        if (expression is not null)
        {
            var modelExpression = _propertyMap.Translate(expression);
            return DbSet.AsNoTracking().Where(modelExpression);
        }

        throw new NotSupportedException(
            $"Specification '{spec.GetType().Name}'에 대한 Expression이 정의되지 않았습니다. " +
            $"ExpressionSpecification<T>을 상속하고 ToExpression()을 구현하세요.");
    }
}
