using System.Diagnostics;
using System.Diagnostics.Metrics;

using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Events;
using Functorium.Testing.Arrangements.Logging;
using Functorium.Tests.Unit.DomainsTests.Entities;

using Mediator;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MsOptions = Microsoft.Extensions.Options.Options;

using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Events;

/// <summary>
/// ObservableDomainEventNotificationPublisher의 Handler Metrics 태그 구조를 검증하는 테스트입니다.
/// </summary>
/// <remarks>
/// <para>
/// 이 테스트는 메트릭 태그 구조가 실수로 변경되는 것을 방지합니다.
/// </para>
/// <para>
/// 메트릭별 태그 구조 비교표:
/// </para>
/// <code>
/// +--------------------------+-------------------+-------------------+-------------------+-------------------+
/// | Tag Key                  | requestCounter    | durationHistogram | responseCounter   | responseCounter   |
/// |                          |                   |                   | (success)         | (failure)         |
/// +--------------------------+-------------------+-------------------+-------------------+-------------------+
/// | request.layer            | "application"     | "application"     | "application"     | "application"     |
/// | request.category         | "usecase"         | "usecase"         | "usecase"         | "usecase"         |
/// | request.category_type    | "event"           | "event"           | "event"           | "event"           |
/// | request.handler          | handler name      | handler name      | handler name      | handler name      |
/// | request.handler_method   | "Handle"          | "Handle"          | "Handle"          | "Handle"          |
/// | response.status          | (none)            | (none)            | "success"         | "failure"         |
/// | error.type               | (none)            | (none)            | (none)            | "expected"/       |
/// |                          |                   |                   |                   | "exceptional"     |
/// | error.code               | (none)            | (none)            | (none)            | error code        |
/// +--------------------------+-------------------+-------------------+-------------------+-------------------+
/// | Total Tags               | 5                 | 5                 | 6                 | 8                 |
/// +--------------------------+-------------------+-------------------+-------------------+-------------------+
/// </code>
/// </remarks>
[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public sealed class DomainEventHandlerMetricsStructureTests : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly IMeterFactory _meterFactory;
    private readonly IOptions<OpenTelemetryOptions> _openTelemetryOptions;
    private readonly MeterListener _listener;
    private readonly List<CapturedMeasurement> _capturedMeasurements;

    public DomainEventHandlerMetricsStructureTests()
    {
        _activitySource = new ActivitySource("Test.DomainEventHandlerMetrics");
        _meterFactory = new TestMeterFactory();
        _openTelemetryOptions = MsOptions.Create(new OpenTelemetryOptions { ServiceNamespace = "TestHandlerMetrics" });

        _capturedMeasurements = [];
        _listener = new MeterListener();
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name.StartsWith("TestHandlerMetrics"))
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
    /// durationHistogram은 requestCounter와 동일한 태그를 가져야 합니다.
    /// </summary>
    [Fact(Skip = ".NET 10 preview: AccessViolationException occurs when calling GetType() on proxy objects")]
    public async Task Handle_DurationTags_ShouldContainSameTagsAsRequestCounter()
    {
        // Arrange
        _capturedMeasurements.Clear();
        using var context = new LogTestContext();
        using var loggerFactory = new TestLoggerFactory(context);
        var sut = new ObservableDomainEventNotificationPublisher(
            _activitySource, loggerFactory, _meterFactory, _openTelemetryOptions);

        var domainEvent = new TestDomainEvent("Test");
        var handler = new TestMetricsDomainEventHandler();
        var handlers = new NotificationHandlers<TestDomainEvent>(
            [handler],
            isArray: true);

        // Act
        await sut.Publish(handlers, domainEvent, CancellationToken.None);

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

    #endregion

    #region Snapshot 태그 구조 테스트

    /// <summary>
    /// Handler Request 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact(Skip = ".NET 10 preview: AccessViolationException occurs when calling GetType() on proxy objects")]
    public async Task Snapshot_Handler_RequestTags()
    {
        // Arrange
        _capturedMeasurements.Clear();
        using var context = new LogTestContext();
        using var loggerFactory = new TestLoggerFactory(context);
        var sut = new ObservableDomainEventNotificationPublisher(
            _activitySource, loggerFactory, _meterFactory, _openTelemetryOptions);

        var domainEvent = new TestDomainEvent("Test");
        var handler = new TestMetricsDomainEventHandler();
        var handlers = new NotificationHandlers<TestDomainEvent>(
            [handler],
            isArray: true);

        // Act
        await sut.Publish(handlers, domainEvent, CancellationToken.None);

        // Assert
        var tags = ExtractAndOrderTags("requests");
        await Verify(tags).UseDirectory("Snapshots/DomainEventHandlerMetricsStructure");
    }

    /// <summary>
    /// Handler Success Response 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact(Skip = ".NET 10 preview: AccessViolationException occurs when calling GetType() on proxy objects")]
    public async Task Snapshot_Handler_SuccessResponse_Tags()
    {
        // Arrange
        _capturedMeasurements.Clear();
        using var context = new LogTestContext();
        using var loggerFactory = new TestLoggerFactory(context);
        var sut = new ObservableDomainEventNotificationPublisher(
            _activitySource, loggerFactory, _meterFactory, _openTelemetryOptions);

        var domainEvent = new TestDomainEvent("Test");
        var handler = new TestMetricsDomainEventHandler();
        var handlers = new NotificationHandlers<TestDomainEvent>(
            [handler],
            isArray: true);

        // Act
        await sut.Publish(handlers, domainEvent, CancellationToken.None);

        // Assert
        var tags = ExtractAndOrderTags("responses", "requests");
        await Verify(tags).UseDirectory("Snapshots/DomainEventHandlerMetricsStructure");
    }

    /// <summary>
    /// Handler Failure Response (Expected Error) 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact(Skip = ".NET 10 preview: AccessViolationException occurs when calling GetType() on proxy objects")]
    public async Task Snapshot_Handler_FailureResponse_ExpectedError_Tags()
    {
        // Arrange
        _capturedMeasurements.Clear();
        using var context = new LogTestContext();
        using var loggerFactory = new TestLoggerFactory(context);
        var sut = new ObservableDomainEventNotificationPublisher(
            _activitySource, loggerFactory, _meterFactory, _openTelemetryOptions);

        var domainEvent = new TestDomainEvent("Test");
        var handler = new TestMetricsDomainEventHandler
        {
            ThrowException = new OperationCanceledException("Operation was cancelled")
        };
        var handlers = new NotificationHandlers<TestDomainEvent>(
            [handler],
            isArray: true);

        // Act
        try
        {
            await sut.Publish(handlers, domainEvent, CancellationToken.None);
        }
        catch (AggregateException)
        {
            // 에러는 무시 - 메트릭 태그 검증이 목적
        }

        // Assert
        var tags = ExtractAndOrderTags("responses", "requests");
        await Verify(tags).UseDirectory("Snapshots/DomainEventHandlerMetricsStructure");
    }

    /// <summary>
    /// Handler Failure Response (Exceptional Error) 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact(Skip = ".NET 10 preview: AccessViolationException occurs when calling GetType() on proxy objects")]
    public async Task Snapshot_Handler_FailureResponse_ExceptionalError_Tags()
    {
        // Arrange
        _capturedMeasurements.Clear();
        using var context = new LogTestContext();
        using var loggerFactory = new TestLoggerFactory(context);
        var sut = new ObservableDomainEventNotificationPublisher(
            _activitySource, loggerFactory, _meterFactory, _openTelemetryOptions);

        var domainEvent = new TestDomainEvent("Test");
        var handler = new TestMetricsDomainEventHandler
        {
            ThrowException = new InvalidOperationException("Database connection failed")
        };
        var handlers = new NotificationHandlers<TestDomainEvent>(
            [handler],
            isArray: true);

        // Act
        try
        {
            await sut.Publish(handlers, domainEvent, CancellationToken.None);
        }
        catch (AggregateException)
        {
            // 에러는 무시 - 메트릭 태그 검증이 목적
        }

        // Assert
        var tags = ExtractAndOrderTags("responses", "requests");
        await Verify(tags).UseDirectory("Snapshots/DomainEventHandlerMetricsStructure");
    }

    /// <summary>
    /// Handler Duration Histogram 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact(Skip = ".NET 10 preview: AccessViolationException occurs when calling GetType() on proxy objects")]
    public async Task Snapshot_Handler_DurationHistogram_Tags()
    {
        // Arrange
        _capturedMeasurements.Clear();
        using var context = new LogTestContext();
        using var loggerFactory = new TestLoggerFactory(context);
        var sut = new ObservableDomainEventNotificationPublisher(
            _activitySource, loggerFactory, _meterFactory, _openTelemetryOptions);

        var domainEvent = new TestDomainEvent("Test");
        var handler = new TestMetricsDomainEventHandler();
        var handlers = new NotificationHandlers<TestDomainEvent>(
            [handler],
            isArray: true);

        // Act
        await sut.Publish(handlers, domainEvent, CancellationToken.None);

        // Assert
        var tags = ExtractAndOrderTags("duration");
        await Verify(tags).UseDirectory("Snapshots/DomainEventHandlerMetricsStructure");
    }

    #endregion

    #region Helper Methods

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

    #endregion

    #region Test Types

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

    /// <summary>
    /// Metrics 테스트용 DomainEvent 핸들러.
    /// </summary>
    private sealed class TestMetricsDomainEventHandler : INotificationHandler<TestDomainEvent>
    {
        public Exception? ThrowException { get; set; }

        public ValueTask Handle(TestDomainEvent notification, CancellationToken cancellationToken)
        {
            if (ThrowException is not null)
            {
                throw ThrowException;
            }

            return ValueTask.CompletedTask;
        }
    }

    #endregion
}
