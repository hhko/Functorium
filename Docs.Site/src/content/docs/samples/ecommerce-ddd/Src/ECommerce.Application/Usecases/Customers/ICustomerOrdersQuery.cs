using Functorium.Applications.Queries;
using ECommerce.Domain.AggregateRoots.Customers;

namespace ECommerce.Application.Usecases.Customers.Ports;

/// <summary>
/// Customer → Order → OrderLine → Product 4-table JOIN 읽기 전용 어댑터 포트.
/// 특정 고객의 모든 주문을 상품명 포함하여 프로젝션합니다.
/// </summary>
public interface ICustomerOrdersQuery : IQueryPort
{
    FinT<IO, CustomerOrdersDto> GetByCustomerId(CustomerId id);
}

public sealed record CustomerOrderLineDto(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record CustomerOrderDto(
    string OrderId,
    Seq<CustomerOrderLineDto> OrderLines,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAt);

public sealed record CustomerOrdersDto(
    string CustomerId,
    string CustomerName,
    Seq<CustomerOrderDto> Orders);
