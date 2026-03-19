using System.Diagnostics.Metrics;

using Functorium.Adapters.Observabilities.Naming;

namespace Functorium.Adapters.Observabilities.Pipelines;

/// <summary>
/// Usecase별 개별 Metric을 생성하기 위한 베이스 클래스
/// OpenTelemetryOptions의 ServiceName을 사용하여 동적으로 Meter 이름을 생성합니다.
/// TRequest 타입으로부터 CQRS 타입(Query/Command)을 자동으로 식별합니다.
/// </summary>
/// <typeparam name="TRequest">Request 타입 (IQueryRequest 또는 ICommandRequest 구현)</typeparam>
public abstract class UsecaseMetricCustomPipelineBase<TRequest>
    : UsecasePipelineBase<TRequest>, ICustomUsecasePipeline
{
    protected const string DurationUnit = "s";
    protected const string CountUnit = "requests";
    protected readonly Meter _meter;
    private readonly string _metricPrefix;

    protected UsecaseMetricCustomPipelineBase(string serviceNamespace, IMeterFactory meterFactory)
    {
        // Meter 이름: {ServiceName}.Application
        string meterName = $"{serviceNamespace}.{ObservabilityNaming.Layers.Application}";
        _meter = meterFactory.Create(meterName);

        // Request 타입으로부터 카테고리 타입 자동 식별
        string categoryType = GetRequestCategoryType(typeof(TRequest));

        // Metric 접두사: application.usecase.{categoryType} (query 또는 command)
        _metricPrefix = $"{ObservabilityNaming.Layers.Application}.{ObservabilityNaming.Categories.Usecase}.{categoryType}";
    }

    /// <summary>
    /// Handler를 포함하는 Metric 이름을 생성합니다.
    /// 형식: application.usecase.{cqrs}.{handler}.{metricName}
    /// Handler 이름은 Request 타입으로부터 자동으로 추출됩니다.
    /// </summary>
    /// <param name="metricName">메트릭 이름 (예: year_distribution)</param>
    /// <returns>Handler를 포함한 전체 메트릭 이름</returns>
    /// <example>
    /// GetMetricName("year_distribution")
    /// → "application.usecase.query.request.year_distribution"
    /// </example>
    protected string GetMetricName(string metricName)
    {
        string handler = GetRequestHandlerLower();
        return $"{_metricPrefix}.{handler}.{metricName}";
    }

    /// <summary>
    /// Handler를 제외한 CQRS 레벨의 Metric 이름을 생성합니다.
    /// 형식: application.usecase.{cqrs}.{metricName}
    /// 모든 Query/Command를 포괄하는 집계 메트릭에 사용됩니다.
    /// </summary>
    /// <param name="metricName">메트릭 이름 (예: total_count)</param>
    /// <returns>Handler를 제외한 전체 메트릭 이름</returns>
    /// <example>
    /// GetMetricNameWithoutHandler("total_count")
    /// → "application.usecase.query.total_count"
    /// </example>
    protected string GetMetricNameWithoutHandler(string metricName)
    {
        return $"{_metricPrefix}.{metricName}";
    }

    /// <summary>
    /// 요청 처리 시간을 측정하기 위한 헬퍼 클래스
    /// using 구문과 함께 사용하여 자동으로 시간 측정 및 기록
    /// </summary>
    public class RequestDuration : IDisposable
    {
        private readonly long _requestStartTime = TimeProvider.System.GetTimestamp();
        private readonly Histogram<double> _histogram;

        public RequestDuration(Histogram<double> histogram)
        {
            _histogram = histogram;
        }

        public void Dispose()
        {
            TimeSpan elapsed = TimeProvider.System.GetElapsedTime(_requestStartTime);
            _histogram.Record(elapsed.TotalSeconds);
            GC.SuppressFinalize(this);
        }
    }
}
