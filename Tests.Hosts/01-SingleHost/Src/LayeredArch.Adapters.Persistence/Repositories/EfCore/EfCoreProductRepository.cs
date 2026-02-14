using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
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

    public string RequestCategory => "Repository";

    public EfCoreProductRepository(LayeredArchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public virtual FinT<IO, Product> Create(Product product)
    {
        return IO.liftAsync(async () =>
        {
            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();
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
                .FirstOrDefaultAsync(p => ((string)(object)p.Name) == (string)name);

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
        return IO.liftAsync(async () =>
        {
            var exists = await _dbContext.Products.AnyAsync(p => p.Id == product.Id);
            if (!exists)
            {
                return AdapterError.For<EfCoreProductRepository>(
                    new NotFound(),
                    product.Id.ToString(),
                    $"상품 ID '{product.Id}'을(를) 찾을 수 없습니다");
            }

            _dbContext.Products.Update(product);
            await _dbContext.SaveChangesAsync();
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
            await _dbContext.SaveChangesAsync();
            return Fin.Succ(unit);
        });
    }

    public virtual FinT<IO, bool> ExistsByName(ProductName name, ProductId? excludeId = null)
    {
        return IO.liftAsync(async () =>
        {
            var nameStr = (string)name;
            bool exists = await _dbContext.Products.AnyAsync(p =>
                ((string)(object)p.Name) == nameStr &&
                (excludeId == null || p.Id != excludeId.Value));

            return Fin.Succ(exists);
        });
    }
}
