using Functorium.Applications.Observabilities;
using LanguageExt;
using TwoWayMappingLayered.Domains.Entities;
using TwoWayMappingLayered.Domains.ValueObjects;

namespace TwoWayMappingLayered.Domains.Repositories;

/// <summary>
/// 상품 리포지토리 인터페이스 (Output Port)
///
/// Two-Way Mapping 전략:
/// - Domain 엔티티(Product)를 직접 반환
/// - Adapter 내부에서 ProductEntity ↔ Product 변환 수행
/// - 비즈니스 메서드가 포함된 완전한 Domain 객체 반환
///
/// IAdapter 상속: 관찰 가능성 로그 지원
/// </summary>
public interface IProductRepository : IAdapter
{
    /// <summary>
    /// 상품 생성
    /// Domain → Adapter: ProductMapper.ToEntity() 변환 후 저장
    /// </summary>
    FinT<IO, Product> Create(Product product);

    /// <summary>
    /// ID로 상품 조회
    /// Adapter → Domain: ProductMapper.ToDomain() 변환 후 반환
    /// </summary>
    FinT<IO, Product> GetById(ProductId id);

    /// <summary>
    /// 모든 상품 조회
    /// Adapter → Domain: 각 ProductEntity를 Product로 변환 후 반환
    /// </summary>
    FinT<IO, Seq<Product>> GetAll();

    /// <summary>
    /// 상품 업데이트
    /// Domain → Adapter: ProductMapper.UpdateEntity() 변환 후 저장
    /// </summary>
    FinT<IO, Product> Update(Product product);

    /// <summary>
    /// 상품명 중복 확인
    /// </summary>
    FinT<IO, bool> ExistsByName(string name);
}
