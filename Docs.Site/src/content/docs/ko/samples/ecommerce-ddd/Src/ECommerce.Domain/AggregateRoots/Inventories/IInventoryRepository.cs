using Functorium.Domains.Repositories;
using ECommerce.Domain.AggregateRoots.Products;

namespace ECommerce.Domain.AggregateRoots.Inventories;

/// <summary>
/// 재고 리포지토리 인터페이스
/// </summary>
public interface IInventoryRepository : IRepository<Inventory, InventoryId>
{
    /// <summary>
    /// 상품 ID로 재고 조회.
    /// </summary>
    FinT<IO, Inventory> GetByProductId(ProductId productId);

    /// <summary>
    /// Specification 기반 존재 여부 확인.
    /// </summary>
    FinT<IO, bool> Exists(Specification<Inventory> spec);
}
