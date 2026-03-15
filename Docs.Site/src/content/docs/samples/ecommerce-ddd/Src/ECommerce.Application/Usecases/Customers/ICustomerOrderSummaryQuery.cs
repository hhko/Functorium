using Functorium.Applications.Queries;
using ECommerce.Domain.AggregateRoots.Customers;

namespace ECommerce.Application.Usecases.Customers.Ports;

/// <summary>
/// Customer + Order LEFT JOIN + GROUP BY 집계 읽기 전용 어댑터 포트.
/// 고객별 주문 요약(총 주문 수, 총 지출, 마지막 주문일)을 프로젝션합니다.
/// </summary>
public interface ICustomerOrderSummaryQuery : IQueryPort<Customer, CustomerOrderSummaryDto> { }

public sealed record CustomerOrderSummaryDto(
    string CustomerId,
    string CustomerName,
    int OrderCount,
    decimal TotalSpent,
    DateTime? LastOrderDate);
