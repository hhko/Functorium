using Cqrs03Functional.Demo.Domain;
using Functorium.Applications.Linq;
using Microsoft.Extensions.Logging;

namespace Cqrs03Functional.Demo.Usecases;

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

        /// <summary>
        /// LINQ 쿼리 표현식을 사용한 함수형 체이닝
        /// FinTUtilites의 SelectMany 확장 메서드를 통해 FinT 모나드 트랜스포머를 LINQ로 처리
        /// </summary>
        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // LINQ 쿼리 표현식: Repository의 FinT<IO, Seq<Product>>를 직접 사용하여 Response로 변환
            // FinTUtilites.SelectMany가 FinT를 LINQ 쿼리 표현식에서 사용 가능하도록 지원
            FinT<IO, Response> usecase =
                from products in _productRepository.GetAll()
                select new Response(
                    products
                        .Select(p => new ProductDto(p.Id, p.Name, p.Price, p.StockQuantity))
                        .ToSeq());

            // FinT<IO, Response> 
            //  -Run()→           IO<Fin<Response>> 
            //  -RunAsync()→      Fin<Response> 
            //  -ToFinResponse()→ FinResponse<Response>
            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
