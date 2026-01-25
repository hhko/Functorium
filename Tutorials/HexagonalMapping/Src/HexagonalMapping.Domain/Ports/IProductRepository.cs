using HexagonalMapping.Domain.Entities;

namespace HexagonalMapping.Domain.Ports;

/// <summary>
/// 출력 포트 (Output Port): 영속성을 위한 추상화입니다.
/// Core에 정의되지만, Adapter에서 구현됩니다.
/// </summary>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
    Task DeleteAsync(ProductId id, CancellationToken cancellationToken = default);
}
