namespace CqrsPipeline.Demo.Domain;

/// <summary>
/// 상품 리포지토리 인터페이스
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// 상품 생성
    /// </summary>
    Task<Fin<Product>> CreateAsync(Product product, CancellationToken cancellationToken);

    /// <summary>
    /// ID로 상품 조회
    /// </summary>
    Task<Fin<Product?>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 모든 상품 조회
    /// </summary>
    Task<Fin<Seq<Product>>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 상품 업데이트
    /// </summary>
    Task<Fin<Product>> UpdateAsync(Product product, CancellationToken cancellationToken);

    /// <summary>
    /// 상품명 중복 확인
    /// </summary>
    Task<Fin<bool>> ExistsByNameAsync(string name, CancellationToken cancellationToken);
}
