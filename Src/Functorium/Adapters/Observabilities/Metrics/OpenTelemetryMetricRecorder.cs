using System.Diagnostics;
using System.Diagnostics.Metrics;
using Functorium.Adapters.Observabilities.Abstractions;
using Functorium.Adapters.Observabilities.Naming;
using LanguageExt.Common;

namespace Functorium.Adapters.Observabilities.Metrics;

/// <summary>
/// System.Diagnostics.Metrics를 사용하는 IMetricRecorder 구현체입니다.
/// </summary>
/// <remarks>
/// <para>
/// 메트릭 태그는 <see cref="TagList"/> 구조체를 사용합니다.
/// <c>KeyValuePair&lt;string, object?&gt;[]</c> 배열 대신 구조체를 사용하여
/// 힙 할당을 방지하고 GC 부담을 최소화합니다.
/// </para>
/// </remarks>
public sealed class OpenTelemetryMetricRecorder : IMetricRecorder, IDisposable
{
    private readonly string _serviceNamespace;

    // 카테고리별 Meter 및 메트릭 관리
    private readonly Dictionary<string, Meter> _meters = new();
    private readonly Dictionary<string, MetricsSet> _metrics = new();
    private readonly object _lock = new();

    private sealed class MetricsSet
    {
        public required Counter<long> RequestCounter { get; init; }
        public required Counter<long> ResponseSuccessCounter { get; init; }
        public required Counter<long> ResponseFailureCounter { get; init; }
        public required Histogram<double> DurationHistogram { get; init; }
    }

    public OpenTelemetryMetricRecorder(string serviceNamespace)
    {
        _serviceNamespace = serviceNamespace;
    }

    public void RecordRequest(string category, string handler, string method)
    {
        EnsureMetricsForCategory(category);

        TagList tags = new()
        {
            { ObservabilityNaming.CustomAttributes.RequestLayer, ObservabilityNaming.Layers.Adapter },
            { ObservabilityNaming.CustomAttributes.RequestCategory, category },
            { ObservabilityNaming.CustomAttributes.RequestHandler, handler },
            { ObservabilityNaming.CustomAttributes.RequestHandlerMethod, method }
        };

        _metrics[category].RequestCounter.Add(1, tags);
    }

    public void RecordResponseSuccess(string category, string handler, string method, double elapsedMs)
    {
        EnsureMetricsForCategory(category);

        TagList tags = new()
        {
            { ObservabilityNaming.CustomAttributes.RequestLayer, ObservabilityNaming.Layers.Adapter },
            { ObservabilityNaming.CustomAttributes.RequestCategory, category },
            { ObservabilityNaming.CustomAttributes.RequestHandler, handler },
            { ObservabilityNaming.CustomAttributes.RequestHandlerMethod, method }
        };

        _metrics[category].ResponseSuccessCounter.Add(1, tags);

        // 밀리초를 초로 변환
        _metrics[category].DurationHistogram.Record(elapsedMs / 1000.0, tags);
    }

    public void RecordResponseFailure(string category, string handler, string method, double elapsedMs, Error error)
    {
        EnsureMetricsForCategory(category);

        TagList tags = new()
        {
            { ObservabilityNaming.CustomAttributes.RequestLayer, ObservabilityNaming.Layers.Adapter },
            { ObservabilityNaming.CustomAttributes.RequestCategory, category },
            { ObservabilityNaming.CustomAttributes.RequestHandler, handler },
            { ObservabilityNaming.CustomAttributes.RequestHandlerMethod, method }
        };

        _metrics[category].ResponseFailureCounter.Add(1, tags);

        // 밀리초를 초로 변환
        _metrics[category].DurationHistogram.Record(elapsedMs / 1000.0, tags);
    }

    private void EnsureMetricsForCategory(string category)
    {
        // TryGetValue: 1회 조회로 존재 여부 확인 (ContainsKey + 인덱서는 2회 조회)
        if (_metrics.TryGetValue(category, out _))
            return;

        lock (_lock)
        {
            if (_metrics.TryGetValue(category, out _))
                return;

            InitializeMetricsForCategory(category);
        }
    }

    private void InitializeMetricsForCategory(string category)
    {
        string categoryLower = category.ToLowerInvariant();

        // 카테고리별 Meter 생성
        Meter meter = new($"{_serviceNamespace}.{ObservabilityNaming.Layers.Adapter}.{categoryLower}");
        _meters[category] = meter;

        MetricsSet metricsSet = new()
        {
            RequestCounter = meter.CreateCounter<long>(
                name: ObservabilityNaming.Metrics.AdapterRequest(categoryLower),
                description: $"Total number of {category} op requests",
                unit: "{request}"),

            ResponseSuccessCounter = meter.CreateCounter<long>(
                name: ObservabilityNaming.Metrics.AdapterResponseSuccess(categoryLower),
                description: $"Total number of {category} op response success",
                unit: "{response}"),

            ResponseFailureCounter = meter.CreateCounter<long>(
                name: ObservabilityNaming.Metrics.AdapterResponseFailure(categoryLower),
                description: $"Total number of {category} op response failure",
                unit: "{response}"),

            DurationHistogram = meter.CreateHistogram<double>(
                name: ObservabilityNaming.Metrics.AdapterDuration(categoryLower),
                description: $"Duration of {category} op execution in seconds",
                unit: "s")
        };

        _metrics[category] = metricsSet;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var meter in _meters.Values)
            {
                meter.Dispose();
            }
            _meters.Clear();
            _metrics.Clear();
        }
    }
}
