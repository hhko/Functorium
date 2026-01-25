namespace HexagonalMapping.Strategy2.OneWayMapping.Domain;

/// <summary>
/// Repository 인터페이스: One-Way Mapping의 핵심입니다.
///
/// 문서 원문: "The adapter returns its own model since it implements the core's interface"
/// (Adapter는 Core의 인터페이스를 구현하므로 자신의 모델을 직접 반환합니다)
///
/// 반환 타입이 IProductModel입니다:
/// - 조회 시 Adapter는 ProductEntity를 직접 반환 (IProductModel로)
/// - Adapter → Domain 방향의 변환이 불필요
/// - 저장 시 Domain은 Product를 직접 전달 (IProductModel로)
/// - Domain → Adapter 방향의 변환만 필요
///
/// 제한사항:
/// - 비즈니스 로직이 필요하면 Product.FromModel()로 변환 필요
/// - 이것이 저자가 "더 많은 오버헤드"라고 말한 부분
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// IProductModel을 반환합니다.
    /// Adapter는 ProductEntity를 직접 반환할 수 있습니다.
    /// </summary>
    Task<IProductModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// IProductModel 컬렉션을 반환합니다.
    /// </summary>
    Task<IReadOnlyList<IProductModel>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// IProductModel을 받습니다.
    /// Domain Product도 IProductModel을 구현하므로 직접 전달 가능합니다.
    /// </summary>
    Task AddAsync(IProductModel product, CancellationToken cancellationToken = default);
    Task UpdateAsync(IProductModel product, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
