using LayeredArch.Adapters.Presentation.Abstractions.Extensions;
using LayeredArch.Application.Usecases.Products.Ports;
using LayeredArch.Application.Usecases.Products.Queries;

namespace LayeredArch.Adapters.Presentation.Endpoints.Products;

/// <summary>
/// 전체 상품 조회 Endpoint
/// GET /api/products
/// </summary>
public sealed class GetAllProductsEndpoint
    : EndpointWithoutRequest<GetAllProductsEndpoint.Response>
{
    private readonly IMediator _mediator;

    public GetAllProductsEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("api/products");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "전체 상품 조회";
            s.Description = "모든 상품 목록을 조회합니다";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAllProductsQuery.Request(), ct);
        var mapped = result.Map(r => new Response(r.Products.ToList()));
        await this.SendFinResponseAsync(mapped, ct);
    }

    /// <summary>
    /// Endpoint Response DTO
    /// </summary>
    public new sealed record Response(List<ProductSummaryDto> Products);
}
