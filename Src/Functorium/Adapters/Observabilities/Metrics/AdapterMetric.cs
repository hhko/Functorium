using System.Diagnostics;
using System.Diagnostics.Metrics;
using Functorium.Applications.Observabilities;

namespace Functorium.Adapters.Observabilities.Metrics;

public class AdapterMetric : IAdapterMetric, IDisposable
{
    private readonly string _serviceNamespace;

    // requestCategory별 메터 관리
    private readonly Dictionary<string, Meter> _meters = new();
    private readonly Dictionary<string, MetricsSet> _metrics = new();

    private class MetricsSet
    {
        public Counter<long> RequestCounter { get; set; } = null!;

        // 분리된 Response Counter - 성능과 쿼리 효율성 향상
        public Counter<long> ResponseSuccessCounter { get; set; } = null!;
        public Counter<long> ResponseFailureCounter { get; set; } = null!;

        public Histogram<double> DurationHistogram { get; set; } = null!;
    }

    public AdapterMetric(IOpenTelemetryOptions openTelemetryOptions)
    {
        _serviceNamespace = openTelemetryOptions.ServiceNamespace;
    }

    //
    // ---
    // request.layer            | O | X | adapter.db. ...   <- adapter
    // request.category         | O | X | adapter.db. ...   <- db
    // request.handler          | O | O |
    // request.handler.method   | O | O |

    public void Request(Activity? activity, string requestCategory, string requestHandler, string requestHandlerMethod, DateTimeOffset startTimestamp)
    {
        if (activity == null)
        return;

        EnsureMetricsForCategory(requestCategory);
        {
            // requestCategory가 없으면 초기화
            EnsureMetricsForCategory(requestCategory);

            // 요청 카운터 증가
            _metrics[requestCategory].RequestCounter.Add(1,
                new KeyValuePair<string, object?>(
                        ObservabilityFields.Request.TelemetryKeys.Layer, 
                        ObservabilityFields.Request.Layer.Adapter),
                new KeyValuePair<string, object?>(
                        ObservabilityFields.Request.TelemetryKeys.Category, 
                        requestCategory),
                new KeyValuePair<string, object?>(
                        ObservabilityFields.Request.TelemetryKeys.Handler, 
                        requestHandler),
                new KeyValuePair<string, object?>(
                        ObservabilityFields.Request.TelemetryKeys.HandlerMethod, 
                        requestHandlerMethod));
        }
    }

    public void ResponseSuccess(Activity? activity, string requestCategory, double elapsed)
    {
        if (activity != null)
        {
            // requestCategory가 없으면 초기화
            EnsureMetricsForCategory(requestCategory);

            // 태그 설정을 위한 키-값 쌍 생성 (Status 태그 제거 - 분리된 Counter 사용)
            KeyValuePair<string, object?>[] tags = new[]
            {
                new KeyValuePair<string, object?>(
                        ObservabilityFields.Request.TelemetryKeys.Layer, 
                        ObservabilityFields.Request.Layer.Adapter),
                new KeyValuePair<string, object?>(
                        ObservabilityFields.Request.TelemetryKeys.Category, 
                        requestCategory),
                new KeyValuePair<string, object?>(
                        ObservabilityFields.Request.TelemetryKeys.Handler, 
                        activity.GetTagItem(ObservabilityFields.Request.TelemetryKeys.Handler)),
                new KeyValuePair<string, object?>(
                        ObservabilityFields.Request.TelemetryKeys.HandlerMethod, 
                        activity.GetTagItem(ObservabilityFields.Request.TelemetryKeys.HandlerMethod))
            };

            // 성공 응답 카운터 증가
            _metrics[requestCategory].ResponseSuccessCounter.Add(1, tags);

            // 응답 시간 히스토그램 기록 (밀리초를 초로 변환)
            _metrics[requestCategory].DurationHistogram.Record(elapsed / 1000.0, tags);
        }
    }

