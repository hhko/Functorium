using Functorium.Domains.Specifications;

namespace TestingStrategies;

public interface IProductRepository
{
    IEnumerable<Product> FindAll(Specification<Product> spec);
    bool Exists(Specification<Product> spec);
}
