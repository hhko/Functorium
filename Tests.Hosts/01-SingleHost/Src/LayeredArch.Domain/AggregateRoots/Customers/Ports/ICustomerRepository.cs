using Functorium.Domains.Repositories;
using LayeredArch.Domain.AggregateRoots.Customers.ValueObjects;

namespace LayeredArch.Domain.AggregateRoots.Customers;

/// <summary>
/// 고객 리포지토리 인터페이스
/// </summary>
public interface ICustomerRepository : IRepository<Customer, CustomerId>
{
    /// <summary>
    /// 이메일 중복 확인
    /// </summary>
    FinT<IO, bool> ExistsByEmail(Email email);
}
