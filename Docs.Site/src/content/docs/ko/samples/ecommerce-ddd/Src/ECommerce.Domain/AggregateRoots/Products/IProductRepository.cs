using Functorium.Domains.Repositories;

namespace ECommerce.Domain.AggregateRoots.Products;

/// <summary>
/// 상품 리포지토리 인터페이스 (Command 전용)
/// </summary>
public interface IProductRepository : IRepository<Product, ProductId>
{
    /// <summary>
    /// Specification 기반 존재 여부 확인.
    /// </summary>
    FinT<IO, bool> Exists(Specification<Product> spec);

    /// <summary>
    /// 삭제된 상품을 포함하여 ID로 조회합니다.
    /// Restore 등 삭제된 상품에 접근이 필요한 경우 사용합니다.
    /// </summary>
    FinT<IO, Product> GetByIdIncludingDeleted(ProductId id);
}
