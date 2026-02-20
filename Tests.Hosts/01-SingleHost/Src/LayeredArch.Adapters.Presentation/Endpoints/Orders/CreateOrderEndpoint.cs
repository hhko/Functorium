using LayeredArch.Adapters.Presentation.Abstractions.Extensions;
using LayeredArch.Application.Usecases.Orders.Commands;

namespace LayeredArch.Adapters.Presentation.Endpoints.Orders;

/// <summary>
/// 주문 생성 Endpoint
/// POST /api/orders
/// </summary>
public sealed class CreateOrderEndpoint
    : Endpoint<CreateOrderEndpoint.Request, CreateOrderEndpoint.Response>
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
            req.CustomerId,
            LanguageExt.Prelude.Seq(req.OrderLines.Select(l => new CreateOrderCommand.OrderLineRequest(l.ProductId, l.Quantity))),
            req.ShippingAddress);

        var result = await _mediator.Send(usecaseRequest, ct);
        var mapped = result.Map(r => new Response(
            r.OrderId,
            r.OrderLines.Select(l => new OrderLineResponse(l.ProductId, l.Quantity, l.UnitPrice, l.LineTotal)).ToList(),
            r.TotalAmount,
            r.ShippingAddress,
            r.CreatedAt));
        await this.SendCreatedFinResponseAsync(mapped, ct);
    }

    public sealed record OrderLineRequest(string ProductId, int Quantity);

    public sealed record Request(
        string CustomerId,
        List<OrderLineRequest> OrderLines,
        string ShippingAddress);

    public sealed record OrderLineResponse(
        string ProductId,
        int Quantity,
        decimal UnitPrice,
        decimal LineTotal);

    /// <summary>
    /// Endpoint Response DTO
    /// </summary>
    public new sealed record Response(
        string OrderId,
        List<OrderLineResponse> OrderLines,
        decimal TotalAmount,
        string ShippingAddress,
        DateTime CreatedAt);
}
