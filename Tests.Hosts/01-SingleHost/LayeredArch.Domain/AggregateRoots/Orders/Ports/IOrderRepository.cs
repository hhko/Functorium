using Functorium.Applications.Observabilities;

namespace LayeredArch.Domain.AggregateRoots.Orders;

/// <summary>
/// 주문 리포지토리 인터페이스
/// </summary>
public interface IOrderRepository : IAdapter
{
    /// <summary>
    /// 주문 생성
    /// </summary>
    FinT<IO, Order> Create(Order order);

    /// <summary>
    /// ID로 주문 조회
    /// </summary>
    FinT<IO, Order> GetById(OrderId id);
}
