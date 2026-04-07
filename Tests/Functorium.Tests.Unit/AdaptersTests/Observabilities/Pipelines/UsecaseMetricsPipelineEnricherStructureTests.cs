using System.Diagnostics.Metrics;

using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Contexts;
using Functorium.Adapters.Observabilities.Pipelines;
using Functorium.Abstractions.Observabilities;
using Functorium.Applications.Usecases;

using Mediator;

using Microsoft.Extensions.Options;

using Serilog.Context;

using MsOptions = Microsoft.Extensions.Options.Options;

using static Functorium.Tests.Unit.Abstractions.Constants.Constants;
using static Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines.TestFixtures;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines;

/// <summary>
/// UsecaseMetricsPipeline + CtxEnricher 통합 메트릭 태그 구조 검증 테스트.
/// CtxEnricherContext.Push로 Push된 MetricsTag ctx.* 필드가
/// MetricsTagContext를 통해 메트릭 태그에 병합되는지 스냅샷으로 고정합니다.
/// </summary>
/// <remarks>
/// <para>
/// Enricher가 추가하는 MetricsTag ctx.* 필드:
/// </para>
/// <code>
/// +----+----------------------------+------------------+---------------------+
/// | #  | ctx 필드                   | CtxPillar        | MetricsTag 포함?   |
/// +----+----------------------------+------------------+---------------------+
/// | 1  | ctx.customer_id            | Default          | No                  |
/// | 2  | ctx.is_express             | All              | Yes                 |
/// | 3  | ctx.item_count             | Default|Value    | No (MetricsValue)   |
/// +----+----------------------------+------------------+---------------------+
/// </code>
/// </remarks>
[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public sealed class UsecaseMetricsPipelineEnricherStructureTests : IDisposable
{
    private readonly IMeterFactory _meterFactory;
    private readonly IOptions<OpenTelemetryOptions> _openTelemetryOptions;
    private readonly MeterListener _listener;
    private readonly List<CapturedMeasurement> _capturedMeasurements;

    public UsecaseMetricsPipelineEnricherStructureTests()
    {
        _meterFactory = new TestMeterFactory();
        _openTelemetryOptions = MsOptions.Create(new OpenTelemetryOptions { ServiceNamespace = "TestUsecaseEnricherMetrics" });

        _capturedMeasurements = [];
        _listener = new MeterListener();
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name.StartsWith("TestUsecaseEnricherMetrics"))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _listener.SetMeasurementEventCallback<double>(OnMeasurementRecordedDouble);
        _listener.Start();

        // CtxEnricherContext.SetPushFactory 설정 — OpenTelemetryBuilder와 동일한 로직
        CtxEnricherContext.SetPushFactory((name, value, pillars) =>
        {
            var disposables = new List<IDisposable>();

            if (pillars.HasFlag(CtxPillar.Logging))
                disposables.Add(LogContext.PushProperty(name, value));

            if (pillars.HasFlag(CtxPillar.Tracing))
                System.Diagnostics.Activity.Current?.SetTag(name, value);

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
        _listener.Dispose();
        // Reset factory to avoid test pollution
        CtxEnricherContext.SetPushFactory(static (_, _, _) => NullDisposable.Instance);
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

    #region Snapshot 태그 구조 테스트

    /// <summary>
    /// Enricher가 Push한 MetricsTag ctx.* 필드가 Request 카운터 태그에 포함되는지 검증합니다.
    /// ctx.is_express (CtxPillar.All → MetricsTag 포함)만 메트릭 태그에 나타나야 합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_Command_RequestTags_WithCtxEnricher()
    {
        // Arrange
        _capturedMeasurements.Clear();
        var enricher = new TestCommandCtx3PillarEnricher();
        var enricherPipeline = new CtxEnricherPipeline<TestCommandRequest, TestResponse>(enricher);
        var metricsPipeline = new UsecaseMetricsPipeline<TestCommandRequest, TestResponse>(
            _openTelemetryOptions,
            _meterFactory);
        var request = new TestCommandRequest("TestName");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        // Act — CtxEnricherPipeline이 MetricsTagContext에 Push → MetricsPipeline이 읽어 병합
        await enricherPipeline.Handle(request,
            (req, ct) => metricsPipeline.Handle(req,
                (_, _) => ValueTask.FromResult(expectedResponse), ct),
            CancellationToken.None);

        // Assert
        var tags = ExtractAndOrderTags("requests");
        await Verify(tags).UseDirectory("Snapshots/CtxEnricher");
    }

    /// <summary>
    /// Enricher가 Push한 MetricsTag ctx.* 필드가 Success Response 카운터 태그에 포함되는지 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_Command_SuccessResponseTags_WithCtxEnricher()
    {
        // Arrange
        _capturedMeasurements.Clear();
        var enricher = new TestCommandCtx3PillarEnricher();
        var enricherPipeline = new CtxEnricherPipeline<TestCommandRequest, TestResponse>(enricher);
        var metricsPipeline = new UsecaseMetricsPipeline<TestCommandRequest, TestResponse>(
            _openTelemetryOptions,
            _meterFactory);
        var request = new TestCommandRequest("TestName");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        // Act
        await enricherPipeline.Handle(request,
            (req, ct) => metricsPipeline.Handle(req,
                (_, _) => ValueTask.FromResult(expectedResponse), ct),
            CancellationToken.None);

        // Assert
        var tags = ExtractAndOrderTags("responses", "requests");
        await Verify(tags).UseDirectory("Snapshots/CtxEnricher");
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
