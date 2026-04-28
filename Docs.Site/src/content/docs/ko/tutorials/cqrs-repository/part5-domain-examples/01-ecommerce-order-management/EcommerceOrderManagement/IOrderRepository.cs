using Functorium.Domains.Repositories;

namespace EcommerceOrderManagement;

/// <summary>
/// 주문 Repository 인터페이스.
/// IRepository를 상속하여 기본 CRUD를 제공합니다.
/// </summary>
public interface IOrderRepository : IRepository<Order, OrderId>
{
}
