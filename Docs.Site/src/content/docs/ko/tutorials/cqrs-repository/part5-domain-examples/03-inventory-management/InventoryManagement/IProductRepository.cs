using Functorium.Domains.Repositories;

namespace InventoryManagement;

/// <summary>
/// 상품 Repository 인터페이스.
/// </summary>
public interface IProductRepository : IRepository<Product, ProductId>
{
}
