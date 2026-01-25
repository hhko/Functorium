using HexagonalMapping.Strategy2.OneWayMapping.Domain;
using Microsoft.EntityFrameworkCore;

namespace HexagonalMapping.Strategy2.OneWayMapping.Adapters.Persistence;

/// <summary>
/// Repository 구현: One-Way Mapping을 사용합니다.
///
/// 핵심 포인트 (문서 원문):
/// "Only one translation direction is needed—from core to adapter."
/// (하나의 변환 방향만 필요 - Core에서 Adapter로)
///
/// "The adapter returns its own model since it implements the core's interface."
/// (Adapter는 Core의 인터페이스를 구현하므로 자신의 모델을 직접 반환)
///
/// 구현:
/// - Domain → Adapter: ProductEntity.FromModel(IProductModel) 변환 필요
/// - Adapter → Domain: 변환 불필요! ProductEntity를 IProductModel로 직접 반환
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly ProductDbContext _context;

    public ProductRepository(ProductDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Adapter → Domain: 변환 불필요!
    /// ProductEntity가 IProductModel을 구현하므로 직접 반환합니다.
    /// </summary>
    public async Task<IProductModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // ProductEntity를 IProductModel로 직접 반환 - 변환 없음!
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <summary>
    /// Adapter → Domain: 변환 불필요!
    /// ProductEntity 컬렉션을 IProductModel 컬렉션으로 직접 반환합니다.
    /// </summary>
    public async Task<IReadOnlyList<IProductModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // ProductEntity 리스트를 IProductModel 리스트로 직접 반환
        List<ProductEntity> entities = await _context.Products.ToListAsync(cancellationToken);
        return entities.Cast<IProductModel>().ToList();
    }

    /// <summary>
    /// Domain → Adapter: 변환 필요 (One-Way의 유일한 변환 방향)
    /// IProductModel을 받아서 ProductEntity로 변환합니다.
    /// </summary>
    public async Task AddAsync(IProductModel product, CancellationToken cancellationToken = default)
    {
        // Domain → Adapter: 이것이 One-Way에서 유일한 변환
        ProductEntity entity = ProductEntity.FromModel(product);
        _context.Products.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(IProductModel product, CancellationToken cancellationToken = default)
    {
        ProductEntity? entity = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == product.Id, cancellationToken);

        if (entity is null)
            throw new InvalidOperationException($"Product {product.Id} not found");

        // 인터페이스를 통해 데이터 복사
        entity.Name = product.Name;
        entity.Price = product.Price;
        entity.Currency = product.Currency;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ProductEntity? entity = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (entity is not null)
        {
            _context.Products.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
