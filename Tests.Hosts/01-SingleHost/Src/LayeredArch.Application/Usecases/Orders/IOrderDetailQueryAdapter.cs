using Functorium.Applications.Queries;
using LayeredArch.Application.Usecases.Orders.Dtos;
using LayeredArch.Domain.AggregateRoots.Orders;

namespace LayeredArch.Application.Usecases.Orders.Ports;

/// <summary>
/// Order 단건 조회용 읽기 전용 어댑터 포트.
/// Aggregate 재구성 없이 DB에서 DTO로 직접 프로젝션합니다.
/// </summary>
public interface IOrderDetailQueryAdapter : IQueryAdapter
{
    FinT<IO, OrderDetailDto> GetById(OrderId id);
}
