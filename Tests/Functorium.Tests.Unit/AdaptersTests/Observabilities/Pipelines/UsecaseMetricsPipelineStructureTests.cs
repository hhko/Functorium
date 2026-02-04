using System.Diagnostics.Metrics;

using Functorium.Abstractions.Errors;
using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Naming;
using Functorium.Adapters.Observabilities.Pipelines;
using Functorium.Applications.Cqrs;

using LanguageExt.Common;

using Mediator;

using Microsoft.Extensions.Options;

using MsOptions = Microsoft.Extensions.Options.Options;

using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines;

/// <summary>
/// UsecaseMetricsPipeline의 태그 구조를 검증하는 테스트입니다.
/// </summary>
/// <remarks>
/// <para>
/// 이 테스트는 메트릭 태그 구조가 실수로 변경되는 것을 방지합니다.
/// </para>
/// <para>
/// 메트릭별 태그 구조 비교표 (옵션 A: 에러 태그 포함):
/// </para>
/// <code>
/// +--------------------------+-------------------+-------------------+-------------------+
/// | Tag Key                  | requestCounter    | responseCounter   | responseCounter   |
/// |                          | durationHistogram | (success)         | (failure)         |
/// +--------------------------+-------------------+-------------------+-------------------+
/// | request.layer            | "application"     | "application"     | "application"     |
/// | request.category         | "usecase"         | "usecase"         | "usecase"         |
/// | request.category.type     | "command"/"query" | "command"/"query" | "command"/"query" |
/// | request.handler          | handler name      | handler name      | handler name      |
/// | request.handler.method   | "Handle"          | "Handle"          | "Handle"          |
/// | response.status          | (none)            | "success"         | "failure"         |
/// | error.type               | (none)            | (none)            | "expected"/       |
/// |                          |                   |                   | "exceptional"/    |
/// |                          |                   |                   | "aggregate"       |
/// | error.code               | (none)            | (none)            | error code        |
/// +--------------------------+-------------------+-------------------+-------------------+
/// | Total Tags               | 5                 | 6                 | 8                 |
/// +--------------------------+-------------------+-------------------+-------------------+
/// </code>
/// </remarks>
[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public class UsecaseMetricsPipelineStructureTests : IDisposable
{
    private readonly IMeterFactory _meterFactory;
    private readonly IOptions<OpenTelemetryOptions> _openTelemetryOptions;
    private readonly MeterListener _listener;
    private readonly List<CapturedMeasurement> _capturedMeasurements;

    public UsecaseMetricsPipelineStructureTests()
    {
        _meterFactory = new TestMeterFactory();
        _openTelemetryOptions = MsOptions.Create(new OpenTelemetryOptions { ServiceNamespace = "TestUsecaseService" });

        _capturedMeasurements = new List<CapturedMeasurement>();
        _listener = new MeterListener();
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name.StartsWith("TestUsecaseService"))
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
        var sut = new UsecaseMetricsPipeline<TestCommandRequest, TestResponse>(
            _openTelemetryOptions,
            _meterFactory);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, Next, CancellationToken.None);

        // Assert
        var requestMeasurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("requests"));
        var durationMeasurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("duration"));

        requestMeasurement.ShouldNotBeNull();
        durationMeasurement.ShouldNotBeNull();

        var requestTagKeys = requestMeasurement.Tags.Select(t => t.Key).OrderBy(k => k).ToArray();
        var durationTagKeys = durationMeasurement.Tags.Select(t => t.Key).OrderBy(k => k).ToArray();

        // durationTags는 requestTags와 동일한 키를 가져야 함
        requestTagKeys.ShouldBe(durationTagKeys);
    }

    /// <summary>
    /// responseCounter는 성공 시 6개 태그, 실패 시 8개 태그를 가져야 합니다.
    /// 실패 시 error.type과 error.code 태그가 추가됩니다.
    /// </summary>
    [Fact]
    public async Task Handle_SuccessAndFailureResponses_ShouldHaveDifferentTagCounts()
    {
        // Arrange
        _capturedMeasurements.Clear();
        var sut = new UsecaseMetricsPipeline<TestCommandRequest, TestResponseWithError>(
            _openTelemetryOptions,
            _meterFactory);
        var request = new TestCommandRequest();

        // Act - 성공 케이스
        await sut.Handle(request, NextSuccessWithError, CancellationToken.None);

        var successMeasurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("responses") && !m.InstrumentName.Contains("requests"));

        // Act - 실패 케이스 (새 인스턴스로)
        _capturedMeasurements.Clear();
        var sut2 = new UsecaseMetricsPipeline<TestCommandRequest, TestResponseWithError>(
            _openTelemetryOptions,
            _meterFactory);
        await sut2.Handle(request, NextFailWithError, CancellationToken.None);

        var failureMeasurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("responses") && !m.InstrumentName.Contains("requests"));

        // Assert
        successMeasurement.ShouldNotBeNull();
        failureMeasurement.ShouldNotBeNull();

        // 성공: 6개 태그 (requestTags 5개 + response.status 1개)
        successMeasurement.Tags.Length.ShouldBe(6);

        // 실패: 8개 태그 (requestTags 5개 + response.status 1개 + error.type 1개 + error.code 1개)
        failureMeasurement.Tags.Length.ShouldBe(8);

        // 메트릭 이름은 동일해야 함 (통합된 단일 카운터)
        successMeasurement.InstrumentName.ShouldBe(failureMeasurement.InstrumentName);
    }

    #endregion

    #region Snapshot 태그 구조 테스트

    /// <summary>
    /// Command Request 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_Command_RequestTags()
    {
        // Arrange
        _capturedMeasurements.Clear();
        var sut = new UsecaseMetricsPipeline<TestCommandRequest, TestResponse>(
            _openTelemetryOptions,
            _meterFactory);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, Next, CancellationToken.None);

        // Assert
        var tags = ExtractAndOrderTags("requests");
        await Verify(tags).UseDirectory("Snapshots");
    }

    /// <summary>
    /// Query Request 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_Query_RequestTags()
    {
        // Arrange
        _capturedMeasurements.Clear();
        var sut = new UsecaseMetricsPipeline<TestQueryRequest, TestResponse>(
            _openTelemetryOptions,
            _meterFactory);
        var request = new TestQueryRequest();

        // Act
        await sut.Handle(request, NextQuery, CancellationToken.None);

        // Assert
        var tags = ExtractAndOrderTags("requests");
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
        var sut = new UsecaseMetricsPipeline<TestCommandRequest, TestResponse>(
            _openTelemetryOptions,
            _meterFactory);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, Next, CancellationToken.None);

        // Assert
        var tags = ExtractAndOrderTags("responses", "requests");
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
        var sut = new UsecaseMetricsPipeline<TestCommandRequest, TestResponseWithError>(
            _openTelemetryOptions,
            _meterFactory);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextFailWithGenericError, CancellationToken.None);

        // Assert
        var tags = ExtractAndOrderTags("responses", "requests");
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
        var sut = new UsecaseMetricsPipeline<TestCommandRequest, TestResponseWithError>(
            _openTelemetryOptions,
            _meterFactory);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextFailWithExceptionalError, CancellationToken.None);

        // Assert
        var tags = ExtractAndOrderTags("responses", "requests");
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
        var sut = new UsecaseMetricsPipeline<TestCommandRequest, TestResponseWithError>(
            _openTelemetryOptions,
            _meterFactory);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextFailWithAggregateError, CancellationToken.None);

        // Assert
        var tags = ExtractAndOrderTags("responses", "requests");
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
        var sut = new UsecaseMetricsPipeline<TestCommandRequest, TestResponse>(
            _openTelemetryOptions,
            _meterFactory);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, Next, CancellationToken.None);

        // Assert
        var tags = ExtractAndOrderTags("duration");
        await Verify(tags).UseDirectory("Snapshots");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 캡처된 측정값에서 패턴에 맞는 태그를 추출하고 정렬된 Dictionary로 반환합니다.
    /// </summary>
    /// <param name="instrumentPattern">측정값 이름에 포함되어야 할 패턴</param>
    /// <param name="excludePattern">측정값 이름에서 제외할 패턴 (선택사항)</param>
    private Dictionary<string, string?> ExtractAndOrderTags(string instrumentPattern, string? excludePattern = null)
    {
        CapturedMeasurement? measurement;

        if (excludePattern != null)
        {
            measurement = _capturedMeasurements
                .FirstOrDefault(m => m.InstrumentName.Contains(instrumentPattern) && !m.InstrumentName.Contains(excludePattern));
        }
        else
        {
            measurement = _capturedMeasurements
                .FirstOrDefault(m => m.InstrumentName.Contains(instrumentPattern));
        }

        measurement.ShouldNotBeNull($"Measurement containing '{instrumentPattern}' should exist");

        return measurement.Tags
            .OrderBy(t => t.Key)
            .ToDictionary(t => t.Key, t => t.Value?.ToString());
    }

    private static void AssertTagValue(
        KeyValuePair<string, object?>[] tags,
        string key,
        string expectedValue)
    {
        var tag = tags.FirstOrDefault(t => t.Key == key);
        tag.Key.ShouldNotBeNull($"Tag '{key}' should exist");
        tag.Value.ShouldBe(expectedValue);
    }

    private static ValueTask<TestResponse> Next(
        TestCommandRequest request,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(TestResponse.CreateSuccess());
    }

    private static ValueTask<TestResponse> NextFail(
        TestCommandRequest request,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(TestResponse.CreateFailure());
    }

    private static ValueTask<TestResponseWithError> NextSuccessWithError(
        TestCommandRequest request,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(TestResponseWithError.CreateSuccess());
    }

    private static ValueTask<TestResponseWithError> NextFailWithError(
        TestCommandRequest request,
        CancellationToken cancellationToken)
    {
        // Expected 에러로 실패
        var error = Error.New("Test validation error");
        return ValueTask.FromResult(TestResponseWithError.CreateFail(error));
    }

    private static ValueTask<TestResponseWithError> NextFailWithGenericError(
        TestCommandRequest request,
        CancellationToken cancellationToken)
    {
        // Generic ErrorCodeExpected<T>로 실패
        var error = new ErrorCodeExpected<int>("Test.GenericError", 42, "Generic error occurred");
        return ValueTask.FromResult(TestResponseWithError.CreateFail(error));
    }

    private static ValueTask<TestResponse> NextQuery(
        TestQueryRequest request,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(TestResponse.CreateSuccess());
    }

    private static ValueTask<TestResponseWithError> NextFailWithExceptionalError(
        TestCommandRequest request,
        CancellationToken cancellationToken)
    {
        // Exceptional 에러로 실패
        var exception = new InvalidOperationException("Exceptional error occurred");
        var error = new ErrorCodeExceptional("Test.ExceptionalError", exception);
        return ValueTask.FromResult(TestResponseWithError.CreateFail(error));
    }

    private static ValueTask<TestResponseWithError> NextFailWithAggregateError(
        TestCommandRequest request,
        CancellationToken cancellationToken)
    {
        // Aggregate 에러로 실패
        var errors = new Error[]
        {
            new ErrorCodeExpected("Test.Error1", "value1", "First error"),
            new ErrorCodeExpected("Test.Error2", "value2", "Second error")
        };
        var error = Error.Many(errors);
        return ValueTask.FromResult(TestResponseWithError.CreateFail(error));
    }

    #endregion

    #region Test Types

    private sealed record CapturedMeasurement(
        string InstrumentName,
        object Value,
        KeyValuePair<string, object?>[] Tags);

    private sealed class TestMeterFactory : IMeterFactory
    {
        private readonly List<Meter> _meters = new();

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

    private sealed record TestCommandRequest : ICommandRequest<TestSuccessData>;

    private sealed record TestQueryRequest : IQueryRequest<TestSuccessData>;

    private sealed record TestSuccessData(Guid Id, string Name);

    private sealed record TestResponse : IFinResponse, IFinResponseFactory<TestResponse>
    {
        private readonly bool _isSucc;

        private TestResponse(bool isSucc) => _isSucc = isSucc;

        public bool IsSucc => _isSucc;
        public bool IsFail => !_isSucc;

        public static TestResponse CreateSuccess() => new(true);
        public static TestResponse CreateFailure() => new(false);

        public static TestResponse CreateFail(Error error) => CreateFailure();
    }

    /// <summary>
    /// IFinResponseWithError를 구현하는 테스트용 Response.
    /// 에러 태그 테스트에 사용됩니다.
    /// </summary>
    private sealed record TestResponseWithError : IFinResponse, IFinResponseFactory<TestResponseWithError>, IFinResponseWithError
    {
        private readonly bool _isSucc;
        private readonly Error? _error;

        private TestResponseWithError(bool isSucc, Error? error = null)
        {
            _isSucc = isSucc;
            _error = error;
        }

        public bool IsSucc => _isSucc;
        public bool IsFail => !_isSucc;
        public Error Error => _error ?? Error.New("Unknown error");

        public static TestResponseWithError CreateSuccess() => new(true);
        public static TestResponseWithError CreateFail(Error error) => new(false, error);
    }

    #endregion
}
