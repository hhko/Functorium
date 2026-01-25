using FastEndpoints;
using Mediator;
using TwoWayMappingLayered.Adapters.Presentation.Extensions;
using TwoWayMappingLayered.Applications.Commands;

namespace TwoWayMappingLayered.Adapters.Presentation.Endpoints;

/// <summary>
/// 상품 생성 Endpoint
/// POST /api/products
///
/// Two-Way Mapping 특징:
/// - Endpoint Request DTO를 Application Request로 변환
/// - Application이 Domain 객체를 사용하여 비즈니스 로직 처리
/// - 결과에서 비즈니스 메서드(FormattedPrice) 직접 사용 가능
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
            s.Description = "새로운 상품을 생성합니다 (Two-Way Mapping 전략)";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        // Endpoint Request -> Usecase Request 변환
        var usecaseRequest = new CreateProductCommand.Request(
            req.Name,
            req.Description,
            req.Price,
            req.Currency,
            req.StockQuantity);

        // Mediator로 Usecase 호출
        var result = await _mediator.Send(usecaseRequest, ct);

        // FinResponse를 HTTP Response로 변환
        await this.SendCreatedFinResponseAsync(result, ct);
    }

    /// <summary>
    /// Endpoint Request DTO
    /// Two-Way Mapping: Presentation 레이어 전용 DTO
    /// </summary>
    public sealed record Request(
        string Name,
        string Description,
        decimal Price,
        string Currency,
        int StockQuantity);
}
