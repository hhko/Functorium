using Functorium.Abstractions.Errors;
using Functorium.Adapters.Observabilities.Pipelines;
using Functorium.Applications.Observabilities;
using Functorium.Testing.Arrangements.Logging;
using Mediator;
using Serilog.Events;
using static Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines.TestFixtures;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines;

/// <summary>
/// UsecaseLoggingPipeline + Enricher 통합 로그 필드 구조 검증 테스트.
/// Enricher가 LogContext.PushProperty로 Push한 ctx.* 필드가
/// 실제 로그 이벤트에 정확히 출력되는지 스냅샷으로 고정합니다.
/// </summary>
/// <remarks>
/// <para>
/// 검증 대상 6가지 ctx 필드 패턴:
/// </para>
/// <code>
/// +----+-------------------------------------------+-------------------+---------+
/// | #  | 패턴                                      | 필드              | 테스트  |
/// +----+-------------------------------------------+-------------------+---------+
/// | 1  | Root 스칼라                                | ctx.customer_id   | Req/Res |
/// | 2  | Root 컬렉션                                | ctx.items_count   | Req/Res |
/// | 3  | Usecase Request 스칼라                     | ctx.*.request.*   | Req     |
/// | 4  | Usecase Request 컬렉션                     | ctx.*.request.*   | Req     |
/// | 5  | Usecase Response 스칼라                    | ctx.*.response.*  | Res     |
/// | 6  | Usecase Response 컬렉션                    | ctx.*.response.*  | Res     |
/// +----+-------------------------------------------+-------------------+---------+
/// </code>
/// </remarks>
public sealed class UsecaseLoggingPipelineEnricherStructureTests
{
    [Fact]
    public async Task Command_RequestWithEnricher_Should_Log_EnrichedFields()
    {
        // Arrange
        using var context = new LogTestContext(LogEventLevel.Debug, enrichFromLogContext: true);
        var logger = context.CreateLogger<UsecaseLoggingPipeline<TestCommandRequest, TestResponse>>();
        var enricher = new TestCommandCtxEnricher();
        var pipeline = new UsecaseLoggingPipeline<TestCommandRequest, TestResponse>(logger);
        var request = new TestCommandRequest("TestName");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act — Enricher로 LogContext에 Push한 뒤 Pipeline 실행 (CtxEnricherPipeline 시뮬레이션)
        using var enrichment = enricher.EnrichRequest(request);
        await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        await Verify(context.ExtractFirstLogData()).UseDirectory("Snapshots");
    }

    [Fact]
    public async Task Command_SuccessResponseWithEnricher_Should_Log_EnrichedFields()
    {
        // Arrange
        using var context = new LogTestContext(LogEventLevel.Debug, enrichFromLogContext: true);
        var logger = context.CreateLogger<UsecaseLoggingPipeline<TestCommandRequest, TestResponse>>();
        var enricher = new TestCommandCtxEnricher();
        var pipeline = new UsecaseLoggingPipeline<TestCommandRequest, TestResponse>(logger);
        var request = new TestCommandRequest("TestName");
        var expectedResponse = TestResponse.CreateSuccess(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "ResponseName");

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act — Enricher로 LogContext에 Push한 뒤 Pipeline 실행
        using var reqEnrichment = enricher.EnrichRequest(request);
        await pipeline.Handle(request, next, CancellationToken.None);
        using var resEnrichment = enricher.EnrichResponse(request, expectedResponse);

        // Assert
        await Verify(context.ExtractSecondLogData())
            .UseDirectory("Snapshots")
            .ScrubMember("response.elapsed");
    }

    [Fact]
    public async Task Command_FailureResponseWithEnricher_Should_Log_EnrichedFields()
    {
        // Arrange
        using var context = new LogTestContext(LogEventLevel.Debug, enrichFromLogContext: true);
        var logger = context.CreateLogger<UsecaseLoggingPipeline<TestCommandRequest, TestResponse>>();
        var enricher = new TestCommandCtxEnricher();
        var pipeline = new UsecaseLoggingPipeline<TestCommandRequest, TestResponse>(logger);
        var request = new TestCommandRequest("TestName");

        var error = ErrorCodeFactory.Create(
            errorCode: "Order.NotFound",
            errorCurrentValue: "order-999",
            errorMessage: "주문을 찾을 수 없습니다");
        var errorResponse = TestResponse.CreateFail(error);

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(errorResponse);

        // Act — Enricher로 LogContext에 Push한 뒤 Pipeline 실행
        using var enrichment = enricher.EnrichRequest(request);
        await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        await Verify(context.ExtractSecondLogData())
            .UseDirectory("Snapshots")
            .ScrubMember("response.elapsed");
    }
}
