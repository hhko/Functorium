using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Products.Ports;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.AggregateRoots.Products.Specifications;

namespace LayeredArch.Application.Usecases.Products.Queries;

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
        string Name = "",
        decimal MinPrice = 0,
        decimal MaxPrice = 0,
        int Page = 1,
        int PageSize = PageRequest.DefaultPageSize,
        string SortBy = "",
        string SortDirection = "") : IQueryRequest<Response>;

    /// <summary>
    /// Query Response - 페이지네이션된 검색 결과
    /// </summary>
    public sealed record Response(
        Seq<ProductSummaryDto> Products,
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
                .MustSatisfyValidation(ProductName.Validate)
                .When(x => x.Name.Length > 0);

            RuleFor(x => x.MinPrice)
                .MustSatisfyValidation(Money.Validate)
                .When(x => x.MinPrice > 0);

            RuleFor(x => x.MinPrice)
                .Must(min => min > 0).When(x => x.MaxPrice > 0)
                .WithMessage("MinPrice is required when MaxPrice is specified");

            RuleFor(x => x.MaxPrice)
                .MustSatisfyValidation(Money.Validate)
                .When(x => x.MaxPrice > 0);

            RuleFor(x => x.MaxPrice)
                .Must(max => max > 0).When(x => x.MinPrice > 0)
                .WithMessage("MaxPrice is required when MinPrice is specified");

            RuleFor(x => x.MaxPrice)
                .GreaterThanOrEqualTo(x => x.MinPrice)
                .When(x => x.MinPrice > 0 && x.MaxPrice > 0)
                .WithMessage("MaxPrice must be greater than or equal to MinPrice");

            RuleFor(x => x.SortBy)
                .Must(sortBy => AllowedSortFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
                .When(x => x.SortBy.Length > 0)
                .WithMessage($"SortBy must be one of: {string.Join(", ", AllowedSortFields)}");

            RuleFor(x => x.SortDirection)
                .Must(dir => string.Equals(dir, "asc", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase))
                .When(x => x.SortDirection.Length > 0)
                .WithMessage("SortDirection must be 'asc' or 'desc'");
        }
    }

    /// <summary>
    /// Query Handler - Read Adapter를 통한 페이지네이션 검색
    /// </summary>
    public sealed class Usecase(IProductQueryAdapter productQuery)
        : IQueryUsecase<Request, Response>
    {
        private readonly IProductQueryAdapter _productQuery = productQuery;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var spec = BuildSpecification(request);
            var pageRequest = new PageRequest(request.Page, request.PageSize);
            var sortExpression = BuildSortExpression(request);

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

        private static Specification<Product>? BuildSpecification(Request request)
        {
            Specification<Product>? spec = null;

            if (request.Name.Length > 0)
            {
                spec = new ProductNameSpec(
                    ProductName.Create(request.Name).ThrowIfFail());
            }

            if (request.MinPrice > 0 && request.MaxPrice > 0)
            {
                var priceSpec = new ProductPriceRangeSpec(
                    Money.Create(request.MinPrice).ThrowIfFail(),
                    Money.Create(request.MaxPrice).ThrowIfFail());

                spec = spec is not null ? spec & priceSpec : priceSpec;
            }

            return spec;
        }

        private static SortExpression BuildSortExpression(Request request)
        {
            if (request.SortBy.Length == 0)
                return SortExpression.Empty;

            var direction = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
                ? Functorium.Applications.Queries.SortDirection.Descending
                : Functorium.Applications.Queries.SortDirection.Ascending;

            return SortExpression.By(request.SortBy, direction);
        }
    }
}
