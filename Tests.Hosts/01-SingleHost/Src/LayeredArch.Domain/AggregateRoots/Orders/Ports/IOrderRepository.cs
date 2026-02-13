using Functorium.Domains.Repositories;

namespace LayeredArch.Domain.AggregateRoots.Orders;

/// <summary>
/// 주문 리포지토리 인터페이스
/// </summary>
public interface IOrderRepository : IRepository<Order, OrderId>
{
}
