using Cqrs03Functional.Demo.Domain;
using Functorium.Applications.Linq;
using Microsoft.Extensions.Logging;

namespace Cqrs03Functional.Demo.Usecases;

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

        /// <summary>
        /// LINQ 쿼리 표현식을 사용한 함수형 체이닝
        /// FinTUtilites의 SelectMany 확장 메서드를 통해 FinT 모나드 트랜스포머를 LINQ로 처리
        /// </summary>
        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // LINQ 쿼리 표현식: Repository의 FinT<IO, Product>를 직접 사용하여 Response로 변환
            // FinTUtilites.SelectMany가 FinT를 LINQ 쿼리 표현식에서 사용 가능하도록 지원
            FinT<IO, Response> usecase =
                from product in _productRepository.GetById(request.ProductId)
                select new Response(
                    product.Id,
                    product.Name,
                    product.Description,
                    product.Price,
                    product.StockQuantity,
                    product.CreatedAt,
                    product.UpdatedAt);

            // FinT<IO, Response> 
            //  -Run()→           IO<Fin<Response>> 
            //  -RunAsync()→      Fin<Response> 
            //  -ToFinResponse()→ FinResponse<Response>
            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
