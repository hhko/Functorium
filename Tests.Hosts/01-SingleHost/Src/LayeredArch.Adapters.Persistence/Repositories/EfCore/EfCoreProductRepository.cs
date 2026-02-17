using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using Functorium.Domains.Specifications;
using Functorium.Domains.Specifications.Expressions;
using LayeredArch.Adapters.Persistence.Repositories.EfCore.Mappers;
using LayeredArch.Adapters.Persistence.Repositories.EfCore.Models;
using Microsoft.EntityFrameworkCore;
using static Functorium.Adapters.Errors.AdapterErrorType;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore;

/// <summary>
/// EF Core 기반 상품 리포지토리 구현
/// </summary>
[GeneratePipeline]
public class EfCoreProductRepository : IProductRepository
{
    private static readonly PropertyMap<Product, ProductModel> _propertyMap =
        new PropertyMap<Product, ProductModel>()
            .Map(p => (decimal)p.Price, m => m.Price)
            .Map(p => (string)p.Name, m => m.Name)
            .Map(p => p.Id.ToString(), m => m.Id);

    private readonly LayeredArchDbContext _dbContext;
    private readonly IDomainEventCollector _eventCollector;

    public string RequestCategory => "Repository";

    public EfCoreProductRepository(LayeredArchDbContext dbContext, IDomainEventCollector eventCollector)
    {
        _dbContext = dbContext;
        _eventCollector = eventCollector;
    }

    public virtual FinT<IO, Product> Create(Product product)
    {
        return IO.liftAsync(async () =>
        {
            _dbContext.Products.Add(product.ToModel());
            _eventCollector.Track(product);
            return Fin.Succ(product);
        });
    }

    public virtual FinT<IO, Product> GetById(ProductId id)
    {
        return IO.liftAsync(async () =>
        {
            var model = await _dbContext.Products
                .AsNoTracking()
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == id.ToString());

            if (model is not null)
            {
                return Fin.Succ(model.ToDomain());
            }

            return AdapterError.For<EfCoreProductRepository>(
                new NotFound(),
                id.ToString(),
                $"상품 ID '{id}'을(를) 찾을 수 없습니다");
        });
    }

    public virtual FinT<IO, Option<Product>> GetByName(ProductName name)
    {
        return IO.liftAsync(async () =>
        {
            var model = await _dbContext.Products
                .AsNoTracking()
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Name == (string)name);

            return Fin.Succ(Optional(model is not null ? model.ToDomain() : null));
        });
    }

    public virtual FinT<IO, Seq<Product>> GetAll()
    {
        return IO.liftAsync(async () =>
        {
            var models = await _dbContext.Products
                .AsNoTracking()
                .Include(p => p.Tags)
                .ToListAsync();

            return Fin.Succ(toSeq(models.Select(m => m.ToDomain())));
        });
    }

    public virtual FinT<IO, Product> Update(Product product)
    {
        return IO.lift(() =>
        {
            _dbContext.Products.Update(product.ToModel());
            _eventCollector.Track(product);
            return Fin.Succ(product);
        });
    }

    public virtual FinT<IO, Unit> Delete(ProductId id)
    {
        return IO.liftAsync(async () =>
        {
            var model = await _dbContext.Products.FindAsync(id.ToString());
            if (model is null)
            {
                return AdapterError.For<EfCoreProductRepository>(
                    new NotFound(),
                    id.ToString(),
                    $"상품 ID '{id}'을(를) 찾을 수 없습니다");
            }

            _dbContext.Products.Remove(model);
            return Fin.Succ(unit);
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

    public virtual FinT<IO, Seq<Product>> FindAll(Specification<Product> spec)
    {
        return IO.liftAsync(async () =>
        {
            var models = await BuildQuery(spec)
                .AsNoTracking().Include(p => p.Tags).ToListAsync();
            return Fin.Succ(toSeq(models.Select(m => m.ToDomain())));
        });
    }

    private IQueryable<ProductModel> BuildQuery(Specification<Product> spec)
    {
        var expression = SpecificationExpressionResolver.TryResolve(spec);
        if (expression is not null)
        {
            var modelExpression = _propertyMap.Translate(expression);
            return _dbContext.Products.Where(modelExpression);
        }

        throw new NotSupportedException(
            $"Specification '{spec.GetType().Name}'에 대한 Expression이 정의되지 않았습니다. " +
            $"ExpressionSpecification<T>을 상속하고 ToExpression()을 구현하세요.");
    }
}
