using Functorium.Domains.Specifications;

namespace CustomerManagement;

/// <summary>
/// 이름으로 고객을 검색하는 Specification.
/// 부분 일치(Contains)로 검색합니다.
/// </summary>
public sealed class CustomerNameSpec(string name) : Specification<Customer>
{
    public override bool IsSatisfiedBy(Customer entity) =>
        entity.Name.Contains(name, StringComparison.OrdinalIgnoreCase);
}
