using LayeredArch.Adapters.Presentation.Abstractions.Extensions;
using LayeredArch.Application.Usecases.Products.Commands;

namespace LayeredArch.Adapters.Presentation.Endpoints.Products;

/// <summary>
/// 상품 벌크 삭제 Endpoint
/// POST /api/products/bulk-delete
/// </summary>
public sealed class BulkDeleteProductsEndpoint
    : Endpoint<BulkDeleteProductsEndpoint.Request, BulkDeleteProductsEndpoint.Response>
{
    private readonly IMediator _mediator;

    public BulkDeleteProductsEndpoint(IMediator mediator) => _mediator = mediator;

    public override void Configure()
    {
        Post("api/products/bulk-delete");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "상품 벌크 삭제";
            s.Description = "여러 상품을 한 번에 삭제합니다 (N * DeletedEvent + IDomainEventBatchHandler 데모)";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new BulkDeleteProductsCommand.Request(req.ProductIds);
        var result = await _mediator.Send(usecaseRequest, ct);
        var mapped = result.Map(r => new Response(r.AffectedCount));
        await this.SendFinResponseAsync(mapped, ct);
    }

    public sealed record Request(List<string> ProductIds);
    public new sealed record Response(int AffectedCount);
}
