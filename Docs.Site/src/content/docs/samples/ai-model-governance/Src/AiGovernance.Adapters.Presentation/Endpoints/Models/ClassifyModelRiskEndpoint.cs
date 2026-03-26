using AiGovernance.Adapters.Presentation.Abstractions.Extensions;
using AiGovernance.Application.Usecases.Models.Commands;

namespace AiGovernance.Adapters.Presentation.Endpoints.Models;

/// <summary>
/// AI 모델 위험 등급 재분류 Endpoint
/// PUT /api/models/{id}/risk
/// </summary>
public sealed class ClassifyModelRiskEndpoint
    : Endpoint<ClassifyModelRiskEndpoint.Request, ClassifyModelRiskEndpoint.Response>
{
    private readonly IMediator _mediator;

    public ClassifyModelRiskEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("api/models/{Id}/risk");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "AI 모델 위험 등급 재분류";
            s.Description = "AI 모델의 위험 등급을 재분류합니다";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new ClassifyModelRiskCommand.Request(
            req.Id,
            req.RiskTier);

        var result = await _mediator.Send(usecaseRequest, ct);
        var mapped = result.Map(_ => new Response());
        await this.SendFinResponseWithNotFoundAsync(mapped, ct);
    }

    public sealed record Request(
        string Id,
        string RiskTier);

    /// <summary>
    /// Endpoint Response DTO
    /// </summary>
    public new sealed record Response;
}
