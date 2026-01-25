using FastEndpoints;
using Mediator;
using TwoWayMappingLayered.Adapters.Presentation.Extensions;
using TwoWayMappingLayered.Applications.Queries;

namespace TwoWayMappingLayered.Adapters.Presentation.Endpoints;

/// <summary>
/// 상품 전체 조회 Endpoint
/// GET /api/products
/// </summary>
public sealed class GetAllProductsEndpoint
    : EndpointWithoutRequest<GetAllProductsQuery.Response>
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
            s.Summary = "상품 전체 조회";
            s.Description = "모든 상품을 조회합니다";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var usecaseRequest = new GetAllProductsQuery.Request();
        var result = await _mediator.Send(usecaseRequest, ct);

        await this.SendFinResponseAsync(result, ct);
    }
}
