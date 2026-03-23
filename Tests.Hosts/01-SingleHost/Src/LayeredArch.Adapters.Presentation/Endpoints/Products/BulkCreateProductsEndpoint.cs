using LayeredArch.Adapters.Presentation.Abstractions.Extensions;
using LayeredArch.Application.Usecases.Products.Commands;

namespace LayeredArch.Adapters.Presentation.Endpoints.Products;

/// <summary>
/// 상품 벌크 생성 Endpoint
/// POST /api/products/bulk
/// </summary>
public sealed class BulkCreateProductsEndpoint
    : Endpoint<BulkCreateProductsEndpoint.Request, BulkCreateProductsEndpoint.Response>
{
    private readonly IMediator _mediator;

    public BulkCreateProductsEndpoint(IMediator mediator) => _mediator = mediator;

    public override void Configure()
    {
        Post("api/products/bulk");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "상품 벌크 생성";
            s.Description = "여러 상품을 한 번에 생성합니다 (CreateRange + BatchHandler 데모)";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new BulkCreateProductsCommand.Request(
            req.Products.Select(p =>
                new BulkCreateProductsCommand.ProductItem(p.Name, p.Description, p.Price, p.StockQuantity))
                .ToList());

        var result = await _mediator.Send(usecaseRequest, ct);
        var mapped = result.Map(r => new Response(r.CreatedCount, r.ProductIds));
        await this.SendCreatedFinResponseAsync(mapped, ct);
    }

    public sealed record ProductItem(string Name, string Description, decimal Price, int StockQuantity);
    public sealed record Request(List<ProductItem> Products);
    public new sealed record Response(int CreatedCount, List<string> ProductIds);
}
