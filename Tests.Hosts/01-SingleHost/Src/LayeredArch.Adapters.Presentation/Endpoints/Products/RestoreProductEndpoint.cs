using LayeredArch.Adapters.Presentation.Abstractions.Extensions;
using LayeredArch.Application.Usecases.Products.Commands;

namespace LayeredArch.Adapters.Presentation.Endpoints.Products;

/// <summary>
/// 삭제된 상품 복원 Endpoint
/// POST /api/products/{id}/restore
/// </summary>
public sealed class RestoreProductEndpoint
    : Endpoint<RestoreProductEndpoint.Request, RestoreProductEndpoint.Response>
{
    private readonly IMediator _mediator;

    public RestoreProductEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("api/products/{Id}/restore");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "삭제된 상품 복원";
            s.Description = "소프트 삭제된 상품을 복원합니다";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new RestoreProductCommand.Request(req.Id);

        var result = await _mediator.Send(usecaseRequest, ct);
        var mapped = result.Map(r => new Response(r.ProductId, r.Name, r.Price));
        await this.SendFinResponseWithNotFoundAsync(mapped, ct);
    }

    public sealed record Request(
        string Id);

    /// <summary>
    /// Endpoint Response DTO
    /// </summary>
    public new sealed record Response(
        string ProductId,
        string Name,
        decimal Price);
}
