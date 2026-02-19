using Functorium.Applications.Queries;
using LayeredArch.Adapters.Presentation.Abstractions.Extensions;
using LayeredArch.Application.Usecases.Products.Ports;
using LayeredArch.Application.Usecases.Products.Queries;

namespace LayeredArch.Adapters.Presentation.Endpoints.Products;

/// <summary>
/// 상품+재고 검색 Endpoint (페이지네이션/정렬 지원)
/// GET /api/products/with-stock
/// </summary>
public sealed class SearchProductsWithStockEndpoint
    : Endpoint<SearchProductsWithStockEndpoint.Request, SearchProductsWithStockEndpoint.Response>
{
    private readonly IMediator _mediator;

    public SearchProductsWithStockEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("api/products/with-stock");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "상품+재고 검색";
            s.Description = "가격 범위, 페이지네이션, 정렬을 지원하는 상품+재고 검색 (Dapper JOIN 데모)";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new SearchProductsWithStockQuery.Request(
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
        [property: QueryParam] decimal? MinPrice = null,
        [property: QueryParam] decimal? MaxPrice = null,
        [property: QueryParam] int? Page = null,
        [property: QueryParam] int? PageSize = null,
        [property: QueryParam] string? SortBy = null,
        [property: QueryParam] string? SortDirection = null);

    public new sealed record Response(
        List<ProductWithStockDto> Products,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages,
        bool HasNextPage,
        bool HasPreviousPage);
}
