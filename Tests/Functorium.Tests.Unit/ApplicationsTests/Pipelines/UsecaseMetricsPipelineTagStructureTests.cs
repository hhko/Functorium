using System.Diagnostics.Metrics;

using Functorium.Adapters.Observabilities;
using Functorium.Applications.Cqrs;
using Functorium.Applications.Observabilities;
using Functorium.Applications.Pipelines;

using LanguageExt.Common;

using Mediator;

using NSubstitute;

using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.ApplicationsTests.Pipelines;

/// <summary>
/// UsecaseMetricsPipeline의 태그 구조를 검증하는 테스트입니다.
/// </summary>
/// <remarks>
/// <para>
/// 이 테스트는 메트릭 태그 구조가 실수로 변경되는 것을 방지합니다.
/// </para>
/// <para>
/// 메트릭별 태그 구조 비교표 (통일된 구조):
/// </para>
/// <code>
/// ┌──────────────────────────┬─────────────────────────┬─────────────────────────┐
/// │ Tag Key                  │ requestCounter          │ responseCounter         │
/// │                          │ durationHistogram       │                         │
/// ├──────────────────────────┼─────────────────────────┼─────────────────────────┤
/// │ request.layer            │ "application"           │ "application"           │
/// │ request.category         │ "usecase"               │ "usecase"               │
/// │ request.handler.cqrs     │ "command"/"query"       │ "command"/"query"       │
/// │ request.handler          │ handler name            │ handler name            │
/// │ request.handler.method   │ "Handle"                │ "Handle"                │
/// │ response.status          │ (none)                  │ "success"/"failure"     │
/// ├──────────────────────────┼─────────────────────────┼─────────────────────────┤
/// │ Total Tags               │ 5                       │ 6                       │
/// └──────────────────────────┴─────────────────────────┴─────────────────────────┘
/// </code>
/// </remarks>
[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public class UsecaseMetricsPipelineTagStructureTests : IDisposable
{
    private readonly IMeterFactory _meterFactory;
    private readonly IOpenTelemetryOptions _openTelemetryOptions;
    private readonly MeterListener _listener;
    private readonly List<CapturedMeasurement> _capturedMeasurements;

    public UsecaseMetricsPipelineTagStructureTests()
    {
        _meterFactory = new TestMeterFactory();
        _openTelemetryOptions = Substitute.For<IOpenTelemetryOptions>();
        _openTelemetryOptions.ServiceNamespace.Returns("TestService");

        _capturedMeasurements = new List<CapturedMeasurement>();
        _listener = new MeterListener();
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name.StartsWith("TestService"))
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

    #region 요청 카운터 태그 구조 테스트

    /// <summary>
    /// requestCounter는 기본 5개 태그를 포함해야 합니다 (통일된 구조).
    /// </summary>
    [Fact]
    public async Task Handle_RequestCounterTags_ShouldContainBaseTags()
    {
        // Arrange
        var sut = new UsecaseMetricsPipeline<TestCommandRequest, TestResponse>(
            _openTelemetryOptions,
            _meterFactory);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, Next, CancellationToken.None);

        // Assert
        var requestMeasurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("requests"));

        requestMeasurement.ShouldNotBeNull();

        // 태그 구조 검증: 5개 태그 (통일된 baseTags)
        requestMeasurement.Tags.Length.ShouldBe(5);

        // 기본 태그가 있어야 함
        requestMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.RequestLayer);
        requestMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.RequestCategory);
        requestMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.RequestHandlerCqrs);
        requestMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.RequestHandler);
        requestMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.RequestHandlerMethod);

        // ResponseStatus 태그가 없어야 함
        requestMeasurement.Tags
            .ShouldNotContain(t => t.Key == ObservabilityNaming.CustomAttributes.ResponseStatus);
    }

    /// <summary>
    /// requestCounter는 올바른 태그 값을 가져야 합니다.
    /// </summary>
    [Fact]
    public async Task Handle_RequestCounterTags_ShouldHaveCorrectValues()
    {
        // Arrange
        var sut = new UsecaseMetricsPipeline<TestCommandRequest, TestResponse>(
            _openTelemetryOptions,
            _meterFactory);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, Next, CancellationToken.None);

        // Assert
        var requestMeasurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("requests"));

        requestMeasurement.ShouldNotBeNull();

        AssertTagValue(requestMeasurement.Tags,
            ObservabilityNaming.CustomAttributes.RequestLayer,
            ObservabilityNaming.Layers.Application);

        AssertTagValue(requestMeasurement.Tags,
            ObservabilityNaming.CustomAttributes.RequestCategory,
            ObservabilityNaming.Categories.Usecase);

        AssertTagValue(requestMeasurement.Tags,
            ObservabilityNaming.CustomAttributes.RequestHandlerMethod,
            "Handle");
    }

    #endregion

    #region 응답 카운터 태그 구조 테스트

    /// <summary>
    /// responseCounter는 성공 시 ResponseStatus 태그에 "success" 값을 가져야 합니다.
    /// </summary>
    [Fact]
    public async Task Handle_ResponseCounterTags_OnSuccess_ShouldContainSuccessStatus()
    {
        // Arrange
        var sut = new UsecaseMetricsPipeline<TestCommandRequest, TestResponse>(
            _openTelemetryOptions,
            _meterFactory);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, Next, CancellationToken.None);

        // Assert
        var responseMeasurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("responses") && !m.InstrumentName.Contains("requests"));

        responseMeasurement.ShouldNotBeNull();

        // 태그 구조 검증: 6개 태그 (requestTags 5개 + ResponseStatus 1개)
        responseMeasurement.Tags.Length.ShouldBe(6);

        // ResponseStatus 태그가 있어야 함
        responseMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.ResponseStatus);

        // ResponseStatus 값이 "success"여야 함
        AssertTagValue(responseMeasurement.Tags,
            ObservabilityNaming.CustomAttributes.ResponseStatus,
            ObservabilityNaming.Status.Success);
    }

    /// <summary>
    /// responseCounter는 성공 시 requestTags + ResponseStatus 태그를 포함해야 합니다.
    /// </summary>
    [Fact]
    public async Task Handle_ResponseCounterTags_OnSuccess_ShouldContainRequestTagsPlusResponseStatus()
    {
        // Arrange
        var sut = new UsecaseMetricsPipeline<TestCommandRequest, TestResponse>(
            _openTelemetryOptions,
            _meterFactory);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, Next, CancellationToken.None);

        // Assert
        var responseMeasurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("responses") && !m.InstrumentName.Contains("requests"));

        responseMeasurement.ShouldNotBeNull();

        // requestTags (5개) + ResponseStatus (1개) = 6개
        responseMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.RequestLayer);
        responseMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.RequestCategory);
        responseMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.RequestHandlerCqrs);
        responseMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.RequestHandler);
        responseMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.RequestHandlerMethod);
        responseMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.ResponseStatus);
    }

    /// <summary>
    /// responseCounter는 실패 시 ResponseStatus 태그에 "failure" 값을 가져야 합니다.
    /// </summary>
    [Fact]
    public async Task Handle_ResponseCounterTags_OnFailure_ShouldContainFailureStatus()
    {
        // Arrange
        var sut = new UsecaseMetricsPipeline<TestCommandRequest, TestResponse>(
            _openTelemetryOptions,
            _meterFactory);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextFail, CancellationToken.None);

        // Assert
        var responseMeasurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("responses") && !m.InstrumentName.Contains("requests"));

        responseMeasurement.ShouldNotBeNull();

        // 태그 구조 검증: 6개 태그 (requestTags 5개 + ResponseStatus 1개)
        responseMeasurement.Tags.Length.ShouldBe(6);

        // ResponseStatus 태그가 있어야 함
        responseMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.ResponseStatus);

        // ResponseStatus 값이 "failure"여야 함
        AssertTagValue(responseMeasurement.Tags,
            ObservabilityNaming.CustomAttributes.ResponseStatus,
            ObservabilityNaming.Status.Failure);
    }

    /// <summary>
    /// responseCounter는 실패 시 requestTags + ResponseStatus 태그를 포함해야 합니다.
    /// </summary>
    [Fact]
    public async Task Handle_ResponseCounterTags_OnFailure_ShouldContainRequestTagsPlusResponseStatus()
    {
        // Arrange
        var sut = new UsecaseMetricsPipeline<TestCommandRequest, TestResponse>(
            _openTelemetryOptions,
            _meterFactory);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextFail, CancellationToken.None);

        // Assert
        var responseMeasurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("responses") && !m.InstrumentName.Contains("requests"));

        responseMeasurement.ShouldNotBeNull();

        // requestTags (5개) + ResponseStatus (1개) = 6개
        responseMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.RequestLayer);
        responseMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.RequestCategory);
        responseMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.RequestHandlerCqrs);
        responseMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.RequestHandler);
        responseMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.RequestHandlerMethod);
        responseMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.ResponseStatus);
    }

    #endregion

    #region 히스토그램 태그 구조 테스트

    /// <summary>
    /// durationHistogram은 기본 5개 태그를 포함해야 합니다 (통일된 구조).
    /// </summary>
    [Fact]
    public async Task Handle_DurationHistogramTags_ShouldContainRequestTags()
    {
        // Arrange
        var sut = new UsecaseMetricsPipeline<TestCommandRequest, TestResponse>(
            _openTelemetryOptions,
            _meterFactory);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, Next, CancellationToken.None);

        // Assert
        var durationMeasurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("duration"));

        durationMeasurement.ShouldNotBeNull();

        // 태그 구조 검증: 5개 태그 (통일된 requestTags)
        durationMeasurement.Tags.Length.ShouldBe(5);

        // 기본 태그가 있어야 함
        durationMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.RequestLayer);
        durationMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.RequestCategory);
        durationMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.RequestHandlerCqrs);
        durationMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.RequestHandler);
        durationMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.RequestHandlerMethod);

        // ResponseStatus 태그가 없어야 함
        durationMeasurement.Tags
            .ShouldNotContain(t => t.Key == ObservabilityNaming.CustomAttributes.ResponseStatus);
    }

    #endregion

    #region 태그 일관성 테스트

    /// <summary>
    /// requestCounter와 durationHistogram은 동일한 태그 키를 가져야 합니다.
    /// </summary>
    [Fact]
    public async Task Handle_RequestAndDurationTags_ShouldHaveSameKeys()
    {
        // Arrange
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

        requestTagKeys.ShouldBe(durationTagKeys);
    }

    /// <summary>
    /// responseCounter는 성공과 실패 시 동일한 태그 키를 가져야 합니다 (값만 다름).
    /// </summary>
    [Fact]
    public async Task Handle_SuccessAndFailureResponses_ShouldHaveSameTagKeys()
    {
        // Arrange
        var sut = new UsecaseMetricsPipeline<TestCommandRequest, TestResponse>(
            _openTelemetryOptions,
            _meterFactory);
        var request = new TestCommandRequest();

        // Act - 성공 케이스
        await sut.Handle(request, Next, CancellationToken.None);

        var successMeasurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("responses") && !m.InstrumentName.Contains("requests"));

        // Act - 실패 케이스 (새 인스턴스로)
        _capturedMeasurements.Clear();
        var sut2 = new UsecaseMetricsPipeline<TestCommandRequest, TestResponse>(
            _openTelemetryOptions,
            _meterFactory);
        await sut2.Handle(request, NextFail, CancellationToken.None);

        var failureMeasurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("responses") && !m.InstrumentName.Contains("requests"));

        // Assert
        successMeasurement.ShouldNotBeNull();
        failureMeasurement.ShouldNotBeNull();

        var successTagKeys = successMeasurement.Tags.Select(t => t.Key).OrderBy(k => k).ToArray();
        var failureTagKeys = failureMeasurement.Tags.Select(t => t.Key).OrderBy(k => k).ToArray();

        successTagKeys.ShouldBe(failureTagKeys);

        // 메트릭 이름도 동일해야 함 (통합된 단일 카운터)
        successMeasurement.InstrumentName.ShouldBe(failureMeasurement.InstrumentName);
    }

    #endregion

    #region Helper Methods

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

    private sealed record TestCommandRequest : IMessage, ICommand<TestResponse>;

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

    #endregion
}
