using Functorium.Domains.Repositories;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Domain.AggregateRoots.Inventories;

/// <summary>
/// 재고 리포지토리 인터페이스
/// </summary>
public interface IInventoryRepository : IRepository<Inventory, InventoryId>
{
    /// <summary>
    /// 상품 ID로 재고 조회.
    /// </summary>
    FinT<IO, Inventory> GetByProductId(ProductId productId);
}
