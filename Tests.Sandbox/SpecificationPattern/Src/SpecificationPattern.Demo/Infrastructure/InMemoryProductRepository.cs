using Functorium.Domains.Specifications;
using SpecificationPattern.Demo.Domain;

namespace SpecificationPattern.Demo.Infrastructure;

/// <summary>
/// InMemory 기반 IProductRepository 어댑터.
/// Repository는 HOW(어떻게 필터링)를 모르고, Specification에 위임합니다.
/// </summary>
public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly List<Product> _products;

    public InMemoryProductRepository(IEnumerable<Product> products)
    {
        _products = products.ToList();
    }

    public IEnumerable<Product> FindAll(Specification<Product> spec)
        => _products.Where(spec.IsSatisfiedBy);

    public bool Exists(Specification<Product> spec)
        => _products.Any(spec.IsSatisfiedBy);
}
