using Functorium.Applications.Observabilities;
using LayeredArch.Domain.AggregateRoots.Customers.ValueObjects;

namespace LayeredArch.Domain.AggregateRoots.Customers;

/// <summary>
/// 고객 리포지토리 인터페이스
/// </summary>
public interface ICustomerRepository : IAdapter
{
    /// <summary>
    /// 고객 생성
    /// </summary>
    FinT<IO, Customer> Create(Customer customer);

    /// <summary>
    /// ID로 고객 조회
    /// </summary>
    FinT<IO, Customer> GetById(CustomerId id);

    /// <summary>
    /// 이메일 중복 확인
    /// </summary>
    FinT<IO, bool> ExistsByEmail(Email email);
}
