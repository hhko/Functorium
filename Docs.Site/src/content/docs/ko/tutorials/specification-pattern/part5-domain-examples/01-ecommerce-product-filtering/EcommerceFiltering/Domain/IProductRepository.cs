using Functorium.Domains.Specifications;

namespace EcommerceFiltering.Domain;

public interface IProductRepository
{
    IEnumerable<Product> FindAll(Specification<Product> spec);
    bool Exists(Specification<Product> spec);
}
