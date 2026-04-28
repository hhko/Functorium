using Functorium.Domains.Specifications;

namespace CustomerManagement;

/// <summary>
/// 이메일로 고객을 검색하는 Specification.
/// 대소문자를 구분하지 않습니다.
/// </summary>
public sealed class CustomerEmailSpec(string email) : Specification<Customer>
{
    public override bool IsSatisfiedBy(Customer entity) =>
        entity.Email.Equals(email, StringComparison.OrdinalIgnoreCase);
}
