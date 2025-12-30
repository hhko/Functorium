using Cqrs02Pipeline.Demo.Domain;
using Microsoft.Extensions.Logging;

namespace Cqrs02Pipeline.Demo.Usecases;

/// <summary>
/// ID로 상품 조회 Query - Logger Pipeline 데모
/// 요청/응답 로깅 및 경과 시간 측정
/// </summary>
public sealed class GetProductByIdQuery
{
    /// <summary>
    /// Query Request - 조회할 상품 ID
    /// </summary>
    public sealed record Request(Guid ProductId) : IQueryRequest<Response>;

    /// <summary>
    /// Query Response - 조회된 상품 정보
    /// </summary>
    public sealed record Response(
        Guid ProductId,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        DateTime CreatedAt,
        DateTime? UpdatedAt);

    /// <summary>
    /// Query Handler - 상품 조회 로직
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
            Fin<Product> getResult = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);

            return getResult.ToFinResponse(product => new Response(
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
