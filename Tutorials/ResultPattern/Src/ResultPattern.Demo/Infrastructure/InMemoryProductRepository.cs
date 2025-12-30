using System.Collections.Concurrent;
using LanguageExt;
using LanguageExt.Common;
using ResultPattern.Demo.Domain;
using static LanguageExt.Prelude;

namespace ResultPattern.Demo.Infrastructure;

/// <summary>
/// 메모리 기반 Product Repository 구현
/// </summary>
public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly ConcurrentDictionary<Guid, Product> _products = new();

    public ValueTask<Fin<Product>> CreateAsync(Product product, CancellationToken cancellationToken = default)
    {
        if (_products.TryAdd(product.Id, product))
        {
            return ValueTask.FromResult(Fin.Succ(product));
        }

        return ValueTask.FromResult(Fin.Fail<Product>(Error.New($"Product with ID {product.Id} already exists")));
    }

    public ValueTask<Fin<Product>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (_products.TryGetValue(id, out var product))
        {
            return ValueTask.FromResult(Fin.Succ(product));
        }

        return ValueTask.FromResult(Fin.Fail<Product>(Error.New($"상품 ID '{id}'를 찾을 수 없습니다")));
    }

    public ValueTask<Fin<IReadOnlyList<Product>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Product> products = _products.Values.ToList().AsReadOnly();
        return ValueTask.FromResult(Fin.Succ(products));
    }
}
