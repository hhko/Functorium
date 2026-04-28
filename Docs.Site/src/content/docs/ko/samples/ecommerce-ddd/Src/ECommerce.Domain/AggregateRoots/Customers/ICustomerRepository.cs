using Functorium.Domains.Repositories;

namespace ECommerce.Domain.AggregateRoots.Customers;

/// <summary>
/// 고객 리포지토리 인터페이스
/// </summary>
public interface ICustomerRepository : IRepository<Customer, CustomerId>
{
    /// <summary>
    /// Specification 기반 존재 여부 확인.
    /// </summary>
    FinT<IO, bool> Exists(Specification<Customer> spec);
}
