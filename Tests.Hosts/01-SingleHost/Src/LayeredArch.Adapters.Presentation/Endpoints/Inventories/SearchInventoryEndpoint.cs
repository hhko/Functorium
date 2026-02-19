using LayeredArch.Adapters.Presentation.Abstractions.Extensions;
using LayeredArch.Application.Usecases.Inventories.Ports;
using LayeredArch.Application.Usecases.Inventories.Queries;

namespace LayeredArch.Adapters.Presentation.Endpoints.Inventories;

/// <summary>
/// 재고 검색 Endpoint (페이지네이션/정렬 지원)
/// GET /api/inventories/search
/// </summary>
public sealed class SearchInventoryEndpoint
    : Endpoint<SearchInventoryEndpoint.Request, SearchInventoryEndpoint.Response>
{
    private readonly IMediator _mediator;

    public SearchInventoryEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("api/inventories/search");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "재고 검색";
            s.Description = "재고 부족 필터, 페이지네이션, 정렬을 지원하는 재고 검색";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new SearchInventoryQuery.Request(
            req.LowStockThreshold ?? 0,
            req.Page ?? 1,
            req.PageSize ?? Functorium.Applications.Queries.PageRequest.DefaultPageSize,
            req.SortBy ?? "",
            req.SortDirection ?? "");

        var result = await _mediator.Send(usecaseRequest, ct);

        var mapped = result.Map(r => new Response(
            r.Inventories.ToList(),
            r.TotalCount,
            r.Page,
            r.PageSize,
            r.TotalPages,
            r.HasNextPage,
            r.HasPreviousPage));

        await this.SendFinResponseAsync(mapped, ct);
    }

    public sealed record Request(
        [property: QueryParam] int? LowStockThreshold = null,
        [property: QueryParam] int? Page = null,
        [property: QueryParam] int? PageSize = null,
        [property: QueryParam] string? SortBy = null,
        [property: QueryParam] string? SortDirection = null);

    public new sealed record Response(
        List<InventorySummaryDto> Inventories,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages,
        bool HasNextPage,
        bool HasPreviousPage);
}
