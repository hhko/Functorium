using System.Collections.Concurrent;
using CqrsPipeline.Demo.Domain;
using static LanguageExt.Prelude;

namespace CqrsPipeline.Demo.Infrastructure;

/// <summary>
/// 메모리 기반 상품 리포지토리 구현
/// </summary>
public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly ConcurrentDictionary<Guid, Product> _products = new();

    public Task<Fin<Product>> CreateAsync(Product product, CancellationToken cancellationToken)
    {
        _products[product.Id] = product;
        return Task.FromResult(Fin.Succ(product));
    }

    public Task<Fin<Product?>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        _products.TryGetValue(id, out Product? product);
        return Task.FromResult(Fin.Succ(product));
    }

    public Task<Fin<Seq<Product>>> GetAllAsync(CancellationToken cancellationToken)
    {
        Seq<Product> products = toSeq(_products.Values);
        return Task.FromResult(Fin.Succ(products));
    }

    public Task<Fin<Product>> UpdateAsync(Product product, CancellationToken cancellationToken)
    {
        if (!_products.ContainsKey(product.Id))
        {
            return Task.FromResult(Fin.Fail<Product>(Error.New($"Product with ID '{product.Id}' not found")));
        }

        _products[product.Id] = product;
        return Task.FromResult(Fin.Succ(product));
    }

    public Task<Fin<bool>> ExistsByNameAsync(string name, CancellationToken cancellationToken)
    {
        bool exists = _products.Values.Any(p =>
            p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(Fin.Succ(exists));
    }
}
