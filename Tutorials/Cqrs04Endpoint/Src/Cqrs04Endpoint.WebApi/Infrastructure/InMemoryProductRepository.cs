using System.Collections.Concurrent;
using Cqrs04Endpoint.WebApi.Domain;
using Functorium.Adapters.SourceGenerator;
using Functorium.Applications.Observabilities;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Cqrs04Endpoint.WebApi.Infrastructure;

/// <summary>
/// 메모리 기반 상품 리포지토리 구현
/// 관찰 가능성 로그를 위한 IAdapter 인터페이스 구현
/// GeneratePipeline 애트리뷰트로 파이프라인 버전 자동 생성
/// </summary>
[GeneratePipeline]
public class InMemoryProductRepository : IProductRepository
{
    private readonly ILogger<InMemoryProductRepository> _logger;
    private readonly ConcurrentDictionary<Guid, Product> _products = new();

    /// <summary>
    /// 관찰 가능성 로그를 위한 요청 카테고리
    /// </summary>
    public string RequestCategory => "Repository";

    /// <summary>
    /// 테스트용 생성자 (ActivityContext 없이)
    /// </summary>
    public InMemoryProductRepository(ILogger<InMemoryProductRepository> logger)
    {
        _logger = logger;
    }

    public virtual FinT<IO, Product> Create(Product product)
    {
        // Pipeline이 자동으로 Activity 생성 및 로깅 처리
        return IO.lift(() =>
        {
            _products[product.Id] = product;
            return Fin.Succ(product);
        });
    }

    public virtual FinT<IO, Product> GetById(Guid id)
    {
        // Pipeline이 자동으로 Activity 생성 및 로깅 처리
        return IO.lift(() =>
        {
            if (_products.TryGetValue(id, out Product? product))
            {
                return Fin.Succ(product);
            }

            return Fin.Fail<Product>(Error.New($"상품 ID '{id}'을(를) 찾을 수 없습니다"));
        });
    }

    public virtual FinT<IO, Seq<Product>> GetAll()
    {
        // Pipeline이 자동으로 Activity 생성 및 로깅 처리
        return IO.lift(() =>
        {
            Seq<Product> products = toSeq(_products.Values);
            return Fin.Succ(products);
        });
    }

    public virtual FinT<IO, Product> Update(Product product)
    {
        // Pipeline이 자동으로 Activity 생성 및 로깅 처리
        return IO.lift(() =>
        {
            if (!_products.ContainsKey(product.Id))
            {
                return Fin.Fail<Product>(Error.New($"상품 ID '{product.Id}'을(를) 찾을 수 없습니다"));
            }

            _products[product.Id] = product;
            return Fin.Succ(product);
        });
    }

    public virtual FinT<IO, bool> ExistsByName(string name)
    {
        // Pipeline이 자동으로 Activity 생성 및 로깅 처리
        return IO.lift(() =>
        {
            bool exists = _products.Values.Any(p =>
                p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return Fin.Succ(exists);
        });
    }
}
