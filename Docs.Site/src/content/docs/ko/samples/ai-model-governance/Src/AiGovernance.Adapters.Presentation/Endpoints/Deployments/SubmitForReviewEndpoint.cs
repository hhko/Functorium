using AiGovernance.Adapters.Presentation.Abstractions.Extensions;
using AiGovernance.Application.Usecases.Deployments.Commands;

namespace AiGovernance.Adapters.Presentation.Endpoints.Deployments;

/// <summary>
/// 배포 검토 제출 Endpoint
/// PUT /api/deployments/{id}/submit
/// </summary>
public sealed class SubmitForReviewEndpoint
    : Endpoint<SubmitForReviewEndpoint.Request, SubmitForReviewEndpoint.Response>
{
    private readonly IMediator _mediator;

    public SubmitForReviewEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("api/deployments/{Id}/submit");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "배포 검토 제출";
            s.Description = "배포를 검토를 위해 제출합니다";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new SubmitDeploymentForReviewCommand.Request(req.Id);

        var result = await _mediator.Send(usecaseRequest, ct);
        var mapped = result.Map(_ => new Response());
        await this.SendFinResponseWithNotFoundAsync(mapped, ct);
    }

    public sealed record Request(string Id);

    /// <summary>
    /// Endpoint Response DTO
    /// </summary>
    public new sealed record Response;
}
