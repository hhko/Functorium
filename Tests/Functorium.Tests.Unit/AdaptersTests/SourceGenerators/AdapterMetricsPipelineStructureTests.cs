using System.Diagnostics;
using System.Diagnostics.Metrics;

using Functorium.Adapters.Observabilities;
using Functorium.Testing.Arrangements.Logging;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

using MsOptions = Microsoft.Extensions.Options.Options;

namespace Functorium.Tests.Unit.AdaptersTests.SourceGenerators;

/// <summary>
/// SourceGenerator로 생성된 Adapter Pipeline의 Metrics 태그 구조 검증 테스트.
/// 런타임에서 실제 Pipeline을 실행하고 메트릭 태그를 스냅샷으로 검증합니다.
/// </summary>
/// <remarks>
/// <para>
/// 이 테스트는 생성된 Pipeline 코드의 메트릭 태그 구조가 실수로 변경되는 것을 방지합니다.
/// </para>
/// <para>
/// 메트릭별 태그 구조 비교표:
/// </para>
/// <code>
/// +--------------------------+-------------------+-------------------+-------------------+
/// | Tag Key                  | requestCounter    | responseCounter   | responseCounter   |
/// |                          | durationHistogram | (success)         | (failure)         |
/// +--------------------------+-------------------+-------------------+-------------------+
/// | request.layer            | "adapter"         | "adapter"         | "adapter"         |
/// | request.category         | category name     | category name     | category name     |
/// | request.handler          | handler name      | handler name      | handler name      |
/// | request.handler.method   | method name       | method name       | method name       |
/// | response.status          | (none)            | "success"         | "failure"         |
/// | error.type               | (none)            | (none)            | "expected"/       |
/// |                          |                   |                   | "exceptional"/    |
/// |                          |                   |                   | "aggregate"       |
/// | error.code               | (none)            | (none)            | error code        |
/// +--------------------------+-------------------+-------------------+-------------------+
/// | Total Tags               | 4                 | 5                 | 7                 |
/// +--------------------------+-------------------+-------------------+-------------------+
/// </code>
/// </remarks>
[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters_SourceGenerator)]
public sealed class AdapterMetricsPipelineStructureTests : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly IMeterFactory _meterFactory;
    private readonly IOptions<OpenTelemetryOptions> _openTelemetryOptions;
    private readonly MeterListener _listener;
    private readonly List<CapturedMeasurement> _capturedMeasurements;
    private readonly LogTestContext _logContext;

    public AdapterMetricsPipelineStructureTests()
    {
        _activitySource = new ActivitySource("Test.AdapterMetrics");
        _meterFactory = new TestMeterFactory();
        _openTelemetryOptions = MsOptions.Create(new OpenTelemetryOptions { ServiceNamespace = "TestAdapterService" });
        _logContext = new LogTestContext();

        _capturedMeasurements = [];
        _listener = new MeterListener();
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            // Adapter 메트릭만 캡처 (다른 테스트와 구분하기 위해 고유한 서비스 이름 사용)
            if (instrument.Meter.Name.StartsWith("TestAdapterService"))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _listener.SetMeasurementEventCallback<double>(OnMeasurementRecordedDouble);
        _listener.Start();
    }

    public void Dispose()
    {
        _listener.Dispose();
        _activitySource.Dispose();
        (_meterFactory as IDisposable)?.Dispose();
        _logContext.Dispose();
    }

    private void OnMeasurementRecorded(
        Instrument instrument,
        long measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state)
    {
        _capturedMeasurements.Add(new CapturedMeasurement(
            instrument.Name,
            measurement,
            tags.ToArray()));
    }

    private void OnMeasurementRecordedDouble(
        Instrument instrument,
        double measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state)
    {
        _capturedMeasurements.Add(new CapturedMeasurement(
            instrument.Name,
            measurement,
            tags.ToArray()));
    }

    #region 태그 일관성 테스트

    /// <summary>
    /// durationHistogram은 requestCounter와 동일한 태그를 포함해야 합니다.
    /// </summary>
    [Fact]
    public async Task Handle_DurationTags_ShouldContainSameTagsAsRequestCounter()
    {
        // Arrange
        _capturedMeasurements.Clear();
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
        var requestMeasurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("requests"));
        var durationMeasurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("duration"));

        requestMeasurement.ShouldNotBeNull();
        durationMeasurement.ShouldNotBeNull();

        var requestTagKeys = requestMeasurement.Tags.Select(t => t.Key).OrderBy(k => k).ToArray();
        var durationTagKeys = durationMeasurement.Tags.Select(t => t.Key).OrderBy(k => k).ToArray();

        // durationTags는 requestTags의 키를 포함해야 함
        foreach (var key in requestTagKeys)
        {
            durationTagKeys.ShouldContain(key);
        }
    }

    /// <summary>
    /// responseCounter는 성공 시 5개 태그, 실패 시 7개 태그를 가져야 합니다.
    /// 실패 시 error.type과 error.code 태그가 추가됩니다.
    /// </summary>
    [Fact]
    public async Task Handle_SuccessAndFailureResponses_ShouldHaveDifferentTagCounts()
    {
        // Arrange
        _capturedMeasurements.Clear();
        var logger = _logContext.CreateLogger<TestObservabilityAdapterPipeline>();
        var pipeline = new TestObservabilityAdapterPipeline(
            _activitySource,
            logger,
            _meterFactory,
            _openTelemetryOptions);

        var testId = Guid.NewGuid();

        // Act - 성공 케이스
        await pipeline.GetById(testId).Run().RunAsync();

        var successMeasurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("responses"));

        // Act - 실패 케이스
        _capturedMeasurements.Clear();
        pipeline.GetByIdHandler = _ => TestErrors.CreateExpectedError();
        try
        {
            await pipeline.GetById(testId).Run().RunAsync();
        }
        catch
        {
            // 에러는 무시 - 메트릭 태그 검증이 목적
        }

        var failureMeasurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("responses"));

        // Assert
        successMeasurement.ShouldNotBeNull();
        failureMeasurement.ShouldNotBeNull();

        // 성공: 5개 태그 (requestTags 4개 + response.status 1개)
        successMeasurement.Tags.Length.ShouldBe(5);

        // 실패: 7개 태그 (requestTags 4개 + response.status 1개 + error.type 1개 + error.code 1개)
        failureMeasurement.Tags.Length.ShouldBe(7);

        // 메트릭 이름은 동일해야 함 (통합된 단일 카운터)
        successMeasurement.InstrumentName.ShouldBe(failureMeasurement.InstrumentName);
    }

    #endregion

    #region Snapshot 태그 구조 테스트

    /// <summary>
    /// Request 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_RequestTags()
    {
        // Arrange
        _capturedMeasurements.Clear();
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
        var measurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("requests"));
        measurement.ShouldNotBeNull();

        var tags = measurement.Tags
            .OrderBy(t => t.Key)
            .ToDictionary(t => t.Key, t => t.Value?.ToString());

        await Verify(tags).UseDirectory("Snapshots");
    }

    /// <summary>
    /// Success Response 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_SuccessResponse_Tags()
    {
        // Arrange
        _capturedMeasurements.Clear();
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
        var measurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("responses"));
        measurement.ShouldNotBeNull();

        var tags = measurement.Tags
            .OrderBy(t => t.Key)
            .ToDictionary(t => t.Key, t => t.Value?.ToString());

        await Verify(tags).UseDirectory("Snapshots");
    }

    /// <summary>
    /// Failure Response (Expected Error) 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_FailureResponse_ExpectedError_Tags()
    {
        // Arrange
        _capturedMeasurements.Clear();
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

        // Act - 에러가 발생해도 메트릭은 기록됨
        try
        {
            await pipeline.GetById(testId).Run().RunAsync();
        }
        catch
        {
            // 에러는 무시 - 메트릭 태그 검증이 목적
        }

        // Assert
        var measurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("responses"));
        measurement.ShouldNotBeNull();

        var tags = measurement.Tags
            .OrderBy(t => t.Key)
            .ToDictionary(t => t.Key, t => t.Value?.ToString());

        await Verify(tags).UseDirectory("Snapshots");
    }

    /// <summary>
    /// Failure Response (Exceptional Error) 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_FailureResponse_ExceptionalError_Tags()
    {
        // Arrange
        _capturedMeasurements.Clear();
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

        // Act - 에러가 발생해도 메트릭은 기록됨
        try
        {
            await pipeline.GetById(testId).Run().RunAsync();
        }
        catch
        {
            // 에러는 무시 - 메트릭 태그 검증이 목적
        }

        // Assert
        var measurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("responses"));
        measurement.ShouldNotBeNull();

        var tags = measurement.Tags
            .OrderBy(t => t.Key)
            .ToDictionary(t => t.Key, t => t.Value?.ToString());

        await Verify(tags).UseDirectory("Snapshots");
    }

    /// <summary>
    /// Failure Response (Aggregate Error) 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_FailureResponse_AggregateError_Tags()
    {
        // Arrange
        _capturedMeasurements.Clear();
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

        // Act - 에러가 발생해도 메트릭은 기록됨
        try
        {
            await pipeline.GetById(testId).Run().RunAsync();
        }
        catch
        {
            // 에러는 무시 - 메트릭 태그 검증이 목적
        }

        // Assert
        var measurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("responses"));
        measurement.ShouldNotBeNull();

        var tags = measurement.Tags
            .OrderBy(t => t.Key)
            .ToDictionary(t => t.Key, t => t.Value?.ToString());

        await Verify(tags).UseDirectory("Snapshots");
    }

    /// <summary>
    /// Duration Histogram 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_DurationHistogram_Tags()
    {
        // Arrange
        _capturedMeasurements.Clear();
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
        var measurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("duration"));
        measurement.ShouldNotBeNull();

        var tags = measurement.Tags
            .OrderBy(t => t.Key)
            .ToDictionary(t => t.Key, t => t.Value?.ToString());

        await Verify(tags).UseDirectory("Snapshots");
    }

    #endregion

    #region Helper Types

    private sealed record CapturedMeasurement(
        string InstrumentName,
        object Value,
        KeyValuePair<string, object?>[] Tags);

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
