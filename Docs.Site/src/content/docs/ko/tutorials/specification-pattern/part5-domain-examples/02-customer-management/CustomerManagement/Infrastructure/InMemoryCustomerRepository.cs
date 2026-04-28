using CustomerManagement.Domain;
using Functorium.Domains.Specifications;

namespace CustomerManagement.Infrastructure;

public sealed class InMemoryCustomerRepository : ICustomerRepository
{
    private readonly List<Customer> _customers;

    public InMemoryCustomerRepository(IEnumerable<Customer> customers)
    {
        _customers = customers.ToList();
    }

    public IEnumerable<Customer> FindAll(Specification<Customer> spec)
    {
        return _customers.Where(spec.IsSatisfiedBy);
    }

    public bool Exists(Specification<Customer> spec)
    {
        return _customers.Any(spec.IsSatisfiedBy);
    }
}
