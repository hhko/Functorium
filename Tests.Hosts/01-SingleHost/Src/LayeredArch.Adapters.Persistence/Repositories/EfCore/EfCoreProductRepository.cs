using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using Microsoft.EntityFrameworkCore;
using static Functorium.Adapters.Errors.AdapterErrorType;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore;

/// <summary>
/// EF Core 기반 상품 리포지토리 구현
/// </summary>
[GeneratePipeline]
public class EfCoreProductRepository : IProductRepository
{
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
            _dbContext.Products.Add(product);
            _eventCollector.Track(product);
            return Fin.Succ(product);
        });
    }

    public virtual FinT<IO, Product> GetById(ProductId id)
    {
        return IO.liftAsync(async () =>
        {
            var product = await _dbContext.Products
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product is not null)
            {
                return Fin.Succ(product);
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
            var product = await _dbContext.Products
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => EF.Property<string>(p, nameof(Product.Name)) == (string)name);

            return Fin.Succ(Optional(product));
        });
    }

    public virtual FinT<IO, Seq<Product>> GetAll()
    {
        return IO.liftAsync(async () =>
        {
            var products = await _dbContext.Products
                .Include(p => p.Tags)
                .ToListAsync();

            return Fin.Succ(toSeq(products));
        });
    }

    public virtual FinT<IO, Product> Update(Product product)
    {
        return IO.lift(() =>
        {
            _dbContext.Products.Update(product);
            _eventCollector.Track(product);
            return Fin.Succ(product);
        });
    }

    public virtual FinT<IO, Unit> Delete(ProductId id)
    {
        return IO.liftAsync(async () =>
        {
            var product = await _dbContext.Products.FindAsync(id);
            if (product is null)
            {
                return AdapterError.For<EfCoreProductRepository>(
                    new NotFound(),
                    id.ToString(),
                    $"상품 ID '{id}'을(를) 찾을 수 없습니다");
            }

            _dbContext.Products.Remove(product);
            return Fin.Succ(unit);
        });
    }

    public virtual FinT<IO, bool> ExistsByName(ProductName name, ProductId? excludeId = null)
    {
        return IO.liftAsync(async () =>
        {
            var nameStr = (string)name;
            bool exists = await _dbContext.Products.AnyAsync(p =>
                EF.Property<string>(p, nameof(Product.Name)) == nameStr &&
                (excludeId == null || p.Id != excludeId.Value));

            return Fin.Succ(exists);
        });
    }
}
