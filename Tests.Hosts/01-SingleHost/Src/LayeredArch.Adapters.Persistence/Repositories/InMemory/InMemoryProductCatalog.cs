using LayeredArch.Domain.AggregateRoots.Products;
using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Adapters.Errors.AdapterErrorType;
using LayeredArch.Application.Usecases.Orders.Ports;

namespace LayeredArch.Adapters.Persistence.Repositories.InMemory;

/// <summary>
/// 공유 Port IProductCatalog 구현
/// InMemoryProductRepository의 데이터를 활용하여 교차 Aggregate 검증을 제공
/// </summary>
[GeneratePortObservable]
public class InMemoryProductCatalog : IProductCatalog
{
    private readonly InMemoryProductRepository _productRepository;

    public string RequestCategory => "Repository";

    public InMemoryProductCatalog(InMemoryProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public virtual FinT<IO, bool> ExistsById(ProductId productId)
    {
        return IO.liftAsync(async () =>
        {
            var result = await _productRepository.GetById(productId).Run().RunAsync();
            return Fin.Succ(result.IsSucc);
        });
    }

    public virtual FinT<IO, Money> GetPrice(ProductId productId)
    {
        return IO.liftAsync(async () =>
        {
            var result = await _productRepository.GetById(productId).Run().RunAsync();
            return result.Match(
                Succ: product => Fin.Succ(product.Price),
                Fail: _ => AdapterError.For<InMemoryProductCatalog>(
                    new NotFound(),
                    productId.ToString(),
                    $"상품 ID '{productId}'의 가격을 조회할 수 없습니다"));
        });
    }
}
