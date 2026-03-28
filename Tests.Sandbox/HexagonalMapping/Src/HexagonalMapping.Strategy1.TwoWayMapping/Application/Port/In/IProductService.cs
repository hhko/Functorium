using HexagonalMapping.Domain.Model;

namespace HexagonalMapping.Strategy1.TwoWayMapping.Application.Port.In;

/// <summary>
/// 입력 포트 (Input Port): Use Case 인터페이스입니다.
/// 애플리케이션의 진입점을 정의합니다.
/// Adapter(Controller 등)에서 이 인터페이스를 통해 Use Case를 호출합니다.
/// </summary>
public interface IProductService
{
    Task<Product> CreateProductAsync(string name, decimal price, string currency, CancellationToken cancellationToken = default);
    Task<Product?> GetProductAsync(ProductId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken cancellationToken = default);
    Task<Product?> UpdateProductPriceAsync(ProductId id, decimal newPrice, string currency, CancellationToken cancellationToken = default);
    Task<bool> DeleteProductAsync(ProductId id, CancellationToken cancellationToken = default);
}
