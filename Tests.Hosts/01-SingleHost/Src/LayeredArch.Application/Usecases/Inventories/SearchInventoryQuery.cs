using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Inventories.Dtos;
using LayeredArch.Application.Usecases.Inventories.Ports;
using LayeredArch.Domain.AggregateRoots.Inventories;
using LayeredArch.Domain.AggregateRoots.Inventories.Specifications;

namespace LayeredArch.Application.Usecases.Inventories;

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
        int? LowStockThreshold = null,
        int? Page = null,
        int? PageSize = null,
        string? SortBy = null,
        string? SortDirection = null) : IQueryRequest<Response>;

    /// <summary>
    /// Query Response - 페이지네이션된 검색 결과
    /// </summary>
    public sealed record Response(
        Seq<InventorySummaryDto> Inventories,
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
                .GreaterThan(0).When(x => x.LowStockThreshold.HasValue)
                .WithMessage("재고 부족 임계값은 0보다 커야 합니다");

            RuleFor(x => x.SortBy)
                .Must(sortBy => AllowedSortFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
                .When(x => x.SortBy is not null)
                .WithMessage($"정렬 필드는 {string.Join(", ", AllowedSortFields)} 중 하나여야 합니다");

            RuleFor(x => x.SortDirection)
                .Must(dir => string.Equals(dir, "asc", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase))
                .When(x => x.SortDirection is not null)
                .WithMessage("정렬 방향은 'asc' 또는 'desc'여야 합니다");
        }
    }

    /// <summary>
    /// Query Handler - Read Adapter를 통한 페이지네이션 검색
    /// </summary>
    public sealed class Usecase(IInventoryQueryAdapter readAdapter)
        : IQueryUsecase<Request, Response>
    {
        private readonly IInventoryQueryAdapter _readAdapter = readAdapter;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var spec = BuildSpecification(request);
            var pageRequest = new PageRequest(
                request.Page ?? 1,
                request.PageSize ?? PageRequest.DefaultPageSize);
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

        private static Specification<Inventory>? BuildSpecification(Request request)
        {
            if (request.LowStockThreshold.HasValue)
            {
                return new InventoryLowStockSpec(
                    Quantity.Create(request.LowStockThreshold.Value).ThrowIfFail());
            }

            return null;
        }

        private static SortExpression BuildSortExpression(Request request)
        {
            if (request.SortBy is null)
                return SortExpression.Empty;

            var direction = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
                ? Functorium.Applications.Queries.SortDirection.Descending
                : Functorium.Applications.Queries.SortDirection.Ascending;

            return SortExpression.By(request.SortBy, direction);
        }
    }
}
