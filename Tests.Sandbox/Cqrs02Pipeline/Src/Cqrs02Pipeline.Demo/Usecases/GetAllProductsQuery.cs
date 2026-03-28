using Cqrs02Pipeline.Demo.Domain;
using Microsoft.Extensions.Logging;

namespace Cqrs02Pipeline.Demo.Usecases;

/// <summary>
/// 모든 상품 조회 Query - Trace/Metric Pipeline 데모
/// OpenTelemetry 트레이싱 및 메트릭 수집
/// </summary>
public sealed class GetAllProductsQuery
{
    /// <summary>
    /// Query Request - 추가 파라미터 없음
    /// </summary>
    public sealed record Request() : IQueryRequest<Response>;

    /// <summary>
    /// Query Response - 상품 목록
    /// </summary>
    public sealed record Response(Seq<ProductDto> Products);

    /// <summary>
    /// 상품 DTO - 클라이언트 응답용
    /// </summary>
    public sealed record ProductDto(
        Guid ProductId,
        string Name,
        decimal Price,
        int StockQuantity);

    /// <summary>
    /// Query Handler - 전체 상품 조회 로직
    /// </summary>
    internal sealed class Usecase(
        ILogger<Usecase> logger,
        IProductRepository productRepository)
        : IQueryUsecase<Request, Response>
    {
        private readonly ILogger<Usecase> _logger = logger;
        private readonly IProductRepository _productRepository = productRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            Fin<Seq<Product>> getAllResult = await _productRepository.GetAllAsync(cancellationToken);

            return getAllResult.ToFinResponse(products =>
            {
                Seq<ProductDto> productDtos = products
                    .Select(p => new ProductDto(p.Id, p.Name, p.Price, p.StockQuantity))
                    .ToSeq();

                return new Response(productDtos);
            });
        }
    }
}
