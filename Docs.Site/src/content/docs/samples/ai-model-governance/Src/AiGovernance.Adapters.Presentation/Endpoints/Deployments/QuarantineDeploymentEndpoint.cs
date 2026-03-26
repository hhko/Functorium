using AiGovernance.Adapters.Presentation.Abstractions.Extensions;
using AiGovernance.Application.Usecases.Deployments.Commands;

namespace AiGovernance.Adapters.Presentation.Endpoints.Deployments;

/// <summary>
/// 배포 격리 Endpoint
/// PUT /api/deployments/{id}/quarantine
/// </summary>
public sealed class QuarantineDeploymentEndpoint
    : Endpoint<QuarantineDeploymentEndpoint.Request, QuarantineDeploymentEndpoint.Response>
{
    private readonly IMediator _mediator;

    public QuarantineDeploymentEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("api/deployments/{Id}/quarantine");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "배포 격리";
            s.Description = "배포를 격리 상태로 전환합니다";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new QuarantineDeploymentCommand.Request(
            req.Id,
            req.Reason);

        var result = await _mediator.Send(usecaseRequest, ct);
        var mapped = result.Map(_ => new Response());
        await this.SendFinResponseWithNotFoundAsync(mapped, ct);
    }

    public sealed record Request(
        string Id,
        string Reason);

    /// <summary>
    /// Endpoint Response DTO
    /// </summary>
    public new sealed record Response;
}
