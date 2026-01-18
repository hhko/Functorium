using LayeredArch.Adapters.Presentation.Extensions;
using LayeredArch.Application.Queries;
using FastEndpoints;
using Mediator;

namespace LayeredArch.Adapters.Presentation.Endpoints;

/// <summary>
/// 상품 ID로 조회 Endpoint
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
        Get("api/products/{Id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "상품 조회";
            s.Description = "ID로 상품을 조회합니다";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new GetProductByIdQuery.Request(req.Id);
        var result = await _mediator.Send(usecaseRequest, ct);
        await this.SendFinResponseWithNotFoundAsync(result, ct);
    }

    public sealed record Request(Guid Id);
}
