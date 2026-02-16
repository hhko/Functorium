using LayeredArch.Adapters.Presentation.Abstractions.Extensions;
using LayeredArch.Application.Usecases.TestErrors;

namespace LayeredArch.Adapters.Presentation.Endpoints.TestErrors;

/// <summary>
/// 에러 테스트 Endpoint - UsecaseMetricsPipeline 에러 태그 검증용
/// POST /api/test-error
/// </summary>
public sealed class TestErrorEndpoint
    : Endpoint<TestErrorEndpoint.Request, TestErrorEndpoint.Response>
{
    private readonly IMediator _mediator;

    public TestErrorEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("api/test-error");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "에러 시나리오 테스트";
            s.Description = "다양한 에러 시나리오를 테스트하여 UsecaseMetricsPipeline의 에러 태그 기능을 검증합니다";
            s.ExampleRequest = new Request(
                Scenario: "SingleExpected",
                TestMessage: "Test error message");
        });
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        // Endpoint Request -> Usecase Request 변환
        if (!Enum.TryParse<TestErrorCommand.ErrorScenario>(req.Scenario, out var scenario))
        {
            ThrowError($"Invalid scenario: {req.Scenario}. Valid values: Success, SingleExpected, SingleExceptional, ManyExpected, ManyMixed, GenericExpected");
            return;
        }

        var usecaseRequest = new TestErrorCommand.Request(scenario, req.TestMessage);

        // Mediator로 Usecase 호출
        var result = await _mediator.Send(usecaseRequest, ct);

        // Usecase Response -> Endpoint Response 매핑 (ErrorScenario enum → string)
        var mapped = result.Map(r => new Response(
            r.Scenario.ToString(), r.Message, r.ExecutedAt));

        // FinResponse를 HTTP Response로 변환
        await this.SendFinResponseAsync(mapped, ct);
    }

    /// <summary>
    /// Endpoint Request DTO
    /// </summary>
    public sealed record Request(
        string Scenario,
        string TestMessage);

    /// <summary>
    /// Endpoint Response DTO
    /// </summary>
    public new sealed record Response(
        string Scenario,
        string Message,
        DateTime ExecutedAt);
}
