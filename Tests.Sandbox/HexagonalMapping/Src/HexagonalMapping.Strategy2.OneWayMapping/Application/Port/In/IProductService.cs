using HexagonalMapping.Strategy2.OneWayMapping.Model;

namespace HexagonalMapping.Strategy2.OneWayMapping.Application.Port.In;

/// <summary>
/// 입력 포트 (Input Port): Use Case 인터페이스입니다.
///
/// One-Way Mapping의 제한사항:
/// 문서 원문: "The interface must expose only data access methods, excluding business logic."
/// (인터페이스는 데이터 접근 메서드만 노출해야 하며, 비즈니스 로직은 제외)
///
/// 결과적으로 Use Case는:
/// - 조회: IProductModel을 반환 (Adapter 모델 직접 반환 가능)
/// - 비즈니스 로직 필요 시: Product.FromModel()로 변환 후 처리
/// </summary>
public interface IProductService
{
    /// <summary>
    /// 새 상품을 생성합니다.
    /// 비즈니스 로직(검증 등)이 필요하므로 Product를 반환합니다.
    /// </summary>
    Task<Product> CreateProductAsync(string name, decimal price, string currency, CancellationToken cancellationToken = default);

    /// <summary>
    /// IProductModel을 반환합니다.
    /// 단순 조회 시에는 변환 없이 Adapter 모델을 직접 반환할 수 있습니다.
    /// </summary>
    Task<IProductModel?> GetProductAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// IProductModel 컬렉션을 반환합니다.
    /// </summary>
    Task<IReadOnlyList<IProductModel>> GetAllProductsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 가격을 수정합니다.
    /// 비즈니스 로직이 필요하므로 내부에서 Product로 변환합니다.
    /// </summary>
    Task<Product?> UpdateProductPriceAsync(Guid id, decimal newPrice, CancellationToken cancellationToken = default);

    Task<bool> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default);
}
