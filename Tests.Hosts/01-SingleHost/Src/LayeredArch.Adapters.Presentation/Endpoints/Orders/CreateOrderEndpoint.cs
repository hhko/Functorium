using LayeredArch.Adapters.Presentation.Abstractions.Extensions;
using LayeredArch.Application.Usecases.Orders;

namespace LayeredArch.Adapters.Presentation.Endpoints.Orders;

/// <summary>
/// 주문 생성 Endpoint
/// POST /api/orders
/// </summary>
public sealed class CreateOrderEndpoint
    : Endpoint<CreateOrderEndpoint.Request, CreateOrderCommand.Response>
{
    private readonly IMediator _mediator;

    public CreateOrderEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("api/orders");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "주문 생성";
            s.Description = "새로운 주문을 생성합니다";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new CreateOrderCommand.Request(
            req.ProductId,
            req.Quantity,
            req.ShippingAddress);

        var result = await _mediator.Send(usecaseRequest, ct);
        await this.SendCreatedFinResponseAsync(result, ct);
    }

    public sealed record Request(
        string ProductId,
        int Quantity,
        string ShippingAddress);
}
