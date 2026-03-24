using System.Diagnostics;

using Functorium.Abstractions.Errors;
using Functorium.Adapters.Observabilities.Contexts;
using Functorium.Adapters.Observabilities.Naming;
using Functorium.Adapters.Observabilities.Pipelines;
using Functorium.Applications.Observabilities;
using Functorium.Applications.Usecases;

using LanguageExt.Common;

using Mediator;

using Serilog.Context;

using static Functorium.Tests.Unit.Abstractions.Constants.Constants;
using static Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines.TestFixtures;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines;

/// <summary>
/// UsecaseTracingPipeline + CtxEnricher 통합 Tracing 태그 구조 검증 테스트.
/// CtxEnricherContext.Push로 Push된 Tracing ctx.* 필드가
/// Activity.Current?.SetTag를 통해 Span 태그에 포함되는지 스냅샷으로 고정합니다.
/// </summary>
/// <remarks>
/// <para>
/// Enricher가 추가하는 Tracing ctx.* 필드:
/// </para>
/// <code>
/// +----+----------------------------+------------------+-------------------+
/// | #  | ctx 필드                   | CtxPillar        | Tracing 포함?    |
/// +----+----------------------------+------------------+-------------------+
/// | 1  | ctx.customer_id            | Default          | Yes               |
/// | 2  | ctx.is_express             | All              | Yes               |
/// | 3  | ctx.item_count             | Default|Value    | Yes               |
/// +----+----------------------------+------------------+-------------------+
/// </code>
/// </remarks>
[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public sealed class UsecaseTracingPipelineEnricherStructureTests : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly ActivityListener _activityListener;
    private Activity? _capturedActivity;

    public UsecaseTracingPipelineEnricherStructureTests()
    {
        _activitySource = new ActivitySource("Test.TracingPipelineEnricherTagStructure");
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == _activitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => _capturedActivity = activity
        };
        ActivitySource.AddActivityListener(_activityListener);

        // CtxEnricherContext.SetPushFactory 설정 — OpenTelemetryBuilder와 동일한 로직
        CtxEnricherContext.SetPushFactory((name, value, pillars) =>
        {
            var disposables = new List<IDisposable>();

            if (pillars.HasFlag(CtxPillar.Logging))
                disposables.Add(LogContext.PushProperty(name, value));

            if (pillars.HasFlag(CtxPillar.Tracing))
                Activity.Current?.SetTag(name, value);

            if (pillars.HasFlag(CtxPillar.MetricsTag))
                disposables.Add(MetricsTagContext.Push(name, value));

            return disposables.Count switch
            {
                0 => NullDisposable.Instance,
                1 => disposables[0],
                _ => new TestCompositeDisposable(disposables)
            };
        });
    }

    public void Dispose()
    {
        _activityListener.Dispose();
        _activitySource.Dispose();
        CtxEnricherContext.SetPushFactory(static (_, _, _) => NullDisposable.Instance);
    }

    #region Snapshot 태그 구조 테스트

    /// <summary>
    /// Enricher가 Push한 Tracing ctx.* 필드가 성공 Activity 태그에 포함되는지 검증합니다.
    /// TracingPipeline이 Activity를 생성한 뒤 CtxEnricherPipeline이 실행되어야
    /// Activity.Current?.SetTag가 실제 Activity에 태그를 설정합니다.
    /// (프로덕션 환경에서는 HTTP 미들웨어가 상위 Activity를 먼저 생성합니다.)
    /// </summary>
    [Fact]
    public async Task Snapshot_Command_SuccessTags_WithCtxEnricher()
    {
        // Arrange
        var enricher = new TestCommandCtx3PillarEnricher();
        var enricherPipeline = new CtxEnricherPipeline<TestCommandRequest, TestResponse>(enricher);
        var tracingPipeline = new UsecaseTracingPipeline<TestCommandRequest, TestResponse>(_activitySource);
        var request = new TestCommandRequest("TestName");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        // Act — TracingPipeline이 Activity 생성 → CtxEnricherPipeline이 Activity.SetTag
        await tracingPipeline.Handle(request,
            (req, ct) => enricherPipeline.Handle(req,
                (_, _) => ValueTask.FromResult(expectedResponse), ct),
            CancellationToken.None);

        // Assert
        var tags = ExtractActivityTags();
        await Verify(tags)
            .UseDirectory("Snapshots/CtxEnricher")
            .ScrubMember(ObservabilityNaming.CustomAttributes.ResponseElapsed);
    }

    /// <summary>
    /// 에러 상황에서도 Enricher가 Push한 ctx.* 필드가 Activity 태그에 포함되는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_Command_FailureTags_WithCtxEnricher()
    {
        // Arrange
        var enricher = new TestCommandCtx3PillarEnricher();
        var enricherPipeline = new CtxEnricherPipeline<TestCommandRequest, TestResponse>(enricher);
        var tracingPipeline = new UsecaseTracingPipeline<TestCommandRequest, TestResponse>(_activitySource);
        var request = new TestCommandRequest("TestName");

        var error = new ErrorCodeExpected("Test.ExpectedError", "currentValue", "Expected error occurred");

        // Act — TracingPipeline이 Activity 생성 → CtxEnricherPipeline이 Activity.SetTag
        await tracingPipeline.Handle(request,
            (req, ct) => enricherPipeline.Handle(req,
                (_, _) => ValueTask.FromResult(TestResponse.CreateFail(error)), ct),
            CancellationToken.None);

        // Assert
        var tags = ExtractActivityTags();
        await Verify(tags)
            .UseDirectory("Snapshots/CtxEnricher")
            .ScrubMember(ObservabilityNaming.CustomAttributes.ResponseElapsed);
    }

    #endregion

    #region Helper Methods

    private Dictionary<string, string?> ExtractActivityTags()
    {
        _capturedActivity.ShouldNotBeNull();
        return _capturedActivity.TagObjects
            .OrderBy(t => t.Key)
            .ToDictionary(t => t.Key, t => t.Value?.ToString());
    }

    #endregion

    #region Test Types

    internal sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();
        public void Dispose() { }
    }

    private sealed class TestCompositeDisposable(List<IDisposable> disposables) : IDisposable
    {
        public void Dispose()
        {
            for (int i = disposables.Count - 1; i >= 0; i--)
                disposables[i].Dispose();
        }
    }

    #endregion
}
