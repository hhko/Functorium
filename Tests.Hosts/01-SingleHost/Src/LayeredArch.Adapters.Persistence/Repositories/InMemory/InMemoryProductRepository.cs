using System.Collections.Concurrent;
using LayeredArch.Domain.AggregateRoots.Products;
using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using Functorium.Domains.Specifications;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using static Functorium.Adapters.Errors.AdapterErrorType;
using static LanguageExt.Prelude;

namespace LayeredArch.Adapters.Persistence.Repositories.InMemory;

/// <summary>
/// 메모리 기반 상품 리포지토리 구현
/// 관찰 가능성 로그를 위한 IObservablePort 인터페이스 구현
/// GenerateObservablePort 애트리뷰트로 Observable 버전 자동 생성
/// </summary>
[GenerateObservablePort]
public class InMemoryProductRepository : IProductRepository
{
    internal static readonly ConcurrentDictionary<ProductId, Product> Products = new();
    private readonly IDomainEventCollector _eventCollector;

    /// <summary>
    /// 관찰 가능성 로그를 위한 요청 카테고리
    /// </summary>
    public string RequestCategory => "Repository";

    public InMemoryProductRepository(IDomainEventCollector eventCollector)
    {
        _eventCollector = eventCollector;
    }

    public virtual FinT<IO, Product> Create(Product product)
    {
        // Observable이 자동으로 Activity 생성 및 로깅 처리
        return IO.lift(() =>
        {
            if (((string)product.Name).Contains("[adapter-error]", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"[{nameof(InMemoryProductRepository)}] 시뮬레이션된 어댑터 예외: Repository Create 예외 처리 데모");
            }

            Products[product.Id] = product;
            _eventCollector.Track(product);
            return Fin.Succ(product);
        });
    }

    public virtual FinT<IO, Product> GetById(ProductId id)
    {
        // Observable이 자동으로 Activity 생성 및 로깅 처리
        return IO.lift(() =>
        {
            if (Products.TryGetValue(id, out Product? product) && product.DeletedAt.IsNone)
            {
                return Fin.Succ(product);
            }

            return AdapterError.For<InMemoryProductRepository>(
                new NotFound(),
                id.ToString(),
                $"상품 ID '{id}'을(를) 찾을 수 없습니다");
        });
    }

    public virtual FinT<IO, Product> Update(Product product)
    {
        // Observable이 자동으로 Activity 생성 및 로깅 처리
        return IO.lift(() =>
        {
            if (!Products.ContainsKey(product.Id))
            {
                return AdapterError.For<InMemoryProductRepository>(
                    new NotFound(),
                    product.Id.ToString(),
                    $"상품 ID '{product.Id}'을(를) 찾을 수 없습니다");
            }

            if (((string)product.Name).Contains("[adapter-error]", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"[{nameof(InMemoryProductRepository)}] 시뮬레이션된 어댑터 예외: Repository Update 예외 처리 데모");
            }

            Products[product.Id] = product;
            _eventCollector.Track(product);
            return Fin.Succ(product);
        });
    }

    public virtual FinT<IO, int> Delete(ProductId id)
    {
        // Observable이 자동으로 Activity 생성 및 로깅 처리
        return IO.lift(() =>
        {
            if (!Products.TryGetValue(id, out var product))
            {
                return Fin.Succ(0);
            }

            product.Delete("system");
            _eventCollector.Track(product);
            return Fin.Succ(1);
        });
    }

    public virtual FinT<IO, Product> GetByIdIncludingDeleted(ProductId id)
    {
        return IO.lift(() =>
        {
            if (Products.TryGetValue(id, out Product? product))
            {
                return Fin.Succ(product);
            }

            return AdapterError.For<InMemoryProductRepository>(
                new NotFound(),
                id.ToString(),
                $"상품 ID '{id}'을(를) 찾을 수 없습니다");
        });
    }

    public virtual FinT<IO, Seq<Product>> CreateRange(IReadOnlyList<Product> products)
    {
        return IO.lift(() =>
        {
            foreach (var product in products)
                Products[product.Id] = product;
            _eventCollector.TrackRange(products);
            return Fin.Succ(toSeq(products));
        });
    }

    public virtual FinT<IO, Seq<Product>> GetByIds(IReadOnlyList<ProductId> ids)
    {
        return IO.lift(() =>
        {
            var result = ids
                .Where(id => Products.TryGetValue(id, out var p) && p.DeletedAt.IsNone)
                .Select(id => Products[id])
                .ToList();
            return Fin.Succ(toSeq(result));
        });
    }

    public virtual FinT<IO, Seq<Product>> UpdateRange(IReadOnlyList<Product> products)
    {
        return IO.lift(() =>
        {
            foreach (var product in products)
                Products[product.Id] = product;
            _eventCollector.TrackRange(products);
            return Fin.Succ(toSeq(products));
        });
    }

    public virtual FinT<IO, int> DeleteRange(IReadOnlyList<ProductId> ids)
    {
        return IO.lift(() =>
        {
            int affected = 0;
            foreach (var id in ids)
            {
                if (Products.TryGetValue(id, out var product))
                {
                    product.Delete("system");
                    affected++;
                }
            }
            return Fin.Succ(affected);
        });
    }

    public virtual FinT<IO, bool> Exists(Specification<Product> spec)
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
