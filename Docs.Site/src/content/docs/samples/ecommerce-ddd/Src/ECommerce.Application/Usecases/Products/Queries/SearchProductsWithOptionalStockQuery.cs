using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using ECommerce.Application.Usecases.Products.Ports;
using ECommerce.Domain.AggregateRoots.Products;
using ECommerce.Domain.AggregateRoots.Products.Specifications;

namespace ECommerce.Application.Usecases.Products.Queries;

/// <summary>
/// 상품+선택적 재고 검색 Query - LEFT JOIN 패턴 데모
/// Product와 Inventory를 LEFT JOIN하여 재고 없는 상품도 포함.
/// </summary>
public sealed class SearchProductsWithOptionalStockQuery
{
    private static readonly string[] AllowedSortFields = ["Name", "Price", "StockQuantity"];

    public sealed record Request(
        Option<decimal> MinPrice = default,
        Option<decimal> MaxPrice = default,
        int Page = 1,
        int PageSize = PageRequest.DefaultPageSize,
        string SortBy = "",
        string SortDirection = "") : IQueryRequest<Response>;

    public sealed record Response(
        IReadOnlyList<ProductWithOptionalStockDto> Products,
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

    public sealed class Usecase(IProductWithOptionalStockQuery readAdapter)
        : IQueryUsecase<Request, Response>
    {
        private readonly IProductWithOptionalStockQuery _readAdapter = readAdapter;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var spec = BuildSpecification(request);
            var pageRequest = new PageRequest(request.Page, request.PageSize);
            var sortExpression = SortExpression.By(request.SortBy, SortDirection.Parse(request.SortDirection));

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
            var spec = Specification<Product>.All;

            request.MinPrice.Bind(min => request.MaxPrice.Map(max => (min, max)))
                .Iter(t => spec = new ProductPriceRangeSpec(
                    Money.Create(t.min).Unwrap(),
                    Money.Create(t.max).Unwrap()));

            return spec;
        }
    }
}
