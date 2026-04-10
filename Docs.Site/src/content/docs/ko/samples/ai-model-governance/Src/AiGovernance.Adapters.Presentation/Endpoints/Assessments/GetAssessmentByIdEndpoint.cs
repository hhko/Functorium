using AiGovernance.Adapters.Presentation.Abstractions.Extensions;
using AiGovernance.Application.Usecases.Assessments.Queries;

namespace AiGovernance.Adapters.Presentation.Endpoints.Assessments;

/// <summary>
/// 컴플라이언스 평가 ID로 조회 Endpoint
/// GET /api/assessments/{id}
/// </summary>
public sealed class GetAssessmentByIdEndpoint
    : Endpoint<GetAssessmentByIdEndpoint.Request, GetAssessmentByIdEndpoint.Response>
{
    private readonly IMediator _mediator;

    public GetAssessmentByIdEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("api/assessments/{Id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "컴플라이언스 평가 조회";
            s.Description = "ID로 컴플라이언스 평가를 조회합니다";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new GetAssessmentByIdQuery.Request(req.Id);
        var result = await _mediator.Send(usecaseRequest, ct);
        var mapped = result.Map(r => new Response(
            r.Id, r.ModelId, r.DeploymentId, r.Score, r.Status, r.Criteria));
        await this.SendFinResponseWithNotFoundAsync(mapped, ct);
    }

    public sealed record Request(string Id);

    /// <summary>
    /// Endpoint Response DTO
    /// </summary>
    public new sealed record Response(
        string Id,
        string ModelId,
        string DeploymentId,
        int? Score,
        string Status,
        List<GetAssessmentByIdQuery.CriterionDto> Criteria);
}
