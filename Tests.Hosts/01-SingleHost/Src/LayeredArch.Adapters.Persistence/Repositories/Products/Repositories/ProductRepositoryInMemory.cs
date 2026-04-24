using System.Collections.Concurrent;
using LayeredArch.Domain.AggregateRoots.Products;
using Functorium.Adapters.Errors;
using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using Functorium.Domains.Specifications;
using static Functorium.Adapters.Errors.AdapterErrorKind;
using static LanguageExt.Prelude;

namespace LayeredArch.Adapters.Persistence.Repositories.Products.Repositories;

/// <summary>
/// 메모리 기반 상품 리포지토리 구현
/// Soft Delete + 에러 시뮬레이션을 위해 다수 메서드를 오버라이드합니다.
/// </summary>
[GenerateObservablePort]
public class ProductRepositoryInMemory
    : InMemoryRepositoryBase<Product, ProductId>, IProductRepository
{
    internal static readonly ConcurrentDictionary<ProductId, Product> Products = new();
    protected override ConcurrentDictionary<ProductId, Product> Store => Products;

    public ProductRepositoryInMemory(IDomainEventCollector eventCollector)
        : base(eventCollector) { }

    // ─── 에러 시뮬레이션 오버라이드 ──────────────────

    public override FinT<IO, Product> Create(Product product)
    {
        return IO.lift(() =>
        {
            if (((string)product.Name).Contains("[adapter-error]", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"[{nameof(ProductRepositoryInMemory)}] 시뮬레이션된 어댑터 예외: Repository Create 예외 처리 데모");
            }

            Products[product.Id] = product;
            EventCollector.Track(product);
            return Fin.Succ(product);
        });
    }

    public override FinT<IO, Product> Update(Product product)
    {
        return IO.lift(() =>
        {
            if (!Products.ContainsKey(product.Id))
            {
                return NotFoundError(product.Id);
            }

            if (((string)product.Name).Contains("[adapter-error]", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"[{nameof(ProductRepositoryInMemory)}] 시뮬레이션된 어댑터 예외: Repository Update 예외 처리 데모");
            }

            Products[product.Id] = product;
            EventCollector.Track(product);
            return Fin.Succ(product);
        });
    }

    // ─── Soft Delete 오버라이드 ──────────────────────

    public override FinT<IO, Product> GetById(ProductId id)
    {
        return IO.lift(() =>
        {
            if (Products.TryGetValue(id, out Product? product) && product.DeletedAt.IsNone)
            {
                return Fin.Succ(product);
            }

            return NotFoundError(id);
        });
    }

    public override FinT<IO, Seq<Product>> GetByIds(IReadOnlyList<ProductId> ids)
    {
        return IO.lift(() =>
        {
            if (ids.Count == 0)
                return Fin.Succ(LanguageExt.Seq<Product>.Empty);

            var distinctIds = ids.Distinct().ToList();
            var result = distinctIds
                .Where(id => Products.TryGetValue(id, out var p) && p.DeletedAt.IsNone)
                .Select(id => Products[id])
                .ToList();

            if (result.Count != distinctIds.Count)
            {
                return PartialNotFoundError(distinctIds, result);
            }

            return Fin.Succ(toSeq(result));
        });
    }

    public override FinT<IO, int> Delete(ProductId id)
    {
        return IO.lift(() =>
        {
            if (!Products.TryGetValue(id, out var product))
            {
                return Fin.Succ(0);
            }

            product.Delete("system");
            EventCollector.Track(product);
            return Fin.Succ(1);
        });
    }

    public override FinT<IO, int> DeleteRange(IReadOnlyList<ProductId> ids)
    {
        return IO.lift(() =>
        {
            if (ids.Count == 0)
                return Fin.Succ(0);

            int affected = 0;
            foreach (var id in ids)
            {
                if (Products.TryGetValue(id, out var product))
                {
                    product.Delete("system");
                    EventCollector.Track(product);
                    affected++;
                }
            }
            return Fin.Succ(affected);
        });
    }

    // ─── Product 고유 메서드 ─────────────────────────

    public virtual FinT<IO, Product> GetByIdIncludingDeleted(ProductId id)
    {
        return IO.lift(() =>
        {
            if (Products.TryGetValue(id, out Product? product))
            {
                return Fin.Succ(product);
            }

            return NotFoundError(id);
        });
    }

    public override FinT<IO, bool> Exists(Specification<Product> spec)
    {
        return IO.lift(() =>
        {
            bool exists = Products.Values
                .Where(p => p.DeletedAt.IsNone)
                .Any(p => spec.IsSatisfiedBy(p));
            return Fin.Succ(exists);
        });
    }
}
