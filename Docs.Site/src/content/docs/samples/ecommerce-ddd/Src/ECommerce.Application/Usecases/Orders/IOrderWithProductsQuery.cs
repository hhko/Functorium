using Functorium.Applications.Queries;
using ECommerce.Domain.AggregateRoots.Orders;

namespace ECommerce.Application.Usecases.Orders;

/// <summary>
/// Order + OrderLine + Product 3-table JOIN 읽기 전용 어댑터 포트.
/// 단건 주문 조회 시 주문 라인에 상품명을 포함하여 프로젝션합니다.
/// </summary>
public interface IOrderWithProductsQuery : IQueryPort
{
    FinT<IO, OrderWithProductsDto> GetById(OrderId id);
}

public sealed record OrderLineWithProductDto(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record OrderWithProductsDto(
    string OrderId,
    string CustomerId,
    Seq<OrderLineWithProductDto> OrderLines,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAt);
