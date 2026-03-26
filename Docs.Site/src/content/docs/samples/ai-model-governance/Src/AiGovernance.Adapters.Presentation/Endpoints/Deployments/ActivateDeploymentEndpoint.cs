using AiGovernance.Adapters.Presentation.Abstractions.Extensions;
using AiGovernance.Application.Usecases.Deployments.Commands;

namespace AiGovernance.Adapters.Presentation.Endpoints.Deployments;

/// <summary>
/// 배포 활성화 Endpoint
/// PUT /api/deployments/{id}/activate
/// </summary>
public sealed class ActivateDeploymentEndpoint
    : Endpoint<ActivateDeploymentEndpoint.Request, ActivateDeploymentEndpoint.Response>
{
    private readonly IMediator _mediator;

    public ActivateDeploymentEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("api/deployments/{Id}/activate");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "배포 활성화";
            s.Description = "컴플라이언스 평가 통과 후 배포를 활성화합니다";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new ActivateDeploymentCommand.Request(
            req.Id,
            req.AssessmentId);

        var result = await _mediator.Send(usecaseRequest, ct);
        var mapped = result.Map(_ => new Response());
        await this.SendFinResponseWithNotFoundAsync(mapped, ct);
    }

    public sealed record Request(
        string Id,
        string AssessmentId);

    /// <summary>
    /// Endpoint Response DTO
    /// </summary>
    public new sealed record Response;
}
