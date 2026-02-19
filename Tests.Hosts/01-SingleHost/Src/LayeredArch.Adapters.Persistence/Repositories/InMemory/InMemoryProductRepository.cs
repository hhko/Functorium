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
/// 관찰 가능성 로그를 위한 IAdapter 인터페이스 구현
/// GeneratePipeline 애트리뷰트로 파이프라인 버전 자동 생성
/// </summary>
[GeneratePipeline]
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
        // Pipeline이 자동으로 Activity 생성 및 로깅 처리
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
        // Pipeline이 자동으로 Activity 생성 및 로깅 처리
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

    public virtual FinT<IO, Product> Update(Product product)
    {
        // Pipeline이 자동으로 Activity 생성 및 로깅 처리
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

    public virtual FinT<IO, Unit> Delete(ProductId id)
    {
        // Pipeline이 자동으로 Activity 생성 및 로깅 처리
        return IO.lift(() =>
        {
            if (!Products.TryRemove(id, out _))
            {
                return AdapterError.For<InMemoryProductRepository>(
                    new NotFound(),
                    id.ToString(),
                    $"상품 ID '{id}'을(를) 찾을 수 없습니다");
            }

            return Fin.Succ(unit);
        });
    }

    public virtual FinT<IO, bool> Exists(Specification<Product> spec)
    {
        return IO.lift(() =>
        {
            bool exists = Products.Values.Any(p => spec.IsSatisfiedBy(p));
            return Fin.Succ(exists);
        });
    }

}
