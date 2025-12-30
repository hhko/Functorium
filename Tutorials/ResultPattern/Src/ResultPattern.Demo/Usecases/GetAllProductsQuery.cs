using ResultPattern.Demo.Cqrs;
using ResultPattern.Demo.Domain;

namespace ResultPattern.Demo.Usecases;

/// <summary>
/// 전체 상품 조회 Query - Result 패턴 데모
/// </summary>
public sealed class GetAllProductsQuery
{
    /// <summary>
    /// Query Request
    /// </summary>
    public sealed record Request() : IQueryRequest<Response>;

    /// <summary>
    /// 개별 상품 정보 - 기본 생성자 없이 정의!
    /// </summary>
    public sealed record ProductItem(
        Guid ProductId,
        string Name,
        decimal Price,
        int StockQuantity);
    // 기본 생성자 boilerplate 없음!

    /// <summary>
    /// Query Response - 기본 생성자 없이 정의!
    /// </summary>
    public sealed record Response(IReadOnlyList<ProductItem> Products);
    // 기본 생성자 boilerplate 없음!

    /// <summary>
    /// Query Handler
    /// </summary>
    internal sealed class Usecase(IProductRepository productRepository)
        : IQueryUsecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // Fin -> FinResponse 변환
            return (await productRepository.GetAllAsync(cancellationToken))
                .ToFinResponse(products => new Response(
                    products.Select(p => new ProductItem(
                        p.Id,
                        p.Name,
                        p.Price,
                        p.StockQuantity)).ToList().AsReadOnly()));
        }
    }
}
