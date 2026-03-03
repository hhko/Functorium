using Functorium.Domains.Repositories;
using Functorium.Domains.Specifications;
using LanguageExt;

namespace CustomerManagement;

/// <summary>
/// 고객 Repository 인터페이스.
/// Specification 기반 존재 여부 확인 메서드를 제공합니다.
/// </summary>
public interface ICustomerRepository : IRepository<Customer, CustomerId>
{
    FinT<IO, bool> Exists(Specification<Customer> spec);
}
