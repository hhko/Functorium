using HexagonalMapping.Strategy4.WeakenedBoundary.Domain;

namespace HexagonalMapping.Strategy4.WeakenedBoundary.Application;

/// <summary>
/// Use Case 구현: 애플리케이션 비즈니스 로직을 담당합니다.
///
/// ⚠️ Weakened Boundaries Anti-pattern의 문제점:
/// 이 Use Case는 정상적으로 보이지만, Domain 엔티티(Product)가
/// 이미 기술 어노테이션([Table], [Column] 등)에 오염되어 있습니다.
///
/// 결과적으로:
/// - Domain이 Persistence 기술에 의존
/// - 테스트 작성이 어려워짐
/// - Domain 모델이 ORM의 제약을 받음
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
    /// </summary>
    public async Task<Product> CreateProductAsync(
        string name,
        decimal price,
        string currency,
        CancellationToken cancellationToken = default)
    {
        // Domain 엔티티 생성 (비즈니스 규칙 적용)
        Product product = Product.Create(name, price, currency);

        // 출력 포트를 통해 영속성 저장
        // Weakened Boundaries: 별도 매핑 없이 Domain 엔티티를 직접 저장
        await _repository.AddAsync(product, cancellationToken);

        return product;
    }

    /// <summary>
    /// Use Case: 상품 조회
    /// </summary>
    public async Task<Product?> GetProductAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdAsync(id, cancellationToken);
    }

    /// <summary>
    /// Use Case: 전체 상품 목록 조회
    /// </summary>
    public async Task<IReadOnlyList<Product>> GetAllProductsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetAllAsync(cancellationToken);
    }

    /// <summary>
    /// Use Case: 상품 가격 수정
    /// </summary>
    public async Task<Product?> UpdateProductPriceAsync(
        Guid id,
        decimal newPrice,
        CancellationToken cancellationToken = default)
    {
        Product? product = await _repository.GetByIdAsync(id, cancellationToken);
        if (product is null)
            return null;

        // Domain 엔티티의 비즈니스 메서드 호출
        product.UpdatePrice(newPrice);

        // 출력 포트를 통해 영속성 갱신
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
        Product? product = await _repository.GetByIdAsync(id, cancellationToken);
        if (product is null)
            return false;

        await _repository.DeleteAsync(id, cancellationToken);
        return true;
    }
}
