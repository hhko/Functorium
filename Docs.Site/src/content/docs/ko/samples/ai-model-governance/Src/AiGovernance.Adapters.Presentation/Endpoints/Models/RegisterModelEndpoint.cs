using AiGovernance.Adapters.Presentation.Abstractions.Extensions;
using AiGovernance.Application.Usecases.Models.Commands;

namespace AiGovernance.Adapters.Presentation.Endpoints.Models;

/// <summary>
/// AI 모델 등록 Endpoint
/// POST /api/models
/// </summary>
public sealed class RegisterModelEndpoint
    : Endpoint<RegisterModelEndpoint.Request, RegisterModelEndpoint.Response>
{
    private readonly IMediator _mediator;

    public RegisterModelEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("api/models");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "AI 모델 등록";
            s.Description = "새로운 AI 모델을 등록합니다";
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var usecaseRequest = new RegisterModelCommand.Request(
            req.Name,
            req.Version,
            req.Purpose);

        var result = await _mediator.Send(usecaseRequest, ct);

        var mapped = result.Map(r => new Response(r.ModelId));

        await this.SendCreatedFinResponseAsync(mapped, ct);
    }

    /// <summary>
    /// Endpoint Request DTO
    /// </summary>
    public sealed record Request(
        string Name,
        string Version,
        string Purpose);

    /// <summary>
    /// Endpoint Response DTO
    /// </summary>
    public new sealed record Response(string ModelId);
}
