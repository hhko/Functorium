using Functorium.Domains.Repositories;
using ECommerce.Domain.AggregateRoots.Customers;
using ECommerce.Domain.AggregateRoots.Orders.Specifications;

namespace ECommerce.Domain.AggregateRoots.Orders;

/// <summary>
/// 주문 리포지토리 인터페이스.
/// IRepository가 제공하는 공통 메서드(Create/Update/Delete·GetById·
/// FindAllSatisfying·Exists 등) 외에, 도메인 언어로 표현되는 전용 메서드를
/// 필요 시 선언합니다.
/// </summary>
/// <remarks>
/// 조건 기반 Aggregate 조회가 자주 사용된다면 Specification을 만들어
/// <see cref="IRepository{TAggregate,TId}.FindAllSatisfying"/> 호출로 우선 해결합니다.
/// Specification 조합이 복잡하거나 호출부 가독성이 중요하면 전용 메서드로
/// 감싸서 도메인 의도를 드러냅니다. 예:
/// <code>
/// FinT&lt;IO, Seq&lt;Order&gt;&gt; FindByCustomer(CustomerId customerId)
///     =&gt; FindAllSatisfying(new OrdersForCustomerSpec(customerId));
/// </code>
/// </remarks>
public interface IOrderRepository : IRepository<Order, OrderId>
{
    /// <summary>
    /// 특정 고객의 모든 주문을 조회합니다.
    /// 구현체는 OrdersForCustomerSpec과 FindAllSatisfying으로 위임합니다.
    /// </summary>
    FinT<IO, Seq<Order>> FindByCustomer(CustomerId customerId)
        => FindAllSatisfying(new OrdersForCustomerSpec(customerId));
}