using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using ECommerce.Application.Usecases.Products.Ports;
using ECommerce.Domain.AggregateRoots.Products;
using ECommerce.Domain.AggregateRoots.Products.Specifications;

namespace ECommerce.Application.Usecases.Products.Queries;

/// <summary>
/// 상품 검색 Query - Specification 패턴 조합 + 페이지네이션/정렬 데모
/// 가격 범위 등 선택적 필터를 Specification으로 조합하여 검색.
/// Read Adapter를 통해 Aggregate 재구성 없이 DTO로 직접 프로젝션합니다.
/// </summary>
public sealed class SearchProductsQuery
{
    private static readonly string[] AllowedSortFields = ["Name", "Price"];

    /// <summary>
    /// Query Request - 선택적 검색 필터 + 페이지네이션/정렬
    /// </summary>
    public sealed record Request(
        Option<string> Name = default,
        Option<decimal> MinPrice = default,
        Option<decimal> MaxPrice = default,
        int Page = 1,
        int PageSize = PageRequest.DefaultPageSize,
        string SortBy = "",
        string SortDirection = "") : IQueryRequest<Response>;

    /// <summary>
    /// Query Response - 페이지네이션된 검색 결과
    /// </summary>
    public sealed record Response(
        IReadOnlyList<ProductSummaryDto> Products,
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
            RuleFor(x => x.Name)
                .MustSatisfyValidation(ProductName.Validate);

            this.MustBePairedRange(
                x => x.MinPrice,
                x => x.MaxPrice,
                Money.Validate,
                inclusive: true);

            RuleFor(x => x.SortBy).MustBeOneOf(AllowedSortFields);

            RuleFor(x => x.SortDirection)
                .MustBeEnumValue<Request, SortDirection>();
        }
    }

    /// <summary>
    /// Query Handler - Read Adapter를 통한 페이지네이션 검색
    /// </summary>
    public sealed class Usecase(IProductQuery productQuery)
        : IQueryUsecase<Request, Response>
    {
        private readonly IProductQuery _productQuery = productQuery;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var spec = BuildSpecification(request);
            var pageRequest = new PageRequest(request.Page, request.PageSize);
            var sortExpression = SortExpression.By(request.SortBy, SortDirection.Parse(request.SortDirection));

            FinT<IO, Response> usecase =
                from result in _productQuery.Search(spec, pageRequest, sortExpression)
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

            request.Name.Iter(name =>
                spec &= new ProductNameSpec(
                    ProductName.Create(name).Unwrap()));

            request.MinPrice.Bind(min => request.MaxPrice.Map(max => (min, max)))
                .Iter(t => spec &= new ProductPriceRangeSpec(
                    Money.Create(t.min).Unwrap(),
                    Money.Create(t.max).Unwrap()));

            return spec;
        }
    }
}
