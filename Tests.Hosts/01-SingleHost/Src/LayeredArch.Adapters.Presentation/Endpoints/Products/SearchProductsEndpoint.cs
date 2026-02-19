using Functorium.Applications.Queries;
using LayeredArch.Adapters.Presentation.Abstractions.Extensions;
using LayeredArch.Application.Usecases.Products.Ports;
using LayeredArch.Application.Usecases.Products.Queries;

namespace LayeredArch.Adapters.Presentation.Endpoints.Products;

/// <summary>
/// 상품 검색 Endpoint (페이지네이션/정렬 지원)
/// GET /api/products/search
/// </summary>
public sealed class SearchProductsEndpoint
    : Endpoint<SearchProductsEndpoint.Request, SearchProductsEndpoint.Response>
{
    private readonly IMediator _mediator;

    public SearchProductsEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("api/products/search");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "상품 검색";
            s.Description = "가격 범위, 페이지네이션, 정렬을 지원하는 상품 검색";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new SearchProductsQuery.Request(
            req.Name ?? "",
            req.MinPrice ?? 0,
            req.MaxPrice ?? 0,
            req.Page ?? 1,
            req.PageSize ?? PageRequest.DefaultPageSize,
            req.SortBy ?? "",
            req.SortDirection ?? "");

        var result = await _mediator.Send(usecaseRequest, ct);

        var mapped = result.Map(r => new Response(
            r.Products.ToList(),
            r.TotalCount,
            r.Page,
            r.PageSize,
            r.TotalPages,
            r.HasNextPage,
            r.HasPreviousPage));

        await this.SendFinResponseAsync(mapped, ct);
    }

    public sealed record Request(
        [property: QueryParam] string? Name = null,
        [property: QueryParam] decimal? MinPrice = null,
        [property: QueryParam] decimal? MaxPrice = null,
        [property: QueryParam] int? Page = null,
        [property: QueryParam] int? PageSize = null,
        [property: QueryParam] string? SortBy = null,
        [property: QueryParam] string? SortDirection = null);

    public new sealed record Response(
        List<ProductSummaryDto> Products,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages,
        bool HasNextPage,
        bool HasPreviousPage);
}
