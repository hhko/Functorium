using System.Linq.Expressions;
using Functorium.Domains.Specifications;
using ECommerce.Domain.AggregateRoots.Customers;

namespace ECommerce.Domain.AggregateRoots.Orders.Specifications;

/// <summary>
/// 특정 고객의 주문 Specification.
/// IRepository.FindAllSatisfying(spec) 또는 IRepository.Count(spec)와 함께
/// "한 고객의 주문 목록 로드·집계"에 사용합니다.
/// Expression 기반으로 EF Core 자동 SQL 번역을 지원합니다(PropertyMap 필요).
/// </summary>
/// <example>
/// var spec = new OrdersForCustomerSpec(customerId);
/// FinT&lt;IO, Seq&lt;Order&gt;&gt; orders = repository.FindAllSatisfying(spec);
/// </example>
public sealed class OrdersForCustomerSpec : ExpressionSpecification<Order>
{
    public CustomerId CustomerId { get; }

    public OrdersForCustomerSpec(CustomerId customerId)
    {
        CustomerId = customerId;
    }

    public override Expression<Func<Order, bool>> ToExpression()
    {
        var customerId = CustomerId;
        return order => order.CustomerId == customerId;
    }
}
