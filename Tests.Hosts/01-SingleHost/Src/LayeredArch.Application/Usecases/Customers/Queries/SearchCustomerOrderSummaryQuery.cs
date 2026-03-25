using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Customers.Ports;
using LayeredArch.Domain.AggregateRoots.Customers;

namespace LayeredArch.Application.Usecases.Customers.Queries;

/// <summary>
/// 고객 주문 요약 검색 Query - LEFT JOIN + GROUP BY 집계 패턴 데모
/// 고객별 총 주문 수, 총 지출, 마지막 주문일을 집계합니다.
/// </summary>
public sealed class SearchCustomerOrderSummaryQuery
{
    private static readonly string[] AllowedSortFields = ["CustomerName", "OrderCount", "TotalSpent", "LastOrderDate"];

    public sealed record Request(
        int Page = 1,
        int PageSize = PageRequest.DefaultPageSize,
        string SortBy = "",
        string SortDirection = "") : IQueryRequest<Response>;

    public sealed record Response(
        IReadOnlyList<CustomerOrderSummaryDto> Customers,
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
            RuleFor(x => x.SortBy)
                .Must(sortBy => AllowedSortFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
                .When(x => x.SortBy.Length > 0)
                .WithMessage($"SortBy must be one of: {string.Join(", ", AllowedSortFields)}");

            RuleFor(x => x.SortDirection)
                .MustBeEnumValue<Request, Functorium.Applications.Queries.SortDirection>()
                .When(x => x.SortDirection.Length > 0);
        }
    }

    public sealed class Usecase(ICustomerOrderSummaryQuery readAdapter)
        : IQueryUsecase<Request, Response>
    {
        private readonly ICustomerOrderSummaryQuery _readAdapter = readAdapter;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var spec = Specification<Customer>.All;
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

        private static SortExpression BuildSortExpression(Request request)
        {
            if (request.SortBy.Length == 0)
                return SortExpression.Empty;

            return SortExpression.By(request.SortBy, Functorium.Applications.Queries.SortDirection.Parse(request.SortDirection));
        }
    }
}
