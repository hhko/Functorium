using FastEndpoints;
using Mediator;
using TwoWayMappingLayered.Adapters.Presentation.Extensions;
using TwoWayMappingLayered.Applications.Commands;

namespace TwoWayMappingLayered.Adapters.Presentation.Endpoints;

/// <summary>
/// 상품 업데이트 Endpoint
/// PUT /api/products/{id}
/// </summary>
public sealed class UpdateProductEndpoint
    : Endpoint<UpdateProductEndpoint.Request, UpdateProductCommand.Response>
{
    private readonly IMediator _mediator;

    public UpdateProductEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("api/products/{ProductId}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "상품 업데이트";
            s.Description = "기존 상품 정보를 수정합니다";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new UpdateProductCommand.Request(
            req.ProductId,
            req.Name,
            req.Description,
            req.Price,
            req.Currency,
            req.StockQuantity);

        var result = await _mediator.Send(usecaseRequest, ct);

        await this.SendFinResponseAsync(result, ct);
    }

    public sealed record Request(
        Guid ProductId,
        string Name,
        string Description,
        decimal Price,
        string Currency,
        int StockQuantity);
}
