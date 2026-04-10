using AiGovernance.Adapters.Presentation.Abstractions.Extensions;
using AiGovernance.Application.Usecases.Deployments.Commands;

namespace AiGovernance.Adapters.Presentation.Endpoints.Deployments;

/// <summary>
/// 배포 생성 Endpoint
/// POST /api/deployments
/// </summary>
public sealed class CreateDeploymentEndpoint
    : Endpoint<CreateDeploymentEndpoint.Request, CreateDeploymentEndpoint.Response>
{
    private readonly IMediator _mediator;

    public CreateDeploymentEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("api/deployments");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "배포 생성";
            s.Description = "새로운 모델 배포를 생성합니다";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new CreateDeploymentCommand.Request(
            req.ModelId,
            req.EndpointUrl,
            req.Environment,
            req.DriftThreshold);

        var result = await _mediator.Send(usecaseRequest, ct);

        var mapped = result.Map(r => new Response(r.DeploymentId));

        await this.SendCreatedFinResponseAsync(mapped, ct);
    }

    /// <summary>
    /// Endpoint Request DTO
    /// </summary>
    public sealed record Request(
        string ModelId,
        string EndpointUrl,
        string Environment,
        decimal DriftThreshold);

    /// <summary>
    /// Endpoint Response DTO
    /// </summary>
    public new sealed record Response(string DeploymentId);
}
