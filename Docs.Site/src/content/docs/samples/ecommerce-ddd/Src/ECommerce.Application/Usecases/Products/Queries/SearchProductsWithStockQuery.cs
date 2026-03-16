using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using ECommerce.Application.Usecases.Products.Ports;
using ECommerce.Domain.AggregateRoots.Products;
using ECommerce.Domain.AggregateRoots.Products.Specifications;

namespace ECommerce.Application.Usecases.Products.Queries;

/// <summary>
/// 상품+재고 검색 Query - Dapper JOIN 패턴 데모
/// Product와 Inventory를 결합하여 재고 수량 포함 검색.
/// Read Adapter를 통해 Aggregate 재구성 없이 DTO로 직접 프로젝션합니다.
/// </summary>
public sealed class SearchProductsWithStockQuery
{
    private static readonly string[] AllowedSortFields = ["Name", "Price", "StockQuantity"];

    /// <summary>
    /// Query Request - 선택적 검색 필터 + 페이지네이션/정렬
    /// </summary>
    public sealed record Request(
        Option<decimal> MinPrice = default,
        Option<decimal> MaxPrice = default,
        int Page = 1,
        int PageSize = PageRequest.DefaultPageSize,
        string SortBy = "",
        string SortDirection = "") : IQueryRequest<Response>;

    /// <summary>
    /// Query Response - 페이지네이션된 검색 결과 (재고 포함)
    /// </summary>
    public sealed record Response(
        IReadOnlyList<ProductWithStockDto> Products,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages,
        bool HasNextPage,
        bool HasPreviousPage);

    /// <summary>
    /// Request Validator - FluentValidation 검증 규칙
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            this.MustBePairedRange(
                x => x.MinPrice,
                x => x.MaxPrice,
                Money.Validate,
                inclusive: true);

            RuleFor(x => x.SortBy).MustBeOneOf(AllowedSortFields);

            RuleFor(x => x.SortDirection)
                .MustBeEnumValue<Request, Functorium.Applications.Queries.SortDirection>();
        }
    }

    /// <summary>
    /// Query Handler - Read Adapter를 통한 페이지네이션 검색 (재고 포함)
    /// </summary>
    public sealed class Usecase(IProductWithStockQuery productWithStockQuery)
        : IQueryUsecase<Request, Response>
    {
        private readonly IProductWithStockQuery _productWithStockQuery = productWithStockQuery;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var spec = BuildSpecification(request);
            var pageRequest = new PageRequest(request.Page, request.PageSize);
            var sortExpression = SortExpression.By(request.SortBy, Functorium.Applications.Queries.SortDirection.Parse(request.SortDirection));

            FinT<IO, Response> usecase =
                from result in _productWithStockQuery.Search(spec, pageRequest, sortExpression)
                select new Response(
                    result.Items,
                    result.TotalCount,
                    result.Page,
                    result.PageSize,
                    result.TotalPages,
                    result.HasNextPage,
                    result.HasPreviousPage);

            Fin<Response> response = await usecase.Run().RunAsync();

            return response.ToFinResponse();
        }

        private static Specification<Product> BuildSpecification(Request request)
        {
            var spec = Specification<Product>.All;

            request.MinPrice.Bind(min => request.MaxPrice.Map(max => (min, max)))
                .Iter(t => spec = new ProductPriceRangeSpec(
                    Money.Create(t.min).ThrowIfFail(),
                    Money.Create(t.max).ThrowIfFail()));

            return spec;
        }
    }
}
