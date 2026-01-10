using System.Diagnostics.Metrics;

using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Configurations;
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
/// ┌──────────────────────────┬─────────────────────────┬─────────────────────────┬─────────────────────────┐
/// │ Tag Key                  │ requestCounter          │ responseCounter         │ responseCounter         │
/// │                          │ durationHistogram       │ (success)               │ (failure)               │
/// ├──────────────────────────┼─────────────────────────┼─────────────────────────┼─────────────────────────┤
/// │ request.layer            │ "application"           │ "application"           │ "application"           │
/// │ request.category         │ "usecase"               │ "usecase"               │ "usecase"               │
/// │ request.handler.cqrs     │ "command"/"query"       │ "command"/"query"       │ "command"/"query"       │
/// │ request.handler          │ handler name            │ handler name            │ handler name            │
/// │ request.handler.method   │ "Handle"                │ "Handle"                │ "Handle"                │
/// │ response.status          │ (none)                  │ "success"               │ "failure"               │
/// │ error.type               │ (none)                  │ (none)                  │ "expected"/"exceptional"│
/// │ error.code               │ (none)                  │ (none)                  │ error code              │
/// ├──────────────────────────┼─────────────────────────┼─────────────────────────┼─────────────────────────┤
/// │ Total Tags               │ 5                       │ 6                       │ 8                       │
/// └──────────────────────────┴─────────────────────────┴─────────────────────────┴─────────────────────────┘
/// </code>
/// </remarks>
[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public class UsecaseMetricsPipelineTagStructureTests : IDisposable
{
    private readonly IMeterFactory _meterFactory;
    private readonly IOptions<OpenTelemetryOptions> _openTelemetryOptions;
    private readonly IOptions<SloConfiguration> _sloConfigurationOptions;
    private readonly MeterListener _listener;
    private readonly List<CapturedMeasurement> _capturedMeasurements;

    public UsecaseMetricsPipelineTagStructureTests()
    {
        _meterFactory = new TestMeterFactory();
        _openTelemetryOptions = MsOptions.Create(new OpenTelemetryOptions { ServiceNamespace = "TestService" });
        _sloConfigurationOptions = MsOptions.Create(new SloConfiguration());

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
            _meterFactory,
            _sloConfigurationOptions);
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
            _meterFactory,
            _sloConfigurationOptions);
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
            _meterFactory,
            _sloConfigurationOptions);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, Next, CancellationToken.None);

        // Assert
        var responseMeasurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("responses") && !m.InstrumentName.Contains("requests"));

        responseMeasurement.ShouldNotBeNull();

        // 태그 구조 검증: 7개 태그 (requestTags 5개 + slo.latency 1개 + response.status 1개)
        responseMeasurement.Tags.Length.ShouldBe(7);

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
            _meterFactory,
            _sloConfigurationOptions);
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
        var sut = new UsecaseMetricsPipeline<TestCommandRequest, TestResponseWithError>(
            _openTelemetryOptions,
            _meterFactory,
            _sloConfigurationOptions);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextFailWithError, CancellationToken.None);

        // Assert
        var responseMeasurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("responses") && !m.InstrumentName.Contains("requests"));

        responseMeasurement.ShouldNotBeNull();

        // 태그 구조 검증: 9개 태그 (requestTags 5개 + slo.latency 1개 + response.status 1개 + error.type 1개 + error.code 1개)
        responseMeasurement.Tags.Length.ShouldBe(9);

        // ResponseStatus 태그가 있어야 함
        responseMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.ResponseStatus);

        // ResponseStatus 값이 "failure"여야 함
        AssertTagValue(responseMeasurement.Tags,
            ObservabilityNaming.CustomAttributes.ResponseStatus,
            ObservabilityNaming.Status.Failure);
    }

    /// <summary>
    /// responseCounter는 실패 시 requestTags + ResponseStatus + error.type + error.code 태그를 포함해야 합니다.
    /// </summary>
    [Fact]
    public async Task Handle_ResponseCounterTags_OnFailure_ShouldContainErrorTags()
    {
        // Arrange
        var sut = new UsecaseMetricsPipeline<TestCommandRequest, TestResponseWithError>(
            _openTelemetryOptions,
            _meterFactory,
            _sloConfigurationOptions);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextFailWithError, CancellationToken.None);

        // Assert
        var responseMeasurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("responses") && !m.InstrumentName.Contains("requests"));

        responseMeasurement.ShouldNotBeNull();

        // requestTags (5개) + ResponseStatus (1개) + error.type (1개) + error.code (1개) = 8개
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
        responseMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.OTelAttributes.ErrorType);
        responseMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.ErrorCode);
    }

    /// <summary>
    /// responseCounter는 Expected 에러 시 error.type이 "expected"여야 합니다.
    /// </summary>
    [Fact]
    public async Task Handle_ResponseCounterTags_OnExpectedError_ShouldHaveExpectedErrorType()
    {
        // Arrange
        var sut = new UsecaseMetricsPipeline<TestCommandRequest, TestResponseWithError>(
            _openTelemetryOptions,
            _meterFactory,
            _sloConfigurationOptions);
        var request = new TestCommandRequest();

        // Act - Expected 에러로 실패
        await sut.Handle(request, NextFailWithError, CancellationToken.None);

        // Assert
        var responseMeasurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("responses") && !m.InstrumentName.Contains("requests"));

        responseMeasurement.ShouldNotBeNull();

        // error.type 값 검증
        AssertTagValue(responseMeasurement.Tags,
            ObservabilityNaming.OTelAttributes.ErrorType,
            ObservabilityNaming.ErrorTypes.Expected);

        // error.code 값 검증 (Expected 에러는 타입명을 사용)
        responseMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.ErrorCode);
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
            _meterFactory,
            _sloConfigurationOptions);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, Next, CancellationToken.None);

        // Assert
        var durationMeasurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("duration"));

        durationMeasurement.ShouldNotBeNull();

        // 태그 구조 검증: 6개 태그 (requestTags 5개 + slo.latency 1개)
        durationMeasurement.Tags.Length.ShouldBe(6);

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
        durationMeasurement.Tags
            .ShouldContain(t => t.Key == ObservabilityNaming.CustomAttributes.SloLatency);

        // ResponseStatus 태그가 없어야 함
        durationMeasurement.Tags
            .ShouldNotContain(t => t.Key == ObservabilityNaming.CustomAttributes.ResponseStatus);
    }

    #endregion

    #region 태그 일관성 테스트

    /// <summary>
    /// durationHistogram은 requestCounter의 모든 태그 + slo.latency 태그를 포함해야 합니다.
    /// </summary>
    [Fact]
    public async Task Handle_DurationTags_ShouldContainRequestTagsPlusSloTag()
    {
        // Arrange
        var sut = new UsecaseMetricsPipeline<TestCommandRequest, TestResponse>(
            _openTelemetryOptions,
            _meterFactory,
            _sloConfigurationOptions);
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

        // durationTags는 requestTags의 모든 키를 포함해야 함
        foreach (var key in requestTagKeys)
        {
            durationTagKeys.ShouldContain(key);
        }

        // durationTags에만 slo.latency 태그가 추가됨
        durationTagKeys.ShouldContain(ObservabilityNaming.CustomAttributes.SloLatency);
        requestTagKeys.ShouldNotContain(ObservabilityNaming.CustomAttributes.SloLatency);
    }

    /// <summary>
    /// responseCounter는 성공 시 7개 태그, 실패 시 9개 태그를 가져야 합니다.
    /// 실패 시 error.type과 error.code 태그가 추가됩니다.
    /// </summary>
    [Fact]
    public async Task Handle_SuccessAndFailureResponses_ShouldHaveDifferentTagCounts()
    {
        // Arrange
        var sut = new UsecaseMetricsPipeline<TestCommandRequest, TestResponseWithError>(
            _openTelemetryOptions,
            _meterFactory,
            _sloConfigurationOptions);
        var request = new TestCommandRequest();

        // Act - 성공 케이스
        await sut.Handle(request, NextSuccessWithError, CancellationToken.None);

        var successMeasurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("responses") && !m.InstrumentName.Contains("requests"));

        // Act - 실패 케이스 (새 인스턴스로)
        _capturedMeasurements.Clear();
        var sut2 = new UsecaseMetricsPipeline<TestCommandRequest, TestResponseWithError>(
            _openTelemetryOptions,
            _meterFactory,
            _sloConfigurationOptions);
        await sut2.Handle(request, NextFailWithError, CancellationToken.None);

        var failureMeasurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("responses") && !m.InstrumentName.Contains("requests"));

        // Assert
        successMeasurement.ShouldNotBeNull();
        failureMeasurement.ShouldNotBeNull();

        // 성공: 7개 태그 (requestTags 5개 + slo.latency.exceeded 1개 + response.status 1개)
        successMeasurement.Tags.Length.ShouldBe(7);

        // 실패: 9개 태그 (requestTags 5개 + slo.latency.exceeded 1개 + response.status 1개 + error.type 1개 + error.code 1개)
        failureMeasurement.Tags.Length.ShouldBe(9);

        // 메트릭 이름은 동일해야 함 (통합된 단일 카운터)
        successMeasurement.InstrumentName.ShouldBe(failureMeasurement.InstrumentName);
    }

    /// <summary>
    /// 제네릭 ErrorCodeExpected&lt;T&gt; 타입도 IHasErrorCode 인터페이스를 통해
    /// 올바른 error.type과 error.code 태그를 생성해야 합니다.
    /// </summary>
    [Fact]
    public async Task Handle_ResponseCounterTags_WithGenericErrorCodeExpected_ShouldHaveCorrectErrorCode()
    {
        // Arrange
        var sut = new UsecaseMetricsPipeline<TestCommandRequest, TestResponseWithError>(
            _openTelemetryOptions,
            _meterFactory,
            _sloConfigurationOptions);
        var request = new TestCommandRequest();

        // Act
        await sut.Handle(request, NextFailWithGenericError, CancellationToken.None);

        // Assert
        var responseMeasurement = _capturedMeasurements
            .FirstOrDefault(m => m.InstrumentName.Contains("responses") && !m.InstrumentName.Contains("requests"));

        responseMeasurement.ShouldNotBeNull();

        // error.type이 "expected"여야 함
        AssertTagValue(responseMeasurement.Tags,
            ObservabilityNaming.OTelAttributes.ErrorType,
            ObservabilityNaming.ErrorTypes.Expected);

        // error.code가 "Test.GenericError"여야 함
        AssertTagValue(responseMeasurement.Tags,
            ObservabilityNaming.CustomAttributes.ErrorCode,
            "Test.GenericError");

        // response.status가 "failure"여야 함
        AssertTagValue(responseMeasurement.Tags,
            ObservabilityNaming.CustomAttributes.ResponseStatus,
            ObservabilityNaming.Status.Failure);

        // 실패: 9개 태그 (requestTags 5개 + slo.latency.exceeded 1개 + response.status 1개 + error.type 1개 + error.code 1개)
        responseMeasurement.Tags.Length.ShouldBe(9);
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
