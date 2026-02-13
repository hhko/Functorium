using Functorium.Domains.Repositories;

namespace LayeredArch.Domain.AggregateRoots.Products;

/// <summary>
/// 상품 리포지토리 인터페이스
/// </summary>
public interface IProductRepository : IRepository<Product, ProductId>
{
    /// <summary>
    /// 상품명으로 조회 (Optional).
    /// 상품이 없으면 None을 반환합니다.
    /// </summary>
    FinT<IO, Option<Product>> GetByName(ProductName name);

    /// <summary>
    /// 모든 상품 조회
    /// </summary>
    FinT<IO, Seq<Product>> GetAll();

    /// <summary>
    /// 상품명 중복 확인.
    /// excludeId가 지정되면 해당 상품은 제외하고 검사합니다.
    /// </summary>
    FinT<IO, bool> ExistsByName(ProductName name, ProductId? excludeId = null);
}
