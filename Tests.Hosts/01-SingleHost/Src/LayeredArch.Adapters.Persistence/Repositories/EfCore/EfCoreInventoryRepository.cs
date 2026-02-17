using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using Functorium.Domains.Specifications;
using Functorium.Domains.Specifications.Expressions;
using LayeredArch.Adapters.Persistence.Repositories.EfCore.Mappers;
using LayeredArch.Adapters.Persistence.Repositories.EfCore.Models;
using LayeredArch.Domain.AggregateRoots.Inventories;
using LayeredArch.Domain.AggregateRoots.Products;
using Microsoft.EntityFrameworkCore;
using static Functorium.Adapters.Errors.AdapterErrorType;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore;

/// <summary>
/// EF Core 기반 재고 리포지토리 구현
/// </summary>
[GeneratePipeline]
public class EfCoreInventoryRepository : IInventoryRepository
{
    private static readonly PropertyMap<Inventory, InventoryModel> _propertyMap =
        new PropertyMap<Inventory, InventoryModel>()
            .Map(i => (int)i.StockQuantity, m => m.StockQuantity)
            .Map(i => i.ProductId.ToString(), m => m.ProductId)
            .Map(i => i.Id.ToString(), m => m.Id);

    private readonly LayeredArchDbContext _dbContext;
    private readonly IDomainEventCollector _eventCollector;

    public string RequestCategory => "Repository";

    public EfCoreInventoryRepository(LayeredArchDbContext dbContext, IDomainEventCollector eventCollector)
    {
        _dbContext = dbContext;
        _eventCollector = eventCollector;
    }

    public virtual FinT<IO, Inventory> Create(Inventory inventory)
    {
        return IO.liftAsync(async () =>
        {
            _dbContext.Inventories.Add(inventory.ToModel());
            _eventCollector.Track(inventory);
            return Fin.Succ(inventory);
        });
    }

    public virtual FinT<IO, Inventory> GetById(InventoryId id)
    {
        return IO.liftAsync(async () =>
        {
            var model = await _dbContext.Inventories
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id.ToString());

            if (model is not null)
            {
                return Fin.Succ(model.ToDomain());
            }

            return AdapterError.For<EfCoreInventoryRepository>(
                new NotFound(),
                id.ToString(),
                $"재고 ID '{id}'을(를) 찾을 수 없습니다");
        });
    }

    public virtual FinT<IO, Inventory> GetByProductId(ProductId productId)
    {
        return IO.liftAsync(async () =>
        {
            var model = await _dbContext.Inventories
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.ProductId == productId.ToString());

            if (model is not null)
            {
                return Fin.Succ(model.ToDomain());
            }

            return AdapterError.For<EfCoreInventoryRepository>(
                new NotFound(),
                productId.ToString(),
                $"상품 ID '{productId}'에 대한 재고를 찾을 수 없습니다");
        });
    }

    public virtual FinT<IO, Inventory> Update(Inventory inventory)
    {
        return IO.lift(() =>
        {
            _dbContext.Inventories.Update(inventory.ToModel());
            _eventCollector.Track(inventory);
            return Fin.Succ(inventory);
        });
    }

    public virtual FinT<IO, Unit> Delete(InventoryId id)
    {
        return IO.liftAsync(async () =>
        {
            var model = await _dbContext.Inventories.FindAsync(id.ToString());
            if (model is null)
            {
                return AdapterError.For<EfCoreInventoryRepository>(
                    new NotFound(),
                    id.ToString(),
                    $"재고 ID '{id}'을(를) 찾을 수 없습니다");
            }

            _dbContext.Inventories.Remove(model);
            return Fin.Succ(unit);
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

    public virtual FinT<IO, Seq<Inventory>> FindAll(Specification<Inventory> spec)
    {
        return IO.liftAsync(async () =>
        {
            var models = await BuildQuery(spec)
                .AsNoTracking().ToListAsync();
            return Fin.Succ(toSeq(models.Select(m => m.ToDomain())));
        });
    }

    private IQueryable<InventoryModel> BuildQuery(Specification<Inventory> spec)
    {
        var expression = SpecificationExpressionResolver.TryResolve(spec);
        if (expression is not null)
        {
            var modelExpression = _propertyMap.Translate(expression);
            return _dbContext.Inventories.Where(modelExpression);
        }

        throw new NotSupportedException(
            $"Specification '{spec.GetType().Name}'에 대한 Expression이 정의되지 않았습니다. " +
            $"ExpressionSpecification<T>을 상속하고 ToExpression()을 구현하세요.");
    }
}
