using AiGovernance.Adapters.Presentation.Abstractions.Extensions;
using AiGovernance.Application.Usecases.Incidents.Queries;

namespace AiGovernance.Adapters.Presentation.Endpoints.Incidents;

/// <summary>
/// 인시던트 ID로 조회 Endpoint
/// GET /api/incidents/{id}
/// </summary>
public sealed class GetIncidentByIdEndpoint
    : Endpoint<GetIncidentByIdEndpoint.Request, GetIncidentByIdEndpoint.Response>
{
    private readonly IMediator _mediator;

    public GetIncidentByIdEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("api/incidents/{Id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "인시던트 조회";
            s.Description = "ID로 인시던트를 조회합니다";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new GetIncidentByIdQuery.Request(req.Id);
        var result = await _mediator.Send(usecaseRequest, ct);
        var mapped = result.Map(r => new Response(
            r.Id, r.DeploymentId, r.ModelId, r.Severity, r.Status, r.Description, r.ResolutionNote));
        await this.SendFinResponseWithNotFoundAsync(mapped, ct);
    }

    public sealed record Request(string Id);

    /// <summary>
    /// Endpoint Response DTO
    /// </summary>
    public new sealed record Response(
        string Id,
        string DeploymentId,
        string ModelId,
        string Severity,
        string Status,
        string Description,
        string? ResolutionNote);
}
