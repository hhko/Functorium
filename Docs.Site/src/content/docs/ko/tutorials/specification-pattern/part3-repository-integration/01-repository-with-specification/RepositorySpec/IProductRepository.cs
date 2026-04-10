using Functorium.Domains.Specifications;

namespace RepositorySpec;

public interface IProductRepository
{
    IEnumerable<Product> FindAll(Specification<Product> spec);
    bool Exists(Specification<Product> spec);
}
