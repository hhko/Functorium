using Functorium.Applications.Observabilities;

namespace Cqrs03Functional.Demo.Domain;

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
    FinT<IO, Product> GetById(Guid id);

    /// <summary>
    /// 모든 상품 조회
    /// </summary>
    FinT<IO, Seq<Product>> GetAll();

    /// <summary>
    /// 상품 업데이트
    /// </summary>
    FinT<IO, Product> Update(Product product);

    /// <summary>
    /// 상품명 중복 확인
    /// </summary>
    FinT<IO, bool> ExistsByName(string name);
}
