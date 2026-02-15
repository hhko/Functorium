using Functorium.Domains.Specifications;
using LayeredArch.Domain.AggregateRoots.Customers.ValueObjects;

namespace LayeredArch.Domain.AggregateRoots.Customers.Specifications;

/// <summary>
/// 이메일 중복 확인 Specification.
/// </summary>
public sealed class CustomerEmailSpec : Specification<Customer>
{
    public Email Email { get; }

    public CustomerEmailSpec(Email email)
    {
        Email = email;
    }

    public override bool IsSatisfiedBy(Customer customer) =>
        (string)customer.Email == (string)Email;
}
