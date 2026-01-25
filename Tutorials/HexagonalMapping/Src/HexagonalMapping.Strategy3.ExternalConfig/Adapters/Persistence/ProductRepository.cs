using HexagonalMapping.Domain.Entities;
using HexagonalMapping.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace HexagonalMapping.Strategy3.ExternalConfig.Adapters.Persistence;

/// <summary>
/// Repository 구현: External Configuration 방식입니다.
/// Domain 엔티티를 직접 사용하므로 매핑이 필요 없습니다.
///
/// 장점: 코드 중복 없음
/// 단점:
/// - Domain 모델이 ORM의 제약을 받음
/// - 매핑 설정이 코드와 분리되어 혼란 야기
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
        // Domain 엔티티 직접 사용 - 매핑 불필요
        return await _context.Products
            .FirstOrDefaultAsync(p => EF.Property<ProductId>(p, "Id") == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // Domain 엔티티 직접 사용 - 매핑 불필요
        return await _context.Products.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        // Domain 엔티티 직접 사용 - 매핑 불필요
        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ProductId id, CancellationToken cancellationToken = default)
    {
        var product = await GetByIdAsync(id, cancellationToken);
        if (product is not null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
