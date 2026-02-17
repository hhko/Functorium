using LayeredArch.Adapters.Presentation.Abstractions.Extensions;
using LayeredArch.Application.Usecases.Products;

namespace LayeredArch.Adapters.Presentation.Endpoints.Products;

/// <summary>
/// 상품 업데이트 Endpoint
/// PUT /api/products/{id}
/// </summary>
public sealed class UpdateProductEndpoint
    : Endpoint<UpdateProductEndpoint.Request, UpdateProductEndpoint.Response>
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
            SimulateException: false);

        var result = await _mediator.Send(usecaseRequest, ct);
        var mapped = result.Map(r => new Response(
            r.ProductId, r.Name, r.Description, r.Price, r.UpdatedAt));
        await this.SendFinResponseWithNotFoundAsync(mapped, ct);
    }

    public sealed record Request(
        string Id,
        string Name,
        string Description,
        decimal Price);

    /// <summary>
    /// Endpoint Response DTO
    /// </summary>
    public new sealed record Response(
        string ProductId,
        string Name,
        string Description,
        decimal Price,
        DateTime UpdatedAt);
}
