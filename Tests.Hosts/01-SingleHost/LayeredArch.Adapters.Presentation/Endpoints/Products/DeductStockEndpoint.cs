using LayeredArch.Adapters.Presentation.Extensions;
using LayeredArch.Application.Usecases.Products;

namespace LayeredArch.Adapters.Presentation.Endpoints.Products;

/// <summary>
/// 재고 차감 Endpoint
/// POST /api/products/{id}/deduct-stock
/// </summary>
public sealed class DeductStockEndpoint
    : Endpoint<DeductStockEndpoint.Request, DeductStockCommand.Response>
{
    private readonly IMediator _mediator;

    public DeductStockEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("api/products/{Id}/deduct-stock");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "재고 차감";
            s.Description = "상품의 재고를 차감합니다";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new DeductStockCommand.Request(
            req.Id,
            req.Quantity);

        var result = await _mediator.Send(usecaseRequest, ct);
        await this.SendFinResponseWithNotFoundAsync(result, ct);
    }

    public sealed record Request(
        string Id,
        int Quantity);
}
