namespace HexagonalMapping.Strategy4.WeakenedBoundary.Domain;

/// <summary>
/// 입력 포트 (Input Port): Use Case 인터페이스입니다.
/// 애플리케이션의 진입점을 정의합니다.
///
/// ⚠️ Weakened Boundaries에서도 Use Case 레이어는 존재합니다.
/// 하지만 Domain 엔티티 자체에 기술 어노테이션이 포함되어 있어
/// 아키텍처의 순수성이 이미 훼손된 상태입니다.
/// </summary>
public interface IProductService
{
    Task<Product> CreateProductAsync(string name, decimal price, string currency, CancellationToken cancellationToken = default);
    Task<Product?> GetProductAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken cancellationToken = default);
    Task<Product?> UpdateProductPriceAsync(Guid id, decimal newPrice, CancellationToken cancellationToken = default);
    Task<bool> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default);
}
