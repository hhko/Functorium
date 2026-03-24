using System.Diagnostics;
using System.Diagnostics.Metrics;

using Functorium.Abstractions.Errors;
using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Events;
using Functorium.Applications.Events;
using Functorium.Domains.Events;
using Functorium.Tests.Unit.DomainsTests.Entities;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using MsOptions = Microsoft.Extensions.Options.Options;

using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Events;

/// <summary>
/// ObservableDomainEventPublisher의 Metrics 태그 구조를 검증하는 테스트입니다.
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
/// | request.layer            | "adapter"         | "adapter"         | "adapter"         | "adapter"         |
/// | request.category         | "event"           | "event"           | "event"           | "event"           |
/// | request.handler          | handler name      | handler name      | handler name      | handler name      |
/// | request.handler_method   | method name       | method name       | method name       | method name       |
/// | response.status          | (none)            | "success"/        | "success"         | "failure"         |
/// |                          |                   | "failure"         |                   |                   |
/// | error.type               | (none)            | (none)            | (none)            | "expected"/       |
/// |                          |                   |                   |                   | "exceptional"     |
/// | error.code               | (none)            | (none)            | (none)            | error code        |
/// +--------------------------+-------------------+-------------------+-------------------+-------------------+
/// | Total Tags               | 4                 | 5                 | 5                 | 7                 |
/// +--------------------------+-------------------+-------------------+-------------------+-------------------+
/// </code>
/// </remarks>
[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public sealed class DomainEventPublisherMetricsStructureTests : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly IMeterFactory _meterFactory;
    private readonly IOptions<OpenTelemetryOptions> _openTelemetryOptions;
    private readonly MeterListener _listener;
    private readonly List<CapturedMeasurement> _capturedMeasurements;
    private readonly IDomainEventPublisher _mockInner;
    private readonly IDomainEventCollector _mockCollector;
    private readonly ILogger<ObservableDomainEventPublisher> _mockLogger;

    public DomainEventPublisherMetricsStructureTests()
    {
        _activitySource = new ActivitySource("Test.DomainEventPublisherMetrics");
        _meterFactory = new TestMeterFactory();
        _openTelemetryOptions = MsOptions.Create(new OpenTelemetryOptions { ServiceNamespace = "TestPublisherMetrics" });
        _mockInner = Substitute.For<IDomainEventPublisher>();
        _mockCollector = Substitute.For<IDomainEventCollector>();
        _mockCollector.GetTrackedAggregates().Returns(new List<IHasDomainEvents>());
        _mockCollector.GetDirectlyTrackedEvents().Returns(new List<IDomainEvent>());
        _mockLogger = Substitute.For<ILogger<ObservableDomainEventPublisher>>();

        _capturedMeasurements = [];
        _listener = new MeterListener();
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name.StartsWith("TestPublisherMetrics"))
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
    /// durationHistogram은 requestCounter의 태그를 포함해야 합니다.
    /// </summary>
    [Fact]
    public async Task Handle_DurationTags_ShouldContainSameBaseTagsAsRequestCounter()
    {
        // Arrange
        _capturedMeasurements.Clear();
        var sut = new ObservableDomainEventPublisher(
            _activitySource, _mockInner, _mockCollector, _mockLogger, _meterFactory, _openTelemetryOptions);

        var domainEvent = new TestDomainEvent("Test");
        _mockInner
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Succ<IO, LanguageExt.Unit>(LanguageExt.Unit.Default));

        // Act
        await sut.Publish(domainEvent).Run().RunAsync();

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

    #endregion

    #region Snapshot 태그 구조 테스트

    /// <summary>
    /// Publish 메서드 Request 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_Publish_RequestTags()
    {
        // Arrange
        _capturedMeasurements.Clear();
        var sut = new ObservableDomainEventPublisher(
            _activitySource, _mockInner, _mockCollector, _mockLogger, _meterFactory, _openTelemetryOptions);

        var domainEvent = new TestDomainEvent("Test");
        _mockInner
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Succ<IO, LanguageExt.Unit>(LanguageExt.Unit.Default));

        // Act
        await sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        var tags = ExtractAndOrderTags("requests");
        await Verify(tags).UseDirectory("Snapshots/Metrics");
    }

    /// <summary>
    /// Publish 메서드 Success Response 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_Publish_SuccessResponse_Tags()
    {
        // Arrange
        _capturedMeasurements.Clear();
        var sut = new ObservableDomainEventPublisher(
            _activitySource, _mockInner, _mockCollector, _mockLogger, _meterFactory, _openTelemetryOptions);

        var domainEvent = new TestDomainEvent("Test");
        _mockInner
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Succ<IO, LanguageExt.Unit>(LanguageExt.Unit.Default));

        // Act
        await sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        var tags = ExtractAndOrderTags("responses", "requests");
        await Verify(tags).UseDirectory("Snapshots/Metrics");
    }

    /// <summary>
    /// Publish 메서드 Failure Response (Expected Error) 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_Publish_FailureResponse_ExpectedError_Tags()
    {
        // Arrange
        _capturedMeasurements.Clear();
        var sut = new ObservableDomainEventPublisher(
            _activitySource, _mockInner, _mockCollector, _mockLogger, _meterFactory, _openTelemetryOptions);

        var domainEvent = new TestDomainEvent("Test");
        var error = new ErrorCodeExpected("Event.NotFound", "testValue", "Event not found");
        _mockInner
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Fail<IO, LanguageExt.Unit>(error));

        // Act
        await sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        var tags = ExtractAndOrderTags("responses", "requests");
        await Verify(tags).UseDirectory("Snapshots/Metrics");
    }

    /// <summary>
    /// Publish 메서드 Failure Response (Exceptional Error) 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_Publish_FailureResponse_ExceptionalError_Tags()
    {
        // Arrange
        _capturedMeasurements.Clear();
        var sut = new ObservableDomainEventPublisher(
            _activitySource, _mockInner, _mockCollector, _mockLogger, _meterFactory, _openTelemetryOptions);

        var domainEvent = new TestDomainEvent("Test");
        var exception = new InvalidOperationException("Connection failed");
        var error = new ErrorCodeExceptional("Event.ConnectionError", exception);
        _mockInner
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Fail<IO, LanguageExt.Unit>(error));

        // Act
        await sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        var tags = ExtractAndOrderTags("responses", "requests");
        await Verify(tags).UseDirectory("Snapshots/Metrics");
    }

    /// <summary>
    /// Publish 메서드 Duration Histogram 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_Publish_DurationHistogram_Tags()
    {
        // Arrange
        _capturedMeasurements.Clear();
        var sut = new ObservableDomainEventPublisher(
            _activitySource, _mockInner, _mockCollector, _mockLogger, _meterFactory, _openTelemetryOptions);

        var domainEvent = new TestDomainEvent("Test");
        _mockInner
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Succ<IO, LanguageExt.Unit>(LanguageExt.Unit.Default));

        // Act
        await sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        var tags = ExtractAndOrderTags("duration");
        await Verify(tags).UseDirectory("Snapshots/Metrics");
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

    #endregion
}
