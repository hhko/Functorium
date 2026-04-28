using Functorium.Domains.Specifications;

namespace InMemoryImpl;

public interface IProductRepository
{
    IEnumerable<Product> FindAll(Specification<Product> spec);
    bool Exists(Specification<Product> spec);
}
