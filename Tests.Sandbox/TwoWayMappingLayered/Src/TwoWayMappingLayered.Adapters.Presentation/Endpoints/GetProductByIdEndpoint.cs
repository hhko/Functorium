using FastEndpoints;
using Mediator;
using TwoWayMappingLayered.Adapters.Presentation.Extensions;
using TwoWayMappingLayered.Applications.Queries;

namespace TwoWayMappingLayered.Adapters.Presentation.Endpoints;

/// <summary>
/// 상품 단건 조회 Endpoint
/// GET /api/products/{id}
/// </summary>
public sealed class GetProductByIdEndpoint
    : Endpoint<GetProductByIdEndpoint.Request, GetProductByIdQuery.Response>
{
    private readonly IMediator _mediator;

    public GetProductByIdEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("api/products/{ProductId}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "상품 단건 조회";
            s.Description = "ID로 상품을 조회합니다";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new GetProductByIdQuery.Request(req.ProductId);
        var result = await _mediator.Send(usecaseRequest, ct);

        await this.SendFinResponseAsync(result, ct);
    }

    public sealed record Request(Guid ProductId);
}