    public void ResponseFailure(Activity? activity, string requestCategory, double elapsed, Error error)
    {
        if (activity != null)
        {
            // requestCategory가 없으면 초기화
            EnsureMetricsForCategory(requestCategory);

            // 태그 설정을 위한 키-값 쌍 생성 (Status 태그 제거 - 분리된 Counter 사용)
            KeyValuePair<string, object?>[] tags =
            [
                new KeyValuePair<string, object?>(
                        ObservabilityFields.Request.TelemetryKeys.Layer, 
                        ObservabilityFields.Request.Layer.Adapter),
                new KeyValuePair<string, object?>(
                        ObservabilityFields.Request.TelemetryKeys.Category, 
                        requestCategory),
                new KeyValuePair<string, object?>(
                        ObservabilityFields.Request.TelemetryKeys.Handler, 
                        activity.GetTagItem(ObservabilityFields.Request.TelemetryKeys.Handler)),
                new KeyValuePair<string, object?>(
                        ObservabilityFields.Request.TelemetryKeys.HandlerMethod, 
                        activity.GetTagItem(ObservabilityFields.Request.TelemetryKeys.HandlerMethod))
            ];

            // 실패 응답 카운터 증가
            _metrics[requestCategory].ResponseFailureCounter.Add(1, tags);

            // 응답 시간 히스토그램 기록 (밀리초를 초로 변환)
            _metrics[requestCategory].DurationHistogram.Record(elapsed / 1000.0, tags);
        }
    }

    private void EnsureMetricsForCategory(string requestCategory)
    {
        if (!_metrics.ContainsKey(requestCategory))
        {
            InitializeMetricsForCategory(requestCategory);
        }
    }

    private void InitializeMetricsForCategory(string requestCategory)
    {
        // requestCategory별 Meter 생성
        Meter meter = new($"{_serviceNamespace}.{ObservabilityFields.Request.Layer.Adapter}.{requestCategory}");
        _meters[requestCategory] = meter;

        //string metricPrefix = ObservabilityFields.MetricPrefix.Adapter.GetPrefix(requestCategory);

        string categoryLower = requestCategory.ToLower();


        MetricsSet metricsSet = new MetricsSet
        {
            // Counter (Prometheus가 자동으로 _total 접미사 추가)
            //   - adapter_db_requests_total
            //   - adapter_db_responses_success_total
            //   - adapter_db_responses_failure_total
            //
            // Histogram
            //   - adapter_db_duration_seconds

            //Duration of scheduled job execution in seconds
            //Total number of scheduled job response success
            //Total number of scheduled job response failure
            //Total number of scheduled job requests


            RequestCounter = meter.CreateCounter<long>(
                name: ObservabilityFields.Metrics.GetRequest(categoryLower), //$"{metricPrefix}.request",
                description: $"Total number of {requestCategory} op requests",
                unit: "{request}"),

            // 분리된 Response Counter - 성능과 쿼리 효율성 향상
            ResponseSuccessCounter = meter.CreateCounter<long>(
                name: ObservabilityFields.Metrics.GetResponseSuccess(categoryLower), // $"{metricPrefix}.response.success",
                description: $"Total number of {requestCategory} op response success",
                unit: "{response}"),

            ResponseFailureCounter = meter.CreateCounter<long>(
                name: ObservabilityFields.Metrics.GetResponseFailure(categoryLower), // $"{metricPrefix}.response.failure",
                description: $"Total number of {requestCategory} op response failure",
                unit: "{response}"),

            DurationHistogram = meter.CreateHistogram<double>(
                name: ObservabilityFields.Metrics.GetDuration(categoryLower), // $"{metricPrefix}.duration",
                description: $"Duration of {requestCategory} op execution in seconds",
                unit: "s"),
        };
        _metrics[requestCategory] = metricsSet;
    }

    public void Dispose()
    {
        // 모든 requestCategory별 Meter Dispose
        foreach (var meter in _meters.Values)
        {
            meter.Dispose();
        }
        _meters.Clear();
        _metrics.Clear();
    }    
}