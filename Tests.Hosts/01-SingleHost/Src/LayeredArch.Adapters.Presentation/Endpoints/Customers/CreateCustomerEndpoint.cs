using LayeredArch.Adapters.Presentation.Abstractions.Extensions;
using LayeredArch.Application.Usecases.Customers;

namespace LayeredArch.Adapters.Presentation.Endpoints.Customers;

/// <summary>
/// 고객 생성 Endpoint
/// POST /api/customers
/// </summary>
public sealed class CreateCustomerEndpoint
    : Endpoint<CreateCustomerEndpoint.Request, CreateCustomerEndpoint.Response>
{
    private readonly IMediator _mediator;

    public CreateCustomerEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("api/customers");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "고객 생성";
            s.Description = "새로운 고객을 생성합니다";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new CreateCustomerCommand.Request(
            req.Name,
            req.Email,
            req.CreditLimit);

        var result = await _mediator.Send(usecaseRequest, ct);
        var mapped = result.Map(r => new Response(
            r.CustomerId, r.Name, r.Email, r.CreditLimit, r.CreatedAt));
        await this.SendCreatedFinResponseAsync(mapped, ct);
    }

    public sealed record Request(
        string Name,
        string Email,
        decimal CreditLimit);

    /// <summary>
    /// Endpoint Response DTO
    /// </summary>
    public new sealed record Response(
        string CustomerId,
        string Name,
        string Email,
        decimal CreditLimit,
        DateTime CreatedAt);
}
