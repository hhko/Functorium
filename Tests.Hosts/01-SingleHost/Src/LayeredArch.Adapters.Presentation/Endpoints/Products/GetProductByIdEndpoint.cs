using LanguageExt;
using LayeredArch.Adapters.Presentation.Abstractions.Extensions;
using LayeredArch.Application.Usecases.Products.Queries;

namespace LayeredArch.Adapters.Presentation.Endpoints.Products;

/// <summary>
/// 상품 ID로 조회 Endpoint
/// GET /api/products/{id}
/// </summary>
public sealed class GetProductByIdEndpoint
    : Endpoint<GetProductByIdEndpoint.Request, GetProductByIdEndpoint.Response>
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
        var mapped = result.Map(r => new Response(
            r.ProductId, r.Name, r.Description, r.Price, r.CreatedAt, r.UpdatedAt.ToNullable()));
        await this.SendFinResponseWithNotFoundAsync(mapped, ct);
    }

    public sealed record Request(string Id);

    /// <summary>
    /// Endpoint Response DTO
    /// </summary>
    public new sealed record Response(
        string ProductId,
        string Name,
        string Description,
        decimal Price,
        DateTime CreatedAt,
        DateTime? UpdatedAt);
}
