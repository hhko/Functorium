using LayerDependencyRules.Domains;
using LayerDependencyRules.Domains.Ports;

namespace LayerDependencyRules.Adapters.Persistence;

public sealed class ProductRepository : IProductRepository
{
    public Task<Product?> GetByNameAsync(string name)
        => Task.FromResult<Product?>(null);
}
