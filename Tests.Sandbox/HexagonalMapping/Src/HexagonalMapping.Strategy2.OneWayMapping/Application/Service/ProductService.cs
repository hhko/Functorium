using HexagonalMapping.Strategy2.OneWayMapping.Application.Port.In;
using HexagonalMapping.Strategy2.OneWayMapping.Application.Port.Out;
using HexagonalMapping.Strategy2.OneWayMapping.Model;

namespace HexagonalMapping.Strategy2.OneWayMapping.Application.Service;

/// <summary>
/// Use Case 구현: One-Way Mapping의 제한사항을 보여줍니다.
///
/// 문서 원문:
/// "I don't like this strategy because it is less intuitive and,
/// in my experience, is more overhead."
/// (이 전략은 덜 직관적이고, 오히려 더 많은 오버헤드가 발생)
///
/// One-Way의 "오버헤드":
/// - 조회: IProductModel 반환 (Adapter 모델 직접)
/// - 비즈니스 로직 필요 시: Product.FromModel()로 변환 필요
/// - 인터페이스에 비즈니스 메서드 포함 불가
/// </summary>
public class ProductService : IProductService
{
    private readonly IProductRepository _repository;

    public ProductService(IProductRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Use Case: 새 상품 생성
    /// 비즈니스 로직(검증)이 필요하므로 Domain Product 생성
    /// </summary>
    public async Task<Product> CreateProductAsync(
        string name,
        decimal price,
        string currency,
        CancellationToken cancellationToken = default)
    {
        // Domain 엔티티 생성 (비즈니스 규칙 적용)
        Product product = Product.Create(name, price, currency);

        // One-Way: Product가 IProductModel을 구현하므로 직접 전달
        await _repository.AddAsync(product, cancellationToken);

        return product;
    }

    /// <summary>
    /// Use Case: 상품 조회
    /// One-Way Mapping: IProductModel 직접 반환 (변환 없음)
    /// </summary>
    public async Task<IProductModel?> GetProductAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Repository가 ProductEntity를 IProductModel로 직접 반환
        // 변환 없이 바로 반환!
        return await _repository.GetByIdAsync(id, cancellationToken);
    }

    /// <summary>
    /// Use Case: 전체 상품 목록 조회
    /// One-Way Mapping: IProductModel 컬렉션 직접 반환
    /// </summary>
    public async Task<IReadOnlyList<IProductModel>> GetAllProductsAsync(
        CancellationToken cancellationToken = default)
    {
        // Repository가 ProductEntity 리스트를 IProductModel 리스트로 직접 반환
        return await _repository.GetAllAsync(cancellationToken);
    }

    /// <summary>
    /// Use Case: 상품 가격 수정
    ///
    /// ⚠️ One-Way의 오버헤드가 발생하는 부분!
    /// 비즈니스 로직(UpdatePrice)이 필요하므로 Product로 변환해야 합니다.
    /// </summary>
    public async Task<Product?> UpdateProductPriceAsync(
        Guid id,
        decimal newPrice,
        CancellationToken cancellationToken = default)
    {
        IProductModel? model = await _repository.GetByIdAsync(id, cancellationToken);
        if (model is null)
            return null;

        // ⚠️ One-Way의 오버헤드:
        // 비즈니스 로직이 필요하면 Product로 변환해야 함
        // 이것이 저자가 "덜 직관적이고 오버헤드가 많다"고 말한 부분
        Product product = Product.FromModel(model);

        // Domain 엔티티의 비즈니스 메서드 호출
        product.UpdatePrice(newPrice);

        // One-Way: Product가 IProductModel을 구현하므로 직접 전달
        await _repository.UpdateAsync(product, cancellationToken);

        return product;
    }

    /// <summary>
    /// Use Case: 상품 삭제
    /// </summary>
    public async Task<bool> DeleteProductAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        IProductModel? model = await _repository.GetByIdAsync(id, cancellationToken);
        if (model is null)
            return false;

        await _repository.DeleteAsync(id, cancellationToken);
        return true;
    }
}
