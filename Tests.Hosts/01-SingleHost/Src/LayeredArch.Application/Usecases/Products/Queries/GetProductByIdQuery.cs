using LayeredArch.Application.Usecases.Products.Ports;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Application.Usecases.Products.Queries;

/// <summary>
/// ID로 상품 조회 Query - Logger Pipeline 데모
/// 요청/응답 로깅 및 경과 시간 측정
/// </summary>
public sealed class GetProductByIdQuery
{
    /// <summary>
    /// Query Request - 조회할 상품 ID
    /// </summary>
    public sealed record Request(string ProductId) : IQueryRequest<Response>;

    /// <summary>
    /// Request Validator
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.ProductId).MustBeEntityId<Request, ProductId>();
        }
    }

    /// <summary>
    /// Query Response - 조회된 상품 정보
    /// </summary>
    public sealed record Response(
        string ProductId,
        string Name,
        string Description,
        decimal Price,
        DateTime CreatedAt,
        Option<DateTime> UpdatedAt);

    /// <summary>
    /// Query Handler - 상품 조회 로직
    /// </summary>
    public sealed class Usecase(IProductDetailQuery productDetailQuery)
        : IQueryUsecase<Request, Response>
    {
        private readonly IProductDetailQuery _productDetailQuery = productDetailQuery;

        /// <summary>
        /// LINQ 쿼리 표현식을 사용한 함수형 체이닝
        /// FinTUtilites의 SelectMany 확장 메서드를 통해 FinT 모나드 트랜스포머를 LINQ로 처리
        /// </summary>
        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var productId = ProductId.Create(request.ProductId);
            FinT<IO, Response> usecase =
                from result in _productDetailQuery.GetById(productId)
                select new Response(
                    result.ProductId,
                    result.Name,
                    result.Description,
                    result.Price,
                    result.CreatedAt,
                    result.UpdatedAt);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
