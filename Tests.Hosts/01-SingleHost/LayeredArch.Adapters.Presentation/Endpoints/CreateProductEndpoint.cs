using LayeredArch.Adapters.Presentation.Extensions;
using LayeredArch.Application.Commands;
using FastEndpoints;
using Mediator;

namespace LayeredArch.Adapters.Presentation.Endpoints;

/// <summary>
/// 상품 생성 Endpoint
/// POST /api/products
/// </summary>
public sealed class CreateProductEndpoint
    : Endpoint<CreateProductEndpoint.Request, CreateProductCommand.Response>
{
    private readonly IMediator _mediator;

    public CreateProductEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("api/products");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "상품 생성";
            s.Description = "새로운 상품을 생성합니다";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        // Endpoint Request -> Usecase Request 변환
        var usecaseRequest = new CreateProductCommand.Request(
            req.Name,
            req.Description,
            req.Price,
            req.StockQuantity);

        // Mediator로 Usecase 호출
        var result = await _mediator.Send(usecaseRequest, ct);

        // FinResponse를 HTTP Response로 변환
        await this.SendCreatedFinResponseAsync(result, ct);
    }

    /// <summary>
    /// Endpoint Request DTO
    /// </summary>
    public sealed record Request(
        string Name,
        string Description,
        decimal Price,
        int StockQuantity);
}
