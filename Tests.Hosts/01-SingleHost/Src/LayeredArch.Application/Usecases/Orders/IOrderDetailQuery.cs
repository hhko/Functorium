using Functorium.Applications.Queries;
using LayeredArch.Domain.AggregateRoots.Orders;

namespace LayeredArch.Application.Usecases.Orders.Ports;

/// <summary>
/// Order 단건 조회용 읽기 전용 어댑터 포트.
/// Aggregate 재구성 없이 DB에서 DTO로 직접 프로젝션합니다.
/// </summary>
public interface IOrderDetailQuery : IQueryAdapter
{
    FinT<IO, OrderDetailDto> GetById(OrderId id);
}

public sealed record OrderDetailDto(
    string OrderId,
    string ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal TotalAmount,
    string ShippingAddress,
    DateTime CreatedAt);
