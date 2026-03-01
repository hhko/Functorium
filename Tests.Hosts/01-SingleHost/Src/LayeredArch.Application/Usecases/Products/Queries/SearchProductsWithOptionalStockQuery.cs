using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Products.Ports;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.AggregateRoots.Products.Specifications;

namespace LayeredArch.Application.Usecases.Products.Queries;

/// <summary>
/// 상품+선택적 재고 검색 Query - LEFT JOIN 패턴 데모
/// Product와 Inventory를 LEFT JOIN하여 재고 없는 상품도 포함.
/// </summary>
public sealed class SearchProductsWithOptionalStockQuery
{
    private static readonly string[] AllowedSortFields = ["Name", "Price", "StockQuantity"];

    public sealed record Request(
        decimal MinPrice = 0,
        decimal MaxPrice = 0,
        int Page = 1,
        int PageSize = PageRequest.DefaultPageSize,
        string SortBy = "",
        string SortDirection = "") : IQueryRequest<Response>;

    public sealed record Response(
        Seq<ProductWithOptionalStockDto> Products,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages,
        bool HasNextPage,
        bool HasPreviousPage);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
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
                .MustBeEnumValue<Request, Functorium.Applications.Queries.SortDirection>()
                .When(x => x.SortDirection.Length > 0);
        }
    }

    public sealed class Usecase(IProductWithOptionalStockQuery readAdapter)
        : IQueryUsecase<Request, Response>
    {
        private readonly IProductWithOptionalStockQuery _readAdapter = readAdapter;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var spec = BuildSpecification(request);
            var pageRequest = new PageRequest(request.Page, request.PageSize);
            var sortExpression = BuildSortExpression(request);

            FinT<IO, Response> usecase =
                from result in _readAdapter.Search(spec, pageRequest, sortExpression)
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
            if (request.MinPrice > 0 && request.MaxPrice > 0)
            {
                return new ProductPriceRangeSpec(
                    Money.Create(request.MinPrice).ThrowIfFail(),
                    Money.Create(request.MaxPrice).ThrowIfFail());
            }

            return Specification<Product>.All;
        }

        private static SortExpression BuildSortExpression(Request request)
        {
            if (request.SortBy.Length == 0)
                return SortExpression.Empty;

            return SortExpression.By(request.SortBy, Functorium.Applications.Queries.SortDirection.Parse(request.SortDirection));
        }
    }
}
