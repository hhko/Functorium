using Functorium.Applications.Queries;
using LayeredArch.Application.Usecases.Customers.Dtos;
using LayeredArch.Domain.AggregateRoots.Customers;

namespace LayeredArch.Application.Usecases.Customers.Ports;

/// <summary>
/// Customer 단건 조회용 읽기 전용 어댑터 포트.
/// Aggregate 재구성 없이 DB에서 DTO로 직접 프로젝션합니다.
/// </summary>
public interface ICustomerDetailQueryAdapter : IQueryAdapter
{
    FinT<IO, CustomerDetailDto> GetById(CustomerId id);
}
