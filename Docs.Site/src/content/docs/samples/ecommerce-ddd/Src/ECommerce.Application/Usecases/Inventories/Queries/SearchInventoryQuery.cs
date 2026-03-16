using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using ECommerce.Application.Usecases.Inventories.Ports;
using ECommerce.Domain.AggregateRoots.Inventories;
using ECommerce.Domain.AggregateRoots.Inventories.Specifications;

namespace ECommerce.Application.Usecases.Inventories.Queries;

/// <summary>
/// 재고 검색 Query - 페이지네이션/정렬 + LowStock 필터 지원
/// Read Adapter를 통해 Aggregate 재구성 없이 DTO로 직접 프로젝션합니다.
/// </summary>
public sealed class SearchInventoryQuery
{
    private static readonly string[] AllowedSortFields = ["StockQuantity", "ProductId"];

    /// <summary>
    /// Query Request - 선택적 검색 필터 + 페이지네이션/정렬
    /// </summary>
    public sealed record Request(
        Option<int> LowStockThreshold = default,
        int Page = 1,
        int PageSize = PageRequest.DefaultPageSize,
        string SortBy = "",
        string SortDirection = "") : IQueryRequest<Response>;

    /// <summary>
    /// Query Response - 페이지네이션된 검색 결과
    /// </summary>
    public sealed record Response(
        IReadOnlyList<InventorySummaryDto> Inventories,
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
            RuleFor(x => x.LowStockThreshold)
                .MustSatisfyValidation(Quantity.Validate);

            RuleFor(x => x.SortBy).MustBeOneOf(AllowedSortFields);

            RuleFor(x => x.SortDirection)
                .MustBeEnumValue<Request, Functorium.Applications.Queries.SortDirection>();
        }
    }

    /// <summary>
    /// Query Handler - Read Adapter를 통한 페이지네이션 검색
    /// </summary>
    public sealed class Usecase(IInventoryQuery readAdapter)
        : IQueryUsecase<Request, Response>
    {
        private readonly IInventoryQuery _readAdapter = readAdapter;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var spec = BuildSpecification(request);
            var pageRequest = new PageRequest(request.Page, request.PageSize);
            var sortExpression = SortExpression.By(request.SortBy, Functorium.Applications.Queries.SortDirection.Parse(request.SortDirection));

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

        private static Specification<Inventory> BuildSpecification(Request request)
        {
            var spec = Specification<Inventory>.All;

            request.LowStockThreshold.Iter(threshold =>
                spec = new InventoryLowStockSpec(
                    Quantity.Create(threshold).ThrowIfFail()));

            return spec;
        }
    }
}
