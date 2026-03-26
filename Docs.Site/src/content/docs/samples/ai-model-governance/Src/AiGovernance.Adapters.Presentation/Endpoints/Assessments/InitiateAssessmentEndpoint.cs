using AiGovernance.Adapters.Presentation.Abstractions.Extensions;
using AiGovernance.Application.Usecases.Assessments.Commands;

namespace AiGovernance.Adapters.Presentation.Endpoints.Assessments;

/// <summary>
/// 컴플라이언스 평가 개시 Endpoint
/// POST /api/assessments
/// </summary>
public sealed class InitiateAssessmentEndpoint
    : Endpoint<InitiateAssessmentEndpoint.Request, InitiateAssessmentEndpoint.Response>
{
    private readonly IMediator _mediator;

    public InitiateAssessmentEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("api/assessments");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "컴플라이언스 평가 개시";
            s.Description = "모델과 배포에 대한 컴플라이언스 평가를 개시합니다";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new InitiateAssessmentCommand.Request(
            req.ModelId,
            req.DeploymentId);

        var result = await _mediator.Send(usecaseRequest, ct);

        var mapped = result.Map(r => new Response(r.AssessmentId));

        await this.SendCreatedFinResponseAsync(mapped, ct);
    }

    /// <summary>
    /// Endpoint Request DTO
    /// </summary>
    public sealed record Request(
        string ModelId,
        string DeploymentId);

    /// <summary>
    /// Endpoint Response DTO
    /// </summary>
    public new sealed record Response(string AssessmentId);
}
