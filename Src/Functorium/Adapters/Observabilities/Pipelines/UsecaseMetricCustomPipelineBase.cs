using System.Diagnostics.Metrics;
using System.Text.RegularExpressions;

using Functorium.Adapters.Observabilities.Naming;
using Functorium.Applications.Cqrs;

namespace Functorium.Adapters.Observabilities.Pipelines;

/// <summary>
/// Usecase별 개별 Metric을 생성하기 위한 베이스 클래스
/// OpenTelemetryOptions의 ServiceName을 사용하여 동적으로 Meter 이름을 생성합니다.
/// TRequest 타입으로부터 CQRS 타입(Query/Command)을 자동으로 식별합니다.
/// </summary>
/// <typeparam name="TRequest">Request 타입 (IQueryRequest 또는 ICommandRequest 구현)</typeparam>
public abstract partial class UsecaseMetricCustomPipelineBase<TRequest>
{
    // GeneratedRegex for AOT-compiled regex patterns (same as UsecasePipelineBase)
    [GeneratedRegex(@"\.([^.+]+)\+", RegexOptions.Compiled)]
    private static partial Regex PlusPattern();

    [GeneratedRegex(@"^([^+]+)\+", RegexOptions.Compiled)]
    private static partial Regex BeforePlusPattern();

    [GeneratedRegex(@"\.([^.+]+)$", RegexOptions.Compiled)]
    private static partial Regex AfterLastDotPattern();

    protected const string DurationUnit = "ms";
    protected const string CountUnit = "requests";
    protected readonly Meter _meter;
    private readonly string _metricPrefix;

    protected UsecaseMetricCustomPipelineBase(string serviceNamespace, IMeterFactory meterFactory)
    {
        // Meter 이름: {ServiceName}.Application
        string meterName = $"{serviceNamespace}.{ObservabilityNaming.Layers.Application}";
        _meter = meterFactory.Create(meterName);

        // Request 타입으로부터 CQRS 타입 자동 식별
        string cqrsType = GetRequestCqrs();

        // Metric 접두사: application.usecase.{cqrs} (query 또는 command)
        _metricPrefix = $"{ObservabilityNaming.Layers.Application}.{ObservabilityNaming.Categories.Usecase}.{cqrsType}";
    }

    /// <summary>
    /// Request 타입의 인터페이스를 분석하여 CQRS 타입을 식별합니다.
    /// UsecasePipelineBase.GetRequestCqrs와 동일한 로직을 사용합니다.
    /// </summary>
    private static string GetRequestCqrs()
    {
        Type[] interfaces = typeof(TRequest).GetInterfaces();

        if (interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandRequest<>)))
            return ObservabilityNaming.Cqrs.Command;

        if (interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryRequest<>)))
            return ObservabilityNaming.Cqrs.Query;

        return ObservabilityNaming.Cqrs.Unknown;
    }

    /// <summary>
    /// Request 타입의 FullName에서 클래스 이름을 추출합니다.
    /// UsecasePipelineBase.GetRequestHandler와 동일한 로직을 사용합니다.
    /// </summary>
    private static string GetRequestHandler()
    {
        string input = typeof(TRequest).FullName!;

        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // "+"가 있는 경우: ".xxx+"
        var plusMatch = PlusPattern().Match(input);
        if (plusMatch.Success)
        {
            return plusMatch.Groups[1].Value.ToLower();
        }

        // "+"가 있으나 "."이 없는 경우: "^([^+]+)\+"
        var beforePlusMatch = BeforePlusPattern().Match(input);
        if (beforePlusMatch.Success)
        {
            return beforePlusMatch.Groups[1].Value.ToLower();
        }

        // "+"가 없는 경우: ".xxx$"
        var afterLastDotMatch = AfterLastDotPattern().Match(input);
        if (afterLastDotMatch.Success)
        {
            return afterLastDotMatch.Groups[1].Value.ToLower();
        }

        return string.Empty;
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
        string handler = GetRequestHandler();
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
            _histogram.Record(elapsed.TotalMilliseconds);
            GC.SuppressFinalize(this);
        }
    }
}
