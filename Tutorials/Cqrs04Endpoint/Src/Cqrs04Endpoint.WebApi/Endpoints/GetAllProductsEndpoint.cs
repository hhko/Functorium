using Cqrs04Endpoint.WebApi.Extensions;
using Cqrs04Endpoint.WebApi.Usecases;
using FastEndpoints;
using Mediator;

namespace Cqrs04Endpoint.WebApi.Endpoints;

/// <summary>
/// 전체 상품 조회 Endpoint
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
            s.Summary = "전체 상품 조회";
            s.Description = "모든 상품 목록을 조회합니다";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAllProductsQuery.Request(), ct);
        await this.SendFinResponseAsync(result, ct);
    }
}
