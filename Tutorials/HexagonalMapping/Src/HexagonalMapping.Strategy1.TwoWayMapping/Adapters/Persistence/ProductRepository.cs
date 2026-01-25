using HexagonalMapping.Domain.Entities;
using HexagonalMapping.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace HexagonalMapping.Strategy1.TwoWayMapping.Adapters.Persistence;

/// <summary>
/// Repository 구현: Two-Way Mapping을 사용합니다.
/// 모든 경계에서 매퍼를 통해 변환이 일어납니다.
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly ProductDbContext _context;

    public ProductRepository(ProductDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default)
    {
        ProductEntity? entity = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id.Value, cancellationToken);

        // Adapter → Domain 매핑
        return entity is null ? null : ProductMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        List<ProductEntity> entities = await _context.Products
            .ToListAsync(cancellationToken);

        // Adapter → Domain 매핑 (컬렉션)
        return entities.Select(ProductMapper.ToDomain).ToList();
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        // Domain → Adapter 매핑
        ProductEntity entity = ProductMapper.ToEntity(product);
        _context.Products.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        ProductEntity? entity = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == product.Id.Value, cancellationToken);

        if (entity is null)
            throw new InvalidOperationException($"Product {product.Id} not found");

        // Domain → Adapter 매핑 (업데이트)
        ProductMapper.UpdateEntity(entity, product);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ProductId id, CancellationToken cancellationToken = default)
    {
        ProductEntity? entity = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id.Value, cancellationToken);

        if (entity is not null)
        {
            _context.Products.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
