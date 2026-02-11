using LayeredArch.Adapters.Presentation.Abstractions.Extensions;
using LayeredArch.Application.Usecases.Customers;

namespace LayeredArch.Adapters.Presentation.Endpoints.Customers;

/// <summary>
/// 고객 ID로 조회 Endpoint
/// GET /api/customers/{id}
/// </summary>
public sealed class GetCustomerByIdEndpoint
    : Endpoint<GetCustomerByIdEndpoint.Request, GetCustomerByIdQuery.Response>
{
    private readonly IMediator _mediator;

    public GetCustomerByIdEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("api/customers/{Id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "고객 조회";
            s.Description = "ID로 고객을 조회합니다";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new GetCustomerByIdQuery.Request(req.Id);
        var result = await _mediator.Send(usecaseRequest, ct);
        await this.SendFinResponseWithNotFoundAsync(result, ct);
    }

    public sealed record Request(string Id);
}
