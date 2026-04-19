using Functorium.Domains.Repositories;

namespace LayeredArch.Domain.AggregateRoots.Customers;

/// <summary>
/// 고객 리포지토리 인터페이스
/// </summary>
public interface ICustomerRepository : IRepository<Customer, CustomerId>
{
}
