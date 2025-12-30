using System.Collections.Concurrent;
using Cqrs02Pipeline.Demo.Domain;
using static LanguageExt.Prelude;

namespace Cqrs02Pipeline.Demo.Infrastructure;

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

    public Task<Fin<Product>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        if (_products.TryGetValue(id, out Product? product))
        {
            return Task.FromResult(Fin.Succ(product));
        }

        return Task.FromResult(Fin.Fail<Product>(Error.New($"상품 ID '{id}'을(를) 찾을 수 없습니다")));
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
            return Task.FromResult(Fin.Fail<Product>(Error.New($"상품 ID '{product.Id}'을(를) 찾을 수 없습니다")));
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
