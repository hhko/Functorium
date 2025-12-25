using CqrsPipeline.Demo.Domain;
using Microsoft.Extensions.Logging;

namespace CqrsPipeline.Demo.Usecases;

/// <summary>
/// 모든 상품 조회 Query - Trace/Metric Pipeline 데모
/// OpenTelemetry 트레이싱 및 메트릭 수집
/// </summary>
public sealed class GetAllProductsQuery
{
    /// <summary>
    /// Query Request - 추가 파라미터 없음
    /// </summary>
    public sealed record class Request() : IQueryRequest<Response>;

    /// <summary>
    /// Query Response - 상품 목록
    /// </summary>
    public sealed record class Response(Seq<ProductDto> Products) : ResponseBase<Response>
    {
        public Response() : this(Seq<ProductDto>.Empty) { }
    }

    /// <summary>
    /// 상품 DTO - 클라이언트 응답용
    /// </summary>
    public sealed record class ProductDto(
        Guid ProductId,
        string Name,
        decimal Price,
        int StockQuantity);

    /// <summary>
    /// Query Usecase - 전체 상품 조회 로직
    /// </summary>
    internal sealed class Usecase(
        ILogger<Usecase> logger,
        IProductRepository productRepository)
        : IQueryUsecase<Request, Response>
    {
        private readonly ILogger<Usecase> _logger = logger;
        private readonly IProductRepository _productRepository = productRepository;

        public async ValueTask<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            //_logger.LogInformation("Getting all products");

            Fin<Seq<Product>> getAllResult = await _productRepository.GetAllAsync(cancellationToken);

            return getAllResult.Match<Response>(
                Succ: products =>
                {
                    Seq<ProductDto> productDtos = products
                        .Select(p => new ProductDto(p.Id, p.Name, p.Price, p.StockQuantity))
                        .ToSeq();

                    //_logger.LogInformation("Found {Count} products", productDtos.Count);
                    return new Response(productDtos);
                },
                Fail: error =>
                {
                    //_logger.LogError("Failed to get all products: {Error}", error.Message);
                    return Response.CreateFail(error);
                });
        }
    }
}
