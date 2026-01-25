using HexagonalMapping.Domain.Entities;
using HexagonalMapping.Domain.Ports;

namespace HexagonalMapping.Strategy3.ExternalConfig.Application;

/// <summary>
/// Use Case 구현: 애플리케이션 비즈니스 로직을 담당합니다.
///
/// External Configuration 전략 특징:
/// - Domain 엔티티를 직접 사용 (별도 Adapter 모델 없음)
/// - ORM 매핑은 XML 또는 Fluent API로 외부 설정
/// - 코드 중복 없음
///
/// ⚠️ 제한사항:
/// - Domain 모델이 ORM의 제약을 받음 (생성자, Value Object 처리)
/// - 매핑 설정이 코드와 분리되어 리팩토링 시 깨지기 쉬움
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
        // External Config: Domain 엔티티를 직접 저장 (별도 매핑 없음)
        await _repository.AddAsync(product, cancellationToken);

        return product;
    }

    /// <summary>
    /// Use Case: 상품 조회
    /// </summary>
    public async Task<Product?> GetProductAsync(
        ProductId id,
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
        ProductId id,
        decimal newPrice,
        string currency,
        CancellationToken cancellationToken = default)
    {
        Product? product = await _repository.GetByIdAsync(id, cancellationToken);
        if (product is null)
            return null;

        // Domain 엔티티의 비즈니스 메서드 호출
        product.UpdatePrice(newPrice, currency);

        // 출력 포트를 통해 영속성 갱신
        await _repository.UpdateAsync(product, cancellationToken);

        return product;
    }

    /// <summary>
    /// Use Case: 상품 삭제
    /// </summary>
    public async Task<bool> DeleteProductAsync(
        ProductId id,
        CancellationToken cancellationToken = default)
    {
        Product? product = await _repository.GetByIdAsync(id, cancellationToken);
        if (product is null)
            return false;

        await _repository.DeleteAsync(id, cancellationToken);
        return true;
    }
}
