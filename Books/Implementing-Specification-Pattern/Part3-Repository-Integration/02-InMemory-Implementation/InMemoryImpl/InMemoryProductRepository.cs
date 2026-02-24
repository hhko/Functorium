using Functorium.Domains.Specifications;

namespace InMemoryImpl;

public class InMemoryProductRepository : IProductRepository
{
    private readonly List<Product> _products;

    public InMemoryProductRepository(IEnumerable<Product> products)
        => _products = products.ToList();

    public IEnumerable<Product> FindAll(Specification<Product> spec)
        => _products.Where(spec.IsSatisfiedBy);

    public bool Exists(Specification<Product> spec)
        => _products.Any(spec.IsSatisfiedBy);
}
