using AiGovernance.Adapters.Presentation.Abstractions.Extensions;
using AiGovernance.Application.Usecases.Incidents.Commands;

namespace AiGovernance.Adapters.Presentation.Endpoints.Incidents;

/// <summary>
/// 인시던트 보고 Endpoint
/// POST /api/incidents
/// </summary>
public sealed class ReportIncidentEndpoint
    : Endpoint<ReportIncidentEndpoint.Request, ReportIncidentEndpoint.Response>
{
    private readonly IMediator _mediator;

    public ReportIncidentEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("api/incidents");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "인시던트 보고";
            s.Description = "배포에 대한 인시던트를 보고합니다";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new ReportIncidentCommand.Request(
            req.DeploymentId,
            req.Severity,
            req.Description);

        var result = await _mediator.Send(usecaseRequest, ct);

        var mapped = result.Map(r => new Response(r.IncidentId));

        await this.SendCreatedFinResponseAsync(mapped, ct);
    }

    /// <summary>
    /// Endpoint Request DTO
    /// </summary>
    public sealed record Request(
        string DeploymentId,
        string Severity,
        string Description);

    /// <summary>
    /// Endpoint Response DTO
    /// </summary>
    public new sealed record Response(string IncidentId);
}
