using CqrsPipeline.Demo.Domain;
using Microsoft.Extensions.Logging;

namespace CqrsPipeline.Demo.Usecases;

/// <summary>
/// ID로 상품 조회 Query - Logger Pipeline 데모
/// 요청/응답 로깅 및 경과 시간 측정
/// </summary>
public sealed class GetProductByIdQuery
{
    /// <summary>
    /// Query Request - 조회할 상품 ID
    /// </summary>
    public sealed record class Request(Guid ProductId) : IQueryRequest<Response>;

    /// <summary>
    /// Query Response - 조회된 상품 정보
    /// </summary>
    public sealed record class Response(
        Guid ProductId,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        DateTime CreatedAt,
        DateTime? UpdatedAt) : IResponse;

    /// <summary>
    /// Query Usecase - 상품 조회 로직
    /// </summary>
    internal sealed class Usecase(
        ILogger<Usecase> logger,
        IProductRepository productRepository)
        : IQueryUsecase<Request, Response>
    {
        private readonly ILogger<Usecase> _logger = logger;
        private readonly IProductRepository _productRepository = productRepository;

        public async ValueTask<IFinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting product by ID: {ProductId}", request.ProductId);

            Fin<Product?> getResult = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);

            if (getResult.IsFail)
            {
                Error error = (Error)getResult;
                _logger.LogError("Failed to get product: {Error}", error.Message);
                return FinResponseUtilites.ToResponseFail<Response>(error);
            }

            Product? product = (Product?)getResult;
            if (product is null)
            {
                _logger.LogWarning("Product not found: {ProductId}", request.ProductId);
                return FinResponseUtilites.ToResponseFail<Response>(
                    Error.New($"상품 ID '{request.ProductId}'을(를) 찾을 수 없습니다"));
            }

            _logger.LogInformation("Product found: {ProductId}, {Name}", product.Id, product.Name);
            return FinResponseUtilites.ToResponse(
                new Response(
                    product.Id,
                    product.Name,
                    product.Description,
                    product.Price,
                    product.StockQuantity,
                    product.CreatedAt,
                    product.UpdatedAt));
        }
    }
}
