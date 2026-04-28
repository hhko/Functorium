namespace LayerDependencyRules.Domains.Ports;

public interface IProductRepository
{
    Task<Product?> GetByNameAsync(string name);
}
