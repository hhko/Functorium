using HexagonalMapping.Domain.Entities;
using HexagonalMapping.Domain.Ports;

namespace HexagonalMapping.Strategy1.TwoWayMapping.Application;

/// <summary>
/// Use Case 구현: 애플리케이션 비즈니스 로직을 담당합니다.
///
/// Hexagonal Architecture에서 Application Layer는:
/// - Domain 엔티티를 조합하여 Use Case를 구현
/// - 출력 포트(IProductRepository)를 통해 영속성 접근
/// - 입력 포트(IProductService)를 구현하여 Adapter에게 기능 제공
///
/// 데이터 흐름:
/// REST Adapter → IProductService(입력 포트) → ProductService → IProductRepository(출력 포트) → Persistence Adapter
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
