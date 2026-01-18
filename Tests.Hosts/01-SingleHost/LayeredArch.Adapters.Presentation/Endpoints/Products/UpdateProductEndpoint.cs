using LayeredArch.Adapters.Presentation.Extensions;
using LayeredArch.Application.Usecases.Products;

namespace LayeredArch.Adapters.Presentation.Endpoints.Products;

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
        Put("api/products/{Id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "상품 업데이트";
            s.Description = "기존 상품 정보를 업데이트합니다";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new UpdateProductCommand.Request(
            req.Id,
            req.Name,
            req.Description,
            req.Price,
            req.StockQuantity,
            SimulateException: false);

        var result = await _mediator.Send(usecaseRequest, ct);
        await this.SendFinResponseWithNotFoundAsync(result, ct);
    }

    public sealed record Request(
        Guid Id,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity);
}
