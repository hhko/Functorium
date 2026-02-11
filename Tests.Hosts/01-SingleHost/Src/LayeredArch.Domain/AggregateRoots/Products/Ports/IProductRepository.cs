using Functorium.Applications.Observabilities;

namespace LayeredArch.Domain.AggregateRoots.Products;

/// <summary>
/// 상품 리포지토리 인터페이스
/// 관찰 가능성 로그를 위한 IAdapter 인터페이스 상속
/// </summary>
public interface IProductRepository : IAdapter
{
    /// <summary>
    /// 상품 생성
    /// </summary>
    FinT<IO, Product> Create(Product product);

    /// <summary>
    /// ID로 상품 조회.
    /// 상품이 없으면 실패(Error)를 반환합니다.
    /// </summary>
    FinT<IO, Product> GetById(ProductId id);

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
    /// 상품 업데이트
    /// </summary>
    FinT<IO, Product> Update(Product product);

    /// <summary>
    /// 상품 삭제
    /// </summary>
    FinT<IO, Unit> Delete(ProductId id);

    /// <summary>
    /// 상품명 중복 확인.
    /// excludeId가 지정되면 해당 상품은 제외하고 검사합니다.
    /// </summary>
    FinT<IO, bool> ExistsByName(ProductName name, ProductId? excludeId = null);
}
