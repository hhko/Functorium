using LanguageExt;

namespace ResultPattern.Demo.Domain;

/// <summary>
/// Product Repository 인터페이스
/// </summary>
public interface IProductRepository
{
    ValueTask<Fin<Product>> CreateAsync(Product product, CancellationToken cancellationToken = default);
    ValueTask<Fin<Product>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    ValueTask<Fin<IReadOnlyList<Product>>> GetAllAsync(CancellationToken cancellationToken = default);
}
