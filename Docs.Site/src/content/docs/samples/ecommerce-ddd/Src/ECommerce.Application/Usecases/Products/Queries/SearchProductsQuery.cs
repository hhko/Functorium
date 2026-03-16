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
                .MustSatisfyValidation(ProductName.Validate)
                .When(x => x.Name.Length > 0);

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

            if (request.Name.Length > 0)
                spec &= new ProductNameSpec(
                    ProductName.Create(request.Name).ThrowIfFail());

            if (request.MinPrice > 0)
                spec &= new ProductPriceRangeSpec(
                    Money.Create(request.MinPrice).ThrowIfFail(),
                    Money.Create(request.MaxPrice).ThrowIfFail());

            return spec;
        }
    }
}
