using EcommerceFiltering.Domain;
using Functorium.Domains.Specifications;

namespace EcommerceFiltering.Infrastructure;

public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly List<Product> _products;

    public InMemoryProductRepository(IEnumerable<Product> products)
    {
        _products = products.ToList();
    }

    public IEnumerable<Product> FindAll(Specification<Product> spec)
    {
        return _products.Where(spec.IsSatisfiedBy);
    }

    public bool Exists(Specification<Product> spec)
    {
        return _products.Any(spec.IsSatisfiedBy);
    }
}
