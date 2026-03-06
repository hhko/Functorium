using Functorium.Domains.Specifications;

namespace CustomerManagement.Domain;

public interface ICustomerRepository
{
    IEnumerable<Customer> FindAll(Specification<Customer> spec);
    bool Exists(Specification<Customer> spec);
}
