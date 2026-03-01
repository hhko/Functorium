using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Customers.Ports;
using LayeredArch.Domain.AggregateRoots.Customers;

using LayeredArch.Adapters.Persistence.Repositories.InMemory.Orders;

namespace LayeredArch.Adapters.Persistence.Repositories.InMemory.Customers;

/// <summary>
/// InMemory 기반 Customer + Order LEFT JOIN + GROUP BY 읽기 전용 어댑터.
/// 고객별 주문 요약(총 주문 수, 총 지출, 마지막 주문일)을 집계합니다.
/// </summary>
[GenerateObservablePort]
public class InMemoryCustomerOrderSummaryQuery
    : InMemoryQueryBase<Customer, CustomerOrderSummaryDto>, ICustomerOrderSummaryQuery
{
    public string RequestCategory => "QueryAdapter";

    protected override string DefaultSortField => "CustomerName";

    protected override IEnumerable<CustomerOrderSummaryDto> GetProjectedItems(Specification<Customer> spec)
    {
        var ordersByCustomer = InMemoryOrderRepository.Orders.Values
            .GroupBy(o => o.CustomerId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return InMemoryCustomerRepository.Customers.Values
            .Where(c => spec.IsSatisfiedBy(c))
            .Select(c =>
            {
                var customerOrders = ordersByCustomer.GetValueOrDefault(c.Id, []);
                return new CustomerOrderSummaryDto(
                    c.Id.ToString(),
                    c.Name,
                    customerOrders.Count,
                    customerOrders.Sum(o => (decimal)o.TotalAmount),
                    customerOrders.Count > 0
                        ? customerOrders.Max(o => o.CreatedAt)
                        : null);
            });
    }

    protected override Func<CustomerOrderSummaryDto, object> SortSelector(string fieldName) => fieldName switch
    {
        "CustomerName" => c => c.CustomerName,
        "OrderCount" => c => c.OrderCount,
        "TotalSpent" => c => c.TotalSpent,
        "LastOrderDate" => c => c.LastOrderDate ?? DateTime.MinValue,
        _ => c => c.CustomerName
    };
}
