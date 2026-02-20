using LayeredArch.Adapters.Presentation.Abstractions.Extensions;
using LayeredArch.Application.Usecases.Orders.Queries;

namespace LayeredArch.Adapters.Presentation.Endpoints.Orders;

/// <summary>
/// 주문 ID로 조회 Endpoint
/// GET /api/orders/{id}
/// </summary>
public sealed class GetOrderByIdEndpoint
    : Endpoint<GetOrderByIdEndpoint.Request, GetOrderByIdEndpoint.Response>
{
    private readonly IMediator _mediator;

    public GetOrderByIdEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("api/orders/{Id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "주문 조회";
            s.Description = "ID로 주문을 조회합니다";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new GetOrderByIdQuery.Request(req.Id);
        var result = await _mediator.Send(usecaseRequest, ct);
        var mapped = result.Map(r => new Response(
            r.OrderId,
            r.OrderLines.Select(l => new OrderLineResponse(l.ProductId, l.Quantity, l.UnitPrice, l.LineTotal)).ToList(),
            r.TotalAmount,
            r.ShippingAddress,
            r.CreatedAt));
        await this.SendFinResponseWithNotFoundAsync(mapped, ct);
    }

    public sealed record Request(string Id);

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
