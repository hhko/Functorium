using System.Diagnostics;
using System.Diagnostics.Metrics;

using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Naming;
using Functorium.Testing.Arrangements.Logging;

using LanguageExt;

using Microsoft.Extensions.Options;

using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

using MsOptions = Microsoft.Extensions.Options.Options;

namespace Functorium.Tests.Unit.AdaptersTests.SourceGenerators;

/// <summary>
/// SourceGenerator로 생성된 Adapter Pipeline의 Tracing 태그 구조 검증 테스트.
/// 런타임에서 실제 Pipeline을 실행하고 Activity 태그를 스냅샷으로 검증합니다.
/// </summary>
/// <remarks>
/// <para>
/// 이 테스트는 생성된 Pipeline 코드의 Tracing 태그 구조가 실수로 변경되는 것을 방지합니다.
/// </para>
/// <para>
/// Activity 태그 구조 비교표:
/// </para>
/// <code>
/// +--------------------------+-------------------+-------------------+
/// | Tag Key                  | Success           | Failure           |
/// +--------------------------+-------------------+-------------------+
/// | request.layer            | "adapter"         | "adapter"         |
/// | request.category         | category name     | category name     |
/// | request.handler          | handler name      | handler name      |
/// | request.handler.method   | method name       | method name       |
/// | response.elapsed         | elapsed seconds   | elapsed seconds   |
/// | response.status          | "success"         | "failure"         |
/// | error.type               | (none)            | "expected"/       |
/// |                          |                   | "exceptional"/    |
/// |                          |                   | "aggregate"       |
/// | error.code               | (none)            | error code        |
/// +--------------------------+-------------------+-------------------+
/// | Total Tags               | 6                 | 8                 |
/// +--------------------------+-------------------+-------------------+
/// </code>
/// </remarks>
[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters_SourceGenerator)]
public sealed class AdapterTracingPipelineStructureTests : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly ActivityListener _activityListener;
    private readonly IMeterFactory _meterFactory;
    private readonly IOptions<OpenTelemetryOptions> _openTelemetryOptions;
    private readonly LogTestContext _logContext;
    private Activity? _capturedActivity;

    public AdapterTracingPipelineStructureTests()
    {
        _activitySource = new ActivitySource("Test.AdapterTracing");
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == _activitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => _capturedActivity = activity
        };
        ActivitySource.AddActivityListener(_activityListener);

        _meterFactory = new TestMeterFactory();
        _openTelemetryOptions = MsOptions.Create(new OpenTelemetryOptions { ServiceNamespace = "TestService" });
        _logContext = new LogTestContext();
    }

    public void Dispose()
    {
        _activityListener.Dispose();
        _activitySource.Dispose();
        (_meterFactory as IDisposable)?.Dispose();
        _logContext.Dispose();
    }

    #region Span 이름 검증

    /// <summary>
    /// Adapter 요청 시 Span 이름이 올바른 패턴을 따라야 합니다.
    /// 패턴: "adapter {category} {handler}.{method}"
    /// </summary>
    [Fact]
    public async Task Handle_ShouldCreateActivityWithCorrectName()
    {
        // Arrange
        var logger = _logContext.CreateLogger<TestObservabilityAdapterPipeline>();
        var pipeline = new TestObservabilityAdapterPipeline(
            _activitySource,
            logger,
            _meterFactory,
            _openTelemetryOptions);

        var testId = Guid.NewGuid();

        // Act
        await pipeline.GetById(testId).Run().RunAsync();

        // Assert
        _capturedActivity.ShouldNotBeNull();
        _capturedActivity.DisplayName.ShouldStartWith("adapter Repository");
        _capturedActivity.DisplayName.ShouldContain(".GetById");
    }

    #endregion

    #region Response 태그 검증 - Success

    /// <summary>
    /// 성공 시 6개 태그를 가져야 합니다.
    /// (request 4개 + response.elapsed 1개 + response.status 1개)
    /// </summary>
    [Fact]
    public async Task Handle_Success_ShouldHaveSixTags()
    {
        // Arrange
        var logger = _logContext.CreateLogger<TestObservabilityAdapterPipeline>();
        var pipeline = new TestObservabilityAdapterPipeline(
            _activitySource,
            logger,
            _meterFactory,
            _openTelemetryOptions);

        var testId = Guid.NewGuid();

        // Act
        await pipeline.GetById(testId).Run().RunAsync();

        // Assert
        _capturedActivity.ShouldNotBeNull();
        _capturedActivity.TagObjects.Count().ShouldBe(6);
    }

    /// <summary>
    /// 성공 시 ActivityStatus가 Ok여야 합니다.
    /// </summary>
    [Fact]
    public async Task Handle_Success_ShouldSetActivityStatusOk()
    {
        // Arrange
        var logger = _logContext.CreateLogger<TestObservabilityAdapterPipeline>();
        var pipeline = new TestObservabilityAdapterPipeline(
            _activitySource,
            logger,
            _meterFactory,
            _openTelemetryOptions);

        var testId = Guid.NewGuid();

        // Act
        await pipeline.GetById(testId).Run().RunAsync();

        // Assert
        _capturedActivity.ShouldNotBeNull();
        _capturedActivity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    #endregion

    #region Response 태그 검증 - Failure

    /// <summary>
    /// 실패 시 8개 태그를 가져야 합니다.
    /// (request 4개 + response.elapsed 1개 + response.status 1개 + error.type 1개 + error.code 1개)
    /// </summary>
    [Fact]
    public async Task Handle_Failure_ShouldHaveEightTags()
    {
        // Arrange
        var logger = _logContext.CreateLogger<TestObservabilityAdapterPipeline>();
        var pipeline = new TestObservabilityAdapterPipeline(
            _activitySource,
            logger,
            _meterFactory,
            _openTelemetryOptions)
        {
            GetByIdHandler = _ => TestErrors.CreateExpectedError()
        };

        var testId = Guid.NewGuid();

        // Act - 에러가 발생해도 Activity 태그는 기록됨
        try
        {
            await pipeline.GetById(testId).Run().RunAsync();
        }
        catch
        {
            // 에러는 무시 - Activity 태그 검증이 목적
        }

        // Assert
        _capturedActivity.ShouldNotBeNull();
        _capturedActivity.TagObjects.Count().ShouldBe(8);
    }

    /// <summary>
    /// 실패 시 ActivityStatus가 Error여야 합니다.
    /// </summary>
    [Fact]
    public async Task Handle_Failure_ShouldSetActivityStatusError()
    {
        // Arrange
        var logger = _logContext.CreateLogger<TestObservabilityAdapterPipeline>();
        var pipeline = new TestObservabilityAdapterPipeline(
            _activitySource,
            logger,
            _meterFactory,
            _openTelemetryOptions)
        {
            GetByIdHandler = _ => TestErrors.CreateExpectedError()
        };

        var testId = Guid.NewGuid();

        // Act - 에러가 발생해도 Activity 태그는 기록됨
        try
        {
            await pipeline.GetById(testId).Run().RunAsync();
        }
        catch
        {
            // 에러는 무시 - Activity 태그 검증이 목적
        }

        // Assert
        _capturedActivity.ShouldNotBeNull();
        _capturedActivity.Status.ShouldBe(ActivityStatusCode.Error);
    }

    #endregion

    #region Snapshot 태그 구조 테스트

    /// <summary>
    /// Success 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_SuccessTags()
    {
        // Arrange
        var logger = _logContext.CreateLogger<TestObservabilityAdapterPipeline>();
        var pipeline = new TestObservabilityAdapterPipeline(
            _activitySource,
            logger,
            _meterFactory,
            _openTelemetryOptions);

        var testId = Guid.NewGuid();

        // Act
        await pipeline.GetById(testId).Run().RunAsync();

        // Assert
        _capturedActivity.ShouldNotBeNull();
        var tags = _capturedActivity.TagObjects
            .OrderBy(t => t.Key)
            .ToDictionary(t => t.Key, t => t.Value?.ToString());

        await Verify(tags)
            .UseDirectory("Snapshots")
            .ScrubMember(ObservabilityNaming.CustomAttributes.ResponseElapsed);
    }

    /// <summary>
    /// Failure (Expected Error) 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_FailureResponse_ExpectedError_Tags()
    {
        // Arrange
        var logger = _logContext.CreateLogger<TestObservabilityAdapterPipeline>();
        var pipeline = new TestObservabilityAdapterPipeline(
            _activitySource,
            logger,
            _meterFactory,
            _openTelemetryOptions)
        {
            GetByIdHandler = _ => TestErrors.CreateExpectedError()
        };

        var testId = Guid.NewGuid();

        // Act - 에러가 발생해도 Activity 태그는 기록됨
        try
        {
            await pipeline.GetById(testId).Run().RunAsync();
        }
        catch
        {
            // 에러는 무시 - Activity 태그 검증이 목적
        }

        // Assert
        _capturedActivity.ShouldNotBeNull();
        var tags = _capturedActivity.TagObjects
            .OrderBy(t => t.Key)
            .ToDictionary(t => t.Key, t => t.Value?.ToString());

        await Verify(tags)
            .UseDirectory("Snapshots")
            .ScrubMember(ObservabilityNaming.CustomAttributes.ResponseElapsed);
    }

    /// <summary>
    /// Failure (Exceptional Error) 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_FailureResponse_ExceptionalError_Tags()
    {
        // Arrange
        var logger = _logContext.CreateLogger<TestObservabilityAdapterPipeline>();
        var pipeline = new TestObservabilityAdapterPipeline(
            _activitySource,
            logger,
            _meterFactory,
            _openTelemetryOptions)
        {
            GetByIdHandler = _ => TestErrors.CreateExceptionalError()
        };

        var testId = Guid.NewGuid();

        // Act - 에러가 발생해도 Activity 태그는 기록됨
        try
        {
            await pipeline.GetById(testId).Run().RunAsync();
        }
        catch
        {
            // 에러는 무시 - Activity 태그 검증이 목적
        }

        // Assert
        _capturedActivity.ShouldNotBeNull();
        var tags = _capturedActivity.TagObjects
            .OrderBy(t => t.Key)
            .ToDictionary(t => t.Key, t => t.Value?.ToString());

        await Verify(tags)
            .UseDirectory("Snapshots")
            .ScrubMember(ObservabilityNaming.CustomAttributes.ResponseElapsed);
    }

    /// <summary>
    /// Failure (Aggregate Error) 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_FailureResponse_AggregateError_Tags()
    {
        // Arrange
        var logger = _logContext.CreateLogger<TestObservabilityAdapterPipeline>();
        var pipeline = new TestObservabilityAdapterPipeline(
            _activitySource,
            logger,
            _meterFactory,
            _openTelemetryOptions)
        {
            GetByIdHandler = _ => TestErrors.CreateAggregateError()
        };

        var testId = Guid.NewGuid();

        // Act - 에러가 발생해도 Activity 태그는 기록됨
        try
        {
            await pipeline.GetById(testId).Run().RunAsync();
        }
        catch
        {
            // 에러는 무시 - Activity 태그 검증이 목적
        }

        // Assert
        _capturedActivity.ShouldNotBeNull();
        var tags = _capturedActivity.TagObjects
            .OrderBy(t => t.Key)
            .ToDictionary(t => t.Key, t => t.Value?.ToString());

        await Verify(tags)
            .UseDirectory("Snapshots")
            .ScrubMember(ObservabilityNaming.CustomAttributes.ResponseElapsed);
    }

    /// <summary>
    /// Failure (Generic Error - ErrorCodeExpected&lt;T&gt;) 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_FailureResponse_GenericError_Tags()
    {
        // Arrange
        var logger = _logContext.CreateLogger<TestObservabilityAdapterPipeline>();
        var pipeline = new TestObservabilityAdapterPipeline(
            _activitySource,
            logger,
            _meterFactory,
            _openTelemetryOptions)
        {
            GetByIdHandler = _ => TestErrors.CreateExpectedErrorT()
        };

        var testId = Guid.NewGuid();

        // Act - 에러가 발생해도 Activity 태그는 기록됨
        try
        {
            await pipeline.GetById(testId).Run().RunAsync();
        }
        catch
        {
            // 에러는 무시 - Activity 태그 검증이 목적
        }

        // Assert
        _capturedActivity.ShouldNotBeNull();
        var tags = _capturedActivity.TagObjects
            .OrderBy(t => t.Key)
            .ToDictionary(t => t.Key, t => t.Value?.ToString());

        await Verify(tags)
            .UseDirectory("Snapshots")
            .ScrubMember(ObservabilityNaming.CustomAttributes.ResponseElapsed);
    }

    #endregion

    #region Helper Types

    private sealed class TestMeterFactory : IMeterFactory
    {
        private readonly List<Meter> _meters = [];

        public Meter Create(MeterOptions options)
        {
            var meter = new Meter(options);
            _meters.Add(meter);
            return meter;
        }

        public void Dispose()
        {
            foreach (var meter in _meters)
            {
                meter.Dispose();
            }
            _meters.Clear();
        }
    }

    #endregion
}
