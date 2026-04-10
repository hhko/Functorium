using AiGovernance.Adapters.Presentation.Abstractions.Extensions;
using AiGovernance.Application.Usecases.Deployments.Queries;

namespace AiGovernance.Adapters.Presentation.Endpoints.Deployments;

/// <summary>
/// 배포 ID로 조회 Endpoint
/// GET /api/deployments/{id}
/// </summary>
public sealed class GetDeploymentByIdEndpoint
    : Endpoint<GetDeploymentByIdEndpoint.Request, GetDeploymentByIdEndpoint.Response>
{
    private readonly IMediator _mediator;

    public GetDeploymentByIdEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("api/deployments/{Id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "배포 조회";
            s.Description = "ID로 배포를 조회합니다";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new GetDeploymentByIdQuery.Request(req.Id);
        var result = await _mediator.Send(usecaseRequest, ct);
        var mapped = result.Map(r => new Response(
            r.Id, r.ModelId, r.EndpointUrl, r.Status, r.Environment, r.DriftThreshold));
        await this.SendFinResponseWithNotFoundAsync(mapped, ct);
    }

    public sealed record Request(string Id);

    /// <summary>
    /// Endpoint Response DTO
    /// </summary>
    public new sealed record Response(
        string Id,
        string ModelId,
        string EndpointUrl,
        string Status,
        string Environment,
        decimal DriftThreshold);
}
