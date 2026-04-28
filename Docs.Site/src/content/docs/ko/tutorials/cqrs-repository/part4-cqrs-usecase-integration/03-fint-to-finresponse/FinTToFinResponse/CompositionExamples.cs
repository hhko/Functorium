using Functorium.Applications.Usecases;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace FinTToFinResponse;

public static class CompositionExamples
{
    public sealed record CreateResponse(string ProductId, string Name, decimal Price);
    public sealed record UpdatePriceResponse(string ProductId, decimal OldPrice, decimal NewPrice);

    /// <summary>
    /// 패턴 1: 단일 연산 - from...select
    /// </summary>
    public static async Task<FinResponse<CreateResponse>> SimpleCreate(
        IProductRepository repository,
        string name,
        decimal price)
    {
        var product = Product.Create(name, price);

        FinT<IO, CreateResponse> usecase =
            from created in repository.Create(product)
            select new CreateResponse(created.Id.ToString(), created.Name, created.Price);

        return await RunUsecase(usecase);
    }

    /// <summary>
    /// 패턴 2: 순차 연산 - from...from...select
    /// </summary>
    public static async Task<FinResponse<UpdatePriceResponse>> ChainedUpdate(
        IProductRepository repository,
        ProductId productId,
        decimal newPrice)
    {
        FinT<IO, UpdatePriceResponse> usecase =
            from existing in repository.GetById(productId)
            let oldPrice = existing.Price
            from updated in repository.Update(existing.UpdatePrice(newPrice))
            select new UpdatePriceResponse(updated.Id.ToString(), oldPrice, updated.Price);

        return await RunUsecase(usecase);
    }

    /// <summary>
    /// 패턴 3: guard로 조건부 중단
    /// </summary>
    public static async Task<FinResponse<UpdatePriceResponse>> GuardedUpdate(
        IProductRepository repository,
        ProductId productId,
        decimal newPrice)
    {
        FinT<IO, UpdatePriceResponse> usecase =
            from existing in repository.GetById(productId)
            from _ in guard(existing.IsActive, Error.New("Product is not active"))
            let oldPrice = existing.Price
            from updated in repository.Update(existing.UpdatePrice(newPrice))
            select new UpdatePriceResponse(updated.Id.ToString(), oldPrice, updated.Price);

        return await RunUsecase(usecase);
    }

    /// <summary>
    /// FinT IO를 실행하고 FinResponse로 변환합니다.
    /// LanguageExt의 ExpectedException을 Fin.Fail로 변환합니다.
    /// </summary>
    private static async Task<FinResponse<T>> RunUsecase<T>(FinT<IO, T> usecase)
    {
        try
        {
            Fin<T> result = await usecase.Run().RunAsync();
            return result.ToFinResponse();
        }
        catch (ErrorException ex)
        {
            return FinResponse.Fail<T>(ex.ToError());
        }
    }
}
