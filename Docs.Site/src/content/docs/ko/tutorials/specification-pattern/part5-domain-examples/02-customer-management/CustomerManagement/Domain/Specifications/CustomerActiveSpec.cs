using Functorium.Domains.Specifications;

namespace CustomerManagement.Domain.Specifications;

public sealed class CustomerActiveSpec : Specification<Customer>
{
    public override bool IsSatisfiedBy(Customer entity) => entity.IsActive;
}
