using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using ECommerce.Application.Usecases.Products.Ports;
using ECommerce.Domain.AggregateRoots.Products;

namespace ECommerce.Application.Usecases.Products.Queries;

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
    public sealed record Response(IReadOnlyList<ProductSummaryDto> Products);

    /// <summary>
    /// Query Handler - Read Adapter를 통한 전체 상품 조회
    /// </summary>
    public sealed class Usecase(IProductQuery productQuery)
        : IQueryUsecase<Request, Response>
    {
        private readonly IProductQuery _productQuery = productQuery;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            PageRequest pageRequest = new(1, int.MaxValue);

            FinT<IO, Response> usecase =
                from result in _productQuery.Search(Specification<Product>.All, pageRequest, SortExpression.Empty)
                select new Response(result.Items);

            Fin<Response> response = await usecase.Run().RunAsync();

            return response.ToFinResponse();
        }
    }
}
