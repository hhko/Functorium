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
        decimal MinPrice = 0,
        decimal MaxPrice = 0,
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
            // --- 가격 범위 필터 (선택적 쌍) ---
            // MinPrice/MaxPrice는 기본값 0 = 미제공.
            // 범위 필터이므로 반드시 함께 제공되거나 함께 생략되어야 한다.
            // 통과 후 보장: 둘 다 0(미제공) 또는 둘 다 유효한 Money이며 MaxPrice >= MinPrice.

            // 값 형식 검증: 제공된 경우에만 유효한 Money인지 검사
            RuleFor(x => x.MinPrice)
                .MustSatisfyValidation(Money.Validate)
                .When(x => x.MinPrice > 0);

            RuleFor(x => x.MaxPrice)
                .MustSatisfyValidation(Money.Validate)
                .When(x => x.MaxPrice > 0);

            // 쌍 제약: 한쪽만 제공하면 실패
            RuleFor(x => x.MinPrice)
                .Must(min => min > 0).When(x => x.MaxPrice > 0)
                .WithMessage("MinPrice is required when MaxPrice is specified");

            RuleFor(x => x.MaxPrice)
                .Must(max => max > 0).When(x => x.MinPrice > 0)
                .WithMessage("MaxPrice is required when MinPrice is specified");

            // 범위 제약: 둘 다 제공된 경우 MaxPrice >= MinPrice
            // 양쪽 모두 확인하는 이유: FluentValidation은 규칙을 독립 실행하므로
            // 쌍 제약이 실패해도 이 규칙은 실행된다. 한쪽만 확인하면
            // MinPrice=100, MaxPrice=0(미제공)일 때 "0 >= 100" 비교가 발생한다.
            RuleFor(x => x.MaxPrice)
                .GreaterThanOrEqualTo(x => x.MinPrice)
                .When(x => x.MinPrice > 0 && x.MaxPrice > 0)
                .WithMessage("MaxPrice must be greater than or equal to MinPrice");

            RuleFor(x => x.SortBy).MustBeOneOf(AllowedSortFields);

            RuleFor(x => x.SortDirection)
                .MustBeEnumValue<Request, SortDirection>()
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
            if (request.MinPrice > 0)
            {
                return new ProductPriceRangeSpec(
                    Money.Create(request.MinPrice).ThrowIfFail(),
                    Money.Create(request.MaxPrice).ThrowIfFail());
            }

            return Specification<Product>.All;
        }
    }
}
