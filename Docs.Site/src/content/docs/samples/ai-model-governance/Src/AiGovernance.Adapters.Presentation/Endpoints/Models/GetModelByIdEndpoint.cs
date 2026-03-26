using AiGovernance.Adapters.Presentation.Abstractions.Extensions;
using AiGovernance.Application.Usecases.Models.Queries;

namespace AiGovernance.Adapters.Presentation.Endpoints.Models;

/// <summary>
/// AI 모델 ID로 조회 Endpoint
/// GET /api/models/{id}
/// </summary>
public sealed class GetModelByIdEndpoint
    : Endpoint<GetModelByIdEndpoint.Request, GetModelByIdEndpoint.Response>
{
    private readonly IMediator _mediator;

    public GetModelByIdEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("api/models/{Id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "AI 모델 조회";
            s.Description = "ID로 AI 모델을 조회합니다";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new GetModelByIdQuery.Request(req.Id);
        var result = await _mediator.Send(usecaseRequest, ct);
        var mapped = result.Map(r => new Response(
            r.Id, r.Name, r.Version, r.Purpose, r.RiskTier, r.CreatedAt));
        await this.SendFinResponseWithNotFoundAsync(mapped, ct);
    }

    public sealed record Request(string Id);

    /// <summary>
    /// Endpoint Response DTO
    /// </summary>
    public new sealed record Response(
        string Id,
        string Name,
        string Version,
        string Purpose,
        string RiskTier,
        DateTimeOffset CreatedAt);
}
