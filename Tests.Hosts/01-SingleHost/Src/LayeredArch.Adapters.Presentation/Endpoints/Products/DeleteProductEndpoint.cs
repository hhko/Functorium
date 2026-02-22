using LayeredArch.Adapters.Presentation.Abstractions.Extensions;
using LayeredArch.Application.Usecases.Products.Commands;

namespace LayeredArch.Adapters.Presentation.Endpoints.Products;

/// <summary>
/// 상품 삭제 Endpoint (Soft Delete)
/// DELETE /api/products/{id}
/// </summary>
public sealed class DeleteProductEndpoint
    : Endpoint<DeleteProductEndpoint.Request, DeleteProductEndpoint.Response>
{
    private readonly IMediator _mediator;

    public DeleteProductEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Delete("api/products/{Id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "상품 삭제";
            s.Description = "상품을 소프트 삭제합니다";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new DeleteProductCommand.Request(
            req.Id,
            req.DeletedBy ?? "system");

        var result = await _mediator.Send(usecaseRequest, ct);
        var mapped = result.Map(r => new Response(r.ProductId));
        await this.SendFinResponseWithNotFoundAsync(mapped, ct);
    }

    public sealed record Request(
        string Id,
        string? DeletedBy);

    /// <summary>
    /// Endpoint Response DTO
    /// </summary>
    public new sealed record Response(
        string ProductId);
}
